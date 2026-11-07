#!/usr/bin/env python3
"""#420: observational cohort/decision persistence in real physical slices.

Disposable CI checkout only. Canonical actor ordering, scans, safety, admission,
mutations, economy and saves are untouched. Not a cache or causal closure proof.
"""
from pathlib import Path
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()
def once(old,new):
    global s
    n=s.count(old)
    assert n==1,(old[:110],n)
    s=s.replace(old,new,1)

once("    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)\n    {", """    static readonly bool Pr57420Enabled=Environment.GetEnvironmentVariable("PR57_420_COHORT_SHADOW")=="1";
    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,
        long X,long Z,long Heading,int Blocker,long Distance,long Step);
    readonly Dictionary<int,Pr57420Decision> _pr57420Last=new();
    readonly Dictionary<int,long> _pr57420Seen=new();
    readonly long[] _pr57420ActorCalls=new long[4],_pr57420ActorStable=new long[4],_pr57420ActorTicks=new long[4];
    long _pr57420Physical,_pr57420Scans,_pr57420FirstScans,_pr57420Consecutive,
         _pr57420SameReader,_pr57420SameBlocker,_pr57420SameDistance,_pr57420SameStep,
         _pr57420SameReaderDecision,_pr57420SameDecision;
    bool _pr57420Printed;
    static long Pr57420Bits(double d)=>BitConverter.DoubleToInt64Bits(d);
    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)
    {""")

once("        Pr57SetupGapStart(dt,operationsDt);", """        var pr57420Sample=Pr57420Enabled&&dt>0&&operationsDt is null&&_pr57420Physical<600;
        if(pr57420Sample)_pr57420Physical++;
        Pr57SetupGapStart(dt,operationsDt);""")

once("        foreach(var v in ordered){\n            Pr57Effect253BeforeActor(v);", """        foreach(var v in ordered){
            var pr57420Kind=0;
            var pr57420Start=0L;
            var pr57420Status=v.Status;
            var pr57420Position=v.Position;
            var pr57420Heading=v.Heading;
            var pr57420Waiting=v.WaitingSince;
            var pr57420RouteIndex=v.RouteIndex;
            var pr57420RouteCount=v.Route.Count;
            var pr57420Fuel=v.Fuel;
            var pr57420Cause=v.Cause;
            if(pr57420Sample){
                pr57420Kind=v.Status!=VehicleStatus.Travelling?0:
                    v.Detour.Count>0||v.SitePathBeforeRoad||v.TowPhase!=TowPhase.None?3:
                    v.WaitingSince>0?1:2;
                _pr57420ActorCalls[pr57420Kind]++;
                pr57420Start=System.Diagnostics.Stopwatch.GetTimestamp();
            }
            Pr57Effect253BeforeActor(v);""")

once("                if(steer is {} bend){v.Heading=bend.Heading;target=bend.To;step=bend.Distance;}", """                if(steer is {} bend){v.Heading=bend.Heading;target=bend.To;step=bend.Distance;}
                if(pr57420Sample){
                    _pr57420Scans++;
                    if(!_pr57420Seen.TryGetValue(v.Id,out var seen)||seen!=_pr57420Physical){
                        _pr57420Seen[v.Id]=_pr57420Physical;
                        _pr57420FirstScans++;
                        var now=new Pr57420Decision(_pr57420Physical,node.Id,enteringLane,
                            v.RouteIndex,v.WaitingSince>0,Pr57420Bits(v.Position.X),
                            Pr57420Bits(v.Position.Z),Pr57420Bits(v.Heading),
                            blocker?.Id??-1,Pr57420Bits(leaderDistance),Pr57420Bits(step));
                        if(_pr57420Last.TryGetValue(v.Id,out var prior)&&prior.Slice==_pr57420Physical-1){
                            _pr57420Consecutive++;
                            var reader=now.Node==prior.Node&&now.Entry==prior.Entry&&
                                now.RouteIndex==prior.RouteIndex&&now.Waiting==prior.Waiting&&
                                now.X==prior.X&&now.Z==prior.Z&&now.Heading==prior.Heading;
                            var blockerSame=now.Blocker==prior.Blocker;
                            var distanceSame=now.Distance==prior.Distance;
                            var stepSame=now.Step==prior.Step;
                            if(reader)_pr57420SameReader++;
                            if(blockerSame)_pr57420SameBlocker++;
                            if(distanceSame)_pr57420SameDistance++;
                            if(stepSame)_pr57420SameStep++;
                            if(blockerSame&&distanceSame&&stepSame)_pr57420SameDecision++;
                            if(reader&&blockerSame&&distanceSame&&stepSame)_pr57420SameReaderDecision++;
                        }
                        _pr57420Last[v.Id]=now;
                    }
                }""")

once("                Pr57ActorShadowAfterActor(v);", """                Pr57ActorShadowAfterActor(v);
                if(pr57420Sample){
                    _pr57420ActorTicks[pr57420Kind]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57420Start;
                    if(v.Status==pr57420Status&&v.RouteIndex==pr57420RouteIndex&&
                       v.Route.Count==pr57420RouteCount&&
                       Pr57420Bits(v.Position.X)==Pr57420Bits(pr57420Position.X)&&
                       Pr57420Bits(v.Position.Z)==Pr57420Bits(pr57420Position.Z)&&
                       Pr57420Bits(v.Heading)==Pr57420Bits(pr57420Heading)&&
                       Pr57420Bits(v.WaitingSince)==Pr57420Bits(pr57420Waiting)&&
                       Pr57420Bits(v.Fuel)==Pr57420Bits(pr57420Fuel)&&
                       string.Equals(v.Cause,pr57420Cause,StringComparison.Ordinal))
                        _pr57420ActorStable[pr57420Kind]++;
                }""")

once("        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36", """        if(pr57420Sample&&_pr57420Physical==600&&!_pr57420Printed){
            _pr57420Printed=true;
            for(var k=0;k<4;k++)
                Console.WriteLine($"PR57_420_COHORT kind={k} actors={_pr57420ActorCalls[k]} scalarStable={_pr57420ActorStable[k]} ticks={_pr57420ActorTicks[k]} frequency={System.Diagnostics.Stopwatch.Frequency} observational=True");
            Console.WriteLine($"PR57_420_SUMMARY physical={_pr57420Physical} scans={_pr57420Scans} firstScans={_pr57420FirstScans} consecutive={_pr57420Consecutive} sameReader={_pr57420SameReader} sameBlocker={_pr57420SameBlocker} sameDistance={_pr57420SameDistance} sameStep={_pr57420SameStep} sameDecision={_pr57420SameDecision} sameReaderDecision={_pr57420SameReaderDecision} observational=True noSkippedSlices=True noDecisionReuse=True causalClosure=False scalarOnly=True");
        }
        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36""")

p.write_text(s)
print("PR57_420_INJECTED observational=True noSkippedSlices=True noDecisionReuse=True")
