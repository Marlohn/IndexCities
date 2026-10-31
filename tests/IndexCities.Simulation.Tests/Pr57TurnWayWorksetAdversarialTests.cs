using System.Collections;
using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #541: adversarial mutations within the private geometry workset's
// lifetime. This is NOT a substitute for public Advance(2) ABBA parity.
public sealed class Pr57TurnWayWorksetAdversarialTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static void Invoke(CitySimulation sim,string name)
    {
        var method=typeof(CitySimulation).GetMethod(name,Private);
        Assert.NotNull(method);
        method.Invoke(sim,null);
    }

    static (object[] Poses,int[] Spaces) Way(CitySimulation sim,string name,Vehicle vehicle)
    {
        var method=typeof(CitySimulation).GetMethod(name,Private);
        Assert.NotNull(method);
        object?[] args=[vehicle,null];
        var poses=((IEnumerable)method.Invoke(sim,args)!).Cast<object>().ToArray();
        var spaces=((IEnumerable<RoadNode>)args[1]!).Select(x=>x.Id).ToArray();
        return (poses,spaces);
    }

    static void MatchesFresh(CitySimulation sim,Vehicle vehicle)
    {
        var cached=Way(sim,"TurnWay",vehicle);
        var fresh=Way(sim,"Pr57TurnWayFresh",vehicle);
        Assert.Equal(fresh.Poses,cached.Poses);
        Assert.Equal(fresh.Spaces,cached.Spaces);
    }

    [Fact]
    public void Pose_heading_route_index_inplace_route_and_road_epoch_invalidate_exactly()
    {
        // First test invocation (without flag) is the unmodified control.
        // Second test invocation in CI enables the candidate and shadow oracle.
        if(Environment.GetEnvironmentVariable("PR57_STEP_TURNWAY_WORKSET")!="1")return;
        var sim=new CitySimulation(42,new(){ExternalTruckCount=1});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-80,0)).Success);
        var route=sim.State.Roads.Where(r=>Math.Abs(r.Position.Z)<.01)
            .OrderBy(r=>r.Position.X).Take(4).Select(r=>r.Id).ToList();
        Assert.True(route.Count>=3);
        var vehicle=sim.State.Vehicles.First();
        vehicle.Status=VehicleStatus.Travelling;
        vehicle.Route=route;
        vehicle.RouteIndex=0;
        vehicle.Position=sim.State.Roads.First(r=>r.Id==route[0]).Position;
        vehicle.Heading=0;

        Invoke(sim,"Pr57TurnWayWorksetBegin");
        try
        {
            MatchesFresh(sim,vehicle); // miss, then fresh baseline
            MatchesFresh(sim,vehicle); // hit; shadow verifies every pose
            vehicle.Position=new Point(vehicle.Position.X+1.25,vehicle.Position.Z-.5);
            MatchesFresh(sim,vehicle);
            vehicle.Heading+=.37;
            MatchesFresh(sim,vehicle);
            vehicle.RouteIndex=1;
            MatchesFresh(sim,vehicle);
            vehicle.Route[1]=route[2]; // same list identity and Count
            MatchesFresh(sim,vehicle);
            var epoch=typeof(CitySimulation).GetField("_roadEpoch",Private);
            Assert.NotNull(epoch);
            epoch.SetValue(sim,(int)epoch.GetValue(sim)!+1);
            MatchesFresh(sim,vehicle);
            MatchesFresh(sim,vehicle); // second hit after all invalidations
            var hits=typeof(CitySimulation).GetField("_pr57TurnWayHits",Private);
            var invalidations=typeof(CitySimulation).GetField("_pr57TurnWayInvalidations",Private);
            Assert.NotNull(hits);
            Assert.NotNull(invalidations);
            Assert.True((long)hits.GetValue(sim)!>=2);
            Assert.True((long)invalidations.GetValue(sim)!>=5);
        }
        finally { Invoke(sim,"Pr57TurnWayWorksetEnd"); }
        MatchesFresh(sim,vehicle); // no cached state after lease closes
    }
}
