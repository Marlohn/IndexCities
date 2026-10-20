namespace IndexCities.Simulation;

// SPEC transporte-circulacao, "Quebras, guincho local e oficina": every motor
// vehicle has a condition that falls with the distance driven; one in use on
// the road may break down (rare in good condition). A broken vehicle stops,
// its people go on foot, the cargo stays and the owner calls the tow at once.
// It is taken to a local mechanic shop with room, otherwise to the exterior
// (external trucks always), or left at the owner's space when the owner cannot
// pay the repair. Repair and revision bring the condition back to 100% with
// real work and time at the shop; the repaired vehicle returns by tow.
public sealed partial class CitySimulation
{
    // Positions in a mechanic shop lot (local x to the right of the facade, z
    // towards the back, as LotSpots), on the anchors of the shop model
    // (Assets/Workshop): the bays inside (0 = lift, 1 = floor), the waiting
    // place in the yard at each bay door, the shop tow's own space and the
    // places by the reception where a driver waits.
    static readonly (double X,double Z)[] ShopBayOffsets=[(-6.905,3.35),(-1.45,4.0)];
    const double ShopWaitingZ=-5.6;
    static readonly (double X,double Z) ShopTowOffset=(6.6,-5.8);
    static readonly (double X,double Z)[] ShopDriverWaitOffsets=[(4.5,-3.1),(5.5,-3.1)];
    public static int ShopBayCount=>ShopBayOffsets.Length;
    // Bays plus one waiting place at each bay door.
    static int ShopCapacity=>ShopBayOffsets.Length*2;
    Point ShopBay(Building shop,int bay)=>LocalPoint(shop,ShopBayOffsets[bay].X,ShopBayOffsets[bay].Z);
    Point ShopWaiting(Building shop,int bay)=>LocalPoint(shop,ShopBayOffsets[bay].X,ShopWaitingZ);
    public static Point ShopTowSpace(Building shop)=>LocalPoint(shop,ShopTowOffset.X,ShopTowOffset.Z);
    // A free place by the reception for a driver waiting for the revision.
    Point ShopDriverWait(Building shop,Citizen driver)
    {
        foreach(var (x,z) in ShopDriverWaitOffsets){var at=LocalPoint(shop,x,z);if(!State.Citizens.Any(c=>c.Id!=driver.Id&&c.Alive&&c.Position.Distance(at)<.3))return at;}
        return LocalPoint(shop,ShopDriverWaitOffsets[0].X,ShopDriverWaitOffsets[0].Z);
    }
    // Vehicles in the yard and on the bays stand nose to the street; the shop
    // tow along the facade, nose to the side it leaves by (local -x).
    static double OutwardHeading(Building shop)=>shop.Rotation+Math.PI;
    public static double TowBaseHeading(Building shop)=>shop.Rotation-Math.PI/2;

    double NextBreakdownRandom()
    {
        var x=State.BreakdownRandomState;if(x==0)x=(ulong)(uint)State.Seed*0x2545F4914F6CDD1DUL+0x632BE59BD9B4E019UL;
        x^=x>>12;x^=x<<25;x^=x>>27;State.BreakdownRandomState=x;return (x*2685821657736338717UL>>11)*(1.0/(1UL<<53));
    }
    // SPEC decisions of 2026-10-20: paving machines have a condition and revision
    // but do not break down; a tow breaks down only while empty.
    static bool MayBreakDown(Vehicle v)=>v.Machine==PavingMachine.None&&!v.BrokenDown&&v.Status==VehicleStatus.Travelling&&(!IsTow(v)||v.TowPhase is TowPhase.None or TowPhase.ToVehicle);
    double BreakdownChance(Vehicle v,double metres)
    {
        var worn=1-v.Condition;var perKm=State.Tuning.BreakdownChancePerKmNew+(State.Tuning.BreakdownChancePerKmWorn-State.Tuning.BreakdownChancePerKmNew)*worn*worn;
        return 1-Math.Exp(-perKm*metres/1000);
    }
    void Wear(Vehicle v,double metres){if(metres>0)v.ConditionUnits=Math.Max(0,FixedPoint.Add(v.ConditionUnits,-metres*State.Tuning.ConditionWearPerMetre,FixedPoint.PointScale));}
    // Wear for the metres just driven on the road; true when the vehicle broke down.
    bool DriveWear(Vehicle v,double metres)
    {
        if(metres<=0)return false;Wear(v,metres);
        if(!MayBreakDown(v)||!InMap(v.Position))return false;
        if(NextBreakdownRandom()>=BreakdownChance(v,metres))return false;
        BreakDown(v);return true;
    }
    static string StrandedReason(Vehicle v)=>v.BrokenDown?"Quebrado":"Sem combustível";
    void BreakDown(Vehicle v)
    {
        v.BrokenDown=true;v.Status=VehicleStatus.Stranded;
        // A tow that breaks on its way: another one takes the call, at no new charge.
        if(IsTow(v)&&v.TowPhase==TowPhase.ToVehicle&&VehicleById(v.TowTargetId) is {} target){if(target.TowTruckId==v.Id){target.TowTruckId=null;target.TowRetryAt=0;target.Cause=StrandedReason(target)+"; o guincho a caminho quebrou, outro será chamado";}}
        if(IsTow(v)){v.TowPhase=TowPhase.None;v.TowTargetId=null;v.TowStationId=null;}
        // A revision trip that breaks down: the people go home on foot.
        if(v.ServicePhase==ServicePhase.ToService){ClearService(v);if(v.PassengerCar&&OwnerHome(v) is {} home){v.DestinationBuildingId=home.Id;v.TripPurpose=JourneyPurpose.Home;}}
        v.RefuellingAt=null;v.SavedRoute=[];
        // The people get off and go on foot; the cargo stays aboard. Non-SIM
        // external crews stay with their truck (SPEC decision of 2026-10-20).
        var claim=v.IntersectionClaim;var route=v.Route.ToList();var index=v.RouteIndex;
        if(v.PassengerCar){if(PeopleAboard(v))LeaveCar(v,"Carro quebrou na via");}
        else if(v.DriverId!=null)Disembark(v,"Veículo quebrou na via");
        v.Status=VehicleStatus.Stranded;v.IntersectionClaim=claim;v.Route=route;v.RouteIndex=index;
        v.Cause="Quebrado na via; chamando guincho";
        v.TowCalledAt=State.ElapsedSeconds;v.TowRetryAt=0;v.TowRoadRevision=null;
    }

