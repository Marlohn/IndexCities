namespace IndexCities.Simulation;

// PR57 #259: bounded observational census of the ACTUAL pedestrian admission
// gate. Counts short-circuit visits and stopwatch time; never substitutes a
// decision, precomputes an occupancy, or skips a movement slice. The counter
// and stopwatch are intrusive; reported time is an upper bound on the original
// predicate's cost, not a benchmark of an optimization.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Crossing259Enabled =
        Environment.GetEnvironmentVariable("PR57_259_CROSSING_GATE") == "1";
    bool _pr57Crossing259Active;
    long _pr57Crossing259Calls, _pr57Crossing259Hits;
    long _pr57Crossing259Visits, _pr57Crossing259Potential;
    long _pr57Crossing259PositiveVisits, _pr57Crossing259Ticks;
    int _pr57Crossing259MaxVisits;
    readonly HashSet<int> _pr57Crossing259Nodes = new();

    void Pr57Crossing259BeginSlice()
    {
        _pr57Crossing259Active = Pr57Crossing259Enabled && _pr57ExclusiveActive;
        if (!_pr57Crossing259Active) return;
        _pr57Crossing259Calls = _pr57Crossing259Hits = 0;
        _pr57Crossing259Visits = _pr57Crossing259Potential = 0;
        _pr57Crossing259PositiveVisits = _pr57Crossing259Ticks = 0;
        _pr57Crossing259MaxVisits = 0;
        _pr57Crossing259Nodes.Clear();
    }

    // The same predicate, same list order and same short-circuit semantics
    // as the canonical State.Citizens.Any in MayEnterJunction. The disabled
    // branch executes the original expression without an extra census.
    bool Pr57Crossing259Check(RoadNode junction)
    {
        if (!_pr57Crossing259Active)
        {
            if (!Pr57Crossing292Enabled)
                return State.Citizens.Any(c => c.Alive && c.VehicleId == null &&
                    c.Moving && c.CrossingNodeId == junction.Id &&
                    c.Position.Distance(junction.Position) <
                        State.Tuning.RoadWidth / 2 + 1.5);
            // Observational only: preserve original Any order and short circuit.
            var start292 = System.Diagnostics.Stopwatch.GetTimestamp();
            var visits292 = 0;
            var blocked292 = State.Citizens.Any(c =>
            {
                visits292++;
                return c.Alive && c.VehicleId == null && c.Moving &&
                    c.CrossingNodeId == junction.Id &&
                    c.Position.Distance(junction.Position) <
                        State.Tuning.RoadWidth / 2 + 1.5;
            });
            Pr57Crossing292Read(junction.Id,blocked292,visits292,start292);
            return blocked292;
        }

        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var visits = 0;
        var blocked = State.Citizens.Any(c =>
        {
            visits++;
            return c.Alive && c.VehicleId == null &&
                c.Moving && c.CrossingNodeId == junction.Id &&
                c.Position.Distance(junction.Position) <
                    State.Tuning.RoadWidth / 2 + 1.5;
        });
        _pr57Crossing259Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57Crossing259Calls++;
        _pr57Crossing259Visits += visits;
        _pr57Crossing259Potential += State.Citizens.Count;
        _pr57Crossing259MaxVisits = Math.Max(_pr57Crossing259MaxVisits, visits);
        _pr57Crossing259Nodes.Add(junction.Id);
        if (blocked)
        {
            _pr57Crossing259Hits++;
            _pr57Crossing259PositiveVisits += visits;
        }
        return blocked;
    }

    void Pr57Crossing259EndSlice()
    {
        if (!_pr57Crossing259Active) return;
        var ms = _pr57Crossing259Ticks * 1000.0 /
                 System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_259_CROSSING sample={_pr57ExclusiveSamples} " +
            $"calls={_pr57Crossing259Calls} blocked={_pr57Crossing259Hits} " +
            $"visits={_pr57Crossing259Visits} potentialVisits={_pr57Crossing259Potential} " +
            $"positiveVisits={_pr57Crossing259PositiveVisits} " +
            $"maxVisits={_pr57Crossing259MaxVisits} " +
            $"distinctNodes={_pr57Crossing259Nodes.Count} elapsedMs={ms:F6} " +
            "scope=REACHED_MAY_ENTER_PEDESTRIAN_GATE_ONLY " +
            "sameCanonicalAny=True observationIntrusive=True " +
            "noCache=True noSkippedSlices=True noOptimization=True");
        _pr57Crossing259Active = false;
    }
}
