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

    [Fact]
    public void Certified_boundary_handles_external_citizen_remove_and_reinsert_at_same_count()
    {
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        var original = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(original);
        var certified = CitySimulation.FromJson(original);
        Assert.True(baseline.State.Citizens.Count > 1);

        baseline.Advance(2);
        certified.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());

        // Count-only validation cannot detect replacement/reinsertion at another index.
        // Keep the same citizen identities and causal state, but mutate list membership order
        // through remove/insert, as an external editor may do between certified quanta.
        foreach (var sim in new[] { baseline, certified })
        {
            var citizens = sim.State.Citizens;
            var moved = citizens[0];
            citizens.RemoveAt(0);
            citizens.Insert(citizens.Count, moved);
        }
        Assert.Equal(baseline.State.Citizens.Count, certified.State.Citizens.Count);
        certified.Pr57InvalidateCertifiedBoundary();

        for (var quantum = 0; quantum < 4; quantum++)
        {
            baseline.Advance(2);
            certified.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        }
    }

    [Fact]
    public void Public_advance_rebuilds_roster_after_external_alive_mutation_at_constant_count()
    {
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        var json = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(json);
        var probe = CitySimulation.FromJson(json);
        var id = baseline.State.Citizens.First(c => c.Alive).Id;
        var count = baseline.State.Citizens.Count;

        static long Rebuilds(CitySimulation sim)
        {
            var field = typeof(CitySimulation).GetField(
                "_pr57RosterRebuilds", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (long)field.GetValue(sim)!;
        }

        // Every public Advance(2) is an untrusted boundary, even if Count is unchanged.
        // Mutate Alive in both copies and compare full serialized causal state each time.
        foreach (var alive in new[] { true, false, true, false, true })
        {
            baseline.State.Citizens.Single(c => c.Id == id).Alive = alive;
            probe.State.Citizens.Single(c => c.Id == id).Alive = alive;
            Assert.Equal(count, baseline.State.Citizens.Count);
            Assert.Equal(count, probe.State.Citizens.Count);
            var before = Rebuilds(probe);
            baseline.Advance(2);
            probe.Advance(2);
            Assert.Equal(baseline.SaveToJson(), probe.SaveToJson());
            if (Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER") == "1")
                Assert.Equal(before + 1, Rebuilds(probe));
        }

        var resumed = CitySimulation.FromJson(probe.SaveToJson());
        baseline.Advance(2);
        resumed.Advance(2);
        Assert.Equal(baseline.SaveToJson(), resumed.SaveToJson());
    }

}
