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
    public void Connected_four_way_does_not_certify_crossing_clearance_invalidation()
    {
        // Regression for the observed four-way geometry: a connected graph alone
        // does not make ClearOfCrossingAhead a positive blocker fixture.
        // Run #44670368041 proved the old assertion (blocked < 10) false.
        // This test deliberately does NOT certify positive cache invalidation.
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
        Assert.Equal(10, blocked);
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

    [Fact]
    public void Crossing_clearance_geometry_matrix_reports_positive_hits_without_mutating_state()
    {
        // Diagnostic, not a cache certification: enumerate connected two-way and
        // four-way junctions, body positions and headings. Record exact positive
        // hits rather than assuming that a road graph alone forces a blocker.
        var method = typeof(CitySimulation).GetMethod("ClearOfCrossingAhead",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        foreach (var fourWay in new[] { false, true })
        {
            var sim = new CitySimulation(42);
            sim.State.Roads.Add(new RoadNode
            {
                Id = 995001, Position = new Point(0, 4),
                Neighbours = fourWay
                    ? new List<int> { 995002, 995003, 995004, 995005 }
                    : new List<int> { 995002, 995003 }
            });
            sim.State.Roads.Add(new RoadNode { Id = 995002, Position = new Point(0, 0), Neighbours = new List<int> { 995001 } });
            sim.State.Roads.Add(new RoadNode { Id = 995003, Position = new Point(0, 8), Neighbours = new List<int> { 995001 } });
            if (fourWay)
            {
                sim.State.Roads.Add(new RoadNode { Id = 995004, Position = new Point(-4, 4), Neighbours = new List<int> { 995001 } });
                sim.State.Roads.Add(new RoadNode { Id = 995005, Position = new Point(4, 4), Neighbours = new List<int> { 995001 } });
            }
            sim.State.Vehicles.Add(new Vehicle { Id = 995101, Position = new Point(0, 0), Heading = 0, Status = VehicleStatus.Travelling });
            sim.State.Vehicles.Add(new Vehicle { Id = 995102, Position = new Point(0, 4), Heading = 0, Status = VehicleStatus.Travelling, IntersectionClaim = 995001 });
            sim = CitySimulation.FromJson(sim.SaveToJson());
            var follower = sim.State.Vehicles.Single(v => v.Id == 995101);
            var blocker = sim.State.Vehicles.Single(v => v.Id == 995102);
            int cases = 0, constrained = 0;
            double minimum = 10;
            foreach (var heading in new[] { 0.0, Math.PI / 2, Math.PI, -Math.PI / 2 })
            foreach (var x in new[] { -4.0, -2.0, 0.0, 2.0, 4.0 })
            foreach (var z in new[] { 0.0, 2.0, 4.0, 6.0, 8.0 })
            {
                blocker.Heading = heading;
                blocker.Position = new Point(x, z);
                var before = sim.SaveToJson();
                var distance = (double)method!.Invoke(sim, new object[] { follower, new Point(0, 10), 10.0 })!;
                Assert.Equal(before, sim.SaveToJson());
                Assert.True(double.IsFinite(distance) && distance >= 0 && distance <= 10,
                    $"Invalid clearance {distance} at ({x},{z}), fourWay={fourWay}");
                cases++;
                if (distance < 10) constrained++;
                minimum = Math.Min(minimum, distance);
            }
            Console.WriteLine($"PR57_CROSSING_GEOMETRY_MATRIX fourWay={fourWay} cases={cases} constrained={constrained} minimum={minimum:R}");
        }
    }

    [Fact]
    public void Crossing_route_eligibility_probe_after_external_mutation_and_reload()
    {
        // Unlike the earlier geometry matrix, these vehicles have explicit
        // connected routes. This is an opt-in research fixture, not a cache.
        var sim = new CitySimulation(42);
        sim.State.Roads.Add(new RoadNode { Id = 996001, Position = new Point(0, 0), Neighbours = new List<int> { 996002 } });
        sim.State.Roads.Add(new RoadNode { Id = 996002, Position = new Point(0, 4), Neighbours = new List<int> { 996001, 996003, 996004, 996005 } });
        sim.State.Roads.Add(new RoadNode { Id = 996003, Position = new Point(0, 8), Neighbours = new List<int> { 996002 } });
        sim.State.Roads.Add(new RoadNode { Id = 996004, Position = new Point(-4, 4), Neighbours = new List<int> { 996002 } });
        sim.State.Roads.Add(new RoadNode { Id = 996005, Position = new Point(4, 4), Neighbours = new List<int> { 996002 } });
        sim.State.Vehicles.Add(new Vehicle {
            Id = 996101, Position = new Point(0, 0), Heading = 0,
            Status = VehicleStatus.Travelling, Route = new List<int> { 996001, 996002, 996003 }
        });
        sim.State.Vehicles.Add(new Vehicle {
            Id = 996102, Position = new Point(0, 4), Heading = Math.PI / 2,
            Status = VehicleStatus.Travelling, IntersectionClaim = 996002,
            Route = new List<int> { 996004, 996002, 996005 }
        });
        sim = CitySimulation.FromJson(sim.SaveToJson());
        var clearance = typeof(CitySimulation).GetMethod("ClearOfCrossingAhead",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(clearance);
        double Check(CitySimulation city) => (double)clearance!.Invoke(city, new object[] {
            city.State.Vehicles.Single(v => v.Id == 996101), new Point(0, 10), 10.0
        })!;
        var before = sim.SaveToJson();
        var withCrossing = Check(sim);
        Assert.Equal(before, sim.SaveToJson());
        var blocker = sim.State.Vehicles.Single(v => v.Id == 996102);
        blocker.Position = new Point(100, 100);
        blocker.IntersectionClaim = null;
        var withoutCrossing = Check(sim);
        var reloaded = CitySimulation.FromJson(sim.SaveToJson());
        Assert.Equal(withoutCrossing, Check(reloaded));
        Assert.Equal(sim.SaveToJson(), reloaded.SaveToJson());
        Console.WriteLine($"PR57_CROSSING_ROUTE_ELIGIBILITY with={withCrossing:R} without={withoutCrossing:R} constrained={withCrossing < withoutCrossing}");
        Assert.InRange(withCrossing, 0, 10);
        Assert.Equal(10, withoutCrossing);
        // A real, route-eligible blocker was observed in #44678375287.
        // Require a strictly positive physical obstruction, not just a
        // diagnostic print or an unconstrained geometry fixture.
        Assert.True(withCrossing < withoutCrossing,
            $"Route-eligible crossing failed to constrain travel: {withCrossing:R} vs {withoutCrossing:R}");
        blocker.Position = new Point(0, 4);
        blocker.IntersectionClaim = 996002;
        var restoredCrossing = Check(sim);
        Assert.Equal(withCrossing, restoredCrossing);
        var blockedSnapshot = sim.SaveToJson();
        var blockedReload = CitySimulation.FromJson(blockedSnapshot);
        Assert.Equal(blockedSnapshot, blockedReload.SaveToJson());
        Assert.Equal(withCrossing, Check(blockedReload));
        // Mutating a loaded city must invalidate the same physical result.
        var loadedBlocker = blockedReload.State.Vehicles.Single(v => v.Id == 996102);
        loadedBlocker.Position = new Point(100, 100);
        loadedBlocker.IntersectionClaim = null;
        Assert.Equal(withoutCrossing, Check(blockedReload));
    }

}
