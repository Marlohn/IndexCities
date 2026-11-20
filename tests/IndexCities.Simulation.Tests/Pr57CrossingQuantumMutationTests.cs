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
        // A bare vehicle with no crossing graph is not necessarily a blocker.
        // Find a genuinely constrained live pose before testing invalidation;
        // never count an always-clear fixture as evidence of cache correctness.
        double blocked = 10;
        Point? blockingPose = null;
        foreach (var x in new[] { -4.0, -2.0, 0.0, 2.0, 4.0 })
        {
            foreach (var z in new[] { 1.0, 2.0, 4.0, 6.0, 8.0 })
            {
                crossing.Position = new Point(x, z);
                blocked = Clearance(sim, follower);
                if (blocked >= 10) continue;
                blockingPose = crossing.Position;
                break;
            }
            if (blockingPose is not null) break;
        }
        Assert.True(blockingPose is not null,
            "No crossing pose constrained travel in the graph-free fixture; a real junction fixture is required.");
        crossing.Position = blockingPose!.Value;
        crossing.Position = new Point(100, 100);
        crossing.IntersectionClaim = null;
        Assert.Equal(10, Clearance(sim, follower));

        var restored = CitySimulation.FromJson(sim.SaveToJson());
        Assert.Equal(10, Clearance(restored, restored.State.Vehicles.Single(v => v.Id == follower.Id)));
        crossing.Position = blockingPose.Value;
        crossing.IntersectionClaim = 994101;
        Assert.Equal(blocked, Clearance(sim, follower));
    }
}
