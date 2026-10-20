namespace IndexCities.Simulation;

// ARCHITECTURE "Espaços compartilhados do movimento local" (and its 2026-10-20
// complement): a junction, a simple bend and a bay/dock corridor have one owner
// at a time. A manoeuvre into or out of a bay takes, in one operation, every
// shared space its body crosses up to the first place where the whole body can
// wait clear of them; until then it is an intention that holds nothing but its
// body. The admission is explicit state (Vehicle.ManoeuvreDock/ManoeuvreSpaces),
// released when the body has physically left. Existing preferences (right hand,
// through road, signal, crossing, bend arrival order, a lot exit yielding to the
// traffic on the road) are unchanged.
public sealed partial class CitySimulation
{
    // SPEC "Curva simples, um de cada vez": a sharp corner of a single road
    // (60 degrees or more, e.g. an L); gentler curves need no wait.
    bool IsBend(RoadNode node)
    {
        if(node.Neighbours.Count!=2||node.Crosswalk||!_roads.TryGetValue(node.Neighbours[0],out var a)||!_roads.TryGetValue(node.Neighbours[1],out var b))return false;
        var p=a.Position-node.Position;var q=b.Position-node.Position;var lp=p.Distance(default);var lq=q.Distance(default);
        return lp>.01&&lq>.01&&p.X*q.X+p.Z*q.Z>=-.5*lp*lq;
    }
    // Route[index] is a bend v drives through (it comes along the road and goes on).
    bool TurnsAtBend(Vehicle v,int index)=>index>0&&index+1<v.Route.Count&&_roads.TryGetValue(v.Route[index],out var node)&&IsBend(node);
    bool SharedNode(RoadNode node)=>node.Neighbours.Count>=3||IsBend(node);
    readonly List<RoadNode> _sharedNodes=[];int _sharedNodesEpoch=-1;
    List<RoadNode> SharedNodes(){if(_sharedNodesEpoch!=_roadEpoch){_sharedNodes.Clear();foreach(var road in State.Roads)if(SharedNode(road))_sharedNodes.Add(road);_sharedNodesEpoch=_roadEpoch;}return _sharedNodes;}
    bool AtGateway(Vehicle v)=>v.Position.Distance(State.Entry)<8;

    // The body enters the actual carriageway square of a junction or bend.
    // Its own extent is added once; JunctionClearance already includes a truck
    // length and would also reserve nearby parallel streets the body never enters.
    static bool EntersArea(Vehicle body,Point at,double heading,RoadNode road,double clearance=CarriagewayHalfWidth)
    {
        var dx=Math.Abs(at.X-road.Position.X);var dz=Math.Abs(at.Z-road.Position.Z);var s=Math.Abs(Math.Sin(heading));var c=Math.Abs(Math.Cos(heading));
        return dx<clearance+BodyHalfLength(body)*s+BodyHalfWidth(body)*c&&dz<clearance+BodyHalfLength(body)*c+BodyHalfWidth(body)*s;
    }
    // How deep two bodies overlap (negative: the gap between them).
    static double Penetration(Vehicle a,Point aAt,double aHeading,Vehicle b,Point bAt,double bHeading)
    {
        static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
        var af=new Point(Math.Sin(aHeading),Math.Cos(aHeading));var ar=new Point(af.Z,-af.X);var bf=new Point(Math.Sin(bHeading),Math.Cos(bHeading));var br=new Point(bf.Z,-bf.X);var d=bAt-aAt;
        return new[]{af,ar,bf,br}.Min(x=>BodyHalfLength(a)*Math.Abs(Dot(af,x))+BodyHalfWidth(a)*Math.Abs(Dot(ar,x))+BodyHalfLength(b)*Math.Abs(Dot(bf,x))+BodyHalfWidth(b)*Math.Abs(Dot(br,x))-Math.Abs(Dot(d,x)));
    }

