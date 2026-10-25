namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    public PlacementQuote QuoteBuilding(BuildingKind kind,Point position,double rotation=0,int? ignoredProject=null)
    {
        var d=Catalog.Definitions[kind];var candidate=new Building{Kind=kind,Position=position,Rotation=rotation};
        var corners=Corners(candidate).ToList();var outside=corners.Any(p=>!InMap(p));var water=BuildingOverlapsWater(candidate);
        var building=State.Buildings.FirstOrDefault(b=>!b.Removed&&b.ProjectId!=ignoredProject&&Overlap(candidate,b));
        var road=State.Roads.FirstOrDefault(n=>Inside(candidate,n.Position,State.Tuning.RoadWidth/2-.05));
        var roadWork=State.Projects.FirstOrDefault(p=>p.Id!=ignoredProject&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress&&p.RoadPoints.Any(pt=>Inside(candidate,pt,State.Tuning.RoadWidth/2-.05)));
        var valid=!outside&&!water&&building==null&&road==null&&roadWork==null;
        var allowed=State.CityHallPlaced||kind==BuildingKind.CityHall;
        // The hall being replaced and its substitute are one institution: moving
        // the substitute work does not count the old seat as a second hall.
        var duplicateHall=kind==BuildingKind.CityHall&&State.Buildings.Any(b=>!b.Removed&&b.Kind==BuildingKind.CityHall&&b.ProjectId!=ignoredProject&&!State.MunicipalRelocations.Any(r=>r.Active&&r.SourceBuildingId==b.Id&&r.TargetProjectId==ignoredProject));
        var accessible=HasExteriorAccess(candidate);var quote=QuoteMaterials(d.Materials,position,d.WorkSeconds);
        var plannedAccess=!accessible&&State.Projects.Any(p=>p.Id!=ignoredProject&&!p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress&&p.RoadPoints.Any(point=>point.Distance(candidate.Door)<=State.Tuning.RoadWidth/2+3));
        var accessMessage=plannedAccess?WorkCause.RoadWorkAtEntrance:Access(candidate)!=null?WorkCause.RoadWithoutExterior:WorkCause.NoUrbanRoad;
        var message=duplicateHall?"A cidade já tem uma Prefeitura":!allowed?"Posicione a Prefeitura primeiro":outside?"O edifício ultrapassa o limite do mapa":water?"O edifício invade a água":building!=null?$"Sobreposição com {building.Definition.Name} #{building.Id}":road!=null?"O edifício invade uma via pronta":roadWork!=null?$"Sobreposição com obra de rua #{roadWork.Id} ainda não concluída":!accessible?accessMessage:kind==BuildingKind.WaterIntake&&!HasNaturalWaterSource(candidate)?WorkCause.IntakeNeedsNaturalSource:WorkCause.ValidPosition;
        return new(valid&&allowed&&!duplicateHall,accessible,quote,new(d.Materials),message);
    }
    bool BuildingOverlapsWater(Building building)=>State.SchemaVersion==1
        ?InWater(building.Position)||Corners(building).Any(p=>InWater(p))
        :LakeGeometry.OverlapsRectangle(State,building.Position,building.Definition.Width,building.Definition.Depth,building.Rotation);

    // Static geography only: ordinary previews still check roads, lots and access.
    public bool IsWaterIntakeSite(Point position,double rotation=0)
    {
        var building=new Building{Kind=BuildingKind.WaterIntake,Position=position,Rotation=rotation};
        return HasNaturalWaterSource(building)&&Corners(building).All(point=>InMap(point))&&!BuildingOverlapsWater(building);
    }
    decimal QuoteMaterials(Dictionary<ResourceKind,double> materials,Point position,double workSeconds,bool crew=true)
    {
        var goods=materials.Sum(kv=>Money((decimal)kv.Value*State.Tuning.ImportPrices[kv.Key]));
        var distance=Math.Abs(position.X-State.Entry.X)+Math.Abs(position.Z-State.Entry.Z);
        var trips=(crew?1:0)+Math.Max(1,materials.Values.Sum(quantity=>(int)Math.Ceiling(quantity/150)));
        return Money(goods+trips*(State.Tuning.FreightBase+(decimal)(distance*2)*State.Tuning.FreightPerMetre+State.Tuning.ImportPrices[ResourceKind.Fuel]*(decimal)State.Tuning.FuelTank)+(crew?State.Tuning.ExternalCrewHourlyPrice*(decimal)(workSeconds/3600):0)+20);
    }
    public CommandResult PlaceBuilding(BuildingKind kind,Point position,double rotation=0)=>PlaceBuilding(kind,position,rotation,null);
    CommandResult PlaceBuilding(BuildingKind kind,Point position,double rotation,int? ignoredProject)
    {
        var q=QuoteBuilding(kind,position,rotation,ignoredProject);if(!q.PhysicallyValid)return new(false,q.Message);
        if(!State.CityHallPlaced&&kind!=BuildingKind.CityHall)return new(false,"Posicione a Prefeitura primeiro");
        if(q.Total>Available)return new(false,"Caixa disponível insuficiente para comprometer esta obra");
        var p=new WorkProject{Id=Id(),Position=position,Required=q.Materials,WorkSeconds=Catalog.Definitions[kind].WorkSeconds,BudgetRemaining=q.Total,ExternalCrewFee=Money(State.Tuning.ExternalCrewHourlyPrice*(decimal)(Catalog.Definitions[kind].WorkSeconds/3600)),Cause=q.Message};
        var b=new Building{Id=Id(),Kind=kind,Position=position,Rotation=rotation,ProjectId=p.Id,SalaryOffer=State.Tuning.HourlySalary};p.BuildingId=b.Id;
        State.Buildings.Add(b);State.Projects.Add(p);if(kind==BuildingKind.CityHall)State.CityHallPlaced=true;
        UpdateCrosswalks();ReserveLocalMaterials();Event("Obra de "+b.Definition.Name+" confirmada; orçamento reservado"+(q.Accessible?"; aguardando materiais e equipe":"; conecte uma rua urbana à entrada"));return new(true,State.LastMessage,b.Id);
    }
    List<Point> NewRoadPoints(Point start,Point end,bool reverse,int? ignoredProject=null)
    {
        // Already built points are excluded by the physical network above. If
        // demolished, they require a fresh paid order rather than remaining
        // reserved forever by a work that is waiting for its connection.
        var planned=State.Projects.Where(p=>p.Id!=ignoredProject&&!p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress).SelectMany(p=>p.RoadPoints.Where(point=>!p.RolledRoadPoints.Contains(point))).ToArray();
        return RoadShape(start,end,reverse).Where(point=>State.Roads.All(road=>road.Position.Distance(point)>.05)&&planned.All(other=>other.Distance(point)>=.1)).ToList();
    }
    public PlacementQuote QuoteRoad(Point start,Point end,bool reverse=false,int? ignoredProject=null)
    {
        var points=NewRoadPoints(start,end,reverse,ignoredProject);
        var outside=points.Any(p=>!InMap(p,State.Tuning.RoadWidth/2));var water=State.SchemaVersion==1?points.Any(p=>InWater(p,State.Tuning.RoadWidth/2)):RoadOverlapsLake(RoadShape(start,end,reverse));
        var building=State.Buildings.FirstOrDefault(b=>!b.Removed&&points.Any(p=>Inside(b,p,State.Tuning.RoadWidth/2-.05)));
        var contact=points.Count>0?RoadContactProblem(RoadShape(start,end,reverse),ignoredProject):null;
        var snappedStart=SnapRoad(start);var snappedEnd=SnapRoad(end);var delta=snappedEnd-snappedStart;
        var corner=reverse?new Point(snappedStart.X,snappedEnd.Z):new Point(snappedEnd.X,snappedStart.Z);
        // Following an already ordered/built street after reaching its junction
        // does not construct a new tiny bend. Keep those real T connections.
        var shortCorner=Math.Abs(delta.X)>.01&&Math.Abs(delta.Z)>.01&&Math.Min(Math.Abs(delta.X),Math.Abs(delta.Z))<State.Tuning.RoadWidth&&points.Contains(corner);
        var valid=points.Count>0&&!outside&&!water&&building==null&&contact==null&&!shortCorner;
        var length=points.Count*State.Tuning.RoadStep;var materials=new Dictionary<ResourceKind,double>{{ResourceKind.Aggregate,length*State.Tuning.RoadWidth*.04},{ResourceKind.Asphalt,length*State.Tuning.RoadWidth*.02}};
        var access=points.Any(p=>State.Roads.Any(r=>r.Position.Distance(p)<=State.Tuning.RoadStep+.05&&_workExteriorConnected.Contains(r.Id)));
        var message=points.Count==0?"Este traçado não adiciona trecho novo às ruas prontas ou em obra":outside?"O traçado ultrapassa o limite do mapa":water?"O traçado invade a água":building!=null?$"O traçado invade {building.Definition.Name} #{building.Id}":!access?WorkCause.NoReadyExteriorLink:WorkCause.ValidStretchExteriorAvailable;
        if(contact!=null&&!outside&&!water&&building==null)message=contact;
        if(shortCorner&&points.Count>0&&!outside&&!water&&building==null&&contact==null)message="Curva muito curta. Alinhe a rua ou afaste a esquina.";
        var machinery=points.Count==0?0:QuoteMachinery(start,Math.Max(120,length*2))*PavingComponents(points);
        return new(valid,access,points.Count==0?0:QuoteMaterials(materials,start,Math.Max(120,length*2),false)+machinery,materials,message){MachineryEstimate=machinery};
    }
    bool RoadOverlapsLake(IReadOnlyList<Point> shape)
    {
        var existing=State.Roads.Select(r=>r.Position).Concat(State.Projects.Where(p=>!p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress).SelectMany(p=>p.RoadPoints)).ToArray();
        foreach(var point in shape){
            var neighbours=shape.Concat(existing).Where(p=>p.Distance(point)>.05&&p.Distance(point)<=State.Tuning.RoadStep+.01).ToArray();
            var horizontal=neighbours.Any(p=>Math.Abs(p.X-point.X)>.1);var vertical=neighbours.Any(p=>Math.Abs(p.Z-point.Z)>.1);
            if(!horizontal&&!vertical)horizontal=true;
            var width=horizontal&&vertical||vertical?State.Tuning.RoadWidth:State.Tuning.RoadStep;
            var depth=horizontal?State.Tuning.RoadWidth:State.Tuning.RoadStep;
            if(LakeGeometry.OverlapsRectangle(State,point,width,depth))return true;
        }
        return false;
    }
    public CommandResult PlaceRoad(Point start,Point end,bool reverse=false)
    {
        var q=QuoteRoad(start,end,reverse);if(!q.PhysicallyValid)return new(false,q.Message);if(q.Total>Available)return new(false,"Caixa disponível insuficiente");
        var points=NewRoadPoints(start,end,reverse);
        var p=new WorkProject{Id=Id(),Position=points[0],RoadPoints=points,PavingVersion=1,MachineryQuoted=QuoteMachinery(start,Math.Max(120,points.Count*State.Tuning.RoadStep*2))*PavingComponents(points),Required=q.Materials,WorkSeconds=Math.Max(120,points.Count*State.Tuning.RoadStep*2),BudgetRemaining=q.Total,Cause=q.Message};State.Projects.Add(p);ReserveLocalMaterials();Event("Obra de rua confirmada; orçamento reservado"+(q.Accessible?"; materiais e equipe serão enviados":"; conecte o trecho a uma via pronta"));return new(true,State.LastMessage,p.Id);
    }
    // PR57-only A/B: index in-flight shipments for reservation queries within
    // this invocation. ToLookup preserves each project's shipment iteration order.
    // Rebuilt every call: no cache survives state changes or dispatches.
    static readonly bool Pr57DispatchShipmentLookup=Environment.GetEnvironmentVariable("PR57_DISPATCH_SHIPMENT_LOOKUP")=="1";
    void ReserveLocalMaterials()
    {
        var depot=State.Buildings.FirstOrDefault(b=>b.Kind==BuildingKind.WorksDepot&&b.Completed&&!b.Removed&&!b.OperationClosed);if(depot==null)return;
        var pendingByProject=Pr57DispatchShipmentLookup
            ?State.Shipments.Where(s=>s.ProjectId!=null&&!s.Complete).ToLookup(s=>s.ProjectId)
            :null;
        foreach(var k in Enum.GetValues<ResourceKind>().Where(k=>k!=ResourceKind.Food&&k!=ResourceKind.Fuel)){
            var free=depot.Quantity(k)-State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress).Sum(p=>p.LocalReserved.GetValueOrDefault(k));
            foreach(var p in State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress).OrderBy(p=>p.Id)){
                var pending=pendingByProject==null?State.Shipments.Where(s=>s.ProjectId==p.Id&&!s.Complete).Sum(s=>s.Cargo.GetValueOrDefault(k)):pendingByProject[p.Id].Sum(s=>s.Cargo.GetValueOrDefault(k));var need=p.Required.GetValueOrDefault(k)-p.Delivered.GetValueOrDefault(k)-pending-p.LocalReserved.GetValueOrDefault(k);var reserve=Math.Max(0,Math.Min(free,need));p.LocalReserved.AddAmount(k,reserve);free-=reserve;
            }
        }
    }
    RoadNode? WorkAccess(WorkProject p)
    {
        // A demolished stretch is skipped: maintenance continues on the remaining network.
        if(p.IsMaintenance&&p.MaintenanceRoadIds.Count>0){var remaining=p.MaintenanceRoadIds.Where(_roads.ContainsKey).ToList();if(remaining.Count==0)return null;var target=_roads[remaining[Math.Min(remaining.Count-1,(int)(p.Progress*remaining.Count))]];return Pr57WorkAccessIndexEnabled?Pr57IndexedMaintenanceAccess(target.Position):State.Roads.Where(r=>_workExteriorConnected.Contains(r.Id)&&r.Position.Distance(target.Position)<=State.Tuning.RoadStep+.05).MinBy(r=>r.Position.Distance(target.Position));}
        if(p.BuildingId!=null)return BuildingById(p.BuildingId) is {} b?ConstructionAccess(b):null;
        return Pr57WorkAccessIndexEnabled?Pr57IndexedRoadProjectAccess(p):State.Roads.Where(r=>p.RoadPoints.Any(a=>a.Distance(r.Position)<=State.Tuning.RoadStep+.05)&&_workExteriorConnected.Contains(r.Id)).OrderBy(r=>r.Position.Distance(p.Position)).FirstOrDefault();
    }
    bool ProjectPay(WorkProject p,decimal value,string category,string reason)
    {
        value=Money(value);if(value>p.BudgetRemaining)return false;
        p.BudgetRemaining-=value;if(!Transfer("city","global",value,category,reason,p.Id)){p.BudgetRemaining+=value;return false;}p.CostPaid+=value;return true;
    }
    void OrderConstruction()
    {
        if(!State.Projects.Any(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress))return;
        ReserveLocalMaterials();
        foreach(var p in State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress).OrderBy(p=>p.IsMaintenance?MaintenancePriority(p):4).ThenBy(p=>p.Id)){
            if(RoadPhysicalWorkDone(p))continue;
            var access=WorkAccess(p);if(access==null||!_workExteriorConnected.Contains(access.Id)){p.Cause=WorkCause.NoMaterialsAccess;continue;}
            if(IsPaving(p))OrderPavingMachines(p,access);else AssignLocalCrew(p,access);
            foreach(var k in p.Required.Keys.ToList()){
                var inflight=State.Shipments.Where(s=>s.ProjectId==p.Id&&!s.Complete).Sum(s=>s.Cargo.GetValueOrDefault(k));
                var missing=p.Required[k]-p.Delivered.GetValueOrDefault(k)-inflight;
                if(missing<=.001)continue;
                var receiver=IsPaving(p)&&k==ResourceKind.Asphalt?AsphaltReceiver(p):null;
                if(receiver!=null&&inflight>.001)continue;
                if(IsPaving(p)&&k==ResourceKind.Asphalt&&receiver==null){p.Cause=WorkCause.AwaitingPavingMachines;continue;}
                var vehicle=AvailableTruck(k);if(vehicle==null){p.Cause=WorkCause.AwaitingExternalTruck;break;}
                var amount=Math.Min(missing,vehicle.CargoCapacity);
                var local=p.LocalReserved.GetValueOrDefault(k);var depot=State.Buildings.FirstOrDefault(b=>b.Kind==BuildingKind.WorksDepot&&b.Completed&&!b.Removed&&!b.OperationClosed&&HasExteriorAccess(b));
                var source=local>.001&&depot!=null?depot:null;if(source!=null)amount=Math.Min(amount,local);
                if(source==null&&State.OutsideStock.GetValueOrDefault(k)<amount){p.Cause=WorkCause.SupplierOutOfMaterial;continue;}
                var route=FindRoadRoute(EntryNode().Id,(source==null?access:Access(source)!).Id,true);if(route.Count==0)continue;
                var travel=RouteLength(route)+(source==null?0:RouteLength(FindRoadRoute(Access(source)!.Id,access.Id,true)));
                // SPEC: the truck must reach the site and return to the exterior with one tank.
                if((travel+RouteLength(FindRoadRoute(access.Id,EntryNode().Id,true)))*FuelRate(vehicle)+4>State.Tuning.FuelTank){p.Cause=WorkCause.RouteBeyondTruckRange;continue;}
                var fuel=Math.Max(0,State.Tuning.FuelTank-vehicle.Fuel);if(State.OutsideStock[ResourceKind.Fuel]<fuel){p.Cause=WorkCause.SupplierOutOfFuel;continue;}
                var fee=State.Tuning.FreightBase+(decimal)(travel*2)*State.Tuning.FreightPerMetre+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel];
                var price=source==null?(decimal)amount*State.Tuning.ImportPrices[k]:0;
                if(!ProjectPay(p,price+fee,p.IsMaintenance?MaintenanceCategory(p):"Obras e materiais",(source==null?"Importação de ":"Transporte municipal de ")+k+"; produto, frete e combustível")){p.Cause=WorkCause.RemainingCommitmentInsufficient;continue;}
                if(source==null)State.OutsideStock.AddAmount(k,-(amount));State.OutsideStock.AddAmount(ResourceKind.Fuel,-(fuel));vehicle.AddFuel(fuel);
                var shipment=new Shipment{Id=Id(),Purpose=p.IsMaintenance?ShipmentPurpose.Maintenance:ShipmentPurpose.Construction,ProjectId=p.Id,SourceBuildingId=source?.Id,DestinationBuildingId=p.BuildingId,Destination=access.Position,VehicleId=vehicle.Id,Cargo=new(){{k,amount}},PricePaid=Money(price+fee),PickedUp=source==null};
                shipment.ReceivingMachineId=receiver?.Id;State.Shipments.Add(shipment);vehicle.ConstructionAccess=true;vehicle.ShipmentId=shipment.Id;vehicle.Cargo=source==null?new(shipment.Cargo):new();vehicle.Route=route;vehicle.RouteIndex=0;vehicle.DockPosition=DockAt(source==null?access:Access(source)!,source??BuildingById(p.BuildingId),vehicle.CrewAboard);vehicle.Status=VehicleStatus.Travelling;vehicle.Position=State.Entry;vehicle.Cause="Entrega para obra "+p.Id;
                // A crew truck waits at the site. Bringing the crew with the
                // first load can occupy the only free truck while more loads
                // remain. Bring it with the final committed delivery instead.
                var allMaterialsDispatched=p.Required.All(required=>p.Delivered.GetValueOrDefault(required.Key)+State.Shipments.Where(s=>s.ProjectId==p.Id&&!s.Complete).Sum(s=>s.Cargo.GetValueOrDefault(required.Key))+.001>=required.Value);
                if(!IsPaving(p)&&p.CrewVehicleId==null&&p.LocalCrew.Count==0&&allMaterialsDispatched){vehicle.CrewAboard=true;vehicle.CrewCount=3;p.CrewVehicleId=vehicle.Id;}
                vehicle.DockPosition=DockAt(source==null?access:Access(source)!,source??BuildingById(p.BuildingId),vehicle.CrewAboard);
                if(source==null&&receiver!=null)ConfigureAsphaltApproach(vehicle,shipment);
                p.Cause=WorkCause.MaterialsInTransit;
            }
            if(!IsPaving(p))AssignLocalCrew(p,access);
            if(IsPaving(p)){OrderPavingMachines(p,access);continue;}
            if(p.CrewVehicleId==null&&p.LocalCrew.Count==0&&p.Required.All(k=>p.Delivered.GetValueOrDefault(k.Key)+.001>=k.Value))SendExternalCrew(p,access);
        }
    }
    // Same margin as the dispatch checks: the leg, then the way back to the exterior.
    bool FuelForTripAndExit(Vehicle v,List<int> route,RoadNode destination)=>FindRoadRoute(destination.Id,EntryNode().Id,true) is {Count:>0} exit&&(RouteLength(route)+RouteLength(exit))*FuelRate(v)+4<=v.Fuel;
    void SendExternalCrew(WorkProject project,RoadNode access)
    {
        var vehicle=AvailableTruck();if(vehicle==null)return;var route=FindRoadRoute(EntryNode().Id,access.Id,true);if(route.Count==0)return;
        if(RouteLength(route)*2*FuelRate(vehicle)+4>State.Tuning.FuelTank){project.Cause=WorkCause.RouteBeyondTruckRange;return;}
        var fuel=Math.Max(0,State.Tuning.FuelTank-vehicle.Fuel);if(State.OutsideStock[ResourceKind.Fuel]<fuel)return;var fee=State.Tuning.FreightBase+(decimal)(RouteLength(route)*2)*State.Tuning.FreightPerMetre+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel];
        if(!ProjectPay(project,fee,project.IsMaintenance?MaintenanceCategory(project):"Equipe externa","Transporte físico e combustível da equipe externa")){project.Cause=WorkCause.CommitmentInsufficientForCrew;return;}
        State.OutsideStock.AddAmount(ResourceKind.Fuel,-(fuel));vehicle.AddFuel(fuel);var shipment=new Shipment{Id=Id(),Purpose=project.IsMaintenance?ShipmentPurpose.Maintenance:ShipmentPurpose.Construction,ProjectId=project.Id,DestinationBuildingId=project.BuildingId,VehicleId=vehicle.Id,PickedUp=true,PricePaid=Money(fee)};State.Shipments.Add(shipment);vehicle.ConstructionAccess=true;vehicle.ShipmentId=shipment.Id;vehicle.Cargo.Clear();vehicle.Route=route;vehicle.RouteIndex=0;vehicle.Position=State.Entry;vehicle.Status=VehicleStatus.Travelling;vehicle.CrewAboard=true;vehicle.CrewCount=3;vehicle.DockPosition=DockAt(access,BuildingById(project.BuildingId),true);project.CrewVehicleId=vehicle.Id;
    }
    void AssignLocalCrew(WorkProject p,RoadNode access)
    {
        p.LocalCrew.RemoveAll(id=>State.Citizens.All(c=>c.Id!=id||!c.Alive||c.ConstructionProjectId!=p.Id||BuildingById(c.WorkplaceId)?.Kind!=BuildingKind.WorksDepot));
        if(p.CrewVehicleId!=null||p.LocalCrew.Count>0)return;
        var depot=State.Buildings.FirstOrDefault(b=>b.Kind==BuildingKind.WorksDepot&&b.Completed&&!b.Removed&&!b.OperationClosed&&Access(b)!=null);if(depot==null)return;
        foreach(var c in State.Citizens.Where(c=>c.Alive&&c.WorkplaceId==depot.Id&&c.ConstructionProjectId==null&&!c.Moving&&c.Position.Distance(depot.Door)<3&&ShiftActive(c)).Take(3)){
            var target=CrewPedestrianSite(access);var route=PedestrianPath(c.Position,target,access);if(route.Count==0)continue;Pr57CitizenAgendaWake(c);c.ConstructionProjectId=p.Id;c.Purpose=JourneyPurpose.Construction;c.Path=route;c.PathIndex=0;c.PathRoadRevision=State.RoadRevision;c.Activity="Indo ao canteiro";p.LocalCrew.Add(c.Id);Personal(c,"Equipe municipal enviada à obra "+p.Id);
        }
    }
    bool ProgressConstruction(double dt)
    {
        var completedConstruction=false;
        foreach(var p in State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress)){
            if(RetireMaintenanceWithoutTargets(p))continue;
            if(IsPaving(p)&&!RoadPhysicalWorkDone(p)){if(ProgressPaving(p,dt)){if(!WaitForRoadConnection(p))completedConstruction|=CompleteConstructionProject(p);}continue;}
            if(RoadPhysicalWorkDone(p)){
                if(!PavingSiteCleared(p)){p.Cause=WorkCause.PavingInProgress;continue;}
                if(!WaitForRoadConnection(p))completedConstruction|=CompleteConstructionProject(p);
                continue;
            }
            var access=WorkAccess(p);if(access==null){p.Cause=WorkCause.NoRoadAccess;continue;}
            var completeMaterials=p.Required.All(kv=>p.Delivered.GetValueOrDefault(kv.Key)+.001>=kv.Value);if(!completeMaterials){
                // Old saves can contain a crew parked with the first load and
                // no truck left to deliver the rest. Return that actual crew
                // to the exterior, then reuse its truck for missing materials.
                var missingUnordered=p.Required.Any(required=>p.Delivered.GetValueOrDefault(required.Key)+State.Shipments.Where(s=>s.ProjectId==p.Id&&!s.Complete).Sum(s=>s.Cargo.GetValueOrDefault(required.Key))+.001<required.Value);
                if(missingUnordered&&p.CrewVehicleId is {} earlyCrewId&&VehicleById(earlyCrewId) is {Status:VehicleStatus.Parked,CrewAboard:true} earlyCrew){p.CrewVehicleId=null;ReturnExternal(earlyCrew);}
                p.Cause=State.Shipments.Any(s=>s.ProjectId==p.Id&&!s.Complete)?WorkCause.AwaitingPhysicalDelivery:WorkCause.MissingMaterials;continue;
            }
            var crew=p.CrewVehicleId==null?null:State.Vehicles.FirstOrDefault(v=>v.Id==p.CrewVehicleId&&v.CrewAboard);
            if(crew is {Status:VehicleStatus.Parked}&&crew.Position.Distance(DockAt(access,BuildingById(p.BuildingId),true))>2.5){
                // A newly built point can change the work's bay. The crew is
                // still at its recorded physical bay; a closer disconnected
                // road across the gap must not become its departure node.
                var atKnownBay=crew.DockPosition is {} dock&&crew.Position.Distance(dock)<.1&&crew.Route.Count>0&&crew.RouteIndex>=crew.Route.Count&&_roads.ContainsKey(crew.Route[^1]);
                var start=atKnownBay?_roads[crew.Route[^1]]:NearestRoad(crew.Position);
                if(start!=null){var route=FindRoadRoute(start.Id,access.Id,true);if(route.Count>0){
                    // SPEC transporte-circulacao: each move is a departure. Without fuel
                    // for it and the return to the exterior, the crew goes back to refuel
                    // on import and the work calls a crew again; it never strands on a lane.
                    if(!FuelForTripAndExit(crew,route,access)){p.CrewVehicleId=null;ReturnExternal(crew);if(crew.Status==VehicleStatus.Travelling)crew.Cause="Retorno ao exterior para reabastecer: combustível não basta para o próximo trecho e a volta";}
                    else{crew.Route=route;crew.RouteIndex=0;crew.DockPosition=DockAt(access,BuildingById(p.BuildingId),true);crew.Status=VehicleStatus.Travelling;}
                }}
            }
            var external=crew is {Status:VehicleStatus.Parked}&&crew.Position.Distance(DockAt(access,BuildingById(p.BuildingId),true))<=2.5?crew:null;
            RelocateMaintenanceCrew(p,access);
            var local=p.LocalCrew.Select(id=>CitizenWithId(id)).Where(c=>c.Alive&&c.ConstructionProjectId==p.Id&&BuildingById(c.WorkplaceId)?.Kind==BuildingKind.WorksDepot&&!c.Moving&&c.Position.Distance(access.Position)<State.Tuning.RoadWidth&&ShiftActive(c)).ToList();
            if(external==null&&local.Count==0){p.Cause=WorkCause.AwaitingCrewOnSite;continue;}
            if(!HasExteriorWorkAccess(p)){p.Cause=WorkCause.WorkRouteInterrupted;continue;}
            var factor=external!=null?1:local.Count/3.0;var used=Math.Min(dt*factor,p.WorkSeconds*(1-p.Progress));
            if(external!=null){var due=Money((decimal)(used/3600)*State.Tuning.ExternalCrewHourlyPrice);if(due>p.BudgetRemaining){p.Cause=WorkCause.NoBudgetForExternalCrew;continue;}p.CrewCostDue+=(decimal)(used/3600)*State.Tuning.ExternalCrewHourlyPrice;if(p.CrewCostDue>=1||p.Progress+used/p.WorkSeconds>=.999999){var amount=Money(p.CrewCostDue);if(!ProjectPay(p,amount,p.IsMaintenance?MaintenanceCategory(p):"Equipe externa",p.IsMaintenance?"Manutenção efetivamente realizada por equipe externa":"Trabalho efetivamente realizado na obra")){p.Cause=WorkCause.ExternalCrewAwaitingPayment;continue;}p.CrewCostDue=0;}}
            
            p.Status=WorkStatus.InProgress;p.AddProgress(used/p.WorkSeconds);p.ProgressUnits=Math.Min(FixedPoint.PointScale,p.ProgressUnits);if(p.Progress>=.999999)p.Progress=1;p.Cause=p.IsMaintenance?WorkCause.MaintenanceInProgress:WorkCause.UnderConstruction;
            if(p.IsMaintenance)ProgressRoadMaintenance(p);

            if(p.Progress<.999999)continue;
            if(RoadPhysicalWorkDone(p)&&WaitForRoadConnection(p))continue;
            completedConstruction|=CompleteConstructionProject(p);
        }
        return completedConstruction;
    }
    static bool RoadPhysicalWorkDone(WorkProject p)=>!p.IsMaintenance&&p.RoadPoints.Count>0&&p.Progress>=1&&p.BuiltRoadNodes.Count>=p.RoadPoints.Count;
    bool WaitForRoadConnection(WorkProject p)
    {
        var connected=true;
        for(var i=0;i<p.RoadPoints.Count;i++){
            // Usual case: linear in this work, with existing graph lookups.
            // A demolished/rebuilt point can have a different real node ID.
            var node=State.Roads.FirstOrDefault(road=>road.Position.Distance(p.RoadPoints[i])<.05);
            if(node==null||node.Position.Distance(p.RoadPoints[i])>=.05)node=State.Roads.FirstOrDefault(road=>road.Position.Distance(p.RoadPoints[i])<.05);
            if(node==null||!_workExteriorConnected.Contains(node.Id)){connected=false;break;}
        }
        if(connected)return false;
        p.Status=WorkStatus.Waiting;p.Cause=WorkCause.RoadBuiltAwaitingLink;
        // The paid physical work is done. Return the actual crew/truck once;
        // waiting for a different road must not consume or summon another crew.
        ReleaseConstructionCrew(p);return true;
    }
    void ReleaseConstructionCrew(WorkProject p)
    {
        ReleasePavingMachines(p);
        if(p.CrewVehicleId is {} id&&State.Vehicles.FirstOrDefault(v=>v.Id==id&&v.CrewAboard) is {} crew){crew.CrewAboard=false;crew.CrewCount=0;ReturnExternal(crew);}
        p.CrewVehicleId=null;
        foreach(var citizenId in p.LocalCrew){var c=CitizenWithId(citizenId);c.ConstructionProjectId=null;var job=BuildingById(c.WorkplaceId);if(job!=null)StartJourney(c,job,JourneyPurpose.Work);}p.LocalCrew.Clear();
    }
    bool CompleteConstructionProject(WorkProject p)
    {
        if(p.CrewCostDue>0){var due=Money(p.CrewCostDue);if(!ProjectPay(p,due,IsPaving(p)?"Máquinas de pavimentação":"Equipe externa","Saldo do trabalho efetivamente realizado")){p.Cause=WorkCause.NoBudgetForPaving;return false;}if(IsPaving(p))p.MachineryPaid+=due;p.CrewCostDue=0;}
        if(IsPaving(p))OpenCompactedRoads(p);
        p.Progress=1;p.Status=WorkStatus.Completed;p.CompletedAt=State.Now;p.BudgetRemaining=0;p.Cause=WorkCause.Completed;
        if(p.IsMaintenance)CompleteMaintenance(p);
        else if(BuildingById(p.BuildingId) is {} b){b.Completed=true;if(!b.Definition.Private)b.OwnerAccount="city";b.Cause=b.Definition.Private?"Procurando adquirente":"Aguardando funcionários";b.MaintenanceDueAt=State.ElapsedSeconds+State.Tuning.PublicMaintenanceIntervalHours*3600;UpdateCrosswalks();Event(b.Definition.Name+" concluída");}else Event("Rua concluída");
        ReleaseConstructionCrew(p);return !p.IsMaintenance;
    }
    // Only cells the roller compacted carry WorkProjectId; planned points never became roads.
    void OpenCompactedRoads(WorkProject p){foreach(var road in State.Roads.Where(r=>r.WorkProjectId==p.Id))road.ConstructionOnly=false;RebuildRoadGraph();SetDefaultJunctions();}
    bool HasExteriorWorkAccess(WorkProject p)=>WorkAccess(p) is {} n&&_workExteriorConnected.Contains(n.Id);
    public CommandResult CancelProject(int projectId)
    {
        var p=State.Projects.FirstOrDefault(p=>p.Id==projectId);if(p==null||p.IsMaintenance||p.Status is WorkStatus.Completed or WorkStatus.Cancelled)return new(false,"Obra não cancelável");
        CancelWork(p);Event("Obra cancelada; pagamentos efetivos preservados");EndRelocationOfCancelledWork(p);return new(true,State.LastMessage);
    }
    void CancelWork(WorkProject p)
    {
        if(p.CrewCostDue>0){ProjectPay(p,Money(p.CrewCostDue),p.IsMaintenance?MaintenanceCategory(p):"Equipe externa","Trabalho realizado antes do cancelamento");p.CrewCostDue=0;}
        ReleasePavingMachines(p);
        if(IsPaving(p))OpenCompactedRoads(p);
        p.Status=WorkStatus.Cancelled;p.BudgetRemaining=0;p.LocalReserved.Clear();p.Cause=WorkCause.Cancelled;
        if(!p.IsMaintenance&&BuildingById(p.BuildingId) is {} b){b.Removed=true;_buildingRemovals++;}
        foreach(var s in State.Shipments.Where(s=>s.ProjectId==p.Id&&!s.Complete)){s.Cancelled=true;var v=State.Vehicles.First(x=>x.Id==s.VehicleId);RedirectCancelled(v,s);}
        if(p.CrewVehicleId is {} vid&&VehicleById(vid) is {} crew&&crew.Status==VehicleStatus.Parked){crew.CrewAboard=false;crew.CrewCount=0;ReturnExternal(crew);}
        foreach(var id in p.LocalCrew){var c=CitizenWithId(id);c.ConstructionProjectId=null;if(BuildingById(c.WorkplaceId) is {} workplace)StartJourney(c,workplace,JourneyPurpose.Work);}p.LocalCrew.Clear();ReserveLocalMaterials();
    }
    public PlacementQuote QuoteMoveBuilding(int projectId,Point position,double rotation)
    {
        var project=State.Projects.FirstOrDefault(p=>p.Id==projectId&&!p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress);var building=project==null?null:BuildingById(project.BuildingId);
        return building==null?new(false,false,0,new(),"Selecione obra de edifício inacabada"):QuoteBuilding(building.Kind,position,rotation,projectId);
    }
    public CommandResult MoveBuildingProject(int projectId,Point position,double rotation)
    {
        var project=State.Projects.FirstOrDefault(p=>p.Id==projectId&&!p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress);var building=project==null?null:BuildingById(project.BuildingId);if(project==null||building==null)return new(false,"Obra não pode ser movida");
        var quote=QuoteMoveBuilding(projectId,position,rotation);if(!quote.PhysicallyValid)return new(false,quote.Message);if(quote.Total>Available+project.BudgetRemaining-Money(project.CrewCostDue))return new(false,"Caixa insuficiente após liberar o compromisso antigo");
        var kind=building.Kind;if(ActiveRelocationForProject(projectId) is {} relocation)return MoveRelocationWork(relocation,project,position,rotation);
        var cancelled=CancelProject(projectId);if(!cancelled.Success)return cancelled;return PlaceBuilding(kind,position,rotation);
    }
    public CommandResult MoveRoadProject(int projectId,Point start,Point end,bool reverse)
    {
        var project=State.Projects.FirstOrDefault(p=>p.Id==projectId&&!p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress);if(project==null||project.RoadPoints.Count==0)return new(false,"Selecione obra viária inacabada");
        var quote=QuoteRoad(start,end,reverse,projectId);if(!quote.PhysicallyValid)return new(false,quote.Message);if(quote.Total>Available+project.BudgetRemaining-Money(project.CrewCostDue))return new(false,"Caixa insuficiente após liberar compromisso antigo");var cancelled=CancelProject(projectId);if(!cancelled.Success)return cancelled;return PlaceRoad(start,end,reverse);
    }
}
