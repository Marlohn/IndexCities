using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;
public sealed class IntegratedCityTests
{
    static CitySimulation Founded(int seed=42)
    {
        var sim=new CitySimulation(seed);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(7200);return sim;
    }
    [Fact] public void Seed_reproduces_map_and_city_starts_without_buildings()
    {
        var a=new CitySimulation(99);var b=new CitySimulation(99);
        Assert.Empty(a.State.Buildings);Assert.Empty(a.State.Citizens);Assert.False(a.State.CityHallPlaced);
        Assert.Equal(a.State.LakeCentre,b.State.LakeCentre);Assert.Equal(a.State.LakeRadius,b.State.LakeRadius);
        Assert.Equal(a.MonetaryTotal,a.State.Tuning.TotalMoney);
    }
    [Fact] public void City_hall_confirmation_unlocks_tools_without_finishing_or_fake_delivery()
    {
        var sim=new CitySimulation(11);var result=sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40));Assert.True(result.Success);
        Assert.True(sim.State.CityHallPlaced);Assert.False(sim.State.Buildings.Single().Completed);
        var balance=sim.CityBalance;var total=sim.MonetaryTotal;sim.Advance(3600);
        Assert.Equal(balance,sim.CityBalance);Assert.Equal(total,sim.MonetaryTotal);
        Assert.Equal(0,sim.State.Projects.Single().Progress);Assert.Contains("acesso",sim.State.Projects.Single().Cause,StringComparison.OrdinalIgnoreCase);
        Assert.True(sim.PlaceBuilding(BuildingKind.House,new(-80,40)).Success);
    }
    [Fact] public void Cancel_before_payment_releases_commitment_without_creating_money()
    {
        var sim=new CitySimulation(11);sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40));var project=sim.State.Projects.Single();var balance=sim.CityBalance;
        Assert.True(sim.Committed>0);Assert.True(sim.CancelProject(project.Id).Success);
        Assert.Equal(0,sim.Committed);Assert.Equal(balance,sim.CityBalance);sim.CheckInvariants();
    }
    [Fact] public void Physical_road_and_city_hall_build_with_paid_imports_and_arrived_crews()
    {
        var sim=Founded();sim.CheckInvariants();
        Assert.Contains(sim.State.Roads,n=>!n.Highway);
        Assert.True(sim.State.Buildings.Single().Completed,string.Join(" | ",sim.State.Projects.Select(p=>$"{p.Id}: {p.Cause}; progress={p.Progress}; budget={p.BudgetRemaining}; delivered={string.Join(",",p.Delivered.Select(k=>$"{k.Key}:{k.Value}"))}")));
        Assert.Contains(sim.State.Ledger,l=>l.From=="city"&&l.To=="global"&&l.Category=="Obras e materiais");
        Assert.All(sim.State.Projects.Where(p=>p.Status==WorkStatus.Completed),p=>Assert.All(p.Required,kv=>Assert.Equal(kv.Value,p.Delivered[kv.Key],4)));
    }
    [Fact] public void Save_load_preserves_money_and_physical_pending_work()
    {
        var sim=Founded();sim.PlaceBuilding(BuildingKind.House,new(-120,14));sim.Advance(1800);
        var restored=CitySimulation.FromJson(sim.SaveToJson());
        Assert.Equal(sim.MonetaryTotal,restored.MonetaryTotal);Assert.Equal(sim.Committed,restored.Committed);Assert.Equal(sim.State.Now,restored.State.Now);
        Assert.Equal(sim.State.RandomState,restored.State.RandomState);Assert.Equal(sim.State.RoadRevision,restored.State.RoadRevision);
        sim.Advance(1800);restored.Advance(1800);
        Assert.Equal(sim.SaveToJson(),restored.SaveToJson());
    }
    [Fact] public void Loan_is_manual_transfers_existing_money_and_rejects_overdue_debt()
    {
        var sim=new CitySimulation(2);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);var before=sim.CityBalance;Assert.True(sim.Borrow(10000).Success);
        Assert.Equal(before+10000,sim.CityBalance);sim.CheckInvariants();
        sim.State.Obligations.Add(new(){Id=99,Debtor="city",Creditor="global",Kind="Empréstimo",Outstanding=50,Due=sim.State.Now.AddDays(-1)});
        Assert.False(sim.Borrow(1000).Success);
    }
}
