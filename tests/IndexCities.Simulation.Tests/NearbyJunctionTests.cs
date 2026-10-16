using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class NearbyJunctionTests
{
    static (CitySimulation Sim,RoadNode First,RoadNode Second,Vehicle Outbound,Vehicle Returning) AdjacentCity()
    {
        var initial=new CitySimulation(42);var first=initial.State.Roads.Single(r=>r.Position==new Point(-232,0));var second=initial.State.Roads.Single(r=>r.Position==new Point(-228,0));
        initial.State.Roads.AddRange([new(){Id=1000,Position=new(-232,4),Crosswalk=true},new(){Id=1001,Position=new(-228,4),Control=JunctionControl.ThroughZ,Crosswalk=true},new(){Id=1002,Position=new(-228,8)}]);var sim=new CitySimulation(initial.State);
        var outbound=sim.State.Vehicles[0];outbound.Position=new(-232,-1.65);outbound.Heading=Math.PI/2;outbound.Fuel=40;outbound.Status=VehicleStatus.Travelling;outbound.Route=[first.Id,second.Id,1001,1002];outbound.RouteIndex=1;outbound.IntersectionClaim=first.Id;outbound.WaitingSince=486;outbound.Cargo=new(){{ResourceKind.Aggregate,8}};
        var returning=sim.State.Vehicles[2];returning.Position=new(-229.65,0);returning.Heading=-.9586438298498121;returning.Fuel=40;returning.Status=VehicleStatus.Travelling;returning.Route=[second.Id,first.Id,6,5,4,3,2,1];returning.RouteIndex=1;returning.IntersectionClaim=second.Id;returning.WaitingSince=484;
        sim.State.EconomyNextAt=86400;return(sim,first,second,outbound,returning);
    }
    [Fact] public void Adjacent_junction_reservations_recover_without_moving_the_other_occupant_or_losing_cargo()
    {
        var (sim,_,_,outbound,returning)=AdjacentCity();var position=outbound.Position;var returnPosition=returning.Position;var fuel=returning.Fuel;
        sim.Advance(.1);Assert.True(returning.Position.Distance(returnPosition)>.1,returning.Cause);Assert.Equal(position,outbound.Position);Assert.True(returning.Fuel<fuel);Assert.Equal(8,outbound.Cargo[ResourceKind.Aggregate]);
        var restored=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(2);restored.Advance(2);Assert.Equal(sim.SaveToJson(),restored.SaveToJson());Assert.True(outbound.Position.Distance(position)>1,outbound.Cause);sim.CheckInvariants();
    }
    [Fact] public void Overlapping_junction_reservations_across_intermediate_road_samples_do_not_deadlock()
    {
        foreach(var distance in new[]{8,12}){
            var initial=new CitySimulation(42);var line=new List<RoadNode>();for(var x=0;x<=distance;x+=4){var node=new RoadNode{Id=1000+x,Position=new(x,40)};line.Add(node);initial.State.Roads.Add(node);}
            initial.State.Roads.AddRange([new(){Id=1100,Position=new(-4,40)},new(){Id=1101,Position=new(0,44)},new(){Id=1102,Position=new(distance+4,40)},new(){Id=1103,Position=new(distance,44)}]);var sim=new CitySimulation(initial.State);
            var a=sim.State.Vehicles[0];a.Position=new(distance/2.0,38.35);a.Heading=Math.PI/2;a.Status=VehicleStatus.Travelling;a.Fuel=40;a.Route=line.Select(r=>r.Id).ToList();a.RouteIndex=line.Count-1;a.IntersectionClaim=line[0].Id;a.WaitingSince=10;
            var b=sim.State.Vehicles[1];b.Position=new(distance/2.0,41.65);b.Heading=-Math.PI/2;b.Status=VehicleStatus.Travelling;b.Fuel=40;b.Route=line.AsEnumerable().Reverse().Select(r=>r.Id).ToList();b.RouteIndex=line.Count-1;b.IntersectionClaim=line[^1].Id;b.WaitingSince=20;
            var pa=a.Position;var pb=b.Position;sim.State.EconomyNextAt=86400;sim.Advance(.1);Assert.True(a.Position.Distance(pa)>0,a.Cause);Assert.Equal(pb,b.Position);Assert.True(a.Fuel<40);sim.Advance(2);Assert.True(b.Position.Distance(pb)>0,b.Cause);sim.CheckInvariants();
        }
    }
    [Fact] public void Nearby_parallel_streets_without_a_short_road_connection_do_not_share_reservations()
    {
        var initial=new CitySimulation(42);var first=new RoadNode{Id=1000,Position=new(0,40)};var second=new RoadNode{Id=1001,Position=new(0,48)};
        initial.State.Roads.AddRange([first,second,new(){Id=1100,Position=new(-4,40)},new(){Id=1101,Position=new(4,40)},new(){Id=1102,Position=new(0,36)},new(){Id=1103,Position=new(-4,48)},new(){Id=1104,Position=new(4,48)},new(){Id=1105,Position=new(0,52)}]);var sim=new CitySimulation(initial.State);
        var busy=sim.State.Vehicles[0];busy.Position=first.Position;busy.IntersectionClaim=first.Id;busy.WaitingSince=10;busy.Status=VehicleStatus.Waiting;
        var passing=sim.State.Vehicles[1];passing.Position=new(-4,46.35);passing.Heading=Math.PI/2;passing.Status=VehicleStatus.Travelling;passing.Route=[1103,second.Id,1104];passing.RouteIndex=1;passing.Fuel=40;
        var enter=typeof(CitySimulation).GetMethod("MayEnterJunction",BindingFlags.Instance|BindingFlags.NonPublic)!;Assert.True((bool)enter.Invoke(sim,[passing,second])!);var position=passing.Position;sim.State.EconomyNextAt=86400;sim.Advance(.1);Assert.True(passing.Position.Distance(position)>0,passing.Cause);sim.CheckInvariants();
    }
    [Fact] public void Parallel_streets_connected_only_by_a_distant_junction_chain_do_not_share_local_reservations()
    {
        var initial=new CitySimulation(42);var lower=new List<RoadNode>();var upper=new List<RoadNode>();
        for(var x=0;x<=100;x+=4){
            var a=new RoadNode{Id=1000+x,Position=new(x,40)};var b=new RoadNode{Id=2000+x,Position=new(x,48)};lower.Add(a);upper.Add(b);initial.State.Roads.AddRange([a,b,new(){Id=3000+x,Position=new(x,36)},new(){Id=4000+x,Position=new(x,52)}]);
        }
        initial.State.Roads.AddRange([new(){Id=5000,Position=new(100,44)},new(){Id=5001,Position=new(-4,40)},new(){Id=5002,Position=new(-4,48)}]);var sim=new CitySimulation(initial.State);
        var busy=sim.State.Vehicles[0];busy.Position=new(0,41.65);busy.IntersectionClaim=lower[0].Id;busy.Status=VehicleStatus.Waiting;busy.WaitingSince=10;
        var passing=sim.State.Vehicles[1];passing.Position=new(-4,46.35);passing.Heading=Math.PI/2;passing.Status=VehicleStatus.Travelling;passing.Route=[5002,upper[0].Id,upper[1].Id];passing.RouteIndex=1;passing.Fuel=40;
        Assert.NotEmpty(sim.FindRoadRoute(lower[0].Id,upper[0].Id));var enter=typeof(CitySimulation).GetMethod("MayEnterJunction",BindingFlags.Instance|BindingFlags.NonPublic)!;Assert.True((bool)enter.Invoke(sim,[passing,upper[0]])!);
        var position=passing.Position;sim.State.EconomyNextAt=86400;sim.Advance(.1);Assert.True(passing.Position.Distance(position)>0,passing.Cause);Assert.Equal(lower[0].Id,busy.IntersectionClaim);sim.CheckInvariants();
    }
    [Fact] public void Adjacent_area_ownership_still_respects_a_walker_and_an_edited_signal_at_the_next_node()
    {
        var (sim,first,second,outbound,returning)=AdjacentCity();returning.IntersectionClaim=null;returning.Status=VehicleStatus.Outside;returning.Position=sim.State.Entry;
        var walker=new Citizen{Id=1100,Position=second.Position,Path=[second.Position,second.Position+new Point(0,8)],CrossingNodeId=second.Id};sim.State.Citizens.Add(walker);
        var enter=typeof(CitySimulation).GetMethod("MayEnterJunction",BindingFlags.Instance|BindingFlags.NonPublic)!;Assert.False((bool)enter.Invoke(sim,[outbound,second])!);Assert.Equal(first.Id,outbound.IntersectionClaim);
        walker.Path.Clear();second.Control=JunctionControl.Signal;second.PlayerEdited=true;sim.State.ElapsedSeconds=31;Assert.False((bool)enter.Invoke(sim,[outbound,second])!);Assert.Equal(first.Id,outbound.IntersectionClaim);Assert.Equal(JunctionControl.Signal,second.Control);
        sim.State.ElapsedSeconds=61;Assert.True((bool)enter.Invoke(sim,[outbound,second])!);Assert.Equal(JunctionControl.Signal,second.Control);sim.CheckInvariants();
    }
    [Fact] public void New_adjacent_admissions_wait_until_the_existing_vehicle_physically_clears_the_shared_pavement()
    {
        var (sim,first,second,outbound,returning)=AdjacentCity();sim.State.Roads.AddRange([new(){Id=1003,Position=new(-228,12)},new(){Id=1004,Position=new(-228,16)}]);sim=new CitySimulation(sim.State);
        outbound.IntersectionClaim=null;outbound.WaitingSince=0;outbound.Route=[first.Id,second.Id,1001,1002,1003,1004];outbound.RouteIndex=1;returning.IntersectionClaim=null;returning.WaitingSince=0;
        var enter=typeof(CitySimulation).GetMethod("MayEnterJunction",BindingFlags.Instance|BindingFlags.NonPublic)!;Assert.True((bool)enter.Invoke(sim,[outbound,first])!);
        Assert.False((bool)enter.Invoke(sim,[returning,second])!);Assert.Null(returning.IntersectionClaim);var otherPosition=returning.Position;
        sim.Advance(.1);Assert.Equal(otherPosition,returning.Position);Assert.NotNull(outbound.IntersectionClaim);Assert.Null(returning.IntersectionClaim);
        sim.Advance(6);Assert.Null(outbound.IntersectionClaim);Assert.True(outbound.Position.Distance(second.Position)>6.9);Assert.True(returning.Position.Distance(otherPosition)>1,returning.Cause);sim.CheckInvariants();
    }
    [Fact] public void A_long_chain_of_adjacent_junctions_does_not_reserve_distant_pavement()
    {
        var initial=new CitySimulation(42);var line=new List<RoadNode>();
        for(var i=0;i<=10;i++){var road=new RoadNode{Id=1000+i,Position=new(-200+i*4,80)};line.Add(road);initial.State.Roads.Add(road);initial.State.Roads.Add(new(){Id=1100+i,Position=road.Position+new Point(0,4)});}
        initial.State.Roads.AddRange([new(){Id=1200,Position=line[0].Position+new Point(0,-4)},new(){Id=1201,Position=line[^1].Position+new Point(0,-4)}]);var sim=new CitySimulation(initial.State);
        var busy=sim.State.Vehicles[0];busy.Position=line[0].Position+new Point(0,-1.65);busy.Status=VehicleStatus.Waiting;busy.IntersectionClaim=line[0].Id;
        var distant=sim.State.Vehicles[1];distant.Position=line[^2].Position+new Point(0,-1.65);distant.Heading=Math.PI/2;distant.Status=VehicleStatus.Travelling;distant.Fuel=40;distant.Route=[line[^2].Id,line[^1].Id];distant.RouteIndex=1;
        var enter=typeof(CitySimulation).GetMethod("MayEnterJunction",BindingFlags.Instance|BindingFlags.NonPublic)!;Assert.True((bool)enter.Invoke(sim,[distant,line[^1]])!);Assert.Equal(line[0].Id,busy.IntersectionClaim);
        var position=distant.Position;sim.Advance(.1);Assert.True(distant.Position.Distance(position)>0,distant.Cause);Assert.Equal(line[0].Id,busy.IntersectionClaim);sim.CheckInvariants();
    }
    [Fact]
    public void Straight_crosswalk_still_yields_to_a_walker_physically_crossing()
    {
        var initial=new CitySimulation(42);var crossing=new RoadNode{Id=1000,Position=new(0,0),Crosswalk=true};
        initial.State.Roads.AddRange([crossing,new(){Id=1001,Position=new(-4,0)},new(){Id=1002,Position=new(4,0)}]);var sim=new CitySimulation(initial.State);
        var walker=new Citizen{Id=1100,Position=new(0,2),Path=[new(0,2),new(0,8)],CrossingNodeId=crossing.Id};sim.State.Citizens.Add(walker);
        var vehicle=sim.State.Vehicles[0];vehicle.Position=new(-6,-1.65);vehicle.Heading=Math.PI/2;vehicle.IntersectionClaim=null;
        var enter=typeof(CitySimulation).GetMethod("MayEnterJunction",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Assert.False((bool)enter.Invoke(sim,[vehicle,crossing])!);walker.Position=new(0,20);Assert.True((bool)enter.Invoke(sim,[vehicle,crossing])!);sim.CheckInvariants();
    }
    [Fact]
    public void Straight_crosswalk_does_not_apply_junction_priority_to_a_truck_leaving_a_loading_bay()
    {
        var initial=new CitySimulation(42);var south=new RoadNode{Id=1000,Position=new(-184,216)};var tee=new RoadNode{Id=1001,Position=new(-184,220),Control=JunctionControl.ThroughX,Crosswalk=true};
        var west=new RoadNode{Id=1002,Position=new(-188,220)};var crossing=new RoadNode{Id=1003,Position=new(-180,220),Crosswalk=true};var east=new RoadNode{Id=1004,Position=new(-176,220)};
        initial.State.Roads.AddRange([south,tee,west,crossing,east]);var sim=new CitySimulation(initial.State);
        var entering=sim.State.Vehicles[0];entering.Position=new(-182.35,220);entering.Heading=2.1829488237399826;entering.Status=VehicleStatus.Travelling;entering.Fuel=40;entering.Route=[south.Id,tee.Id,crossing.Id,east.Id];entering.RouteIndex=2;entering.IntersectionClaim=tee.Id;entering.Cargo=new(){{ResourceKind.Wood,10}};
        var leaving=sim.State.Vehicles[1];leaving.Position=new(-180,226.75);leaving.Heading=Math.PI;leaving.Status=VehicleStatus.Travelling;leaving.Fuel=40;leaving.Route=[crossing.Id,tee.Id,south.Id];
        Assert.Equal(2,crossing.Neighbours.Count);sim.State.EconomyNextAt=86400;var position=entering.Position;var fuel=entering.Fuel;sim.Advance(2);
        Assert.True(entering.Position.Distance(position)>1,entering.Cause);Assert.True(entering.Fuel<fuel);Assert.Equal(10,entering.Cargo[ResourceKind.Wood]);sim.CheckInvariants();
    }
    static (CitySimulation Sim,RoadNode First,RoadNode Second,Vehicle Outbound,Vehicle Returning) City()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-192,24)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-192,-24)).Success);sim.Advance(7200);
        Assert.True(sim.PlaceRoad(new(-192,-12),new(-144,-12)).Success);sim.Advance(7200);
        var first=sim.State.Roads.Single(r=>r.Position==new Point(-192,0));
        var second=sim.State.Roads.Single(r=>r.Position==new Point(-192,-12));
        Assert.Equal(JunctionControl.ThroughZ,second.Control);
        var fuelled=sim.State.Vehicles.Where(v=>v.Machine==PavingMachine.None&&v.Fuel>0).ToArray();var outbound=fuelled[0];var returning=fuelled[1];
        outbound.Position=new(-193.65,-8);outbound.Status=VehicleStatus.Travelling;outbound.Heading=Math.PI;
        outbound.Route=sim.FindRoadRoute(sim.State.Roads.Single(r=>r.Position==new Point(-192,-8)).Id,sim.State.Roads.Single(r=>r.Position==new Point(-144,-12)).Id);
        outbound.RouteIndex=1;outbound.DockPosition=null;outbound.ShipmentId=null;outbound.IntersectionClaim=first.Id;
        returning.Position=new(-190.35,-4);returning.Status=VehicleStatus.Travelling;returning.Heading=0;
        returning.Route=sim.FindRoadRoute(sim.State.Roads.Single(r=>r.Position==new Point(-192,-4)).Id,sim.State.Roads.Single(r=>r.Position==sim.State.Entry).Id);
        returning.RouteIndex=1;returning.DockPosition=null;returning.ShipmentId=null;returning.IntersectionClaim=second.Id;
        Assert.True(outbound.Fuel>0);Assert.True(returning.Fuel>0);sim.State.EconomyNextAt=86400;
        return(sim,first,second,outbound,returning);
    }

    [Fact]
    public void Trucks_that_passed_nearby_junctions_release_old_reservations_before_waiting_at_the_next()
    {
        var (sim,_,_,outbound,returning)=City();
        var positions=new[]{outbound.Position,returning.Position};var fuels=new[]{outbound.Fuel,returning.Fuel};
        sim.Advance(2);
        Assert.True(outbound.Position.Distance(positions[0])>1,outbound.Cause);
        Assert.True(returning.Position.Distance(positions[1])>1,returning.Cause);
        Assert.True(outbound.Fuel<fuels[0]);Assert.True(returning.Fuel<fuels[1]);
        sim.CheckInvariants();
    }

    [Fact]
    public void Reservation_stays_while_the_truck_body_has_not_cleared_the_junction()
    {
        var (sim,first,_,outbound,returning)=City();
        outbound.Position=new(-193.65,-4);outbound.Status=VehicleStatus.Waiting;outbound.IntersectionClaim=first.Id;
        returning.Position=new(-196,-1.65);returning.Heading=Math.PI/2;returning.IntersectionClaim=null;
        returning.Route=sim.FindRoadRoute(sim.State.Roads.Single(r=>r.Position==new Point(-196,0)).Id,sim.State.Roads.Single(r=>r.Position==new Point(-144,-12)).Id);
        returning.RouteIndex=1;var before=returning.Position;
        sim.Advance(.1);
        Assert.Equal(first.Id,outbound.IntersectionClaim);Assert.Equal(before,returning.Position);
        Assert.Contains("Aguardando",returning.Cause);sim.CheckInvariants();
    }
}
