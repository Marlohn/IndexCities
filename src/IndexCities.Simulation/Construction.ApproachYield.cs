namespace IndexCities.Simulation;

// The asphalt truck's approach to the receiving paver runs off the lane model,
// often over the public street next to the paver's bay. Lane traffic respects
// that truck's body and the rest of its path, as it does a moving machine. A
// truck that finds lane traffic already on the rest of its approach backs out
// the way it came to the end of its lane and waits there for the street to
// clear, instead of both waiting head-on for each other.
public sealed partial class CitySimulation
{
    static bool OnSitePath(Vehicle v)=>v.Machine==PavingMachine.None&&v.Status==VehicleStatus.Travelling&&(v.SitePathBeforeRoad?v.SitePath.Count>0:v.Route.Count>0&&v.RouteIndex>=v.Route.Count&&v.SitePathIndex<v.SitePath.Count);
    static bool OnAsphaltApproach(Vehicle v)=>OnSitePath(v)&&!v.SitePathBeforeRoad;
    static bool LaneTraffic(Vehicle v)=>v.Machine==PavingMachine.None&&v.Status is VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded&&v.Route.Count>0&&v.RouteIndex<v.Route.Count&&!OnSitePath(v);

    // The asphalt truck unloading at the receiving paver's bay occupies the street
    // with its present body only; its future path is not held.
    bool UnloadingAsphaltBody(Vehicle v)=>v.Machine==PavingMachine.None&&v.Status==VehicleStatus.Unloading&&v.ShipmentId is {} id&&State.Shipments.Any(s=>s.Id==id&&s.ReceivingMachineId!=null);
    // Bodies lane traffic never drives or turns into: a parked machine, an
    // asphalt truck on its site path (also where its hold stops at a body
    // already on it, or at its lane end) or one unloading at the paver bay.
    bool StreetBody(Vehicle o)=>o.Machine!=PavingMachine.None&&o.Status==VehicleStatus.Parked||OnSitePath(o)||UnloadingAsphaltBody(o);

    bool LaneEnd(Vehicle v,out Point at,out double heading)
    {
        at=default;heading=v.Heading;
        if(v.Route.Count<2||!_roads.TryGetValue(v.Route[^1],out var node)||!_roads.TryGetValue(v.Route[^2],out var previous))return false;
        var delta=node.Position-previous.Position;var length=delta.Distance(default);if(length<=.01)return false;
        at=node.Position+new Point(delta.Z/length*1.65,-delta.X/length*1.65);heading=Math.Atan2(delta.X,delta.Z);return true;
    }

    // Start of site segment i: the previous point, or the lane end for the first.
    Point SegmentStart(Vehicle v,int index,Point laneEnd)=>index>0?v.SitePath[index-1]:laneEnd;
    double SegmentHeading(Vehicle v,int index,Point from)
    {
        var to=v.SitePath[index];
        return v.SiteDockHeading is {} dock&&index==v.SitePath.Count-1?dock:to.Distance(from)>.001?Math.Atan2(to.X-from.X,to.Z-from.Z):v.Heading;
    }

    static void SamplePoses(List<(Point Position,double Heading)> poses,Point from,Point to,double heading)
    {
        var length=from.Distance(to);if(length<=.001)return;
        for(var along=0.0;;along=Math.Min(length,along+.5)){poses.Add((from.MoveTowards(to,along),heading));if(along>=length)break;}
    }

    List<(Point Position,double Heading)> RemainingSitePoses(Vehicle v)
    {
        var poses=new List<(Point,double)>();var at=v.Position;
        for(var index=v.SitePathIndex;index<v.SitePath.Count;index++){
            var target=v.SitePath[index];
            SamplePoses(poses,at,target,v.SiteDockHeading is {} dock&&index==v.SitePath.Count-1?dock:Math.Atan2(target.X-at.X,target.Z-at.Z));at=target;
        }
        return poses;
    }

