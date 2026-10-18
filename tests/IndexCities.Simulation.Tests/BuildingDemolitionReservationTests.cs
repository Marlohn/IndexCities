using System.Reflection;
using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

public sealed class BuildingDemolitionReservationTests
{
    static readonly MethodInfo Order=typeof(CitySimulation).GetMethod("OrderLocalFood",BindingFlags.NonPublic|BindingFlags.Instance)!;
    static BuildingDemolitionSafeBayTests.Fixture PaidPurchase()
    {
        var f=BuildingDemolitionSafeBayTests.LoadedLocalTruck(false);
        // Exactly the twenty sold units remain physically at the farm.
        f.Farm.Stock[ResourceKind.Food]=20;
        Assert.True(f.Shipment.PricePaid>0);Assert.False(f.Shipment.PickedUp);
        return f;
    }
    static double Free(CitySimulation sim,Building farm)=>farm.Quantity(ResourceKind.Food)-sim.State.Shipments.Where(s=>s.SourceBuildingId==farm.Id&&!s.Complete&&!s.PickedUp).Sum(s=>s.Cargo.GetValueOrDefault(ResourceKind.Food));

    [Fact] public void Paid_goods_remain_reserved_at_the_intact_farm_without_a_truck_or_resale_and_survive_save_load()
    {
        var f=PaidPurchase();var seller=f.Sim.Balance(f.FarmCo.Account);var buyer=f.Sim.Balance(f.ShopCo.Account);var total=f.Sim.MonetaryTotal;
        var fuel=f.Truck.Fuel;var pose=f.Truck.Position;var quote=f.Sim.QuoteBuildingDemolition(f.Target.Id);
        Assert.True(f.Sim.DemolishBuilding(f.Target.Id,quote).Success);
        Assert.False(f.Shipment.Complete);Assert.False(f.Shipment.Cancelled);Assert.False(f.Shipment.PickedUp);
        Assert.Equal(0,Free(f.Sim,f.Farm),6);Assert.Equal(20,f.Farm.Quantity(ResourceKind.Food),6);
        Assert.Null(f.Truck.ShipmentId);Assert.Null(f.Truck.DriverId);Assert.Null(f.Worker.BoardingVehicleId);Assert.Null(f.Worker.VehicleId);
        Assert.Equal(VehicleStatus.Parked,f.Truck.Status);Assert.Empty(f.Truck.Cargo);Assert.Equal(pose,f.Truck.Position);Assert.Equal(fuel,f.Truck.Fuel);
        // Even a direct second order cannot sell the same physical units again.
        f.Worker.Path.Clear();f.Worker.PathIndex=0;f.Worker.Position=f.Farm.Door;
        Assert.False((bool)Order.Invoke(f.Sim,[f.Farm,f.Sister,20d])!);
        Assert.Equal(seller,f.Sim.Balance(f.FarmCo.Account));Assert.Equal(buyer+quote.Compensation,f.Sim.Balance(f.ShopCo.Account));Assert.Equal(total,f.Sim.MonetaryTotal);
        var loaded=CitySimulation.FromJson(f.Sim.SaveToJson());var farm=loaded.State.Buildings.Single(b=>b.Id==f.Farm.Id);var shipment=loaded.State.Shipments.Single(s=>s.Id==f.Shipment.Id);
        Assert.Equal(0,Free(loaded,farm),6);Assert.Equal(f.ShopCo.Account,shipment.GoodsOwnerAccount);Assert.Equal(20,shipment.Cargo[ResourceKind.Food]);Assert.False(shipment.Complete);
        loaded.CheckInvariants();
    }

