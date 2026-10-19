using Godot;
using IndexCities.Simulation;
using System.IO;
namespace IndexCities.Game;

public partial class Main
{
    // SPEC transporte-circulacao, "Socorro a veículos parados", with the real 2 s
    // quantum in the renderer: a car out of fuel on the southern street and an
    // external truck on the northern one each call a tow from the exterior. The
    // platform tow carries the car (and its driver) to the station; the heavy
    // tow lifts the truck and pulls it behind. Visuals stay on the kit anchors
    // (CarryAnchor/LiftAnchor, or the fallback while the scenes are missing),
    // the queue clears and a save taken mid-tow restores the towing state.
    async void TowSmoke()
    {
        var directory=Path.GetFullPath(_reviewSaveDir??ProjectSettings.GlobalizePath("res://../../artifacts/verification/tow"));
        Directory.CreateDirectory(directory);
        try{
            Require(DisplayServer.GetName()!="headless","Guincho exige janela renderizada");
            _speed=0;_autosaveMinutes=0;_clockBudget=0;ClearTool();ClearSelection();
            CitySaves.VerificationDirectory=Path.Combine(directory,"cities");
            var captures=new List<string>();
            async System.Threading.Tasks.Task Shot(string name,Point at,float zoom=40){
                _focus=ToV(at);_zoom=zoom;UpdateCamera();_world.Sync(_sim.State);RefreshHud();
                await RecoveryCapture(Path.Combine(directory,name+".png"),GetViewport());
                File.WriteAllText(Path.Combine(directory,name+".json"),_sim.SaveToJson());captures.Add(name);
            }
            var (car,driver,truck,station)=TowQACity();
            _world.Reset(_sim.State);BuildCatalog();
            var models=new[]{"vehicle.tow_truck.a","vehicle.tow_truck_heavy.a"}.Select(id=>id+(ResourceLoader.Exists("res://Assets/Construction/Scenes/"+id+".tscn")?"=final":"=fallback")).ToArray();
            await Shot("tow-stranded",new(0,20),70);
            bool carLoaded=false,truckLoaded=false,carMounted=false,truckMounted=false,carDelivered=false,truckDelivered=false;string? midTow=null;
            Vehicle? Tow(Vehicle target)=>_sim.State.Vehicles.FirstOrDefault(v=>v.TowTargetId==target.Id&&v.TowPhase!=TowPhase.None);
            for(var step=0;step<1200&&!(carDelivered&&truckDelivered&&_sim.State.Vehicles.Where(v=>v.Body is TruckBody.Tow or TruckBody.TowHeavy).All(v=>v.Status==VehicleStatus.Outside));step++){
                _sim.Advance(2);_world.Sync(_sim.State);
                var t=_sim.State.ElapsedSeconds;
                if(Tow(car) is {} platform){
                    Require(_world.RenderedVehicle(platform.Id)=="vehicle.tow_truck.a",$"t={t}: guincho de plataforma usa vehicle.tow_truck.a");
                    if(platform.TowPhase==TowPhase.Loading){Require(platform.Position.X>car.Position.X+5&&Math.Abs(platform.Position.Z+1.65)<.3,$"t={t}: guincho parou na frente do carro, na faixa dele");if(!carLoaded){carLoaded=true;await Shot("tow-car-loading",car.Position,30);}}
                    if(car.Status==VehicleStatus.Towed){
                        Require(driver.VehicleId==car.Id,$"t={t}: motorista segue no guincho");
                        if(platform.TowPhase==TowPhase.Delivering&&_world.TowAnchorPosition(platform) is {} anchor&&_world.VehicleVisualPosition(car.Id) is {} shown){Require(anchor.DistanceTo(shown)<.05f,$"t={t}: carro desenhado no CarryAnchor ({anchor} × {shown})");if(!carMounted){carMounted=true;midTow=_sim.SaveToJson();await Shot("tow-car-carried",platform.Position,30);}}
                    }
                }
                if(Tow(truck) is {} heavy){
                    Require(_world.RenderedVehicle(heavy.Id)=="vehicle.tow_truck_heavy.a",$"t={t}: caminhão é rebocado pelo guincho pesado");
                    if(heavy.TowPhase==TowPhase.Loading&&!truckLoaded){truckLoaded=true;await Shot("tow-truck-lifting",truck.Position,30);}
                    if(truck.Status==VehicleStatus.Towed){
                        Require(Math.Abs(heavy.Position.Distance(truck.Position)-6.5)<.01,$"t={t}: caminhão segue atrás do guincho pesado");
                        if(heavy.TowPhase==TowPhase.Delivering&&_world.TowAnchorPosition(heavy) is {} lift&&_world.FrontAxlePosition(truck.Id) is {} axle){Require(lift.DistanceTo(axle)<.05f,$"t={t}: eixo dianteiro do caminhão no LiftAnchor ({lift} × {axle})");if(!truckMounted){truckMounted=true;await Shot("tow-truck-pulled",heavy.Position,30);}}
                    }
                }
                if(!carDelivered&&carMounted&&car.Status!=VehicleStatus.Towed){carDelivered=true;Require(car.Fuel>1,"Carro abastecido no posto pelo dono");await Shot("tow-car-at-station",station.Door,35);}
                if(!truckDelivered&&truckMounted&&truck.Status!=VehicleStatus.Towed)truckDelivered=true;
            }
            Require(carLoaded&&carMounted&&carDelivered,$"Carro rebocado ao posto: {car.Status} «{car.Cause}»");
            Require(truckLoaded&&truckMounted&&truckDelivered,$"Caminhão rebocado pelo guincho pesado: {truck.Status} «{truck.Cause}»");
            // The external truck belongs to the Global Reserve itself: only the car owner leaves a movement.
            Require(_sim.State.Ledger.Any(m=>m.Category=="Guincho"&&m.From==driver.Account&&m.To=="global"),"A dona do carro pagou o guincho à Reserva Global");
            Require(_sim.State.Vehicles.Where(v=>v.Status==VehicleStatus.Travelling&&v.Cause.StartsWith("Aguardando veículo",StringComparison.Ordinal)).All(v=>v.Id==car.Id||v.Id==truck.Id),"Ninguém continua parado atrás de veículo sem combustível");
            _sim.CheckInvariants();Require(_sim.MonetaryTotal==_sim.State.Tuning.TotalMoney,"Oferta monetária preservada");
            await Shot("tow-done",new(0,20),70);
            // Save/load mid-tow: the restored city shows the car still on the platform.
            _sim=CitySimulation.FromJson(midTow!);_world.Reset(_sim.State);_world.Sync(_sim.State);
            var restored=_sim.State.Vehicles.Single(v=>v.Id==car.Id);var restoredTow=_sim.State.Vehicles.Single(v=>v.TowTargetId==car.Id);
            Require(restored.Status==VehicleStatus.Towed&&restoredTow.TowPhase==TowPhase.Delivering,"Save guarda o carro no guincho");
            Require(_world.TowAnchorPosition(restoredTow) is {} reloaded&&_world.VehicleVisualPosition(car.Id) is {} again&&reloaded.DistanceTo(again)<.05f,"Carregado, o carro aparece no guincho");
            await Shot("tow-reloaded",restoredTow.Position,30);
            File.WriteAllText(Path.Combine(directory,"evidence.txt"),string.Join(System.Environment.NewLine,captures));
            GD.Print($"INDEXCITIES_TOW_OK models={string.Join(",",models)} captures={captures.Count} directory={directory} car=front-loaded-carried-refuelled truck=lifted-pulled-delivered queue=clear saveload=towed");
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
    // Street z=0 to x=60 with a northern street (z=40) joined at x=-60 and x=60;
    // a station at x=30 and a shop; the car heads east on the southern street,
    // the external truck heads west on the northern one back to the exterior.
    (Vehicle Car,Citizen Driver,Vehicle Truck,Building Station) TowQACity()
    {
        var s=new CitySimulation(42).State;int Next()=>s.NextId++;
        void Road(double x,double z)=>s.Roads.Add(new(){Id=Next(),Position=new(x,z)});
        for(var x=-188;x<=60;x+=4)Road(x,0);for(var x=-60;x<=60;x+=4)Road(x,40);for(var z=4;z<=36;z+=4){Road(-60,z);Road(60,z);}
        void Fund(string account,decimal amount){s.Wallets["global"]-=amount;s.Wallets[account]=s.Wallets.GetValueOrDefault(account)+amount;}
        Company Owner(string name){var c=new Company{Id=Next(),Name=name};s.Companies.Add(c);Fund(c.Account,1000);return c;}
        Citizen Person(string name,Point at){var family=Next();var c=new Citizen{Id=Next(),Name=name,FamilyId=family,Age=30,Resident=true,InsideMap=true,Position=at};s.Citizens.Add(c);s.Wallets[c.Account]=0;s.Families.Add(new(){Id=family,Members=[c.Id]});return c;}
        var owner=Owner("Rota QA");var station=new Building{Id=Next(),Kind=BuildingKind.FuelStation,Position=new(30,19),Completed=true,CompanyId=owner.Id,OwnerAccount=owner.Account,SalePrice=7};station.Stock[ResourceKind.Fuel]=300;s.Buildings.Add(station);owner.Buildings.Add(station.Id);
        Person("Frentista",station.Door).WorkplaceId=station.Id;
        var market=Owner("Mercado QA");var shop=new Building{Id=Next(),Kind=BuildingKind.Grocery,Position=new(40,-16),Rotation=Math.PI,Completed=true,CompanyId=market.Id,OwnerAccount=market.Account};s.Buildings.Add(shop);market.Buildings.Add(shop.Id);
        var driver=Person("Ana",new(0,-1.65));Fund(driver.Account,5000);
        var east=s.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-20&&r.Position.X<=40).OrderBy(r=>r.Position.X).Select(r=>r.Id).ToList();
        var car=new Vehicle{Id=Next(),OwnerAccount=driver.Account,PassengerCar=true,CargoCapacity=0,Position=new(0,-1.65),Heading=Math.PI/2,Status=VehicleStatus.Travelling,Route=east,RouteIndex=6,DestinationBuildingId=shop.Id,ParkingSpot="b:"+shop.Id+":0",TripPurpose=JourneyPurpose.Shopping,DriverId=driver.Id};
        s.Vehicles.Add(car);driver.VehicleId=car.Id;driver.Purpose=JourneyPurpose.Shopping;driver.DestinationId=shop.Id;
        var truck=s.Vehicles.First(v=>v.External&&v.Body==TruckBody.Cargo);
        var home=s.Roads.Where(r=>r.Position.Z==40&&r.Position.X<=40).OrderByDescending(r=>r.Position.X).Concat(s.Roads.Where(r=>r.Position.X==-60&&r.Position.Z is >0 and <40).OrderByDescending(r=>r.Position.Z)).Concat(s.Roads.Where(r=>r.Position.Z==0&&r.Position.X<=-60).OrderByDescending(r=>r.Position.X)).Select(r=>r.Id).ToList();
        truck.Position=new(0,41.65);truck.Heading=-Math.PI/2;truck.Status=VehicleStatus.Travelling;truck.Route=home;truck.RouteIndex=11;truck.Cause="Retorno ao exterior";
        // Followers behind each stranded vehicle, so the queue can be seen to clear.
        var south=s.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-64&&r.Position.X<=60).OrderBy(r=>r.Position.X).Select(r=>r.Id).ToList();
        s.Vehicles.Add(new Vehicle{Id=Next(),OwnerAccount="global",PassengerCar=true,External=true,Position=new(-30,-1.65),Heading=Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=south,RouteIndex=south.FindIndex(id=>s.Roads.First(r=>r.Id==id).Position.X>-30)});
        s.Now=new(2026,1,1,8,0,0);s.EconomyNextAt=1e9;
        _sim=new CitySimulation(s);_cityId=CitySaves.NewId();
        return (car,driver,truck,station);
    }
}
