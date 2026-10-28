namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // Derived, scoped to one movement batch; no new persisted gameplay state.
    HashSet<int>? _vehicleMovementBatch;
    Dictionary<int,Vehicle>? _entryFrame;readonly Dictionary<int,Vehicle> _entryFrameBuffer=new();
    // PR57 opt-in: reuse immutable-for-the-slice entry-frame copies across slices.
    // Never reuse these for projected EntrySnapshot() copies, which are mutated.
    readonly Dictionary<int,Vehicle> _entrySnapshotReuse=new();
    double _vehicleMovementOffset;
    double? _vehicleMovementClock;
    double VehicleMovementTime=>_vehicleMovementClock??State.ElapsedSeconds;
    // PR57-only observation: substep decisions by simulated hour, never used by motion.
    // Samples are taken only when a junction boundary wins; they are not a continuous vehicle trajectory.
    sealed class LaneWinnerTrace
    {
        public long Wins,SameTargetTransitions,NoPositionProgress,NoTargetApproach,RouteIndexChanges,LongestNoProgressRun;
        public int FirstRouteIndex,LastRouteIndex,LastTarget=-1;
        public Point FirstPosition,LastPosition;
        public double FirstDistance,LastDistance,SampledDistance,MaxSampledDelta;
        public readonly Dictionary<int,long> TargetCounts=new();
        long _noProgressRun;
        bool _hasPrevious;
        public void Observe(Vehicle vehicle,int targetNodeId,double targetDistance)
        {
            var position=vehicle.Position;
            if(!_hasPrevious){
                FirstRouteIndex=vehicle.RouteIndex;
                FirstPosition=position;
                FirstDistance=targetDistance;
            }else{
                var displacement=position.Distance(LastPosition);
                SampledDistance+=displacement;
                MaxSampledDelta=Math.Max(MaxSampledDelta,displacement);
                if(vehicle.RouteIndex!=LastRouteIndex)RouteIndexChanges++;
                if(targetNodeId==LastTarget){
                    SameTargetTransitions++;
                    if(displacement<=.000001){
                        NoPositionProgress++;
                        _noProgressRun++;
                        LongestNoProgressRun=Math.Max(LongestNoProgressRun,_noProgressRun);
                    }else _noProgressRun=0;
                    if(targetDistance>=LastDistance-.000001)NoTargetApproach++;
                }else _noProgressRun=0;
            }
            Wins++;
            TargetCounts.TryGetValue(targetNodeId,out var count);
            TargetCounts[targetNodeId]=count+1;
            LastRouteIndex=vehicle.RouteIndex;
            LastPosition=position;
            LastDistance=targetDistance;
            LastTarget=targetNodeId;
            _hasPrevious=true;
        }
    }
    // PR57-only post-slice evidence: did the predicted junction winner actually move?
    sealed class LaneWinnerOutcome
    {
        public long Samples,Stationary,Waiting,NewWaiting,RouteChanged,Claimed,EntryEligible,NotTravelling;
        public double Distance,MaxDistance,FirstWaitingSince,LastWaitingSince;
        public readonly Dictionary<string,long> Causes=new(StringComparer.Ordinal);
        public void Observe(Vehicle v,Point before,double waitingBefore,int routeBefore,bool entryEligible)
        {
            if(Samples==0)FirstWaitingSince=v.WaitingSince;
            Samples++;
            var moved=v.Position.Distance(before);
            Distance+=moved;MaxDistance=Math.Max(MaxDistance,moved);
            if(moved<=.000001)Stationary++;
            if(v.WaitingSince>0)Waiting++;
            if(waitingBefore<=0&&v.WaitingSince>0)NewWaiting++;
            if(v.RouteIndex!=routeBefore)RouteChanged++;
            if(v.IntersectionClaim!=null)Claimed++;
            if(entryEligible)EntryEligible++;
            if(v.Status!=VehicleStatus.Travelling)NotTravelling++;
            LastWaitingSince=v.WaitingSince;
            var cause=v.Cause??"";
            Causes.TryGetValue(cause,out var count);Causes[cause]=count+1;
        }
    }
    sealed class LaneSliceHour
    {
        public readonly long[] Reasons=new long[4]; // no-entry, full horizon, movement boundary, projected recheck
        public readonly long[] Durations=new long[5];
        public readonly long[] MovementCauses=new long[5]; // fuel, junction/crosswalk, speed, turn, route end
        public readonly Dictionary<int,long> JunctionWinnerCounts=new(); // observation only, never read by movement
        public readonly Dictionary<int,LaneWinnerTrace> JunctionWinnerTraces=new();
        public readonly Dictionary<int,LaneWinnerOutcome> JunctionWinnerOutcomes=new();
        public long BlockedEntryChecks,ProjectionCandidates,ProjectionRechecks;
        // PR57 observational only: did the vehicle predicting a speed boundary
        // actually advance in the subsequent physical slice?
        public long SpeedWinnerSamples,SpeedWinnerStationary,SpeedWinnerRouteChanged,SpeedWinnerWaiting;
        public double SpeedWinnerDistance;
        public readonly Dictionary<int,long> SpeedWinnerIds=new();
        public double MinSlice=double.PositiveInfinity,MaxSlice;
    }
    // Observational slice-delta census for the authentic blocked checkpoint only.
    // #187: observational partial net-state census, never a causal independence proof.
    static readonly bool Pr57SliceWorksetEnabled=Environment.GetEnvironmentVariable("PR57_187_SLICE_WORKSET")=="1";
    // #302: bounded, opt-in writer classification; does not authorize reuse or skipping.
    static readonly bool Pr57302CauseIdsEnabled=Environment.GetEnvironmentVariable("PR57_302_CAUSE_IDS")=="1";
    long _pr57302CurrentSlice,_pr57302BlockedWrites,_pr57302JunctionWrites,_pr57302WriterPrints;
    void Pr57302Writer(Vehicle v,string branch,string? before,int nodeId)
    {
        if(!Pr57302CauseIdsEnabled||_pr57300Calls<76||_pr57300Calls>100||
           string.Equals(before,v.Cause,StringComparison.Ordinal))return;
        if(branch=="blocked")_pr57302BlockedWrites++;else _pr57302JunctionWrites++;
        if(_pr57302WriterPrints++<120)
            Console.WriteLine($"PR57_302_WRITER call={_pr57300Calls} slice={_pr57302CurrentSlice} id={v.Id} branch={branch} node={nodeId} before={Uri.EscapeDataString(before??"")} after={Uri.EscapeDataString(v.Cause??"")} scope=PARTIAL_WRITE_NOT_CLOSURE");
    }
    long _pr57_187MotionAttempts;
    long _pr57TraceSlices,_pr57TraceMovedSlices,_pr57TraceVehicleMoves,_pr57TraceRouteChanges,_pr57TraceStatusChanges,_pr57TraceClaimChanges;
    readonly Dictionary<long,LaneSliceHour> _laneSliceHours=new();
    long _lastLaneSliceHourPrinted=-1;
    Vehicle? _laneObservedWinner;
    Vehicle? _speedObservedWinner;
    Point _speedObservedPosition;
    int _speedObservedRouteIndex;
    long _speedObservedHour;
    Point _laneObservedWinnerPosition;
    double _laneObservedWinnerWaitingSince;
    int _laneObservedWinnerRouteIndex;
    long _laneObservedWinnerHour;
    void ObserveLaneWinnerOutcome()
    {
        var speed=_speedObservedWinner;
        _speedObservedWinner=null;
        if(speed is not null&&_laneSliceHours.TryGetValue(_speedObservedHour,out var speedHour)){
            speedHour.SpeedWinnerSamples++;
            var moved=speed.Position.Distance(_speedObservedPosition);
            speedHour.SpeedWinnerDistance+=moved;
            if(moved<=.000001)speedHour.SpeedWinnerStationary++;
            if(speed.RouteIndex!=_speedObservedRouteIndex)speedHour.SpeedWinnerRouteChanged++;
            if(speed.WaitingSince>0)speedHour.SpeedWinnerWaiting++;
            speedHour.SpeedWinnerIds.TryGetValue(speed.Id,out var wins);
            speedHour.SpeedWinnerIds[speed.Id]=wins+1;
        }
        var v=_laneObservedWinner;
        _laneObservedWinner=null;
        if(v is null||!_laneSliceHours.TryGetValue(_laneObservedWinnerHour,out var hour))return;
        if(!hour.JunctionWinnerOutcomes.TryGetValue(v.Id,out var outcome))hour.JunctionWinnerOutcomes[v.Id]=outcome=new();
        outcome.Observe(v,_laneObservedWinnerPosition,_laneObservedWinnerWaitingSince,_laneObservedWinnerRouteIndex,TryLaneEntry(v,out _));
    }
    void ObserveLaneSlice(double slice,int reason,int movementCause,long blockedEntryChecks,long projectionCandidates,long projectionRechecks,Vehicle? winnerVehicle=null)
    {
        var hour=(long)Math.Floor(VehicleMovementTime/3600);
        if(!_laneSliceHours.TryGetValue(hour,out var stats))_laneSliceHours[hour]=stats=new();
        stats.Reasons[reason]++;
        _laneObservedWinner=reason==2&&movementCause==1?winnerVehicle:null;
        _speedObservedWinner=reason==2&&movementCause==2?winnerVehicle:null;
        if(_speedObservedWinner is not null){
            _speedObservedHour=hour;
            _speedObservedPosition=_speedObservedWinner.Position;
            _speedObservedRouteIndex=_speedObservedWinner.RouteIndex;
        }
        if(_laneObservedWinner is not null){
            _laneObservedWinnerHour=hour;
            _laneObservedWinnerPosition=_laneObservedWinner.Position;
            _laneObservedWinnerWaitingSince=_laneObservedWinner.WaitingSince;
            _laneObservedWinnerRouteIndex=_laneObservedWinner.RouteIndex;
        }
        if(reason==2&&movementCause>=0&&movementCause<5)stats.MovementCauses[movementCause]++;
        if(reason==2&&movementCause==1&&winnerVehicle is not null){
            stats.JunctionWinnerCounts.TryGetValue(winnerVehicle.Id,out var count);
            stats.JunctionWinnerCounts[winnerVehicle.Id]=count+1;
            if(TryMovementTarget(winnerVehicle,out _,out var target,out _,out _,out _)){
                var nodeId=winnerVehicle.RouteIndex>=0&&winnerVehicle.RouteIndex<winnerVehicle.Route.Count?winnerVehicle.Route[winnerVehicle.RouteIndex]:-1;
                if(!stats.JunctionWinnerTraces.TryGetValue(winnerVehicle.Id,out var trace))
                    stats.JunctionWinnerTraces[winnerVehicle.Id]=trace=new();
                trace.Observe(winnerVehicle,nodeId,winnerVehicle.Position.Distance(target));
            }
        }
        stats.Durations[slice<=.0001?0:slice<=.01?1:slice<=.1?2:slice<=1?3:4]++;
        stats.BlockedEntryChecks+=blockedEntryChecks;
        stats.ProjectionCandidates+=projectionCandidates;
        stats.ProjectionRechecks+=projectionRechecks;
        stats.MinSlice=Math.Min(stats.MinSlice,slice);
        stats.MaxSlice=Math.Max(stats.MaxSlice,slice);
    }
    void PrintCompletedLaneSliceHours(double endTime)
    {
        var last=(long)Math.Floor((endTime+1e-8)/3600)-1;
        for(var hour=_lastLaneSliceHourPrinted+1;hour<=last;hour++)
        {
            if(!_laneSliceHours.Remove(hour,out var stats))continue;
            var top=stats.JunctionWinnerCounts.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).Take(3).ToArray();
            var topId=top.Length>0?top[0].Key:-1;
            var topWins=top.Length>0?top[0].Value:0;
            var top3Wins=top.Sum(x=>x.Value);
            Console.WriteLine($"LANE_EVENT_HOUR hour={hour} noEntry={stats.Reasons[0]} full={stats.Reasons[1]} movementBoundary={stats.Reasons[2]} projectionRecheck={stats.Reasons[3]} blockedEntryChecks={stats.BlockedEntryChecks} projectedCandidates={stats.ProjectionCandidates} projectedRechecks={stats.ProjectionRechecks} movementFuel={stats.MovementCauses[0]} movementJunction={stats.MovementCauses[1]} movementSpeed={stats.MovementCauses[2]} movementTurn={stats.MovementCauses[3]} movementRouteEnd={stats.MovementCauses[4]} junctionUniqueWinners={stats.JunctionWinnerCounts.Count} junctionTopId={topId} junctionTopWins={topWins} junctionTop3Wins={top3Wins} sliceLe100us={stats.Durations[0]} sliceLe10ms={stats.Durations[1]} sliceLe100ms={stats.Durations[2]} sliceLe1s={stats.Durations[3]} sliceGt1s={stats.Durations[4]} minSlice={stats.MinSlice:F8} maxSlice={stats.MaxSlice:F6}");
            var speedTop=string.Join(";",stats.SpeedWinnerIds.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).Take(3).Select(x=>$"{x.Key}:{x.Value}"));
            Console.WriteLine($"LANE_SPEED_WINNER_HOUR hour={hour} samples={stats.SpeedWinnerSamples} stationary={stats.SpeedWinnerStationary} routeChanged={stats.SpeedWinnerRouteChanged} waiting={stats.SpeedWinnerWaiting} movedMeters={stats.SpeedWinnerDistance:F6} uniqueWinners={stats.SpeedWinnerIds.Count} top3={speedTop}");
            foreach(var winner in top){
                if(!stats.JunctionWinnerTraces.TryGetValue(winner.Key,out var trace))continue;
                var target=trace.TargetCounts.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).First();
                Console.WriteLine($"LANE_WINNER_TRACE hour={hour} vehicle={winner.Key} wins={trace.Wins} uniqueTargets={trace.TargetCounts.Count} topTarget={target.Key} topTargetWins={target.Value} sameTargetTransitions={trace.SameTargetTransitions} noPositionProgress={trace.NoPositionProgress} noTargetApproach={trace.NoTargetApproach} longestNoProgressRun={trace.LongestNoProgressRun} routeIndexChanges={trace.RouteIndexChanges} sampledDistance={trace.SampledDistance:F6} maxSampledDelta={trace.MaxSampledDelta:F6} firstRouteIndex={trace.FirstRouteIndex} lastRouteIndex={trace.LastRouteIndex} firstTargetDistance={trace.FirstDistance:F6} lastTargetDistance={trace.LastDistance:F6} firstX={trace.FirstPosition.X:F4} firstZ={trace.FirstPosition.Z:F4} lastX={trace.LastPosition.X:F4} lastZ={trace.LastPosition.Z:F4}");
                if(stats.JunctionWinnerOutcomes.TryGetValue(winner.Key,out var outcome)){
                    var causeTop=outcome.Causes.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key,StringComparer.Ordinal).Take(3).ToArray();
                    var causes=string.Join(";",causeTop.Select(x=>$"{x.Value}:{x.Key.Replace(' ','_').Replace(';','_')}"));
                    Console.WriteLine($"LANE_WINNER_OUTCOME hour={hour} vehicle={winner.Key} samples={outcome.Samples} stationary={outcome.Stationary} waiting={outcome.Waiting} newWaiting={outcome.NewWaiting} routeChanged={outcome.RouteChanged} claimed={outcome.Claimed} entryEligible={outcome.EntryEligible} notTravelling={outcome.NotTravelling} totalMoved={outcome.Distance:F6} maxMoved={outcome.MaxDistance:F6} firstWaitingSince={outcome.FirstWaitingSince:F6} lastWaitingSince={outcome.LastWaitingSince:F6} causes={causes}");
                }
            }
        }
        _lastLaneSliceHourPrinted=last;
    }
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
    bool HeldBehindStranded(Vehicle v)=>Pr57StrandedGraphEnabled?Pr57HeldBehindStranded(v):HeldInStrandedChain(v,false);
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
        var pr57300=Pr57300Enabled?new Pr57300Step():null;
        _pr57300Step=pr57300;
        var pr57300Call=pr57300 is null?0:++_pr57300Calls;
        var hasEntry=Pr57StrandedGraphEnabled?Pr57HasLiveLaneEntry():State.Vehicles.Any(LiveLaneEntry);
        var pr57EntryFrameReuse=Environment.GetEnvironmentVariable("PR57_ENTRY_FRAME_REUSE")=="1";
        if(profile is not null)profile.MarkVehicle(0,ref started);
        if(!hasEntry){
            MoveVehiclesUnsplit(dt);
            if(profile is not null){profile.MarkVehicle(3,ref started);profile.VehicleUnsplitSteps++;PrintCompletedLaneSliceHours(State.ElapsedSeconds+dt);}
            _pr57VehicleBvh=null; // never persist a spatial index across public Advance(2)
            Pr57JunctionCensusReportIfReady(State.ElapsedSeconds+dt);
            pr57300?.Print(pr57300Call,false);
            _pr57300Step=null;
            return;
        }
        if(profile is not null)profile.VehicleBatchedSteps++;
        var pr57Workset=Pr57SliceWorksetEnabled&&profile is not null;
        long worksetSlices=0,worksetCandidates=0,worksetAttempts=0,worksetPartialChanged=0;
        long worksetMoved=0,worksetCauseChanged=0,worksetWaitingChanged=0,worksetClaimChanged=0;
        long worksetOtherChanged=0,worksetNewVehicles=0,worksetProbeTicks=0,worksetMovementTicks=0;
        long pr57302EligibleNet=0,pr57302OtherNet=0,pr57302Printed=0,pr57302Omitted=0;
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
                _entryFrameBuffer.Clear();
                foreach(var v in State.Vehicles.Where(v=>OccupiesLane(v)&&!TryLaneEntry(v,out _))){
                    Vehicle snapshot;
                    if(pr57EntryFrameReuse&&_entrySnapshotReuse.TryGetValue(v.Id,out var cached)){
                        snapshot=cached;
                        snapshot.Position=v.Position;
                        snapshot.Heading=v.Heading;
                        snapshot.Status=v.Status;
                        snapshot.Fuel=v.Fuel;
                        snapshot.PassengerCar=v.PassengerCar;
                        snapshot.RouteIndex=v.RouteIndex;
                        snapshot.Route.Clear();
                        snapshot.Route.AddRange(v.Route);
                    }else{
                        snapshot=EntrySnapshot(v);
                        if(pr57EntryFrameReuse)_entrySnapshotReuse.Add(v.Id,snapshot);
                    }
                    _entryFrameBuffer.Add(v.Id,snapshot);
                }
                _entryFrame=_entryFrameBuffer;
                var pr57300ScheduleStart=pr57300 is null?0:System.Diagnostics.Stopwatch.GetTimestamp();
                var slice=Pr57StrandedGraphEnabled?Pr57NextMovementEvent(remaining):NextVehicleMovementEvent(remaining);
                if(pr57300 is not null)pr57300.ScheduleTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57300ScheduleStart;
                // PR57-only: compare actual physical state across each predicted boundary.
                // This is not an event journal: transitions that reverse inside a slice are not visible.
                var worksetProbeStart=pr57Workset?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                // Selected fields only; unchanged net state is NOT safe to skip.
                var worksetBefore=pr57Workset?State.Vehicles.ToDictionary(v=>v.Id,v=>(
                    v.Position,v.Heading,v.RouteIndex,v.Status,v.IntersectionClaim,
                    v.WaitingSince,v.Cause,v.FuelUnits,RouteCount:v.Route.Count,
                    SpaceCount:v.ManoeuvreSpaces.Count,DetourCount:v.Detour.Count,
                    Eligible:_vehicleMovementBatch!.Contains(v.Id)&&v.Status==VehicleStatus.Travelling)):null;
                if(worksetBefore is not null){
                    worksetCandidates+=worksetBefore.Values.Count(p=>p.Eligible);
                    worksetProbeTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-worksetProbeStart;
                }
                var worksetAttemptsBefore=_pr57_187MotionAttempts;
                var traceSlice=profile is not null&&Environment.GetEnvironmentVariable("PR57_TARGET_BLOCK_TRACE")=="1";
                var traceBefore=traceSlice?State.Vehicles.ToDictionary(v=>v.Id,v=>(v.Position,v.Heading,v.RouteIndex,v.Status,v.IntersectionClaim)):null;
                if(profile is not null)profile.MarkVehicle(2,ref started);
                var worksetMoveStart=pr57Workset?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                pr57300?.Capture(State.Vehicles);
                var pr57300MoveStart=pr57300 is null?0:System.Diagnostics.Stopwatch.GetTimestamp();
                if(Pr57302CauseIdsEnabled)_pr57302CurrentSlice=worksetSlices+1;
                MoveVehiclesUnsplit(slice);
                if(pr57300 is not null){pr57300.MoveTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57300MoveStart;pr57300.After(State.Vehicles);}
                if(worksetBefore is not null){
                    worksetMovementTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-worksetMoveStart;
                    worksetProbeStart=System.Diagnostics.Stopwatch.GetTimestamp();
                    worksetSlices++;
                    worksetAttempts+=_pr57_187MotionAttempts-worksetAttemptsBefore;
                    foreach(var v in State.Vehicles){
                        if(!worksetBefore.TryGetValue(v.Id,out var prior)){worksetNewVehicles++;continue;}
                        var moved=!v.Position.Equals(prior.Position);
                        var causeChanged=v.Cause!=prior.Cause;
                        if(causeChanged&&Pr57302CauseIdsEnabled&&pr57300Call is >=76 and <=100){
                            if(prior.Eligible)pr57302EligibleNet++;else pr57302OtherNet++;
                            if(pr57302Printed++<120)
                                Console.WriteLine($"PR57_302_NET call={pr57300Call} slice={worksetSlices} id={v.Id} eligible={(prior.Eligible?1:0)} before={Uri.EscapeDataString(prior.Cause??"")} after={Uri.EscapeDataString(v.Cause??"")} statusBefore={prior.Status} statusAfter={v.Status} claimBefore={prior.IntersectionClaim} claimAfter={v.IntersectionClaim} waitingBefore={prior.WaitingSince:F6} waitingAfter={v.WaitingSince:F6}");
                            else pr57302Omitted++;
                        }
                        var waitingChanged=v.WaitingSince!=prior.WaitingSince;
                        var claimChanged=v.IntersectionClaim!=prior.IntersectionClaim;
                        var changed=moved||v.Heading!=prior.Heading||v.RouteIndex!=prior.RouteIndex||
                            v.Status!=prior.Status||claimChanged||waitingChanged||causeChanged||
                            v.FuelUnits!=prior.FuelUnits||v.Route.Count!=prior.RouteCount||
                            v.ManoeuvreSpaces.Count!=prior.SpaceCount||v.Detour.Count!=prior.DetourCount;
                        if(prior.Eligible){
                            if(changed)worksetPartialChanged++;
                            if(moved)worksetMoved++;
                            if(causeChanged)worksetCauseChanged++;
                            if(waitingChanged)worksetWaitingChanged++;
                            if(claimChanged)worksetClaimChanged++;
                        }else if(changed)worksetOtherChanged++;
                    }
                    worksetProbeTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-worksetProbeStart;
                }
                if(traceBefore is not null){
                    int moved=0,heading=0,route=0,status=0,claim=0;
                    foreach(var vehicle in State.Vehicles){
                        if(!traceBefore.TryGetValue(vehicle.Id,out var prior)){status++;continue;}
                        if(vehicle.Position.Distance(prior.Position)>.000001)moved++;
                        if(vehicle.Heading!=prior.Heading)heading++;
                        if(vehicle.RouteIndex!=prior.RouteIndex)route++;
                        if(vehicle.Status!=prior.Status)status++;
                        if(vehicle.IntersectionClaim!=prior.IntersectionClaim)claim++;
                    }
                    _pr57TraceSlices++;if(moved>0)_pr57TraceMovedSlices++;
                    _pr57TraceVehicleMoves+=moved;_pr57TraceRouteChanges+=route;
                    _pr57TraceStatusChanges+=status;_pr57TraceClaimChanges+=claim;
                    if(_pr57TraceSlices<=3||_pr57TraceSlices%250==0)
                        Console.WriteLine($"PR57_TRACE_SLICE index={_pr57TraceSlices} time={VehicleMovementTime:F6} slice={slice:F9} moved={moved} heading={heading} route={route} status={status} claim={claim}");
                }
                if(profile is not null){ObserveLaneWinnerOutcome();profile.MarkVehicle(3,ref started);profile.VehicleSlices++;}
                _vehicleMovementOffset+=slice;
            }
        }finally{_laneObservedWinner=null;_vehicleMovementBatch=null;_entryFrame=null;_entryFrameBuffer.Clear();_entrySnapshotReuse.Clear();_vehicleMovementClock=null;_vehicleMovementOffset=0;_pr57VehicleBvh=null;_pr57300Step=null;}
        pr57300?.Print(pr57300Call,true);
        if(Pr57302CauseIdsEnabled&&pr57300Call is >=76 and <=100)
            Console.WriteLine($"PR57_302_STEP call={pr57300Call} slices={worksetSlices} eligibleNet={pr57302EligibleNet} otherNet={pr57302OtherNet} omitted={pr57302Omitted} blockedWritesCumulative={_pr57302BlockedWrites} junctionWritesCumulative={_pr57302JunctionWrites} scope=PARTIAL_NET_AND_TWO_WRITERS");
        if(pr57Workset)
            Console.WriteLine($"PR57_187_BATCH time={State.ElapsedSeconds:F3} slices={worksetSlices} initialEligible={worksetCandidates} actualAttempts={worksetAttempts} partialChanged={worksetPartialChanged} moved={worksetMoved} causeChanged={worksetCauseChanged} waitingChanged={worksetWaitingChanged} claimChanged={worksetClaimChanged} otherChanged={worksetOtherChanged} newVehicles={worksetNewVehicles} observerMs={worksetProbeTicks*1000.0/System.Diagnostics.Stopwatch.Frequency:F3} movementMs={worksetMovementTicks*1000.0/System.Diagnostics.Stopwatch.Frequency:F3} scope=INTRUSIVE_PARTIAL_NET_STATE_NOT_CAUSAL_INDEPENDENCE");
        if(profile is not null&&Environment.GetEnvironmentVariable("PR57_TARGET_BLOCK_TRACE")=="1"){
            var targets=string.Join(";",State.Vehicles.Where(v=>v.Id is 1107 or 487 or 1174).OrderBy(v=>v.Id).Select(v=>$"{v.Id}:{v.Position.X:F6}:{v.Position.Z:F6}:{v.RouteIndex}:{v.Status}:{v.WaitingSince:F3}"));
            Console.WriteLine($"PR57_TRACE_OUTER end={State.ElapsedSeconds+dt:F3} slices={_pr57TraceSlices} movedSlices={_pr57TraceMovedSlices} vehicleMoves={_pr57TraceVehicleMoves} routeChanges={_pr57TraceRouteChanges} statusChanges={_pr57TraceStatusChanges} claimChanges={_pr57TraceClaimChanges} targets={targets}");
        }
        if(profile is not null)PrintCompletedLaneSliceHours(State.ElapsedSeconds+dt);
        Pr57JunctionCensusReportIfReady(State.ElapsedSeconds+dt);
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
    double TimeToMovementChange(Vehicle v,double horizon,out int reason)
    {
        reason=-1;
        if(v.Status!=VehicleStatus.Travelling||!TryMovementTarget(v,out var node,out var target,out _,out _,out var speed))return horizon;
        var distance=v.Position.Distance(target);var heading=distance>.00001?(target-v.Position)*(1/distance):new Point(Math.Sin(v.Heading),Math.Cos(v.Heading));
        var until=distance/speed;var fuelTime=v.Fuel/FuelRate(v)/speed;
        if(fuelTime>MovementEventEpsilon&&fuelTime<horizon){horizon=fuelTime;reason=0;}
        var pathReason=4;
        // Skip road samples on the same straight stretch with the same speed.
        // A turn, speed boundary, crossing or destination is a real event.
        for(var index=v.RouteIndex+1;index<v.Route.Count&&until<horizon;index++){
            if(node.Crosswalk||node.Neighbours.Count>=3){pathReason=1;break;}
            if(!_roads.TryGetValue(v.Route[index],out var next))break;
            var edge=next.Position-node.Position;var len=edge.Distance(default);if(len<.01)break;
            var nextTarget=next.Position+new Point(edge.Z/len*1.65,-edge.X/len*1.65);
            var nextSpeed=(next.Highway?State.Tuning.HighwaySpeed:State.Tuning.VehicleSpeed)*Math.Max(.15,next.MaintenanceRatio);
            var segment=nextTarget-target;var segmentLength=segment.Distance(default);
            if(Math.Abs(nextSpeed-speed)>.000001){pathReason=2;break;}
            if(segmentLength<.01||(segment.X*heading.X+segment.Z*heading.Z)/segmentLength<.999999){pathReason=3;break;}
            until+=segmentLength/speed;node=next;target=nextTarget;
        }
        if(until>MovementEventEpsilon&&until<horizon){reason=pathReason;return until;}
        return horizon;
    }
    // PR57 isolated POC: a swept, conservative envelope narrows ONLY the
    // projected recheck loop. All candidate times and global slices are retained.
    // No state is cached between calls or persisted.
    static readonly bool Pr57EntrySweepFilterEnabled =
        Environment.GetEnvironmentVariable("PR57_ENTRY_SWEEP_FILTER")=="1";
    bool _pr57EntrySweepFilterAnnounced;
    bool Pr57CanBlockDuringEntrySweep(Vehicle entrant,LaneAccess access,Vehicle other,Point velocity,double horizon)
    {
        // BlocksLaneEntry first requires both lateral bounds. The projected
        // position is affine in t over [0,horizon], so endpoint extrema suffice.
        var projectedEnd=other.Position+velocity*horizon;
        var side0=access.Side(other.Position);
        var side1=access.Side(projectedEnd);
        var half=State.Tuning.RoadWidth/2;
        var lower=Math.Max(-half,Math.Min(access.FromSide,access.ToSide)-.9);
        var upper=Math.Min(half,Math.Max(access.FromSide,access.ToSide)+.9);
        var sideSlack=1e-6*(1+Math.Abs(side0)+Math.Abs(side1)+Math.Abs(lower)+Math.Abs(upper));
        if(Math.Max(side0,side1)<lower-sideSlack||Math.Min(side0,side1)>upper+sideSlack)return false;

        // The crossing along-coordinate is a convex combination of the
        // entrant's position and target. Approach speed is bounded by the
        // largest of the two nominal speeds in BlocksLaneEntry, regardless of
        // route or heading. Excluding an entire disjoint interval is exact.
        var along0=access.Along(other.Position);
        var along1=access.Along(projectedEnd);
        var cross0=access.Along(entrant.Position);
        var cross1=access.Along(access.Target);
        var maxClearance=8+access.Distance*Math.Max(State.Tuning.VehicleSpeed,State.Tuning.HighwaySpeed)/access.Speed;
        var alongSlack=1e-6*(1+Math.Abs(along0)+Math.Abs(along1)+Math.Abs(cross0)+Math.Abs(cross1)+Math.Abs(maxClearance));
        return !(Math.Max(along0,along1)<Math.Min(cross0,cross1)-maxClearance-alongSlack||
                 Math.Min(along0,along1)>Math.Max(cross0,cross1)+maxClearance+alongSlack);
    }
    double NextVehicleMovementEvent(double horizon)
    {
        var profile=CostProfile;
        var initialHorizon=horizon;
        long blockedEntryChecks=0,projectionCandidates=0,projectionRechecks=0;
        var movementCause=-1;Vehicle? movementWinner=null;
        var entries=State.Vehicles.Where(v=>_vehicleMovementBatch!.Contains(v.Id)&&LiveLaneEntry(v)).ToList();
        _pr57300Step?.Entries(entries);
        if(entries.Count==0){if(profile is not null)ObserveLaneSlice(horizon,0,-1,0,0,0);_pr57300Step?.Decision(0,-1,null,0,0,0);return horizon;}
        var held=State.Vehicles.Any(o=>o.Status==VehicleStatus.Stranded)?State.Vehicles.Where(HeldBehindStranded).Select(o=>o.Id).ToHashSet():null;
        foreach(var v in State.Vehicles.Where(v=>_vehicleMovementBatch!.Contains(v.Id))){
            // A blocked lot entry is not travelling to its lane target yet.
            // Its hypothetical arrival must not generate an extra substep.
            if(TryLaneEntry(v,out var waitingAccess)&&State.Vehicles.Where(o=>o.Id!=v.Id&&OccupiesLane(o)).Select(EntryFrameVehicle).Any(o=>BlocksLaneEntry(v,waitingAccess,o))){if(profile is not null)blockedEntryChecks++;continue;}
            if(held?.Contains(v.Id)==true)continue;
            var candidate=TimeToMovementChange(v,horizon,out var cause);
            if(candidate<horizon-1e-9){movementCause=cause;movementWinner=v;}
            horizon=Math.Min(horizon,candidate);
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
            if(profile is not null)projectionCandidates+=candidates.Count;
            // Keep the exact original candidate set and ordering. Only the
            // expensive all-occupant projected predicate is narrowed by a
            // superset of bodies able to intersect the entry envelope.
            var recheckOccupants=Pr57EntrySweepFilterEnabled&&candidates.Count>0
                ?occupants.Where(other=>Pr57CanBlockDuringEntrySweep(v,access,other,ProjectedVelocity(other,held),horizon)).ToList()
                :occupants;
            if(Pr57EntrySweepFilterEnabled&&!_pr57EntrySweepFilterAnnounced&&
               recheckOccupants.Count<occupants.Count&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
            {
                Console.WriteLine($"PR57_ENTRY_SWEEP_FILTER active=True occupants={occupants.Count} candidates={recheckOccupants.Count}");
                _pr57EntrySweepFilterAnnounced=true;
            }
            foreach(var t in candidates){
                if(t>=earliest)break;
                if(profile is not null)projectionRechecks++;
                var blocked=false;
                foreach(var other in recheckOccupants){var projected=EntrySnapshot(other);projected.Position+=ProjectedVelocity(other,held)*t;if(BlocksLaneEntry(v,access,projected)){blocked=true;break;}}
                // A prediction selects a time to RECHECK, never grants entry.
                // If a queue or crossing stopped a vehicle, the actual frame at
                // that event still blocks; it is never assumed to have left.
                if(!blocked){earliest=t;break;}
            }
        }
        var slice=Math.Max(Math.Min(earliest,horizon),Math.Min(MovementEventEpsilon,horizon));
        var reason=earliest<horizon-1e-9?3:horizon<initialHorizon-1e-9?2:1;
        if(profile is not null)ObserveLaneSlice(slice,reason,movementCause,blockedEntryChecks,projectionCandidates,projectionRechecks,movementWinner);
        _pr57300Step?.Decision(reason,movementCause,movementWinner,blockedEntryChecks,projectionCandidates,projectionRechecks);
        return slice;
    }
}