    List<(Point Position,double Heading)> BackingPoses(Vehicle v,Point laneEnd,double laneHeading)
    {
        var poses=new List<(Point,double)>();var at=v.Position;
        for(var index=v.SitePathIndex;index>=0;index--){
            var start=SegmentStart(v,index,laneEnd);SamplePoses(poses,at,start,SegmentHeading(v,index,start));at=start;
        }
        poses.Add((laneEnd,laneHeading));
        return poses;
    }

    bool ApproachObstructed(Vehicle v)
    {
        var poses=RemainingSitePoses(v);
        return State.Vehicles.Any(o=>o.Id!=v.Id&&LaneTraffic(o)&&poses.Any(pose=>pose.Position.Distance(o.Position)<12&&BodiesOverlap(v,pose.Position,pose.Heading,o,o.Position,o.Heading)));
    }

    // What a truck on its site path holds against lane traffic: the rest of
    // its path, or while yielding the way back to its lane end. Holding stops
    // at the first body already on it, as for machines; waiting at the lane
    // end it holds nothing beyond its own body.
    List<(Vehicle Machine,Point Position,double Heading)> SiteTruckReservation(Vehicle truck)
    {
        var held=new List<(Vehicle,Point,double)>();if(!OnSitePath(truck))return held;
        List<(Point Position,double Heading)> poses;
        if(OnAsphaltApproach(truck)&&ApproachObstructed(truck)){
            if(!LaneEnd(truck,out var laneEnd,out var laneHeading)||truck.SitePathIndex==0&&truck.Position.Distance(laneEnd)<=.001)return held;
            poses=BackingPoses(truck,laneEnd,laneHeading);
        }else poses=RemainingSitePoses(truck);
        foreach(var pose in poses){
            if(State.Vehicles.Any(other=>other.Id!=truck.Id&&other.Status!=VehicleStatus.Outside&&other.Position.Distance(pose.Position)<12&&BodiesOverlap(truck,pose.Position,pose.Heading,other,other.Position,other.Heading)))break;
            held.Add((truck,pose.Position,pose.Heading));
        }
        return held;
    }

    // Returns true while the truck is yielding (backing out or waiting at its
    // lane end); the approach resumes once no lane traffic stands on it.
    bool YieldAsphaltApproach(Vehicle v,ref double remaining,double speed)
    {
        if(!OnAsphaltApproach(v)||!LaneEnd(v,out var laneEnd,out var laneHeading)||!ApproachObstructed(v))return false;
        v.Cause=WorkCause.PavingSiteOccupied;
        while(remaining>.000001){
            var index=v.SitePathIndex;var start=SegmentStart(v,index,laneEnd);
            if(v.Position.Distance(start)<=.001){
                if(index>0){v.SitePathIndex--;continue;}
                // At the lane end the truck faces along its lane again, when clear.
                var previous=v.Heading;v.Heading=laneHeading;
                if(State.Vehicles.Any(o=>o.Id!=v.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(v.Position)<12&&VehicleBodiesOverlap(v,v.Position,o)))v.Heading=previous;
                return true;
            }
            var distance=v.Position.Distance(start);var travel=Math.Min(distance,Math.Min(speed*remaining,v.Fuel/State.Tuning.FuelPerMetre));
            if(travel<=.000001){v.Cause="Sem combustível para manobra no canteiro";return true;}
            var heading=v.Heading;v.Heading=SegmentHeading(v,index,start);travel=SafeSiteTravel(v,start,travel);
            if(travel<=.000001){v.Heading=heading;return true;}
            v.Position=v.Position.MoveTowards(start,travel);v.Fuel=Math.Max(0,v.Fuel-travel*State.Tuning.FuelPerMetre);v.FuelBurnedLastStep+=travel*State.Tuning.FuelPerMetre;remaining-=travel/speed;
            if(travel<distance-.000001)return true;
        }
        return true;
    }
}
