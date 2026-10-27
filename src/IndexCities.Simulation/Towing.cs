namespace IndexCities.Simulation;

// SPEC transporte-circulacao, "Socorro a veículos parados": a vehicle out of
// fuel on the road calls a tow from the exterior at once (one tow per call).
// The tow drives in, stops in front of it on the free side, carries it and the
// people aboard to the nearest operating fuel station (or to the exterior when
// no station operates) and returns; the owner pays, the city covers a shortfall
// and the owner owes it to the city (A1).
public sealed partial class CitySimulation
{
    // Body half-lengths along the lane (calibration of the kit geometry). A heavy
    // tow lifts the truck's front and pulls it behind, the pair taking the summed length;
    // the trail (6.5 m) is shorter than the half-lengths because the truck's front
    // axle sits on the lift behind the tow (kit: LiftAnchor 3.9 m back, axle 2.55 m
    // ahead of the truck origin).
    const double TowHalfLength=3.6,HeavyTowHalfLength=4.0,TruckHalfLength=3.6,CarHalfLength=2.2,HeavyTowTrail=6.5;
    // A waiting tow with no delivery plan looks again after this many seconds.
    const double TowReplanSeconds=10;
    static bool IsTow(Vehicle v)=>v.Body is TruckBody.Tow or TruckBody.TowHeavy;
    // A truck trailing a heavy tow still occupies the lane behind it.
    static bool OccupiesLane(Vehicle o)=>o.Status is VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded||o.Status==VehicleStatus.Towed&&!o.PassengerCar;
    static bool PeopleAboard(Vehicle v)=>v.DriverId!=null||v.Passengers.Count>0;
    // External trucks always keep their (non-SIM) driver aboard.
    static bool Crewed(Vehicle v)=>PeopleAboard(v)||v.External;
    IEnumerable<Citizen> Occupants(Vehicle v)=>State.Citizens.Where(c=>c.VehicleId==v.Id);
    bool OnRoadway(Vehicle v)=>OnRoadway(v.Position);
    bool OnRoadway(Point at)=>NearestRoad(at) is {} n&&n.Position.Distance(at)<=State.Tuning.RoadWidth/2+1;
    bool AtPumpBay(Vehicle v)=>v.RefuellingAt is {} id&&BuildingById(id) is {} station&&Access(station) is {} access&&v.Position.Distance(DockAt(access,station,false))<3;
    bool Towable(Vehicle v)=>(!IsTow(v)||v.BrokenDown)&&v.Machine==PavingMachine.None&&!AtPumpBay(v)&&OnRoadway(v);
    // Waiting for its tow: stopped on the road, or parked off it (left at the
    // owner's space, at a shop after the repair, or at the exterior).
    static bool AwaitsTow(Vehicle t)=>t.Status is VehicleStatus.Stranded or VehicleStatus.Parked or VehicleStatus.Outside;
    // Where a tow returns after a service: the exterior, or its shop.
    RoadNode? TowHomeNode(Vehicle tow)=>tow.External?EntryNode():BuildingById(tow.HomeBuildingId) is {} shop?Access(shop):null;
    void ReturnTow(Vehicle tow){if(tow.External)ReturnExternal(tow);else ReturnLocalTow(tow);}
    static string TowPrefix(Vehicle t)=>t.BrokenDown?"Quebrado":t.ServicePhase==ServicePhase.AwaitingReturn?"Consertado":"Sem combustível";
    static Point Forward(double heading)=>new(Math.Sin(heading),Math.Cos(heading));

    void RunOutOfFuel(Vehicle v)
    {
        v.Status=VehicleStatus.Stranded;v.Cause="Sem combustível; nenhuma viagem fictícia";
        // The call itself happens in the next operational update ("na hora").
        v.TowCalledAt=State.ElapsedSeconds;v.TowRetryAt=0;v.TowRoadRevision=null;
    }
    void TendStranded(Vehicle t)
    {
        if(t.TowTruckId!=null){
            if(PeopleAboard(t)&&State.ElapsedSeconds-t.TowCalledAt>State.Tuning.TowMaxWaitSeconds){var tow=t.TowTruckId;Disembark(t,"Guincho demorou mais que o limite de espera");t.Cause=$"{TowPrefix(t)}; aguardando guincho {tow}";}
            return;
        }
        if(!Towable(t)){if(t.PassengerCar&&PeopleAboard(t))LeaveCar(t,t.BrokenDown?"Carro quebrado parado na via":"Carro sem combustível parado na via");return;}
        if(t.TowRoadRevision==State.RoadRevision||State.ElapsedSeconds<t.TowRetryAt)return;
        CallTow(t);
    }
    void Disembark(Vehicle t,string cause)
    {
        if(t.PassengerCar){if(PeopleAboard(t))LeaveCar(t,cause);return;}
        if(t.DriverId is {} id&&State.Citizens.FirstOrDefault(c=>c.Id==id) is {} driver){Personal(driver,cause+"; segue a pé");ReleaseDriver(t);}
    }
    // Lane points ahead along the stranded vehicle's own route, so a bend ahead
    // keeps the tow on the carriageway; a cleared route continues straight.
    bool PickupPose(Vehicle t,double gap,out Point pose,out RoadNode? node)
    {
        pose=default;node=null;var points=new List<Point>{t.Position};
        for(var i=Math.Max(t.RouteIndex,0);i<t.Route.Count;i++){
            if(!_roads.TryGetValue(t.Route[i],out var n))break;
            var previous=i>0&&_roads.TryGetValue(t.Route[i-1],out var p)?p.Position:t.Position;var axis=n.Position-previous;var length=axis.Distance(default);
            points.Add(length>.01?n.Position+new Point(axis.Z/length*1.65,-axis.X/length*1.65)+InnerCornerOffset(t.Route,i,n,axis,length):n.Position);
        }
        var left=gap;pose=t.Position+Forward(t.Heading)*gap;
        for(var i=1;i<points.Count;i++){var d=points[i-1].Distance(points[i]);if(d>=left){pose=points[i-1].MoveTowards(points[i],left);break;}left-=d;if(i==points.Count-1){var dir=d>.01?(points[i]-points[i-1])*(1/d):Forward(t.Heading);pose=points[i]+dir*left;}}
        var avoid=StrandedRoads(t);var at=pose;
        node=State.Roads.Where(r=>!avoid.Contains(r.Id)&&!r.ConstructionOnly&&r.MaintenanceRatio>.05).MinBy(r=>r.Position.Distance(at));
        return node!=null&&node.Position.Distance(pose)<=State.Tuning.RoadWidth/2+1;
    }
    HashSet<int> StrandedRoads(Vehicle t)=>State.Roads.Where(r=>r.Position.Distance(t.Position)<State.Tuning.RoadStep+.5).Select(r=>r.Id).ToHashSet();
    static double PickupGap(Vehicle t)=>t.PassengerCar?CarHalfLength+TowHalfLength+1:HeavyTowTrail;

