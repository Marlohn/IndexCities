namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    bool _pedestrianCrossingsChecked;
    bool ShiftActive(Citizen c)
    {
        var b=BuildingById(c.WorkplaceId);if(b==null||!b.Completed||!c.Alive||!c.Resident)return false;
        var day=(int)State.Now.DayOfWeek;if(day==c.WeeklyDayOff||day==(c.WeeklyDayOff+1)%7)return false;
        var hour=State.Now.TimeOfDay.TotalHours;
        if(b.Kind is BuildingKind.WaterIntake or BuildingKind.WaterTreatment or BuildingKind.SewageTreatment or BuildingKind.WindPower)return c.Shift switch{0=>hour>=6&&hour<14,1=>hour>=14&&hour<22,_=>hour>=22||hour<6};
        return b.Kind is BuildingKind.Grocery or BuildingKind.FuelStation?(c.Shift==0?hour>=7&&hour<15:hour>=14&&hour<22):hour>=7&&hour<15;
    }
    bool Open(Building b)=>b.Kind is BuildingKind.Grocery or BuildingKind.FuelStation?State.Now.Hour>=7&&State.Now.Hour<22:State.Now.Hour>=6&&State.Now.Hour<19;
    // Read-only opening hours for presentation (problem alerts over buildings).
    // Same rule the simulation already uses for building causes; no behaviour change.
    public bool BuildingOpen(Building building)=>Open(building);
    public bool WorkerPresentAt(Citizen c,Building b)=>c.Alive&&c.WorkplaceId==b.Id&&c.VehicleId==null&&c.ConstructionProjectId==null&&!c.Moving&&c.Position.Distance(b.Door)<3&&ShiftActive(c);
    public int WorkersPresent(Building b)=>State.Citizens.Count(c=>WorkerPresentAt(c,b));
    List<Point> CitizenRoute(Citizen citizen,Building destination)
    {
        // Replanning beside the destination door must not take the walker to
        // the default sidewalk, across the road and back to that same door.
        var access=Access(destination);
        if(citizen.Position.Distance(destination.Door)<3&&access!=null&&!CrossesLane(citizen.Position,destination.Door,access))return [citizen.Position,destination.Door];
        return PedestrianRoute(citizen.Position,destination);
    }
    bool StartJourney(Citizen c,Building destination,JourneyPurpose purpose)
    {
        if(TryDrive(c,destination,purpose))return true;
        var path=CitizenRoute(c,destination);if(path.Count==0){c.Activity="Sem rota para "+destination.Definition.Name;c.Decision="Acesso viário ou travessia indisponível";c.NextDecisionAt=State.ElapsedSeconds+300;return false;}
        c.Path=path;c.PathIndex=0;c.PathRoadRevision=State.RoadRevision;c.Purpose=purpose;c.DestinationId=destination.Id;c.Activity=purpose switch{JourneyPurpose.Work=>"Indo trabalhar",JourneyPurpose.Home=>"Voltando para casa",JourneyPurpose.Shopping=>"Indo comprar alimentos",JourneyPurpose.Arriving=>"Chegando à cidade",JourneyPurpose.Visit=>"Indo visitar outra família",JourneyPurpose.Stroll=>"Passeando a pé",_=>"Indo ao canteiro"};Personal(c,c.Activity+" em "+destination.Definition.Name);return true;
    }
    void MoveCitizens(double dt)
    {
        // Refresh derived crossings once for an old save before retrying routes.
        // Loading itself still preserves the serialized state exactly.
        if(!_pedestrianCrossingsChecked){UpdateCrosswalks();_pedestrianCrossingsChecked=true;}
        foreach(var c in State.Citizens.Where(c=>c.Alive)){
            if(!c.Moving&&c.PathRoadRevision!=State.RoadRevision&&c.Activity.StartsWith("Sem rota",StringComparison.Ordinal))c.NextDecisionAt=Math.Min(c.NextDecisionAt,State.ElapsedSeconds);
            if(c.ConstructionProjectId!=null&&ShiftActive(c)){c.WorkSecondsToday+=dt;AccrueWage(c,dt);}
            if(c.VehicleId is {} vid){var v=State.Vehicles.FirstOrDefault(v=>v.Id==vid);if(v!=null){c.Position=v.Position;c.InsideMap=InMap(v.Position);}continue;}
            if(c.Moving){
                if(c.Purpose==JourneyPurpose.Boarding)c.PathRoadRevision=State.RoadRevision;
                if(c.PathRoadRevision!=State.RoadRevision&&c.Purpose==JourneyPurpose.Construction&&c.ConstructionProjectId is {} projectId){var project=State.Projects.First(p=>p.Id==projectId);var access=WorkAccess(project);var replacement=access==null?new List<Point>():PedestrianPath(c.Position,CrewPedestrianSite(access),access);c.Path=replacement;c.PathIndex=0;c.PathRoadRevision=State.RoadRevision;if(replacement.Count==0){c.Activity="Equipe aguardando rota de obra";continue;}}
                var journeyDestination=BuildingById(c.DestinationId);
                var closeDoorDetour=journeyDestination!=null&&c.Position.Distance(journeyDestination.Door)<3&&c.Path.Skip(c.PathIndex).Any(point=>point.Distance(journeyDestination.Door)>3)&&Access(journeyDestination) is {} doorAccess&&!CrossesLane(c.Position,journeyDestination.Door,doorAccess);
                if((c.PathRoadRevision!=State.RoadRevision||closeDoorDetour)&&c.Purpose!=JourneyPurpose.Construction){
                    var dest=journeyDestination;if(dest!=null){var replacement=CitizenRoute(c,dest);if(replacement.Count==0){c.Activity="Viagem interrompida: rota removida";c.Path.Clear();c.NextDecisionAt=State.ElapsedSeconds+300;continue;}c.Path=replacement;c.PathIndex=0;c.PathRoadRevision=State.RoadRevision;}
                }
                var distance=State.Tuning.WalkingSpeed*dt;
                while(distance>0&&c.PathIndex<c.Path.Count){var target=c.Path[c.PathIndex];var d=c.Position.Distance(target);
                    if(c.CrossingNodeId is {} old&&(!_roads.TryGetValue(old,out var marked)||c.Position.Distance(marked.Position)>State.Tuning.RoadWidth+1))c.CrossingNodeId=null;
                    // A long sidewalk segment can pass several future crossings.
                    // Only negotiate the crossing reached by this physical step,
                    // and preserve an ongoing crossing before nearby alternatives.
                    var nextPosition=c.Position.MoveTowards(target,Math.Min(distance,d));
                    var crossing=State.Roads.Where(n=>n.Crosswalk&&(n.Position.Distance(c.Position)<State.Tuning.RoadWidth+1||n.Position.Distance(nextPosition)<State.Tuning.RoadWidth+1)&&CrossesLane(c.Position,nextPosition,n)).OrderBy(n=>n.Id==c.CrossingNodeId?0:1).ThenBy(n=>n.Position.Distance(c.Position)).FirstOrDefault();
                    if(crossing!=null&&c.CrossingNodeId!=crossing.Id){
                        // A vehicle already stopped in a queue does not advance onto the
                        // walker; only one entering or passing the crossing does. This
                        // breaks the circular wait between queues and waiting walkers.
                        var occupied=State.Vehicles.Any(v=>v.Status==VehicleStatus.Travelling&&(v.IntersectionClaim==crossing.Id||v.WaitingSince==0&&v.Position.Distance(crossing.Position)<3.5));
                        if(occupied){c.Activity="Aguardando travessia";break;}c.CrossingNodeId=crossing.Id;
                    }
                    var heading=d>.001?(target-c.Position)*(1/d):new Point(0,0);var gap=State.Citizens.Where(other=>other.Id!=c.Id&&other.Alive&&other.Moving&&other.VehicleId==null).Select(other=>{var relative=other.Position-c.Position;var otherTarget=other.Path[other.PathIndex];var otherDelta=otherTarget-other.Position;var otherLength=otherDelta.Distance(new(0,0));var alignment=otherLength<.001?1:(heading.X*otherDelta.X+heading.Z*otherDelta.Z)/otherLength;
                        // At a shared turning point, two converging headings can
                        // each see the other ahead. Only the closer walker leads
                        // that merge; stable identity resolves an exact tie.
                        var sharedTurn=otherTarget.Distance(target)<.05;var precedes=!sharedTurn||otherLength<d-.001||Math.Abs(otherLength-d)<=.001&&other.Id<c.Id;
                        return (Ahead:relative.X*heading.X+relative.Z*heading.Z,Side:Math.Abs(relative.X*heading.Z-relative.Z*heading.X),Alignment:alignment,Precedes:precedes);}).Where(other=>other.Precedes&&other.Ahead>.001&&other.Side<.55&&other.Alignment>.5).Select(other=>other.Ahead).DefaultIfEmpty(double.MaxValue).Min();var travelled=Math.Min(distance,Math.Min(d,Math.Max(0,gap-.7)));if(travelled<.000001&&d>.001){c.Activity="Fila de pedestres";break;}c.Position=c.Position.MoveTowards(target,travelled);distance-=travelled;if(d<=travelled+.00001)c.PathIndex++;else break;
                }
                c.InsideMap=InMap(c.Position);if(!c.Moving){c.CrossingNodeId=null;CitizenArrived(c);}continue;
            }
            if(State.ElapsedSeconds<c.NextDecisionAt)continue;c.NextDecisionAt=State.ElapsedSeconds+60;
            CitizenRoutine(c);
        }
    }
    void CitizenArrived(Citizen c)
    {
        if(c.Purpose==JourneyPurpose.Boarding){var vehicle=State.Vehicles.FirstOrDefault(v=>v.Id==c.BoardingVehicleId&&v.DriverId==c.Id);if(vehicle!=null&&vehicle.Position.Distance(c.Position)<.1){c.VehicleId=vehicle.Id;c.Activity=vehicle.PassengerCar?"Dirigindo carro particular":"Embarcou para entrega real";if(vehicle.PassengerCar){vehicle.Status=VehicleStatus.Travelling;vehicle.Cause="Em viagem";}}else {c.Activity="Veículo indisponível para embarque";if(State.Vehicles.FirstOrDefault(v=>v.Id==c.BoardingVehicleId&&v.PassengerCar&&v.DriverId==c.Id) is {} reserved)reserved.DriverId=null;}c.BoardingVehicleId=null;c.Path.Clear();c.PathIndex=0;return;}
        if(c.Purpose==JourneyPurpose.Leaving){c.Resident=false;c.InsideMap=false;c.Activity="Residindo no exterior";c.WorkplaceId=null;Personal(c,"Saiu fisicamente da cidade; dinheiro, patrimônio e dívidas persistem");return;}
        if(c.Purpose==JourneyPurpose.Construction){c.Activity="Trabalhando no canteiro";return;}
        var destination=BuildingById(c.DestinationId);if(destination==null){c.Activity="Destino indisponível";return;}
        c.Position=destination.Door;c.Path.Clear();c.PathIndex=0;
        if(c.Purpose==JourneyPurpose.Arriving){c.Resident=true;c.InsideMap=true;c.Activity="Em casa";Personal(c,"Chegou à residência adquirida");}
        else if(c.Purpose==JourneyPurpose.Work)c.Activity="No trabalho";
        else if(c.Purpose==JourneyPurpose.Home){if(FamilyById(c.FamilyId) is {} family){family.Pantry+=c.BagFood;c.BagFood=0;}c.Activity="Em casa";}
        else if(c.Purpose==JourneyPurpose.Shopping){
            if(TryBuyFood(c,destination)){if(BuildingById(FamilyById(c.FamilyId)?.HomeId) is {} home)StartJourney(c,home,JourneyPurpose.Home);}
            else if(!SeekFood(c,destination.Id)&&BuildingById(FamilyById(c.FamilyId)?.HomeId) is {} home)StartJourney(c,home,JourneyPurpose.Home);
        }
        else if(c.Purpose==JourneyPurpose.Visit)c.Activity="Visitando família em "+destination.Definition.Name;
        else if(c.Purpose==JourneyPurpose.Stroll){if(BuildingById(FamilyById(c.FamilyId)?.HomeId) is {} home&&StartJourney(c,home,JourneyPurpose.Home))c.Activity="Voltando do passeio";}
    }
    void CitizenRoutine(Citizen c)
    {
        if(!c.Resident||!c.InsideMap)return;var family=FamilyById(c.FamilyId);var home=BuildingById(family?.HomeId);var job=BuildingById(c.WorkplaceId);
        if(c.ConstructionProjectId!=null){if(ShiftActive(c))return;var p=State.Projects.FirstOrDefault(p=>p.Id==c.ConstructionProjectId);p?.LocalCrew.Remove(c.Id);c.ConstructionProjectId=null;}
        if(job!=null&&ShiftActive(c)){
            if(c.Position.Distance(job.Door)>3)StartJourney(c,job,JourneyPurpose.Work);else c.Activity="No trabalho";return;
        }
        if(home==null){c.Activity="Sem moradia";return;}
        var adults=family!.Members.Select(id=>State.Citizens.First(x=>x.Id==id)).Where(x=>x.Alive&&x.Age>=18&&x.Resident).ToList();
        var existingShopper=adults.Any(x=>x.Id!=c.Id&&(x.Purpose==JourneyPurpose.Shopping&&x.Moving||x.BagFood>0));
        if(family.Pantry<family.Members.Count*1.5&&c.Age>=18&&!existingShopper&&State.Now.Hour>=7&&State.Now.Hour<21){c.TriedShops.Clear();if(SeekFood(c))return;}
        if(c.Purpose==JourneyPurpose.Visit&&c.OutingUntil>State.ElapsedSeconds&&c.Position.Distance(home.Door)>3){
            // A failed food search updates its diagnosis, but does not make an
            // arrived visitor leave the actual interior or end the visit.
            if(BuildingById(c.DestinationId) is {Completed:true} host&&c.Position.Distance(host.Door)<1)c.Activity="Visitando família em "+host.Definition.Name;
            return;
        }
        if(c.Position.Distance(home.Door)<=3&&c.Age>=18&&FreeTime(c)&&NextRandom()<State.Tuning.OutingChancePerMinute&&TryOuting(c,home))return;
        if(c.Position.Distance(home.Door)>3)StartJourney(c,home,JourneyPurpose.Home);else c.Activity="Em casa";
    }
    bool SeekFood(Citizen c,int? excluded=null)
    {
        if(excluded!=null&&!c.TriedShops.Contains(excluded.Value))c.TriedShops.Add(excluded.Value);
        // Choose by the family's real total cost: shelf price for what it needs plus
        // a calibrated round-trip cost; a shop that cannot fill the basket costs
        // another trip elsewhere.
        var family=FamilyById(c.FamilyId);var wanted=Math.Max(1,(family?.Members.Count??1)*State.Tuning.PurchaseFoodPerPerson-(family?.Pantry??0));
        decimal TripCost(Building shop)=>(decimal)(shop.Position.Distance(c.Position)*2)*State.Tuning.ShoppingTripCostPerMetre;
        foreach(var shop in State.Buildings.Where(b=>b.Kind==BuildingKind.Grocery&&b.Completed&&!b.Removed&&b.CompanyId!=null&&!c.TriedShops.Contains(b.Id)).OrderBy(b=>b.SalePrice*(decimal)wanted+TripCost(b)*(b.Quantity(ResourceKind.Food)>=wanted?1:2)).ThenBy(b=>b.Id)){
            if(!Open(shop)||shop.OperationalCapacity<=0||shop.Quantity(ResourceKind.Food)<.5)continue;
            if(StartJourney(c,shop,JourneyPurpose.Shopping))return true;
        }
        c.Activity="Alimentos indisponíveis ou inacessíveis";c.Decision="Não existe loja operante, estoque e rota viáveis";return false;
    }
    void ReturnExternal(Vehicle v)
    {
        v.ShipmentId=null;
        var atKnownBay=v.DockPosition is {} dock&&v.Position.Distance(dock)<.1&&v.Route.Count>0&&v.RouteIndex>=v.Route.Count&&_roads.ContainsKey(v.Route[^1]);var n=atKnownBay?_roads[v.Route[^1]]:NearestRoad(v.Position);if(n==null||!atKnownBay&&n.Position.Distance(v.Position)>State.Tuning.RoadWidth/2+3){v.Status=VehicleStatus.Waiting;v.Cause="Sem via para voltar ao exterior";return;}
        var route=FindRoadRoute(n.Id,EntryNode().Id);if(route.Count==0){v.Status=VehicleStatus.Waiting;v.Cause="Conexão externa interrompida";return;}
        v.Route=route;v.RouteIndex=0;v.DockPosition=null;v.Status=VehicleStatus.Travelling;v.DestinationBuildingId=null;v.ShipmentId=null;v.Cause="Retorno ao exterior";
    }
    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)
    {
        // Release a passed junction even while a vehicle waits at the next one.
        // Carriageway half-width (3.3 m) + truck half-length (3.6 m) covers the
        // actual rear of the truck; the former 9 m reservation could overlap
        // nearby junctions and make opposite queues hold each other forever.
        foreach(var vehicle in State.Vehicles)ClearPassedJunction(vehicle);
        foreach(var v in State.Vehicles.OrderBy(v=>v.WaitingSince).ThenBy(v=>v.Id)){
            if(_vehicleMovementBatch==null||operationsDt!=null){
                var operationTime=operationsDt??dt;
                v.FuelBurnedLastStep=0;
                if(!v.External&&v.Status is VehicleStatus.Travelling or VehicleStatus.Unloading or VehicleStatus.Waiting){var actualDriver=State.Citizens.FirstOrDefault(c=>c.Id==v.DriverId&&c.Alive&&(v.PassengerCar||c.Resident&&c.InsideMap)&&c.VehicleId==v.Id);if(actualDriver==null){v.Cause=v.PassengerCar?"Aguardando motorista embarcar":"Sem motorista local presente; carga e viagem preservadas";continue;}}
                if(!v.PassengerCar&&v.DriverId is {} driver&&State.Citizens.FirstOrDefault(c=>c.Id==driver) is {} citizen&&v.Status is VehicleStatus.Travelling or VehicleStatus.Unloading){citizen.Position=v.Position;citizen.WorkSecondsToday+=operationTime;AccrueWage(citizen,operationTime);citizen.Activity="Conduzindo entrega real";}
                if(v.Status==VehicleStatus.Unloading){v.LoadTimer-=operationTime;if(v.LoadTimer<=0)VehicleArrived(v);continue;}
                if(v.PassengerCar&&v.Status==VehicleStatus.Waiting){ReplanCar(v);continue;}
                if(v.PassengerCar&&v.Status==VehicleStatus.Stranded&&(v.DriverId!=null||v.Passengers.Count>0)){LeaveCar(v,"Carro sem combustível parado na via");continue;}
                if(v.Status==VehicleStatus.Waiting){if(v.ShipmentId is {} si&&State.Shipments.FirstOrDefault(s=>s.Id==si) is {} shipment){if(shipment.Cancelled)RedirectCancelled(v,shipment);else ReplanVehicle(v,shipment);}else if(v.External)ReturnExternal(v);continue;}
                if(v.Status!=VehicleStatus.Travelling)continue;
                if(v.Route.Count==0||v.Route.Any(id=>!_roads.ContainsKey(id))){v.Status=VehicleStatus.Waiting;v.Cause="Rota interrompida; aguarda ligação viária";continue;}
                _vehicleMovementBatch?.Add(v.Id);
            }else if(!_vehicleMovementBatch.Contains(v.Id)||v.Status!=VehicleStatus.Travelling)continue;
            var remaining=dt;
            while(remaining>.000001&&v.Status==VehicleStatus.Travelling){
                if(_vehicleMovementBatch!=null)_vehicleMovementClock=State.ElapsedSeconds+_vehicleMovementOffset+dt-remaining;
                if(v.RouteIndex>=v.Route.Count){var carSpot=v.PassengerCar&&v.RefuellingAt==null;if(v.DockPosition is {} dock&&v.Position.Distance(dock)>.01){var blocked=!carSpot&&State.Vehicles.Any(o=>o.Id!=v.Id&&!(o.PassengerCar&&o.Status==VehicleStatus.Parked)&&o.Status is VehicleStatus.Unloading or VehicleStatus.Parked&&o.Position.Distance(dock)<4);if(blocked){v.Cause="Fila para acesso de carga";break;}var distance=v.Position.Distance(dock);var moved=Math.Min(distance,State.Tuning.VehicleSpeed*remaining);if(v.Fuel<moved*FuelRate(v)){v.Status=VehicleStatus.Stranded;break;}var toDock=dock-v.Position;if(v.PassengerCar&&distance>.01)v.Heading=Math.Atan2(toDock.X,toDock.Z);v.Position=v.Position.MoveTowards(dock,moved);v.Fuel-=moved*FuelRate(v);v.FuelBurnedLastStep+=moved*FuelRate(v);remaining-=moved/State.Tuning.VehicleSpeed;if(moved<distance-.01)break;}if(carSpot){CarReachedSpot(v);break;}v.Status=VehicleStatus.Unloading;v.LoadTimer=15;v.IntersectionClaim=null;v.Cause=v.RefuellingAt!=null?"Abastecendo combustível":v.ShipmentId is {} shipmentId&&State.Shipments.Any(s=>s.Id==shipmentId&&!s.PickedUp)?"Carregando na origem":"Descarregando no destino";break;}
                var node=_roads[v.Route[v.RouteIndex]];if(!node.Highway&&node.MaintenanceRatio<=.05){v.Status=VehicleStatus.Waiting;v.Cause="Via sem capacidade por manutenção não realizada";break;}var previous=v.RouteIndex>0?_roads[v.Route[v.RouteIndex-1]].Position:v.Position;
                var delta=v.RouteIndex==0&&v.Route.Count>1?_roads[v.Route[1]].Position-node.Position:node.Position-previous;var len=delta.Distance(new(0,0));var target=node.Position;
                if(len>.01)target+=new Point(delta.Z/len*1.65,-delta.X/len*1.65);
                var d=v.Position.Distance(target);var speed=(node.Highway?State.Tuning.HighwaySpeed:State.Tuning.VehicleSpeed)*Math.Max(.15,node.MaintenanceRatio);
                var approachHeading=v.Heading;
                var facing=target-v.Position;if(d>.01)v.Heading=Math.Atan2(facing.X,facing.Z);
                // The exterior gateway is a staging origin (all external trucks
                // start there). A waiting cargo outside the map must not prevent
                // another real truck leaving to repair its broken road route.
                var insideMap=InMap(v.Position);
                double RoadSide(Point p)=>((p.X-node.Position.X)*delta.Z-(p.Z-node.Position.Z)*delta.X)/len;
                var fromSide=len>.01?RoadSide(v.Position):0;var toSide=len>.01?RoadSide(target):0;
                var access=new LaneAccess(node,delta,len,target,d,speed,fromSide,toSide);
                var enteringLane=v.PassengerCar&&v.RouteIndex==0&&insideMap&&len>.01&&Math.Abs(fromSide-toSide)>.1&&!AlreadyFollowingLane(v,node,approachHeading);
                bool BlocksEntry(Vehicle other)=>enteringLane&&BlocksLaneEntry(v,access,other);
                // Apply the existing ID tie-break to the whole coincidence case.
                // A tiny positive forward projection must not make its winner
                // yield back to the other vehicle and freeze both indefinitely.
                var leaderDistance=double.MaxValue;Vehicle? blocker=null;
                void HoldFor(Vehicle other,double distance){if(distance<leaderDistance){leaderDistance=distance;blocker=other;}}
                foreach(var liveOther in State.Vehicles){
                    var other=enteringLane?EntryFrameVehicle(liveOther):liveOther;
                    if(other.Id==v.Id||other.Status is not (VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded))continue;
                    var relative=other.Position-v.Position;
                    if(insideMap&&relative.Distance(new(0,0))<.1){if(other.Status==VehicleStatus.Stranded||other.Id<v.Id)HoldFor(other,0);continue;}
                    // Entry occupancy is measured along the ROAD axis, including
                    // the opposite lane, rather than the diagonal lot heading.
                    if(BlocksEntry(other)){HoldFor(other,0);continue;}
                    var forward=relative.X*Math.Sin(v.Heading)+relative.Z*Math.Cos(v.Heading);
                    var side=Math.Abs(relative.X*Math.Cos(v.Heading)-relative.Z*Math.Sin(v.Heading));
                    if(Math.Cos(other.Heading-v.Heading)>.5&&forward>0&&side<2.6)HoldFor(other,forward);
                }
                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));
                if(step<.00001&&d>.00001){v.Cause=blocker?.Status==VehicleStatus.Stranded?$"Aguardando veículo {blocker.Id} sem combustível na via":enteringLane?"Aguardando espaço para acessar a faixa":"Fila na faixa";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;}
                if((node.Neighbours.Count>=3||node.Crosswalk)&&d<9&&!MayEnterJunction(v,node)){if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;}
                if(v.Fuel<=.000001){v.Status=VehicleStatus.Stranded;v.Cause="Sem combustível; nenhuma viagem fictícia";break;}
                step=Math.Min(step,v.Fuel/FuelRate(v));v.Position=v.Position.MoveTowards(target,step);v.Fuel=Math.Max(0,v.Fuel-step*FuelRate(v));v.FuelBurnedLastStep+=step*FuelRate(v);remaining-=step/speed;v.WaitingSince=0;v.Cause="Em viagem";
                ClearPassedJunction(v);
                if(d<=step+.001)v.RouteIndex++;else break;
            }
        }
    }
    void ReplanVehicle(Vehicle v,Shipment s)
    {
        var start=NearestRoad(v.Position);if(start==null||start.Position.Distance(v.Position)>State.Tuning.RoadWidth/2+3)return;
        var target=!s.PickedUp?Access(BuildingById(s.SourceBuildingId)!):s.ProjectId is {} pId?WorkAccess(State.Projects.First(p=>p.Id==pId)):Access(BuildingById(s.DestinationBuildingId)!);
        if(target==null)return;var route=FindRoadRoute(start.Id,target.Id);if(route.Count==0)return;v.Route=route;v.RouteIndex=0;v.Status=VehicleStatus.Travelling;
    }
    bool CrossesLane(Point from,Point to,RoadNode n)
    {
        var a=from-n.Position;var b=to-n.Position;
        var horizontal=n.Neighbours.Select(id=>_roads[id].Position-n.Position).Where(p=>Math.Abs(p.X)>.1).Select(p=>p.X).Append(0).ToArray();
        var vertical=n.Neighbours.Select(id=>_roads[id].Position-n.Position).Where(p=>Math.Abs(p.Z)>.1).Select(p=>p.Z).Append(0).ToArray();
        // A corner's incoming carriageway ends at the bend. Its infinite axis
        // must not be mistaken for road beyond that end while walking the curb.
        bool Cross(double first,double second,double alongFirst,double alongSecond,double[] extent)=>extent.Length>1&&Math.Min(alongFirst,alongSecond)<=extent.Max()+3.3&&Math.Max(alongFirst,alongSecond)>=extent.Min()-3.3&&((Math.Abs(first)<3.3||Math.Abs(second)<3.3)||Math.Sign(first)!=Math.Sign(second))&&Math.Abs(first-second)>.05;
        return Cross(a.Z,b.Z,a.X,b.X,horizontal)||Cross(a.X,b.X,a.Z,b.Z,vertical);
    }
    bool MayEnterJunction(Vehicle v,RoadNode junction)
    {
        v.Cause="Aguardando preferência/semáforo/travessia";var area=_junctionAreas.GetValueOrDefault(junction.Id);
        var continuing=area!=null&&v.IntersectionClaim is {} held&&area.Contains(held);
        if(area!=null){var owner=NearbyAreaOwner(area,junction,v);if(owner!=null&&owner.Id!=v.Id){v.Cause="Aguardando liberação do cruzamento pelo veículo "+owner.Id;return false;}}
        if(v.IntersectionClaim==junction.Id)return true;
        if(area==null&&junction.Neighbours.Count>=3&&State.Vehicles.FirstOrDefault(o=>o.Id!=v.Id&&o.IntersectionClaim==junction.Id&&o.Position.Distance(junction.Position)<9) is {} occupant){v.Cause="Aguardando liberação do cruzamento pelo veículo "+occupant.Id;return false;}
        // Only a walker physically at this crossing holds traffic back.
        if(State.Citizens.Any(c=>c.Alive&&c.VehicleId==null&&c.Moving&&c.CrossingNodeId==junction.Id&&c.Position.Distance(junction.Position)<State.Tuning.RoadWidth/2+1.5))return false;
        // A straight loading access/crosswalk is not a road intersection.
        // Applying right-hand priority there can make the road vehicle yield
        // forever to a truck that is itself waiting for the road to clear.
        if(junction.Neighbours.Count<3)return true;
        var horizontal=Math.Abs(Math.Sin(v.Heading))>.7;
        if(junction.Control==JunctionControl.Signal&&((VehicleMovementTime%60<30)!=horizontal))return false;
        // A vehicle already admitted to shared pavement must be able to leave
        // it before another occupant/approach takes the same physical space.
        if(continuing){ReserveJunction(v,junction,area);return true;}
        var competitors=State.Vehicles.Where(o=>o.Id!=v.Id&&o.Status==VehicleStatus.Travelling&&o.RouteIndex<o.Route.Count&&o.Route[o.RouteIndex]==junction.Id&&o.Position.Distance(junction.Position)<14).ToList();
        if(junction.Control==JunctionControl.ThroughX&&!horizontal&&competitors.Any(o=>Math.Abs(Math.Sin(o.Heading))>.7))return false;
        if(junction.Control==JunctionControl.ThroughZ&&horizontal&&competitors.Any(o=>Math.Abs(Math.Cos(o.Heading))>.7))return false;
        if(junction.Control==JunctionControl.RightHand){var right=new Point(-Math.Cos(v.Heading),Math.Sin(v.Heading));if(competitors.Any(o=>{var p=o.Position-junction.Position;return p.X*right.X+p.Z*right.Z>2;}))return false;}
        ReserveJunction(v,junction,area);return true;
    }
    void VehicleArrived(Vehicle v)
    {
        if(v.RefuellingAt is {} stationId&&BuildingById(stationId) is {} station&&Access(station) is {} stationAccess&&v.Position.Distance(DockAt(stationAccess,station,false))<3){Refuel(v,stationId);return;}
        var s=v.ShipmentId==null?null:State.Shipments.FirstOrDefault(s=>s.Id==v.ShipmentId);
        if(s==null){if(v.External&&v.Position.Distance(State.Entry)<8){v.Position=State.Entry;v.Status=VehicleStatus.Outside;v.CrewAboard=false;v.CrewCount=0;v.Route.Clear();v.IntersectionClaim=null;v.Cause="Disponível no exterior";}else {v.Status=VehicleStatus.Parked;ReleaseDriver(v);}return;}
        if(s.Complete&&v.CrewAboard){v.Status=VehicleStatus.Parked;v.Cause="Equipe externa no canteiro";return;}
        if(s.Cancelled){RedirectCancelled(v,s);return;}
        if(!s.PickedUp&&s.SourceBuildingId is {} sourceId){var source=BuildingById(sourceId);if(source==null||s.Cargo.Any(kv=>source.Quantity(kv.Key)<kv.Value)){v.Status=VehicleStatus.Waiting;v.Cause="Carga reservada indisponível na origem";return;}foreach(var kv in s.Cargo)source.AddStock(kv.Key,-kv.Value);if(source.Kind==BuildingKind.Farm)source.FoodSoldToday+=s.Cargo.GetValueOrDefault(ResourceKind.Food);v.Cargo=new(s.Cargo);s.PickedUp=true;
            if(s.ProjectId is {} pi&&State.Projects.FirstOrDefault(p=>p.Id==pi) is {} project)foreach(var kv in s.Cargo)project.LocalReserved[kv.Key]=Math.Max(0,project.LocalReserved.GetValueOrDefault(kv.Key)-kv.Value);
            var target=s.ProjectId is {} pId?WorkAccess(State.Projects.First(p=>p.Id==pId)):Access(BuildingById(s.DestinationBuildingId)!);var start=NearestRoad(v.Position);if(start==null||target==null){v.Status=VehicleStatus.Waiting;v.Cause="Destino sem via";return;}if(!EnsureFuel(v,target.Id)){v.Status=VehicleStatus.Waiting;return;}if(v.RefuellingAt==null)v.DockPosition=DockAt(target,BuildingById(s.DestinationBuildingId),v.CrewAboard);v.Status=VehicleStatus.Travelling;return;
        }
        if(s.ProjectId is {} id&&s.Purpose is ShipmentPurpose.Construction or ShipmentPurpose.Maintenance){var p=State.Projects.First(p=>p.Id==id);if(p.Status==WorkStatus.Cancelled){s.Cancelled=true;RedirectCancelled(v,s);return;}if(RetireMaintenanceWithoutTargets(p))return;foreach(var kv in v.Cargo)p.Delivered[kv.Key]=p.Delivered.GetValueOrDefault(kv.Key)+kv.Value;v.Cargo.Clear();s.Complete=true;
            if(v.CrewAboard){v.Status=VehicleStatus.Parked;v.IntersectionClaim=null;v.Cause="Equipe externa no canteiro";}else ReturnExternal(v);return;
        }
        if(s.Purpose==ShipmentPurpose.Vehicle){s.Complete=true;v.External=false;v.ExternalDriverId=null;v.Status=VehicleStatus.Parked;v.ShipmentId=null;v.Cause="Caminhão adquirido; aguarda motorista SIM";return;}
        var dest=BuildingById(s.DestinationBuildingId);if(dest==null){v.Status=VehicleStatus.Waiting;v.Cause="Destino indisponível: carga preservada";return;}
        var atBay=State.Vehicles.Any(o=>o.Id<v.Id&&o.Status==VehicleStatus.Unloading&&o.ShipmentId is {} other&&State.Shipments.Any(x=>x.Id==other&&x.DestinationBuildingId==dest.Id));
        if(atBay){v.Status=VehicleStatus.Unloading;v.LoadTimer=10;v.Cause="Fila física de descarga";return;}
        var space=Math.Max(0,dest.Definition.Capacity-dest.Stock.Values.Sum());var quantity=v.Cargo.Values.Sum();if(quantity>space+.001){v.Status=VehicleStatus.Unloading;v.LoadTimer=30;v.Cause="Estoque de destino sem capacidade";return;}
        foreach(var kv in v.Cargo)dest.AddStock(kv.Key,kv.Value);v.Cargo.Clear();s.Complete=true;
        if(v.External)ReturnExternal(v);else ReturnLocal(v);
    }
    void ReturnLocal(Vehicle v)
    {
        var source=BuildingById(v.HomeBuildingId);var start=NearestRoad(v.Position);var target=source==null?null:Access(source);if(start==null||target==null){v.Status=VehicleStatus.Waiting;v.Cause="Sem acesso para retorno";return;}v.Route=FindRoadRoute(start.Id,target.Id);v.RouteIndex=0;v.ShipmentId=null;v.DockPosition=DockAt(target,source,false);v.Status=v.Route.Count>0?VehicleStatus.Travelling:VehicleStatus.Waiting;v.DestinationBuildingId=source!.Id;
    }
    void ReleaseDriver(Vehicle v){if(v.DriverId is {} id&&State.Citizens.FirstOrDefault(c=>c.Id==id) is {} c){c.VehicleId=null;c.Position=v.Position;var workplace=BuildingById(c.WorkplaceId);if(workplace!=null)StartJourney(c,workplace,JourneyPurpose.Work);}v.DriverId=null;}
    void RedirectCancelled(Vehicle v,Shipment s)
    {
        if(!s.PickedUp){s.Complete=true;v.Cargo.Clear();v.CrewAboard=false;ReturnExternal(v);return;}
        // A crew-only trip has no paid cargo to store or redirect. Keep the real
        // crew aboard its truck and send it home by the remaining road network.
        if(v.Cargo.Values.All(quantity=>quantity==0)){s.Complete=true;v.HoldingPosition=null;ReturnExternal(v);return;}
        if(TryRedirectMunicipalCargo(v,s))return;
        var depot=State.Buildings.FirstOrDefault(b=>b.Kind==BuildingKind.WorksDepot&&b.Completed&&!b.Removed&&Access(b)!=null&&b.Stock.Values.Sum()+v.Cargo.Values.Sum()<=b.Definition.Capacity);
        var start=NearestRoad(v.Position);var target=depot==null?null:Access(depot);var route=start==null||target==null?new List<int>():FindRoadRoute(start.Id,target.Id);
        if(depot==null||route.Count==0){ParkCancelledCargo(v);return;}
        v.HoldingPosition=null;s.Cancelled=false;s.ProjectId=null;s.Purpose=ShipmentPurpose.Maintenance;s.DestinationBuildingId=depot.Id;s.Destination=depot.Door;v.CrewAboard=false;v.CrewCount=0;v.Route=route;v.RouteIndex=0;v.DockPosition=DockAt(target!,depot,false);v.Status=VehicleStatus.Travelling;v.Cause="Redirecionando carga municipal ao Pátio";
    }
    void ParkCancelledCargo(Vehicle vehicle)
    {
        var candidates=State.Roads.Where(r=>r.Highway).OrderBy(r=>r.Position.Distance(State.Entry)).Where((road,index)=>index>=2&&index%2==0).Select(road=>(Road:road,Position:road.Position+new Point(0,-State.Tuning.RoadWidth/2-1.5)));
        var slot=candidates.FirstOrDefault(candidate=>State.Buildings.All(b=>b.Removed||!Inside(b,candidate.Position,4))&&State.Vehicles.All(other=>other.Id==vehicle.Id||other.HoldingPosition is not {} reserved||reserved.Distance(candidate.Position)>=8));
        var start=NearestRoad(vehicle.Position);if(slot.Road==null||start==null){vehicle.Status=VehicleStatus.Waiting;vehicle.Cause="Carga municipal aguardando estoque; não há espaço físico de espera";return;}
        vehicle.HoldingPosition=slot.Position;if(vehicle.Position.Distance(slot.Position)<.1){vehicle.Status=VehicleStatus.Waiting;vehicle.Cause="Carga municipal estacionada fora das faixas; aguarda estoque ou obra compatível";return;}
        var route=FindRoadRoute(start.Id,slot.Road.Id);if(route.Count==0){vehicle.Status=VehicleStatus.Waiting;vehicle.Cause="Sem rota física para estacionar carga preservada";return;}
        vehicle.Route=route;vehicle.RouteIndex=0;vehicle.DockPosition=slot.Position;vehicle.Status=VehicleStatus.Travelling;vehicle.Cause="Transportando carga cancelada para espaço real de espera";
    }
    bool TryRedirectMunicipalCargo(Vehicle vehicle,Shipment shipment)
    {
        if(shipment.GoodsOwnerAccount!="city"||vehicle.Cargo.Count==0)return false;var start=NearestRoad(vehicle.Position);if(start==null||start.Position.Distance(vehicle.Position)>State.Tuning.RoadWidth/2+3)return false;
        foreach(var project in State.Projects.Where(p=>p.Id!=shipment.ProjectId&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress).OrderBy(p=>p.Id)){
            var compatible=vehicle.Cargo.All(k=>k.Value<=project.Required.GetValueOrDefault(k.Key)-project.Delivered.GetValueOrDefault(k.Key)-project.LocalReserved.GetValueOrDefault(k.Key)-State.Shipments.Where(s=>s.Id!=shipment.Id&&s.ProjectId==project.Id&&!s.Complete).Sum(s=>s.Cargo.GetValueOrDefault(k.Key))+.001);if(!compatible)continue;
            var destination=WorkAccess(project);if(destination==null||!_exteriorConnected.Contains(destination.Id))continue;var route=FindRoadRoute(start.Id,destination.Id);if(route.Count==0)continue;var returnTrip=FindRoadRoute(destination.Id,EntryNode().Id);if(returnTrip.Count==0||vehicle.Fuel<(RouteLength(route)+RouteLength(returnTrip)+20)*State.Tuning.FuelPerMetre)continue;
            var fee=State.Tuning.FreightBase+(decimal)RouteLength(route)*State.Tuning.FreightPerMetre;if(!ProjectPay(project,fee,project.IsMaintenance?MaintenanceCategory(project):"Transporte municipal","Redirecionamento físico de material municipal já pago"))continue;
            vehicle.HoldingPosition=null;shipment.OriginalProjectId??=shipment.ProjectId;shipment.ProjectId=project.Id;shipment.Cancelled=false;shipment.Purpose=project.IsMaintenance?ShipmentPurpose.Maintenance:ShipmentPurpose.Construction;shipment.DestinationBuildingId=project.BuildingId;shipment.Destination=destination.Position;shipment.PricePaid+=Money(fee);
            if(vehicle.CrewAboard&&project.CrewVehicleId==null&&project.LocalCrew.Count==0)project.CrewVehicleId=vehicle.Id;else {vehicle.CrewAboard=false;vehicle.CrewCount=0;}vehicle.Route=route;vehicle.RouteIndex=0;vehicle.DockPosition=DockAt(destination,BuildingById(project.BuildingId),vehicle.CrewAboard);vehicle.Status=VehicleStatus.Travelling;vehicle.Cause="Carga municipal redirecionada fisicamente à obra "+project.Id;return true;
        }return false;
    }
    Point DockAt(RoadNode road,Building? building,bool crew)
    {var normal=road.Highway||road.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-road.Position.X)>.1)?new Point(0,1):new Point(1,0);var delta=building==null?normal:building.Door-road.Position;var length=delta.Distance(new(0,0));var side=length<.01?normal:delta*(1/length);return road.Position+side*(State.Tuning.RoadWidth/2+1.5)+(crew?new Point(side.Z,-side.X)*8.5:new Point(0,0));}
    void Refuel(Vehicle v,int stationId)
    {
        var station=BuildingById(stationId);if(station==null||station.OperationalCapacity<=0||station.CompanyId==null||station.Quantity(ResourceKind.Fuel)<=0){v.LoadTimer=60;v.Status=VehicleStatus.Unloading;v.Cause="Posto sem operação/estoque";return;}
        var amount=Math.Min(State.Tuning.FuelTank-v.Fuel,Math.Min(station.Quantity(ResourceKind.Fuel),(double)(Balance(v.OwnerAccount)/station.SalePrice)));
        if(amount<=.001){
            // A private car that cannot pay stays parked at the station, still
            // usable once the family has money; it is not stranded on the lane.
            if(v.PassengerCar){v.RefuellingAt=null;v.SavedRoute=[];v.Status=VehicleStatus.Parked;LeaveCar(v,"Carro parado no posto: sem dinheiro para abastecer");return;}
            v.Status=VehicleStatus.Stranded;v.Cause="Sem dinheiro/combustível para abastecer";return;}
        var price=Money((decimal)amount*station.SalePrice);if(!Transfer(v.OwnerAccount,"company:"+station.CompanyId,price,"Combustível","Abastecimento físico no posto",v.Id))return;
        station.AddStock(ResourceKind.Fuel,-amount);v.Fuel+=amount;v.RefuellingAt=null;v.Route=v.SavedRoute;v.SavedRoute=[];if(v.Route.Count>0){var shipment=State.Shipments.FirstOrDefault(s=>s.Id==v.ShipmentId);var destination=BuildingById(shipment?.PickedUp==false?shipment.SourceBuildingId:shipment?.DestinationBuildingId??v.HomeBuildingId);v.DockPosition=DockAt(_roads[v.Route[^1]],destination,false);}if(v.PassengerCar&&SpotByKey(v.ParkingSpot) is {} carSpot)v.DockPosition=carSpot.Position;v.RouteIndex=0;v.Status=VehicleStatus.Travelling;v.Cause="Abastecido com combustível real";
    }
    bool EnsureFuel(Vehicle v,int destination)
    {
        var start=NearestRoad(v.Position);if(start==null)return false;var normal=FindRoadRoute(start.Id,destination);if(normal.Count==0)return false;
        var need=RouteLength(normal)*FuelRate(v)+(v.PassengerCar?1:4);if(v.Fuel>=need){v.Route=normal;v.RouteIndex=0;return true;}
        foreach(var station in State.Buildings.Where(b=>b.Kind==BuildingKind.FuelStation&&b.Completed&&b.OperationalCapacity>0&&b.Quantity(ResourceKind.Fuel)>0&&Access(b)!=null).OrderBy(b=>b.Position.Distance(v.Position))){var n=Access(station)!;var toStation=FindRoadRoute(start.Id,n.Id);if(toStation.Count==0||RouteLength(toStation)*FuelRate(v)>v.Fuel)continue;v.SavedRoute=FindRoadRoute(n.Id,destination);if(v.SavedRoute.Count==0)continue;v.RefuellingAt=station.Id;v.DockPosition=DockAt(n,station,false);v.Route=toStation;v.RouteIndex=0;return true;}
        v.Cause="Combustível insuficiente; não há posto viável no alcance";return false;
    }
}
