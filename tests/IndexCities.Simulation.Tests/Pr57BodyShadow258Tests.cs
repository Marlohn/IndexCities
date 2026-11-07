using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #258: ARTIFICIAL adversarial fixture. The shadow never decides motion.
public sealed class Pr57BodyShadow258Tests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);

    [Fact]
    public void Candidate_dock_or_passenger_edit_invalidates_a_previously_positive_full_body_read()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(50, 50), Heading = 0
        };
        var other = new Vehicle {
            Id = sim.State.NextId++, Body = TruckBody.Cargo,
            Status = VehicleStatus.Parked, Position = new Point(50, 60),
            DockPosition = new Point(50, 60), Heading = 0
        };
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([reader, other]);
        var frozen = Call(sim, "Pr57Body258Capture", other)!;
        Assert.Equal(10.0, (double)Call(sim, "BodyAcrossLane", reader, other)!);
        Assert.Equal(10.0, (double)Call(sim, "Pr57Body258Evaluate", reader, frozen)!);

        other.DockPosition = new Point(100, 100); // public edit, no writer journal
        Assert.Null(Call(sim, "BodyAcrossLane", reader, other));
        Assert.Equal(10.0, (double)Call(sim, "Pr57Body258Evaluate", reader, frozen)!);

        other.DockPosition = other.Position;
        other.PassengerCar = true; // also absent from the old #218 projection
        Assert.Null(Call(sim, "BodyAcrossLane", reader, other));
        Assert.Equal(10.0, (double)Call(sim, "Pr57Body258Evaluate", reader, frozen)!);
    }
}
