using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class VehicleMergeTests
{
    // Controlled traffic fixture: the pair has already converged within the
    // numerical coincidence tolerance. Neither positions nor fuel may jump.
    static (CitySimulation Sim,Vehicle First,Vehicle Second) Pair(double gap,bool corner=false)
    {
        var initial=new CitySimulation(42);
        for(var x=-40;x<=(corner?0:40);x+=4)initial.State.Roads.Add(new(){Id=1000+x,Position=new(x,40)});
        if(corner)for(var z=44;z<=80;z+=4)initial.State.Roads.Add(new(){Id=2000+z-40,Position=new(0,z)});
        var sim=new CitySimulation(initial.State);
        var pair=sim.State.Vehicles.OrderBy(v=>v.Id).Take(2).ToArray();
        for(var i=0;i<2;i++) {
            var vehicle=pair[i];vehicle.Position=new(i*gap,38.35);vehicle.Heading=Math.PI/2;
            vehicle.Status=VehicleStatus.Travelling;vehicle.Route=Enumerable.Range(0,11).Select(n=>1000+n*4).ToList();vehicle.RouteIndex=1;
            vehicle.Fuel=40;vehicle.WaitingSince=0;vehicle.IntersectionClaim=null;
        }
        sim.State.EconomyNextAt=86400;return(sim,pair[0],pair[1]);
    }

    [Fact] public void Nearly_coincident_vehicles_do_not_block_each_other_forever()
    {
        foreach(var gap in new[]{0,.05,.099}) {
            var (sim,first,second)=Pair(gap);var origin=first.Position;var secondOrigin=second.Position;
            sim.Advance(.1);
            Assert.True(first.Position.Distance(origin)>.1,first.Cause);
            Assert.True(first.Fuel<40);Assert.Equal(secondOrigin,second.Position);Assert.Equal(40,second.Fuel);
            var restored=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(2);restored.Advance(2);
            Assert.True(second.Position.Distance(secondOrigin)>.1,second.Cause);
            Assert.Equal(sim.SaveToJson(),restored.SaveToJson());sim.CheckInvariants();
        }
    }

    [Fact] public void Ordinary_queue_keeps_clearance_behind_a_stranded_vehicle()
    {
        var (sim,first,second)=Pair(9);second.Status=VehicleStatus.Stranded;second.Fuel=0;
        var obstacle=second.Position;sim.Advance(2);
        Assert.Equal(obstacle,second.Position);Assert.True(first.Position.Distance(obstacle)>=8-.000001);
        Assert.True(first.Position.X<=1+.000001);sim.CheckInvariants();
    }

    [Fact] public void A_car_leaving_a_lot_yields_to_a_vehicle_already_on_the_road_at_another_heading()
    {
        var (sim,entering,obstacle)=Pair(0);
        entering.PassengerCar=true;entering.RouteIndex=0;entering.Position=new(0,41.5);
        obstacle.Position=new(0,38.35);obstacle.Status=VehicleStatus.Stranded;obstacle.Fuel=0;
        var position=entering.Position;sim.Advance(.1);
        Assert.Equal(position,entering.Position);Assert.Equal(40,entering.Fuel);
        Assert.Equal($"Aguardando veículo {obstacle.Id} sem combustível na via",entering.Cause);sim.CheckInvariants();
    }

    [Fact] public void Coincidence_recovery_does_not_drive_through_a_stranded_vehicle()
    {
        foreach(var gap in new[]{0,.05,.099}) {
            var (sim,first,second)=Pair(gap);second.Status=VehicleStatus.Stranded;second.Fuel=0;
            var position=first.Position;sim.Advance(.1);
            Assert.Equal(position,first.Position);Assert.Equal(40,first.Fuel);Assert.Equal(0,second.Fuel);sim.CheckInvariants();
        }
    }

    static void Westbound(Vehicle vehicle,Point position,int index)
    {
        vehicle.PassengerCar=true;vehicle.Position=position;vehicle.Heading=-Math.PI/2;
        vehicle.Route=Enumerable.Range(0,21).Select(n=>1040-n*4).ToList();vehicle.RouteIndex=index;
    }

    [Fact] public void Lot_entry_waits_for_opposite_traffic_then_resumes_after_it_passes()
    {
        foreach(var rotated in new[]{false,true}){
            var (sim,entering,oncoming)=Pair(0);
            entering.PassengerCar=true;entering.RouteIndex=0;entering.Position=new(0,48.4);
            Westbound(oncoming,new(4.2,41.65),9);
            if(rotated){
                foreach(var node in sim.State.Roads.Where(r=>r.Id is >=960 and <=1040))node.Position=new(-node.Position.X,80-node.Position.Z);
                foreach(var vehicle in new[]{entering,oncoming}){vehicle.Position=new(-vehicle.Position.X,80-vehicle.Position.Z);vehicle.Heading+=Math.PI;}
            }
            var origin=entering.Position;sim.Advance(.1);
            Assert.Equal(origin,entering.Position);Assert.Equal(40,entering.Fuel);
            Assert.Equal("Aguardando espaço para acessar a faixa",entering.Cause);
            sim.Advance(2);Assert.Equal(origin,entering.Position);
            sim.Advance(1);Assert.True(entering.Position.Distance(origin)>.1,entering.Cause);
            Assert.True(entering.Fuel<40);sim.CheckInvariants();
        }
    }

    [Fact] public void Replanned_car_already_on_its_lane_does_not_wait_for_the_opposite_lane()
    {
        var (sim,east,west)=Pair(0);
        east.PassengerCar=true;east.RouteIndex=0;
        Westbound(west,new(0,41.65),10);
        sim.Advance(.1);
        Assert.True(east.Position.X>0,east.Cause);Assert.True(west.Position.X<0,west.Cause);
        Assert.True(east.Fuel<40);Assert.True(west.Fuel<40);sim.CheckInvariants();
    }

    [Fact] public void Two_lot_entries_from_opposite_sides_make_progress_without_mutual_wait()
    {
        var (sim,east,west)=Pair(0);
        east.PassengerCar=true;east.RouteIndex=0;east.Position=new(0,45);
        Westbound(west,new(0,35),10);
        west.Route=west.Route.Skip(10).ToList();west.RouteIndex=0;
        for(var step=0;step<100;step++)sim.Advance(.1);
        Assert.True(east.Position.X>10,east.Cause);Assert.True(west.Position.X< -10,west.Cause);
        Assert.True(east.Fuel<40);Assert.True(west.Fuel<40);sim.CheckInvariants();
    }

    [Fact] public void Nearby_farm_parking_exits_keep_normal_clearance_after_joining_the_lane()
    {
        var (sim,first,second)=Pair(0);
        foreach(var node in sim.State.Roads.Where(r=>r.Id is >=960 and <=1040))node.Position=new(node.Position.X,48);
        Westbound(first,new(28,39.6),3);Westbound(second,new(23.4,39.6),4);
        first.Route=first.Route.Skip(3).ToList();second.Route=second.Route.Skip(4).ToList();first.RouteIndex=second.RouteIndex=0;
        var checkedLane=false;
        for(var step=0;step<20;step++){
            sim.Advance(.5);
            if(first.RouteIndex>0&&second.RouteIndex>0){checkedLane=true;Assert.True(first.Position.Distance(second.Position)>=8-.000001);}
        }
        Assert.True(checkedLane);Assert.True(first.Position.X<0,first.Cause);Assert.True(second.Position.X<0,second.Cause);sim.CheckInvariants();
    }

    [Fact] public void A_car_replanned_before_a_corner_recognizes_its_existing_incoming_lane()
    {
        var (sim,replanned,oncoming)=Pair(0,corner:true);
        replanned.PassengerCar=true;replanned.Position=new(-1.5,38.35);replanned.Heading=Math.PI/2;
        replanned.Route=new[]{1000}.Concat(Enumerable.Range(1,10).Select(n=>2000+n*4)).ToList();replanned.RouteIndex=0;
        oncoming.PassengerCar=true;oncoming.Position=new(-1,41.65);oncoming.Heading=-Math.PI/2;
        oncoming.Route=Enumerable.Range(0,11).Select(n=>1000-n*4).ToList();oncoming.RouteIndex=1;
        var origin=replanned.Position;sim.Advance(.1);
        Assert.True(replanned.Position.Distance(origin)>.1,replanned.Cause);
        Assert.Equal("Em viagem",replanned.Cause);Assert.True(replanned.Fuel<40);sim.CheckInvariants();
    }
}
