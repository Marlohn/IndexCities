using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class NearbyJunctionTests
{
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
        var outbound=sim.State.Vehicles[0];var returning=sim.State.Vehicles[1];
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
