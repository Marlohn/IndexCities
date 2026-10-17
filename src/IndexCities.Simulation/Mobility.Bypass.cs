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
        if(blocker.Status!=VehicleStatus.Stranded||v.Detour.Count>0||v.WaitingSince<=0||v.RouteIndex<1||v.RouteIndex>=v.Route.Count)return false;
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
    bool OppositeLaneClear(Vehicle v,Vehicle? ignored,Point dir,Point left,double end)
    {
        var duration=end/State.Tuning.VehicleSpeed;
        foreach(var o in State.Vehicles){
            if(o.Id==v.Id||o.Id==ignored?.Id||o.Status is not (VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded))continue;
            var relative=o.Position-v.Position;var along=relative.X*dir.X+relative.Z*dir.Z;var side=relative.X*left.X+relative.Z*left.Z;
            var oncoming=Math.Cos(o.Heading-v.Heading)<-.5&&o.Status==VehicleStatus.Travelling;
            var reach=end+8+(oncoming?Math.Max(State.Tuning.VehicleSpeed,State.Tuning.HighwaySpeed)*duration:0);
            if(side>1&&side<5.5&&along>-8&&along<reach)return false;
            if(side>-2&&side<=1&&along>end-8&&along<end+8)return false;
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
            if(i>=v.RouteIndex&&(node.Neighbours.Count>=3||node.Crosswalk||node.Highway))return false;
            if(previous is {} p){var step=node.Position-p;var len=step.Distance(default);if(len<.01||(step.X*dir.X+step.Z*dir.Z)/len<.999)return false;}
            previous=node.Position;
            var along=(node.Position-v.Position).X*dir.X+(node.Position-v.Position).Z*dir.Z;
            if(i>=v.RouteIndex&&along>end+.5){resume=i;break;}
        }
        if(resume<0||!OppositeLaneClear(v,blocker,dir,left,end))return false;
        v.Detour=[v.Position+dir*2+left*OppositeLaneOffset,v.Position+dir*(blockerAlong+6)+left*OppositeLaneOffset,v.Position+dir*end];
        v.DetourResumeIndex=resume;v.WaitingSince=0;v.Cause="Ultrapassando veículo parado pela contramão";
        return true;
    }
    bool TryUTurn(Vehicle v,Vehicle blocker)
    {
        if(!RoadAxis(v,out var dir,out var left))return false;
        var ahead=v.Route[v.RouteIndex];var behind=v.Route[v.RouteIndex-1];
        // Only the road samples the stopped vehicle physically occupies are closed.
        var avoid=State.Roads.Where(r=>r.Position.Distance(blocker.Position)<State.Tuning.RoadStep+.5).Select(r=>r.Id).ToHashSet();
        if(avoid.Contains(behind)){v.Cause="Rua bloqueada por veículo parado; sem espaço para meia-volta";return false;}
        var alternative=FindRoadRoute(behind,v.Route[^1],avoid);
        if(alternative.Count==0){v.Cause="Rua bloqueada por veículo parado; sem rota alternativa";return false;}
        if(!OppositeLaneClear(v,blocker,dir,left,8))return false;
        // The first entry only gives the reversed lane geometry; motion starts at the node behind.
        v.Route=[ahead,..alternative];v.RouteIndex=1;v.IntersectionClaim=null;v.WaitingSince=0;v.Cause="Meia-volta: rua bloqueada por veículo parado";
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
            if(o.Id==v.Id||o.Status is not (VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded))continue;
            var relative=o.Position-v.Position;var forward=relative.X*heading.X+relative.Z*heading.Z;var side=Math.Abs(relative.X*heading.Z-relative.Z*heading.X);
            if(forward>0&&side<2.6&&Math.Cos(o.Heading-v.Heading)>.5)leader=Math.Min(leader,forward);
        }
        var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leader-8)));
        if(step<.00001&&d>.00001){v.Cause="Ultrapassagem aguardando espaço";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;remaining=0;return;}
        if(v.Fuel<=.000001){v.Status=VehicleStatus.Stranded;v.Cause="Sem combustível; nenhuma viagem fictícia";remaining=0;return;}
        step=Math.Min(step,v.Fuel/FuelRate(v));if(d>.01)v.Heading=Math.Atan2(heading.X,heading.Z);
        v.Position=v.Position.MoveTowards(target,step);v.Fuel=Math.Max(0,v.Fuel-step*FuelRate(v));v.FuelBurnedLastStep+=step*FuelRate(v);remaining-=step/speed;v.WaitingSince=0;
        if(d<=step+.001){v.Detour.RemoveAt(0);if(v.Detour.Count==0){v.RouteIndex=v.DetourResumeIndex;v.Cause="Em viagem";}}
    }
}
