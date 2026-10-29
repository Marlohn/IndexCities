#!/usr/bin/env python3
"""PR57 #390: bounded, observational lane scan cost and binding by actor state.
Apply only in disposable CI checkout. No causal closure, skip, cache or reordering.
"""
from pathlib import Path
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)
patch("public sealed partial class CitySimulation\n{", """public sealed partial class CitySimulation
{
    static readonly bool Pr57390Enabled=Environment.GetEnvironmentVariable("PR57_390_LANE_SHADOW")=="1";
    long _pr57390PhysicalCalls;
    // Actor-entry wait (2) x lane-entry (2) x winner result (3).
    readonly long[] _pr57390Calls=new long[12],_pr57390Ticks=new long[12],
        _pr57390Candidates=new long[12],_pr57390RawZero=new long[12],
        _pr57390FinalZero=new long[12];
""")
patch("        Pr57SetupGapStart(dt,operationsDt);", """        var pr57390Sample=Pr57390Enabled&&dt>0&&_pr57390PhysicalCalls<600;
        if(pr57390Sample)_pr57390PhysicalCalls++;
        Pr57SetupGapStart(dt,operationsDt);""")
patch("        foreach(var v in ordered){", """        foreach(var v in ordered){
            var pr57390WaitAtActorStart=pr57390Sample&&v.WaitingSince>0;""")
patch("                var pr57LaneList=", """                var pr57390LaneStart=pr57390Sample?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                var pr57LaneList=""")
patch("                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));", """                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));
                var pr57390Cell=-1;
                if(pr57390Sample){
                    var rawCap=Math.Min(d,speed*remaining);
                    var leaderCap=Math.Max(0,leaderDistance-8);
                    var outcome=blocker is null?0:leaderCap<rawCap?2:1;
                    pr57390Cell=(pr57390WaitAtActorStart?6:0)+(enteringLane?3:0)+outcome;
                    _pr57390Calls[pr57390Cell]++;
                    _pr57390Ticks[pr57390Cell]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57390LaneStart;
                    _pr57390Candidates[pr57390Cell]+=pr57LaneList.Count;
                    if(rawCap<=.00001)_pr57390RawZero[pr57390Cell]++;
                }""")
patch("                try{\n                    if(pr57IdleProbe)Pr57BlockMarkStage(2);", """                if(pr57390Cell>=0&&step<.00001&&d>.00001)
                    _pr57390FinalZero[pr57390Cell]++;
                try{
                    if(pr57IdleProbe)Pr57BlockMarkStage(2);""")
patch("        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36", """        if(pr57390Sample&&_pr57390PhysicalCalls==600)
            for(var cell=0;cell<12;cell++)
                Console.WriteLine($"PR57_390_CELL cell={cell} waitCohort={(cell>=6?1:0)} enteringLane={((cell%6)>=3?1:0)} outcome={cell%3} calls={_pr57390Calls[cell]} candidates={_pr57390Candidates[cell]} ticks={_pr57390Ticks[cell]} frequency={System.Diagnostics.Stopwatch.Frequency} rawZero={_pr57390RawZero[cell]} finalZero={_pr57390FinalZero[cell]} physicalCalls={_pr57390PhysicalCalls} observationOnly=True noReorder=True noSkippedSlices=True");
        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36""")
p.write_text(s,encoding="utf-8")
print("PR57_390_INJECTED observational=True noDecisionReuse=True")
