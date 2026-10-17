using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class BuildingDemolitionQuoteTests
{
    // Fixture sintética: injeta prédios, famílias e cidadãos direto no estado para
    // exercitar o orçamento somente-leitura. Não é uma partida completa: pula
    // ReviewMarkets, obras físicas e chegada real de moradores.
    static CitySimulation City()=>new(7);
    static Building Add(CitySimulation sim,BuildingKind kind,bool completed=true,string? owner=null)
    {
        var building=new Building{Id=sim.State.NextId++,Kind=kind,Position=new(220,220),Completed=completed,OwnerAccount=owner};
        sim.State.Buildings.Add(building);return building;
    }
    static Citizen AddCitizen(CitySimulation sim,int id,string name,int familyId,bool resident)
    {
        var citizen=new Citizen{Id=id,Name=name,FamilyId=familyId,Resident=resident,Age=30,Alive=true};
        sim.State.Citizens.Add(citizen);return citizen;
    }
    static void Home(CitySimulation sim,int familyId,int houseId,IEnumerable<int> members)
    {
        var family=new Family{Id=familyId,HomeId=houseId};family.Members.AddRange(members);sim.State.Families.Add(family);
    }
    [Fact] public void Private_building_without_owner_has_no_indemnity_to_the_reserve()
    {
        var sim=City();var house=Add(sim,BuildingKind.House);var before=sim.SaveToJson();var reserve=sim.Balance("global");var quote=sim.QuoteBuildingDemolition(house.Id);
        Assert.True(quote.Allowed);Assert.Equal(0m,quote.Compensation);Assert.True(quote.RemovalCost>0);Assert.Equal(quote.RemovalCost,quote.TotalCost);
        Assert.Null(quote.OwnerAccount);Assert.Equal("",quote.OwnerName);Assert.Empty(quote.ResidentIds);Assert.Empty(quote.WorkerIds);Assert.False(quote.WaitsForResidents);
        Assert.Contains("sem indenização",quote.Message);Assert.Equal(before,sim.SaveToJson());Assert.Equal(reserve,sim.Balance("global"));
    }
    [Fact] public void Private_house_with_SIM_owner_pays_full_market_value_to_the_owner()
    {
        var sim=City();var house=Add(sim,BuildingKind.House,owner:"sim:900");AddCitizen(sim,900,"Ana 9",5,true);Home(sim,5,house.Id,new[]{900});
        house.Stock[ResourceKind.Wood]=4;house.Stock[ResourceKind.Steel]=0;var market=sim.MarketValue(house);
        var quote=sim.QuoteBuildingDemolition(house.Id);
        Assert.True(quote.Allowed);Assert.Equal(market,quote.Compensation);Assert.Equal(quote.RemovalCost+market,quote.TotalCost);
        Assert.Equal("sim:900",quote.OwnerAccount);Assert.Equal("Ana 9",quote.OwnerName);Assert.Equal(new List<int>{900},quote.ResidentIds);Assert.True(quote.WaitsForResidents);
        Assert.True(quote.Stock.ContainsKey(ResourceKind.Wood));Assert.False(quote.Stock.ContainsKey(ResourceKind.Steel));Assert.Empty(quote.WorkerIds);
    }
    [Fact] public void Private_business_with_company_owner_indemnifies_the_company()
    {
        var sim=City();var shop=Add(sim,BuildingKind.Grocery,owner:"company:3");sim.State.Companies.Add(new Company{Id=3,Name="Mercado 3"});
        AddCitizen(sim,901,"Bruno 9",6,true).WorkplaceId=shop.Id;var market=sim.MarketValue(shop);var quote=sim.QuoteBuildingDemolition(shop.Id);
        Assert.True(quote.Allowed);Assert.Equal(market,quote.Compensation);Assert.Equal(quote.RemovalCost+market,quote.TotalCost);
        Assert.Equal("Mercado 3",quote.OwnerName);Assert.Equal(new List<int>{901},quote.WorkerIds);Assert.Empty(quote.ResidentIds);
    }
    [Fact] public void Municipal_buildings_are_removed_without_indemnity()
    {
        var sim=City();var depot=Add(sim,BuildingKind.WorksDepot);var quote=sim.QuoteBuildingDemolition(depot.Id);
        Assert.True(quote.Allowed);Assert.Equal(0m,quote.Compensation);Assert.Equal(quote.RemovalCost,quote.TotalCost);Assert.Contains("municipal",quote.Message);
    }
    [Fact] public void Completed_city_hall_only_allows_moving_the_seat()
    {
        var sim=City();var hall=Add(sim,BuildingKind.CityHall);var quote=sim.QuoteBuildingDemolition(hall.Id);
        Assert.False(quote.Allowed);Assert.Contains("Mover",quote.Message);Assert.Equal(0m,quote.TotalCost);
    }
    [Fact] public void Removed_incomplete_and_unknown_buildings_are_rejected()
    {
        var sim=City();var removed=Add(sim,BuildingKind.House);removed.Removed=true;var incomplete=Add(sim,BuildingKind.House,completed:false);
        Assert.False(sim.QuoteBuildingDemolition(removed.Id).Allowed);Assert.False(sim.QuoteBuildingDemolition(incomplete.Id).Allowed);Assert.False(sim.QuoteBuildingDemolition(999999).Allowed);
    }
    [Fact] public void Insufficient_free_cash_rejects_without_touching_balances()
    {
        var sim=City();var house=Add(sim,BuildingKind.House,owner:"sim:902");AddCitizen(sim,902,"Clara 9",7,true);Home(sim,7,house.Id,new[]{902});
        sim.State.Wallets["global"]+=sim.CityBalance;sim.State.Wallets["city"]=0;var before=sim.SaveToJson();var quote=sim.QuoteBuildingDemolition(house.Id);
        Assert.False(quote.Allowed);Assert.True(quote.TotalCost>0);Assert.Contains("insuficiente",quote.Message);Assert.Equal(before,sim.SaveToJson());
    }
    [Fact] public void Quote_is_read_only_for_save_stock_and_wallets()
    {
        var sim=City();var house=Add(sim,BuildingKind.House,owner:"sim:903");AddCitizen(sim,903,"Daniel 9",8,true);Home(sim,8,house.Id,new[]{903});house.Stock[ResourceKind.Wood]=3;
        var before=sim.SaveToJson();var wallets=sim.State.Wallets.ToDictionary(entry=>entry.Key,entry=>entry.Value);var balance=sim.CityBalance;
        var quote=sim.QuoteBuildingDemolition(house.Id);
        Assert.True(quote.Allowed);Assert.Equal(before,sim.SaveToJson());Assert.Equal(wallets.Count,sim.State.Wallets.Count);Assert.All(wallets,entry=>Assert.Equal(entry.Value,sim.State.Wallets[entry.Key]));Assert.Equal(balance,sim.CityBalance);
    }
}
