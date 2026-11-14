using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// Run this test in two fresh processes with PR57_DYNAMIC_BVH=0 and =1.
// The complete persisted state (not only the vehicle positions) is compared
// by the integrated-scale workflow, with no numeric tolerance.
public sealed class Pr57DynamicBvhTests
{
    [Fact]
    public void Bvh_preserves_integrated_vehicle_and_economic_state_across_save_load()
    {
        var sim = CityCycleTests.City();
        Assert.NotEmpty(sim.State.Vehicles);
        for (var hour = 0; hour < 24; hour++)
            sim.Advance(3600);
        sim.CheckInvariants();
        Assert.NotEmpty(sim.State.Vehicles);
        Assert.NotEmpty(sim.State.Citizens.Where(c => c.Alive));

        // Short host-cadence movement slices exercise refit after individual
        // vehicles move, while longer slices exercise between-slice invalidation.
        for (var i = 0; i < 40; i++)
        {
            sim.Advance(2);
            sim.CheckInvariants();
        }
        var checkpoint = sim.SaveToJson();
        var resumed = CitySimulation.FromJson(checkpoint);
        Assert.Equal(checkpoint, resumed.SaveToJson());
        // First construct the opt-in BVH on the resumed object. Mutating
        // positions after this slice must refit an existing tree, rather than
        // merely populate a fresh tree from already-mutated positions.
        resumed.Advance(2);
        resumed.CheckInvariants();
        Assert.True(resumed.State.Vehicles.Count > 1);
        // Swap existing positions without changing roster identity. Both
        // baseline and opt-in must observe the mutation on the next slice.
        var first = resumed.State.Vehicles[0];
        var second = resumed.State.Vehicles[1];
        var firstPosition = first.Position;
        first.Position = second.Position;
        second.Position = firstPosition;
        resumed.Advance(2);
        resumed.CheckInvariants();
        // Separately exercise roster invalidation after a tree already exists.
        // Reversing the list must rebuild index-to-ID mapping before queries.
        resumed.State.Vehicles.Reverse();
        for (var i = 0; i < 40; i++)
        {
            resumed.Advance(2);
            resumed.CheckInvariants();
        }
        Assert.NotEmpty(resumed.State.Vehicles);
        Assert.NotEmpty(resumed.State.Citizens.Where(c => c.Alive));
        Assert.Equal(resumed.State.Tuning.TotalMoney, resumed.MonetaryTotal);
        var json = resumed.SaveToJson();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        Console.WriteLine($"PR57_DYNAMIC_BVH_JSON mode={Environment.GetEnvironmentVariable("PR57_DYNAMIC_BVH") ?? "0"} sha256={hash} vehicles={resumed.State.Vehicles.Count} alive={resumed.State.Citizens.Count(c => c.Alive)}");
    }
}
