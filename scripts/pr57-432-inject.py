#!/usr/bin/env python3
"""#432: mutually exclusive timing regions of REAL waiting vehicles.

Opt-in observational probe in a disposable CI checkout only. Keeps canonical
Advance(2), all actor/slice/candidate order, decisions, effects and saves.
The timers are intrusive; no gain or causal closure is claimed.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-420-inject.py", run_name="__main__")
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()

def once(old,new):
    global s
    n=s.count(old)
    assert n==1,(old[:100],n)
    s=s.replace(old,new,1)

once('    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,',
'''    static readonly bool Pr57432Enabled=Environment.GetEnvironmentVariable("PR57_432_STAGE_COST")=="1";
    readonly long[] _pr57432Ticks=new long[11],_pr57432Visits=new long[11];
    long _pr57432Actors,_pr57432Blocked,_pr57432PoseEligible;
    long _pr57432BypassCalls,_pr57432BypassTicks,_pr57432BypassSuccess;
    long _pr57432TowWaitCalls,_pr57432TowWaitTicks;
    static void Pr57432Mark(ref int stage,ref long last,long[] ticks,long[] visits,int next,bool enabled){
        if(!enabled)return;
        var now=System.Diagnostics.Stopwatch.GetTimestamp();
        ticks[stage]+=now-last;
        visits[stage]++;
        stage=next;
        last=now;
    }
    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,''')

once('            Pr57Effect253BeforeActor(v);',
'''            var pr57432Measure=Pr57432Enabled&&pr57420Sample&&pr57420Kind==1&&_pr57420Physical%10==0;
            var pr57432Last=pr57432Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var pr57432Stage=0;
            Pr57Effect253BeforeActor(v);''')

once('            while(remaining>.000001&&v.Status==VehicleStatus.Travelling){',
'''            while(remaining>.000001&&v.Status==VehicleStatus.Travelling){
                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,1,pr57432Measure);''')

once('                var laneScanStart=scanProfile is null?0L:System.Diagnostics.Stopwatch.GetTimestamp();',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,2,pr57432Measure);
                var laneScanStart=scanProfile is null?0L:System.Diagnostics.Stopwatch.GetTimestamp();''')

once('                var bendScanStart=scanProfile is null?0L:System.Diagnostics.Stopwatch.GetTimestamp();',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,3,pr57432Measure);
                var bendScanStart=scanProfile is null?0L:System.Diagnostics.Stopwatch.GetTimestamp();''')

once('                var free=step;step=SafeSiteTravel(v,target,step,true);',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,4,pr57432Measure);
                var free=step;step=SafeSiteTravel(v,target,step,true);''')

once('                var pr57CrossingAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,5,pr57432Measure);
                var pr57CrossingAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();''')

once('                var pr57TurnAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,6,pr57432Measure);
                var pr57TurnAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();''')

once('                Pr57PoseGuardBegin(v,step,d);',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,7,pr57432Measure);
                Pr57PoseGuardBegin(v,step,d);''')

once('                if(step<.00001&&d>.00001&&!pr57PoseNoop&&',
'''                if(pr57432Measure&&step<.00001&&d>.00001&&!pr57PoseNoop)_pr57432PoseEligible++;
                if(step<.00001&&d>.00001&&!pr57PoseNoop&&''')

once('                try{\n                    if(pr57IdleProbe)Pr57BlockMarkStage(2);',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,8,pr57432Measure);
                try{
                    if(pr57IdleProbe)Pr57BlockMarkStage(2);''')

once('                    if(step<.00001&&d>.00001){',
'''                    if(step<.00001&&d>.00001){
                        if(pr57432Measure)_pr57432Blocked++;''')

once('                            var bypassed=TryBypassStopped(v,blocker);',
'''                            var pr57432BypassStart=pr57432Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                            var bypassed=TryBypassStopped(v,blocker);
                            if(pr57432Measure){
                                _pr57432BypassCalls++;
                                _pr57432BypassTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57432BypassStart;
                                if(bypassed)_pr57432BypassSuccess++;
                            }''')

once('                        TowWaitsWithoutAccess(v);\n                        if(scanProfile is not null){_pr57BlockedAllocBytes[5]',
'''                        var pr57432TowWaitStart=pr57432Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                        TowWaitsWithoutAccess(v);
                        if(pr57432Measure){
                            _pr57432TowWaitCalls++;
                            _pr57432TowWaitTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57432TowWaitStart;
                        }
                        if(scanProfile is not null){_pr57BlockedAllocBytes[5]''')

once('                pr57RegionStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();\n                Pr57ExclusiveMark(7);',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,9,pr57432Measure);
                pr57RegionStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                Pr57ExclusiveMark(7);''')

once('                Pr57Effect253AfterActor(v);',
'''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,10,pr57432Measure);
                Pr57Effect253AfterActor(v);''')

once('                Pr57ActorShadowAfterActor(v);',
'''                Pr57ActorShadowAfterActor(v);
                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,11,pr57432Measure);
                if(pr57432Measure)_pr57432Actors++;''')

once('            _pr57420Printed=true;',
'''            if(Pr57432Enabled){
                Console.WriteLine($"PR57_432_SUMMARY sampledPhysical=60 sampledActors={_pr57432Actors} blocked={_pr57432Blocked} poseEligible={_pr57432PoseEligible} bypassCalls={_pr57432BypassCalls} bypassSuccess={_pr57432BypassSuccess} bypassTicks={_pr57432BypassTicks} towWaitCalls={_pr57432TowWaitCalls} towWaitTicks={_pr57432TowWaitTicks} frequency={System.Diagnostics.Stopwatch.Frequency} sampleStride=10 cohort=travelling_waiting observational=True intrusive=True causalClosure=False noDecisionReuse=True");
                var names=new[]{"pre_operations","route_shared_junction_setup","lane_scan_and_step","bend_scan","safe_site","crossing","turn_clearance","pose_guard","blocked_recovery","admission_and_movement","actor_finally"};
                for(var i=0;i<names.Length;i++)
                    Console.WriteLine($"PR57_432_STAGE stage={i} name={names[i]} ticks={_pr57432Ticks[i]} visits={_pr57432Visits[i]} observational=True");
            }
            _pr57420Printed=true;''')

p.write_text(s)
print("PR57_432_INJECTED observational=True noSkippedSlices=True noDecisionReuse=True")
