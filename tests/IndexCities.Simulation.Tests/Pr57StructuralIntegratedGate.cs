using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 isolated integrated gate. The alternate opt-in engine paths are selected
// at process startup, never toggled in-process (their switches are static).
// Hashes are of the FULL canonical JSON, not a selected subset of fields.
public sealed class Pr57StructuralIntegratedGate
{
    [Fact]
    public void Authentic_save_integrated_alternate_mode_checkpoints()
    {
        var path = Environment.GetEnvironmentVariable("PR57_STRUCTURAL_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_STRUCTURAL_CASE") ?? "unknown";
        var mode = Environment.GetEnvironmentVariable("PR57_STRUCTURAL_MODE") ?? "baseline";
        var alternative = mode == "alternate";
        Assert.True(mode is "baseline" or "alternate");
        Assert.Equal(alternative ? "1" : null,
            Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER"));
        Assert.Equal(alternative ? "1" : null,
            Environment.GetEnvironmentVariable("PR57_CITIZEN_AGENDA"));

        var engine = CitySimulation.FromJson(File.ReadAllText(path));
        Assert.NotEmpty(engine.State.Citizens);
        Assert.NotEmpty(engine.State.Buildings);
        Assert.NotEmpty(engine.State.Vehicles);
        long advanceTicks = 0, advanceAllocated = 0;
        long checkpointTicks = 0, checkpointAllocated = 0;
        for (var step = 1; step <= 120; step++)
        {
            // An external edit at the public boundary must invalidate the
            // alternative agenda exactly as it does in the canonical engine.
            if (step == 31)
            {
                var citizen = engine.State.Citizens[0];
                citizen.NextDecisionAt = Math.Min(citizen.NextDecisionAt,
                    engine.State.ElapsedSeconds);
            }
            var advanceStart = Stopwatch.GetTimestamp();
            var advanceBytes = GC.GetAllocatedBytesForCurrentThread();
            engine.Advance(2);
            advanceTicks += Stopwatch.GetTimestamp() - advanceStart;
            advanceAllocated += GC.GetAllocatedBytesForCurrentThread() - advanceBytes;
            if (step % 10 != 0) continue;
            var checkpointStart = Stopwatch.GetTimestamp();
            var checkpointBytes = GC.GetAllocatedBytesForCurrentThread();
            var json = engine.SaveToJson();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(json, CitySimulation.FromJson(json).SaveToJson());
            Console.WriteLine($"PR57_STRUCTURAL_CHECKPOINT case={label} step={step} sha256={hash}");
            checkpointTicks += Stopwatch.GetTimestamp() - checkpointStart;
            checkpointAllocated += GC.GetAllocatedBytesForCurrentThread() - checkpointBytes;
        }
        double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine($"PR57_STRUCTURAL_COST case={label} mode={mode} steps=120 advanceWallMs={Ms(advanceTicks):F3} advanceAllocBytes={advanceAllocated} checkpointWallMs={Ms(checkpointTicks):F3} checkpointAllocBytes={checkpointAllocated} checkpointCount=12 includesJsonInAdvance=False");
    }
}
