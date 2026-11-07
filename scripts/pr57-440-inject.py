#!/usr/bin/env python3
"""PR57 #440: expose existing per-Stage DetailTicks, observational only.

Applies solely to a disposable CI checkout. All public Advance(2) calls,
physical slices, economy, histories, mutations and save semantics stay intact.
The DetailTicks are already recorded whenever CostProfile is present.
"""
from pathlib import Path

p = Path("scripts/pr57-actor-regime-337/Program.cs")
s = p.read_text()

def once(old, new):
    global s
    assert s.count(old) == 1, (old[:100], s.count(old))
    s = s.replace(old, new, 1)

once("    var before = sim.Pr57298VehicleCostSnapshot();", """    var before = sim.Pr57298VehicleCostSnapshot();
    var pr57440DetailBefore = sim.CostProfile!.DetailTicks.ToArray();
    var pr57440PhaseBefore = sim.CostProfile.Ticks.ToArray();""")

once("    var after=sim.Pr57298VehicleCostSnapshot();", """    var after=sim.Pr57298VehicleCostSnapshot();
    for (var d=0;d<StepCostProfile.DetailNames.Length;d++)
    {
        var ticks=sim.CostProfile!.DetailTicks[d]-pr57440DetailBefore[d];
        Console.WriteLine($"PR57_440_STAGE hour={hour} name={StepCostProfile.DetailNames[d]} ticks={ticks} ms={ticks*1000.0/Stopwatch.Frequency:F6} scope=CANONICAL_DETAIL_TICKS_NO_SKIP");
    }
    for (var phase=0;phase<StepCostProfile.Names.Length;phase++)
    {
        var ticks=sim.CostProfile!.Ticks[phase]-pr57440PhaseBefore[phase];
        Console.WriteLine($"PR57_440_PHASE hour={hour} name={StepCostProfile.Names[phase]} ticks={ticks} ms={ticks*1000.0/Stopwatch.Frequency:F6}");
    }
    Console.WriteLine($"PR57_440_STATE hour={hour} living={sim.State.Citizens.Count(c=>c.Alive)} vehicles={sim.State.Vehicles.Count} activeBuildings={sim.State.Buildings.Count(b=>b.Completed&&!b.Removed)} roads={sim.State.Roads.Count} citizens={sim.State.Citizens.Count} elapsedUnits={sim.State.ElapsedSecondsUnits}");""")
p.write_text(s)
print("PR57_440_INJECTED observational=True noDecisionReuse=True everyAdvance2=True")
