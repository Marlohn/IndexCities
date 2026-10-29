#!/usr/bin/env python3
"""#356 checkout-only adversarial test: #351 witness is not a complete s3 readset.
No production files or simulation rules are changed; no throughput claim.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
shared=(root/"src/IndexCities.Simulation/Mobility.SharedSpace.cs").read_text()
approach=(root/"src/IndexCities.Simulation/Construction.ApproachYield.cs").read_text()
witness=(root/"scripts/pr57-351-inject.py").read_text()
assert "double? BodyAcrossLane(Vehicle v,Vehicle o)" in shared
assert "||OnSitePath(o)" in shared
assert "v.SitePath.Count>v.SitePathIndex" in approach
key=witness.split("static string Pr57351Key(Vehicle o)=>",1)[1].split("void Pr57351Observe",1)[0]
assert "o.SitePathBeforeRoad" in key and "o.SitePathIndex" in key
assert "o.SitePath.Count" not in key and "string.Join(\",\",o.SitePath)" not in key
target=root/"tests/IndexCities.Simulation.Tests/Pr57ReadsetCounterexample356Tests.cs"
assert not target.exists()
target.write_text(r'''using System;
using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// #356: A narrowly scoped counterexample to treating #351/#355 as a reusable
// negative lane-scan certificate. Only a private canonical predicate is invoked.
// This is NOT an end-to-end scan, optimization, or proof about the REAL save.
public sealed class Pr57ReadsetCounterexample356Tests
{
    // Exact selected-field string from the #351 checkout-only observer.
    static string Witness(Vehicle o) =>
        $"@{o.Id}|@{o.Position.X:R},@{o.Position.Z:R}|@{o.Heading:R}|@{o.Status}|" +
        $"@{o.RouteIndex}|@{string.Join(",",o.Route)}|@{string.Join(",",o.Detour)}|" +
        $"@{o.TowPhase}|@{o.TowTargetId}|@{o.TowTruckId}|@{o.WaitingSince:R}|" +
        $"@{o.IntersectionClaim}|@{string.Join(",",o.ManoeuvreSpaces)}|" +
        $"@{o.DockPosition}|@{o.ManoeuvreDock}|@{o.SitePathBeforeRoad}|" +
        $"@{o.SitePathIndex}|@{o.PassengerCar}|@{o.Machine}";

    [Fact]
    public void Site_path_content_changes_canonical_dock_blocker_while_351_witness_is_identical()
    {
        var sim=new CitySimulation(42,new SimulationTuning {
            ExternalFamilyCandidates=0,ExternalCompanyCandidates=0,ExternalTruckCount=0
        });
        var reader=new Vehicle { Id=101,Status=VehicleStatus.Travelling,
            PassengerCar=true,Position=new Point(0,0),Heading=0 };
        var other=new Vehicle { Id=102,Status=VehicleStatus.Travelling,
            PassengerCar=false,Machine=PavingMachine.None,
            Position=new Point(0,5),Heading=Math.PI/2,
            Route=[1000],RouteIndex=1,SitePathIndex=0,SitePathBeforeRoad=false };
        var method=typeof(CitySimulation).GetMethod("BodyAcrossLane",
            BindingFlags.NonPublic|BindingFlags.Instance);
        Assert.NotNull(method);
        double? Canonical() => (double?)method!.Invoke(sim,new object[]{reader,other});

        var before=Witness(other);
        Assert.Null(Canonical()); // no site path: not a dock blocker
        other.SitePath.Add(new Point(0,5)); // field absent from #351 projection
        Assert.Equal(before,Witness(other));
        Assert.Equal(5d,Canonical()!.Value,10); // same poses, now a dock blocker
    }
}
''')
print("PR57_356_INJECTED adversarial selected-witness counterexample; no production changes")
