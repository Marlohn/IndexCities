using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only exactness adversarials for the opt-in building-work broad phase.
// These call the candidate directly, even in the flag-off test process, and
// compare its selected road with the original full State.Roads scan.
public sealed class Pr57BuildingAccessAdversarial
{
    static readonly BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly MethodInfo Begin=typeof(CitySimulation).GetMethod("Pr57BeginBuildingAccessAdvance",PrivateInstance)!;
    static readonly MethodInfo End=typeof(CitySimulation).GetMethod("Pr57EndBuildingAccessAdvance",PrivateInstance)!;
    static readonly MethodInfo Indexed=typeof(CitySimulation).GetMethod("Pr57IndexedBuildingAccess",PrivateInstance)!;
    static readonly FieldInfo Connected=typeof(CitySimulation).GetField("_workExteriorConnected",PrivateInstance)!;

    static RoadNode? FullScan(CitySimulation sim,Building b)
    {
        var connected=(HashSet<int>)Connected.GetValue(sim)!;
        var radius=sim.State.Tuning.RoadWidth/2+3;
        return sim.State.Roads
            .Where(r=>!r.Highway&&r.MaintenanceRatio>.05&&
                      connected.Contains(r.Id)&&r.Position.Distance(b.Door)<=radius)
            .MinBy(r=>r.Position.Distance(b.Door));
    }

    static void AssertExact(CitySimulation sim,IEnumerable<Building> buildings,string phase)
    {
        Begin.Invoke(sim,null); // Public Advance always invalidates membership at entry.
        try
        {
            foreach(var b in buildings)
            {
                var expected=FullScan(sim,b)?.Id;
                var actual=((RoadNode?)Indexed.Invoke(sim,[b]))?.Id;
                Assert.True(expected==actual,
                    $"PR57 building access {phase} building={b.Id} expected={expected} actual={actual}");
            }
        }
        finally { End.Invoke(sim,null); }
    }

    [Fact]
    public void Full_scan_matches_index_after_live_maintenance_and_external_road_reordering()
    {
        var sim=CityCycleTests.City(); // Paid, physically built road network and genuine buildings.
        var buildings=sim.State.Buildings.Where(b=>!b.Removed).ToArray();
        Assert.True(buildings.Length>=30);
        AssertExact(sim,buildings,"initial");
        var road=sim.State.Roads.First(r=>!r.Highway&&r.MaintenanceRatio>.05);
        var previous=road.MaintenanceRatio;
        road.MaintenanceRatio=.04; // Live predicate changes between public calls.
        AssertExact(sim,buildings,"maintenance-unavailable");
        road.MaintenanceRatio=previous;
        AssertExact(sim,buildings,"maintenance-restored");
        sim.State.Roads.Reverse(); // Externally mutable State: also changes MinBy tie order.
        AssertExact(sim,buildings,"externally-reordered");
        sim.Advance(2);
        AssertExact(sim,buildings,"after-host-quantum");
        sim.CheckInvariants();
    }

    [Fact]
    public void Full_scan_matches_index_after_paid_road_demolition_and_reconstruction()
    {
        var sim=new CitySimulation(42,new(){ExternalTruckCount=1});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-80,0)).Success);
        sim.Advance(7200);
        var buildings=sim.State.Buildings.Where(b=>!b.Removed).ToArray();
        Assert.NotEmpty(buildings);
        AssertExact(sim,buildings,"before-demolition");
        var road=sim.State.Roads.Single(r=>r.Position==new Point(-80,0));
        var point=road.Position;
        Assert.True(sim.DemolishRoad(road.Id).Success);
        AssertExact(sim,buildings,"after-demolition");
        var replacement=sim.PlaceRoad(point,point);
        Assert.True(replacement.Success);
        sim.Advance(120);
        AssertExact(sim,buildings,"after-rebuild-quanta");
        sim.CheckInvariants();
    }
}
