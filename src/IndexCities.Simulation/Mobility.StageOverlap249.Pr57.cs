namespace IndexCities.Simulation;

// PR57 #249: bounded, read-only overlap census between the *actual* s3
// occupancy/forward candidate paths and the s4 bend winners, inside the
// same actor iteration. A matching selected vehicle is not a causal proof:
// rejected candidates, roads, signals, pedestrians, reservations, clocks,
// transient writers and external edits are outside this projection.
// This probe never reuses a result, changes a decision or skips an actor.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Stage249Enabled =
        Environment.GetEnvironmentVariable("PR57_249_STAGE_OVERLAP") == "1";

    readonly record struct Pr57Stage249State(
        long X, long Z, long Heading, VehicleStatus Status, int RouteIndex,
        int? Claim, TowPhase TowPhase, long Waiting,
        bool HasDock, long DockX, long DockZ);

    static Pr57Stage249State Pr57Stage249Snapshot(Vehicle v)
    {
        static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);
        var dock = v.DockPosition;
        return new(Bits(v.Position.X), Bits(v.Position.Z), Bits(v.Heading),
            v.Status, v.RouteIndex, v.IntersectionClaim, v.TowPhase,
            Bits(v.WaitingSince), dock.HasValue,
            dock is { } p ? Bits(p.X) : 0,
            dock is { } q ? Bits(q.Z) : 0);
    }

    readonly Dictionary<int, Pr57Stage249State> _pr57Stage249Tier3 = new();
    readonly Dictionary<int, Pr57Stage249State> _pr57Stage249Tier9 = new();
    bool _pr57Stage249Active, _pr57Stage249LaneOpen;
    long _pr57Stage249Eligible, _pr57Stage249Samples, _pr57Stage249Queries;
    long _pr57Stage249LaneVisits, _pr57Stage249Tier3Calls, _pr57Stage249Tier9Calls;
    long _pr57Stage249BendVisits, _pr57Stage249BendWinners;
    long _pr57Stage249Overlap3, _pr57Stage249Overlap9;
    long _pr57Stage249Same3, _pr57Stage249Same9;
    long _pr57Stage249ProbeTicks, _pr57Stage249ProbeBytes;

    void Pr57Stage249BeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57Stage249Active = false;
        _pr57Stage249LaneOpen = false;
        if (!Pr57Stage249Enabled || !profiled || dt <= 0 || operationsDt is not null) return;
        _pr57Stage249Eligible++;
        // Same three consecutive physical slices as #206/#244; GRID uses
        // one physical slice per Advance(2), so no 300-call reporting gate.
        if (_pr57Stage249Eligible is < 37 or > 39) return;
        _pr57Stage249Active = true;
        _pr57Stage249Samples++;
        _pr57Stage249Queries = _pr57Stage249LaneVisits = 0;
        _pr57Stage249Tier3Calls = _pr57Stage249Tier9Calls = 0;
        _pr57Stage249BendVisits = _pr57Stage249BendWinners = 0;
        _pr57Stage249Overlap3 = _pr57Stage249Overlap9 = 0;
        _pr57Stage249Same3 = _pr57Stage249Same9 = 0;
        _pr57Stage249ProbeTicks = _pr57Stage249ProbeBytes = 0;
    }

    void Pr57Stage249BeginLane(int candidateCount)
    {
        if (!_pr57Stage249Active) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57Stage249Tier3.Clear();
        _pr57Stage249Tier9.Clear();
        _pr57Stage249LaneOpen = true;
        _pr57Stage249Queries++;
        _pr57Stage249LaneVisits += candidateCount;
        _pr57Stage249ProbeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57Stage249ProbeBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57Stage249Lane(Vehicle candidate, int tier)
    {
        if (!_pr57Stage249Active || !_pr57Stage249LaneOpen) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var snapshot = Pr57Stage249Snapshot(candidate);
        if (tier == 3)
        {
            _pr57Stage249Tier3Calls++;
            _pr57Stage249Tier3[candidate.Id] = snapshot;
        }
        else
        {
            _pr57Stage249Tier9Calls++;
            _pr57Stage249Tier9[candidate.Id] = snapshot;
        }
        _pr57Stage249ProbeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57Stage249ProbeBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57Stage249FinishBend(int candidates, List<Vehicle> selected)
    {
        if (!_pr57Stage249Active || !_pr57Stage249LaneOpen) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57Stage249BendVisits += candidates;
        _pr57Stage249BendWinners += selected.Count;
        foreach (var other in selected)
        {
            var current = Pr57Stage249Snapshot(other);
            if (_pr57Stage249Tier3.TryGetValue(other.Id, out var tier3))
            {
                _pr57Stage249Overlap3++;
                if (tier3 == current) _pr57Stage249Same3++;
            }
            if (_pr57Stage249Tier9.TryGetValue(other.Id, out var tier9))
            {
                _pr57Stage249Overlap9++;
                if (tier9 == current) _pr57Stage249Same9++;
            }
        }
        _pr57Stage249LaneOpen = false;
        _pr57Stage249ProbeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57Stage249ProbeBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57Stage249EndSlice()
    {
        if (!_pr57Stage249Active) return;
        _pr57Stage249Active = _pr57Stage249LaneOpen = false;
        _pr57Stage249Tier3.Clear();
        _pr57Stage249Tier9.Clear();
        Console.WriteLine(
            $"PR57_249_STAGE_OVERLAP sample={_pr57Stage249Samples} " +
            $"laneQueries={_pr57Stage249Queries} laneVisits={_pr57Stage249LaneVisits} " +
            $"tier3Calls={_pr57Stage249Tier3Calls} tier9Calls={_pr57Stage249Tier9Calls} " +
            $"bendVisits={_pr57Stage249BendVisits} bendWinners={_pr57Stage249BendWinners} " +
            $"tier3Overlap={_pr57Stage249Overlap3} tier9Overlap={_pr57Stage249Overlap9} " +
            $"sameProjected3={_pr57Stage249Same3} sameProjected9={_pr57Stage249Same9} " +
            $"probeMs={_pr57Stage249ProbeTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F5} " +
            $"probeMB={_pr57Stage249ProbeBytes / 1e6:F5} " +
            "scope=THREE_PHYSICAL_SLICES selectedBendOnly=True " +
            "projectionNotCausalClosure=True noReuse=True observational=True intrusive=True");
    }
}
