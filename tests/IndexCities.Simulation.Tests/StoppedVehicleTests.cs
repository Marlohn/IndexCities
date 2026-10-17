using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC transporte-circulacao, "Veículo parado na via não trava a rua".
public sealed class StoppedVehicleTests
{
    // Straight two-way street along z=40; eastbound lane at z=38.35.
    static (CitySimulation Sim,Vehicle Stopped,Vehicle Follower,Vehicle Third) Street(bool junctionAtStop=false)
    {
        var initial=new CitySimulation(42);
        for(var x=-60;x<=60;x+=4)initial.State.Roads.Add(new(){Id=1000+x,Position=new(x,40)});
        // A side street makes (4,40) a real junction: no overtaking there.
        if(junctionAtStop)initial.State.Roads.AddRange([new(){Id=5000,Position=new(4,36)},new(){Id=5001,Position=new(4,32)}]);
        var sim=new CitySimulation(initial.State);var v=sim.State.Vehicles.OrderBy(v=>v.Id).Take(3).ToArray();
        var east=Enumerable.Range(0,31).Select(n=>940+n*4).ToList();
        v[0].Position=new(0,38.35);v[0].Heading=Math.PI/2;v[0].Status=VehicleStatus.Stranded;v[0].Fuel=0;v[0].Route=east;v[0].RouteIndex=16;v[0].Cause="Sem combustível; nenhuma viagem fictícia";
        v[1].Position=new(-20,38.35);v[1].Heading=Math.PI/2;v[1].Status=VehicleStatus.Travelling;v[1].Fuel=40;v[1].Route=east;v[1].RouteIndex=11;
        sim.State.EconomyNextAt=1e9;return (sim,v[0],v[1],v[2]);
    }
    [Fact] public void A_vehicle_behind_a_stopped_one_overtakes_through_the_free_opposite_lane_and_returns()
    {
        var (sim,stopped,follower,_)=Street();var stoppedAt=stopped.Position;var sawOpposite=false;
        for(var i=0;i<400;i++){
            sim.Advance(.1);
            if(follower.Position.Z>40)sawOpposite=true;
            if(i==120){var restored=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(2);restored.Advance(2);Assert.Equal(sim.SaveToJson(),restored.SaveToJson());}
        }
        Assert.True(sawOpposite,follower.Cause);Assert.True(follower.Position.X>20,follower.Cause);
        Assert.True(Math.Abs(follower.Position.Z-38.35)<.2,follower.Position.ToString());
        Assert.Empty(follower.Detour);Assert.True(follower.Fuel<40);Assert.Equal(stoppedAt,stopped.Position);sim.CheckInvariants();
    }
    [Fact] public void Overtaking_waits_for_oncoming_traffic_and_the_oncoming_car_is_never_met_head_on()
    {
        var (sim,stopped,follower,oncoming)=Street();
        oncoming.Position=new(50,41.65);oncoming.Heading=-Math.PI/2;oncoming.Status=VehicleStatus.Travelling;oncoming.Fuel=40;
        oncoming.Route=Enumerable.Range(0,31).Select(n=>1060-n*4).ToList();oncoming.RouteIndex=3;
        var startedBeforeItPassed=false;
        for(var i=0;i<500;i++){
            sim.Advance(.1);
            if(follower.Detour.Count>0&&oncoming.Position.X>follower.Position.X)startedBeforeItPassed=true;
            if(Math.Abs(follower.Position.Z-oncoming.Position.Z)<1.5)Assert.True(follower.Position.Distance(oncoming.Position)>8,$"{follower.Position} {oncoming.Position}");
        }
        Assert.False(startedBeforeItPassed);Assert.True(oncoming.Position.X< -20,oncoming.Cause);Assert.True(follower.Position.X>20,follower.Cause);sim.CheckInvariants();
    }
    [Fact] public void Without_a_legal_overtaking_stretch_the_vehicle_turns_back_and_takes_another_route()
    {
        // Ring: south street z=40 and north street z=80 joined at x=-60 and x=60.
        var (sim,stopped,follower,_)=Street(junctionAtStop:true);
        var initial=sim.State;
        for(var x=-60;x<=60;x+=4)initial.Roads.Add(new(){Id=2000+x,Position=new(x,80)});
        for(var z=44;z<=76;z+=4){initial.Roads.Add(new(){Id=3000+z,Position=new(-60,z)});initial.Roads.Add(new(){Id=4000+z,Position=new(60,z)});}
        sim=new CitySimulation(initial);follower=sim.State.Vehicles.Single(v=>v.Id==follower.Id);stopped=sim.State.Vehicles.Single(v=>v.Id==stopped.Id);
        follower.Route=follower.Route.Take(28).ToList();var destination=follower.Route[^1];
        sim.Advance(60);Assert.True(follower.Status==VehicleStatus.Travelling,follower.Status+" "+follower.Cause+" "+follower.Position);Assert.True(follower.Position.X<0);Assert.Empty(follower.Detour);
        for(var i=0;i<400;i++)sim.Advance(2);
        Assert.True(follower.Status==VehicleStatus.Parked||follower.RouteIndex>=follower.Route.Count||follower.Position.Distance(sim.State.Roads.Single(r=>r.Id==destination).Position)<6,follower.Cause+" "+follower.Position);
        Assert.Contains(follower.Route,id=>id is >=1940 and <=2060);Assert.Equal(new Point(0,38.35),stopped.Position);sim.CheckInvariants();
    }
    [Fact] public void A_truck_does_not_leave_for_a_work_whose_round_trip_exceeds_its_tank()
    {
        var sim=new CitySimulation(42,new SimulationTuning{FuelTank=3});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(600);
        Assert.Equal(WorkCause.RouteBeyondTruckRange,sim.State.Projects.Single(p=>p.RoadPoints.Count>0).Cause);Assert.Empty(sim.State.Shipments);
        Assert.All(sim.State.Vehicles,v=>Assert.Equal(VehicleStatus.Outside,v.Status));sim.CheckInvariants();
    }
}
