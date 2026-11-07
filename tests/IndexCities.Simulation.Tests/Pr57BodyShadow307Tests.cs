using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #307 ARTIFICIAL adversarial fixtures. Actual writers are invoked, but
// no synthetic outcome is claimed to represent a sustainable city or speedup.
public sealed class Pr57BodyShadow307Tests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(CitySimulation sim, string method, params object[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);
    static void Flag(CitySimulation sim, bool enabled) =>
        typeof(CitySimulation).GetField("_pr57307Nonlocal", Private)!.SetValue(sim, enabled);
    static long Count(CitySimulation sim, string field) =>
        (long)typeof(CitySimulation).GetField(field, Private)!.GetValue(sim)!;

    static (CitySimulation Sim, Vehicle Tow, Vehicle Target) Fixture(
        bool passengerCar, TowPhase phase, VehicleStatus targetStatus)
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var tow = new Vehicle
        {
            Id = state.NextId++, External = true,
            Body = passengerCar ? TruckBody.Tow : TruckBody.TowHeavy,
            Status = phase == TowPhase.Loading ? VehicleStatus.Waiting : VehicleStatus.Travelling,
            TowPhase = phase, LoadTimer = 0, Position = new Point(80, 40),
            Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle
        {
            Id = state.NextId++, External = true, PassengerCar = passengerCar,
            Body = TruckBody.Cargo, Status = targetStatus, TowTruckId = tow.Id,
            Position = new Point(20, 20), Heading = 0, Cause = "Sem combustível"
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target]);
        return (sim, tow, target);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Actual_loading_writer_requires_target_publication(bool publish)
    {
        var (sim, tow, target) = Fixture(false, TowPhase.Loading, VehicleStatus.Stranded);
        Flag(sim, publish);
        Call(sim, "Pr57306Build");
        var time = sim.State.ElapsedSeconds;
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(VehicleStatus.Towed, target.Status);
        Assert.Equal(time, sim.State.ElapsedSeconds);
        Call(sim, "Pr57306AfterActor", tow);
        Call(sim, "Pr57306Read", tow, target);
        Assert.Equal(publish ? 0L : 1L, Count(sim, "_pr57306Stale"));
        Assert.Equal(publish ? 1L : 0L, Count(sim, "_pr57307Published"));
        Call(sim, "Pr57306EndSlice");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void Actual_post_loop_sync_requires_target_publication(bool passengerCar, bool publish)
    {
        var (sim, tow, target) = Fixture(passengerCar, TowPhase.Delivering, VehicleStatus.Towed);
        Flag(sim, publish);
        Call(sim, "Pr57306Build");
        Call(sim, "Pr57306AfterActor", tow);
        Call(sim, "Pr57306AfterActor", target);
        var before = target.Position;
        Call(sim, "SyncTowedVehicles");
        Assert.NotEqual(before, target.Position);
        Call(sim, "Pr57306Read", tow, target);
        Assert.Equal(publish ? 0L : 1L, Count(sim, "_pr57306Stale"));
        Assert.Equal(publish ? 1L : 0L, Count(sim, "_pr57307Published"));
        Call(sim, "Pr57306EndSlice");
        Assert.Equal(publish ? 0L : 1L, Count(sim, "_pr57306PostSync"));
    }

    [Fact]
    public void Public_position_edit_still_bypasses_partial_publication()
    {
        var (sim, tow, target) = Fixture(false, TowPhase.Delivering, VehicleStatus.Towed);
        Flag(sim, true);
        Call(sim, "Pr57306Build");
        Call(sim, "Pr57306AfterActor", target);
        target.Position = new Point(35, 35);
        Call(sim, "Pr57306Read", tow, target);
        Assert.Equal(1L, Count(sim, "_pr57306Stale"));
        Assert.Equal(0L, Count(sim, "_pr57307Published"));
        Call(sim, "Pr57306EndSlice");
    }
}
