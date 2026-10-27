namespace IndexCities.Simulation;

// PR57 #168: strictly observational and intentionally intrusive. This is a
// boundary audit, NOT an exact dependency tracker: transient writes undone
// before a boundary, other entity types, signal clocks and external edits
// remain outside its coverage. Never use these snapshots for gameplay.
public sealed partial class CitySimulation
{
    static readonly bool Pr57MutationAuditEnabled =
        Environment.GetEnvironmentVariable("PR57_VEHICLE_MUTATION_AUDIT") == "1";

    // #173: deliberately intrusive sampled writer-closure falsification.
    // These IDs are only observational. The live physics never reads them.
    static readonly bool Pr57DirtyClosureProbeEnabled =
        Environment.GetEnvironmentVariable("PR57_DIRTY_CLOSURE_PROBE") == "1";
    HashSet<int>? _pr57DirtyTouched;
    long _pr57DirtyMarkCalls, _pr57DirtyMarkTicks, _pr57DirtyMarkBytes,
        _pr57DirtyUnique, _pr57DirtyActual, _pr57DirtyMissed,
        _pr57DirtyOvermarked, _pr57DirtyNewIds, _pr57DirtyPreQueryMisses;

    // The cost of this HashSet.Add is a LOWER BOUND for a physical index:
    // it does not update any payload or perform the expensive exact audit.
    // Stopwatch and allocation counters are included in the measured hook.
    void Pr57DirtyTouch(Vehicle? vehicle)
    {
        // #223: mirror-only journal of already marked potential writers.
        // No effect when the bounded dense probe is inactive.
        Pr57DenseNotePotentialWrite(vehicle);
        Pr57DockWriterTouch(vehicle); // #233: journal only already-marked potential writers
        if (!Pr57DirtyClosureProbeEnabled || !_pr57MutationSampleActive ||
            _pr57DirtyTouched is null || vehicle is null) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57DirtyTouched.Add(vehicle.Id);
        _pr57DirtyMarkTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57DirtyMarkBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
        _pr57DirtyMarkCalls++;
    }

    // Exact full-JSON verification is confined to the sampled audit, never to
    // a production read path. Overmarks are allowed; misses falsify closure.
    void Pr57DirtyCheck(Dictionary<int, string> previous, Dictionary<int, string> next)
    {
        if (!Pr57DirtyClosureProbeEnabled || _pr57DirtyTouched is null) return;
        var actual = new HashSet<int>();
        foreach (var (id, value) in next)
            if (!previous.TryGetValue(id, out var old) ||
                !string.Equals(old, value, StringComparison.Ordinal))
            {
                actual.Add(id);
                if (!previous.ContainsKey(id)) _pr57DirtyNewIds++;
            }
        foreach (var id in previous.Keys)
            if (!next.ContainsKey(id)) actual.Add(id);
        _pr57DirtyUnique += _pr57DirtyTouched.Count;
        _pr57DirtyActual += actual.Count;
        foreach (var id in actual)
            if (!_pr57DirtyTouched.Contains(id)) _pr57DirtyMissed++;
        foreach (var id in _pr57DirtyTouched)
            if (!actual.Contains(id)) _pr57DirtyOvermarked++;
        _pr57DirtyTouched.Clear();
    }

    Dictionary<int, string>? _pr57MutationMirror;
    bool _pr57MutationSampleActive;
    long _pr57MutationSlices, _pr57MutationSamples, _pr57MutationVehiclePasses,
        _pr57MutationSelfChangedPasses, _pr57MutationThirdPartyChangedPasses,
        _pr57MutationThirdPartyVehicleChanges, _pr57MutationUnchangedPasses,
        _pr57MutationBeforeQueryChecks, _pr57MutationBeforeQueryDivergences,
        _pr57MutationSyncVehicleChanges, _pr57MutationListChanges;

    // Full persisted Vehicle shape, including route, reservation collections,
    // towing, cargo and timers. Exact string comparison avoids hash collisions.
    // Serialize only 1/97 slices, and never report its wall as throughput.
    string Pr57MutationSnapshot(Vehicle v) =>
        System.Text.Json.JsonSerializer.Serialize(v, SaveOptions);

    Dictionary<int, string> Pr57MutationWorld() =>
        State.Vehicles.ToDictionary(v => v.Id, Pr57MutationSnapshot);

    void Pr57MutationBeginSlice()
    {
        _pr57MutationSampleActive = ++_pr57MutationSlices % 97 == 0;
        if (!_pr57MutationSampleActive) return;
        _pr57MutationSamples++;
        // Called after global junction/manoeuvre cleanup, before the ordered
        // vehicle loop. The cleanup is intentionally NOT considered independent.
        _pr57MutationMirror = Pr57MutationWorld();
        if (Pr57DirtyClosureProbeEnabled) _pr57DirtyTouched = new HashSet<int>();
    }