    // A truck about to drive from its bay or dock to its first lane point, or doing so.
    bool LeavesDock(Vehicle v)=>v.Status==VehicleStatus.Travelling&&v.RouteIndex==0&&v.Route.Count>1&&!v.PassengerCar&&!IsTow(v)&&v.Detour.Count==0&&!v.SitePathBeforeRoad&&InMap(v.Position)&&!AtGateway(v);
    // Its body already stands on the road at its first node: the way out has begun.
    bool OnCarriageway(Vehicle v)
    {
        if(v.Route.Count<2||!_roads.TryGetValue(v.Route[0],out var first)||!_roads.TryGetValue(v.Route[1],out var second))return false;
        var axis=second.Position-first.Position;var length=axis.Distance(default);if(length<=.01)return false;
        var lane=first.Position+new Point(axis.Z/length,-axis.X/length)*1.65;
        return v.Position.Distance(lane)<.1;
    }
    // Still at the works-site dock it is about to drive back from.
    static bool AtSiteDock(Vehicle v)=>v.SitePathBeforeRoad&&v.SitePathIndex<=1&&v.SitePath.Count>0&&v.Position.Distance(v.SitePath[0])<.01;
    bool Leaving(Vehicle v)=>v.SitePathBeforeRoad&&v.Status==VehicleStatus.Travelling&&v.SitePath.Count>v.SitePathIndex||LeavesDock(v);
    bool DockAdmitted(Vehicle v,Point dock)=>v.ManoeuvreDock is {} owned&&owned.Distance(dock)<.1&&
        (v.ManoeuvreDeparture??(Leaving(v)&&v.Position.Distance(owned)>.01))==Leaving(v);
    // How far ahead of v (along its heading) a truck stopped at its dock stands,
    // when its body lies across v's lane; null otherwise.
    double? BodyAcrossLane(Vehicle v,Vehicle o)
    {
        if(o.PassengerCar||o.Machine!=PavingMachine.None||o.Position.Distance(v.Position)>20)return null;
        var docked=o.Status is VehicleStatus.Unloading or VehicleStatus.Parked&&o.DockPosition is {} at&&at.Distance(o.Position)<.1||LeavesDock(o)&&!OnCarriageway(o)||OnSitePath(o);
        if(!docked)return null;
        var relative=o.Position-v.Position;var forward=relative.X*Math.Sin(v.Heading)+relative.Z*Math.Cos(v.Heading);if(forward<=0)return null;
        var side=Math.Abs(relative.X*Math.Cos(v.Heading)-relative.Z*Math.Sin(v.Heading));var turn=o.Heading-v.Heading;
        return side<BodyHalfLength(o)*Math.Abs(Math.Sin(turn))+BodyHalfWidth(o)*Math.Abs(Math.Cos(turn))+BodyHalfWidth(v)?forward:null;
    }
    // Holds the node: its lane reservation, or a space of its admitted manoeuvre.
    static bool Holds(Vehicle o,int node)=>o.IntersectionClaim==node||o.ManoeuvreSpaces.Contains(node);
    // The machine that receives this truck's load: feeding it never waits for it to
    // leave (their bodies are still checked by the sweep).
    bool Receives(Vehicle machine,Vehicle truck)=>machine.Machine!=PavingMachine.None&&truck.ShipmentId is {} id&&State.Shipments.Any(s=>s.Id==id&&s.ReceivingMachineId==machine.Id);

