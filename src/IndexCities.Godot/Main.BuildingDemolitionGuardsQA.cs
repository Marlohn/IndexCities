using Godot;
using IndexCities.Simulation;
using System.Reflection;
using Point = IndexCities.Simulation.Point;
namespace IndexCities.Game;

public partial class Main
{
    async System.Threading.Tasks.Task BuildingDemolitionGuardsSmoke(string directory)
    {
        _sim=new CitySimulation(42);ClearSelection();ClearTool();_world.Reset(_sim.State);
        Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"R1/R2 Prefeitura");
        Require(_sim.PlaceRoad(new(-192,0),new(100,0)).Success,"R1/R2 via real");await RecoveryAdvance(7200);
        var placed=_sim.PlaceBuilding(BuildingKind.House,new(-100,14));Require(placed.Success,"R1/R2 casa");
        for(var i=0;i<96&&!_sim.State.Families.Any(f=>f.HomeId==placed.Id);i++)await RecoveryAdvance(1800);
        Company Add(string name,bool active){var c=new Company{Id=_sim.State.NextId++,Name=name,Active=active};_sim.State.Companies.Add(c);_sim.State.Wallets[c.Account]=5000;_sim.State.Wallets["global"]-=5000;return c;}
        var farmCo=Add("Campo QA",true);var buyer=Add("Mercado QA",false);
        var farm=new Building{Id=_sim.State.NextId++,Kind=BuildingKind.Farm,Position=new(-40,-28),Rotation=Math.PI,Completed=true,OperationalCapacity=1,OwnerAccount=farmCo.Account,CompanyId=farmCo.Id,SalaryOffer=80};
        farm.Stock[ResourceKind.Food]=20;farm.Cause="Alimentos existentes na fazenda";_sim.State.Buildings.Add(farm);farmCo.Buildings.Add(farm.Id);
        Building Shop(double x){var b=new Building{Id=_sim.State.NextId++,Kind=BuildingKind.Grocery,Position=new(x,14),Completed=true,OperationalCapacity=1,OwnerAccount=buyer.Account,CompanyId=buyer.Id};_sim.State.Buildings.Add(b);buyer.Buildings.Add(b.Id);return b;}
        var target=Shop(-60);var sister=Shop(-20);sister.Stock[ResourceKind.Food]=sister.Definition.Capacity;
        var dockAt=typeof(CitySimulation).GetMethod("DockAt",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var truck=new Vehicle{Id=_sim.State.NextId++,OwnerAccount=farmCo.Account,HomeBuildingId=farm.Id,Fuel=_sim.State.Tuning.FuelTank,Status=VehicleStatus.Parked};
        truck.Position=(Point)dockAt.Invoke(_sim,[_sim.Access(farm)!,farm,false])!;_sim.State.Vehicles.Add(truck);
        for(var i=0;i<200&&!(_sim.State.Now.Hour is >=8 and <12&&_sim.State.Now.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday));i++)await RecoveryAdvance(1800);
        var worker=_sim.State.Citizens.First(c=>c.Id==_sim.State.Families.Single(f=>f.HomeId==placed.Id).Members[0]);
        worker.WorkplaceId=farm.Id;worker.Shift=0;worker.WeeklyDayOff=((int)_sim.State.Now.DayOfWeek+3)%7;worker.Path.Clear();worker.PathIndex=0;worker.Position=farm.Door;
        var order=typeof(CitySimulation).GetMethod("OrderLocalFood",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Require((bool)order.Invoke(_sim,[farm,target,20d])!,"R1 compra real paga");var shipment=_sim.State.Shipments.Last(s=>s.VehicleId==truck.Id);
        Require(shipment.PricePaid>0&&!shipment.PickedUp,"R1 paga antes de carregar");
        _world.Reset(_sim.State);ShowBuildingDemolition(target.Id);await RecoveryFrame();await RecoveryFrame();
        Require(RecoveryNodes<Label>(_modal!).Any(l=>l.Text.Contains("continuam reservadas ao comprador")),"R1 prévia explica reserva");
        await RecoveryCaptureOnce("building-demolition-prepaid-preview",_modal!);
        RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="✓ Confirmar demolição").EmitSignal(Godot.Button.SignalName.Pressed);await RecoveryFrame();
        Require(!shipment.Complete&&!shipment.Cancelled&&!shipment.PickedUp&&truck.ShipmentId==null&&truck.DriverId==null&&worker.BoardingVehicleId==null,"R1 reserva preservada, transporte livre");
        Require(farm.Quantity(ResourceKind.Food)==20&&shipment.GoodsOwnerAccount==buyer.Account,"R1 localização e titular preservados");
        _focus=ToV(farm.Position);_zoom=70;UpdateCamera();ClearSelection();_selectedBuilding=farm.Id;_inspectionKey="";RefreshInspection();await RecoveryFrame();
        Require(RecoveryNodes<Label>(_inspection!).Any(l=>l.Text.Contains("Compra paga reservada")&&l.Text.Contains("Mercado QA")),"R1 reserva diagnosticável na fazenda");
        await RecoveryCaptureOnce("building-demolition-prepaid-reserved",GetViewport());
        var reservationSave=System.IO.Path.Combine(directory,"building-demolition-prepaid-reserved.json");System.IO.File.WriteAllText(reservationSave,_sim.SaveToJson());
        _sim=CitySimulation.FromJson(System.IO.File.ReadAllText(reservationSave));_world.Reset(_sim.State);
        farm=_sim.State.Buildings.Single(b=>b.Id==farm.Id);sister=_sim.State.Buildings.Single(b=>b.Id==sister.Id);truck=_sim.State.Vehicles.Single(v=>v.Id==truck.Id);shipment=_sim.State.Shipments.Single(s=>s.Id==shipment.Id);worker=_sim.State.Citizens.Single(c=>c.Id==worker.Id);
        var buyerMoney=_sim.Balance(buyer.Account);var sellerMoney=_sim.Balance(farmCo.Account);var total=_sim.MonetaryTotal;
        sister.Stock[ResourceKind.Food]=0;worker.Position=farm.Door;worker.Path.Clear();worker.PathIndex=0;_sim.State.EconomyNextAt=_sim.State.ElapsedSeconds;
        var boarded=false;
        for(var i=0;i<3000&&!(shipment.PickedUp&&truck.Status==VehicleStatus.Travelling);i++){var at=truck.Position;var aboard=worker.VehicleId==truck.Id;_sim.Advance(.1);if(at.Distance(truck.Position)>.000001)Require(aboard,"R1 nenhum movimento sem SIM");boarded|=worker.VehicleId==truck.Id;if(i%30==0)await RecoveryFrame();}
        Require(boarded&&shipment.PickedUp&&shipment.DestinationBuildingId==sister.Id,"R1 mesma compra retoma com embarque e carga reais");
        Require(_sim.Balance(buyer.Account)==buyerMoney&&_sim.Balance(farmCo.Account)==sellerMoney,"R1 sem nova cobrança");
        // Exact R2 geometry: bay (-20,6.75), parked truck (-14,6.75), PI/2.
        var bay=(Point)dockAt.Invoke(_sim,[_sim.Access(sister)!,sister,false])!;
        var blocker=new Vehicle{Id=_sim.State.NextId++,External=true,ExternalDriverId="Motorista externo QA",Position=bay+new Point(6,0),Heading=Math.PI/2,Status=VehicleStatus.Parked,Fuel=20};_sim.State.Vehicles.Add(blocker);
        var overlap=typeof(CitySimulation).GetMethod("VehicleBodiesOverlap",BindingFlags.NonPublic|BindingFlags.Static)!;
        bool Touch()=>(bool)overlap.Invoke(null,[truck,truck.Position,blocker])!;
        Require(!Touch(),"R2 corpos separados antes da ordem");
        ClearSelection();_selectedBuilding=sister.Id;_inspectionKey="";RefreshInspection();ShowBuildingDemolition(sister.Id);await RecoveryFrame();
        RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="✓ Confirmar demolição").EmitSignal(Godot.Button.SignalName.Pressed);await RecoveryFrame();
        var overlaps=0;
        for(var i=0;i<6000;i++){_sim.Advance(.1);if(Touch())overlaps++;if(i%100==0)await RecoveryFrame();}
        Require(overlaps==0&&truck.Status!=VehicleStatus.Parked&&worker.VehicleId==truck.Id&&truck.Cause.Contains("ocupad"),"R2 100 ms: baia bloqueada, sem colisão/liberação do SIM");
        var pose=truck.Position;var fuel=truck.Fuel;for(var i=0;i<600;i++)_sim.Advance(.1);
        Require(truck.Position==pose&&truck.Fuel==fuel&&truck.Cargo[ResourceKind.Food]==20,"R2 pose, fuel e carga preservados");
        _focus=ToV(bay);_zoom=45;UpdateCamera();ClearSelection();_selectedVehicle=truck.Id;_inspectionKey="";RefreshInspection();RefreshHud();for(var i=0;i<4;i++)await RecoveryFrame();
        await RecoveryCaptureOnce("building-demolition-bay-blocked",GetViewport());System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"building-demolition-bay-blocked.json"),_sim.SaveToJson());
        // Physically drive the obstacle forward along the frontage, with its real driver.
        var obstacleBefore=blocker.Position;blocker.Route=[_sim.Access(sister)!.Id];blocker.RouteIndex=1;blocker.DockPosition=blocker.Position+new Point(20,0);blocker.Status=VehicleStatus.Travelling;
        for(var i=0;i<36000&&truck.Status!=VehicleStatus.Parked;i++){_sim.Advance(.1);Require(!Touch(),"R2 nenhum corpo atravessado na retomada");if(i%100==0)await RecoveryFrame();}
        Require(blocker.Position.Distance(obstacleBefore)>12&&truck.Status==VehicleStatus.Parked&&truck.DriverId==null&&worker.VehicleId==null,"R2 obstáculo saiu fisicamente; só depois estaciona e libera SIM");
        Require(truck.Cargo[ResourceKind.Food]==20&&shipment.GoodsOwnerAccount==buyer.Account&&!shipment.Complete&&_sim.MonetaryTotal==total,"R2 carga, titular e dinheiro preservados");
        _inspectionKey="";RefreshInspection();RefreshHud();for(var i=0;i<4;i++)await RecoveryFrame();await RecoveryCaptureOnce("building-demolition-bay-cleared",GetViewport());
        GD.Print("INDEXCITIES_DEMOLITION_GUARDS_OK prepaid-reserved=true prepaid-resumed=true no-second-charge=true bay-body-overlaps="+overlaps+" samples=6000 obstacle-departed-physically=true driver-released-after-parking=true");
    }
}
