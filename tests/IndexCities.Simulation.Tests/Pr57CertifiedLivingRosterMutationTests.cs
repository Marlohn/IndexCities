using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only adversarial replay: public State aliases can change Alive while
// Citizens.Count stays constant. No official simulation behavior is changed.
public sealed class Pr57CertifiedLivingRosterMutationTests
{
    [Fact]
    public void Certified_quanta_reuse_roster_until_explicit_external_invalidation()
    {
        // The opt-in must do actual avoided enumeration, not merely produce
        // matching JSON while rebuilding the roster on every quantum.
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        var original = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(original);
        var certified = CitySimulation.FromJson(original);
        static long Counter(CitySimulation sim, string name)
        {
            var field = typeof(CitySimulation).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (long)field.GetValue(sim)!;
        }

        for (var quantum = 0; quantum < 8; quantum++)
        {
            baseline.Advance(2);
            certified.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        }
        if (Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER") != "1") return;

        var steps = Counter(certified, "_pr57RosterSteps");
        var rebuilds = Counter(certified, "_pr57RosterRebuilds");
        var scanned = Counter(certified, "_pr57RosterScanned");
        Assert.Equal(8, steps);
        Assert.True(rebuilds < steps,
            $"Certified quanta did not avoid a roster rebuild: {rebuilds}/{steps}");
        Assert.True(scanned < steps * certified.State.Citizens.Count,
            $"Certified quanta did not avoid citizen scans: {scanned}");

        // Explicitly yield the certified boundary to external writers.
        certified.Pr57InvalidateCertifiedBoundary();
        var before = Counter(certified, "_pr57RosterRebuilds");
        baseline.Advance(2);
        certified.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        Assert.Equal(before + 1, Counter(certified, "_pr57RosterRebuilds"));
    }

    [Fact]
    public void Certified_boundary_rebuilds_living_roster_after_external_alive_mutation()
    {
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        Assert.NotEmpty(seed.State.Citizens.Where(c => c.Alive));
        var original = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(original);
        var certified = CitySimulation.FromJson(original);
        var id = baseline.State.Citizens.First(c => c.Alive).Id;
        Assert.Equal(baseline.State.Citizens.Count, certified.State.Citizens.Count);

        // First certified quantum establishes the reusable roster.
        baseline.Advance(2);
        certified.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());

        foreach (var alive in new[] { false, true, false, true })
        {
            baseline.State.Citizens.Single(c => c.Id == id).Alive = alive;
            certified.State.Citizens.Single(c => c.Id == id).Alive = alive;
            Assert.Equal(baseline.State.Citizens.Count, certified.State.Citizens.Count);
            certified.Pr57InvalidateCertifiedBoundary();
            if (Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER") == "1")
            {
                var dirty = typeof(CitySimulation).GetField(
                    "_pr57LivingRosterDirty", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(dirty);
                Assert.Equal(true, dirty.GetValue(certified));
            }
            baseline.Advance(2);
            certified.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        }
        // A save/load boundary must not preserve stale private roster contents.
        var resumed = CitySimulation.FromJson(certified.SaveToJson());
        baseline.Advance(2);
        resumed.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), resumed.SaveToJson());
    }
    [Fact]
    public void Certified_boundary_handles_external_citizen_reordering_without_count_change()
    {
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        var json = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(json);
        var certified = CitySimulation.FromJson(json);
        Assert.True(baseline.State.Citizens.Count > 1);

        baseline.Advance(2);
        certified.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());

        // External writers can reorder the same citizen objects without changing Count.
        // A cached iteration order must not survive the explicit certified boundary.
        baseline.State.Citizens.Reverse();
        certified.State.Citizens.Reverse();
        certified.Pr57InvalidateCertifiedBoundary();
        for (var quantum = 0; quantum < 4; quantum++)
        {
            baseline.Advance(2);
            certified.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        }
    }

}
