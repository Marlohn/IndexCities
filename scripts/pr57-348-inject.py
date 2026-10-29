#!/usr/bin/env python3
"""PR57 #348: checkout-only stage-boundary effect census in authentic jam.

No canonical decisions, writes, order, actor selection, or sub-slices are changed.
Samples six physical slices inherited from #346; stage-boundary snapshots cannot
detect writes reversed *inside* a stage or mutations of list contents in place.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
residual = root / "src/IndexCities.Simulation/Mobility.ResidualExclusive.Pr57.cs"
mobility = root / "src/IndexCities.Simulation/Mobility.cs"

def replace_once(source: str, old: str, new: str, label: str) -> str:
    assert source.count(old) == 1, f"#348 anchor {label} expected once, got {source.count(old)}"
    return source.replace(old, new, 1)

s = residual.read_text(encoding="utf-8")
s = replace_once(s, "        _pr57ExclusiveOldPosition = vehicle.Position;\n",
                 "        _pr57ExclusiveOldPosition = vehicle.Position;\n        Pr57348Begin(vehicle);\n", "begin")
s = replace_once(s,
                 "        if (!_pr57ExclusiveVehicleActive) return;\n        var now = System.Diagnostics.Stopwatch.GetTimestamp();",
                 "        if (!_pr57ExclusiveVehicleActive) return;\n        Pr57348Mark(_pr57ExclusiveStage);\n        var now = System.Diagnostics.Stopwatch.GetTimestamp();", "mark")
s = replace_once(s, "        Pr57ExclusiveMark(0);\n        if (Pr57State244Enabled)",
                 "        Pr57ExclusiveMark(0);\n        Pr57348Finish(vehicle);\n        if (Pr57State244Enabled)", "finish")
s = replace_once(s, "        Pr57DenseEndSlice();\n        _pr57ExclusiveActive = false;",
                 "        Pr57DenseEndSlice();\n        Pr57348Report();\n        _pr57ExclusiveActive = false;", "report")
residual.write_text(s, encoding="utf-8")

s = mobility.read_text(encoding="utf-8")
s = replace_once(s,
                 "if(step<.00001&&d>.00001){\n                        // A distant leader",
                 "if(step<.00001&&d>.00001){\n                        Pr57348Blocked(v,blocker,enteringLane);\n                        // A distant leader", "blocked")
s = replace_once(s, "var bypassed=TryBypassStopped(v,blocker);",
                 "var bypassed=TryBypassStopped(v,blocker);\n                            Pr57348Bypass(bypassed);", "bypass")
mobility.write_text(s, encoding="utf-8")

cs = r'''namespace IndexCities.Simulation;

// PR57 #348: observational stage-boundary projections only. Not a causal
// readset, not a writer journal, not a speed optimization.
public sealed partial class CitySimulation
{
    static readonly bool Pr57348Enabled =
        Environment.GetEnvironmentVariable("PR57_348_STAGE_EFFECTS") == "1";

    readonly record struct Pr57348Snap(
        Point Position, double Heading, VehicleStatus Status, int RouteIndex,
        string Cause, double WaitingSince, int? IntersectionClaim,
        int RouteCount, int DetourCount, int ManoeuvreSpacesCount,
        int DockCorridorCount, int? AdmissionIndex, int? AdmissionRouteCount,
        TowPhase TowPhase, int? TowTargetId, int? TowTruckId, long FuelUnits);

    static readonly string[] Pr57348Fields =
    [
        "position", "heading", "status", "routeIndex", "cause",
        "waitingSince", "claim", "routeCount", "detourCount", "spacesCount",
        "corridorCount", "admissionIndex", "admissionRouteCount",
        "towPhase", "towTarget", "towTruck", "fuelUnits"
    ];
    static readonly string[] Pr57348Groups =
    [
        "persistent-still", "persistent-other", "other-travelling-still",
        "other-travelling-moved", "other"
    ];

    static Pr57348Snap Pr57348Read(Vehicle v) => new(
        v.Position, v.Heading, v.Status, v.RouteIndex, v.Cause,
        v.WaitingSince, v.IntersectionClaim, v.Route.Count, v.Detour.Count,
        v.ManoeuvreSpaces.Count, v.DockCorridor.Count,
        v.MovementAdmission?.EndRouteIndex, v.MovementAdmission?.Route.Count,
        v.TowPhase, v.TowTargetId, v.TowTruckId, v.FuelUnits);

    static int Pr57348Diff(Pr57348Snap a, Pr57348Snap b)
    {
        int mask = 0;
        if (!a.Position.Equals(b.Position)) mask |= 1 << 0;
        if (a.Heading != b.Heading) mask |= 1 << 1;
        if (a.Status != b.Status) mask |= 1 << 2;
        if (a.RouteIndex != b.RouteIndex) mask |= 1 << 3;
        if (a.Cause != b.Cause) mask |= 1 << 4;
        if (a.WaitingSince != b.WaitingSince) mask |= 1 << 5;
        if (a.IntersectionClaim != b.IntersectionClaim) mask |= 1 << 6;
        if (a.RouteCount != b.RouteCount) mask |= 1 << 7;
        if (a.DetourCount != b.DetourCount) mask |= 1 << 8;
        if (a.ManoeuvreSpacesCount != b.ManoeuvreSpacesCount) mask |= 1 << 9;
        if (a.DockCorridorCount != b.DockCorridorCount) mask |= 1 << 10;
        if (a.AdmissionIndex != b.AdmissionIndex) mask |= 1 << 11;
        if (a.AdmissionRouteCount != b.AdmissionRouteCount) mask |= 1 << 12;
        if (a.TowPhase != b.TowPhase) mask |= 1 << 13;
        if (a.TowTargetId != b.TowTargetId) mask |= 1 << 14;
        if (a.TowTruckId != b.TowTruckId) mask |= 1 << 15;
        if (a.FuelUnits != b.FuelUnits) mask |= 1 << 16;
        return mask;
    }

    Vehicle? _pr57348Actor;
    Pr57348Snap _pr57348Start, _pr57348Last;
    bool _pr57348StartedTravelling;
    long _pr57348Sample = -1;
    int _pr57348Boundaries, _pr57348Blocked, _pr57348WithBlocker;
    int _pr57348Entering, _pr57348BypassAttempts, _pr57348BypassSucceeded;
    readonly int[,] _pr57348Current = new int[9, 17];
    readonly long[,] _pr57348StageFields = new long[5, 9];
    readonly long[,] _pr57348FieldsByGroup = new long[5, 17];
    readonly long[,] _pr57348NetFieldsByGroup = new long[5, 17];
    readonly long[] _pr57348Actors = new long[5];
    readonly long[] _pr57348ChangedActors = new long[5];
    readonly long[] _pr57348NetActors = new long[5];
    readonly long[] _pr57348ChangedBoundaries = new long[5];
    readonly long[] _pr57348BlockedByGroup = new long[5];
    readonly long[] _pr57348WithBlockerByGroup = new long[5];
    readonly long[] _pr57348EnteringByGroup = new long[5];
    readonly long[] _pr57348BypassByGroup = new long[5];
    readonly long[] _pr57348BypassSucceededByGroup = new long[5];

    void Pr57348Begin(Vehicle v)
    {
        if (!Pr57348Enabled || !_pr57ExclusiveActive) return;
        if (_pr57348Sample != _pr57ExclusiveSamples)
        {
            _pr57348Sample = _pr57ExclusiveSamples;
            Array.Clear(_pr57348StageFields);
            Array.Clear(_pr57348FieldsByGroup);
            Array.Clear(_pr57348NetFieldsByGroup);
            foreach (var array in new[]
            {
                _pr57348Actors, _pr57348ChangedActors, _pr57348NetActors,
                _pr57348ChangedBoundaries, _pr57348BlockedByGroup,
                _pr57348WithBlockerByGroup, _pr57348EnteringByGroup,
                _pr57348BypassByGroup, _pr57348BypassSucceededByGroup
            }) Array.Clear(array);
        }
        _pr57348Actor = v;
        _pr57348Start = _pr57348Last = Pr57348Read(v);
        _pr57348StartedTravelling = v.Status == VehicleStatus.Travelling;
        _pr57348Boundaries = _pr57348Blocked = _pr57348WithBlocker = 0;
        _pr57348Entering = _pr57348BypassAttempts = _pr57348BypassSucceeded = 0;
        Array.Clear(_pr57348Current);
    }

    void Pr57348Mark(int stage)
    {
        if (!Pr57348Enabled || _pr57348Actor is null) return;
        var now = Pr57348Read(_pr57348Actor);
        var mask = Pr57348Diff(_pr57348Last, now);
        if (mask != 0) _pr57348Boundaries++;
        for (var i = 0; i < Pr57348Fields.Length; i++)
            if ((mask & (1 << i)) != 0) _pr57348Current[stage, i]++;
        _pr57348Last = now;
    }

    void Pr57348Blocked(Vehicle v, Vehicle? blocker, bool entering)
    {
        if (!Pr57348Enabled || !ReferenceEquals(_pr57348Actor, v)) return;
        _pr57348Blocked++;
        if (blocker is not null) _pr57348WithBlocker++;
        if (entering) _pr57348Entering++;
    }

    void Pr57348Bypass(bool succeeded)
    {
        if (!Pr57348Enabled || _pr57348Actor is null) return;
        _pr57348BypassAttempts++;
        if (succeeded) _pr57348BypassSucceeded++;
    }

    void Pr57348Finish(Vehicle v)
    {
        if (!Pr57348Enabled || !ReferenceEquals(_pr57348Actor, v)) return;
        var now = Pr57348Read(v);
        var netMask = Pr57348Diff(_pr57348Start, now);
        var moved = v.Position.Distance(_pr57348Start.Position) > 1e-7;
        var travelling = _pr57348StartedTravelling && v.Status == VehicleStatus.Travelling;
        var persistent = Pr57State244PersistentIds.Contains(v.Id);
        var group = persistent ? (travelling && !moved ? 0 : 1)
            : travelling ? (moved ? 3 : 2) : 4;
        _pr57348Actors[group]++;
        if (_pr57348Boundaries > 0) _pr57348ChangedActors[group]++;
        if (netMask != 0) _pr57348NetActors[group]++;
        _pr57348ChangedBoundaries[group] += _pr57348Boundaries;
        _pr57348BlockedByGroup[group] += _pr57348Blocked;
        _pr57348WithBlockerByGroup[group] += _pr57348WithBlocker;
        _pr57348EnteringByGroup[group] += _pr57348Entering;
        _pr57348BypassByGroup[group] += _pr57348BypassAttempts;
        _pr57348BypassSucceededByGroup[group] += _pr57348BypassSucceeded;
        for (var stage = 0; stage < 9; stage++)
            for (var i = 0; i < Pr57348Fields.Length; i++)
            {
                var n = _pr57348Current[stage, i];
                _pr57348StageFields[group, stage] += n;
                _pr57348FieldsByGroup[group, i] += n;
            }
        for (var i = 0; i < Pr57348Fields.Length; i++)
            if ((netMask & (1 << i)) != 0) _pr57348NetFieldsByGroup[group, i]++;
        if (group is 0 or 2)
            Console.WriteLine(
                $"PR57_348_ACTOR sample={_pr57348Sample} id={v.Id} group={Pr57348Groups[group]} " +
                $"changedBoundaries={_pr57348Boundaries} netFields={System.Numerics.BitOperations.PopCount((uint)netMask)} " +
                $"blocked={_pr57348Blocked} blocker={_pr57348WithBlocker} entering={_pr57348Entering} " +
                $"bypass={_pr57348BypassAttempts} bypassSuccess={_pr57348BypassSucceeded} " +
                "scope=STAGE_BOUNDARIES_ONLY");
        _pr57348Actor = null;
    }

    void Pr57348Report()
    {
        if (!Pr57348Enabled || _pr57348Sample != _pr57ExclusiveSamples) return;
        for (var g = 0; g < 5; g++)
        {
            var stages = string.Join(",", Enumerable.Range(0, 9)
                .Select(s => $"{s}:{_pr57348StageFields[g, s]}"));
            var fields = string.Join(",", Enumerable.Range(0, Pr57348Fields.Length)
                .Select(i => $"{Pr57348Fields[i]}:{_pr57348FieldsByGroup[g, i]}"));
            var netFields = string.Join(",", Enumerable.Range(0, Pr57348Fields.Length)
                .Select(i => $"{Pr57348Fields[i]}:{_pr57348NetFieldsByGroup[g, i]}"));
            Console.WriteLine(
                $"PR57_348_EFFECT sample={_pr57348Sample} group={Pr57348Groups[g]} " +
                $"actors={_pr57348Actors[g]} changedActors={_pr57348ChangedActors[g]} " +
                $"netActors={_pr57348NetActors[g]} changedBoundaries={_pr57348ChangedBoundaries[g]} " +
                $"blocked={_pr57348BlockedByGroup[g]} blocker={_pr57348WithBlockerByGroup[g]} " +
                $"entering={_pr57348EnteringByGroup[g]} bypass={_pr57348BypassByGroup[g]} " +
                $"bypassSuccess={_pr57348BypassSucceededByGroup[g]} " +
                $"stages={stages} fields={fields} netFields={netFields} " +
                "scope=STAGE_BOUNDARIES_NOT_IMMEDIATE_WRITES observational=True intrusive=True");
        }
    }
}
'''
out = root / "src/IndexCities.Simulation/Mobility.StateEffects348.Pr57.cs"
assert not out.exists(), "#348 source already exists; do not overwrite"
out.write_text(cs, encoding="utf-8")
print("PR57_348_INJECTED: six authentic slices, per-actor boundary effects and blocked/bypass calls")
