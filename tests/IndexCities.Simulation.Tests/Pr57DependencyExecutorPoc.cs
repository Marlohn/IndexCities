using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 ONLY: executable, opt-in integrated dependency executor scaffold.
// Conservative by design: until internal writer coverage is complete, no
// phase may be skipped. This is a falsifiable M0, NOT a speedup claim.
public sealed class Pr57DependencyExecutorPoc
{
    private enum Domain { CitizenNeeds, BuildingEconomy, PhysicalMovement, Construction }
    private readonly record struct Command(Domain Domain, int EntityId, long Value, decimal Money = 0);
    private readonly record struct ExecutionCost(long RequiredSteps, long AvoidedBoundaryRebuilds,
        long ExternalInvalidations, long FallbackSteps, long Commands);

    private sealed class Executor
    {
        private CitySimulation _engine;
        private readonly Queue<Command> _pending = new();
        private long _required, _invalidations, _fallback, _commands, _avoided;
        private bool _dirty = true;
        private readonly bool _certified;
        public Executor(string json)
        {
            _engine = CitySimulation.FromJson(json);
            _certified = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_M1") == "1";
        }
        public string Snapshot() => _engine.SaveToJson();
        // Explicit persistence boundary: no internal agenda survives reload.
        // Rebuild from the serialized state and require canonical Advance(2)
        // before certified quanta resume. Pending commands must not be lost.
        public void Reload()
        {
            if (_pending.Count != 0)
                throw new InvalidOperationException("Cannot reload with pending external commands");
            _engine = CitySimulation.FromJson(_engine.SaveToJson());
            _dirty = true;
        }

        public ExecutionCost Cost => new(_required, _avoided, _invalidations, _fallback, _commands);

        public void SubmitNeeds(int id, long hunger)
        {
            _pending.Enqueue(new Command(Domain.CitizenNeeds, id, hunger));
            _dirty = true;
        }

        public void SubmitPrice(int id, decimal price)
        {
            _pending.Enqueue(new Command(Domain.BuildingEconomy, id, 0, price));
            _dirty = true;
        }

        public void SubmitFoodStock(int id, long units)
        {
            _pending.Enqueue(new Command(Domain.Construction, id, units));
            _dirty = true;
        }

        // M0 supports only three explicit external edits. No aliases escape.
        // No mutable engine aliases are ever returned.
        public void Advance()
        {
            while (_pending.TryDequeue(out var command))
            {
                switch (command.Domain)
                {
                    case Domain.CitizenNeeds:
                        _engine.State.Citizens.Single(c => c.Id == command.EntityId)
                            .HungerMicroseconds = command.Value;
                        break;
                    case Domain.BuildingEconomy:
                        _engine.State.Buildings.Single(b => b.Id == command.EntityId)
                            .SalePrice = command.Money;
                        break;
                    case Domain.Construction:
                        _engine.State.Buildings.Single(b => b.Id == command.EntityId)
                            .Stock.Units[ResourceKind.Food] = command.Value;
                        break;
                    default: throw new NotSupportedException("Uncertified external mutation");
                }
                _commands++;
                _invalidations++;
            }
            // Invalidation graph is deliberately conservative: needs may
            // influence citizen travel, business activity and economic effects.
            // Without a complete internal-writer contract the dependency closure
            // includes all 13 original phases. Preserve exact causal order.
            // M1: controlled internal boundary. Reuse incremental citizen and
            // maintenance agendas only after a canonical public Advance has
            // certified the state; any queued external edit forces fallback.
            // All 13 phases and both seconds still execute in original order.
            if (_certified && !_dirty)
            {
                _engine.Pr57AdvanceCertifiedQuantum();
                _avoided++; // avoided PUBLIC agenda rebuild, not a simulation Step
            }
            else
            {
                _engine.Advance(2);
                _fallback++;
            }
            _required++;
            _dirty = false;
        }
    }


