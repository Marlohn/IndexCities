namespace IndexCities.Simulation;

// PR57 #248: count repeated explicit Manoeuvre arguments INSIDE one physical
// movement slice. This is an observational falsification probe, not a cache:
// roads/shared nodes, body fields, callback captures and transient writers are
// not a closed causal read set. Never reuse a result or skip a physical slice.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Man248Enabled =
        Environment.GetEnvironmentVariable("PR57_248_MANOEUVRE_REPEATS") == "1";

    readonly HashSet<string> _pr57Man248Seen = new(StringComparer.Ordinal);
    readonly HashSet<int> _pr57Man248Actors = new();
    bool _pr57Man248Active;
    long _pr57Man248Slices, _pr57Man248Calls, _pr57Man248Callbacks;
    long _pr57Man248Repeated, _pr57Man248RepeatActors, _pr57Man248Unique;
    long _pr57Man248Ticks, _pr57Man248RepeatTicks;
    long _pr57Man248Bytes, _pr57Man248RepeatBytes;
    long _pr57Man248Poses, _pr57Man248RepeatPoses;

    void Pr57Man248BeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57Man248Active = Pr57Man248Enabled && profiled && dt > 0 && operationsDt is null;
        _pr57Man248Seen.Clear();
        _pr57Man248Actors.Clear();
        if (_pr57Man248Active) _pr57Man248Slices++;
    }

    // The key is an exact bitwise fingerprint of EXPLICIT arguments and a
    // selected actor field. It deliberately omits _roads, SharedNodes, and
    // other actor/body fields; equality is NOT sufficient for memoization.
    (bool Active, bool Repeated, long Start, long Bytes) Pr57Man248Before(
        Vehicle o, List<int> route, int from, Point at, double facing,
        IReadOnlyList<Point> site, bool approach, int minimum,
        Func<Point, double, bool>? extraClear, int? through)
    {
        if (!_pr57Man248Active) return default;
        _pr57Man248Calls++;
        if (!_pr57Man248Actors.Add(o.Id)) _pr57Man248RepeatActors++;
        if (extraClear is not null)
        {
            _pr57Man248Callbacks++;
            return (true, false, System.Diagnostics.Stopwatch.GetTimestamp(),
                GC.GetAllocatedBytesForCurrentThread());
        }
        static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);
        var key = new System.Text.StringBuilder(96 + route.Count * 8 + site.Count * 42);
        key.Append(o.Id).Append('|').Append(from).Append('|').Append(minimum)
            .Append('|').Append(approach ? 1 : 0).Append('|').Append(through?.ToString() ?? "null")
            .Append('|').Append(Bits(at.X)).Append('|').Append(Bits(at.Z))
            .Append('|').Append(Bits(facing)).Append('|')
            .Append(o.SiteDockHeading is {} dock ? Bits(dock).ToString() : "null")
            .Append('|').Append(route.Count);
        foreach (var id in route) key.Append(',').Append(id);
        key.Append('|').Append(site.Count);
        foreach (var p in site) key.Append(',').Append(Bits(p.X)).Append(':').Append(Bits(p.Z));
        var repeated = !_pr57Man248Seen.Add(key.ToString());
        if (repeated) _pr57Man248Repeated++;
        else _pr57Man248Unique++;
        return (true, repeated, System.Diagnostics.Stopwatch.GetTimestamp(),
            GC.GetAllocatedBytesForCurrentThread());
    }

    void Pr57Man248After((bool Active, bool Repeated, long Start, long Bytes) probe, int poses)
    {
        if (!probe.Active) return;
        var elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - probe.Start;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - probe.Bytes;
        _pr57Man248Ticks += elapsed;
        _pr57Man248Bytes += allocated;
        _pr57Man248Poses += poses;
        if (!probe.Repeated) return;
        _pr57Man248RepeatTicks += elapsed;
        _pr57Man248RepeatBytes += allocated;
        _pr57Man248RepeatPoses += poses;
    }

    void Pr57Man248EndSlice()
    {
        _pr57Man248Active = false;
        _pr57Man248Seen.Clear();
        _pr57Man248Actors.Clear();
    }

    void Pr57Man248Report(long slices)
    {
        if (!Pr57Man248Enabled) return;
        static double Ms(long ticks) => ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_248_MANOEUVRE slices={slices} observedSlices={_pr57Man248Slices} " +
            $"calls={_pr57Man248Calls} callbackCalls={_pr57Man248Callbacks} " +
            $"distinctExplicitArgKeys={_pr57Man248Unique} repeatedExplicitArgKeys={_pr57Man248Repeated} " +
            $"repeatedActorCalls={_pr57Man248RepeatActors} " +
            $"totalMs={Ms(_pr57Man248Ticks):F4} repeatedMs={Ms(_pr57Man248RepeatTicks):F4} " +
            $"totalMB={_pr57Man248Bytes / 1e6:F4} repeatedMB={_pr57Man248RepeatBytes / 1e6:F4} " +
            $"totalPoses={_pr57Man248Poses} repeatedPoses={_pr57Man248RepeatPoses} " +
            "scope=ONE_PHYSICAL_SLICE_ONLY explicitArgsOnly=True callbacksExcludedFromRepeats=True " +
            "roadAndExternalWritersUnclosed=True noReuse=True observational=True intrusive=True");
    }
}
