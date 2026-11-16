using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57: the blocker is re-evaluated after an external position mutation.
// This deliberately exercises the integrated admission predicate, not a synthetic cache.
public sealed class Pr57JunctionBlockerMutationTests
{
    [Fact]
    public void Junction_occupant_selection_rechecks_live_position_and_first_match()
    {
        var seed = new CitySimulation(42);
        var junction = new RoadNode
        {
            Id = 990101, Position = new Point(0, 0),
            Neighbours = new List<int> { 990102, 990103, 990104, 990105 }
        };
        seed.State.Roads.Add(junction);
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 990201, Position = new Point(0, 0),
            Status = VehicleStatus.Travelling
        });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 990202, Position = new Point(1, 0),
            Status = VehicleStatus.Travelling, IntersectionClaim = junction.Id
        });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 990203, Position = new Point(2, 0),
            Status = VehicleStatus.Travelling, IntersectionClaim = junction.Id
        });

        var initial = seed.SaveToJson();
        var sim = CitySimulation.FromJson(initial);
        var node = sim.State.Roads.Single(r => r.Id == junction.Id);
        var entrant = sim.State.Vehicles.Single(v => v.Id == 990201);
        var first = sim.State.Vehicles.Single(v => v.Id == 990202);
        var second = sim.State.Vehicles.Single(v => v.Id == 990203);
        var method = typeof(CitySimulation).GetMethod("MayEnterJunction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        bool Admit() => (bool)method!.Invoke(sim, new object[] { entrant, node })!;

        Assert.False(Admit());
        Assert.Contains(first.Id.ToString(), entrant.Cause);
        first.Position = new Point(30, 0); // external mutation between admissions
        Assert.False(Admit());
        Assert.Contains(second.Id.ToString(), entrant.Cause);
        Assert.DoesNotContain(first.Id.ToString(), entrant.Cause);
        Assert.Equal(sim.SaveToJson(), CitySimulation.FromJson(sim.SaveToJson()).SaveToJson());
    }
}
