using System.Diagnostics;

namespace IndexCities.Simulation;

// PR57 #200: bounded, observation-only shadow of *reservation membership*.
// Rebuilt after the real all-vehicle cleanup in ONE physical sub-slice; the
// existing #198 explicit writer callsites update it. The authoritative state
// still drives every physical decision. A full oracle checks it at admissions.
// Missing direct/nonlocal writers are recorded, NOT silently reconciled.
// No cache crosses Advance(2), no physics, rules, or saves are changed.
public sealed partial class CitySimulation
{
    static readonly bool Pr57ReservationShadowEnabled =
        Environment.GetEnvironmentVariable("PR57_200_RESERVATION_SHADOW") == "1";
    bool _pr57ShadowActive, _pr57ShadowDone;
    readonly Dictionary<int,(int? Claim,int[] Spaces)> _pr57ShadowStates = new();
    readonly Dictionary<int,HashSet<int>> _pr57ShadowClaims = new();
    readonly Dictionary<int,HashSet<int>> _pr57ShadowSpaces = new();
    readonly HashSet<int> _pr57ShadowStaleIds = new();
    long _pr57ShadowBuildTicks, _pr57ShadowHookTicks, _pr57ShadowOracleTicks;
    long _pr57ShadowHooks, _pr57ShadowChanges, _pr57ShadowAdmissions;
    long _pr57ShadowOracleVehicles, _pr57ShadowStaleReads;
    long _pr57ShadowMissingCandidates, _pr57ShadowExtraCandidates;
    long _pr57ShadowIndexedCandidates, _pr57ShadowActualCandidates;
    int _pr57ShadowMaxIndexed, _pr57ShadowMaxActual;
    string _pr57ShadowFirstStale = "none";

    static void Pr57ShadowAdd(Dictionary<int,HashSet<int>> index,int node,int id)
    {
        if (!index.TryGetValue(node,out var ids))
            index[node] = ids = new HashSet<int>();
        ids.Add(id);
    }

    static void Pr57ShadowRemove(Dictionary<int,HashSet<int>> index,int node,int id)
    {
        if (index.TryGetValue(node,out var ids) && ids.Remove(id) && ids.Count==0)
            index.Remove(node);
    }

    void Pr57ReservationShadowBegin()
    {
        if (!Pr57ReservationShadowEnabled || !_pr57JrActive || _pr57ShadowDone ||
            _pr57JrTime < 3611.05 || _pr57JrTime >= 3611.06) return;
        var started = Stopwatch.GetTimestamp();
        _pr57ShadowStates.Clear();
        _pr57ShadowClaims.Clear();
        _pr57ShadowSpaces.Clear();
        _pr57ShadowStaleIds.Clear();
        foreach (var v in State.Vehicles)
        {
            var state = (Claim:v.IntersectionClaim,Spaces:v.ManoeuvreSpaces.ToArray());
            _pr57ShadowStates.Add(v.Id,state);
            if (state.Claim is {} claim) Pr57ShadowAdd(_pr57ShadowClaims,claim,v.Id);
            foreach (var node in state.Spaces) Pr57ShadowAdd(_pr57ShadowSpaces,node,v.Id);
        }
        _pr57ShadowBuildTicks += Stopwatch.GetTimestamp()-started;
        _pr57ShadowActive = true;
    }

    // Called only at explicit #198 engine writer sites, including when the
    // expensive #197 snapshot observer is disabled. This is NOT complete:
    // #199 proves direct writes from towing, road edits and public setters.
    void Pr57ReservationShadowWrite(Vehicle v)
    {
        if (!_pr57ShadowActive) return;
        var started=Stopwatch.GetTimestamp();
        _pr57ShadowHooks++;
        var now=(Claim:v.IntersectionClaim,Spaces:v.ManoeuvreSpaces.ToArray());
        var existed=_pr57ShadowStates.TryGetValue(v.Id,out var before);
        if (!existed || before.Claim!=now.Claim || !before.Spaces.SequenceEqual(now.Spaces))
        {
            _pr57ShadowChanges++;
            if (existed)
            {
                if (before.Claim is {} oldClaim) Pr57ShadowRemove(_pr57ShadowClaims,oldClaim,v.Id);
                foreach (var node in before.Spaces) Pr57ShadowRemove(_pr57ShadowSpaces,node,v.Id);
            }
            _pr57ShadowStates[v.Id]=now;
            if (now.Claim is {} claim) Pr57ShadowAdd(_pr57ShadowClaims,claim,v.Id);
            foreach (var node in now.Spaces) Pr57ShadowAdd(_pr57ShadowSpaces,node,v.Id);
        }
        _pr57ShadowHookTicks += Stopwatch.GetTimestamp()-started;
    }

