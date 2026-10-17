using IndexCities.Simulation;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class LegacyTruckFleetTests
{
    static CitySimulation City()=>new(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
    static string LegacyJson(CitySimulation sim,bool keepBodies=false)
    {
        var json=JsonNode.Parse(sim.SaveToJson())!.AsObject();json.Remove("ConstructionStateVersion");
        foreach(var vehicle in json["Vehicles"]!.AsArray()){
            if(!keepBodies)vehicle!.AsObject().Remove("Body");
            vehicle!.AsObject().Remove("LegacyBodyAfterReturn");
        }
        return json.ToJsonString();
    }
    static void AssertMix(CitySimulation sim)
    {
        Assert.Equal(6,sim.State.Vehicles.Count);Assert.Equal(3,sim.State.Vehicles.Count(v=>v.Body==TruckBody.Dump));
        foreach(var body in new[]{TruckBody.Mixer,TruckBody.Flatbed,TruckBody.Cargo})Assert.Single(sim.State.Vehicles,v=>v.Body==body);
        Assert.All(sim.State.Vehicles,v=>Assert.Null(v.LegacyBodyAfterReturn));
    }
    static void SendLoads(CitySimulation sim,Building depot,bool asphalt)
    {
        var access=sim.Access(depot)!;var trucks=sim.State.Vehicles.OrderBy(v=>v.Id).ToArray();
        for(var i=0;i<trucks.Length;i++){
            var v=trucks[i];var start=sim.State.Roads.Single(r=>r.Position==sim.State.Entry+new Point(12*i,0));
            var kind=asphalt?ResourceKind.Asphalt:v.Body switch{TruckBody.Dump=>ResourceKind.Aggregate,TruckBody.Mixer=>ResourceKind.Concrete,TruckBody.Flatbed=>ResourceKind.Steel,_=>ResourceKind.Food};
            // A legacy snapshot of paid construction cargo redirected to a real
            // depot uses the existing Construction shipment with no active work.
            var shipment=new Shipment{Id=sim.State.NextId++,VehicleId=v.Id,Purpose=ShipmentPurpose.Construction,PickedUp=true,DestinationBuildingId=depot.Id,Destination=access.Position,Cargo=new(){{kind,10+i}}};
            sim.State.Shipments.Add(shipment);v.ShipmentId=shipment.Id;v.Cargo=new(shipment.Cargo);
            v.Position=start.Position;v.Route=sim.FindRoadRoute(start.Id,access.Id);v.RouteIndex=0;v.DockPosition=access.Position+new Point(0,sim.State.Tuning.RoadWidth/2+1.5);
            v.Status=VehicleStatus.Travelling;v.Heading=Math.PI/2;v.Fuel=40;v.CrewAboard=false;v.CrewCount=0;
        }
    }
    static (CitySimulation Sim,Building Depot) Incoming()
    {
        var sim=City();Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,60)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-160,0)).Success);sim.Advance(3600);
        var placed=sim.PlaceBuilding(BuildingKind.WorksDepot,new(-176,18));Assert.True(placed.Success,placed.Message);sim.Advance(3600);
        var depot=sim.State.Buildings.Single(b=>b.Id==placed.Id);Assert.True(depot.Completed);
        sim.State.Vehicles.RemoveAll(v=>v.Machine!=PavingMachine.None);Assert.Equal(6,sim.State.Vehicles.Count);
        sim.State.EconomyNextAt=double.MaxValue;SendLoads(sim,depot,true);return(sim,depot);
    }
    static void FinishReturns(CitySimulation sim,bool expectLegacyBodies)
    {
        var sawEmptyReturn=false;
        for(var i=0;i<1200&&sim.State.Vehicles.Any(v=>v.Status!=VehicleStatus.Outside);i++){
            sim.Advance(2);
            foreach(var v in sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside)){
                if(expectLegacyBodies)Assert.Equal(TruckBody.Dump,v.Body);
                if(v.Cargo.Count==0&&v.Status==VehicleStatus.Travelling)sawEmptyReturn=true;
            }
        }
        Assert.True(sawEmptyReturn);Assert.All(sim.State.Vehicles,v=>Assert.Equal(VehicleStatus.Outside,v.Status));
        Assert.All(sim.State.Shipments,s=>Assert.True(s.Complete));sim.CheckInvariants();
    }

    [Fact]
    public void Six_legacy_asphalt_trucks_keep_loads_ids_drivers_and_money_then_convert_only_outside_once()
    {
        var (original,depot)=Incoming();var ids=original.State.Vehicles.Select(v=>v.Id).ToArray();
        var cargo=original.State.Vehicles.ToDictionary(v=>v.Id,v=>JsonSerializer.Serialize(v.Cargo));
        var drivers=original.State.Vehicles.ToDictionary(v=>v.Id,v=>(v.DriverId,v.ExternalDriverId,v.OwnerAccount,v.CargoCapacity,v.Fuel));
        var balance=original.CityBalance;var committed=original.Committed;var wallets=JsonSerializer.Serialize(original.State.Wallets);var ledger=original.State.Ledger.Count;
        var sim=CitySimulation.FromJson(LegacyJson(original));
        Assert.Equal(ids,sim.State.Vehicles.Select(v=>v.Id));Assert.Equal(wallets,JsonSerializer.Serialize(sim.State.Wallets));Assert.Equal(committed,sim.Committed);
        foreach(var v in sim.State.Vehicles){Assert.Equal(TruckBody.Dump,v.Body);Assert.NotNull(v.LegacyBodyAfterReturn);Assert.Equal(cargo[v.Id],JsonSerializer.Serialize(v.Cargo));Assert.Equal(drivers[v.Id],(v.DriverId,v.ExternalDriverId,v.OwnerAccount,v.CargoCapacity,v.Fuel));}
        FinishReturns(sim,true);AssertMix(sim);Assert.Equal(75,sim.State.Buildings.Single(b=>b.Id==depot.Id).Quantity(ResourceKind.Asphalt));
        Assert.Equal(balance,sim.CityBalance);Assert.Equal(committed,sim.Committed);Assert.Equal(ledger,sim.State.Ledger.Count);Assert.Equal(ids,sim.State.Vehicles.Select(v=>v.Id));
        var bodies=sim.State.Vehicles.ToDictionary(v=>v.Id,v=>v.Body);var saved=sim.SaveToJson();sim=CitySimulation.FromJson(saved);Assert.Equal(saved,sim.SaveToJson());
        SendLoads(sim,sim.State.Buildings.Single(b=>b.Id==depot.Id),false);FinishReturns(sim,false);
        Assert.All(sim.State.Vehicles,v=>Assert.Equal(bodies[v.Id],v.Body));AssertMix(sim);
        saved=sim.SaveToJson();Assert.Equal(saved,CitySimulation.FromJson(saved).SaveToJson());
    }

    [Fact]
    public void Reload_during_conversion_preserves_individual_targets_and_deterministic_returns()
    {
        var (original,_)=Incoming();var sim=CitySimulation.FromJson(LegacyJson(original));sim.Advance(20);
        Assert.Contains(sim.State.Vehicles,v=>v.LegacyBodyAfterReturn!=null);var saved=sim.SaveToJson();var loaded=CitySimulation.FromJson(saved);Assert.Equal(saved,loaded.SaveToJson());
        var targets=sim.State.Vehicles.ToDictionary(v=>v.Id,v=>v.LegacyBodyAfterReturn);
        Assert.All(loaded.State.Vehicles,v=>Assert.Equal(targets[v.Id],v.LegacyBodyAfterReturn));
        FinishReturns(sim,true);FinishReturns(loaded,true);Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());AssertMix(loaded);
    }

    [Fact]
    public void Idle_legacy_trucks_convert_immediately_and_targets_are_stable_by_identity()
    {
        var original=City();var json=JsonNode.Parse(LegacyJson(original))!.AsObject();var reverse=json.DeepClone().AsObject();
        reverse["Vehicles"]=new JsonArray(reverse["Vehicles"]!.AsArray().Reverse().Select(v=>v!.DeepClone()).ToArray());
        var a=CitySimulation.FromJson(json.ToJsonString());var b=CitySimulation.FromJson(reverse.ToJsonString());AssertMix(a);AssertMix(b);
        Assert.All(a.State.Vehicles,v=>Assert.Equal(v.Body,b.State.Vehicles.Single(other=>other.Id==v.Id).Body));
        Assert.Equal(original.CityBalance,a.CityBalance);Assert.Equal(original.Committed,a.Committed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Explicit_modern_bodies_never_enter_the_legacy_exception(bool removeConstructionVersion)
    {
        var original=City();foreach(var v in original.State.Vehicles)v.Body=TruckBody.Dump;
        var sim=CitySimulation.FromJson(removeConstructionVersion?LegacyJson(original,true):original.SaveToJson());
        Assert.Equal(6,sim.State.Vehicles.Count);Assert.All(sim.State.Vehicles,v=>{Assert.Equal(TruckBody.Dump,v.Body);Assert.Null(v.LegacyBodyAfterReturn);});
        var saved=sim.SaveToJson();Assert.Equal(saved,CitySimulation.FromJson(saved).SaveToJson());
    }

    [Fact]
    public void Company_owned_and_purchased_trucks_do_not_enter_the_external_fleet_mix()
    {
        var original=City();var local=new Vehicle{Id=original.State.NextId++,External=false,HomeBuildingId=123,OwnerAccount="company:123",Status=VehicleStatus.Parked,DriverId=456,Cargo=new(){{ResourceKind.Food,7}}};
        var purchased=new Vehicle{Id=original.State.NextId++,External=true,HomeBuildingId=123,OwnerAccount="company:123",Status=VehicleStatus.Travelling,ExternalDriverId="entrega de aquisiÃ§Ã£o"};
        original.State.Vehicles.AddRange([local,purchased]);var sim=CitySimulation.FromJson(LegacyJson(original));Assert.Equal(8,sim.State.Vehicles.Count);
        foreach(var id in new[]{local.Id,purchased.Id}){var v=sim.State.Vehicles.Single(v=>v.Id==id);Assert.Equal(TruckBody.Cargo,v.Body);Assert.Null(v.LegacyBodyAfterReturn);Assert.Equal("company:123",v.OwnerAccount);Assert.Equal(123,v.HomeBuildingId);}
        Assert.Equal(456,sim.State.Vehicles.Single(v=>v.Id==local.Id).DriverId);Assert.Equal(7,sim.State.Vehicles.Single(v=>v.Id==local.Id).Cargo[ResourceKind.Food]);
        Assert.Equal("entrega de aquisiÃ§Ã£o",sim.State.Vehicles.Single(v=>v.Id==purchased.Id).ExternalDriverId);
    }
}