    // Who the vehicle returns to: the owner's home space for a car, the base
    // building for a company or municipal vehicle.
    Building? OwnerHome(Vehicle v)
    {
        if(v.PassengerCar)return v.OwnerAccount.StartsWith("sim:",StringComparison.Ordinal)&&int.TryParse(v.OwnerAccount.AsSpan(4),out var id)&&CitizenById(id) is {} owner?BuildingById(FamilyById(owner.FamilyId)?.HomeId):null;
        return BuildingById(v.HomeBuildingId);
    }
    (Point Position,double Heading)? HomeSpace(Vehicle v,Building home)
    {
        if(IsTow(v)&&home.Kind==BuildingKind.MechanicShop)return (ShopTowSpace(home),TowBaseHeading(home));
        if(v.PassengerCar){var spot=ParkingSpots(home).FirstOrDefault(s=>s.Key==v.ParkingSpot&&SpotFree(s,v.Id));if(spot.Key==null)spot=ParkingSpots(home).FirstOrDefault(s=>SpotFree(s,v.Id));return spot.Key==null?null:(spot.Position,spot.Heading);}
        return Access(home) is {} access?(DockAt(access,home,false),v.Heading):null;
    }

    public static bool ShopOperatesFor(Building b)=>b.Kind==BuildingKind.MechanicShop&&b.Completed&&!b.Removed&&!b.OperationClosed&&b.CompanyId!=null;
    bool ShopOperates(Building b)=>ShopOperatesFor(b)&&CompanyById(b.CompanyId) is {Active:true}&&Access(b)!=null;
    int ShopLoad(Building shop,Vehicle? except=null)=>State.Vehicles.Count(v=>v.ServiceShopId==shop.Id&&v.Id!=except?.Id);
    // SPEC decision (7A): room while the vehicles destined to it (in service,
    // waiting in the yard, on their way) are fewer than bays plus waiting places.
    bool ShopHasRoom(Building shop,Vehicle v)=>ShopOperates(shop)&&ShopLoad(shop,v)<ShopCapacity;
    decimal ServicePrice(Building? shop,ServiceKind kind)
    {
        var seconds=shop==null?(kind==ServiceKind.Repair?State.Tuning.ExteriorRepairSeconds:State.Tuning.ExteriorRevisionSeconds):kind==ServiceKind.Repair?State.Tuning.LocalRepairSeconds:State.Tuning.LocalRevisionSeconds;
        return Money((shop==null?State.Tuning.ExteriorServiceHourlyPrice:shop.SalePrice)*(decimal)(seconds/3600));
    }
    double ServiceSeconds(Building? shop,ServiceKind kind)=>shop==null?(kind==ServiceKind.Repair?State.Tuning.ExteriorRepairSeconds:State.Tuning.ExteriorRevisionSeconds):kind==ServiceKind.Repair?State.Tuning.LocalRepairSeconds:State.Tuning.LocalRevisionSeconds;
    string Provider(Building? shop)=>shop==null?"global":"company:"+shop.CompanyId;
    // The external owner (Global Reserve) pays itself; nobody else has credit for repairs.
    bool CanPay(Vehicle v,Building? shop,ServiceKind kind)=>Provider(shop)==v.OwnerAccount||v.OwnerAccount=="global"||Spendable(v.OwnerAccount)>=ServicePrice(shop,kind);
    bool PayService(Vehicle v,Building? shop,ServiceKind kind)
    {
        if(Provider(shop)==v.OwnerAccount||v.OwnerAccount=="global")return true;
        var price=ServicePrice(shop,kind);if(Spendable(v.OwnerAccount)<price)return false;
        return Transfer(v.OwnerAccount,Provider(shop),price,kind==ServiceKind.Repair?"Conserto de veículo":"Revisão de veículo",(shop==null?"Serviço mecânico no exterior":"Serviço mecânico na oficina "+shop.Id)+"; trabalho e tempo reais",v.Id);
    }
    bool CanPayRepairSomewhere(Vehicle v)=>CanPay(v,null,ServiceKind.Repair)||State.Buildings.Any(b=>ShopHasRoom(b,v)&&CanPay(v,b,ServiceKind.Repair));
    void ClearService(Vehicle v){v.Service=ServiceKind.None;v.ServicePhase=ServicePhase.None;v.ServiceShopId=null;v.ServiceBay=null;v.ServiceSpot=null;v.ServiceWork=0;v.ServiceUntil=0;}

