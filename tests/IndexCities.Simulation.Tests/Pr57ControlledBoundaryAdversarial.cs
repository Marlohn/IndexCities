using System.Diagnostics;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 ONLY. Lateral controlled-state boundary over an authentic engine save.
// The production CitySimulation API and its public mutable State are unchanged.
// This proves a restricted external-command contract, NOT internal writer
// invalidation or a cross-Advance scheduler.
public sealed class Pr57ControlledBoundaryAdversarial
{
    abstract record Command;
    sealed record Hunger(int Id, long Units) : Command;
    sealed record Price(int Id, decimal Value) : Command;
    sealed record FoodStock(int Id, long Units) : Command;
    sealed record ReverseRoads() : Command;
    sealed record ReverseCitizens() : Command;
    readonly record struct BuildingView(int Id, decimal SalePrice, long FoodUnits);
    readonly record struct CitizenView(int Id, long HungerUnits, long HealthUnits);
    readonly record struct JournalEntry(long Version, string Kind);

    static void Apply(CityState state, Command command)
    {
        switch (command)
        {
            case Hunger h:
                state.Citizens.Single(c => c.Id == h.Id).HungerMicroseconds = h.Units;
                break;
            case Price p:
                state.Buildings.Single(b => b.Id == p.Id).SalePrice = p.Value;
                break;
            case FoodStock f:
                state.Buildings.Single(b => b.Id == f.Id).Stock.Units[ResourceKind.Food] = f.Units;
                break;
            default:
                // List reorder/replacement requires the real engine's graph
                // and entity-index invalidation protocol. Not proven here.
                throw new NotSupportedException("External list reorder is outside this boundary POC");
        }
    }

    sealed class ControlledBoundary
    {
        readonly CitySimulation _engine;
        readonly List<JournalEntry> _journal = [];
        long _version;

        // Clone ONCE at ingress. Caller may retain and arbitrarily mutate its
        // original CityState; it cannot access the private engine or its lists.
        public ControlledBoundary(CityState callerOwned)
        {
            var json = new CitySimulation(callerOwned).SaveToJson();
            _engine = CitySimulation.FromJson(json);
        }

        public long Version => _version;
        public JournalEntry[] Journal() => _journal.ToArray();
        public BuildingView[] Buildings() => _engine.Buildings
            .Select(b => new BuildingView(b.Id, b.SalePrice,
                b.Stock.Units.GetValueOrDefault(ResourceKind.Food))).ToArray();
        public CitizenView[] Citizens() => _engine.State.Citizens
            .Select(c => new CitizenView(c.Id, c.HungerMicroseconds, c.HealthUnits)).ToArray();
        public void Execute(Command command)
        {
            Apply(_engine.State, command);
            _journal.Add(new JournalEntry(++_version, command.GetType().Name));
        }
        public void Advance() => _engine.Advance(2);
        public string Save() => _engine.SaveToJson();
    }

