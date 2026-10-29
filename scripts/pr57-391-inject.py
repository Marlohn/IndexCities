#!/usr/bin/env python3
"""PR57 #391: observational, slice-local compact physical-state shadow.

Test whether publishing only the actor just processed leaves stale physical
fields when a later lane scan reads another vehicle. Never use the shadow for
a decision, never skip/reorder a vehicle or slice. Disposable CI checkout only.
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
    static readonly bool Pr57391Enabled=Environment.GetEnvironmentVariable("PR57_391_SHADOW")=="1";
    // Compact state is a lower bound: excludes nested objects, road/signals,
    // pedestrians, external commands, entry-frame and transient mutations.
    readonly record struct Pr57391State(long X,long Z,long Heading,int Status,
        long Waiting,int RouteIndex,int RouteCount,int DetourCount,
        int TowPhase,int Machine,int ManoeuvreCount);
    static Pr57391State Pr57391Capture(Vehicle v)=>new(
        BitConverter.DoubleToInt64Bits(v.Position.X),
        BitConverter.DoubleToInt64Bits(v.Position.Z),
        BitConverter.DoubleToInt64Bits(v.Heading),(int)v.Status,
        BitConverter.DoubleToInt64Bits(v.WaitingSince),v.RouteIndex,
        v.Route.Count,v.Detour.Count,(int)v.TowPhase,(int)v.Machine,
        v.ManoeuvreSpaces.Count);
    readonly Dictionary<int,Pr57391State> _pr57391States=new();
    long _pr57391PhysicalCalls,_pr57391BuildRows,_pr57391Published,
         _pr57391ChangedActors,_pr57391CandidateReads,_pr57391StaleReads,
         _pr57391StalePose,_pr57391StaleStatus,_pr57391StaleWait,
         _pr57391StaleRoute,_pr57391StaleOther,_pr57391Missing,
         _pr57391BuildTicks,_pr57391PublishTicks;
""")
patch("        Pr57SetupGapEnd(ordered.Length,pr57ScanCandidates.Count);", """        Pr57SetupGapEnd(ordered.Length,pr57ScanCandidates.Count);
        var pr57391Sample=Pr57391Enabled&&dt>0&&_pr57391PhysicalCalls<600;
        if(pr57391Sample){
            _pr57391PhysicalCalls++;
            var t=System.Diagnostics.Stopwatch.GetTimestamp();
            _pr57391States.Clear();
            foreach(var actor in State.Vehicles){
                _pr57391States.Add(actor.Id,Pr57391Capture(actor));
                _pr57391BuildRows++;
            }
            _pr57391BuildTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
        }""")
patch("                foreach(var liveOther in candidates){", """                foreach(var liveOther in candidates){
                    if(pr57391Sample){
                        _pr57391CandidateReads++;
                        if(!_pr57391States.TryGetValue(liveOther.Id,out var published))
                            _pr57391Missing++;
                        else{
                            var actual=Pr57391Capture(liveOther);
                            if(!published.Equals(actual)){
                                _pr57391StaleReads++;
                                if(published.X!=actual.X||published.Z!=actual.Z||
                                   published.Heading!=actual.Heading)_pr57391StalePose++;
                                if(published.Status!=actual.Status)_pr57391StaleStatus++;
                                if(published.Waiting!=actual.Waiting)_pr57391StaleWait++;
                                if(published.RouteIndex!=actual.RouteIndex||
                                   published.RouteCount!=actual.RouteCount)_pr57391StaleRoute++;
                                if(published.DetourCount!=actual.DetourCount||
                                   published.TowPhase!=actual.TowPhase||
                                   published.Machine!=actual.Machine||
                                   published.ManoeuvreCount!=actual.ManoeuvreCount)_pr57391StaleOther++;
                                if(_pr57391StaleReads<=8)
                                    Console.WriteLine($"PR57_391_STALE reader={v.Id} candidate={liveOther.Id} physicalCall={_pr57391PhysicalCalls} pose={(published.X!=actual.X||published.Z!=actual.Z||published.Heading!=actual.Heading)} status={(published.Status!=actual.Status)} waiting={(published.Waiting!=actual.Waiting)} route={(published.RouteIndex!=actual.RouteIndex||published.RouteCount!=actual.RouteCount)} other={(published.DetourCount!=actual.DetourCount||published.TowPhase!=actual.TowPhase||published.Machine!=actual.Machine||published.ManoeuvreCount!=actual.ManoeuvreCount)}");
                            }
                        }
                    }""")
patch("            }finally{\n                Pr57Effect253AfterActor(v);", """            }finally{
                if(pr57391Sample){
                    var t=System.Diagnostics.Stopwatch.GetTimestamp();
                    var current=Pr57391Capture(v);
                    if(_pr57391States.TryGetValue(v.Id,out var old)&&!old.Equals(current))
                        _pr57391ChangedActors++;
                    _pr57391States[v.Id]=current;
                    _pr57391Published++;
                    _pr57391PublishTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
                }
                Pr57Effect253AfterActor(v);""")
patch("        // A towed vehicle moves only with its tow (platform or trailing a heavy tow).", """        if(pr57391Sample&&_pr57391PhysicalCalls==600)
            Console.WriteLine($"PR57_391_SHADOW physicalCalls={_pr57391PhysicalCalls} buildRows={_pr57391BuildRows} published={_pr57391Published} changedActors={_pr57391ChangedActors} candidateReads={_pr57391CandidateReads} staleReads={_pr57391StaleReads} stalePose={_pr57391StalePose} staleStatus={_pr57391StaleStatus} staleWait={_pr57391StaleWait} staleRoute={_pr57391StaleRoute} staleOther={_pr57391StaleOther} missing={_pr57391Missing} buildTicks={_pr57391BuildTicks} publishTicks={_pr57391PublishTicks} frequency={System.Diagnostics.Stopwatch.Frequency} observational=True noDecisionReuse=True noSkippedSlices=True");
        // A towed vehicle moves only with its tow (platform or trailing a heavy tow).""")
p.write_text(s,encoding="utf-8")
print("PR57_391_INJECTED observational=True compactShadow=True noDecisionReuse=True")
