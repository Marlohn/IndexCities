using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 baseline adversary: observe floating-point order without enabling the
// canonical-pollution flag or changing the simulation's pollution arithmetic.
public sealed class Pr57LakeBaselineOrderTests
{
    [Fact]
    public void Lake_sample_observes_exact_dictionary_order_before_and_after_save_load()
    {
        // Distinct insertion orders with identical pollution cell key/value pairs.
        // This is an adversarial unit fixture, not a speed or integrated parity claim.
        var cells = Enumerable.Range(0, 32)
            .Select(i => (Key: ((long)(i % 8) << 32) | (uint)(i / 8),
                          Value: (decimal)(i % 3 == 0 ? 0.00000001 : i % 3 == 1 ? 0.001 : 0.1)))
            .ToArray();
        var forward = new CitySimulation(42);
        var reverse = new CitySimulation(42);
        foreach (var cell in cells) forward.State.PollutionCells.Add(cell.Key, cell.Value);
        foreach (var cell in cells.Reverse()) reverse.State.PollutionCells.Add(cell.Key, cell.Value);

        static long SampleBits(CitySimulation sim)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var type = typeof(CitySimulation);
            var snapshot = type.GetMethod("SnapshotPollution", flags);
            var sample = type.GetMethod("SampledPollution", flags, null, new[] { typeof(Point) }, null);
            Assert.NotNull(snapshot);
            Assert.NotNull(sample);
            snapshot!.Invoke(sim, null);
            return BitConverter.DoubleToInt64Bits((double)sample!.Invoke(sim, new object[] { sim.State.LakeCentre })!);
        }

        var a1 = SampleBits(forward);
        var b1 = SampleBits(reverse);
        var forwardJson = forward.SaveToJson();
        var reverseJson = reverse.SaveToJson();
        var a2 = SampleBits(CitySimulation.FromJson(forwardJson));
        var b2 = SampleBits(CitySimulation.FromJson(reverseJson));
        Assert.Equal(cells.Length, forward.State.PollutionCells.Count);
        Assert.Equal(cells.Length, reverse.State.PollutionCells.Count);
        Assert.Equal(
            forward.State.PollutionCells.OrderBy(p => p.Key).ToArray(),
            reverse.State.PollutionCells.OrderBy(p => p.Key).ToArray());
        // The baseline's own bit patterns are recorded, not rounded away.
        // An order-dependent ULP difference is diagnostic, not an assertion
        // that every runtime must produce one.
        Console.WriteLine($"PR57_LAKE_BASELINE_ORDER a1={a1:X16} a2={a2:X16} b1={b1:X16} b2={b2:X16} " +
                          $"crossOrderDifferent={a1 != b1} forwardReloadDifferent={a1 != a2} reverseReloadDifferent={b1 != b2}");
        Assert.True(double.IsFinite(BitConverter.Int64BitsToDouble(a1)));
        Assert.True(double.IsFinite(BitConverter.Int64BitsToDouble(b1)));
        Assert.True(double.IsFinite(BitConverter.Int64BitsToDouble(a2)));
        Assert.True(double.IsFinite(BitConverter.Int64BitsToDouble(b2)));
    }
    [Fact]
    public void Lake_sample_tracks_external_cell_reinsertion_without_normalizing_baseline()
    {
        // A public-state mutation changes enumeration order without changing
        // the cell multiset. This exercises the real snapshot invalidation path.
        var sim = new CitySimulation(42);
        var cells = Enumerable.Range(0, 32)
            .Select(i => (Key: ((long)(i % 8) << 32) | (uint)(i / 8),
                          Value: (decimal)(i % 3 == 0 ? 0.00000001 : i % 3 == 1 ? 0.001 : 0.1)))
            .ToArray();
        foreach (var cell in cells) sim.State.PollutionCells.Add(cell.Key, cell.Value);
        static long SampleBits(CitySimulation city)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var type = typeof(CitySimulation);
            var snapshot = type.GetMethod("SnapshotPollution", flags)!;
            var sample = type.GetMethod("SampledPollution", flags, null, new[] { typeof(Point) }, null)!;
            snapshot.Invoke(city, null);
            return BitConverter.DoubleToInt64Bits((double)sample.Invoke(city, new object[] { city.State.LakeCentre })!);
        }
        var before = SampleBits(sim);
        // Remove/reinsert in reverse order: no value changes, only enumeration.
        foreach (var cell in cells) Assert.True(sim.State.PollutionCells.Remove(cell.Key));
        foreach (var cell in cells.Reverse()) sim.State.PollutionCells.Add(cell.Key, cell.Value);
        var after = SampleBits(sim);
        var reloaded = CitySimulation.FromJson(sim.SaveToJson());
        var afterReload = SampleBits(reloaded);
        Assert.Equal(cells.Length, sim.State.PollutionCells.Count);
        Assert.Equal(sim.State.PollutionCells.OrderBy(p => p.Key).ToArray(),
                     reloaded.State.PollutionCells.OrderBy(p => p.Key).ToArray());
        Assert.Equal(after, afterReload);
        Console.WriteLine($"PR57_LAKE_EXTERNAL_REINSERT before={before:X16} after={after:X16} reload={afterReload:X16} changed={before != after}");
    }

}