    [Fact]
    public void Authentic_state_aliases_and_commands_have_byte_exact_engine_parity()
    {
        var path = Environment.GetEnvironmentVariable("PR57_CONTROLLED_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return; // opt-in CI, not a default fixture
        var label = Environment.GetEnvironmentVariable("PR57_CONTROLLED_CASE") ?? "unspecified";
        var initial = File.ReadAllText(path);
        var baseline = CitySimulation.FromJson(initial);
        var caller = CitySimulation.FromJson(initial);
        var callerOwned = caller.State;
        Assert.NotEmpty(callerOwned.Citizens);
        Assert.NotEmpty(callerOwned.Buildings);
        Assert.NotEmpty(callerOwned.Roads);

        var cloneTicks = Stopwatch.GetTimestamp();
        var cloneBytes = GC.GetAllocatedBytesForCurrentThread();
        var boundary = new ControlledBoundary(callerOwned);
        var cloneMs = Stopwatch.GetElapsedTime(cloneTicks).TotalMilliseconds;
        var cloneAlloc = GC.GetAllocatedBytesForCurrentThread() - cloneBytes;
        var pristine = boundary.Save();
        Assert.Equal(baseline.SaveToJson(), pristine);

        // Adversarial retained constructor alias, same-Count replacement,
        // list replacement/reorder, nested dictionary and public Buildings alias.
        callerOwned.Citizens[0].HungerMicroseconds += 100000;
        callerOwned.Citizens[0].History.Clear();
        callerOwned.Citizens = callerOwned.Citizens.ToList();
        callerOwned.Citizens.Reverse();
        callerOwned.Citizens[0] = new Citizen { Id = callerOwned.Citizens[0].Id };
        callerOwned.Roads.Reverse();
        callerOwned.Buildings[0].Stock.Units[ResourceKind.Food] =
            callerOwned.Buildings[0].Stock.Units.GetValueOrDefault(ResourceKind.Food) + 123;
        caller.Buildings[0].SalePrice += 100m;
        ((List<Building>)caller.Buildings).Reverse();
        Assert.Equal(pristine, boundary.Save());

        // Read-model contains only value types; no mutable Building/Stock/Citizen.
        var views = boundary.Buildings();
        views[0] = views[0] with { SalePrice = 999999m };
        var people = boundary.Citizens();
        people[0] = people[0] with { HungerUnits = -999 };
        Assert.Equal(pristine, boundary.Save());

        var citizen = baseline.State.Citizens[0];
        var building = baseline.State.Buildings[0];
        Command[] commands =
        [
            new Hunger(citizen.Id, citizen.HungerMicroseconds + 1),
            new Price(building.Id, building.SalePrice + .01m),
            new FoodStock(building.Id,
                building.Stock.Units.GetValueOrDefault(ResourceKind.Food) + 1)
        ];
        foreach (var command in commands)
        {
            Apply(baseline.State, command);
            boundary.Execute(command);
        }
        Assert.Throws<NotSupportedException>(() => boundary.Execute(new ReverseRoads()));
        Assert.Throws<NotSupportedException>(() => boundary.Execute(new ReverseCitizens()));
        Assert.Equal((long)commands.Length, boundary.Version);
        Assert.Equal(commands.Length, boundary.Journal().Length);
        Assert.Equal(baseline.SaveToJson(), boundary.Save());

        // Both paths run the unmodified integrated engine: all Advance(2),
        // including construction, congestion, economy, history and save/load.
        var checkpoints = 1;
        for (var i = 1; i <= 120; i++)
        {
            baseline.Advance(2);
            boundary.Advance();
            if (i % 10 != 0) continue;
            var actual = boundary.Save();
            Assert.Equal(baseline.SaveToJson(), actual);
            Assert.Equal(actual, CitySimulation.FromJson(actual).SaveToJson());
            checkpoints++;
        }

        // Separate boundary-maintenance costs, not a claim of engine speedup.
        var direct = CitySimulation.FromJson(initial);
        var candidate = new ControlledBoundary(CitySimulation.FromJson(initial).State);
        var id = direct.State.Buildings[0].Id;
        var original = direct.State.Buildings[0].SalePrice;
        const int edits = 300;
        var directStart = Stopwatch.GetTimestamp();
        var directAllocStart = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < edits; i++)
            Apply(direct.State, new Price(id, original + (i % 2 == 0 ? .01m : .02m)));
        var directMs = Stopwatch.GetElapsedTime(directStart).TotalMilliseconds;
        var directAlloc = GC.GetAllocatedBytesForCurrentThread() - directAllocStart;
        var journalStart = Stopwatch.GetTimestamp();
        var journalAllocStart = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < edits; i++)
            candidate.Execute(new Price(id, original + (i % 2 == 0 ? .01m : .02m)));
        var journalMs = Stopwatch.GetElapsedTime(journalStart).TotalMilliseconds;
        var journalAlloc = GC.GetAllocatedBytesForCurrentThread() - journalAllocStart;
        Assert.Equal(direct.SaveToJson(), candidate.Save());

