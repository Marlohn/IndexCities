using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class EconomyAndEnvironmentTests
{
    [Fact] public void Annual_tax_is_collected_monthly_from_the_actual_owner_only()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);Assert.All(sim.State.TaxRates,rate=>Assert.Equal(.01m,rate));Assert.False(sim.SetTax(0,.081m).Success);Assert.True(sim.SetTax(0,.08m).Success);
        var owner=new Citizen{Id=1000,Name="Proprietário",Resident=false,InsideMap=false};sim.State.Citizens.Add(owner);sim.State.Wallets[owner.Account]=1000;sim.State.Wallets["global"]-=1000;
        var house=new Building{Id=1001,Kind=BuildingKind.House,Position=new(-220,20),Completed=true,OwnerAccount=owner.Account};var unsold=new Building{Id=1002,Kind=BuildingKind.House,Position=new(-200,20),Completed=true};sim.State.Buildings.AddRange([house,unsold]);
        var due=decimal.Round(sim.MarketValue(house)*.08m/12,2,MidpointRounding.AwayFromZero);var cityBefore=sim.CityBalance;sim.State.Now=sim.State.NextTaxDate;sim.State.NextDaily=sim.State.Now.Date.AddDays(1);sim.State.EconomyNextAt=10000;sim.Advance(2);
        var payment=Assert.Single(sim.State.Ledger,l=>l.Category=="Imposto imobiliário");Assert.Equal(owner.Account,payment.From);Assert.Equal(house.Id,payment.EntityId);Assert.Equal(due,payment.Amount);Assert.Equal(cityBefore+due,sim.CityBalance);sim.CheckInvariants();
    }
    [Fact] public void Local_vehicle_without_a_real_driver_preserves_position_fuel_and_cargo()
    {
        var sim=new CitySimulation(42);var vehicle=new Vehicle{Id=1000,External=false,Position=sim.State.Entry,Fuel=10,Status=VehicleStatus.Travelling,Route=sim.State.Roads.Select(r=>r.Id).ToList(),Cargo=new(){{ResourceKind.Food,5}}};sim.State.Vehicles.Add(vehicle);var before=vehicle.Position;sim.Advance(60);
        Assert.Equal(before,vehicle.Position);Assert.Equal(10,vehicle.Fuel);Assert.Equal(5,vehicle.Cargo[ResourceKind.Food]);Assert.Contains("motorista",vehicle.Cause);sim.CheckInvariants();
    }
    [Fact] public void Traffic_pollution_is_local_persists_on_save_and_decays_after_traffic_stops()
    {
        var sim=new CitySimulation(42);var truck=sim.State.Vehicles.First();truck.Fuel=20;truck.Status=VehicleStatus.Travelling;truck.Route=sim.State.Roads.Select(r=>r.Id).ToList();sim.Advance(3);Assert.NotEmpty(sim.State.PollutionCells);var near=sim.LocalPollution(truck.Position);Assert.True(near>sim.LocalPollution(sim.State.LakeCentre));
        truck.Status=VehicleStatus.Parked;var restored=CitySimulation.FromJson(sim.SaveToJson());Assert.Equal(sim.SaveToJson(),restored.SaveToJson());var before=sim.LocalPollution(truck.Position);sim.Advance(3600);restored.Advance(3600);var after=sim.LocalPollution(truck.Position);Assert.True(after>0&&after<before);Assert.Equal(sim.SaveToJson(),restored.SaveToJson());
    }
}
