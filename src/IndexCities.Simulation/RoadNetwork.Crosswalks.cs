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
    // Every `to` passing SameRoadLine(from,to,horizontal) after its coordinate
    // check, from one breadth-first visit: the same parents as FindRoadRoute
    // (stopping early never changes one), and a node qualifies when it and
    // every node before it on that route are city roads on from's line.
    HashSet<int> RoadLineFrom(RoadNode from,bool horizontal)
    {
        var line=new HashSet<int>();if(!_roads.TryGetValue(from.Id,out var start)||start.ConstructionOnly)return line;
        bool OnLine(RoadNode n)=>!n.Highway&&Math.Abs(horizontal?n.Position.Z-from.Position.Z:n.Position.X-from.Position.X)<.01;
        var onRoute=new Dictionary<int,bool>{{from.Id,OnLine(start)}};var queue=new Queue<int>();queue.Enqueue(from.Id);
        while(queue.Count>0){var id=queue.Dequeue();foreach(var next in _roads[id].Neighbours){if(onRoute.ContainsKey(next)||_roads[next].MaintenanceRatio<=.05||_roads[next].ConstructionOnly)continue;onRoute[next]=onRoute[id]&&OnLine(_roads[next]);queue.Enqueue(next);}}
        foreach(var (id,valid) in onRoute)if(valid)line.Add(id);
        return line;
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
        // SameRoadLine(node,r,horizontal), with one route visit for all r.
        HashSet<int>? line=null;
        return State.Roads.Where(r=>r.Crosswalk&&!r.Highway&&!(Math.Abs(horizontal?node.Position.Z-r.Position.Z:node.Position.X-r.Position.X)>.01)&&(line??=RoadLineFrom(node,horizontal)).Contains(r.Id))
            .SelectMany(r=>CrosswalkLocations(r,State).Where(p=>p.HorizontalRoad==horizontal&&Math.Abs(horizontal?p.Centre.Z-node.Position.Z:p.Centre.X-node.Position.X)<.01).Select(p=>(Node:r,Centre:p.Centre)))
            .OrderBy(p=>p.Centre.Distance(start)+p.Centre.Distance(finish)).ThenBy(p=>p.Node.Id)
            .Select(p=>((RoadNode Node,Point Centre)?)p).FirstOrDefault();
    }
}
