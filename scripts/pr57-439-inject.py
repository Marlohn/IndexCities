#!/usr/bin/env python3
"""PR57 #439: compose existing nonvehicle observers in a disposable CI checkout.

No engine path, decision, state or save is changed. The #337 runner keeps every
public Advance(2). NET phase differences are NOT a causal skip certificate.
"""
from pathlib import Path

p = Path('scripts/pr57-actor-regime-337/Program.cs')
s = p.read_text()

def once(old: str, new: str):
    global s
    assert s.count(old) == 1, (old[:100], s.count(old))
    s = s.replace(old, new, 1)

once('sim.CostProfile = new StepCostProfile();', '''sim.CostProfile = new StepCostProfile();
var pr57439 = Environment.GetEnvironmentVariable("PR57_439_NONVEHICLE") == "1";
if (pr57439)
{
    sim.CostProfile.PhaseEffects = new Pr57PhaseEffects(300);
    sim.CostProfile.Pr57ConstructionProjects = new Pr57ConstructionProjectProbe();
}
''')
once('    var before = sim.Pr57298VehicleCostSnapshot();', '''    var before = sim.Pr57298VehicleCostSnapshot();
    var pr57439BeforeTicks = sim.CostProfile!.Ticks.ToArray();
    var pr57439BeforeBytes = sim.CostProfile.PhaseAllocatedBytes.ToArray();''')
once('    var after=sim.Pr57298VehicleCostSnapshot();', '''    var after=sim.Pr57298VehicleCostSnapshot();
    for (var phase=0;phase<StepCostProfile.Names.Length;phase++)
    {
        var phaseTicks=sim.CostProfile!.Ticks[phase]-pr57439BeforeTicks[phase];
        var phaseBytes=sim.CostProfile.PhaseAllocatedBytes[phase]-pr57439BeforeBytes[phase];
        Console.WriteLine($"PR57_439_PHASE hour={hour} name={StepCostProfile.Names[phase]} " +
            $"ms={phaseTicks*1000.0/Stopwatch.Frequency:F6} allocatedBytes={phaseBytes} " +
            $"scope=ALL_REAL_STEPS_INTRUSIVE_NO_SKIP");
    }''')
once('sim.CostProfile=null;', '''if (pr57439)
{
    foreach (var line in sim.Pr57CitizenStateCostReport()) Console.WriteLine(line);
    foreach (var line in sim.Pr57Citizen239Report()) Console.WriteLine(line);
    foreach (var line in sim.CostProfile!.PhaseEffects!.ReportLines()) Console.WriteLine(line);
    foreach (var line in sim.CostProfile.Pr57ConstructionProjects!.Rows()) Console.WriteLine(line);
    Console.WriteLine($"PR57_439_EFFECTS dispatchActiveSteps={sim.CostProfile.ConstructionDispatchActiveSteps} " +
        $"dispatchNoShipmentOrLedgerSteps={sim.CostProfile.ConstructionDispatchNoShipmentOrLedgerSteps} " +
        $"dispatchShipmentChangedSteps={sim.CostProfile.ConstructionDispatchShipmentChangedSteps} " +
        $"dispatchLedgerChangedSteps={sim.CostProfile.ConstructionDispatchLedgerChangedSteps} " +
        $"orderProjectVisits={sim.CostProfile.ConstructionOrderProjects} " +
        $"progressProjectVisits={sim.CostProfile.ConstructionProgressProjects} " +
        $"reserveProjectVisits={sim.CostProfile.ConstructionReserveProjects} " +
        "classification=SELECTED_NET_FIELDS_NOT_CAUSAL_INACTIVITY");
}
sim.CostProfile=null;''')
p.write_text(s)
print('PR57_439_INJECTED observational=True noDecisionReuse=True everyAdvance2=True')
