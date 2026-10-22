using System.Collections;
using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class RoadAccessCacheScaleTests
{
    static int CacheCount(CitySimulation sim)
    {
        var field=typeof(CitySimulation).GetField("_accessCache",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.NotNull(field);
        return ((IDictionary)field!.GetValue(sim)!).Count;
    }

    [Theory]
    [InlineData(7)]
    [InlineData(42)]
    public void MoreThan4096RealBuildingQueriesStayCachedAndEquivalent(int seed)
    {
        var sim=new CitySimulation(seed);
        // Synthetic buildings only: no product population, construction or jobs.
        for(var i=0;i<4500;i++)
            sim.State.Buildings.Add(new Building{Id=10000+i,Kind=BuildingKind.House,
                Position=new Point(-160+(i%90)*3.2,-160+(i/90)*3.2),Rotation=0});
        foreach(var b in sim.State.Buildings){
            var expected=sim.State.Roads
                .Where(r=>!r.Highway&&!r.ConstructionOnly&&r.MaintenanceRatio>.05&&
                          r.Position.Distance(b.Door)<=sim.State.Tuning.RoadWidth/2+3)
                .MinBy(r=>r.Position.Distance(b.Door));
            Assert.Equal(expected?.Id,sim.Access(b)?.Id);
        }
        Assert.Equal(4500,CacheCount(sim));
        foreach(var b in sim.State.Buildings)sim.Access(b);
        Assert.Equal(4500,CacheCount(sim));
    }

    [Fact]
    public void RebuildingTheNetworkInvalidatesTheCacheWithoutStaleAccess()
    {
        var sim=new CitySimulation(42);
        var house=new Building{Id=10001,Kind=BuildingKind.House,Position=new Point(-128,18)};
        sim.State.Buildings.Add(house);
        Assert.Null(sim.Access(house));
        var road=new RoadNode{Id=20001,Position=house.Door,Highway=false};
        sim.State.Roads.Add(road);
        Rebuild(sim);
        Assert.Equal(road.Id,sim.Access(house)?.Id);
        road.MaintenanceRatio=0;
        Rebuild(sim);
        Assert.Null(sim.Access(house));
    }

    static void Rebuild(CitySimulation sim)
    {
        var method=typeof(CitySimulation).GetMethod("RebuildRoadGraph",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(sim,[true]);
    }
}
