#!/usr/bin/env python3
"""#435: actual MayEnterJunction return-reason and inclusive cost census.

Strictly observational and sampled on the same REAL waiting-actor calls as
#434. No alternate predicate, skipped slice, changed order or decision reuse.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-434-inject.py", run_name="__main__")
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()

def once(old,new):
    global s
    n=s.count(old)
    assert n==1,(old[:100],n)
    s=s.replace(old,new,1)

once('    static readonly bool Pr57434Enabled=Environment.GetEnvironmentVariable("PR57_434_ADMISSION_COST")=="1";',
'''    static readonly bool Pr57434Enabled=Environment.GetEnvironmentVariable("PR57_434_ADMISSION_COST")=="1";
    static readonly bool Pr57435Enabled=Environment.GetEnvironmentVariable("PR57_435_REASON_COST")=="1";
    bool _pr57435Active;
    long _pr57435StartTicks;
    readonly long[] _pr57435ReasonCalls=new long[18],_pr57435ReasonTicks=new long[18];''')

once('                            try{junctionAllowed=MayEnterJunction(v,node);}',
'''                            try{
                                if(pr57434Measure&&Pr57435Enabled){
                                    _pr57435Active=true;
                                    _pr57435StartTicks=System.Diagnostics.Stopwatch.GetTimestamp();
                                }
                                junctionAllowed=MayEnterJunction(v,node);
                            }''')

once('                            finally{\n                                if(pr57434Measure){\n                                    _pr57434MayCalls++;',
'''                            finally{
                                _pr57435Active=false;
                                if(pr57434Measure){
                                    _pr57434MayCalls++;''')

once('            _pr57420Printed=true;',
'''            if(Pr57435Enabled)
                foreach(var reason in Enum.GetValues<Pr57JunctionExit>())
                    Console.WriteLine($"PR57_435_REASON reason={reason} calls={_pr57435ReasonCalls[(int)reason]} ticks={_pr57435ReasonTicks[(int)reason]} frequency={System.Diagnostics.Stopwatch.Frequency} sampledPhysical=60 cohort=travelling_waiting observational=True intrusive=True noDecisionReuse=True causalClosure=False");
            _pr57420Printed=true;''')

p.write_text(s)

p=Path("src/IndexCities.Simulation/Mobility.JunctionReturns.Pr57.cs")
s=p.read_text()
once('''    bool Pr57JunctionDecision(Vehicle reader, RoadNode junction, Pr57JunctionExit reason, bool allowed)
    {
        if (reason == Pr57JunctionExit.Pedestrian)''',
'''    bool Pr57JunctionDecision(Vehicle reader, RoadNode junction, Pr57JunctionExit reason, bool allowed)
    {
        if(_pr57435Active){
            _pr57435ReasonCalls[(int)reason]++;
            _pr57435ReasonTicks[(int)reason]+=System.Diagnostics.Stopwatch.GetTimestamp()-_pr57435StartTicks;
        }
        if (reason == Pr57JunctionExit.Pedestrian)''')
p.write_text(s)
print("PR57_435_INJECTED observational=True exactReturnSites=True noDecisionReuse=True")