    // A manoeuvre inside a lot at walking pace; no road traffic involved.
    bool Push(Vehicle v,double dt)
    {
        if(v.PushPath.Count==0)return false;
        var left=State.Tuning.ShopPushSpeed*dt;
        while(left>1e-9&&v.PushPath.Count>0){
            var to=v.PushPath[0];var d=v.Position.Distance(to);
            // It backs into a space whose final heading points against the motion.
            if(d>.01){var dir=to-v.Position;var heading=Math.Atan2(dir.X,dir.Z);if(v.PushHeading is {} final&&Math.Cos(heading-final)<0)heading+=Math.PI;v.Heading=heading;}
            if(d<=left){v.Position=to;v.PushPath.RemoveAt(0);left-=d;}else{v.Position=v.Position.MoveTowards(to,left);left=0;}
        }
        if(v.PushPath.Count==0){if(v.PushHeading is {} heading)v.Heading=heading;v.PushHeading=null;Pushed(v);}
        return true;
    }
    void Pushed(Vehicle v)
    {
        if(v.ServicePhase!=ServicePhase.AwaitingReturn||v.Service!=ServiceKind.Revision)return;
        // Revised: back at the shop's own space (its tow), or the driver takes it home.
        if(IsTow(v)&&v.HomeBuildingId==v.ServiceShopId){ClearService(v);v.Cause="Guincho da oficina revisado e disponível";return;}
        DriveBackFromService(v);
    }

    // Collected once per step in the vehicle pass (no extra scan of all vehicles).
    readonly List<Vehicle> _serviceVehicles=[];
    // Returns true when the vehicle needs nothing else this step.
    bool TendService(Vehicle v,double dt)
    {
        if(v.ServiceShopId!=null)_serviceVehicles.Add(v);
        if(v.Status==VehicleStatus.Outside){
            if(v.ServicePhase==ServicePhase.AtExterior){if(State.ElapsedSeconds>=v.ServiceUntil)FinishExteriorService(v);}
            else if(v.ServicePhase==ServicePhase.AwaitingReturn&&v.TowTruckId==null)TryCallService(v);
            else if(v.External&&v.ServicePhase==ServicePhase.None&&v.Condition<State.Tuning.RevisionThreshold&&v.TowPhase==TowPhase.None&&v.ShipmentId==null&&v.MachineProjectId==null)StartExteriorRevision(v);
            return true;
        }
        if(v.Status!=VehicleStatus.Parked)return false;
        if(Push(v,dt))return true;
        if(v.ServicePhase==ServicePhase.AwaitingMoney&&v.TowTruckId==null)TryCallService(v);
        else if(v.ServicePhase==ServicePhase.AwaitingReturn&&v.Service==ServiceKind.Repair&&v.TowTruckId==null)TryCallService(v);
        return v.ServicePhase!=ServicePhase.None;
    }
    void TryCallService(Vehicle v)
    {
        if(v.TowRoadRevision==State.RoadRevision||State.ElapsedSeconds<v.TowRetryAt)return;
        if(v.ServicePhase==ServicePhase.AwaitingMoney&&!CanPayRepairSomewhere(v)){v.TowRetryAt=State.ElapsedSeconds+State.Tuning.TowRetrySeconds;v.Cause="Quebrado; aguardando dinheiro para o conserto";return;}
        CallTow(v);
    }
    // External vehicles (Global Reserve) are revised at the exterior while idle
    // there, unavailable meanwhile and with no payment (SPEC decision 8A).
    void StartExteriorRevision(Vehicle v)
    {
        v.Service=ServiceKind.Revision;v.ServicePhase=ServicePhase.AtExterior;v.ServiceSince=State.ElapsedSeconds;v.ServiceUntil=State.ElapsedSeconds+State.Tuning.ExteriorRevisionSeconds;v.Cause="Em revisão no exterior";
    }
    // Service at the exterior: paid by the owner to the Global Reserve on arrival.
    void StartExteriorService(Vehicle v,ServiceKind kind)
    {
        v.Position=State.Entry;v.HoldingPosition=null;v.Route.Clear();v.RouteIndex=0;v.DockPosition=null;v.IntersectionClaim=null;v.WaitingSince=0;v.PushPath.Clear();
        if(!PayService(v,null,kind)){
            // SPEC: the city never pays repairs; without the money the vehicle goes back.
            if(kind==ServiceKind.Revision){v.Status=VehicleStatus.Outside;v.Service=kind;FinishExteriorService(v,serviced:false);return;}
            v.Status=VehicleStatus.Outside;v.Service=kind;v.ServicePhase=ServicePhase.AwaitingReturn;v.TowRetryAt=0;v.TowRoadRevision=null;v.Cause="Quebrado no exterior; dono sem dinheiro para o conserto, voltando para a vaga";return;
        }
        v.Status=VehicleStatus.Outside;v.Service=kind;v.ServicePhase=ServicePhase.AtExterior;v.ServiceShopId=null;v.ServiceSince=State.ElapsedSeconds;v.ServiceUntil=State.ElapsedSeconds+ServiceSeconds(null,kind);
        v.Cause=kind==ServiceKind.Repair?"Em conserto no exterior":"Em revisão no exterior";
        foreach(var c in Occupants(v))c.Activity="Aguardando a revisão do veículo no exterior";
    }
    void FinishExteriorService(Vehicle v,bool serviced=true)
    {
        var kind=v.Service;if(serviced){v.Condition=1;v.BrokenDown=false;}
        if(v.External){
            ClearService(v);
            // SPEC decision 3A: with a delivery pending, it comes back driving to finish it.
            if(PendingShipment(v)!=null){ResumeFromExterior(v);return;}
            v.Status=VehicleStatus.Outside;v.Position=State.Entry;v.Cause="Disponível no exterior";return;
        }
        if(kind==ServiceKind.Revision){DriveBackFromService(v);return;}
        // Repaired: the return to the owner's space is a new tow call (SPEC decision 4A).
        v.ServicePhase=ServicePhase.AwaitingReturn;v.TowRetryAt=0;v.TowRoadRevision=null;v.Cause="Consertado no exterior; aguardando guincho para voltar à vaga";
    }

