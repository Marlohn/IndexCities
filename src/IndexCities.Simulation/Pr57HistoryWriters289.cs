using System.Diagnostics;

namespace IndexCities.Simulation;

// #289: opt-in observational accounting of the three canonical append helpers.
// Counts successful appends only; other direct writers are NOT covered.
// The stopwatch/allocation census is intrusive and is never used by gameplay.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Writer289Enabled =
        Environment.GetEnvironmentVariable("PR57_289_WRITERS")=="1";
    int _pr57Writer289Phase=13; // 0..12 Step phases, 13 outside Step.
    readonly long[,] _pr57Writer289Calls=new long[14,3];
    readonly long[,] _pr57Writer289Ticks=new long[14,3];
    readonly long[,] _pr57Writer289Bytes=new long[14,3];
    static readonly string[] Pr57Writer289Names={"Personal","Event","Transfer"};

    long Pr57Writer289Start(out long allocated)
    {
        if(!Pr57Writer289Enabled){allocated=0;return 0;}
        allocated=GC.GetAllocatedBytesForCurrentThread();
        return Stopwatch.GetTimestamp();
    }
    void Pr57Writer289End(int kind,long started,long allocated)
    {
        if(!Pr57Writer289Enabled)return;
        var ticks=Stopwatch.GetTimestamp()-started;
        var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
        var phase=_pr57Writer289Phase;
        _pr57Writer289Calls[phase,kind]++;
        _pr57Writer289Ticks[phase,kind]+=ticks;
        _pr57Writer289Bytes[phase,kind]+=bytes;
    }
    public IEnumerable<string> Pr57Writer289Lines(int calls)
    {
        if(!Pr57Writer289Enabled)yield break;
        for(var kind=0;kind<3;kind++)
        {
            long count=0,ticks=0,bytes=0;
            for(var phase=0;phase<14;phase++)
            {
                count+=_pr57Writer289Calls[phase,kind];
                ticks+=_pr57Writer289Ticks[phase,kind];
                bytes+=_pr57Writer289Bytes[phase,kind];
                if(_pr57Writer289Calls[phase,kind]==0)continue;
                var name=phase==13?"outside":StepCostProfile.Names[phase];
                yield return $"PR57_289_WRITER calls={calls} phase={name} kind={Pr57Writer289Names[kind]} count={_pr57Writer289Calls[phase,kind]} ticks={_pr57Writer289Ticks[phase,kind]} allocatedBytes={_pr57Writer289Bytes[phase,kind]}";
            }
            yield return $"PR57_289_WRITER_SUMMARY calls={calls} kind={Pr57Writer289Names[kind]} count={count} ticks={ticks} allocatedBytes={bytes}";
        }
    }
}
