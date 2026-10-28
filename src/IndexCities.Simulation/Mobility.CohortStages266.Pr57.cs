namespace IndexCities.Simulation;

// PR57 #266: bounded, observational per-ID/per-stage physical cost for the
// authentic 24-vehicle queue. No actor is skipped; no decisions or saves read
// these counters. Times include stopwatch overhead and are not an A/B speedup.
public sealed partial class CitySimulation
{
    static readonly bool Pr57266Enabled =
        Environment.GetEnvironmentVariable("PR57_266_COHORT_STAGES") == "1";
    int[] _pr57266Ids = [];
    readonly Dictionary<int, int> _pr57266Slots = new();
    readonly long[,,] _pr57266Ticks = new long[24, 6, 9];
    readonly long[,,] _pr57266Bytes = new long[24, 6, 9];
    readonly long[,,] _pr57266Segments = new long[24, 6, 9];
    readonly long[,] _pr57266Passes = new long[24, 6];
    readonly long[,] _pr57266CauseChanges = new long[24, 6];
    int _pr57266Slices, _pr57266Slot, _pr57266Window, _pr57266Stage;
    long _pr57266StartTick, _pr57266StartBytes;
    string? _pr57266PriorCause;
    bool _pr57266Active, _pr57266InActor;

    void Pr57266BeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57266Active = Pr57266Enabled && profiled && dt > 0 &&
            operationsDt is null && _pr57266Slices < 600 &&
            Pr57State244PersistentIds.Count == 24;
        if (!_pr57266Active) return;
        if (_pr57266Slices == 0)
        {
            _pr57266Ids = Pr57State244PersistentIds.OrderBy(id => id).ToArray();
            for (var i = 0; i < _pr57266Ids.Length; i++)
                _pr57266Slots.Add(_pr57266Ids[i], i);
        }
        _pr57266Slices++;
    }

    void Pr57266BeginVehicle(Vehicle vehicle)
    {
        _pr57266InActor = _pr57266Active &&
            _pr57266Slots.TryGetValue(vehicle.Id, out _pr57266Slot);
        if (!_pr57266InActor) return;
        _pr57266Window = (_pr57266Slices - 1) / 100;
        _pr57266Stage = 0;
        _pr57266PriorCause = vehicle.Cause;
        _pr57266StartBytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57266StartTick = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    void Pr57266Mark(int nextStage)
    {
        if (!_pr57266InActor) return;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var stage = _pr57266Stage;
        _pr57266Ticks[_pr57266Slot, _pr57266Window, stage] += now - _pr57266StartTick;
        _pr57266Bytes[_pr57266Slot, _pr57266Window, stage] += bytes - _pr57266StartBytes;
        _pr57266Segments[_pr57266Slot, _pr57266Window, stage]++;
        _pr57266Stage = nextStage;
        _pr57266StartTick = now;
        _pr57266StartBytes = bytes;
    }

    void Pr57266FinishVehicle(Vehicle vehicle)
    {
        if (!_pr57266InActor) return;
        Pr57266Mark(0); // Include break/continue/early-exit tail exactly once.
        _pr57266Passes[_pr57266Slot, _pr57266Window]++;
        if (!string.Equals(_pr57266PriorCause, vehicle.Cause, StringComparison.Ordinal))
            _pr57266CauseChanges[_pr57266Slot, _pr57266Window]++;
        _pr57266InActor = false;
    }

    void Pr57266EndSlice()
    {
        if (!_pr57266Active) return;
        _pr57266Active = false;
        if (_pr57266Slices != 600) return;
        static double Ms(long ticks) =>
            ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        long totalPasses = 0, totalTicks = 0, totalBytes = 0, causeChanges = 0;
        for (var slot = 0; slot < 24; slot++)
            for (var window = 0; window < 6; window++)
            {
                totalPasses += _pr57266Passes[slot, window];
                causeChanges += _pr57266CauseChanges[slot, window];
                for (var stage = 0; stage < 9; stage++)
                {
                    totalTicks += _pr57266Ticks[slot, window, stage];
                    totalBytes += _pr57266Bytes[slot, window, stage];
                }
            }
        Console.WriteLine(
            $"PR57_266_SUM slices={_pr57266Slices} cohort={_pr57266Ids.Length} " +
            $"passes={totalPasses} causeChanges={causeChanges} " +
            $"totalMs={Ms(totalTicks):F6} totalMB={totalBytes / 1e6:F6} " +
            "scope=REAL_14H_600_SLICES_PER_ID_STAGE intrusive=True " +
            "noReuse=True noSkip=True notCausalClosure=True");
        for (var window = 0; window < 6; window++)
            for (var slot = 0; slot < 24; slot++)
            {
                long ticks = 0, bytes = 0;
                for (var stage = 0; stage < 9; stage++)
                {
                    ticks += _pr57266Ticks[slot, window, stage];
                    bytes += _pr57266Bytes[slot, window, stage];
                }
                Console.WriteLine(
                    $"PR57_266_ROW window={window} id={_pr57266Ids[slot]} " +
                    $"passes={_pr57266Passes[slot, window]} " +
                    $"causeChanges={_pr57266CauseChanges[slot, window]} " +
                    $"totalMs={Ms(ticks):F6} totalMB={bytes / 1e6:F6} " +
                    string.Join(" ", Enumerable.Range(0, 9).Select(stage =>
                        $"s{stage}Ms={Ms(_pr57266Ticks[slot, window, stage]):F6} " +
                        $"s{stage}Segments={_pr57266Segments[slot, window, stage]}")) +
                    " observational=True");
            }
    }
}
