#!/usr/bin/env python3
"""PR57 #382: partition measured MoveVehiclesUnsplit cost by operational/physical regime.

Apply after #367..#381 in a disposable checkout. Shadow timing only:
the original loop, decisions, saves, and every Advance(2) are unchanged.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
p=root/"Mobility.cs"
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)

patch("        Pr57381BeginSlice(dt,operationsDt);",
      "        Pr57382Begin(dt,operationsDt);\n        Pr57381BeginSlice(dt,operationsDt);")
patch("        foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}",
      "        Pr57382Mark(0); // setup\n        foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}\n        Pr57382Mark(1); // global cleanup")
patch("        foreach(var v in ordered){",
      "        Pr57382Mark(2); // preparation and candidate indexes\n        foreach(var v in ordered){")
patch("        SyncTowedVehicles();",
      "        Pr57382Mark(3); // ordered actor loop\n        SyncTowedVehicles();\n        Pr57382Mark(4); // tow synchronization")
patch("        if(pr57Bvh)_pr57VehicleBvh!.Refresh();\n        if(scanProfile is not null){",
      "        if(pr57Bvh)_pr57VehicleBvh!.Refresh();\n        Pr57382End(); // post-loop hooks, no changes to executor\n        if(scanProfile is not null){")
p.write_text(s,encoding="utf-8")

out=root/"Mobility.RegimeCost382.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;

namespace IndexCities.Simulation;

// Timing probe for the FIRST 600 MoveVehiclesUnsplit calls in the REAL fixture.
// Not a scheduler; no reused values, no skipped calls, no causal certificate.
// Segment timings are inclusive of previously installed observational hooks.
public sealed partial class CitySimulation
{
    static readonly bool Pr57382Enabled =
        Environment.GetEnvironmentVariable("PR57_382_REGIME_COST")=="1";
    const int Pr57382Limit=600;
    readonly long[,] _pr57382Ticks=new long[2,6];
    readonly long[,] _pr57382Bytes=new long[2,6];
    readonly long[] _pr57382GroupCalls=new long[2];
    readonly long[] _pr57382GroupOps=new long[2];
    readonly long[] _pr57382GroupTicks=new long[2];
    readonly long[] _pr57382GroupBytes=new long[2];
    readonly List<long>[] _pr57382Samples=[new(),new()];
    int _pr57382Calls,_pr57382Group;
    long _pr57382StartTick,_pr57382LastTick,_pr57382StartBytes,_pr57382LastBytes;
    bool _pr57382Active;
    void Pr57382Begin(double dt,double? operationsDt)
    {
        _pr57382Active=Pr57382Enabled&&_pr57382Calls<Pr57382Limit;
        if(!_pr57382Active)return;
        _pr57382Group=dt<=0?0:1;
        if(operationsDt.HasValue)_pr57382GroupOps[_pr57382Group]++;
        _pr57382StartBytes=_pr57382LastBytes=GC.GetAllocatedBytesForCurrentThread();
        _pr57382StartTick=_pr57382LastTick=Stopwatch.GetTimestamp();
    }
    void Pr57382Mark(int stage)
    {
        if(!_pr57382Active)return;
        long now=Stopwatch.GetTimestamp();
        long bytes=GC.GetAllocatedBytesForCurrentThread();
        _pr57382Ticks[_pr57382Group,stage]+=now-_pr57382LastTick;
        _pr57382Bytes[_pr57382Group,stage]+=bytes-_pr57382LastBytes;
        _pr57382LastTick=now;
        _pr57382LastBytes=bytes;
    }
    void Pr57382End()
    {
        if(!_pr57382Active)return;
        Pr57382Mark(5);
        long totalTicks=_pr57382LastTick-_pr57382StartTick;
        long totalBytes=_pr57382LastBytes-_pr57382StartBytes;
        int group=_pr57382Group;
        _pr57382GroupCalls[group]++;
        _pr57382GroupTicks[group]+=totalTicks;
        _pr57382GroupBytes[group]+=totalBytes;
        _pr57382Samples[group].Add(totalTicks);
        _pr57382Calls++;
        _pr57382Active=false;
        if(_pr57382Calls!=Pr57382Limit)return;
        if(_pr57382GroupCalls[0]!=84||_pr57382GroupCalls[1]!=516||
           _pr57382GroupOps[0]!=84||_pr57382GroupOps[1]!=0)
            throw new InvalidOperationException("#382 regime counts disagree with #381");
        for(int g=0;g<2;g++)
        {
            var a=_pr57382Samples[g];
            a.Sort();
            double Ms(long ticks)=>ticks*1000.0/Stopwatch.Frequency;
            long segments=0,bytes=0;
            for(int j=0;j<6;j++){segments+=_pr57382Ticks[g,j];bytes+=_pr57382Bytes[g,j];}
            if(segments!=_pr57382GroupTicks[g]||bytes!=_pr57382GroupBytes[g])
                throw new InvalidOperationException("#382 segment accounting mismatch");
            Console.WriteLine($"PR57_382_SUMMARY regime={(g==0?"operational":"physical")} "+
                $"calls={_pr57382GroupCalls[g]} operationsNonNull={_pr57382GroupOps[g]} "+
                $"totalMs={Ms(_pr57382GroupTicks[g]):F3} "+
                $"meanMs={Ms(_pr57382GroupTicks[g])/_pr57382GroupCalls[g]:F6} "+
                $"p50Ms={Ms(a[(a.Count-1)/2]):F6} "+
                $"p95Ms={Ms(a[(int)Math.Ceiling(a.Count*.95)-1]):F6} "+
                $"maxMs={Ms(a[^1]):F6} "+
                $"allocatedBytes={_pr57382GroupBytes[g]} "+
                $"setupMs={Ms(_pr57382Ticks[g,0]):F3} "+
                $"cleanupMs={Ms(_pr57382Ticks[g,1]):F3} "+
                $"prepareMs={Ms(_pr57382Ticks[g,2]):F3} "+
                $"actorMs={Ms(_pr57382Ticks[g,3]):F3} "+
                $"towSyncMs={Ms(_pr57382Ticks[g,4]):F3} "+
                $"postMs={Ms(_pr57382Ticks[g,5]):F3} "+
                $"setupBytes={_pr57382Bytes[g,0]} "+
                $"cleanupBytes={_pr57382Bytes[g,1]} "+
                $"prepareBytes={_pr57382Bytes[g,2]} "+
                $"actorBytes={_pr57382Bytes[g,3]} "+
                $"towSyncBytes={_pr57382Bytes[g,4]} "+
                $"postBytes={_pr57382Bytes[g,5]} "+
                "scope=FIRST_600_UNSPLIT_REAL_PARTIAL_SHADOW "+
                "noCausalClosure=True noDecisionReuse=True noSkippedSlices=True observational=True");
        }
        var total=_pr57382GroupTicks[0]+_pr57382GroupTicks[1];
        Console.WriteLine($"PR57_382_BOUND operationsPct={100.0*_pr57382GroupTicks[0]/total:F3} "+
            $"physicalPct={100.0*_pr57382GroupTicks[1]/total:F3} "+
            $"measuredCalls={_pr57382Calls} "+
            "scope=ONLY_UNSPLIT_NOT_WHOLE_ADVANCE upperBoundIfOperationsFree=True");
    }
}
''',encoding="utf-8")
print("PR57_382_INJECTED observational operational-vs-physical stage timing")
