using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #227: ARTIFICIAL adversarial checks of the #226 read-only shadow.
// They test stale-read classification and the actual tow writer, NOT a
// production cache, universal writer closure, or integrated speedup.
public sealed class Pr57S6GateShadowAdversarialTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static object? Call(CitySimulation sim, string method, params object[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);

    private static long Counter(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;

    private static void SeedShadow(CitySimulation sim, params Vehicle[] vehicles)
    {
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange(vehicles);
        var shadow = (Dictionary<int, (VehicleStatus Status, Point Position)>)typeof(CitySimulation)
            .GetField("_pr57S6GateShadow", Private)!.GetValue(sim)!;
        shadow.Clear();
        foreach (var vehicle in vehicles)
            shadow.Add(vehicle.Id, (vehicle.Status, vehicle.Position));
        typeof(CitySimulation).GetField("_pr57S6GateShadowActive", Private)!
            .SetValue(sim, true);
    }

    [Fact]
    public void Own_position_changed_before_guard_is_stale_but_self_gate_remains_false()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle
        {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(20, 20)
        };
        SeedShadow(sim, reader);
        reader.Position = new Point(21, 20);
        Assert.Equal(true, Call(sim, "Pr57S6GateShadowObserve", reader, reader));
        Assert.Equal(1L, Counter(sim, "_pr57S6GateStaleSelf"));
        Assert.Equal(0L, Counter(sim, "_pr57S6GateStaleThird"));
        Assert.Equal(0L, Counter(sim, "_pr57S6GateDisagree"));
    }

    [Fact]
    public void Unmarked_public_edit_changes_third_party_gate_until_that_writer_is_refreshed()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle
        {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(20, 20)
        };
        var other = new Vehicle
        {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(100, 100)
        };
        SeedShadow(sim, reader, other);
        other.Position = new Point(21, 20);
        Assert.Equal(true, Call(sim, "Pr57S6GateShadowObserve", reader, other));
        Assert.Equal(1L, Counter(sim, "_pr57S6GateStaleThird"));
        Assert.Equal(1L, Counter(sim, "_pr57S6GateStaleThirdDisagree"));

        // Refreshing the reader cannot repair an unmarked third-party writer.
        Call(sim, "Pr57S6GateShadowAfterVehicle", reader);
        Call(sim, "Pr57S6GateShadowObserve", reader, other);
        Assert.Equal(2L, Counter(sim, "_pr57S6GateStaleThird"));
        Assert.Equal(2L, Counter(sim, "_pr57S6GateDisagree"));

        Call(sim, "Pr57S6GateShadowAfterVehicle", other);
        Call(sim, "Pr57S6GateShadowObserve", reader, other);
        Assert.Equal(2L, Counter(sim, "_pr57S6GateStaleThird"));
        Assert.Equal(2L, Counter(sim, "_pr57S6GateDisagree"));
    }

    [Fact]
    public void Actual_tow_loading_stales_target_even_after_tow_actor_refresh()
    {
        var sim = new CitySimulation(42);
        var tow = new Vehicle
        {
            Id = sim.State.NextId++, External = true, Body = TruckBody.TowHeavy,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.Loading,
            LoadTimer = 0, Position = new Point(80, 40),
            Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle
        {
            Id = sim.State.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            Position = new Point(20, 20), Heading = 0, Cause = "Sem combustível"
        };
        tow.TowTargetId = target.Id;
        SeedShadow(sim, tow, target);
        var before = sim.State.ElapsedSeconds;
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(before, sim.State.ElapsedSeconds);
        Assert.Equal(VehicleStatus.Towed, target.Status);

        Call(sim, "Pr57S6GateShadowObserve", tow, target);
        Assert.Equal(1L, Counter(sim, "_pr57S6GateStaleThird"));
        Call(sim, "Pr57S6GateShadowAfterVehicle", tow);
        Call(sim, "Pr57S6GateShadowObserve", tow, target);
        Assert.Equal(2L, Counter(sim, "_pr57S6GateStaleThird"));

        Call(sim, "Pr57S6GateShadowAfterVehicle", target);
        Call(sim, "Pr57S6GateShadowObserve", tow, target);
        Assert.Equal(2L, Counter(sim, "_pr57S6GateStaleThird"));
    }
}