    // The poses of `o` driving from `at` as the lane model and the site path move
    // it: first the site points (each segment's heading, the last approach one the
    // paver's), then the lane points of `route` from `from` (turn in place, then
    // straight). They run to the first lane point, after at least `minimum`,
    // where the whole body is clear of every junction and bend area; `spaces`
    // are the ones the body enters on the way, in order.
    List<(Vehicle Body,Point Position,double Heading)> Manoeuvre(Vehicle o,List<int> route,int from,Point at,double facing,IReadOnlyList<Point> site,bool approach,int minimum,out List<RoadNode> spaces)
    {
        var held=new List<(Vehicle Body,Point Position,double Heading)>();spaces=[];
        var near=SharedNodes().Where(n=>n.Position.Distance(at)<120).ToList();var found=spaces;
        void Note(Point p,double h){foreach(var n in near)if(!found.Contains(n)&&EntersArea(o,p,h,n))found.Add(n);}
        for(var index=0;index<site.Count;index++){
            var to=site[index];var heading=approach&&o.SiteDockHeading is {} dockHeading&&index==site.Count-1?dockHeading:to.Distance(at)>.001?Math.Atan2(to.X-at.X,to.Z-at.Z):facing;
            var run=new List<(Point Position,double Heading)>();SamplePoses(run,at,to,heading);foreach(var p in run){held.Add((o,p.Position,p.Heading));Note(p.Position,p.Heading);}at=to;facing=heading;
        }
        var points=0;
        for(var i=from;i<route.Count&&i<from+12;i++){
            if(!_roads.TryGetValue(route[i],out var road))break;
            var edge=i>0&&_roads.TryGetValue(route[i-1],out var previous)?road.Position-previous.Position:route.Count>1&&_roads.TryGetValue(route[1],out var second)?second.Position-road.Position:default;
            var metres=edge.Distance(default);if(metres<.01)continue;
            var lane=road.Position+new Point(edge.Z/metres,-edge.X/metres)*1.65+(i>0?InnerCornerOffset(route,i,road,edge,metres):default);
            var direction=lane.Distance(at)>.001?Math.Atan2(lane.X-at.X,lane.Z-at.Z):facing;var turn=Math.IEEERemainder(direction-facing,2*Math.PI);
            // The lane model turns in place at once: the turn counts for the spaces it
            // enters, the body only takes the new heading.
            for(var part=0;part<=8;part++)Note(at,facing+turn*part/8);held.Add((o,at,direction));
            var straight=new List<(Point Position,double Heading)>();SamplePoses(straight,at,lane,direction);foreach(var p in straight){held.Add((o,p.Position,p.Heading));Note(p.Position,p.Heading);}
            at=lane;facing=direction;points++;
            if(points>=minimum&&near.All(n=>!EntersArea(o,at,facing,n)))break;
        }
        return held;
    }
    // A truck's way out of the dock at `at` toward home (predicted while it is
    // still there, or along the route it now drives).
    List<(Vehicle Body,Point Position,double Heading)> PredictedWayOut(Vehicle o,Point at,double heading,IReadOnlyList<Point> site,out List<RoadNode> spaces)
    {
        spaces=[];if(o.Route.Count==0||!_roads.TryGetValue(o.Route[^1],out var access))return [];
        var home=o.External?EntryNode():BuildingById(o.HomeBuildingId) is {} source?Access(source):null;if(home==null)return [];
        var route=FindRoadRoute(access.Id,home.Id,ConstructionTrip(o));if(route.Count<2)return [];
        return Manoeuvre(o,route,0,at,heading,site,false,2,out spaces);
    }
    // o's way out from where it stands: driving it now (site path back, or the
    // lane exit from RouteIndex 0), or, still at its dock, the predicted one.
    List<(Vehicle Body,Point Position,double Heading)> WayOut(Vehicle o,out List<RoadNode> spaces)
    {
        spaces=[];
        if(o.Status==VehicleStatus.Travelling&&o.SitePathBeforeRoad)return Manoeuvre(o,o.Route,0,o.Position,o.Heading,o.SitePath.Skip(o.SitePathIndex).ToList(),false,2,out spaces);
        if(LeavesDock(o))return Manoeuvre(o,o.Route,0,o.Position,o.Heading,[],false,2,out spaces);
        if(o.Status is VehicleStatus.Unloading or VehicleStatus.Parked&&!o.PassengerCar&&o.Machine==PavingMachine.None){
            var site=o.SitePath.Count>0&&o.SitePathIndex>=o.SitePath.Count?Enumerable.Reverse(o.SitePath).Skip(1).ToList():new List<Point>();
            return PredictedWayOut(o,o.Position,o.Heading,site,out spaces);
        }
        return [];
    }

