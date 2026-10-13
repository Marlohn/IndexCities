using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class NearbyJunctionTests
{
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
