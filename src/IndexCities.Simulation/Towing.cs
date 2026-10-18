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
    static bool IsTow(Vehicle v)=>v.Body is TruckBody.Tow or TruckBody.TowHeavy;
    // A truck trailing a heavy tow still occupies the lane behind it.
    static bool OccupiesLane(Vehicle o)=>o.Status is VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded||o.Status==VehicleStatus.Towed&&!o.PassengerCar;
    static bool PeopleAboard(Vehicle v)=>v.DriverId!=null||v.Passengers.Count>0;
    // External trucks always keep their (non-SIM) driver aboard.
    static bool Crewed(Vehicle v)=>PeopleAboard(v)||v.External;
    IEnumerable<Citizen> Occupants(Vehicle v)=>State.Citizens.Where(c=>c.VehicleId==v.Id);
    bool OnRoadway(Vehicle v)=>NearestRoad(v.Position) is {} n&&n.Position.Distance(v.Position)<=State.Tuning.RoadWidth/2+1;
    bool AtPumpBay(Vehicle v)=>v.RefuellingAt is {} id&&BuildingById(id) is {} station&&Access(station) is {} access&&v.Position.Distance(DockAt(access,station,false))<3;
    bool Towable(Vehicle v)=>!IsTow(v)&&v.Machine==PavingMachine.None&&!AtPumpBay(v)&&OnRoadway(v);
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
            if(PeopleAboard(t)&&State.ElapsedSeconds-t.TowCalledAt>State.Tuning.TowMaxWaitSeconds){var tow=t.TowTruckId;Disembark(t,"Guincho demorou mais que o limite de espera");t.Cause=$"Sem combustível; aguardando guincho {tow}";}
            return;
        }
        if(!Towable(t)){if(t.PassengerCar&&PeopleAboard(t))LeaveCar(t,"Carro sem combustível parado na via");return;}
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
            points.Add(length>.01?n.Position+new Point(axis.Z/length*1.65,-axis.X/length*1.65):n.Position);
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
    // A route around the closed roads when one exists, otherwise the plain route.
    List<int> TowRoute(int from,int to,HashSet<int> closed)
    {
        var open=closed.Contains(from)||closed.Contains(to)?closed.Where(id=>id!=from&&id!=to).ToHashSet():closed;
        var route=FindRoadRoute(from,to,false,open);return route.Count>0?route:FindRoadRoute(from,to);
    }
    sealed record TowPlan(TowDestination Kind,Building? Station,Point Dock,List<int> Route,double Back);
    // A station that operates: open for business with fuel to sell.
    static bool StationOperates(Building b)=>b.Kind==BuildingKind.FuelStation&&b.Completed&&!b.Removed&&!b.OperationClosed&&b.CompanyId!=null&&b.OperationalCapacity>0&&b.Quantity(ResourceKind.Fuel)>0;
    // The nearest operating station by road; without one the exterior, except
    // for a vehicle nobody is aboard, which waits off the road by the highway.
    TowPlan? PlanTowDelivery(Vehicle t,RoadNode start,double fuel,double travelled,HashSet<int> closed)
    {
        var entry=EntryNode();bool Fits(List<int> route,double back)=>(travelled+RouteLength(route)+back)*State.Tuning.FuelPerMetre+4<=fuel;
        TowPlan? best=null;
        foreach(var station in State.Buildings.Where(StationOperates)){
            if(Access(station) is not {} access)continue;var route=TowRoute(start.Id,access.Id,closed);if(route.Count==0)continue;
            var back=FindRoadRoute(access.Id,entry.Id);if(back.Count==0||!Fits(route,RouteLength(back)))continue;
            if(best==null||RouteLength(route)<RouteLength(best.Route))best=new(TowDestination.Station,station,DockAt(access,station,false),route,RouteLength(back));
        }
        if(best!=null)return best;
        if(Crewed(t)){var exit=TowRoute(start.Id,entry.Id,closed);return exit.Count>0&&Fits(exit,0)?new(TowDestination.Exterior,null,State.Entry,exit,0):null;}
        var slot=HoldingSlot(t);if(slot.Road==null)return null;var toSlot=TowRoute(start.Id,slot.Road.Id,closed);if(toSlot.Count==0)return null;
        var leave=RouteLength(FindRoadRoute(slot.Road.Id,entry.Id));return Fits(toSlot,leave)?new(TowDestination.Holding,null,slot.Position,toSlot,leave):null;
    }
    // Same physical waiting spaces beside the highway as preserved municipal cargo.
    (RoadNode? Road,Point Position) HoldingSlot(Vehicle vehicle)
    {
        var candidates=State.Roads.Where(r=>r.Highway).OrderBy(r=>r.Position.Distance(State.Entry)).Where((road,index)=>index>=2&&index%2==0).Select(road=>(Road:(RoadNode?)road,Position:road.Position+new Point(0,-State.Tuning.RoadWidth/2-1.5)));
        return candidates.FirstOrDefault(candidate=>State.Buildings.All(b=>b.Removed||!Inside(b,candidate.Position,4))&&State.Vehicles.All(other=>other.Id==vehicle.Id||other.HoldingPosition is not {} reserved||reserved.Distance(candidate.Position)>=8));
    }
    Vehicle TowTruck(bool heavy)
    {
        // One tow per call: an idle one at the exterior or a new one; no fleet cap.
        var body=heavy?TruckBody.TowHeavy:TruckBody.Tow;
        if(State.Vehicles.FirstOrDefault(v=>v.Body==body&&v.External&&v.Status==VehicleStatus.Outside&&v.TowPhase==TowPhase.None) is {} idle)return idle;
        var tow=new Vehicle{Id=Id(),Body=body,External=true,ExternalDriverId=heavy?"guincheiro externo (pesado)":"guincheiro externo",Status=VehicleStatus.Outside,Position=State.Entry,CargoCapacity=0,Cause="Disponível no exterior"};
        State.Vehicles.Add(tow);return tow;
    }
    void CallTow(Vehicle t)
    {
        t.TowRetryAt=State.ElapsedSeconds+State.Tuning.TowRetrySeconds;if(t.TowCalledAt<=0)t.TowCalledAt=State.ElapsedSeconds;
        t.TowResumeNode??=t.SavedRoute.Count>0?t.SavedRoute[^1]:t.Route.Count>0?t.Route[^1]:null;
        void Impossible(string reason){t.TowRoadRevision=State.RoadRevision;Disembark(t,"Guincho impossível: "+reason);t.Cause="Sem combustível; guincho impossível: "+reason+" (novo chamado quando a malha mudar)";}
        var entry=EntryNode();
        if(!PickupPose(t,PickupGap(t),out var pose,out var pickup)){Impossible("sem espaço na via à frente do veículo");return;}
        var closed=TowClosedRoads(null);var toVehicle=TowRoute(entry.Id,pickup!.Id,closed);
        if(toVehicle.Count==0){Impossible("sem ligação viária com o exterior");return;}
        var plan=PlanTowDelivery(t,pickup,State.Tuning.FuelTank,RouteLength(toVehicle),closed);
        if(plan==null){Impossible("viagem fora da autonomia do guincho ou sem destino com acesso");return;}
        var tow=TowTruck(!t.PassengerCar);var fuel=Math.Max(0,State.Tuning.FuelTank-tow.Fuel);
        if(State.OutsideStock[ResourceKind.Fuel]<fuel){t.Cause="Sem combustível; guincho aguardando combustível no exterior";return;}
        var metres=RouteLength(toVehicle)+RouteLength(plan.Route)+plan.Back;
        var fee=Money(State.Tuning.TowServiceBase+(decimal)metres*State.Tuning.FreightPerMetre+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]);
        // One paid service per rescue: a tow sent back (fuel no longer covering a
        // replanned trip) is replaced at no new charge.
        if(!t.TowPrepaid)ChargeOwner(t,fee,"Guincho","Reboque físico: frete e combustível do guincho");
        t.TowPrepaid=true;
        State.OutsideStock[ResourceKind.Fuel]-=fuel;tow.Fuel+=fuel;
        tow.Position=State.Entry;tow.Route=toVehicle;tow.RouteIndex=0;tow.DockPosition=pose;tow.Detour.Clear();tow.WaitingSince=0;tow.IntersectionClaim=null;tow.Status=VehicleStatus.Travelling;
        tow.TowPhase=TowPhase.ToVehicle;tow.TowTargetId=t.Id;tow.TowDestination=plan.Kind;tow.TowStationId=plan.Station?.Id;tow.Cause=$"Guincho a caminho do veículo {t.Id}";
        t.TowTruckId=tow.Id;t.TowRoadRevision=null;t.Cause=$"Sem combustível; aguardando guincho {tow.Id}";
        foreach(var c in Occupants(t)){c.Activity="Aguardando guincho";Personal(c,$"Veículo sem combustível na via; guincho {tow.Id} chamado do exterior ({fee})");}
    }
    // SPEC A1: the owner pays what it can to the Global Reserve; the city pays the
    // rest and the owner owes it to the city. What the city's available cash cannot
    // cover becomes the city's debt with the Global Reserve (existing recovery).
    void ChargeOwner(Vehicle t,decimal due,string category,string reason)
    {
        var owner=t.OwnerAccount;var paid=Pay(owner,"global",due,category,reason,t.Id);var unpaid=Money(due-paid);if(unpaid<=0)return;
        var cityPaid=owner=="city"?0:Pay("city","global",unpaid,category,reason+"; pago pela Prefeitura (dono sem dinheiro)",t.Id);
        if(owner!="city")Owe(owner,"city",unpaid,category,null);
        Owe("city","global",unpaid-cityPaid,category,null);
    }
    bool OperateTow(Vehicle tow,double dt)
    {
        if(tow.TowPhase==TowPhase.None)return false;
        var t=State.Vehicles.FirstOrDefault(o=>o.Id==tow.TowTargetId);
        if(t==null||t.TowTruckId!=tow.Id||tow.TowPhase is TowPhase.ToVehicle or TowPhase.Loading&&t.Status!=VehicleStatus.Stranded){AbortTow(tow,t);return true;}
        switch(tow.TowPhase){
            case TowPhase.Loading:tow.LoadTimer-=dt;if(tow.LoadTimer<=0)LoadTowed(tow,t);return true;
            case TowPhase.Unloading:tow.LoadTimer-=dt;if(tow.LoadTimer<=0)DeliverTowed(tow,t);return true;
        }
        // SPEC waiting limit: people still stuck on a tow that cannot move go on foot.
        if(tow.TowPhase==TowPhase.Delivering&&PeopleAboard(t)&&State.ElapsedSeconds-t.TowCalledAt>State.Tuning.TowMaxWaitSeconds&&(tow.Status==VehicleStatus.Waiting||tow.WaitingSince>0&&VehicleMovementTime-tow.WaitingSince>=State.Tuning.UTurnWaitSeconds)){Disembark(t,"Guincho parado além do limite de espera");t.Status=VehicleStatus.Towed;t.Cause="Sendo rebocado; pessoas seguiram a pé";}
        if(tow.Status==VehicleStatus.Waiting){ReplanTow(tow,t);return true;}
        return false;
    }
    // SPEC: the tow never goes on without fuel for the rest of its trip. After a
    // replan that no longer fits, it returns and the same call gets another tow;
    // a loaded tow picks a destination it can still reach.
    void CheckTowFuel(Vehicle tow)
    {
        if(State.Vehicles.FirstOrDefault(o=>o.Id==tow.TowTargetId) is not {} t||tow.Route.Count==0)return;
        var ahead=tow.Route.GetRange(Math.Max(0,tow.RouteIndex-1),tow.Route.Count-Math.Max(0,tow.RouteIndex-1));var remaining=RouteLength(ahead)+(ahead.Count>0&&_roads.TryGetValue(ahead[0],out var first)?first.Position.Distance(tow.Position):0);
        if(tow.TowPhase==TowPhase.ToVehicle){if(!_roads.TryGetValue(tow.Route[^1],out var pickup)||PlanTowDelivery(t,pickup,tow.Fuel,remaining,TowClosedRoads(tow))==null){AbortTow(tow,t);t.Cause="Sem combustível; outro guincho a caminho (o anterior voltou para reabastecer)";}return;}
        if(tow.TowPhase!=TowPhase.Delivering)return;
        var back=tow.TowDestination==TowDestination.Exterior?0:RouteLength(FindRoadRoute(tow.Route[^1],EntryNode().Id));
        if((remaining+back)*State.Tuning.FuelPerMetre+4>tow.Fuel)StartDelivery(tow,t);
    }
    void AbortTow(Vehicle tow,Vehicle? t)
    {
        if(t!=null&&t.TowTruckId==tow.Id)t.TowTruckId=null;
        tow.TowPhase=TowPhase.None;tow.TowTargetId=null;tow.TowStationId=null;ReturnExternal(tow);
    }
    void ReplanTow(Vehicle tow,Vehicle t)
    {
        var start=NearestRoad(tow.Position);if(start==null)return;
        if(tow.TowPhase==TowPhase.ToVehicle){
            if(!PickupPose(t,PickupGap(t),out var pose,out var pickup)){tow.Cause="Guincho aguardando espaço na via à frente do veículo "+t.Id;return;}
            var route=TowRoute(start.Id,pickup!.Id,TowClosedRoads(tow));
            if(route.Count==0){tow.Cause="Guincho aguardando rota até o veículo "+t.Id;return;}
            tow.Route=route;tow.RouteIndex=0;tow.DockPosition=pose;tow.Status=VehicleStatus.Travelling;tow.Cause=$"Guincho a caminho do veículo {t.Id}";CheckTowFuel(tow);return;
        }
        if(!StartDelivery(tow,t))tow.Cause="Guincho aguardando rota para entregar o veículo "+t.Id;
    }
    // Departure from wherever the tow stands: the nearby road sample (not under a
    // stranded vehicle or a locked junction) that gives the shortest way out, with
    // the sample before it so the lane is known and it can still turn back.
    bool StartDelivery(Vehicle tow,Vehicle t)
    {
        var closed=TowClosedRoads(tow);TowPlan? best=null;RoadNode? from=null;double cost=double.MaxValue;
        var near=State.Roads.Where(r=>!r.ConstructionOnly&&r.Position.Distance(tow.Position)<=State.Tuning.RoadWidth).OrderBy(r=>closed.Contains(r.Id)?1:0).ThenBy(r=>r.Position.Distance(tow.Position)).ToList();
        if(near.Count==0&&NearestRoad(tow.Position) is {} nearest)near.Add(nearest);
        foreach(var node in near){
            if(best!=null&&closed.Contains(node.Id))break;
            if(PlanTowDelivery(t,node,tow.Fuel,0,closed) is not {} option)continue;
            var total=node.Position.Distance(tow.Position)+RouteLength(option.Route);if(total<cost){cost=total;best=option;from=node;}
        }
        if(best==null||from==null){tow.Status=VehicleStatus.Waiting;return false;}
        tow.TowDestination=best.Kind;tow.TowStationId=best.Station?.Id;tow.Route=best.Route;tow.RouteIndex=0;tow.DockPosition=best.Dock;tow.Status=VehicleStatus.Travelling;tow.WaitingSince=0;
        if(from.Neighbours.Where(id=>best.Route.Count<2||id!=best.Route[1]).Select(id=>_roads[id]).MinBy(n=>n.Position.Distance(tow.Position)) is {} behind){tow.Route.Insert(0,behind.Id);tow.RouteIndex=1;}
        var plan=best;
        if(plan.Kind==TowDestination.Holding)t.HoldingPosition=plan.Dock;
        var where=plan.Kind switch{TowDestination.Station=>"ao posto "+plan.Station!.Id,TowDestination.Exterior=>"ao exterior",_=>"ao espaço de espera junto à rodovia"};
        tow.Cause=$"Guincho levando o veículo {t.Id} {where}";t.Cause="Sendo rebocado "+where;
        foreach(var c in Occupants(t)){c.Activity="No guincho, a caminho "+(plan.Kind==TowDestination.Station?"do posto "+plan.Station!.Id:"do exterior");Personal(c,"Veículo carregado no guincho; seguem juntos "+where);}
        return true;
    }
    void TowReachedDock(Vehicle tow)
    {
        tow.IntersectionClaim=null;tow.WaitingSince=0;var t=State.Vehicles.FirstOrDefault(o=>o.Id==tow.TowTargetId);
        if(tow.TowPhase==TowPhase.ToVehicle){
            if(t is not {Status:VehicleStatus.Stranded}){AbortTow(tow,t);return;}
            // It stops facing away from the stranded vehicle, rear to its front.
            var away=tow.Position-t.Position;if(away.Distance(default)>.01)tow.Heading=Math.Atan2(away.X,away.Z);
            tow.Status=VehicleStatus.Waiting;tow.TowPhase=TowPhase.Loading;tow.LoadTimer=State.Tuning.TowLoadSeconds;tow.Cause=$"Guincho carregando o veículo {t.Id}";t.Cause=$"Sendo carregado no guincho {tow.Id}";return;
        }
        // The station must still operate on arrival (stock, removal); otherwise the
        // nearest one that does, or the exterior, under the same rule.
        if(tow.TowDestination==TowDestination.Station&&t!=null&&(BuildingById(tow.TowStationId) is not {} station||!StationOperates(station))){if(!StartDelivery(tow,t))tow.Cause="Guincho aguardando rota para entregar o veículo "+t.Id;return;}
        tow.Status=VehicleStatus.Unloading;tow.TowPhase=TowPhase.Unloading;tow.LoadTimer=State.Tuning.TowUnloadSeconds;
        tow.Cause=$"Guincho descarregando o veículo {t?.Id}"+(tow.TowDestination==TowDestination.Station?" no posto "+tow.TowStationId:tow.TowDestination==TowDestination.Exterior?" no exterior":" no espaço de espera");
    }
    void LoadTowed(Vehicle tow,Vehicle t)
    {
        t.Status=VehicleStatus.Towed;t.Route.Clear();t.RouteIndex=0;t.Detour.Clear();t.IntersectionClaim=null;t.IntersectionEnteredAt=null;t.WaitingSince=0;t.DockPosition=null;SyncTowed(tow,t);
        tow.TowPhase=TowPhase.Delivering;tow.LoadTimer=0;
        if(!StartDelivery(tow,t))tow.Cause="Guincho aguardando rota para entregar o veículo "+t.Id;
    }
    void SyncTowedVehicles()
    {
        foreach(var tow in State.Vehicles){
            if(tow.TowPhase is not (TowPhase.Delivering or TowPhase.Unloading))continue;
            if(State.Vehicles.FirstOrDefault(o=>o.Id==tow.TowTargetId) is {Status:VehicleStatus.Towed} t)SyncTowed(tow,t);
        }
    }
    static void SyncTowed(Vehicle tow,Vehicle t)
    {
        // A car rides on the platform; a truck trails the heavy tow, front lifted.
        t.Heading=tow.Heading;t.Position=t.PassengerCar?tow.Position:tow.Position-Forward(tow.Heading)*HeavyTowTrail;
    }
    void DeliverTowed(Vehicle tow,Vehicle t)
    {
        var destination=tow.TowDestination;var station=BuildingById(tow.TowStationId);
        // The station can stop operating while the tow unloads: the same rule sends it on.
        if(destination==TowDestination.Station&&(station==null||!StationOperates(station))){tow.TowPhase=TowPhase.Delivering;if(!StartDelivery(tow,t))tow.Cause="Guincho aguardando rota para entregar o veículo "+t.Id;return;}
        // A car comes down from the platform where the tow stands; a truck is set
        // down where it trails, along the curb of the bay (no jump).
        t.TowTruckId=null;t.TowPrepaid=false;if(t.PassengerCar)t.Position=tow.Position;t.Heading=tow.Heading;t.Status=VehicleStatus.Parked;t.WaitingSince=0;
        tow.TowPhase=TowPhase.None;tow.TowTargetId=null;tow.TowStationId=null;
        if(destination==TowDestination.Exterior){tow.Position=State.Entry;tow.Status=VehicleStatus.Outside;tow.Route.Clear();tow.DockPosition=null;tow.IntersectionClaim=null;tow.Cause="Disponível no exterior";}
        else ReturnExternal(tow);
        if(destination==TowDestination.Exterior){ResumeFromExterior(t);return;}
        if(destination==TowDestination.Holding){t.Cause="Rebocado; aguardando o dono";t.TowResumeNode=null;return;}
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
        t.Position=State.Entry;t.HoldingPosition=null;
        if(t.External&&PendingShipment(t)==null){
            foreach(var project in State.Projects.Where(p=>p.CrewVehicleId==t.Id))project.CrewVehicleId=null;
            t.Status=VehicleStatus.Outside;t.CrewAboard=false;t.CrewCount=0;t.ConstructionAccess=false;t.Route.Clear();t.DockPosition=null;t.IntersectionClaim=null;t.ShipmentId=null;t.TowResumeNode=null;t.Cause="Disponível no exterior";return;
        }
        if(t.PassengerCar&&t.TripPurpose==JourneyPurpose.Leaving){t.TowResumeNode=null;CarReachedSpot(t);return;}
        var fuel=Math.Min(Math.Max(0,State.Tuning.FuelTank-t.Fuel),State.OutsideStock[ResourceKind.Fuel]);
        if(fuel>0){ChargeOwner(t,Money((decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]),"Combustível","Combustível importado no exterior após reboque");State.OutsideStock[ResourceKind.Fuel]-=fuel;t.Fuel+=fuel;}
        var target=TowTripTarget(t);t.TowResumeNode=null;var route=target==null?[]:FindRoadRoute(EntryNode().Id,target.Id,ConstructionTrip(t));
        t.Route=route;t.RouteIndex=0;t.Status=route.Count>0?VehicleStatus.Travelling:VehicleStatus.Waiting;t.Cause=route.Count>0?"Reabastecido no exterior após reboque; voltando para a viagem":"Reabastecido no exterior; sem rota para retomar a viagem";
        if(route.Count==0)return;
        if(t.PassengerCar){if(SpotByKey(t.ParkingSpot) is {} spot)t.DockPosition=spot.Position;else t.DockPosition=null;}
        else{var s=PendingShipment(t);var building=BuildingById(s==null?t.HomeBuildingId:s.PickedUp?s.DestinationBuildingId:s.SourceBuildingId);t.DockPosition=DockAt(_roads[route[^1]],building,t.CrewAboard);}
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
    // From behind a queue (in the stranded vehicle's lane, or held at the junction
    // it blocks): the empty opposite lane up to the free side in front, under the
    // same safety check as overtaking (straight stretch, opposite lane clear); the
    // only junction allowed is the one at the pickup itself.
    bool TryTowContraflow(Vehicle v)
    {
        if(v.Detour.Count>0||v.DockPosition is not {} pose||v.RouteIndex<1||v.RouteIndex>=v.Route.Count||State.Vehicles.FirstOrDefault(o=>o.Id==v.TowTargetId) is not {Status:VehicleStatus.Stranded} t)return false;
        if(v.WaitingSince<=0||VehicleMovementTime-v.WaitingSince<State.Tuning.BypassWaitSeconds||!RoadAxis(v,out var dir,out var left))return false;
        var toPose=pose-v.Position;var end=toPose.X*dir.X+toPose.Z*dir.Z;var poseSide=toPose.X*left.X+toPose.Z*left.Z;
        if(end<=4||end>150||poseSide< -2.6||poseSide>OppositeLaneOffset+2.2)return false;
        Point? previous=null;
        for(var i=v.RouteIndex-1;i<v.Route.Count;i++){
            if(!_roads.TryGetValue(v.Route[i],out var node))return false;
            var nodeAlong=(node.Position-v.Position).X*dir.X+(node.Position-v.Position).Z*dir.Z;if(i>=v.RouteIndex&&nodeAlong>end+.5)break;
            if(node.Position.Distance(pose)<=State.Tuning.RoadWidth/2+1)break;
            var nearPassed=i==v.RouteIndex-1&&node.Position.Distance(v.Position)<State.Tuning.RoadWidth;
            if((i>=v.RouteIndex||nearPassed)&&(node.Neighbours.Count>=3||node.Crosswalk))return false;
            if(previous is {} p){var step=node.Position-p;var len=step.Distance(default);if(len<.01||(step.X*dir.X+step.Z*dir.Z)/len<.999)return false;}
            previous=node.Position;
        }
        // Vehicles stopped behind the stranded one cannot come through it: not oncoming.
        var targetAlong=(t.Position-v.Position).X*dir.X+(t.Position-v.Position).Z*dir.Z;
        if(!OppositeLaneClear(v,t,dir,left,end,returnsToLane:false,alsoIgnored:o=>o.WaitingSince>0&&o.Detour.Count==0&&((o.Position-v.Position).X*dir.X+(o.Position-v.Position).Z*dir.Z)>targetAlong&&HeldBehindStranded(o)))return false;
        // The pickup pose itself must be free (the queue stands behind it).
        if(State.Vehicles.Any(o=>o.Id!=v.Id&&o.Id!=t.Id&&OccupiesLane(o)&&o.Position.Distance(pose)<3))return false;
        var detour=new List<Point>{v.Position+dir*2+left*OppositeLaneOffset};if(end-4>2)detour.Add(v.Position+dir*(end-4)+left*OppositeLaneOffset);detour.Add(pose);
        v.Detour=detour;v.DetourResumeNode=-1;v.WaitingSince=0;v.Cause=$"Guincho pela contramão vazia até o veículo {t.Id}";
        return true;
    }
}
