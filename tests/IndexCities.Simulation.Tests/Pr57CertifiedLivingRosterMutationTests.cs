using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only adversarial replay: public State aliases can change Alive while
// Citizens.Count stays constant. No official simulation behavior is changed.
public sealed class Pr57CertifiedLivingRosterMutationTests
{
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
}
