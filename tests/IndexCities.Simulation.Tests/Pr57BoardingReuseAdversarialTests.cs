using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57: force the otherwise rare failed-boarding path in the real simulation.
// This checks causal equivalence, not a speedup or an end-to-end benchmark.
public sealed class Pr57BoardingReuseAdversarialTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Failed_boarding_reuse_matches_original_full_json(bool car, bool matchingDriver)
    {
        var seed = new CitySimulation(42);
        var citizen = new Citizen
        {
            Id = 901001, Position = new Point(0, 0),
            Purpose = JourneyPurpose.Boarding, BoardingVehicleId = 901002
        };
        seed.State.Citizens.Add(citizen);
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 901002, PassengerCar = car,
            DriverId = matchingDriver ? citizen.Id : 901003,
            Position = new Point(50, 50), Status = VehicleStatus.Parked
        });
        var initial = seed.SaveToJson();
        var plain = CitySimulation.FromJson(initial);
        var reuse = CitySimulation.FromJson(initial);
        var previous = Environment.GetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP");
        try
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", null);
            Arrive(plain);
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", "1");
            Arrive(reuse);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", previous);
        }
        Assert.Equal(plain.SaveToJson(), reuse.SaveToJson());
        Assert.Null(reuse.State.Citizens.Single(c => c.Id == 901001).BoardingVehicleId);
        Assert.Equal(car && matchingDriver ? null : matchingDriver ? 901001 : 901003,
            reuse.State.Vehicles.Single(v => v.Id == 901002).DriverId);
        Assert.Equal(reuse.SaveToJson(), CitySimulation.FromJson(reuse.SaveToJson()).SaveToJson());
    }

    private static void Arrive(CitySimulation sim)
    {
        var method = typeof(CitySimulation).GetMethod("CitizenArrived",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(sim, new object[] { sim.State.Citizens.Single(c => c.Id == 901001) });
    }
}
