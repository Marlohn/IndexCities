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
    private readonly record struct ExecutionCost(long RequiredSteps, long AvoidedSteps,
        long ExternalInvalidations, long FallbackSteps, long Commands);

    private sealed class Executor
    {
        private readonly CitySimulation _engine;
        private readonly Queue<Command> _pending = new();
        private long _required, _invalidations, _fallback, _commands;
        private bool _dirty = true;
        public Executor(string json) => _engine = CitySimulation.FromJson(json);
        public string Snapshot() => _engine.SaveToJson();
        public ExecutionCost Cost => new(_required, 0, _invalidations, _fallback, _commands);

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
            _engine.Advance(2);
            _required++;
            _fallback++;
            _dirty = false;
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
        long baselineTicks = 0, alternateTicks = 0;
        long baselineBytes = 0, alternateBytes = 0;
        for (var batch = 1; batch <= 12; batch++)
        {
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
        Console.WriteLine($"PR57_BATCH_BOUNDARY_COST case={label} baselineWallMs={baselineTicks * 1000.0 / Stopwatch.Frequency:F3} alternateWallMs={alternateTicks * 1000.0 / Stopwatch.Frequency:F3} baselineAllocBytes={baselineBytes} alternateAllocBytes={alternateBytes} speedup={(double)baselineTicks / Math.Max(1, alternateTicks):F5} publicBoundariesAvoided=108 internalQuantaAvoided=0 causalPhasesAvoided=0 comparison=paired-in-process-not-ABBA");
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
            if (step % 10 != 0) continue;
            var expected = baseline.SaveToJson();
            var actual = candidate.Snapshot();
            Assert.Equal(expected, actual);
            Assert.Equal(actual, CitySimulation.FromJson(actual).SaveToJson());
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(actual)));
            Console.WriteLine($"PR57_DEPENDENCY_CHECKPOINT case={label} step={step} sha256={hash}");
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
        Assert.Equal(10, resumedCandidate.Cost.FallbackSteps);
        Assert.Equal(3, resumedCandidate.Cost.ExternalInvalidations);
        Console.WriteLine($"PR57_DEPENDENCY_RESUME case={label} postLoadSteps=10 byteEqual=True externalCommands=3 fallbackSteps=10 avoidedSteps=0");
        var cost = candidate.Cost;
        Assert.Equal(120, cost.RequiredSteps);
        Assert.Equal(0, cost.AvoidedSteps);
        Assert.Equal(120, cost.FallbackSteps);
        Assert.Equal(3, cost.ExternalInvalidations);
        Assert.Equal(3, cost.Commands);
        Console.WriteLine($"PR57_DEPENDENCY_INTEGRATED_COST case={label} steps=120 baselineWallMs={baselineTicks * 1000.0 / Stopwatch.Frequency:F3} alternateWallMs={elapsedTicks * 1000.0 / Stopwatch.Frequency:F3} baselineAllocBytes={baselineAllocated} alternateAllocBytes={allocated} speedup={((double)baselineTicks / Math.Max(1, elapsedTicks)):F5} avoidedSteps={cost.AvoidedSteps} comparison=paired-in-process-not-ABBA-no-speedup-claim");
        Console.WriteLine($"PR57_DEPENDENCY_M0 case={label} requiredSteps={cost.RequiredSteps} avoidedSteps={cost.AvoidedSteps} invalidations={cost.ExternalInvalidations} fallbackSteps={cost.FallbackSteps} commands={cost.Commands} alternateWallMs={elapsedTicks * 1000.0 / Stopwatch.Frequency:F3} alternateAllocBytes={allocated} byteEqual=True accelerationClaim=False internalWriterCoverage=False");
    }
}