    // Isolated parity gate for the empty municipal-relocation fastpath.
    // The flag is process-global: compare trajectories sequentially, never
    // toggle it while another test is using the simulation.
    [Fact]
    public void Authentic_saves_empty_relocation_fastpath_exact_causal_gate()
    {
        var path = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_CASE") ?? "unknown";
        var original = File.ReadAllText(path);
        var old = Environment.GetEnvironmentVariable("PR57_EMPTY_RELOCATION_FASTPATH");
        try
        {
            (string[] Checkpoints, long WallTicks, long AllocBytes, long CpuTicks, long EmptySteps, long NonEmptySteps) Run(bool fastpath)
            {
                Environment.SetEnvironmentVariable("PR57_EMPTY_RELOCATION_FASTPATH", fastpath ? "1" : null);
                var engine = CitySimulation.FromJson(original);
                var checkpoints = new string[120];
                long wallTicks = 0, allocated = 0, cpuTicks = 0;
                long emptySteps = 0, nonEmptySteps = 0;
                for (var step = 1; step <= 120; step++)
                {
                    if (step == 31)
                    {
                        engine.State.Citizens[0].HungerMicroseconds++;
                        engine.State.Buildings[0].SalePrice += .01m;
                        var stock = engine.State.Buildings[0].Stock.Units;
                        stock[ResourceKind.Food] = stock.GetValueOrDefault(ResourceKind.Food) + 1;
                    }
                    if (step == 71)
                        engine = CitySimulation.FromJson(engine.SaveToJson());
                    if (engine.State.MunicipalRelocations.Count == 0) emptySteps++;
                    else nonEmptySteps++;
                    var bytesBefore = GC.GetAllocatedBytesForCurrentThread();
                    var cpuBefore = Process.GetCurrentProcess().TotalProcessorTime.Ticks;
                    var started = Stopwatch.GetTimestamp();
                    engine.Advance(2);
                    wallTicks += Stopwatch.GetTimestamp() - started;
                    cpuTicks += Process.GetCurrentProcess().TotalProcessorTime.Ticks - cpuBefore;
                    allocated += GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
                    checkpoints[step - 1] = engine.SaveToJson();
                }
                return (checkpoints, wallTicks, allocated, cpuTicks, emptySteps, nonEmptySteps);
            }
            // ABBA isolates this single opt-in, unlike the broader structural
            // workflow's alternate mode which enables several unrelated flags.
            // Checkpoints are outside the measured Advance(2) region.
            var a1 = Run(false);
            var b1 = Run(true);
            var b2 = Run(true);
            var a2 = Run(false);
            foreach (var trial in new[] { b1, b2, a2 })
                for (var i = 0; i < a1.Checkpoints.Length; i++)
                    Assert.True(string.Equals(a1.Checkpoints[i], trial.Checkpoints[i], StringComparison.Ordinal),
                        $"PR57_EMPTY_RELOCATION_DIVERGENCE case={label} step={i + 1}");
            Assert.True(b1.EmptySteps > 0, $"PR57_EMPTY_RELOCATION_NOT_EXERCISED case={label}");
            Assert.Equal(a1.EmptySteps, b1.EmptySteps);
            Assert.Equal(a1.NonEmptySteps, b1.NonEmptySteps);
            Assert.Equal(a1.EmptySteps, b2.EmptySteps);
            Assert.Equal(a1.NonEmptySteps, b2.NonEmptySteps);
            Assert.Equal(a1.EmptySteps, a2.EmptySteps);
            Assert.Equal(a1.NonEmptySteps, a2.NonEmptySteps);
            var baselineTicks = (a1.WallTicks + a2.WallTicks) / 2.0;
            var alternateTicks = (b1.WallTicks + b2.WallTicks) / 2.0;
            var speedup = baselineTicks / Math.Max(1, alternateTicks);
            Console.WriteLine($"PR57_EMPTY_RELOCATION_PARITY case={label} steps=120 byteEqual=True externalEdits=3 saveLoadAt=71 comparison=sequential-same-process-ABBA");
            Console.WriteLine($"PR57_EMPTY_RELOCATION_ABBA case={label} baselineWallMs={baselineTicks * 1000 / Stopwatch.Frequency:F3} alternateWallMs={alternateTicks * 1000 / Stopwatch.Frequency:F3} speedup={speedup:F5} baselineAllocBytes={(a1.AllocBytes + a2.AllocBytes) / 2} alternateAllocBytes={(b1.AllocBytes + b2.AllocBytes) / 2} baselineCpuTicks={(a1.CpuTicks + a2.CpuTicks) / 2} alternateCpuTicks={(b1.CpuTicks + b2.CpuTicks) / 2} trials=2-per-mode sameProcess=True speedupCertified=False scope=single-empty-relocation-fastpath emptySteps={b1.EmptySteps} nonEmptyFallbackSteps={b1.NonEmptySteps} maxAvoidableFractionOfSteps={(double)b1.EmptySteps / 120:F5}");

        }
        finally
        {
            Environment.SetEnvironmentVariable("PR57_EMPTY_RELOCATION_FASTPATH", old);
        }
    }

