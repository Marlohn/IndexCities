namespace IndexCities.Simulation;

// SPEC transporte-circulacao (cruzamentos, capacidade física de carga/descarga):
// a vehicle waiting for junction admission stands with its body outside the
// crossing carriageway, and clear of the turn of whoever already holds that
// junction, so a turn through the junction does not sweep into it.
// A delivery whose bay lies just beyond a junction, while another truck still
// uses that bay, waits before presenting itself at the junction when no lane
// position between the junction and the bay fits its own body clear of the
// crossing carriageway, of the occupant's way out and of bodies already there.
// It is then not yet an approach of that junction: preference, signals and
// claims are unchanged, and it never stops on the occupant's only exit.
public sealed partial class CitySimulation
{
    const double CarriagewayHalfWidth=3.3;

    // How far before the lane point of `node` (next to a junction) v waits, so
    // that its front stays out of the crossing carriageway (0 if the node
    // already leaves that room, or the approach is not straight).
    double StopLineBefore(Vehicle v,RoadNode node,RoadNode junction,Point incoming,double length)
    {
        var onward=junction.Position-node.Position;var gap=onward.Distance(default);
        if(gap<=.01||length<=.01||Math.Abs(onward.X*incoming.Z-onward.Z*incoming.X)>=.01*gap*length||onward.X*incoming.X+onward.Z*incoming.Z<=0)return 0;
        return Math.Max(0,CarriagewayHalfWidth+BodyHalfLength(v)-gap);
    }

    // The poses o passes through along `route` from index `from`, as the lane
    // model drives it (turn in place toward each lane point, then straight),
    // until its body is past `junction`.
    void CrossingPoses(List<(Vehicle Body,Point Position,double Heading)> held,Vehicle o,List<int> route,int from,Point at,double facing,RoadNode junction)
    {
        var crossing=route.IndexOf(junction.Id);if(crossing<0||crossing<from)return;
        held.Add((o,at,facing));
        for(var i=from;i<route.Count;i++){
            if(!_roads.TryGetValue(route[i],out var road))break;
            var edge=i>0?road.Position-_roads[route[i-1]].Position:route.Count>1&&_roads.TryGetValue(route[1],out var second)?second.Position-road.Position:default;
            var metres=edge.Distance(default);if(metres<.01)continue;
            var lane=road.Position+new Point(edge.Z/metres,-edge.X/metres)*1.65;
            var direction=Math.Atan2(lane.X-at.X,lane.Z-at.Z);var turn=Math.IEEERemainder(direction-facing,2*Math.PI);
            for(var part=0;part<=8;part++)held.Add((o,at,facing+turn*part/8));
            var run=new List<(Point Position,double Heading)>();SamplePoses(run,at,lane,direction);held.AddRange(run.Select(p=>(o,p.Position,p.Heading)));
            at=lane;facing=direction;
            if(i>crossing&&at.Distance(junction.Position)>JunctionClearance)break;
        }
    }

    // Remaining crossing of the vehicle that already holds this junction.
    List<(Vehicle Body,Point Position,double Heading)> ClaimHolderPoses(Vehicle v,RoadNode junction)
    {
        var held=new List<(Vehicle Body,Point Position,double Heading)>();
        foreach(var o in State.Vehicles)
            if(o.Id!=v.Id&&o.IntersectionClaim==junction.Id&&o.Status==VehicleStatus.Travelling&&o.RouteIndex<o.Route.Count)CrossingPoses(held,o,o.Route,o.RouteIndex,o.Position,o.Heading,junction);
        return held;
    }

