#!/usr/bin/env python3
"""PR57 #350: checkout-only ordered step-limit and safety-call attribution.

Replays the canonical calls, preserving every sub-slice, actor and side effect.
Only six late-jam physical samples are observed; no caching or scheduling.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
mobility=root/"src/IndexCities.Simulation/Mobility.cs"
effects=root/"src/IndexCities.Simulation/Mobility.StateEffects348.Pr57.cs"
residual=root/"src/IndexCities.Simulation/Mobility.ResidualExclusive.Pr57.cs"

def replace(path,old,new,label):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==1, f"#350 {label}: expected one anchor, found {s.count(old)}"
    path.write_text(s.replace(old,new,1),encoding="utf-8")

replace(mobility,"var free=step;step=SafeSiteTravel(v,target,step,true);",
        "var free=step;step=Pr57350Site(v,target,step);","site")
replace(mobility,"if(node.Neighbours.Count<3&&!node.Crosswalk)step=ClearOfCrossingAhead(v,target,step);",
        "if(node.Neighbours.Count<3&&!node.Crosswalk)step=Pr57350Crossing(v,target,step);","crossing")
replace(mobility,"var steer=step<.00001&&free>.00001?TurnClearance(v,approachHeading,free,access):null;",
        "var pr57350TurnStart=Pr57350TurnBegin(step<.00001&&free>.00001);\n"
        "                var steer=step<.00001&&free>.00001?TurnClearance(v,approachHeading,free,access):null;\n"
        "                Pr57350TurnEnd(pr57350TurnStart,steer is not null);","turn")
replace(mobility,
        'if(scanProfile is not null){_pr57RegionAllocBytes[4]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;pr57RegionStart=GC.GetAllocatedBytesForCurrentThread();}',
        'Pr57350Gate(blocker,leaderDistance,free,afterSiteStep,afterCrossingStep,step,bendLeaders.Count);\n'
        '                if(scanProfile is not null){_pr57RegionAllocBytes[4]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;pr57RegionStart=GC.GetAllocatedBytesForCurrentThread();}',
        "gate")
replace(effects,"        _pr57348Actor = v;",
        "        _pr57348Actor = v;\n        Pr57350Begin();","begin")
replace(effects,"        _pr57348Actor = null;",
        "        Pr57350Finish(group);\n        _pr57348Actor = null;","finish")
replace(residual,"        Pr57348Report();",
        "        Pr57348Report();\n        Pr57350Report();","report")

cs=r'''namespace IndexCities.Simulation;

// PR57 #350 is an intrusive, checkout-only observer of *actual* safety calls.
// The return values and ordering are canonical; counters never control physics.
public sealed partial class CitySimulation
{
    static readonly bool Pr57350Enabled =
        Environment.GetEnvironmentVariable("PR57_350_STEP_SAFETY")=="1";

    static readonly string[] Pr57350Keys =
    [
        "siteCalls","siteZeroInput","siteRestricted",
        "crossCalls","crossZeroInput","crossRestricted",
        "turnCalls","turnAccepted","gateVisits","preSiteZero",
        "siteMadeZero","crossMadeZero","finalZero",
        "leaderWithinGap","bendLeaders","siteStateDiff",
        "crossStateDiff","siteInputPositive","crossInputPositive",
        "blockerPresent","blockerNotStranded","blockerStranded",
        "finalPositive"
    ];

    readonly long[,] _pr57350Totals=new long[5,23];
    readonly long[,] _pr57350Ticks=new long[5,3];
    readonly long[,] _pr57350Bytes=new long[5,3];
    readonly long[] _pr57350Now=new long[23];
    readonly long[] _pr57350NowTicks=new long[3];
    readonly long[] _pr57350NowBytes=new long[3];
    long _pr57350Sample=-1, _pr57350TurnAllocStart;

    void Pr57350Begin()
    {
        if(!Pr57350Enabled||!_pr57ExclusiveActive)return;
        if(_pr57350Sample!=_pr57ExclusiveSamples)
        {
            _pr57350Sample=_pr57ExclusiveSamples;
            Array.Clear(_pr57350Totals);
            Array.Clear(_pr57350Ticks);
            Array.Clear(_pr57350Bytes);
        }
        Array.Clear(_pr57350Now);
        Array.Clear(_pr57350NowTicks);
        Array.Clear(_pr57350NowBytes);
    }

    double Pr57350Site(Vehicle v,Point target,double requested)
    {
        if(!Pr57350Enabled||!_pr57ExclusiveActive)
            return SafeSiteTravel(v,target,requested,true);
        _pr57350Now[0]++;
        if(requested<=.00001)_pr57350Now[1]++;
        else _pr57350Now[17]++;
        var cause=v.Cause;
        var heading=v.Heading;
        var wait=v.WaitingSince;
        var pos=v.Position;
        var route=v.RouteIndex;
        var alloc=GC.GetAllocatedBytesForCurrentThread();
        var tick=System.Diagnostics.Stopwatch.GetTimestamp();
        var allowed=SafeSiteTravel(v,target,requested,true);
        _pr57350NowTicks[0]+=System.Diagnostics.Stopwatch.GetTimestamp()-tick;
        _pr57350NowBytes[0]+=GC.GetAllocatedBytesForCurrentThread()-alloc;
        if(allowed<requested-.000001)_pr57350Now[2]++;
        if(v.Cause!=cause||v.Heading!=heading||v.WaitingSince!=wait||
           !v.Position.Equals(pos)||v.RouteIndex!=route)_pr57350Now[15]++;
        return allowed;
    }

    double Pr57350Crossing(Vehicle v,Point target,double requested)
    {
        if(!Pr57350Enabled||!_pr57ExclusiveActive)
            return ClearOfCrossingAhead(v,target,requested);
        _pr57350Now[3]++;
        if(requested<=.00001)_pr57350Now[4]++;
        else _pr57350Now[18]++;
        var cause=v.Cause;
        var heading=v.Heading;
        var wait=v.WaitingSince;
        var pos=v.Position;
        var route=v.RouteIndex;
        var alloc=GC.GetAllocatedBytesForCurrentThread();
        var tick=System.Diagnostics.Stopwatch.GetTimestamp();
        var allowed=ClearOfCrossingAhead(v,target,requested);
        _pr57350NowTicks[1]+=System.Diagnostics.Stopwatch.GetTimestamp()-tick;
        _pr57350NowBytes[1]+=GC.GetAllocatedBytesForCurrentThread()-alloc;
        if(allowed<requested-.000001)_pr57350Now[5]++;
        if(v.Cause!=cause||v.Heading!=heading||v.WaitingSince!=wait||
           !v.Position.Equals(pos)||v.RouteIndex!=route)_pr57350Now[16]++;
        return allowed;
    }

    long Pr57350TurnBegin(bool called)
    {
        if(!Pr57350Enabled||!_pr57ExclusiveActive||!called)return 0;
        _pr57350Now[6]++;
        _pr57350TurnAllocStart=GC.GetAllocatedBytesForCurrentThread();
        return System.Diagnostics.Stopwatch.GetTimestamp();
    }

    void Pr57350TurnEnd(long tick,bool accepted)
    {
        if(tick==0)return;
        _pr57350NowTicks[2]+=System.Diagnostics.Stopwatch.GetTimestamp()-tick;
        _pr57350NowBytes[2]+=GC.GetAllocatedBytesForCurrentThread()-_pr57350TurnAllocStart;
        if(accepted)_pr57350Now[7]++;
    }

    void Pr57350Gate(Vehicle? blocker,double leaderDistance,double beforeSite,
                     double afterSite,double afterCross,double finalStep,int bendCount)
    {
        if(!Pr57350Enabled||!_pr57ExclusiveActive)return;
        _pr57350Now[8]++;
        if(beforeSite<=.00001)_pr57350Now[9]++;
        if(beforeSite>.00001&&afterSite<=.00001)_pr57350Now[10]++;
        if(afterSite>.00001&&afterCross<=.00001)_pr57350Now[11]++;
        if(finalStep<=.00001)_pr57350Now[12]++;
        else _pr57350Now[22]++;
        if(leaderDistance<=8)_pr57350Now[13]++;
        if(bendCount>0)_pr57350Now[14]++;
        if(blocker is not null)
        {
            _pr57350Now[19]++;
            if(blocker.Status==VehicleStatus.Stranded)_pr57350Now[21]++;
            else _pr57350Now[20]++;
        }
    }

    void Pr57350Finish(int group)
    {
        if(!Pr57350Enabled||!_pr57ExclusiveActive)return;
        for(var i=0;i<23;i++)_pr57350Totals[group,i]+=_pr57350Now[i];
        for(var i=0;i<3;i++)
        {
            _pr57350Ticks[group,i]+=_pr57350NowTicks[i];
            _pr57350Bytes[group,i]+=_pr57350NowBytes[i];
        }
    }

    void Pr57350Report()
    {
        if(!Pr57350Enabled||_pr57350Sample!=_pr57ExclusiveSamples)return;
        for(var g=0;g<5;g++)
        {
            var counters=string.Join(" ",Enumerable.Range(0,23)
                .Select(i=>$"{Pr57350Keys[i]}={_pr57350Totals[g,i]}"));
            var freq=System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(
                $"PR57_350_SAFETY sample={_pr57350Sample} group={Pr57348Groups[g]} " +
                counters+" "+
                $"siteMs={_pr57350Ticks[g,0]*1000.0/freq:F6} " +
                $"crossMs={_pr57350Ticks[g,1]*1000.0/freq:F6} " +
                $"turnMs={_pr57350Ticks[g,2]*1000.0/freq:F6} " +
                $"siteBytes={_pr57350Bytes[g,0]} " +
                $"crossBytes={_pr57350Bytes[g,1]} " +
                $"turnBytes={_pr57350Bytes[g,2]} " +
                "scope=CANONICAL_SAFETY_CALLS_INTRUSIVE " +
                "notExactReadset=True noSkippedSlices=True");
        }
    }
}
'''
out=root/"src/IndexCities.Simulation/Mobility.StepSafety350.Pr57.cs"
assert not out.exists(), "#350 output exists"
out.write_text(cs,encoding="utf-8")
print("PR57_350_INJECTED observational safety wrappers, step gates and per-actor group totals")
