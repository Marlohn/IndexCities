#!/usr/bin/env python3
"""PR57 #444: opt-in zero-demand electricity additive no-op POC.

Run only in disposable CI checkout. All Step/Advance(2), allocation,
ratio publications and causal ordering stay canonical. No product change.
For finite dt and exactly zero demand, the original contribution is +/-0;
FixedPoint.Add(units,0,scale) == units for any valid long units.
NaN/Infinity and all nonzero demands retain the canonical path.
"""
from pathlib import Path
p=Path('src/IndexCities.Simulation/Infrastructure.cs')
s=p.read_text()

def replace(old,new):
    global s
    assert s.count(old)==1, (old[:130],s.count(old))
    s=s.replace(old,new,1)

replace('    void UpdateInfrastructure(double dt)\n',
        '    static readonly bool Pr57444NoopAddEnabled=Environment.GetEnvironmentVariable("PR57_444_ZERO_DEMAND_ADD")=="1";\n'
        '    void UpdateInfrastructure(double dt)\n')
replace('for(var i=0;i<active.Count;i++){var b=active[i];b.ElectricityRatio=b.ElectricityDemand<=0?0:allocation.Result[i]/b.ElectricityDemand;b.AddElectricityConsumedToday(b.ElectricityDemand*b.ElectricityRatio*dt/86400);}',
'''for(var i=0;i<active.Count;i++){
            var b=active[i];
            b.ElectricityRatio=b.ElectricityDemand<=0?0:allocation.Result[i]/b.ElectricityDemand;
            // Never suppress the ratio publication, even when stale public state was edited.
            if(!Pr57444NoopAddEnabled || b.ElectricityDemand!=0 || !double.IsFinite(dt))
                b.AddElectricityConsumedToday(b.ElectricityDemand*b.ElectricityRatio*dt/86400);
        }''')
replace('for(var j=0;j<streets.Count;j++){var n=streets[j];var demand=allocation.Demands[active.Count+j];n.ElectricityRatio=demand<=0?0:allocation.Result[active.Count+j]/demand;State.AddStreetElectricityConsumedToday(demand*n.ElectricityRatio*dt/86400);}',
'''for(var j=0;j<streets.Count;j++){
            var n=streets[j];var demand=allocation.Demands[active.Count+j];
            n.ElectricityRatio=demand<=0?0:allocation.Result[active.Count+j]/demand;
            // The global accumulator remains ordered for every nonzero demand.
            if(!Pr57444NoopAddEnabled || demand!=0 || !double.IsFinite(dt))
                State.AddStreetElectricityConsumedToday(demand*n.ElectricityRatio*dt/86400);
        }''')
p.write_text(s)
print('PR57_444_INJECTED zeroDemandAddOnly=True ratioWritesPreserved=True nonFiniteFallback=True')