    // Poses through which a truck using v's bay leaves across Route[junctionIndex].
    // Empty when the bay is not in use that way.
    List<(Vehicle Body,Point Position,double Heading)> BayExitPoses(Vehicle v,int junctionIndex)
    {
        var held=new List<(Vehicle Body,Point Position,double Heading)>();
        if(v.PassengerCar||v.Machine!=PavingMachine.None||v.ShipmentId==null||v.DockPosition is not {} dock||junctionIndex>=v.Route.Count-1||
            !_roads.TryGetValue(v.Route[^1],out var access)||!_roads.TryGetValue(v.Route[junctionIndex],out var junction))return held;
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.PassengerCar||o.Machine!=PavingMachine.None)continue;
            if(o.DockPosition is {} bay&&bay.Distance(dock)<.1&&o.Status is VehicleStatus.Unloading or VehicleStatus.Travelling or VehicleStatus.Waiting&&
                (o.Status!=VehicleStatus.Travelling||o.RouteIndex>=o.Route.Count||o.Route.IndexOf(junction.Id) is var k&&k>=0&&(o.RouteIndex>k||o.IntersectionClaim==junction.Id))){
                // Unloading at the bay, or already on its way in past the junction.
                var home=o.External?EntryNode():BuildingById(o.HomeBuildingId) is {} source?Access(source):null;if(home==null)continue;
                CrossingPoses(held,o,FindRoadRoute(access.Id,home.Id,ConstructionTrip(o)),0,dock,o.Heading,junction);
            }else if(o.Status==VehicleStatus.Travelling&&o.ShipmentId==null&&o.DockPosition==null&&o.Route.Count>1&&o.Route[0]==access.Id)
                CrossingPoses(held,o,o.Route,o.RouteIndex,o.Position,o.Heading,junction);
        }
        return held;
    }

    // True when v, about to present itself at Route[junctionIndex], must wait
    // before it: its bay just beyond is in use and no lane position on the way
    // there fits its body clear of the crossing, the occupant's exit and the
    // bodies already present. A longer access keeps its existing admission.
    bool HoldBeforeBayJunction(Vehicle v,int junctionIndex,out List<(Vehicle Body,Point Position,double Heading)> exits)
    {
        exits=BayExitPoses(v,junctionIndex);if(exits.Count==0)return false;
        var junction=_roads[v.Route[junctionIndex]];
        for(var i=junctionIndex+1;i<v.Route.Count;i++){
            if(!_roads.TryGetValue(v.Route[i],out var road)||!_roads.TryGetValue(v.Route[i-1],out var previous))return true;
            var edge=road.Position-previous.Position;var metres=edge.Distance(default);if(metres<.01)continue;
            var offset=new Point(edge.Z/metres,-edge.X/metres)*1.65;var heading=Math.Atan2(edge.X,edge.Z);
            var poses=new List<(Point Position,double Heading)>();SamplePoses(poses,previous.Position+offset,road.Position+offset,heading);
            foreach(var pose in poses){
                var rear=i==junctionIndex+1?(pose.Position.X-junction.Position.X)*Math.Sin(heading)+(pose.Position.Z-junction.Position.Z)*Math.Cos(heading):pose.Position.Distance(junction.Position);
                if(rear<CarriagewayHalfWidth+BodyHalfLength(v))continue;
                if(exits.Any(p=>p.Position.Distance(pose.Position)<12&&BodiesOverlap(v,pose.Position,pose.Heading,p.Body,p.Position,p.Heading)))continue;
                if(State.Vehicles.Any(o=>o.Id!=v.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(pose.Position)<12&&BodiesOverlap(v,pose.Position,pose.Heading,o,o.Position,o.Heading)))continue;
                return false;
            }
        }
        return true;
    }

    // Turning in place from its verified heading would put v's body into o's
    // (actual contact; the travel checks keep their safety margin).
    static bool TurnsInto(Vehicle v,double approachHeading,Vehicle o)=>BodiesOverlap(v,v.Position,v.Heading,o,o.Position,o.Heading,0)&&!BodiesOverlap(v,v.Position,approachHeading,o,o.Position,o.Heading,0);

    // Travel toward target that keeps v's body clear of the given poses.
    static double ClearOfPoses(Vehicle v,Point target,double distance,List<(Vehicle Body,Point Position,double Heading)> poses)
    {
        var safe=0.0;
        while(safe<distance-.000001){var next=Math.Min(distance,safe+.5);var at=v.Position.MoveTowards(target,next);
            if(poses.Any(p=>p.Position.Distance(at)<12&&BodiesOverlap(v,at,v.Heading,p.Body,p.Position,p.Heading)))break;
            safe=next;
        }
        return safe;
    }
}
