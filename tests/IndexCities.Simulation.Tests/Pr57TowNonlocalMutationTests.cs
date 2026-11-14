using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #169: ARTIFICIAL adversarial counterexample, not an optimization.
// A tow's final synchronization writes the carried vehicle after the ordered
// vehicle loop. A mirror updated only for the current actor becomes stale.
// This test deliberately invokes the existing physical synchronization;
// it does not alter towing, skip a slice or invent economic activity.
public sealed class Pr57TowNonlocalMutationTests
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static void Call(CitySimulation sim, string name) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, null);

    static long Counter(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Towed_car_or_heavy_truck_changes_pose_after_the_vehicle_loop(bool passengerCar)
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var tow = new Vehicle
        {
            Id = state.NextId++, External = true,
            Body = passengerCar ? TruckBody.Tow : TruckBody.TowHeavy,
            Status = VehicleStatus.Travelling, TowPhase = TowPhase.Delivering,
            Position = new Point(80, 40), Heading = Math.PI / 2, Fuel = 40
        };
        var carried = new Vehicle
        {
            Id = state.NextId++, External = true,
            PassengerCar = passengerCar, Body = TruckBody.Cargo,
            Status = VehicleStatus.Towed, TowTruckId = tow.Id,
            Position = new Point(20, 20), Heading = 0
        };
        tow.TowTargetId = carried.Id;
        state.Vehicles.AddRange([tow, carried]);

        var previousCarriedPosition = carried.Position;
        var previousTowPosition = tow.Position;
        var previousTime = state.ElapsedSeconds;
        // Force a sampled slice of the strictly observational #168 audit.
        typeof(CitySimulation).GetField("_pr57MutationSlices", Private)!.SetValue(sim, 96L);
        Call(sim, "Pr57MutationBeginSlice");
        Call(sim, "SyncTowedVehicles");
        Call(sim, "Pr57MutationAfterSync");

        var expected = passengerCar ? tow.Position
            : tow.Position - new Point(Math.Sin(tow.Heading), Math.Cos(tow.Heading)) * 6.5;
        Assert.NotEqual(previousCarriedPosition, carried.Position);
        Assert.InRange(carried.Position.Distance(expected), 0, 1e-8);
        Assert.Equal(tow.Heading, carried.Heading);
        Assert.Equal(previousTowPosition, tow.Position);
        Assert.Equal(previousTime, state.ElapsedSeconds);
        Assert.Equal(1L, Counter(sim, "_pr57MutationSyncVehicleChanges"));
        Assert.Equal(0L, Counter(sim, "_pr57MutationThirdPartyChangedPasses"));
    }
}
