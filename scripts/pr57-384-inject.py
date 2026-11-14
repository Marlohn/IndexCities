#!/usr/bin/env python3
"""PR57 #384: expose existing StepCostProfile's 13 phase timings in #337 REAL replay.

Apply only in a disposable checkout. No changes to the simulation or its scheduling.
Both arms still run 10,800 individual Advance(2); the optional allocation census is
the only A/B dimension. Phase and actor timers are intrusive nested observations.
"""
from pathlib import Path

p=Path(__file__).resolve().parents[1]/"scripts/pr57-actor-regime-337/Program.cs"
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)

patch("    var before = sim.Pr57298VehicleCostSnapshot();",
"""    var phaseBefore=(long[])sim.CostProfile!.Ticks.Clone();
    var phaseBytesBefore=(long[])sim.CostProfile.PhaseAllocatedBytes.Clone();
    var stepsBefore=sim.CostProfile.Steps;
    var before = sim.Pr57298VehicleCostSnapshot();""")
patch("    clock.Stop();\n    var after=sim.Pr57298VehicleCostSnapshot();",
"""    clock.Stop();
    var profile=sim.CostProfile!;
    if(profile.Steps-stepsBefore!=1800)throw new InvalidOperationException("#384 lost Step(2)");
    long phaseTicks=0,phaseBytes=0;
    var phaseVehicleTicks=profile.Ticks[0]-phaseBefore[0];
    for(var k=0;k<StepCostProfile.Names.Length;k++)
    {
        var ticks=profile.Ticks[k]-phaseBefore[k];
        var bytes=profile.PhaseAllocatedBytes[k]-phaseBytesBefore[k];
        if(ticks<0||bytes<0)throw new InvalidOperationException("#384 non-monotone phase counter");
        phaseTicks+=ticks;phaseBytes+=bytes;
        Console.WriteLine($"PR57_384_PHASE hour={hour} index={k} name={StepCostProfile.Names[k]} "+
            $"ms={ticks*1000.0/Stopwatch.Frequency:F3} allocatedBytes={bytes} steps=1800");
    }
    var after=sim.Pr57298VehicleCostSnapshot();""")
patch("    long actorEvaluations=0, actorBytes=0, claimChanges=0, spaceChanges=0;",
      "    long actorEvaluations=0, actorBytes=0, actorTicks=0, claimChanges=0, spaceChanges=0;")
patch("        actorEvaluations+=calls; actorBytes+=bytes; claimChanges+=claims; spaceChanges+=spaces;",
      "        actorEvaluations+=calls; actorBytes+=bytes; actorTicks+=ticks; claimChanges+=claims; spaceChanges+=spaces;")
patch("    // #155 also counts extra zero-dt operations; never assume 7 calls per Advance(2).",
"""    if(actorTicks>phaseVehicleTicks)
        throw new InvalidOperationException("#384 actor timing exceeds enclosing vehicle phase");
    var hz=Stopwatch.Frequency;
    Console.WriteLine($"PR57_384_HOUR hour={hour} steps=1800 "+
        $"phaseMs={phaseTicks*1000.0/hz:F3} vehicleMs={phaseVehicleTicks*1000.0/hz:F3} "+
        $"actorMs={actorTicks*1000.0/hz:F3} "+
        $"vehicleNonActorMs={(phaseVehicleTicks-actorTicks)*1000.0/hz:F3} "+
        $"otherPhaseMs={(phaseTicks-phaseVehicleTicks)*1000.0/hz:F3} "+
        $"outsidePhasesMs={clock.Elapsed.TotalMilliseconds-phaseTicks*1000.0/hz:F3} "+
        $"wallMs={clock.Elapsed.TotalMilliseconds:F3} phaseAllocatedBytes={phaseBytes} "+
        "scope=INTRUSIVE_NESTED_PHASES_NOT_CAUSAL_CERTIFICATE");
    // #155 also counts extra zero-dt operations; never assume 7 calls per Advance(2).""")
p.write_text(s,encoding="utf-8")
print("PR57_384_INJECTED read-only 13-phase/actor residual partition")
