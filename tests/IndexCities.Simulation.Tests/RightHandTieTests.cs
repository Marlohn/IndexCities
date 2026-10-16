using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC transporte-circulacao: when everyone waiting at an unsignalled junction
// yields to the right, the vehicle waiting longest crosses first.
public sealed class RightHandTieTests
{
    static (CitySimulation Sim,Vehicle[] Cars) FourWay(double[] waitingSince)
    {
        var initial=new CitySimulation(42);var id=1000;
        for(var x=-12;x<=12;x+=4)initial.State.Roads.Add(new(){Id=id++,Position=new(x,40)});
        for(var z=28;z<=52;z+=4)if(z!=40)initial.State.Roads.Add(new(){Id=id++,Position=new(0,z)});
        var sim=new CitySimulation(initial.State);
        int Node(double x,double z)=>sim.State.Roads.Single(r=>r.Position==new Point(x,z)).Id;
        var arms=new (Point Position,double Heading,List<int> Route)[]{
            (new(-6,38.35),Math.PI/2,[Node(-12,40),Node(-8,40),Node(-4,40),Node(0,40),Node(4,40),Node(8,40),Node(12,40)]),
            (new(6,41.65),-Math.PI/2,[Node(12,40),Node(8,40),Node(4,40),Node(0,40),Node(-4,40),Node(-8,40),Node(-12,40)]),
            (new(1.65,34),0,[Node(0,28),Node(0,32),Node(0,36),Node(0,40),Node(0,44),Node(0,48),Node(0,52)]),
            (new(-1.65,46),Math.PI,[Node(0,52),Node(0,48),Node(0,44),Node(0,40),Node(0,36),Node(0,32),Node(0,28)])};
        var cars=sim.State.Vehicles.Take(4).ToArray();
        for(var i=0;i<4;i++){var v=cars[i];v.Position=arms[i].Position;v.Heading=arms[i].Heading;v.Route=arms[i].Route;v.RouteIndex=3;v.Status=VehicleStatus.Travelling;v.Fuel=40;v.WaitingSince=waitingSince[i];}
        sim.State.EconomyNextAt=86400;return (sim,cars);
    }
    [Fact] public void Four_cars_yielding_to_the_right_let_the_longest_waiting_cross_first()
    {
        foreach(var first in new[]{0,1,2,3}){
            var waits=new double[]{20,20,20,20};waits[first]=5;
            var (sim,cars)=FourWay(waits);var origins=cars.Select(c=>c.Position).ToArray();
            sim.Advance(.1);
            for(var i=0;i<4;i++)if(i==first)Assert.True(cars[i].Position.Distance(origins[i])>0,cars[i].Cause);else Assert.Equal(origins[i],cars[i].Position);
            for(var step=0;step<300;step++)sim.Advance(.1);
            Assert.All(cars,c=>Assert.True(c.RouteIndex>3,c.Cause));sim.CheckInvariants();
        }
    }
    [Fact] public void A_free_right_hand_order_is_unchanged_when_not_everyone_is_blocked()
    {
        // Only two perpendicular cars (no cycle): the one with nobody on its right
        // goes, even if the other has waited longer.
        var (sim,cars)=FourWay([20,20,5,20]);
        cars[1].Status=VehicleStatus.Parked;cars[1].Position=new(0,-200);cars[3].Status=VehicleStatus.Parked;cars[3].Position=new(0,-210);
        var west=cars[0].Position;var south=cars[2].Position;sim.Advance(.1);
        Assert.True(cars[0].Position.Distance(west)>0,cars[0].Cause);Assert.Equal(south,cars[2].Position);
    }
}
