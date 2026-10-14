using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;
public sealed class RoadBootstrapTests
{
    [Fact]
    public void Pending_road_accepts_a_shared_connection_and_a_real_short_connection_unblocks_construction()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-200,16)).Success);
        var pending=sim.PlaceRoad(new(-216,-16),new(-184,36));
        Assert.True(pending.Success);
        sim.Advance(3600);
        var work=sim.State.Projects.Single(p=>p.Id==pending.Id);
        Assert.Equal(0,work.Progress);
        Assert.Empty(work.Delivered);
        var connection=sim.QuoteRoad(new(-192,0),new(-128,0));
        Assert.True(connection.PhysicallyValid,connection.Message);
        Assert.True(connection.Materials[ResourceKind.Asphalt]>0);
        var bridge=sim.PlaceRoad(new(-192,0),new(-188,0));
        Assert.True(bridge.Success);
        sim.Advance(3600);
        Assert.Equal(WorkStatus.Completed,work.Status);
        Assert.Equal(WorkStatus.Completed,sim.State.Projects.Single(p=>p.Id==bridge.Id).Status);
        var second=sim.PlaceRoad(new(-188,0),new(-128,0));
        Assert.True(second.Success);
        sim.Advance(3600);
        Assert.Equal(WorkStatus.Completed,sim.State.Projects.Single(p=>p.Id==second.Id).Status);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);
        sim.CheckInvariants();
    }
}
