#!/usr/bin/env python3
"""PR57 #442: mutually exclusive electric-stage timings and live-demand census.

Disposable CI checkout only. No skipped Advance(2), no cached decisions,
no changed physics, allocations, ledgers, history or save serialization.
Do not interpret Stopwatch times as nonintrusive performance or A/B gain.
"""
from pathlib import Path

def patch(path, pairs):
    p = Path(path)
    s = p.read_text()
    for old, new in pairs:
        assert s.count(old) == 1, (path, old[:110], s.count(old))
        s = s.replace(old, new, 1)
    p.write_text(s)

patch("src/IndexCities.Simulation/Infrastructure.cs", [
    ("readonly List<RoadNode> _streets=[];readonly List<Point> _sources=[];readonly CapacityAllocation _allocation=new();",
     "readonly List<RoadNode> _streets=[];readonly List<Point> _sources=[];readonly CapacityAllocation _allocation=new();\n    public long[] Pr57442ElectricStage=new long[15];"),
    ("var wind=active.Where(b=>b.Kind==BuildingKind.WindPower&&HasExteriorAccess(b)).ToList();",
     "var pr57442T0=System.Diagnostics.Stopwatch.GetTimestamp();\n        var wind=active.Where(b=>b.Kind==BuildingKind.WindPower&&HasExteriorAccess(b)).ToList();"),
    ("State.ElectricityCapacity=wind.Sum(b=>b.OperationalCapacity);State.AddElectricityAvailableToday(State.ElectricityCapacity*dt/86400);",
     "State.ElectricityCapacity=wind.Sum(b=>b.OperationalCapacity);State.AddElectricityAvailableToday(State.ElectricityCapacity*dt/86400);\n        var pr57442T1=System.Diagnostics.Stopwatch.GetTimestamp();"),
    ("State.ElectricityDemand=active.Sum(b=>b.ElectricityDemand)+streetDemand;",
     "State.ElectricityDemand=active.Sum(b=>b.ElectricityDemand)+streetDemand;\n        var pr57442T2=System.Diagnostics.Stopwatch.GetTimestamp();"),
    ("_sources.Clear();foreach(var b in wind)_sources.Add(b.Position);allocation.Allocate(State.ElectricityCapacity,_sources);",
     """_sources.Clear();foreach(var b in wind)_sources.Add(b.Position);
        var pr57442T3=System.Diagnostics.Stopwatch.GetTimestamp();
        allocation.Allocate(State.ElectricityCapacity,_sources);
        var pr57442T4=System.Diagnostics.Stopwatch.GetTimestamp();"""),
    ("for(var j=0;j<streets.Count;j++){var n=streets[j];var demand=allocation.Demands[active.Count+j];n.ElectricityRatio=demand<=0?0:allocation.Result[active.Count+j]/demand;State.AddStreetElectricityConsumedToday(demand*n.ElectricityRatio*dt/86400);}",
     """for(var j=0;j<streets.Count;j++){var n=streets[j];var demand=allocation.Demands[active.Count+j];n.ElectricityRatio=demand<=0?0:allocation.Result[active.Count+j]/demand;State.AddStreetElectricityConsumedToday(demand*n.ElectricityRatio*dt/86400);}
        var pr57442T5=System.Diagnostics.Stopwatch.GetTimestamp();
        Pr57442ElectricStage[0]+=pr57442T1-pr57442T0;
        Pr57442ElectricStage[1]+=pr57442T2-pr57442T1;
        Pr57442ElectricStage[2]+=pr57442T3-pr57442T2;
        Pr57442ElectricStage[3]+=pr57442T4-pr57442T3;
        Pr57442ElectricStage[4]+=pr57442T5-pr57442T4;
        Pr57442ElectricStage[5]++;
        if(night)Pr57442ElectricStage[6]++;
        Pr57442ElectricStage[7]+=active.Count;
        Pr57442ElectricStage[8]+=streets.Count;
        Pr57442ElectricStage[9]+=_sources.Count;
        var pr57442PositiveBuildings=0;
        foreach(var b in active)if(b.ElectricityDemand>0)pr57442PositiveBuildings++;
        var pr57442PositiveStreets=0;
        for(var j=0;j<streets.Count;j++)if(allocation.Demands[active.Count+j]>0)pr57442PositiveStreets++;
        Pr57442ElectricStage[10]+=pr57442PositiveBuildings;
        Pr57442ElectricStage[11]+=pr57442PositiveStreets;
        Pr57442ElectricStage[12]+=active.Count(b=>b.ElectricityRatio>0);
        Pr57442ElectricStage[13]+=streets.Count(n=>n.ElectricityRatio>0);
        Pr57442ElectricStage[14]+=pr57442PositiveBuildings+pr57442PositiveStreets;""")
])
patch("scripts/pr57-actor-regime-337/Program.cs", [
    ("    var before = sim.Pr57298VehicleCostSnapshot();",
     "    var before = sim.Pr57298VehicleCostSnapshot();\n    var pr57442Before=sim.Pr57442ElectricStage.ToArray();"),
    ("    var after=sim.Pr57298VehicleCostSnapshot();",
     """    var after=sim.Pr57298VehicleCostSnapshot();
    string[] pr57442Names={"windGenerationTicks","consumerSetupTicks","sourceSetupTicks","allocationTicks","ratioConsumptionTicks","calls","nightCalls","activeBuildings","streets","sources","positiveBuildingDemands","positiveStreetDemands","positiveBuildingRatios","positiveStreetRatios","positiveTotalDemands"};
    for(var k=0;k<pr57442Names.Length;k++)
        Console.WriteLine($"PR57_442_ELECTRIC hour={hour} metric={pr57442Names[k]} value={sim.Pr57442ElectricStage[k]-pr57442Before[k]} frequency={Stopwatch.Frequency}");""")
])
print("PR57_442_INJECTED observational=True electricExclusiveTimers=True everyAdvance2=True")
