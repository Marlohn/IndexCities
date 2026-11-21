namespace IndexCities.Simulation;

// PR57 #231: bounded actor-major s3 candidate projection audit.
// This is an observational shadow, NOT a cache, dependency closure or
// replacement for the live canonical candidate. Every Advance(2), candidate,
// movement, event and save follows the original code. The projection omits
// list contents, docks, roads, signals, pedestrians, time and external writers.
public sealed partial class CitySimulation
{
    static readonly bool Pr57ActorShadowEnabled =
        Environment.GetEnvironmentVariable("PR57_231_ACTOR_SHADOW") == "1";

    readonly Dictionary<int, Pr57PhaseExposureSnapshot> _pr57ActorShadow = new();
    readonly HashSet<int> _pr57ActorShadowProcessed = new();
    readonly HashSet<int> _pr57ActorShadowStaleIds = new();
    bool _pr57ActorShadowActive;
    long _pr57ActorShadowReads, _pr57ActorShadowStable, _pr57ActorShadowFrozen;
    long _pr57ActorShadowStaleSelf, _pr57ActorShadowStaleThird;
    long _pr57ActorShadowStaleAfterProcessed, _pr57ActorShadowStaleBeforeProcessed;
    long _pr57ActorShadowMissing, _pr57ActorShadowRefreshes;
    long _pr57ActorShadowFinalMismatches, _pr57ActorShadowBuildTicks;
    long _pr57ActorShadowRefreshTicks, _pr57ActorShadowBuildBytes;

    void Pr57ActorShadowBeginSlice()
    {
        if (!Pr57ActorShadowEnabled || !_pr57ExclusiveActive) return;
        Pr57ActorShadowBuild();
    }

    // Private test seam: no environment flag needed for synthetic counterexamples.
    void Pr57ActorShadowBuild()
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57ActorShadowActive = true;
        _pr57ActorShadow.Clear();
        _pr57ActorShadowProcessed.Clear();
        _pr57ActorShadowStaleIds.Clear();
        _pr57ActorShadowReads = _pr57ActorShadowStable = _pr57ActorShadowFrozen = 0;
        _pr57ActorShadowStaleSelf = _pr57ActorShadowStaleThird = 0;
        _pr57ActorShadowStaleAfterProcessed = _pr57ActorShadowStaleBeforeProcessed = 0;
        _pr57ActorShadowMissing = _pr57ActorShadowRefreshes = 0;
        _pr57ActorShadowFinalMismatches = _pr57ActorShadowRefreshTicks = 0;
        foreach (var v in State.Vehicles)
            _pr57ActorShadow[v.Id] = Pr57PhaseSnapshot(v);
        _pr57ActorShadowBuildTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57ActorShadowBuildBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57ActorShadowRead(Vehicle reader, Vehicle candidate, bool live)
    {
        if (!_pr57ActorShadowActive) return;
        // The canonical entry frame may intentionally be a different object.
        if (!live) { _pr57ActorShadowFrozen++; return; }
        _pr57ActorShadowReads++;
        if (!_pr57ActorShadow.TryGetValue(candidate.Id, out var shadow))
        {
            _pr57ActorShadowMissing++;
            return;
        }
        // This per-read canonical comparison is an expensive ORACLE only.
        // It must not be retained in an actual fast execution path.
        if (shadow == Pr57PhaseSnapshot(candidate))
        {
            _pr57ActorShadowStable++;
            return;
        }
        _pr57ActorShadowStaleIds.Add(candidate.Id);
        if (reader.Id == candidate.Id) _pr57ActorShadowStaleSelf++;
        else
        {
            _pr57ActorShadowStaleThird++;
            if (_pr57ActorShadowProcessed.Contains(candidate.Id))
                _pr57ActorShadowStaleAfterProcessed++;
            else _pr57ActorShadowStaleBeforeProcessed++;
        }
        // Do NOT repair on read: this distinguishes a missing writer hook
        // from the candidate's later own actor refresh.
    }

    void Pr57ActorShadowAfterActor(Vehicle actor)
    {
        if (!_pr57ActorShadowActive) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57ActorShadow[actor.Id] = Pr57PhaseSnapshot(actor);
        _pr57ActorShadowProcessed.Add(actor.Id);
        _pr57ActorShadowRefreshes++;
        _pr57ActorShadowRefreshTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57ActorShadowEndSlice()
    {
        if (!_pr57ActorShadowActive) return;
        // Single O(N) post-sync audit; no census after each actor.
        foreach (var v in State.Vehicles)
            if (!_pr57ActorShadow.TryGetValue(v.Id, out var shadow) ||
                shadow != Pr57PhaseSnapshot(v))
                _pr57ActorShadowFinalMismatches++;
        static double Ms(long ticks) =>
            Math.Round(ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency, 6);
        Console.WriteLine(
            "PR57_231_ACTOR_SHADOW sample=" + _pr57ExclusiveSamples +
            " vehicles=" + _pr57ActorShadow.Count +
            " reads=" + _pr57ActorShadowReads +
            " stable=" + _pr57ActorShadowStable +
            " frozen=" + _pr57ActorShadowFrozen +
            " staleSelf=" + _pr57ActorShadowStaleSelf +
            " staleThird=" + _pr57ActorShadowStaleThird +
            " staleAfterProcessed=" + _pr57ActorShadowStaleAfterProcessed +
            " staleBeforeProcessed=" + _pr57ActorShadowStaleBeforeProcessed +
            " staleIds=" + _pr57ActorShadowStaleIds.Count +
            " missing=" + _pr57ActorShadowMissing +
            " refreshes=" + _pr57ActorShadowRefreshes +
            " finalMismatches=" + _pr57ActorShadowFinalMismatches +
            " buildMs=" + Ms(_pr57ActorShadowBuildTicks) +
            " refreshMs=" + Ms(_pr57ActorShadowRefreshTicks) +
            " buildMB=" + (_pr57ActorShadowBuildBytes / 1e6).ToString("F6") +
            " selectedFieldsOnly=True perReadOracle=True noDecisionWrites=True" +
            " externalAndTransientWritersNotClosed=True intrusive=True");
        _pr57ActorShadowActive = false;
        _pr57ActorShadow.Clear();
        _pr57ActorShadowProcessed.Clear();
        _pr57ActorShadowStaleIds.Clear();
    }
}
