using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class AdjacentJunctionQueueTests
{
    static CitySimulation Street()
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
        for(var x=-24;x<=24;x+=4)sim.State.Roads.Add(new(){Id=1000+x,Position=new(x,40)});
        sim.State.Roads.AddRange(new[]{new RoadNode{Id=2000,Position=new(0,44)},new RoadNode{Id=2012,Position=new(12,44)}});
        sim=new CitySimulation(sim.State);sim.State.Vehicles.Clear();sim.State.EconomyNextAt=86400;return sim;
    }
    static Vehicle Leader()=>new(){Id=1,External=true,PassengerCar=true,Position=new(.01,41.65),Heading=-Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=[1004,1000,996,992,988,984,980],RouteIndex=1,IntersectionClaim=1000,IntersectionEnteredAt=2};
    static Vehicle Follower()=>new(){Id=2,External=true,PassengerCar=true,Position=new(8.01,41.65),Heading=-Math.PI/2,Fuel=40,Status=VehicleStatus.Travelling,Route=[1012,1008,1004,1000,996,992,988,984],RouteIndex=1,IntersectionClaim=1012,IntersectionEnteredAt=1};

    [Theory][InlineData(.1)][InlineData(.5)][InlineData(2)]
    public void A_follower_holding_the_previous_crossing_cannot_stop_its_admitted_leader(double dt)
    {
        var sim=Street();var leader=Leader();var follower=Follower();sim.State.Vehicles.AddRange([leader,follower]);
        var first=leader.Position;var fuel=leader.Fuel;
        sim.Advance(dt);
        Assert.True(leader.Position.X<first.X-.1,leader.Cause);Assert.True(leader.Fuel<fuel);
        Assert.True(follower.Position.X-leader.Position.X>=8-.000001);
        var loaded=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(2);loaded.Advance(2);Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());sim.CheckInvariants();
    }

    [Theory][InlineData(false)][InlineData(true)]
    public void A_different_lane_holder_still_blocks_an_admitted_car(bool includeFollower)
    {
        var sim=Street();var leader=Leader();sim.State.Vehicles.Add(leader);
        if(includeFollower)sim.State.Vehicles.Add(Follower());
        sim.State.Vehicles.Add(new(){Id=3,External=true,PassengerCar=true,Position=new(6.5,38.35),Heading=Math.PI/2,Fuel=0,Status=VehicleStatus.Stranded,IntersectionClaim=1012,IntersectionEnteredAt=1.5});
        var first=leader.Position;var fuel=leader.Fuel;sim.Advance(.1);
        Assert.Equal(first,leader.Position);Assert.Equal(fuel,leader.Fuel);Assert.Contains("cruzamento",leader.Cause);sim.CheckInvariants();
    }
}