    // Roads a tow plans around: under stranded vehicles, and junctions held by a
    // stranded vehicle or by one stuck behind it (they stay locked until a tow
    // clears them). Queues themselves block one lane only and are not closed.
    HashSet<int> TowClosedRoads(Vehicle? tow)
    {
        var closed=new HashSet<int>();
        foreach(var o in State.Vehicles){
            if(o.Id==tow?.Id)continue;
            if(o.Status==VehicleStatus.Stranded)closed.UnionWith(StrandedRoads(o));
            if(o.IntersectionClaim is {} held&&(o.Status==VehicleStatus.Stranded||o.Status==VehicleStatus.Travelling&&o.WaitingSince>0&&HeldBehindStranded(o))){closed.Add(held);if(_junctionAreas.TryGetValue(held,out var area))closed.UnionWith(area);}
        }
        return closed;
    }
    // A route around the closed roads when one exists, otherwise the plain route;
    // the strictly closed roads (a queue to stay out of) are never taken.
    List<int> TowRoute(int from,int to,HashSet<int> closed,HashSet<int>? strict=null)
    {
        HashSet<int> Open(HashSet<int> set)=>set.Contains(from)||set.Contains(to)?set.Where(id=>id!=from&&id!=to).ToHashSet():set;
        var route=FindRoadRoute(from,to,false,Open(strict==null?closed:closed.Union(strict).ToHashSet()));if(route.Count>0)return route;
        return strict==null?FindRoadRoute(from,to):FindRoadRoute(from,to,false,Open(strict));
    }
    sealed record TowPlan(TowDestination Kind,Building? Station,Point Dock,List<int> Route,double Back);
    // A station that operates: open for business with fuel to sell.
    static bool StationOperates(Building b)=>b.Kind==BuildingKind.FuelStation&&b.Completed&&!b.Removed&&!b.OperationClosed&&b.CompanyId!=null&&b.OperationalCapacity>0&&b.Quantity(ResourceKind.Fuel)>0;
    // The nearest operating station by road within the fuel; without one (none
    // operates, or none fits the fuel left) the exterior the tow came from, except
    // for a vehicle nobody is aboard, which waits off the road by the highway.
    // A broken vehicle (SPEC "Quebras, guincho local e oficina") goes to the nearest
    // local shop with room whose repair the owner can pay, otherwise to the exterior
    // (external trucks always); without money for any repair, to the owner's space.
    // A repaired vehicle returns to the owner's space. `home` is where the tow
    // returns afterwards (the exterior, or its shop).
    TowPlan? PlanTowDelivery(Vehicle t,RoadNode start,double fuel,double travelled,HashSet<int> closed,HashSet<int>? strict=null,RoadNode? home=null)
    {
        var entry=EntryNode();home??=entry;bool Fits(List<int> route,double back)=>(travelled+RouteLength(route)+back)*State.Tuning.FuelPerMetre+4<=fuel;
        double Back(RoadNode from)=>from.Id==home.Id?0:FindRoadRoute(from.Id,home.Id) is {Count:>0} back?RouteLength(back):double.PositiveInfinity;
        TowPlan? To(TowDestination kind,Building? building,RoadNode access,Point dock){var route=TowRoute(start.Id,access.Id,closed,strict);if(route.Count==0)return null;var back=Back(access);return double.IsFinite(back)&&Fits(route,back)?new(kind,building,dock,route,back):null;}
        TowPlan? Holding(){var slot=TowHoldingSlot(t);return slot.Road==null?null:To(TowDestination.Holding,null,slot.Road,slot.Position);}
        TowPlan? HomePlan()=>OwnerHome(t) is {} b&&Access(b) is {} a?To(TowDestination.Home,b,a,DockAt(a,b,false)):Holding();
        TowPlan? Exterior()=>To(TowDestination.Exterior,null,entry,State.Entry);
        if(t.ServicePhase==ServicePhase.AwaitingReturn)return HomePlan();
        if(t.BrokenDown){
            if(!t.External){
                TowPlan? shop=null;
                foreach(var b in State.Buildings.Where(b=>ShopHasRoom(b,t)&&CanPay(t,b,ServiceKind.Repair)))if(ServiceAccess(b) is {} access&&To(TowDestination.Shop,b,access,ServiceDock(b)) is {} option&&(shop==null||RouteLength(option.Route)<RouteLength(shop.Route)))shop=option;
                if(shop!=null)return shop;
                if(!CanPay(t,null,ServiceKind.Repair))return HomePlan();
            }
            return Exterior();
        }
        TowPlan? best=null;
        foreach(var station in State.Buildings.Where(StationOperates)){
            if(Access(station) is not {} access)continue;
            if(To(TowDestination.Station,station,access,DockAt(access,station,false)) is {} option&&(best==null||RouteLength(option.Route)<RouteLength(best.Route)))best=option;
        }
        if(best!=null)return best;
        return Crewed(t)?Exterior():Holding();
    }
    // AwaitingMoney at the owner's space: a tow back to the same place is no service.
    static bool WorthTowing(Vehicle t,TowPlan plan)
    {
        if(t.ServicePhase!=ServicePhase.AwaitingMoney||plan.Kind is not (TowDestination.Home or TowDestination.Holding))return true;
        t.Cause="Quebrado; aguardando dinheiro para o conserto";return false;
    }
    // Same physical waiting spaces beside the highway as preserved municipal cargo.
    (RoadNode? Road,Point Position) TowHoldingSlot(Vehicle vehicle)
    {
        var candidates=State.Roads.Where(r=>r.Highway).OrderBy(r=>r.Position.Distance(State.Entry)).Where((road,index)=>index>=2&&index%2==0).Select(road=>(Road:(RoadNode?)road,Position:road.Position+new Point(0,-State.Tuning.RoadWidth/2-1.5)));
        return candidates.FirstOrDefault(candidate=>State.Buildings.All(b=>b.Removed||!Inside(b,candidate.Position,4))&&State.Vehicles.All(other=>other.Id==vehicle.Id||other.HoldingPosition is not {} reserved||reserved.Distance(candidate.Position)>=8));
    }
    Vehicle TowTruck(bool heavy)
    {
        // One tow per call: an idle one at the exterior or a new one; no fleet cap.
        var body=heavy?TruckBody.TowHeavy:TruckBody.Tow;
        if(State.Vehicles.FirstOrDefault(v=>v.Body==body&&v.External&&v.Status==VehicleStatus.Outside&&v.TowPhase==TowPhase.None&&v.ServicePhase==ServicePhase.None&&!v.BrokenDown&&v.ShipmentId==null) is {} idle)return idle;
        var tow=new Vehicle{Id=Id(),Body=body,External=true,ExternalDriverId=heavy?"guincheiro externo (pesado)":"guincheiro externo",Status=VehicleStatus.Outside,Position=State.Entry,CargoCapacity=0,Cause="Disponível no exterior"};
        State.Vehicles.Add(tow);Pr57DirtyTouch(tow);return tow;
    }
    void CallTow(Vehicle t)
    {
        t.TowRetryAt=State.ElapsedSeconds+State.Tuning.TowRetrySeconds;if(t.TowCalledAt<=0)t.TowCalledAt=State.ElapsedSeconds;
        if(!t.BrokenDown&&t.ServicePhase==ServicePhase.None)t.TowResumeNode??=t.SavedRoute.Count>0?t.SavedRoute[^1]:t.Route.Count>0?t.Route[^1]:null;
        void Impossible(string reason){t.TowRoadRevision=State.RoadRevision;Disembark(t,"Guincho impossível: "+reason);t.Cause=TowPrefix(t)+"; guincho impossível: "+reason+" (novo chamado quando a malha mudar)";}
        var entry=EntryNode();
        if(!TowPickup(t,out var pose,out var pickup)){Impossible(t.Status==VehicleStatus.Stranded?"sem espaço na via à frente do veículo":"sem acesso viário ao veículo");return;}
        var closed=TowClosedRoads(null);
        // SPEC "Guincho local": the shop's own tow serves car calls when free.
        if(t.PassengerCar&&TryLocalTow(t,pose,pickup!,closed))return;
        var toVehicle=TowRoute(entry.Id,pickup!.Id,closed);
        if(toVehicle.Count==0){Impossible("sem ligação viária com o exterior");return;}
        var plan=PlanTowDelivery(t,pickup,State.Tuning.FuelTank,RouteLength(toVehicle),closed);
        if(plan==null){Impossible("viagem fora da autonomia do guincho ou sem destino com acesso");return;}
        if(!WorthTowing(t,plan))return;
        var tow=TowTruck(!t.PassengerCar);var fuel=Math.Max(0,State.Tuning.FuelTank-tow.Fuel);
        if(State.OutsideStock[ResourceKind.Fuel]<fuel){t.Cause=TowPrefix(t)+"; guincho aguardando combustível no exterior";return;}
        var metres=RouteLength(toVehicle)+RouteLength(plan.Route)+plan.Back;
        var fee=Money(State.Tuning.TowServiceBase+(decimal)metres*State.Tuning.FreightPerMetre+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]);
        // One paid service per rescue: a tow sent back (fuel no longer covering a
        // replanned trip) is replaced at no new charge.
        if(!t.TowPrepaid)ChargeOwner(t,fee,"Guincho","Reboque físico: frete e combustível do guincho");
        t.TowPrepaid=true;
        State.OutsideStock.AddAmount(ResourceKind.Fuel,-(fuel));tow.AddFuel(fuel);
        _pr57AxisSweepDirty=true;
        tow.Position=State.Entry;DispatchTow(tow,t,toVehicle,pose,plan);
        foreach(var c in Occupants(t)){c.Activity="Aguardando guincho";Personal(c,$"Veículo parado na via; guincho {tow.Id} chamado do exterior ({fee})");}
    }
    void DispatchTow(Vehicle tow,Vehicle t,List<int> toVehicle,Point pose,TowPlan plan)
    {
        tow.Route=toVehicle;tow.RouteIndex=0;tow.DockPosition=pose;tow.Detour.Clear();tow.WaitingSince=0;tow.IntersectionClaim=null;tow.Status=VehicleStatus.Travelling;
        tow.TowPhase=TowPhase.ToVehicle;tow.TowTargetId=t.Id;tow.TowDestination=plan.Kind;tow.TowStationId=plan.Station?.Id;tow.Cause=$"Guincho a caminho do veículo {t.Id}";
        t.TowTruckId=tow.Id;t.TowRoadRevision=null;AssignTowDestination(t,plan);t.Cause=$"{TowPrefix(t)}; aguardando guincho {tow.Id}";
        Pr57DirtyTouch(tow);Pr57DirtyTouch(t);
    }
    // A vehicle bound for a shop counts against its room from the call on; one
    // still at a shop (repaired) keeps its place there until loaded.
    static void AssignTowDestination(Vehicle t,TowPlan plan)
    {
        if(t.ServicePhase==ServicePhase.AwaitingReturn&&t.Status!=VehicleStatus.Towed)return;
        t.ServiceShopId=plan.Kind==TowDestination.Shop?plan.Station!.Id:null;
    }
    // On the road: in front of the vehicle on the free side. Off the road: at the
    // street side of the lot where it stands (the exterior gateway when outside).
    bool TowPickup(Vehicle t,out Point pose,out RoadNode? node)
    {
        if(t.Status==VehicleStatus.Stranded)return PickupPose(t,PickupGap(t),out pose,out node);
        pose=default;node=null;
        if(t.Status==VehicleStatus.Outside){node=EntryNode();pose=State.Entry;return true;}
        var building=BuildingById(t.ServiceShopId)??(OwnerHome(t) is {} home&&home.Position.Distance(t.Position)<40?home:null);
        if(building!=null&&Access(building) is {} access){node=access;pose=DockAt(access,building,false);}
        else if(NearestRoad(t.Position,false) is {} near){node=near;pose=DockAt(near,null,false);}
        else return false;
        if(pose.Distance(t.Position)<PickupGap(t)){
            // Clear of the vehicle itself, along the street.
            var road=node;var alongX=road.Highway||road.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-road.Position.X)>.1);
            pose+=(alongX?new Point(1,0):new Point(0,1))*PickupGap(t);
        }
        return true;
    }
    // SPEC "Guincho local" (decision 6A): the shop's own platform tow takes a car
    // call when free, with a shop worker present to drive it and fuel for the
    // whole service (to the car, to the destination and back to the shop).
    bool TryLocalTow(Vehicle t,Point pose,RoadNode pickup,HashSet<int> closed)
    {
        foreach(var shop in State.Buildings.Where(ShopOperates).OrderBy(b=>b.Position.Distance(t.Position)).ThenBy(b=>b.Id)){
            var tow=State.Vehicles.FirstOrDefault(v=>IsTow(v)&&!v.External&&v.HomeBuildingId==shop.Id&&v.OwnerAccount=="company:"+shop.CompanyId&&v.Status==VehicleStatus.Parked&&v.TowPhase==TowPhase.None&&!v.BrokenDown&&v.ServicePhase==ServicePhase.None&&v.DriverId==null&&v.PushPath.Count==0);
            if(tow==null)continue;var driver=State.Citizens.FirstOrDefault(c=>WorkerPresentAt(c,shop));if(driver==null)continue;
            var origin=Access(shop)!;var toVehicle=TowRoute(origin.Id,pickup.Id,closed);if(toVehicle.Count==0)continue;
            var plan=PlanTowDelivery(t,pickup,tow.Fuel,RouteLength(toVehicle)+tow.Position.Distance(origin.Position),closed,null,origin);if(plan==null)continue;
            if(!WorthTowing(t,plan))return true;
            var metres=RouteLength(toVehicle)+RouteLength(plan.Route)+plan.Back;var fee=Money(State.Tuning.LocalTowBase+(decimal)metres*State.Tuning.FreightPerMetre);
            if(!t.TowPrepaid)ChargeOwner(t,fee,"Guincho","Reboque pelo guincho da oficina "+shop.Id,"company:"+shop.CompanyId);
            t.TowPrepaid=true;DispatchTow(tow,t,toVehicle,pose,plan);tow.Cause=$"Guincho da oficina {shop.Id} a caminho do veículo {t.Id}";
            tow.DriverId=driver.Id;Pr57CitizenAgendaWake(driver);driver.BoardingVehicleId=tow.Id;driver.Purpose=JourneyPurpose.Boarding;driver.DestinationId=null;driver.Path=[driver.Position,tow.Position];driver.PathIndex=0;driver.PathRoadRevision=State.RoadRevision;driver.Activity="Indo dirigir o guincho da oficina";
            Personal(driver,$"Vai dirigir o guincho {tow.Id} até o veículo {t.Id}");
            foreach(var c in Occupants(t)){c.Activity="Aguardando guincho";Personal(c,$"Veículo parado na via; guincho {tow.Id} da oficina {shop.Id} chamado ({fee})");}
            return true;
        }
        return false;
    }
    // SPEC A1: the owner pays what it can to the provider (the Global Reserve, or
    // the shop company for its own tow); the city pays the rest and the owner owes
    // it to the city. What the city's available cash cannot cover becomes the
    // city's debt with the provider (existing recovery).
    void ChargeOwner(Vehicle t,decimal due,string category,string reason,string provider="global")
    {
        var owner=t.OwnerAccount;var paid=Pay(owner,provider,due,category,reason,t.Id);var unpaid=Money(due-paid);if(unpaid<=0)return;
        var cityPaid=owner=="city"?0:Pay("city",provider,unpaid,category,reason+"; pago pela Prefeitura (dono sem dinheiro)",t.Id);
        if(owner!="city")Owe(owner,"city",unpaid,category,null);
        Owe("city",provider,unpaid-cityPaid,category,null);
    }
    bool OperateTow(Vehicle tow,double dt)
    {
        if(tow.TowPhase==TowPhase.None){
            // The shop's tow that found no way back (access or fuel) tries again.
            if(!tow.External&&tow.Status==VehicleStatus.Waiting&&tow.Route.Count==0&&tow.ServicePhase==ServicePhase.None&&!tow.BrokenDown&&State.ElapsedSeconds>=tow.TowRetryAt&&BuildingById(tow.HomeBuildingId) is {Kind:BuildingKind.MechanicShop})ReturnLocalTow(tow);
            return false;
        }
        var t=State.Vehicles.FirstOrDefault(o=>o.Id==tow.TowTargetId);
        Pr57DirtyTouch(t); // target may be changed by loading, delivery or abort
        if(t==null||t.TowTruckId!=tow.Id||tow.TowPhase is TowPhase.ToVehicle or TowPhase.Loading&&!AwaitsTow(t)){AbortTow(tow,t);return true;}
        switch(tow.TowPhase){
            case TowPhase.Loading:tow.AddLoadTimer(-(dt));if(tow.LoadTimer<=0)LoadTowed(tow,t);return true;
            case TowPhase.Unloading:tow.AddLoadTimer(-(dt));if(tow.LoadTimer<=0)DeliverTowed(tow,t);return true;
        }
        // SPEC waiting limit: people still stuck on a tow that cannot move go on foot.
        if(tow.TowPhase==TowPhase.Delivering&&PeopleAboard(t)&&State.ElapsedSeconds-t.TowCalledAt>State.Tuning.TowMaxWaitSeconds&&(tow.Status==VehicleStatus.Waiting||tow.WaitingSince>0&&VehicleMovementTime-tow.WaitingSince>=State.Tuning.UTurnWaitSeconds)){Disembark(t,"Guincho parado além do limite de espera");t.Status=VehicleStatus.Towed;t.Cause="Sendo rebocado; pessoas seguiram a pé";}
        if(tow.Status==VehicleStatus.Waiting){if(State.ElapsedSeconds>=tow.TowRetryAt)ReplanTow(tow,t);return true;}
        return false;
    }
    // SPEC: the tow never goes on without fuel for the rest of its trip. After a
    // replan that no longer fits, it returns and the same call gets another tow;
    // a loaded tow picks a destination it can still reach.
    void CheckTowFuel(Vehicle tow)
    {
        if(State.Vehicles.FirstOrDefault(o=>o.Id==tow.TowTargetId) is not {} t||tow.Route.Count==0)return;
        Pr57DirtyTouch(t);
        var ahead=tow.Route.GetRange(Math.Max(0,tow.RouteIndex-1),tow.Route.Count-Math.Max(0,tow.RouteIndex-1));var remaining=RouteLength(ahead)+(ahead.Count>0&&_roads.TryGetValue(ahead[0],out var first)?first.Position.Distance(tow.Position):0);
        var home=TowHomeNode(tow)??EntryNode();
        if(tow.TowPhase==TowPhase.ToVehicle){if(!_roads.TryGetValue(tow.Route[^1],out var pickup)||PlanTowDelivery(t,pickup,tow.Fuel,remaining,TowClosedRoads(tow),null,home)==null){AbortTow(tow,t);t.Cause=TowPrefix(t)+"; outro guincho a caminho (o anterior voltou para reabastecer)";}return;}
        if(tow.TowPhase!=TowPhase.Delivering)return;
        var back=tow.Route[^1]==home.Id?0:RouteLength(FindRoadRoute(tow.Route[^1],home.Id));
        if((remaining+back)*State.Tuning.FuelPerMetre+4>tow.Fuel)StartDelivery(tow,t);
    }
    void AbortTow(Vehicle tow,Vehicle? t)
    {
        Pr57DirtyTouch(t);
        if(t!=null&&t.TowTruckId==tow.Id)t.TowTruckId=null;
        tow.TowPhase=TowPhase.None;tow.TowTargetId=null;tow.TowStationId=null;tow.TowExitWaitSince=0;tow.TowExitNoOther=false;ReturnTow(tow);
    }
    void ReplanTow(Vehicle tow,Vehicle t)
    {
        var start=NearestRoad(tow.Position);if(start==null)return;
        if(tow.TowPhase==TowPhase.ToVehicle){
            if(!TowPickup(t,out var pose,out var pickup)){tow.Cause="Guincho aguardando espaço na via à frente do veículo "+t.Id;return;}
            var route=TowRoute(start.Id,pickup!.Id,TowClosedRoads(tow));
            if(route.Count==0){tow.Cause="Guincho aguardando rota até o veículo "+t.Id;return;}
            tow.Route=route;tow.RouteIndex=0;tow.DockPosition=pose;tow.Status=VehicleStatus.Travelling;tow.Cause=$"Guincho a caminho do veículo {t.Id}";CheckTowFuel(tow);return;
        }
        StartDelivery(tow,t);
    }
    // Departure from wherever the tow stands: the nearby road sample (not under a
    // stranded vehicle or a locked junction) that gives the shortest way out, with
    // the sample before it so the lane is known and it can still turn back.
    bool StartDelivery(Vehicle tow,Vehicle t,HashSet<int>? avoid=null)
    {
        Pr57DirtyTouch(t);
        var closed=TowClosedRoads(tow);TowPlan? best=null;RoadNode? from=null;double cost=double.MaxValue;var home=TowHomeNode(tow)??EntryNode();
        var near=State.Roads.Where(r=>!r.ConstructionOnly&&r.Position.Distance(tow.Position)<=State.Tuning.RoadWidth).OrderBy(r=>closed.Contains(r.Id)?1:0).ThenBy(r=>r.Position.Distance(tow.Position)).ToList();
        if(near.Count==0&&NearestRoad(tow.Position) is {} nearest)near.Add(nearest);
        foreach(var node in near){
            if(best!=null&&closed.Contains(node.Id))break;
            if(avoid?.Contains(node.Id)==true)continue;
            if(PlanTowDelivery(t,node,tow.Fuel,0,closed,avoid,home) is not {} option)continue;
            var total=node.Position.Distance(tow.Position)+RouteLength(option.Route);if(total<cost){cost=total;best=option;from=node;}
        }
        if(best==null||from==null){
            if(avoid!=null)return false;
            // SPEC: without even the way back out within its fuel (only with fuel
            // forced in tests) the tow waits where it stands; no fictitious trip.
            // Retried every few seconds rather than every step (calibration).
            tow.Status=VehicleStatus.Waiting;tow.TowRetryAt=State.ElapsedSeconds+TowReplanSeconds;var reachable=near.Any(node=>PlanTowDelivery(t,node,double.MaxValue,0,closed,null,home)!=null);
            tow.Cause=reachable?$"Guincho sem combustível para levar o veículo {t.Id} a qualquer destino; parado no lugar":"Guincho aguardando rota para entregar o veículo "+t.Id;
            if(reachable)t.Cause="Sendo rebocado pelo guincho "+tow.Id+"; guincho sem combustível para seguir";
            return false;
        }
        tow.TowDestination=best.Kind;tow.TowStationId=best.Station?.Id;tow.Route=best.Route;tow.RouteIndex=0;tow.DockPosition=best.Dock;tow.Status=VehicleStatus.Travelling;tow.WaitingSince=0;
        if(from.Neighbours.Where(id=>best.Route.Count<2||id!=best.Route[1]).Select(id=>_roads[id]).MinBy(n=>n.Position.Distance(tow.Position)) is {} behind){tow.Route.Insert(0,behind.Id);tow.RouteIndex=1;}
        var plan=best;AssignTowDestination(t,plan);
        if(plan.Kind==TowDestination.Holding)t.HoldingPosition=plan.Dock;
        var where=plan.Kind switch{TowDestination.Station=>"ao posto "+plan.Station!.Id,TowDestination.Exterior=>"ao exterior",TowDestination.Shop=>"à oficina "+plan.Station!.Id,TowDestination.Home=>"à vaga do dono",_=>"ao espaço de espera junto à rodovia"};
        tow.Cause=$"Guincho levando o veículo {t.Id} {where}";t.Cause="Sendo rebocado pelo guincho "+tow.Id+" "+where;
        foreach(var c in Occupants(t)){c.Activity="No guincho, a caminho "+(plan.Kind==TowDestination.Station?"do posto "+plan.Station!.Id:"do exterior");Personal(c,"Veículo carregado no guincho; seguem juntos "+where);}
        TowLeavesPastQueue(tow,t);
        return true;
    }
    // Leaving toward the queue that faces it in its lane (it loaded from behind, or
    // turns back past the stranded vehicle's queue): it first moves into the lane of
    // its route beside the queue, as an overtake starts (2 m ahead), and only with
    // that lane free there; otherwise it waits in place, holding the queue. SPEC
    // "Saída depois de carregar por trás": after the U-turn wait it looks for another
    // way out that keeps off the queue; with none, the queue turns back.
    void TowLeavesPastQueue(Vehicle tow,Vehicle t)
    {
        void Clear(){tow.TowExitWaitSince=0;tow.TowExitNoOther=false;}
        if(tow.RouteIndex<1||tow.RouteIndex>=tow.Route.Count||!_roads.TryGetValue(tow.Route[tow.RouteIndex],out var node)||!_roads.TryGetValue(tow.Route[tow.RouteIndex-1],out var previous)){Clear();return;}
        var axis=node.Position-previous.Position;var length=axis.Distance(default);if(length<.01){Clear();return;}
        var dir=axis*(1/length);var lane=node.Position+new Point(dir.Z*1.65,-dir.X*1.65);var heading=Math.Atan2(dir.X,dir.Z);
        double Along(Point p,Point from)=>(p-from).X*dir.X+(p-from).Z*dir.Z;double Across(Point p,Point from)=>Math.Abs((p-from).X*dir.Z-(p-from).Z*dir.X);
        bool Facing(Vehicle o)=>o.Id!=tow.Id&&o.Id!=t.Id&&OccupiesLane(o)&&Math.Cos(o.Heading-heading)< -.5&&Along(o.Position,tow.Position) is >0 and <=12&&Across(o.Position,tow.Position)<2.6;
        if(Across(tow.Position,lane)<1||!State.Vehicles.Any(Facing)){Clear();return;}
        var side=lane+dir*(Along(tow.Position,lane)+2);
        // A vehicle stopped behind in that lane, clear of the tow's body there, is no traffic arriving.
        bool StoppedBehind(Vehicle o)=>o.WaitingSince>0&&Along(o.Position,side)< -(BodyHalfLength(tow)+BodyHalfLength(o)+1);
        var free=!State.Vehicles.Any(o=>o.Id!=tow.Id&&o.Id!=t.Id&&OccupiesLane(o)&&(Across(o.Position,lane)<2.6&&Along(o.Position,side) is >-15 and <8&&!StoppedBehind(o)||o.Position.Distance(side)<12&&BodiesOverlap(tow,side,heading,o,o.Position,o.Heading)));
        if(free){Clear();tow.Detour=[side];tow.DetourResumeNode=tow.Route[tow.RouteIndex];return;}
        if(tow.TowExitWaitSince<=0)tow.TowExitWaitSince=State.ElapsedSeconds;
        tow.Status=VehicleStatus.Waiting;tow.TowRetryAt=State.ElapsedSeconds+2;
        tow.Cause=tow.TowExitNoOther?$"Guincho com o veículo {t.Id} sem outra saída; aguardando a fila fazer meia-volta":$"Guincho com o veículo {t.Id} aguardando a faixa ao lado livre para sair";
        if(tow.TowExitNoOther||State.ElapsedSeconds-tow.TowExitWaitSince<State.Tuning.UTurnWaitSeconds)return;
        // Marked before replanning, so the replan's own departure check does not search again.
        var queue=State.Roads.Where(r=>State.Vehicles.Any(o=>Facing(o)&&o.Position.Distance(r.Position)<State.Tuning.RoadStep+.5)).Select(r=>r.Id).ToHashSet();
        tow.TowExitNoOther=true;
        if(StartDelivery(tow,t,queue))return;
        tow.Cause=$"Guincho com o veículo {t.Id} sem outra saída; aguardando a fila fazer meia-volta";
    }
    // SPEC: the queue facing a loaded tow that found no other way out turns back,
    // as before a stopped vehicle.
    static bool TowExitHolds(Vehicle tow)=>tow.TowExitNoOther&&tow.Status==VehicleStatus.Waiting;
    void TowReachedDock(Vehicle tow)
    {
        tow.IntersectionClaim=null;tow.WaitingSince=0;var t=State.Vehicles.FirstOrDefault(o=>o.Id==tow.TowTargetId);
        Pr57DirtyTouch(t);
        if(tow.TowPhase==TowPhase.ToVehicle){
            if(t==null||!AwaitsTow(t)){AbortTow(tow,t);return;}
            // It stops facing away from the stranded vehicle, rear to its front.
            var away=tow.Position-t.Position;if(away.Distance(default)>.01&&t.Status!=VehicleStatus.Outside)tow.Heading=Math.Atan2(away.X,away.Z);
            // Off the road the vehicle is pushed out of its bay or space to the tow's rear.
            if(t.Status==VehicleStatus.Parked){var rear=tow.Position-Forward(tow.Heading)*PickupGap(t);t.PushPath=t.ServiceBay is {} bay&&BuildingById(t.ServiceShopId) is {} bayShop?[ShopWaiting(bayShop,bay),rear]:[rear];t.PushHeading=tow.Heading;t.ServiceBay=null;}
            tow.Status=VehicleStatus.Waiting;tow.TowPhase=TowPhase.Loading;tow.LoadTimer=State.Tuning.TowLoadSeconds;tow.Cause=$"Guincho carregando o veículo {t.Id}";t.Cause=$"Sendo carregado no guincho {tow.Id}";return;
        }
        // The station (or shop) must still operate on arrival; otherwise the nearest
        // destination that does, under the same rule.
        if(tow.TowDestination==TowDestination.Station&&t!=null&&(BuildingById(tow.TowStationId) is not {} station||!StationOperates(station))){StartDelivery(tow,t);return;}
        if(tow.TowDestination==TowDestination.Shop&&t!=null&&(BuildingById(tow.TowStationId) is not {} shop||!ShopOperates(shop))){t.ServiceShopId=null;StartDelivery(tow,t);return;}
        tow.Status=VehicleStatus.Unloading;tow.TowPhase=TowPhase.Unloading;tow.LoadTimer=State.Tuning.TowUnloadSeconds;
        tow.Cause=$"Guincho descarregando o veículo {t?.Id}"+tow.TowDestination switch{TowDestination.Station=>" no posto "+tow.TowStationId,TowDestination.Exterior=>" no exterior",TowDestination.Shop=>" na oficina "+tow.TowStationId,TowDestination.Home=>" na vaga do dono",_=>" no espaço de espera"};
    }
    void LoadTowed(Vehicle tow,Vehicle t)
    {
        Pr57DirtyTouch(t);
        t.PushPath.Clear();t.PushHeading=null;
        // A repaired vehicle leaves the shop now (its place there is free).
        if(t.ServicePhase==ServicePhase.AwaitingReturn){t.ServiceShopId=null;t.ServiceBay=null;t.ServiceSpot=null;}
        _pr57AxisSweepDirty=true;
        t.Status=VehicleStatus.Towed;t.Route.Clear();t.RouteIndex=0;t.Detour.Clear();t.IntersectionClaim=null;t.IntersectionEnteredAt=null;t.WaitingSince=0;t.DockPosition=null;SyncTowed(tow,t);
        tow.TowPhase=TowPhase.Delivering;tow.LoadTimer=0;
        StartDelivery(tow,t);
    }
    void SyncTowedVehicles()
    {
        foreach(var tow in State.Vehicles){
            if(tow.TowPhase is not (TowPhase.Delivering or TowPhase.Unloading))continue;
            if(State.Vehicles.FirstOrDefault(o=>o.Id==tow.TowTargetId) is {Status:VehicleStatus.Towed} t){Pr57DirtyTouch(t);SyncTowed(tow,t);}
        }
    }
    static void SyncTowed(Vehicle tow,Vehicle t)
    {
        // A car rides on the platform; a truck trails the heavy tow, front lifted.
        t.Heading=tow.Heading;t.Position=t.PassengerCar?tow.Position:tow.Position-Forward(tow.Heading)*HeavyTowTrail;
    }
    void DeliverTowed(Vehicle tow,Vehicle t)
    {
        Pr57DirtyTouch(t);
        _pr57AxisSweepDirty=true;
        var destination=tow.TowDestination;var station=BuildingById(tow.TowStationId);
        // The station can stop operating while the tow unloads: the same rule sends it on.
        if(destination==TowDestination.Station&&(station==null||!StationOperates(station))){tow.TowPhase=TowPhase.Delivering;StartDelivery(tow,t);return;}
        if(destination==TowDestination.Shop&&(station==null||!ShopOperates(station))){t.ServiceShopId=null;tow.TowPhase=TowPhase.Delivering;StartDelivery(tow,t);return;}
        // A car comes down from the platform where the tow stands; a truck is set
        // down where it trails, along the curb of the bay (no jump).
        t.TowTruckId=null;t.TowPrepaid=false;if(t.PassengerCar)t.Position=tow.Position;t.Heading=tow.Heading;t.Status=VehicleStatus.Parked;t.WaitingSince=0;
        tow.TowPhase=TowPhase.None;tow.TowTargetId=null;tow.TowStationId=null;tow.TowExitWaitSince=0;tow.TowExitNoOther=false;
        if(destination==TowDestination.Exterior&&tow.External){tow.Position=State.Entry;tow.Status=VehicleStatus.Outside;tow.Route.Clear();tow.DockPosition=null;tow.IntersectionClaim=null;tow.Cause="Disponível no exterior";}
        else ReturnTow(tow);
        if(destination==TowDestination.Exterior){if(t.BrokenDown)StartExteriorService(t,ServiceKind.Repair);else ResumeFromExterior(t);return;}
        if(destination==TowDestination.Shop){t.HoldingPosition=null;t.TowResumeNode=null;EnterShop(t,station!,ServiceKind.Repair);return;}
        if(destination==TowDestination.Home){ArriveHome(t);return;}
        if(destination==TowDestination.Holding){t.Cause=t.BrokenDown?"Quebrado; rebocado ao espaço de espera, aguardando o dono":"Rebocado; aguardando o dono";t.TowResumeNode=null;if(t.BrokenDown||t.ServicePhase==ServicePhase.AwaitingReturn){t.ServiceShopId=null;t.Service=ServiceKind.Repair;t.ServicePhase=t.BrokenDown?ServicePhase.AwaitingMoney:ServicePhase.None;}return;}
        t.HoldingPosition=null;
        if(station==null){Disembark(t,"Posto removido na entrega do guincho");t.Status=VehicleStatus.Parked;t.Cause="Rebocado; posto indisponível na entrega";return;}
        if(!Crewed(t)){
            // Nobody aboard (they went on foot): it stays parked where the tow left it
            // at the station, off the road, as a car that could not pay for fuel.
            t.Cause="Rebocado ao posto "+station.Id+"; estacionado aguardando o dono";t.TowResumeNode=null;return;
        }
        // The owner deals with it under the existing station rules.
        var access=Access(station);var target=TowTripTarget(t);
        t.SavedRoute=access==null||target==null?[]:FindRoadRoute(access.Id,target.Id,ConstructionTrip(t));t.Route=[];t.RouteIndex=0;t.RefuellingAt=station.Id;t.TowResumeNode=null;
        foreach(var c in Occupants(t))Personal(c,"Veículo entregue pelo guincho no posto "+station.Id);
        Refuel(t,station.Id);
    }
    RoadNode? TowTripTarget(Vehicle t)
    {
        if(t.TowResumeNode is {} id&&_roads.TryGetValue(id,out var resume))return resume;
        if(t.PassengerCar)return t.TripPurpose==JourneyPurpose.Leaving?EntryNode():BuildingById(t.DestinationBuildingId) is {} destination?Access(destination):null;
        if(PendingShipment(t) is {} s)return !s.PickedUp?Access(BuildingById(s.SourceBuildingId)!):s.ProjectId is {} project?WorkAccess(State.Projects.First(p=>p.Id==project)):BuildingById(s.DestinationBuildingId) is {} to?Access(to):null;
        if(t.External)return EntryNode();
        return BuildingById(t.HomeBuildingId) is {} home?Access(home):null;
    }
    Shipment? PendingShipment(Vehicle t)=>t.ShipmentId is {} id&&State.Shipments.FirstOrDefault(s=>s.Id==id) is {Complete:false,Cancelled:false} s?s:null;
    // SPEC: without an operating station the owner pays the fuel at the exterior
    // at import price (A1 when short) and drives back to continue; an external
    // truck with nothing left to deliver simply leaves.
    void ResumeFromExterior(Vehicle t)
    {
        _pr57AxisSweepDirty=true;
        t.Position=State.Entry;t.HoldingPosition=null;
        if(t.External&&PendingShipment(t)==null){
            foreach(var project in State.Projects.Where(p=>p.CrewVehicleId==t.Id))project.CrewVehicleId=null;
            t.Status=VehicleStatus.Outside;t.CrewAboard=false;t.CrewCount=0;t.ConstructionAccess=false;t.Route.Clear();t.DockPosition=null;t.IntersectionClaim=null;t.ShipmentId=null;t.TowResumeNode=null;t.Cause="Disponível no exterior";return;
        }
        if(t.PassengerCar&&t.TripPurpose==JourneyPurpose.Leaving){t.TowResumeNode=null;CarReachedSpot(t);return;}
        var fuel=Math.Min(Math.Max(0,State.Tuning.FuelTank-t.Fuel),State.OutsideStock[ResourceKind.Fuel]);
        if(fuel>0){ChargeOwner(t,Money((decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]),"Combustível","Combustível importado no exterior após reboque");State.OutsideStock.AddAmount(ResourceKind.Fuel,-(fuel));t.AddFuel(fuel);}
        var target=TowTripTarget(t);t.TowResumeNode=null;var route=target==null?[]:FindRoadRoute(EntryNode().Id,target.Id,ConstructionTrip(t));
        t.Route=route;t.RouteIndex=0;t.Status=route.Count>0?VehicleStatus.Travelling:VehicleStatus.Waiting;t.Cause=route.Count>0?"Reabastecido no exterior após reboque; voltando para a viagem":"Reabastecido no exterior; sem rota para retomar a viagem";
        if(route.Count==0)return;
        if(t.PassengerCar){if(SpotByKey(t.ParkingSpot) is {} spot)t.DockPosition=spot.Position;else t.DockPosition=null;}
        else{var s=PendingShipment(t);var building=BuildingById(s==null?t.HomeBuildingId:s.PickedUp?s.DestinationBuildingId:s.SourceBuildingId);t.DockPosition=DockAt(_roads[route[^1]],building,t.CrewAboard);
            // Asphalt for a paver is delivered by feeding it again: a fresh approach
            // from this route, not the generic dock nor the site path left before.
            if(s is {ReceivingMachineId:not null,PickedUp:true}){t.SitePath=[];t.SitePathIndex=0;ConfigureAsphaltApproach(t,s);}}
        foreach(var c in Occupants(t))Personal(c,"Combustível comprado no exterior após o reboque; voltando para a viagem");
    }
    // A tow held for minutes by a stranded vehicle or the queue it causes (directly
    // behind it, or jammed as long at a junction it locks nearby).
    bool TowHeldTooLong(Vehicle v,Vehicle? blocker)=>IsTow(v)&&v.TowPhase is TowPhase.ToVehicle or TowPhase.Delivering&&v.WaitingSince>0&&VehicleMovementTime-v.WaitingSince>=State.Tuning.UTurnWaitSeconds&&(blocker==null||blocker.Status==VehicleStatus.Stranded||HeldBehindStranded(blocker)||blocker.WaitingSince>0&&VehicleMovementTime-blocker.WaitingSince>=State.Tuning.UTurnWaitSeconds&&State.Vehicles.Any(o=>o.Status==VehicleStatus.Stranded&&o.Position.Distance(v.Position)<150));
    // The vehicle holding this junction (or its shared area) because of a stranded
    // vehicle: the stranded one itself, or one stuck right behind it.
    Vehicle? JunctionHeldByStranded(Vehicle v,RoadNode junction)
    {
        var area=_junctionAreas.GetValueOrDefault(junction.Id);
        return State.Vehicles.FirstOrDefault(o=>o.Id!=v.Id&&o.Id!=v.TowTargetId&&o.IntersectionClaim is {} held&&(held==junction.Id||area?.Contains(held)==true)&&(o.Status==VehicleStatus.Stranded||o.Status==VehicleStatus.Travelling&&o.WaitingSince>0&&HeldBehindStranded(o)));
    }
    // Head-on arrival: the tow crosses into the stranded vehicle's lane in front of
    // it only with the path to the pose free; vehicles merely near it (a car
    // waiting to leave a lot, the queue behind) do not hold it.
    bool PickupLaneClear(Vehicle tow,Point dock)
    {
        var from=tow.Position;var segment=dock-from;var length2=segment.X*segment.X+segment.Z*segment.Z;
        foreach(var o in State.Vehicles){
            if(o.Id==tow.Id||o.Id==tow.TowTargetId||!OccupiesLane(o))continue;
            var t=length2<1e-9?0:Math.Clamp(((o.Position.X-from.X)*segment.X+(o.Position.Z-from.Z)*segment.Z)/length2,0,1);
            if(o.Position.Distance(from+segment*t)<3)return false;
        }
        return true;
    }
    // SPEC: a junction locked by the situation itself: held by the stranded vehicle
    // or by those waiting behind it in its lane, and by nobody else entering it.
    // Anything else keeps the ban on overtaking in a junction.
    bool JunctionLockedBySituation(Vehicle tow,Vehicle t,RoadNode junction)
    {
        if(junction.Neighbours.Count<3)return false;
        var area=_junctionAreas.GetValueOrDefault(junction.Id);var locked=false;
        foreach(var o in State.Vehicles){
            if(o.Id==tow.Id||o.IntersectionClaim is not {} held||held!=junction.Id&&area?.Contains(held)!=true)continue;
            if(o.Id!=t.Id&&!InStrandedQueue(tow,o))return false;
            locked=true;
        }
        return locked;
    }
    // Waiting in the tow's lane (same heading) behind the stranded vehicle.
    bool InStrandedQueue(Vehicle tow,Vehicle o)
    {
        var r=o.Position-tow.Position;var across=Math.Abs(r.X*Math.Cos(tow.Heading)-r.Z*Math.Sin(tow.Heading));
        return Math.Cos(o.Heading-tow.Heading)>.9&&across<1.5&&o.WaitingSince>0&&o.Detour.Count==0&&HeldBehindStranded(o);
    }
    // The tow's body along the detour clears every vehicle but the one it comes
    // for and the queue it passes (crossing a junction, where waiting vehicles
    // stand at the mouths).
    bool DetourBodyClear(Vehicle tow,Vehicle t,List<Point> detour)
    {
        var from=tow.Position;
        for(var i=0;i<detour.Count;i++){
            var to=detour[i];var leg=to-from;var length=leg.Distance(default);var heading=length>.01?Math.Atan2(leg.X,leg.Z):tow.Heading;
            // The stranded vehicle itself (it may stand askew) counts until the last
            // leg, which ends at the pickup pose in front of it.
            for(var d=i==detour.Count-1?1.0:0;d<=length;d+=1){var at=from.MoveTowards(to,d);
                if(State.Vehicles.Any(o=>o.Id!=tow.Id&&(o.Id!=t.Id||i<detour.Count-1)&&OccupiesLane(o)&&o.Position.Distance(at)<12&&!InStrandedQueue(tow,o)&&BodiesOverlap(tow,at,heading,o,o.Position,o.Heading)))return false;}
            from=to;
        }
        return true;
    }
    // From behind a queue (in the stranded vehicle's lane, or held at the junction
    // it blocks): the empty opposite lane up to the free side in front, under the
    // same safety check as overtaking (straight stretch, opposite lane clear); the
    // only junctions allowed are the one at the pickup itself and one locked by
    // the situation (above).
    bool TryTowContraflow(Vehicle v)
    {
        if(v.Detour.Count>0||v.DockPosition is not {} pose||v.RouteIndex<1||v.RouteIndex>=v.Route.Count||State.Vehicles.FirstOrDefault(o=>o.Id==v.TowTargetId) is not {Status:VehicleStatus.Stranded} t)return false;
        if(v.WaitingSince<=0||VehicleMovementTime-v.WaitingSince<State.Tuning.BypassWaitSeconds||!RoadAxis(v,out var dir,out var left))return false;
        var toPose=pose-v.Position;var end=toPose.X*dir.X+toPose.Z*dir.Z;var poseSide=toPose.X*left.X+toPose.Z*left.Z;
        if(end<=4||end>150||poseSide< -2.6||poseSide>OppositeLaneOffset+2.2)return false;
        Point? previous=null;var crossesJunction=false;
        for(var i=v.RouteIndex-1;i<v.Route.Count;i++){
            if(!_roads.TryGetValue(v.Route[i],out var node))return false;
            var nodeAlong=(node.Position-v.Position).X*dir.X+(node.Position-v.Position).Z*dir.Z;if(i>=v.RouteIndex&&nodeAlong>end+.5)break;
            if(node.Position.Distance(pose)<=State.Tuning.RoadWidth/2+1)break;
            var nearPassed=i==v.RouteIndex-1&&node.Position.Distance(v.Position)<State.Tuning.RoadWidth;
            if((i>=v.RouteIndex||nearPassed)&&(node.Neighbours.Count>=3||node.Crosswalk)){if(!JunctionLockedBySituation(v,t,node))return false;crossesJunction=true;}
            if(previous is {} p){var step=node.Position-p;var len=step.Distance(default);if(len<.01||(step.X*dir.X+step.Z*dir.Z)/len<.999)return false;}
            previous=node.Position;
        }
        // Vehicles stopped behind the stranded one cannot come through it: not oncoming.
        var targetAlong=(t.Position-v.Position).X*dir.X+(t.Position-v.Position).Z*dir.Z;
        if(!OppositeLaneClear(v,t,dir,left,end,returnsToLane:false,alsoIgnored:o=>o.WaitingSince>0&&o.Detour.Count==0&&((o.Position-v.Position).X*dir.X+(o.Position-v.Position).Z*dir.Z)>targetAlong&&HeldBehindStranded(o)))return false;
        // The pickup pose itself must be free (the queue stands behind it).
        if(State.Vehicles.Any(o=>o.Id!=v.Id&&o.Id!=t.Id&&OccupiesLane(o)&&o.Position.Distance(pose)<3))return false;
        var detour=new List<Point>{v.Position+dir*2+left*OppositeLaneOffset};if(end-4>2)detour.Add(v.Position+dir*(end-4)+left*OppositeLaneOffset);detour.Add(pose);
        if(crossesJunction&&!DetourBodyClear(v,t,detour))return false;
        v.Detour=detour;v.DetourResumeNode=-1;v.WaitingSince=0;v.Cause=$"Guincho pela contramão vazia até o veículo {t.Id}";
        return true;
    }
    // SPEC "Chegada por trás" (IA-027): right behind the vehicle it came for, with
    // nobody between, after the U-turn wait without reaching the front, the tow
    // pulls up behind it and loads it from there as it would from the front.
    bool TryTowFromBehind(Vehicle v)
    {
        if(v.TowPhase!=TowPhase.ToVehicle||v.Detour.Count>0||v.WaitingSince<=0||VehicleMovementTime-v.WaitingSince<State.Tuning.UTurnWaitSeconds||State.Vehicles.FirstOrDefault(o=>o.Id==v.TowTargetId) is not {Status:VehicleStatus.Stranded} t)return false;
        var dir=Forward(v.Heading);var relative=t.Position-v.Position;var ahead=relative.X*dir.X+relative.Z*dir.Z;
        if(ahead<=0||ahead>15||Math.Abs(relative.X*dir.Z-relative.Z*dir.X)>2.6||Math.Cos(t.Heading-v.Heading)<=.5)return false;
        // Its nearest same-direction leader is the vehicle itself (same test as the queue).
        if(State.Vehicles.Any(o=>o.Id!=v.Id&&o.Id!=t.Id&&OccupiesLane(o)&&(o.Position-v.Position) is var r&&r.X*dir.X+r.Z*dir.Z is var f&&f>0&&f<ahead&&Math.Abs(r.X*dir.Z-r.Z*dir.X)<2.6))return false;
        // It stops the pickup gap behind it (backing up if closer), its body clear of everyone else.
        var pose=t.Position-dir*PickupGap(t);
        if(!OnRoadway(pose)||!PickupLaneClear(v,pose)||State.Vehicles.Any(o=>o.Id!=v.Id&&o.Id!=t.Id&&OccupiesLane(o)&&o.Position.Distance(pose)<12&&BodiesOverlap(v,pose,v.Heading,o,o.Position,o.Heading)))return false;
        v.RouteIndex=v.Route.Count;v.DockPosition=pose;v.WaitingSince=0;v.Cause=$"Guincho encostando por trás do veículo {t.Id} (sem acesso pela frente)";
        return true;
    }
    // Held by a stranded vehicle's jam as long as a U-turn wait, with no way in
    // from the front, from behind or around: the cause says why it stays.
    void TowWaitsWithoutAccess(Vehicle v)
    {
        if(v.TowPhase==TowPhase.ToVehicle&&v.WaitingSince>0&&VehicleMovementTime-v.WaitingSince>=State.Tuning.UTurnWaitSeconds&&HeldInStrandedChain(v,orAtItsJunction:true))v.Cause=$"Guincho sem acesso ao veículo {v.TowTargetId} pela frente nem por trás; aguardando a via abrir";
    }
}
