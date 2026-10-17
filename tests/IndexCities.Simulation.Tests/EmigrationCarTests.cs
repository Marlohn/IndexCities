using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC populacao: a family with a car emigrates with it, never on foot leaving it
// behind; it refuels first when it can pay, otherwise it waits.
public sealed class EmigrationCarTests
{
    static (CitySimulation Sim,Family Family,Vehicle Car,Building Station) Desperate()
    {
        var sim=CityCycleTests.City();sim.Advance(6*3600);
        var car=sim.State.Vehicles.First(v=>v.PassengerCar&&v.Status==VehicleStatus.Parked&&v.DriverId==null);
        var family=sim.State.Families.Single(f=>f.Members.Any(id=>car.OwnerAccount=="sim:"+id));var home=sim.Buildings.Single(b=>b.Id==family.HomeId);
        foreach(var c in sim.State.Citizens.Where(c=>family.Members.Contains(c.Id))){c.HungerHours=60;c.WorkplaceId=null;c.Path.Clear();c.PathIndex=0;c.VehicleId=null;c.Position=home.Door;var excess=Math.Max(0,sim.State.Wallets[c.Account]-5);Move(sim,c.Account,"global",excess);}
        car.Position=sim.ParkingSpots(home).Single(s=>s.Key==car.ParkingSpot).Position;car.Fuel=.3;family.LastMigrationReview=-1e9;
        var station=sim.Buildings.Single(b=>b.Kind==BuildingKind.FuelStation);station.AddStock(ResourceKind.Fuel,100);station.OperationalCapacity=1;
        return (sim,family,car,station);
    }
    static void Review(CitySimulation sim)=>typeof(CitySimulation).GetMethod("ReviewEmigration",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(sim,null);
    static void Move(CitySimulation sim,string from,string to,decimal amount){sim.State.Wallets[from]-=amount;sim.State.Wallets[to]+=amount;}
    [Fact] public void A_family_that_cannot_pay_for_exit_fuel_waits_with_its_car_instead_of_walking_out()
    {
        var (sim,family,car,_)=Desperate();Review(sim);
        Assert.False(family.Emigrating);Assert.NotNull(family.HomeId);Assert.Equal(VehicleStatus.Parked,car.Status);
        Assert.All(sim.State.Citizens.Where(c=>family.Members.Contains(c.Id)),c=>{Assert.False(c.Moving);Assert.Equal("Família aguardando dinheiro para o combustível da saída",c.Activity);});
        sim.CheckInvariants();
    }
    [Fact] public void A_family_that_can_pay_refuels_on_the_way_out()
    {
        var (sim,family,car,station)=Desperate();Move(sim,"global",car.OwnerAccount,14);Review(sim);
        var activity=sim.State.Citizens.First(c=>family.Members.Contains(c.Id)).Activity;Assert.True(family.Emigrating,activity+" fuel="+car.Fuel+" balance="+sim.Balance(car.OwnerAccount));Assert.Equal(VehicleStatus.Travelling,car.Status);Assert.Equal(station.Id,car.RefuellingAt);Assert.Equal(JourneyPurpose.Leaving,car.TripPurpose);
        sim.CheckInvariants();
    }
}
