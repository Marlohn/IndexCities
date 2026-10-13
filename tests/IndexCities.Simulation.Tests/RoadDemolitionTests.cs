using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class RoadDemolitionTests
{
    static CitySimulation ConnectedCity()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(-80,0)).Success);sim.Advance(7200);Assert.True(sim.State.Projects.All(p=>p.Status==WorkStatus.Completed));return sim;
    }
    [Fact] public void Preview_is_read_only_and_paid_cut_preserves_people_and_cargo_until_real_reconnection()
    {
        var sim=ConnectedCity();var houseResult=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(houseResult.Success);var house=sim.State.Buildings.First(b=>b.Id==houseResult.Id);var project=sim.State.Projects.First(p=>p.BuildingId==house.Id);
        for(var i=0;i<1000&&!sim.State.Shipments.Any(s=>s.ProjectId==project.Id);i++)sim.Advance(2);
        Assert.Contains(sim.State.Shipments,s=>s.ProjectId==project.Id&&!s.Complete);var node=sim.State.Roads.Single(r=>r.Position.Distance(new(-132,0))<.1);var before=sim.SaveToJson();var quote=sim.QuoteRoadDemolition(node.Id);Assert.Equal(before,sim.SaveToJson());Assert.True(quote.Allowed);Assert.Contains(house.Id,quote.IsolatedBuildingIds);Assert.True(quote.InterruptedWorks>0);Assert.True(quote.AffectedVehicleRoutes>0);
        var city=sim.CityBalance;var global=sim.Balance("global");var physical=sim.State.Vehicles.ToDictionary(v=>v.Id,v=>(v.Position,v.Fuel,string.Join(";",v.Cargo.OrderBy(k=>k.Key))));var people=sim.State.Citizens.ToDictionary(c=>c.Id,c=>c.Position);
        Assert.True(sim.DemolishRoad(node.Id).Success);Assert.Equal(city-quote.Cost,sim.CityBalance);Assert.Equal(global+quote.Cost,sim.Balance("global"));Assert.False(sim.HasExteriorAccess(house));Assert.DoesNotContain(sim.State.Roads,r=>r.Id==node.Id);Assert.All(sim.State.Vehicles,v=>Assert.Equal(physical[v.Id],(v.Position,v.Fuel,string.Join(";",v.Cargo.OrderBy(k=>k.Key)))));Assert.All(sim.State.Citizens,c=>Assert.Equal(people[c.Id],c.Position));sim.CheckInvariants();
        sim.Advance(60);Assert.Equal(0,project.Progress);Assert.True(sim.PlaceRoad(new(-132,0),new(-132,0)).Success);sim.Advance(7200);Assert.True(sim.HasExteriorAccess(house));Assert.True(house.Completed,string.Join(" | ",sim.State.Projects.Select(p=>p.Cause)));sim.CheckInvariants();
    }
    [Fact] public void Insufficient_free_cash_rejects_removal_without_spending_reserved_construction_money()
    {
        var sim=ConnectedCity();var node=sim.State.Roads.First(r=>!r.Highway);var quote=sim.QuoteRoadDemolition(node.Id);sim.State.Wallets["global"]+=sim.CityBalance;sim.State.Wallets["city"]=0;var before=sim.SaveToJson();Assert.False(sim.DemolishRoad(node.Id).Success);Assert.Equal(before,sim.SaveToJson());Assert.False(sim.QuoteRoadDemolition(sim.State.Roads.First(r=>r.Highway).Id).Allowed);Assert.True(quote.Cost>0);sim.CheckInvariants();
    }
}
