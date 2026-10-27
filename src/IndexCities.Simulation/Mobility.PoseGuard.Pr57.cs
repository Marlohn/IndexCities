namespace IndexCities.Simulation;

// PR57 #207: observational subdivision of the s6 post-safety region.
// It never decides whether a vehicle may move, and never suppresses a slice.
// Only samples the same three consecutive physical slices as #206.
public sealed partial class CitySimulation
{
    static readonly bool Pr57PoseGuardEnabled =
        Environment.GetEnvironmentVariable("PR57_207_POSE_GUARD") == "1";

    long _pr57PoseCalls, _pr57PoseEligible, _pr57PoseCandidates, _pr57PoseHeadingChanges;
    long _pr57PoseTicks, _pr57PoseBytes;
    long _pr57PoseStartTicks, _pr57PoseStartBytes;
    double _pr57PoseOldHeading;
    bool _pr57PoseActive;

    void Pr57PoseGuardReset()
    {
        _pr57PoseCalls = _pr57PoseEligible = _pr57PoseCandidates = _pr57PoseHeadingChanges = 0;
        _pr57PoseTicks = _pr57PoseBytes = 0;
        _pr57PoseActive = false;
    }

    void Pr57PoseGuardBegin(Vehicle vehicle, double step, double d)
    {
        _pr57PoseActive = Pr57PoseGuardEnabled && _pr57ExclusiveActive;
        if (!_pr57PoseActive) return;
        _pr57PoseCalls++;
        if (step < .00001 && d > .00001) _pr57PoseEligible++;
        _pr57PoseOldHeading = vehicle.Heading;
        _pr57PoseStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57PoseStartBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    // Called only after the original ID, status and distance short-circuits.
    // Returning true preserves the original predicate; this is not a blocker count.
    bool Pr57PoseGuardCandidateReached()
    {
        if (_pr57PoseActive) _pr57PoseCandidates++;
        return true;
    }

    void Pr57PoseGuardEnd(Vehicle vehicle)
    {
        if (!_pr57PoseActive) return;
        _pr57PoseTicks += System.Diagnostics.Stopwatch.GetTimestamp() - _pr57PoseStartTicks;
        _pr57PoseBytes += GC.GetAllocatedBytesForCurrentThread() - _pr57PoseStartBytes;
        if (vehicle.Heading != _pr57PoseOldHeading) _pr57PoseHeadingChanges++;
        _pr57PoseActive = false;
    }

    void Pr57PoseGuardReport()
    {
        if (!Pr57PoseGuardEnabled || !_pr57ExclusiveActive) return;
        var ms = _pr57PoseTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_207_POSE sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
            $"calls={_pr57PoseCalls} eligible={_pr57PoseEligible} geometryCandidates={_pr57PoseCandidates} " +
            $"headingChanges={_pr57PoseHeadingChanges} guardMs={ms:F6} guardMB={_pr57PoseBytes / 1e6:F6} " +
            "scope=s6-pose-guard-only observational=True intrusive=True");
    }
}
