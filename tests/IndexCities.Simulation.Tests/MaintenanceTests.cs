using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class MaintenanceTests
{
    static CitySimulation Founded(){var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(-80,0)).Success);sim.Advance(7200);Assert.True(sim.State.Buildings.Single().Completed);return sim;}
    [Fact] public void Maintenance_requires_delivered_materials_and_present_paid_external_crew()
    {
        var sim=Founded();var hall=sim.State.Buildings.Single();hall.MaintenanceDueAt=sim.State.ElapsedSeconds-1;hall.MaintenanceRatio=.5;var roads=sim.State.Roads.Where(r=>!r.Highway).Take(3).ToList();foreach(var road in roads){road.MaintenanceDueAt=hall.MaintenanceDueAt;road.MaintenanceRatio=.7;}sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);
        var maintenance=sim.State.Projects.Where(p=>p.IsMaintenance).ToList();Assert.NotEmpty(maintenance);Assert.All(maintenance,p=>Assert.Equal(0,p.Progress));Assert.True(hall.MaintenanceRatio<=.5);
        for(var i=0;i<8&&maintenance.Any(p=>p.Status!=WorkStatus.Completed);i++){sim.Advance(1800);sim.CheckInvariants();}
        Assert.All(maintenance,p=>Assert.Equal(WorkStatus.Completed,p.Status));Assert.Equal(1,hall.MaintenanceRatio);Assert.All(roads,r=>Assert.Equal(1,r.MaintenanceRatio));Assert.All(maintenance,p=>Assert.All(p.Required,k=>Assert.Equal(k.Value,p.Delivered[k.Key],5)));
        Assert.Contains(sim.State.Ledger,l=>l.Category.StartsWith("Manutenção /")&&l.To=="global"&&l.Amount>0);Assert.Equal(sim.SaveToJson(),CitySimulation.FromJson(sim.SaveToJson()).SaveToJson());Assert.False(sim.CancelProject(maintenance[0].Id).Success);
    }
    [Fact] public void No_cash_does_not_fake_maintenance_and_recovery_uses_real_loan_and_deliveries()
    {
        var sim=Founded();var hall=sim.State.Buildings.Single();sim.State.Wallets["global"]+=sim.CityBalance;sim.State.Wallets["city"]=0;hall.MaintenanceDueAt=sim.State.ElapsedSeconds-1;sim.State.EconomyNextAt=sim.State.ElapsedSeconds;var total=sim.MonetaryTotal;sim.Advance(86400);
        Assert.True(hall.MaintenanceRatio>0&&hall.MaintenanceRatio<1);Assert.DoesNotContain(sim.State.Projects,p=>p.IsMaintenance);Assert.Equal(total,sim.MonetaryTotal);Assert.Equal(0,sim.CityBalance);
        Assert.True(sim.Borrow(10000).Success);sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);var project=Assert.Single(sim.State.Projects,p=>p.IsMaintenance&&p.BuildingId==hall.Id);Assert.Equal(0,project.Progress);Assert.True(hall.MaintenanceRatio<1);
        sim.Advance(14400);Assert.Equal(WorkStatus.Completed,project.Status);Assert.Equal(1,hall.MaintenanceRatio);sim.CheckInvariants();
    }
    [Fact] public void Removing_every_target_road_releases_only_unpaid_maintenance_and_preserves_paid_cargo()
    {
        var sim=Founded();var roads=sim.State.Roads.Where(r=>!r.Highway).TakeLast(3).ToList();
        foreach(var road in roads)road.MaintenanceDueAt=sim.State.ElapsedSeconds-1;
        sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);
        var project=Assert.Single(sim.State.Projects,p=>p.IsMaintenance);Assert.True(project.BudgetRemaining>0);Assert.True(project.CostPaid>0);
        var paid=project.CostPaid;var total=sim.MonetaryTotal;var shipments=sim.State.Shipments.Where(s=>s.ProjectId==project.Id&&!s.Complete).ToList();Assert.NotEmpty(shipments);
        var cargo=shipments.ToDictionary(s=>s.VehicleId,s=>sim.State.Vehicles.Single(v=>v.Id==s.VehicleId).Cargo.Values.Sum());
        foreach(var road in roads)Assert.True(sim.DemolishRoad(road.Id).Success);
        Assert.Equal(WorkStatus.Cancelled,project.Status);Assert.Equal(0,project.BudgetRemaining);Assert.Equal(paid,project.CostPaid);Assert.Equal(0,project.Progress);
        Assert.All(shipments,s=>{Assert.True(s.Cancelled);Assert.False(s.Complete);Assert.Equal(cargo[s.VehicleId],sim.State.Vehicles.Single(v=>v.Id==s.VehicleId).Cargo.Values.Sum());});
        Assert.Equal(total,sim.MonetaryTotal);sim.CheckInvariants();
        var restored=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(120);restored.Advance(120);Assert.Equal(sim.SaveToJson(),restored.SaveToJson());
    }
    [Fact] public void Existing_save_with_no_remaining_maintenance_road_is_reconciled_on_next_step()
    {
        var sim=Founded();var roads=sim.State.Roads.Where(r=>!r.Highway).TakeLast(3).ToList();
        foreach(var road in roads)road.MaintenanceDueAt=sim.State.ElapsedSeconds-1;
        sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);var project=Assert.Single(sim.State.Projects,p=>p.IsMaintenance);
        var shipment=sim.State.Shipments.First(s=>s.ProjectId==project.Id);var truck=sim.State.Vehicles.Single(v=>v.Id==shipment.VehicleId);
        truck.Position=shipment.Destination+new Point(0,sim.State.Tuning.RoadWidth/2+1.5);truck.Status=VehicleStatus.Unloading;truck.LoadTimer=0;truck.RouteIndex=truck.Route.Count;
        var cargo=truck.Cargo.Values.Sum();Assert.True(cargo>0);
        sim.State.Roads.RemoveAll(r=>roads.Contains(r));sim=CitySimulation.FromJson(sim.SaveToJson());var paid=project.CostPaid;
        sim.Advance(2);project=sim.State.Projects.Single(p=>p.Id==project.Id);
        Assert.Equal(WorkStatus.Cancelled,project.Status);Assert.Equal(0,project.BudgetRemaining);Assert.Equal(paid,project.CostPaid);
        Assert.Empty(project.Delivered);Assert.Equal(cargo,sim.State.Vehicles.Single(v=>v.Id==truck.Id).Cargo.Values.Sum());sim.CheckInvariants();
    }
    [Fact] public void Removing_one_target_keeps_maintenance_of_existing_roads_active()
    {
        var sim=Founded();var roads=sim.State.Roads.Where(r=>!r.Highway).TakeLast(3).ToList();
        foreach(var road in roads){road.MaintenanceDueAt=sim.State.ElapsedSeconds-1;road.MaintenanceRatio=.7;}
        sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);var project=Assert.Single(sim.State.Projects,p=>p.IsMaintenance);
        Assert.True(sim.DemolishRoad(roads[^1].Id).Success);Assert.NotEqual(WorkStatus.Cancelled,project.Status);sim.Advance(7200);
        Assert.Equal(WorkStatus.Completed,project.Status);Assert.All(roads.Take(2),r=>Assert.Equal(1,r.MaintenanceRatio));sim.CheckInvariants();
    }
    [Fact] public void Isolated_public_building_stays_pending_and_resumes_after_real_reconnection()
    {
        var sim=Founded();var hall=sim.State.Buildings.Single();hall.MaintenanceDueAt=sim.State.ElapsedSeconds-1;hall.MaintenanceRatio=.5;
        sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);var project=Assert.Single(sim.State.Projects,p=>p.IsMaintenance);
        var road=sim.State.Roads.Single(r=>r.Position==new Point(-164,0));Assert.True(sim.DemolishRoad(road.Id).Success);
        sim.Advance(60);Assert.False(sim.HasExteriorAccess(hall));Assert.NotEqual(WorkStatus.Cancelled,project.Status);Assert.Equal(0,project.Progress);Assert.True(project.BudgetRemaining>0);Assert.True(hall.MaintenanceRatio<=.5);
        Assert.True(sim.PlaceRoad(road.Position,road.Position).Success);sim.Advance(7200);
        Assert.Equal(WorkStatus.Completed,project.Status);Assert.Equal(1,hall.MaintenanceRatio);Assert.False(hall.Removed);sim.CheckInvariants();
    }
    [Fact] public void Removing_maintenance_target_pays_work_already_done_and_returns_the_actual_crew()
    {
        var sim=Founded();var road=sim.State.Roads.Single(r=>r.Position==new Point(-180,0));road.MaintenanceDueAt=sim.State.ElapsedSeconds-1;
        sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);var project=Assert.Single(sim.State.Projects,p=>p.IsMaintenance);
        for(var i=0;i<1000&&project.Progress==0;i++)sim.Advance(2);
        Assert.True(project.Progress>0&&project.Progress<1);Assert.True(project.CrewCostDue>0);
        var crew=sim.State.Vehicles.Single(v=>v.Id==project.CrewVehicleId);var position=crew.Position;var paid=project.CostPaid;var due=decimal.Round(project.CrewCostDue,2,MidpointRounding.AwayFromZero);
        Assert.True(sim.DemolishRoad(road.Id).Success);Assert.Equal(WorkStatus.Cancelled,project.Status);Assert.Equal(paid+due,project.CostPaid);Assert.Equal(0,project.CrewCostDue);Assert.Equal(position,crew.Position);
        Assert.Equal(VehicleStatus.Waiting,crew.Status);sim.Advance(60);Assert.Equal(position,crew.Position);
        Assert.True(sim.PlaceRoad(road.Position,road.Position).Success);sim.Advance(7200);Assert.Equal(VehicleStatus.Outside,crew.Status);Assert.False(crew.CrewAboard);sim.CheckInvariants();
    }
    [Fact] public void Obsolete_maintenance_returns_crew_only_trip_without_reserving_a_cargo_holding_space()
    {
        var sim=Founded();var road=sim.State.Roads.Single(r=>r.Position==new Point(-180,0));
        // Restore the physical state after materials were consumed at the site,
        // with no crew present yet; dispatch must use a genuine empty crew trip.
        var project=new WorkProject{Id=sim.State.NextId++,IsMaintenance=true,MaintenanceRoadIds=[road.Id],RoadPoints=[road.Position],Position=road.Position,Required=new(){{ResourceKind.Asphalt,.1}},Delivered=new(){{ResourceKind.Asphalt,.1}},WorkSeconds=60,BudgetRemaining=1000};sim.State.Projects.Add(project);
        sim.Advance(2);var crew=sim.State.Vehicles.Single(v=>v.Id==project.CrewVehicleId);var shipment=sim.State.Shipments.Single(s=>s.ProjectId==project.Id);Assert.Empty(crew.Cargo);Assert.True(crew.CrewAboard);var position=crew.Position;
        Assert.True(sim.DemolishRoad(road.Id).Success);Assert.Equal(WorkStatus.Cancelled,project.Status);Assert.Equal(position,crew.Position);
        sim.Advance(120);Assert.Equal(VehicleStatus.Outside,crew.Status);Assert.Null(crew.HoldingPosition);Assert.True(shipment.Complete);sim.CheckInvariants();
    }
}
