using IndexCities.Simulation;
using Xunit;
using System.Reflection;
namespace IndexCities.Simulation.Tests;
public sealed class LogisticsAndPriorityTests
{
    [Fact] public void Right_hand_priority_yields_to_the_physical_right_side_in_world_coordinates()
    {
        var initial=new CitySimulation(42);var centre=new RoadNode{Id=1000,Position=new(0,0)};initial.State.Roads.AddRange([centre,new(){Id=1001,Position=new(-4,0)},new(){Id=1002,Position=new(4,0)},new(){Id=1003,Position=new(0,-4)},new(){Id=1004,Position=new(0,4)}]);var sim=new CitySimulation(initial.State);
        var eastbound=new Vehicle{Id=1100,External=true,Status=VehicleStatus.Travelling,Position=new(-4,-1.65),Heading=Math.PI/2,Route=[centre.Id]};var southApproach=new Vehicle{Id=1101,External=true,Status=VehicleStatus.Travelling,Position=new(-1.65,4),Heading=Math.PI,Route=[centre.Id]};sim.State.Vehicles.AddRange([eastbound,southApproach]);var enter=typeof(CitySimulation).GetMethod("MayEnterJunction",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Assert.False((bool)enter.Invoke(sim,[eastbound,centre])!);southApproach.Position=new(1.65,-4);southApproach.Heading=0;Assert.True((bool)enter.Invoke(sim,[eastbound,centre])!);
    }
    [Fact] public void Cancelled_paid_cargo_can_physically_supply_a_depot_and_release_the_blocked_fleet()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);sim.Advance(7200);
        Assert.True(sim.PlaceBuilding(BuildingKind.House,new(-120,14)).Success);Assert.True(sim.PlaceBuilding(BuildingKind.House,new(-100,14)).Success);sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);
        var projects=sim.State.Projects.Where(p=>p.BuildingId is {} id&&sim.State.Buildings.First(b=>b.Id==id).Kind==BuildingKind.House).ToList();var cancelledDeliveries=sim.State.Shipments.Where(s=>projects.Any(p=>p.Id==s.ProjectId)&&!s.Complete).ToArray();Assert.NotEmpty(cancelledDeliveries);foreach(var project in projects)Assert.True(sim.CancelProject(project.Id).Success);sim.Advance(120);Assert.Equal(cancelledDeliveries.Length,sim.State.Vehicles.Count(v=>v.Status==VehicleStatus.Waiting&&v.Cargo.Count>0));
        var total=sim.MonetaryTotal;var placed=sim.PlaceBuilding(BuildingKind.WorksDepot,new(-160,-18),Math.PI);Assert.True(placed.Success);sim.Advance(14400);sim.CheckInvariants();var depot=sim.State.Buildings.First(b=>b.Id==placed.Id);var dump=System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,"../../../../../artifacts/verification/cancelled-fleet.json"));System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dump)!);System.IO.File.WriteAllText(dump,sim.SaveToJson());Assert.True(depot.Completed,string.Join(" | ",sim.State.Projects.Where(p=>p.Status!=WorkStatus.Completed).Select(p=>$"{p.Id}:{p.Cause}; paid={p.CostPaid}; remaining={p.BudgetRemaining}; delivered={string.Join(",",p.Delivered.Select(k=>$"{k.Key}={k.Value}"))}")));
        Assert.Contains(sim.State.Shipments,s=>s.OriginalProjectId!=null&&s.ProjectId==depot.ProjectId&&s.Complete);Assert.All(cancelledDeliveries,s=>Assert.True(s.Complete));Assert.Equal(total,sim.MonetaryTotal);Assert.Contains(sim.State.Vehicles,v=>v.Status==VehicleStatus.Outside);
    }
}
