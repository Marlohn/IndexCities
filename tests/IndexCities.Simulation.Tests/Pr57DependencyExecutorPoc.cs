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
    private readonly record struct Command(Domain Domain, int EntityId, long Value);
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

        // The only supported external edit in M0 is an explicit command.
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
        long elapsedTicks = 0, allocated = 0;
        for (var step = 1; step <= 120; step++)
        {
            if (step == 31)
            {
                var hunger = baseline.State.Citizens[0].HungerMicroseconds + 1;
                baseline.State.Citizens[0].HungerMicroseconds = hunger;
                candidate.SubmitNeeds(id, hunger);
            }
            var start = Stopwatch.GetTimestamp();
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            candidate.Advance();
            elapsedTicks += Stopwatch.GetTimestamp() - start;
            allocated += GC.GetAllocatedBytesForCurrentThread() - bytes;
            baseline.Advance(2);
            if (step % 10 != 0) continue;
            var expected = baseline.SaveToJson();
            var actual = candidate.Snapshot();
            Assert.Equal(expected, actual);
            Assert.Equal(actual, CitySimulation.FromJson(actual).SaveToJson());
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(actual)));
            Console.WriteLine($"PR57_DEPENDENCY_CHECKPOINT case={label} step={step} sha256={hash}");
        }
        var cost = candidate.Cost;
        Assert.Equal(120, cost.RequiredSteps);
        Assert.Equal(0, cost.AvoidedSteps);
        Assert.Equal(120, cost.FallbackSteps);
        Assert.Equal(1, cost.ExternalInvalidations);
        Assert.Equal(1, cost.Commands);
        Console.WriteLine($"PR57_DEPENDENCY_M0 case={label} requiredSteps={cost.RequiredSteps} avoidedSteps={cost.AvoidedSteps} invalidations={cost.ExternalInvalidations} fallbackSteps={cost.FallbackSteps} commands={cost.Commands} alternateWallMs={elapsedTicks * 1000.0 / Stopwatch.Frequency:F3} alternateAllocBytes={allocated} byteEqual=True accelerationClaim=False internalWriterCoverage=False");
    }
}
