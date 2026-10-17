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
        car.Position=sim.ParkingSpots(home).Single(s=>s.Key==car.ParkingSpot).Position;family.LastMigrationReview=-1e9;
        // Just short of the direct exit: the car must refuel before leaving.
        T Call<T>(string name,params object?[] a)=>(T)typeof(CitySimulation).GetMethods(BindingFlags.Instance|BindingFlags.NonPublic).First(m=>m.Name==name&&m.GetParameters().Length==a.Length).Invoke(sim,a)!;
        var exit=sim.FindRoadRoute(Call<RoadNode>("NearestRoad",car.Position,false).Id,Call<RoadNode>("EntryNode").Id);car.Fuel=Call<double>("RouteLength",exit)*sim.State.Tuning.CarFuelPerMetre+1-.2;
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
    [Fact] public void A_family_without_a_car_still_walks_out()
    {
        var (sim,family,car,_)=Desperate();car.Status=VehicleStatus.Outside;Review(sim);
        Assert.True(family.Emigrating);Assert.All(sim.State.Citizens.Where(c=>family.Members.Contains(c.Id)),c=>{Assert.True(c.Moving);Assert.Equal(JourneyPurpose.Leaving,c.Purpose);});
        sim.CheckInvariants();
    }
    [Fact] public void If_the_station_runs_dry_before_arrival_the_family_parks_there_and_walks_home_keeping_its_house()
    {
        var (sim,family,car,station)=Desperate();Move(sim,"global",car.OwnerAccount,14);var home=family.HomeId;Review(sim);
        Assert.True(family.Emigrating);Assert.Equal(home,family.HomeId);
        station.AddStock(ResourceKind.Fuel,-station.Quantity(ResourceKind.Fuel));
        for(var i=0;i<600&&car.Status!=VehicleStatus.Parked;i++)sim.Advance(2);
        var info=$"car={car.Status} {car.Cause} fuel={car.Fuel:F2} refAt={car.RefuellingAt} emig={family.Emigrating} home={family.HomeId} stock={station.Quantity(ResourceKind.Fuel):F1} purposes={string.Join(",",sim.State.Citizens.Where(c=>family.Members.Contains(c.Id)).Select(c=>c.Purpose+"/"+c.Activity))}";
        Assert.True(car.Status==VehicleStatus.Parked,info);Assert.False(family.Emigrating,info);Assert.Equal(home,family.HomeId);
        Assert.Equal(family.Id,sim.Buildings.Single(b=>b.Id==home).FamilyId);Assert.Null(car.ParkingSpot);
        Assert.All(sim.State.Citizens.Where(c=>family.Members.Contains(c.Id)),c=>{Assert.Null(c.VehicleId);Assert.Equal(JourneyPurpose.Home,c.Purpose);});
        Assert.All(sim.State.Vehicles.Where(v=>!v.PassengerCar),v=>Assert.DoesNotContain("Fila para acesso de carga",v.Cause));
        sim.CheckInvariants();
    }
    [Fact] public void A_family_that_can_pay_refuels_on_the_way_out()
    {
        var (sim,family,car,station)=Desperate();Move(sim,"global",car.OwnerAccount,14);Review(sim);
        var activity=sim.State.Citizens.First(c=>family.Members.Contains(c.Id)).Activity;Assert.True(family.Emigrating,activity+" fuel="+car.Fuel+" balance="+sim.Balance(car.OwnerAccount));Assert.Equal(VehicleStatus.Travelling,car.Status);Assert.Equal(station.Id,car.RefuellingAt);Assert.Equal(JourneyPurpose.Leaving,car.TripPurpose);
        sim.CheckInvariants();
    }
}
