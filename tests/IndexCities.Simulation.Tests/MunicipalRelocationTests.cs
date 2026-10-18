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

    [Fact] public void Stock_without_room_waits_then_is_lost_once_with_visible_cause()
    {
        var sim=City();sim.State.Tuning.MunicipalRelocationStockWaitSeconds=2*3600;var depot=Single(sim,BuildingKind.WorksDepot);
        depot.Stock[ResourceKind.Wood]=100;depot.Stock[ResourceKind.Steel]=80;
        var at=Destination(sim,depot);var quote=sim.QuoteMunicipalRelocation(depot.Id,at,Math.PI);
        Assert.True(quote.StockBeyondDestination>0);Assert.Contains("risco de perda",quote.Message);
        Assert.True(sim.RelocateMunicipalBuilding(depot.Id,at,Math.PI,quote).Success);
        var order=RunUntilDone(sim,sim.State.MunicipalRelocations.Single());
        var target=Single(sim,BuildingKind.WorksDepot);
        Assert.True(target.Stock.Values.Sum()<=target.Definition.Capacity+.001);
        Assert.NotEmpty(order.StockLost);Assert.Contains("estoque perdido na retirada",order.Cause);Assert.Contains("sem espaço",order.Cause);
        var delivered=sim.State.Shipments.Where(s=>order.TransferShipmentIds.Contains(s.Id)&&s.Complete&&!s.Cancelled).Sum(s=>s.Cargo.Values.Sum());
        Assert.Equal(180,delivered+order.StockLost.Values.Sum()+SuppliedToWorks(sim,depot,ResourceKind.Wood)+SuppliedToWorks(sim,depot,ResourceKind.Steel),3);
        Assert.True(depot.Removed);Assert.Empty(depot.Stock);
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
        var sim=City();sim.State.Tuning.MunicipalRelocationStockWaitSeconds=2*3600;var depot=Single(sim,BuildingKind.WorksDepot);depot.Stock[ResourceKind.Wood]=60;
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
