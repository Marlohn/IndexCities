#!/usr/bin/env python3
"""PR57 #364: cross-phase effective vehicle writers and boundary net changes.

Checkout-only, opt-in. Apply after #359/#360/#361/#362/#363 injectors.
Uses #363's setter callback with #363 itself disabled; no gameplay decisions
or slice cadence are changed. A phase-boundary snapshot is NOT causal closure.
"""
from pathlib import Path

base=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
p=base/"CitySimulation.cs"
s=p.read_text(encoding="utf-8")

def patch(old,new):
    global s
    count=s.count(old)
    assert count==1,(old,count)
    s=s.replace(old,new,1)

patch("if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));",
      "if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));\n        Pr57364BeginAdvance(seconds);")
patch("        if(_pr57BoundaryActive)\n        {\n            _pr57BoundaryAdvanceTicks+=",
      "        Pr57364EndAdvance();\n        if(_pr57BoundaryActive)\n        {\n            _pr57BoundaryAdvanceTicks+=")

phase_anchors=[
    "MoveVehicles(dt);",
    "ReturnPavingMachines(dt);",
    "MoveCitizens(dt);",
    "var completedConstruction=ProgressConstruction(dt);",
    "UpdateMaintenanceCondition(dt);",
    "ProgressMunicipalRelocations(dt);",
    "UpdateInfrastructure(dt);",
    "OperateBusinesses(dt);",
    "UpdateNeeds(dt);",
    "State.AddElapsedSeconds(dt);State.Now=State.Now.AddSeconds(dt);",
    "OrderConstruction();",
    "if(reviewEconomy){RetryPurchasesAwaitingDestination();RestockBusinesses();ReviewEmigration();RecoverDebt();RetryCargoAwaitingDestination();ReviewVehicleRevisions();ResumeHeldDeliveries();RefuelIdleShopTows();}",
    "ProgressBuildingDemolitions();",
]
for phase,anchor in enumerate(phase_anchors):
    patch("        "+anchor,"        Pr57364Phase("+str(phase)+");\n        "+anchor)
patch("        ProcessLoans();\n        if(profile is not null)profile.Mark(12,ref started);",
      "        ProcessLoans();\n        Pr57364Phase(13);\n        if(profile is not null)profile.Mark(12,ref started);")
p.write_text(s,encoding="utf-8")

out=base/"Mobility.CrossPhaseWriters364.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// #364 observes all 13 canonical Step phases and public Advance boundaries.
// The callback is the same four setter witness from #363, enabled for entire
// selected Advance calls; #363 itself MUST be disabled. It is not a journal
// for nested lists, route contents, network edits, or arbitrary external writes.
public sealed partial class CitySimulation
{
    static readonly bool Pr57364Enabled =
        Environment.GetEnvironmentVariable("PR57_364_CROSS_PHASE_WRITES")=="1";
    const int Pr57364Rows=15; // pre, 13 phases, post
    readonly long[,] _pr57364Assign=new long[Pr57364Rows,4];
    readonly long[,] _pr57364Effective=new long[Pr57364Rows,4];
    readonly long[] _pr57364Flips=new long[Pr57364Rows];
    readonly long[] _pr57364NetVehicle=new long[Pr57364Rows];
    readonly long[] _pr57364NetPose=new long[Pr57364Rows];
    readonly long[] _pr57364NetRoute=new long[Pr57364Rows];
    readonly long[] _pr57364NetDock=new long[Pr57364Rows];
    readonly long[] _pr57364NetClaim=new long[Pr57364Rows];
    readonly long[] _pr57364NetCitizen=new long[Pr57364Rows];
    readonly long[] _pr57364Road=new long[Pr57364Rows];
    readonly HashSet<int> _pr57364UniqueEffective=new();
    readonly HashSet<Vehicle> _pr57364Canonical =
        new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
    Pr57364Snapshot? _pr57364Before, _pr57364PreviousEnd;
    long _pr57364Calls,_pr57364ExternalVehicle,_pr57364ExternalCitizen;
    long _pr57364ExternalRoad,_pr57364TempEffective,_pr57364AdvanceCount;
    int _pr57364Phase=-1,_pr57364Window=-1;
    bool _pr57364Active;

    readonly record struct Pr57364Vehicle(
        VehicleStatus Status,bool Passenger,long X,long Z,long Heading,
        int RouteIndex,int RouteCount,int? Claim,long? DockX,long? DockZ,
        int SpaceCount,long Waiting,int? Driver,int PassengerCount,
        bool Admission,bool? Departure);
    readonly record struct Pr57364Citizen(long X,long Z,int? Vehicle,int? Crossing);
    sealed record Pr57364Snapshot(
        Dictionary<int,Pr57364Vehicle> Vehicles,
        Dictionary<int,Pr57364Citizen> Citizens,int RoadRevision);