    // The shops work their bays: a waiting vehicle enters a free bay and pays,
    // the work advances only with a mechanic present, and when done the vehicle
    // leaves (revision) or waits in the bay for the tow that takes it home.
    void OperateMechanicShops(double dt,Dictionary<int,List<Citizen>> staff)
    {
        if(_serviceVehicles.Count==0)return;
        foreach(var group in _serviceVehicles.Where(v=>v.ServiceShopId!=null).GroupBy(v=>v.ServiceShopId!.Value).OrderBy(g=>g.Key)){
            var shop=BuildingById(group.Key);var vehicles=group.ToList();
            if(shop==null||!ShopOperates(shop)){foreach(var v in vehicles)LeaveClosedShop(v,shop);continue;}
            var present=staff.TryGetValue(shop.Id,out var workers)?workers.Count(c=>WorkerPresentAt(c,shop)):0;var open=Open(shop);
            var occupied=new Vehicle?[ShopBayCount];foreach(var v in vehicles)if(v.ServiceBay is {} bay&&bay<occupied.Length)occupied[bay]=v;
            foreach(var v in vehicles.Where(v=>v.ServicePhase==ServicePhase.Queued&&v.PushPath.Count==0&&v.Status==VehicleStatus.Parked).OrderBy(v=>v.ServiceSince).ThenBy(v=>v.Id)){
                if(v.ServiceSpot is not {} bay||occupied[bay]!=null){v.Cause=(v.BrokenDown?"Quebrado; ":"")+"aguardando vaga de serviço na oficina "+shop.Id;continue;}
                if(!open||present==0){v.Cause=(v.BrokenDown?"Quebrado; ":"")+"aguardando mecânico na oficina "+shop.Id;continue;}
                if(!PayService(v,shop,v.Service)){NoMoneyAtShop(v,shop);continue;}
                occupied[bay]=v;v.ServiceBay=bay;v.ServiceSpot=null;v.ServicePhase=ServicePhase.InService;v.ServiceWork=ServiceSeconds(shop,v.Service);v.PushPath=[ShopBay(shop,bay)];v.PushHeading=OutwardHeading(shop);
                v.Cause=(v.Service==ServiceKind.Repair?"Em conserto":"Em revisão")+" na oficina "+shop.Id;
            }
            var working=0;
            for(var bay=0;bay<occupied.Length;bay++){
                if(occupied[bay] is not {ServicePhase:ServicePhase.InService} v||v.PushPath.Count>0)continue;
                if(!open||working>=present){v.Cause=(v.Service==ServiceKind.Repair?"Em conserto":"Em revisão")+"; aguardando mecânico na oficina "+shop.Id;continue;}
                working++;v.ServiceWorkUnits=Math.Max(0,FixedPoint.Add(v.ServiceWorkUnits,-dt,FixedPoint.TimeScale));
                v.Cause=(v.Service==ServiceKind.Repair?"Em conserto":"Em revisão")+" na oficina "+shop.Id+" ("+(1-v.ServiceWork/ServiceSeconds(shop,v.Service)).ToString("P0")+")";
                if(v.ServiceWorkUnits==0)ServiceDone(v,shop,bay);
            }
        }
        _serviceVehicles.Clear();
    }
    void ServiceDone(Vehicle v,Building shop,int bay)
    {
        v.Condition=1;v.BrokenDown=false;v.ServicePhase=ServicePhase.AwaitingReturn;
        if(v.Service==ServiceKind.Revision){
            // Out through the bay door to the street side, where the driver takes it.
            v.ServiceBay=null;var own=IsTow(v)&&v.HomeBuildingId==shop.Id;
            v.PushPath=own?[ShopWaiting(shop,bay),ShopTowSpace(shop)]:[ShopWaiting(shop,bay),ServiceDock(shop)];v.PushHeading=own?TowBaseHeading(shop):null;
            v.Cause="Revisado na oficina "+shop.Id+(own?"":"; saindo para o motorista");return;
        }
        // Repaired: it waits in the bay for the tow back to the owner's space (4A).
        v.TowRetryAt=0;v.TowRoadRevision=null;v.Cause="Consertado na oficina "+shop.Id+"; aguardando guincho para voltar à vaga";
    }
    // The shop's street-side point where tows set vehicles down and drivers come in.
    Point ServiceDock(Building shop)=>Access(shop) is {} access?DockAt(access,shop,false):shop.Door;
    void NoMoneyAtShop(Vehicle v,Building shop)
    {
        if(v.Service==ServiceKind.Revision){v.ServicePhase=ServicePhase.AwaitingReturn;v.Cause="Revisão não feita: dono sem dinheiro";v.PushPath=[ServiceDock(shop)];v.ServiceSpot=null;return;}
        // SPEC: without money for the repair the tow leaves it broken at the owner's space.
        v.ServicePhase=ServicePhase.AwaitingReturn;v.TowRetryAt=0;v.TowRoadRevision=null;v.Cause="Quebrado; dono sem dinheiro para o conserto, aguardando guincho para voltar à vaga";
    }
    // A shop that stops operating (bankrupt, closed, demolished) gives the vehicles back.
    void LeaveClosedShop(Vehicle v,Building? shop)
    {
        if(v.ServicePhase is ServicePhase.AwaitingReturn||v.Status!=VehicleStatus.Parked)return;
        v.ServiceBay=null;v.ServiceSpot=null;
        if(v.Service==ServiceKind.Revision&&v.DriverId!=null){v.ServicePhase=ServicePhase.AwaitingReturn;v.Cause="Oficina sem operação; voltando sem revisão";if(shop!=null)v.PushPath=[ServiceDock(shop)];else DriveBackFromService(v);return;}
        v.ServicePhase=ServicePhase.AwaitingReturn;v.Service=v.BrokenDown?ServiceKind.Repair:v.Service;v.TowRetryAt=0;v.TowRoadRevision=null;v.Cause="Oficina sem operação; aguardando guincho para voltar à vaga";
    }

