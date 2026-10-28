namespace IndexCities.Simulation;

// PR57 #275: observational, over-inclusive, PARTIAL projection of potential
// external invalidators before actual dock helpers. Never used for physics.
// Omits transient writes, list contents, road/signal edits, and many other
// fields; hash collisions are possible. Not an exact causal certificate.
public sealed partial class CitySimulation
{
    static readonly bool Pr57275Enabled =
        Environment.GetEnvironmentVariable("PR57_275_FINGERPRINT") == "1";
    sealed class Pr57275Row
    {
        public int[]? Vehicles, Citizens;
        public double Clock;
        public long Calls, Compared, VehicleChanges, CitizenChanges;
        public long VehicleDirtyCalls, CitizenDirtyCalls, UnchangedCalls;
        public long ClockChanged, Ticks, Bytes;
    }
    // 2 windows x 3 target IDs x 2 helpers
    readonly Pr57275Row[] _pr57275Rows =
        Enumerable.Range(0, 12).Select(_ => new Pr57275Row()).ToArray();

    static int Pr57275VehicleHash(Vehicle o)
    {
        var h = new HashCode();
        h.Add(o.Id); h.Add(o.Position); h.Add(o.Heading);
        h.Add(o.Status); h.Add(o.RouteIndex); h.Add(o.WaitingSince);
        h.Add(o.DockPosition); h.Add(o.ManoeuvreDock);
        h.Add(o.IntersectionClaim); h.Add(o.ManoeuvreSpaces.Count);
        h.Add(o.MovementAdmission?.EndRouteIndex);
        h.Add(o.MovementAdmission?.ReachesDock);
        h.Add(o.Route.Count); h.Add(o.SitePathIndex);
        return h.ToHashCode();
    }
    static int Pr57275CitizenHash(Citizen c)
    {
        var h = new HashCode();
        h.Add(c.Id); h.Add(c.Position); h.Add(c.Alive);
        h.Add(c.Moving); h.Add(c.VehicleId); h.Add(c.CrossingNodeId);
        return h.ToHashCode();
    }
    void Pr57275Observe(Vehicle reader, int slot, int helper)
    {
        if (!Pr57275Enabled) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var row = _pr57275Rows[_pr57273Window * 6 + slot * 2 + helper];
        var compare = row.Vehicles != null && row.Citizens != null &&
            row.Vehicles.Length == State.Vehicles.Count &&
            row.Citizens.Length == State.Citizens.Count;
        if (!compare)
        {
            row.Vehicles = new int[State.Vehicles.Count];
            row.Citizens = new int[State.Citizens.Count];
        }
        int vehicles = 0, citizens = 0;
        for (var i = 0; i < State.Vehicles.Count; i++)
        {
            var o = State.Vehicles[i];
            var hash = Pr57275VehicleHash(o);
            if (compare && o.Id != reader.Id && row.Vehicles![i] != hash) vehicles++;
            row.Vehicles![i] = hash;
        }
        for (var i = 0; i < State.Citizens.Count; i++)
        {
            var hash = Pr57275CitizenHash(State.Citizens[i]);
            if (compare && row.Citizens![i] != hash) citizens++;
            row.Citizens![i] = hash;
        }
        if (compare)
        {
            row.Compared++;
            row.VehicleChanges += vehicles; row.CitizenChanges += citizens;
            if (vehicles != 0) row.VehicleDirtyCalls++;
            if (citizens != 0) row.CitizenDirtyCalls++;
            if (vehicles == 0 && citizens == 0) row.UnchangedCalls++;
            if (row.Clock != VehicleMovementTime) row.ClockChanged++;
        }
        row.Clock = VehicleMovementTime;
        row.Calls++;
        row.Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        row.Bytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
    }
    void Pr57275Report()
    {
        if (!Pr57275Enabled) return;
        for (var w = 0; w < 2; w++)
        for (var s = 0; s < 3; s++)
        for (var h = 0; h < 2; h++)
        {
            var row = _pr57275Rows[w * 6 + s * 2 + h];
            Console.WriteLine(
                $"PR57_275_ROW window={w} id={Pr57273Ids[s]} " +
                $"helper={(h == 0 ? "AdmitManoeuvre" : "BayEntryHold")} " +
                $"calls={row.Calls} compared={row.Compared} " +
                $"vehicleChanges={row.VehicleChanges} citizenChanges={row.CitizenChanges} " +
                $"vehicleDirtyCalls={row.VehicleDirtyCalls} citizenDirtyCalls={row.CitizenDirtyCalls} " +
                $"unchangedCalls={row.UnchangedCalls} clockChanged={row.ClockChanged} " +
                $"probeMs={row.Ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F6} " +
                $"probeBytes={row.Bytes} partialProjection=True notCausalProof=True " +
                "noReuse=True noSkip=True");
        }
    }
}
