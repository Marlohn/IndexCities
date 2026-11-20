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


    [Fact]
    public void Route_eligible_crossing_rechecks_external_status_and_claim_mutations_after_reload()
    {
        // A positive route-eligible crossing is known to constrain clearance.
        // Compare every live mutation to an independently deserialized state;
        // do not assume that Stranded necessarily stops occupying the crossing.
        var sim = new CitySimulation(42);
        sim.State.Roads.Add(new RoadNode { Id = 998001, Position = new Point(0, 0), Neighbours = new List<int> { 998002 } });
        sim.State.Roads.Add(new RoadNode { Id = 998002, Position = new Point(0, 4), Neighbours = new List<int> { 998001, 998003, 998004, 998005 } });
        sim.State.Roads.Add(new RoadNode { Id = 998003, Position = new Point(0, 8), Neighbours = new List<int> { 998002 } });
        sim.State.Roads.Add(new RoadNode { Id = 998004, Position = new Point(-4, 4), Neighbours = new List<int> { 998002 } });
        sim.State.Roads.Add(new RoadNode { Id = 998005, Position = new Point(4, 4), Neighbours = new List<int> { 998002 } });
        sim.State.Vehicles.Add(new Vehicle
        {
            Id = 998101, Position = new Point(0, 0), Heading = 0,
            Status = VehicleStatus.Travelling, Route = new List<int> { 998001, 998002, 998003 }
        });
        sim.State.Vehicles.Add(new Vehicle
        {
            Id = 998102, Position = new Point(0, 4), Heading = Math.PI / 2,
            Status = VehicleStatus.Travelling, IntersectionClaim = 998002,
            Route = new List<int> { 998004, 998002, 998005 }
        });
        sim = CitySimulation.FromJson(sim.SaveToJson());
        var method = typeof(CitySimulation).GetMethod("ClearOfCrossingAhead",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        double Clearance(CitySimulation city) => (double)method!.Invoke(city, new object[]
        {
            city.State.Vehicles.Single(v => v.Id == 998101), new Point(0, 10), 10.0
        })!;
        var blocker = sim.State.Vehicles.Single(v => v.Id == 998102);
        Assert.True(Clearance(sim) < 10);
        foreach (var status in new[] { VehicleStatus.Stranded, VehicleStatus.Travelling,
                     VehicleStatus.Stranded, VehicleStatus.Travelling })
        {
            blocker.Status = status;
            var snapshot = sim.SaveToJson();
            var reloaded = CitySimulation.FromJson(snapshot);
            Assert.Equal(snapshot, reloaded.SaveToJson());
            var before = Clearance(sim);
            Assert.Equal(before, Clearance(reloaded));
            Assert.Equal(snapshot, sim.SaveToJson());
            Assert.InRange(before, 0, 10);
        }
        // Route membership is also externally mutable. A cross-quantum lane
        // cache must not retain eligibility from the previous route; compare
        // the live result with a fresh deserialize at each mutation boundary.
        var originalRoute = blocker.Route.ToList();
        foreach (var route in new[] {
            new List<int>(),
            new List<int> { 998004, 998002, 998005 },
            new List<int> { 998005, 998002, 998004 },
            originalRoute
        })
        {
            blocker.Route = route;
            var routeSnapshot = sim.SaveToJson();
            var routeFresh = CitySimulation.FromJson(routeSnapshot);
            Assert.Equal(routeSnapshot, routeFresh.SaveToJson());
            Assert.Equal(Clearance(routeFresh), Clearance(sim));
            Assert.Equal(routeSnapshot, sim.SaveToJson());
        }
        // The querying vehicle's route is mutable too. A cached lane result
        // keyed only by blocker identity would miss this external mutation.
        var follower = sim.State.Vehicles.Single(v => v.Id == 998101);
        var followerRoute = follower.Route.ToList();
        foreach (var route in new[] {
            new List<int>(),
            new List<int> { 998003, 998002, 998001 },
            followerRoute
        })
        {
            follower.Route = route;
            var snapshot = sim.SaveToJson();
            var fresh = CitySimulation.FromJson(snapshot);
            Assert.Equal(snapshot, fresh.SaveToJson());
            Assert.Equal(Clearance(fresh), Clearance(sim));
            Assert.Equal(snapshot, sim.SaveToJson());
        }
        // External movement of the querying vehicle changes the geometry of
        // the clearance request even when the blocker and both routes stay put.
        // A future cross-quantum reuse must re-evaluate this dependency.
        var originalFollowerPosition = follower.Position;
        foreach (var position in new[] {
            new Point(0.5, 0), new Point(0, 1), originalFollowerPosition
        })
        {
            follower.Position = position;
            var snapshot = sim.SaveToJson();
            var fresh = CitySimulation.FromJson(snapshot);
            Assert.Equal(snapshot, fresh.SaveToJson());
            var liveClearance = Clearance(sim);
            Assert.Equal(Clearance(fresh), liveClearance);
            Assert.True(double.IsFinite(liveClearance) && liveClearance >= 0 && liveClearance <= 10);
            Assert.Equal(snapshot, sim.SaveToJson());
        }
        // Road graph topology is canonicalized during FromJson even when both
        // Neighbours lists are edited symmetrically: the loader restores edges
        // from road geometry. Such an edit is not a valid JSON-parity fixture.
        // Status, routes and claims above remain valid external mutation guards.
        blocker.IntersectionClaim = null;
        var noClaim = CitySimulation.FromJson(sim.SaveToJson());
        Assert.Equal(Clearance(noClaim), Clearance(sim));
        blocker.IntersectionClaim = 998002;
        var restored = CitySimulation.FromJson(sim.SaveToJson());
        Assert.Equal(Clearance(restored), Clearance(sim));
    }

    [Fact]
    public void Lane_occupancy_predicate_rechecks_external_status_mutation_against_fresh_state()
    {
        // Research guard for a future laneExact cache: status changes are external
        // inputs, not a property of the querying actor or its route.
        var method = typeof(CitySimulation).GetMethod("Pr57StageOccupies",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var city = new CitySimulation(42);
        city.State.Vehicles.Add(new Vehicle
        {
            Id = 997101, Position = new Point(0, 4), Heading = 0,
            Status = VehicleStatus.Travelling
        });
        bool Occupies(CitySimulation sim) => (bool)method!.Invoke(sim, new object[]
        {
            sim.State.Vehicles.Single(v => v.Id == 997101)
        })!;
        foreach (var status in new[] { VehicleStatus.Travelling, VehicleStatus.Stranded,
                     VehicleStatus.Travelling })
        {
            city.State.Vehicles.Single(v => v.Id == 997101).Status = status;
            var fresh = CitySimulation.FromJson(city.SaveToJson());
            Assert.Equal(Occupies(fresh), Occupies(city));
            Assert.Equal(fresh.SaveToJson(), city.SaveToJson());
        }
    }
    [Fact]
    public void Integrated_advance_rechecks_competing_lane_vehicles_after_external_mutation_and_reload()
    {
        // Exercise the real vehicle movement path rather than treating a
        // standalone occupancy predicate as proof of leader selection.
        var city = new CitySimulation(42);
        city.State.Roads.Add(new RoadNode { Id = 999001, Position = new Point(0, 0), Neighbours = new List<int> { 999002 } });
        city.State.Roads.Add(new RoadNode { Id = 999002, Position = new Point(0, 20), Neighbours = new List<int> { 999001, 999003 } });
        city.State.Roads.Add(new RoadNode { Id = 999003, Position = new Point(0, 40), Neighbours = new List<int> { 999002 } });
        foreach (var (id, z) in new[] { (999101, 0.0), (999102, 9.0), (999103, 15.0) })
            city.State.Vehicles.Add(new Vehicle
            {
                Id = id, Position = new Point(0, z), Heading = 0,
                Status = VehicleStatus.Travelling,
                Route = new List<int> { 999001, 999002, 999003 }
            });
        city = CitySimulation.FromJson(city.SaveToJson());
        foreach (var (status, z) in new[] {
            (VehicleStatus.Travelling, 9.0),
            (VehicleStatus.Stranded, 9.0),
            (VehicleStatus.Travelling, 30.0),
            (VehicleStatus.Travelling, 9.0)
        })
        {
            var candidate = city.State.Vehicles.Single(v => v.Id == 999102);
            candidate.Status = status;
            candidate.Position = new Point(0, z);
            var snapshot = city.SaveToJson();
            var fresh = CitySimulation.FromJson(snapshot);
            Assert.Equal(snapshot, fresh.SaveToJson());
            city.Advance(2);
            fresh.Advance(2);
            // Full serialized state includes movement, claims, economy and
            // history; any stale lane winner must become observable here.
            Assert.Equal(fresh.SaveToJson(), city.SaveToJson());
            Assert.Equal(3, city.State.Vehicles.Count(v => v.Id is >= 999101 and <= 999103));
        }
    }

    [Fact]
    public void Integrated_lane_competition_probe_requires_observable_follower_response()
    {
        // A positive control for lane-candidate optimizations: identical
        // integrated states differ only in a candidate's external position.
        // Without an observable follower response, the existing live-vs-reload
        // equality test cannot certify lane-winner invalidation.
        var city = new CitySimulation(42);
        city.State.Roads.Add(new RoadNode { Id = 999201, Position = new Point(0, 0), Neighbours = new List<int> { 999202 } });
        city.State.Roads.Add(new RoadNode { Id = 999202, Position = new Point(0, 20), Neighbours = new List<int> { 999201, 999203 } });
        city.State.Roads.Add(new RoadNode { Id = 999203, Position = new Point(0, 40), Neighbours = new List<int> { 999202 } });
        foreach (var (id, z) in new[] { (999301, 0.0), (999302, 9.0) })
            city.State.Vehicles.Add(new Vehicle
            {
                Id = id, Position = new Point(0, z), Heading = 0,
                Status = VehicleStatus.Travelling, External = true,
                // A default-constructed vehicle has no usable fuel. The prior
                // positive control was stationary even with external admission.
                Fuel = 30,
                Route = new List<int> { 999201, 999202, 999203 }
            });
        var snapshot = city.SaveToJson();
        var near = CitySimulation.FromJson(snapshot);
        var far = CitySimulation.FromJson(snapshot);
        far.State.Vehicles.Single(v => v.Id == 999302).Position = new Point(100, 100);
        near.Advance(2);
        far.Advance(2);
        var nearFollower = near.State.Vehicles.Single(v => v.Id == 999301);
        var farFollower = far.State.Vehicles.Single(v => v.Id == 999301);
        Console.WriteLine($"PR57_INTEGRATED_LANE_POSITIVE near={nearFollower.Position} far={farFollower.Position}");
        // The earlier stationary fixture had no driver and could never enter
        // physical movement. External vehicles use the canonical driver-free
        // travel path. Require actual motion before testing a lane winner.
        Assert.NotEqual(new Point(0, 0), farFollower.Position);
        Assert.NotEqual(nearFollower.Position, farFollower.Position);
        // Establish a counterfactual at the mutation boundary, not merely
        // live-vs-reload parity: both worlds share the same first quantum.
        // A cache that ignores external position would incorrectly reuse the
        // previous winner. The follower must react to the changed blocker.
        var stillBlocked = CitySimulation.FromJson(near.SaveToJson());
        stillBlocked.Advance(2);
        // Once lane competition is observable, mutate the live blocker
        // between integrated quanta and compare against a reloaded snapshot.
        // A stale cross-quantum winner must not change the full state.
        near.State.Vehicles.Single(v => v.Id == 999302).Position = new Point(100, 100);
        var mutatedSnapshot = near.SaveToJson();
        var reloaded = CitySimulation.FromJson(mutatedSnapshot);
        Assert.Equal(mutatedSnapshot, reloaded.SaveToJson());
        near.Advance(2);
        reloaded.Advance(2);
        Assert.Equal(reloaded.SaveToJson(), near.SaveToJson());
        var blockedFollower = stillBlocked.State.Vehicles.Single(v => v.Id == 999301);
        var releasedFollower = near.State.Vehicles.Single(v => v.Id == 999301);
        Console.WriteLine($"PR57_LANE_MUTATION_COUNTERFACTUAL blocked={blockedFollower.Position} released={releasedFollower.Position}");
        Assert.NotEqual(blockedFollower.Position, releasedFollower.Position);
        // Reverse the external mutation after a successful release. A retained
        // index must also notice a blocker re-entering the lane, not only
        // leaving it; compare a fresh integrated reference at the boundary.
        near.State.Vehicles.Single(v => v.Id == 999302).Position = new Point(0, 9);
        var reentrySnapshot = near.SaveToJson();
        var reentryFresh = CitySimulation.FromJson(reentrySnapshot);
        Assert.Equal(reentrySnapshot, reentryFresh.SaveToJson());
        near.Advance(2);
        reentryFresh.Advance(2);
        Assert.Equal(reentryFresh.SaveToJson(), near.SaveToJson());
        // A caller may remove and reinsert an entire vehicle between Advance
        // boundaries. An index retained across quanta must not keep a stale
        // reference to the removed instance, nor miss the reinserted instance.
        var removedBlocker = near.State.Vehicles.Single(v => v.Id == 999302);
        near.State.Vehicles.Remove(removedBlocker);
        var removalSnapshot = near.SaveToJson();
        var removalFresh = CitySimulation.FromJson(removalSnapshot);
        Assert.Equal(removalSnapshot, removalFresh.SaveToJson());
        near.Advance(2);
        removalFresh.Advance(2);
        Assert.Equal(removalFresh.SaveToJson(), near.SaveToJson());
        // Reinsert the same persisted identity with a new object and a nearby
        // location. Compare to an independent reload before another quantum.
        removedBlocker.Position = new Point(0, 9);
        near.State.Vehicles.Add(removedBlocker);
        var reinsertionSnapshot = near.SaveToJson();
        var reinsertionFresh = CitySimulation.FromJson(reinsertionSnapshot);
        Assert.Equal(reinsertionSnapshot, reinsertionFresh.SaveToJson());
        near.Advance(2);
        reinsertionFresh.Advance(2);
        Assert.Equal(reinsertionFresh.SaveToJson(), near.SaveToJson());
        // A Step-scoped spatial workset must also remain causal on subsequent
        // quanta, not merely at the first post-mutation boundary. Reload a
        // fresh reference at every checkpoint to expose retained stale state.
        for (var quantum = 0; quantum < 3; quantum++)
        {
            var checkpoint = near.SaveToJson();
            var freshCheckpoint = CitySimulation.FromJson(checkpoint);
            Assert.Equal(checkpoint, freshCheckpoint.SaveToJson());
            near.Advance(2);
            freshCheckpoint.Advance(2);
            Assert.Equal(freshCheckpoint.SaveToJson(), near.SaveToJson());
        }
    }

}