    // A vehicle set down or driven into the shop: it takes the waiting place of a
    // bay (or the bay itself, when free) and joins the queue.
    void EnterShop(Vehicle v,Building shop,ServiceKind kind)
    {
        v.Service=kind;v.ServicePhase=ServicePhase.Queued;v.ServiceShopId=shop.Id;v.ServiceSince=State.ElapsedSeconds;v.Status=VehicleStatus.Parked;v.Route.Clear();v.RouteIndex=0;v.DockPosition=null;v.IntersectionClaim=null;v.WaitingSince=0;
        var taken=State.Vehicles.Where(o=>o.Id!=v.Id&&o.ServiceShopId==shop.Id).ToList();
        int? place=null;for(var bay=0;bay<ShopBayCount&&place==null;bay++)if(taken.All(o=>o.ServiceSpot!=bay&&o.ServiceBay!=bay))place=bay;
        for(var bay=0;bay<ShopBayCount&&place==null;bay++)if(taken.All(o=>o.ServiceSpot!=bay))place=bay;
        v.ServiceSpot=place??0;var spot=v.ServiceSpot.Value;
        v.PushPath=[ShopWaiting(shop,spot)];v.PushHeading=OutwardHeading(shop);
        v.Cause=(v.BrokenDown?"Quebrado; ":"")+"na fila da oficina "+shop.Id;
    }

    // Set down at the owner's space (or base): eased into it; still broken when the
    // owner could not pay the repair, until there is money and the tow is called again.
    void ArriveHome(Vehicle t)
    {
        t.HoldingPosition=null;t.TowResumeNode=null;t.Route.Clear();t.RouteIndex=0;t.DockPosition=null;t.ServiceShopId=null;t.ServiceBay=null;t.ServiceSpot=null;
        if(OwnerHome(t) is {} home&&HomeSpace(t,home) is {} space){if(t.PassengerCar)t.ParkingSpot=ParkingSpots(home).FirstOrDefault(s=>s.Position.Distance(space.Position)<.01).Key;t.PushPath=[space.Position];t.PushHeading=space.Heading;}
        if(t.BrokenDown){t.Service=ServiceKind.Repair;t.ServicePhase=ServicePhase.AwaitingMoney;t.TowRetryAt=State.ElapsedSeconds+State.Tuning.TowRetrySeconds;t.TowRoadRevision=null;t.Cause="Quebrado na vaga do dono; aguardando dinheiro para o conserto";return;}
        ClearService(t);t.Cause=t.PassengerCar?"Estacionado":"Consertado e de volta à base";
    }

