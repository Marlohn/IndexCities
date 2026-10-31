using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// #542: mutations inside one lease, including a route changed in place and
// road/tuning changes that do not necessarily advance the road epoch.
public sealed class Pr57MovementTargetWorksetTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Invoke(CitySimulation sim,string name)
    {
        var method=typeof(CitySimulation).GetMethod(name,Private);
        Assert.NotNull(method);
        method.Invoke(sim,null);
    }
    static object?[] Target(CitySimulation sim,string name,Vehicle v)
    {
        var method=typeof(CitySimulation).GetMethod(name,Private);
        Assert.NotNull(method);
        object?[] args=[v,null,null,null,0d,0d];
        var found=(bool)method.Invoke(sim,args)!;
        Assert.True(found);
        return args;
    }
    static void MatchesFresh(CitySimulation sim,Vehicle v)
    {
        var cached=Target(sim,"TryMovementTarget",v);
        var fresh=Target(sim,"Pr57MovementTargetFresh",v);
        for(var i=1;i<cached.Length;i++)Assert.Equal(fresh[i],cached[i]);
    }
    [Fact]
    public void Route_road_and_tuning_writers_invalidate_before_next_reader()
    {
        if(Environment.GetEnvironmentVariable("PR57_STEP_MOVEMENT_TARGET_WORKSET")!="1")return;
        var sim=new CitySimulation(42,new(){ExternalTruckCount=1});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-80,0)).Success);
        var route=sim.State.Roads.Where(r=>Math.Abs(r.Position.Z)<.01)
            .OrderBy(r=>r.Position.X).Take(4).Select(r=>r.Id).ToList();
        Assert.True(route.Count>=3);
        var vehicle=sim.State.Vehicles.First();
        vehicle.Route=route;
        vehicle.RouteIndex=1;
        var node=sim.State.Roads.First(r=>r.Id==vehicle.Route[1]);
        Invoke(sim,"Pr57MovementTargetBegin");
        try
        {
            MatchesFresh(sim,vehicle); // initial miss
            MatchesFresh(sim,vehicle); // hit
            vehicle.Route[2]=route[0]; // same Count, different next node
            MatchesFresh(sim,vehicle);
            node.Position=new Point(node.Position.X+.25,node.Position.Z);
            MatchesFresh(sim,vehicle);
            node.MaintenanceRatioUnits+=1000;
            MatchesFresh(sim,vehicle);
            node.Highway=!node.Highway;
            MatchesFresh(sim,vehicle);
            node.Crosswalk=!node.Crosswalk;
            MatchesFresh(sim,vehicle);
            sim.State.Tuning.VehicleSpeed+=.25;
            MatchesFresh(sim,vehicle);
            var hits=typeof(CitySimulation).GetField("_pr57MovementTargetHits",Private);
            var invalidations=typeof(CitySimulation).GetField("_pr57MovementTargetInvalidations",Private);
            Assert.NotNull(hits);Assert.NotNull(invalidations);
            Assert.True((long)hits.GetValue(sim)!>=1);
            Assert.True((long)invalidations.GetValue(sim)!>=6);
        }
        finally { Invoke(sim,"Pr57MovementTargetEnd"); }
        MatchesFresh(sim,vehicle);
    }
}
