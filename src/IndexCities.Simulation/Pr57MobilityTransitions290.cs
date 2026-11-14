using System.Diagnostics;

namespace IndexCities.Simulation;

// PR57 #290: observational, opt-in census at the canonical physical sub-slice
// and citizen-phase boundaries. Snapshots are never used by simulation logic.
// Net field differences are NOT writer events or a causal dependency closure.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Transitions290Enabled =
        Environment.GetEnvironmentVariable("PR57_290_TRANSITIONS")=="1";

    readonly record struct Pr57CitizenSnapshot290(
        int? VehicleId, int? CrossingNodeId, Point Position,
        int? BoardingVehicleId, bool Moving);
    readonly record struct Pr57VehicleSnapshot290(
        int? DriverId, Point Position, int? IntersectionClaim,
        VehicleStatus Status);

    readonly Dictionary<int,Pr57CitizenSnapshot290> _pr57CitizenBefore290=new();
    readonly Dictionary<int,Pr57VehicleSnapshot290> _pr57VehicleBefore290=new();
    // stages: 0=operations-only physical pass, 1=motion sub-slice,
    // 2=complete citizen-mobility phase. Counts are cumulative.
    // metrics: 0=intervals, 1=citizen visits, 2=vehicle visits,
    // 3=Citizen.VehicleId, 4=Citizen.CrossingNodeId,
    // 5=Citizen.Position, 6=Citizen.BoardingVehicleId, 7=Citizen.Moving,
    // 8=Vehicle.DriverId, 9=Vehicle.Position,
    // 10=Vehicle.IntersectionClaim, 11=Vehicle.Status,
    // 12=intervals with any projected change, 13=changed citizens,
    // 14=changed vehicles, 15=missing/new identities.
    readonly long[,] _pr57Metrics290=new long[3,16];
    readonly long[] _pr57ObserverTicks290=new long[3];
    readonly long[] _pr57ObserverBytes290=new long[3];
    static readonly string[] Pr57Stages290={"operations","physical","citizens"};

    void Pr57Transitions290Begin(int stage)
    {
        if(!Pr57Transitions290Enabled)return;
        var ticks=Stopwatch.GetTimestamp();
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        _pr57CitizenBefore290.Clear();
        _pr57VehicleBefore290.Clear();
        foreach(var c in State.Citizens)
        {
            _pr57CitizenBefore290[c.Id]=new(c.VehicleId,c.CrossingNodeId,c.Position,
                c.BoardingVehicleId,c.Moving);
            _pr57Metrics290[stage,1]++;
        }
        foreach(var v in State.Vehicles)
        {
            _pr57VehicleBefore290[v.Id]=new(v.DriverId,v.Position,
                v.IntersectionClaim,v.Status);
            _pr57Metrics290[stage,2]++;
        }
        _pr57ObserverTicks290[stage]+=Stopwatch.GetTimestamp()-ticks;
        _pr57ObserverBytes290[stage]+=GC.GetAllocatedBytesForCurrentThread()-bytes;
    }

    void Pr57Transitions290End(int stage)
    {
        if(!Pr57Transitions290Enabled)return;
        var ticks=Stopwatch.GetTimestamp();
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        var any=false;
        foreach(var c in State.Citizens)
        {
            _pr57Metrics290[stage,1]++;
            if(!_pr57CitizenBefore290.TryGetValue(c.Id,out var before))
            {
                _pr57Metrics290[stage,15]++;
                any=true;
                continue;
            }
            var changed=false;
            if(before.VehicleId!=c.VehicleId){_pr57Metrics290[stage,3]++;changed=true;}
            if(before.CrossingNodeId!=c.CrossingNodeId){_pr57Metrics290[stage,4]++;changed=true;}
            if(before.Position!=c.Position){_pr57Metrics290[stage,5]++;changed=true;}
            if(before.BoardingVehicleId!=c.BoardingVehicleId){_pr57Metrics290[stage,6]++;changed=true;}
            if(before.Moving!=c.Moving){_pr57Metrics290[stage,7]++;changed=true;}
            if(changed){_pr57Metrics290[stage,13]++;any=true;}
        }
        foreach(var v in State.Vehicles)
        {
            _pr57Metrics290[stage,2]++;
            if(!_pr57VehicleBefore290.TryGetValue(v.Id,out var before))
            {
                _pr57Metrics290[stage,15]++;
                any=true;
                continue;
            }
            var changed=false;
            if(before.DriverId!=v.DriverId){_pr57Metrics290[stage,8]++;changed=true;}
            if(before.Position!=v.Position){_pr57Metrics290[stage,9]++;changed=true;}
            if(before.IntersectionClaim!=v.IntersectionClaim){_pr57Metrics290[stage,10]++;changed=true;}
            if(before.Status!=v.Status){_pr57Metrics290[stage,11]++;changed=true;}
            if(changed){_pr57Metrics290[stage,14]++;any=true;}
        }
        if(State.Citizens.Count!=_pr57CitizenBefore290.Count ||
           State.Vehicles.Count!=_pr57VehicleBefore290.Count)
        {
            _pr57Metrics290[stage,15]+=
                Math.Abs(State.Citizens.Count-_pr57CitizenBefore290.Count)+
                Math.Abs(State.Vehicles.Count-_pr57VehicleBefore290.Count);
            any=true;
        }
        if(any)_pr57Metrics290[stage,12]++;
        _pr57Metrics290[stage,0]++;
        _pr57ObserverTicks290[stage]+=Stopwatch.GetTimestamp()-ticks;
        _pr57ObserverBytes290[stage]+=GC.GetAllocatedBytesForCurrentThread()-bytes;
    }

    public IEnumerable<string> Pr57Transitions290Lines(int calls)
    {
        if(!Pr57Transitions290Enabled)yield break;
        for(var stage=0;stage<3;stage++)
        {
            yield return FormattableString.Invariant(
                $"PR57_290_STAGE calls={calls} stage={Pr57Stages290[stage]} intervals={_pr57Metrics290[stage,0]} citizenVisits={_pr57Metrics290[stage,1]} vehicleVisits={_pr57Metrics290[stage,2]} citizenVehicleNet={_pr57Metrics290[stage,3]} citizenCrossingNet={_pr57Metrics290[stage,4]} citizenPositionNet={_pr57Metrics290[stage,5]} citizenBoardingNet={_pr57Metrics290[stage,6]} citizenMovingNet={_pr57Metrics290[stage,7]} vehicleDriverNet={_pr57Metrics290[stage,8]} vehiclePositionNet={_pr57Metrics290[stage,9]} vehicleClaimNet={_pr57Metrics290[stage,10]} vehicleStatusNet={_pr57Metrics290[stage,11]} intervalsChanged={_pr57Metrics290[stage,12]} citizensChanged={_pr57Metrics290[stage,13]} vehiclesChanged={_pr57Metrics290[stage,14]} identityMismatch={_pr57Metrics290[stage,15]} observerMs={_pr57ObserverTicks290[stage]*1000.0/Stopwatch.Frequency:F5} observerAllocatedBytes={_pr57ObserverBytes290[stage]}");
        }
    }
}
