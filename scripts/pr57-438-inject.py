#!/usr/bin/env python3
"""PR57 #438: attribute negative BayEntryHold to exact EntryPlan phases.

Opt-in observational CI checkout. Composes #437 and reuses the existing #277
stage timers with a different, precisely bounded cohort. No decisions reused,
no skipped slices, no physical/economic rule changes. Timers are intrusive.
"""
from pathlib import Path
import runpy
runpy.run_path("scripts/pr57-437-inject.py",run_name="__main__")

def once(path,old,new):
    p=Path(path)
    s=p.read_text()
    n=s.count(old)
    assert n==1,(path,old[:100],n)
    p.write_text(s.replace(old,new,1))

mob="src/IndexCities.Simulation/Mobility.cs"
probe="src/IndexCities.Simulation/Mobility.EntryPlanProbe277.Pr57.cs"
gate="src/IndexCities.Simulation/Mobility.GateProbe276.Pr57.cs"

once(mob,'    static readonly bool Pr57437Enabled =','''    bool _pr57438InBay;
    readonly Dictionary<int,int> _pr57438BayIds=new();
    readonly long[] _pr57438Gates=new long[13];
    long _pr57438BayCalls;
    static readonly bool Pr57437Enabled =''')
once(mob,'''    List<(Vehicle Body,Point Position,double Heading)> Pr57437BayEntryHold(Vehicle v)
    {
        if (!_pr57437Active) return Pr57273BayEntryHold(v);
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var result=Pr57273BayEntryHold(v);
        Pr57437Record(3,start,result.Count>0);
        return result;
    }''','''    List<(Vehicle Body,Point Position,double Heading)> Pr57437BayEntryHold(Vehicle v)
    {
        if (!_pr57437Active) return Pr57273BayEntryHold(v);
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        if(Pr57277Enabled)
        {
            _pr57438InBay=true;
            _pr57438BayCalls++;
            _pr57438BayIds[v.Id]=_pr57438BayIds.GetValueOrDefault(v.Id)+1;
        }
        try
        {
            var result=Pr57273BayEntryHold(v);
            Pr57437Record(3,start,result.Count>0);
            return result;
        }
        finally { _pr57438InBay=false; }
    }''')
once(mob,'            _pr57420Printed=true;','''            if(Pr57277Enabled)
            {
                Console.WriteLine("PR57_438_SUM bayCalls="+_pr57438BayCalls+
                    " observational=True intrusive=True noDecisionReuse=True causalClosure=False");
                foreach(var pair in _pr57438BayIds.OrderBy(p=>p.Key))
                    Console.WriteLine("PR57_438_ID id="+pair.Key+" calls="+pair.Value);
                for(var i=0;i<_pr57438Gates.Length;i++)
                    if(_pr57438Gates[i]>0)
                        Console.WriteLine("PR57_438_GATE gate="+i+" calls="+_pr57438Gates[i]);
                Pr57277Report();
            }
            _pr57420Printed=true;''')

once(probe,'''        Pr57277Enabled && _pr57273Sampling && v.Id == 23''',
'''        Pr57277Enabled && _pr57438InBay''')
once(probe,'''        if(!Pr57277Enabled||!_pr57273Sampling||v.Id!=23)return;''',
'''        if(!Pr57277Enabled||!_pr57438InBay)return;''')
once(probe,'''        var w=_pr57273Window;''','''        var w=0;''')
once(probe,'''        _pr57277Ticks[_pr57273Window,stage]+=now-start;''',
'''        _pr57277Ticks[0,stage]+=now-start;''')
once(gate,'        if (!Pr57276Enabled || !_pr57273Sampling) return;',
'''        if(_pr57438InBay)_pr57438Gates[gate]++;
        if (!Pr57276Enabled || !_pr57273Sampling) return;''')
print("PR57_438_INJECTED exactEntryPlanStages=True observational=True noDecisionReuse=True")