    // SPEC 8A: revisions. Reviewed with the economy, not every step.
    void ReviewVehicleRevisions()
    {
        foreach(var v in State.Vehicles){
            if(v.External||v.Status!=VehicleStatus.Parked||v.BrokenDown||v.ServicePhase!=ServicePhase.None||v.Condition>=State.Tuning.RevisionThreshold||v.DriverId!=null||v.TowPhase!=TowPhase.None||v.TowTruckId!=null||v.Machine!=PavingMachine.None||v.PushPath.Count>0)continue;
            var home=OwnerHome(v);if(home==null)continue;
            // The shop's own tow is revised in place, in a free bay of its shop.
            if(IsTow(v)){if(home.Kind==BuildingKind.MechanicShop&&ShopHasRoom(home,v)&&v.Position.Distance(ShopTowSpace(home))<1)EnterShop(v,home,ServiceKind.Revision);continue;}
            Citizen? driver;
            if(v.PassengerCar){
                if(SpotByKey(v.ParkingSpot) is not {} spot||spot.BuildingId!=home.Id||v.Position.Distance(spot.Position)>1)continue;
                driver=FamilyById(home.FamilyId)?.Members.Select(id=>CitizenById(id)).FirstOrDefault(c=>c is {Alive:true,Resident:true,Moving:false,VehicleId:null}&&c.Age>=18&&c.Position.Distance(home.Door)<3&&FreeTime(c));
            }else{
                if(v.ShipmentId!=null||v.Cargo.Values.Sum()>.001)continue;
                driver=State.Citizens.FirstOrDefault(c=>WorkerPresentAt(c,home));
            }
            if(driver==null)continue;
            var shop=State.Buildings.Where(b=>ShopHasRoom(b,v)&&CanPay(v,b,ServiceKind.Revision)).OrderBy(b=>b.Position.Distance(v.Position)).ThenBy(b=>b.Id).FirstOrDefault();
            if(shop==null&&!CanPay(v,null,ServiceKind.Revision))continue;
            StartRevisionTrip(v,driver,shop);
        }
    }
    void StartRevisionTrip(Vehicle v,Citizen driver,Building? shop)
    {
        var target=shop==null?EntryNode():Access(shop);if(target==null||!EnsureFuel(v,target.Id)){v.Route.Clear();v.RouteIndex=0;return;}
        v.Service=ServiceKind.Revision;v.ServicePhase=ServicePhase.ToService;v.ServiceShopId=shop?.Id;
        if(v.RefuellingAt==null)v.DockPosition=shop==null?State.Entry:ServiceDock(shop);
        v.DestinationBuildingId=shop?.Id;v.TripPurpose=JourneyPurpose.Service;v.DriverId=driver.Id;v.Passengers.Clear();v.Status=v.PassengerCar?VehicleStatus.Parked:VehicleStatus.Travelling;
        v.Cause="Indo à revisão "+(shop==null?"no exterior":"na oficina "+shop.Id)+"; aguardando motorista embarcar";
        driver.BoardingVehicleId=v.Id;driver.Purpose=JourneyPurpose.Boarding;driver.Path=[driver.Position,v.Position];driver.PathIndex=0;driver.PathRoadRevision=State.RoadRevision;driver.DestinationId=null;driver.Activity="Levando o veículo à revisão";
        Personal(driver,"Leva o veículo "+v.Id+" à revisão "+(shop==null?"no exterior":"na oficina "+shop.Id)+" (condição "+v.Condition.ToString("P0")+")");
    }
    // Arrived at the shop's street side (or the exterior) on a revision trip.
    void ServiceArrived(Vehicle v)
    {
        var shop=BuildingById(v.ServiceShopId);
        if(shop==null){
            if(v.ServiceShopId!=null){ClearService(v);v.Service=ServiceKind.Revision;DriveBackFromService(v,"Oficina indisponível na chegada; voltando");return;}
            StartExteriorService(v,ServiceKind.Revision);return;
        }
        if(!ShopOperates(shop)){ClearService(v);v.Service=ServiceKind.Revision;DriveBackFromService(v,"Oficina sem operação na chegada; voltando");return;}
        EnterShop(v,shop,ServiceKind.Revision);
        // The driver waits at the shop door (SPEC decision 8A).
        if(CitizenById(v.DriverId) is {} driver&&driver.VehicleId==v.Id){driver.VehicleId=null;driver.Position=v.Position;driver.Path=[v.Position,ShopDriverWait(shop,driver)];driver.PathIndex=0;driver.PathRoadRevision=State.RoadRevision;driver.Purpose=JourneyPurpose.Service;driver.DestinationId=shop.Id;driver.Activity="Levando o veículo à revisão";Personal(driver,"Deixou o veículo "+v.Id+" na oficina "+shop.Id+"; espera na porta");}
    }
    // After a revision (or none) the driver takes the vehicle back to its space.
    void DriveBackFromService(Vehicle v,string? cause=null)
    {
        var home=OwnerHome(v);var driver=CitizenById(v.DriverId);ClearService(v);
        var start=NearestRoad(v.Position,!InMap(v.Position));var target=home==null?null:Access(home);
        if(driver==null||!driver.Alive||home==null||start==null||target==null){v.DriverId=null;v.Status=v.Status==VehicleStatus.Outside?VehicleStatus.Outside:VehicleStatus.Parked;v.Cause=cause??"Revisado; sem motorista ou rota para voltar";if(driver!=null&&driver.VehicleId==v.Id){driver.VehicleId=null;driver.Position=v.Position;}return;}
        var outside=v.Status==VehicleStatus.Outside;if(outside)v.Position=State.Entry;
        if(!EnsureFuel(v,target.Id)){v.Route.Clear();v.RouteIndex=0;v.Status=outside?VehicleStatus.Outside:VehicleStatus.Parked;v.Cause="Revisado; sem combustível para voltar";if(driver.VehicleId==v.Id&&!outside){driver.VehicleId=null;driver.Position=v.Position;}return;}
        v.DestinationBuildingId=home.Id;v.TripPurpose=JourneyPurpose.Home;
        if(v.PassengerCar&&HomeSpace(v,home) is {} space){var key=ParkingSpots(home).FirstOrDefault(s=>s.Position.Distance(space.Position)<.01).Key;v.ParkingSpot=key;if(v.RefuellingAt==null)v.DockPosition=space.Position;}
        else if(v.RefuellingAt==null)v.DockPosition=DockAt(target,home,false);
        v.Cause=cause??"Revisado; voltando";
        if(driver.VehicleId==v.Id){v.Status=VehicleStatus.Travelling;foreach(var c in Occupants(v)){c.Purpose=JourneyPurpose.Home;c.DestinationId=home.Id;c.Activity="Voltando com o veículo revisado";}return;}
        // The driver waiting at the shop door walks to it and drives.
        v.Status=v.PassengerCar?VehicleStatus.Parked:VehicleStatus.Travelling;
        driver.BoardingVehicleId=v.Id;driver.Purpose=JourneyPurpose.Boarding;driver.Path=[driver.Position,v.Position];driver.PathIndex=0;driver.PathRoadRevision=State.RoadRevision;driver.DestinationId=null;driver.Activity="Indo buscar o veículo revisado";
    }
    // A driver waiting at the shop door for the vehicle being revised.
    bool WaitingForService(Citizen c)=>c.Purpose==JourneyPurpose.Service&&State.Vehicles.Any(v=>v.DriverId==c.Id&&v.ServicePhase is ServicePhase.Queued or ServicePhase.InService or ServicePhase.ToService);

