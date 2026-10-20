using System.Collections;
using System.Reflection;
using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

public sealed class WalkRouteCacheTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;

    // Stored walks, and how many differ from a fresh search over the network as it is now.
    static (int Stored,int Stale) StoredWalks(CitySimulation sim)
    {
        var routes=(IDictionary)typeof(CitySimulation).GetField("_walkRoutes",Private)!.GetValue(sim)!;
        var find=typeof(CitySimulation).GetMethod("FindPedestrianPath",Private)!;var stale=0;
        foreach(DictionaryEntry entry in routes){
            double Coordinate(string name)=>BitConverter.Int64BitsToDouble((long)entry.Key.GetType().GetProperty(name)!.GetValue(entry.Key)!);
            var access=(RoadNode?)entry.Key.GetType().GetProperty("Access")!.GetValue(entry.Key);
            var fresh=(List<Point>)find.Invoke(sim,[new Point(Coordinate("FromX"),Coordinate("FromZ")),new Point(Coordinate("DoorX"),Coordinate("DoorZ")),access])!;
            if(!fresh.SequenceEqual((Point[])entry.Value!))stale++;
        }
        return (routes.Count,stale);
    }

    [Fact]
    public void Stored_walks_do_not_survive_network_changes_made_between_advances()
    {
        var sim=CityCycleTests.City();
        for(var i=0;i<1800;i++)sim.Advance(2);
        var (stored,stale)=StoredWalks(sim);Assert.True(stored>0);Assert.Equal(0,stale);
        // Changes from outside the simulation code move no epoch: paint removed,
        // streets worn out and a lot dropped directly in the state.
        var crossings=sim.State.Roads.Where(r=>r.Crosswalk).ToList();var worn=sim.State.Roads.Where(r=>!r.Highway).Take(20).ToList();
        var lot=sim.State.Buildings.First(b=>b.Kind==BuildingKind.House);
        foreach(var crossing in crossings)crossing.Crosswalk=false;
        foreach(var road in worn)road.MaintenanceRatio=0;
        lot.Removed=true;
        Assert.True(StoredWalks(sim).Stale>0);
        sim.Advance(2);Assert.Equal(0,StoredWalks(sim).Stale);
        for(var i=0;i<1800;i++)sim.Advance(2);
        (stored,stale)=StoredWalks(sim);Assert.True(stored>0);Assert.Equal(0,stale);
        sim.CheckInvariants();
    }

    [Fact]
    public void Restoring_a_network_changed_between_advances_does_not_revive_routes_stored_meanwhile()
    {
        var sim=CityCycleTests.City();
        for(var i=0;i<1800;i++)sim.Advance(2);
        var crossings=sim.State.Roads.Where(r=>r.Crosswalk).ToList();
        foreach(var crossing in crossings)crossing.Crosswalk=false;
        // Walks planned over the unpainted network, with no epoch change on the way.
        var stamp=typeof(CitySimulation).GetProperty("WalkStamp",Private)!;var unpainted=stamp.GetValue(sim);
        sim.Advance(3600);Assert.Equal(unpainted,stamp.GetValue(sim));
        var (stored,stale)=StoredWalks(sim);Assert.True(stored>0);Assert.Equal(0,stale);
        foreach(var crossing in crossings)crossing.Crosswalk=true;
        Assert.True(StoredWalks(sim).Stale>0);
        sim.Advance(2);Assert.Equal(0,StoredWalks(sim).Stale);
        sim.CheckInvariants();
    }
}
