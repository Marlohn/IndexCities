#!/usr/bin/env python3
"""PR57 #437: exact shared-space setup helper costs in REAL waiting actors.

Composes #432 in a disposable CI checkout. Measures only the four canonical
helper call sites during stage 1 of the sampled actor; every canonical call,
state write, actor, physical sub-slice and Advance(2) remains unchanged.
This is observational, intrusive, and does not prove causal closure.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-432-inject.py", run_name="__main__")
p = Path("src/IndexCities.Simulation/Mobility.cs")
s = p.read_text()

def once(old, new):
    global s
    count = s.count(old)
    assert count == 1, (old[:100], count)
    s = s.replace(old, new, 1)

once('    static readonly bool Pr57432Enabled=', '''    static readonly bool Pr57437Enabled =
        Environment.GetEnvironmentVariable("PR57_437_SETUP_COST") == "1";
    bool _pr57437Active;
    readonly long[] _pr57437Calls = new long[4];
    readonly long[] _pr57437Hits = new long[4];
    readonly long[] _pr57437Ticks = new long[4];
    void Pr57437Record(int index, long start, bool hit)
    {
        _pr57437Calls[index]++;
        _pr57437Ticks[index] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        if (hit) _pr57437Hits[index]++;
    }
    bool Pr57437NextShared(Vehicle v, RoadNode node, Point incoming,
        double length, out RoadNode junction, out int index)
    {
        if (!_pr57437Active)
            return Pr57273NextShared(v,node,incoming,length,out junction,out index);
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var found=Pr57273NextShared(v,node,incoming,length,out junction,out index);
        Pr57437Record(0,start,found);
        return found;
    }
    bool Pr57437HoldBefore(Vehicle v,int junctionIndex,
        out List<(Vehicle Body,Point Position,double Heading)> exits)
    {
        if (!_pr57437Active)
            return Pr57273HoldBefore(v,junctionIndex,out exits);
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var held=Pr57273HoldBefore(v,junctionIndex,out exits);
        Pr57437Record(1,start,held);
        return held;
    }
    List<(Vehicle Body,Point Position,double Heading)> Pr57437Claim(
        Vehicle v,RoadNode junction)
    {
        if (!_pr57437Active) return Pr57273Claim(v,junction);
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var result=Pr57273Claim(v,junction);
        Pr57437Record(2,start,result.Count>0);
        return result;
    }
    List<(Vehicle Body,Point Position,double Heading)> Pr57437BayEntryHold(Vehicle v)
    {
        if (!_pr57437Active) return Pr57273BayEntryHold(v);
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var result=Pr57273BayEntryHold(v);
        Pr57437Record(3,start,result.Count>0);
        return result;
    }
    static readonly bool Pr57432Enabled=''')

once('                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,1,pr57432Measure);',
     '''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,1,pr57432Measure);
                _pr57437Active=Pr57437Enabled&&pr57432Measure;''')
once('                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,2,pr57432Measure);',
     '''                Pr57432Mark(ref pr57432Stage,ref pr57432Last,_pr57432Ticks,_pr57432Visits,2,pr57432Measure);
                _pr57437Active=false;''')
once('                Pr57Effect253AfterActor(v);',
     '''                _pr57437Active=false;
                Pr57Effect253AfterActor(v);''')

once('Pr57273NextShared(v,node,delta,len,out var crossing,out var crossingIndex)',
     'Pr57437NextShared(v,node,delta,len,out var crossing,out var crossingIndex)')
once('Pr57273HoldBefore(v,crossingIndex,out var exits)',
     'Pr57437HoldBefore(v,crossingIndex,out var exits)')
once('Pr57273Claim(v,crossing)',
     'Pr57437Claim(v,crossing)')
once('EntersBay(v)&&Pr57273BayEntryHold(v) is {Count:>0} entryHold',
     'EntersBay(v)&&Pr57437BayEntryHold(v) is {Count:>0} entryHold')

once('            _pr57420Printed=true;', '''            if(Pr57437Enabled){
                var names=new[]{"next_shared","hold_before","claim_holder_poses","bay_entry_hold"};
                for(var i=0;i<names.Length;i++)
                    Console.WriteLine("PR57_437_COMPONENT component="+names[i]+
                        " calls="+_pr57437Calls[i]+" hits="+_pr57437Hits[i]+
                        " ticks="+_pr57437Ticks[i]+
                        " frequency="+System.Diagnostics.Stopwatch.Frequency+
                        " sampledPhysical=60 cohort=travelling_waiting stage=setup"+
                        " observational=True intrusive=True noDecisionReuse=True causalClosure=False");
            }
            _pr57420Printed=true;''')
p.write_text(s)
print("PR57_437_INJECTED observational=True exactSetupHelpers=True noDecisionReuse=True")
