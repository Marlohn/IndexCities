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
            // The opposite vehicle reaches the 8 m clearance at ~1.743 s.
            // Keep waiting before that physical instant, then use its slot
            // inside the next 2 s step instead of asserting the old sampling delay.
            sim.Advance(1.4);Assert.Equal(origin,entering.Position);Assert.Equal(40,entering.Fuel);
            sim.Advance(2);Assert.True(entering.Position.Distance(origin)>.1,entering.Cause);
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

    static (CitySimulation Sim,Vehicle Exiting,Vehicle Oncoming) Crossing(double distance,double maintenance=1)
    {
        var (sim,exiting,oncoming)=Pair(0);
        foreach(var road in sim.State.Roads.Where(r=>r.Id is >=960 and <=1040))road.MaintenanceRatio=maintenance;
        Westbound(exiting,new(0,33.25),10);
        exiting.Route=exiting.Route.Skip(10).ToList();exiting.RouteIndex=0;
        oncoming.PassengerCar=true;oncoming.Position=new(-distance,38.35);oncoming.Heading=Math.PI/2;
        oncoming.Route=Enumerable.Range(0,21).Select(n=>960+n*4).ToList();oncoming.RouteIndex=(int)Math.Ceiling((40-distance)/4);
        return(sim,exiting,oncoming);
    }

    [Theory]
    [InlineData(2,20)][InlineData(.5,20)]
    [InlineData(2,24)][InlineData(.5,24)]
    [InlineData(2,28)][InlineData(.5,28)]
    public void A_lot_exit_uses_the_initial_gap_then_continues_through_repeated_opposite_traffic(double dt,double spacing)
    {
        var (sim,exiting,_)=Crossing(20);var origin=exiting.Position;
        var traffic=sim.State.Vehicles.Where(v=>v.Id!=exiting.Id&&v.PassengerCar).ToList();
        var period=spacing/sim.State.Tuning.VehicleSpeed;var next=0.0;var elapsed=0.0;
        for(var step=0;step<12/dt;step++){
            if(elapsed>=next){
                var car=new Vehicle{Id=sim.State.Vehicles.Max(v=>v.Id)+1,External=true,ExternalDriverId="controlled-test",PassengerCar=true,
                    Position=new(-40,38.35),Heading=Math.PI/2,Status=VehicleStatus.Travelling,Fuel=40,
                    Route=Enumerable.Range(0,21).Select(n=>960+n*4).ToList(),RouteIndex=0};
                sim.State.Vehicles.Add(car);traffic.Add(car);next+=period;
            }
            var previous=exiting.Position;sim.Advance(dt);elapsed+=dt;
            Assert.True(exiting.Position.Distance(previous)<=dt*sim.State.Tuning.VehicleSpeed+.000001);
            if(step==0){Assert.True(exiting.Position.Distance(origin)>.1,exiting.Cause);Assert.True(exiting.Fuel<40);}
            // This samples the opposite lane while the exit crosses it; the
            // normal 3.3 m spacing BETWEEN lanes is not an 8 m queue violation.
            if(Math.Abs(exiting.Position.Z-38.35)<1)
                Assert.All(traffic.Where(v=>v.Status==VehicleStatus.Travelling),v=>Assert.True(Math.Abs(exiting.Position.X-v.Position.X)>=6));
        }
        Assert.True(exiting.Position.X< -20,exiting.Cause);sim.CheckInvariants();
        var restored=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(2);restored.Advance(2);
        Assert.Equal(sim.SaveToJson(),restored.SaveToJson());
    }

    [Theory]
    [InlineData(.5,12,1)][InlineData(2,12,1)]
    [InlineData(.5,16,1)][InlineData(2,16,1)]
    [InlineData(.5,20,.5)][InlineData(2,20,.5)]
    public void A_lot_exit_waits_for_close_traffic_even_with_a_short_step_or_slow_road(double dt,double distance,double maintenance)
    {
        var (sim,exiting,oncoming)=Crossing(distance,maintenance);var origin=exiting.Position;
        sim.Advance(dt);
        Assert.Equal(origin,exiting.Position);Assert.Equal(40,exiting.Fuel);
        Assert.Equal("Aguardando espaço para acessar a faixa",exiting.Cause);
        for(var elapsed=dt;elapsed<12;elapsed+=dt){
            sim.Advance(dt);
            if(Math.Abs(exiting.Position.Z-38.35)<1&&oncoming.Status==VehicleStatus.Travelling)
                Assert.True(Math.Abs(exiting.Position.X-oncoming.Position.X)>=6);
        }
        Assert.True(exiting.Position.Distance(origin)>10,exiting.Cause);Assert.True(exiting.Fuel<40);sim.CheckInvariants();
    }

    [Theory]
    [InlineData(.1,18,1)][InlineData(.5,18,1)][InlineData(2,18,1)]
    [InlineData(.1,20,1)][InlineData(.5,20,1)][InlineData(2,20,1)]
    [InlineData(.1,28,.5)][InlineData(.5,28,.5)][InlineData(2,28,.5)]
    [InlineData(.1,30,.5)][InlineData(.5,30,.5)][InlineData(2,30,.5)]
    [InlineData(.1,32,.5)][InlineData(.5,32,.5)][InlineData(2,32,.5)]
    public void A_street_exit_finishes_crossing_without_stopping_in_front_of_faster_highway_traffic(double dt,double distance,double maintenance)
    {
        foreach(var rotated in new[]{false,true}){
            var (sim,exiting,oncoming)=Crossing(distance);
            foreach(var road in sim.State.Roads.Where(r=>r.Id is >=960 and <=1040)){
                road.Highway=road.Position.X<0;road.MaintenanceRatio=road.Id==1000?maintenance:1;
            }
            if(rotated){
                foreach(var node in sim.State.Roads.Where(r=>r.Id is >=960 and <=1040))node.Position=new(-node.Position.X,80-node.Position.Z);
                foreach(var vehicle in new[]{exiting,oncoming}){vehicle.Position=new(-vehicle.Position.X,80-vehicle.Position.Z);vehicle.Heading+=Math.PI;}
            }
            var origin=exiting.Position;sim.Advance(dt);
            Assert.Equal(origin,exiting.Position);Assert.Equal(40,exiting.Fuel);
            var joined=false;
            for(var elapsed=dt;elapsed<20;elapsed+=dt){
                sim.Advance(dt);
                if(exiting.RouteIndex>0)joined=true;
                var z=rotated?80-exiting.Position.Z:exiting.Position.Z;
                if(exiting.RouteIndex==0&&z>34.75&&z<41.15){
                    Assert.NotEqual("Aguardando espaço para acessar a faixa",exiting.Cause);
                    if(oncoming.Status==VehicleStatus.Travelling)Assert.True(exiting.Position.Distance(oncoming.Position)>=6);
                }
            }
            Assert.True(joined,exiting.Cause);Assert.True(exiting.Fuel<40);sim.CheckInvariants();
        }
    }

    [Theory]
    [InlineData(-16,18)][InlineData(-16,20)]
    [InlineData(-24,18)][InlineData(-24,20)]
    public void Entry_prediction_includes_a_faster_segment_between_the_other_car_and_the_access(double highwayStart,double distance)
    {
        var (sim,exiting,oncoming)=Crossing(distance);
        foreach(var road in sim.State.Roads.Where(r=>r.Id is >=960 and <=1040))road.Highway=road.Position.X>=highwayStart&&road.Position.X<0;
        var origin=exiting.Position;sim.Advance(.1);
        Assert.Equal(origin,exiting.Position);Assert.Equal(40,exiting.Fuel);
        var joined=false;
        for(var step=0;step<200;step++){
            sim.Advance(.1);
            if(exiting.RouteIndex>0)joined=true;
            if(exiting.RouteIndex==0&&exiting.Position.Z>34.75&&exiting.Position.Z<41.15){
                Assert.NotEqual("Aguardando espaço para acessar a faixa",exiting.Cause);
                if(oncoming.Status==VehicleStatus.Travelling)Assert.True(exiting.Position.Distance(oncoming.Position)>=6);
            }
        }
        Assert.True(joined,exiting.Cause);sim.CheckInvariants();
    }
}
