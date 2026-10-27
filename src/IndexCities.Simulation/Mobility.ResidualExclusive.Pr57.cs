namespace IndexCities.Simulation;

// PR57 #206: bounded, strictly observational exclusive timing of consecutive
// regions in the physical vehicle executor. No decisions, candidate sets,
// ordering, or slices are changed. Stopwatch and allocation probes are
// intrusive; values are NOT clean A/B timings or universal CPU fractions.
public sealed partial class CitySimulation
{
    static readonly bool Pr57ResidualExclusiveEnabled =
        Environment.GetEnvironmentVariable("PR57_206_RESIDUAL_EXCLUSIVE") == "1";
    // PR57 #235: bounded cost of movement-slice setup before the ordered actor loop.
    // Observation only; excludes the loop itself and later tow synchronization.
    static readonly bool Pr57SetupGapEnabled =
        Environment.GetEnvironmentVariable("PR57_235_SETUP_GAP") == "1";
    // #244: bounded state/cohort attribution of the existing #206 stage timings.
    // Read-only, opt-in; no physical decision, actor order or save observes this.
    static readonly bool Pr57State244Enabled =
        Environment.GetEnvironmentVariable("PR57_244_STATE_COST") == "1";
    static readonly HashSet<int> Pr57State244PersistentIds =
        (Environment.GetEnvironmentVariable("PR57_244_PERSISTENT_IDS") ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(id => int.Parse(id)).ToHashSet();
    readonly long[][] _pr57State244Ticks = Enumerable.Range(0, 5).Select(_ => new long[9]).ToArray();
    readonly long[] _pr57State244Calls = new long[5];
    readonly long[] _pr57State244CurrentTicks = new long[9];
    bool _pr57State244StartedTravelling;
    bool _pr57SetupGapRunning;
    long _pr57SetupGapStartTicks, _pr57SetupGapStartBytes;

    void Pr57SetupGapStart(double dt, double? operationsDt)
    {
        _pr57SetupGapRunning = Pr57SetupGapEnabled && Pr57ResidualExclusiveEnabled
            && CostProfile is not null && dt > 0 && operationsDt is null
            && _pr57ExclusiveEligibleSlices is >= 36 and <= 38;
        if (!_pr57SetupGapRunning) return;
        _pr57SetupGapStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57SetupGapStartBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    void Pr57SetupGapEnd(int orderedCount, int candidateCount)
    {
        if (!_pr57SetupGapRunning) return;
        _pr57SetupGapRunning = false;
        var elapsedTicks = System.Diagnostics.Stopwatch.GetTimestamp() - _pr57SetupGapStartTicks;
        var bytes = GC.GetAllocatedBytesForCurrentThread() - _pr57SetupGapStartBytes;
        if (!_pr57ExclusiveActive) return;
        Console.WriteLine(
            $"PR57_235_SETUP sample={_pr57ExclusiveSamples} vehicles={State.Vehicles.Count} " +
            $"ordered={orderedCount} candidates={candidateCount} " +
            $"setupMs={elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F6} " +
            $"setupMB={bytes / 1e6:F6} scope=PRE_ORDERED_LOOP " +
            "includesCleanupSortIndexes=True excludesActorLoopAndTowSync=True " +
            "observational=True intrusive=True");
    }


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
        if (Pr57State244Enabled)
        {
            foreach (var row in _pr57State244Ticks) Array.Clear(row);
            Array.Clear(_pr57State244Calls);
        }
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
        if (Pr57State244Enabled)
        {
            Array.Clear(_pr57State244CurrentTicks);
            _pr57State244StartedTravelling = vehicle.Status == VehicleStatus.Travelling;
        }
        Pr57ExecBeginVehicle(vehicle);
        Pr57DenseBeginVehicle();
    }

    void Pr57ExclusiveMark(int nextStage)
    {
        if (!_pr57ExclusiveVehicleActive) return;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var elapsed = now - _pr57ExclusiveStartTick;
        _pr57ExclusiveTicks[_pr57ExclusiveStage] += elapsed;
        if (Pr57State244Enabled) _pr57State244CurrentTicks[_pr57ExclusiveStage] += elapsed;
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
        if (Pr57State244Enabled)
        {
            var moved = vehicle.Position.Distance(_pr57ExclusiveOldPosition) > 1e-7;
            var travelling = _pr57State244StartedTravelling && vehicle.Status == VehicleStatus.Travelling;
            var persistent = Pr57State244PersistentIds.Contains(vehicle.Id);
            // 0 persistent/still travelling/still; 1 other persistent;
            // 2 other travelling/still; 3 other travelling/moved; 4 other.
            var group = persistent ? (travelling && !moved ? 0 : 1)
                : travelling ? (moved ? 3 : 2) : 4;
            _pr57State244Calls[group]++;
            for (var i = 0; i < 9; i++)
                _pr57State244Ticks[group][i] += _pr57State244CurrentTicks[i];
        }
        Pr57ExecFinishVehicle(vehicle);
        Pr57DenseFinishVehicle(vehicle);
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
        Pr57DenseEndSlice();
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
        if (Pr57State244Enabled)
        {
            string[] names = ["persistent-still", "persistent-other", "other-travelling-still",
                "other-travelling-moved", "other"];
            for (var group = 0; group < names.Length; group++)
            {
                var ticks = _pr57State244Ticks[group];
                Console.WriteLine(
                    $"PR57_244_STATE sample={_pr57ExclusiveSamples} group={names[group]} " +
                    $"actors={_pr57State244Calls[group]} totalMs={Ms(ticks.Sum())} " +
                    $"s3to5Ms={Ms(ticks[3] + ticks[4] + ticks[5])} " +
                    string.Join(" ", Enumerable.Range(0, 9).Select(i => $"s{i}Ms={Ms(ticks[i])}")) +
                    $" cohortIds={Pr57State244PersistentIds.Count} " +
                    "scope=three-physical-slices sampled-post-state noCausalClosure=True " +
                    "observational=True intrusive=True");
            }
        }
    }
}