    // A local truck brought back to its base with a delivery pending: a worker
    // present resumes the same delivery (SPEC decision 5A).
    void ResumeHeldDeliveries()
    {
        foreach(var v in State.Vehicles){
            if(v.External||v.PassengerCar||IsTow(v)||v.Status!=VehicleStatus.Parked||v.BrokenDown||v.ServicePhase!=ServicePhase.None||v.DriverId!=null||PendingShipment(v) is not {} s)continue;
            var home=BuildingById(v.HomeBuildingId);if(home==null)continue;var driver=State.Citizens.FirstOrDefault(c=>WorkerPresentAt(c,home));if(driver==null)continue;
            var target=s.PickedUp?s.ProjectId is {} project?WorkAccess(State.Projects.First(p=>p.Id==project)):BuildingById(s.DestinationBuildingId) is {} to?Access(to):null:BuildingById(s.SourceBuildingId) is {} from?Access(from):null;
            if(target==null||!EnsureFuel(v,target.Id))continue;
            if(v.RefuellingAt==null)v.DockPosition=DockAt(target,BuildingById(s.PickedUp?s.DestinationBuildingId:s.SourceBuildingId),false);
            v.DriverId=driver.Id;v.Status=VehicleStatus.Travelling;v.Cause="Retomando a entrega depois do conserto";
            driver.BoardingVehicleId=v.Id;driver.Purpose=JourneyPurpose.Boarding;driver.DestinationId=null;driver.Path=[driver.Position,v.Position];driver.PathIndex=0;driver.PathRoadRevision=State.RoadRevision;driver.Activity="Caminhando para retomar a entrega";
        }
    }

