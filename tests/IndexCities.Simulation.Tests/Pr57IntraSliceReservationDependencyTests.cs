using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57: ARTIFICIAL two-vehicle dependency repro. It does not change the
// scheduler, simulate a sustainable city, or claim a performance improvement.
// Calls are deliberately made at the same movement time: an occupancy cache
// built once at a slice boundary would miss these causal transitions.
public sealed class Pr57IntraSliceReservationDependencyTests
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Admit = typeof(CitySimulation).GetMethod("MayEnterJunction", Private)!;
    static readonly MethodInfo Release = typeof(CitySimulation).GetMethod("ReleaseManoeuvre", Private)!;

    static bool MayEnter(CitySimulation sim, Vehicle car, RoadNode node) =>
        (bool)Admit.Invoke(sim, [car, node])!;

    [Fact]
    public void Release_and_new_admission_change_the_winner_without_moving_any_vehicle_or_advancing_time()
    {
        var initial = new CitySimulation(42);
        var west = new RoadNode { Id = 1000, Position = new Point(-4, 40) };
        var crossing = new RoadNode { Id = 1001, Position = new Point(0, 40),
            Control = JunctionControl.RightHand };
        var east = new RoadNode { Id = 1002, Position = new Point(4, 40) };
        var south = new RoadNode { Id = 1003, Position = new Point(0, 36) };
        var north = new RoadNode { Id = 1004, Position = new Point(0, 44) };
        initial.State.Roads.AddRange([west, crossing, east, south, north]);
        var sim = new CitySimulation(initial.State);
        crossing = sim.State.Roads.Single(r => r.Id == crossing.Id);
        Assert.True(crossing.Neighbours.Count >= 3);
        sim.State.Vehicles.Clear();
        sim.State.Citizens.Clear();

        // A stale physical reservation can belong to a vehicle with no
        // movement, and ReleaseManoeuvre clears it during movement cleanup.
        var owner = new Vehicle
        {
            Id = 2000, External = true, Status = VehicleStatus.Waiting,
            Position = new Point(10000, 10000), ManoeuvreSpaces = [crossing.Id]
        };
        var eastbound = new Vehicle
        {
            Id = 2001, External = true, PassengerCar = true,
            Status = VehicleStatus.Travelling, Fuel = 40,
            Position = new Point(-8, 38.35), Heading = Math.PI / 2,
            Route = [west.Id, crossing.Id, east.Id], RouteIndex = 1
        };
        var northbound = new Vehicle
        {
            Id = 2002, External = true, PassengerCar = true,
            Status = VehicleStatus.Travelling, Fuel = 40,
            Position = new Point(1.65, 30), Heading = 0,
            Route = [south.Id, crossing.Id, north.Id], RouteIndex = 1
        };
        sim.State.Vehicles.AddRange([owner, eastbound, northbound]);
        var revision = sim.State.RoadRevision;
        var time = sim.State.ElapsedSeconds;
        var poses = sim.State.Vehicles.Select(v => v.Position).ToArray();

        Assert.False(MayEnter(sim, eastbound, crossing));
        Assert.Null(eastbound.IntersectionClaim);
        Assert.Contains(crossing.Id, owner.ManoeuvreSpaces);

        Release.Invoke(sim, [owner]);
        Assert.Empty(owner.ManoeuvreSpaces);
        Assert.True(MayEnter(sim, eastbound, crossing));
        Assert.Equal(crossing.Id, eastbound.IntersectionClaim);
        Assert.Contains(crossing.Id, eastbound.ManoeuvreSpaces);

        // The newly admitted owner immediately blocks a different approach,
        // even though neither car has advanced to the crossing yet.
        Assert.False(MayEnter(sim, northbound, crossing));
        Assert.Null(northbound.IntersectionClaim);
        Assert.Equal(revision, sim.State.RoadRevision);
        Assert.Equal(time, sim.State.ElapsedSeconds);
        Assert.Equal(poses, sim.State.Vehicles.Select(v => v.Position).ToArray());
    }
}
