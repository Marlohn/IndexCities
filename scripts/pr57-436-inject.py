#!/usr/bin/env python3
"""#436: isolate exact BodyOnWay and TurnWay costs inside sampled REAL MayEnterJunction.

Composes #435 in a disposable CI checkout. No alternate physics, skipped actors,
decision reuse or state publication. Timers are intrusive and exclusive of
each other; #435 return-reason timings are inclusive and must NOT be added.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-435-inject.py", run_name="__main__")
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()

def once(old,new):
    global s
    n=s.count(old)
    assert n==1,(old[:100],n)
    s=s.replace(old,new,1)

once('    bool _pr57435Active;\n    long _pr57435StartTicks;',
'''    bool _pr57435Active;
    long _pr57435StartTicks;
    static readonly bool Pr57436Enabled=Environment.GetEnvironmentVariable("PR57_436_BODY_COST")=="1";
    readonly long[] _pr57436Calls=new long[4],_pr57436Ticks=new long[4],_pr57436Hits=new long[4];''')

def measured(original, variable, index, hit=False):
    stamp=f"pr57436Start{index}"
    suffix=f"if({stamp}!=0){{_pr57436Calls[{index}]++;_pr57436Ticks[{index}]+=System.Diagnostics.Stopwatch.GetTimestamp()-{stamp};"
    if hit:
        suffix+=f"if({variable} is not null)_pr57436Hits[{index}]++;"
    suffix+="}"
    return f"""var {stamp}=_pr57435Active&&Pr57436Enabled?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            {original}
            {suffix}"""

once('var way=TurnWay(v,out var spaces);',
     measured('var way=TurnWay(v,out var spaces);','way',0))
once('var bendBody=BodyOnWay(v,way);',
     measured('var bendBody=BodyOnWay(v,way);','bendBody',1,True))
once('var onward=TurnWay(v,out var crossed);',
     measured('var onward=TurnWay(v,out var crossed);','onward',2))
once('var onwardBody=BodyOnWay(v,onward,Pr57305Enabled);',
     measured('var onwardBody=BodyOnWay(v,onward,Pr57305Enabled);','onwardBody',3,True))

once('            _pr57420Printed=true;',
'''            if(Pr57436Enabled){
                var names=new[]{"bend_turn_way","bend_body_on_way","onward_turn_way","onward_body_on_way"};
                for(var i=0;i<names.Length;i++)
                    Console.WriteLine($"PR57_436_COMPONENT component={names[i]} calls={_pr57436Calls[i]} hits={_pr57436Hits[i]} ticks={_pr57436Ticks[i]} frequency={System.Diagnostics.Stopwatch.Frequency} sampledPhysical=60 cohort=travelling_waiting observational=True intrusive=True noDecisionReuse=True causalClosure=False");
            }
            _pr57420Printed=true;''')

p.write_text(s)
print("PR57_436_INJECTED observational=True exactBodySites=True noDecisionReuse=True")
