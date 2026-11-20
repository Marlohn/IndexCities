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
    [Fact]
    public void Connected_crossing_rechecks_live_blocker_after_external_mutation_and_reload()
    {
        // Positive control: unlike the graph-free fixture, this junction has
        // connected approaches. A clearance cache must be invalidated by
        // editor changes to both pose and claim, even without a road revision.
        var sim = new CitySimulation(42);
        sim.State.Roads.Add(new RoadNode
        {
            Id = 994101, Position = new Point(0, 4),
            Neighbours = new List<int> { 994102, 994103, 994104, 994105 }
        });
        sim.State.Roads.Add(new RoadNode { Id = 994102, Position = new Point(0, 0), Neighbours = new List<int> { 994101 } });
        sim.State.Roads.Add(new RoadNode { Id = 994103, Position = new Point(0, 8), Neighbours = new List<int> { 994101 } });
        sim.State.Roads.Add(new RoadNode { Id = 994104, Position = new Point(-4, 4), Neighbours = new List<int> { 994101 } });
        sim.State.Roads.Add(new RoadNode { Id = 994105, Position = new Point(4, 4), Neighbours = new List<int> { 994101 } });
        sim.State.Vehicles.Add(new Vehicle { Id = 994301, Position = new Point(0, 0), Heading = 0, Status = VehicleStatus.Travelling });
        sim.State.Vehicles.Add(new Vehicle
        {
            Id = 994302, Position = new Point(0, 4), Heading = Math.PI / 2,
            Status = VehicleStatus.Travelling, IntersectionClaim = 994101
        });
        sim = CitySimulation.FromJson(sim.SaveToJson());
        var method = typeof(CitySimulation).GetMethod("ClearOfCrossingAhead",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        double Clearance(CitySimulation city) =>
            (double)method!.Invoke(city, new object[]
            {
                city.State.Vehicles.Single(v => v.Id == 994301), new Point(0, 10), 10.0
            })!;
        var blocker = sim.State.Vehicles.Single(v => v.Id == 994302);
        var blocked = Clearance(sim);
        Assert.True(blocked < 10, $"Connected junction did not constrain travel: {blocked:R}");
        blocker.Position = new Point(100, 100);
        blocker.IntersectionClaim = null;
        var unblocked = Clearance(sim);
        Assert.Equal(10, unblocked);
        var snapshot = sim.SaveToJson();
        var reloaded = CitySimulation.FromJson(snapshot);
        Assert.Equal(unblocked, Clearance(reloaded));
        blocker.Position = new Point(0, 4);
        blocker.IntersectionClaim = 994101;
        Assert.Equal(blocked, Clearance(sim));
        var blockedReload = CitySimulation.FromJson(sim.SaveToJson());
        Assert.Equal(blocked, Clearance(blockedReload));
    }

}
