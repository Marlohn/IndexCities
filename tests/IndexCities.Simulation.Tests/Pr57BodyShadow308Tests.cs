using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #308: ARTIFICIAL adversarial fixture, real OperateTow -> AbortTow writer.
// Only the observational #306 shadow is published; authoritative state is unchanged.
public sealed class Pr57BodyShadow308Tests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(CitySimulation sim, string method, params object[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);
    static long Count(CitySimulation sim, string field) =>
        (long)typeof(CitySimulation).GetField(field, Private)!.GetValue(sim)!;
    static void Flag(CitySimulation sim, bool enabled) =>
        typeof(CitySimulation).GetField("_pr57307Nonlocal", Private)!.SetValue(sim, enabled);

    static (CitySimulation Sim, Vehicle Tow, Vehicle Target) Fixture(bool linked)
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var tow = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Tow,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.ToVehicle,
            Position = new Point(80, 40), Heading = 0, Fuel = 40
        };
        var target = new Vehicle
        {
            Id = state.NextId++, External = true, PassengerCar = true,
            Status = VehicleStatus.Travelling, Position = new Point(20, 20),
            Heading = 0, TowTruckId = linked ? tow.Id : null
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target]);
        return (sim, tow, target);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Actual_abort_writer_requires_target_publication_even_after_target_processed(
        bool publish, bool targetAlreadyProcessed)
    {
        var (sim, tow, target) = Fixture(linked: true);
        Flag(sim, publish);
        Call(sim, "Pr57306Build");
        if (targetAlreadyProcessed) Call(sim, "Pr57306AfterActor", target);
        var time = sim.State.ElapsedSeconds;
        // Travelling target is not AwaitingTow: actual OperateTow abort branch.
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Null(target.TowTruckId);
        Assert.Equal(time, sim.State.ElapsedSeconds);
        Call(sim, "Pr57306AfterActor", tow);
        Call(sim, "Pr57306Read", tow, target);
        Assert.Equal(publish ? 0L : 1L, Count(sim, "_pr57306Stale"));
        Assert.Equal(publish ? 1L : 0L, Count(sim, "_pr57307Published"));
        Assert.Equal(targetAlreadyProcessed && !publish ? 1L : 0L,
            Count(sim, "_pr57306StaleAfterProcessed"));
        Call(sim, "Pr57306EndSlice");
    }

    [Fact]
    public void Abort_of_unlinked_target_does_not_publish_unchanged_target()
    {
        var (sim, tow, target) = Fixture(linked: false);
        Flag(sim, true);
        Call(sim, "Pr57306Build");
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Null(target.TowTruckId);
        Call(sim, "Pr57306AfterActor", tow);
        Call(sim, "Pr57306Read", tow, target);
        Assert.Equal(0L, Count(sim, "_pr57306Stale"));
        Assert.Equal(0L, Count(sim, "_pr57307Published"));
        Call(sim, "Pr57306EndSlice");
    }
}
