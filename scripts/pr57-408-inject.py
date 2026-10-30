#!/usr/bin/env python3
"""#408 observational winner-cause census in canonical lane scan; disposable checkout only."""
from pathlib import Path
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()
def once(old,new):
    global s
    assert s.count(old)==1,(old[:90],s.count(old))
    s=s.replace(old,new,1)
once("public sealed partial class CitySimulation\n{", """public sealed partial class CitySimulation
{
    static readonly bool Pr57408Enabled=Environment.GetEnvironmentVariable("PR57_408_WINNER_CAUSE")=="1";
    long _pr57408Physical,_pr57408Scans,_pr57408Candidates,_pr57408Winners,_pr57408Binding;
    bool _pr57408Printed;
    // wait-at-actor-entry (2) x winner cause (7) x binding (2)
    // 0 none, 1 dock, 2 coincidence, 3 entry, 4 detour, 5 tow, 6 ordinary.
    readonly long[] _pr57408Cells=new long[28],_pr57408Holds=new long[7];
""")
once("        Pr57SetupGapStart(dt,operationsDt);", """        var pr57408Sample=Pr57408Enabled&&dt>0&&operationsDt is null&&_pr57408Physical<600;
        if(pr57408Sample)_pr57408Physical++;
        Pr57SetupGapStart(dt,operationsDt);""")
once("        foreach(var v in ordered){", """        foreach(var v in ordered){
            var pr57408Wait=pr57408Sample&&v.WaitingSince>0;""")
once("                var leaderDistance=double.MaxValue;Vehicle? blocker=null;", """                var leaderDistance=double.MaxValue;Vehicle? blocker=null;
                var pr57408Cause=0;""")
once("                void HoldFor(Vehicle other,double distance){Pr57PhaseImpactHold(other,distance);Pr57CausalHold(v,other);if(scanProfile is not null){laneCandidateHits++;scanProfile.ObserveLaneHoldDistance(v.Position,other.Position,enteringLane);}if(distance<leaderDistance){leaderDistance=distance;blocker=other;}}",
     "                void HoldFor(Vehicle other,double distance,int cause){Pr57PhaseImpactHold(other,distance);Pr57CausalHold(v,other);if(pr57408Sample)_pr57408Holds[cause]++;if(scanProfile is not null){laneCandidateHits++;scanProfile.ObserveLaneHoldDistance(v.Position,other.Position,enteringLane);}if(distance<leaderDistance){leaderDistance=distance;blocker=other;if(pr57408Sample)pr57408Cause=cause;}}")
once("HoldFor(other,docked);continue;", "HoldFor(other,docked,1);continue;")
once("HoldFor(other,0);continue;", "HoldFor(other,0,2);continue;")
once("HoldFor(other,0);continue;", "HoldFor(other,0,3);continue;")
once("HoldFor(other,ahead);continue;", "HoldFor(other,ahead,4);continue;")
once("HoldFor(other,forward);continue;", "HoldFor(other,forward,5);continue;")
once("HoldFor(other,forward);", "HoldFor(other,forward,6);")
once("                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));", """                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));
                if(pr57408Sample){
                    var rawCap=Math.Min(d,speed*remaining);
                    var binding=blocker is not null&&Math.Max(0,leaderDistance-8)<rawCap;
                    _pr57408Cells[(pr57408Wait?14:0)+pr57408Cause*2+(binding?1:0)]++;
                    _pr57408Scans++;
                    _pr57408Candidates+=pr57LaneList.Count;
                    if(blocker is not null)_pr57408Winners++;
                    if(binding)_pr57408Binding++;
                }""")
once("        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36", """        if(pr57408Sample&&_pr57408Physical==600&&!_pr57408Printed){
            _pr57408Printed=true;
            for(var wait=0;wait<2;wait++)
                for(var cause=0;cause<7;cause++)
                    for(var binding=0;binding<2;binding++)
                        Console.WriteLine($"PR57_408_CELL wait={wait} cause={cause} binding={binding} scans={_pr57408Cells[wait*14+cause*2+binding]} observational=True");
            for(var cause=0;cause<7;cause++)
                Console.WriteLine($"PR57_408_HOLD cause={cause} attempts={_pr57408Holds[cause]} observational=True");
            Console.WriteLine($"PR57_408_SUMMARY physical={_pr57408Physical} scans={_pr57408Scans} candidates={_pr57408Candidates} winners={_pr57408Winners} binding={_pr57408Binding} observational=True noSkippedSlices=True causalClosure=False");
        }
        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36""")
p.write_text(s)
print("PR57_408_INJECTED observational=True noSkippedSlices=True")
