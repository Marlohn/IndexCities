namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    bool StraightRoad(RoadNode node, out bool horizontal)
    {
        horizontal=node.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-node.Position.X)>.1);
        var vertical=node.Neighbours.Any(id=>Math.Abs(_roads[id].Position.Z-node.Position.Z)>.1);
        return !node.Highway && horizontal!=vertical;
    }

    bool SameRoadLine(RoadNode from,RoadNode to,bool horizontal)
    {
        if(Math.Abs(horizontal?from.Position.Z-to.Position.Z:from.Position.X-to.Position.X)>.01)return false;
        var route=FindRoadRoute(from.Id,to.Id);
        return route.Count>0 && route.All(id=>!_roads[id].Highway && Math.Abs(horizontal?_roads[id].Position.Z-from.Position.Z:_roads[id].Position.X-from.Position.X)<.01);
    }
    // SameRoadLine(from,to,horizontal) after its coordinate check, for any `to`,
    // from one breadth-first visit resumed only as far as each query needs. The
    // visit assigns FindRoadRoute's parents (stopping early never changes one),
    // and a node qualifies when it and every node before it on that route are
    // city roads on from's line: once no qualifying node is left to expand, no
    // undiscovered node can qualify.
    sealed class RoadLine
    {
        readonly CitySimulation _sim;readonly RoadNode _from;readonly bool _horizontal;
        readonly Dictionary<int,bool> _onRoute=new();readonly Queue<int> _queue=new();int _queuedOnRoute;
        public RoadLine(CitySimulation sim,RoadNode from,bool horizontal)
        {
            _sim=sim;_from=from;_horizontal=horizontal;
            if(sim._roads.TryGetValue(from.Id,out var start)&&!start.ConstructionOnly)Discover(from.Id,OnLine(start));
        }
        bool OnLine(RoadNode n)=>!n.Highway&&Math.Abs(_horizontal?n.Position.Z-_from.Position.Z:n.Position.X-_from.Position.X)<.01;
        void Discover(int id,bool valid){_onRoute[id]=valid;_queue.Enqueue(id);if(valid)_queuedOnRoute++;}
        public bool Contains(int to)
        {
            var roads=_sim._roads;
            while(!_onRoute.ContainsKey(to)&&_queuedOnRoute>0){
                var id=_queue.Dequeue();var valid=_onRoute[id];if(valid)_queuedOnRoute--;
                foreach(var next in roads[id].Neighbours){if(_onRoute.ContainsKey(next)||roads[next].MaintenanceRatio<=.05||roads[next].ConstructionOnly)continue;Discover(next,valid&&OnLine(roads[next]));}
            }
            return _onRoute.TryGetValue(to,out var result)&&result;
        }
    }

    void UpdateCrosswalks()
    {
        _roadEpoch++;
        var marked=State.Roads.Where(n=>!n.Highway&&(n.Neighbours.Any(id=>_roads[id].Highway)||n.Neighbours.Count>=3)&&CrosswalkGeometry(n,State).Any()).Select(n=>n.Id).ToHashSet();
        // A curve alone is not a crossing. Accesses on the same straight run
        // share a physical crossing; all accesses are considered in a stable order.
        foreach(var access in State.Buildings.Where(b=>!b.Removed).Select(Access).OfType<RoadNode>().DistinctBy(n=>n.Id).OrderBy(n=>n.Position.Z).ThenBy(n=>n.Position.X)) {
            var node=access;
            if(!StraightRoad(node,out var horizontal)) {
                if(marked.Contains(node.Id))continue;
                node=node.Neighbours.Select(id=>_roads[id]).Where(n=>StraightRoad(n,out _)).MinBy(n=>n.Position.Distance(access.Position));
                if(node==null||!StraightRoad(node,out horizontal))continue;
            }
            if(!CrosswalkGeometry(node,State).Any())continue;
            if(marked.Any(id=>_roads[id].Position.Distance(node.Position)<=State.Tuning.CrosswalkSpacing&&SameRoadLine(node,_roads[id],horizontal)))continue;
            marked.Add(node.Id);
        }
        var changed=false;
        foreach(var node in State.Roads){var value=marked.Contains(node.Id)&&CrosswalkGeometry(node,State).Any();if(node.Crosswalk!=value){node.Crosswalk=value;changed=true;}}
        if(changed)State.RoadRevision++;
    }

    // Both paint and pedestrian paths use these actual geometric crossing bands.
    public static IEnumerable<(Point Centre,bool HorizontalRoad)> CrosswalkLocations(RoadNode node,CityState state)
    {
        if(!node.Crosswalk||node.Highway)yield break;
        foreach(var band in CrosswalkGeometry(node,state))yield return band;
    }

    // CrosswalkLocations(node,State), kept only while Advance runs: there the
    // inputs (road list, neighbours, crossings) change only through code that
    // moves the road epoch. Outside it, or across calls, always recomputed.
    readonly Dictionary<RoadNode,(Point Centre,bool HorizontalRoad)[]> _crosswalkBands=new(ReferenceEqualityComparer.Instance);
    bool _crosswalkBandsActive;int _crosswalkBandsEpoch;
    IEnumerable<(Point Centre,bool HorizontalRoad)> CrosswalkBands(RoadNode node)
    {
        if(!_crosswalkBandsActive)return CrosswalkLocations(node,State);
        if(_crosswalkBandsEpoch!=_roadEpoch){_crosswalkBands.Clear();_crosswalkBandsEpoch=_roadEpoch;}
        if(!_crosswalkBands.TryGetValue(node,out var bands))_crosswalkBands[node]=bands=CrosswalkLocations(node,State).ToArray();
        return bands;
    }
    static IEnumerable<(Point Centre,bool HorizontalRoad)> CrosswalkGeometry(RoadNode node,CityState state)
    {
        if(node.Highway)yield break;
        var neighbours=state.Roads.Where(r=>node.Neighbours.Contains(r.Id)).ToArray();
        var horizontal=neighbours.Any(r=>Math.Abs(r.Position.X-node.Position.X)>.1);
        var vertical=neighbours.Any(r=>Math.Abs(r.Position.Z-node.Position.Z)>.1);
        if(horizontal&&vertical) {
            foreach(var neighbour in neighbours) {
                var delta=neighbour.Position-node.Position;var alongX=Math.Abs(delta.X)>.1;
                var distance=state.Tuning.RoadWidth/2-.7;
                var centre=node.Position+(alongX?new Point(Math.Sign(delta.X)*distance,0):new Point(0,Math.Sign(delta.Z)*distance));
                if(!CrosswalkOverlapsHighway(centre,alongX,state))yield return (centre,alongX);
            }
        } else if(horizontal||vertical){var centre=CrosswalkCentre(node,state);if(!CrosswalkOverlapsHighway(centre,horizontal,state))yield return(centre,horizontal);}
    }

    // Five .7 m stripes at .8 m centres, across the V1 carriageway. An
    // entrance may bend beside the highway; paint never extends onto its lanes.
    static bool CrosswalkOverlapsHighway(Point centre,bool horizontal,CityState state)
    {
        var laneHalf=state.Tuning.RoadWidth*(6.5/10.5)/2;
        var halfX=horizontal?1.95:laneHalf;var halfZ=horizontal?laneHalf:1.95;
        return state.Roads.Any(r=>r.Highway&&Math.Abs(centre.X-r.Position.X)<halfX+state.Tuning.RoadStep/2&&Math.Abs(centre.Z-r.Position.Z)<halfZ+laneHalf);
    }

    (RoadNode Node,Point Centre)? SharedCrossing(RoadNode node,bool horizontal,Point start,Point finish)
    {
        // Bands on this road line, nearest first: distance, then Id, then road
        // and band order (the stable sort's order). The first whose road passes
        // SameRoadLine wins, so the route visit only goes as far as it needs.
        var candidates=new List<(RoadNode Node,Point Centre,double Distance,int Order)>();
        foreach(var r in State.Roads){
            if(!r.Crosswalk||r.Highway||Math.Abs(horizontal?node.Position.Z-r.Position.Z:node.Position.X-r.Position.X)>.01)continue;
            foreach(var p in CrosswalkBands(r))if(p.HorizontalRoad==horizontal&&Math.Abs(horizontal?p.Centre.Z-node.Position.Z:p.Centre.X-node.Position.X)<.01)candidates.Add((r,p.Centre,p.Centre.Distance(start)+p.Centre.Distance(finish),candidates.Count));
        }
        candidates.Sort(static (a,b)=>a.Distance.CompareTo(b.Distance) is var d and not 0?d:a.Node.Id.CompareTo(b.Node.Id) is var i and not 0?i:a.Order.CompareTo(b.Order));
        RoadLine? line=null;
        foreach(var candidate in candidates)if((line??=new RoadLine(this,node,horizontal)).Contains(candidate.Node.Id))return (candidate.Node,candidate.Centre);
        return null;
    }
}
