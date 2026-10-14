using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC: human-approved pending connections, 2026-10-14.
// Synthetic seed/candidate settings; all works, materials and payments use real APIs.
public sealed class ConnectedRoadProjectTests
{
    static CitySimulation NewCity()
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,-60)).Success);
        return sim;
    }

    static WorkProject Order(CitySimulation sim,Point from,Point to,bool reverse=false)
    {
        var quote=sim.QuoteRoad(from,to,reverse);var before=sim.SaveToJson();
        Assert.True(quote.PhysicallyValid,quote.Message);
        Assert.Equal(before,sim.SaveToJson());
        var result=sim.PlaceRoad(from,to,reverse);Assert.True(result.Success,result.Message);
        var work=sim.State.Projects.Single(p=>p.Id==result.Id);
        Assert.Equal(quote.Total,work.BudgetRemaining);
        Assert.Equal(quote.Materials.OrderBy(p=>p.Key),work.Required.OrderBy(p=>p.Key));
        return work;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Crossing_and_extending_pending_roads_enqueues_each_point_once(bool reverse)
    {
        var sim=NewCity();
        var first=Order(sim,new(-192,0),new(-192,48));
        var crossing=Order(sim,new(-216,32),new(-144,32),reverse);
        var extension=Order(sim,new(-192,48),new(-144,72),reverse);
        Assert.All(new[]{crossing,extension},work=>Assert.False(sim.QuoteRoad(work.RoadPoints[0],work.RoadPoints[^1]).Accessible));
        var points=sim.State.Projects.SelectMany(p=>p.RoadPoints).ToArray();
        Assert.Equal(points.Length,points.Distinct().Count());
        Assert.Contains(new Point(-192,32),first.RoadPoints);
        Assert.DoesNotContain(new Point(-192,32),crossing.RoadPoints);
        Assert.DoesNotContain(new Point(-192,48),extension.RoadPoints);
        foreach(var resource in new[]{ResourceKind.Asphalt,ResourceKind.Aggregate}){
            var ratio=resource==ResourceKind.Asphalt?.02:.04;
            Assert.Equal(points.Length*sim.State.Tuning.RoadStep*sim.State.Tuning.RoadWidth*ratio,
                sim.State.Projects.Sum(p=>p.RoadPoints.Count>0?p.Required[resource]:0),7);
        }
        var loaded=CitySimulation.FromJson(sim.SaveToJson());
        Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());
        sim.Advance(14400);loaded.Advance(14400);
        Assert.All(new[]{first,crossing,extension},work=>Assert.Equal(WorkStatus.Completed,work.Status));
        Assert.All(points,point=>Assert.Single(sim.State.Roads,n=>n.Position==point));
        Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }

    [Fact]
    public void A_duplicate_pending_trace_creates_no_work_or_commitment()
    {
        var sim=NewCity();Order(sim,new(-192,0),new(-192,48));
        var before=sim.SaveToJson();
        var quote=sim.QuoteRoad(new(-192,0),new(-192,48));
        Assert.False(quote.PhysicallyValid);Assert.Equal(0,quote.Total);
        Assert.False(sim.PlaceRoad(new(-192,0),new(-192,48)).Success);
        Assert.Equal(before,sim.SaveToJson());
    }

    [Fact]
    public void A_partially_built_dependency_keeps_its_real_nodes_when_cancelled()
    {
        var sim=NewCity();var first=Order(sim,new(-192,0),new(-192,48));
        for(var step=0;step<3600&&first.BuiltRoadNodes.Count==0;step++)sim.Advance(2);
        Assert.NotEmpty(first.BuiltRoadNodes);Assert.True(first.BuiltRoadNodes.Count<first.RoadPoints.Count);
        var realNodes=first.BuiltRoadNodes.ToArray();
        var second=Order(sim,new(-192,48),new(-144,48));
        Assert.DoesNotContain(new Point(-192,48),second.RoadPoints);
        Assert.True(sim.CancelProject(first.Id).Success);sim.Advance(600);
        Assert.All(realNodes,id=>Assert.Single(sim.State.Roads,node=>node.Id==id));
        Assert.Equal(0,second.Progress);Assert.Empty(second.Delivered);
        var replacement=Order(sim,new(-192,0),new(-192,48));
        Assert.Equal(first.RoadPoints.Where(point=>sim.State.Roads.All(node=>node.Position.Distance(point)>.05)),replacement.RoadPoints);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }

    [Fact]
    public void Cancelling_a_dependency_does_not_grant_physical_access_or_invent_materials()
    {
        var sim=NewCity();var first=Order(sim,new(-112,80),new(-112,112));
        var second=Order(sim,new(-112,112),new(-80,112));
        Assert.True(sim.CancelProject(first.Id).Success);sim.Advance(600);
        Assert.Equal(0,second.Progress);Assert.Empty(second.Delivered);
        Assert.DoesNotContain(sim.State.Shipments,s=>s.ProjectId==second.Id);
        Assert.DoesNotContain(sim.State.Roads,n=>second.RoadPoints.Contains(n.Position));
        // The cancelled trace no longer owns its still-unbuilt points.
        var replacement=Order(sim,new(-112,80),new(-112,112));
        Assert.Equal(first.RoadPoints,replacement.RoadPoints);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }
}
