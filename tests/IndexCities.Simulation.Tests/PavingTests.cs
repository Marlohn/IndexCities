using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

public sealed class PavingTests
{
    static CitySimulation City()=>new(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
    static WorkProject Road(CitySimulation sim,Point start,Point end){var result=sim.PlaceRoad(start,end);Assert.True(result.Success,result.Message);return sim.State.Projects.Single(p=>p.Id==result.Id);}
    internal static string Diagnostic(CitySimulation sim)=>string.Join("; ",sim.State.Projects.Select(p=>$"P{p.Id} {p.Status} {p.Progress}: {p.Cause}, {p.AppliedRoadPoints.Count}/{p.RolledRoadPoints.Count}, verba {p.BudgetRemaining}"))+" | "+string.Join("; ",sim.State.Vehicles.Select(v=>$"V{v.Id} {v.Machine} {v.MachinePhase} {v.Status} {v.Position} route {v.RouteIndex}/{v.Route.Count}: {v.Cause}"));

    [Fact]
    public void Bootstrap_paves_and_rolls_with_paid_external_equipment_and_no_depot()
    {
        var sim=City();var p=Road(sim,new(-192,0),new(-192,24));var budget=p.BudgetRemaining;
        sim.Advance(3600);
        Assert.True(p.Status==WorkStatus.Completed,Diagnostic(sim));
        Assert.Equal(p.RoadPoints.Count,p.AppliedRoadPoints.Count);Assert.Equal(p.RoadPoints.Count,p.RolledRoadPoints.Count);
        Assert.Equal(2,sim.State.Vehicles.Count(v=>v.Machine!=PavingMachine.None));
        Assert.All(sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None),v=>Assert.Equal(VehicleStatus.Outside,v.Status));
        Assert.True(p.MachineryPaid>0&&p.CostPaid<=budget);Assert.Equal(p.CostPaid,sim.State.Ledger.Where(l=>l.EntityId==p.Id).Sum(l=>l.Amount));
        Assert.All(p.Required,kv=>Assert.Equal(kv.Value,p.Delivered[kv.Key],5));
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }

    [Fact]
    public void Finished_cells_allow_only_construction_routes_until_whole_street_opens()
    {
        var sim=City();var p=Road(sim,new(-192,0),new(-192,48));
        for(var i=0;i<1800&&p.RolledRoadPoints.Count<2;i++)sim.Advance(2);
        Assert.True(p.RolledRoadPoints.Count>=2&&p.Status!=WorkStatus.Completed,Diagnostic(sim));
        var node=sim.State.Roads.Single(r=>r.Position==p.RolledRoadPoints.Last());var entry=sim.State.Roads.First();
        Assert.True(node.ConstructionOnly);Assert.Empty(sim.FindRoadRoute(entry.Id,node.Id));Assert.NotEmpty(sim.FindRoadRoute(entry.Id,node.Id,true));
        var clone=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(3600);clone.Advance(3600);Assert.Equal(sim.SaveToJson(),clone.SaveToJson());
        Assert.Equal(WorkStatus.Completed,p.Status);Assert.NotEmpty(sim.FindRoadRoute(entry.Id,node.Id));
    }

    [Fact]
    public void Cancelling_a_partial_street_opens_only_the_compacted_cells()
    {
        var sim=City();var p=Road(sim,new(-192,0),new(-192,48));
        for(var i=0;i<1800&&p.RolledRoadPoints.Count<2;i++)sim.Advance(2);
        Assert.True(p.RolledRoadPoints.Count>=2&&p.Status!=WorkStatus.Completed,Diagnostic(sim));
        var rolled=p.RolledRoadPoints.ToList();var pending=p.RoadPoints.Except(rolled).ToList();Assert.NotEmpty(pending);
        var node=sim.State.Roads.Single(r=>r.Position==rolled.Last());var entry=sim.State.Roads.First();var roads=sim.State.Roads.Count;
        Assert.True(sim.CancelProject(p.Id).Success);
        Assert.False(node.ConstructionOnly);Assert.NotEmpty(sim.FindRoadRoute(entry.Id,node.Id));Assert.Equal(roads,sim.State.Roads.Count);
        Assert.DoesNotContain(sim.State.Roads,r=>pending.Contains(r.Position)&&r.WorkProjectId==p.Id);
        var clone=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(3600);clone.Advance(3600);Assert.Equal(sim.SaveToJson(),clone.SaveToJson());
        Assert.All(sim.State.Roads.Where(r=>r.WorkProjectId==p.Id),r=>Assert.False(r.ConstructionOnly));sim.CheckInvariants();
    }

