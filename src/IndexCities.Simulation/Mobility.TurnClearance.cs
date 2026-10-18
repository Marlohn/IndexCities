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
    (double Heading,Point To,double Distance)? TurnClearance(Vehicle v,double approachHeading,double distance)
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
            var ahead=v.Position+new Point(Math.Sin(heading),Math.Cos(heading))*Math.Min(distance,1);
            // Leaving the lane line, the body is checked against every body.
            v.Heading=heading;var safe=Math.Min(SafeSiteTravel(v,ahead,Math.Min(distance,1),true),SafeSiteTravel(v,ahead,Math.Min(distance,1)));v.Heading=turned;
            if(safe>=.05)return(heading,v.Position.MoveTowards(ahead,safe),safe);
        }
        return null;
    }

    bool ParkedMachineOverlap(Vehicle v,Point at,double heading)=>State.Vehicles.Any(o=>o.Id!=v.Id&&o.Machine!=PavingMachine.None&&o.Status==VehicleStatus.Parked&&o.Position.Distance(at)<12&&BodiesOverlap(v,at,heading,o,o.Position,o.Heading));
    bool BodyOverlapAt(Vehicle v,Point at,double heading)=>State.Vehicles.Any(o=>o.Id!=v.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(at)<12&&BodiesOverlap(v,at,heading,o,o.Position,o.Heading));

    // At a bend without junction rules, a vehicle ahead across the lane (one
    // mid-turn) is no same-direction leader, yet its body is there. When each
    // is ahead of the other only the higher ID yields, as for leaders. Bodies
    // stacked at the exterior staging origin are not on the map yet.
    static double Ahead(Vehicle from,Vehicle other)=>(other.Position.X-from.Position.X)*Math.Sin(from.Heading)+(other.Position.Z-from.Position.Z)*Math.Cos(from.Heading);
    double ClearOfCrossingAhead(Vehicle v,Point target,double distance)
    {
        // Junction admission already orders claim holders; they are left to it.
        if(v.IntersectionClaim!=null)return distance;
        var across=State.Vehicles.Where(o=>o.Id!=v.Id&&o.IntersectionClaim==null&&InMap(o.Position)&&LaneTraffic(o)&&Ahead(v,o)>0&&(Ahead(o,v)<=0||v.Id>o.Id)&&Math.Abs(Math.Cos(o.Heading-v.Heading))<=.5&&o.Position.Distance(v.Position)<=distance+12).ToList();
        if(across.Count==0)return distance;
        var safe=0.0;
        while(safe<distance-.000001){var next=Math.Min(distance,safe+.5);var at=v.Position.MoveTowards(target,next);if(across.Any(o=>VehicleBodiesOverlap(v,at,o)))break;safe=next;}
        return safe;
    }
}