    static long Pr57364Bits(double x)=>BitConverter.DoubleToInt64Bits(x);
    Pr57364Snapshot Pr57364Capture()
    {
        var vehicles=new Dictionary<int,Pr57364Vehicle>(State.Vehicles.Count);
        foreach(var v in State.Vehicles)
        {
            var dock=v.DockPosition;
            vehicles.Add(v.Id,new Pr57364Vehicle(
                v.Status,v.PassengerCar,Pr57364Bits(v.Position.X),
                Pr57364Bits(v.Position.Z),Pr57364Bits(v.Heading),
                v.RouteIndex,v.Route.Count,v.IntersectionClaim,
                dock.HasValue?Pr57364Bits(dock.Value.X):null,
                dock.HasValue?Pr57364Bits(dock.Value.Z):null,
                v.ManoeuvreSpaces.Count,Pr57364Bits(v.WaitingSince),
                v.DriverId,v.Passengers.Count,v.MovementAdmission is not null,
                v.ManoeuvreDeparture));
        }
        var citizens=new Dictionary<int,Pr57364Citizen>(State.Citizens.Count);
        foreach(var c in State.Citizens)
            citizens.Add(c.Id,new Pr57364Citizen(
                Pr57364Bits(c.Position.X),Pr57364Bits(c.Position.Z),
                c.VehicleId,c.CrossingNodeId));
        return new Pr57364Snapshot(vehicles,citizens,State.RoadRevision);
    }

    static (long Vehicles,long Citizens,long Road) Pr57364Boundary(
        Pr57364Snapshot before,Pr57364Snapshot after)
    {
        long v=0,c=0;
        foreach(var (id,row) in after.Vehicles)
            if(!before.Vehicles.TryGetValue(id,out var old)||old!=row)v++;
        foreach(var id in before.Vehicles.Keys)
            if(!after.Vehicles.ContainsKey(id))v++;
        foreach(var (id,row) in after.Citizens)
            if(!before.Citizens.TryGetValue(id,out var old)||old!=row)c++;
        foreach(var id in before.Citizens.Keys)
            if(!after.Citizens.ContainsKey(id))c++;
        return(v,c,before.RoadRevision==after.RoadRevision?0:1);
    }

    void Pr57364Diff(int row,Pr57364Snapshot after)
    {
        var before=_pr57364Before;
        if(before is null){_pr57364Before=after;return;}
        foreach(var (id,current) in after.Vehicles)
        {
            if(!before.Vehicles.TryGetValue(id,out var old))
            {
                _pr57364NetVehicle[row]++;continue;
            }
            if(current==old)continue;
            _pr57364NetVehicle[row]++;
            if(current.X!=old.X||current.Z!=old.Z||current.Heading!=old.Heading)
                _pr57364NetPose[row]++;
            if(current.RouteIndex!=old.RouteIndex||current.RouteCount!=old.RouteCount)
                _pr57364NetRoute[row]++;
            if(current.DockX!=old.DockX||current.DockZ!=old.DockZ)
                _pr57364NetDock[row]++;
            if(current.Claim!=old.Claim||current.SpaceCount!=old.SpaceCount)
                _pr57364NetClaim[row]++;
        }
        foreach(var id in before.Vehicles.Keys)
            if(!after.Vehicles.ContainsKey(id))_pr57364NetVehicle[row]++;
        foreach(var (id,current) in after.Citizens)
            if(!before.Citizens.TryGetValue(id,out var old)||current!=old)
                _pr57364NetCitizen[row]++;
        foreach(var id in before.Citizens.Keys)
            if(!after.Citizens.ContainsKey(id))_pr57364NetCitizen[row]++;
        if(after.RoadRevision!=before.RoadRevision)_pr57364Road[row]++;
        _pr57364Before=after;
    }

