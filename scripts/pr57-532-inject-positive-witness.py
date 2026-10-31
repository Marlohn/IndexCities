#!/usr/bin/env python3
"""PR57 #532: disposable exact-positive-body-witness POC.

Only JunctionPreferenceAllows -> CanTurnNow is opted in. A previous blocker
is merely a hint: every hit must revalidate its membership, current eligibility,
current contact limit, and an actual collision against the freshly generated
TurnWay poses. No cached false decision, pose or global slice is reused.
The identity of the first blocker is diagnostic-only in CanTurnNow; its boolean
return is preserved. This is not a general-purpose BodyOnWay replacement.
"""
from pathlib import Path

root = Path("src/IndexCities.Simulation")
mobility = root / "Mobility.cs"
shared = root / "Mobility.SharedSpace.cs"

def replace_once(path, old, new):
    s = path.read_text(encoding="utf-8")
    assert s.count(old) == 1, (str(path), old[:120], s.count(old))
    path.write_text(s.replace(old, new, 1), encoding="utf-8")

replace_once(
    mobility,
    "&&o.Position.Distance(junction.Position)<14&&CanTurnNow(o)).ToList();",
    "&&o.Position.Distance(junction.Position)<14&&Pr57532CanTurn(o)).ToList();",
)
replace_once(
    shared,
    "if(poses.Count==0)return null;var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;",
    """if(poses.Count==0)return null;var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;
        if(Pr57532TryWitness(v,poses,reach) is {} pr57532Blocker)return pr57532Blocker;""",
)
replace_once(
    shared,
    """if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);
                    return o;""",
    """if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);
                    Pr57532Remember(v,o);
                    return o;""",
)
replace_once(
    mobility,
    "        Pr57JunctionCensusEndSlice();",
    "        Pr57532EndSlice();\n        Pr57JunctionCensusEndSlice();",
)

(root / "Pr57532PositiveWitness.CiOnly.cs").write_text(r'''using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexCities.Simulation;

// Experimental shortcut for a boolean reader only. The original TurnWay is
// always regenerated; current physical inputs are checked at each attempted
// hit. There is no inference from a stationary reader or an unchanged slice.
public sealed partial class CitySimulation
{
    static readonly bool Pr57532Enabled =
        Environment.GetEnvironmentVariable("PR57_532_POSITIVE_WITNESS") == "1";

    readonly Dictionary<int, Vehicle> _pr57532Witness = new();
    Vehicle? _pr57532Candidate;
    long _pr57532Attempts, _pr57532Hits, _pr57532Misses, _pr57532Records;
    long _pr57532Calls, _pr57532Slices;

    bool Pr57532CanTurn(Vehicle candidate)
    {
        if (!Pr57532Enabled) return CanTurnNow(candidate);
        _pr57532Calls++;
        var prior = _pr57532Candidate;
        _pr57532Candidate = candidate;
        try { return CanTurnNow(candidate); }
        finally { _pr57532Candidate = prior; }
    }

    Vehicle? Pr57532TryWitness(
        Vehicle v, List<(Vehicle Body, Point Position, double Heading)> poses,
        double reach)
    {
        if (!Pr57532Enabled || !ReferenceEquals(_pr57532Candidate, v) ||
            !_pr57532Witness.TryGetValue(v.Id, out var o)) return null;
        _pr57532Attempts++;
        // Membership is checked by reference, not ID or count: removed/replaced
        // vehicles must never be treated as physical blockers.
        if (!State.Vehicles.Any(current => ReferenceEquals(current, o)))
        {
            _pr57532Witness.Remove(v.Id);
            _pr57532Misses++;
            return null;
        }
        if (o.Id != v.Id && o.Id != v.TowTargetId &&
            o.TowTruckId != v.Id &&
            o.Status is not (VehicleStatus.Outside or VehicleStatus.Towed) &&
            o.Position.Distance(v.Position) <= reach &&
            !(AtGateway(o) && AtGateway(v)))
        {
            // Identical collision predicate and contact-depth rule as BodyOnWay.
            // A previously blocked body can become unblocked: always fall back.
            var now = Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);
            var limit = Math.Max(-.1,now+.001);
            foreach (var p in poses)
                if (p.Position.Distance(o.Position)<12 &&
                    Penetration(v,p.Position,p.Heading,o,o.Position,o.Heading)>limit)
                {
                    _pr57532Hits++;
                    return o;
                }
        }
        _pr57532Misses++;
        return null;
    }

    void Pr57532Remember(Vehicle v, Vehicle blocker)
    {
        if (!Pr57532Enabled || !ReferenceEquals(_pr57532Candidate,v)) return;
        _pr57532Witness[v.Id] = blocker;
        _pr57532Records++;
    }

    void Pr57532EndSlice()
    {
        if (!Pr57532Enabled) return;
        _pr57532Slices++;
        if (_pr57532Slices == 1 || _pr57532Slices % 300 == 0)
            Console.WriteLine($"PR57_532_WITNESS_SUM slices={_pr57532Slices} " +
                $"calls={_pr57532Calls} attempts={_pr57532Attempts} " +
                $"hits={_pr57532Hits} misses={_pr57532Misses} " +
                $"records={_pr57532Records} distinct={_pr57532Witness.Count} " +
                "scope=PREFERENCE_CANTURN_BOOLEAN_ONLY exactPositiveRecheck=True " +
                "allSlicesPreserved=True noGlobalCertificate=True");
    }
}
''', encoding="utf-8")
print("PR57_532_DISPOSABLE_EXACT_WITNESS_INJECTION_PASS")
