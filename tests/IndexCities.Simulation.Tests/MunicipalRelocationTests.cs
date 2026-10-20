using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// Ciclos reais de Mover prédio municipal concluído sobre a cidade do ciclo
// integrado (obras executadas fisicamente até o fim, SIMs contratados). Cada
// teste parte do mesmo save serializado para isolar mutações.
public sealed class MunicipalRelocationTests
{
    static readonly Lazy<string> Built=new(()=>
    {
        var sim=CityCycleTests.City();
        for(var i=0;i<240&&sim.State.Projects.Any(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress&&!p.IsMaintenance);i++)sim.Advance(3600);
        Assert.DoesNotContain(sim.State.Projects,p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress&&!p.IsMaintenance);
        sim.Advance(6*3600);return sim.SaveToJson();
    });
    static CitySimulation City()=>CitySimulation.FromJson(Built.Value);
    static Building Single(CitySimulation sim,BuildingKind kind)=>sim.State.Buildings.Single(b=>!b.Removed&&b.Kind==kind);
    // First free lot south of the main street (door facing it) where the
    // relocation is allowed and the destination already has street access.
    static Point Destination(CitySimulation sim,Building source)
    {
        for(var x=140.0;x<=232;x+=4)for(var z=-14.0;z>=-40;z-=2){var at=new Point(x,z);var quote=sim.QuoteMunicipalRelocation(source.Id,at,Math.PI);if(quote.Allowed&&quote.Construction!.Accessible)return at;}
        throw new InvalidOperationException("Sem destino acessível para "+source.Definition.Name);
    }
    static MunicipalRelocationOrder RunUntilDone(CitySimulation sim,MunicipalRelocationOrder order,int hours=200)
    {
        for(var i=0;i<hours&&order.Active;i++){sim.Advance(3600);sim.CheckInvariants();order=sim.State.MunicipalRelocations.Single(r=>r.Id==order.Id);}
        Assert.False(order.Active,"Mudança não terminou: "+order.Cause);return order;
    }
    // Material the old depot legitimately supplied to works (the substitute's own
    // included) by real loads before it closed: part of the conservation balance.
    static double SuppliedToWorks(CitySimulation sim,Building depot,ResourceKind kind)=>sim.State.Shipments.Where(s=>s.SourceBuildingId==depot.Id&&s.ProjectId!=null&&s.PickedUp).Sum(s=>s.Cargo.GetValueOrDefault(kind));
    static decimal Paid(CitySimulation sim,string reasonStart)=>sim.State.Ledger.Where(m=>m.Category=="Mudança municipal"&&m.Reason.StartsWith(reasonStart)).Sum(m=>m.Amount);

    [Fact] public void City_hall_moves_with_real_work_one_institution_and_tools_kept()
    {
        var sim=City();var hall=Single(sim,BuildingKind.CityHall);var at=Destination(sim,hall);
        var quote=sim.QuoteMunicipalRelocation(hall.Id,at,Math.PI);var committed=sim.Committed;
        var result=sim.RelocateMunicipalBuilding(hall.Id,at,Math.PI,quote);
        Assert.True(result.Success,result.Message);
        var order=sim.State.MunicipalRelocations.Single();var target=sim.State.Buildings.Single(b=>b.Id==result.Id);
        Assert.Equal(MunicipalRelocationStage.Constructing,order.Stage);Assert.False(target.Completed);
        Assert.Equal(committed+quote.TotalCost,sim.Committed);
        // Old seat stays physically and the management keeps working.
        Assert.False(hall.Removed);Assert.True(sim.State.CityHallPlaced);
        Assert.True(sim.SetTax(0,.02m).Success);
        Assert.False(sim.PlaceBuilding(BuildingKind.CityHall,new(200,-60),Math.PI).Success);
        Assert.False(sim.QuoteMunicipalRelocation(hall.Id,new(200,-60),Math.PI).Allowed);
        order=RunUntilDone(sim,order);
        Assert.Equal(MunicipalRelocationStage.Completed,order.Stage);
        Assert.True(target.Completed);Assert.True(hall.Removed);
        Assert.Single(sim.State.Buildings,b=>!b.Removed&&b.Kind==BuildingKind.CityHall);
        Assert.Equal(quote.RemovalCost,Paid(sim,"Retirada física"));Assert.Equal(0m,order.RemovalReserved+order.TransferReserved);
        Assert.True(sim.State.CityHallPlaced);Assert.True(sim.SetTax(0,.03m).Success);
        Assert.False(sim.PlaceBuilding(BuildingKind.CityHall,new(200,-60),Math.PI).Success);
        Assert.Contains("concluída",order.Cause);
    }

