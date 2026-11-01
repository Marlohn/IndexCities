using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class Pr57OrderFrontier570Tests
{
    static readonly MethodInfo Ordered = typeof(CitySimulation).GetMethod(
        "Pr57OrderedVehicles570", BindingFlags.NonPublic | BindingFlags.Instance)!;

    static void Exact(CitySimulation sim)
    {
        var expected = sim.State.Vehicles.ToArray();
        Array.Sort(expected, static (a,b) =>
            a.WaitingSince.CompareTo(b.WaitingSince) is var c and not 0 ? c : a.Id.CompareTo(b.Id));
        var actual = (Vehicle[])Ordered.Invoke(sim, null)!;
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++) Assert.Same(expected[i], actual[i]);
    }

    [Fact]
    public void External_edits_and_intraslice_writers_keep_exact_canonical_order()
    {
        var sim = new CitySimulation(42, new() { ExternalTruckCount = 1 });
        var vehicles = sim.State.Vehicles;
        vehicles.Clear();
        var first = new Vehicle { Id = 1001, WaitingSince = 4 };
        var second = new Vehicle { Id = 1002, WaitingSince = 0 };
        var third = new Vehicle { Id = 1003, WaitingSince = 4 };
        vehicles.AddRange([first, second, third]);
        Exact(sim); Exact(sim);
        first.WaitingSince = -3; Exact(sim);
        first.WaitingSince = 4; Exact(sim);
        third.Id = 999; Exact(sim);
        vehicles.Reverse(); Exact(sim);
        vehicles[0] = new Vehicle { Id = 1003, WaitingSince = 4 }; Exact(sim);
        vehicles.Add(new Vehicle { Id = 2000, WaitingSince = -0.0 }); Exact(sim);
        vehicles.RemoveAt(1); Exact(sim);
        vehicles[0].WaitingSince = double.PositiveInfinity; Exact(sim);
        vehicles[0].WaitingSince = double.NaN; Exact(sim);
    }
}
