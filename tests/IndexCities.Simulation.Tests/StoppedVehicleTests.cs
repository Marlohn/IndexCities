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
    static CitySimulation Ring(CitySimulation sim)
    {
        var initial=sim.State;
        for(var x=-60;x<=60;x+=4)initial.Roads.Add(new(){Id=2000+x,Position=new(x,80)});
        for(var z=44;z<=76;z+=4){initial.Roads.Add(new(){Id=3000+z,Position=new(-60,z)});initial.Roads.Add(new(){Id=4000+z,Position=new(60,z)});}
        return new CitySimulation(initial);
    }
    [Fact] public void A_blocked_queue_turns_back_from_its_front_through_the_side_lane_without_overlapping()
    {
        var (street,_,_,_)=Street(junctionAtStop:true);var sim=Ring(street);
        var cars=sim.State.Vehicles.OrderBy(v=>v.Id).Take(3).ToArray();var front=cars[1];var rear=cars[2];
        front.Route=front.Route.Take(28).ToList();
        rear.Position=new(-40,38.35);rear.Heading=Math.PI/2;rear.Status=VehicleStatus.Travelling;rear.Fuel=40;rear.Route=front.Route.ToList();rear.RouteIndex=6;
        double? frontTurned=null,rearTurned=null;
        for(var i=0;i<600;i++){
            sim.Advance(1);
            if(rearTurned==null&&rear.Route.Any(id=>id is >=1940 and <=2060))rearTurned=sim.State.ElapsedSeconds;
            if(frontTurned==null&&front.Route.Any(id=>id is >=1940 and <=2060))frontTurned=sim.State.ElapsedSeconds;
            if(front.Status==VehicleStatus.Travelling&&rear.Status==VehicleStatus.Travelling&&Math.Abs(front.Position.Z-rear.Position.Z)<1.5&&Math.Cos(front.Heading-rear.Heading)>.5)Assert.True(front.Position.Distance(rear.Position)>=4,$"{front.Position} {rear.Position}");
        }
        Assert.True(rearTurned!=null,$"rear {rear.Position} {rear.Status} {rear.Cause} idx={rear.RouteIndex}");Assert.True(frontTurned!=null,$"front {front.Position} {front.Status} {front.Cause} idx={front.RouteIndex}");Assert.True(frontTurned<rearTurned,$"{frontTurned} {rearTurned}");
        Assert.All(new[]{front,rear},c=>Assert.Contains(c.Route,id=>id is >=1940 and <=2060));sim.CheckInvariants();
    }
    [Fact] public void Waiting_at_a_junction_held_by_a_stopped_vehicle_turns_back_after_the_same_minutes_and_takes_another_route()
    {
        // The stopped car stands in the junction (4,40) holding it; a westbound car
        // waits right at its mouth, so close that it turns straight across.
        var (street,_,_,_)=Street(junctionAtStop:true);var sim=Ring(street);
        var cars=sim.State.Vehicles.OrderBy(v=>v.Id).Take(2).ToArray();var stopped=cars[0];var waiter=cars[1];
        // Cars: the stopped one ends 2.4 m short of the waiter, too close to swing in ahead.
        stopped.PassengerCar=waiter.PassengerCar=true;stopped.Position=new(4,38.35);stopped.RouteIndex=17;stopped.IntersectionClaim=1004;
        var west=Enumerable.Range(0,19).Select(n=>1012-n*4).ToList();
        waiter.Position=new(8.5,41.65);waiter.Heading=-Math.PI/2;waiter.Route=west;waiter.RouteIndex=2;
        double? turnedAt=null;
        for(var i=0;i<600;i++){
            sim.Advance(1);
            if(turnedAt==null&&waiter.Route.Any(id=>id is >=1940 and <=2060))turnedAt=sim.State.ElapsedSeconds;
            Assert.False(waiter.Status==VehicleStatus.Travelling&&waiter.Position.Distance(stopped.Position)<6&&Math.Abs(waiter.Position.X-stopped.Position.X)<3.9,waiter.Position.ToString());
        }
        Assert.True(turnedAt!=null,$"{waiter.Position} {waiter.Status} {waiter.Cause}");Assert.InRange(turnedAt!.Value,sim.State.Tuning.UTurnWaitSeconds,sim.State.Tuning.UTurnWaitSeconds+30);
        Assert.True(waiter.Position.Distance(sim.State.Roads.Single(r=>r.Id==940).Position)<6||waiter.Status==VehicleStatus.Parked||waiter.RouteIndex>=waiter.Route.Count,waiter.Cause+" "+waiter.Position);
        Assert.Equal(new Point(4,38.35),stopped.Position);sim.CheckInvariants();
    }
    [Fact] public void Behind_a_vehicle_stopped_in_a_junction_one_waiting_at_its_far_mouth_is_not_oncoming_traffic()
    {
        // The stopped car holds the junction (4,40). Beyond it a westbound car waits at
        // the junction for the dead-end side street (no other route): it cannot come
        // through, so the car right behind the stopped one still turns back.
        var (street,_,_,_)=Street(junctionAtStop:true);var sim=Ring(street);
        var cars=sim.State.Vehicles.OrderBy(v=>v.Id).Take(3).ToArray();var stopped=cars[0];var follower=cars[1];var beyond=cars[2];
        stopped.PassengerCar=follower.PassengerCar=beyond.PassengerCar=true;stopped.Position=new(4,38.35);stopped.RouteIndex=17;stopped.IntersectionClaim=1004;
        follower.Position=new(-4,38.35);follower.Route=follower.Route.Take(28).ToList();follower.RouteIndex=15;
        beyond.Position=new(10,41.65);beyond.Heading=-Math.PI/2;beyond.Status=VehicleStatus.Travelling;beyond.Fuel=40;beyond.Route=[1016,1012,1008,1004,5000,5001];beyond.RouteIndex=2;
        double? turnedAt=null;
        for(var i=0;i<400&&turnedAt==null;i++){sim.Advance(1);if(follower.Route.Any(id=>id is >=1940 and <=2060))turnedAt=sim.State.ElapsedSeconds;}
        Assert.True(turnedAt!=null,$"{follower.Position} {follower.Cause} | {beyond.Position} {beyond.Cause}");Assert.InRange(turnedAt!.Value,sim.State.Tuning.UTurnWaitSeconds,sim.State.Tuning.UTurnWaitSeconds+30);
        Assert.True(beyond.Position.X>4,beyond.Position.ToString());Assert.Equal(new Point(4,38.35),stopped.Position);sim.CheckInvariants();
    }
    [Fact] public void A_bypass_is_dropped_when_the_vehicle_stops_and_survives_a_replanned_route()
    {
        var (sim,_,follower,_)=Street();
        for(var i=0;i<200&&follower.Detour.Count==0;i++)sim.Advance(.1);
        Assert.NotEmpty(follower.Detour);
        // Replanned mid-bypass: the old resume node is not on the new route.
        var replanned=CitySimulation.FromJson(sim.SaveToJson());var other=replanned.State.Vehicles.Single(v=>v.Id==follower.Id);
        other.Route=Enumerable.Range(0,10).Select(n=>1000+40-n*4).Prepend(1000+44).ToList();other.RouteIndex=1;
        for(var i=0;i<100;i++)replanned.Advance(.1);
        Assert.Empty(other.Detour);Assert.InRange(other.RouteIndex,1,other.Route.Count);replanned.CheckInvariants();
        follower.Status=VehicleStatus.Parked;sim.Advance(.1);Assert.Empty(follower.Detour);sim.CheckInvariants();
    }
    // Two parallel streets 60 m apart joined only at their west end, both inside
    // one 64 m maintenance cell: the crew zigzags between them (the grid case).
    static CitySimulation ZigzagMaintenance(SimulationTuning? tuning=null)
    {
        var founded=new CitySimulation(42,tuning);Assert.True(founded.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(founded.PlaceRoad(new(-192,0),new(-80,0)).Success);founded.Advance(7200);
        var state=founded.State;
        for(var z=4;z<=60;z+=4)state.Roads.Add(new(){Id=90000+z,Position=new(-192,z)});
        for(var x=-188;x<=-80;x+=4)state.Roads.Add(new(){Id=91000-x,Position=new(x,60)});
        var sim=new CitySimulation(state);var now=sim.State.ElapsedSeconds;
        foreach(var road in sim.State.Roads.Where(r=>!r.Highway))road.MaintenanceDueAt=road.Position.X>=-128&&road.Position.Z is 0 or 60?now-1:now+1e7;
        foreach(var b in sim.State.Buildings)b.MaintenanceDueAt=now+1e7;
        sim.State.EconomyNextAt=now;return sim;
    }
    [Fact] public void A_maintenance_crew_moves_on_only_with_fuel_for_the_next_stretch_and_the_way_out()
    {
        var sim=ZigzagMaintenance();var refuelReturn=false;var drained=false;var paid=new HashSet<(int?,int)>();
        for(var i=0;i<20000&&(i<10||sim.State.Projects.Any(p=>p.IsMaintenance&&p.Status!=WorkStatus.Completed));i++){
            sim.Advance(5);
            // Along the street the stretches are close; leave the crew on site with
            // fuel for the way out only, so its next move must refuel first.
            if(!drained&&sim.State.Vehicles.FirstOrDefault(v=>v.Status==VehicleStatus.Parked&&v.Cause=="Equipe externa no canteiro") is {} crew){
                var exit=sim.FindRoadRoute(crew.Route[^1],sim.State.Roads.First(r=>r.Highway&&r.Position.Distance(sim.State.Entry)<.1).Id,true);Assert.NotEmpty(exit);
                crew.Fuel=exit.Zip(exit.Skip(1)).Sum(pair=>sim.State.Roads.Single(r=>r.Id==pair.First).Position.Distance(sim.State.Roads.Single(r=>r.Id==pair.Second).Position))*sim.State.Tuning.FuelPerMetre+2;drained=true;
            }
            Assert.DoesNotContain(sim.State.Vehicles,v=>v.Status==VehicleStatus.Stranded);
            refuelReturn|=sim.State.Vehicles.Any(v=>v.Cause.StartsWith("Retorno ao exterior para reabastecer"));
            foreach(var s in sim.State.Shipments.Where(s=>s.Purpose==ShipmentPurpose.Maintenance&&s.PricePaid>0))paid.Add((s.ProjectId,s.Id));
        }
        var maintenance=sim.State.Projects.Where(p=>p.IsMaintenance).ToList();Assert.NotEmpty(maintenance);
        Assert.All(maintenance,p=>Assert.True(p.Status==WorkStatus.Completed,p.Cause));Assert.True(refuelReturn);
        // The crew sent again after refuelling outside pays freight and fuel again.
        Assert.Contains(paid.GroupBy(x=>x.Item1),g=>g.Count()>=2);
        Assert.All(sim.State.Roads.Where(r=>!r.Highway&&r.Position.X>=-128&&r.Position.Z is 0 or 60),r=>Assert.Equal(1,r.MaintenanceRatio));sim.CheckInvariants();
    }
    // A street with a side street making a T, all due in one maintenance cell.
    static CitySimulation TeeMaintenance()
    {
        var founded=new CitySimulation(42);Assert.True(founded.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(founded.PlaceRoad(new(-192,0),new(-80,0)).Success);founded.Advance(7200);
        var state=founded.State;for(var z=4;z<=16;z+=4)state.Roads.Add(new(){Id=92000+z,Position=new(-104,z)});
        var sim=new CitySimulation(state);var now=sim.State.ElapsedSeconds;
        foreach(var road in sim.State.Roads.Where(r=>!r.Highway))road.MaintenanceDueAt=road.Position.X>=-120&&road.Position.Z==0||road.Id>=92000?now-1:now+1e7;
        foreach(var b in sim.State.Buildings)b.MaintenanceDueAt=now+1e7;
        sim.State.EconomyNextAt=now;return sim;
    }
    [Fact] public void Maintenance_stretches_follow_one_street_at_a_time_along_it()
    {
        foreach(var (sim,maxJumps) in new[]{(ZigzagMaintenance(),1),(TeeMaintenance(),2)}){
            var due=sim.State.Roads.Where(r=>!r.Highway&&r.MaintenanceDueAt<sim.State.ElapsedSeconds).Select(r=>r.Id).ToHashSet();
            sim.Advance(5);
            var projects=sim.State.Projects.Where(p=>p.IsMaintenance&&p.MaintenanceRoadIds.Count>0).ToList();Assert.NotEmpty(projects);
            // The same stretches are maintained, only the order changes.
            Assert.Equal(due.OrderBy(id=>id),projects.SelectMany(p=>p.MaintenanceRoadIds).OrderBy(id=>id));
            // Consecutive stretches are neighbours along the street; the crew only
            // jumps when it passes to the next street (whatever the street's direction).
            foreach(var p in projects){
                var roads=p.MaintenanceRoadIds.Select(id=>sim.State.Roads.Single(r=>r.Id==id)).ToList();
                var jumps=roads.Zip(roads.Skip(1)).Count(pair=>!pair.First.Neighbours.Contains(pair.Second.Id));
                Assert.True(jumps<=maxJumps,string.Join(" ",roads.Select(r=>$"({r.Position.X},{r.Position.Z})")));
            }
        }
    }
    static T Private<T>(CitySimulation sim,string name,params object[] args)=>(T)typeof(CitySimulation).GetMethod(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(sim,args)!;
    [Fact] public void A_lot_entry_held_by_a_stranded_vehicle_and_its_queue_needs_no_motion_substeps()
    {
        var initial=new CitySimulation(42);for(var x=-60;x<=60;x+=4)initial.State.Roads.Add(new(){Id=1000+x,Position=new(x,40)});
        var sim=new CitySimulation(initial.State);sim.State.Vehicles.Clear();sim.State.EconomyNextAt=1e9;
        var east=Enumerable.Range(0,31).Select(n=>940+n*4).ToList();
        var stopped=new Vehicle{Id=1,External=true,ExternalDriverId="fixture",Position=new(2,38.35),Heading=Math.PI/2,Status=VehicleStatus.Stranded,Fuel=0,Route=east,RouteIndex=16,Cause="Sem combustível; nenhuma viagem fictícia"};
        var queued=new Vehicle{Id=2,External=true,ExternalDriverId="fixture",PassengerCar=true,Position=new(-6,38.35),Heading=Math.PI/2,Status=VehicleStatus.Travelling,Fuel=40,Route=east,RouteIndex=15};
        var exit=new Vehicle{Id=3,External=true,ExternalDriverId="fixture",PassengerCar=true,Position=new(0,33.25),Heading=-Math.PI/2,Status=VehicleStatus.Travelling,Fuel=40,Route=Enumerable.Range(0,11).Select(n=>1000-n*4).ToList()};
        sim.State.Vehicles.AddRange([stopped,queued,exit]);
        // Control: the same entry next to a vehicle that can move is a live entry.
        stopped.Status=VehicleStatus.Travelling;Assert.True(Private<bool>(sim,"LiveLaneEntry",exit));stopped.Status=VehicleStatus.Stranded;
        sim.Advance(4);Assert.True(queued.WaitingSince>0,queued.Cause);Assert.Equal(0,exit.RouteIndex);
        Assert.True(Private<bool>(sim,"HeldBehindStranded",queued));Assert.False(Private<bool>(sim,"LiveLaneEntry",exit));
        var restored=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(4);restored.Advance(4);Assert.Equal(sim.SaveToJson(),restored.SaveToJson());
        Assert.Equal(new Point(2,38.35),stopped.Position);sim.CheckInvariants();
    }
    [Fact] public void A_truck_does_not_leave_for_a_work_whose_round_trip_exceeds_its_tank()
    {
        var sim=new CitySimulation(42,new SimulationTuning{FuelTank=3});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(600);
        var road=sim.State.Projects.Single(p=>p.RoadPoints.Count>0);Assert.True(road.Cause is WorkCause.RouteBeyondTruckRange or WorkCause.PavingInsufficientRange,road.Cause+" shipments="+sim.State.Shipments.Count+" vehicles="+string.Join(",",sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside).Select(v=>v.Status+"/"+v.Cause+"/"+v.Machine)));Assert.Empty(sim.State.Shipments);
        Assert.All(sim.State.Vehicles,v=>Assert.Equal(VehicleStatus.Outside,v.Status));sim.CheckInvariants();
        // Control: the same work with the default tank does dispatch a truck.
        var normal=new CitySimulation(42);normal.PlaceBuilding(BuildingKind.CityHall,new(-152,16));normal.PlaceRoad(new(-192,0),new(100,0));normal.Advance(600);
        Assert.NotEmpty(normal.State.Shipments);
    }
}