    void Pr57ReservationShadowAdmission(RoadNode junction)
    {
        if (!_pr57ShadowActive) return;
        _pr57ShadowAdmissions++;
        Pr57ReservationShadowCheck(junction);
    }

    // The full scan is deliberately an independent *oracle*, not an index
    // maintenance mechanism. Compare all IDs and all reservation values, not
    // only positive blockers, to expose stale negatives as well as positives.
    void Pr57ReservationShadowCheck(RoadNode? junction)
    {
        if (!_pr57ShadowActive) return;
        var started=Stopwatch.GetTimestamp();
        var indexed = new HashSet<int>();
        var actual = new HashSet<int>();
        HashSet<int>? area = junction is null ? null :
            _junctionAreas.GetValueOrDefault(junction.Id);
        if (junction is not null)
        {
            void Include(int node)
            {
                if (_pr57ShadowClaims.TryGetValue(node,out var claims))
                    indexed.UnionWith(claims);
                if (_pr57ShadowSpaces.TryGetValue(node,out var spaces))
                    indexed.UnionWith(spaces);
            }
            if (area is null) Include(junction.Id);
            else foreach (var node in area) Include(node);
        }
        foreach (var v in State.Vehicles)
        {
            _pr57ShadowOracleVehicles++;
            if (!_pr57ShadowStates.TryGetValue(v.Id,out var stored) ||
                stored.Claim!=v.IntersectionClaim ||
                !stored.Spaces.SequenceEqual(v.ManoeuvreSpaces))
            {
                _pr57ShadowStaleReads++;
                _pr57ShadowStaleIds.Add(v.Id);
                if (_pr57ShadowFirstStale=="none")
                    _pr57ShadowFirstStale=$"id:{v.Id},time:{_pr57JrTime:F9}";
            }
            if (junction is null) continue;
            var claim=v.IntersectionClaim is {} c &&
                (area?.Contains(c) ?? c==junction.Id);
            var space=v.ManoeuvreSpaces.Any(node=>
                area?.Contains(node) ?? node==junction.Id);
            if (claim || space) actual.Add(v.Id);
        }
        if (junction is not null)
        {
            _pr57ShadowIndexedCandidates+=indexed.Count;
            _pr57ShadowActualCandidates+=actual.Count;
            _pr57ShadowMaxIndexed=Math.Max(_pr57ShadowMaxIndexed,indexed.Count);
            _pr57ShadowMaxActual=Math.Max(_pr57ShadowMaxActual,actual.Count);
            _pr57ShadowMissingCandidates+=actual.Count(id=>!indexed.Contains(id));
            _pr57ShadowExtraCandidates+=indexed.Count(id=>!actual.Contains(id));
        }
        _pr57ShadowOracleTicks+=Stopwatch.GetTimestamp()-started;
    }

    void Pr57ReservationShadowEnd()
    {
        if (!_pr57ShadowActive) return;
        Pr57ReservationShadowCheck(null); // also catches writes after last admission
        var indexedIds=_pr57ShadowStates.Count;
        var nodes=_pr57ShadowClaims.Keys.Concat(_pr57ShadowSpaces.Keys).Distinct().Count();
        var scale=1000.0/Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_200_RESERVATION_SHADOW time={_pr57JrTime:F9} " +
            $"admissions={_pr57ShadowAdmissions} initialVehicles={indexedIds} " +
            $"indexedNodes={nodes} hookCalls={_pr57ShadowHooks} hookChanges={_pr57ShadowChanges} " +
            $"oracleVehicleChecks={_pr57ShadowOracleVehicles} staleReads={_pr57ShadowStaleReads} " +
            $"distinctStaleIds={_pr57ShadowStaleIds.Count} firstStale={_pr57ShadowFirstStale} " +
            $"missingCandidateReads={_pr57ShadowMissingCandidates} extraCandidateReads={_pr57ShadowExtraCandidates} " +
            $"indexedCandidatesSum={_pr57ShadowIndexedCandidates} actualCandidatesSum={_pr57ShadowActualCandidates} " +
            $"maxIndexedCandidates={_pr57ShadowMaxIndexed} maxActualCandidates={_pr57ShadowMaxActual} " +
            $"buildMs={_pr57ShadowBuildTicks*scale:F3} hookMs={_pr57ShadowHookTicks*scale:F3} " +
            $"oracleMs={_pr57ShadowOracleTicks*scale:F3} " +
            "scope=ONE_SLICE_SHADOW_RESERVATIONS_ONLY_NOT_EXACT_PHYSICAL_GRAPH_NOT_AB");
        _pr57ShadowActive=false;
        _pr57ShadowDone=true;
    }
}
