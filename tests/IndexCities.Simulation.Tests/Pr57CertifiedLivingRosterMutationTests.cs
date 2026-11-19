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
    public void Public_advance_rebuilds_roster_after_external_reorder_at_constant_count()
    {
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        var json = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(json);
        var probe = CitySimulation.FromJson(json);
        Assert.True(baseline.State.Citizens.Count > 1);

        static long Rebuilds(CitySimulation sim)
        {
            var field = typeof(CitySimulation).GetField(
                "_pr57RosterRebuilds", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (long)field.GetValue(sim)!;
        }

        // A public Advance is an untrusted boundary: changing iteration order
        // without changing Count must not reuse the prior roster.
        for (var step = 0; step < 5; step++)
        {
            baseline.State.Citizens.Reverse();
            probe.State.Citizens.Reverse();
            Assert.Equal(baseline.State.Citizens.Count, probe.State.Citizens.Count);
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


    [Fact]
    public void Certified_boundary_handles_simultaneous_alive_and_order_mutation_at_constant_count()
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

        // Both membership of the living subset and iteration order change at
        // the same external boundary while the public collection size stays fixed.
        foreach (var sim in new[] { baseline, certified })
        {
            var citizens = sim.State.Citizens;
            var index = citizens.FindIndex(c => c.Alive);
            Assert.True(index >= 0);
            citizens[index].Alive = false;
            citizens.Reverse();
        }
        certified.Pr57InvalidateCertifiedBoundary();
        for (var quantum = 0; quantum < 5; quantum++)
        {
            baseline.Advance(2);
            certified.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        }

        var resumed = CitySimulation.FromJson(certified.SaveToJson());
        baseline.Advance(2);
        resumed.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), resumed.SaveToJson());
    }

    [Fact]
    public void Certified_boundary_rebuilds_when_living_membership_swaps_at_fixed_population_and_alive_count()
    {
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        var json = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(json);
        var certified = CitySimulation.FromJson(json);
        var ids = baseline.State.Citizens.Where(c => c.Alive).Take(2).Select(c => c.Id).ToArray();
        Assert.Equal(2, ids.Length);
        var population = baseline.State.Citizens.Count;

        // Establish a cached roster, then remove one member at a certified boundary.
        baseline.Advance(2);
        certified.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        foreach (var sim in new[] { baseline, certified })
            sim.State.Citizens.Single(c => c.Id == ids[0]).Alive = false;
        certified.Pr57InvalidateCertifiedBoundary();
        baseline.Advance(2);
        certified.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());

        // Count the actual invalidation cost, not just JSON equality: a stale
        // roster may look correct when both swapped citizens are temporarily idle.
        static long Counter(CitySimulation sim, string name)
        {
            var field = typeof(CitySimulation).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (long)field.GetValue(sim)!;
        }
        var aliveBeforeSwap = certified.State.Citizens.Count(c => c.Alive);
        var rebuildsBeforeSwap = Counter(certified, "_pr57RosterRebuilds");
        var scannedBeforeSwap = Counter(certified, "_pr57RosterScanned");

        // Swap membership without changing either Citizens.Count or living count.
        // A count-only roster cache would silently retain the wrong citizen.
        foreach (var sim in new[] { baseline, certified })
        {
            var first = sim.State.Citizens.Single(c => c.Id == ids[0]);
            var second = sim.State.Citizens.Single(c => c.Id == ids[1]);
            Assert.False(first.Alive);
            Assert.True(second.Alive);
            first.Alive = true;
            second.Alive = false;
            Assert.Equal(population, sim.State.Citizens.Count);
            Assert.Equal(aliveBeforeSwap, sim.State.Citizens.Count(c => c.Alive));
        }
        certified.Pr57InvalidateCertifiedBoundary();
        for (var quantum = 0; quantum < 5; quantum++)
        {
            baseline.Advance(2);
            certified.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        }
        if (Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER") == "1")
        {
            Assert.Equal(rebuildsBeforeSwap + 1,
                Counter(certified, "_pr57RosterRebuilds"));
            Assert.Equal(scannedBeforeSwap + population,
                Counter(certified, "_pr57RosterScanned"));
        }
        var resumed = CitySimulation.FromJson(certified.SaveToJson());
        baseline.Advance(2);
        resumed.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), resumed.SaveToJson());
    }

    [Fact]
    public void Certified_save_load_discards_cached_roster_and_rebuilds_once()
    {
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        var json = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(json);
        var certified = CitySimulation.FromJson(json);
        for (var quantum = 0; quantum < 6; quantum++)
        {
            baseline.Advance(2);
            certified.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), certified.SaveToJson());
        }

        // Serialization must never carry the private certified cache into a
        // new simulation, even when the public population remains unchanged.
        var resumed = CitySimulation.FromJson(certified.SaveToJson());
        var count = resumed.State.Citizens.Count;
        static long Counter(CitySimulation sim, string name)
        {
            var field = typeof(CitySimulation).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (long)field.GetValue(sim)!;
        }
        if (Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER") == "1")
        {
            Assert.Equal(0, Counter(resumed, "_pr57RosterRebuilds"));
            Assert.Equal(0, Counter(resumed, "_pr57RosterScanned"));
        }
        for (var quantum = 0; quantum < 6; quantum++)
        {
            baseline.Advance(2);
            resumed.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), resumed.SaveToJson());
        }
        if (Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER") == "1")
        {
            Assert.Equal(1, Counter(resumed, "_pr57RosterRebuilds"));
            Assert.Equal(count, Counter(resumed, "_pr57RosterScanned"));
        }
    }

    [Fact]
    public void Certified_roster_is_invalidated_by_interleaved_public_advance_and_external_alive_change()
    {
        var seed = CityCycleTests.City();
        for (var hour = 0; hour < 48; hour++) seed.Advance(3600);
        var json = seed.SaveToJson();
        var baseline = CitySimulation.FromJson(json);
        var candidate = CitySimulation.FromJson(json);
        var id = baseline.State.Citizens.First(c => c.Alive).Id;

        for (var quantum = 0; quantum < 4; quantum++)
        {
            baseline.Advance(2);
            candidate.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), candidate.SaveToJson());
        }

        // Public Advance is untrusted and must break the prior certified
        // reuse window even when the collection count is unchanged.
        foreach (var sim in new[] { baseline, candidate })
            sim.State.Citizens.Single(c => c.Id == id).Alive = false;
        static long Rebuilds(CitySimulation sim)
        {
            var field = typeof(CitySimulation).GetField(
                "_pr57RosterRebuilds", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (long)field.GetValue(sim)!;
        }
        var beforePublic = Rebuilds(candidate);
        baseline.Advance(2);
        candidate.Advance(2);
        Assert.Equal(baseline.SaveToJson(), candidate.SaveToJson());
        if (Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER") == "1")
            Assert.Equal(beforePublic + 1, Rebuilds(candidate));
        var afterPublic = Rebuilds(candidate);

        for (var quantum = 0; quantum < 5; quantum++)
        {
            baseline.Advance(2);
            candidate.Pr57AdvanceCertifiedQuantum();
            Assert.Equal(baseline.SaveToJson(), candidate.SaveToJson());
        }

        if (Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER") == "1")
            Assert.Equal(afterPublic, Rebuilds(candidate));

        var resumed = CitySimulation.FromJson(candidate.SaveToJson());
        baseline.Advance(2);
        resumed.Pr57AdvanceCertifiedQuantum();
        Assert.Equal(baseline.SaveToJson(), resumed.SaveToJson());
    }
}
