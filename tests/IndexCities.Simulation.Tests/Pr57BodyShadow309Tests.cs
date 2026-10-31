using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #309 ARTIFICIAL fixture invoking the real OperateTow -> DeliverTowed
// -> ResumeFromExterior path. No throughput or integrated city claim.
public sealed class Pr57BodyShadow309Tests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static object? Call(CitySimulation sim, string method, params object[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);

    static void Flag(CitySimulation sim, string name, bool value) =>
        typeof(CitySimulation).GetField(name, Private)!.SetValue(sim, value);

    static long Count(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;

    static (CitySimulation Sim, Vehicle Tow, Vehicle Target) Fixture()
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var tow = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Tow,
            Status = VehicleStatus.Unloading, TowPhase = TowPhase.Unloading,
            TowDestination = TowDestination.Exterior, LoadTimer = 0,
            Position = new Point(80, 40), Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle
        {
            Id = state.NextId++, External = true, PassengerCar = true,
            Status = VehicleStatus.Towed, TowTruckId = tow.Id,
            Position = new Point(20, 20), Heading = 0, Cause = "Sendo rebocado"
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target]);
        return (sim, tow, target);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void Actual_delivery_requires_nested_publication_for_final_projection(
        bool unloadPublication, bool resumePublication, bool targetAlreadyProcessed)
    {
        var (sim, tow, target) = Fixture();
        Flag(sim, "_pr57307Nonlocal", unloadPublication);
        Flag(sim, "_pr57309Resume", resumePublication);
        Call(sim, "Pr57306Build");
        if (targetAlreadyProcessed) Call(sim, "Pr57306AfterActor", target);
        var time = sim.State.ElapsedSeconds;
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(VehicleStatus.Outside, target.Status);
        Assert.Null(target.TowTruckId);
        Assert.Equal(sim.State.Entry, target.Position);
        Assert.Equal(time, sim.State.ElapsedSeconds);
        Call(sim, "Pr57306AfterActor", tow);
        Call(sim, "Pr57306Read", tow, target);
        Assert.Equal(unloadPublication && resumePublication ? 0L : 1L,
            Count(sim, "_pr57306Stale"));
        Assert.Equal(unloadPublication ? (resumePublication ? 2L : 1L) : 0L,
            Count(sim, "_pr57307Published"));
        Assert.Equal(targetAlreadyProcessed && !(unloadPublication && resumePublication) ? 1L : 0L,
            Count(sim, "_pr57306StaleAfterProcessed"));
        Call(sim, "Pr57306EndSlice");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Direct_resume_writer_is_nonlocal_to_previous_actor_refresh(bool publish)
    {
        var (sim, tow, target) = Fixture();
        target.Status = VehicleStatus.Stranded;
        target.TowTruckId = null;
        Flag(sim, "_pr57307Nonlocal", true);
        Flag(sim, "_pr57309Resume", publish);
        Call(sim, "Pr57306Build");
        Call(sim, "Pr57306AfterActor", target);
        Call(sim, "ResumeFromExterior", target);
        Assert.Equal(VehicleStatus.Outside, target.Status);
        Call(sim, "Pr57306Read", tow, target);
        Assert.Equal(publish ? 0L : 1L, Count(sim, "_pr57306Stale"));
        Assert.Equal(publish ? 1L : 0L, Count(sim, "_pr57307Published"));
        Call(sim, "Pr57306EndSlice");
    }
}
