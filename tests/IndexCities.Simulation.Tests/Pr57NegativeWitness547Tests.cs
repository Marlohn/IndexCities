using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #547: deliberately narrow adversarials for the negative certificate.
// The integrated shadow run separately checks every actual cache hit against
// the unmodified CanTurnNow path on authentic saved cities.
public sealed class Pr57NegativeWitness547Tests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(CitySimulation sim,string method,params object[] args)
    {
        var m=typeof(CitySimulation).GetMethod(method,Private);
        Assert.NotNull(m);
        m.Invoke(sim,args);
    }
    static bool Try(CitySimulation sim,Vehicle v)
    {
        var m=typeof(CitySimulation).GetMethod("Pr57NegativeWitnessTry",Private);
        Assert.NotNull(m);
        return (bool)m.Invoke(sim,[v])!;
    }

    [Fact]
    public void Live_blocker_survives_unrelated_writes_but_not_witness_or_road_changes()
    {
        if(Environment.GetEnvironmentVariable("PR57_NEGATIVE_WITNESS")!="1" ||
           Environment.GetEnvironmentVariable("PR57_NEGATIVE_WITNESS_VERIFY")=="1")return;
        var sim=new CitySimulation(42,new(){ExternalTruckCount=3});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-80,0)).Success);
        Assert.True(sim.State.Vehicles.Count>=3);
        var v=sim.State.Vehicles[0];
        var blocker=sim.State.Vehicles[1];
        var unrelated=sim.State.Vehicles[2];
        v.Status=VehicleStatus.Travelling;
        blocker.Status=VehicleStatus.Travelling;
        v.Position=new Point(100,100);
        blocker.Position=new Point(100,109);
        v.Heading=blocker.Heading=0;
        v.Route=[sim.State.Roads[0].Id];
        v.RouteIndex=0;
        var poses=new List<(Vehicle Body,Point Position,double Heading)>
            { (v,new Point(100,109),0) };
        void Store()=>Call(sim,"Pr57NegativeWitnessStore",v,poses,blocker);
        Call(sim,"Pr57TurnWayWorksetBegin");
        try
        {
            Store();
            Assert.True(Try(sim,v)); // existing live blocker is sufficient
            unrelated.Position=new Point(400,400);
            Assert.True(Try(sim,v)); // other actors cannot turn a negative into true
            blocker.Position=new Point(100,130);
            Assert.False(Try(sim,v)); // witness no longer obstructs
            blocker.Position=new Point(100,109);
            Store();
            v.Route[0]=int.MaxValue; // in-place route edit
            Assert.False(Try(sim,v));
            v.Route[0]=sim.State.Roads[0].Id;
            var road=sim.State.Roads[0];
            var originalRoadPosition=road.Position;
            Store();
            road.Position=new Point(originalRoadPosition.X+3,originalRoadPosition.Z); // no epoch change
            Assert.False(Try(sim,v));
            road.Position=originalRoadPosition;
            Call(sim,"Pr57TurnWayCaptureRoads");
            Store();
            // Removing the exact blocker at its original index invalidates.
            sim.State.Vehicles.Remove(blocker);
            Assert.False(Try(sim,v));
        }
        finally { Call(sim,"Pr57TurnWayWorksetEnd"); }
    }
}
