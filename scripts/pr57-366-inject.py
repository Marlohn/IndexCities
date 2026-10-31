#!/usr/bin/env python3
"""PR57 #366: immediate physical writer snapshots in the first active REAL hour.

Checkout-only opt-in: wraps existing canonical calls without skipping them.
Covers selected callsites, not all writers; exact ordered nested contents and
bitwise doubles. Does not reuse decisions or prove causal closure.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "src/IndexCities.Simulation"
mob = root / "Mobility.cs"
shared = root / "Mobility.SharedSpace.cs"

def patch(path, old, new, expected=1):
    s = path.read_text(encoding="utf-8")
    assert s.count(old) == expected, (path.name, old[:100], s.count(old))
    path.write_text(s.replace(old, new), encoding="utf-8")

patch(mob,
    "        var pr57Detail=CostProfile is not null;long pr57DetailStart=0;long pr57DetailTickStart=0;",
    "        Pr57366Begin();\n        var pr57Detail=CostProfile is not null;long pr57DetailStart=0;long pr57DetailTickStart=0;")
patch(mob,
    "foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}",
    'foreach(var vehicle in State.Vehicles){Pr57366Clear(vehicle,"cleanup-clear");Pr57366Release(vehicle);}')
patch(mob, "ClearPassedJunction(v);", 'Pr57366Clear(v,"movement-clear");', expected=2)
patch(mob, "ReserveJunction(v,junction,area);", "Pr57366Reserve(v,junction,area);", expected=3)
patch(mob, "ReserveJunction(v,junction,null);", "Pr57366Reserve(v,junction,null);")
patch(mob, "RememberLaneManoeuvre(v,spaces);", "Pr57366Remember(v,spaces);")
patch(mob, "RememberLaneManoeuvre(v,crossed);", "Pr57366Remember(v,crossed);")
patch(mob,
    'v.ManoeuvreSpaces.Add(junction.Id);Pr57ReservationWrite(v,"bend-add");',
    'var pr57366Before=Pr57366Before(v);v.ManoeuvreSpaces.Add(junction.Id);Pr57ReservationWrite(v,"bend-add");Pr57366After(v,"bend-add",pr57366Before);')
patch(mob,
    'v.ManoeuvreSpaces.Add(junction.Id);Pr57ReservationWrite(v,"crossing-add");',
    'var pr57366Before=Pr57366Before(v);v.ManoeuvreSpaces.Add(junction.Id);Pr57ReservationWrite(v,"crossing-add");Pr57366After(v,"crossing-add",pr57366Before);')
patch(shared,
    "        Pr57261Hit(4);\n        var admitted=DockAdmitted(v,dock);",
    "        var pr57366Before=Pr57366Before(v);\n        Pr57261Hit(4);\n        var admitted=DockAdmitted(v,dock);")
patch(shared,
    "        Pr57276Gate(v,4);\n        return true;",
    '        Pr57276Gate(v,4);\n        Pr57366After(v,"admit-manoeuvre",pr57366Before);\n        return true;')
patch(shared,
    "        m.ManoeuvreSpaces.AddRange(needed.Select(node=>node.Id));\n        Pr57ReservationWrite(m,\"machine-spaces\");",
    '        var pr57366Before=Pr57366Before(m);\n        m.ManoeuvreSpaces.AddRange(needed.Select(node=>node.Id));\n        Pr57ReservationWrite(m,"machine-spaces");\n        Pr57366After(m,"machine-spaces",pr57366Before);')

out = root / "Mobility.ImmediateWriter366.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;

namespace IndexCities.Simulation;

// #366 observes exact BEFORE/AFTER at selected canonical physical writer calls.
// Opt-in and checkout-only. Does not cover other writers, transients inside a
// wrapped method, pedestrian/signal/deadline changes or external edits.
public sealed partial class CitySimulation
{
    static readonly bool Pr57366Enabled =
        Environment.GetEnvironmentVariable("PR57_366_IMMEDIATE_WRITERS")=="1";
    static readonly DateTime Pr57366Start=new(2026,1,7,14,0,0);
    static readonly DateTime Pr57366Stop=new(2026,1,7,15,0,0);
    bool _pr57366Active,_pr57366Done;
    long _pr57366CaptureTicks,_pr57366Comparisons,_pr57366Noncanonical;
    long _pr57366EndNet,_pr57366EndMissing,_pr57366EndNew;
    readonly HashSet<Vehicle> _pr57366Canonical=new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<Vehicle,Pr57366Row> _pr57366StartRows=new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<string,Pr57366Site> _pr57366Sites=new();
    readonly HashSet<int> _pr57366ChangedIds=new();

    readonly record struct Pr57366Row(
        int? Claim,string Spaces,string Route,string Site,string Dock,
        string Admission,string Detour,string ManoeuvreDock,
        int RouteIndex,VehicleStatus Status,bool Passenger,
        long X,long Z,long Heading);
    sealed class Pr57366Site
    {
        public long Calls,Changed,Claim,Spaces,Route,Site,Dock,Admission,Detour,Pose;
        public readonly HashSet<int> Unique=new();
    }
    static long Pr57366Bits(double x)=>BitConverter.DoubleToInt64Bits(x);
    static string Pr57366Ints(IEnumerable<int> xs)=>string.Join(",",xs);
    static string Pr57366Points(IEnumerable<Point> xs)=>string.Join(";",xs.Select(
        p=>$"{Pr57366Bits(p.X)}:{Pr57366Bits(p.Z)}"));
    static string Pr57366Poses(IEnumerable<MovementPose> xs)=>string.Join(";",xs.Select(
        p=>$"{Pr57366Bits(p.Position.X)}:{Pr57366Bits(p.Position.Z)}:{Pr57366Bits(p.Heading)}"));
    static string Pr57366Admission(LocalMovementAdmission? a)=>a is null?"null":
        $"{Pr57366Ints(a.Route)}|{Pr57366Points(a.SitePath)}|{a.EndRouteIndex}|{a.ReachesDock}";
    Pr57366Row Pr57366Capture(Vehicle v)
    {
        var start=Stopwatch.GetTimestamp();
        var row=new Pr57366Row(
            v.IntersectionClaim,Pr57366Ints(v.ManoeuvreSpaces),
            Pr57366Ints(v.Route),Pr57366Points(v.SitePath),
            Pr57366Poses(v.DockCorridor),Pr57366Admission(v.MovementAdmission),
            Pr57366Points(v.Detour),
            v.ManoeuvreDock is {} p?$"{Pr57366Bits(p.X)}:{Pr57366Bits(p.Z)}":"null",
            v.RouteIndex,v.Status,v.PassengerCar,Pr57366Bits(v.Position.X),
            Pr57366Bits(v.Position.Z),Pr57366Bits(v.Heading));
        _pr57366CaptureTicks+=Stopwatch.GetTimestamp()-start;
        return row;
    }
    void Pr57366Begin()
    {
        if(!Pr57366Enabled||_pr57366Done)return;
        if(State.Now>=Pr57366Stop)
        {
            if(_pr57366Active)Pr57366Finish();
            return;
        }
        if(_pr57366Active||State.Now<Pr57366Start)return;
        _pr57366Active=true;
        foreach(var v in State.Vehicles)
        {
            _pr57366Canonical.Add(v);
            _pr57366StartRows.Add(v,Pr57366Capture(v));
        }
    }
    Pr57366Row? Pr57366Before(Vehicle v)=>
        _pr57366Active&&State.Now<Pr57366Stop?Pr57366Capture(v):null;
    void Pr57366After(Vehicle v,string site,Pr57366Row? before)
    {
        if(before is null)return;
        var after=Pr57366Capture(v);
        _pr57366Comparisons++;
        if(!_pr57366Canonical.Contains(v)){_pr57366Noncanonical++;return;}
        if(!_pr57366Sites.TryGetValue(site,out var row))
            _pr57366Sites.Add(site,row=new Pr57366Site());
        row.Calls++;
        var prior=before.Value;
        if(prior==after)return;
        row.Changed++;row.Unique.Add(v.Id);_pr57366ChangedIds.Add(v.Id);
        if(prior.Claim!=after.Claim)row.Claim++;
        if(prior.Spaces!=after.Spaces)row.Spaces++;
        if(prior.Route!=after.Route)row.Route++;
        if(prior.Site!=after.Site)row.Site++;
        if(prior.Dock!=after.Dock||prior.ManoeuvreDock!=after.ManoeuvreDock)row.Dock++;
        if(prior.Admission!=after.Admission)row.Admission++;
        if(prior.Detour!=after.Detour)row.Detour++;
        if(prior.X!=after.X||prior.Z!=after.Z||prior.Heading!=after.Heading)row.Pose++;
    }
    void Pr57366Clear(Vehicle v,string site)
    {
        var before=Pr57366Before(v);
        ClearPassedJunction(v);
        Pr57366After(v,site,before);
    }
    void Pr57366Release(Vehicle v)
    {
        var before=Pr57366Before(v);
        ReleaseManoeuvre(v);
        Pr57366After(v,"release-manoeuvre",before);
    }
    void Pr57366Remember(Vehicle v,List<RoadNode> spaces)
    {
        var before=Pr57366Before(v);
        RememberLaneManoeuvre(v,spaces);
        Pr57366After(v,"remember-lane",before);
    }
    void Pr57366Reserve(Vehicle v,RoadNode node,HashSet<int>? area)
    {
        var before=Pr57366Before(v);
        ReserveJunction(v,node,area);
        Pr57366After(v,"reserve-junction",before);
    }
    void Pr57366Finish()
    {
        if(!_pr57366Active||_pr57366Done)return;
        foreach(var v in State.Vehicles)
        {
            if(!_pr57366StartRows.TryGetValue(v,out var before)){_pr57366EndNew++;continue;}
            if(before!=Pr57366Capture(v))_pr57366EndNet++;
        }
        foreach(var v in _pr57366StartRows.Keys)
            if(!State.Vehicles.Contains(v))_pr57366EndMissing++;
        foreach(var (site,row) in _pr57366Sites.OrderBy(x=>x.Key))
            Console.WriteLine(
                $"PR57_366_SITE site={site} calls={row.Calls} changed={row.Changed} "+
                $"unique={row.Unique.Count} claim={row.Claim} spaces={row.Spaces} "+
                $"route={row.Route} sitePath={row.Site} dock={row.Dock} "+
                $"admission={row.Admission} detour={row.Detour} pose={row.Pose} "+
                "scope=SELECTED_IMMEDIATE_WRITER_BOUNDARIES");
        Console.WriteLine(
            $"PR57_366_SUMMARY canonicalInitial={_pr57366Canonical.Count} "+
            $"sites={_pr57366Sites.Count} comparisons={_pr57366Comparisons} "+
            $"noncanonical={_pr57366Noncanonical} changedIds={_pr57366ChangedIds.Count} "+
            $"endNetFromHourStart={_pr57366EndNet} endNew={_pr57366EndNew} "+
            $"endMissing={_pr57366EndMissing} "+
            $"captureMs={_pr57366CaptureTicks*1000.0/Stopwatch.Frequency:F3} "+
            "hour=14to15 scope=SELECTED_WRITERS_NOT_COMPLETE_JOURNAL "+
            "intermediateWithinMethodUnseen=True noSkippedSlices=True observational=True");
        _pr57366Active=false;_pr57366Done=true;
    }
}
''',encoding="utf-8")
print("PR57_366_INJECTED first active REAL hour immediate selected writer boundaries")