    [Fact] public void Depot_keeps_operating_during_work_then_moves_staff_and_stock_by_truck()
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);
        // Fixture: material already stored at the depot (as from a redirected load).
        depot.Stock[ResourceKind.Wood]=40;
        var staff=sim.State.Citizens.Where(c=>c.Alive&&c.WorkplaceId==depot.Id).Select(c=>c.Id).ToList();Assert.NotEmpty(staff);
        var at=Destination(sim,depot);var quote=sim.QuoteMunicipalRelocation(depot.Id,at,Math.PI);
        Assert.True(quote.TransferEstimate>0);Assert.Contains("caminhão",quote.Message);
        var result=sim.RelocateMunicipalBuilding(depot.Id,at,Math.PI,quote);Assert.True(result.Success,result.Message);
        var order=sim.State.MunicipalRelocations.Single();var target=sim.State.Buildings.Single(b=>b.Id==result.Id);
        // While the substitute is built the old depot keeps its staff, stock and
        // operation (it even supplies the new work); no artificial closure.
        for(var i=0;i<600&&order.Stage==MunicipalRelocationStage.Constructing;i++){
            Assert.False(depot.OperationClosed);Assert.False(depot.Removed);Assert.Contains(sim.State.Citizens,c=>c.WorkplaceId==depot.Id);
            sim.Advance(30);sim.CheckInvariants();order=sim.State.MunicipalRelocations.Single();
        }
        Assert.NotEqual(MunicipalRelocationStage.Constructing,order.Stage);
        order=RunUntilDone(sim,order);
        Assert.Equal(MunicipalRelocationStage.Completed,order.Stage);Assert.True(depot.Removed);Assert.True(target.Completed);
        Assert.DoesNotContain(sim.State.Citizens,c=>c.WorkplaceId==depot.Id);
        Assert.True(sim.State.Citizens.Count(c=>c.Alive&&c.WorkplaceId==target.Id)<=target.Definition.Jobs);
        Assert.Equal(staff.Count,order.WorkersContinued+order.WorkersLeft);
        // Stock arrived physically: never copied, never lost while a truck could carry it.
        Assert.Empty(order.StockLost);Assert.NotEmpty(order.TransferShipmentIds);
        Assert.All(order.TransferShipmentIds,id=>Assert.True(sim.State.Shipments.Single(s=>s.Id==id).Complete));
        var moved=sim.State.Shipments.Where(s=>order.TransferShipmentIds.Contains(s.Id)).Sum(s=>s.Cargo.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(40,moved+SuppliedToWorks(sim,depot,ResourceKind.Wood),3);Assert.True(order.TransferPaid>0);Assert.Equal(order.TransferPaid,Paid(sim,"Frete e combustível"));
        Assert.Equal(quote.RemovalCost,Paid(sim,"Retirada física"));
        Assert.Single(sim.State.Buildings,b=>!b.Removed&&b.Kind==BuildingKind.WorksDepot);
        Assert.Contains("estoque levado por caminhão",order.Cause);
    }

    // Fixture above the capacity of the same type: no common path fills a depot
    // past 150, but a destination already holding stock gives the same block.
    // Before the approved no-progress deadline, room becoming available resumes recovery.
    [Fact] public void Stock_without_room_keeps_the_old_address_waiting_with_visible_cause()
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);
        depot.Stock[ResourceKind.Wood]=100;depot.Stock[ResourceKind.Steel]=80;
        var at=Destination(sim,depot);var quote=sim.QuoteMunicipalRelocation(depot.Id,at,Math.PI);
        Assert.True(quote.StockBeyondDestination>0);Assert.Contains("não cabe",quote.Message);
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,at,Math.PI,quote).Success);
        var order=sim.State.MunicipalRelocations.Single();
        for(var i=0;i<12;i++){sim.Advance(3600);sim.CheckInvariants();}
        var target=sim.State.Buildings.Single(b=>b.Id==order.TargetBuildingId);
        Assert.Equal(MunicipalRelocationStage.Transferring,order.Stage);Assert.True(target.Completed);
        Assert.False(depot.Removed);Assert.True(depot.OperationClosed);Assert.Empty(order.StockLost);
        Assert.Contains("sem espaço",order.Cause);Assert.Contains("prazo",order.Cause);
        Assert.True(target.Stock.Values.Sum()<=target.Definition.Capacity+.001);
        var delivered=sim.State.Shipments.Where(s=>order.TransferShipmentIds.Contains(s.Id)&&s.Complete&&!s.Cancelled).Sum(s=>s.Cargo.Values.Sum());
        Assert.Equal(180,delivered+depot.Stock.Values.Sum()+SuppliedToWorks(sim,depot,ResourceKind.Wood)+SuppliedToWorks(sim,depot,ResourceKind.Steel),3);
        Assert.True(order.RemovalReserved>0);Assert.Equal(0m,Paid(sim,"Retirada física"));
        // Room at the new address (here: material used elsewhere) lets the rest leave.
        target.Stock.Clear();
        order=RunUntilDone(sim,order);
        Assert.Equal(MunicipalRelocationStage.Completed,order.Stage);Assert.Empty(order.StockLost);Assert.True(depot.Removed);
    }

    [Fact] public void Cancelling_the_substitute_work_ends_the_move_and_keeps_the_old_building()
    {
        var sim=City();var plant=Single(sim,BuildingKind.WaterTreatment);var at=Destination(sim,plant);
        var quote=sim.QuoteMunicipalRelocation(plant.Id,at,Math.PI);var before=sim.Committed;
        var result=sim.RelocateMunicipalBuilding(plant.Id,at,Math.PI,quote);Assert.True(result.Success,result.Message);
        sim.Advance(600);
        var project=sim.State.Projects.Single(p=>p.BuildingId==result.Id);
        Assert.Equal(quote.RemovalCost+quote.TransferEstimate,sim.RelocationReservedFor(project.Id));
        Assert.True(sim.CancelProject(project.Id).Success);
        var order=sim.State.MunicipalRelocations.Single();
        Assert.Equal(MunicipalRelocationStage.Cancelled,order.Stage);Assert.Equal(0m,order.RemovalReserved+order.TransferReserved);
        Assert.True(sim.Committed<=before+.01m);Assert.Equal(0m,Paid(sim,"Retirada física"));
        Assert.False(plant.Removed);Assert.False(plant.OperationClosed);Assert.Contains(sim.State.Citizens,c=>c.WorkplaceId==plant.Id);
        sim.Advance(3600);sim.CheckInvariants();Assert.True(sim.QuoteMunicipalRelocation(plant.Id,at,Math.PI).Allowed);
    }

    [Fact] public void Residual_stock_is_lost_once_after_a_day_without_room_and_survives_save_load()
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);
        depot.Stock[ResourceKind.Wood]=100;depot.Stock[ResourceKind.Steel]=80;
        var quote=sim.QuoteMunicipalRelocation(depot.Id,Destination(sim,depot),Math.PI);
        Assert.Contains("24 h",quote.Message);Assert.Contains("perda",quote.Message);
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,Destination(sim,depot),Math.PI,quote).Success);
        var order=sim.State.MunicipalRelocations.Single();
        for(var i=0;i<360&&order.StockRecoveryBlockedSeconds<3600;i++)sim.Advance(60);
        Assert.True(order.StockRecoveryBlockedSeconds>=3600,order.Cause);
        Assert.False(depot.Removed);Assert.Empty(order.StockLost);
        var elapsed=order.StockRecoveryBlockedSeconds;
        sim=CitySimulation.FromJson(sim.SaveToJson());order=sim.State.MunicipalRelocations.Single();
        Assert.Equal(elapsed,order.StockRecoveryBlockedSeconds);
        order=RunUntilDone(sim,order,30);Assert.NotEmpty(order.StockLost);
        depot=sim.State.Buildings.Single(b=>b.Id==depot.Id);
        var delivered=sim.State.Shipments.Where(s=>order.TransferShipmentIds.Contains(s.Id)&&s.Complete&&!s.Cancelled).Sum(s=>s.Cargo.Values.Sum());
        Assert.Equal(180,delivered+order.StockLost.Values.Sum()+SuppliedToWorks(sim,depot,ResourceKind.Wood)+SuppliedToWorks(sim,depot,ResourceKind.Steel),3);
        Assert.True(depot.Removed);Assert.Empty(depot.Stock);Assert.Equal(quote.RemovalCost,Paid(sim,"Retirada física"));
        Assert.Contains("perda do titular city",order.Cause);
        var loss=order.StockLost.Values.Sum();var removal=order.RemovalPaid;
        sim=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(3600);sim.CheckInvariants();
        order=sim.State.MunicipalRelocations.Single();Assert.Equal(loss,order.StockLost.Values.Sum());Assert.Equal(removal,Paid(sim,"Retirada física"));
    }

    // Any positive quantity left at the old address is a real loss: recorded once,
    // exactly, even below what the trucks would carry.
    [Fact] public void Residual_below_a_truckload_unit_is_recorded_exactly_as_loss()
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);
        depot.Stock.Clear();depot.Stock.Units[ResourceKind.Wood]=500;
        var at=Destination(sim,depot);var quote=sim.QuoteMunicipalRelocation(depot.Id,at,Math.PI);
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,at,Math.PI,quote).Success);
        var order=RunUntilDone(sim,sim.State.MunicipalRelocations.Single());
        Assert.True(sim.State.Buildings.Single(b=>b.Id==depot.Id).Removed);
        // The event states what happened: no 24 h deadline, the exact amount written off.
        Assert.DoesNotContain("24 h",order.Cause);Assert.Contains("Madeira 0.0000005",order.Cause);Assert.Contains("perda do titular",order.Cause);
        Assert.Equal(500,order.StockLost.Units.GetValueOrDefault(ResourceKind.Wood));
        order=CitySimulation.FromJson(sim.SaveToJson()).State.MunicipalRelocations.Single();
        Assert.Equal(500,order.StockLost.Units.GetValueOrDefault(ResourceKind.Wood));
    }

    [Theory]
    [InlineData("truck")][InlineData("cash")][InlineData("fuel")]
    public void Temporary_transport_shortages_pause_the_loss_clock_then_recovery_resumes(string shortage)
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);
        depot.Stock[ResourceKind.Wood]=100;depot.Stock[ResourceKind.Steel]=80;
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,Destination(sim,depot),Math.PI).Success);
        var order=sim.State.MunicipalRelocations.Single();
        for(var i=0;i<360&&order.StockRecoveryBlockedSeconds<600;i++)sim.Advance(60);
        Assert.True(order.StockRecoveryBlockedSeconds>=600,order.Cause);
        var target=sim.State.Buildings.Single(b=>b.Id==order.TargetBuildingId);
        // Fixture: stock was consumed elsewhere. No room/route impediment remains.
        target.Stock.Clear();var elapsed=order.StockRecoveryBlockedSeconds;
        var trucks=sim.State.Tuning.ExternalTruckCount;var fuel=sim.State.OutsideStock[ResourceKind.Fuel];var balance=sim.CityBalance;
        if(shortage=="truck")sim.State.Tuning.ExternalTruckCount=0;
        if(shortage=="fuel"){
            sim.State.OutsideStock[ResourceKind.Fuel]=0;
            foreach(var truck in sim.State.Vehicles.Where(v=>v.External&&v.Status==VehicleStatus.Outside))truck.Fuel=0;
        }
        if(shortage=="cash"){
            order.TransferReserved=0;sim.State.Wallets["global"]+=balance-sim.Committed;sim.State.Wallets["city"]=sim.Committed;
        }
        sim.Advance(120);sim.CheckInvariants();
        Assert.True(order.Active);Assert.False(depot.Removed);Assert.Empty(order.StockLost);
        Assert.Equal(elapsed,order.StockRecoveryBlockedSeconds);Assert.Contains("pausado",order.Cause);
        sim.State.Tuning.ExternalTruckCount=trucks;sim.State.OutsideStock[ResourceKind.Fuel]=fuel;
        if(shortage=="cash"){var restore=balance-sim.CityBalance;sim.State.Wallets["global"]-=restore;sim.State.Wallets["city"]+=restore;}
        order=RunUntilDone(sim,order);Assert.Empty(order.StockLost);Assert.True(depot.Removed);
    }

    [Fact] public void A_completed_destination_without_road_access_does_not_close_the_viable_old_service()
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);depot.Stock[ResourceKind.Wood]=40;
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,Destination(sim,depot),Math.PI).Success);
        var order=sim.State.MunicipalRelocations.Single();
        // Finish the real work but intercept the completion boundary by removing
        // its ordinary-road access while the crew can still use construction roads.
        var target=sim.State.Buildings.Single(b=>b.Id==order.TargetBuildingId);
        for(var i=0;i<3000&&!target.Completed;i++){
            var p=sim.State.Projects.Single(p=>p.Id==order.TargetProjectId);
            if(p.Progress>.9){
                foreach(var road in sim.State.Roads.Where(r=>r.Position.Distance(target.Door)<12))road.ConstructionOnly=true;
                sim=CitySimulation.FromJson(sim.SaveToJson());order=sim.State.MunicipalRelocations.Single();
                target=sim.State.Buildings.Single(b=>b.Id==order.TargetBuildingId);depot=sim.State.Buildings.Single(b=>b.Id==depot.Id);
            }
            sim.Advance(10);
        }
        Assert.True(target.Completed);Assert.Equal(MunicipalRelocationStage.Constructing,order.Stage);
        Assert.True(sim.HasExteriorAccess(depot));Assert.False(sim.HasExteriorAccess(target));
        Assert.False(depot.OperationClosed);Assert.False(depot.Removed);Assert.True(target.OperationClosed);
        Assert.Contains(sim.State.Citizens,c=>c.WorkplaceId==depot.Id);Assert.DoesNotContain(sim.State.Citizens,c=>c.WorkplaceId==target.Id);
        Assert.Equal(0m,Paid(sim,"Retirada física"));Assert.Contains("fisicamente",order.Cause.Replace("estrutura/acesso aptos","fisicamente apta"));
        foreach(var road in sim.State.Roads.Where(r=>r.Position.Distance(target.Door)<12))road.ConstructionOnly=false;
        sim=CitySimulation.FromJson(sim.SaveToJson());order=RunUntilDone(sim,sim.State.MunicipalRelocations.Single());
        Assert.Equal(MunicipalRelocationStage.Completed,order.Stage);Assert.Empty(order.StockLost);
    }

    [Fact] public void Moving_the_substitute_work_keeps_the_relocation_linked()
    {
        var sim=City();var hall=Single(sim,BuildingKind.CityHall);var at=Destination(sim,hall);
        var result=sim.RelocateMunicipalBuilding(hall.Id,at,Math.PI);Assert.True(result.Success,result.Message);
        var project=sim.State.Projects.Single(p=>p.BuildingId==result.Id);
        var other=new Point(at.X,at.Z-24);if(!sim.QuoteMoveBuilding(project.Id,other,Math.PI).PhysicallyValid)other=new Point(at.X+28,at.Z);
        var moved=sim.MoveBuildingProject(project.Id,other,Math.PI);Assert.True(moved.Success,moved.Message);
        var order=sim.State.MunicipalRelocations.Single();
        Assert.True(order.Active);Assert.Equal(moved.Id,order.TargetBuildingId);
        Assert.Equal(2,sim.State.Buildings.Count(b=>!b.Removed&&b.Kind==BuildingKind.CityHall));
    }

    [Fact] public void A_missing_route_counts_toward_residual_loss_even_without_an_available_truck()
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);
        depot.Stock[ResourceKind.Wood]=100;depot.Stock[ResourceKind.Steel]=80;
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,Destination(sim,depot),Math.PI).Success);
        var order=sim.State.MunicipalRelocations.Single();
        for(var i=0;i<360&&order.StockRecoveryBlockedSeconds<600;i++)sim.Advance(60);
        Assert.True(order.StockRecoveryBlockedSeconds>=600,order.Cause);
        var target=sim.State.Buildings.Single(b=>b.Id==order.TargetBuildingId);target.Stock.Clear();
        foreach(var road in sim.State.Roads.Where(r=>r.Position.Distance(depot.Door)<12))road.ConstructionOnly=true;
        sim.State.Tuning.ExternalTruckCount=0;
        sim=CitySimulation.FromJson(sim.SaveToJson());order=sim.State.MunicipalRelocations.Single();
        sim.Advance(60);Assert.Contains("acesso viário",order.Cause);
        order=RunUntilDone(sim,order,30);Assert.NotEmpty(order.StockLost);
        Assert.True(sim.State.Buildings.Single(b=>b.Id==depot.Id).Removed);sim.CheckInvariants();
    }

    [Theory]
    [InlineData("moving")][InlineData("fuel")][InlineData("capacity")][InlineData("ready")]
    public void Loaded_cargo_is_redirected_only_from_the_old_bay_with_room_fuel_and_paid_freight(string condition)
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);depot.Stock[ResourceKind.Wood]=60;
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,Destination(sim,depot),Math.PI).Success);
        var order=sim.State.MunicipalRelocations.Single();
        for(var i=0;i<3000&&order.TransferShipmentIds.Count==0;i++)sim.Advance(2);
        Assert.Equal(MunicipalRelocationStage.Transferring,order.Stage);
        var target=sim.State.Buildings.Single(b=>b.Id==order.TargetBuildingId);
        var load=sim.State.Shipments.First(s=>order.TransferShipmentIds.Contains(s.Id));
        var vehicle=sim.State.Vehicles.Single(v=>v.Id==load.VehicleId);
        // Fixture at arrival: already-owned material loaded into the same body,
        // bound for the old depot. Ownership/quantity remain explicit.
        foreach(var item in load.Cargo)depot.AddStock(item.Key,-item.Value);
        load.PickedUp=true;load.DestinationBuildingId=depot.Id;vehicle.Cargo=new(load.Cargo);
        vehicle.Position=vehicle.DockPosition!.Value;vehicle.RouteIndex=vehicle.Route.Count;vehicle.Status=VehicleStatus.Unloading;vehicle.LoadTimer=1000;vehicle.Fuel=sim.State.Tuning.FuelTank;
        if(condition=="fuel")vehicle.Fuel=0;
        if(condition=="capacity")target.Stock[ResourceKind.Steel]=target.Definition.Capacity;
        if(condition=="moving"){vehicle.Position=sim.State.Entry+new Point(2,0);vehicle.RouteIndex=1;vehicle.Status=VehicleStatus.Travelling;}
        var route=vehicle.Route.ToList();var paid=order.TransferPaid;var quantity=vehicle.Cargo.Values.Sum();var body=vehicle.Body;
        sim.Advance(2);sim.CheckInvariants();
        Assert.Equal(body,vehicle.Body);Assert.Equal("city",load.GoodsOwnerAccount);Assert.Equal(quantity,vehicle.Cargo.Values.Sum());
        if(condition!="ready"){
            Assert.Equal(depot.Id,load.DestinationBuildingId);Assert.Equal(route,vehicle.Route);Assert.Equal(paid,order.TransferPaid);
        }else{
            Assert.Equal(target.Id,load.DestinationBuildingId);Assert.True(order.TransferPaid>paid);Assert.Equal(VehicleStatus.Travelling,vehicle.Status);
            order=RunUntilDone(sim,order);Assert.Empty(order.StockLost);Assert.True(load.Complete);Assert.Equal(order.TransferPaid,Paid(sim,"Frete e combustível"));
        }
    }

    [Fact] public void Deadline_removes_only_residual_stock_and_preserves_loaded_inbound_cargo_after_reload()
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);
        depot.Stock[ResourceKind.Wood]=100;depot.Stock[ResourceKind.Steel]=80;
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,Destination(sim,depot),Math.PI).Success);
        var order=sim.State.MunicipalRelocations.Single();
        for(var i=0;i<360&&order.StockRecoveryBlockedSeconds<600;i++)sim.Advance(60);
        Assert.True(order.StockRecoveryBlockedSeconds>=600,order.Cause);
        // Boundary fixture: loaded, owned wood travelling to the old depot just
        // as the real no-room deadline expires. It is not inside the structure.
        order.StockRecoveryBlockedSeconds=CitySimulation.MunicipalStockRecoveryDeadline-2;
        var vehicle=sim.State.Vehicles.First(v=>v.External&&v.Body==TruckBody.Flatbed&&v.Status==VehicleStatus.Outside);
        var access=sim.Access(depot)!;var route=sim.FindRoadRoute(sim.State.Roads.Single(r=>r.Position.Distance(sim.State.Entry)<.1).Id,access.Id);
        var cargo=new Dictionary<ResourceKind,double>{{ResourceKind.Wood,5}};
        var load=new Shipment{Id=sim.State.NextId++,VehicleId=vehicle.Id,DestinationBuildingId=depot.Id,Destination=access.Position,PickedUp=true,GoodsOwnerAccount="city",Cargo=new(cargo),Purpose=ShipmentPurpose.Maintenance};
        sim.State.Shipments.Add(load);sim.State.OutsideStock[ResourceKind.Wood]-=5;
        vehicle.ShipmentId=load.Id;vehicle.Cargo=new(cargo);vehicle.Route=route;vehicle.RouteIndex=0;vehicle.Position=sim.State.Entry;vehicle.Fuel=sim.State.Tuning.FuelTank;vehicle.Status=VehicleStatus.Travelling;
        vehicle.DockPosition=access.Position+new Point(0,-(sim.State.Tuning.RoadWidth/2+1.5));
        var residual=depot.Stock.Values.Sum();sim.Advance(2);sim.CheckInvariants();
        Assert.True(depot.Removed);Assert.Equal(residual,order.StockLost.Values.Sum(),3);Assert.True(load.Cancelled);
        Assert.Equal(5,vehicle.Cargo.GetValueOrDefault(ResourceKind.Wood));Assert.Equal(route,vehicle.Route);Assert.Equal("city",load.GoodsOwnerAccount);
        sim=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(120);sim.CheckInvariants();
        load=sim.State.Shipments.Single(s=>s.Id==load.Id);Assert.Equal("city",load.GoodsOwnerAccount);Assert.Equal(5,load.Cargo[ResourceKind.Wood]);
        Assert.Equal(residual,sim.State.MunicipalRelocations.Single().StockLost.Values.Sum(),3);
        Assert.Equal(5,sim.State.Vehicles.Single(v=>v.Id==vehicle.Id).Cargo.GetValueOrDefault(ResourceKind.Wood));
    }

    [Fact] public void Refusals_and_stale_previews_do_not_mutate_the_city()
    {
        var sim=City();var plant=Single(sim,BuildingKind.SewageTreatment);var at=Destination(sim,plant);
        var reviewed=sim.QuoteMunicipalRelocation(plant.Id,at,Math.PI);
        var before=sim.SaveToJson();
        Assert.False(sim.RelocateMunicipalBuilding(plant.Id,sim.State.LakeCentre,0).Success);
        Assert.False(sim.RelocateMunicipalBuilding(plant.Id,plant.Position,plant.Rotation).Success);
        var stale=reviewed with{TotalCost=reviewed.TotalCost+1};
        var refused=sim.RelocateMunicipalBuilding(plant.Id,at,Math.PI,stale);
        Assert.False(refused.Success);Assert.Equal(CitySimulation.StaleRelocationPreview,refused.Message);
        Assert.Equal(before,sim.SaveToJson());
        // Without free cash for the whole commitment nothing is ordered.
        sim.State.Wallets["global"]+=sim.CityBalance-sim.Committed;sim.State.Wallets["city"]=sim.Committed;
        var poor=sim.SaveToJson();
        var noCash=sim.RelocateMunicipalBuilding(plant.Id,at,Math.PI);
        Assert.False(noCash.Success);Assert.Contains("insuficiente",noCash.Message);Assert.Equal(poor,sim.SaveToJson());
    }

    [Fact] public void Save_and_load_during_each_stage_never_charge_twice_or_duplicate_the_seat()
    {
        var sim=City();var depot=Single(sim,BuildingKind.WorksDepot);depot.Stock[ResourceKind.Wood]=60;
        var at=Destination(sim,depot);var quote=sim.QuoteMunicipalRelocation(depot.Id,at,Math.PI);
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,at,Math.PI,quote).Success);
        sim.Advance(30);
        sim=CitySimulation.FromJson(sim.SaveToJson());
        var order=sim.State.MunicipalRelocations.Single();Assert.Equal(MunicipalRelocationStage.Constructing,order.Stage);Assert.Equal(quote.RemovalCost,order.RemovalReserved);
        for(var i=0;i<2000&&order.Stage==MunicipalRelocationStage.Constructing;i++){sim.Advance(10);order=sim.State.MunicipalRelocations.Single();}
        Assert.Equal(MunicipalRelocationStage.Transferring,order.Stage);
        var reloaded=CitySimulation.FromJson(sim.SaveToJson());
        var copy=reloaded.State.MunicipalRelocations.Single();Assert.Equal(order.TransferPaid,copy.TransferPaid);Assert.Equal(order.Stage,copy.Stage);
        copy=RunUntilDone(reloaded,copy);
        Assert.Equal(MunicipalRelocationStage.Completed,copy.Stage);
        Assert.Equal(quote.RemovalCost,Paid(reloaded,"Retirada física"));
        Assert.Single(reloaded.State.Buildings,b=>!b.Removed&&b.Kind==BuildingKind.WorksDepot);
        Assert.Equal(copy.TransferPaid,Paid(reloaded,"Frete e combustível"));
    }
}
