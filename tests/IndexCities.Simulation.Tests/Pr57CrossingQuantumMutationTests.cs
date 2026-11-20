using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57: a crossing query must not retain a previous quantum's result after
// an external editor moves a vehicle or changes its crossing claim.
public sealed class Pr57CrossingQuantumMutationTests
{
    [Fact]
    public void Crossing_clearance_rechecks_live_body_after_external_mutation_and_reload()
    {
        var sim = new CitySimulation(42);
        var follower = new Vehicle
        {
            Id = 994201, Position = new Point(0, 0), Heading = 0,
            Status = VehicleStatus.Travelling
        };
        var crossing = new Vehicle
        {
            Id = 994202, Position = new Point(0, 4), Heading = Math.PI / 2,
            Status = VehicleStatus.Travelling, IntersectionClaim = 994101
        };
        sim.State.Vehicles.Add(follower);
        sim.State.Vehicles.Add(crossing);
        sim = CitySimulation.FromJson(sim.SaveToJson());
        var method = typeof(CitySimulation).GetMethod("ClearOfCrossingAhead",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        double Clearance(CitySimulation city, Vehicle vehicle) =>
            (double)method!.Invoke(city, new object[] { vehicle, new Point(0, 10), 10.0 })!;

        follower = sim.State.Vehicles.Single(v => v.Id == 994201);
        crossing = sim.State.Vehicles.Single(v => v.Id == 994202);
        // Negative control: without a junction graph the crossing claim must not
        // manufacture a physical blocker. This is NOT a positive invalidation
        // test and cannot certify a cross-quantum clearance cache.
        foreach (var x in new[] { -4.0, -2.0, 0.0, 2.0, 4.0 })
        {
            foreach (var z in new[] { 1.0, 2.0, 4.0, 6.0, 8.0 })
            {
                crossing.Position = new Point(x, z);
                Assert.Equal(10, Clearance(sim, follower));
            }
        }
        crossing.Position = new Point(100, 100);
        crossing.IntersectionClaim = null;
        var afterMutation = sim.SaveToJson();
        Assert.Equal(10, Clearance(sim, follower));
        var restored = CitySimulation.FromJson(afterMutation);
        var restoredFollower = restored.State.Vehicles.Single(v => v.Id == follower.Id);
        Assert.Equal(10, Clearance(restored, restoredFollower));
        Assert.Equal(sim.SaveToJson(), restored.SaveToJson());

    }
}
