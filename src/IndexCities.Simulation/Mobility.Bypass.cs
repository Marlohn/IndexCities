namespace IndexCities.Simulation;

// SPEC transporte-circulacao, "Veículo parado na via não trava a rua": a vehicle
// held behind one stopped on the road (out of fuel) overtakes through the free
// opposite lane, away from junctions and crossings; if that never opens, it
// turns back and takes another route. The stopped vehicle stays where it is.
public sealed partial class CitySimulation
{
    // Own lane centre is 1.65 m right of the axis; the opposite lane is 3.3 m left of it.
    const double OppositeLaneOffset=3.3;
    bool TryBypassStopped(Vehicle v,Vehicle blocker)
    {
        if(v.Detour.Count>0||v.WaitingSince<=0||v.RouteIndex<1||v.RouteIndex>=v.Route.Count)return false;
        // Only the vehicle right behind the stopped one acts; the queue behind it
        // moves up and repeats, so a blocked queue unwinds from its front.
        if(blocker.Status!=VehicleStatus.Stranded)return false;
        var waited=VehicleMovementTime-v.WaitingSince;
        if(waited>=State.Tuning.BypassWaitSeconds&&TryOvertake(v,blocker))return true;
        if(waited>=State.Tuning.UTurnWaitSeconds&&TryUTurn(v,blocker))return true;
        return false;
    }
    bool RoadAxis(Vehicle v,out Point dir,out Point left)
    {
        dir=left=default;if(!_roads.TryGetValue(v.Route[v.RouteIndex],out var node)||!_roads.TryGetValue(v.Route[v.RouteIndex-1],out var previous))return false;
        var axis=node.Position-previous.Position;var length=axis.Distance(default);if(length<.01)return false;
        dir=axis*(1/length);left=new Point(-dir.Z,dir.X);return true;
    }
    // The opposite lane must stay free for the whole manoeuvre, including an
    // oncoming vehicle covering the distance during it, and the return point too.
    bool OppositeLaneClear(Vehicle v,Vehicle? ignored,Point dir,Point left,double end,bool returnsToLane=true,Func<Vehicle,bool>? alsoIgnored=null)
    {
        var duration=end/State.Tuning.VehicleSpeed;
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.Id==ignored?.Id||!OccupiesLane(o)||alsoIgnored?.Invoke(o)==true)continue;
            var relative=o.Position-v.Position;var along=relative.X*dir.X+relative.Z*dir.Z;var side=relative.X*left.X+relative.Z*left.Z;
            var oncoming=Math.Cos(o.Heading-v.Heading)<-.5&&o.Status==VehicleStatus.Travelling;
            var reach=end+8+(oncoming?Math.Max(State.Tuning.VehicleSpeed,State.Tuning.HighwaySpeed)*duration:0);
            if(side>1&&side<5.5&&along>-8&&along<reach)return false;
            if(returnsToLane&&side>-2&&side<=1&&along>end-8&&along<end+8)return false;
        }
        return true;
    }
    bool TryOvertake(Vehicle v,Vehicle blocker)
    {
        if(!RoadAxis(v,out var dir,out var left))return false;
        var relative=blocker.Position-v.Position;var blockerAlong=relative.X*dir.X+relative.Z*dir.Z;
        if(blockerAlong<=0||blockerAlong>15||Math.Abs(relative.X*left.X+relative.Z*left.Z)>2.6)return false;
        var end=blockerAlong+10;
        // Straight stretch only: no junction, crossing or bend until the lane return.
        var resume=-1;Point? previous=null;
        for(var i=v.RouteIndex-1;i<v.Route.Count;i++){
            if(!_roads.TryGetValue(v.Route[i],out var node))return false;
            // Includes the node just passed while the vehicle is still on its pavement.
            var nearPassed=i==v.RouteIndex-1&&node.Position.Distance(v.Position)<State.Tuning.RoadWidth;
            if((i>=v.RouteIndex||nearPassed)&&(node.Neighbours.Count>=3||node.Crosswalk))return false;
            if(previous is {} p){var step=node.Position-p;var len=step.Distance(default);if(len<.01||(step.X*dir.X+step.Z*dir.Z)/len<.999)return false;}
            previous=node.Position;
            var along=(node.Position-v.Position).X*dir.X+(node.Position-v.Position).Z*dir.Z;
            if(i>=v.RouteIndex&&along>end+.5){resume=i;break;}
        }
        if(resume<0||!OppositeLaneClear(v,blocker,dir,left,end))return false;
        v.Detour=[v.Position+dir*2+left*OppositeLaneOffset,v.Position+dir*(blockerAlong+6)+left*OppositeLaneOffset,v.Position+dir*end];
        v.DetourResumeNode=v.Route[resume];v.WaitingSince=0;v.Cause="Ultrapassando veículo parado pela contramão";
        return true;
    }
    bool TryUTurn(Vehicle v,Vehicle? blocker,HashSet<int>? alsoClosed=null)
    {
        if(v.RouteIndex<1||v.RouteIndex>=v.Route.Count||v.Detour.Count>0||!RoadAxis(v,out var dir,out var left))return false;
        var ahead=v.Route[v.RouteIndex];var behind=v.Route[v.RouteIndex-1];
        // Only the road samples the stopped vehicle physically occupies are closed;
        // the sample the vehicle turns back from stays open (it stands there).
        var avoid=blocker==null?new HashSet<int>():State.Roads.Where(r=>r.Position.Distance(blocker.Position)<State.Tuning.RoadStep+.5).Select(r=>r.Id).ToHashSet();
        if(alsoClosed!=null)avoid.UnionWith(alsoClosed);
        avoid.Remove(behind);
        // It swings into the lane beside it 2 m ahead, or straight across when the
        // stopped vehicle is that close (one stopped inside the junction it waits at).
        Point Turn(double ahead)=>v.Position+dir*ahead+left*OppositeLaneOffset;
        bool Hits(Point at)=>blocker!=null&&(at-v.Position) is var to&&BodiesOverlap(v,at,Math.Atan2(to.X,to.Z),blocker,blocker.Position,blocker.Heading);
        var turn=Turn(2);if(Hits(turn))turn=Turn(0);
        if(Hits(turn)){v.Cause="Rua bloqueada por veículo parado; sem espaço para meia-volta";return false;}
        var alternative=FindRoadRoute(behind,v.Route[^1],false,avoid);
        if(alternative.Count==0){v.Cause="Rua bloqueada por veículo parado; sem rota alternativa";return false;}
        // Vehicles held behind the stopped one beyond it (it stands in a junction,
        // they in the lane beside) cannot come through it: not oncoming.
        double Along(Vehicle o)=>(o.Position-v.Position).X*dir.X+(o.Position-v.Position).Z*dir.Z;
        bool HeldBeyond(Vehicle o)=>blocker is {Status:VehicleStatus.Stranded}&&o.WaitingSince>0&&o.Detour.Count==0&&Along(o)>Along(blocker)&&HeldBehindStranded(o);
        if(!OppositeLaneClear(v,blocker,dir,left,8,returnsToLane:false,alsoIgnored:HeldBeyond))return false;
        // It moves into the lane beside it and drives back along that lane; the
        // first route entry only gives the reversed lane geometry.
        v.Route=[ahead,..alternative];v.RouteIndex=1;v.Detour=[turn];v.DetourResumeNode=behind;v.IntersectionClaim=null;v.WaitingSince=0;v.Cause="Meia-volta: rua bloqueada por veículo parado";
        return true;
    }
    // Motion along committed bypass waypoints. Only a same-direction vehicle on
    // the waypoint path holds it; oncoming traffic yields to it (see MoveVehicles).
    void MoveAlongDetour(Vehicle v,ref double remaining)
    {
        var target=v.Detour[0];var d=v.Position.Distance(target);var speed=State.Tuning.VehicleSpeed;
        var heading=d>.01?(target-v.Position)*(1/d):new Point(Math.Sin(v.Heading),Math.Cos(v.Heading));
        var leader=double.MaxValue;
        foreach(var o in State.Vehicles){
            // A tow passes the vehicle it comes for (it may stand askew in a junction).
            if(o.Id==v.Id||!OccupiesLane(o)||o.Id==v.TowTargetId&&v.TowPhase==TowPhase.ToVehicle)continue;
            var relative=o.Position-v.Position;var forward=relative.X*heading.X+relative.Z*heading.Z;var side=Math.Abs(relative.X*heading.Z-relative.Z*heading.X);
            if(forward>0&&side<2.6&&Math.Cos(o.Heading-v.Heading)>.5)leader=Math.Min(leader,forward);
        }
        var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leader-8)));
        if(step<.00001&&d>.00001){v.Cause="Ultrapassagem aguardando espaço";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;remaining=0;return;}
        if(v.Fuel<=.000001){RunOutOfFuel(v);remaining=0;return;}
        step=Math.Min(step,v.Fuel/FuelRate(v));if(d>.01)v.Heading=Math.Atan2(heading.X,heading.Z);
        v.Position=v.Position.MoveTowards(target,step);v.Fuel=Math.Max(0,v.Fuel-step*FuelRate(v));v.FuelBurnedLastStep+=step*FuelRate(v);remaining-=step/speed;v.WaitingSince=0;
        if(d<=step+.001){v.Detour.RemoveAt(0);if(v.Detour.Count==0){var resume=v.Route.IndexOf(v.DetourResumeNode,Math.Min(v.RouteIndex,v.Route.Count));if(resume>=0)v.RouteIndex=resume;
            // A tow that drove the empty opposite lane is already at its pickup pose.
            else if(v.TowPhase==TowPhase.ToVehicle&&v.DockPosition is {} pose&&v.Position.Distance(pose)<.05)v.RouteIndex=v.Route.Count;v.Cause="Em viagem";}}
    }
}
