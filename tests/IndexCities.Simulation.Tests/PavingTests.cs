using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

public sealed class PavingTests
{
    static CitySimulation City()=>new(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
    static WorkProject Road(CitySimulation sim,Point start,Point end){var result=sim.PlaceRoad(start,end);Assert.True(result.Success,result.Message);return sim.State.Projects.Single(p=>p.Id==result.Id);}
    static string Diagnostic(CitySimulation sim)=>string.Join("; ",sim.State.Projects.Select(p=>$"P{p.Id} {p.Status} {p.Progress}: {p.Cause}, {p.AppliedRoadPoints.Count}/{p.RolledRoadPoints.Count}, verba {p.BudgetRemaining}"))+" | "+string.Join("; ",sim.State.Vehicles.Select(v=>$"V{v.Id} {v.Machine} {v.MachinePhase} {v.Status} {v.Position} route {v.RouteIndex}/{v.Route.Count}: {v.Cause}"));

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
        Assert.Equal(WorkStatus.Completed,second.Status);Assert.Equal(machines.Keys.OrderBy(id=>id),sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).Select(v=>v.Id).OrderBy(id=>id));
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
    public void Legacy_partial_streets_keep_their_nodes_money_and_consumed_materials()
    {
        var sim=City();var work=Road(sim,new(-192,0),new(-192,48));
        for(var i=0;i<2000&&work.RolledRoadPoints.Count<2;i++)sim.Advance(2);
        var legacy=System.Text.Json.Nodes.JsonNode.Parse(sim.SaveToJson())!.AsObject();legacy.Remove("ConstructionStateVersion");legacy.Remove("PavingFleetInitialized");
        var vehicles=legacy["Vehicles"]!.AsArray();foreach(var machine in vehicles.Where(v=>v!["Machine"]!.GetValue<string>()!="None").ToArray())vehicles.Remove(machine);
        foreach(var v in vehicles){v!.AsObject().Remove("Body");v.AsObject().Remove("ConstructionAccess");}
        foreach(var node in legacy["Roads"]!.AsArray()){node!.AsObject().Remove("ConstructionOnly");node.AsObject().Remove("WorkProjectId");}
        foreach(var p in legacy["Projects"]!.AsArray())foreach(var key in new[]{"PavingVersion","PaverId","RollerId","AppliedRoadPoints","RolledRoadPoints","MachineryQuoted","MachineryPaid"})p!.AsObject().Remove(key);
        var balance=sim.CityBalance;var ids=work.BuiltRoadNodes.ToArray();var paid=work.CostPaid;var consumed=work.Delivered.ToDictionary(k=>k.Key,k=>k.Value);
        sim=CitySimulation.FromJson(legacy.ToJsonString());var restored=sim.State.Projects.Single(p=>p.Id==work.Id);
        Assert.Equal(balance,sim.CityBalance);Assert.Equal(paid,restored.CostPaid);Assert.All(ids,id=>Assert.Contains(sim.State.Roads,r=>r.Id==id&&!r.ConstructionOnly));
        Assert.All(consumed,k=>Assert.Equal(k.Value,restored.Delivered[k.Key]));var clone=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(3600);clone.Advance(3600);
        Assert.Equal(sim.SaveToJson(),clone.SaveToJson());Assert.True(restored.Status==WorkStatus.Completed,Diagnostic(sim));Assert.All(ids,id=>Assert.Contains(sim.State.Roads,r=>r.Id==id));sim.CheckInvariants();
    }
}
