using System.Reflection;
using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// SPEC construcao (2026-10-18): sem destino apto, o caminhão local com carga privada de um
// prédio demolido espera num local físico SEGURO. A baia junto ao terreno só serve se o corpo
// do caminhão cabe nela e chega até ela sem atravessar outro veículo; ocupada, ele mantém pose,
// carga, titular e motorista com causa legível e só estaciona quando o obstáculo realmente sai.
// Corpos são conferidos a cada 100 ms, não só na pose final.
public sealed class BuildingDemolitionSafeBayTests
{
    static readonly MethodInfo Overlap=typeof(CitySimulation).GetMethod("VehicleBodiesOverlap",BindingFlags.NonPublic|BindingFlags.Static)!;
    static bool Touch(Vehicle a,Vehicle b)=>(bool)Overlap.Invoke(null,[a,a.Position,b])!;

    internal sealed record Fixture(CitySimulation Sim,Building Target,Building Sister,Vehicle Truck,Shipment Shipment,Citizen Worker,Company FarmCo,Company ShopCo,Point Bay,Building Farm);

    // Caso real OrderLocalFood: fazenda com motorista SIM leva comida já vendida a uma loja;
    // a outra loja da mesma empresa está cheia, então não há destino apto após demolir.
    internal static Fixture LoadedLocalTruck(bool load=true)
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(7200);
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(placed.Success,placed.Message);var house=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        for(var i=0;i<96&&!sim.State.Families.Any(f=>f.HomeId==house.Id);i++)sim.Advance(1800);
        var worker=sim.State.Citizens.First(c=>c.Id==sim.State.Families.Single(f=>f.HomeId==house.Id).Members[0]);
        Company Add(int id){var c=new Company{Id=id,Name="Empresa "+id,Active=false};sim.State.Companies.Add(c);sim.State.Wallets[c.Account]=5000;sim.State.Wallets["global"]-=5000;return c;}
        var farmCo=Add(910);farmCo.Active=true;var shopCo=Add(911);
        var farm=new Building{Id=sim.State.NextId++,Kind=BuildingKind.Farm,Position=new(-40,-28),Rotation=Math.PI,Completed=true,OperationalCapacity=1,OwnerAccount=farmCo.Account,CompanyId=farmCo.Id,SalaryOffer=80};
        farm.Stock[ResourceKind.Food]=60;sim.State.Buildings.Add(farm);farmCo.Buildings.Add(farm.Id);
        Building Shop(double x){var b=new Building{Id=sim.State.NextId++,Kind=BuildingKind.Grocery,Position=new(x,14),Completed=true,OperationalCapacity=1,OwnerAccount=shopCo.Account,CompanyId=shopCo.Id};sim.State.Buildings.Add(b);shopCo.Buildings.Add(b.Id);return b;}
        var target=Shop(-20);var sister=Shop(60);sister.Stock[ResourceKind.Food]=sister.Definition.Capacity;
        var dockAt=typeof(CitySimulation).GetMethod("DockAt",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var truck=new Vehicle{Id=sim.State.NextId++,OwnerAccount=farmCo.Account,HomeBuildingId=farm.Id,Fuel=sim.State.Tuning.FuelTank,Status=VehicleStatus.Parked,Cause="Na fazenda"};
        truck.Position=(Point)dockAt.Invoke(sim,[sim.Access(farm)!,farm,false])!;sim.State.Vehicles.Add(truck);
        for(var i=0;i<200&&!(sim.State.Now.Hour is >=8 and <12&&sim.State.Now.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday));i++)sim.Advance(1800);
        worker.WorkplaceId=farm.Id;worker.Shift=0;worker.WeeklyDayOff=((int)sim.State.Now.DayOfWeek+3)%7;worker.Path.Clear();worker.PathIndex=0;worker.Position=farm.Door;
        var order=typeof(CitySimulation).GetMethod("OrderLocalFood",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Assert.True((bool)order.Invoke(sim,[farm,target,20d])!);
        var shipment=sim.State.Shipments.Last(s=>s.VehicleId==truck.Id);
        if(load){for(var i=0;i<3000&&!(shipment.PickedUp&&truck.Status==VehicleStatus.Travelling);i++)sim.Advance(1);
        Assert.True(shipment.PickedUp,truck.Cause);Assert.Equal(worker.Id,truck.DriverId);}
        var bay=(Point)dockAt.Invoke(sim,[sim.Access(target)!,target,false])!;
        return new(sim,target,sister,truck,shipment,worker,farmCo,shopCo,bay,farm);
    }
    // Another vehicle standing beside the bay, physically separate from the delivery truck.
    static Vehicle Park(Fixture f,Point at,bool car=false)
    {
        var other=new Vehicle{Id=f.Sim.State.NextId++,External=true,ExternalDriverId="Motorista externo QA",PassengerCar=car,Position=at,Heading=Math.PI/2,Status=VehicleStatus.Parked,Fuel=20,Cause="Parado junto à loja"};
        f.Sim.State.Vehicles.Add(other);Assert.False(Touch(f.Truck,other));return other;
    }
    // Physical departure along the frontage, with the external driver: no position edit.
    static void Leave(Fixture f,Vehicle other){other.Route=[f.Sim.Access(f.Target)!.Id];other.RouteIndex=1;other.DockPosition=other.Position+new Point(20,0);other.Status=VehicleStatus.Travelling;}
    static bool Aboard(Fixture f)=>f.Sim.State.Citizens.Any(c=>c.Id==f.Truck.DriverId&&c.VehicleId==f.Truck.Id&&c.Alive);
    // Advances in 100 ms steps, failing at the first sample where the truck body touches another vehicle.
    static void Step(Fixture f,double seconds,Func<bool>? until=null)
    {
        for(var i=0;i<seconds*10&&!(until?.Invoke()??false);i++){
            f.Sim.Advance(.1);
            foreach(var o in f.Sim.State.Vehicles.Where(o=>o.Id!=f.Truck.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(f.Truck.Position)<12))
                Assert.False(Touch(f.Truck,o),$"t={f.Sim.State.ElapsedSeconds:0.0} truck {f.Truck.Position} h={f.Truck.Heading:0.00} {f.Truck.Status} touches #{o.Id} at {o.Position}; {f.Truck.Cause}");
        }
    }

