#!/usr/bin/env python3
"""PR57 #392: split #391 stale reads into self versus cross-actor reads.

The self state can change *inside* its actor before the lane scan; a shadow
published only at actor exit is expected to be stale for that actor. This
observational refinement tests the actual cross-actor publication hypothesis.
"""
import subprocess
from pathlib import Path
subprocess.run(["python3","scripts/pr57-391-inject.py"],check=True)
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)
patch("    static Pr57391State Pr57391Capture(Vehicle v)=>new(", """    long _pr57392SelfStale,_pr57392CrossStale,_pr57392CrossPose,
         _pr57392CrossRoute,_pr57392CrossStatus,_pr57392CrossWait,
         _pr57392CrossOther;
    static Pr57391State Pr57391Capture(Vehicle v)=>new(""")
patch("                                _pr57391StaleReads++;", """                                _pr57391StaleReads++;
                                if(liveOther.Id==v.Id)_pr57392SelfStale++;
                                else{
                                    _pr57392CrossStale++;
                                    if(published.X!=actual.X||published.Z!=actual.Z||
                                       published.Heading!=actual.Heading)_pr57392CrossPose++;
                                    if(published.Status!=actual.Status)_pr57392CrossStatus++;
                                    if(published.Waiting!=actual.Waiting)_pr57392CrossWait++;
                                    if(published.RouteIndex!=actual.RouteIndex||
                                       published.RouteCount!=actual.RouteCount)_pr57392CrossRoute++;
                                    if(published.DetourCount!=actual.DetourCount||
                                       published.TowPhase!=actual.TowPhase||
                                       published.Machine!=actual.Machine||
                                       published.ManoeuvreCount!=actual.ManoeuvreCount)_pr57392CrossOther++;
                                    if(_pr57392CrossStale<=8)
                                        Console.WriteLine($"PR57_392_CROSS reader={v.Id} candidate={liveOther.Id} physicalCall={_pr57391PhysicalCalls} pose={(published.X!=actual.X||published.Z!=actual.Z||published.Heading!=actual.Heading)} status={(published.Status!=actual.Status)} waiting={(published.Waiting!=actual.Waiting)} route={(published.RouteIndex!=actual.RouteIndex||published.RouteCount!=actual.RouteCount)} other={(published.DetourCount!=actual.DetourCount||published.TowPhase!=actual.TowPhase||published.Machine!=actual.Machine||published.ManoeuvreCount!=actual.ManoeuvreCount)}");
                                }""")
patch("        // A towed vehicle moves only with its tow (platform or trailing a heavy tow).", """        if(pr57391Sample&&_pr57391PhysicalCalls==600)
            Console.WriteLine($"PR57_392_SPLIT physicalCalls={_pr57391PhysicalCalls} selfStale={_pr57392SelfStale} crossStale={_pr57392CrossStale} crossPose={_pr57392CrossPose} crossStatus={_pr57392CrossStatus} crossWait={_pr57392CrossWait} crossRoute={_pr57392CrossRoute} crossOther={_pr57392CrossOther} observational=True");
        // A towed vehicle moves only with its tow (platform or trailing a heavy tow).""")
p.write_text(s,encoding="utf-8")
print("PR57_392_INJECTED observational=True crossActorSplit=True")
