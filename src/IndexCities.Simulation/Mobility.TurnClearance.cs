namespace IndexCities.Simulation;

// SPEC transporte-circulacao/construcao: bodies never overlap and a machine
// parked at its real bay does not lock the street. Turning is modelled as a
// pivot about the body centre, so at a bend the rear of a truck stopped at the
// lane point would swing its whole half-length into a bay beside the corner
// although the truck moves away from it. Such a vehicle keeps its verified
// pose and steers through the bend instead: it turns only as far as every
// intermediate heading is clear, then advances along that heading while its
// body stays clear, and completes the turn once the rear has cleared the bay.
public sealed partial class CitySimulation
{
    (double Heading,Point To,double Distance)? TurnClearance(Vehicle v,double approachHeading,double distance,LaneAccess lane)
    {
        var turned=v.Heading;
        if(ParkedMachineOverlap(v,v.Position,approachHeading)||!ParkedMachineOverlap(v,v.Position,turned))return null;
        var turn=Math.IEEERemainder(turned-approachHeading,2*Math.PI);
        // Part 0 drives on straight ahead; each further eighth of the turn
        // counts only if it and all before it are clear of every body.
        var widest=0;
        while(widest<7&&!BodyOverlapAt(v,v.Position,approachHeading+turn*(widest+1)/8))widest++;
        for(var part=widest;part>=0;part--){
            var heading=approachHeading+turn*part/8;
            // Only headings that still close on the lane point make progress.
            if(Math.Cos(heading-turned)<.2)continue;
            // Steering ends at the target lane line rather than past it: from
            // beyond that line the lane point lies across the body's path and
            // the turn back can sweep into whoever waits in the opposite lane.
            var direction=new Point(Math.Sin(heading),Math.Cos(heading));var reach=Math.Min(distance,1);
            var offset=lane.Side(v.Position)-lane.ToSide;var closing=lane.Side(v.Position+direction)-lane.Side(v.Position);
            if(offset*closing<0)reach=Math.Min(reach,-offset/closing);else if(Math.Abs(offset)<.01&&Math.Abs(closing)>.000001)reach=Math.Min(reach,.01/Math.Abs(closing));
            var ahead=v.Position+direction*reach;
            // Leaving the lane line, the body is checked against every body.
            v.Heading=heading;var safe=Math.Min(SafeSiteTravel(v,ahead,reach,true),SafeSiteTravel(v,ahead,reach));v.Heading=turned;
            if(safe>=.05)return(heading,v.Position.MoveTowards(ahead,safe),safe);
        }
        return null;
    }

    bool ParkedMachineOverlap(Vehicle v,Point at,double heading)=>State.Vehicles.Any(o=>o.Id!=v.Id&&o.Machine!=PavingMachine.None&&o.Status==VehicleStatus.Parked&&o.Position.Distance(at)<12&&BodiesOverlap(v,at,heading,o,o.Position,o.Heading));
    bool BodyOverlapAt(Vehicle v,Point at,double heading)=>State.Vehicles.Any(o=>o.Id!=v.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(at)<12&&BodiesOverlap(v,at,heading,o,o.Position,o.Heading));

    // At a bend without junction rules, a vehicle ahead across the lane (one
    // mid-turn) is no same-direction leader, yet its body is there. Bodies
    // stacked at the exterior staging origin are not on the map yet.
    static double Ahead(Vehicle from,Vehicle other)=>(other.Position.X-from.Position.X)*Math.Sin(from.Heading)+(other.Position.Z-from.Position.Z)*Math.Cos(from.Heading);
    double ClearOfCrossingAhead(Vehicle v,Point target,double distance)
    {
        // Junction admission already orders claim holders; they are left to it.
        // One turning out of the junction ahead still has its body across this
        // lane before this vehicle reaches admission, so it counts here.
        if(v.IntersectionClaim!=null)return distance;
        var across=State.Vehicles.Where(o=>o.Id!=v.Id&&InMap(o.Position)&&LaneTraffic(o)&&Ahead(v,o)>0&&(o.IntersectionClaim!=null||Math.Abs(Math.Cos(o.Heading-v.Heading))<=.5)&&o.Position.Distance(v.Position)<=distance+12).ToList();
        if(across.Count==0)return distance;
        // At an informal crossing both can be ahead of one another. Preserve
        // the existing ID winner, but never let it pass through the other body.
        // Only its extra safety margin is waived, and only when the ENTIRE
        // straight corridor has a real side gap. Waiving it for a single step
        // could bring diagonal traffic too close to finish the following turn.
        bool SideGap(Vehicle other){
            var relative=other.Position-v.Position;var side=Math.Abs(relative.X*Math.Cos(v.Heading)-relative.Z*Math.Sin(v.Heading));
            var angle=other.Heading-v.Heading;
            return side>BodyHalfWidth(v)+BodyHalfLength(other)*Math.Abs(Math.Sin(angle))+BodyHalfWidth(other)*Math.Abs(Math.Cos(angle));
        }
        bool Blocked(Point at,Vehicle other)=>VehicleBodiesOverlap(v,at,other)&&
            !(other.IntersectionClaim==null&&Ahead(other,v)>0&&v.Id<other.Id&&SideGap(other));
        var safe=0.0;
        while(safe<distance-.000001){var next=Math.Min(distance,safe+.5);var at=v.Position.MoveTowards(target,next);if(across.Any(o=>Blocked(at,o)))break;safe=next;}
        return safe;
    }
}
