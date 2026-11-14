namespace IndexCities.Simulation;

// PR57 #310: phase-boundary census of actual persistent physical/link changes.
// Observational only: does not decide traffic, skip work, or enter saves.
// Net differences across a phase are NOT a complete mutation journal.
public sealed partial class CitySimulation
{
    static readonly bool Pr57310Enabled =
        Environment.GetEnvironmentVariable("PR57_310_PHASE_FANOUT") == "1";
    readonly record struct Pr57310Vehicle(
        Pr57306BodyRow Body, int RouteIndex, int RouteCount,
        int? IntersectionClaim, int? DriverId, int PassengerCount,
        Point? DockPosition, double WaitingSince, string? Cause);
    readonly record struct Pr57310Citizen(
        long X, long Z, int? VehicleId, int? CrossingNodeId);
    readonly record struct Pr57310Snapshot(
        Dictionary<int, Pr57310Vehicle> Vehicles,
        Dictionary<int, Pr57310Citizen> Citizens, int RoadRevision);
    int _pr57310Calls;
    bool _pr57310Active;
    Pr57310Snapshot _pr57310Before;
    long _pr57310Ticks, _pr57310Bytes;

    Pr57310Snapshot Pr57310Capture()
    {
        var v = new Dictionary<int, Pr57310Vehicle>(State.Vehicles.Count);
        foreach (var o in State.Vehicles)
            v.Add(o.Id, new Pr57310Vehicle(Pr57306Capture(o),
                o.RouteIndex, o.Route.Count, o.IntersectionClaim, o.DriverId,
                o.Passengers.Count, o.DockPosition, o.WaitingSince, o.Cause));
        var c = new Dictionary<int, Pr57310Citizen>(State.Citizens.Count);
        foreach (var o in State.Citizens)
            c.Add(o.Id, new Pr57310Citizen(
                BitConverter.DoubleToInt64Bits(o.Position.X),
                BitConverter.DoubleToInt64Bits(o.Position.Z),
                o.VehicleId, o.CrossingNodeId));
        return new Pr57310Snapshot(v, c, State.RoadRevision);
    }

    void Pr57310Begin()
    {
        if (!Pr57310Enabled) return;
        _pr57310Calls++;
        var start = int.TryParse(Environment.GetEnvironmentVariable("PR57_310_START"), out var n) ? n : 76;
        _pr57310Active = _pr57310Calls >= start;
        if (!_pr57310Active) return;
        _pr57310Ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57310Bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57310Before = Pr57310Capture();
    }

    void Pr57310Boundary(string phase)
    {
        if (!_pr57310Active) return;
        var after = Pr57310Capture();
        int vChanged = 0, vBody = 0, vRoute = 0, vClaim = 0, vDriver = 0;
        int cChanged = 0, cPose = 0, cVehicle = 0, cCrossing = 0;
        foreach (var (id, current) in after.Vehicles)
        {
            if (!_pr57310Before.Vehicles.TryGetValue(id, out var previous))
            {
                vChanged++; vBody++; continue;
            }
            if (current == previous) continue;
            vChanged++;
            if (current.Body != previous.Body) vBody++;
            if (current.RouteIndex != previous.RouteIndex ||
                current.RouteCount != previous.RouteCount) vRoute++;
            if (current.IntersectionClaim != previous.IntersectionClaim) vClaim++;
            if (current.DriverId != previous.DriverId ||
                current.PassengerCount != previous.PassengerCount) vDriver++;
        }
        foreach (var (id, current) in after.Citizens)
        {
            if (!_pr57310Before.Citizens.TryGetValue(id, out var previous))
            {
                cChanged++; cPose++; continue;
            }
            if (current == previous) continue;
            cChanged++;
            if (current.X != previous.X || current.Z != previous.Z) cPose++;
            if (current.VehicleId != previous.VehicleId) cVehicle++;
            if (current.CrossingNodeId != previous.CrossingNodeId) cCrossing++;
        }
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp() - _pr57310Ticks;
        var bytes = GC.GetAllocatedBytesForCurrentThread() - _pr57310Bytes;
        Console.WriteLine(
            $"PR57_310_PHASE call={_pr57310Calls} phase={phase} " +
            $"vehicles={after.Vehicles.Count} citizens={after.Citizens.Count} " +
            $"vehicleChanged={vChanged} bodyChanged={vBody} routeChanged={vRoute} " +
            $"claimChanged={vClaim} driverChanged={vDriver} " +
            $"citizenChanged={cChanged} citizenPose={cPose} citizenVehicle={cVehicle} " +
            $"citizenCrossing={cCrossing} roadRevisionChanged={after.RoadRevision != _pr57310Before.RoadRevision} " +
            $"probeMs={ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F5} " +
            $"probeBytes={bytes} scope=NET_PHASE_DIFF_NOT_CAUSAL_JOURNAL");
        _pr57310Before = after;
        _pr57310Ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57310Bytes = GC.GetAllocatedBytesForCurrentThread();
        if (phase == "citizens") _pr57310Active = false;
    }
}
