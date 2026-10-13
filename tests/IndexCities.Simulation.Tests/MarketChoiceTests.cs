using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;
public sealed class MarketChoiceTests
{
    static CitySimulation Founded()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(7200);return sim;
    }
    static object? Call(CitySimulation sim,string name,params object?[] args)=>typeof(CitySimulation).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(sim,args);
    static int _next=5000;
    static Company Company(CitySimulation sim,decimal cash)
    {
        var company=new Company{Id=_next++,Name="Teste"};sim.State.Companies.Add(company);sim.State.Wallets[company.Account]=cash;sim.State.Wallets["global"]-=cash;return company;
    }
    static Building Owned(CitySimulation sim,BuildingKind kind,double x,Company company)
    {
        var b=new Building{Id=_next++,Kind=kind,Position=new(x,14),Completed=true,OwnerAccount=company.Account,CompanyId=company.Id,OperationalCapacity=1};sim.State.Buildings.Add(b);company.Buildings.Add(b.Id);return b;
    }
    static Citizen Resident(CitySimulation sim,double x)
    {
        var house=new Building{Id=_next++,Kind=BuildingKind.House,Position=new(x,14),Completed=true};sim.State.Buildings.Add(house);
        var family=new Family{Id=_next++,HomeId=house.Id};var c=new Citizen{Id=_next++,Name="Cliente",FamilyId=family.Id,Age=30,Resident=true,InsideMap=true,Position=house.Door};
        family.Members.Add(c.Id);house.FamilyId=family.Id;house.OwnerAccount=c.Account;sim.State.Families.Add(family);sim.State.Citizens.Add(c);sim.State.Wallets[c.Account]=0;return c;
    }
    [Fact] public void Shopper_weighs_shelf_price_against_distance_instead_of_always_using_the_nearest_shop()
    {
        var sim=Founded();var c=Resident(sim,-150);
        var near=Owned(sim,BuildingKind.Grocery,-130,Company(sim,0));var far=Owned(sim,BuildingKind.Grocery,40,Company(sim,0));
        near.AddStock(ResourceKind.Food,50);far.AddStock(ResourceKind.Food,50);
        near.SalePrice=10;far.SalePrice=3;
        Assert.True((bool)Call(sim,"SeekFood",c,null)!);Assert.Equal(far.Id,c.DestinationId);
        c.Path.Clear();near.SalePrice=3;
        Assert.True((bool)Call(sim,"SeekFood",c,null)!);Assert.Equal(near.Id,c.DestinationId);
        sim.CheckInvariants();
    }
    [Fact] public void Farm_reserve_follows_real_orders_not_population()
    {
        var sim=Founded();var farm=Owned(sim,BuildingKind.Farm,-60,Company(sim,0));
        for(var i=0;i<40;i++)Resident(sim,-150);
        Assert.Equal(16,(double)Call(sim,"FarmReserveTarget",farm)!);
        // A competitive shop that finds the farm without stock signals the real order size.
        var shop=Owned(sim,BuildingKind.Grocery,-130,Company(sim,50000));farm.SalePrice=1;
        Call(sim,"RestockBusinesses");
        Assert.True(farm.FoodDemandEstimate>=40,farm.FoodDemandEstimate.ToString());Assert.True((double)Call(sim,"FarmReserveTarget",farm)!>=60);
        // Without dispatched sales the planned reserve decays day after day.
        var before=farm.FoodDemandEstimate;for(var day=0;day<10;day++)Call(sim,"ReviewFarmDemand",farm);
        Assert.True(farm.FoodDemandEstimate<before*.1);Assert.Equal(16,(double)Call(sim,"FarmReserveTarget",farm)!);
        farm.FoodSoldToday=100;Call(sim,"ReviewFarmDemand",farm);Assert.True(farm.FoodDemandEstimate>=30);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }
    [Fact] public void Company_in_risk_shows_cause_and_a_bankrupt_truck_is_not_used_by_the_next_owner()
    {
        var sim=Founded();var bankrupt=Company(sim,10);var farm=Owned(sim,BuildingKind.Farm,-60,bankrupt);
        var truck=new Vehicle{Id=_next++,OwnerAccount=bankrupt.Account,HomeBuildingId=farm.Id,Position=farm.Door,Status=VehicleStatus.Parked,Fuel=20};sim.State.Vehicles.Add(truck);
        sim.State.Obligations.Add(new Obligation{Id=_next++,Debtor=bankrupt.Account,Creditor="city",Outstanding=500,Kind="Imposto",BuildingId=farm.Id,Due=sim.State.Now});
        Call(sim,"ReviewInsolvency");
        Assert.True(bankrupt.Active);Assert.Contains("risco de falência",bankrupt.Decision);Assert.Contains("Imposto",bankrupt.Decision);
        bankrupt.LossStartedAt=sim.State.ElapsedSeconds-15*86400;Call(sim,"ReviewInsolvency");
        Assert.False(bankrupt.Active);Assert.Equal("unclaimed",truck.OwnerAccount);Assert.Null(farm.OwnerAccount);
        var buyer=Company(sim,0);farm.CompanyId=buyer.Id;farm.OwnerAccount=buyer.Account;buyer.Buildings.Add(farm.Id);
        Assert.Null(Call(sim,"LocalDeliveryVehicle",farm));
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);sim.CheckInvariants();
    }
}