    void Pr57364BeginAdvance(double seconds)
    {
        if(!Pr57364Enabled||seconds!=Quantum)return;
        _pr57364Calls++;
        var call=_pr57364Calls;
        var window=call is >=5715 and <=5801?0:
                   call is >=7143 and <=7230?1:-1;
        if(window<0){_pr57364PreviousEnd=null;return;}
        if(Vehicle.Pr57363Write is not null)
            throw new InvalidOperationException("#364 requires #363 observer disabled");
        if(window!=_pr57364Window)
        {
            _pr57364Window=window;
            Array.Clear(_pr57364Assign);Array.Clear(_pr57364Effective);
            foreach(var a in new[]{_pr57364Flips,_pr57364NetVehicle,
                _pr57364NetPose,_pr57364NetRoute,_pr57364NetDock,
                _pr57364NetClaim,_pr57364NetCitizen,_pr57364Road})Array.Clear(a);
            _pr57364UniqueEffective.Clear();
            _pr57364ExternalVehicle=_pr57364ExternalCitizen=0;
            _pr57364ExternalRoad=_pr57364TempEffective=_pr57364AdvanceCount=0;
            _pr57364PreviousEnd=null;
        }
        _pr57364Active=true;_pr57364Phase=-1;_pr57364AdvanceCount++;
        _pr57364Canonical.Clear();
        foreach(var v in State.Vehicles)_pr57364Canonical.Add(v);
        var snapshot=Pr57364Capture();
        if(_pr57364PreviousEnd is not null)
        {
            var boundary=Pr57364Boundary(_pr57364PreviousEnd,snapshot);
            _pr57364ExternalVehicle+=boundary.Vehicles;
            _pr57364ExternalCitizen+=boundary.Citizens;
            _pr57364ExternalRoad+=boundary.Road;
        }
        _pr57364Before=snapshot;
        Vehicle.Pr57363Write=Pr57364Observe;
    }

    void Pr57364Observe(Vehicle vehicle,int field,bool changed,bool flipped)
    {
        if(!_pr57364Active)return;
        if(!_pr57364Canonical.Contains(vehicle))
        {
            if(changed)_pr57364TempEffective++;
            return;
        }
        var row=Math.Clamp(_pr57364Phase+1,0,Pr57364Rows-1);
        _pr57364Assign[row,field]++;
        if(changed)
        {
            _pr57364Effective[row,field]++;
            _pr57364UniqueEffective.Add(vehicle.Id);
        }
        if(flipped)_pr57364Flips[row]++;
    }

    void Pr57364Phase(int next)
    {
        if(!_pr57364Active)return;
        if(next!=_pr57364Phase+1)
            throw new InvalidOperationException("#364 phase sequence changed");
        Pr57364Diff(_pr57364Phase+1,Pr57364Capture());
        _pr57364Phase=next;
    }

    void Pr57364EndAdvance()
    {
        if(!_pr57364Active)return;
        Pr57364Diff(Pr57364Rows-1,Pr57364Capture());
        Vehicle.Pr57363Write=null;
        _pr57364Active=false;
        _pr57364PreviousEnd=_pr57364Before;
        if(_pr57364Phase!=13)
            throw new InvalidOperationException("#364 missing Step phase");
        if(_pr57364Calls is not (5801 or 7230))return;
        var window=_pr57364Window;
        string[] labels=["pre","vehicles","paving","citizens","construction",
            "maintenance","relocation","infrastructure","businesses","needs",
            "economy","dispatch","followups","demolition","post"];
        long assigns=0,effective=0,flips=0,netV=0,netC=0,road=0;
        for(var row=0;row<Pr57364Rows;row++)
        {
            long a=0,e=0;
            for(var field=0;field<4;field++)
            {
                a+=_pr57364Assign[row,field];
                e+=_pr57364Effective[row,field];
            }
            assigns+=a;effective+=e;flips+=_pr57364Flips[row];
            netV+=_pr57364NetVehicle[row];
            netC+=_pr57364NetCitizen[row];
            road+=_pr57364Road[row];
            Console.WriteLine(
                $"PR57_364_PHASE window={window} phase={labels[row]} "+
                $"assignments={a} effective={e} occupancyFlips={_pr57364Flips[row]} "+
                $"netVehicle={_pr57364NetVehicle[row]} netPose={_pr57364NetPose[row]} "+
                $"netRoute={_pr57364NetRoute[row]} netDock={_pr57364NetDock[row]} "+
                $"netClaim={_pr57364NetClaim[row]} netCitizen={_pr57364NetCitizen[row]} "+
                $"roadRevision={_pr57364Road[row]} observational=True");
        }
        Console.WriteLine(
            $"PR57_364_WINDOW window={window} calls={_pr57364AdvanceCount} "+
            $"assignments={assigns} effective={effective} occupancyFlips={flips} "+
            $"uniqueEffectiveVehicles={_pr57364UniqueEffective.Count} "+
            $"tempEffective={_pr57364TempEffective} netVehicle={netV} "+
            $"netCitizen={netC} roadRevision={road} "+
            $"interAdvanceVehicle={_pr57364ExternalVehicle} "+
            $"interAdvanceCitizen={_pr57364ExternalCitizen} "+
            $"interAdvanceRoad={_pr57364ExternalRoad} "+
            "scope=FOUR_SETTERS_AND_PHASE_NET_PROJECTIONS "+
            "routeContentsAndTransientNestedWritesUnseen=True "+
            "noSkippedSlices=True notCausalClosure=True observational=True");
    }
}
''',encoding="utf-8")
print("PR57_364_INJECTED cross-phase four-setter and net projection census")
