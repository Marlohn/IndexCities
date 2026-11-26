#!/usr/bin/env python3
"""PR57 #424: classify the time horizon of the first canonical lane scan.

Compose the already observational #422 in a disposable checkout. No physics,
ordering, decisions, actor or slice is skipped. Equality is IEEE-754 bitwise.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-422-inject.py", run_name="__main__")
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()
def once(old,new):
    global s
    assert s.count(old)==1,(old[:100],s.count(old))
    s=s.replace(old,new,1)

once(
    "    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,\n",
    """    static readonly bool Pr57424Enabled=Environment.GetEnvironmentVariable("PR57_424_HORIZON_SHADOW")=="1";
    // 0=first scans, 1=remaining equals dt, 2=differs, 3=changed reader,
    // 4=dt changed, 5=dt unchanged, 6=both scans had remaining=dt,
    // 7=at least one scan had remaining!=dt.
    readonly long[] _pr57424Counts=new long[8];
    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,
"""
)
once(
    "long Poses,long Bend,long Site,long Crossing,long Free);",
    "long Poses,long Bend,long Site,long Crossing,long Free,long Dt);"
)
once(
    "Pr57420Bits(afterCrossingStep),Pr57420Bits(free));",
    "Pr57420Bits(afterCrossingStep),Pr57420Bits(free),Pr57420Bits(dt));"
)
once(
    "                        if(_pr57420Last.TryGetValue(v.Id,out var prior)&&prior.Slice==_pr57420Physical-1){",
    """                        if(Pr57424Enabled){
                            _pr57424Counts[0]++;
                            if(now.Remaining==now.Dt)_pr57424Counts[1]++;
                            else _pr57424Counts[2]++;
                        }
                        if(_pr57420Last.TryGetValue(v.Id,out var prior)&&prior.Slice==_pr57420Physical-1){"""
)
once(
    "                                _pr57422ReaderChanged++;",
    """                                _pr57422ReaderChanged++;
                                if(Pr57424Enabled){
                                    _pr57424Counts[3]++;
                                    if(now.Dt!=prior.Dt)_pr57424Counts[4]++;
                                    else _pr57424Counts[5]++;
                                    if(now.Remaining==now.Dt&&prior.Remaining==prior.Dt)
                                        _pr57424Counts[6]++;
                                    else _pr57424Counts[7]++;
                                }"""
)
once(
    '            if(Pr57422Enabled){\n                Console.WriteLine("PR57_422_SUMMARY physical="',
    '''            if(Pr57422Enabled){
                if(Pr57424Enabled)
                    Console.WriteLine("PR57_424_SUMMARY physical="+_pr57420Physical+
                        " first="+_pr57424Counts[0]+
                        " remainingEqDt="+_pr57424Counts[1]+
                        " remainingNeDt="+_pr57424Counts[2]+
                        " changed="+_pr57424Counts[3]+
                        " dtChanged="+_pr57424Counts[4]+
                        " dtSame="+_pr57424Counts[5]+
                        " eqDtBoth="+_pr57424Counts[6]+
                        " preConsumedEither="+_pr57424Counts[7]+
                        " observational=True causalClosure=False noDecisionReuse=True");
                Console.WriteLine("PR57_422_SUMMARY physical="'''
)
p.write_text(s)
print("PR57_424_INJECTED observational=True noSkippedSlices=True noDecisionReuse=True")
