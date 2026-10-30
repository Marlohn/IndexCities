#!/usr/bin/env python3
"""#434: isolate junction admission vs actual movement in REAL waiting actors.

Strictly observational opt-in, injected only into a disposable checkout.
Canonical Advance(2), every sub-slice, candidate, decision and effect remain.
Nested MayEnterJunction time is NOT additive to admission time.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-432-inject.py", run_name="__main__")
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()

def once(old,new):
    global s
    n=s.count(old)
    assert n==1,(old[:110],n)
    s=s.replace(old,new,1)

once('    static readonly bool Pr57432Enabled=Environment.GetEnvironmentVariable("PR57_432_STAGE_COST")=="1";',
'''    static readonly bool Pr57432Enabled=Environment.GetEnvironmentVariable("PR57_432_STAGE_COST")=="1";
    static readonly bool Pr57434Enabled=Environment.GetEnvironmentVariable("PR57_434_ADMISSION_COST")=="1";
    long _pr57434AdmissionVisits,_pr57434AdmissionTicks,_pr57434SharedAhead,_pr57434EntryHeld;
    long _pr57434MayCalls,_pr57434MayAllowed,_pr57434MayDenied,_pr57434MayTicks;
    long _pr57434MovementVisits,_pr57434MovementTicks;''')

once('            var pr57432Stage=0;',
'''            var pr57432Stage=0;
            var pr57434Measure=Pr57434Enabled&&pr57432Measure;''')

once('                Pr57ExclusiveMark(7);',
'''                Pr57ExclusiveMark(7);
                var pr57434AdmissionStart=pr57434Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;''')

once('                    var sharedAhead=node.Neighbours.Count>=3||node.Crosswalk||TurnsAtBend(v,v.RouteIndex);',
'''                    var sharedAhead=node.Neighbours.Count>=3||node.Crosswalk||TurnsAtBend(v,v.RouteIndex);
                    if(pr57434Measure&&sharedAhead)_pr57434SharedAhead++;''')

once('                        v.Cause="Fila para acesso de carga";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;',
'''                        if(pr57434Measure)_pr57434EntryHeld++;
                        v.Cause="Fila para acesso de carga";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;''')

once('                        try{Pr57CausalJunction(v,node);Pr57JunctionCensusObserve(v,node);Pr57JunctionReadersAdmission(v,node);junctionAllowed=MayEnterJunction(v,node);}',
'''                        try{
                            Pr57CausalJunction(v,node);Pr57JunctionCensusObserve(v,node);Pr57JunctionReadersAdmission(v,node);
                            var pr57434MayStart=pr57434Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                            try{junctionAllowed=MayEnterJunction(v,node);}
                            finally{
                                if(pr57434Measure){
                                    _pr57434MayCalls++;
                                    _pr57434MayTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57434MayStart;
                                }
                            }
                            if(pr57434Measure){
                                if(junctionAllowed)_pr57434MayAllowed++;
                                else _pr57434MayDenied++;
                            }
                        }''')

once('                }finally{\n                    if(scanProfile is not null)_pr57RegionAllocBytes[6]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;\n                }',
'''                }finally{
                    if(scanProfile is not null)_pr57RegionAllocBytes[6]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;
                    if(pr57434Measure){
                        _pr57434AdmissionVisits++;
                        _pr57434AdmissionTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57434AdmissionStart;
                    }
                }''')

once('                Pr57ExclusiveMark(8);\n                if(v.Fuel<=.000001)',
'''                Pr57ExclusiveMark(8);
                var pr57434MovementStart=pr57434Measure?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                try{
                if(v.Fuel<=.000001)''')

once('                if(DriveWear(v,step)||steer==null&&!advanced)break;\n            }\n            }finally{',
'''                if(DriveWear(v,step)||steer==null&&!advanced)break;
                }finally{
                    if(pr57434Measure){
                        _pr57434MovementVisits++;
                        _pr57434MovementTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57434MovementStart;
                    }
                }
            }
            }finally{''')

once('            _pr57420Printed=true;',
'''            if(Pr57434Enabled)
                Console.WriteLine($"PR57_434_SUMMARY sampledPhysical=60 cohort=travelling_waiting admissionVisits={_pr57434AdmissionVisits} admissionTicks={_pr57434AdmissionTicks} sharedAhead={_pr57434SharedAhead} entryHeld={_pr57434EntryHeld} mayCalls={_pr57434MayCalls} mayAllowed={_pr57434MayAllowed} mayDenied={_pr57434MayDenied} mayTicks={_pr57434MayTicks} movementVisits={_pr57434MovementVisits} movementTicks={_pr57434MovementTicks} frequency={System.Diagnostics.Stopwatch.Frequency} observational=True intrusive=True noDecisionReuse=True causalClosure=False");
            _pr57420Printed=true;''')

p.write_text(s)
print("PR57_434_INJECTED observational=True noSkippedSlices=True noDecisionReuse=True")
