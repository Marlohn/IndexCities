using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #170: ARTIFICIAL adversarial test of the ACTUAL OperateTow branch
// reached from MoveVehiclesUnsplit. A loading tow mutates the carried vehicle
// before the next actor is processed. No production rules are changed.
public sealed class Pr57TowIntraSliceMutationTests
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static object? Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);

    private static long Counter(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;

    [Fact]
    public void Loading_tow_writes_other_vehicle_status_and_pose_before_next_actor()
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();

        var tow = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.TowHeavy,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.Loading,
            LoadTimer = 0, Position = new Point(80, 40), Heading = Math.PI / 2,
            Fuel = 40
        };
        var stranded = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            Position = new Point(20, 20), Heading = 0,
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
        // This is a test of its detection boundary, not a sparse mirror.
        typeof(CitySimulation).GetField("_pr57MutationSlices", Private)!.SetValue(sim, 96L);
        Call(sim, "Pr57MutationBeginSlice");

        var oldTime = state.ElapsedSeconds;
        var oldTargetPosition = stranded.Position;
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));

        Assert.Equal(VehicleStatus.Towed, stranded.Status);
        Assert.NotEqual(oldTargetPosition, stranded.Position);
        Assert.Equal(tow.Heading, stranded.Heading);
        Assert.Equal(oldTime, state.ElapsedSeconds);

        // A mirror refreshed for the current actor alone remains stale:
        // another actor's subsequent physical read observes the target write.
        Call(sim, "Pr57MutationBeforeLeaderScan", nextActor);
        Assert.Equal(1L, Counter(sim, "_pr57MutationBeforeQueryDivergences"));

        // The expensive full-world audit catches this write and resets its
        // observation baseline; it is NOT an efficient write-through design.
        Call(sim, "Pr57MutationAfterVehicle", tow);
        Assert.Equal(1L, Counter(sim, "_pr57MutationThirdPartyChangedPasses"));
        Assert.Equal(1L, Counter(sim, "_pr57MutationThirdPartyVehicleChanges"));
        Call(sim, "Pr57MutationBeforeLeaderScan", nextActor);
        Assert.Equal(1L, Counter(sim, "_pr57MutationBeforeQueryDivergences"));
        Assert.Equal(2L, Counter(sim, "_pr57MutationBeforeQueryChecks"));
    }
}
