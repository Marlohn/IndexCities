using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #232: ARTIFICIAL negative-dependency counterexample.
// This is a read-only probe of the canonical s3 dock-body predicate, not a cache.
public sealed class Pr57ShadowReadsetAdversarialTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static object? Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);

    static long Count(CitySimulation sim, string field) =>
        (long)typeof(CitySimulation).GetField(field, Private)!.GetValue(sim)!;

    [Fact]
    public void Dock_position_changes_s3_hold_while_actor_shadow_six_fields_remain_identical()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(50, 50), Heading = 0
        };
        var candidate = new Vehicle {
            Id = sim.State.NextId++, Body = TruckBody.Cargo,
            Status = VehicleStatus.Parked, Position = new Point(50, 60),
            DockPosition = new Point(50, 60), Heading = 0
        };
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([reader, candidate]);

        Call(sim, "Pr57ActorShadowBuild");
        Call(sim, "Pr57ActorShadowAfterActor", candidate);
        var original = Call(sim, "BodyAcrossLane", reader, candidate);
        Assert.NotNull(original);
        Assert.True((double)original! > 0);

        // A public edit of DockPosition does not alter any of the six #231
        // fields (status, position, heading, route index, claim, space count).
        // This edit is deliberately outside any canonical writer hook.
        candidate.DockPosition = new Point(100, 100);
        Call(sim, "Pr57ActorShadowRead", reader, candidate, true);
        Assert.Equal(1L, Count(sim, "_pr57ActorShadowStable"));
        Assert.Equal(0L, Count(sim, "_pr57ActorShadowStaleThird"));

        // The same canonical s3 dock predicate now rejects this candidate.
        Assert.Null(Call(sim, "BodyAcrossLane", reader, candidate));
    }
}
