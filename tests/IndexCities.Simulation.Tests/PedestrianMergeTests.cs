using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class PedestrianMergeTests
{
    [Fact]
    public void Converging_walkers_do_not_yield_to_each_other_forever_at_a_shared_turn()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(0,0)).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.House,new(-128,14)).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.House,new(-108,14)).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.Grocery,new(-124,-16),Math.PI).Success);
        sim.Advance(7200);
        Assert.True(sim.State.Citizens.Count>=2);
        var turn=new Point(-124,4.2);var finish=new Point(-124.4,-4.55);
        var pair=sim.State.Citizens.Take(2).ToArray();
        var origins=new[]{new Point(-123.60137094166454,4.166911121950283),new Point(-123.69404902988943,4.391492358619452)};
        sim.State.EconomyNextAt=86400;
        for(var i=0;i<pair.Length;i++){
            var citizen=pair[i];citizen.Position=origins[i];citizen.Path=[origins[i],turn,finish];citizen.PathIndex=1;
            citizen.PathRoadRevision=sim.State.RoadRevision;citizen.Purpose=JourneyPurpose.Construction;
            citizen.WorkplaceId=null;citizen.ConstructionProjectId=null;citizen.CrossingNodeId=null;
            citizen.NextDecisionAt=double.MaxValue;
        }
        sim.Advance(10);
        Assert.All(pair,c=>Assert.False(c.Moving,$"{c.Id}: {c.Position} {c.Activity}"));
        Assert.All(pair,c=>Assert.Equal(finish,c.Position));
        Assert.All(pair,c=>Assert.Null(c.CrossingNodeId));
        sim.CheckInvariants();
    }
}
