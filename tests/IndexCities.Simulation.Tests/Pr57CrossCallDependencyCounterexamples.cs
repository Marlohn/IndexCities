using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57: narrow, ARTIFICIAL counterexamples to a cross-Advance(2) cache keyed
// only by vehicle pose, sampled blocker fields and road revision. These tests
// do not implement a scheduler or claim an end-to-end speedup.
public sealed class Pr57CrossCallDependencyCounterexamples
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Preference =
        typeof(CitySimulation).GetMethod("JunctionPreferenceAllows", Private)!;
    static readonly MethodInfo Release =
        typeof(CitySimulation).GetMethod("ReleaseManoeuvre", Private)!;

    static bool Allows(CitySimulation sim, Vehicle car, RoadNode node) =>
        (bool)Preference.Invoke(sim, [car, node])!;

    [Fact]
    public void Signal_phase_and_external_control_edit_change_a_decision_without_vehicle_or_road_revision_changes()
    {
        var sim = new CitySimulation(42);
        sim.Advance(2); // A real public host-cadence boundary before external edits.
        sim.State.Vehicles.Clear();
        var node = sim.State.Roads.FirstOrDefault();
        if (node is null)
        {
            node = new RoadNode { Id = sim.State.NextId++, Position = new Point(0, 40) };
            sim.State.Roads.Add(node);
        }
        node.Control = JunctionControl.Signal;
        var car = new Vehicle
        {
            Id = sim.State.NextId++, External = true, PassengerCar = true,
            Status = VehicleStatus.Travelling, Heading = Math.PI / 2,
            Position = new Point(-6, 38.35), Fuel = 40
        };
        sim.State.Vehicles.Add(car);
        var revision = sim.State.RoadRevision;
        var pose = car.Position;
        var heading = car.Heading;

        // Same vehicle and road topology: the signal changes by time alone.
        sim.State.ElapsedSeconds = 29;
        Assert.True(Allows(sim, car, node));
        sim.State.ElapsedSeconds = 31;
        Assert.False(Allows(sim, car, node));

        // State is publicly mutable between Advance(2) calls. An edit to the
        // control predicate does not have to increment RoadRevision.
        var before = sim.SaveToJson();
        node.Control = JunctionControl.RightHand;
        Assert.Equal(revision, sim.State.RoadRevision);
        Assert.True(Allows(sim, car, node));
        node.Control = JunctionControl.Signal;
        Assert.False(Allows(sim, car, node));
        Assert.Equal(before, sim.SaveToJson());
        Assert.Equal(pose, car.Position);
        Assert.Equal(heading, car.Heading);
    }

    [Fact]
    public void Release_can_remove_a_physical_reservation_without_motion_or_road_revision()
    {
        var sim = new CitySimulation(42);
        sim.Advance(2);
        var node = sim.State.Roads.FirstOrDefault();
        if (node is null)
        {
            node = new RoadNode { Id = sim.State.NextId++, Position = new Point(0, 40) };
            sim.State.Roads.Add(node);
        }
        sim.State.Vehicles.Clear();
        var truck = new Vehicle
        {
            Id = sim.State.NextId++, External = true,
            Status = VehicleStatus.Waiting, Position = new Point(10000, 10000),
            ManoeuvreSpaces = [node.Id]
        };
        sim.State.Vehicles.Add(truck);
        var before = sim.SaveToJson();
        var revision = sim.State.RoadRevision;
        var position = truck.Position;

        // A stale held space can be released without moving its owner. A
        // pose-only invalidator cannot observe this physical ownership change.
        Release.Invoke(sim, [truck]);
        Assert.Empty(truck.ManoeuvreSpaces);
        Assert.Equal(position, truck.Position);
        Assert.Equal(revision, sim.State.RoadRevision);
        Assert.NotEqual(before, sim.SaveToJson());

        // A before/after snapshot also misses an intermediate write followed
        // by restoration. This is a synthetic restoration, not a claim that
        // the engine performs this restoration on the same slice.
        truck.ManoeuvreSpaces.Add(node.Id);
        Assert.Equal(before, sim.SaveToJson());
    }
}
