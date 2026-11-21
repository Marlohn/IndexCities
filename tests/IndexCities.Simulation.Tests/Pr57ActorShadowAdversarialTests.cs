using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// #231 ARTIFICIAL counterexamples for the read-only actor-major shadow.
// These do not authorize reuse of shadow values by the canonical engine.
public sealed class Pr57ActorShadowAdversarialTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(CitySimulation sim, string method, params object[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);
    static long Count(CitySimulation sim, string field) =>
        (long)typeof(CitySimulation).GetField(field, Private)!.GetValue(sim)!;

    [Fact]
    public void Canonical_actor_refresh_keeps_later_selected_s3_read_coherent()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle { Id = sim.State.NextId++, Status = VehicleStatus.Travelling, Position = new Point(20, 20) };
        var other = new Vehicle { Id = sim.State.NextId++, Status = VehicleStatus.Travelling, Position = new Point(20, 10) };
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([reader, other]);
        Call(sim, "Pr57ActorShadowBuild");
        other.Position = new Point(20, 25);
        Call(sim, "Pr57ActorShadowAfterActor", other);
        Call(sim, "Pr57ActorShadowRead", reader, other, true);
        Assert.Equal(1L, Count(sim, "_pr57ActorShadowStable"));
        Assert.Equal(0L, Count(sim, "_pr57ActorShadowStaleThird"));
    }

    [Fact]
    public void Public_edit_after_refresh_is_stale_until_the_target_itself_refreshes()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle { Id = sim.State.NextId++, Status = VehicleStatus.Travelling, Position = new Point(20, 20) };
        var other = new Vehicle { Id = sim.State.NextId++, Status = VehicleStatus.Travelling, Position = new Point(20, 10) };
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([reader, other]);
        Call(sim, "Pr57ActorShadowBuild");
        Call(sim, "Pr57ActorShadowAfterActor", other);
        other.Position = new Point(20, 25); // no writer hook
        Call(sim, "Pr57ActorShadowRead", reader, other, true);
        Call(sim, "Pr57ActorShadowAfterActor", reader);
        Call(sim, "Pr57ActorShadowRead", reader, other, true);
        Assert.Equal(2L, Count(sim, "_pr57ActorShadowStaleThird"));
        Assert.Equal(2L, Count(sim, "_pr57ActorShadowStaleAfterProcessed"));
        Call(sim, "Pr57ActorShadowAfterActor", other);
        Call(sim, "Pr57ActorShadowRead", reader, other, true);
        Call(sim, "Pr57ActorShadowRead", reader, other, false);
        Assert.Equal(1L, Count(sim, "_pr57ActorShadowStable"));
        Assert.Equal(1L, Count(sim, "_pr57ActorShadowFrozen"));
    }

    [Fact]
    public void Actual_tow_writer_stales_target_even_after_refreshing_tow_actor()
    {
        var sim = new CitySimulation(42);
        var tow = new Vehicle {
            Id = sim.State.NextId++, External = true, Body = TruckBody.TowHeavy,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.Loading,
            LoadTimer = 0, Position = new Point(80, 40),
            Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle {
            Id = sim.State.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            Position = new Point(20, 20), Heading = 0, Cause = "Sem combustível"
        };
        tow.TowTargetId = target.Id;
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([tow, target]);
        Call(sim, "Pr57ActorShadowBuild");
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(VehicleStatus.Towed, target.Status);
        Call(sim, "Pr57ActorShadowAfterActor", tow);
        Call(sim, "Pr57ActorShadowRead", tow, target, true);
        Assert.Equal(1L, Count(sim, "_pr57ActorShadowStaleThird"));
        Call(sim, "Pr57ActorShadowAfterActor", target);
        Call(sim, "Pr57ActorShadowRead", tow, target, true);
        Assert.Equal(1L, Count(sim, "_pr57ActorShadowStable"));
    }
}
