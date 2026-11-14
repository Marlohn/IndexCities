#!/usr/bin/env python3
"""PR57 #345: checkout-only hourly phase census using the existing REAL #337 harness.

The simulation and its canonical calls are untouched. The existing opt-in
PR57_HOT_TIMING counters in Mobility.cs measure nested subregions; they are
not additive and cannot be interpreted as acceleration.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
target = root / "scripts/pr57-actor-regime-337/Program.cs"
source = target.read_text(encoding="utf-8")

def replace_once(old: str, new: str) -> None:
    global source
    count = source.count(old)
    if count != 1:
        raise RuntimeError(f"#345 expected one harness anchor, found {count}: {old!r}")
    source = source.replace(old, new, 1)

replace_once(
    "    var before = sim.Pr57298VehicleCostSnapshot();",
    """    var before = sim.Pr57298VehicleCostSnapshot();
    var pr57345PhaseBefore = (long[])sim.CostProfile!.Ticks.Clone();"""
)
replace_once(
    "    clock.Stop();\n    var after=sim.Pr57298VehicleCostSnapshot();",
    """    clock.Stop();
    var pr57345PhaseAfter = sim.CostProfile!.Ticks;
    for (var phase=0;phase<StepCostProfile.Names.Length;phase++)
        Console.WriteLine($"PR57_345_PHASE hour={hour} phase={StepCostProfile.Names[phase]} ms={(pr57345PhaseAfter[phase]-pr57345PhaseBefore[phase])*1000.0/Stopwatch.Frequency:F3} scope=CANONICAL_PHASE_INTRUSIVE");
    var after=sim.Pr57298VehicleCostSnapshot();"""
)
target.write_text(source, encoding="utf-8")
print("PR57_345_INJECTED: only harness hourly phase counters; no simulation changes")
