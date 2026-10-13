using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class PedestrianCrossingTests
{
    static (CitySimulation Sim,Citizen Walker,Vehicle Truck,Building Home,RoadNode Corner,RoadNode Access) CornerCity()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-128,132),true).Success);
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-190,146));Assert.True(placed.Success);
        sim.Advance(7200);
        var home=sim.State.Buildings.Single(b=>b.Id==placed.Id);Assert.True(home.Completed);
        var citizen=sim.State.Citizens.First();
        var corner=sim.State.Roads.Single(r=>r.Position==new Point(-192,132));
        var access=sim.Access(home)!;Assert.True(corner.Crosswalk);Assert.True(access.Crosswalk);
        var truck=sim.State.Vehicles.First();truck.Position=new(-190.35,132);truck.Status=VehicleStatus.Travelling;
        truck.Route=[corner.Id,access.Id];truck.RouteIndex=1;truck.DockPosition=null;truck.ShipmentId=null;
        truck.Fuel=40;truck.Heading=2.1829488237399826;truck.IntersectionClaim=corner.Id;
        citizen.WorkplaceId=null;citizen.ConstructionProjectId=null;citizen.NextDecisionAt=double.MaxValue;
        citizen.PathRoadRevision=sim.State.RoadRevision;citizen.Purpose=JourneyPurpose.Home;citizen.DestinationId=home.Id;
        sim.State.EconomyNextAt=86400;
        return(sim,citizen,truck,home,corner,access);
    }

    [Fact]
    public void Distant_crossing_does_not_stop_a_walker_on_a_seventy_metre_sidewalk_approach()
    {
        var (sim,walker,truck,_,_,_)=CornerCity();
        walker.Position=new(-187.1,59.6);walker.Path=[walker.Position,new(-187.1,136.2)];walker.PathIndex=1;walker.CrossingNodeId=null;
        var before=walker.Position;sim.Advance(.1);
        Assert.True(walker.Position.Distance(before)>.1,$"{walker.Activity}: {walker.Position}");
        Assert.Null(walker.CrossingNodeId);Assert.True(truck.Position.Distance(before)>60);
        sim.CheckInvariants();
    }

    [Fact]
    public void Curb_beyond_the_end_of_a_corner_is_not_another_carriageway_crossing()
    {
        var (sim,walker,truck,home,_,access)=CornerCity();
        walker.Position=new(-187.6291547132232,136.34991255209428);
        walker.Path=[walker.Position,new(-189.4388363353766,136.31650900612985),home.Door];walker.PathIndex=1;walker.CrossingNodeId=access.Id;
        var truckBefore=truck.Position;sim.Advance(10);
        Assert.False(walker.Moving,$"{walker.Activity}: {walker.Position}");
        Assert.Equal(home.Door,walker.Position);Assert.Null(walker.CrossingNodeId);
        Assert.True(truck.Position.Distance(truckBefore)>1,$"{truck.Cause}: {truck.Position}");
        sim.CheckInvariants();
    }

    [Fact]
    public void Saved_detour_beside_the_destination_door_resumes_without_crossing_the_road_and_back()
    {
        var (sim,walker,_,home,_,access)=CornerCity();
        walker.Position=home.Door+new Point(0,1);
        walker.Path=[walker.Position,new(-192,127.45),home.Door];walker.PathIndex=1;
        walker.CrossingNodeId=access.Id;
        // The road revision is unchanged, as in an already stalled old save.
        walker.PathRoadRevision=sim.State.RoadRevision;
        var balance=sim.CityBalance;sim.Advance(1);
        Assert.False(walker.Moving);Assert.Equal(home.Door,walker.Position);
        Assert.Null(walker.CrossingNodeId);Assert.Equal(balance,sim.CityBalance);
        sim.CheckInvariants();
    }
}
