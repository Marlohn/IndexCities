namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // Physical walking surface, from the same rectangles CityWorld draws for
    // each road cell. Urban asphalt is crossed only inside painted bands. The
    // highway has no paint: as before, walkers keep to its +Z side and cross it
    // only where an urban road branches off it, never at its end by the paint.
    sealed class WalkSurface
    {
        public readonly record struct Box(double X0,double X1,double Z0,double Z1)
        {
            public bool Has(Point p)=>p.X>X0+1e-6&&p.X<X1-1e-6&&p.Z>Z0+1e-6&&p.Z<Z1-1e-6;
        }
        public const int Urban=0,Highway=1,Band=2,Zone=3;
        public readonly List<Box>[] Boxes=[new(),new(),new(),new()];
        public readonly Dictionary<(int,int),List<(int Kind,int Index)>> Cells=new();
        public readonly Dictionary<(int,int),List<Building>> Lots=new();
        // Sidewalk graph: points beside road cells and crosswalk ends. Edges join
        // the same or adjacent road cells only, so separate fragments stay apart.
        public readonly List<(Point Position,int Road,int Partner)> Vertices=new();
        public readonly Dictionary<int,List<int>> RoadVertices=new();
        public List<(int To,double Cost)>?[] Edges=[];
        public long Signature;
    }
    WalkSurface? _walkSurface;
    const double WalkCell=8,WalkLink=6.5,CrosswalkHalfLength=1.95,UnpaintedTolerance=.05;

    static IEnumerable<(int,int)> WalkCells(double x0,double x1,double z0,double z1)
    {
        for(var x=(int)Math.Floor(x0/WalkCell);x<=(int)Math.Floor(x1/WalkCell);x++)
        for(var z=(int)Math.Floor(z0/WalkCell);z<=(int)Math.Floor(z1/WalkCell);z++)yield return (x,z);
    }

    WalkSurface Surface()
    {
        var p324start=Pr57324Enabled?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        if(Pr57324Enabled)_pr57324SurfaceCalls++;
        // Paint and lots change without a new road graph; buildings never move.
        long signature=State.Roads.Count;foreach(var r in State.Roads)if(r.Crosswalk)signature=signature*31+r.Id;
        foreach(var b in State.Buildings)if(!b.Removed)signature=signature*17+b.Id;
        if(_walkSurface is {} cached&&cached.Signature==signature){if(Pr57324Enabled)_pr57324SurfaceTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-p324start;return cached;}
        if(Pr57324Enabled)_pr57324SurfaceBuilds++;
        var s=new WalkSurface{Signature=signature};var half=State.Tuning.RoadWidth/2;var step=State.Tuning.RoadStep;
        var asphalt=State.Tuning.RoadWidth*(6.5/10.5);var corner=half-asphalt/2;var side=half-.7;
        void Add(int kind,Point c,double width,double depth)
        {
            var box=new WalkSurface.Box(c.X-width/2,c.X+width/2,c.Z-depth/2,c.Z+depth/2);s.Boxes[kind].Add(box);
            foreach(var cell in WalkCells(box.X0,box.X1,box.Z0,box.Z1)){if(!s.Cells.TryGetValue(cell,out var list))s.Cells[cell]=list=new();list.Add((kind,s.Boxes[kind].Count-1));}
        }
        foreach(var road in State.Roads.Where(r=>!r.ConstructionOnly)){
            var kind=road.Highway?WalkSurface.Highway:WalkSurface.Urban;
            var horizontal=road.Highway||road.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-road.Position.X)>.1);var vertical=road.Neighbours.Any(id=>Math.Abs(_roads[id].Position.Z-road.Position.Z)>.1);
            if(!horizontal&&!vertical)horizontal=true;
            if(horizontal&&vertical){
                Add(kind,road.Position,asphalt,asphalt);
                foreach(var neighbour in road.Neighbours.Select(id=>_roads[id].Position-road.Position)){
                    var alongX=Math.Abs(neighbour.X)>.1;var at=alongX?new Point(Math.Sign(neighbour.X)*(asphalt/2+corner/2),0):new Point(0,Math.Sign(neighbour.Z)*(asphalt/2+corner/2));
                    Add(kind,road.Position+at,alongX?corner:asphalt,alongX?asphalt:corner);
                }
            }
            else if(horizontal)Add(kind,road.Position,step,asphalt);else Add(kind,road.Position,asphalt,step);
            foreach(var band in CrosswalkLocations(road,State))Add(WalkSurface.Band,band.Centre,band.HorizontalRoad?CrosswalkHalfLength*2:asphalt,band.HorizontalRoad?asphalt:CrosswalkHalfLength*2);
            // Where an urban road branches off the highway, a walk along its
            // sidewalks crosses the highway there, as before.
            if(road.Highway&&road.Neighbours.Any(id=>!_roads[id].Highway&&Math.Abs(_roads[id].Position.Z-road.Position.Z)>.1))Add(WalkSurface.Zone,road.Position,(side+CrosswalkHalfLength)*2,asphalt);
        }
        foreach(var lot in State.Buildings.Where(b=>!b.Removed)){
            var reach=Math.Sqrt(lot.Definition.Width*lot.Definition.Width+lot.Definition.Depth*lot.Definition.Depth)/2;
            foreach(var cell in WalkCells(lot.Position.X-reach,lot.Position.X+reach,lot.Position.Z-reach,lot.Position.Z+reach)){if(!s.Lots.TryGetValue(cell,out var list))s.Lots[cell]=list=new();list.Add(lot);}
        }
        // The map edge continues the highway outward; nobody walks around it.
        bool Walkable(Point p)=>InMap(p)&&!InAsphalt(s,p)&&!Blocked(s,p,p);
        void Vertex(Point p,int road,int partner=-1)
        {
            if(!Walkable(p))return;s.Vertices.Add((p,road,partner));
            if(!s.RoadVertices.TryGetValue(road,out var own))s.RoadVertices[road]=own=new();own.Add(s.Vertices.Count-1);
        }
        foreach(var road in State.Roads.Where(r=>!r.ConstructionOnly)){
            var deltas=road.Neighbours.Select(id=>_roads[id].Position-road.Position).ToArray();
            var horizontal=road.Highway||deltas.Any(d=>Math.Abs(d.X)>.1);var vertical=deltas.Any(d=>Math.Abs(d.Z)>.1);
            if(horizontal&&vertical||deltas.Length<2){
                // Corners, junctions and road ends: walk around the actual cell.
                foreach(var x in new[]{-side,0,side})foreach(var z in new[]{-side,0,side})if(x!=0||z!=0)Vertex(road.Position+new Point(x,z),road.Id);
            }
            // Along the highway only its +Z side is walked, as before.
            else if(horizontal){if(!road.Highway)Vertex(road.Position+new Point(0,-side),road.Id);Vertex(road.Position+new Point(0,side),road.Id);}
            else {Vertex(road.Position+new Point(-side,0),road.Id);Vertex(road.Position+new Point(side,0),road.Id);}
        }
        foreach(var road in State.Roads.Where(r=>!r.ConstructionOnly))foreach(var band in CrosswalkLocations(road,State)){
            var across=band.HorizontalRoad?new Point(0,side):new Point(side,0);var first=band.Centre-across;var second=band.Centre+across;
            if(!Walkable(first)||!Walkable(second))continue;
            var index=s.Vertices.Count;Vertex(first,road.Id,index+1);Vertex(second,road.Id,index);
        }
        s.Edges=new List<(int,double)>?[s.Vertices.Count];
        if(Pr57324Enabled)_pr57324SurfaceTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-p324start;
        return _walkSurface=s;
    }

    static IEnumerable<(int Kind,int Index)> Nearby(WalkSurface s,Point a,Point b)
    {
        var seen=new HashSet<(int,int)>();
        foreach(var cell in WalkCells(Math.Min(a.X,b.X),Math.Max(a.X,b.X),Math.Min(a.Z,b.Z),Math.Max(a.Z,b.Z)))
            if(s.Cells.TryGetValue(cell,out var list))foreach(var item in list)if(seen.Add(item))yield return item;
    }
    static bool InAsphalt(WalkSurface s,Point p)=>Nearby(s,p,p).Any(i=>i.Kind<=WalkSurface.Highway&&s.Boxes[i.Kind][i.Index].Has(p));
    // Inside asphalt, outside the paint (urban) or junction crossing (highway).
    static bool OffCrossing(WalkSurface s,Point p)
    {
        var items=Nearby(s,p,p).ToArray();
        bool In(int kind)=>items.Any(i=>i.Kind==kind&&s.Boxes[kind][i.Index].Has(p));
        return In(WalkSurface.Urban)&&!In(WalkSurface.Band)||In(WalkSurface.Highway)&&!In(WalkSurface.Zone);
    }
    // Through a building footprint; its edge, where the door is, stays open.
    static bool Blocked(WalkSurface s,Point a,Point b)
    {
        var lots=WalkCells(Math.Min(a.X,b.X),Math.Max(a.X,b.X),Math.Min(a.Z,b.Z),Math.Max(a.Z,b.Z)).SelectMany(cell=>s.Lots.GetValueOrDefault(cell)??[]).Distinct().ToArray();
        if(lots.Length==0)return false;
        var samples=Math.Max(1,(int)Math.Ceiling(a.Distance(b)/.25));
        for(var k=0;k<=samples;k++){var p=a+(b-a)*(k/(double)samples);if(lots.Any(lot=>Inside(lot,p,-.05)))return true;}
        return false;
    }

    // Metres of a→b over asphalt outside a crossing. While recovering, a walk
    // that starts inside a lane (old saves, entry spawn) is exempt until it
    // first leaves that region; the rest of the walk is checked normally.
    static double UnpaintedLength(WalkSurface s,Point a,Point b,ref bool recovering)
    {
        var d=b-a;var length=d.Distance(new(0,0));if(length<1e-9)return 0;
        var spans=new List<(double,double)>[4];for(var k=0;k<4;k++)spans[k]=new();
        foreach(var (kind,index) in Nearby(s,a,b)){
            var box=s.Boxes[kind][index];
            // Exact bounds keep adjoining cells continuous; a walk along an edge
            // (a curb line) is outside, as in Box.Has.
            (double,double)? Slab(double start,double delta,double low,double high)=>Math.Abs(delta)<1e-12?(start>low+1e-6&&start<high-1e-6?(double.NegativeInfinity,double.PositiveInfinity):null):(Math.Min((low-start)/delta,(high-start)/delta),Math.Max((low-start)/delta,(high-start)/delta));
            if(Slab(a.X,d.X,box.X0,box.X1) is not {} x||Slab(a.Z,d.Z,box.Z0,box.Z1) is not {} z)continue;
            var t0=Math.Max(0,Math.Max(x.Item1,z.Item1));var t1=Math.Min(1,Math.Min(x.Item2,z.Item2));if(t1>t0)spans[kind].Add((t0,t1));
        }
        var bad=Union(Subtract(Union(spans[WalkSurface.Urban]),Union(spans[WalkSurface.Band])).Concat(Subtract(Union(spans[WalkSurface.Highway]),Union(spans[WalkSurface.Zone]))).ToList());
        if(recovering){if(bad.Count>0&&bad[0].Item1<=1e-9){if(bad[0].Item2>=1-1e-9)return 0;bad.RemoveAt(0);}recovering=false;}
        return bad.Sum(span=>span.Item2-span.Item1)*length;
    }
    static List<(double,double)> Union(List<(double,double)> spans)
    {
        var result=new List<(double,double)>();
        foreach(var span in spans.OrderBy(s=>s.Item1)){if(result.Count>0&&span.Item1<=result[^1].Item2+1e-9)result[^1]=(result[^1].Item1,Math.Max(result[^1].Item2,span.Item2));else result.Add(span);}
        return result;
    }
    static List<(double,double)> Subtract(List<(double,double)> spans,List<(double,double)> cuts)
    {
        var result=new List<(double,double)>();
        foreach(var span in spans){
            var start=span.Item1;
            foreach(var cut in cuts){if(cut.Item2<=start||cut.Item1>=span.Item2)continue;if(cut.Item1>start)result.Add((start,cut.Item1));start=Math.Max(start,cut.Item2);}
            if(start<span.Item2)result.Add((start,span.Item2));
        }
        return result;
    }

    bool LegalWalk(IReadOnlyList<Point> route)
    {
        var s=Surface();var recovering=OffCrossing(s,route[0]);
        for(var i=1;i<route.Count;i++)if(UnpaintedLength(s,route[i-1],route[i],ref recovering)>UnpaintedTolerance)return false;
        return true;
    }

    // Shortest walk over the sidewalk graph to the door, entering it from the
    // destination's own road cell. An urban lane is left only through paint,
    // as before; only a spawn on the highway steps off it to the side. Every
    // link stays clear of building footprints, as sidewalks do.
    List<Point> SidewalkWalk(Point start,RoadNode from,Point door,RoadNode access)
    {
        var p324start=Pr57324Enabled?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        if(Pr57324Enabled)_pr57324SidewalkCalls++;
        try{
        var s=Surface();if(OffCrossing(s,door))return [];
        var offHighway=OffCrossing(s,start);if(offHighway&&Nearby(s,start,start).Any(i=>i.Kind==WalkSurface.Urban&&s.Boxes[i.Kind][i.Index].Has(start)))return [];
        bool Usable(int road)=>_roads.TryGetValue(road,out var node)&&(node.Highway||node.MaintenanceRatio>.05);
        bool Legal(Point a,Point b,bool recovering=false){if(Pr57324Enabled)_pr57324SidewalkLegalChecks++;return UnpaintedLength(s,a,b,ref recovering)<=UnpaintedTolerance&&!Blocked(s,a,b);}
        var goalLinks=new Dictionary<int,double>();
        foreach(var road in access.Neighbours.Append(access.Id))if(s.RoadVertices.TryGetValue(road,out var own))foreach(var v in own){var p=s.Vertices[v].Position;if(p.Distance(door)<=State.Tuning.RoadWidth&&Usable(road)&&Legal(p,door))goalLinks[v]=p.Distance(door);}
        if(Pr57324Enabled)_pr57324SidewalkGoalLinks+=goalLinks.Count;
        if(goalLinks.Count==0)return [];
        var count=s.Vertices.Count;var startId=count;var goalId=count+1;var across=State.Tuning.RoadWidth-1.4+.1;
        var cost=new Dictionary<int,double>{{startId,0}};var parent=new Dictionary<int,int>();var closed=new HashSet<int>();
        var queue=new PriorityQueue<int,(double,int)>(Comparer<(double,int)>.Create((x,y)=>x.Item1!=y.Item1?x.Item1.CompareTo(y.Item1):x.Item2.CompareTo(y.Item2)));
        queue.Enqueue(startId,(start.Distance(door),startId));
        IEnumerable<(int To,double Cost)> Next(int id)
        {
            if(id==startId){
                // Leave from the start road cell, as the road route does: a nearby
                // but separate street is not a shortcut the network lacks.
                foreach(var road in from.Neighbours.Append(from.Id))if(s.RoadVertices.TryGetValue(road,out var own))foreach(var v in own){var p=s.Vertices[v].Position;if(p.Distance(start)<=WalkLink&&Legal(start,p,offHighway))yield return (v,p.Distance(start));}
                yield break;
            }
            if(s.Edges[id]==null){
                if(Pr57324Enabled)_pr57324SidewalkEdgesBuilt++;
                var (position,road,partner)=s.Vertices[id];var edges=new List<(int,double)>();
                // Across its own cell a walk may reach the opposite curb: legal only
                // through paint, or on the highway where an urban road meets it.
                foreach(var other in _roads[road].Neighbours.Append(road))if(s.RoadVertices.TryGetValue(other,out var own))foreach(var v in own){if(v==id)continue;var p=s.Vertices[v].Position;if((p.Distance(position)<=(other==road?across:WalkLink)||v==partner)&&Legal(position,p))edges.Add((v,p.Distance(position)));}
                s.Edges[id]=edges;
            }
            foreach(var edge in s.Edges[id]!)yield return edge;
            if(goalLinks.TryGetValue(id,out var last))yield return (goalId,last);
        }
        while(queue.TryDequeue(out var id,out _)){
            if(Pr57324Enabled)_pr57324SidewalkPops++;
            if(id==goalId)break;if(!closed.Add(id))continue;
            if(Pr57324Enabled)_pr57324SidewalkExpanded++;
            foreach(var (to,step) in Next(id)){
                if(to<count&&!Usable(s.Vertices[to].Road))continue;
                var total=cost[id]+step;if(cost.TryGetValue(to,out var known)&&known<=total)continue;
                cost[to]=total;parent[to]=id;queue.Enqueue(to,(total+(to==goalId?0:s.Vertices[to].Position.Distance(door)),to));
            }
        }
        if(!parent.ContainsKey(goalId))return [];
        var points=new List<Point>{door};for(var id=parent[goalId];id!=startId;id=parent[id])points.Add(s.Vertices[id].Position);points.Add(start);points.Reverse();
        // A corner vertex sits beside the next sidewalk sample; skip such short
        // zig-zags when the direct step is legal and clear of buildings. The
        // bound keeps every shortcut on the same sidewalk.
        var smooth=new List<Point>{points[0]};
        for(var i=1;i<points.Count-1;i++)if(smooth[^1].Distance(points[i+1])>WalkLink||!Legal(smooth[^1],points[i+1]))smooth.Add(points[i]);
        smooth.Add(points[^1]);points=smooth;
        // Keep only turning points; collinear sidewalk samples add nothing.
        var result=new List<Point>{points[0]};
        for(var i=1;i<points.Count;i++){
            if(points[i].Distance(result[^1])<.001)continue;
            if(result.Count>1){var a=result[^1]-result[^2];var b=points[i]-result[^1];if(Math.Abs(a.X*b.Z-a.Z*b.X)<1e-6&&a.X*b.X+a.Z*b.Z>0){result[^1]=points[i];continue;}}
            result.Add(points[i]);
        }
        return result;
        }finally{if(Pr57324Enabled)_pr57324SidewalkTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-p324start;}
    }
}
