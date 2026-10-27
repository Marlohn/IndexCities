namespace IndexCities.Simulation;

// PR57 #206: bounded, strictly observational exclusive timing of consecutive
// regions in the physical vehicle executor. No decisions, candidate sets,
// ordering, or slices are changed. Stopwatch and allocation probes are
// intrusive; values are NOT clean A/B timings or universal CPU fractions.
public sealed partial class CitySimulation
{
    static readonly bool Pr57ResidualExclusiveEnabled =
        Environment.GetEnvironmentVariable("PR57_206_RESIDUAL_EXCLUSIVE") == "1";

    // 0 operations/eligibility, 1 route/pose, 2 shared/bay,
    // 3 lane, 4 bend, 5 site/crossing/turn, 6 post-safety,
    // 7 junction admission, 8 drive/exit.
    readonly long[] _pr57ExclusiveTicks = new long[9];
    readonly long[] _pr57ExclusiveBytes = new long[9];
    readonly long[] _pr57ExclusiveSegments = new long[9];
    long _pr57ExclusiveEligibleSlices, _pr57ExclusiveSamples;
    long _pr57ExclusiveVehicles, _pr57ExclusiveTravelling, _pr57ExclusiveMoved;
    bool _pr57ExclusiveActive, _pr57ExclusiveVehicleActive;
    int _pr57ExclusiveStage;
    long _pr57ExclusiveStartTick, _pr57ExclusiveStartBytes;
    Point _pr57ExclusiveOldPosition;

    void Pr57ExclusiveBeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57ExclusiveActive = false;
        if (!Pr57ResidualExclusiveEnabled || !profiled || dt <= 0 || operationsDt is not null) return;
        _pr57ExclusiveEligibleSlices++;
        // Three CONSECUTIVE physical sub-slices; unlike a sparse phase
        // census, this also exposes immediate re-evaluation costs.
        if (_pr57ExclusiveEligibleSlices < 37 || _pr57ExclusiveEligibleSlices > 39) return;
        _pr57ExclusiveSamples++;
        _pr57ExclusiveActive = true;
        Array.Clear(_pr57ExclusiveTicks);
        Array.Clear(_pr57ExclusiveBytes);
        Array.Clear(_pr57ExclusiveSegments);
        _pr57ExclusiveVehicles = _pr57ExclusiveTravelling = _pr57ExclusiveMoved = 0;
        Pr57PoseGuardReset();
        Pr57ExecBeginSlice();
    }

    void Pr57ExclusiveBeginVehicle(Vehicle vehicle)
    {
        if (!_pr57ExclusiveActive) return;
        _pr57ExclusiveVehicleActive = true;
        _pr57ExclusiveStage = 0;
        _pr57ExclusiveStartTick = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57ExclusiveStartBytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57ExclusiveOldPosition = vehicle.Position;
        Pr57ExecBeginVehicle(vehicle);
    }

    void Pr57ExclusiveMark(int nextStage)
    {
        if (!_pr57ExclusiveVehicleActive) return;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57ExclusiveTicks[_pr57ExclusiveStage] += now - _pr57ExclusiveStartTick;
        _pr57ExclusiveBytes[_pr57ExclusiveStage] += bytes - _pr57ExclusiveStartBytes;
        _pr57ExclusiveSegments[_pr57ExclusiveStage]++;
        Pr57ExecRecordSegment(_pr57ExclusiveStage,
            now - _pr57ExclusiveStartTick, bytes - _pr57ExclusiveStartBytes);
        Pr57ExecMark(nextStage);
        _pr57ExclusiveStage = nextStage;
        _pr57ExclusiveStartTick = now;
        _pr57ExclusiveStartBytes = bytes;
    }

    void Pr57ExclusiveFinishVehicle(Vehicle vehicle)
    {
        if (!_pr57ExclusiveVehicleActive) return;
        Pr57ExclusiveMark(0);
        Pr57ExecFinishVehicle(vehicle);
        _pr57ExclusiveVehicleActive = false;
        _pr57ExclusiveVehicles++;
        if (vehicle.Status == VehicleStatus.Travelling) _pr57ExclusiveTravelling++;
        if (vehicle.Position.Distance(_pr57ExclusiveOldPosition) > 1e-7) _pr57ExclusiveMoved++;
    }

    void Pr57ExclusiveEndSlice()
    {
        if (!_pr57ExclusiveActive) return;
        Pr57PoseGuardReport();
        Pr57ExecEndSlice();
        _pr57ExclusiveActive = false;
        static double Ms(long ticks) =>
            Math.Round(ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency, 6);
        var totalTicks = _pr57ExclusiveTicks.Sum();
        var totalBytes = _pr57ExclusiveBytes.Sum();
        Console.WriteLine(
            $"PR57_206_RESIDUAL sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
            $"vehicles={_pr57ExclusiveVehicles} travelling={_pr57ExclusiveTravelling} moved={_pr57ExclusiveMoved} " +
            $"totalMs={Ms(totalTicks)} totalMB={totalBytes / 1e6:F6} " +
            string.Join(" ", Enumerable.Range(0, 9).Select(i =>
                $"s{i}Ms={Ms(_pr57ExclusiveTicks[i])} s{i}MB={_pr57ExclusiveBytes[i] / 1e6:F6} s{i}Segments={_pr57ExclusiveSegments[i]}")) +
            " scope=ordered-vehicle-loop only observational=True exclusiveWithinLoop=True intrusive=True");
    }
}