    [Fact]
    public void Equipment_shortage_and_fuel_shortage_cannot_advance_paving()
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0,ExternalPavingTeams=0});var p=Road(sim,new(-192,0),new(-192,24));
        sim.Advance(600);Assert.Equal(0,p.Progress);Assert.Equal(WorkCause.AwaitingPavingMachines,p.Cause);
        sim=City();p=Road(sim,new(-192,0),new(-192,24));
        for(var i=0;i<1000&&p.AppliedRoadPoints.Count<1;i++)sim.Advance(2);
        var machine=sim.State.Vehicles.Single(v=>v.Id==p.PaverId);machine.Fuel=0;var progress=p.Progress;var cost=p.CostPaid;
        sim.Advance(60);Assert.Equal(progress,p.Progress);Assert.Equal(cost,p.CostPaid);Assert.Equal(WorkCause.PavingOutOfFuel,p.Cause);
    }

    [Fact]
    public void Truck_bodies_follow_material_and_survive_unloading_empty_return_and_next_trip()
    {
        var sim=City();Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Road(sim,new(-192,0),new(-128,0));
        var body=sim.State.Vehicles.ToDictionary(v=>v.Id,v=>v.Body);var seen=new HashSet<ResourceKind>();var emptyReturn=false;
        for(var step=0;step<3600;step++){
            sim.Advance(2);
            foreach(var v in sim.State.Vehicles.Where(v=>v.Machine==PavingMachine.None)){
                Assert.Equal(body[v.Id],v.Body);
                foreach(var k in v.Cargo.Where(k=>k.Value>0).Select(k=>k.Key)){Assert.Equal(CitySimulation.BodyFor(k),v.Body);seen.Add(k);}
                if(v.Status==VehicleStatus.Travelling&&v.ShipmentId==null&&v.Cargo.Count==0&&v.Body!=TruckBody.Cargo)emptyReturn=true;
            }
        }
        Assert.True(emptyReturn);Assert.Contains(ResourceKind.Concrete,seen);Assert.Contains(ResourceKind.Steel,seen);Assert.Contains(ResourceKind.Wood,seen);Assert.Contains(ResourceKind.Asphalt,seen);
        Assert.All(sim.State.Projects,p=>Assert.True(p.Status==WorkStatus.Completed,Diagnostic(sim)));
    }

    [Fact]
    public void Concurrent_roads_share_a_finite_pair_and_cancel_returns_the_same_paid_equipment()
    {
        var sim=City();var first=Road(sim,new(-192,0),new(-192,48));var second=Road(sim,new(-208,0),new(-208,-40));
        for(var i=0;i<2000&&first.AppliedRoadPoints.Count<3;i++)sim.Advance(2);
        Assert.True(first.AppliedRoadPoints.Count>=3,Diagnostic(sim));Assert.Equal(0,second.Progress);
        var machines=sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).ToDictionary(v=>v.Id,v=>v.Position);
        Assert.Equal(2,machines.Count);Assert.All(sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None),v=>Assert.Equal(first.Id,v.MachineProjectId));
        var consumed=first.Delivered.ToDictionary(k=>k.Key,k=>k.Value);var balance=sim.CityBalance;var budget=first.BudgetRemaining;var due=decimal.Round(first.CrewCostDue,2,MidpointRounding.AwayFromZero);
        Assert.True(sim.CancelProject(first.Id).Success);Assert.Equal(balance-due,sim.CityBalance);Assert.Equal(0,first.BudgetRemaining);
        Assert.All(sim.State.Vehicles.Where(v=>machines.ContainsKey(v.Id)),v=>Assert.Equal(machines[v.Id],v.Position));
        var paid=first.CostPaid;var clone=CitySimulation.FromJson(sim.SaveToJson());
        sim.Advance(3600);clone.Advance(3600);Assert.Equal(sim.SaveToJson(),clone.SaveToJson());
        Assert.True(budget>due);Assert.Equal(paid,first.CostPaid);Assert.All(consumed,k=>Assert.Equal(k.Value,first.Delivered[k.Key]));
        Assert.True(second.Status==WorkStatus.Completed,Diagnostic(sim));Assert.Equal(machines.Keys.OrderBy(id=>id),sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).Select(v=>v.Id).OrderBy(id=>id));
        Assert.All(sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None),v=>Assert.Equal(VehicleStatus.Outside,v.Status));sim.CheckInvariants();
    }

    [Fact]
    public void Access_crew_and_money_are_real_requirements_on_the_paving_site()
    {
        foreach(var blocker in new[]{"access","crew","money"}){
            var sim=City();var approach=Road(sim,new(-192,0),new(-144,0));sim.Advance(3600);Assert.Equal(WorkStatus.Completed,approach.Status);
            var work=Road(sim,new(-144,0),new(-144,40));for(var i=0;i<2000&&work.AppliedRoadPoints.Count<2;i++)sim.Advance(2);
            Assert.True(work.AppliedRoadPoints.Count>=2,Diagnostic(sim));
            var paver=sim.State.Vehicles.Single(v=>v.Id==work.PaverId);
            if(blocker=="access")Assert.True(sim.DemolishRoad(sim.State.Roads.Single(r=>r.Position==new Point(-172,0)).Id).Success);
            if(blocker=="crew")paver.CrewAboard=false;
            if(blocker=="money")work.BudgetRemaining=0;
            var progress=work.Progress;var paid=work.CostPaid;var at=paver.Position;sim.Advance(20);
            Assert.Equal(progress,work.Progress);Assert.Equal(paid,work.CostPaid);Assert.Equal(at,paver.Position);
            Assert.Equal(blocker switch{"access"=>WorkCause.NoMaterialsAccess,"crew"=>WorkCause.AwaitingCrewOnSite,_=>WorkCause.NoBudgetForPaving},work.Cause);
        }
    }

    [Fact]
    public void Saves_from_before_material_trucks_are_refused_instead_of_converted()
    {
        var legacy=System.Text.Json.Nodes.JsonNode.Parse(City().SaveToJson())!.AsObject();legacy.Remove("ConstructionStateVersion");
        Assert.Throws<InvalidDataException>(()=>CitySimulation.FromJson(legacy.ToJsonString()));
    }

    [Fact]
    public void Asphalt_is_unloaded_into_the_present_paver_and_consumed_once_after_real_docking()
    {
        var sim=City();var p=Road(sim,new(-192,0),new(-188,0));sim.Advance(2);
        Assert.NotNull(p.PaverId);Assert.DoesNotContain(sim.State.Shipments,s=>s.ProjectId==p.Id&&s.Cargo.ContainsKey(ResourceKind.Asphalt));
        Shipment? shipment=null;Vehicle? truck=null;
        for(var i=0;i<2000;i++){sim.Advance(2);shipment=sim.State.Shipments.FirstOrDefault(s=>s.ProjectId==p.Id&&s.Cargo.ContainsKey(ResourceKind.Asphalt));truck=sim.State.Vehicles.FirstOrDefault(v=>v.ShipmentId==shipment?.Id);if(truck is {Status:VehicleStatus.Unloading,LoadTimer:<=9})break;}
        Assert.NotNull(shipment);Assert.NotNull(truck);Assert.Equal(VehicleStatus.Unloading,truck.Status);
        var paver=sim.State.Vehicles.Single(v=>v.Id==shipment.ReceivingMachineId);Assert.True(paver.FeedingReady);Assert.Equal(p.Id,paver.MachineProjectId);
        Assert.InRange(truck.Position.Distance(paver.Position),6.59,6.61);Assert.InRange(Math.Cos(truck.Heading-paver.Heading),.999,1.001);
        var cargo=truck.Cargo[ResourceKind.Asphalt];Assert.Equal(0,p.Delivered.GetValueOrDefault(ResourceKind.Asphalt));paver.CrewAboard=false;sim.Advance(20);
        Assert.False(shipment.Complete);Assert.Equal(cargo,truck.Cargo[ResourceKind.Asphalt]);Assert.Equal(0,p.Progress);
        paver.CrewAboard=true;var clone=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(3600);clone.Advance(3600);Assert.Equal(sim.SaveToJson(),clone.SaveToJson());
        Assert.True(p.Status==WorkStatus.Completed,Diagnostic(sim));Assert.Equal(cargo,p.Delivered[ResourceKind.Asphalt]);Assert.Empty(truck.Cargo);sim.CheckInvariants();
    }

    [Fact]
    public void Specialist_bodies_do_not_add_trucks_and_loading_does_not_reserve_more_money()
    {
        var sim=City();var ids=sim.State.Vehicles.Select(v=>v.Id).ToArray();Assert.Equal(6,ids.Length);
        Assert.Equal(3,sim.State.Vehicles.Count(v=>v.Body==TruckBody.Dump));Assert.All(new[]{TruckBody.Mixer,TruckBody.Flatbed,TruckBody.Cargo},body=>Assert.Single(sim.State.Vehicles,v=>v.Body==body));
        var p=Road(sim,new(-192,0),new(-192,24));var balance=sim.CityBalance;var commitment=sim.Committed;var restored=CitySimulation.FromJson(sim.SaveToJson());
        Assert.Equal(ids,restored.State.Vehicles.Select(v=>v.Id));Assert.Equal(balance,restored.CityBalance);Assert.Equal(commitment,restored.Committed);
        restored.Advance(3600);Assert.Equal(ids,restored.State.Vehicles.Where(v=>v.Machine==PavingMachine.None).Select(v=>v.Id));restored.CheckInvariants();
    }

    [Fact]
    public void Insufficient_round_trip_range_never_hires_or_dispatches_the_machines()
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0,FuelTank=1});var p=Road(sim,new(-192,0),new(-192,24));sim.Advance(2);
        Assert.Equal(WorkCause.PavingInsufficientRange,p.Cause);Assert.Null(p.PaverId);Assert.Null(p.RollerId);Assert.Equal(0,p.MachineryPaid);
        Assert.All(sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None),v=>Assert.Equal(VehicleStatus.Outside,v.Status));sim.CheckInvariants();
    }

    [Theory]
    [InlineData(2.0)]
    [InlineData(1.0)]
    public void Blocked_leaving_roller_keeps_its_pose_so_a_passing_truck_and_the_corner_street_finish(double step)
    {
        // Inverted-elbow street beside a house: the leaving roller once turned in
        // place into a passing truck and both waited on each other forever.
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        var road=sim.PlaceRoad(new(-192,0),new(-128,132),true);Assert.True(road.Success);
        var house=sim.PlaceBuilding(BuildingKind.House,new(-190,146));Assert.True(house.Success);
        var p=sim.State.Projects.Single(x=>x.Id==road.Id);
        for(var t=0.0;t<7200;t+=step){
            var before=sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&v.MachinePhase==MachinePhase.LeavingSite).ToDictionary(v=>v.Id,v=>(v.Position,v.Heading));
            sim.Advance(step);
            foreach(var v in sim.State.Vehicles.Where(v=>before.ContainsKey(v.Id)&&v.MachinePhase==MachinePhase.LeavingSite&&v.Position==before[v.Id].Position))
                Assert.True(v.Heading==before[v.Id].Heading,$"t={sim.State.ElapsedSeconds}: V{v.Id} turned in place while blocked | {Diagnostic(sim)}");
        }
        Assert.True(p.Status==WorkStatus.Completed,Diagnostic(sim));
        Assert.True(sim.State.Buildings.Single(b=>b.Id==house.Id).Completed,Diagnostic(sim));
        Assert.All(sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None),v=>Assert.Equal(VehicleStatus.Outside,v.Status));
        sim.CheckInvariants();
    }

    [Fact]
    public void Two_teams_never_reassign_a_returned_paver_while_its_roller_is_still_working()
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0,ExternalPavingTeams=2});var first=Road(sim,new(-192,0),new(-192,96));
        for(var i=0;i<3000;i++){sim.Advance(2);if(first.PaverId is {} id&&sim.State.Vehicles.Single(v=>v.Id==id).Status==VehicleStatus.Outside&&first.Progress<1)break;}
        var paver=sim.State.Vehicles.Single(v=>v.Id==first.PaverId);Assert.Equal(VehicleStatus.Outside,paver.Status);Assert.Equal(first.Id,paver.MachineProjectId);Assert.True(first.Progress<1,Diagnostic(sim));
        var second=Road(sim,new(-212,0),new(-212,-48));sim.Advance(2);Assert.NotNull(second.PaverId);Assert.NotEqual(first.PaverId,second.PaverId);Assert.NotEqual(first.RollerId,second.RollerId);
        Assert.Equal(first.Id,paver.MachineProjectId);var loaded=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(3600);loaded.Advance(3600);
        Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());Assert.True(first.Status==WorkStatus.Completed&&second.Status==WorkStatus.Completed,Diagnostic(sim));Assert.Equal(4,sim.State.Vehicles.Count(v=>v.Machine!=PavingMachine.None));sim.CheckInvariants();
    }
}