    [Fact] public void Occupied_bay_is_never_crossed_cargo_and_driver_wait_with_the_cause_and_it_parks_when_the_obstacle_really_leaves()
    {
        var f=LoadedLocalTruck();
        // Revisão R2: outro caminhão parado a 6 m do centro da baia, mesmo heading; seu corpo ocupa parte dela.
        var blocker=Park(f,f.Bay+new Point(6,0));
        var quote=f.Sim.QuoteBuildingDemolition(f.Target.Id);Assert.True(f.Sim.DemolishBuilding(f.Target.Id,quote).Success);
        var money=f.Sim.Balance(f.ShopCo.Account);var farmMoney=f.Sim.Balance(f.FarmCo.Account);var total=f.Sim.MonetaryTotal;
        Step(f,600);
        // Not parked over the other body: still with its SIM, cargo and titular, cause readable.
        Assert.NotEqual(VehicleStatus.Parked,f.Truck.Status);Assert.True(Aboard(f),f.Truck.Cause);Assert.Equal(f.Worker.Id,f.Truck.DriverId);
        Assert.Contains("ocupad",f.Truck.Cause);
        Assert.Equal(20,f.Truck.Cargo[ResourceKind.Food],6);Assert.Equal(f.ShopCo.Account,f.Shipment.GoodsOwnerAccount);Assert.False(f.Shipment.Complete);Assert.False(f.Shipment.Cancelled);
        var pose=f.Truck.Position;var fuel=f.Truck.Fuel;
        Step(f,120);
        Assert.Equal(pose,f.Truck.Position);Assert.Equal(fuel,f.Truck.Fuel,6);
        // Waiting survives save/load with pose, cargo, titular and the SIM still aboard.
        var loaded=CitySimulation.FromJson(f.Sim.SaveToJson());var held=loaded.State.Vehicles.Single(v=>v.Id==f.Truck.Id);
        Assert.Equal(pose,held.Position);Assert.Equal(f.Worker.Id,held.DriverId);Assert.Equal(20,held.Cargo[ResourceKind.Food],6);
        // The other vehicle physically drives clear of the bay.
        Leave(f,blocker);
        Step(f,3600,()=>f.Truck.Status==VehicleStatus.Parked);
        Assert.Equal(VehicleStatus.Parked,f.Truck.Status);Assert.True(f.Truck.Position.Distance(f.Bay)<.1);Assert.True(blocker.Position.Distance(f.Bay)>12);
        Assert.Null(f.Truck.DriverId);Assert.Null(f.Worker.VehicleId);Assert.Contains("estacionada junto ao terreno",f.Truck.Cause);
        Assert.Equal(20,f.Truck.Cargo[ResourceKind.Food],6);Assert.Equal(f.ShopCo.Account,f.Shipment.GoodsOwnerAccount);Assert.False(f.Shipment.Complete);
        Assert.Equal(money,f.Sim.Balance(f.ShopCo.Account));Assert.Equal(farmMoney,f.Sim.Balance(f.FarmCo.Account),0);Assert.Equal(total,f.Sim.MonetaryTotal);f.Sim.CheckInvariants();
    }

