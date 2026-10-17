using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class FuelDeliveryTests
{
    [Fact] public void Loaded_local_truck_really_refuels_at_the_selected_station_before_delivering_food()
    {
        var sim=CityCycleTests.City();Vehicle? truck=null;Building? farm=null,shop=null,station=null;
        for(var step=0;step<336&&truck==null;step++){
            sim.Advance(1800);farm=sim.State.Buildings.First(b=>b.Kind==BuildingKind.Farm);shop=sim.State.Buildings.First(b=>b.Kind==BuildingKind.Grocery);station=sim.State.Buildings.First(b=>b.Kind==BuildingKind.FuelStation);
            var candidate=sim.State.Vehicles.FirstOrDefault(v=>!v.External&&v.HomeBuildingId==farm.Id&&v.Status==VehicleStatus.Parked&&v.DriverId==null&&v.Cargo.Values.Sum()<.001);
            var driver=sim.State.Citizens.Any(c=>c.Alive&&c.WorkplaceId==farm.Id&&c.VehicleId==null&&c.ConstructionProjectId==null&&!c.Moving&&c.Position.Distance(farm.Door)<3);
            if(candidate!=null&&driver&&farm.OperationalCapacity>0&&farm.Quantity(ResourceKind.Food)>=10&&station.OperationalCapacity>0&&station.Quantity(ResourceKind.Fuel)>20)truck=candidate;
        }
        Assert.NotNull(truck);Assert.NotNull(farm);Assert.NotNull(shop);Assert.NotNull(station);truck!.Fuel=2;shop!.AddStock(ResourceKind.Food,-10);
        var method=typeof(CitySimulation).GetMethod("OrderLocalFood",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;Assert.True((bool)method.Invoke(sim,[farm!,shop,10d])!);var shipment=sim.State.Shipments.Last(s=>s.VehicleId==truck.Id&&!s.Complete);var stationId=station!.Id;Assert.Equal(stationId,truck.RefuellingAt);var refuelled=false;
        for(var step=0;step<3600&&!shipment.Complete;step++){sim.Advance(2);refuelled|=truck.Fuel>10;sim.CheckInvariants();}
        Assert.True(refuelled,$"{truck.Cause}, {truck.Fuel} L, {truck.Position}");Assert.True(shipment.Complete,truck.Cause);Assert.True(shipment.PickedUp);Assert.Equal(0,truck.Cargo.Values.Sum());Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);
    }
    // SPEC transporte-circulacao: the way back to the farm is a departure too.
    [Fact] public void Local_truck_refuels_before_returning_when_the_tank_does_not_cover_the_way_back()
    {
        var sim=CityCycleTests.City();Vehicle? truck=null;Building? farm=null,shop=null,station=null;
        for(var step=0;step<336&&truck==null;step++){
            sim.Advance(1800);farm=sim.State.Buildings.First(b=>b.Kind==BuildingKind.Farm);shop=sim.State.Buildings.First(b=>b.Kind==BuildingKind.Grocery);station=sim.State.Buildings.First(b=>b.Kind==BuildingKind.FuelStation);
            var candidate=sim.State.Vehicles.FirstOrDefault(v=>!v.External&&v.HomeBuildingId==farm.Id&&v.Status==VehicleStatus.Parked&&v.DriverId==null&&v.Cargo.Values.Sum()<.001);
            var driver=sim.State.Citizens.Any(c=>c.Alive&&c.WorkplaceId==farm.Id&&c.VehicleId==null&&c.ConstructionProjectId==null&&!c.Moving&&c.Position.Distance(farm.Door)<3);
            if(candidate!=null&&driver&&farm.OperationalCapacity>0&&farm.Quantity(ResourceKind.Food)>=10&&station.OperationalCapacity>0&&station.Quantity(ResourceKind.Fuel)>20)truck=candidate;
        }
        Assert.NotNull(truck);truck!.Fuel=sim.State.Tuning.FuelTank;shop!.AddStock(ResourceKind.Food,-10);
        var method=typeof(CitySimulation).GetMethod("OrderLocalFood",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;Assert.True((bool)method.Invoke(sim,[farm!,shop,10d])!);
        var shipment=sim.State.Shipments.Last(s=>s.VehicleId==truck.Id&&!s.Complete);Assert.Null(truck.RefuellingAt);
        // Just enough for the delivery leg and its margin: the same way back is not covered.
        var roads=sim.State.Roads.ToDictionary(r=>r.Id);var leg=truck.Route.Zip(truck.Route.Skip(1),(a,b)=>roads[a].Position.Distance(roads[b].Position)).Sum();
        truck.Fuel=leg*sim.State.Tuning.FuelPerMetre+4.05;
        for(var step=0;step<3600&&!shipment.Complete;step++){sim.Advance(2);Assert.NotEqual(VehicleStatus.Stranded,truck.Status);}
        Assert.True(shipment.Complete,truck.Cause);Assert.Equal(station!.Id,truck.RefuellingAt);var refuelled=false;
        for(var step=0;step<3600&&truck.Status!=VehicleStatus.Parked;step++){sim.Advance(2);refuelled|=truck.Fuel>10;Assert.NotEqual(VehicleStatus.Stranded,truck.Status);sim.CheckInvariants();}
        Assert.True(refuelled,$"{truck.Cause}, {truck.Fuel} L");Assert.Equal(VehicleStatus.Parked,truck.Status);Assert.True(truck.Position.Distance(farm!.Door)<30,truck.Position+" "+farm.Door);Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);
    }
}
