#!/usr/bin/env python3
"""PR57 #329: inject bounded, opt-in physical-candidate observation in CI checkout only."""
from pathlib import Path

def inject(path, old, new):
    p = Path(path)
    s = p.read_text()
    assert s.count(old) == 1, (path, old, s.count(old))
    p.write_text(s.replace(old, new, 1))

inject("src/IndexCities.Simulation/Mobility.LaneEntry.cs",
       "        var pr57300Call=pr57300 is null?0:++_pr57300Calls;",
       "        var pr57300Call=pr57300 is null?0:++_pr57300Calls;\n"
       "        if(Pr57329Enabled && pr57300 is null) ++_pr57300Calls;")
inject("src/IndexCities.Simulation/Mobility.cs",
       "        Pr57JunctionReadersAfterCleanup();",
       "        Pr57JunctionReadersAfterCleanup();\n        Pr57329BeginSlice(dt,operationsDt);")
inject("src/IndexCities.Simulation/Mobility.cs",
       "                Pr57ActorShadowAfterActor(v);",
       "                Pr57ActorShadowAfterActor(v);\n                Pr57329AfterActor();")
inject("src/IndexCities.Simulation/Mobility.cs",
       "        SyncTowedVehicles();",
       "        SyncTowedVehicles();\n        Pr57329AfterSync();")
inject("src/IndexCities.Simulation/Mobility.cs",
       "        Pr57JunctionReadersEndSlice();",
       "        Pr57329EndSlice();\n        Pr57JunctionReadersEndSlice();")
inject("src/IndexCities.Simulation/Mobility.SharedSpace.cs",
       "        Pr57261Hit(0);",
       "        Pr57261Hit(0);\n        Pr57329BodyCall();")
inject("src/IndexCities.Simulation/Mobility.SharedSpace.cs",
       "            if(census){visited++;Pr57306Read(v,o);}",
       "            if(census){visited++;Pr57306Read(v,o);}\n            Pr57329Visit(v,o,false);")
inject("src/IndexCities.Simulation/Mobility.SharedSpace.cs",
       "            if(census)eligible++;",
       "            if(census)eligible++;\n            Pr57329Visit(v,o,true);")

out = Path("src/IndexCities.Simulation/Mobility.PhysicalExposure329.Pr57.cs")
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// #329: read-time and actor-boundary physical signature exposure, opt-in ONLY.
// This is NOT a canonical mutation journal, exact dependency graph, or cache.
// It misses transient writes within an actor and other physical/citizen readers.
public sealed partial class CitySimulation
{
    static readonly bool Pr57329Enabled =
        Environment.GetEnvironmentVariable("PR57_329_PHYSICAL_EXPOSURE") == "1";
    readonly record struct Pr57329State(
        Point Position, long HeadingBits, VehicleStatus Status,
        int? TowTruckId, int? TowTargetId, TruckBody Body,
        PavingMachine Machine, bool PassengerCar);
    static Pr57329State Pr57329Snapshot(Vehicle v) =>
        new(v.Position, BitConverter.DoubleToInt64Bits(v.Heading),
            v.Status, v.TowTruckId, v.TowTargetId,
            v.Body, v.Machine, v.PassengerCar);

    bool _pr57329Active;
    long _pr57329Call = -1;
    int _pr57329Slice;
    long _pr57329BodyCalls, _pr57329Visits, _pr57329EligibleVisits;
    long _pr57329ReadTransitions, _pr57329ActorTransitions, _pr57329SyncTransitions;
    long _pr57329PoseChanges, _pr57329StatusChanges, _pr57329TowChanges, _pr57329ShapeChanges;
    long _pr57329VisitTicks, _pr57329BoundaryTicks;
    readonly Dictionary<int,Pr57329State> _pr57329Last = new();
    readonly Dictionary<int,HashSet<int>> _pr57329Readers = new();
    readonly Dictionary<int,HashSet<int>> _pr57329EligibleReaders = new();
    readonly HashSet<(int Candidate,int Reader)> _pr57329Dirty = new();
    readonly HashSet<(int Candidate,int Reader)> _pr57329EligibleDirty = new();
    readonly HashSet<int> _pr57329ChangedIds = new();

    void Pr57329BeginSlice(double dt, double? operationsDt)
    {
        _pr57329Active = false;
        if (!Pr57329Enabled || dt <= 0 || operationsDt is not null) return;
        if (_pr57329Call != _pr57300Calls)
        {
            _pr57329Call = _pr57300Calls;
            _pr57329Slice = 0;
        }
        _pr57329Slice++;
        var real = State.Vehicles.Count <= 150;
        var selected = real
            ? _pr57300Calls is >= 76 and <= 100
            : (_pr57300Calls is >= 46 and <= 50) && _pr57329Slice == 1;
        if (!selected) return;
        _pr57329Active = true;
        _pr57329BodyCalls = _pr57329Visits = _pr57329EligibleVisits = 0;
        _pr57329ReadTransitions = _pr57329ActorTransitions = _pr57329SyncTransitions = 0;
        _pr57329PoseChanges = _pr57329StatusChanges = _pr57329TowChanges = _pr57329ShapeChanges = 0;
        _pr57329VisitTicks = _pr57329BoundaryTicks = 0;
        _pr57329Last.Clear();
        _pr57329Readers.Clear();
        _pr57329EligibleReaders.Clear();
        _pr57329Dirty.Clear();
        _pr57329EligibleDirty.Clear();
        _pr57329ChangedIds.Clear();
    }

