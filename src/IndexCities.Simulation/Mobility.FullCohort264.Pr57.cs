namespace IndexCities.Simulation;

// PR57 #264: bounded observational full-window cohort timing. This measures
// complete canonical vehicle passes, not a safe-to-skip subset. The 24 IDs
// are taken from authentic 08h/14h economic saves by the workflow.
// Never supplies a physical decision, changes ordering, or caches across steps.
public sealed partial class CitySimulation
{
    static readonly bool Pr57264Enabled =
        Environment.GetEnvironmentVariable("PR57_264_FULL_COHORT") == "1";
    readonly long[] _pr57264Ticks = new long[3];
    readonly long[] _pr57264Bytes = new long[3];
    readonly long[] _pr57264Passes = new long[3];
    readonly long[] _pr57264Moved = new long[3];
    readonly long[] _pr57264CauseChanges = new long[3];
    readonly long[,] _pr57264WindowTicks = new long[6, 3];
    readonly long[,] _pr57264WindowPasses = new long[6, 3];
    int _pr57264Slices, _pr57264Group;
    bool _pr57264Active, _pr57264InActor;
    long _pr57264StartTick, _pr57264StartBytes, _pr57264HeadingBits;
    Point _pr57264BeforePosition;
    string? _pr57264BeforeCause;

    void Pr57264BeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57264Active = Pr57264Enabled && profiled && dt > 0 &&
            operationsDt is null && _pr57264Slices < 600 &&
            Pr57State244PersistentIds.Count > 0;
        if (_pr57264Active) _pr57264Slices++;
    }

    void Pr57264BeginVehicle(Vehicle v)
    {
        if (!_pr57264Active) return;
        _pr57264InActor = true;
        // Group 0: the exact persistent cohort; 1: other travelling at entry;
        // 2: remaining vehicle states. Status is sampled before the actor.
        _pr57264Group = Pr57State244PersistentIds.Contains(v.Id) ? 0 :
            v.Status == VehicleStatus.Travelling ? 1 : 2;
        _pr57264BeforePosition = v.Position;
        _pr57264HeadingBits = BitConverter.DoubleToInt64Bits(v.Heading);
        _pr57264BeforeCause = v.Cause;
        _pr57264StartBytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57264StartTick = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    void Pr57264FinishVehicle(Vehicle v)
    {
        if (!_pr57264InActor) return;
        var elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - _pr57264StartTick;
        var bytes = GC.GetAllocatedBytesForCurrentThread() - _pr57264StartBytes;
        var group = _pr57264Group;
        var window = (_pr57264Slices - 1) / 100;
        _pr57264Ticks[group] += elapsed;
        _pr57264Bytes[group] += bytes;
        _pr57264Passes[group]++;
        _pr57264WindowTicks[window, group] += elapsed;
        _pr57264WindowPasses[window, group]++;
        if (v.Position != _pr57264BeforePosition ||
            BitConverter.DoubleToInt64Bits(v.Heading) != _pr57264HeadingBits)
            _pr57264Moved[group]++;
        if (!string.Equals(v.Cause, _pr57264BeforeCause, StringComparison.Ordinal))
            _pr57264CauseChanges[group]++;
        _pr57264InActor = false;
    }

    void Pr57264EndSlice()
    {
        if (!_pr57264Active) return;
        _pr57264Active = false;
        if (_pr57264Slices != 600) return;
        static double Ms(long ticks) =>
            ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            "PR57_264_COHORT slices=" + _pr57264Slices +
            " persistentIds=" + Pr57State244PersistentIds.Count +
            " passes=" + _pr57264Passes.Sum() +
            " totalMs=" + Ms(_pr57264Ticks.Sum()).ToString("F6") +
            " persistentMs=" + Ms(_pr57264Ticks[0]).ToString("F6") +
            " otherTravellingMs=" + Ms(_pr57264Ticks[1]).ToString("F6") +
            " otherStatesMs=" + Ms(_pr57264Ticks[2]).ToString("F6") +
            " persistentPasses=" + _pr57264Passes[0] +
            " otherTravellingPasses=" + _pr57264Passes[1] +
            " otherStatesPasses=" + _pr57264Passes[2] +
            " persistentMB=" + (_pr57264Bytes[0] / 1e6).ToString("F6") +
            " otherTravellingMB=" + (_pr57264Bytes[1] / 1e6).ToString("F6") +
            " otherStatesMB=" + (_pr57264Bytes[2] / 1e6).ToString("F6") +
            " persistentMoved=" + _pr57264Moved[0] +
            " otherTravellingMoved=" + _pr57264Moved[1] +
            " otherStatesMoved=" + _pr57264Moved[2] +
            " persistentCauseChanges=" + _pr57264CauseChanges[0] +
            " otherTravellingCauseChanges=" + _pr57264CauseChanges[1] +
            " otherStatesCauseChanges=" + _pr57264CauseChanges[2] +
            " scope=REAL_14H_600_PHYSICAL_SLICES actorPassesOnly=True " +
            "excludesSetupTowAndOtherPhases=True intrusive=True noReuse=True noSkip=True");
        for (var window = 0; window < 6; window++)
        {
            Console.WriteLine(
                "PR57_264_WINDOW index=" + window +
                " persistentMs=" + Ms(_pr57264WindowTicks[window, 0]).ToString("F6") +
                " otherTravellingMs=" + Ms(_pr57264WindowTicks[window, 1]).ToString("F6") +
                " otherStatesMs=" + Ms(_pr57264WindowTicks[window, 2]).ToString("F6") +
                " persistentPasses=" + _pr57264WindowPasses[window, 0] +
                " otherTravellingPasses=" + _pr57264WindowPasses[window, 1] +
                " otherStatesPasses=" + _pr57264WindowPasses[window, 2] +
                " observational=True");
        }
    }
}
