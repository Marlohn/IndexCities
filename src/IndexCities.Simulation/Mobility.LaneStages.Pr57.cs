namespace IndexCities.Simulation;

// PR57 #204: observational cost/guard census of actual lane scans in bounded
// physical sub-slices. Does NOT cache, filter, reorder, or alter any decision.
// Timings are intrusive and include the cost of these probes; they cannot be
// interpreted as an A/B speedup or a rigorous exclusive CPU breakdown.
public sealed partial class CitySimulation
{
    static readonly bool Pr57StageProbeEnabled =
        Environment.GetEnvironmentVariable("PR57_204_LANE_STAGES") == "1";

    readonly long[] _pr57StageCounts = new long[10];
    // 0 = full foreach scan, 1 = BodyAcrossLane, 2 = OccupiesLane,
    // 3 = BlocksLaneEntry, 4 = EntryFrameVehicle.
    readonly long[] _pr57StageTicks = new long[5];
    readonly long[] _pr57StageCalls = new long[5];
    readonly HashSet<int> _pr57StageActors = new();
    long _pr57StageEligibleSlices, _pr57StageSamples, _pr57StageScans;
    bool _pr57StageActive;

    void Pr57StageBeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57StageActive = false;
        if (!Pr57StageProbeEnabled || !profiled || dt <= 0 || operationsDt is not null) return;
        _pr57StageEligibleSlices++;
        if (_pr57StageSamples >= 3 || _pr57StageEligibleSlices % 37 != 0) return;
        _pr57StageSamples++;
        _pr57StageActive = true;
        Array.Clear(_pr57StageCounts);
        Array.Clear(_pr57StageTicks);
        Array.Clear(_pr57StageCalls);
        _pr57StageActors.Clear();
        _pr57StageScans = 0;
    }

    void Pr57StageMark(int stage)
    {
        if (_pr57StageActive) _pr57StageCounts[stage]++;
    }

    long Pr57StageScanBegin(Vehicle actor)
    {
        if (!_pr57StageActive) return 0;
        _pr57StageActors.Add(actor.Id);
        _pr57StageScans++;
        return System.Diagnostics.Stopwatch.GetTimestamp();
    }

    void Pr57StageScanEnd(long start)
    {
        if (start != 0)
        {
            _pr57StageCalls[0]++;
            _pr57StageTicks[0] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        }
    }

    double? Pr57StageBodyAcross(Vehicle actor, Vehicle other)
    {
        if (!_pr57StageActive) return BodyAcrossLane(actor, other);
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try { return BodyAcrossLane(actor, other); }
        finally
        {
            _pr57StageCalls[1]++;
            _pr57StageTicks[1] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        }
    }

    bool Pr57StageOccupies(Vehicle other)
    {
        if (!_pr57StageActive) return OccupiesLane(other);
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try { return OccupiesLane(other); }
        finally
        {
            _pr57StageCalls[2]++;
            _pr57StageTicks[2] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        }
    }

    bool Pr57StageBlocksEntry(Vehicle actor, LaneAccess access, Vehicle other)
    {
        if (!_pr57StageActive) return BlocksLaneEntry(actor, access, other);
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try { return BlocksLaneEntry(actor, access, other); }
        finally
        {
            _pr57StageCalls[3]++;
            _pr57StageTicks[3] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        }
    }

    Vehicle Pr57StageEntryFrame(Vehicle other)
    {
        if (!_pr57StageActive) return EntryFrameVehicle(other);
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try { return EntryFrameVehicle(other); }
        finally
        {
            _pr57StageCalls[4]++;
            _pr57StageTicks[4] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        }
    }

    static double Pr57StageMs(long ticks) =>
        Math.Round(ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency, 6);

    void Pr57StageEndSlice()
    {
        if (!_pr57StageActive) return;
        _pr57StageActive = false;
        Console.WriteLine(
            $"PR57_204_STAGE sample={_pr57StageSamples} eligibleSlice={_pr57StageEligibleSlices} " +
            $"scans={_pr57StageScans} distinctActors={_pr57StageActors.Count} " +
            $"visits={_pr57StageCounts[0]} bodyHits={_pr57StageCounts[1]} " +
            $"earlyReject={_pr57StageCounts[2]} occupied={_pr57StageCounts[3]} " +
            $"coincident={_pr57StageCounts[4]} entryBlocked={_pr57StageCounts[5]} " +
            $"reachedGate={_pr57StageCounts[6]} forwardReject={_pr57StageCounts[7]} " +
            $"sideReject={_pr57StageCounts[8]} reachedHeading={_pr57StageCounts[9]} " +
            $"scanMs={Pr57StageMs(_pr57StageTicks[0])} " +
            $"bodyMs={Pr57StageMs(_pr57StageTicks[1])} bodyCalls={_pr57StageCalls[1]} " +
            $"occupiesMs={Pr57StageMs(_pr57StageTicks[2])} occupiesCalls={_pr57StageCalls[2]} " +
            $"entryMs={Pr57StageMs(_pr57StageTicks[3])} entryCalls={_pr57StageCalls[3]} " +
            $"frameMs={Pr57StageMs(_pr57StageTicks[4])} frameCalls={_pr57StageCalls[4]} " +
            "scope=actual-lane-candidates observational=True reusableDecisionProven=False");
        _pr57StageActors.Clear();
    }
}
