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
    bool _pr57BlockHadPrevious,_pr57BlockSeenThisVehicle;
    long _pr57BlockAttempts,_pr57BlockWithLeader,_pr57BlockNoLeader,
         _pr57BlockTowAttempts,_pr57BlockStrandedLeaders,_pr57BlockPastUTurn,
         _pr57BlockRepeat,_pr57BlockSameLeaderId,_pr57BlockSameLeaderRead,
         _pr57BlockSameSelfRead,_pr57BlockSameFullRead,_pr57BlockStableThirty,
         _pr57BlockWakeMoved,_pr57BlockWakeNoHold;

    void Pr57BlockBeginVehicle(Vehicle v)
    {
        _pr57BlockHadPrevious=_pr57LastBlocked.ContainsKey(v.Id);
        _pr57BlockSeenThisVehicle=false;
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
        if(_pr57LastBlocked.TryGetValue(v.Id,out var prior))
        {
            _pr57BlockRepeat++;
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
                sample=sample with {StableSince=prior.StableSince};
                if(now-prior.StableSince>=30)
                {
                    _pr57BlockStableThirty++;
                    _pr57StableThirtyVehicles.Add(v.Id);
                }
            }
        }
        _pr57LastBlocked[v.Id]=sample;
    }

    void Pr57BlockFinishVehicle(Vehicle v,Pr57IdleSample before)
    {
        var moved=!v.Position.Equals(before.Position)||v.Heading!=before.Heading;
        if(_pr57BlockHadPrevious)
        {
            if(moved)_pr57BlockWakeMoved++;
            else if(!_pr57BlockSeenThisVehicle)_pr57BlockWakeNoHold++;
        }
        if(moved||!_pr57BlockSeenThisVehicle)_pr57LastBlocked.Remove(v.Id);
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
            $"PR57_IDLE_PROBE slices={_pr57IdleProbeSlices} vehicles={_pr57IdleProbeVehicles} "+
            $"poseUnchanged={_pr57IdleProbePoseSame} sampledFieldsUnchanged={_pr57IdleProbeFieldsSame} "+
            $"laneVisits={_pr57IdleProbeLaneVisits} lanePoseUnchanged={_pr57IdleProbeLanePoseSame} "+
            $"laneSampledFieldsUnchanged={_pr57IdleProbeLaneFieldsSame} "+
            $"bendVisits={_pr57IdleProbeBendVisits} bendPoseUnchanged={_pr57IdleProbeBendPoseSame} "+
            $"bendSampledFieldsUnchanged={_pr57IdleProbeBendFieldsSame}");
    }
}
