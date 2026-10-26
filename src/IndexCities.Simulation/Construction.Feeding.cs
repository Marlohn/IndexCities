namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57-only A/B: zero distance has no swept path. Do not build machine/site
    // reservations when no displacement can occur; off by default.
    static readonly bool Pr57ZeroSiteSkip=Environment.GetEnvironmentVariable("PR57_ZERO_SITE_SKIP")=="1";
    (RoadNode Road,Point Position) PavingBay(RoadNode access,PavingMachine kind,WorkProject? work=null)
    {
        var route=FindRoadRoute(EntryNode().Id,access.Id,true);
        var index=Math.Max(1,route.Count-1-(kind==PavingMachine.Paver?4:7));index=Math.Min(index,route.Count-1);
        foreach(var i in Enumerable.Range(0,route.Count).OrderBy(i=>Math.Abs(i-index)).ThenBy(i=>i)){var road=_roads[route[i]];var dock=DockAt(road,null,false);if(kind==PavingMachine.Roller)dock=road.Position*2-dock;
            var axis=i>0?road.Position-_roads[route[i-1]].Position:new Point(1,0);
            // Feeding happens in front of the paver, facing back along its
            // approach. A paver clear of a corner is insufficient if its real
            // full-size feeder would stand across the crossing carriageway.
            var feeder=dock-axis*(6.6/Math.Max(.001,axis.Distance(default)));
            if(kind==PavingMachine.Paver&&BayBodyBlocksRoad(feeder,Math.Abs(axis.X)>.01,3.6,1.45))continue;
            // Nor on the street being built (both machines drive over its points),
            // nor where trucks turning at a nearby junction sweep.
            if(InTurnSweep(new Vehicle{Machine=kind},dock,Math.Abs(axis.X)>.01?Math.PI/2:0))continue;
            if(work!=null&&WorkRoadBlocksBay(work,dock,Math.Abs(axis.X)>.01))continue;
            // Nor on the dock of another work still receiving material: the machine would wait in the lane
            // inside the way out of the truck unloading there.
            if(State.Projects.Any(p=>p.Id!=work?.Id&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress&&p.Required.Any(k=>p.Delivered.GetValueOrDefault(k.Key)+.001<k.Value)&&WorkAccess(p) is {} other&&DockAt(other,BuildingById(p.BuildingId),false).Distance(dock)<=7))continue;
            if(!MachineBayBlocksRoad(dock,Math.Abs(axis.X)>.01)&&State.Buildings.All(b=>b.Removed||!Inside(b,dock,2.5))&&State.Vehicles.All(v=>v.Status==VehicleStatus.Outside||v.DockPosition is not {} occupied||occupied.Distance(dock)>7))return(road,dock);
        }
        // Exterior staging origin already exists in the road model. Equipment
        // still travels to/from it and retains its position while waiting.
        var entry=EntryNode();return(entry,entry.Position+new Point(0,kind==PavingMachine.Paver?6.75:-6.75));
    }

    // The poses a full-size truck sweeps through each way across a shared node,
    // as the lane model drives it (the same turns the stop lines keep clear of).
    readonly Dictionary<int,List<(Vehicle Body,Point Position,double Heading)>> _turnSweeps=[];int _turnSweepsEpoch=-1;
    List<(Vehicle Body,Point Position,double Heading)> TurnSweep(RoadNode junction)
    {
        if(_turnSweepsEpoch!=_roadEpoch){_turnSweeps.Clear();_turnSweepsEpoch=_roadEpoch;}
        if(_turnSweeps.TryGetValue(junction.Id,out var cached))return cached;
        var poses=new List<(Vehicle Body,Point Position,double Heading)>();var truck=new Vehicle();
        foreach(var fromId in junction.Neighbours)foreach(var toId in junction.Neighbours.Where(id=>id!=fromId)){
            if(!_roads.TryGetValue(fromId,out var from)||!_roads.TryGetValue(toId,out var to))continue;
            var direction=junction.Position-from.Position;var metres=direction.Distance(default);if(metres<=.01)continue;
            var start=from.Position+new Point(direction.Z/metres,-direction.X/metres)*1.65;var route=new List<int>{fromId,junction.Id,toId};
            var continuation=to.Neighbours.FirstOrDefault(id=>id!=junction.Id);if(continuation!=0)route.Add(continuation);
            CrossingPoses(poses,truck,route,1,start,Math.Atan2(direction.X,direction.Z),junction);
        }
        return _turnSweeps[junction.Id]=poses;
    }
    bool InTurnSweep(Vehicle body,Point at,double heading)=>SharedNodes().Where(n=>n.Position.Distance(at)<20)
        .Any(n=>TurnSweep(n).Any(p=>p.Position.Distance(at)<12&&BodiesOverlap(body,at,heading,p.Body,p.Position,p.Heading)));

    // A machine bay overlapping the carriageway of a point of the work's own new
    // street (its orientation is not known yet, so on both axes).
    bool WorkRoadBlocksBay(WorkProject work,Point bay,bool horizontalMachine)
    {
        var halfX=horizontalMachine?2.31:1.27;var halfZ=horizontalMachine?1.27:2.31;var lane=State.Tuning.RoadWidth*(6.5/10.5)/2;
        return work.RoadPoints.Any(point=>Math.Abs(bay.X-point.X)<halfX+lane+.2&&Math.Abs(bay.Z-point.Z)<halfZ+lane+.2);
    }

    bool MachineBayBlocksRoad(Point bay,bool horizontalMachine)
        =>BayBodyBlocksRoad(bay,horizontalMachine,2.31,1.27);

    bool BayBodyBlocksRoad(Point bay,bool horizontalMachine,double halfLength,double halfWidth,Func<RoadNode,bool>? exempt=null)
    {
        var halfX=horizontalMachine?halfLength:halfWidth;var halfZ=horizontalMachine?halfWidth:halfLength;
        var lane=State.Tuning.RoadWidth*(6.5/10.5)/2;var step=State.Tuning.RoadStep/2;
        foreach(var road in State.Roads){
            if(exempt?.Invoke(road)==true)continue;
            var horizontal=road.Highway||road.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-road.Position.X)>.01);
            var vertical=road.Neighbours.Any(id=>Math.Abs(_roads[id].Position.Z-road.Position.Z)>.01);
            var roadX=vertical?lane:step;var roadZ=horizontal?lane:step;
            if(Math.Abs(bay.X-road.Position.X)<halfX+roadX+.2&&Math.Abs(bay.Z-road.Position.Z)<halfZ+roadZ+.2)return true;
        }
        return false;
    }

    List<Point> PavingApproachRoute(WorkProject p,Point from,Point target)
    {
        var route=PavingSiteRoute(p,from,target);var result=new List<Point>();
        for(var i=0;i<route.Count;i++){
            var axis=i==0?(route.Count>1?route[1]-route[0]:route[0]-from):route[i]-route[i-1];var length=axis.Distance(default);
            result.Add(i<route.Count-1&&length>.01&&!p.RoadPoints.Contains(route[i])?route[i]+new Point(axis.Z/length,-axis.X/length)*1.65:route[i]);
        }
        return result;
    }

    Vehicle? AsphaltReceiver(WorkProject p)=>State.Vehicles.FirstOrDefault(v=>v.Id==p.PaverId&&v.MachineProjectId==p.Id&&v.MachinePhase==MachinePhase.OnSite&&v.FeedingReady&&v.CrewAboard&&v.CrewCount>0&&v.Fuel>0);

    (List<int> Route,List<Point> Site,Point Dock) AsphaltApproach(Vehicle paver,List<int> route)
    {
        var forward=new Point(Math.Sin(paver.Heading),Math.Cos(paver.Heading));var side=new Point(forward.Z,-forward.X)*(State.Tuning.RoadWidth/2-1.5);
        route=FindRoadRoute(route[0],paver.Route[^1],true);
        var dock=paver.Position+forward*6.6;
        while(route.Count>1&&_roads[route[^1]].Position.Distance(paver.Position)<12)route.RemoveAt(route.Count-1);
        var at=_roads[route[^1]].Position;
        var bayOffset=paver.Position-_roads[paver.Route[^1]].Position;
        return(route,[at,at+bayOffset,dock+forward*4,dock],dock);
    }

    void ConfigureAsphaltApproach(Vehicle truck,Shipment shipment)
    {
        if(shipment.ReceivingMachineId is not {} id)return;
        var paver=State.Vehicles.First(v=>v.Id==id);var approach=AsphaltApproach(paver,truck.Route);
        truck.Route=approach.Route;truck.SitePath=approach.Site;truck.SitePathIndex=0;truck.SitePathBeforeRoad=false;
        truck.SiteProjectId=shipment.ProjectId;truck.SiteDockHeading=paver.Heading;truck.DockPosition=approach.Dock;
    }

    bool MoveConstructionSitePath(Vehicle vehicle,ref double remaining,double speed)
    {
        if(vehicle.Machine==PavingMachine.None&&YieldAsphaltApproach(vehicle,ref remaining,speed))return false;
        while(remaining>.000001&&vehicle.SitePathIndex<vehicle.SitePath.Count){
            var target=vehicle.SitePath[vehicle.SitePathIndex];var delta=target-vehicle.Position;var distance=delta.Distance(default);
            var travel=Math.Min(distance,Math.Min(speed*remaining,vehicle.Fuel/State.Tuning.FuelPerMetre));
            if(travel<=.000001&&distance>.001){vehicle.Cause="Sem combustível para manobra no canteiro";return false;}
            // A blocked body keeps its verified pose: turning in place toward the
            // next point is untested occupancy and can lock an adjacent vehicle.
            var previousHeading=vehicle.Heading;
            if(distance>.001)vehicle.Heading=vehicle.SiteDockHeading is {} heading&&vehicle.SitePathIndex==vehicle.SitePath.Count-1?heading:Math.Atan2(delta.X,delta.Z);
            travel=vehicle.Machine!=PavingMachine.None&&!MachineMayEnter(vehicle)?0:SafeSiteTravel(vehicle,target,travel);
            if(travel<=.000001&&distance>.001){vehicle.Heading=previousHeading;vehicle.Cause=WorkCause.PavingSiteOccupied;return false;}
            vehicle.Position=vehicle.Position.MoveTowards(target,travel);vehicle.AddFuel(-(travel*State.Tuning.FuelPerMetre));vehicle.FuelUnits=Math.Max(0,vehicle.FuelUnits);vehicle.AddFuelBurnedLastStep(travel*State.Tuning.FuelPerMetre);remaining-=travel/speed;
            if(travel>=distance-.000001)vehicle.SitePathIndex++;else return false;
        }
        return vehicle.SitePathIndex>=vehicle.SitePath.Count;
    }

    // PR57 opt-in: one conservative owner roster per movement slice. A normal
    // passenger car cannot own a paver path, asphalt delivery, or dock manoeuvre.
    // Include any externally edited car with a special path, dock or shipment;
    // rebuild at every unsplit slice (and every public Advance boundary).
    // This is a SUPerset: live predicates and exact body tests remain unchanged.
    static readonly bool Pr57SiteOwnerRosterEnabled =
        Environment.GetEnvironmentVariable("PR57_SITE_OWNER_ROSTER")=="1";
    List<Vehicle>? _pr57SiteOwnerRoster;
    List<Vehicle>? _pr57SiteOwnerSource;
    List<Vehicle>.Enumerator _pr57SiteOwnerListVersion;
    int _pr57SiteOwnerVehicleCount;
    bool _pr57SiteOwnerRosterAnnounced, _pr57SiteOwnerRosterUsedAnnounced;
    void Pr57PrepareSiteOwnerRoster()
    {
        if(!Pr57SiteOwnerRosterEnabled){_pr57SiteOwnerRoster=null;return;}
        _pr57SiteOwnerSource=State.Vehicles;
        _pr57SiteOwnerListVersion=State.Vehicles.GetEnumerator();
        _pr57SiteOwnerVehicleCount=State.Vehicles.Count;
        _pr57SiteOwnerRoster=State.Vehicles.Where(v=>!v.PassengerCar||
            v.Machine!=PavingMachine.None||v.SitePath.Count>0||
            v.ManoeuvreDock!=null||v.ShipmentId!=null).ToList();
        if(!_pr57SiteOwnerRosterAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine($"PR57_SITE_OWNER_ROSTER active=True candidates={_pr57SiteOwnerRoster.Count} vehicles={State.Vehicles.Count}");
            _pr57SiteOwnerRosterAnnounced=true;
        }
    }

    // PR57 #161: reuse capacity only. The buffer is cleared for every physical
    // query and after its result is consumed; no reservation decision survives
    // an Advance(2), slice, vehicle mutation, or even the next query.
    static readonly bool Pr57SafeSiteBufferEnabled =
        Environment.GetEnvironmentVariable("PR57_SAFE_SITE_BUFFER")=="1";
    readonly List<(Vehicle Machine,Point Position,double Heading)> _pr57SafeSiteReservationScratch=[];
    bool _pr57SafeSiteBufferAnnounced;
    List<(Vehicle Machine,Point Position,double Heading)> Pr57CollectSiteReservations(
        Vehicle vehicle,List<Vehicle> siteOwners,double distance,bool machinesOnly)
    {
        var reserved=_pr57SafeSiteReservationScratch;
        reserved.Clear();
        var reach=distance+12;
        if(vehicle.Machine==PavingMachine.None)
            foreach(var machine in siteOwners)
            {
                if(machine.Id==vehicle.Id||machine.Machine==PavingMachine.None)continue;
                foreach(var pose in MachineReservation(machine))
                    if(pose.Position.Distance(vehicle.Position)<=reach)reserved.Add(pose);
            }
        if(machinesOnly)
        {
            foreach(var truck in siteOwners)
            {
                if(truck.Id==vehicle.Id||!OnSitePath(truck)||truck.Position.Distance(vehicle.Position)>distance+40)continue;
                foreach(var pose in SiteTruckReservation(truck))
                    if(pose.Position.Distance(vehicle.Position)<=reach)reserved.Add(pose);
            }
            foreach(var other in siteOwners)
            {
                if(other.Id==vehicle.Id||other.ManoeuvreDock==null||OnSitePath(other)||
                   other.Position.Distance(vehicle.Position)>distance+40)continue;
                foreach(var pose in AdmittedManoeuvre(other))
                    if(pose.Position.Distance(vehicle.Position)<=reach)reserved.Add(pose);
            }
            foreach(var truck in siteOwners)
            {
                if(truck.Id==vehicle.Id||!(OnAsphaltApproach(truck)||UnloadingAsphaltBody(truck))||
                   truck.Position.Distance(vehicle.Position)>reach||
                   (truck.Position.Distance(State.Entry)<8&&vehicle.Position.Distance(State.Entry)<8))continue;
                reserved.Add((truck,truck.Position,truck.Heading));
            }
        }
        return reserved;
    }

    double SafeSiteTravel(Vehicle vehicle,Point target,double distance,bool machinesOnly=false)
    {
        if(Pr57ZeroSiteSkip&&distance==0)return 0;
        // List<T>.Enumerator detects same-count replacements, reversals and
        // remove/add pairs: Count alone cannot protect an in-slice roster.
        // This is O(1) per query, with the original scan as the fallback.
        // The public engine is synchronous; object-property edits between
        // Advance(2) calls are covered by rebuilding at each movement slice.
        var rosterValid=Pr57SiteOwnerRosterEnabled&&_pr57SiteOwnerRoster is not null&&
            ReferenceEquals(_pr57SiteOwnerSource,State.Vehicles)&&
            _pr57SiteOwnerVehicleCount==State.Vehicles.Count;
        if(rosterValid)
        {
            try{_pr57SiteOwnerListVersion.MoveNext();}
            catch(InvalidOperationException){rosterValid=false;_pr57SiteOwnerRoster=null;}
        }
        var siteOwners=rosterValid?_pr57SiteOwnerRoster!:State.Vehicles;
        if(Pr57SiteOwnerRosterEnabled&&ReferenceEquals(siteOwners,_pr57SiteOwnerRoster)&&
           !_pr57SiteOwnerRosterUsedAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine($"PR57_SITE_OWNER_ROSTER used=True candidates={siteOwners.Count} vehicles={State.Vehicles.Count}");
            _pr57SiteOwnerRosterUsedAnnounced=true;
        }
        // A moving machine holds its whole remaining path, not only its body:
        // a vehicle stopped anywhere on it (behind a turn, across an oncoming
        // lane) would wait for the machine while the machine waits for it.
        // The hold stops at the first body already on the path: space behind a
        // blocked machine is not held, and whoever stands on the path may leave.
        // Only a paving machine has reserved poses (MachineReservation is empty otherwise).
        List<(Vehicle Machine,Point Position,double Heading)> reserved;
        if(Pr57SafeSiteBufferEnabled)
        {
            reserved=Pr57CollectSiteReservations(vehicle,siteOwners,distance,machinesOnly);
            if(!_pr57SafeSiteBufferAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
            {
                Console.WriteLine("PR57_SAFE_SITE_BUFFER used=True");
                _pr57SafeSiteBufferAnnounced=true;
            }
        }
        else
        {
            reserved=vehicle.Machine!=PavingMachine.None?[]:siteOwners.Where(m=>m.Id!=vehicle.Id&&m.Machine!=PavingMachine.None).Select(MachineReservation)
                .SelectMany(poses=>poses).Where(pose=>pose.Position.Distance(vehicle.Position)<=distance+12).ToList();
            // Lane traffic also respects an asphalt truck on its site path (see
            // Construction.ApproachYield).
            if(machinesOnly)reserved.AddRange(siteOwners.Where(t=>t.Id!=vehicle.Id&&OnSitePath(t)&&t.Position.Distance(vehicle.Position)<=distance+40)
                .SelectMany(SiteTruckReservation).Where(pose=>pose.Position.Distance(vehicle.Position)<=distance+12));
            // And an admitted way into or out of a bay (Mobility.SharedSpace).
            if(machinesOnly)reserved.AddRange(siteOwners.Where(t=>t.Id!=vehicle.Id&&t.ManoeuvreDock!=null&&!OnSitePath(t)&&t.Position.Distance(vehicle.Position)<=distance+40)
                .SelectMany(AdmittedManoeuvre).Where(pose=>pose.Position.Distance(vehicle.Position)<=distance+12));
            // An asphalt truck on its site path or unloading at the paver bay keeps
            // its present body there (see Construction.ApproachYield). Trucks still
            // stacked together at the exterior staging origin have not entered yet.
            if(machinesOnly)reserved.AddRange(siteOwners.Where(t=>t.Id!=vehicle.Id&&(OnAsphaltApproach(t)||UnloadingAsphaltBody(t))&&t.Position.Distance(vehicle.Position)<=distance+12&&
                    !(t.Position.Distance(State.Entry)<8&&vehicle.Position.Distance(State.Entry)<8))
                .Select(t=>(t,t.Position,t.Heading)));
        }
        if(machinesOnly&&reserved.Count==0&&!siteOwners.Any(other=>other.Machine!=PavingMachine.None&&other.Status!=VehicleStatus.Outside&&other.Position.Distance(vehicle.Position)<=distance+12))return distance;
        var safe=0.0;
        while(safe<distance-.000001){var next=Math.Min(distance,safe+.5);var at=vehicle.Position.MoveTowards(target,next);
            if((machinesOnly?siteOwners:State.Vehicles).Any(other=>other.Id!=vehicle.Id&&other.Status!=VehicleStatus.Outside&&(!machinesOnly||other.Machine!=PavingMachine.None&&(other.Status==VehicleStatus.Parked||!(other.Position.Distance(State.Entry)<8&&vehicle.Position.Distance(State.Entry)<8)))&&other.Position.Distance(at)<12&&VehicleBodiesOverlap(vehicle,at,other)))break;
            if(reserved.Any(pose=>pose.Position.Distance(at)<12&&BodiesOverlap(vehicle,at,vehicle.Heading,pose.Machine,pose.Position,pose.Heading)))break;
            safe=next;
        }
        if(Pr57SafeSiteBufferEnabled)_pr57SafeSiteReservationScratch.Clear();
        return safe;
    }

    // Publish the same whole manoeuvre checked by MachineMayEnter, never a
    // prefix of an intention blocked by a body or an unacquired space. The
    // machine's present body and any physically held spaces remain obstacles.
    List<(Vehicle Machine,Point Position,double Heading)> MachineReservation(Vehicle machine)
    {
        var poses=new List<(Vehicle,Point,double)>();
        if(machine.Machine==PavingMachine.None||machine.Status!=VehicleStatus.Parked||machine.MachinePhase is not (MachinePhase.OnSite or MachinePhase.LeavingSite)||machine.SitePathIndex>=machine.SitePath.Count||machine.Fuel<=.000001||machine.Cause is WorkCause.PavingOutOfFuel or WorkCause.NoBudgetForPaving)return poses;
        var way=MachinePathAhead(machine);
        if(BodyOnWay(machine,way)!=null||machine.MachinePhase==MachinePhase.LeavingSite&&ConflictsWithDockWay(machine,way)||SharedNodes().Any(n=>!machine.ManoeuvreSpaces.Contains(n.Id)&&way.Any(p=>EntersArea(machine,p.Position,p.Heading,n))))return poses;
        return way;
    }

    static bool VehicleBodiesOverlap(Vehicle a,Point at,Vehicle b)=>BodiesOverlap(a,at,a.Heading,b,b.Position,b.Heading);

    static double BodyHalfLength(Vehicle v)=>v.PassengerCar?2.1:v.Machine==PavingMachine.Paver?2.31:v.Machine==PavingMachine.Roller?1.99:3.6;
    static double BodyHalfWidth(Vehicle v)=>v.PassengerCar?.9:v.Machine==PavingMachine.Paver?1.27:v.Machine==PavingMachine.Roller?1.16:1.45;

    static bool BodiesOverlap(Vehicle a,Point aAt,double aHeading,Vehicle b,Point bAt,double bHeading,double margin=.1)
    {
        static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
        var af=new Point(Math.Sin(aHeading),Math.Cos(aHeading));var ar=new Point(af.Z,-af.X);
        var bf=new Point(Math.Sin(bHeading),Math.Cos(bHeading));var br=new Point(bf.Z,-bf.X);var delta=bAt-aAt;
        foreach(var axis in new[]{af,ar,bf,br})if(Math.Abs(Dot(delta,axis))>=BodyHalfLength(a)*Math.Abs(Dot(af,axis))+BodyHalfWidth(a)*Math.Abs(Dot(ar,axis))+BodyHalfLength(b)*Math.Abs(Dot(bf,axis))+BodyHalfWidth(b)*Math.Abs(Dot(br,axis))+margin)return false;
        return true;
    }

    List<Point> PavingExitRoute(WorkProject p,Point from,Point target)
    {
        var route=PavingSiteRoute(p,from,target);var result=new List<Point>();
        for(var i=0;i<route.Count;i++){
            var axis=i==0?(route.Count>1?route[1]-route[0]:route[0]-from):route[i]-route[i-1];var length=axis.Distance(default);
            var offset=1.65;
            var frontage=State.Buildings.Where(b=>!b.Removed&&b.Door.Distance(route[i])<State.Tuning.RoadWidth+12).MinBy(b=>b.Door.Distance(route[i]));
            if(frontage!=null&&length>.01){var normal=new Point(axis.Z/length,-axis.X/length);var side=frontage.Door-route[i];offset=(side.X*normal.X+side.Z*normal.Z>=0?-1:1)*(State.Tuning.RoadWidth/2-.85);}
            result.Add(length>.01&&State.Roads.Any(r=>r.Position==route[i])?route[i]+new Point(axis.Z/length,-axis.X/length)*offset:route[i]);
        }
        return result;
    }

    void PrepareConstructionSiteDeparture(Vehicle v)
    {
        if(v.Machine!=PavingMachine.None||v.SitePathBeforeRoad||v.SitePathIndex==0)return;
        v.SitePath=v.SitePath.Take(v.SitePathIndex).Reverse().ToList();v.SitePathIndex=0;v.SitePathBeforeRoad=true;v.SiteDockHeading=null;
    }
}
