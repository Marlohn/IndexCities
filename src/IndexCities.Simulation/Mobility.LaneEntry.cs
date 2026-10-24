namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // Derived, scoped to one movement batch; no new persisted gameplay state.
    HashSet<int>? _vehicleMovementBatch;
    Dictionary<int,Vehicle>? _entryFrame;readonly Dictionary<int,Vehicle> _entryFrameBuffer=new();
    double _vehicleMovementOffset;
    double? _vehicleMovementClock;
    double VehicleMovementTime=>_vehicleMovementClock??State.ElapsedSeconds;
    const double MovementEventEpsilon=.00001;
    readonly record struct LaneAccess(RoadNode Node,Point Axis,double Length,Point Target,double Distance,double Speed,double FromSide,double ToSide)
    {
        public double Side(Point p)=>((p.X-Node.Position.X)*Axis.Z-(p.Z-Node.Position.Z)*Axis.X)/Length;
        public double Along(Point p)=>((p.X-Node.Position.X)*Axis.X+(p.Z-Node.Position.Z)*Axis.Z)/Length;
    }
    bool AlreadyFollowingLane(Vehicle v,RoadNode node,double heading)
    {
        foreach(var neighbour in node.Neighbours){
            if(!_roads.TryGetValue(neighbour,out var adjacent))continue;
            var edge=adjacent.Position-node.Position;var length=edge.Distance(new(0,0));if(length<.01)continue;
            var direction=(Math.Sin(heading)*edge.X+Math.Cos(heading)*edge.Z)/length;
            if(Math.Abs(direction)<.9)continue;
            var relative=v.Position-node.Position;
            var along=(relative.X*edge.X+relative.Z*edge.Z)/length;
            var side=(relative.X*edge.Z-relative.Z*edge.X)/length;
            if(along>=-State.Tuning.RoadWidth/2&&along<=length+State.Tuning.RoadWidth/2&&Math.Abs(side*Math.Sign(direction)-1.65)<.35)return true;
        }
        return false;
    }
    bool TryMovementTarget(Vehicle v,out RoadNode node,out Point target,out Point axis,out double length,out double speed)
    {
        node=null!;target=axis=default;length=speed=0;
        if(v.RouteIndex>=v.Route.Count||v.RouteIndex<0||!_roads.TryGetValue(v.Route[v.RouteIndex],out node!))return false;
        var previous=v.RouteIndex>0&&_roads.TryGetValue(v.Route[v.RouteIndex-1],out var prior)?prior.Position:v.Position;
        if(v.RouteIndex==0&&v.Route.Count>1){if(!_roads.TryGetValue(v.Route[1],out var next))return false;axis=next.Position-node.Position;}else axis=node.Position-previous;
        length=axis.Distance(default);target=node.Position;
        if(length>.01)target+=new Point(axis.Z/length*1.65,-axis.X/length*1.65);
        if(v.RouteIndex>0)target+=InnerCornerOffset(v.Route,v.RouteIndex,node,axis,length);
        speed=(node.Highway?State.Tuning.HighwaySpeed:State.Tuning.VehicleSpeed)*Math.Max(.15,node.MaintenanceRatio);
        return speed>0;
    }
    bool TryLaneEntry(Vehicle v,out LaneAccess access)
    {
        access=default;
        if(!v.PassengerCar||v.Status!=VehicleStatus.Travelling||v.RouteIndex!=0||!InMap(v.Position)||!TryMovementTarget(v,out var node,out var target,out var axis,out var length,out var speed)||length<=.01||AlreadyFollowingLane(v,node,v.Heading))return false;
        var geometry=new LaneAccess(node,axis,length,target,v.Position.Distance(target),speed,0,0);
        access=geometry with{FromSide=geometry.Side(v.Position),ToSide=geometry.Side(target)};
        return Math.Abs(access.FromSide-access.ToSide)>.1;
    }
    Vehicle EntryFrameVehicle(Vehicle current)=>_entryFrame!=null&&_entryFrame.TryGetValue(current.Id,out var sameTime)?sameTime:current;
    bool BlocksLaneEntry(Vehicle v,LaneAccess access,Vehicle other)
    {
        var side=access.Side(other.Position);var half=State.Tuning.RoadWidth/2;
        if(Math.Abs(side)>half||side<Math.Min(access.FromSide,access.ToSide)-.9||side>Math.Max(access.FromSide,access.ToSide)+.9)return false;
        var fraction=Math.Clamp((side-access.FromSide)/(access.ToSide-access.FromSide),0,1);
        var crossAlong=access.Along(v.Position)*(1-fraction)+access.Along(access.Target)*fraction;
        var gap=access.Along(other.Position)-crossAlong;
        var direction=(Math.Sin(other.Heading)*access.Axis.X+Math.Cos(other.Heading)*access.Axis.Z)/access.Length;
        if(other.Status!=VehicleStatus.Stranded&&Math.Abs(direction)<.5&&other.PassengerCar&&other.RouteIndex==0&&Math.Abs(access.FromSide)<=half&&other.Id>v.Id)return false;
        var approaching=other.Status==VehicleStatus.Travelling&&gap*direction<0;
        // Preserve F1's nominal speed bound across the other vehicle's route,
        // including a faster node reachable before this entire manoeuvre ends.
        var approachSpeed=State.Tuning.VehicleSpeed;
        if(approaching){
            var ahead=Math.Max(State.Tuning.VehicleSpeed,State.Tuning.HighwaySpeed)*access.Distance/access.Speed;
            var position=other.Position;
            for(var index=other.RouteIndex;index<other.Route.Count&&ahead>=0;index++){
                if(!_roads.TryGetValue(other.Route[index],out var upcoming))break;
                approachSpeed=Math.Max(approachSpeed,upcoming.Highway?State.Tuning.HighwaySpeed:State.Tuning.VehicleSpeed);
                ahead-=position.Distance(upcoming.Position);position=upcoming.Position;
            }
        }
        return Math.Abs(gap)<8+(approaching?access.Distance*approachSpeed/access.Speed:0);
    }
    // A vehicle stranded on the lane never moves, and the queue held behind it
    // only moves when its front bypasses it. Neither is a motion event: a lot
    // entry they block is rechecked every step without substeps of its own, and
    // their next road sample is not a predicted arrival. Without a stranded
    // vehicle nothing here applies, so ordinary traffic keeps the same substeps.
    bool HeldBehindStranded(Vehicle v)=>HeldInStrandedChain(v,false);
    bool HeldInStrandedChain(Vehicle v,bool orAtItsJunction)
    {
        var current=v;
        for(var hops=0;hops<State.Vehicles.Count&&current.Status==VehicleStatus.Travelling&&current.WaitingSince>0&&current.Detour.Count==0;hops++){
            // Optionally the chain may also end at one held at the junction a stranded vehicle locks.
            if(orAtItsJunction&&HeldAtStrandedJunction(current))return true;
            // Same-direction leader within the hold distance, as in MoveVehiclesUnsplit.
            Vehicle? leader=null;var nearest=double.MaxValue;var sin=Math.Sin(current.Heading);var cos=Math.Cos(current.Heading);
            foreach(var o in State.Vehicles){
                if(o.Id==current.Id||!OccupiesLane(o))continue;
                var relative=o.Position-current.Position;var forward=relative.X*sin+relative.Z*cos;var side=Math.Abs(relative.X*cos-relative.Z*sin);
                if(forward>0&&side<2.6&&Math.Cos(o.Heading-current.Heading)>.5&&forward<nearest){nearest=forward;leader=o;}
            }
            if(leader==null||nearest>8+.000001)return false;
            if(leader.Status==VehicleStatus.Stranded)return true;
            current=leader;
        }
        return false;
    }
    bool StaticOnLane(Vehicle o)=>o.Status==VehicleStatus.Stranded||HeldBehindStranded(o);
    bool LiveLaneEntry(Vehicle v)=>TryLaneEntry(v,out var access)&&!(State.Vehicles.Any(o=>o.Status==VehicleStatus.Stranded)&&State.Vehicles.Any(o=>o.Id!=v.Id&&o.Status is VehicleStatus.Travelling or VehicleStatus.Stranded&&BlocksLaneEntry(v,access,EntryFrameVehicle(o))&&StaticOnLane(o)));
    static Vehicle EntrySnapshot(Vehicle v)=>new(){Id=v.Id,Position=v.Position,Heading=v.Heading,Status=v.Status,Fuel=v.Fuel,PassengerCar=v.PassengerCar,RouteIndex=v.RouteIndex,Route=v.Route.ToList()};
    void MoveVehicles(double dt)
    {
        var profile=CostProfile;
        var started=profile is null?0L:System.Diagnostics.Stopwatch.GetTimestamp();
        var hasEntry=State.Vehicles.Any(LiveLaneEntry);
        if(profile is not null)profile.MarkVehicle(0,ref started);
        if(!hasEntry){
            MoveVehiclesUnsplit(dt);
            if(profile is not null){profile.MarkVehicle(3,ref started);profile.VehicleUnsplitSteps++;}
            return;
        }
        if(profile is not null)profile.VehicleBatchedSteps++;
        _vehicleMovementBatch=[];_vehicleMovementOffset=0;
        try{
            // Driver wages, loading timers, replanning and boarding transitions
            // retain one operational update per outer step. Motion alone is split.
            MoveVehiclesUnsplit(0,dt);
            if(profile is not null)profile.MarkVehicle(1,ref started);
            while(dt-_vehicleMovementOffset>.000001){
                var remaining=dt-_vehicleMovementOffset;
                _vehicleMovementClock=State.ElapsedSeconds+_vehicleMovementOffset;
                // Same entries as ToDictionary, in a dictionary reused across slices.
                _entryFrameBuffer.Clear();foreach(var v in State.Vehicles.Where(v=>OccupiesLane(v)&&!TryLaneEntry(v,out _)))_entryFrameBuffer.Add(v.Id,EntrySnapshot(v));_entryFrame=_entryFrameBuffer;
                var slice=NextVehicleMovementEvent(remaining);
                if(profile is not null)profile.MarkVehicle(2,ref started);
                MoveVehiclesUnsplit(slice);
                if(profile is not null){profile.MarkVehicle(3,ref started);profile.VehicleSlices++;}
                _vehicleMovementOffset+=slice;
            }
        }finally{_vehicleMovementBatch=null;_entryFrame=null;_entryFrameBuffer.Clear();_vehicleMovementClock=null;_vehicleMovementOffset=0;}
    }
    Point ProjectedVelocity(Vehicle v,HashSet<int>? held)
    {
        // A competing lot entry may itself be blocked. Never forecast it as
        // free-flowing traffic: its actual progress and the existing ID rule
        // are observed when the movement batch is committed and rechecked.
        if(TryLaneEntry(v,out _)||held?.Contains(v.Id)==true)return default;
        if(v.Status!=VehicleStatus.Travelling||_vehicleMovementBatch==null||!_vehicleMovementBatch.Contains(v.Id)||v.Fuel<=.000001||!TryMovementTarget(v,out var node,out var target,out _,out _,out var speed)||!node.Highway&&node.MaintenanceRatio<=.05)return default;
        var d=v.Position.Distance(target);return d>.00001?(target-v.Position)*(speed/d):default;
    }
    double TimeToMovementChange(Vehicle v,double horizon)
    {
        if(v.Status!=VehicleStatus.Travelling||!TryMovementTarget(v,out var node,out var target,out _,out _,out var speed))return horizon;
        var distance=v.Position.Distance(target);var heading=distance>.00001?(target-v.Position)*(1/distance):new Point(Math.Sin(v.Heading),Math.Cos(v.Heading));
        var until=distance/speed;var fuelTime=v.Fuel/FuelRate(v)/speed;
        if(fuelTime>MovementEventEpsilon)horizon=Math.Min(horizon,fuelTime);
        // Skip road samples on the same straight stretch with the same speed.
        // A turn, speed boundary, crossing or destination is a real event.
        for(var index=v.RouteIndex+1;index<v.Route.Count&&until<horizon;index++){
            if(node.Crosswalk||node.Neighbours.Count>=3)break;
            if(!_roads.TryGetValue(v.Route[index],out var next))break;
            var edge=next.Position-node.Position;var len=edge.Distance(default);if(len<.01)break;
            var nextTarget=next.Position+new Point(edge.Z/len*1.65,-edge.X/len*1.65);
            var nextSpeed=(next.Highway?State.Tuning.HighwaySpeed:State.Tuning.VehicleSpeed)*Math.Max(.15,next.MaintenanceRatio);
            var segment=nextTarget-target;var segmentLength=segment.Distance(default);
            if(Math.Abs(nextSpeed-speed)>.000001||segmentLength<.01||(segment.X*heading.X+segment.Z*heading.Z)/segmentLength<.999999)break;
            until+=segmentLength/speed;node=next;target=nextTarget;
        }
        return until>MovementEventEpsilon?Math.Min(horizon,until):horizon;
    }
    double NextVehicleMovementEvent(double horizon)
    {
        var entries=State.Vehicles.Where(v=>_vehicleMovementBatch!.Contains(v.Id)&&LiveLaneEntry(v)).ToList();
        if(entries.Count==0)return horizon;
        var held=State.Vehicles.Any(o=>o.Status==VehicleStatus.Stranded)?State.Vehicles.Where(HeldBehindStranded).Select(o=>o.Id).ToHashSet():null;
        foreach(var v in State.Vehicles.Where(v=>_vehicleMovementBatch!.Contains(v.Id))){
            // A blocked lot entry is not travelling to its lane target yet.
            // Its hypothetical arrival must not generate an extra substep.
            if(TryLaneEntry(v,out var waitingAccess)&&State.Vehicles.Where(o=>o.Id!=v.Id&&OccupiesLane(o)).Select(EntryFrameVehicle).Any(o=>BlocksLaneEntry(v,waitingAccess,o)))continue;
            if(held?.Contains(v.Id)==true)continue;
            horizon=Math.Min(horizon,TimeToMovementChange(v,horizon));
        }
        var earliest=horizon;
        foreach(var v in entries){
            TryLaneEntry(v,out var access);
            var occupants=State.Vehicles.Where(o=>o.Id!=v.Id&&OccupiesLane(o)).Select(EntryFrameVehicle).ToList();
            if(!occupants.Any(o=>BlocksLaneEntry(v,access,o)))continue;
            var candidates=new SortedSet<double>();
            foreach(var other in occupants){
                var velocity=ProjectedVelocity(other,held);if(velocity.Distance(default)<.000001)continue;
                var side=access.Side(other.Position);var sideSpeed=(velocity.X*access.Axis.Z-velocity.Z*access.Axis.X)/access.Length;
                var cuts=new SortedSet<double>{0,horizon};
                if(Math.Abs(sideSpeed)>.000001)foreach(var boundary in new[]{-State.Tuning.RoadWidth/2,State.Tuning.RoadWidth/2,Math.Min(access.FromSide,access.ToSide)-.9,Math.Max(access.FromSide,access.ToSide)+.9,access.FromSide,access.ToSide}){var t=(boundary-side)/sideSpeed;if(t>0&&t<horizon)cuts.Add(t);}
                var times=cuts.ToArray();
                for(var i=0;i<times.Length-1;i++){
                    var begin=times[i];var end=times[i+1];
                    double Gap(double t){var p=other.Position+velocity*t;var fraction=Math.Clamp((access.Side(p)-access.FromSide)/(access.ToSide-access.FromSide),0,1);return access.Along(p)-access.Along(v.Position)*(1-fraction)-access.Along(access.Target)*fraction;}
                    var gap=Gap(begin);var rate=(Gap(end)-gap)/(end-begin);if(Math.Abs(rate)<.000001)continue;
                    foreach(var nominal in new[]{0,State.Tuning.VehicleSpeed,State.Tuning.HighwaySpeed}){
                        var clearance=8+access.Distance*nominal/access.Speed;
                        foreach(var bound in new[]{-clearance,clearance}){var t=begin+(bound-gap)/rate;if(t>=begin&&t<=end){t+=MovementEventEpsilon;if(t>MovementEventEpsilon&&t<horizon)candidates.Add(t);}}
                    }
                }
                foreach(var cut in cuts){var t=cut+MovementEventEpsilon;if(t>MovementEventEpsilon&&t<horizon)candidates.Add(t);}
            }
            foreach(var t in candidates){
                if(t>=earliest)break;
                var blocked=false;
                foreach(var other in occupants){var projected=EntrySnapshot(other);projected.Position+=ProjectedVelocity(other,held)*t;if(BlocksLaneEntry(v,access,projected)){blocked=true;break;}}
                // A prediction selects a time to RECHECK, never grants entry.
                // If a queue or crossing stopped a vehicle, the actual frame at
                // that event still blocks; it is never assumed to have left.
                if(!blocked){earliest=t;break;}
            }
        }
        return Math.Max(Math.Min(earliest,horizon),Math.Min(MovementEventEpsilon,horizon));
    }
}