    // The shop company buys its platform tow with real money, imported and
    // delivered physically like the farm truck (SPEC "Guincho local").
    void AcquireShopTow(Building shop)
    {
        if(!ShopOperates(shop)||State.Vehicles.Any(v=>IsTow(v)&&v.HomeBuildingId==shop.Id&&v.OwnerAccount=="company:"+shop.CompanyId)||State.Shipments.Any(s=>s.Purpose==ShipmentPurpose.Vehicle&&s.DestinationBuildingId==shop.Id&&!s.Complete))return;
        var account="company:"+shop.CompanyId;var fuel=State.Tuning.FuelTank;var price=State.Tuning.TowTruckPrice+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]+State.Tuning.FreightBase;
        if(Spendable(account)<price+500||State.OutsideStock[ResourceKind.Fuel]<fuel)return;var access=Access(shop)!;var route=FindRoadRoute(EntryNode().Id,access.Id);if(route.Count==0)return;
        if(RouteLength(route)*State.Tuning.FuelPerMetre+4>fuel){if(CompanyById(shop.CompanyId) is {} owner)owner.Decision="Guincho não comprado: a entrega desde a entrada passa da autonomia do tanque";return;}
        if(!Transfer(account,"global",price,"Veículos","Aquisição/importação real do guincho da oficina e combustível inicial",shop.Id))return;State.OutsideStock.AddAmount(ResourceKind.Fuel,-(fuel));
        var v=new Vehicle{Id=Id(),Body=TruckBody.Tow,OwnerAccount=account,External=true,ExternalDriverId="entregador externo de veículo "+shop.Id,HomeBuildingId=shop.Id,Position=State.Entry,Fuel=fuel,CargoCapacity=0,Route=route,DockPosition=ShopTowSpace(shop),Status=VehicleStatus.Travelling,Cause="Guincho comprado pela oficina em entrega"};
        var s=new Shipment{Id=Id(),Purpose=ShipmentPurpose.Vehicle,DestinationBuildingId=shop.Id,VehicleId=v.Id,PickedUp=true,PricePaid=price,GoodsOwnerAccount=account};v.ShipmentId=s.Id;State.Vehicles.Add(v);State.Shipments.Add(s);
        if(CompanyById(shop.CompanyId) is {} company)company.Decision="Comprou guincho de plataforma importado";
    }
    // The shop's tow back to its own space at the shop.
    void ReturnLocalTow(Vehicle tow)
    {
        tow.ShipmentId=null;tow.TowExitWaitSince=0;tow.TowExitNoOther=false;
        var shop=BuildingById(tow.HomeBuildingId);var target=shop==null?null:Access(shop);var start=NearestRoad(tow.Position);
        if(shop==null||target==null||start==null){tow.Status=VehicleStatus.Waiting;tow.Cause="Guincho sem oficina com acesso para voltar";return;}
        if(!EnsureFuel(tow,target.Id)){tow.Route.Clear();tow.RouteIndex=0;tow.Status=VehicleStatus.Waiting;tow.Cause="Guincho sem combustível ou rota para voltar à oficina";return;}
        if(tow.RefuellingAt==null)tow.DockPosition=ShopTowSpace(shop);tow.DestinationBuildingId=shop.Id;tow.Status=VehicleStatus.Travelling;tow.Cause="Guincho voltando à oficina "+shop.Id;
    }
    // The shop's tow refuels while idle when its tank no longer covers a full service.
    void RefuelIdleShopTows()
    {
        foreach(var tow in State.Vehicles){
            if(!IsTow(tow)||tow.External||tow.Status!=VehicleStatus.Parked||tow.TowPhase!=TowPhase.None||tow.DriverId!=null||tow.BrokenDown||tow.ServicePhase!=ServicePhase.None||tow.Fuel>=State.Tuning.FuelTank*.6)continue;
            var shop=BuildingById(tow.HomeBuildingId);if(shop==null||Access(shop) is not {} access)continue;var driver=State.Citizens.FirstOrDefault(c=>WorkerPresentAt(c,shop));if(driver==null)continue;
            var station=State.Buildings.Where(b=>StationOperates(b)&&Access(b)!=null).OrderBy(b=>b.Position.Distance(shop.Position)).FirstOrDefault();if(station==null)continue;
            var there=FindRoadRoute(access.Id,Access(station)!.Id);var back=FindRoadRoute(Access(station)!.Id,access.Id);if(there.Count==0||back.Count==0||RouteLength(there)*FuelRate(tow)>tow.Fuel)continue;
            tow.Route=there;tow.RouteIndex=0;tow.SavedRoute=back;tow.RefuellingAt=station.Id;tow.DockPosition=DockAt(Access(station)!,station,false);tow.DriverId=driver.Id;tow.DestinationBuildingId=shop.Id;tow.Status=VehicleStatus.Travelling;tow.Cause="Guincho indo abastecer no posto "+station.Id;
            driver.BoardingVehicleId=tow.Id;driver.Purpose=JourneyPurpose.Boarding;driver.DestinationId=null;driver.Path=[driver.Position,tow.Position];driver.PathIndex=0;driver.PathRoadRevision=State.RoadRevision;driver.Activity="Levando o guincho para abastecer";
        }
    }

    // Presentation (read-only): the vehicle on each bay of a shop, and a short
    // service stage for the inspection.
    public int?[] ServiceBays(Building shop)
    {
        var bays=new int?[ShopBayCount];foreach(var v in State.Vehicles)if(v.ServiceShopId==shop.Id&&v.ServiceBay is {} bay&&bay<bays.Length)bays[bay]=v.Id;return bays;
    }
    public int BrokenAwaitingTow=>State.Vehicles.Count(v=>v.BrokenDown&&v.Status==VehicleStatus.Stranded);
    public string ServiceStage(Vehicle v)
    {
        if(v.Status==VehicleStatus.Towed&&State.Vehicles.FirstOrDefault(t=>t.Id==v.TowTruckId) is {} tow)return "Sendo rebocado pelo guincho "+tow.Id+(tow.TowDestination switch{TowDestination.Shop=>" à oficina "+tow.TowStationId,TowDestination.Home=>" à vaga do dono",TowDestination.Station=>" ao posto "+tow.TowStationId,TowDestination.Exterior=>" ao exterior",_=>" ao espaço de espera"});
        if(v.BrokenDown&&v.Status==VehicleStatus.Stranded)return v.TowTruckId is {} id?"Quebrado; guincho "+id+" a caminho":"Quebrado; aguardando guincho";
        return v.ServicePhase switch{
            ServicePhase.ToService=>"A caminho da revisão",
            ServicePhase.Queued=>(v.BrokenDown?"Quebrado; ":"")+"na fila da oficina "+v.ServiceShopId,
            ServicePhase.InService=>(v.Service==ServiceKind.Repair?"Em conserto":"Em revisão")+" na oficina "+v.ServiceShopId+" (vaga "+(v.ServiceBay==0?"do elevador":"do piso")+")",
            ServicePhase.AtExterior=>v.Service==ServiceKind.Repair?"Em conserto no exterior":"Em revisão no exterior",
            ServicePhase.AwaitingReturn=>v.TowTruckId is {} id?"Aguardando o guincho "+id+" para voltar à vaga":v.Service==ServiceKind.Revision?"Revisado; saindo da oficina":"Aguardando guincho para voltar à vaga",
            ServicePhase.AwaitingMoney=>"Quebrado; aguardando dinheiro para o conserto",
            _=>v.BrokenDown?"Quebrado":""};
    }
}
