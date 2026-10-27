using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #170: ARTIFICIAL adversarial test. A tow reaching its pickup point
// writes to a different vehicle DURING the ordered movement loop, not only
// in the final SyncTowedVehicles pass (#169). No production rules are changed.
public sealed class Pr57TowIntraSliceMutationTests
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static void Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);

    private static long Counter(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;

    [Fact]
    public void Pickup_writes_to_other_vehicle_before_next_actor_reads_the_world()
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();

        var tow = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Tow,
            Status = VehicleStatus.Travelling, TowPhase = TowPhase.ToVehicle,
            Position = new Point(80, 40), Heading = 0, Fuel = 40
        };
        var stranded = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            Position = new Point(80, 50), Heading = 0,
            Cause = "Sem combustível"
        };
        var nextActor = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Travelling, Position = new Point(80, 60)
        };
        tow.TowTargetId = stranded.Id;
        state.Vehicles.AddRange([tow, stranded, nextActor]);

        // Force one sampled slice of the existing exact full-Vehicle audit.
        // The audit is observational, disabled in normal simulation.
        typeof(CitySimulation).GetField("_pr57MutationSlices", Private)!.SetValue(sim, 96L);
        Call(sim, "Pr57MutationBeginSlice");

        var oldTime = state.ElapsedSeconds;
        var oldTargetPosition = stranded.Position;
        Call(sim, "TowReachedDock", tow);

        Assert.Equal(TowPhase.Loading, tow.TowPhase);
        Assert.Equal(VehicleStatus.Waiting, tow.Status);
        Assert.Equal(VehicleStatus.Stranded, stranded.Status);
        Assert.Contains("Sendo carregado", stranded.Cause);
        Assert.Equal(oldTargetPosition, stranded.Position);
        Assert.Equal(oldTime, state.ElapsedSeconds);

        // Updating only the current actor (tow) leaves the stranded vehicle
        // stale. A different actor's next physical read must see that change.
        Call(sim, "Pr57MutationBeforeLeaderScan", nextActor);
        Assert.Equal(1L, Counter(sim, "_pr57MutationBeforeQueryDivergences"));

        // The existing expensive FULL-WORLD audit detects and repairs its
        // observation mirror. This is NOT a sparse write-through optimization.
        Call(sim, "Pr57MutationAfterVehicle", tow);
        Assert.Equal(1L, Counter(sim, "_pr57MutationThirdPartyChangedPasses"));
        Assert.Equal(1L, Counter(sim, "_pr57MutationThirdPartyVehicleChanges"));
        Call(sim, "Pr57MutationBeforeLeaderScan", nextActor);
        Assert.Equal(1L, Counter(sim, "_pr57MutationBeforeQueryDivergences"));
        Assert.Equal(2L, Counter(sim, "_pr57MutationBeforeQueryChecks"));
    }
}
