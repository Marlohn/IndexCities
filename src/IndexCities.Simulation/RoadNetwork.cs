namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    Dictionary<int,RoadNode> _roads=new();
    HashSet<int> _exteriorConnected=new();Dictionary<(BuildingKind,Point,double),int?> _accessCache=new();
    void RebuildRoadGraph(bool changed=true){_roads=State.Roads.ToDictionary(r=>r.Id);foreach(var n in State.Roads){n.Neighbours=State.Roads.Where(r=>r.Id!=n.Id&&r.Position.Distance(n.Position)<=State.Tuning.RoadStep+.01).Select(r=>r.Id).ToList();}RebuildJunctionAreas();if(changed)State.RoadRevision++;_accessCache.Clear();_exteriorConnected.Clear();var q=new Queue<int>();q.Enqueue(EntryNode().Id);while(q.Count>0){var id=q.Dequeue();if(!_exteriorConnected.Add(id))continue;foreach(var next in _roads[id].Neighbours)if(_roads[next].MaintenanceRatio>.05)q.Enqueue(next);}}
    RoadNode AddRoad(Point point,bool highway=false)
    {
        var existing=State.Roads.FirstOrDefault(r=>r.Position.Distance(point)<.05);if(existing!=null)return existing;
        var node=new RoadNode{Id=Id(),Position=point,Highway=highway,MaintenanceDueAt=State.ElapsedSeconds+State.Tuning.PublicMaintenanceIntervalHours*3600};State.Roads.Add(node);return node;
    }
    public Point SnapRoad(Point p)=>new(Math.Round(p.X/State.Tuning.RoadStep)*State.Tuning.RoadStep,Math.Round(p.Z/State.Tuning.RoadStep)*State.Tuning.RoadStep);
    public IReadOnlyList<Point> RoadShape(Point start,Point end,bool reverseCorner=false)
    {
        start=SnapRoad(start);end=SnapRoad(end);var corner=reverseCorner?new Point(start.X,end.Z):new Point(end.X,start.Z);var list=new List<Point>();
        void Line(Point a,Point b){var d=a.Distance(b);var count=(int)Math.Round(d/State.Tuning.RoadStep);for(var i=0;i<=count;i++){var f=count==0?0:i/(double)count;var p=new Point(a.X+(b.X-a.X)*f,a.Z+(b.Z-a.Z)*f);if(list.Count==0||list[^1].Distance(p)>.05)list.Add(p);}}
        Line(start,corner);Line(corner,end);return list;
    }
    public RoadNode? Access(Building b)
    {
        var key=(b.Kind,b.Position,b.Rotation);if(_accessCache.TryGetValue(key,out var id))return id==null?null:_roads.GetValueOrDefault(id.Value);
        var result=State.Roads.Where(r=>!r.Highway&&r.MaintenanceRatio>.05&&r.Position.Distance(b.Door)<=State.Tuning.RoadWidth/2+3).MinBy(r=>r.Position.Distance(b.Door));if(_accessCache.Count>4096)_accessCache.Clear();_accessCache[key]=result?.Id;return result;
    }
    RoadNode? NearestRoad(Point p,bool allowHighway=true)=>State.Roads.Where(r=>allowHighway||!r.Highway).MinBy(r=>r.Position.Distance(p));
    public bool HasExteriorAccess(Building b)=>Access(b) is { } n&&_exteriorConnected.Contains(n.Id);
    RoadNode EntryNode()=>State.Roads.First(r=>r.Highway&&r.Position.Distance(State.Entry)<.1);
    public List<int> FindRoadRoute(int from,int to)
    {
        if(!_roads.ContainsKey(from)||!_roads.ContainsKey(to))return [];
        var queue=new Queue<int>();var parent=new Dictionary<int,int>{{from,-1}};queue.Enqueue(from);
        while(queue.Count>0){var id=queue.Dequeue();if(id==to)break;foreach(var next in _roads[id].Neighbours){if(parent.ContainsKey(next)||_roads[next].MaintenanceRatio<=.05)continue;parent[next]=id;queue.Enqueue(next);}}
        if(!parent.ContainsKey(to))return [];var result=new List<int>();for(var id=to;id!=-1;id=parent[id])result.Add(id);result.Reverse();return result;
    }
    double RouteLength(IReadOnlyList<int> route){double length=0;for(var i=1;i<route.Count;i++)length+=_roads[route[i-1]].Position.Distance(_roads[route[i]].Position);return length;}
    public List<Point> PedestrianRoute(Point from,Building destination)=>PedestrianPath(from,destination.Door,Access(destination));
    List<Point> PedestrianPath(Point from,Point door,RoadNode? b)
    {
        var originalFrom=from;Point? crossingApproach=null;var laneApproach=new List<Point>();var a=NearestRoad(from);
        if(a!=null&&!a.Highway&&(!a.Crosswalk||StraightRoad(a,out _))){
            var horizontal=a.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-a.Position.X)>.1);var vertical=a.Neighbours.Any(id=>Math.Abs(_roads[id].Position.Z-a.Position.Z)>.1);
            var withinCarriageway=horizontal&&Math.Abs(from.Z-a.Position.Z)<=3.3||vertical&&Math.Abs(from.X-a.Position.X)<=3.3;
            if(withinCarriageway){
                // Old paths could leave a walker inside a straight carriageway,
                // including at a marked node whose paint is offset near a highway.
                // Approach an actual crosswalk along that same road first, then
                // cross it physically instead of jumping sideways to a curb.
                var straight=State.Roads.Where(r=>r.Crosswalk&&!r.Highway&&r.Position.Distance(from)<=Math.Max(24,State.Tuning.CrosswalkSpacing+State.Tuning.RoadStep)&&((vertical&&!horizontal&&Math.Abs(r.Position.X-a.Position.X)<.1&&r.Neighbours.All(id=>Math.Abs(_roads[id].Position.X-r.Position.X)<.1))||(horizontal&&!vertical&&Math.Abs(r.Position.Z-a.Position.Z)<.1&&r.Neighbours.All(id=>Math.Abs(_roads[id].Position.Z-r.Position.Z)<.1)))&&SameRoadLine(a,r,horizontal)).MinBy(r=>CrosswalkCentre(r,State).Distance(from));
                if(straight!=null){var centre=CrosswalkCentre(straight,State);crossingApproach=vertical?new Point(from.X,centre.Z):new Point(centre.X,from.Z);from=crossingApproach.Value;a=straight;}
                else {
                    var recovery=State.Roads.Where(r=>r.Crosswalk&&!r.Highway).Select(r=>(Node:r,Route:FindRoadRoute(a.Id,r.Id))).Where(r=>r.Route.Count>0&&RouteLength(r.Route)<=State.Tuning.CrosswalkSpacing*3+State.Tuning.RoadWidth).OrderBy(r=>RouteLength(r.Route)).ThenBy(r=>r.Node.Id).FirstOrDefault();
                    if(recovery.Node!=null){laneApproach.AddRange(recovery.Route.Select(id=>_roads[id].Position));a=recovery.Node;crossingApproach=CrosswalkCentre(a,State);from=crossingApproach.Value;}
                }
            }
        }
        if(a==null||b==null||a.Position.Distance(from)>24)return [];var ids=FindRoadRoute(a.Id,b.Id);if(ids.Count==0)return [];var offset=State.Tuning.RoadWidth/2-.7;
        var anchors=new List<RoadNode>{_roads[ids[0]]};
        for(var i=1;i<ids.Count-1;i++){var prior=_roads[ids[i-1]];var n=_roads[ids[i]];var next=_roads[ids[i+1]];var incoming=n.Position-prior.Position;var outgoing=next.Position-n.Position;if(Math.Abs(incoming.X*outgoing.Z-incoming.Z*outgoing.X)>.01||(prior.Highway&&n.Highway)!=(n.Highway&&next.Highway))anchors.Add(n);}
        if(ids.Count>1)anchors.Add(_roads[ids[^1]]);
        var directions=anchors.Zip(anchors.Skip(1)).Select(pair=>pair.Second.Position-pair.First.Position).ToList();if(directions.Count==0)directions.Add(new(1,0));
        var firstDirection=directions[0];var perpendicular=new Point(firstDirection.Z,-firstDirection.X);var lateral=from-a.Position;
        var sideSign=lateral.X*perpendicular.X+lateral.Z*perpendicular.Z<0?-1:1;
        Point Side(Point direction,bool highway){if(highway)return new(0,State.Tuning.RoadWidth/2+1+(direction.X>=0?-.35:.35));var length=direction.Distance(new(0,0));return length<.001?new(0,offset):new Point(direction.Z/length,-direction.X/length)*(sideSign*offset);}
        var firstHighway=a.Highway&&(anchors.Count==1||anchors[1].Highway);var firstSide=Side(firstDirection,firstHighway);
        var startCentre=crossingApproach!=null?CrosswalkCentre(a,State):a.Position;var walk=new List<Point>{startCentre+firstSide};
        for(var i=1;i<anchors.Count-1;i++){var node=anchors[i];var incoming=directions[i-1];var outgoing=directions[i];var before=i==1?firstSide:Side(incoming,anchors[i-1].Highway&&node.Highway);var after=Side(outgoing,node.Highway&&anchors[i+1].Highway);
            if(Math.Abs(incoming.X*outgoing.Z-incoming.Z*outgoing.X)>.01)walk.Add(node.Position+before+after);else {walk.Add(node.Position+before);if(before!=after)walk.Add(node.Position+after);}}
        if(anchors.Count>1)walk.Add(b.Position+Side(directions[^1],anchors[^2].Highway&&b.Highway));var points=new List<Point>{originalFrom};
        void Add(Point point){if(points[^1].Distance(point)>.001)points.Add(point);}
        bool Crossing(RoadNode node,Point start,Point finish){
            if(!CrossesLane(start,finish,node)){Add(finish);return true;}
            if(node.Highway){Add(finish);return true;}
            // A diagonal approach does not determine the carriageway's axis.
            var horizontal=StraightRoad(node,out var roadHorizontal)?roadHorizontal:Math.Abs(finish.Z-start.Z)>=Math.Abs(finish.X-start.X);
            if(SharedCrossing(node,horizontal,start,finish) is not {} crossing)return false;
            var startSign=Math.Sign(horizontal?start.Z-node.Position.Z:start.X-node.Position.X);if(startSign==0)startSign=1;
            var finishSign=Math.Sign(horizontal?finish.Z-node.Position.Z:finish.X-node.Position.X);if(finishSign==0)finishSign=-startSign;
            var source=horizontal?crossing.Centre+new Point(0,startSign*offset):crossing.Centre+new Point(startSign*offset,0);
            var target=horizontal?crossing.Centre+new Point(0,finishSign*offset):crossing.Centre+new Point(finishSign*offset,0);
            var delta=target-source;var length=delta.Distance(new(0,0));var lane=length<.001?new Point(0,0):new Point(delta.Z/length,-delta.X/length)*.25;
            Add(source+lane);Add(target+lane);Add(finish);return true;
        }
        foreach(var point in laneApproach)Add(point);
        if(crossingApproach!=null)Add(crossingApproach.Value);
        // Crossing constructs the approach to real paint. A radial curb point
        // inserted beforehand could already cross the lane outside that paint.
        if(CrossesLane(from,walk[0],a)){if(!Crossing(a,points[^1],walk[0]))return [];}else Add(walk[0]);
        foreach(var point in walk.Skip(1))Add(point);var entry=b.Position+SideTowards(b.Position,door,offset);if(!Crossing(b,points[^1],entry))return [];Add(door);return points;
    }

    static Point SideTowards(Point road,Point door,double offset){var delta=door-road;var d=delta.Distance(new(0,0));return d<.01?new(0,offset):new(delta.X/d*offset,delta.Z/d*offset);}
    Point CrewPedestrianSite(RoadNode access){var horizontal=access.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-access.Position.X)>.1);var offset=State.Tuning.RoadWidth/2-.7;return access.Position+(horizontal?new Point(0,offset):new Point(offset,0));}
    public CommandResult SetJunction(int id,JunctionControl control)
    {
        if(!_roads.TryGetValue(id,out var node)||node.Highway||node.Neighbours.Count<3)return new(false,"Selecione um cruzamento urbano");node.Control=control;node.PlayerEdited=true;return new(true,"Controle do cruzamento atualizado");
    }
    void SetDefaultJunctions(){UpdateCrosswalks();foreach(var n in State.Roads.Where(n=>!n.PlayerEdited&&!n.Highway&&n.Neighbours.Count>=3)){if(n.Neighbours.Count>=4){n.Control=JunctionControl.RightHand;continue;}var ns=n.Neighbours.Select(id=>_roads[id]).ToArray();n.Control=ns.Any(a=>ns.Any(b=>a.Id!=b.Id&&Math.Abs(a.Position.Z-n.Position.Z)<.1&&Math.Abs(b.Position.Z-n.Position.Z)<.1))?JunctionControl.ThroughX:JunctionControl.ThroughZ;}}
    public static Point CrosswalkCentre(RoadNode node,CityState state)
    {
        var highway=node.Highway?null:state.Roads.FirstOrDefault(r=>r.Highway&&node.Neighbours.Contains(r.Id));
        if(highway==null)return node.Position;
        var neighbours=state.Roads.Where(r=>node.Neighbours.Contains(r.Id)).ToArray();
        if(neighbours.Any(r=>Math.Abs(r.Position.X-node.Position.X)>.1)&&neighbours.Any(r=>Math.Abs(r.Position.Z-node.Position.Z)>.1))return node.Position;
        var direction=node.Position-highway.Position;var length=direction.Distance(new(0,0));
        return length<.001?node.Position:highway.Position+direction*((state.Tuning.RoadWidth/2+1)/length);
    }
    bool InWater(Point p,double margin=0)=>State.SchemaVersion==1?p.Distance(State.LakeCentre)<State.LakeRadius+margin:LakeGeometry.SignedDistance(State,p)<margin;
    public double DistanceToLakeShore(Point p)=>Math.Abs(LakeGeometry.SignedDistance(State,p));
    bool InMap(Point p,double margin=0)=>Math.Abs(p.X)+margin<State.Tuning.MapHalfSize&&Math.Abs(p.Z)+margin<State.Tuning.MapHalfSize;
    static bool Inside(Building b,Point p,double padding=0)
    {
        var d=p-b.Position;var x=d.X*Math.Cos(b.Rotation)-d.Z*Math.Sin(b.Rotation);var z=d.X*Math.Sin(b.Rotation)+d.Z*Math.Cos(b.Rotation);
        return Math.Abs(x)<b.Definition.Width/2+padding&&Math.Abs(z)<b.Definition.Depth/2+padding;
    }
    static IEnumerable<Point> Corners(Building b){var w=b.Definition.Width/2;var d=b.Definition.Depth/2;foreach(var x in new[]{-w,w})foreach(var z in new[]{-d,d})yield return b.Position+new Point(x*Math.Cos(b.Rotation)+z*Math.Sin(b.Rotation),-x*Math.Sin(b.Rotation)+z*Math.Cos(b.Rotation));}
    static bool Overlap(Building a,Building b)
    {
        var ac=Corners(a).ToArray();var bc=Corners(b).ToArray();var axes=new[]{new Point(Math.Cos(a.Rotation),-Math.Sin(a.Rotation)),new Point(Math.Sin(a.Rotation),Math.Cos(a.Rotation)),new Point(Math.Cos(b.Rotation),-Math.Sin(b.Rotation)),new Point(Math.Sin(b.Rotation),Math.Cos(b.Rotation))};
        foreach(var axis in axes){var ap=ac.Select(p=>p.X*axis.X+p.Z*axis.Z);var bp=bc.Select(p=>p.X*axis.X+p.Z*axis.Z);if(ap.Max()<=bp.Min()+.05||bp.Max()<=ap.Min()+.05)return false;}return true;
    }
}
