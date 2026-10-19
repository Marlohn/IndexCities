using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class LaneTurnBodyTests
{
    [Theory]
    [InlineData(.2,false)]
    [InlineData(.5,false)]
    [InlineData(2,false)]
    [InlineData(.2,true)]
    [InlineData(.5,true)]
    [InlineData(2,true)]
    public void An_opposing_truck_respects_a_turning_body_but_parallel_lanes_pass_freely(double dt,bool straight)
    {
        var initial=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0,ExternalTruckCount=0});
        var id=1000;
        for(var x=-188;x<=-172;x+=4)initial.State.Roads.Add(new(){Id=id++,Position=new(x,0)});
        for(var z=4;z<=16;z+=4)initial.State.Roads.Add(new(){Id=id++,Position=new(-172,z)});
        for(var x=-168;x<=-148;x+=4)initial.State.Roads.Add(new(){Id=id++,Position=new(x,16)});
        var sim=new CitySimulation(initial.State);sim.State.EconomyNextAt=86400;
        RoadNode At(Point p)=>sim.State.Roads.Single(r=>r.Position==p);
        var returning=new Vehicle{Id=800,External=true,ExternalDriverId="return",Status=VehicleStatus.Travelling,Fuel=40,Position=new(-160.5,17.65),Heading=-Math.PI/2,
            Route=sim.FindRoadRoute(At(new(-160,16)).Id,At(sim.State.Entry).Id),RouteIndex=1};
        var turning=new Vehicle{Id=801,External=true,ExternalDriverId="delivery",Status=VehicleStatus.Travelling,Fuel=40,Position=straight?new(-166,14.35):new(-170.35,12),Heading=straight?Math.PI/2:0,
            Route=sim.FindRoadRoute(At(new(-172,12)).Id,At(new(-148,16)).Id),RouteIndex=straight?3:1};
        sim.State.Vehicles.AddRange([returning,turning]);var fuel=80.0;var money=sim.MonetaryTotal;var burned=0.0;
        var first=returning.Position;var second=turning.Position;
        for(var t=0.0;t<90;t+=dt){
            sim.Advance(dt);burned+=sim.State.Vehicles.Sum(v=>v.FuelBurnedLastStep);
            Assert.True(MachineExitTests.Penetration(returning,turning)<=0,$"t={t+dt}: returning={returning.Position} h={returning.Heading} {returning.Cause}; turning={turning.Position} h={turning.Heading} {turning.Cause}");
            if(straight&&t+dt>=2&&t<2){Assert.True(returning.Position.Distance(first)>10,returning.Cause);Assert.True(turning.Position.Distance(second)>10,turning.Cause);}
        }
        Assert.Equal(VehicleStatus.Outside,returning.Status);Assert.Equal(VehicleStatus.Parked,turning.Status);
        Assert.Equal(fuel,sim.State.Vehicles.Sum(v=>v.Fuel)+burned,6);Assert.Equal(money,sim.MonetaryTotal);sim.CheckInvariants();
    }
}