    void Pr57MutationBeforeLeaderScan(Vehicle actor)
    {
        if (!_pr57MutationSampleActive || _pr57MutationMirror is not { } old) return;
        _pr57MutationBeforeQueryChecks++;
        // Can a third-party vehicle have changed during the current actor's
        // processing, before the next physical read? Do not update the mirror.
        foreach (var v in State.Vehicles)
            if (v.Id != actor.Id &&
                (!old.TryGetValue(v.Id, out var prior) ||
                 !string.Equals(prior, Pr57MutationSnapshot(v), StringComparison.Ordinal)))
            {
                _pr57MutationBeforeQueryDivergences++;
                if (Pr57DirtyClosureProbeEnabled && _pr57DirtyTouched?.Contains(v.Id) != true)
                    _pr57DirtyPreQueryMisses++;
                break;
            }
    }

    void Pr57MutationAfterVehicle(Vehicle actor)
    {
        if (!_pr57MutationSampleActive || _pr57MutationMirror is not { } old) return;
        _pr57MutationVehiclePasses++;
        Pr57DirtyTouch(actor); // conservative actor writer, including unchanged passes
        var next = Pr57MutationWorld();
        var selfChanged = false;
        var thirdChanged = 0;
        foreach (var (id, snapshot) in next)
        {
            if (old.TryGetValue(id, out var previous) &&
                string.Equals(previous, snapshot, StringComparison.Ordinal)) continue;
            if (id == actor.Id) selfChanged = true;
            else thirdChanged++;
        }
        foreach (var id in old.Keys)
            if (!next.ContainsKey(id))
            {
                if (id == actor.Id) selfChanged = true;
                else thirdChanged++;
            }
        if (selfChanged) _pr57MutationSelfChangedPasses++;
        if (thirdChanged > 0)
        {
            _pr57MutationThirdPartyChangedPasses++;
            _pr57MutationThirdPartyVehicleChanges += thirdChanged;
        }
        if (!selfChanged && thirdChanged == 0) _pr57MutationUnchangedPasses++;
        if (old.Count != next.Count) _pr57MutationListChanges++;
        Pr57DirtyCheck(old, next);
        _pr57MutationMirror = next;
    }

    void Pr57MutationAfterSync()
    {
        if (!_pr57MutationSampleActive || _pr57MutationMirror is not { } old) return;
        var next = Pr57MutationWorld();
        foreach (var (id, snapshot) in next)
            if (!old.TryGetValue(id, out var previous) ||
                !string.Equals(previous, snapshot, StringComparison.Ordinal))
                _pr57MutationSyncVehicleChanges++;
        foreach (var id in old.Keys)
            if (!next.ContainsKey(id)) _pr57MutationSyncVehicleChanges++;
        Pr57DirtyCheck(old, next);
        _pr57DirtyTouched = null;
        _pr57MutationMirror = null;
        _pr57MutationSampleActive = false;
        Console.WriteLine(
            $"PR57_MUTATION_AUDIT slices={_pr57MutationSlices} samples={_pr57MutationSamples} " +
            $"passes={_pr57MutationVehiclePasses} selfChangedPasses={_pr57MutationSelfChangedPasses} " +
            $"thirdChangedPasses={_pr57MutationThirdPartyChangedPasses} " +
            $"thirdVehicleChanges={_pr57MutationThirdPartyVehicleChanges} " +
            $"unchangedPasses={_pr57MutationUnchangedPasses} " +
            $"beforeQueryChecks={_pr57MutationBeforeQueryChecks} " +
            $"beforeQueryDivergences={_pr57MutationBeforeQueryDivergences} " +
            $"syncVehicleChanges={_pr57MutationSyncVehicleChanges} " +
            $"listChanges={_pr57MutationListChanges} " +
            "sampled=1of97 observational=True exactDependencies=False");
        if (Pr57DirtyClosureProbeEnabled)
            Console.WriteLine(
                $"PR57_DIRTY_CLOSURE samples={_pr57MutationSamples} marks={_pr57DirtyMarkCalls} " +
                $"uniqueTouched={_pr57DirtyUnique} actualChanged={_pr57DirtyActual} " +
                $"missed={_pr57DirtyMissed} overmarked={_pr57DirtyOvermarked} " +
                $"newIds={_pr57DirtyNewIds} preQueryMisses={_pr57DirtyPreQueryMisses} " +
                $"markTicks={_pr57DirtyMarkTicks} markBytes={_pr57DirtyMarkBytes} " +
                "scope=towHooksPlusActor observational=True fullClosureProven=False");
    }
}