    [Fact] public void The_same_paid_purchase_resumes_with_real_boarding_loading_and_delivery_after_save_load_without_a_second_charge()
    {
        var f=PaidPurchase();Assert.True(f.Sim.DemolishBuilding(f.Target.Id).Success);
        var sim=CitySimulation.FromJson(f.Sim.SaveToJson());
        var truck=sim.State.Vehicles.Single(v=>v.Id==f.Truck.Id);var s=sim.State.Shipments.Single(s=>s.Id==f.Shipment.Id);
        var farm=sim.State.Buildings.Single(b=>b.Id==f.Farm.Id);var sister=sim.State.Buildings.Single(b=>b.Id==f.Sister.Id);var worker=sim.State.Citizens.Single(c=>c.Id==f.Worker.Id);
        sister.Stock[ResourceKind.Food]=0;worker.Position=farm.Door;worker.Path.Clear();worker.PathIndex=0;
        var buyer=sim.Balance(f.ShopCo.Account);var seller=sim.Balance(f.FarmCo.Account);var paid=s.PricePaid;var fuel=truck.Fuel;var total=sim.MonetaryTotal;
        sim.State.EconomyNextAt=sim.State.ElapsedSeconds;
        sim.Advance(.1);
        Assert.False(s.PickedUp);Assert.NotNull(worker.BoardingVehicleId);Assert.Equal(20,farm.Quantity(ResourceKind.Food),6);Assert.Empty(truck.Cargo);
        var boarded=false;var loaded=false;
        for(var i=0;i<18000&&!s.Complete;i++){
            var at=truck.Position;var aboard=worker.VehicleId==truck.Id&&truck.DriverId==worker.Id;
            sim.Advance(.1);if(at.Distance(truck.Position)>.000001)Assert.True(aboard,"Movement without a real SIM aboard");
            boarded|=worker.VehicleId==truck.Id;loaded|=s.PickedUp;
        }
        Assert.True(boarded);Assert.True(loaded);Assert.True(s.Complete,truck.Cause);Assert.Equal(sister.Id,s.DestinationBuildingId);
        Assert.Equal(20,sister.Quantity(ResourceKind.Food),6);Assert.Equal(0,farm.Quantity(ResourceKind.Food),6);Assert.Empty(truck.Cargo);
        Assert.Equal(paid,s.PricePaid);Assert.Equal(buyer,sim.Balance(f.ShopCo.Account));Assert.Equal(seller,sim.Balance(f.FarmCo.Account));Assert.Equal(total,sim.MonetaryTotal);Assert.True(truck.Fuel<fuel);
        Assert.Single(sim.State.Ledger,m=>m.Category=="Fornecedor local"&&m.EntityId==f.Target.Id);
        sim.CheckInvariants();
    }

    [Theory]
    [InlineData("driver")][InlineData("fuel")][InlineData("capacity")][InlineData("owner")][InlineData("inbound")]
    public void An_inapt_destination_or_unavailable_transport_keeps_the_reservation_and_empty_truck_free(string reason)
    {
        var f=PaidPurchase();Assert.True(f.Sim.DemolishBuilding(f.Target.Id).Success);
        f.Worker.Path.Clear();f.Worker.PathIndex=0;f.Worker.Position=f.Farm.Door;f.Sister.Stock[ResourceKind.Food]=0;
        if(reason=="driver")foreach(var citizen in f.Sim.State.Citizens)citizen.WeeklyDayOff=(int)f.Sim.State.Now.DayOfWeek;
        if(reason=="fuel")f.Truck.Fuel=0;
        if(reason=="capacity")f.Sister.Stock[ResourceKind.Food]=f.Sister.Definition.Capacity-19;
        if(reason=="owner"){f.Sister.CompanyId=f.FarmCo.Id;f.Sister.OwnerAccount=f.FarmCo.Account;}
        if(reason=="inbound"){f.Sister.Stock[ResourceKind.Food]=f.Sister.Definition.Capacity-20;f.Sim.State.Shipments.Add(new Shipment{Id=f.Sim.State.NextId++,DestinationBuildingId=f.Sister.Id,Cargo=new(){{ResourceKind.Food,1}},GoodsOwnerAccount=f.ShopCo.Account});}
        var pose=f.Truck.Position;var fuel=f.Truck.Fuel;
        f.Sim.State.EconomyNextAt=f.Sim.State.ElapsedSeconds;f.Sim.Advance(30);
        Assert.False(f.Shipment.PickedUp);Assert.False(f.Shipment.Complete);Assert.False(f.Shipment.Cancelled);Assert.Equal(0,Free(f.Sim,f.Farm),6);
        Assert.Null(f.Truck.ShipmentId);Assert.Null(f.Truck.DriverId);Assert.Equal(pose,f.Truck.Position);Assert.Equal(fuel,f.Truck.Fuel);Assert.Empty(f.Truck.Cargo);
        f.Sim.CheckInvariants();
    }

    [Fact] public void Demolishing_the_origin_after_the_destination_records_real_loss_without_releasing_goods_for_resale()
    {
        var f=PaidPurchase();Assert.True(f.Sim.DemolishBuilding(f.Target.Id).Success);
        var quote=f.Sim.QuoteBuildingDemolition(f.Farm.Id);
        Assert.Equal(ShipmentImpactKind.LostReserved,quote.Shipments.Single().Kind);
        Assert.True(f.Sim.DemolishBuilding(f.Farm.Id,quote).Success);
        Assert.True(f.Shipment.Complete);Assert.True(f.Shipment.Cancelled);Assert.Empty(f.Farm.Stock);Assert.Empty(f.Truck.Cargo);
        Assert.Contains(f.Sim.State.CityEvents,e=>e.Description.Contains("reservas já vendidas a "+f.ShopCo.Name));f.Sim.CheckInvariants();
    }
}