    // Same 6.5 m between centres: a parked truck reaches into the bay, a shorter car does not.
    [Theory]
    [InlineData(false,false)]
    [InlineData(true,true)]
    public void The_bay_check_uses_the_real_bodies_not_a_centre_distance(bool car,bool parks)
    {
        var f=LoadedLocalTruck();
        Park(f,f.Bay+new Point(6.5,0),car);
        Assert.True(f.Sim.DemolishBuilding(f.Target.Id,f.Sim.QuoteBuildingDemolition(f.Target.Id)).Success);
        Step(f,600,()=>f.Truck.Status==VehicleStatus.Parked);
        Assert.Equal(parks,f.Truck.Status==VehicleStatus.Parked);
        Assert.Equal(parks,f.Truck.DriverId==null);Assert.Equal(20,f.Truck.Cargo[ResourceKind.Food],6);
    }

    // While it waits for the occupied bay, real room at the same company's other shop resumes the delivery.
    [Fact] public void A_truck_waiting_for_an_occupied_bay_still_delivers_when_an_apt_destination_appears()
    {
        var f=LoadedLocalTruck();
        Park(f,f.Bay+new Point(6,0));
        Assert.True(f.Sim.DemolishBuilding(f.Target.Id,f.Sim.QuoteBuildingDemolition(f.Target.Id)).Success);
        Step(f,300);Assert.Contains("ocupad",f.Truck.Cause);
        var money=f.Sim.Balance(f.ShopCo.Account);var total=f.Sim.MonetaryTotal;
        f.Sister.Stock[ResourceKind.Food]=0;
        Step(f,f.Sim.State.Tuning.EconomyInterval+1200,()=>f.Shipment.Complete);
        Assert.True(f.Shipment.Complete,f.Truck.Cause);Assert.Equal(20,f.Sister.Quantity(ResourceKind.Food),6);
        Assert.Equal(money,f.Sim.Balance(f.ShopCo.Account));Assert.Equal(total,f.Sim.MonetaryTotal);f.Sim.CheckInvariants();
    }

    // The obstacle shows up only after the truck has already set off for the bay.
    [Fact] public void A_vehicle_that_stops_beside_the_bay_during_the_trip_is_not_crossed_either()
    {
        var f=LoadedLocalTruck();
        Assert.True(f.Sim.DemolishBuilding(f.Target.Id,f.Sim.QuoteBuildingDemolition(f.Target.Id)).Success);
        Step(f,2);Assert.Equal(VehicleStatus.Travelling,f.Truck.Status);
        var blocker=Park(f,f.Bay+new Point(6,0));
        Step(f,600);
        Assert.NotEqual(VehicleStatus.Parked,f.Truck.Status);Assert.True(Aboard(f),f.Truck.Cause);Assert.Contains("ocupad",f.Truck.Cause);
        Leave(f,blocker);
        Step(f,3600,()=>f.Truck.Status==VehicleStatus.Parked);
        Assert.Equal(VehicleStatus.Parked,f.Truck.Status);Assert.Null(f.Truck.DriverId);Assert.Equal(20,f.Truck.Cargo[ResourceKind.Food],6);
    }

    [Fact] public void A_free_final_bay_does_not_allow_crossing_a_body_on_the_approach()
    {
        var f=LoadedLocalTruck();
        var blocker=Park(f,f.Bay+new Point(6,-3.2));
        var atBay=new Vehicle{Position=f.Bay,Heading=Math.PI/2};
        Assert.False(Touch(atBay,blocker));
        Assert.True(f.Sim.DemolishBuilding(f.Target.Id).Success);
        Step(f,600);
        Assert.NotEqual(VehicleStatus.Parked,f.Truck.Status);Assert.True(Aboard(f));Assert.Contains("ocupad",f.Truck.Cause);
        Leave(f,blocker);Step(f,3600,()=>f.Truck.Status==VehicleStatus.Parked);
        Assert.Equal(VehicleStatus.Parked,f.Truck.Status);Assert.Null(f.Truck.DriverId);
    }
}