        var readStart = Stopwatch.GetTimestamp();
        var readAllocStart = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 30; i++)
        {
            _ = candidate.Buildings();
            _ = candidate.Citizens();
        }
        var readMs = Stopwatch.GetElapsedTime(readStart).TotalMilliseconds;
        var readAlloc = GC.GetAllocatedBytesForCurrentThread() - readAllocStart;
        var epochStart = Stopwatch.GetTimestamp();
        long checksum = 0;
        for (var i = 0; i < 1800; i++) checksum += candidate.Version;
        var epochMs = Stopwatch.GetElapsedTime(epochStart).TotalMilliseconds;
        Assert.True(checksum > 0);
        Console.WriteLine($"PR57_CONTROLLED_BOUNDARY case={label} citizens={baseline.State.Citizens.Count} buildings={baseline.State.Buildings.Count} roads={baseline.State.Roads.Count} checkpoints={checkpoints} byteEqual=True aliasIsolation=True commandJournal={candidate.Version} cloneMs={cloneMs:F4} cloneAllocBytes={cloneAlloc} direct300Ms={directMs:F4} direct300AllocBytes={directAlloc} journal300Ms={journalMs:F4} journal300AllocBytes={journalAlloc} read30Ms={readMs:F4} read30AllocBytes={readAlloc} epoch1800Ms={epochMs:F4} internalWriterJournal=False engineSpeedupClaim=False");
    }

    // PR57 isolated negative gate. The engine's existing in-band needs probe
    // observes real post-hunger inputs; it is NOT a complete internal-writer
    // journal. Full SaveToJson each public boundary is an intentionally
    // expensive, conservative detector, never a proposed production scheduler.
    [Fact]
    public void Real_boundary_full_snapshot_cost_and_needs_change_gate()
    {
        var path = Environment.GetEnvironmentVariable("PR57_CONTROLLED_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_CONTROLLED_CASE") ?? "unspecified";
        var initial = File.ReadAllText(path);
        var baseline = CitySimulation.FromJson(initial);
        var observed = CitySimulation.FromJson(initial);
        var needs = new Pr57NeedsStepProbe();
        observed.Pr57NeedsStepProbe = needs;

        const int steps = 120;
        var baselineCheckpoints = new List<string>();
        long baselineTicks = 0, baselineAlloc = 0;
        for (var i = 1; i <= steps; i++)
        {
            var started = Stopwatch.GetTimestamp();
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            baseline.Advance(2);
            baselineTicks += Stopwatch.GetTimestamp() - started;
            baselineAlloc += GC.GetAllocatedBytesForCurrentThread() - bytes;
            if (i % 10 == 0) baselineCheckpoints.Add(baseline.SaveToJson());
        }

        long observedTicks = 0, observedAlloc = 0;
        long fingerprintTicks = 0, fingerprintAlloc = 0;
        string? previous = null;
        int changedFingerprints = 0, checkpointCount = 0;
        for (var i = 1; i <= steps; i++)
        {
            var started = Stopwatch.GetTimestamp();
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            observed.Advance(2);
            observedTicks += Stopwatch.GetTimestamp() - started;
            observedAlloc += GC.GetAllocatedBytesForCurrentThread() - bytes;

            started = Stopwatch.GetTimestamp();
            bytes = GC.GetAllocatedBytesForCurrentThread();
            var snapshot = observed.SaveToJson();
            fingerprintTicks += Stopwatch.GetTimestamp() - started;
            fingerprintAlloc += GC.GetAllocatedBytesForCurrentThread() - bytes;
            if (!string.Equals(previous, snapshot, StringComparison.Ordinal))
                changedFingerprints++;
            previous = snapshot;
            if (i % 10 != 0) continue;
            Assert.Equal(baselineCheckpoints[checkpointCount++], snapshot);
            Assert.Equal(snapshot, CitySimulation.FromJson(snapshot).SaveToJson());
        }

        Assert.Equal(steps, needs.Records.Count);
        Assert.Equal(steps, changedFingerprints); // clock alone changes each save
        Assert.Equal(steps / 10, checkpointCount);
        Assert.Equal(baseline.SaveToJson(), observed.SaveToJson());

        long needsVisits = needs.Records.Sum(x => (long)x.Citizens);
        long hungerChanged = needs.Records.Sum(x => (long)x.ChangedHunger);
        long stableHomeInputs = needs.Records.Sum(x => (long)x.SameHomeInputs);
        long changedHomeInputs = needs.Records.Sum(x => (long)x.ChangedHomeInputs);
        Assert.True(needsVisits > 0);
        double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_CONTROLLED_GATE case={label} steps={steps} checkpoints={checkpointCount} " +
            $"byteEqual=True fingerprintChanged={changedFingerprints} " +
            $"baselineAdvanceMs={Ms(baselineTicks):F3} baselineAdvanceAlloc={baselineAlloc} " +
            $"needsProbeAdvanceMs={Ms(observedTicks):F3} needsProbeAdvanceAlloc={observedAlloc} " +
            $"fullFingerprintMs={Ms(fingerprintTicks):F3} fullFingerprintAlloc={fingerprintAlloc} " +
            $"needsVisits={needsVisits} changedHungerBetweenSteps={hungerChanged} " +
            $"stableHomeInputs={stableHomeInputs} changedHomeInputs={changedHomeInputs} " +
            $"internalWriterJournal=False safeSkippedSteps=0 engineSpeedupClaim=False");
    }
}
