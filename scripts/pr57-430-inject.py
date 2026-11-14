#!/usr/bin/env python3
"""#430: sampled canonical waiting-vehicle lane-stage cost. CI checkout only.
All actor/candidate order, predicates, effects, and public Advance(2) preserved.
Intrusive observational timings are not an A/B speedup or causal closure.
"""
from pathlib import Path
import runpy
runpy.run_path("scripts/pr57-420-inject.py", run_name="__main__")
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()
def once(old,new):
    global s
    assert s.count(old)==1,(old[:100],s.count(old))
    s=s.replace(old,new,1)

once('    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,',
'''    static readonly bool Pr57430Enabled=Environment.GetEnvironmentVariable("PR57_430_STAGE_COST")=="1";
    readonly long[] _pr57430CandidateCounts=new long[10],_pr57430CandidateTicks=new long[10];
    long _pr57430Actors,_pr57430TotalTicks,_pr57430LaneTicks,_pr57430BendTicks;
    long _pr57430LaneCalls,_pr57430BendCalls,_pr57430Holds;
    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,''')
once('            Pr57Effect253BeforeActor(v);',
'''            var pr57430Measure=Pr57430Enabled&&pr57420Sample&&pr57420Kind==1&&_pr57420Physical%10==0;
            var pr57430ActorStart=pr57430Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            long pr57430LaneTicks=0,pr57430BendTicks=0;
            Pr57Effect253BeforeActor(v);''')
once('void HoldFor(Vehicle other,double distance){Pr57PhaseImpactHold(other,distance);',
'''void HoldFor(Vehicle other,double distance){if(pr57430Measure)_pr57430Holds++;Pr57PhaseImpactHold(other,distance);''')
once('                foreach(var liveOther in candidates){',
'''                foreach(var liveOther in candidates){
                    var pr57430CandidateStage=9;
                    var pr57430CandidateStart=pr57430Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                    try{''')
once('is {} docked){Pr57StageMark(1);','is {} docked){pr57430CandidateStage=1;Pr57StageMark(1);')
once('if(other.Id==v.Id||!Pr57StageOccupies(other)){Pr57StageMark(2);',
     'if(other.Id==v.Id||!Pr57StageOccupies(other)){pr57430CandidateStage=2;Pr57StageMark(2);')
once('if(insideMap&&relative.Distance(new(0,0))<.1){Pr57StageMark(4);',
     'if(insideMap&&relative.Distance(new(0,0))<.1){pr57430CandidateStage=4;Pr57StageMark(4);')
once('if(BlocksEntry(other)){Pr57StageMark(5);',
     'if(BlocksEntry(other)){pr57430CandidateStage=5;Pr57StageMark(5);')
once('''                }
                Pr57StageScanEnd(pr57StageScanStart);''',
'''                    }finally{
                        if(pr57430Measure){
                            _pr57430CandidateCounts[pr57430CandidateStage]++;
                            _pr57430CandidateTicks[pr57430CandidateStage]+=
                                System.Diagnostics.Stopwatch.GetTimestamp()-pr57430CandidateStart;
                        }
                    }
                }
                Pr57StageScanEnd(pr57StageScanStart);''')
once('                Pr57ScanLane(pr57LaneList);\n                Pr57CandidateEndQuery();',
'''                var pr57430LaneStart=pr57430Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                Pr57ScanLane(pr57LaneList);
                if(pr57430Measure){
                    pr57430LaneTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57430LaneStart;
                    _pr57430LaneCalls++;
                }
                Pr57CandidateEndQuery();''')
once('                var bendLeaders=pr57BendReuse?',
'''                var pr57430BendStart=pr57430Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                var bendLeaders=pr57BendReuse?''')
once('                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=4;',
'''                if(pr57430Measure){
                    pr57430BendTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57430BendStart;
                    _pr57430BendCalls++;
                }
                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=4;''')
once('                Pr57ActorShadowAfterActor(v);',
'''                Pr57ActorShadowAfterActor(v);
                if(pr57430Measure){
                    _pr57430Actors++;
                    _pr57430TotalTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57430ActorStart;
                    _pr57430LaneTicks+=pr57430LaneTicks;
                    _pr57430BendTicks+=pr57430BendTicks;
                }''')
once('            _pr57420Printed=true;',
'''            if(Pr57430Enabled){
                Console.WriteLine($"PR57_430_SUMMARY sampledPhysical=60 sampledActors={_pr57430Actors} "+
                    $"actorTicks={_pr57430TotalTicks} laneTicks={_pr57430LaneTicks} "+
                    $"bendTicks={_pr57430BendTicks} laneCalls={_pr57430LaneCalls} "+
                    $"bendCalls={_pr57430BendCalls} holds={_pr57430Holds} "+
                    $"frequency={System.Diagnostics.Stopwatch.Frequency} "+
                    "sampleStride=10 cohort=travelling_waiting observational=True "+
                    "intrusive=True causalClosure=False noDecisionReuse=True");
                foreach(var stage in new[]{1,2,4,5,9})
                    Console.WriteLine($"PR57_430_STAGE stage={stage} "+
                        $"candidates={_pr57430CandidateCounts[stage]} "+
                        $"ticks={_pr57430CandidateTicks[stage]} "+
                        "nestedInLane=True intrusive=True");
            }
            _pr57420Printed=true;''')
p.write_text(s)
print("PR57_430_INJECTED observational=True noSkippedSlices=True noDecisionReuse=True")
