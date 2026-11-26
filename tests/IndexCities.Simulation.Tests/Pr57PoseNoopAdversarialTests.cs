using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #237: artificial adversarial checks of the exact #236 heading state gate.
// They do not replace integrated Advance(2) parity or prove throughput at scale.
public sealed class Pr57PoseNoopAdversarialTests
{
    const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    static bool Noop(double current, double approach) =>
        (bool)typeof(CitySimulation).GetMethod("Pr57PoseHeadingNoop", PrivateStatic)!
            .Invoke(null, [current, approach])!;

    [Fact]
    public void Bitwise_heading_gate_distinguishes_signed_zero_and_real_turns()
    {
        var negativeZero = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000UL));
        Assert.True(Noop(0.0, 0.0));
        Assert.True(Noop(Math.PI / 2, Math.PI / 2));
        Assert.False(Noop(0.0, negativeZero));
        Assert.False(Noop(negativeZero, 0.0));
        Assert.False(Noop(0.0, Math.PI / 2));
    }

    [Fact]
    public void Differing_heading_can_reach_a_real_pose_blocker_and_must_not_be_skipped()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle
        {
            Id = sim.State.NextId++, Status = VehicleStatus.Waiting,
            Position = new Point(50, 50), Heading = 0, Body = TruckBody.Cargo
        };
        var other = new Vehicle
        {
            Id = sim.State.NextId++, Status = VehicleStatus.Waiting,
            Position = new Point(50, 55.8), Heading = 0, Body = TruckBody.Cargo
        };
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([reader, other]);
        var predicate = typeof(CitySimulation).GetMethod("Pr57PoseGuardForeach", PrivateInstance)!;
        Assert.False(Noop(reader.Heading, Math.PI / 2));
        Assert.True((bool)predicate.Invoke(sim, [reader, Math.PI / 2])!);
        Assert.True(Noop(reader.Heading, reader.Heading));
        Assert.False((bool)predicate.Invoke(sim, [reader, reader.Heading])!);
    }
}
