namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 diagnostic ONLY. This is an upper-bound census of movement scans
    // ending without displacement, not a claim that any scan may be skipped.
    // Other vehicles, wages, history and reservations can still change.
    static readonly bool Pr57IdleProbeEnabled =
        Environment.GetEnvironmentVariable("PR57_IDLE_SCAN_PROBE")=="1";

    readonly record struct Pr57IdleSample(
        Point Position,double Heading,VehicleStatus Status,int RouteIndex,
        int? Claim,double WaitingSince,string? Cause,long FuelUnits,
        int RouteCount,int ManoeuvreCount,int DetourCount,
        TowPhase TowPhase,ServicePhase ServicePhase);

    long _pr57IdleProbeSlices,_pr57IdleProbeVehicles,_pr57IdleProbePoseSame,
         _pr57IdleProbeFieldsSame,_pr57IdleProbeLaneVisits,
         _pr57IdleProbeLanePoseSame,_pr57IdleProbeLaneFieldsSame,
         _pr57IdleProbeBendVisits,_pr57IdleProbeBendPoseSame,
         _pr57IdleProbeBendFieldsSame;

    static Pr57IdleSample Pr57IdleSnapshot(Vehicle v)=>new(
        v.Position,v.Heading,v.Status,v.RouteIndex,v.IntersectionClaim,
        v.WaitingSince,v.Cause,v.FuelUnits,v.Route.Count,
        v.ManoeuvreSpaces.Count,v.Detour.Count,v.TowPhase,v.ServicePhase);

    void Pr57IdleObserve(Vehicle v,Pr57IdleSample before,long laneVisits,long bendVisits)
    {
        _pr57IdleProbeVehicles++;
        _pr57IdleProbeLaneVisits+=laneVisits;
        _pr57IdleProbeBendVisits+=bendVisits;
        var poseSame=v.Position.Equals(before.Position)&&v.Heading==before.Heading;
        if(!poseSame)return;
        _pr57IdleProbePoseSame++;
        _pr57IdleProbeLanePoseSame+=laneVisits;
        _pr57IdleProbeBendPoseSame+=bendVisits;
        var fieldsSame=v.Status==before.Status&&v.RouteIndex==before.RouteIndex
            &&v.IntersectionClaim==before.Claim&&v.WaitingSince==before.WaitingSince
            &&string.Equals(v.Cause,before.Cause,StringComparison.Ordinal)
            &&v.FuelUnits==before.FuelUnits&&v.Route.Count==before.RouteCount
            &&v.ManoeuvreSpaces.Count==before.ManoeuvreCount
            &&v.Detour.Count==before.DetourCount&&v.TowPhase==before.TowPhase
            &&v.ServicePhase==before.ServicePhase;
        if(!fieldsSame)return;
        _pr57IdleProbeFieldsSame++;
        _pr57IdleProbeLaneFieldsSame+=laneVisits;
        _pr57IdleProbeBendFieldsSame+=bendVisits;
    }


    // Observational blocker read-set census. Equality here is deliberately
    // INCOMPLETE: signals, pedestrians, third-party claims, time-dependent
    // recovery and external edits are not represented. Never use it to skip work.
    readonly record struct Pr57BlockedRead(
        Point SelfPosition,double SelfHeading,VehicleStatus SelfStatus,
        int SelfRouteIndex,int? SelfClaim,double SelfWait,string? SelfCause,long SelfFuel,
        int? BlockerId,Point BlockerPosition,double BlockerHeading,
        VehicleStatus BlockerStatus,int? BlockerClaim,double BlockerWait,
        int BlockerRouteIndex,string? BlockerCause,long BlockerFuel,
        int NodeId,bool EnteringLane,bool BayHold,bool EntryHeld,
        double ObservedAt,double StableSince);

    readonly Dictionary<int,Pr57BlockedRead> _pr57LastBlocked=new();
    readonly HashSet<int> _pr57StableThirtyVehicles=new();
    readonly HashSet<int> _pr57StableSixtyVehicles=new();
    readonly HashSet<int> _pr57OperationGapVehicles=new();
    bool _pr57BlockHadPrevious,_pr57BlockSeenThisVehicle;
    // Stage is observational: 0=pre-loop, 1=in loop, 2=step gate,
    // 3=positive step, 4=junction denied, 5=movement commit reached.
    int _pr57BlockStage;
    readonly long[] _pr57WakeNoHoldByStage=new long[6];
    long _pr57WakeNoHoldShortGap,_pr57WakeNoHoldSameSelf,
         _pr57WakeNoHoldCauseChanged,_pr57WakeNoHoldStatusChanged;
    long _pr57WakeOperational,_pr57WakeOperationalRetained,
         _pr57WakeOperationalChanged,_pr57WakeOtherPreLoop,
         _pr57BlockRepeatAfterOperational,_pr57BlockSameAfterOperational,
         _pr57BlockStableSixty;
    int _pr57WakeTraceCount;

    void Pr57BlockMarkStage(int stage)
    {
        if(stage>_pr57BlockStage)_pr57BlockStage=stage;
    }
    long _pr57BlockAttempts,_pr57BlockWithLeader,_pr57BlockNoLeader,
         _pr57BlockTowAttempts,_pr57BlockStrandedLeaders,_pr57BlockPastUTurn,
         _pr57BlockRepeat,_pr57BlockSameLeaderId,_pr57BlockSameLeaderRead,
         _pr57BlockSameSelfRead,_pr57BlockSameFullRead,_pr57BlockStableThirty,
         _pr57BlockWakeMoved,_pr57BlockWakeNoHold;

    void Pr57BlockBeginVehicle(Vehicle v)
    {
        _pr57BlockHadPrevious=_pr57LastBlocked.ContainsKey(v.Id);
        _pr57BlockSeenThisVehicle=false;
        _pr57BlockStage=0;
    }

    void Pr57BlockObserve(Vehicle v,Vehicle? blocker,RoadNode node,
        bool enteringLane,bool bayHold,bool entryHeld)
    {
        _pr57BlockSeenThisVehicle=true;
        _pr57BlockAttempts++;
        if(blocker is null)_pr57BlockNoLeader++;else _pr57BlockWithLeader++;
        if(v.TowPhase!=TowPhase.None)_pr57BlockTowAttempts++;
        if(blocker?.Status==VehicleStatus.Stranded)_pr57BlockStrandedLeaders++;
        var now=VehicleMovementTime;
        if(v.WaitingSince>0&&now-v.WaitingSince>=State.Tuning.UTurnWaitSeconds)
            _pr57BlockPastUTurn++;
        var sample=new Pr57BlockedRead(
            v.Position,v.Heading,v.Status,v.RouteIndex,v.IntersectionClaim,
            v.WaitingSince,v.Cause,v.FuelUnits,
            blocker?.Id,blocker?.Position??default,blocker?.Heading??0,
            blocker?.Status??default,blocker?.IntersectionClaim,
            blocker?.WaitingSince??0,blocker?.RouteIndex??0,
            blocker?.Cause,blocker?.FuelUnits??0,
            node.Id,enteringLane,bayHold,entryHeld,now,now);
        var afterOperational=_pr57OperationGapVehicles.Remove(v.Id);
        if(_pr57LastBlocked.TryGetValue(v.Id,out var prior))
        {
            _pr57BlockRepeat++;
            if(afterOperational)_pr57BlockRepeatAfterOperational++;
            var sameId=prior.BlockerId==sample.BlockerId;
            if(sameId)_pr57BlockSameLeaderId++;
            var sameLeader=sameId&&prior.BlockerPosition.Equals(sample.BlockerPosition)
                &&prior.BlockerHeading==sample.BlockerHeading
                &&prior.BlockerStatus==sample.BlockerStatus
                &&prior.BlockerClaim==sample.BlockerClaim
                &&prior.BlockerWait==sample.BlockerWait
                &&prior.BlockerRouteIndex==sample.BlockerRouteIndex
                &&prior.BlockerFuel==sample.BlockerFuel
                &&string.Equals(prior.BlockerCause,sample.BlockerCause,StringComparison.Ordinal);
            if(sameLeader)_pr57BlockSameLeaderRead++;
            var sameSelf=prior.SelfPosition.Equals(sample.SelfPosition)
                &&prior.SelfHeading==sample.SelfHeading
                &&prior.SelfStatus==sample.SelfStatus
                &&prior.SelfRouteIndex==sample.SelfRouteIndex
                &&prior.SelfClaim==sample.SelfClaim
                &&prior.SelfWait==sample.SelfWait
                &&prior.SelfFuel==sample.SelfFuel
                &&string.Equals(prior.SelfCause,sample.SelfCause,StringComparison.Ordinal);
            if(sameSelf)_pr57BlockSameSelfRead++;
            if(sameSelf&&sameLeader&&prior.NodeId==sample.NodeId
                &&prior.EnteringLane==sample.EnteringLane
                &&prior.BayHold==sample.BayHold&&prior.EntryHeld==sample.EntryHeld)
            {
                _pr57BlockSameFullRead++;
                if(afterOperational)_pr57BlockSameAfterOperational++;
                sample=sample with {StableSince=prior.StableSince};
                if(now-prior.StableSince>=30)
                {
                    _pr57BlockStableThirty++;
                    _pr57StableThirtyVehicles.Add(v.Id);
                }
                if(now-prior.StableSince>=60)
                {
                    _pr57BlockStableSixty++;
                    _pr57StableSixtyVehicles.Add(v.Id);
                }
            }
        }
        _pr57LastBlocked[v.Id]=sample;
        Pr57DependencyObserve(v,sample);
    }

    void Pr57BlockFinishVehicle(Vehicle v,Pr57IdleSample before,double dt,double? operationsDt)
    {
        var moved=!v.Position.Equals(before.Position)||v.Heading!=before.Heading;
        var retainAcrossOperational=false;
        if(_pr57BlockHadPrevious)
        {
            if(moved)_pr57BlockWakeMoved++;
            else if(!_pr57BlockSeenThisVehicle)
            {
                _pr57BlockWakeNoHold++;
                _pr57WakeNoHoldByStage[Math.Clamp(_pr57BlockStage,0,5)]++;
                if(_pr57LastBlocked.TryGetValue(v.Id,out var previous))
                {
                    var gap=VehicleMovementTime-previous.ObservedAt;
                    if(gap<2)_pr57WakeNoHoldShortGap++;
                    if(v.Status!=previous.SelfStatus)_pr57WakeNoHoldStatusChanged++;
                    if(!string.Equals(v.Cause,previous.SelfCause,StringComparison.Ordinal))
                        _pr57WakeNoHoldCauseChanged++;
                    var sameSelf=v.Position.Equals(previous.SelfPosition)
                        &&v.Heading==previous.SelfHeading
                        &&v.Status==previous.SelfStatus
                        &&v.RouteIndex==previous.SelfRouteIndex
                        &&v.IntersectionClaim==previous.SelfClaim
                        &&v.WaitingSince==previous.SelfWait
                        &&v.FuelUnits==previous.SelfFuel
                        &&string.Equals(v.Cause,previous.SelfCause,StringComparison.Ordinal);
                    if(sameSelf)_pr57WakeNoHoldSameSelf++;
                    // A batched MoveVehiclesUnsplit(0,dt) runs operations only.
                    // Retain the sample for diagnosis, NEVER for skipping work:
                    // third parties, routes, signals and other inputs can change.
                    if(_pr57BlockStage==0)
                    {
                        if(dt<=.000001&&operationsDt is not null&&_vehicleMovementBatch is not null)
                        {
                            _pr57WakeOperational++;
                            if(sameSelf)
                            {
                                _pr57WakeOperationalRetained++;
                                retainAcrossOperational=true;
                                _pr57OperationGapVehicles.Add(v.Id);
                            }
                            else _pr57WakeOperationalChanged++;
                        }
                        else _pr57WakeOtherPreLoop++;
                    }
                    if(_pr57WakeTraceCount<16)
                    {
                        _pr57WakeTraceCount++;
                        Console.WriteLine($"PR57_WAKE_TRACE vehicle={v.Id} at={VehicleMovementTime:F6} gap={gap:F6} stage={_pr57BlockStage} status={v.Status} oldStatus={previous.SelfStatus} route={v.RouteIndex} oldRoute={previous.SelfRouteIndex} cause={v.Cause?.Replace(' ','_')} oldCause={previous.SelfCause?.Replace(' ','_')} sameSelf={sameSelf}");
                    }
                }
            }
        }
        if(moved||!_pr57BlockSeenThisVehicle&&!retainAcrossOperational)
        {
            _pr57LastBlocked.Remove(v.Id);
            _pr57OperationGapVehicles.Remove(v.Id);
        }
    }


    // PR57 observation only: a 60s census of OTHER mutable physical actors
    // while one blocked vehicle has an apparently stable sampled read-set.
    // A digest at a retry is NOT a write journal: intermediate writes, signals,
    // junction ownership, external edits and time-dependent effects can be missed.
    readonly record struct Pr57DependencyDigest(int Vehicles,int Nearby,int Pedestrians,int RoadRevision);
    int _pr57DependencyTarget;
    double _pr57DependencyStart,_pr57DependencyLastAt=double.NegativeInfinity;
    Pr57DependencyDigest _pr57DependencyLast;
    long _pr57DependencySamples,_pr57DependencyGlobalChanges,
         _pr57DependencyNearbyChanges,_pr57DependencyPedestrianChanges,
         _pr57DependencyRoadChanges,_pr57DependencySampleReset;
    bool _pr57DependencyComplete;
    // Boundary census is observational, only for the small authentic city.
    // It samples after each vehicle, not every field write or external mutation.
    readonly Dictionary<int,int> _pr57BoundaryActorHashes=new();
    readonly Dictionary<int,bool> _pr57BoundaryActorNear=new();
    long _pr57BoundarySamples,_pr57BoundaryChanged,_pr57BoundaryNearbyChanged,
         _pr57BoundarySelfChanged,_pr57BoundaryClock30sTransitions;
    int _pr57BoundaryFirstChangedActor,_pr57BoundaryLastClockPhase=int.MinValue;
    int? _pr57BoundarySelfHash;

    static void Pr57DependencyVehicle(ref HashCode hash,Vehicle o)
    {
        hash.Add(o.Id);hash.Add(o.Position);hash.Add(o.Heading);
        hash.Add(o.Status);hash.Add(o.RouteIndex);hash.Add(o.IntersectionClaim);
        hash.Add(o.WaitingSince);hash.Add(o.Cause);hash.Add(o.FuelUnits);
        hash.Add(o.DockPosition);hash.Add(o.HoldingPosition);
        hash.Add(o.ManoeuvreDock);hash.Add(o.ManoeuvreSpaces.Count);
        hash.Add(o.Detour.Count);hash.Add(o.TowPhase);hash.Add(o.ServicePhase);
        hash.Add(o.ManoeuvreDeparture);hash.Add(o.DetourResumeNode);
        hash.Add(o.MovementAdmission?.EndRouteIndex);
        foreach(var id in o.ManoeuvreSpaces)hash.Add(id);
        foreach(var pose in o.DockCorridor){hash.Add(pose.Position);hash.Add(pose.Heading);}
        foreach(var id in o.Route)hash.Add(id);
    }

    // Compare physical actors at EACH completed vehicle boundary during one
    // selected 60s window. This detects changes hidden by the old 2s digest,
    // but does not detect write-and-restore within a single vehicle operation.
    // Never consulted by physics, and never enabled for the large grid.
    void Pr57DependencyBoundary(Vehicle processed)
    {
        if(_pr57DependencyTarget==0||_pr57DependencyComplete||State.Vehicles.Count>200)return;
        var target=State.Vehicles.FirstOrDefault(o=>o.Id==_pr57DependencyTarget);
        if(target is null)return;
        _pr57BoundarySamples++;
        var clockPhase=(int)Math.Floor(VehicleMovementTime/30);
        if(_pr57BoundaryLastClockPhase!=int.MinValue&&clockPhase!=_pr57BoundaryLastClockPhase)
            _pr57BoundaryClock30sTransitions++;
        _pr57BoundaryLastClockPhase=clockPhase;
        foreach(var other in State.Vehicles)
        {
            var h=new HashCode();Pr57DependencyVehicle(ref h,other);
            var digest=h.ToHashCode();
            if(other.Id==target.Id)
            {
                if(_pr57BoundarySelfHash is {} previous&&previous!=digest)_pr57BoundarySelfChanged++;
                _pr57BoundarySelfHash=digest;
                continue;
            }
            var near=other.Position.Distance(target.Position)<=32;
            if(_pr57BoundaryActorHashes.TryGetValue(other.Id,out var old)&&old!=digest)
            {
                _pr57BoundaryChanged++;
                if(near||_pr57BoundaryActorNear.GetValueOrDefault(other.Id))
                    _pr57BoundaryNearbyChanged++;
                if(_pr57BoundaryFirstChangedActor==0)_pr57BoundaryFirstChangedActor=other.Id;
            }
            _pr57BoundaryActorHashes[other.Id]=digest;
            _pr57BoundaryActorNear[other.Id]=near;
        }
    }

    void Pr57DependencyObserve(Vehicle v,Pr57BlockedRead sample)
    {
        if(_pr57DependencyComplete)return;
        var now=VehicleMovementTime;
        if(_pr57DependencyTarget==0)
        {
            if(sample.BlockerId is null||now-sample.StableSince<60)return;
            _pr57DependencyTarget=v.Id;
            _pr57DependencyStart=now;
        }
        if(v.Id!=_pr57DependencyTarget||now-_pr57DependencyLastAt<2-1e-6)return;
        var global=new HashCode();
        var nearby=new HashCode();
        foreach(var other in State.Vehicles)
        {
            if(other.Id==v.Id)continue;
            Pr57DependencyVehicle(ref global,other);
            if(other.Position.Distance(v.Position)<=32)
                Pr57DependencyVehicle(ref nearby,other);
        }
        var pedestrians=new HashCode();
        foreach(var c in State.Citizens)
        {
            if(!c.Alive||!c.InsideMap)continue;
            pedestrians.Add(c.Id);pedestrians.Add(c.Position);
            pedestrians.Add(c.PathIndex);pedestrians.Add(c.CrossingNodeId);
            pedestrians.Add(c.VehicleId);pedestrians.Add(c.BoardingVehicleId);
        }
        var next=new Pr57DependencyDigest(
            global.ToHashCode(),nearby.ToHashCode(),pedestrians.ToHashCode(),State.RoadRevision);
        if(_pr57DependencySamples>0)
        {
            if(next.Vehicles!=_pr57DependencyLast.Vehicles)_pr57DependencyGlobalChanges++;
            if(next.Nearby!=_pr57DependencyLast.Nearby)_pr57DependencyNearbyChanges++;
            if(next.Pedestrians!=_pr57DependencyLast.Pedestrians)_pr57DependencyPedestrianChanges++;
            if(next.RoadRevision!=_pr57DependencyLast.RoadRevision)_pr57DependencyRoadChanges++;
            if(sample.StableSince>_pr57DependencyStart)_pr57DependencySampleReset++;
        }
        _pr57DependencyLast=next;
        _pr57DependencyLastAt=now;
        _pr57DependencySamples++;
        if(now-_pr57DependencyStart<60)return;
        _pr57DependencyComplete=true;
        Console.WriteLine(
            $"PR57_DEPENDENCY_WINDOW target={_pr57DependencyTarget} elapsed={now-_pr57DependencyStart:F3} " +
            $"samples={_pr57DependencySamples} globalChanges={_pr57DependencyGlobalChanges} " +
            $"nearbyChanges={_pr57DependencyNearbyChanges} pedestrianChanges={_pr57DependencyPedestrianChanges} " +
            $"roadRevisionChanges={_pr57DependencyRoadChanges} sampledReadResets={_pr57DependencySampleReset} " +
            $"boundarySamples={_pr57BoundarySamples} boundaryChanged={_pr57BoundaryChanged} " +
            $"boundaryNearbyChanged={_pr57BoundaryNearbyChanged} boundarySelfChanged={_pr57BoundarySelfChanged} " +
            $"clock30sTransitions={_pr57BoundaryClock30sTransitions} firstChangedActor={_pr57BoundaryFirstChangedActor}");
    }

    void Pr57IdleProbeTick()
    {
        _pr57IdleProbeSlices++;
        if(_pr57IdleProbeSlices!=1&&_pr57IdleProbeSlices%30!=0)return;
        Console.WriteLine(
            $"PR57_BLOCK_PROBE slices={_pr57IdleProbeSlices} attempts={_pr57BlockAttempts} " +
            $"withLeader={_pr57BlockWithLeader} noLeader={_pr57BlockNoLeader} " +
            $"tow={_pr57BlockTowAttempts} strandedLeader={_pr57BlockStrandedLeaders} " +
            $"pastUTurnWait={_pr57BlockPastUTurn} repeated={_pr57BlockRepeat} " +
            $"sameLeaderId={_pr57BlockSameLeaderId} sameLeaderRead={_pr57BlockSameLeaderRead} " +
            $"sameSelfRead={_pr57BlockSameSelfRead} sameSampledReadSet={_pr57BlockSameFullRead} " +
            $"stableReadSet30s={_pr57BlockStableThirty} stableVehicles30s={_pr57StableThirtyVehicles.Count} " +
            $"wokeMoved={_pr57BlockWakeMoved} wokeWithoutHold={_pr57BlockWakeNoHold}");
        Console.WriteLine(
            $"PR57_BLOCK_WAKE_CLASS slices={_pr57IdleProbeSlices} noHold={_pr57BlockWakeNoHold} " +
            $"preLoop={_pr57WakeNoHoldByStage[0]} inLoop={_pr57WakeNoHoldByStage[1]} " +
            $"stepGate={_pr57WakeNoHoldByStage[2]} positiveStep={_pr57WakeNoHoldByStage[3]} " +
            $"junctionDenied={_pr57WakeNoHoldByStage[4]} movementCommit={_pr57WakeNoHoldByStage[5]} " +
            $"gapUnder2s={_pr57WakeNoHoldShortGap} sameSelf={_pr57WakeNoHoldSameSelf} " +
            $"causeChanged={_pr57WakeNoHoldCauseChanged} statusChanged={_pr57WakeNoHoldStatusChanged}");
        Console.WriteLine(
            $"PR57_BLOCK_OPERATION slices={_pr57IdleProbeSlices} preLoopOperational={_pr57WakeOperational} " +
            $"preLoopOther={_pr57WakeOtherPreLoop} retained={_pr57WakeOperationalRetained} " +
            $"changed={_pr57WakeOperationalChanged} physicalAfterOperational={_pr57BlockRepeatAfterOperational} " +
            $"sameAfterOperational={_pr57BlockSameAfterOperational} " +
            $"stableReadSet60s={_pr57BlockStableSixty} stableVehicles60s={_pr57StableSixtyVehicles.Count}");
        Console.WriteLine(
            $"PR57_IDLE_PROBE slices={_pr57IdleProbeSlices} vehicles={_pr57IdleProbeVehicles} "+
            $"poseUnchanged={_pr57IdleProbePoseSame} sampledFieldsUnchanged={_pr57IdleProbeFieldsSame} "+
            $"laneVisits={_pr57IdleProbeLaneVisits} lanePoseUnchanged={_pr57IdleProbeLanePoseSame} "+
            $"laneSampledFieldsUnchanged={_pr57IdleProbeLaneFieldsSame} "+
            $"bendVisits={_pr57IdleProbeBendVisits} bendPoseUnchanged={_pr57IdleProbeBendPoseSame} "+
            $"bendSampledFieldsUnchanged={_pr57IdleProbeBendFieldsSame}");
    }
}
