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
    // Keyed by the waiting body's dimensions: a car, a truck and each machine stop apart.
    readonly Dictionary<(int Junction,int Approach,double HalfLength,double HalfWidth),double> _stopDistances=[];
    int _stopDistancesEpoch=-1;

    bool NextSharedOnApproach(Vehicle v,RoadNode node,Point incoming,double length,out RoadNode junction,out int index)
    {
        junction=null!;index=-1;if(length<=.01)return false;
        for(var i=v.RouteIndex+1;i<v.Route.Count;i++){
            var next=_roads[v.Route[i]];var delta=next.Position-node.Position;var gap=delta.Distance(default);
            if(gap<=.01||Math.Abs(delta.X*incoming.Z-delta.Z*incoming.X)>=.01*gap*length||delta.X*incoming.X+delta.Z*incoming.Z<=0)return false;
            if(next.Neighbours.Count>=3||TurnsAtBend(v,i)){junction=next;index=i;return true;}
            if(gap>2*(CarriagewayHalfWidth+BodyHalfLength(v)+BodyHalfWidth(v)))return false;
        }
        return false;
    }

    // How far before the lane point of `node` (next to a junction) v waits, so
    // that its front stays out of the crossing carriageway (0 if the node
    // already leaves that room, or the approach is not straight).
    double StopLineBefore(Vehicle v,RoadNode node,RoadNode junction,Point incoming,double length)
    {
        var onward=junction.Position-node.Position;var gap=onward.Distance(default);
        if(gap<=.01||length<=.01||Math.Abs(onward.X*incoming.Z-onward.Z*incoming.X)>=.01*gap*length||onward.X*incoming.X+onward.Z*incoming.Z<=0)return 0;
        if(_stopDistancesEpoch!=_roadEpoch){_stopDistances.Clear();_stopDistancesEpoch=_roadEpoch;}
        var key=(junction.Id,node.Id,BodyHalfLength(v),BodyHalfWidth(v));
        if(!_stopDistances.TryGetValue(key,out var distance)){
            distance=CarriagewayHalfWidth+BodyHalfLength(v);
            var forward=incoming*(1/length);var lane=junction.Position+new Point(forward.Z,-forward.X)*1.65;
            // The crossing's turns use the same lane points and body dimensions as
            // actual motion. Their diagonal sweep can extend past the carriageway
            // edge. Keep this waiting body's entire rectangle outside that sweep.
            var truck=new Vehicle();
            foreach(var fromId in junction.Neighbours.Where(id=>id!=node.Id))foreach(var toId in junction.Neighbours.Where(id=>id!=fromId)){
                var from=_roads[fromId];var to=_roads[toId];var direction=junction.Position-from.Position;var metres=direction.Distance(default);if(metres<=.01)continue;
                var heading=Math.Atan2(direction.X,direction.Z);
                var start=from.Position+new Point(direction.Z/metres,-direction.X/metres)*1.65;
                var route=new List<int>{fromId,junction.Id,toId};
                var continuation=to.Neighbours.FirstOrDefault(id=>id!=junction.Id);if(continuation!=0)route.Add(continuation);
                var poses=new List<(Vehicle Body,Point Position,double Heading)>();CrossingPoses(poses,truck,route,1,start,heading,junction);
                foreach(var pose in poses)distance=Math.Max(distance,LastOverlapOnApproach(v,lane,forward,truck,pose.Position,pose.Heading));
            }
            _stopDistances[key]=distance;
        }
        return Math.Max(0,distance-gap);
    }

    // The stop line of the next shared node also applies while v still holds the
    // claim of the junction it has just passed, once its waiting place lies past
    // that junction (where the claim is released): it never waits inside it.
    bool WaitsClearOfPassedClaim(Vehicle v,RoadNode node,RoadNode junction,Point incoming,double length,Point target,double distance)
    {
        if(v.IntersectionClaim is not {} held||!_roads.TryGetValue(held,out var passed))return true;
        var wait=v.Position.MoveTowards(target,Math.Max(0,distance-StopLineBefore(v,node,junction,incoming,length)));
        return wait.Distance(passed.Position)>JunctionClearance;
    }

    // End of the exact SAT overlap interval for a waiting body moved backwards
    // along its lane. The existing 0.1 m body margin applies once, on every axis.
    static double LastOverlapOnApproach(Vehicle waiting,Point lane,Point forward,Vehicle turning,Point at,double heading)
    {
        static double Dot(Point a,Point b)=>a.X*b.X+a.Z*b.Z;
        var right=new Point(forward.Z,-forward.X);var turn=new Point(Math.Sin(heading),Math.Cos(heading));var turnRight=new Point(turn.Z,-turn.X);var delta=at-lane;
        var low=double.NegativeInfinity;var high=double.PositiveInfinity;
        foreach(var axis in new[]{forward,right,turn,turnRight}){
            var extent=BodyHalfLength(waiting)*Math.Abs(Dot(forward,axis))+BodyHalfWidth(waiting)*Math.Abs(Dot(right,axis))+BodyHalfLength(turning)*Math.Abs(Dot(turn,axis))+BodyHalfWidth(turning)*Math.Abs(Dot(turnRight,axis))+.1;
            var slope=Dot(forward,axis);var offset=Dot(delta,axis);
            if(Math.Abs(slope)<1e-9){if(Math.Abs(offset)>=extent)return 0;continue;}
            var a=(-extent-offset)/slope;var b=(extent-offset)/slope;low=Math.Max(low,Math.Min(a,b));high=Math.Min(high,Math.Max(a,b));if(low>=high)return 0;
        }
        return high>0?high:0;
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
            var lane=road.Position+new Point(edge.Z/metres,-edge.X/metres)*1.65+(i>0?InnerCornerOffset(route,i,road,edge,metres):default);
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
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||!Holds(o,junction.Id))continue;
            // A holder leaving a bay or on a works-site path: its admitted manoeuvre.
            if(Leaving(o))held.AddRange(WayOut(o,out _));
            else if(o.Status==VehicleStatus.Travelling&&o.RouteIndex<o.Route.Count)CrossingPoses(held,o,o.Route,o.RouteIndex,o.Position,o.Heading,junction);
        }
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
            }else if(o.Status==VehicleStatus.Travelling&&o.ShipmentId==null&&(o.DockPosition==null||!o.External&&o.DockPosition is {} home&&home.Distance(dock)>=.1)&&o.Route.Count>1&&o.Route[0]==access.Id)
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

public sealed partial class CitySimulation
{
    // A right turn pivots where its incoming and outgoing lane lines meet,
    // before the node, instead of running on to the crossing road's centre
    // line and cutting back across it diagonally. Left turns keep their point.
    Point InnerCornerOffset(List<int> route,int index,RoadNode node,Point incoming,double length)
    {
        if(node.Neighbours.Count>=3||node.Crosswalk||index<1||index+1>=route.Count||length<=.01||!_roads.TryGetValue(route[index+1],out var next))return default;
        var outgoing=next.Position-node.Position;var metres=outgoing.Distance(default);if(metres<=.01)return default;
        var cross=incoming.X*outgoing.Z-incoming.Z*outgoing.X;
        // Square turn to the right of travel (the lane side): out ≈ (in.Z,-in.X).
        if(Math.Abs(cross)<.99*length*metres||outgoing.X*incoming.Z-outgoing.Z*incoming.X<=0)return default;
        return new Point(outgoing.Z/metres*1.65,-outgoing.X/metres*1.65);
    }
}