    // Independent structural candidate: amortize public-boundary invalidations
    // across ten *real* 2-second quanta. Every internal phase still executes.
    // Reject immediately if the boundary has observable causal semantics.
    [Fact]
    public void Authentic_saves_batched_public_boundary_exact_causal_gate()
    {
        var path = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_CASE") ?? "unknown";
        var json = File.ReadAllText(path);
        var baseline = CitySimulation.FromJson(json);
        var candidate = CitySimulation.FromJson(json);
        // A boundary transformation must also survive external changes and
        // persistence, not merely match an unmodified trajectory.
        // Mutate both engines at the same PUBLIC boundary, never mid-quantum.
        long baselineTicks = 0, alternateTicks = 0;
        long baselineBytes = 0, alternateBytes = 0;
        for (var batch = 1; batch <= 12; batch++)
        {
            if (batch == 4)
            {
                var citizenId = baseline.State.Citizens[0].Id;
                var buildingId = baseline.State.Buildings[0].Id;
                var hunger = baseline.State.Citizens[0].HungerMicroseconds + 1;
                var price = baseline.State.Buildings[0].SalePrice + .01m;
                var stock = baseline.State.Buildings[0].Stock.Units.GetValueOrDefault(ResourceKind.Food) + 1;
                foreach (var engine in new[] { baseline, candidate })
                {
                    engine.State.Citizens.Single(c => c.Id == citizenId).HungerMicroseconds = hunger;
                    var building = engine.State.Buildings.Single(b => b.Id == buildingId);
                    building.SalePrice = price;
                    building.Stock.Units[ResourceKind.Food] = stock;
                }
            }
            if (batch == 8)
            {
                baseline = CitySimulation.FromJson(baseline.SaveToJson());
                candidate = CitySimulation.FromJson(candidate.SaveToJson());
            }
            var before = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            for (var i = 0; i < 10; i++) baseline.Advance(2);
            baselineTicks += Stopwatch.GetTimestamp() - started;
            baselineBytes += GC.GetAllocatedBytesForCurrentThread() - before;

            before = GC.GetAllocatedBytesForCurrentThread();
            started = Stopwatch.GetTimestamp();
            candidate.Advance(20);
            alternateTicks += Stopwatch.GetTimestamp() - started;
            alternateBytes += GC.GetAllocatedBytesForCurrentThread() - before;

            var expected = baseline.SaveToJson();
            var actual = candidate.SaveToJson();
            var equal = string.Equals(expected, actual, StringComparison.Ordinal);
            Console.WriteLine($"PR57_BATCH_BOUNDARY_CHECKPOINT case={label} step={batch * 10} byteEqual={equal} sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(actual)))}");
            Assert.True(equal, $"Public-boundary batching changed full JSON at step {batch * 10}; reject this candidate");
        }
        Console.WriteLine($"PR57_BATCH_BOUNDARY_COST case={label} baselineWallMs={baselineTicks * 1000.0 / Stopwatch.Frequency:F3} alternateWallMs={alternateTicks * 1000.0 / Stopwatch.Frequency:F3} baselineAllocBytes={baselineBytes} alternateAllocBytes={alternateBytes} speedup={(double)baselineTicks / Math.Max(1, alternateTicks):F5} publicBoundariesAvoided=108 internalQuantaAvoided=0 causalPhasesAvoided=0 externalEdits=3 saveLoadAtBatch=8 comparison=paired-in-process-not-ABBA");
    }

