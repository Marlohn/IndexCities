namespace IndexCities.Simulation;

// PR57 #277: opt-in, read-only decomposition of the *canonical* EntryPlan for
// the real blocked vehicle 23 in the first/last 100 of 600 physical sub-slices.
// No shortcuts, no persisted state, no extra geometry queries, no changed order.
// Timings include probe overhead; a positive result is not a speedup.
public sealed partial class CitySimulation
{
    static readonly bool Pr57277Enabled =
        Environment.GetEnvironmentVariable("PR57_277_ENTRY_PLAN") == "1";
    static readonly string[] Pr57277Stages =
        ["owners-way-out", "manoeuvre", "safe-check", "way-in-fallback", "need-and-limit"];
    readonly long[,] _pr57277Ticks = new long[2,5];
    readonly long[] _pr57277Calls = new long[2];
    readonly long[] _pr57277EmptyExits = new long[2];
    readonly long[] _pr57277EmptySpaces = new long[2];
    readonly long[] _pr57277Safe = new long[2];
    readonly long[] _pr57277Needed = new long[2];
    readonly long[] _pr57277Fallback = new long[2];
    readonly long[] _pr57277ExitPoses = new long[2];
    readonly long[] _pr57277WayPoses = new long[2];
    readonly long[] _pr57277NoNeedEmptyExits = new long[2];

    long Pr57277Begin(Vehicle v) =>
        Pr57277Enabled && _pr57273Sampling && v.Id == 23
            ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

    long Pr57277Stage(long start,int stage)
    {
        if(start==0)return 0;
        var now=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57277Ticks[_pr57273Window,stage]+=now-start;
        return now;
    }

    void Pr57277Record(Vehicle v,int exitPoses,int spaces,int wayPoses,
        bool safe,bool needed)
    {
        if(!Pr57277Enabled||!_pr57273Sampling||v.Id!=23)return;
        var w=_pr57273Window;
        _pr57277Calls[w]++;
        _pr57277ExitPoses[w]+=exitPoses;
        _pr57277WayPoses[w]+=wayPoses;
        if(exitPoses==0)_pr57277EmptyExits[w]++;
        if(spaces==0)_pr57277EmptySpaces[w]++;
        if(safe)_pr57277Safe[w]++;
        if(!safe)_pr57277Fallback[w]++;
        if(needed)_pr57277Needed[w]++;
        if(!needed&&exitPoses==0)_pr57277NoNeedEmptyExits[w]++;
    }

    void Pr57277Report()
    {
        if(!Pr57277Enabled)return;
        for(var w=0;w<2;w++)
        {
            Console.WriteLine(
                $"PR57_277_ENTRY window={w} id=23 calls={_pr57277Calls[w]} " +
                $"emptyExitPlans={_pr57277EmptyExits[w]} exitPoses={_pr57277ExitPoses[w]} " +
                $"emptySpaces={_pr57277EmptySpaces[w]} safe={_pr57277Safe[w]} " +
                $"fallback={_pr57277Fallback[w]} needed={_pr57277Needed[w]} " +
                $"noNeedAndEmptyExits={_pr57277NoNeedEmptyExits[w]} " +
                $"wayPoses={_pr57277WayPoses[w]} observational=True noSkip=True");
            for(var stage=0;stage<Pr57277Stages.Length;stage++)
                Console.WriteLine(
                    $"PR57_277_STAGE window={w} id=23 stage={Pr57277Stages[stage]} " +
                    $"ms={_pr57277Ticks[w,stage]*1000.0/System.Diagnostics.Stopwatch.Frequency:F6} " +
                    "inclusiveOfProbeOverhead=True");
        }
    }
}
