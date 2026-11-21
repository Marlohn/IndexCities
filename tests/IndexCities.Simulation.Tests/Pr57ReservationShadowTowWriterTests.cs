using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #201: ARTIFICIAL adversarial test of a third-party reservation write.
// The shadow remains observation-only; this is not a performance or universal
// invalidation test. Public mutable state is deliberately left unchanged.
public sealed class Pr57ReservationShadowTowWriterTests
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static object? Invoke(CitySimulation sim, string method, params object?[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);

    static T Field<T>(CitySimulation sim, string name) =>
        (T)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;

    [Fact]
    public void Loading_tow_updates_third_party_reservation_shadow_but_public_edits_still_bypass_it()
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var roadId = state.Roads.First().Id;
        var tow = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.TowHeavy,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.Loading,
            LoadTimer = 0, Position = new Point(80, 40),
            Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            IntersectionClaim = roadId, Position = new Point(20, 20),
            Heading = 0, Cause = "Sem combustível"
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target]);

        // Force only the existing observation-only shadow on this synthetic
        // fixture, without depending on the one-slice environment time gate.
        typeof(CitySimulation).GetField("_pr57ShadowActive", Private)!.SetValue(sim, true);
        foreach (var v in state.Vehicles)
            Invoke(sim, "Pr57ReservationShadowWrite", v);
        Assert.Contains(target.Id, Field<Dictionary<int, HashSet<int>>>(
            sim, "_pr57ShadowClaims")[roadId]);

        var beforeTime = state.ElapsedSeconds;
        Assert.Equal(true, Invoke(sim, "OperateTow", tow, 2.0));
        Assert.Equal(beforeTime, state.ElapsedSeconds);
        Assert.Equal(VehicleStatus.Towed, target.Status);
        Assert.Null(target.IntersectionClaim);

        // A hook at LoadTowed must update the TARGET, not merely the tow.
        Invoke(sim, "Pr57ReservationShadowCheck", new object?[] { null });
        Assert.Equal(0L, Field<long>(sim, "_pr57ShadowStaleReads"));
        var shadow = Field<Dictionary<int, (int? Claim, int[] Spaces)>>(
            sim, "_pr57ShadowStates");
        Assert.Null(shadow[target.Id].Claim);
        Assert.False(Field<Dictionary<int, HashSet<int>>>(
            sim, "_pr57ShadowClaims").TryGetValue(roadId, out var ids)
            && ids.Contains(target.Id));

        // Counterexample remains: public setters/list mutation between
        // Advance calls do not invoke any engine hook.
        target.IntersectionClaim = roadId;
        target.ManoeuvreSpaces.Add(roadId);
        Invoke(sim, "Pr57ReservationShadowCheck", new object?[] { null });
        Assert.Equal(1L, Field<long>(sim, "_pr57ShadowStaleReads"));
        Assert.Contains(target.Id, Field<HashSet<int>>(sim, "_pr57ShadowStaleIds"));
    }
}
