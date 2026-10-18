namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    const double JunctionClearance=3.3+3.6;
    readonly Dictionary<int,HashSet<int>> _junctionAreas=new();
    void RebuildJunctionAreas()
    {
        _junctionAreas.Clear();foreach(var junction in State.Roads.Where(r=>r.Neighbours.Count>=3)){
            var area=NearbyJunctions(junction.Id).Append(junction.Id).ToHashSet();if(area.Count>1)_junctionAreas[junction.Id]=area;
        }
    }
    IEnumerable<int> NearbyJunctions(int start)
    {
        // Reservation footprints can overlap across intermediate road samples.
        // Follow the road rather than joining nearby, disconnected parallel streets.
        var distances=new Dictionary<int,double>{{start,0}};var queue=new PriorityQueue<int,double>();queue.Enqueue(start,0);
        while(queue.TryDequeue(out var id,out var distance)){
            if(distance>distances[id])continue;if(id!=start&&_roads[id].Neighbours.Count>=3)yield return id;
            foreach(var next in _roads[id].Neighbours){var total=distance+_roads[id].Position.Distance(_roads[next].Position);if(total>JunctionClearance*2||distances.TryGetValue(next,out var previous)&&previous<=total)continue;distances[next]=total;queue.Enqueue(next,total);}
        }
    }
    void ClearPassedJunction(Vehicle vehicle)
    {
        // A heavy tow keeps its claim until the truck trailing it has also passed.
        var trailing=vehicle.Body==TruckBody.TowHeavy&&vehicle.TowPhase is TowPhase.Delivering or TowPhase.Unloading;
        if(vehicle.IntersectionClaim is {} held&&(!_roads.TryGetValue(held,out var road)||vehicle.Position.Distance(road.Position)>JunctionClearance&&(!trailing||(vehicle.Position-Forward(vehicle.Heading)*HeavyTowTrail).Distance(road.Position)>JunctionClearance)))vehicle.IntersectionClaim=null;
        if(vehicle.IntersectionClaim==null)vehicle.IntersectionEnteredAt=null;
        else if(_junctionAreas.ContainsKey(vehicle.IntersectionClaim.Value))vehicle.IntersectionEnteredAt??=vehicle.WaitingSince>0?vehicle.WaitingSince:VehicleMovementTime;
    }
    Vehicle? NearbyAreaOwner(HashSet<int> area,RoadNode requested,Vehicle requester)
    {
        // A chain of adjacent junctions can be long. Only bodies close to the
        // requested crossing reserve this local pavement, not the entire chain.
        bool FollowingBehind(Vehicle other){
            // An admitted car must finish clearing its own crossing. A follower
            // queued behind it can hold the previous crossing, but cannot make
            // the leader yield backwards and deadlock the same physical lane.
            if(requester.IntersectionClaim!=requested.Id||other.IntersectionClaim==requested.Id||requester.Position.Distance(requested.Position)>JunctionClearance||Math.Cos(other.Heading-requester.Heading)<.999999)return false;
            var relative=other.Position-requester.Position;
            var forward=relative.X*Math.Sin(requester.Heading)+relative.Z*Math.Cos(requester.Heading);
            var lateral=Math.Abs(relative.X*Math.Cos(requester.Heading)-relative.Z*Math.Sin(requester.Heading));
            return forward<-.1&&lateral<2.6;
        }
        return State.Vehicles.Where(v=>v.IntersectionClaim is {} claim&&area.Contains(claim)&&v.Position.Distance(requested.Position)<9&&!FollowingBehind(v))
            .OrderBy(v=>v.IntersectionEnteredAt??(v.WaitingSince>0?v.WaitingSince:VehicleMovementTime)).ThenBy(v=>v.Id).FirstOrDefault();
    }
    void ReserveJunction(Vehicle vehicle,RoadNode junction,HashSet<int>? area)
    {
        var continuing=area!=null&&vehicle.IntersectionClaim is {} old&&area.Contains(old);
        if(area==null)vehicle.IntersectionEnteredAt=null;else if(!continuing)vehicle.IntersectionEnteredAt=VehicleMovementTime;
        else vehicle.IntersectionEnteredAt??=vehicle.WaitingSince>0?vehicle.WaitingSince:VehicleMovementTime;
        vehicle.IntersectionClaim=junction.Id;
    }
}