    void Pr57329BodyCall()
    {
        if (_pr57329Active) _pr57329BodyCalls++;
    }

    void Pr57329MarkChanged(int id, Pr57329State old, Pr57329State now)
    {
        _pr57329ChangedIds.Add(id);
        if (old.Position != now.Position || old.HeadingBits != now.HeadingBits)
            _pr57329PoseChanges++;
        if (old.Status != now.Status) _pr57329StatusChanges++;
        if (old.TowTruckId != now.TowTruckId || old.TowTargetId != now.TowTargetId)
            _pr57329TowChanges++;
        if (old.Body != now.Body || old.Machine != now.Machine ||
            old.PassengerCar != now.PassengerCar) _pr57329ShapeChanges++;
        if (_pr57329Readers.TryGetValue(id,out var readers))
            foreach (var reader in readers) _pr57329Dirty.Add((id,reader));
        if (_pr57329EligibleReaders.TryGetValue(id,out var eligible))
            foreach (var reader in eligible) _pr57329EligibleDirty.Add((id,reader));
    }

    void Pr57329Visit(Vehicle reader, Vehicle candidate, bool eligible)
    {
        if (!_pr57329Active) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        if (eligible)
        {
            _pr57329EligibleVisits++;
            if (!_pr57329EligibleReaders.TryGetValue(candidate.Id,out var selected))
                _pr57329EligibleReaders[candidate.Id] = selected = new HashSet<int>();
            selected.Add(reader.Id);
        }
        else
        {
            _pr57329Visits++;
            var now = Pr57329Snapshot(candidate);
            if (_pr57329Last.TryGetValue(candidate.Id,out var old) && old != now)
            {
                _pr57329ReadTransitions++;
                Pr57329MarkChanged(candidate.Id,old,now);
            }
            _pr57329Last[candidate.Id] = now;
            if (!_pr57329Readers.TryGetValue(candidate.Id,out var readers))
                _pr57329Readers[candidate.Id] = readers = new HashSet<int>();
            readers.Add(reader.Id);
        }
        _pr57329VisitTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57329ObserveBoundary(bool afterSync)
    {
        if (!_pr57329Active) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        foreach (var candidate in State.Vehicles)
        {
            if (!_pr57329Last.TryGetValue(candidate.Id,out var old)) continue;
            var now = Pr57329Snapshot(candidate);
            if (old == now) continue;
            if (afterSync) _pr57329SyncTransitions++;
            else _pr57329ActorTransitions++;
            Pr57329MarkChanged(candidate.Id,old,now);
            _pr57329Last[candidate.Id] = now;
        }
        _pr57329BoundaryTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }
    void Pr57329AfterActor() => Pr57329ObserveBoundary(false);
    void Pr57329AfterSync() => Pr57329ObserveBoundary(true);

    void Pr57329EndSlice()
    {
        if (!_pr57329Active) return;
        var allEdges = _pr57329Readers.Values.Sum(x => x.Count);
        var eligibleEdges = _pr57329EligibleReaders.Values.Sum(x => x.Count);
        var hz = (double)System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_329_EXPOSURE call={_pr57300Calls} slice={_pr57329Slice} " +
            $"vehicles={State.Vehicles.Count} bodyCalls={_pr57329BodyCalls} " +
            $"visits={_pr57329Visits} eligibleVisits={_pr57329EligibleVisits} " +
            $"allEdges={allEdges} eligibleEdges={eligibleEdges} " +
            $"readTransitions={_pr57329ReadTransitions} actorTransitions={_pr57329ActorTransitions} " +
            $"syncTransitions={_pr57329SyncTransitions} changedCandidates={_pr57329ChangedIds.Count} " +
            $"poseChanges={_pr57329PoseChanges} statusChanges={_pr57329StatusChanges} " +
            $"towChanges={_pr57329TowChanges} shapeChanges={_pr57329ShapeChanges} " +
            $"dirtyAllEdges={_pr57329Dirty.Count} dirtyEligibleEdges={_pr57329EligibleDirty.Count} " +
            $"visitMs={_pr57329VisitTicks*1000/hz:F6} " +
            $"boundaryMs={_pr57329BoundaryTicks*1000/hz:F6} " +
            "scope=PARTIAL_SIGNATURE_READTIME_AND_ACTOR_BOUNDARY_NOT_CANONICAL_WRITES " +
            "observational=True noCausalClosure=True");
        _pr57329Active = false;
    }
}
''')
print("PR57_329_INJECTED anchors=8 observational=True noEngineBranchChange=True")
