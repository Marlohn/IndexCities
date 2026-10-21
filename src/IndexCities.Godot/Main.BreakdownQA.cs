using Godot;
using IndexCities.Simulation;
using System.IO;
namespace IndexCities.Game;

public partial class Main
{
    // SPEC transporte-circulacao, "Quebras, guincho local e oficina", with the real
    // 2 s quantum in the renderer. City A (with a mechanic shop, its own tow and a
    // mechanic on shift): a worn car breaks down on the street, the follower goes
    // around it, the shop's tow carries it to the shop, it is repaired on a bay and
    // brought back to the owner's space; an external truck breaks down and the heavy
    // external tow takes it to the exterior, from where it comes back repaired.
    // City B (no shop): the broken car goes to the exterior and back. Repair times
    // are shortened in these fixtures (calibration) to keep the run short.
    async void BreakdownSmoke()
    {
        var directory=Path.GetFullPath(_reviewSaveDir??ProjectSettings.GlobalizePath("res://../../artifacts/verification/breakdown"));
        Directory.CreateDirectory(directory);
        try{
            Require(DisplayServer.GetName()!="headless","Quebras exigem janela renderizada");
            _speed=0;_autosaveMinutes=0;_clockBudget=0;ClearTool();ClearSelection();
            CitySaves.VerificationDirectory=Path.Combine(directory,"cities");
            var captures=new List<string>();
            async System.Threading.Tasks.Task Shot(string name,Point at,float zoom=40,Vehicle? inspect=null){
                if(inspect!=null){ClearSelection();_selectedVehicle=inspect.Id;}
                // The pointer off the scene, so no hover tooltip covers it.
                Input.WarpMouse(new(8,GetViewport().GetVisibleRect().Size.Y-8));
                _focus=ToV(at);_zoom=zoom;UpdateCamera();_world.Sync(_sim.State);RefreshHud();
                await RecoveryCapture(Path.Combine(directory,name+".png"),GetViewport());
                File.WriteAllText(Path.Combine(directory,name+".json"),_sim.SaveToJson());captures.Add(name);
            }
            bool Run(double seconds,Func<bool> until){for(var t=0.0;t<seconds;t+=2){_sim.Advance(2);_world.Sync(_sim.State);if(until())return true;}return false;}
            void Calm(){_sim.State.Tuning.BreakdownChancePerKmNew=0;_sim.State.Tuning.BreakdownChancePerKmWorn=0;}
            // Animations (hood, lift, gate) run in real time between simulation steps.
            async System.Threading.Tasks.Task Settle(double seconds)=>await ToSignal(GetTree().CreateTimer(seconds),SceneTreeTimer.SignalName.Timeout);
            void Parts(Vehicle v,bool broken,string when){var shown=_world.BreakdownParts(v.Id);Require(shown==(broken,broken,broken,broken),$"{when}: capô aberto, pisca, fumaça e triângulo {(broken?"presentes":"ausentes")} ({shown})");}

            void LiftedPose(Vehicle v,string when,bool raised){
                var pose=_world.VehiclePose(v.Id);Require(pose is {} p&&Math.Abs(p.X-v.Position.X)<.6&&Math.Abs(p.Z-v.Position.Z)<.6,$"Pose do carro {when}: sobre a vaga do elevador ({pose} x simulação {v.Position})");
                Require(raised?pose!.Value.Y is >1.2f and <1.6f:pose!.Value.Y is >-.1f and <.15f,$"Pose do carro {when}: altura {(raised?"elevada (~1,4 m)":"no chão")} ({pose!.Value.Y:0.00} m)");
            }

            // City A.
            var a=BreakdownQACity(shop:true);var car=a.Car;var shopId=a.Shop!.Id;
            _world.Reset(_sim.State);BuildCatalog();
            await Shot("breakdown-a-start",new(-10,10),70);
            _sim.Advance(2);_world.Sync(_sim.State);Calm();
            Require(car.BrokenDown&&car.Status==VehicleStatus.Stranded,$"Carro quebrou na via: {car.Status} «{car.Cause}»");
            Require(a.Driver.VehicleId==null,"A motorista desceu e segue a pé");
            Parts(car,true,"Carro quebrado na via");await Settle(1.2);
            await Shot("breakdown-a-car-broken",car.Position,14,car);
            Require(Run(240,()=>a.Follower.Position.X>car.Position.X+8),$"O carro de trás contornou o quebrado: {a.Follower.Position} «{a.Follower.Cause}»");
            await Shot("breakdown-a-bypass",car.Position,35);
            Require(Run(60,()=>car.TowTruckId!=null),"Socorro chamado");
            var tow=_sim.State.Vehicles.Single(v=>v.Id==car.TowTruckId);
            Require(!tow.External&&tow.HomeBuildingId==shopId,$"Veio o guincho da oficina (veio {tow.Id}, externo={tow.External})");
            Require(Run(1800,()=>car.Status==VehicleStatus.Towed&&tow.TowPhase==TowPhase.Delivering),$"Guincho local carregou o carro: «{tow.Cause}»");
            Parts(car,false,"Carro carregado no guincho");await Settle(1.2);
            await Shot("breakdown-a-local-tow-carrying",tow.Position,30,car);
            Require(Run(1800,()=>car.ServicePhase==ServicePhase.InService&&car.PushPath.Count==0),$"Carro numa vaga da oficina: «{car.Cause}»");
            Require(car.ServiceShopId==shopId&&car.ServiceBay is not null&&_sim.ServiceBays(a.Shop).Contains(car.Id),"Contrato de apresentação: oficina e vaga do veículo");
            Require(car.ServiceBay==0&&_world.VehicleOnLift(car.Id)&&_world.ShopPresentation(shopId).LiftUp==true,$"Carro no elevador e elevador subindo (vaga {car.ServiceBay}, {_world.ShopPresentation(shopId)})");
            Require(_world.BreakdownParts(car.Id).Hood&&!_world.BreakdownParts(car.Id).Triangle,"Em serviço: capô aberto, sem triângulo");Require(_world.ShopPresentation(shopId).GateOpen==false,"Portão do piso fechado sem veículo na vaga do piso");
            // Lift from two of the player's camera angles: the start of the rise, then the raised pose.
            var bay=a.Shop.Position+new Point(6.9,0);foreach(var yaw in new[]{-45f,45f}){_yaw=yaw;await Shot("breakdown-a-lift-start-yaw"+yaw,bay,14,car);}
            await Settle(1.2);await Shot("breakdown-a-lift-rising-yaw45",bay,14,car);_yaw=-45;await Settle(2.2);await Shot("breakdown-a-in-service",bay,14,car);_yaw=45;
            // The drawn pose, not only the metadata: over the bay's anchor and raised by the lift (asset contract: 1.4 m).
            LiftedPose(car,"em serviço",true);
            // The floor bay's gate: with the lift busy, the shop's own tow goes to revision
            // on the floor bay (grocery stocked so the review imports nothing).
            Require(Run(600,()=>tow.Status==VehicleStatus.Parked&&tow.PushPath.Count==0&&tow.Position.Distance(CitySimulation.ShopTowSpace(a.Shop))<.5),$"Guincho de volta à oficina: «{tow.Cause}»");
            var grocery=_sim.State.Buildings.Single(b=>b.Kind==BuildingKind.Grocery);grocery.AddStock(ResourceKind.Food,grocery.Definition.Capacity-grocery.Quantity(ResourceKind.Food));
            _sim.State.Tuning.LocalRevisionSeconds=300;tow.Condition=.3;_sim.State.EconomyNextAt=0;_sim.Advance(2);_sim.State.EconomyNextAt=1e9;_world.Sync(_sim.State);
            Require(tow.Service==ServiceKind.Revision&&tow.ServiceShopId==shopId,$"Guincho da oficina em revisão: «{tow.Cause}»");
            Require(Run(600,()=>tow.ServiceBay==1&&tow.PushPath.Count>0),$"Guincho entrando na vaga do piso: «{tow.Cause}»");
            Require(_world.ShopPresentation(shopId).GateOpen==true,"Portão abre com veículo entrando");
            await Settle(1.8);await Shot("breakdown-a-gate-open",bay+new Point(-5.5,0),16);
            Require(Run(600,()=>tow.ServicePhase==ServicePhase.InService&&tow.PushPath.Count==0),$"Guincho em revisão na vaga do piso: «{tow.Cause}»");
            Require(_world.ShopPresentation(shopId).GateOpen==true,"Portão aberto com veículo em serviço no horário");
            Require(Run(900,()=>tow.ServicePhase==ServicePhase.None&&tow.PushPath.Count==0),$"Guincho revisado de volta à vaga: «{tow.Cause}»");
            Require(_world.ShopPresentation(shopId).GateOpen==false&&tow.Condition>.999,"Portão fecha sem veículo na vaga do piso; guincho revisado");
            Require(Run(1800,()=>car.ServicePhase==ServicePhase.AwaitingReturn),$"Conserto concluído: «{car.Cause}»");
            Require(car.Condition>.999&&!car.BrokenDown,"Conserto devolve 100%");
            Require(_world.ShopPresentation(shopId).LiftUp==false,"Elevador desce ao fim do serviço");
            await Settle(3.4);LiftedPose(car,"serviço concluído",false);
            Require(Run(2400,()=>car.Status==VehicleStatus.Towed),$"Guincho carregou o carro consertado: «{car.Cause}»");Require(!_world.VehicleOnLift(car.Id),"Carro saiu do elevador ao ser carregado");Parts(car,false,"Carro consertado sendo devolvido");await Settle(1.2);
            await Shot("breakdown-a-return-carrying",_sim.State.Vehicles.Single(v=>v.Id==car.TowTruckId).Position,30,car);
            Require(Run(2400,()=>car.Status==VehicleStatus.Parked&&car.ServicePhase==ServicePhase.None&&car.PushPath.Count==0&&car.TowTruckId==null),$"Carro devolvido à vaga da dona: «{car.Cause}»");
            Require(car.Position.Distance(new(-6.6,-7.7))<.1,$"Na vaga de casa: {car.Position}");
            await Shot("breakdown-a-car-home",car.Position,30,car);
            Require(_sim.State.Ledger.Any(m=>m.Category=="Conserto de veículo"&&m.From==a.Driver.Account&&m.To=="company:"+a.Shop.CompanyId),"Conserto pago pela dona à oficina");
            Require(_sim.State.Ledger.Count(m=>m.Category=="Guincho"&&m.From==a.Driver.Account&&m.To=="company:"+a.Shop.CompanyId)==2,"Socorro e devolução pagos à oficina");

            // External truck in city A.
            var truck=a.Truck;truck.Condition=.1;_sim.State.Tuning.BreakdownChancePerKmNew=0;_sim.State.Tuning.BreakdownChancePerKmWorn=1e7;
            truck.Position=new(0,41.65);truck.Heading=-Math.PI/2;truck.Status=VehicleStatus.Travelling;truck.RouteIndex=11;truck.Fuel=40;
            _sim.Advance(2);_world.Sync(_sim.State);Calm();
            Require(truck.BrokenDown,$"Caminhão quebrou: «{truck.Cause}»");
            Parts(truck,true,"Caminhão quebrado na via");await Settle(1.2);
            await Shot("breakdown-a-truck-broken",truck.Position,22,truck);
            Require(Run(120,()=>truck.TowTruckId!=null),"Socorro do caminhão chamado");
            var heavy=_sim.State.Vehicles.Single(v=>v.Id==truck.TowTruckId);Require(heavy.External&&heavy.Body==TruckBody.TowHeavy,"Caminhão atendido pelo guincho pesado externo");
            Require(Run(1800,()=>truck.Status==VehicleStatus.Towed&&heavy.TowPhase==TowPhase.Delivering),$"Guincho pesado rebocando: «{heavy.Cause}»");
            Parts(truck,false,"Caminhão no guincho pesado");await Settle(1.2);
            await Shot("breakdown-a-heavy-tow",heavy.Position,30,truck);
            Require(Run(1800,()=>truck.ServicePhase==ServicePhase.AtExterior),$"Caminhão no exterior para conserto: «{truck.Cause}»");
            Require(Run(1800,()=>truck.ServicePhase==ServicePhase.None&&truck.Status==VehicleStatus.Outside),$"Caminhão consertado no exterior: «{truck.Cause}»");
            Require(truck.Condition>.999&&!truck.BrokenDown,"Caminhão consertado a 100%");
            _sim.CheckInvariants();Require(_sim.MonetaryTotal==_sim.State.Tuning.TotalMoney,"Oferta monetária preservada (cidade A)");
            var cityA=_sim.SaveToJson();var restored=CitySimulation.FromJson(cityA);Require(restored.SaveToJson()==cityA,"Save/load preserva o estado de condição e serviço");

            // City B: no shop.
            var b=BreakdownQACity(shop:false);car=b.Car;_world.Reset(_sim.State);BuildCatalog();
            _sim.Advance(2);_world.Sync(_sim.State);Calm();Require(car.BrokenDown,"Carro quebrou (cidade sem oficina)");
            Require(Run(600,()=>car.TowTruckId!=null&&_sim.State.Vehicles.Single(v=>v.Id==car.TowTruckId).TowPhase==TowPhase.Delivering),$"Sem oficina, o guincho externo carrega o carro: «{car.Cause}»");
            await Settle(1.2);await Shot("breakdown-b-external-tow",_sim.State.Vehicles.Single(v=>v.Id==car.TowTruckId).Position,30,car);
            Require(Run(1800,()=>car.ServicePhase==ServicePhase.AtExterior),$"Sem oficina, o carro vai ao exterior: «{car.Cause}»");
            await Shot("breakdown-b-exterior",new(-230,0),45,car);
            Require(_sim.State.Ledger.Any(m=>m.Category=="Conserto de veículo"&&m.From==b.Driver.Account&&m.To=="global"),"Conserto no exterior pago à Reserva Global");
            Require(Run(3600,()=>car.Status==VehicleStatus.Parked&&car.ServicePhase==ServicePhase.None&&car.PushPath.Count==0&&car.TowTruckId==null),$"Carro volta do exterior à vaga: «{car.Cause}»");
            Require(car.Condition>.999,"Consertado no exterior");
            await Shot("breakdown-b-car-home",car.Position,30,car);
            _sim.CheckInvariants();Require(_sim.MonetaryTotal==_sim.State.Tuning.TotalMoney,"Oferta monetária preservada (cidade B)");
            File.WriteAllText(Path.Combine(directory,"evidence.txt"),string.Join(System.Environment.NewLine,captures));
            GD.Print($"INDEXCITIES_BREAKDOWN_OK captures={captures.Count} directory={directory} shop=local-tow-repaired-returned truck=heavy-exterior-repaired noshop=exterior-repaired-returned saveload=ok");
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
    sealed record BreakdownQAScene(Vehicle Car,Citizen Driver,Vehicle Follower,Vehicle Truck,Building? Shop);
    // Street z=0 to x=60 with a northern ring (z=40); the car's home faces the
    // street at x=-10, the shop at x=-30 (also south, facing the camera), the grocery at x=40. A worn
    // car heads east with a follower behind it; an external truck waits on the ring.
    BreakdownQAScene BreakdownQACity(bool shop)
    {
        var s=new CitySimulation(42).State;int Next()=>s.NextId++;
        void Road(double x,double z)=>s.Roads.Add(new(){Id=Next(),Position=new(x,z)});
        for(var x=-188;x<=60;x+=4)Road(x,0);for(var x=-60;x<=60;x+=4)Road(x,40);for(var z=4;z<=36;z+=4){Road(-60,z);Road(60,z);}
        void Fund(string account,decimal amount){s.Wallets["global"]-=amount;s.Wallets[account]=s.Wallets.GetValueOrDefault(account)+amount;}
        Company Owner(string name){var c=new Company{Id=Next(),Name=name};s.Companies.Add(c);Fund(c.Account,2000);return c;}
        Citizen Person(string name,Point at){var f=Next();var c=new Citizen{Id=Next(),Name=name,FamilyId=f,Age=30,Resident=true,InsideMap=true,Position=at};s.Citizens.Add(c);s.Wallets[c.Account]=0;s.Families.Add(new(){Id=f,Members=[c.Id]});return c;}
        var market=Owner("Mercado QA");var grocery=new Building{Id=Next(),Kind=BuildingKind.Grocery,Position=new(40,-16),Rotation=Math.PI,Completed=true,CompanyId=market.Id,OwnerAccount=market.Account};s.Buildings.Add(grocery);market.Buildings.Add(grocery.Id);
        var driver=Person("Ana",new(0,-1.65));Fund(driver.Account,5000);
        var house=new Building{Id=Next(),Kind=BuildingKind.House,Position=new(-10,-14),Rotation=Math.PI,Completed=true,OwnerAccount=driver.Account,FamilyId=driver.FamilyId};s.Buildings.Add(house);s.Families.Single(f=>f.Id==driver.FamilyId).HomeId=house.Id;
        Building? garage=null;
        if(shop){
            var owner=Owner("Oficina QA");garage=new Building{Id=Next(),Kind=BuildingKind.MechanicShop,Position=new(-30,-15.5),Rotation=Math.PI,Completed=true,CompanyId=owner.Id,OwnerAccount=owner.Account,SalePrice=120};s.Buildings.Add(garage);owner.Buildings.Add(garage.Id);
            foreach(var name in new[]{"Mecânico","Mecânica"}){var mechanic=Person(name,garage.Door);mechanic.WorkplaceId=garage.Id;mechanic.Activity="No trabalho";}
            s.Vehicles.Add(new Vehicle{Id=Next(),Body=TruckBody.Tow,OwnerAccount=owner.Account,HomeBuildingId=garage.Id,CargoCapacity=0,Position=CitySimulation.ShopTowSpace(garage),Heading=CitySimulation.TowBaseHeading(garage),Fuel=40,Status=VehicleStatus.Parked,Cause="Guincho da oficina disponível"});
        }
        var east=s.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-20&&r.Position.X<=40).OrderBy(r=>r.Position.X).Select(r=>r.Id).ToList();
        var car=new Vehicle{Id=Next(),OwnerAccount=driver.Account,PassengerCar=true,CargoCapacity=0,Position=new(0,-1.65),Heading=Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=east,RouteIndex=6,DestinationBuildingId=grocery.Id,ParkingSpot="b:"+grocery.Id+":0",TripPurpose=JourneyPurpose.Shopping,DriverId=driver.Id,Cause="Em viagem",Condition=.2};
        s.Vehicles.Add(car);driver.VehicleId=car.Id;driver.Purpose=JourneyPurpose.Shopping;driver.DestinationId=grocery.Id;driver.Activity="Dirigindo carro particular";
        // The follower drives on to the exterior (up the eastern side, along the ring and back west):
        // a route ending at the corner would leave it parked in the lane, in the way of the tow turning there.
        var south=s.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-32&&r.Position.X<=60).OrderBy(r=>r.Position.X).Concat(s.Roads.Where(r=>r.Position.X==60&&r.Position.Z is >0 and <40).OrderBy(r=>r.Position.Z)).Concat(s.Roads.Where(r=>r.Position.Z==40).OrderByDescending(r=>r.Position.X)).Concat(s.Roads.Where(r=>r.Position.X==-60&&r.Position.Z is >0 and <40).OrderByDescending(r=>r.Position.Z)).Concat(s.Roads.Where(r=>r.Position.Z==0&&r.Position.X<=-60).OrderByDescending(r=>r.Position.X)).Select(r=>r.Id).ToList();
        var follower=new Vehicle{Id=Next(),OwnerAccount="global",PassengerCar=true,External=true,Position=new(-30,-1.65),Heading=Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=south,RouteIndex=south.FindIndex(id=>s.Roads.First(r=>r.Id==id).Position.X>-30)};s.Vehicles.Add(follower);
        var truck=s.Vehicles.First(v=>v.External&&v.Body==TruckBody.Cargo);
        truck.Route=s.Roads.Where(r=>r.Position.Z==40&&r.Position.X<=40).OrderByDescending(r=>r.Position.X).Concat(s.Roads.Where(r=>r.Position.X==-60&&r.Position.Z is >0 and <40).OrderByDescending(r=>r.Position.Z)).Concat(s.Roads.Where(r=>r.Position.Z==0&&r.Position.X<=-60).OrderByDescending(r=>r.Position.X)).Select(r=>r.Id).ToList();
        // The worn car breaks at once (forced hazard, nothing at full condition), shortened service times (calibration of this fixture).
        s.Tuning.BreakdownChancePerKmNew=0;s.Tuning.BreakdownChancePerKmWorn=1e7;s.Tuning.LocalRepairSeconds=900;s.Tuning.ExteriorRepairSeconds=1200;
        s.Now=new(2026,1,1,8,0,0);s.EconomyNextAt=1e9;
        _sim=new CitySimulation(s);_cityId=CitySaves.NewId();
        return new(car,driver,follower,truck,garage);
    }
}
