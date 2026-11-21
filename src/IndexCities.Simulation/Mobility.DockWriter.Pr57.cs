namespace IndexCities.Simulation;

// PR57 #233: dock-only, read-only actor-major writer/readset audit.
// Builds once after canonical cleanup, refreshes the processed actor and
// existing Pr57DirtyTouch writers, then checks canonical BodyAcrossLane reads.
// Does NOT substitute a predicate or prove a complete writer closure.
// External public edits and unmarked writers remain deliberately uncovered.
public sealed partial class CitySimulation
{
    static readonly bool Pr57DockWriterEnabled =
        Environment.GetEnvironmentVariable("PR57_233_DOCK_WRITER") == "1";

    readonly Dictionary<int, (Vehicle Ref, Point? Dock)> _pr57DockWriterShadow = new();
    readonly HashSet<Vehicle> _pr57DockWriterTouched = new();
    bool _pr57DockWriterActive;
    long _pr57DockWriterReads, _pr57DockWriterStable, _pr57DockWriterFrozen;
    long _pr57DockWriterStaleSelf, _pr57DockWriterStaleThird, _pr57DockWriterMissing;
    long _pr57DockWriterRefreshes, _pr57DockWriterActorChanges;
    long _pr57DockWriterJournalTouches, _pr57DockWriterJournalRefreshes;
    long _pr57DockWriterJournalChanges, _pr57DockWriterFinalMisses;
    long _pr57DockWriterBuildTicks, _pr57DockWriterRefreshTicks, _pr57DockWriterBuildBytes;

    void Pr57DockWriterBeginSlice()
    {
        if (Pr57DockWriterEnabled && _pr57ExclusiveActive) Pr57DockWriterBuild();
    }

    // Test seam, independent of the environment flag.
    void Pr57DockWriterBuild()
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57DockWriterActive = true;
        _pr57DockWriterShadow.Clear();
        _pr57DockWriterTouched.Clear();
        _pr57DockWriterReads = _pr57DockWriterStable = _pr57DockWriterFrozen = 0;
        _pr57DockWriterStaleSelf = _pr57DockWriterStaleThird = _pr57DockWriterMissing = 0;
        _pr57DockWriterRefreshes = _pr57DockWriterActorChanges = 0;
        _pr57DockWriterJournalTouches = _pr57DockWriterJournalRefreshes = 0;
        _pr57DockWriterJournalChanges = _pr57DockWriterFinalMisses = 0;
        _pr57DockWriterRefreshTicks = 0;
        foreach (var v in State.Vehicles)
            _pr57DockWriterShadow[v.Id] = (v, v.DockPosition);
        _pr57DockWriterBuildTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57DockWriterBuildBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57DockWriterRead(Vehicle reader, Vehicle candidate)
    {
        if (!_pr57DockWriterActive) return;
        if (!_pr57DockWriterShadow.TryGetValue(candidate.Id, out var shadow))
        {
            _pr57DockWriterMissing++;
            return;
        }
        // Entry-frame snapshots are intentionally frozen and are not live reads.
        if (!ReferenceEquals(shadow.Ref, candidate))
        {
            _pr57DockWriterFrozen++;
            return;
        }
        _pr57DockWriterReads++;
        if (shadow.Dock == candidate.DockPosition) _pr57DockWriterStable++;
        else if (reader.Id == candidate.Id) _pr57DockWriterStaleSelf++;
        else _pr57DockWriterStaleThird++;
        // No repair on read: a missing writer hook must remain observable.
    }

    void Pr57DockWriterTouch(Vehicle? vehicle)
    {
        if (!_pr57DockWriterActive || vehicle is null) return;
        _pr57DockWriterJournalTouches++;
        _pr57DockWriterTouched.Add(vehicle);
    }

    void Pr57DockWriterFlushJournal()
    {
        if (!_pr57DockWriterActive) return;
        foreach (var vehicle in _pr57DockWriterTouched)
        {
            _pr57DockWriterJournalRefreshes++;
            var now = vehicle.DockPosition;
            if (!_pr57DockWriterShadow.TryGetValue(vehicle.Id, out var old) ||
                !ReferenceEquals(old.Ref, vehicle) || old.Dock != now)
                _pr57DockWriterJournalChanges++;
            _pr57DockWriterShadow[vehicle.Id] = (vehicle, now);
        }
        _pr57DockWriterTouched.Clear();
    }

    void Pr57DockWriterAfterActor(Vehicle actor)
    {
        if (!_pr57DockWriterActive) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        if (!_pr57DockWriterShadow.TryGetValue(actor.Id, out var old) ||
            !ReferenceEquals(old.Ref, actor) || old.Dock != actor.DockPosition)
            _pr57DockWriterActorChanges++;
        _pr57DockWriterShadow[actor.Id] = (actor, actor.DockPosition);
        _pr57DockWriterRefreshes++;
        Pr57DockWriterFlushJournal(); // includes nonlocal tow targets marked by DirtyTouch
        _pr57DockWriterRefreshTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57DockWriterEndSlice()
    {
        if (!_pr57DockWriterActive) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        Pr57DockWriterFlushJournal(); // SyncTowedVehicles can write other vehicles
        // O(N) final oracle once per sampled slice, NOT after every actor.
        foreach (var v in State.Vehicles)
            if (!_pr57DockWriterShadow.TryGetValue(v.Id, out var old) ||
                !ReferenceEquals(old.Ref, v) || old.Dock != v.DockPosition)
                _pr57DockWriterFinalMisses++;
        _pr57DockWriterRefreshTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        static double Ms(long ticks) =>
            Math.Round(ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency, 6);
        Console.WriteLine(
            "PR57_233_DOCK_WRITER sample=" + _pr57ExclusiveSamples +
            " vehicles=" + _pr57DockWriterShadow.Count +
            " reads=" + _pr57DockWriterReads +
            " stable=" + _pr57DockWriterStable +
            " staleSelf=" + _pr57DockWriterStaleSelf +
            " staleThird=" + _pr57DockWriterStaleThird +
            " missing=" + _pr57DockWriterMissing +
            " frozen=" + _pr57DockWriterFrozen +
            " refreshes=" + _pr57DockWriterRefreshes +
            " actorChanges=" + _pr57DockWriterActorChanges +
            " journalTouches=" + _pr57DockWriterJournalTouches +
            " journalRefreshes=" + _pr57DockWriterJournalRefreshes +
            " journalChanges=" + _pr57DockWriterJournalChanges +
            " finalMisses=" + _pr57DockWriterFinalMisses +
            " buildMs=" + Ms(_pr57DockWriterBuildTicks) +
            " refreshMs=" + Ms(_pr57DockWriterRefreshTicks) +
            " buildMB=" + (_pr57DockWriterBuildBytes / 1e6).ToString("F6") +
            " dockOnly=True readOnly=True perReadOracle=True " +
            " publicEditsNotHooked=True fullDependencyClosure=False intrusive=True");
        _pr57DockWriterActive = false;
        _pr57DockWriterShadow.Clear();
        _pr57DockWriterTouched.Clear();
    }
}
