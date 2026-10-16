using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC: human-approved pending connections, 2026-10-14.
// Synthetic seed/candidate settings; all works, materials and payments use real APIs.
public sealed class ConnectedRoadProjectTests
{
    [Fact]
    public void Streets_build_and_can_be_removed_before_the_hall_without_unlocking_other_buildings()
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
        Assert.False(sim.QuoteBuilding(BuildingKind.House,new(-100,-60)).PhysicallyValid);
        var road=Order(sim,new(-192,0),new(-192,24));
        Assert.False(sim.State.CityHallPlaced);Assert.Empty(sim.State.Buildings);
        Assert.True(sim.Committed>0);
        sim.Advance(14400);
        Assert.Equal(WorkStatus.Completed,road.Status);Assert.True(road.CostPaid>0);
        Assert.Contains(sim.State.Shipments,s=>s.ProjectId==road.Id);
        Assert.False(sim.State.CityHallPlaced);
        var node=sim.State.Roads.Single(r=>r.Position==new Point(-192,24));
        var quote=sim.QuoteRoadDemolition(node.Id);Assert.True(quote.Allowed);
        var balance=sim.CityBalance;Assert.True(sim.DemolishRoad(node.Id).Success);
        Assert.Equal(balance-quote.Cost,sim.CityBalance);
        Assert.False(sim.QuoteRoadDemolition(sim.State.Roads.First(r=>r.Highway).Id).Allowed);
        Assert.False(sim.QuoteBuilding(BuildingKind.House,new(-100,-60)).PhysicallyValid);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }

    [Fact]
    public void A_short_approach_that_reaches_and_follows_a_pending_street_keeps_its_real_tee()
    {
        var sim=NewCity();var trunk=Order(sim,new(-192,0),new(-192,64));
        var branch=Order(sim,new(-200,32),new(-192,56));
        Assert.Equal(new[]{new Point(-200,32),new Point(-196,32)},branch.RoadPoints);
        sim.Advance(14400);
        Assert.Equal(WorkStatus.Completed,trunk.Status);Assert.Equal(WorkStatus.Completed,branch.Status);
        Assert.Equal(3,sim.State.Roads.Single(r=>r.Position==new Point(-192,32)).Neighbours.Count);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }

    [Theory]
    [InlineData(4,false)]
    [InlineData(8,false)]
    [InlineData(4,true)]
    [InlineData(8,true)]
    public void A_bend_shorter_than_the_street_width_is_rejected_without_ordering(double leg,bool reverse)
    {
        var sim=NewCity();var before=sim.SaveToJson();
        var quote=sim.QuoteRoad(new(-160,80),new(-120,80+leg),reverse);
        Assert.False(quote.PhysicallyValid);Assert.Contains("Curva muito curta",quote.Message);
        Assert.False(sim.PlaceRoad(new(-160,80),new(-120,80+leg),reverse).Success);
        Assert.Equal(before,sim.SaveToJson());
        Assert.True(sim.QuoteRoad(new(-160,80),new(-120,92),reverse).PhysicallyValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Half_step_hall_can_lack_a_valid_front_street_but_full_step_hall_fits(int quarterTurn)
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
        var rotation=quarterTurn*Math.PI/2;
        var hall=new Point(-150,14);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,hall,rotation).Success);
        // Front-facing streets need both physical clearance and the existing
        // door-to-road access distance. No new access rule is assumed here.
        var candidates=Enumerable.Range(-10,11).Select(i=>quarterTurn==0
            ?(A:new Point(-176,4*i),B:new Point(-128,4*i))
            :(A:new Point(-176+4*i,-8),B:new Point(-176+4*i,36))).ToArray();
        var building=sim.State.Buildings.Single();
        var feasible=candidates.Where(c=>sim.QuoteRoad(c.A,c.B).PhysicallyValid)
            .Where(c=>sim.RoadShape(c.A,c.B).Any(p=>p.Distance(building.Door)<=sim.State.Tuning.RoadWidth/2+3)).ToArray();
        Assert.Empty(feasible);
        var aligned=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
        Assert.True(aligned.PlaceBuilding(BuildingKind.CityHall,new(-152,16),rotation).Success);
        var start=quarterTurn==0?new Point(-176,0):new Point(-168,-8);
        var end=quarterTurn==0?new Point(-128,0):new Point(-168,36);
        Assert.True(aligned.QuoteRoad(start,end).PhysicallyValid);
        Assert.Contains(aligned.RoadShape(start,end),p=>p.Distance(aligned.State.Buildings.Single().Door)<=aligned.State.Tuning.RoadWidth/2+3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Surface_overlap_without_a_local_graph_connection_is_rejected(bool completed)
    {
        var sim=NewCity();var trunk=Order(sim,new(-192,0),new(-192,64));
        if(completed){sim.Advance(14400);Assert.Equal(WorkStatus.Completed,trunk.Status);}
        var before=sim.SaveToJson();
        var quote=sim.QuoteRoad(new(-184,16),new(-184,48));
        Assert.False(quote.PhysicallyValid);Assert.Contains("sem encontro viário",quote.Message);
        Assert.False(sim.PlaceRoad(new(-184,16),new(-184,48)).Success);
        Assert.Equal(before,sim.SaveToJson());
        // Move onto the actual axis: an ordinary T and a crossing stay legal.
        var tee=Order(sim,new(-192,32),new(-144,32));
        var crossing=Order(sim,new(-216,48),new(-160,48));
        sim.Advance(14400);
        Assert.True(tee.Status==WorkStatus.Completed&&crossing.Status==WorkStatus.Completed,string.Join("; ",sim.State.Projects.Select(p=>$"P{p.Id} {p.Status} {p.Progress}: {p.Cause}"))+" | "+string.Join("; ",sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside).Select(v=>$"V{v.Id} {v.Machine} {v.MachinePhase} {v.Status} {v.Position} -> {v.DockPosition}: {v.Cause}")));
        var a=sim.State.Roads.Single(r=>r.Position==new Point(-192,32));
        var b=sim.State.Roads.Single(r=>r.Position==new Point(-144,32));
        Assert.NotEmpty(sim.FindRoadRoute(a.Id,b.Id));sim.CheckInvariants();
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_close_parallel_cannot_fold_back_over_a_street_even_when_the_graph_connects(bool completed)
    {
        var sim=NewCity();var trunk=Order(sim,new(-192,0),new(-192,64));
        if(completed){sim.Advance(14400);Assert.Equal(WorkStatus.Completed,trunk.Status);}
        // The first arm follows an existing road. Its total length hides the
        // 4 m separation of the returning arm from the trunk.
        var before=sim.SaveToJson();
        var quote=sim.QuoteRoad(new(-208,0),new(-188,48));
        Assert.False(quote.PhysicallyValid);
        Assert.False(sim.PlaceRoad(new(-208,0),new(-188,48)).Success);
        Assert.Equal(before,sim.SaveToJson());
        // A short T remains a real junction, with no returning parallel arm.
        var tee=Order(sim,new(-192,32),new(-184,32));
        sim.Advance(14400);Assert.True(tee.Status==WorkStatus.Completed,PavingTests.Diagnostic(sim));
        Assert.Equal(3,sim.State.Roads.Single(r=>r.Position==new Point(-192,32)).Neighbours.Count);
        sim.CheckInvariants();Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);
    }

    [Theory]
    [InlineData(false,4)]
    [InlineData(true,4)]
    [InlineData(false,8)]
    [InlineData(true,8)]
    public void A_straight_order_cannot_make_a_tiny_offset_at_a_ready_or_pending_endpoint(bool completed,double offset)
    {
        var sim=NewCity();var trunk=Order(sim,new(-192,0),new(-160,0));
        if(completed){sim.Advance(14400);Assert.Equal(WorkStatus.Completed,trunk.Status);}
        if(offset>sim.State.Tuning.RoadStep)Order(sim,new(-160,0),new(-160,offset));
        var before=sim.SaveToJson();var quote=sim.QuoteRoad(new(-160,offset),new(-144,offset));
        Assert.False(quote.PhysicallyValid);Assert.Contains("Curva muito curta",quote.Message);
        Assert.False(sim.PlaceRoad(new(-160,offset),new(-144,offset)).Success);
        Assert.Equal(before,sim.SaveToJson());
        Assert.True(sim.QuoteRoad(new(-160,0),new(-160,24)).PhysicallyValid);
    }

    [Theory]
    [InlineData(false,4)]
    [InlineData(true,4)]
    [InlineData(false,8)]
    [InlineData(true,8)]
    public void A_short_tee_near_an_unchanged_corner_remains_valid(bool completed,double offset)
    {
        var sim=NewCity();var trunk=Order(sim,new(-192,0),new(-160,48));
        if(completed){sim.Advance(14400);Assert.Equal(WorkStatus.Completed,trunk.Status);}
        var tee=Order(sim,new(-160,offset),new(-152,offset));
        sim.Advance(14400);Assert.True(tee.Status==WorkStatus.Completed,PavingTests.Diagnostic(sim));
        Assert.Equal(3,sim.State.Roads.Single(r=>r.Position==new Point(-160,offset)).Neighbours.Count);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }

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
    public void Planned_building_access_is_described_without_granting_a_delivery_route()
    {
        var sim=NewCity();var road=Order(sim,new(-192,48),new(-144,48));
        var before=sim.SaveToJson();
        var quote=sim.QuoteBuilding(BuildingKind.House,new(-168,62));
        Assert.True(quote.PhysicallyValid,quote.Message);Assert.False(quote.Accessible);
        Assert.Contains("Rua em obra",quote.Message);Assert.Equal(before,sim.SaveToJson());
        Assert.True(sim.CancelProject(road.Id).Success);
        quote=sim.QuoteBuilding(BuildingKind.House,new(-168,62));
        Assert.DoesNotContain("Rua em obra",quote.Message);Assert.False(quote.Accessible);
        sim.CheckInvariants();
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

    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void A_crossing_uses_finished_work_access_and_never_crosses_an_unbuilt_shared_point(bool cancelOwner,bool demolishOwnPoint)
    {
        var sim=NewCity();var owner=Order(sim,new(-180,-16),new(-180,16));
        var crossing=Order(sim,new(-192,0),new(-160,0));
        for(var step=0;step<3600&&crossing.RolledRoadPoints.Count<2;step++)sim.Advance(2);
        Assert.True(crossing.Progress>0&&crossing.Progress<1);Assert.NotEqual(WorkStatus.Completed,crossing.Status);
        Assert.Null(crossing.CompletedAt);
        Assert.Null(crossing.CrewVehicleId);Assert.Empty(crossing.LocalCrew);
        Assert.DoesNotContain(sim.State.Roads,node=>node.Position==new Point(-180,0));
        var paid=crossing.CostPaid;var shipments=sim.State.Shipments.Count(s=>s.ProjectId==crossing.Id);
        if(cancelOwner){
            Assert.True(sim.CancelProject(owner.Id).Success);
            if(demolishOwnPoint)Assert.True(sim.DemolishRoad(sim.State.Roads.Single(node=>node.Position==new Point(-184,0)).Id).Success);
            var budget=crossing.BudgetRemaining;var loaded=CitySimulation.FromJson(sim.SaveToJson());
            sim.Advance(600);loaded.Advance(600);
            Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());
            Assert.NotEqual(WorkStatus.Completed,crossing.Status);Assert.Equal(budget,crossing.BudgetRemaining);
            Assert.Equal(paid,crossing.CostPaid);Assert.Equal(shipments,sim.State.Shipments.Count(s=>s.ProjectId==crossing.Id));
            var repair=Order(sim,new(-188,0),new(-176,0));
            Assert.Contains(new Point(-180,0),repair.RoadPoints);
            if(demolishOwnPoint)Assert.Contains(new Point(-184,0),repair.RoadPoints);
            Assert.True(repair.Required[ResourceKind.Asphalt]>0);
        }
        for(var step=0;step<3600&&crossing.Status!=WorkStatus.Completed;step++)sim.Advance(2);
        Assert.True(crossing.Status==WorkStatus.Completed,string.Join("; ",sim.State.Projects.Select(p=>$"#{p.Id} {p.Status} {p.Progress}: {p.Cause}"))+" | "+string.Join("; ",sim.State.Vehicles.Select(v=>$"V{v.Id} {v.Status} {v.Position}: {v.Cause}")));Assert.NotNull(crossing.CompletedAt);
        Assert.True(crossing.CostPaid>=paid);Assert.Equal(0,crossing.BudgetRemaining);
        Assert.Equal(shipments,sim.State.Shipments.Count(s=>s.ProjectId==crossing.Id));
        var entry=sim.State.Roads.Single(node=>node.Highway&&node.Position==sim.State.Entry);
        Assert.All(crossing.RoadPoints,point=>Assert.NotEmpty(sim.FindRoadRoute(entry.Id,sim.State.Roads.Single(node=>node.Position==point).Id)));
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }
}