    // Same-process differential gate: isolates certified-boundary semantics from
    // cross-process hash instability. Both engines have identical static opt-ins;
    // only the PUBLIC versus CERTIFIED entry point differs.
    [Fact]
    public void Authentic_saves_certified_boundary_same_process_every_quantum()
    {
        var path = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        Assert.Equal("1", Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER"));
        Assert.Equal("1", Environment.GetEnvironmentVariable("PR57_CITIZEN_AGENDA"));
        var label = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_CASE") ?? "unknown";
        var original = File.ReadAllText(path);
        var baseline = CitySimulation.FromJson(original);
        var candidate = CitySimulation.FromJson(original);
        Assert.NotEmpty(baseline.State.Citizens);
        Assert.NotEmpty(baseline.State.Vehicles);
        Assert.NotEmpty(baseline.State.Buildings);
        long publicSteps = 0, certifiedSteps = 0;
        for (var step = 1; step <= 120; step++)
        {
            if (step == 31)
            {
                foreach (var engine in new[] { baseline, candidate })
                {
                    engine.State.Citizens[0].NextDecisionAt = Math.Min(
                        engine.State.Citizens[0].NextDecisionAt, engine.State.ElapsedSeconds);
                }
            }
            baseline.Advance(2);
            publicSteps++;
            if (step == 31) candidate.Advance(2);
            else { candidate.Pr57AdvanceCertifiedQuantum(); certifiedSteps++; }
            var expected = baseline.SaveToJson();
            var actual = candidate.SaveToJson();
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                var directory = Environment.GetEnvironmentVariable("PR57_SAME_PROCESS_DIAGNOSTIC_DIR");
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, $"baseline-step-{step}.json"), expected);
                    File.WriteAllText(Path.Combine(directory, $"alternate-step-{step}.json"), actual);
                }
                Assert.Fail($"PR57_SAME_PROCESS_DIVERGENCE case={label} step={step} publicSteps={publicSteps} certifiedSteps={certifiedSteps}");
            }
        }
        Console.WriteLine($"PR57_SAME_PROCESS_PARITY case={label} steps=120 byteEqual=True publicSteps={publicSteps} certifiedSteps={certifiedSteps} fallbackSteps=1 comparison=same-process-every-quantum-not-speedup");
    }

    [Fact]
    public void Authentic_saves_dependency_executor_m0_causal_parity_and_cost()
    {
        var path = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_CASE") ?? "unknown";
        var json = File.ReadAllText(path);
        var baseline = CitySimulation.FromJson(json);
        var candidate = new Executor(json);
        Assert.NotEmpty(baseline.State.Citizens);
        Assert.NotEmpty(baseline.State.Vehicles);
        Assert.NotEmpty(baseline.State.Buildings);
        var id = baseline.State.Citizens[0].Id;
        var buildingId = baseline.State.Buildings[0].Id;
        long elapsedTicks = 0, allocated = 0, baselineTicks = 0, baselineAllocated = 0;
        for (var step = 1; step <= 120; step++)
        {
            if (step == 31)
            {
                var hunger = baseline.State.Citizens[0].HungerMicroseconds + 1;
                baseline.State.Citizens[0].HungerMicroseconds = hunger;
                candidate.SubmitNeeds(id, hunger);
                var building = baseline.State.Buildings[0];
                var price = building.SalePrice + .01m;
                var food = building.Stock.Units.GetValueOrDefault(ResourceKind.Food) + 1;
                building.SalePrice = price;
                building.Stock.Units[ResourceKind.Food] = food;
                candidate.SubmitPrice(buildingId, price);
                candidate.SubmitFoodStock(buildingId, food);
            }
            var start = Stopwatch.GetTimestamp();
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            candidate.Advance();
            elapsedTicks += Stopwatch.GetTimestamp() - start;
            allocated += GC.GetAllocatedBytesForCurrentThread() - bytes;
            var baselineStart = Stopwatch.GetTimestamp();
            var baselineBytes = GC.GetAllocatedBytesForCurrentThread();
            baseline.Advance(2);
            baselineTicks += Stopwatch.GetTimestamp() - baselineStart;
            baselineAllocated += GC.GetAllocatedBytesForCurrentThread() - baselineBytes;
            // Exact parity at EVERY public Advance(2) boundary: a divergence
            // between sparse checkpoints may later self-heal and go unnoticed.
            // Serialization remains outside the timed engine intervals.
            var expected = baseline.SaveToJson();
            var actual = candidate.Snapshot();
            Assert.True(string.Equals(expected, actual, StringComparison.Ordinal),
                $"PR57_DEPENDENCY_DIVERGENCE case={label} step={step}");
            Assert.Equal(actual, CitySimulation.FromJson(actual).SaveToJson());
            if (step % 10 == 0)
            {
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(actual)));
                Console.WriteLine($"PR57_DEPENDENCY_CHECKPOINT case={label} step={step} sha256={hash}");
            }
        }
        // M0 persistence gate: reloading must preserve future causal evolution,
        // not merely reproduce the same JSON at the instant of serialization.
        // Use the same external commands on both reloaded engines, including
        // a change to a need and to a building's economic/stock state.
        var persisted = candidate.Snapshot();
        var resumedBaseline = CitySimulation.FromJson(baseline.SaveToJson());
        var resumedCandidate = new Executor(persisted);
        for (var resumedStep = 1; resumedStep <= 10; resumedStep++)
        {
            if (resumedStep == 3)
            {
                var resumedHunger = resumedBaseline.State.Citizens[0].HungerMicroseconds + 1;
                resumedBaseline.State.Citizens[0].HungerMicroseconds = resumedHunger;
                resumedCandidate.SubmitNeeds(id, resumedHunger);
                var resumedBuilding = resumedBaseline.State.Buildings[0];
                var resumedPrice = resumedBuilding.SalePrice + .01m;
                var resumedFood = resumedBuilding.Stock.Units.GetValueOrDefault(ResourceKind.Food) + 1;
                resumedBuilding.SalePrice = resumedPrice;
                resumedBuilding.Stock.Units[ResourceKind.Food] = resumedFood;
                resumedCandidate.SubmitPrice(buildingId, resumedPrice);
                resumedCandidate.SubmitFoodStock(buildingId, resumedFood);
            }
            resumedCandidate.Advance();
            resumedBaseline.Advance(2);
            Assert.Equal(resumedBaseline.SaveToJson(), resumedCandidate.Snapshot());
        }
        Assert.Equal(10, resumedCandidate.Cost.RequiredSteps);
        Assert.Equal(Environment.GetEnvironmentVariable("PR57_DEPENDENCY_M1") == "1" ? 2 : 10, resumedCandidate.Cost.FallbackSteps);
        Assert.Equal(3, resumedCandidate.Cost.ExternalInvalidations);
        Console.WriteLine($"PR57_DEPENDENCY_RESUME case={label} postLoadSteps=10 byteEqual=True externalCommands=3 fallbackSteps={resumedCandidate.Cost.FallbackSteps} avoidedPublicRebuilds={resumedCandidate.Cost.AvoidedBoundaryRebuilds}");
        var cost = candidate.Cost;
        Assert.Equal(120, cost.RequiredSteps);
        Assert.Equal(Environment.GetEnvironmentVariable("PR57_DEPENDENCY_M1") == "1" ? 118 : 0, cost.AvoidedBoundaryRebuilds);
        Assert.Equal(Environment.GetEnvironmentVariable("PR57_DEPENDENCY_M1") == "1" ? 2 : 120, cost.FallbackSteps);
        Assert.Equal(3, cost.ExternalInvalidations);
        Assert.Equal(3, cost.Commands);
        Console.WriteLine($"PR57_DEPENDENCY_INTEGRATED_COST case={label} steps=120 baselineWallMs={baselineTicks * 1000.0 / Stopwatch.Frequency:F3} alternateWallMs={elapsedTicks * 1000.0 / Stopwatch.Frequency:F3} baselineAllocBytes={baselineAllocated} alternateAllocBytes={allocated} speedup={((double)baselineTicks / Math.Max(1, elapsedTicks)):F5} avoidedSteps=0 avoidedBoundaryRebuilds={cost.AvoidedBoundaryRebuilds} comparison=paired-in-process-not-ABBA-no-speedup-claim");
        Console.WriteLine($"PR57_DEPENDENCY_M0 case={label} requiredSteps={cost.RequiredSteps} avoidedSteps=0 avoidedBoundaryRebuilds={cost.AvoidedBoundaryRebuilds} invalidations={cost.ExternalInvalidations} fallbackSteps={cost.FallbackSteps} commands={cost.Commands} alternateWallMs={elapsedTicks * 1000.0 / Stopwatch.Frequency:F3} alternateAllocBytes={allocated} byteEqual=True accelerationClaim=False internalWriterCoverage=certified-boundary-only");
    }

    // End-to-end ABBA screen of the REAL integrated Advance(2) executor.
    // Every step is compared outside timing; fresh .NET processes must repeat
    // this test before any speedup can be certified.
    [Fact]
    public void Authentic_saves_dependency_m1_abba_integrated_screen()
    {
        var path = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path) ||
            Environment.GetEnvironmentVariable("PR57_DEPENDENCY_M1") != "1") return;
        var label = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_CASE") ?? "unknown";
        var json = File.ReadAllText(path);
        var reference = CitySimulation.FromJson(json);
        var hashes = new string[120];
        // Exercise a real cross-domain external edit, forcing the certified
        // executor to fall back at the same causal boundary as public Advance.
        // A later save/load is deliberately covered by the separate resume gate.
        void ApplyExternalEdit(CitySimulation engine)
        {
            engine.State.Citizens[0].HungerMicroseconds++;
            engine.State.Buildings[0].SalePrice += .01m;
            var stock = engine.State.Buildings[0].Stock.Units;
            stock[ResourceKind.Food] = stock.GetValueOrDefault(ResourceKind.Food) + 1;
        }
        for (var step = 0; step < 120; step++)
        {
            if (step == 30) ApplyExternalEdit(reference);
            if (step == 70) reference = CitySimulation.FromJson(reference.SaveToJson());
            reference.Advance(2);
            hashes[step] = reference.SaveToJson();
        }
        // Prime both execution paths before the measured ABBA trajectories.
        // The warm-up uses disposable clones, never the reference checkpoints;
        // otherwise first-use JIT and serializer initialization bias A vs B.
        var warmBaseline = CitySimulation.FromJson(json);
        var warmAlternate = new Executor(json);
        for (var warmStep = 0; warmStep < 8; warmStep++)
        {
            warmBaseline.Advance(2);
            warmAlternate.Advance();
            Assert.Equal(warmBaseline.SaveToJson(), warmAlternate.Snapshot());
        }
        Console.WriteLine($"PR57_M1_WARMUP case={label} steps=8 byteEqual=True excludedFromABBA=True");
        var order = new[] { false, true, true, false };
        var elapsed = new long[4];
        // End-to-end includes external edits, reload and every JSON checkpoint.
        // Advance-only wall excludes those costs and must not be called integrated throughput.
        var trajectoryWall = new long[4];
        var trajectoryAllocated = new long[4];
        var allocated = new long[4];
        var cpuTicks = new long[4];
        for (var trial = 0; trial < order.Length; trial++)
        {
            var alternative = order[trial];
            var baseline = alternative ? null : CitySimulation.FromJson(json);
            var candidate = alternative ? new Executor(json) : null;
            var trialCpuStart = Process.GetCurrentProcess().TotalProcessorTime.Ticks;
            var trialAllocatedStart = GC.GetAllocatedBytesForCurrentThread();
            var trialWallStart = Stopwatch.GetTimestamp();
            for (var step = 0; step < 120; step++)
            {
                if (step == 70)
                {
                    if (alternative) candidate!.Reload();
                    else baseline = CitySimulation.FromJson(baseline!.SaveToJson());
                }
                if (step == 30)
                {
                    if (alternative)
                    {
                        // Submit the exact same edits through the controlled
                        // boundary, never through an escaped mutable alias.
                        var citizen = CitySimulation.FromJson(candidate!.Snapshot()).State.Citizens[0];
                        var building = CitySimulation.FromJson(candidate.Snapshot()).State.Buildings[0];
                        candidate.SubmitNeeds(citizen.Id, citizen.HungerMicroseconds + 1);
                        candidate.SubmitPrice(building.Id, building.SalePrice + .01m);
                        candidate.SubmitFoodStock(building.Id,
                            building.Stock.Units.GetValueOrDefault(ResourceKind.Food) + 1);
                    }
                    else ApplyExternalEdit(baseline!);
                }
                var bytes = GC.GetAllocatedBytesForCurrentThread();
                // CPU is sampled once per trajectory: querying process CPU
                // at every quantum dominates short Advance(2) wall timings.
                // CPU totals include JSON assertions and are diagnostic only.
                var start = Stopwatch.GetTimestamp();
                if (alternative) candidate!.Advance();
                else baseline!.Advance(2);
                elapsed[trial] += Stopwatch.GetTimestamp() - start;
                allocated[trial] += GC.GetAllocatedBytesForCurrentThread() - bytes;
                var snapshot = alternative ? candidate!.Snapshot() : baseline!.SaveToJson();
                Assert.True(string.Equals(hashes[step], snapshot, StringComparison.Ordinal),
                    $"PR57_M1_ABBA_DIVERGENCE case={label} trial={trial} step={step + 1}");
            }
            trajectoryWall[trial] = Stopwatch.GetTimestamp() - trialWallStart;
            trajectoryAllocated[trial] = GC.GetAllocatedBytesForCurrentThread() - trialAllocatedStart;
            cpuTicks[trial] = Process.GetCurrentProcess().TotalProcessorTime.Ticks - trialCpuStart;
            if (alternative)
            {
                Assert.Equal(120, candidate!.Cost.RequiredSteps);
                Assert.Equal(117, candidate.Cost.AvoidedBoundaryRebuilds);
                Assert.Equal(3, candidate.Cost.FallbackSteps);
            }
        }
        var baselineTicks = elapsed[0] + elapsed[3];
        var alternateTicks = elapsed[1] + elapsed[2];
        var baselineTrajectoryTicks = trajectoryWall[0] + trajectoryWall[3];
        var alternateTrajectoryTicks = trajectoryWall[1] + trajectoryWall[2];
        Console.WriteLine($"PR57_M1_TRAJECTORY case={label} stepsPerTrial=120 order=ABBA " +
            $"baselineWallMs={baselineTrajectoryTicks * 1000.0 / Stopwatch.Frequency:F3} " +
            $"alternateWallMs={alternateTrajectoryTicks * 1000.0 / Stopwatch.Frequency:F3} " +
            $"baselineAllocBytes={trajectoryAllocated[0] + trajectoryAllocated[3]} " +
            $"alternateAllocBytes={trajectoryAllocated[1] + trajectoryAllocated[2]} " +
            $"speedup={(double)baselineTrajectoryTicks / Math.Max(1, alternateTrajectoryTicks):F5} " +
            "includesExternalEdits=True includesSaveLoad=True includesCheckpoints=True certified=False");
        Console.WriteLine($"PR57_M1_ABBA case={label} stepsPerTrial=120 order=ABBA " +
            $"baselineWallMs={baselineTicks * 1000.0 / Stopwatch.Frequency:F3} " +
            $"alternateWallMs={alternateTicks * 1000.0 / Stopwatch.Frequency:F3} " +
            $"baselineAllocBytes={allocated[0] + allocated[3]} " +
            $"alternateAllocBytes={allocated[1] + allocated[2]} " +
            $"baselineCpuMs={(cpuTicks[0] + cpuTicks[3]) / (double)TimeSpan.TicksPerMillisecond:F3} " +
            $"alternateCpuMs={(cpuTicks[1] + cpuTicks[2]) / (double)TimeSpan.TicksPerMillisecond:F3} " +
            $"speedup={(double)baselineTicks / Math.Max(1, alternateTicks):F5} " +
            "byteEqual=True saveLoadAt=71 fallbackSteps=3 avoidedInternalSteps=0 certified=False");
    }

    // Quantitative falsification gate for the proposed construction-only cut.
    // Measures the actual integrated engine, not a synthetic dispatch loop.
    // Even a perfect elimination of these two phases cannot beat this ceiling.
    [Fact]
    public void Authentic_saves_construction_phase_ideal_speedup_ceiling()
    {
        var path = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_CASE") ?? "unknown";
        var engine = CitySimulation.FromJson(File.ReadAllText(path));
        var profile = new StepCostProfile();
        engine.CostProfile = profile;
        long wallTicks = 0;
        for (var quantum = 1; quantum <= 120; quantum++)
        {
            if (quantum == 31)
            {
                engine.State.Citizens[0].HungerMicroseconds++;
                engine.State.Buildings[0].SalePrice += .01m;
                var stock = engine.State.Buildings[0].Stock.Units;
                stock[ResourceKind.Food] = stock.GetValueOrDefault(ResourceKind.Food) + 1;
            }
            if (quantum == 71)
            {
                engine = CitySimulation.FromJson(engine.SaveToJson());
                engine.CostProfile = profile;
            }
            var started = Stopwatch.GetTimestamp();
            engine.Advance(2);
            wallTicks += Stopwatch.GetTimestamp() - started;
        }
        Assert.Equal(120, profile.Steps);
        Assert.Equal(StepCostProfile.Names.Length, profile.Ticks.Length);
        var totalProfiled = profile.Ticks.Sum();
        // Index 3 is construction progress; index 10 is construction dispatch.
        var constructionTicks = profile.Ticks[3] + profile.Ticks[10];
        var fraction = totalProfiled == 0 ? 0 : constructionTicks / (double)totalProfiled;
        var idealCeiling = fraction >= 1 ? double.PositiveInfinity : 1 / (1 - fraction);
        // Rank all original causal phases on the same integrated trajectory.
        // This is an Amdahl upper bound, NOT a phase-skipping implementation:
        // timings include profiling overhead and do not certify a speedup.
        var ranked = profile.Ticks
            .Select((ticks, index) => (Index: index, Ticks: ticks))
            .OrderByDescending(entry => entry.Ticks)
            .ThenBy(entry => entry.Index)
            .ToArray();
        var topFourTicks = ranked.Take(4).Sum(entry => entry.Ticks);
        var topFourFraction = totalProfiled == 0 ? 0 : topFourTicks / (double)totalProfiled;
        var topFourCeiling = topFourFraction >= 1 ? double.PositiveInfinity : 1 / (1 - topFourFraction);
        // The normalized phase-only ceiling ignores time outside Mark() spans.
        // Report a second, wall-referenced ideal bound to expose that omission.
        // Both remain intrusive observations, not predictions of optimized wall time.
        var topFourWallFraction = wallTicks <= 0 ? 0 : Math.Min(1, topFourTicks / (double)wallTicks);
        var topFourWallCeiling = topFourWallFraction >= 1
            ? double.PositiveInfinity : 1 / (1 - topFourWallFraction);
        var constructionWallFraction = wallTicks <= 0 ? 0 : Math.Min(1, constructionTicks / (double)wallTicks);
        var constructionWallCeiling = constructionWallFraction >= 1
            ? double.PositiveInfinity : 1 / (1 - constructionWallFraction);
        var rankedPhases = string.Join(",", ranked.Select(entry =>
            $"{StepCostProfile.Names[entry.Index]}:{(totalProfiled == 0 ? 0 : entry.Ticks / (double)totalProfiled):F6}"));
        Console.WriteLine($"PR57_CROSS_PHASE_CEILING case={label} quanta=120 " +
            $"rankedShares={rankedPhases} topFourFraction={topFourFraction:F6} " +
            $"topFourIdealSpeedupCeiling={topFourCeiling:F5} " +
            $"topFourWallFraction={topFourWallFraction:F6} topFourWallIdealCeiling={topFourWallCeiling:F5} " +
            $"profiledVsWallFraction={(wallTicks == 0 ? 0 : totalProfiled / (double)wallTicks):F6} " +
            "scope=intrusive-ideal-bound-no-skipped-phases-no-speedup-claim");
        Console.WriteLine($"PR57_CONSTRUCTION_CEILING case={label} quanta=120 " +
            $"profiledTicks={totalProfiled} constructionTicks={constructionTicks} " +
            $"constructionFraction={fraction:F6} idealSpeedupCeiling={idealCeiling:F5} " +
            $"constructionWallFraction={constructionWallFraction:F6} constructionWallIdealCeiling={constructionWallCeiling:F5} " +
            $"integratedWallMs={wallTicks * 1000.0 / Stopwatch.Frequency:F3} " +
            $"constructionProjectsVisited={profile.ConstructionProgressProjects} " +
            $"dispatchProjectsVisited={profile.ConstructionOrderProjects} " +
            "externalEdits=3 saveLoadAt=71 scope=ideal-upper-bound-not-optimization");
    }

    // The intrusive cross-phase ceiling must not perturb the canonical state.
    // This gate compares every intermediate full JSON, including external edits
    // and the persistence boundary, before trusting any phase ranking.
    [Fact]
    public void Authentic_saves_phase_cost_profile_is_causally_observational()
    {
        var path = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_DEPENDENCY_CASE") ?? "unknown";
        var original = File.ReadAllText(path);
        var baseline = CitySimulation.FromJson(original);
        var observed = CitySimulation.FromJson(original);
        var profile = new StepCostProfile();
        observed.CostProfile = profile;
        var baselineCheckpoints = new string[120];
        for (var quantum = 1; quantum <= 120; quantum++)
        {
            if (quantum == 31)
            {
                foreach (var engine in new[] { baseline, observed })
                {
                    engine.State.Citizens[0].HungerMicroseconds++;
                    engine.State.Buildings[0].SalePrice += .01m;
                    var stock = engine.State.Buildings[0].Stock.Units;
                    stock[ResourceKind.Food] = stock.GetValueOrDefault(ResourceKind.Food) + 1;
                }
            }
            if (quantum == 71)
            {
                baseline = CitySimulation.FromJson(baseline.SaveToJson());
                observed = CitySimulation.FromJson(observed.SaveToJson());
                observed.CostProfile = profile;
            }
            baseline.Advance(2);
            observed.Advance(2);
            Assert.Equal(baseline.SaveToJson(), observed.SaveToJson());
            baselineCheckpoints[quantum - 1] = baseline.SaveToJson();
        }
        Assert.Equal(120, profile.Steps);
        // A phase profile is intrusive: quantify its measurement overhead in
        // the same integrated engine before interpreting wall-referenced ceilings.
        // ABBA uses fresh clones and identical edits/reload/checkpoints in every
        // trajectory. JSON equality is checked outside each Advance timing.
        var profileOrder = new[] { false, true, true, false };
        var profileWall = new long[4];
        var profileCpu = new long[4];
        var profileAlloc = new long[4];
        for (var trial = 0; trial < profileOrder.Length; trial++)
        {
            var instrumented = profileOrder[trial];
            var simulation = CitySimulation.FromJson(original);
            var trialProfile = instrumented ? new StepCostProfile() : null;
            simulation.CostProfile = trialProfile;
            var cpuBefore = Process.GetCurrentProcess().TotalProcessorTime.Ticks;
            var allocBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var quantum = 1; quantum <= 120; quantum++)
            {
                if (quantum == 31)
                {
                    simulation.State.Citizens[0].HungerMicroseconds++;
                    simulation.State.Buildings[0].SalePrice += .01m;
                    var stock = simulation.State.Buildings[0].Stock.Units;
                    stock[ResourceKind.Food] = stock.GetValueOrDefault(ResourceKind.Food) + 1;
                }
                if (quantum == 71)
                {
                    simulation = CitySimulation.FromJson(simulation.SaveToJson());
                    simulation.CostProfile = trialProfile;
                }
                var started = Stopwatch.GetTimestamp();
                simulation.Advance(2);
                profileWall[trial] += Stopwatch.GetTimestamp() - started;
                Assert.Equal(baselineCheckpoints[quantum - 1], simulation.SaveToJson());
            }
            profileCpu[trial] = Process.GetCurrentProcess().TotalProcessorTime.Ticks - cpuBefore;
            profileAlloc[trial] = GC.GetAllocatedBytesForCurrentThread() - allocBefore;
            if (instrumented) Assert.Equal(120, trialProfile!.Steps);
        }
        var unprofiledTicks = profileWall[0] + profileWall[3];
        var profiledTicks = profileWall[1] + profileWall[2];
        Console.WriteLine($"PR57_CROSS_PHASE_PROFILE_OVERHEAD case={label} order=ABBA quantaPerTrial=120 " +
            $"unprofiledAdvanceWallMs={unprofiledTicks * 1000.0 / Stopwatch.Frequency:F3} " +
            $"profiledAdvanceWallMs={profiledTicks * 1000.0 / Stopwatch.Frequency:F3} " +
            $"wallRatio={(double)profiledTicks / Math.Max(1, unprofiledTicks):F5} " +
            $"unprofiledCpuMs={(profileCpu[0] + profileCpu[3]) / (double)TimeSpan.TicksPerMillisecond:F3} " +
            $"profiledCpuMs={(profileCpu[1] + profileCpu[2]) / (double)TimeSpan.TicksPerMillisecond:F3} " +
            $"unprofiledAllocBytes={profileAlloc[0] + profileAlloc[3]} " +
            $"profiledAllocBytes={profileAlloc[1] + profileAlloc[2]} " +
            "byteEqual=True externalEdits=3 saveLoadAt=71 scope=intrusive-profile-calibration-not-speedup");
        Console.WriteLine($"PR57_CROSS_PHASE_PROFILE_PARITY case={label} quanta=120 byteEqual=True externalEdits=3 saveLoadAt=71");
    }
}
