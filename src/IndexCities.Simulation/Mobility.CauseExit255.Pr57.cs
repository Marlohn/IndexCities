namespace IndexCities.Simulation;

// PR57 #255: bounded observation of the canonical junction exit for two
// authentic queue vehicles. Selected-body snapshots are NOT causal closure.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Cause255Enabled =
        Environment.GetEnvironmentVariable("PR57_255_JUNCTION_EXIT") == "1";
    readonly Dictionary<int, List<string>> _pr57Cause255ActorExits = new();
    readonly Dictionary<int, string> _pr57Cause255Selected = new();
    readonly Dictionary<(int Id, Pr57JunctionExit Reason), long> _pr57Cause255Reasons = new();
    long _pr57Cause255Calls, _pr57Cause255Net, _pr57Cause255MissingBody;

    bool Pr57Cause255Track(Vehicle v) =>
        Pr57Cause255Enabled && _pr57Effect253Active &&
        (v.Id == 1243 || v.Id == 1071) &&
        Pr57State244PersistentIds.Contains(v.Id);

    void Pr57Cause255BeforeActor(Vehicle v)
    {
        if (!Pr57Cause255Track(v)) return;
        if (!_pr57Cause255ActorExits.TryGetValue(v.Id, out var exits))
            _pr57Cause255ActorExits[v.Id] = exits = new List<string>();
        exits.Clear();
        _pr57Cause255Selected.Remove(v.Id);
    }

    void Pr57Cause255SelectedBlocker(Vehicle reader, Vehicle other, string kind)
    {
        if (!Pr57Cause255Track(reader)) return;
        _pr57Cause255Selected[reader.Id] =
            $"id:{other.Id},kind:{kind},status:{other.Status}," +
            $"x:{other.Position.X:F6},z:{other.Position.Z:F6}," +
            $"heading:{other.Heading:F6},claim:{other.IntersectionClaim}";
    }

    void Pr57Cause255Exit(Vehicle reader, RoadNode junction,
        Pr57JunctionExit reason, bool allowed)
    {
        if (!Pr57Cause255Track(reader)) return;
        _pr57Cause255Calls++;
        var key = (reader.Id, reason);
        _pr57Cause255Reasons[key] = _pr57Cause255Reasons.GetValueOrDefault(key) + 1;
        var selected = _pr57Cause255Selected.Remove(reader.Id, out var body) ? body : "none";
        if ((reason is Pr57JunctionExit.BendBody or Pr57JunctionExit.OnwardBody) &&
            selected == "none") _pr57Cause255MissingBody++;
        // Only count pedestrians when the actual canonical return is Pedestrian.
        var walkers = reason == Pr57JunctionExit.Pedestrian
            ? State.Citizens.Count(c => c.Alive && c.VehicleId == null &&
                c.Moving && c.CrossingNodeId == junction.Id &&
                c.Position.Distance(junction.Position) < State.Tuning.RoadWidth / 2 + 1.5)
            : 0;
        _pr57Cause255ActorExits[reader.Id].Add(
            $"node:{junction.Id},reason:{reason},allowed:{allowed}," +
            $"blocker:({selected}),control:{junction.Control}," +
            $"signalClock:{VehicleMovementTime % 60:F3},pedestrians:{walkers}," +
            $"cause:{Uri.EscapeDataString(reader.Cause ?? "")}");
    }

    void Pr57Cause255AfterActor(Vehicle v, string? previousCause)
    {
        if (!Pr57Cause255Track(v) ||
            string.Equals(previousCause, v.Cause, StringComparison.Ordinal)) return;
        _pr57Cause255Net++;
        _pr57Cause255ActorExits.TryGetValue(v.Id, out var exits);
        Console.WriteLine(
            $"PR57_255_CAUSE_EXIT id={v.Id} time={VehicleMovementTime:F6} " +
            $"before={Uri.EscapeDataString(previousCause ?? "")} " +
            $"after={Uri.EscapeDataString(v.Cause ?? "")} " +
            $"exits={exits?.Count ?? 0} path={string.Join("|", exits ?? new List<string>())}");
    }

    void Pr57Cause255Report()
    {
        if (!Pr57Cause255Enabled) return;
        Console.WriteLine(
            $"PR57_255_SUMMARY calls={_pr57Cause255Calls} netCause={_pr57Cause255Net} " +
            $"missingSelectedBody={_pr57Cause255MissingBody} " +
            "scope=two-authentic-vehicles observational=True noCausalClosure=True noSkippedSlices=True");
        foreach (var entry in _pr57Cause255Reasons.OrderBy(x => x.Key.Id).ThenBy(x => x.Key.Reason))
            Console.WriteLine($"PR57_255_REASON id={entry.Key.Id} reason={entry.Key.Reason} calls={entry.Value}");
    }
}
