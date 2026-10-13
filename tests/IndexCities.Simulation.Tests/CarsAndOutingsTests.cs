using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;
public sealed class CarsAndOutingsTests
{
    static CitySimulation Founded()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(7200);return sim;
    }
    [Fact] public void Migrant_family_with_capital_buys_an_imported_car_and_parks_it_in_the_house_lot()
    {
        var sim=Founded();foreach(var c in sim.State.FamilyCandidates)c.Capital=150000;
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(placed.Success);var house=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        var total=sim.MonetaryTotal;var outsideFuel=sim.State.OutsideStock[ResourceKind.Fuel];
        for(var i=0;i<3600&&house.FamilyId==null;i++)sim.Advance(2);
        var car=Assert.Single(sim.State.Vehicles,v=>v.PassengerCar);
        Assert.Equal(total,sim.MonetaryTotal);Assert.True(outsideFuel>sim.State.OutsideStock[ResourceKind.Fuel]);
        Assert.Contains(sim.State.Ledger,l=>l.Category=="Veículos"&&l.To=="global"&&l.From==car.OwnerAccount);
        for(var i=0;i<1800&&car.Status!=VehicleStatus.Parked;i++)sim.Advance(2);
        Assert.Equal(VehicleStatus.Parked,car.Status);
        var spot=sim.ParkingSpots(house).Single();Assert.Equal(spot.Key,car.ParkingSpot);Assert.True(car.Position.Distance(spot.Position)<.01);
        for(var i=0;i<600&&sim.State.Citizens.Any(c=>c.Moving);i++)sim.Advance(2);
        Assert.All(sim.State.Citizens,c=>{Assert.Null(c.VehicleId);Assert.True(c.Position.Distance(house.Door)<3);});
        sim.CheckInvariants();
    }
    [Fact] public void Family_without_enough_money_arrives_without_a_car()
    {
        var sim=Founded();foreach(var c in sim.State.FamilyCandidates)c.Capital=46000;
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-100,14));var house=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        for(var i=0;i<3600&&house.FamilyId==null;i++)sim.Advance(2);
        Assert.NotNull(house.FamilyId);Assert.DoesNotContain(sim.State.Vehicles,v=>v.PassengerCar);
    }
    [Fact] public void Driving_a_private_car_burns_fuel_but_is_not_paid_work()
    {
        var sim=Founded();foreach(var c in sim.State.FamilyCandidates)c.Capital=150000;
        var placed=sim.PlaceBuilding(BuildingKind.House,new(60,14));var house=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        for(var i=0;i<3600&&house.FamilyId==null;i++)sim.Advance(2);
        var car=sim.State.Vehicles.Single(v=>v.PassengerCar);var fuel=car.Fuel;var driver=sim.State.Citizens.First(c=>c.Id==car.DriverId);var worked=driver.WorkSecondsToday;
        for(var i=0;i<1800&&car.Status!=VehicleStatus.Parked;i++)sim.Advance(2);
        Assert.True(car.Fuel<fuel);Assert.Equal(worked,driver.WorkSecondsToday);sim.CheckInvariants();
    }
    [Fact] public void Only_one_city_hall_can_be_ordered_until_it_is_cancelled()
    {
        var sim=new CitySimulation(11);var first=sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40));Assert.True(first.Success);
        var quote=sim.QuoteBuilding(BuildingKind.CityHall,new(-40,40));Assert.False(quote.PhysicallyValid);Assert.Contains("Prefeitura",quote.Message);
        Assert.False(sim.PlaceBuilding(BuildingKind.CityHall,new(-40,40)).Success);
        Assert.True(sim.CancelProject(sim.State.Projects.Single().Id).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-40,40)).Success);
    }
    [Fact] public void Free_residents_make_physical_visits_and_walks_without_moving_money()
    {
        var sim=Founded();
        for(var x=-120.0;x<=60;x+=13)sim.PlaceBuilding(BuildingKind.House,new(x,14));
        sim.Advance(12*3600);var ledger=sim.State.Ledger.Count;var total=sim.MonetaryTotal;
        Assert.True(sim.State.Citizens.Count>=4);
        sim.Advance(48*3600);
        var history=sim.State.Citizens.SelectMany(c=>c.History).Select(h=>h.Description).ToList();
        Assert.Contains(history,h=>h.StartsWith("Indo visitar outra família",StringComparison.Ordinal)||h.StartsWith("Passeando a pé",StringComparison.Ordinal));
        Assert.Equal(total,sim.MonetaryTotal);sim.CheckInvariants();
    }
}
