using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class ConstructionAndRoutingTests
{
    [Fact] public void Moving_unpaid_work_releases_old_commitment_and_keeps_total_money()
    {
        var sim=new CitySimulation(9);var first=sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40));Assert.True(first.Success);var old=sim.State.Projects.Single();var balance=sim.CityBalance;
        Assert.True(sim.MoveBuildingProject(old.Id,new(-104,40),0).Success);Assert.Equal(WorkStatus.Cancelled,old.Status);Assert.Equal(0,old.BudgetRemaining);Assert.Equal(balance,sim.CityBalance);Assert.Single(sim.State.Buildings,b=>!b.Removed);Assert.False(sim.State.Buildings.Last().Completed);sim.CheckInvariants();
    }
    [Fact] public void Invalid_or_unaffordable_move_does_not_cancel_the_old_project()
    {
        var probe=new CitySimulation(9);var quote=probe.QuoteBuilding(BuildingKind.CityHall,new(-100,40));var sim=new CitySimulation(9,new(){CityInitialMoney=quote.Total});Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);var project=sim.State.Projects.Single();var before=sim.SaveToJson();
        Assert.False(sim.MoveBuildingProject(project.Id,new(220,-200),0).Success);Assert.Equal(before,sim.SaveToJson());
        Assert.False(sim.MoveBuildingProject(project.Id,sim.State.LakeCentre,0).Success);Assert.Equal(before,sim.SaveToJson());
    }
    [Fact] public void Paid_cargo_survives_cancel_without_depot_or_fictitious_refund()
    {
        var sim=new CitySimulation(42);sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16));sim.PlaceRoad(new(-192,0),new(100,0));sim.Advance(7200);var result=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(result.Success);var project=sim.State.Projects.First(p=>p.BuildingId==result.Id);
        for(var i=0;i<2000&&!sim.State.Shipments.Any(s=>s.ProjectId==project.Id);i++)sim.Advance(2);
        var deliveries=sim.State.Shipments.Where(s=>s.ProjectId==project.Id&&!s.Complete).ToList();Assert.NotEmpty(deliveries);var quantity=deliveries.Sum(s=>s.Cargo.Values.Sum());var paid=project.CostPaid;var balance=sim.CityBalance;Assert.True(paid>0);
        Assert.True(sim.CancelProject(project.Id).Success);Assert.Equal(balance,sim.CityBalance);sim.Advance(3600);
        Assert.Equal(quantity,deliveries.Sum(s=>sim.State.Vehicles.First(v=>v.Id==s.VehicleId).Cargo.Values.Sum()),5);Assert.All(deliveries,s=>Assert.False(s.Complete));Assert.Equal(paid,project.CostPaid);Assert.Equal(0,project.BudgetRemaining);sim.CheckInvariants();
        var loaded=CitySimulation.FromJson(sim.SaveToJson());Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());
    }
    [Fact] public void Pedestrian_path_turns_without_backtracking_into_its_own_queue()
    {
        var sim=new CitySimulation(42);sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16));Assert.True(sim.PlaceRoad(new(-192,0),new(-184,44)).Success);var placed=sim.PlaceBuilding(BuildingKind.House,new(-184,58));Assert.True(placed.Success);sim.Advance(7200);var house=sim.State.Buildings.First(b=>b.Id==placed.Id);Assert.True(house.Completed);
        var path=sim.PedestrianRoute(sim.State.Entry,house);Assert.NotEmpty(path);foreach(var pair in path.Zip(path.Skip(1)).Where(p=>p.First.X>-186&&p.First.Z<43))Assert.True(pair.Second.Z>=pair.First.Z-.01,$"Backtracking: {pair.First} -> {pair.Second}");
        Assert.Contains(sim.State.Roads,n=>n.Crosswalk&&n.Position.Distance(new(-184,0))<.1);
    }
    [Fact] public void Crew_on_inner_corner_sidewalk_can_return_home_without_crossing_an_unmarked_lane()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(240,0)).Success);sim.Advance(7200);Assert.True(sim.PlaceRoad(new(240,0),new(240,44)).Success);var house=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(house.Success);sim.Advance(7200);
        var home=sim.State.Buildings.Single(b=>b.Id==house.Id);var source=new Point(236,4.55);var nearest=sim.State.Roads.MinBy(r=>r.Position.Distance(source))!;Assert.Equal(new Point(240,4),nearest.Position);Assert.False(nearest.Crosswalk);var route=sim.PedestrianRoute(source,home);Assert.NotEmpty(route);Assert.Equal(source,route[0]);Assert.Equal(home.Door,route[^1]);Assert.Contains(route,p=>p.X<240&&p.X>234&&Math.Abs(p.Z-4)<.1);
        var straight=sim.PedestrianRoute(new(-100,-4.55),home);Assert.NotEmpty(straight);sim.CheckInvariants();
    }
    [Fact] public void External_crew_leaves_the_real_loading_bay_at_a_short_road_end_and_releases_the_fleet()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);var road=sim.PlaceRoad(new(-192,0),new(-192,12));Assert.True(road.Success);sim.Advance(7200);var project=sim.State.Projects.Single(p=>p.Id==road.Id);Assert.Equal(WorkStatus.Completed,project.Status);Assert.All(sim.State.Vehicles.Where(v=>v.External),v=>Assert.True(v.Status==VehicleStatus.Outside,$"{v.Id}: {v.Position} {v.Status} {v.Cause}"));Assert.True(project.CostPaid>0);sim.CheckInvariants();
    }
    [Fact] public void Replanning_inside_an_existing_corner_crosswalk_does_not_strand_the_pedestrian()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(0,0)).Success);sim.Advance(7200);Assert.True(sim.PlaceRoad(new(-184,0),new(-120,44),true).Success);var house=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(house.Success);sim.Advance(7200);var origin=new Point(-179.8,43.8);var nearest=sim.State.Roads.MinBy(r=>r.Position.Distance(origin))!;Assert.False(nearest.Crosswalk);Assert.Contains(sim.State.Roads,r=>r.Crosswalk&&r.Position.Distance(new(-184,44))<.1);var path=sim.PedestrianRoute(origin,sim.State.Buildings.Single(b=>b.Id==house.Id));Assert.NotEmpty(path);Assert.Equal(origin,path[0]);sim.CheckInvariants();
    }
}
