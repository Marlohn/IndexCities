using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC transporte-circulacao, "Quebras, guincho local e oficina".
public sealed class BreakdownTests
{
    sealed record Scene(CitySimulation Sim,Vehicle Car,Citizen Driver,Building House,Building? Shop,Company? ShopOwner,Vehicle? LocalTow,Citizen? Mechanic)
    {
        public CityState S=>Sim.State;
    }
    // Street z=0 from the highway to x=60 with a northern ring (z=40). The car's
    // home faces the street at x=-10 (south side); the shop faces it at x=-30
    // (north side). The car drives east to the grocery at x=40.
    static Scene City(bool shop=true,bool localTow=true,decimal ownerMoney=5000,bool breakNow=true,int mechanics=1,int hour=8)
    {
        var s=new CitySimulation(42).State;int Next()=>s.NextId++;
        void Road(double x,double z)=>s.Roads.Add(new(){Id=Next(),Position=new(x,z)});
        for(var x=-188;x<=60;x+=4)Road(x,0);for(var x=-60;x<=60;x+=4)Road(x,40);for(var z=4;z<=36;z+=4){Road(-60,z);Road(60,z);}
        void Fund(string account,decimal amount){s.Wallets["global"]-=amount;s.Wallets[account]=s.Wallets.GetValueOrDefault(account)+amount;}
        Company Owner(string name){var c=new Company{Id=Next(),Name=name};s.Companies.Add(c);Fund(c.Account,2000);return c;}
        Citizen Person(string name,Point at,int? family=null){var f=family??Next();var c=new Citizen{Id=Next(),Name=name,FamilyId=f,Age=30,Resident=true,InsideMap=true,Position=at,WeeklyDayOff=0,Shift=0};s.Citizens.Add(c);s.Wallets[c.Account]=0;if(family==null)s.Families.Add(new(){Id=f,Members=[c.Id]});return c;}
        var market=Owner("Mercado teste");var grocery=new Building{Id=Next(),Kind=BuildingKind.Grocery,Position=new(40,-16),Rotation=Math.PI,Completed=true,CompanyId=market.Id,OwnerAccount=market.Account};s.Buildings.Add(grocery);market.Buildings.Add(grocery.Id);
        var driver=Person("Ana",new(0,-1.65));Fund(driver.Account,ownerMoney);
        var house=new Building{Id=Next(),Kind=BuildingKind.House,Position=new(-10,-14),Rotation=Math.PI,Completed=true,OwnerAccount=driver.Account,FamilyId=driver.FamilyId};s.Buildings.Add(house);s.Families.Single(f=>f.Id==driver.FamilyId).HomeId=house.Id;
        Building? garage=null;Company? owner=null;Vehicle? tow=null;Citizen? mechanic=null;
        if(shop){
            owner=Owner("Oficina teste");garage=new Building{Id=Next(),Kind=BuildingKind.MechanicShop,Position=new(-30,15),Completed=true,CompanyId=owner.Id,OwnerAccount=owner.Account,SalePrice=120};s.Buildings.Add(garage);owner.Buildings.Add(garage.Id);
            for(var i=0;i<mechanics;i++){var m=Person("Mecânico "+i,garage.Door);m.WorkplaceId=garage.Id;m.Shift=hour>=14?1:0;m.Activity="No trabalho";mechanic??=m;}
            if(localTow){tow=new Vehicle{Id=Next(),Body=TruckBody.Tow,OwnerAccount=owner.Account,HomeBuildingId=garage.Id,CargoCapacity=0,Position=new(-30+5.5,15-6.1),Heading=Math.PI/2,Fuel=40,Status=VehicleStatus.Parked,Cause="Guincho da oficina disponível"};s.Vehicles.Add(tow);}
        }
        var east=s.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-20&&r.Position.X<=40).OrderBy(r=>r.Position.X).Select(r=>r.Id).ToList();
        var car=new Vehicle{Id=Next(),OwnerAccount=driver.Account,PassengerCar=true,CargoCapacity=0,Position=new(0,-1.65),Heading=Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=east,RouteIndex=6,DestinationBuildingId=grocery.Id,ParkingSpot="b:"+grocery.Id+":0",TripPurpose=JourneyPurpose.Shopping,DriverId=driver.Id,Cause="Em viagem",Condition=.2};
        s.Vehicles.Add(car);driver.VehicleId=car.Id;driver.Purpose=JourneyPurpose.Shopping;driver.DestinationId=grocery.Id;driver.Activity="Dirigindo carro particular";
        if(breakNow){s.Tuning.BreakdownChancePerKmNew=1e7;s.Tuning.BreakdownChancePerKmWorn=1e7;}
        s.Now=new(2026,1,1,hour,0,0);s.EconomyNextAt=1e9;
        return new(new CitySimulation(s),car,driver,house,garage,owner,tow,mechanic);
    }
    // After the first breakdown nothing else breaks (tows, the returning car).
    static void BreakOnce(Scene scene){scene.Sim.Advance(2);Assert.True(scene.Car.BrokenDown,scene.Car.Cause);scene.S.Tuning.BreakdownChancePerKmNew=0;scene.S.Tuning.BreakdownChancePerKmWorn=0;}
    static bool Run(Scene scene,double seconds,Func<bool> until){for(var t=0.0;t<seconds;t+=2){scene.Sim.Advance(2);if(until())return true;}return false;}
    static bool Home(Scene scene)=>scene.Car.Status==VehicleStatus.Parked&&scene.Car.ServicePhase==ServicePhase.None&&scene.Car.PushPath.Count==0&&scene.Car.TowTruckId==null&&scene.Car.Position.Distance(new(-6.6,-7.7))<.1;

    [Fact] public void Condition_falls_with_distance_and_a_new_vehicle_rarely_breaks()
    {
        var scene=City(shop:false,breakNow:false);scene.Car.Condition=1;scene.S.Tuning.BreakdownChancePerKmNew=0;scene.S.Tuning.BreakdownChancePerKmWorn=0;
        Run(scene,20,()=>false);
        Assert.True(scene.Car.Condition<1);Assert.False(scene.Car.BrokenDown);
        // Parked vehicles do not wear or break.
        var parked=City(shop:false,breakNow:true);parked.Car.Status=VehicleStatus.Parked;parked.Car.DriverId=null;parked.Driver.VehicleId=null;var before=parked.Car.ConditionUnits;
        Run(parked,20,()=>false);Assert.False(parked.Car.BrokenDown);Assert.Equal(before,parked.Car.ConditionUnits);
    }
    [Fact] public void A_broken_car_stops_its_driver_walks_on_and_the_local_tow_takes_it_to_the_shop_repairs_and_returns_it()
    {
        var scene=City();var before=scene.Sim.Balance(scene.Driver.Account);
        BreakOnce(scene);
        Assert.Equal(VehicleStatus.Stranded,scene.Car.Status);Assert.Null(scene.Driver.VehicleId);Assert.True(scene.Driver.Moving,"a motorista segue a pé");
        Assert.True(Run(scene,30,()=>scene.Car.TowTruckId!=null));Assert.Equal(scene.LocalTow!.Id,scene.Car.TowTruckId);Assert.Equal(scene.Shop!.Id,scene.Car.ServiceShopId);
        var towFee=scene.S.Ledger.Single(m=>m.Category=="Guincho");Assert.Equal(scene.Driver.Account,towFee.From);Assert.Equal(scene.ShopOwner!.Account,towFee.To);
        Assert.True(Run(scene,1800,()=>scene.Car.ServicePhase==ServicePhase.InService&&scene.Car.PushPath.Count==0),scene.Car.Cause+" / tow: "+scene.LocalTow.Cause);
        Assert.NotNull(scene.Car.ServiceBay);Assert.Contains(scene.Car.Id,scene.Sim.ServiceBays(scene.Shop).OfType<int>());Assert.True(scene.Car.BrokenDown);
        var repair=scene.S.Ledger.Single(m=>m.Category=="Conserto de veículo");Assert.Equal(scene.Driver.Account,repair.From);Assert.Equal(scene.ShopOwner.Account,repair.To);
        Assert.True(Run(scene,3*3600,()=>Home(scene)),scene.Car.Cause+" | "+string.Join(" ; ",scene.S.Vehicles.Where(v=>v.Body is TruckBody.Tow or TruckBody.TowHeavy).Select(v=>$"{v.Id} {v.Status} {v.TowPhase} {v.Position} driver={v.DriverId} «{v.Cause}»"))+" mech="+scene.Mechanic!.Activity+" "+scene.Mechanic.Position);
        Assert.True(scene.Car.Condition>.99);Assert.False(scene.Car.BrokenDown);Assert.Null(scene.Car.ServiceShopId);
        Assert.Equal(2,scene.S.Ledger.Count(m=>m.Category=="Guincho"&&m.To==scene.ShopOwner.Account));
        Assert.Equal(before-scene.S.Ledger.Where(m=>m.From==scene.Driver.Account).Sum(m=>m.Amount),scene.Sim.Balance(scene.Driver.Account));
        Assert.DoesNotContain(scene.S.Obligations,o=>o.Outstanding>0);Assert.Equal(scene.S.Tuning.TotalMoney,scene.Sim.MonetaryTotal);scene.Sim.CheckInvariants();
    }
    [Fact] public void Without_a_shop_the_external_tow_takes_the_car_to_the_exterior_for_repair_and_back()
    {
        var scene=City(shop:false);BreakOnce(scene);
        Assert.True(Run(scene,1800,()=>scene.Car.ServicePhase==ServicePhase.AtExterior),scene.Car.Cause);
        Assert.Equal(VehicleStatus.Outside,scene.Car.Status);
        var repair=scene.S.Ledger.Single(m=>m.Category=="Conserto de veículo");Assert.Equal("global",repair.To);
        Assert.True(Run(scene,6*3600,()=>Home(scene)),scene.Car.Cause);
        Assert.Equal(1,scene.Car.Condition,6);Assert.Equal(2,scene.S.Ledger.Count(m=>m.Category=="Guincho"&&m.To=="global"));scene.Sim.CheckInvariants();
    }
    [Fact] public void A_full_shop_sends_the_broken_car_to_the_exterior()
    {
        var scene=City(localTow:false);
        for(var i=0;i<4;i++)scene.S.Vehicles.Add(new Vehicle{Id=scene.S.NextId++,PassengerCar=true,OwnerAccount="global",External=true,Status=VehicleStatus.Outside,Position=scene.S.Entry,ServiceShopId=scene.Shop!.Id,ServicePhase=ServicePhase.ToService,Service=ServiceKind.Revision});
        BreakOnce(scene);
        Assert.True(Run(scene,60,()=>scene.Car.TowTruckId!=null));
        var tow=scene.S.Vehicles.Single(v=>v.Id==scene.Car.TowTruckId);Assert.True(tow.External);Assert.Equal(TowDestination.Exterior,tow.TowDestination);Assert.Null(scene.Car.ServiceShopId);
    }
    [Fact] public void An_owner_without_money_gets_the_tow_paid_by_the_city_and_the_car_left_broken_at_home_until_there_is_money()
    {
        var scene=City(shop:false,ownerMoney:0);var cityBefore=scene.Sim.CityBalance;BreakOnce(scene);
        Assert.True(Run(scene,1800,()=>scene.Car.ServicePhase==ServicePhase.AwaitingMoney&&scene.Car.PushPath.Count==0),scene.Car.Cause);
        Assert.True(scene.Car.BrokenDown);Assert.Equal(VehicleStatus.Parked,scene.Car.Status);Assert.True(scene.Car.Position.Distance(new(-6.6,-7.7))<.1,scene.Car.Position.ToString());
        Assert.DoesNotContain(scene.S.Ledger,m=>m.Category=="Conserto de veículo");
        var paid=scene.S.Ledger.Single(m=>m.Category=="Guincho");Assert.Equal("city",paid.From);Assert.Equal(cityBefore-paid.Amount,scene.Sim.CityBalance);
        Assert.Contains(scene.S.Obligations,o=>o.Kind=="Guincho"&&o.Debtor==scene.Driver.Account&&o.Creditor=="city");
        // Not usable meanwhile; with money the tow is called again for the repair.
        Run(scene,600,()=>false);Assert.Equal(ServicePhase.AwaitingMoney,scene.Car.ServicePhase);Assert.Null(scene.Car.TowTruckId);
        scene.S.Wallets["global"]-=5000;scene.S.Wallets[scene.Driver.Account]+=5000;
        Assert.True(Run(scene,600,()=>scene.Car.TowTruckId!=null),scene.Car.Cause);
        Assert.True(Run(scene,8*3600,()=>Home(scene)&&!scene.Car.BrokenDown),scene.Car.Cause);
        Assert.Contains(scene.S.Ledger,m=>m.Category=="Conserto de veículo"&&m.From==scene.Driver.Account);scene.Sim.CheckInvariants();
    }
    [Fact] public void Without_a_mechanic_present_the_repair_does_not_advance()
    {
        var scene=City(localTow:false,mechanics:0);BreakOnce(scene);
        Assert.True(Run(scene,1800,()=>scene.Car.ServicePhase==ServicePhase.Queued&&scene.Car.PushPath.Count==0),scene.Car.Cause);
        Run(scene,1200,()=>false);Assert.Equal(ServicePhase.Queued,scene.Car.ServicePhase);Assert.Contains("aguardando mecânico",scene.Car.Cause);Assert.True(scene.Car.BrokenDown);
    }
    [Fact] public void A_broken_external_truck_goes_to_the_exterior_with_its_crew_and_comes_back_repaired()
    {
        var scene=City(localTow:false,breakNow:false);var s=scene.S;scene.Car.Status=VehicleStatus.Parked;scene.Car.DriverId=null;scene.Driver.VehicleId=null;scene.Car.Position=new(-6.6,-7.7);
        var truck=s.Vehicles.First(v=>v.External&&v.Body==TruckBody.Cargo);
        var home=s.Roads.Where(r=>r.Position.Z==40&&r.Position.X<=40).OrderByDescending(r=>r.Position.X).Concat(s.Roads.Where(r=>r.Position.X==-60&&r.Position.Z is >0 and <40).OrderByDescending(r=>r.Position.Z)).Concat(s.Roads.Where(r=>r.Position.Z==0&&r.Position.X<=-60).OrderByDescending(r=>r.Position.X)).Select(r=>r.Id).ToList();
        truck.Position=new(0,41.65);truck.Heading=-Math.PI/2;truck.Status=VehicleStatus.Travelling;truck.Route=home;truck.RouteIndex=11;truck.Fuel=40;truck.Condition=.1;truck.Cause="Retorno ao exterior";
        s.Tuning.BreakdownChancePerKmNew=1e7;s.Tuning.BreakdownChancePerKmWorn=1e7;scene.Sim.Advance(2);Assert.True(truck.BrokenDown);s.Tuning.BreakdownChancePerKmNew=0;s.Tuning.BreakdownChancePerKmWorn=0;
        Assert.True(Run(scene,60,()=>truck.TowTruckId!=null));Assert.Equal(TruckBody.TowHeavy,s.Vehicles.Single(v=>v.Id==truck.TowTruckId).Body);Assert.Null(truck.ServiceShopId);
        Assert.True(Run(scene,1800,()=>truck.ServicePhase==ServicePhase.AtExterior),truck.Cause);
        Assert.True(Run(scene,5*3600,()=>truck.ServicePhase==ServicePhase.None),truck.Cause);
        Assert.Equal(VehicleStatus.Outside,truck.Status);Assert.Equal(1,truck.Condition,6);Assert.False(truck.BrokenDown);scene.Sim.CheckInvariants();
    }
    [Fact] public void A_worn_car_goes_to_revision_with_its_owner_who_waits_and_drives_it_home()
    {
        var scene=City(localTow:false,breakNow:false,hour:18);var s=scene.S;var car=scene.Car;var ana=scene.Driver;
        car.Status=VehicleStatus.Parked;car.DriverId=null;car.Route.Clear();car.Position=new(-6.6,-7.7);car.Heading=Math.PI/2;car.ParkingSpot="b:"+scene.House.Id+":0";car.Condition=.3;
        ana.VehicleId=null;ana.OutingUntil=-1e9;ana.WorkplaceId=s.Buildings.Single(b=>b.Kind==BuildingKind.Grocery).Id;ana.Shift=0;ana.Position=scene.House.Door;ana.Purpose=JourneyPurpose.Home;ana.Activity="Em casa";
        s.Tuning.BreakdownChancePerKmNew=0;s.Tuning.BreakdownChancePerKmWorn=0;s.EconomyNextAt=0;
        scene.Sim.Advance(2);Assert.True(car.ServicePhase==ServicePhase.ToService,car.Cause+" | "+ana.Activity+" "+ana.Decision+" "+ana.Moving);Assert.Equal(scene.Shop!.Id,car.ServiceShopId);s.EconomyNextAt=1e9;
        Assert.True(Run(scene,1800,()=>car.ServicePhase==ServicePhase.InService),car.Cause);
        Assert.True(ana.Position.Distance(scene.Shop.Door)<3,"espera na porta: "+ana.Activity);
        Assert.True(Run(scene,3*3600,()=>car.Status==VehicleStatus.Parked&&car.ServicePhase==ServicePhase.None&&car.Position.Distance(new(-6.6,-7.7))<.1&&ana.VehicleId==null),car.Cause+" / "+ana.Activity);
        Assert.True(car.Condition>.99,car.Condition.ToString());var paid=s.Ledger.Single(m=>m.Category=="Revisão de veículo");Assert.Equal(ana.Account,paid.From);Assert.Equal(scene.ShopOwner!.Account,paid.To);scene.Sim.CheckInvariants();
    }
    [Fact] public void Breakdowns_and_service_are_saved_and_reproducible()
    {
        var scene=City();BreakOnce(scene);string? mid=null;
        Run(scene,1800,()=>{if(scene.Car.ServicePhase==ServicePhase.InService){mid=scene.Sim.SaveToJson();return true;}return false;});
        Assert.NotNull(mid);var a=CitySimulation.FromJson(mid!);var b=CitySimulation.FromJson(mid!);
        Assert.Equal(ServicePhase.InService,a.State.Vehicles.Single(v=>v.Id==scene.Car.Id).ServicePhase);Assert.True(a.State.Vehicles.Single(v=>v.Id==scene.Car.Id).BrokenDown);
        for(var i=0;i<300;i++){a.Advance(2);b.Advance(2);}Assert.Equal(a.SaveToJson(),b.SaveToJson());
        // An older save without vehicle state loads with full condition.
        var old=System.Text.Json.Nodes.JsonNode.Parse(new CitySimulation(7).SaveToJson())!.AsObject();old.Remove("VehicleStateVersion");foreach(var v in old["Vehicles"]!.AsArray())v!.AsObject().Remove("ConditionUnits");
        var loaded=CitySimulation.FromJson(old.ToJsonString());Assert.Equal(1,loaded.State.VehicleStateVersion);Assert.All(loaded.State.Vehicles,v=>Assert.Equal(1,v.Condition,6));
    }
}
