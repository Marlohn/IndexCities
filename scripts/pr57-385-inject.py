#!/usr/bin/env python3
"""PR57 #385: observational full-horizon MoveVehiclesUnsplit region census.

Apply after #384 to a disposable checkout. Six region boundaries in the
canonical method, with no decision reuse, no skipped actors, and no new
persistent state. All four replay arms retain the #337 actor timer.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
p=root/"src/IndexCities.Simulation/Mobility.cs"
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)

patch("        Pr57SetupGapStart(dt,operationsDt);",
      "        Pr57385Begin(dt,operationsDt);\n        Pr57SetupGapStart(dt,operationsDt);")
patch("        foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}",
      "        Pr57385Mark(0);\n        foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}\n        Pr57385Mark(1);")
patch("        foreach(var v in ordered){",
      "        Pr57385Mark(2);\n        foreach(var v in ordered){")
patch("        SyncTowedVehicles();",
      "        Pr57385Mark(3);\n        SyncTowedVehicles();\n        Pr57385Mark(4);")
patch("        if(pr57Bvh)_pr57VehicleBvh!.Refresh();\n        if(scanProfile is not null){",
      "        if(pr57Bvh)_pr57VehicleBvh!.Refresh();\n        Pr57385End();\n        if(scanProfile is not null){")
p.write_text(s,encoding="utf-8")

out=root/"src/IndexCities.Simulation/Mobility.Regions385.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;

namespace IndexCities.Simulation;

// Shadow-only wall and allocation census; never consulted by gameplay.
// Region boundaries cover the whole Unsplit body, not all of MoveVehicles or Step.
public sealed partial class CitySimulation
{
    static readonly bool Pr57385Enabled =
        Environment.GetEnvironmentVariable("PR57_385_SEGMENT_PROBE")=="1";
    // 0-1 calls, 2-3 operationsDt-present calls,
    // 4-15 six region ticks per regime, 16-27 corresponding allocated bytes.
    readonly long[] _pr57385Totals=new long[28];
    long _pr57385LastTick,_pr57385LastBytes;
    int _pr57385Group;
    bool _pr57385Active;
    void Pr57385Begin(double dt,double? operationsDt)
    {
        _pr57385Active=Pr57385Enabled;
        if(!_pr57385Active)return;
        _pr57385Group=dt<=0?0:1;
        if(operationsDt.HasValue)_pr57385Totals[2+_pr57385Group]++;
        _pr57385LastTick=Stopwatch.GetTimestamp();
        _pr57385LastBytes=GC.GetAllocatedBytesForCurrentThread();
    }
    void Pr57385Mark(int stage)
    {
        if(!_pr57385Active)return;
        long tick=Stopwatch.GetTimestamp();
        long bytes=GC.GetAllocatedBytesForCurrentThread();
        _pr57385Totals[4+_pr57385Group*6+stage]+=tick-_pr57385LastTick;
        _pr57385Totals[16+_pr57385Group*6+stage]+=bytes-_pr57385LastBytes;
        _pr57385LastTick=tick;
        _pr57385LastBytes=bytes;
    }
    void Pr57385End()
    {
        if(!_pr57385Active)return;
        Pr57385Mark(5);
        _pr57385Totals[_pr57385Group]++;
        _pr57385Active=false;
    }
    public long[] Pr57385Snapshot()=>(long[])_pr57385Totals.Clone();
    public void Pr57385Report(int hour,long[] before)
    {
        var now=Pr57385Snapshot();
        if(before.Length!=28)throw new InvalidOperationException("#385 snapshot length");
        for(int g=0;g<2;g++)
        {
            var ms=new double[6];
            long ticks=0,bytes=0;
            for(int j=0;j<6;j++)
            {
                var t=now[4+g*6+j]-before[4+g*6+j];
                var b=now[16+g*6+j]-before[16+g*6+j];
                if(t<0||b<0)throw new InvalidOperationException("#385 negative region");
                ms[j]=t*1000.0/Stopwatch.Frequency;
                ticks+=t;bytes+=b;
            }
            long calls=now[g]-before[g];
            long ops=now[2+g]-before[2+g];
            if(Pr57385Enabled&&(calls<=0||ops>calls))
                throw new InvalidOperationException("#385 invalid regime census");
            Console.WriteLine($"PR57_385_SEGMENT hour={hour} regime={(g==0?"operational":"physical")} "+
                $"enabled={Pr57385Enabled} calls={calls} operationsNonNull={ops} "+
                $"totalMs={ticks*1000.0/Stopwatch.Frequency:F3} allocatedBytes={bytes} "+
                $"setupMs={ms[0]:F3} cleanupMs={ms[1]:F3} prepareMs={ms[2]:F3} "+
                $"actorLoopMs={ms[3]:F3} towSyncMs={ms[4]:F3} postMs={ms[5]:F3} "+
                "scope=FULL_6H_REAL_UNSPLIT_INTRUSIVE noSkippedSlices=True noDecisionReuse=True");
        }
    }
}
''',encoding="utf-8")

p=root/"scripts/pr57-actor-regime-337/Program.cs"
s=p.read_text(encoding="utf-8")
def runner_patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)
runner_patch("    var phaseBefore=(long[])sim.CostProfile!.Ticks.Clone();",
             "    var regionsBefore=sim.Pr57385Snapshot();\n    var phaseBefore=(long[])sim.CostProfile!.Ticks.Clone();")
runner_patch("    clock.Stop();\n    var profile=sim.CostProfile!;",
             "    clock.Stop();\n    sim.Pr57385Report(hour,regionsBefore);\n    var profile=sim.CostProfile!;")
p.write_text(s,encoding="utf-8")
print("PR57_385_INJECTED full-horizon six-region observational census")
