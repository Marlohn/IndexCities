using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class ConstructionDispatchTests
{
    [Fact]
    public void Confirmed_work_dispatches_on_next_step_without_waiting_for_market_review()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);
        sim.State.EconomyNextAt=86400;
        var balance=sim.CityBalance;
        var placed=sim.PlaceRoad(new(-192,0),new(-192,12));
        Assert.True(placed.Success);
        Assert.Empty(sim.State.Shipments);
        Assert.Equal(balance,sim.CityBalance);
        sim.Advance(2);
        var project=sim.State.Projects.Single(p=>p.Id==placed.Id);
        Assert.Contains(sim.State.Shipments,s=>s.ProjectId==project.Id&&!s.Complete);
        Assert.True(project.CostPaid>0);
        Assert.Equal(0,project.Progress);
        Assert.Empty(project.Delivered);
        Assert.Contains(sim.State.Vehicles,v=>v.Cargo.Values.Sum()>0&&v.Status==VehicleStatus.Travelling);
        Assert.Equal(86400,sim.State.EconomyNextAt);
        sim.CheckInvariants();
    }

    [Fact]
    public void Returned_truck_resumes_remaining_deliveries_before_next_market_review()
    {
        var sim=new CitySimulation(42,new(){ExternalTruckCount=1});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);
        sim.State.EconomyNextAt=86400;
        var placed=sim.PlaceRoad(new(-192,0),new(-192,12));
        Assert.True(placed.Success);
        sim.Advance(600);
        var project=sim.State.Projects.Single(p=>p.Id==placed.Id);
        Assert.Equal(WorkStatus.Completed,project.Status);
        Assert.All(project.Required,kv=>Assert.Equal(kv.Value,project.Delivered[kv.Key],4));
        Assert.True(sim.State.Shipments.Count(s=>s.ProjectId==project.Id)>=2);
        Assert.True(project.CostPaid>0);
        Assert.True(sim.State.EconomyNextAt<86400);
        sim.CheckInvariants();
    }

    [Fact]
    public void Completed_house_can_be_acquired_before_periodic_review_but_arrival_stays_physical()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(0,0)).Success);
        sim.Advance(7200);
        Assert.All(sim.State.Vehicles,v=>Assert.Equal(VehicleStatus.Outside,v.Status));
        sim.State.EconomyNextAt=sim.State.ElapsedSeconds+86400;
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-100,14));
        Assert.True(placed.Success);
        var house=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        var before=sim.MonetaryTotal;
        for(var i=0;i<1000&&!house.Completed;i++)sim.Advance(2);
        Assert.True(house.Completed);
        Assert.NotNull(house.OwnerAccount);
        Assert.NotEmpty(sim.State.Citizens);
        Assert.All(sim.State.Citizens,c=>Assert.True(c.Moving));
        Assert.All(sim.State.Citizens,c=>Assert.True(c.Position.Distance(house.Door)>5));
        Assert.Contains(sim.State.Ledger,l=>l.Category=="Aquisição residencial"&&l.EntityId==house.Id);
        Assert.Equal(before,sim.MonetaryTotal);
        sim.CheckInvariants();
        var restored=CitySimulation.FromJson(sim.SaveToJson());
        sim.Advance(120);restored.Advance(120);
        Assert.Equal(sim.SaveToJson(),restored.SaveToJson());
    }

    [Fact]
    public void Saved_early_crew_returns_physically_and_releases_the_only_truck_for_missing_materials()
    {
        var sim=new CitySimulation(42,new(){ExternalTruckCount=1});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);
        var placed=sim.PlaceRoad(new(-192,0),new(-192,12));
        Assert.True(placed.Success);sim.Advance(2);
        var project=sim.State.Projects.Single(p=>p.Id==placed.Id);
        var truck=sim.State.Vehicles.Single();
        // A previous version sent the crew aboard the first of multiple loads.
        truck.CrewAboard=true;truck.CrewCount=3;project.CrewVehicleId=truck.Id;
        sim=CitySimulation.FromJson(sim.SaveToJson());
        sim.Advance(600);
        project=sim.State.Projects.Single(p=>p.Id==placed.Id);
        Assert.Equal(WorkStatus.Completed,project.Status);
        Assert.All(project.Required,kv=>Assert.Equal(kv.Value,project.Delivered[kv.Key],4));
        Assert.Contains(sim.State.Shipments,s=>s.ProjectId==project.Id&&s.Cargo.ContainsKey(ResourceKind.Asphalt));
        sim.CheckInvariants();
    }
}