    // A present body on v's way: any vehicle but v and the one it tows. A body
    // already in contact with v stays an obstacle: v may only move so that the
    // contact does not deepen (it separates), never through it.
    Vehicle? BodyOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses)
    {
        if(poses.Count==0)return null;var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.Id==v.TowTargetId||o.TowTruckId==v.Id||o.Status is VehicleStatus.Outside or VehicleStatus.Towed||o.Position.Distance(v.Position)>reach||AtGateway(o)&&AtGateway(v))continue;
            var now=Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);var limit=Math.Max(-.1,now+.001);
            foreach(var p in poses)if(p.Position.Distance(o.Position)<12&&Penetration(v,p.Position,p.Heading,o,o.Position,o.Heading)>limit)return o;
        }
        return null;
    }
    // Traffic already circulating that would reach the way within the manoeuvre
    // (SPEC "Saída do lote": the road keeps its priority): its straight run over
    // the time the truck needs to drive the way.
    Vehicle? TrafficOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses)
    {
        if(poses.Count==0)return null;var length=0.0;for(var i=1;i<poses.Count;i++)length+=poses[i-1].Position.Distance(poses[i].Position);
        var seconds=length/State.Tuning.VehicleSpeed+1;
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.Id==v.TowTargetId||o.Status!=VehicleStatus.Travelling||o.WaitingSince>0||Leaving(o)||o.Machine!=PavingMachine.None||o.Position.Distance(v.Position)>seconds*State.Tuning.HighwaySpeed+20)continue;
            var forward=new Point(Math.Sin(o.Heading),Math.Cos(o.Heading));var reach=State.Tuning.VehicleSpeed*seconds;
            for(var run=0.0;run<=reach;run+=1){var at=o.Position+forward*run;if(poses.Any(p=>p.Position.Distance(at)<12&&Penetration(v,p.Position,p.Heading,o,at,o.Heading)>-.1))return o;}
        }
        return null;
    }
    // The way o drives through a bend to its first safe waiting place, with the
    // queue gap beyond it; and whether that way is free of bodies now (a vehicle
    // a parked body keeps from turning does not hold the turn for others).
    List<(Vehicle Body,Point Position,double Heading)> TurnWay(Vehicle o,out List<RoadNode> spaces)
    {
        var way=Manoeuvre(o,o.Route,o.RouteIndex,o.Position,o.Heading,[],false,1,out spaces);
        if(way.Count>0){var end=way[^1];var run=new List<(Point Position,double Heading)>();SamplePoses(run,end.Position,end.Position+new Point(Math.Sin(end.Heading),Math.Cos(end.Heading))*8,end.Heading);way.AddRange(run.Select(p=>(o,p.Position,p.Heading)));}
        return way;
    }
    // A stopped lane vehicle whose way on is blocked by that body.
    bool WaitsOnBody(Vehicle v,Vehicle body)=>v.Status==VehicleStatus.Travelling&&v.WaitingSince>0&&v.RouteIndex<v.Route.Count&&BodyOnWay(v,TurnWay(v,out _))?.Id==body.Id;
    // A delivery whose bay entry is not admitted turns only into that entry: a
    // dock owner or a body on its way in keeps it from turning, like a body on
    // the turn (intention holds no one).
    bool CanTurnNow(Vehicle o)=>o.RouteIndex<o.Route.Count&&BodyOnWay(o,TurnWay(o,out _)) is null&&
        !(EntersBay(o)&&o.DockPosition is {} dock&&!DockAdmitted(o,dock)&&(DockOwners(o,dock).Any()||BodyOnWay(o,WayIn(o,dock,out _)) is {}));
    // Whether v, leaving or entering a bay, may take the shared node now: no one
    // else holds it, no traffic presents at it, no walker is on its crossing and
    // a signal shows green for the way v enters.
    bool SpaceFree(Vehicle v,RoadNode node,Point entering,bool yieldToTraffic=true)
    {
        var area=_junctionAreas.GetValueOrDefault(node.Id);
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.Id==v.TowTargetId||o.TowTruckId==v.Id||Receives(o,v))continue;
            if(Holds(o,node.Id)||area!=null&&(o.IntersectionClaim is {} held&&area.Contains(held)&&o.Position.Distance(node.Position)<9||o.ManoeuvreSpaces.Any(area.Contains))){v.Cause=(IsBend(node)?"Aguardando liberação da curva pelo veículo ":"Aguardando liberação do cruzamento pelo veículo ")+o.Id;return false;}
            // A lot exit yields to traffic presenting at the node; a vehicle entering
            // its bay through it is traffic itself and waits only for who arrived first.
            if(o.Status==VehicleStatus.Travelling&&!Leaving(o)&&o.RouteIndex<o.Route.Count&&o.Route[o.RouteIndex]==node.Id&&o.Position.Distance(node.Position)<14&&CanTurnNow(o)&&(yieldToTraffic||o.WaitingSince>0&&(v.WaitingSince<=0||o.WaitingSince<v.WaitingSince||o.WaitingSince==v.WaitingSince&&o.Id<v.Id))){v.Cause=yieldToTraffic?"Aguardando espaço para acessar a faixa":"Fila para acesso de carga";return false;}
        }
        if(State.Citizens.Any(c=>c.Alive&&c.VehicleId==null&&c.Moving&&c.CrossingNodeId==node.Id&&c.Position.Distance(node.Position)<State.Tuning.RoadWidth/2+1.5)){v.Cause="Aguardando preferência/semáforo/travessia";return false;}
        if(node.Control==JunctionControl.Signal&&node.Neighbours.Count>=3&&((VehicleMovementTime%60<30)!=(Math.Abs(entering.X)>Math.Abs(entering.Z)))){v.Cause="Aguardando preferência/semáforo/travessia";return false;}
        return true;
    }
    // ARCHITECTURE: the manoeuvre is admitted whole — every space up to the safe
    // waiting place free, no traffic arriving on it and no body on it — and its
    // dock corridor and spaces are then held as explicit state. Already admitted,
    // it keeps them and only its present way is checked against bodies.
    bool AdmitManoeuvre(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> way,List<RoadNode> spaces,Point dock,bool yieldToTraffic)
    {
        var admitted=DockAdmitted(v,dock);
        if(!admitted){
            foreach(var space in spaces){
                if(Holds(v,space.Id))continue;
                var index=way.FindIndex(p=>EntersArea(v,p.Position,p.Heading,space));var from=index>0?way[index-1].Position:v.Position;
                if(!SpaceFree(v,space,space.Position-from,yieldToTraffic))return false;
            }
            if(yieldToTraffic&&TrafficOnWay(v,way) is {}){v.Cause="Aguardando espaço para acessar a faixa";return false;}
        }
        if(BodyOnWay(v,way) is {} body){if(!admitted)v.Cause=yieldToTraffic?"Aguardando espaço para acessar a faixa":"Fila para acesso de carga";return false;}
        if(!admitted){v.ManoeuvreDock=dock;v.ManoeuvreDeparture=Leaving(v);v.ManoeuvreSpaces=v.ManoeuvreSpaces.Concat(spaces.Select(s=>s.Id)).Where(id=>v.IntersectionClaim!=id).Distinct().ToList();}
        return true;
    }

    // Trucks that own a dock corridor overlapping v's dock: a truck admitted into,
    // at or out of a dock whose body pose overlaps v's at its dock.
    IEnumerable<Vehicle> DockOwners(Vehicle v,Point dock)
    {
        List<(Vehicle Body,Point Position,double Heading)>? entering=null;
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.PassengerCar||o.Machine!=PavingMachine.None||o.Status is VehicleStatus.Outside or VehicleStatus.Towed)continue;
            var at=o.ManoeuvreDock??(o.Status is VehicleStatus.Unloading or VehicleStatus.Parked?o.DockPosition:null);if(at is not {} other||other.Distance(dock)>16)continue;
            if(other.Distance(dock)<.1||Penetration(v,dock,v.Heading,o,other,o.Heading)>-.1||Penetration(v,dock,v.Heading,o,o.Position,o.Heading)>-.1){yield return o;continue;}
            // Distinct dock endpoints can share the same physical corridor. A
            // newcomer must wait outside that owner's exit too, instead of
            // stopping on it while waiting for a different occupied bay.
            entering??=WayIn(v,dock,out _);
            if(DockWayOut(o).Any(exit=>entering.Any(p=>p.Position.Distance(exit.Position)<12&&BodiesOverlap(v,p.Position,p.Heading,o,exit.Position,exit.Heading))))yield return o;
        }
    }
    List<(Vehicle Body,Point Position,double Heading)> DockWayOut(Vehicle o)=>o.Status==VehicleStatus.Travelling&&o.ManoeuvreDock!=null&&!Leaving(o)&&o.DockPosition is {} bay?PredictedWayOut(o,bay,o.Heading,[],out _):WayOut(o,out _);
    // The ways out of the trucks owning v's dock corridor, which v keeps out of
    // until its own entry is admitted.
    List<(Vehicle Body,Point Position,double Heading)> OwnersWayOut(Vehicle v,Point dock)
    {
        var held=new List<(Vehicle Body,Point Position,double Heading)>();
        foreach(var o in DockOwners(v,dock))held.AddRange(DockWayOut(o));
        return held;
    }
    // The slide from the lane into the dock: trucks keep their lane heading.
    static List<(Vehicle Body,Point Position,double Heading)> SlidePoses(Vehicle v,Point from,Point dock)
    {
        var run=new List<(Point Position,double Heading)>();SamplePoses(run,from,dock,v.Heading);return run.Select(p=>(v,p.Position,p.Heading)).ToList();
    }

    // A delivery entering its bay: the rest of its route and the slide, taken
    // whole once the corridor has no other owner. Until then it does not drive
    // into the corridor (its own way in and the predicted way out of a truck at
    // that dock) nor into a dock owner's way out.
    bool EntersBay(Vehicle v)=>v.ShipmentId!=null&&!v.PassengerCar&&v.Machine==PavingMachine.None&&!IsTow(v)&&v.RefuellingAt==null&&v.DockPosition!=null&&!v.SitePathBeforeRoad&&v.SitePathIndex==0&&v.Status==VehicleStatus.Travelling&&v.Route.Count-v.RouteIndex<=4&&InMap(v.Position);
    List<(Vehicle Body,Point Position,double Heading)> WayIn(Vehicle v,Point dock,out List<RoadNode> spaces)
    {
        var lane=Manoeuvre(v,v.Route,v.RouteIndex,v.Position,v.Heading,[],false,99,out spaces);
        var end=lane.Count>0?lane[^1].Position:v.Position;var heading=lane.Count>0?lane[^1].Heading:v.Heading;
        // A works-site approach drives its site path to the paver's dock; a bay, the slide.
        if(v.SitePath.Count>v.SitePathIndex){
            var site=Manoeuvre(v,[],0,end,heading,v.SitePath.Skip(v.SitePathIndex).ToList(),true,0,out var siteSpaces);lane.AddRange(site);
            foreach(var space in siteSpaces)if(!spaces.Contains(space))spaces.Add(space);
            return lane;
        }
        var run=new List<(Point Position,double Heading)>();SamplePoses(run,end,dock,heading);lane.AddRange(run.Select(p=>(v,p.Position,p.Heading)));
        return lane;
    }
    // Returns the poses v keeps out of while its entry is not admitted (empty
    // once admitted, or when it is admitted now).
    List<(Vehicle Body,Point Position,double Heading)> BayEntryHold(Vehicle v)
    {
        if(v.DockPosition is not {} dock||DockAdmitted(v,dock))return [];
        var crewHeading=v.Route.Count>0&&_roads.TryGetValue(v.Route[^1],out var crewAccess)&&
            (crewAccess.Highway||crewAccess.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-crewAccess.Position.X)>.1))?Math.PI/2:0;
        if(v.Machine==PavingMachine.None&&v.CrewAboard&&!CrewDockClear(dock,crewHeading)){
            // Road completion can invalidate a previously chosen curb position.
            // Replan only the unadmitted intention, without moving the body.
            var shipment=State.Shipments.FirstOrDefault(s=>s.Id==v.ShipmentId);
            if(v.Route.Count>0&&_roads.TryGetValue(v.Route[^1],out var access)){
                var revised=DockAt(access,BuildingById(shipment?.DestinationBuildingId),true);
                if(CrewDockClear(revised,crewHeading)){v.DockPosition=dock=revised;}
                else {v.Cause=WorkCause.PavingSiteOccupied;return WayIn(v,dock,out _);}
            }
        }
        var owners=DockOwners(v,dock).ToList();
        var way=WayIn(v,dock,out var spaces);
        if(owners.Count==0&&AdmitManoeuvre(v,way,spaces,dock,false))return [];
        if(owners.Count>0)v.Cause="Fila para acesso de carga";
        var hold=OwnersWayOut(v,dock);
        hold.AddRange(PredictedWayOut(v,dock,v.Heading,[],out _));
        // Its own way in beyond the corridor entrance: the slide and the last lane point.
        hold.AddRange(way.Where(p=>p.Position.Distance(dock)<State.Tuning.RoadWidth+4));
        return hold;
    }

    // The machine's manoeuvre ends at the first path point where its entire
    // body can wait outside shared pavement, using the same poses for admission
    // and publication. A fixed look-ahead can stop halfway through a crossing.
    List<(Vehicle Body,Point Position,double Heading)> MachinePathAhead(Vehicle m)
    {
        var poses=new List<(Vehicle Body,Point Position,double Heading)>();var at=m.Position;
        for(var index=m.SitePathIndex;index<m.SitePath.Count;index++){
            var target=m.SitePath[index];var length=at.Distance(target);if(length<=.001)continue;var heading=Math.Atan2(target.X-at.X,target.Z-at.Z);
            var run=new List<(Point Position,double Heading)>();SamplePoses(run,at,target,heading);poses.AddRange(run.Select(p=>(m,p.Position,p.Heading)));at=target;
            if(SharedNodes().All(node=>!EntersArea(m,at,heading,node)))break;
        }
        // The end of the site path is not necessarily a safe waiting place:
        // leaving equipment continues from there onto the real return route.
        // Acquire that continuation together, before entering its first space.
        if(m.MachinePhase==MachinePhase.LeavingSite&&poses.Count>0&&SharedNodes().Any(node=>EntersArea(m,at,poses[^1].Heading,node))&&NearestRoad(at) is {} access){
            var route=FindRoadRoute(access.Id,EntryNode().Id,true);
            if(route.Count>0)poses.AddRange(Manoeuvre(m,route,0,at,poses[^1].Heading,[],false,1,out _));
        }
        return poses;
    }
    // Acquisition is atomic for a machine too: a body on the path or a held
    // node prevents acquiring any new space, without releasing occupied ones.
    bool MachineMayEnter(Vehicle m)
    {
        var ahead=MachinePathAhead(m);
        var needed=SharedNodes().Where(node=>!m.ManoeuvreSpaces.Contains(node.Id)&&ahead.Any(p=>EntersArea(m,p.Position,p.Heading,node))).ToList();
        if(BodyOnWay(m,ahead)!=null)return false;
        foreach(var node in needed){
            var area=_junctionAreas.GetValueOrDefault(node.Id);
            if(State.Vehicles.Any(o=>o.Id!=m.Id&&(Holds(o,node.Id)||area!=null&&(o.IntersectionClaim is {} held&&area.Contains(held)&&o.Position.Distance(node.Position)<9||o.ManoeuvreSpaces.Any(area.Contains)))))
                return false;
        }
        m.ManoeuvreSpaces.AddRange(needed.Select(node=>node.Id));
        return true;
    }
    // Admitted manoeuvres lane traffic keeps out of: a way out of a bay or a way
    // into it, while held. The hold stops at the first body already on it.
    List<(Vehicle Machine,Point Position,double Heading)> AdmittedManoeuvre(Vehicle o)
    {
        var held=new List<(Vehicle,Point,double)>();if(o.ManoeuvreDock is not {} dock||!DockAdmitted(o,dock)||o.Status!=VehicleStatus.Travelling)return held;
        var way=Leaving(o)?WayOut(o,out _):o.DockPosition is {} bay&&bay.Distance(dock)<.1?WayIn(o,bay,out _):[];
        foreach(var pose in way){
            if(State.Vehicles.Any(other=>other.Id!=o.Id&&other.Status!=VehicleStatus.Outside&&other.Position.Distance(pose.Position)<12&&BodiesOverlap(o,pose.Position,pose.Heading,other,other.Position,other.Heading)))break;
            held.Add(pose);
        }
        return held;
    }
    // A works-site truck's path holds lane traffic once admitted, or once begun
    // (a save from before the explicit admission).
    bool SitePathHolds(Vehicle t)=>t.ManoeuvreDock is {} dock&&DockAdmitted(t,dock)||!AtSiteDock(t)&&t.SitePathIndex>0;

    // Physical release: a space once the body is clear of it and the rest of the
    // manoeuvre no longer enters it; the corridor once the body is clear of the
    // dock and of the way between the dock and its lane.
    void ReleaseManoeuvre(Vehicle v)
    {
        if(v.ManoeuvreDock==null&&v.ManoeuvreSpaces.Count==0)return;
        if(v.Status is VehicleStatus.Outside or VehicleStatus.Towed||v.PassengerCar){v.ManoeuvreDock=null;v.ManoeuvreSpaces.Clear();return;}
        if(v.Status==VehicleStatus.Stranded||v.Machine!=PavingMachine.None&&v.Status!=VehicleStatus.Parked&&v.Status!=VehicleStatus.Travelling)return;
        List<(Vehicle Body,Point Position,double Heading)> rest=v.Machine!=PavingMachine.None?MachinePathAhead(v)
            :v.Status!=VehicleStatus.Travelling?[]
            :Leaving(v)?WayOut(v,out _)
            :v.ManoeuvreDock is {} target&&v.DockPosition is {} bay&&bay.Distance(target)<.1?WayIn(v,bay,out _)
            :v.RouteIndex<v.Route.Count?Manoeuvre(v,v.Route,v.RouteIndex,v.Position,v.Heading,[],false,1,out _):[];
        // The body itself is measured on the carriageway square: the planning area
        // already counts a truck length around the node, which a parked body beside
        // it (a paver at the curb) would never leave.
        v.ManoeuvreSpaces.RemoveAll(id=>!_roads.TryGetValue(id,out var node)||!EntersArea(v,v.Position,v.Heading,node,CarriagewayHalfWidth)&&!rest.Any(p=>EntersArea(v,p.Position,p.Heading,node)));
        if(v.ManoeuvreDock is {} dock){
            var atDock=v.Status is VehicleStatus.Unloading or VehicleStatus.Parked&&v.DockPosition is {} parked&&parked.Distance(dock)<.1;
            var entering=v.Status==VehicleStatus.Travelling&&v.DockPosition is {} goal&&goal.Distance(dock)<.1;
            var inCorridor=Penetration(v,v.Position,v.Heading,v,dock,v.Heading)>-.1||v.Route.Count>0&&_roads.TryGetValue(v.Route[0],out var first)&&Leaving(v)&&v.Position.Distance(first.Position)>State.Tuning.RoadWidth/2;
            if(!atDock&&!entering&&!inCorridor)v.ManoeuvreDock=null;
        }
    }
}
