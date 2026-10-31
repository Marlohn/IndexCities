using System.Collections;
using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #545: a logical writer journal must retain the original vehicle order
// and publish in-place route mutations before another physical reader runs.
public sealed class Pr57WriterJunctionTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static object? Call(CitySimulation sim, string method, params object?[] args)
    {
        var m = typeof(CitySimulation).GetMethod(method, Private);
        Assert.NotNull(m);
        return m.Invoke(sim, args);
    }

    static Vehicle[] Source(CitySimulation sim, RoadNode node) =>
        ((IEnumerable<Vehicle>)Call(sim, "Pr57WriterJunctionCandidates", node)!).ToArray();

    [Fact]
    public void Writer_updates_inplace_route_and_keeps_original_vehicle_order()
    {
        if (Environment.GetEnvironmentVariable("PR57_WRITER_JUNCTION_INDEX") != "1") return;
        var sim = new CitySimulation(42, new() { ExternalTruckCount = 3 });
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success);
        Assert.True(sim.PlaceRoad(new(-192, 0), new(-80, 0)).Success);
        var ids = sim.State.Roads.Where(r => Math.Abs(r.Position.Z) < .01)
            .OrderBy(r => r.Position.X).Take(4).Select(r => r.Id).ToArray();
        Assert.True(ids.Length >= 3);
        var actors = sim.State.Vehicles.Take(3).ToArray();
        Assert.Equal(3, actors.Length);
        foreach (var v in actors)
        {
            v.Status = VehicleStatus.Travelling;
            v.Route = [ids[0], ids[1], ids[2]];
            v.RouteIndex = 1;
        }
        var first = sim.State.Roads.First(r => r.Id == ids[1]);
        var second = sim.State.Roads.First(r => r.Id == ids[2]);
        Call(sim, "Pr57WriterJunctionBegin", 2.0, null);
        try
        {
            Assert.Equal(sim.State.Vehicles.Where(v => v.RouteIndex < v.Route.Count &&
                v.Route[v.RouteIndex] == first.Id), Source(sim, first));
            // Mutate the same route object inside the active writer quantum.
            Call(sim, "Pr57WriterJunctionBeforeActor", actors[0]);
            actors[0].RouteIndex = 2;
            Assert.Equal(sim.State.Vehicles.Where(v => v.RouteIndex < v.Route.Count &&
                v.Route[v.RouteIndex] == first.Id), Source(sim, first));
            Call(sim, "Pr57WriterJunctionAfterActor", actors[0]);
            Assert.Equal(new[] { actors[0] }, Source(sim, second));
            Call(sim, "Pr57WriterJunctionBeforeActor", actors[1]);
            actors[1].Route[1] = ids[2]; // in-place, same Count
            Assert.Equal(new[] { actors[0], actors[1] }, Source(sim, second));
            Call(sim, "Pr57WriterJunctionAfterActor", actors[1]);
            // An untracked collection edit invalidates the lease and falls
            // back to the complete original-order scan, never a partial set.
            sim.State.Vehicles.Add(new Vehicle { Id = 987654 });
            Assert.Equal(sim.State.Vehicles, Source(sim, first));
        }
        finally { Call(sim, "Pr57WriterJunctionEnd"); }
        Assert.Equal(sim.State.Vehicles, Source(sim, first));
    }
}
