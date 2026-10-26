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

    void Pr57IdleProbeTick()
    {
        _pr57IdleProbeSlices++;
        if(_pr57IdleProbeSlices!=1&&_pr57IdleProbeSlices%30!=0)return;
        Console.WriteLine(
            $"PR57_IDLE_PROBE slices={_pr57IdleProbeSlices} vehicles={_pr57IdleProbeVehicles} "+
            $"poseUnchanged={_pr57IdleProbePoseSame} sampledFieldsUnchanged={_pr57IdleProbeFieldsSame} "+
            $"laneVisits={_pr57IdleProbeLaneVisits} lanePoseUnchanged={_pr57IdleProbeLanePoseSame} "+
            $"laneSampledFieldsUnchanged={_pr57IdleProbeLaneFieldsSame} "+
            $"bendVisits={_pr57IdleProbeBendVisits} bendPoseUnchanged={_pr57IdleProbeBendPoseSame} "+
            $"bendSampledFieldsUnchanged={_pr57IdleProbeBendFieldsSame}");
    }
}
