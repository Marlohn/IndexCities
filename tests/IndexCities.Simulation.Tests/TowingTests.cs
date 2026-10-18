using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC transporte-circulacao, "Socorro a veículos parados".
public sealed class TowingTests
{
    sealed record Scene(CitySimulation Sim,Vehicle Car,Citizen Driver,Building? Station,Building Shop,Company? StationOwner)
    {
        public Vehicle? Tow=>Sim.State.Vehicles.FirstOrDefault(v=>v.Id==Car.TowTruckId)??Sim.State.Vehicles.Where(v=>v.Body is TruckBody.Tow or TruckBody.TowHeavy).OrderByDescending(v=>v.Id).FirstOrDefault();
    }
    // Street z=0 from the highway to x=60 (eastbound lane z=-1.65). With the ring,
    // a northern street (z=40) joined at x=-60 and x=60 lets the tow reach the
    // stranded car's front from ahead. Station door faces the street at x=30.
    static Scene City(bool ring=true,bool station=true,decimal ownerMoney=5000,bool connected=true,SimulationTuning? tuning=null,decimal? cityMoney=null)
    {
        var s=new CitySimulation(42,tuning).State;int Next()=>s.NextId++;
        void Road(double x,double z)=>s.Roads.Add(new(){Id=Next(),Position=new(x,z)});
        for(var x=connected?-188:-120;x<=60;x+=4)Road(x,0);
        if(ring){for(var x=-60;x<=60;x+=4)Road(x,40);for(var z=4;z<=36;z+=4){Road(-60,z);Road(60,z);}}
        void Fund(string account,decimal amount){s.Wallets["global"]-=amount;s.Wallets[account]=s.Wallets.GetValueOrDefault(account)+amount;}
        if(cityMoney is {} city){s.Wallets["global"]+=s.Wallets["city"]-city;s.Wallets["city"]=city;}
        Company Owner(string name){var c=new Company{Id=Next(),Name=name};s.Companies.Add(c);Fund(c.Account,1000);return c;}
        Citizen Person(string name,int family,Point at){var c=new Citizen{Id=Next(),Name=name,FamilyId=family,Age=30,Resident=true,InsideMap=true,Position=at,WeeklyDayOff=0,Shift=0};s.Citizens.Add(c);s.Wallets[c.Account]=0;s.Families.Add(new(){Id=family,Members=[c.Id]});return c;}
        Company? stationOwner=null;Building? fuel=null;
        if(station){
            stationOwner=Owner("Rota teste");fuel=new Building{Id=Next(),Kind=BuildingKind.FuelStation,Position=new(30,19),Completed=true,CompanyId=stationOwner.Id,OwnerAccount=stationOwner.Account,SalePrice=7};fuel.Stock[ResourceKind.Fuel]=300;s.Buildings.Add(fuel);stationOwner.Buildings.Add(fuel.Id);
            var attendant=Person("Frentista",Next(),fuel.Door);attendant.WorkplaceId=fuel.Id;attendant.Activity="No trabalho";
        }
        var shopOwner=Owner("Mercado teste");var shop=new Building{Id=Next(),Kind=BuildingKind.Grocery,Position=new(40,-16),Rotation=Math.PI,Completed=true,CompanyId=shopOwner.Id,OwnerAccount=shopOwner.Account};s.Buildings.Add(shop);shopOwner.Buildings.Add(shop.Id);
        var driver=Person("Ana",Next(),new(0,-1.65));Fund(driver.Account,ownerMoney);
        var east=s.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-20&&r.Position.X<=40).OrderBy(r=>r.Position.X).Select(r=>r.Id).ToList();
        var car=new Vehicle{Id=Next(),OwnerAccount=driver.Account,PassengerCar=true,CargoCapacity=0,Position=new(0,-1.65),Heading=Math.PI/2,Fuel=0,Status=VehicleStatus.Travelling,Route=east,RouteIndex=6,DestinationBuildingId=shop.Id,ParkingSpot="b:"+shop.Id+":0",TripPurpose=JourneyPurpose.Shopping,DriverId=driver.Id,Cause="Em viagem"};
        s.Vehicles.Add(car);driver.VehicleId=car.Id;driver.Purpose=JourneyPurpose.Shopping;driver.DestinationId=shop.Id;driver.Activity="Dirigindo carro particular";
        s.Now=new(2026,1,1,8,0,0);s.EconomyNextAt=1e9;
        var sim=new CitySimulation(s);
        return new(sim,car,driver,fuel,shop,stationOwner);
    }
    static void Run(Scene scene,double seconds,Action? each=null){for(var t=0.0;t<seconds;t+=2){scene.Sim.Advance(2);each?.Invoke();}}

    [Fact] public void A_car_out_of_fuel_calls_a_tow_at_once_and_the_owner_pays_it_to_the_global_reserve()
    {
        var scene=City();var before=scene.Sim.Balance(scene.Driver.Account);
        Run(scene,4);
        Assert.Equal(VehicleStatus.Stranded,scene.Car.Status);var tow=scene.Tow;Assert.NotNull(tow);
        Assert.Equal(TruckBody.Tow,tow!.Body);Assert.Equal(tow.Id,scene.Car.TowTruckId);Assert.Equal(VehicleStatus.Travelling,tow.Status);Assert.Equal(TowPhase.ToVehicle,tow.TowPhase);
        Assert.Equal(scene.Sim.State.Tuning.FuelTank,tow.Fuel,3);Assert.Equal(scene.Car.Id,scene.Driver.VehicleId);Assert.Equal("Aguardando guincho",scene.Driver.Activity);
        var fee=scene.Sim.State.Ledger.Single(m=>m.Category=="Guincho");Assert.Equal(scene.Driver.Account,fee.From);Assert.Equal("global",fee.To);Assert.Equal(before-fee.Amount,scene.Sim.Balance(scene.Driver.Account));
        Assert.DoesNotContain(scene.Sim.State.Obligations,o=>o.Kind=="Guincho");Assert.Equal(scene.Sim.State.Tuning.TotalMoney,scene.Sim.MonetaryTotal);scene.Sim.CheckInvariants();
    }
    [Fact] public void The_tow_stops_in_front_carries_the_car_and_its_people_to_the_station_where_the_owner_refuels_and_continues()
    {
        var scene=City();var car=scene.Car;var sawFront=false;var sawCarried=false;string? savedMidTow=null;double minFollowerGap=double.MaxValue;
        Run(scene,1500,()=>{
            var tow=scene.Tow;if(tow==null)return;
            if(tow.TowPhase==TowPhase.Loading){sawFront=true;Assert.True(tow.Position.X>car.Position.X+5,$"{tow.Position} {car.Position}");Assert.True(Math.Abs(tow.Position.Z+1.65)<.3,tow.Position.ToString());Assert.True(Math.Cos(tow.Heading-car.Heading)>.99);}
            if(car.Status==VehicleStatus.Towed){sawCarried=true;Assert.Equal(tow.Position,car.Position);Assert.Equal(car.Id,scene.Driver.VehicleId);savedMidTow??=scene.Sim.SaveToJson();}
        });
        Assert.True(sawFront);Assert.True(sawCarried);
        Assert.True(car.Fuel>1,car.Cause+" "+car.Status);var fuel=scene.Sim.State.Ledger.Single(m=>m.Category=="Combustível");Assert.Equal(scene.Driver.Account,fuel.From);Assert.Equal(scene.StationOwner!.Account,fuel.To);
        Assert.Equal(VehicleStatus.Parked,car.Status);Assert.Equal("Estacionado",car.Cause);Assert.Null(scene.Driver.VehicleId);Assert.Null(car.TowTruckId);
        Assert.Equal(VehicleStatus.Outside,scene.Tow!.Status);Assert.Equal(TowPhase.None,scene.Tow.TowPhase);
        Assert.Equal(scene.Sim.State.Tuning.TotalMoney,scene.Sim.MonetaryTotal);scene.Sim.CheckInvariants();
        // Save/load preserves the towing state: the restored city continues identically.
        var a=CitySimulation.FromJson(savedMidTow!);var b=CitySimulation.FromJson(savedMidTow!);
        Assert.Equal(VehicleStatus.Towed,a.State.Vehicles.Single(v=>v.Id==car.Id).Status);
        for(var i=0;i<60;i++){a.Advance(2);b.Advance(2);}Assert.Equal(a.SaveToJson(),b.SaveToJson());
    }
    [Fact] public void From_behind_a_queue_the_tow_reaches_the_free_front_through_the_empty_opposite_lane()
    {
        var scene=City(ring:false);var sawOpposite=false;var sawFront=false;
        Run(scene,900,()=>{var tow=scene.Tow;if(tow==null)return;if(tow.TowPhase==TowPhase.ToVehicle&&tow.Position.X>-10&&tow.Position.Z>0)sawOpposite=true;if(tow.TowPhase==TowPhase.Loading){sawFront=true;Assert.True(tow.Position.X>scene.Car.Position.X+5);Assert.True(Math.Abs(tow.Position.Z+1.65)<.3);}});
        Assert.True(sawOpposite,scene.Tow?.Cause);Assert.True(sawFront,scene.Tow?.Cause);Assert.NotEqual(VehicleStatus.Stranded,scene.Car.Status);scene.Sim.CheckInvariants();
    }
    [Fact] public void The_tow_waits_while_an_oncoming_vehicle_uses_the_opposite_lane()
    {
        var scene=City(ring:false);var sim=scene.Sim;Run(scene,4);
        // Oncoming car approaching on the opposite lane, as the overtaking check requires.
        var west=sim.State.Roads.Where(r=>r.Position.Z==0&&r.Position.X<=60&&r.Position.X>=-180).OrderByDescending(r=>r.Position.X).Select(r=>r.Id).ToList();
        var oncoming=new Vehicle{Id=sim.State.NextId++,OwnerAccount="global",PassengerCar=true,External=true,Position=new(58,1.65),Heading=-Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=west,RouteIndex=1};sim.State.Vehicles.Add(oncoming);
        Run(scene,600,()=>{var tow=scene.Tow!;if(tow.Detour.Count>0&&Math.Abs(tow.Position.Z-oncoming.Position.Z)<1.5)Assert.True(tow.Position.Distance(oncoming.Position)>8,$"{tow.Position} {oncoming.Position}");});
        Assert.NotEqual(VehicleStatus.Stranded,scene.Car.Status);sim.CheckInvariants();
    }
    [Fact] public void An_owner_without_money_gets_the_tow_paid_by_the_city_owes_it_and_the_car_stays_parked_at_the_station()
    {
        var scene=City(ownerMoney:0);var cityBefore=scene.Sim.CityBalance;
        Run(scene,1500);
        var paid=scene.Sim.State.Ledger.Single(m=>m.Category=="Guincho");Assert.Equal("city",paid.From);Assert.Equal("global",paid.To);Assert.Equal(cityBefore-paid.Amount,scene.Sim.CityBalance);
        var debt=scene.Sim.State.Obligations.Single(o=>o.Kind=="Guincho");Assert.Equal(scene.Driver.Account,debt.Debtor);Assert.Equal("city",debt.Creditor);Assert.Equal(paid.Amount,debt.Outstanding);
        Assert.DoesNotContain(scene.Sim.State.Obligations,o=>o.Debtor=="city");
        Assert.Equal(VehicleStatus.Parked,scene.Car.Status);Assert.Equal("Carro parado no posto: sem dinheiro para abastecer",scene.Car.Cause);Assert.Null(scene.Driver.VehicleId);
        Assert.True(scene.Car.Position.Distance(scene.Station!.Door)<scene.Car.Position.Distance(new Point(0,0)));Assert.True(Math.Abs(scene.Car.Position.Z)>5.25,"fora da rua: "+scene.Car.Position);
        Assert.Equal(scene.Sim.State.Tuning.TotalMoney,scene.Sim.MonetaryTotal);scene.Sim.CheckInvariants();
    }
    [Fact] public void When_the_city_cash_cannot_cover_it_the_tow_still_comes_and_the_city_owes_the_global_reserve()
    {
        var scene=City(ownerMoney:10,cityMoney:0);Run(scene,4);
        var tow=scene.Tow;Assert.NotNull(tow);Assert.Equal(TowPhase.ToVehicle,tow!.TowPhase);
        var fee=scene.Sim.State.Ledger.Single(m=>m.Category=="Guincho"&&m.From==scene.Driver.Account);Assert.Equal(10,fee.Amount);
        var owner=scene.Sim.State.Obligations.Single(o=>o.Kind=="Guincho"&&o.Debtor==scene.Driver.Account);var city=scene.Sim.State.Obligations.Single(o=>o.Kind=="Guincho"&&o.Debtor=="city");
        Assert.Equal("city",owner.Creditor);Assert.Equal("global",city.Creditor);Assert.Equal(owner.Outstanding,city.Outstanding);Assert.True(owner.Outstanding>0);
        Assert.Equal(scene.Sim.State.Tuning.TotalMoney,scene.Sim.MonetaryTotal);scene.Sim.CheckInvariants();
    }
    [Fact] public void An_external_truck_is_pulled_by_a_heavy_tow_occupying_the_summed_length_and_leaves_after_refuelling()
    {
        var scene=City();var sim=scene.Sim;var car=scene.Car;
        // The car is not part of this case: it waits parked at the shop.
        car.Status=VehicleStatus.Parked;car.Fuel=40;car.Position=new(46.6,-12.5);scene.Driver.VehicleId=null;car.DriverId=null;
        var truck=sim.State.Vehicles.First(v=>v.External&&!v.PassengerCar&&v.Status==VehicleStatus.Outside&&v.Body==TruckBody.Cargo);
        // Returning to the exterior: westbound lane (z=+1.65), the station behind it.
        var west=sim.State.Roads.Where(r=>r.Position.Z==0&&r.Position.X<=40).OrderByDescending(r=>r.Position.X).Select(r=>r.Id).ToList();
        truck.Position=new(20,1.65);truck.Heading=-Math.PI/2;truck.Fuel=0;truck.Status=VehicleStatus.Travelling;truck.Route=west;truck.RouteIndex=6;
        var follower=new Vehicle{Id=sim.State.NextId++,OwnerAccount="global",PassengerCar=true,External=true,Position=new(60,1.65),Heading=-Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=sim.State.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-64&&r.Position.X<=64).OrderByDescending(r=>r.Position.X).Select(r=>r.Id).ToList(),RouteIndex=1};
        var sawTrail=false;
        for(var t=0;t<1800;t+=2){
            sim.Advance(2);var tow=sim.State.Vehicles.FirstOrDefault(v=>v.Body==TruckBody.TowHeavy);
            if(t==20)sim.State.Vehicles.Add(follower);
            if(tow!=null&&truck.Status==VehicleStatus.Towed){
                sawTrail=true;Assert.Equal(6.5,tow.Position.Distance(truck.Position),3);
                if(sim.State.Vehicles.Contains(follower)&&follower.Status==VehicleStatus.Travelling&&Math.Abs(follower.Position.Z-truck.Position.Z)<1.5&&Math.Cos(follower.Heading-truck.Heading)>.9&&follower.Position.X>truck.Position.X)Assert.True(truck.Position.Distance(follower.Position)>=4,$"{truck.Position} {follower.Position}");
            }
        }
        Assert.True(sawTrail);var heavy=sim.State.Vehicles.Single(v=>v.Body==TruckBody.TowHeavy);Assert.Equal(VehicleStatus.Outside,heavy.Status);
        Assert.Equal(VehicleStatus.Outside,truck.Status);Assert.True(truck.Fuel>0);Assert.Contains(sim.State.Ledger,m=>m.Category=="Combustível"&&m.From=="global"&&m.To==scene.StationOwner!.Account);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }
    [Fact] public void Without_an_operating_station_the_tow_takes_the_car_to_the_exterior_where_the_owner_buys_fuel_and_drives_back()
    {
        var scene=City(station:false);var car=scene.Car;var sawExterior=false;
        Run(scene,2400,()=>{if(car.Status==VehicleStatus.Towed&&car.Position.X< -200)sawExterior=true;});
        Assert.True(sawExterior);var fuel=scene.Sim.State.Ledger.Single(m=>m.Category=="Combustível");Assert.Equal(scene.Driver.Account,fuel.From);Assert.Equal("global",fuel.To);
        Assert.Equal(VehicleStatus.Parked,car.Status);Assert.Equal("Estacionado",car.Cause);Assert.True(car.Position.Distance(new(46.6,-12.5))<1,car.Position.ToString());
        Assert.Equal(VehicleStatus.Outside,scene.Tow!.Status);Assert.Equal(scene.Sim.State.Tuning.TotalMoney,scene.Sim.MonetaryTotal);scene.Sim.CheckInvariants();
    }
    [Fact] public void An_impossible_call_lets_people_walk_and_is_retried_once_the_road_network_changes()
    {
        var scene=City(connected:false);var sim=scene.Sim;Run(scene,10);
        Assert.Equal(VehicleStatus.Stranded,scene.Car.Status);Assert.Null(scene.Car.TowTruckId);Assert.Contains("guincho impossível",scene.Car.Cause);
        Assert.Null(scene.Driver.VehicleId);Assert.DoesNotContain(sim.State.Ledger,m=>m.Category=="Guincho");
        Run(scene,300);Assert.Null(scene.Car.TowTruckId);
        // Connecting the street to the highway changes the network; the call is made again.
        for(var x=-188;x<-120;x+=4)sim.State.Roads.Add(new(){Id=sim.State.NextId++,Position=new(x,0)});
        typeof(CitySimulation).GetMethod("RebuildRoadGraph",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(sim,[true]);
        Run(scene,1500);
        Assert.Contains(sim.State.Ledger,m=>m.Category=="Guincho");Assert.Equal(VehicleStatus.Parked,scene.Car.Status);Assert.Contains("aguardando o dono",scene.Car.Cause);
        Assert.True(scene.Car.Position.Distance(scene.Station!.Door)<4,scene.Car.Position.ToString());sim.CheckInvariants();
    }
    [Fact] public void After_the_waiting_limit_people_walk_on_and_the_tow_leaves_the_car_parked_at_the_station()
    {
        var scene=City(tuning:new SimulationTuning{TowMaxWaitSeconds=20});var walkedAt=-1.0;
        Run(scene,1500,()=>{if(walkedAt<0&&scene.Driver.VehicleId==null)walkedAt=scene.Sim.State.ElapsedSeconds;});
        Assert.InRange(walkedAt,20,40);Assert.Contains(scene.Sim.State.Ledger,m=>m.Category=="Guincho");
        Assert.Equal(VehicleStatus.Parked,scene.Car.Status);Assert.Contains("aguardando o dono",scene.Car.Cause);Assert.DoesNotContain(scene.Sim.State.Ledger,m=>m.Category=="Combustível");
        Assert.Equal(VehicleStatus.Outside,scene.Tow!.Status);scene.Sim.CheckInvariants();
    }
    [Fact] public void Without_a_station_a_car_nobody_is_aboard_waits_off_the_road_by_the_highway()
    {
        var scene=City(station:false,tuning:new SimulationTuning{TowMaxWaitSeconds=20});
        Run(scene,1500);
        Assert.Equal(VehicleStatus.Parked,scene.Car.Status);Assert.Equal("Rebocado; aguardando o dono",scene.Car.Cause);Assert.NotNull(scene.Car.HoldingPosition);
        Assert.True(scene.Car.Position.Z< -5,scene.Car.Position.ToString());Assert.True(scene.Car.Position.X< -180);Assert.Equal(VehicleStatus.Outside,scene.Tow!.Status);scene.Sim.CheckInvariants();
    }
    [Fact] public void The_queue_behind_the_stranded_car_flows_again_once_it_is_towed()
    {
        var scene=City(tuning:new SimulationTuning{BypassWaitSeconds=1e9,UTurnWaitSeconds=1e9});var sim=scene.Sim;
        var route=sim.State.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-64&&r.Position.X<=60).OrderBy(r=>r.Position.X).Select(r=>r.Id).ToList();
        // Bypassing is disabled here: only the tow can free the queue.
        var queue=Enumerable.Range(0,3).Select(i=>new Vehicle{Id=sim.State.NextId++,OwnerAccount="global",PassengerCar=true,External=true,Position=new(-12-i*9,-1.65),Heading=Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=route.ToList(),RouteIndex=route.FindIndex(id=>sim.State.Roads.First(r=>r.Id==id).Position.X>-12-i*9)}).ToList();
        sim.State.Vehicles.AddRange(queue);
        Run(scene,1500);
        var tows=string.Join(" | ",sim.State.Vehicles.Where(v=>v.Body==TruckBody.Tow).Select(v=>$"{v.Status} {v.TowPhase} {v.Position} «{v.Cause}»"));
        Assert.All(queue,v=>Assert.True(v.Position.X>30||v.RouteIndex>=v.Route.Count,v.Cause+" "+v.Position+" guinchos: "+tows));sim.CheckInvariants();
    }
    [Fact] public void A_car_stranded_at_a_junction_mouth_holding_it_is_still_reached_and_the_junction_frees()
    {
        // A side street north at x=4 makes (4,0) a junction right in front of the car.
        var scene=City(ring:false);var s=scene.Sim.State;
        for(var z=4;z<=40;z+=4)s.Roads.Add(new(){Id=s.NextId++,Position=new(4,z)});
        var sim=new CitySimulation(s);var car=sim.State.Vehicles.Single(v=>v.Id==scene.Car.Id);var junction=sim.State.Roads.Single(r=>r.Position==new Point(4,0));
        car.IntersectionClaim=junction.Id;
        var reached=false;for(var t=0;t<900;t+=2){sim.Advance(2);if(sim.State.Vehicles.Any(v=>v.TowTargetId==car.Id&&v.TowPhase==TowPhase.Loading))reached=true;}
        Assert.True(reached,string.Join(" | ",sim.State.Vehicles.Where(v=>v.Body==TruckBody.Tow).Select(v=>v.Position+" "+v.Cause)));
        Assert.NotEqual(VehicleStatus.Stranded,car.Status);Assert.Null(car.IntersectionClaim);sim.CheckInvariants();
    }
}
