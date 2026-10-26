namespace IndexCities.Simulation;

// PR57 #168: strictly observational and intentionally intrusive. This is a
// boundary audit, NOT an exact dependency tracker: transient writes undone
// before a boundary, other entity types, signal clocks and external edits
// remain outside its coverage. Never use these snapshots for gameplay.
public sealed partial class CitySimulation
{
    static readonly bool Pr57MutationAuditEnabled =
        Environment.GetEnvironmentVariable("PR57_VEHICLE_MUTATION_AUDIT") == "1";

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
                break;
            }
    }

    void Pr57MutationAfterVehicle(Vehicle actor)
    {
        if (!_pr57MutationSampleActive || _pr57MutationMirror is not { } old) return;
        _pr57MutationVehiclePasses++;
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
    }
}
