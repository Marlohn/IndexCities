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
    // #270 opt-in: reuse only an ephemeral sampling list inside one Manoeuvre call.
    // Every sampled pose is copied into `held` before this buffer is cleared.
    static readonly bool Pr57270ManoeuvreScratchEnabled =
        Environment.GetEnvironmentVariable("PR57_270_MANOEUVRE_SCRATCH") == "1";
    // PR57 #540: opt-in reentrant lease for *temporary* pose samples. No
    // geometry or physical decision survives a call: only empty list capacity.
    // Different nested Manoeuvre calls own distinct depth slots.
    static readonly bool Pr57ReentrantManoeuvreScratchEnabled =
        Environment.GetEnvironmentVariable("PR57_REENTRANT_MANOEUVRE_SCRATCH") == "1";
    readonly List<List<(Point Position,double Heading)>> _pr57ManoeuvreScratchSlots = [];
    int _pr57ManoeuvreScratchDepth;
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
    // PR57-only reversible allocation POC. The same four separating axes and
    // arithmetic are evaluated in the same order; only the temporary heap array
    // and LINQ selector are removed. The flag is fixed before class initialization.
    static readonly bool Pr57PenetrationStackAxes=Environment.GetEnvironmentVariable("PR57_PENETRATION_STACK_AXES")=="1";
    static double Penetration(Vehicle a,Point aAt,double aHeading,Vehicle b,Point bAt,double bHeading)
    {
        if(Pr57271Enabled)Pr57271PenetrationObserve(a,aAt,aHeading,b,bAt,bHeading);
        if(!Pr57272MemoEnabled||!_pr57272MemoActive)
            return PenetrationCore(a,aAt,aHeading,b,bAt,bHeading);
        var key=Pr57272Key(a,aAt,aHeading,b,bAt,bHeading);
        if(_pr57272Memo!.TryGetValue(key,out var cached))
        {
            _pr57272SliceHits++;
            return cached;
        }
        var result=PenetrationCore(a,aAt,aHeading,b,bAt,bHeading);
        _pr57272Memo.Add(key,result);
        _pr57272SliceMisses++;
        return result;
    }
    static double PenetrationCore(Vehicle a,Point aAt,double aHeading,Vehicle b,Point bAt,double bHeading)
    {
        static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
        var af=new Point(Math.Sin(aHeading),Math.Cos(aHeading));var ar=new Point(af.Z,-af.X);var bf=new Point(Math.Sin(bHeading),Math.Cos(bHeading));var br=new Point(bf.Z,-bf.X);var d=bAt-aAt;
        if(!Pr57PenetrationStackAxes)
            return new[]{af,ar,bf,br}.Min(x=>BodyHalfLength(a)*Math.Abs(Dot(af,x))+BodyHalfWidth(a)*Math.Abs(Dot(ar,x))+BodyHalfLength(b)*Math.Abs(Dot(bf,x))+BodyHalfWidth(b)*Math.Abs(Dot(br,x))-Math.Abs(Dot(d,x)));
        ReadOnlySpan<Point> axes=stackalloc Point[]{af,ar,bf,br};
        var minimum=double.PositiveInfinity;
        foreach(var x in axes){
            var depth=BodyHalfLength(a)*Math.Abs(Dot(af,x))+BodyHalfWidth(a)*Math.Abs(Dot(ar,x))+BodyHalfLength(b)*Math.Abs(Dot(bf,x))+BodyHalfWidth(b)*Math.Abs(Dot(br,x))-Math.Abs(Dot(d,x));
            // Enumerable.Min(double) propagates NaN; do not silently change it.
            if(double.IsNaN(depth))return depth;
            if(depth<minimum)minimum=depth;
        }
        return minimum;
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
    bool AdmissionPathMatches(Vehicle v)=>v.MovementAdmission is {} a&&a.Route.SequenceEqual(v.Route)&&a.SitePath.SequenceEqual(v.SitePath);
    bool Leaving(Vehicle v)=>v.SitePathBeforeRoad&&v.Status==VehicleStatus.Travelling&&v.SitePath.Count>v.SitePathIndex||LeavesDock(v)||
        v.Status==VehicleStatus.Travelling&&v.ManoeuvreDeparture==true&&AdmissionPathMatches(v)&&v.RouteIndex<=v.MovementAdmission!.EndRouteIndex;
    bool DockAdmitted(Vehicle v,Point dock)=>v.ManoeuvreDock is {} owned&&owned.Distance(dock)<.1&&
        v.ManoeuvreDeparture==Leaving(v)&&AdmissionPathMatches(v)&&
        (v.MovementAdmission!.ReachesDock||v.RouteIndex<=v.MovementAdmission.EndRouteIndex);
    // How far ahead of v (along its heading) a truck stopped at its dock stands,
    // when its body lies across v's lane; null otherwise.
    double? BodyAcrossLane(Vehicle v,Vehicle o)
    {
        // Trucks still stacked at the exterior staging origin have not entered yet.
        if(o.PassengerCar||o.Machine!=PavingMachine.None||o.Position.Distance(v.Position)>20||AtGateway(o)&&AtGateway(v))return null;
        Pr57DockWriterRead(v,o); // #233: read-only dock dependency oracle after the canonical early guard
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
        =>Manoeuvre(o,route,from,at,facing,site,approach,minimum,out spaces,out _);

    List<(Vehicle Body,Point Position,double Heading)> Manoeuvre(Vehicle o,List<int> route,int from,Point at,double facing,IReadOnlyList<Point> site,bool approach,int minimum,out List<RoadNode> spaces,out int endIndex,Func<Point,double,bool>? extraClear=null,int? through=null)
    {
        var pr57Man248 = Pr57Man248Before(o, route, from, at, facing, site, approach, minimum, extraClear, through);
        // A lease is scoped to this invocation. Nested calls use the next slot,
        // so their SamplePoses cannot overwrite an outer invocation's samples.
        var pr57Lease = Pr57ReentrantManoeuvreScratchEnabled ? _pr57ManoeuvreScratchDepth++ : -1;
        try
        {
        var held=new List<(Vehicle Body,Point Position,double Heading)>();spaces=[];endIndex=from-1;
        // Scratch never escapes this invocation; no state or decision is cached.
        List<(Point Position,double Heading)>? pr57270Scratch=null;
        if(pr57Lease>=0)
        {
            while(_pr57ManoeuvreScratchSlots.Count<=pr57Lease)
                _pr57ManoeuvreScratchSlots.Add(new List<(Point Position,double Heading)>());
            pr57270Scratch=_pr57ManoeuvreScratchSlots[pr57Lease];
        }
        var near=SharedNodes();var found=spaces;
        // One scratch buffer per manoeuvre, not per sampled pose. The broad
        // phase only selects possible nodes; the exact predicate is unchanged.
        var pr57NodeCandidates=Pr57SharedNodeGridEnabled?new List<int>():null;
        void Note(Point p,double h)
        {
            if(pr57NodeCandidates is not null&&Pr57GatherSharedNodeCandidates(o,p,near,pr57NodeCandidates))
            {
                // A manoeuvre records spaces in the original SharedNodes order.
                pr57NodeCandidates.Sort();
                foreach(var index in pr57NodeCandidates)
                {
                    var node=near[index];
                    if(!found.Contains(node)&&EntersArea(o,p,h,node))found.Add(node);
                }
                return;
            }
            foreach(var n in near)if(!found.Contains(n)&&EntersArea(o,p,h,n))found.Add(n);
        }
        bool TouchesSharedNode(Point p,double h)
        {
            if(pr57NodeCandidates is not null&&Pr57GatherSharedNodeCandidates(o,p,near,pr57NodeCandidates))
            {
                foreach(var index in pr57NodeCandidates)
                    if(EntersArea(o,p,h,near[index]))return true;
                return false;
            }
            return near.Any(n=>EntersArea(o,p,h,n));
        }
        for(var index=0;index<site.Count;index++){
            var to=site[index];var heading=approach&&o.SiteDockHeading is {} dockHeading&&index==site.Count-1?dockHeading:to.Distance(at)>.001?Math.Atan2(to.X-at.X,to.Z-at.Z):facing;
            var run=(Pr57ReentrantManoeuvreScratchEnabled||Pr57270ManoeuvreScratchEnabled)?(pr57270Scratch??=new List<(Point Position,double Heading)>()):new List<(Point Position,double Heading)>();
            run.Clear();SamplePoses(run,at,to,heading);foreach(var p in run){held.Add((o,p.Position,p.Heading));Note(p.Position,p.Heading);}at=to;facing=heading;
        }
        var points=0;
        for(var i=from;i<route.Count&&i<=(through??int.MaxValue);i++){
            if(!_roads.TryGetValue(route[i],out var road))break;
            var edge=i>0&&_roads.TryGetValue(route[i-1],out var previous)?road.Position-previous.Position:route.Count>1&&_roads.TryGetValue(route[1],out var second)?second.Position-road.Position:default;
            var metres=edge.Distance(default);if(metres<.01)continue;
            var lane=road.Position+new Point(edge.Z/metres,-edge.X/metres)*1.65+(i>0?InnerCornerOffset(route,i,road,edge,metres):default);
            var direction=lane.Distance(at)>.001?Math.Atan2(lane.X-at.X,lane.Z-at.Z):facing;var turn=Math.IEEERemainder(direction-facing,2*Math.PI);
            // The lane model turns in place at once: the turn counts for the spaces it
            // enters, the body only takes the new heading.
            for(var part=0;part<=8;part++)Note(at,facing+turn*part/8);held.Add((o,at,direction));
            var straight=(Pr57ReentrantManoeuvreScratchEnabled||Pr57270ManoeuvreScratchEnabled)?(pr57270Scratch??=new List<(Point Position,double Heading)>()):new List<(Point Position,double Heading)>();
            straight.Clear();SamplePoses(straight,at,lane,direction);foreach(var p in straight){held.Add((o,p.Position,p.Heading));Note(p.Position,p.Heading);}
            at=lane;facing=direction;points++;endIndex=i;
            if(points>=minimum&&(pr57NodeCandidates is null?near.All(n=>!EntersArea(o,at,facing,n)):!TouchesSharedNode(at,facing))&&(extraClear?.Invoke(at,facing)??true))break;
        }
        Pr57Man248After(pr57Man248, held.Count);
        return held;
        }
        finally
        {
            if(pr57Lease>=0)
            {
                _pr57ManoeuvreScratchSlots[pr57Lease].Clear();
                _pr57ManoeuvreScratchDepth--;
            }
        }
    }
    // A truck's way out of the dock at `at` toward home (predicted while it is
    // still there, or along the route it now drives).
    List<(Vehicle Body,Point Position,double Heading)> PredictedWayOut(Vehicle o,Point at,double heading,IReadOnlyList<Point> site,out List<RoadNode> spaces)
    {
        spaces=[];if(o.Route.Count==0||!_roads.TryGetValue(o.Route[^1],out var access))return [];
        var home=o.External?EntryNode():BuildingById(o.HomeBuildingId) is {} source?Access(source):null;if(home==null)return [];
        var route=FindRoadRoute(access.Id,home.Id,ConstructionTrip(o));if(route.Count<2)return [];
        return Manoeuvre(o,route,0,at,heading,site,false,2,out spaces,out _,ClearOfDockWay(o,route,at,heading,site));
    }
    // The first lane point is not a whole-body waiting pose if the rear still
    // occupies the dock sweep. Acquire the next crossing together in that case.
    Func<Point,double,bool> ClearOfDockWay(Vehicle o,List<int> route,Point at,double heading,IReadOnlyList<Point> site)
    {
        var corridor=Manoeuvre(o,route,0,at,heading,site,false,int.MaxValue,out _,out _,through:0);
        return (p,h)=>!corridor.Any(q=>BodiesOverlap(o,p,h,o,q.Position,q.Heading));
    }
    // o's way out from where it stands: driving it now (site path back, or the
    // lane exit from RouteIndex 0), or, still at its dock, the predicted one.
    List<(Vehicle Body,Point Position,double Heading)> WayOut(Vehicle o,out List<RoadNode> spaces)
        =>WayOut(o,out spaces,out _);
    List<(Vehicle Body,Point Position,double Heading)> WayOut(Vehicle o,out List<RoadNode> spaces,out int endIndex)
    {
        spaces=[];endIndex=-1;
        var limit=o.ManoeuvreDeparture==true&&AdmissionPathMatches(o)?o.MovementAdmission:null;
        if(o.Status==VehicleStatus.Travelling&&o.SitePathBeforeRoad){
            var site=o.SitePath.Skip(o.SitePathIndex).ToList();
            return Manoeuvre(o,o.Route,0,o.Position,o.Heading,site,false,limit==null?2:int.MaxValue,out spaces,out endIndex,limit==null?ClearOfDockWay(o,o.Route,o.Position,o.Heading,site):null,through:limit?.EndRouteIndex);
        }
        if(LeavesDock(o)||o.Status==VehicleStatus.Travelling&&limit!=null)return Manoeuvre(o,o.Route,o.RouteIndex,o.Position,o.Heading,[],false,limit==null?2:int.MaxValue,out spaces,out endIndex,limit==null?ClearOfDockWay(o,o.Route,o.Position,o.Heading,[]):null,through:limit?.EndRouteIndex);
        if(o.Status is VehicleStatus.Unloading or VehicleStatus.Parked&&!o.PassengerCar&&o.Machine==PavingMachine.None){
            var site=o.SitePath.Count>0&&o.SitePathIndex>=o.SitePath.Count?Enumerable.Reverse(o.SitePath).Skip(1).ToList():new List<Point>();
            return PredictedWayOut(o,o.Position,o.Heading,site,out spaces);
        }
        return [];
    }

    // A present body on v's way: any vehicle but v and the one it tows. A body
    // already in contact with v stays an obstacle: v may only move so that the
    // contact does not deepen (it separates), never through it.
    Vehicle? BodyOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses,bool pr57305Onward=false)
    {
        Pr57261Hit(0);
        // #546: alternate per-call contact representation; no state, decision or
        // geometry is reused across calls or across ordered actor writes.
        if(Pr57ContactFramesEnabled&&!Pr57271Enabled&&!Pr57272MemoEnabled
           &&!(pr57305Onward&&Pr57305Enabled))
        {
            var result=Pr57BodyOnWayFrames(v,poses);
            if(Pr57ContactFramesVerify)
            {
                var canonical=Pr57BodyOnWayCanonical(v,poses,pr57305Onward);
                if(!ReferenceEquals(result,canonical))
                    throw new InvalidOperationException("PR57_546_FIRST_BLOCKER_MISMATCH");
            }
            return result;
        }
        return Pr57BodyOnWayCanonical(v,poses,pr57305Onward);
    }
    Vehicle? Pr57BodyOnWayCanonical(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses,bool pr57305Onward)
    {
        // #305: counters observe the original ordered short-circuit; never drive physics.
        var census=pr57305Onward&&Pr57305Target(v);
        var visited=0;var eligible=0;var nearNegative=0;var geometry=0;
        var started=census?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        if(poses.Count==0)return null;var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;
        foreach(var o in State.Vehicles){
            if(census){visited++;Pr57306Read(v,o);}
            if(o.Id==v.Id||o.Id==v.TowTargetId||o.TowTruckId==v.Id||o.Status is VehicleStatus.Outside or VehicleStatus.Towed||o.Position.Distance(v.Position)>reach||AtGateway(o)&&AtGateway(v))continue;
            if(census)eligible++;
            var now=Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);var limit=Math.Max(-.1,now+.001);
            var tested=false;
            foreach(var p in poses)if(p.Position.Distance(o.Position)<12){
                if(census){geometry++;tested=true;}
                if(Penetration(v,p.Position,p.Heading,o,o.Position,o.Heading)>limit){
                    if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);
                    return o;
                }
            }
            if(census&&tested)nearNegative++;
        }
        if(census)Pr57305Body(v,null,poses.Count,visited,eligible,nearNegative,geometry,started);
        return null;
    }
    // Traffic already circulating that would reach the way within the manoeuvre
    // (SPEC "Saída do lote": the road keeps its priority): its straight run over
    // the time the truck needs to drive the way.
    Vehicle? TrafficOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses)
    {
        Pr57261Hit(1);
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
        Pr57261Hit(2);
        if(Pr57TurnWayWorksetTryGet(o,out var cached,out spaces))return cached;
        var way=Pr57TurnWayFresh(o,out spaces);
        Pr57TurnWayWorksetStore(o,way,spaces);
        return way;
    }
    // Original physical construction, retained verbatim for fallback and
    // optional shadow recomputation of every cache hit.
    List<(Vehicle Body,Point Position,double Heading)> Pr57TurnWayFresh(Vehicle o,out List<RoadNode> spaces)
    {
        var way=Manoeuvre(o,o.Route,o.RouteIndex,o.Position,o.Heading,[],false,1,out spaces);
        if(way.Count>0){var end=way[^1];var run=new List<(Point Position,double Heading)>();SamplePoses(run,end.Position,end.Position+new Point(Math.Sin(end.Heading),Math.Cos(end.Heading))*8,end.Heading);way.AddRange(run.Select(p=>(o,p.Position,p.Heading)));}
        return way;
    }
    void RememberLaneManoeuvre(Vehicle v,List<RoadNode> spaces)
    {
        Manoeuvre(v,v.Route,v.RouteIndex,v.Position,v.Heading,[],false,1,out _,out var endIndex);
        v.MovementAdmission=new(){Route=v.Route.ToList(),SitePath=v.SitePath.ToList(),EndRouteIndex=endIndex};
        v.ManoeuvreDeparture=false;
        v.ManoeuvreSpaces=spaces.Select(s=>s.Id).Distinct().ToList();
        Pr57ReservationWrite(v,"remember-lane-manoeuvre");
    }
    // A stopped lane vehicle whose way on is blocked by that body.
    bool WaitsOnBody(Vehicle v,Vehicle body)=>v.Status==VehicleStatus.Travelling&&v.WaitingSince>0&&v.RouteIndex<v.Route.Count&&BodyOnWay(v,TurnWay(v,out _))?.Id==body.Id;
    // Stopped by that body, directly or through the stopped vehicles queued ahead of it.
    bool HeldByBody(Vehicle v,Vehicle body,int depth=0)=>v.Status==VehicleStatus.Travelling&&v.WaitingSince>0&&v.RouteIndex<v.Route.Count&&
        BodyOnWay(v,TurnWay(v,out _)) is {} ahead&&(ahead.Id==body.Id||depth<4&&HeldByBody(ahead,body,depth+1));
    // A delivery whose bay entry is not admitted turns only into that entry: a
    // dock owner or a body on its way in keeps it from turning, like a body on
    // the turn (intention holds no one).
    bool CanTurnNow(Vehicle o)
    {
        var pr57263Start=Pr57263Begin();
        if(o.RouteIndex>=o.Route.Count)
            return Pr57263Finish(o,pr57263Start,0,null,false);
        var pr57263StageStart=pr57263Start==0?0:System.Diagnostics.Stopwatch.GetTimestamp();
        var turn=TurnWay(o,out _);
        Pr57263Stage(0,pr57263StageStart);
        pr57263StageStart=pr57263Start==0?0:System.Diagnostics.Stopwatch.GetTimestamp();
        var body=BodyOnWay(o,turn);
        Pr57263Stage(1,pr57263StageStart);
        if(body!=null)
            return Pr57263Finish(o,pr57263Start,1,body.Id,false);
        if(EntersBay(o)&&o.DockPosition is {} dock&&!DockAdmitted(o,dock)){
            pr57263StageStart=pr57263Start==0?0:System.Diagnostics.Stopwatch.GetTimestamp();
            var way=EntryPlan(o,dock,out _,out _,out var needed);
            Pr57263Stage(2,pr57263StageStart);
            if(needed){
                pr57263StageStart=pr57263Start==0?0:System.Diagnostics.Stopwatch.GetTimestamp();
                var dockBody=BodyOnWay(o,way);
                Pr57263Stage(3,pr57263StageStart);
                if(dockBody!=null)
                    return Pr57263Finish(o,pr57263Start,2,dockBody.Id,false);
            }
        }
        return Pr57263Finish(o,pr57263Start,3,null,true);
    }
    // Whether v, leaving or entering a bay, may take the shared node now: no one
    // else holds it, no traffic presents at it, no walker is on its crossing and
    // a signal shows green for the way v enters.
    bool SpaceFree(Vehicle v,RoadNode node,Point entering,bool yieldToTraffic=true)
    {
        Pr57261Hit(3);
        var area=_junctionAreas.GetValueOrDefault(node.Id);
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.Id==v.TowTargetId||o.TowTruckId==v.Id||Receives(o,v))continue;
            if(Holds(o,node.Id)||area!=null&&(o.IntersectionClaim is {} held&&area.Contains(held)&&o.Position.Distance(node.Position)<9||o.ManoeuvreSpaces.Any(area.Contains))){v.Cause=(IsBend(node)?"Aguardando liberação da curva pelo veículo ":"Aguardando liberação do cruzamento pelo veículo ")+o.Id;return false;}
            // A lot exit yields to traffic presenting at the node; a vehicle entering
            // its bay through it is traffic itself and waits only for who arrived first.
            if(o.Status==VehicleStatus.Travelling&&!Leaving(o)&&o.RouteIndex<o.Route.Count&&o.Route[o.RouteIndex]==node.Id&&o.Position.Distance(node.Position)<14&&CanTurnNow(o)&&(yieldToTraffic||IsBend(node)&&o.WaitingSince>0&&(v.WaitingSince<=0||o.WaitingSince<v.WaitingSince||o.WaitingSince==v.WaitingSince&&o.Id<v.Id))){v.Cause=yieldToTraffic?"Aguardando espaço para acessar a faixa":"Fila para acesso de carga";return false;}
        }
        if(State.Citizens.Any(c=>c.Alive&&c.VehicleId==null&&c.Moving&&c.CrossingNodeId==node.Id&&c.Position.Distance(node.Position)<State.Tuning.RoadWidth/2+1.5)){v.Cause="Aguardando preferência/semáforo/travessia";return false;}
        if(node.Control==JunctionControl.Signal&&node.Neighbours.Count>=3&&((VehicleMovementTime%60<30)!=(Math.Abs(entering.X)>Math.Abs(entering.Z)))){v.Cause="Aguardando preferência/semáforo/travessia";return false;}
        if(!yieldToTraffic&&node.Neighbours.Count>=3){
            var heading=v.Heading;v.Heading=Math.Atan2(entering.X,entering.Z);
            var allowed=JunctionPreferenceAllows(v,node);v.Heading=heading;
            if(!allowed){v.Cause="Aguardando preferência/semáforo/travessia";return false;}
        }
        return true;
    }
    // ARCHITECTURE: the manoeuvre is admitted whole — every space up to the safe
    // waiting place free, no traffic arriving on it and no body on it — and its
    // dock corridor and spaces are then held as explicit state. Already admitted,
    // it keeps them and only its present way is checked against bodies.
    bool AdmitManoeuvre(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> way,List<RoadNode> spaces,Point dock,bool yieldToTraffic,LocalMovementAdmission? limit=null)
    {
        Pr57261Hit(4);
        var admitted=DockAdmitted(v,dock);
        if(!admitted){
            // A straight street has no shared node, but admitted dock sweeps
            // still exclude each other. Check the whole new plan atomically.
            if(yieldToTraffic&&ConflictsWithDockWay(v,way)){
                Pr57276Gate(v,0);
                v.Cause=yieldToTraffic?"Aguardando espaço para acessar a faixa":"Fila para acesso de carga";return false;
            }
            foreach(var space in spaces){
                if(Holds(v,space.Id))continue;
                var index=way.FindIndex(p=>EntersArea(v,p.Position,p.Heading,space));var from=index>0?way[index-1].Position:v.Position;
                if(!SpaceFree(v,space,space.Position-from,yieldToTraffic)){Pr57276Gate(v,1);return false;}
            }
            if(yieldToTraffic&&TrafficOnWay(v,way) is {}){Pr57276Gate(v,2);v.Cause="Aguardando espaço para acessar a faixa";return false;}
        }
        if(BodyOnWay(v,way) is {} body){Pr57276Gate(v,3);if(!admitted)v.Cause=yieldToTraffic?"Aguardando espaço para acessar a faixa":"Fila para acesso de carga";return false;}
        if(!admitted){
            var departure=Leaving(v);
            if(limit==null){WayOut(v,out _,out var endIndex);limit=new(){Route=v.Route.ToList(),SitePath=v.SitePath.ToList(),EndRouteIndex=endIndex};}
            // PR57: admission can turn an otherwise ordinary passenger car into
            // an owner of a physical dock reservation during this same slice.
            // Such a car was excluded from the initial owner superset. Fall back
            // to the exact full scan for the rest of this slice, rather than
            // silently miss its newly admitted path. No cross-Advance cache.
            if(Pr57SiteOwnerRosterEnabled&&_pr57SiteOwnerRoster is not null&&
                v.PassengerCar&&v.Machine==PavingMachine.None&&
                v.SitePath.Count==0&&v.ShipmentId==null&&v.ManoeuvreDock==null)
                _pr57SiteOwnerRoster=null;
            v.ManoeuvreDock=dock;v.ManoeuvreDeparture=departure;v.MovementAdmission=limit;
            // A reroute can retain the previous dock identity while the body
            // waits elsewhere. The newly acquired exit starts at its real pose,
            // never along a fictitious diagonal from that old dock to Route[0].
            v.DockCorridor=PhysicalDockWay(v,departure?v.Position:dock,departure).Select(p=>new MovementPose{Position=p.Position,Heading=p.Heading}).ToList();
            v.ManoeuvreSpaces=v.ManoeuvreSpaces.Concat(spaces.Select(s=>s.Id)).Where(id=>v.IntersectionClaim!=id).Distinct().ToList();
            Pr57ReservationWrite(v,"admit-dock");
        }
        Pr57276Gate(v,4);
        return true;
    }

    // Only the physical connection dock <-> lane, not the onward lane trip.
    List<(Vehicle Body,Point Position,double Heading)> PhysicalDockWay(Vehicle v,Point dock,bool departure)
    {
        if(departure)return Manoeuvre(v,v.Route,0,dock,v.Heading,v.SitePathBeforeRoad?v.SitePath.Skip(v.SitePathIndex).ToList():[],false,int.MaxValue,out _,out _,through:0);
        var lane=Manoeuvre(v,v.Route,v.RouteIndex,v.Position,v.Heading,[],false,int.MaxValue,out _);
        var start=lane.Count>0?lane[^1].Position:v.Position;var heading=lane.Count>0?lane[^1].Heading:v.Heading;
        if(v.SitePath.Count>v.SitePathIndex)return Manoeuvre(v,[],0,start,heading,v.SitePath.Skip(v.SitePathIndex).ToList(),true,0,out _);
        var poses=new List<(Point Position,double Heading)>();SamplePoses(poses,start,dock,heading);
        return poses.Select(p=>(v,p.Position,p.Heading)).ToList();
    }
    List<(Vehicle Body,Point Position,double Heading)> OwnedDockWay(Vehicle o)
    {
        if(o.ManoeuvreDock is not {} dock||o.Status is VehicleStatus.Outside or VehicleStatus.Towed)return [];
        var way=o.ManoeuvreDeparture==true?o.DockCorridor.Select(p=>(o,p.Position,p.Heading)).ToList():[];
        if(DockAdmitted(o,dock))way.AddRange(Leaving(o)?WayOut(o,out _):AdmittedEntryWay(o,dock,out _));
        return way;
    }
    bool ConflictsWithDockWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> way)
        =>State.Vehicles.Any(o=>o.Id!=v.Id&&o.ManoeuvreDock!=null&&
            OwnedDockWay(o).Any(q=>way.Any(p=>p.Position.Distance(q.Position)<12&&BodiesOverlap(v,p.Position,p.Heading,o,q.Position,q.Heading))));

    // Trucks that own a dock corridor overlapping v's dock: a truck admitted into,
    // at or out of a dock whose body pose overlaps v's at its dock.
    IEnumerable<Vehicle> DockOwners(Vehicle v,Point dock)
    {
        List<(Vehicle Body,Point Position,double Heading)>? entering=null;
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.PassengerCar||o.Machine!=PavingMachine.None||o.Status is VehicleStatus.Outside or VehicleStatus.Towed)continue;
            var at=o.ManoeuvreDock is {} owned&&(o.ManoeuvreDeparture==true||o.MovementAdmission?.ReachesDock==true)?owned:o.Status is VehicleStatus.Unloading or VehicleStatus.Parked?o.DockPosition:null;if(at is not {} other||other.Distance(dock)>16)continue;
            if(other.Distance(dock)<.1||Penetration(v,dock,v.Heading,o,other,o.Heading)>-.1||Penetration(v,dock,v.Heading,o,o.Position,o.Heading)>-.1){yield return o;continue;}
            // Distinct dock endpoints can share the same physical corridor. A
            // newcomer must wait outside that owner's exit too, instead of
            // stopping on it while waiting for a different occupied bay.
            entering??=WayIn(v,dock,out _);
            if(DockWayOut(o).Any(exit=>entering.Any(p=>p.Position.Distance(exit.Position)<12&&BodiesOverlap(v,p.Position,p.Heading,o,exit.Position,exit.Heading))))yield return o;
        }
    }
    List<(Vehicle Body,Point Position,double Heading)> DockWayOut(Vehicle o)=>o.ManoeuvreDock!=null&&o.ManoeuvreDeparture==true?OwnedDockWay(o):o.Status==VehicleStatus.Travelling&&o.ManoeuvreDock!=null&&!Leaving(o)&&o.DockPosition is {} bay?PredictedWayOut(o,bay,o.Heading,[],out _):WayOut(o,out _);
    // The ways out of the trucks owning v's dock corridor, which v keeps out of
    // until its own entry is admitted.
    List<(Vehicle Body,Point Position,double Heading)> OwnersWayOut(Vehicle v,Point dock)
    {
        var held=new List<(Vehicle Body,Point Position,double Heading)>();
        var pr57278Sample=Pr57278Sample(v);
        var pr57278Owners=0;
        foreach(var o in DockOwners(v,dock)){
            var exit=DockWayOut(o);
            if(pr57278Sample){Pr57278Owner(v,o,exit);pr57278Owners++;}
            held.AddRange(exit);
        }
        if(pr57278Sample)Pr57278End(v,pr57278Owners,held.Count);
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
    bool EntersBay(Vehicle v)=>v.ShipmentId!=null&&!v.PassengerCar&&v.Machine==PavingMachine.None&&!IsTow(v)&&v.RefuellingAt==null&&v.DockPosition!=null&&!v.SitePathBeforeRoad&&v.SitePathIndex==0&&v.Status==VehicleStatus.Travelling&&InMap(v.Position);
    List<(Vehicle Body,Point Position,double Heading)> WayIn(Vehicle v,Point dock,out List<RoadNode> spaces)
    {
        var lane=Manoeuvre(v,v.Route,v.RouteIndex,v.Position,v.Heading,[],false,int.MaxValue,out spaces);
        var end=lane.Count>0?lane[^1].Position:v.Position;var heading=lane.Count>0?lane[^1].Heading:v.Heading;
        // A works-site approach drives its site path to the paver's dock; a bay, the slide.
        if(v.SitePath.Count>v.SitePathIndex){
            var site=Manoeuvre(v,[],0,end,heading,v.SitePath.Skip(v.SitePathIndex).ToList(),true,0,out var siteSpaces);lane.AddRange(site);
            foreach(var space in siteSpaces)if(!spaces.Contains(space))spaces.Add(space);
            return lane;
        }
        var run=new List<(Point Position,double Heading)>();SamplePoses(run,end,dock,heading);lane.AddRange(run.Select(p=>(v,p.Position,p.Heading)));
        // The final slide is physical motion too. It may cross a junction from
        // a lane point that is already outside it, especially at a curb near a T.
        foreach(var node in SharedNodes())if(!spaces.Contains(node)&&run.Any(p=>EntersArea(v,p.Position,p.Heading,node)))spaces.Add(node);
        return lane;
    }
    // A delivery is still lane traffic until it reaches a real shared space.
    // Its local plan crosses only as far as the first whole-body waiting pose
    // outside that pavement and outside the exits of present dock owners.
    List<(Vehicle Body,Point Position,double Heading)> EntryPlan(Vehicle v,Point dock,out List<RoadNode> spaces,out LocalMovementAdmission limit,out bool needed)
    {
        // #277 observes the original execution, including all negative geometry.
        // The timer never gates work or caches a decision.
        var pr57277Time=Pr57277Begin(v);
        var exits=OwnersWayOut(v,dock);
        pr57277Time=Pr57277Stage(pr57277Time,0);
        bool Clear(Point p,double h)=>!exits.Any(o=>o.Position.Distance(p)<12&&BodiesOverlap(v,p,h,o.Body,o.Position,o.Heading));
        var way=Manoeuvre(v,v.Route,v.RouteIndex,v.Position,v.Heading,[],false,1,out spaces,out var endIndex,Clear);
        pr57277Time=Pr57277Stage(pr57277Time,1);
        var safe=way.Count>0&&SharedNodes().All(n=>!EntersArea(v,way[^1].Position,way[^1].Heading,n))&&Clear(way[^1].Position,way[^1].Heading);
        pr57277Time=Pr57277Stage(pr57277Time,2);
        if(!safe)way=WayIn(v,dock,out spaces);
        pr57277Time=Pr57277Stage(pr57277Time,3);
        limit=new(){Route=v.Route.ToList(),SitePath=v.SitePath.ToList(),EndRouteIndex=endIndex,ReachesDock=!safe};
        needed=spaces.Count>0||!safe||exits.Any(o=>way.Any(p=>p.Position.Distance(o.Position)<12&&BodiesOverlap(v,p.Position,p.Heading,o.Body,o.Position,o.Heading)));
        Pr57277Stage(pr57277Time,4);
        Pr57277Record(v,exits.Count,spaces.Count,way.Count,safe,needed);
        return way;
    }

    List<(Vehicle Body,Point Position,double Heading)> AdmittedEntryWay(Vehicle v,Point dock,out List<RoadNode> spaces)
    {
        if(v.MovementAdmission?.ReachesDock==true)return WayIn(v,dock,out spaces);
        return Manoeuvre(v,v.Route,v.RouteIndex,v.Position,v.Heading,[],false,int.MaxValue,out spaces,out _,through:v.MovementAdmission?.EndRouteIndex??v.RouteIndex-1);
    }
    // Returns the poses v keeps out of while its entry is not admitted (empty
    // once admitted, or when it is admitted now).
    List<(Vehicle Body,Point Position,double Heading)> BayEntryHold(Vehicle v)
    {
        if(v.DockPosition is not {} dock){Pr57276Gate(v,5);return [];}
        if(DockAdmitted(v,dock)){Pr57276Gate(v,6);return [];}
        var way=EntryPlan(v,dock,out var spaces,out var limit,out var needed);
        if(!needed){Pr57276Gate(v,7);return [];}
        var crewHeading=v.Route.Count>0&&_roads.TryGetValue(v.Route[^1],out var crewAccess)&&
            (crewAccess.Highway||crewAccess.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-crewAccess.Position.X)>.1))?Math.PI/2:0;
        if(limit.ReachesDock&&v.Machine==PavingMachine.None&&v.CrewAboard&&!CrewDockClear(dock,crewHeading,_roads.GetValueOrDefault(v.Route[^1]))){
            // Road completion can invalidate a previously chosen curb position.
            // Replan only the unadmitted intention, without moving the body.
            var shipment=State.Shipments.FirstOrDefault(s=>s.Id==v.ShipmentId);
            if(v.Route.Count>0&&_roads.TryGetValue(v.Route[^1],out var access)){
                var revised=DockAt(access,BuildingById(shipment?.DestinationBuildingId),true);
                if(CrewDockClear(revised,crewHeading,access)){v.DockPosition=dock=revised;way=EntryPlan(v,dock,out spaces,out limit,out needed);}
                else {Pr57276Gate(v,8);v.Cause=WorkCause.PavingSiteOccupied;return way;}
            }
        }
        var occupied=false;
        if(limit.ReachesDock){
            // Reaching the end of the physical route is not proof that it has
            // a safe endpoint. A missing safe pose never grants the whole trip.
            var end=way.Count>0?way[^1]:(v,v.Position,v.Heading);
            // At the dock the acquired space is solely this vehicle's during
            // its physical service (ARCHITECTURE "onde se espera"). It need
            // not be outside its own acquired crossing, only other owners' exits.
            occupied=OwnersWayOut(v,dock).Any(o=>o.Position.Distance(end.Item2)<12&&BodiesOverlap(v,end.Item2,end.Item3,o.Body,o.Position,o.Heading))||
                DockOwners(v,dock).Any(o=>o.ManoeuvreDock is {} other&&Penetration(v,dock,v.Heading,o,other,o.Heading)>-.1);
        }
        if(!occupied&&AdmitManoeuvre(v,way,spaces,dock,false,limit)){Pr57276Gate(v,11);return [];}
        Pr57276Gate(v,occupied?9:10);
        v.Cause="Fila para acesso de carga";
        // No prefix may be taken while the whole local manoeuvre is blocked.
        Pr57276Gate(v,12);
        return way;
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
        // The next site point may be outside a junction while an admitted
        // truck's body sweeps through it. Do not enter that corridor first and
        // only discover the truck's junction claim at the following point.
        if(m.MachinePhase==MachinePhase.LeavingSite&&ConflictsWithDockWay(m,ahead))return false;
        foreach(var node in needed){
            var area=_junctionAreas.GetValueOrDefault(node.Id);
            if(State.Vehicles.Any(o=>o.Id!=m.Id&&(Holds(o,node.Id)||area!=null&&(o.IntersectionClaim is {} held&&area.Contains(held)&&o.Position.Distance(node.Position)<9||o.ManoeuvreSpaces.Any(area.Contains)))))
                return false;
        }
        m.ManoeuvreSpaces.AddRange(needed.Select(node=>node.Id));
        Pr57ReservationWrite(m,"machine-spaces");
        return true;
    }
    // Admitted manoeuvres lane traffic keeps out of: a way out of a bay or a way
    // into it, while held. The hold stops at the first body already on it.
    List<(Vehicle Machine,Point Position,double Heading)> AdmittedManoeuvre(Vehicle o)
    {
        var held=new List<(Vehicle,Point,double)>();if(o.ManoeuvreDock==null||o.Status!=VehicleStatus.Travelling)return held;
        var way=o.ManoeuvreDock is {} dock&&DockAdmitted(o,dock)?Leaving(o)?WayOut(o,out _):AdmittedEntryWay(o,dock,out _):OwnedDockWay(o);
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
        if(v.Status is VehicleStatus.Outside or VehicleStatus.Towed||v.PassengerCar){v.ManoeuvreDock=null;v.DockCorridor.Clear();v.MovementAdmission=null;v.ManoeuvreSpaces.Clear();Pr57ReservationWrite(v,"release-clear");return;}
        if(v.Status==VehicleStatus.Stranded||v.Machine!=PavingMachine.None&&v.Status!=VehicleStatus.Parked&&v.Status!=VehicleStatus.Travelling)return;
        List<(Vehicle Body,Point Position,double Heading)> rest=v.Machine!=PavingMachine.None?MachinePathAhead(v)
            :v.Status!=VehicleStatus.Travelling?[]
            :v.ManoeuvreDock is {} target&&DockAdmitted(v,target)?Leaving(v)?WayOut(v,out _):AdmittedEntryWay(v,target,out _)
            :v.ManoeuvreDock==null&&AdmissionPathMatches(v)&&v.RouteIndex<=v.MovementAdmission!.EndRouteIndex?Manoeuvre(v,v.Route,v.RouteIndex,v.Position,v.Heading,[],false,int.MaxValue,out _,out _,through:v.MovementAdmission.EndRouteIndex):[];
        // The body itself is measured on the carriageway square: the planning area
        // already counts a truck length around the node, which a parked body beside
        // it (a paver at the curb) would never leave.
        v.ManoeuvreSpaces.RemoveAll(id=>!_roads.TryGetValue(id,out var node)||!EntersArea(v,v.Position,v.Heading,node,CarriagewayHalfWidth)&&!rest.Any(p=>EntersArea(v,p.Position,p.Heading,node)));
        Pr57ReservationWrite(v,"release-remove");
        if(v.ManoeuvreDock is {} dock){
            var atDock=v.Status is VehicleStatus.Unloading or VehicleStatus.Parked&&v.Position.Distance(dock)<.1;
            // A destination equal to an old dock is not an active entry. A
            // rerouted vehicle keeps only physically occupied old possessions,
            // never an expired departure permission all the way to that target.
            var entering=v.Status==VehicleStatus.Travelling&&v.ManoeuvreDeparture!=true&&v.DockPosition is {} goal&&goal.Distance(dock)<.1&&DockAdmitted(v,dock);
            // Old saves have no sweep: reconstruct from the retained dock and
            // actual route; never infer clearance from distance to a node.
            if(v.DockCorridor.Count==0)v.DockCorridor=PhysicalDockWay(v,dock,v.ManoeuvreDeparture==true).Select(p=>new MovementPose{Position=p.Position,Heading=p.Heading}).ToList();
            var inCorridor=v.DockCorridor.Any(p=>BodiesOverlap(v,v.Position,v.Heading,v,p.Position,p.Heading));
            if(!atDock&&!entering&&!inCorridor){v.ManoeuvreDock=null;v.DockCorridor.Clear();}
        }
    }
}
