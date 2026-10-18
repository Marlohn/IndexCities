using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// Regressões do checkpoint Demolir: a espera pela desocupação depende só de
// moradores que ainda estão na cidade, e a prévia mostra as remessas afetadas.
public sealed class BuildingDemolitionFixTests
{
    static (CitySimulation Sim,Building House,Family Family) OccupiedCity()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(7200);
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(placed.Success,placed.Message);
        var house=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        for(var i=0;i<96&&!(house.Completed&&sim.State.Families.Any(f=>f.HomeId==house.Id));i++)sim.Advance(1800);
        return (sim,house,sim.State.Families.Single(f=>f.HomeId==house.Id));
    }

    static Building Add(CitySimulation sim,BuildingKind kind,string? owner=null,int? companyId=null)
    {
        var building=new Building{Id=sim.State.NextId++,Kind=kind,Position=new(220,220),Completed=true,OwnerAccount=owner,CompanyId=companyId};
        sim.State.Buildings.Add(building);return building;
    }

    // Família que já emigrou: membros fora da cidade, mas o vínculo de moradia ainda aponta para a casa.
    [Fact] public void Emigrated_family_without_residents_is_removed_immediately_with_neutral_result()
    {
        var (sim,house,family)=OccupiedCity();
        family.Emigrating=true;
        foreach(var c in sim.State.Citizens.Where(c=>family.Members.Contains(c.Id))){c.Resident=false;c.InsideMap=false;}
        var owner=house.OwnerAccount!;var ownerBefore=sim.Balance(owner);var total=sim.MonetaryTotal;
        var memberMoney=family.Members.Sum(id=>sim.Balance(sim.State.Citizens.Single(c=>c.Id==id).Account));
        var quote=sim.QuoteBuildingDemolition(house.Id);
        Assert.True(quote.Allowed,quote.Message);Assert.False(quote.WaitsForResidents);Assert.Empty(quote.FamilyIds);
        var result=sim.DemolishBuilding(house.Id,quote);
        Assert.True(result.Success,result.Message);
        // Retirada imediata: a casa sai na confirmação, sem prazo nem família "sem moradia".
        Assert.True(house.Removed);Assert.Null(family.HomeId);Assert.Null(house.FamilyId);
        Assert.Equal(ownerBefore+quote.Compensation,sim.Balance(owner));
        // The titular is one of the family's members: only the indemnity changes their money.
        Assert.Equal(memberMoney+quote.Compensation,family.Members.Sum(id=>sim.Balance(sim.State.Citizens.Single(c=>c.Id==id).Account)));
        Assert.Equal(total,sim.MonetaryTotal);Assert.True(family.Emigrating);
        Assert.DoesNotContain("ficaram sem moradia",sim.State.LastMessage);
        Assert.DoesNotContain(sim.State.CityEvents,e=>e.Description.Contains("ficaram sem moradia"));
        Assert.Equal(quote.Compensation,sim.State.Ledger.Where(m=>m.Category=="Indenização"&&m.EntityId==house.Id).Sum(m=>m.Amount));
        sim.CheckInvariants();
    }

    [Fact] public void Resident_family_still_waits_for_the_deadline_and_then_reports_homelessness()
    {
        var (sim,house,family)=OccupiedCity();
        var quote=sim.QuoteBuildingDemolition(house.Id);Assert.True(quote.WaitsForResidents);Assert.Single(quote.FamilyIds);
        Assert.True(sim.DemolishBuilding(house.Id,quote).Success);
        Assert.False(house.Removed);
        sim.Advance(sim.State.Tuning.AssistedVacatingDays*86400+3600);
        Assert.True(house.Removed);Assert.Null(family.HomeId);
        Assert.Contains(sim.State.CityEvents,e=>e.Description.Contains("1 família(s) ficaram sem moradia"));
    }

    // Preview lists the affected shipments; loaded cargo is not stock lost in the building.
    [Fact] public void Preview_lists_shipments_by_real_kind_without_changing_price_accounts_or_cargo()
    {
        var sim=new CitySimulation(7);
        var shop=Add(sim,BuildingKind.Grocery,"company:3",3);sim.State.Companies.Add(new Company{Id=3,Name="Mercado 3",Buildings=[shop.Id]});sim.State.Wallets["company:3"]=0;
        shop.Stock[ResourceKind.Food]=5;
        var farm=Add(sim,BuildingKind.Farm,"company:4",4);sim.State.Companies.Add(new Company{Id=4,Name="Campo 4"});sim.State.Wallets["company:4"]=0;
        var baseline=sim.QuoteBuildingDemolition(shop.Id);
        // Remessa não carregada da fazenda para a loja: a carga fica na origem.
        var unloaded=new Shipment{Id=sim.State.NextId++,Purpose=ShipmentPurpose.Food,SourceBuildingId=farm.Id,DestinationBuildingId=shop.Id,VehicleId=-1,GoodsOwnerAccount="company:3",Cargo=new(){{ResourceKind.Food,20}}};
        // Remessa carregada para a loja: caminhão com carga e titular reais.
        var truck=new Vehicle{Id=sim.State.NextId++,OwnerAccount="company:4",Position=farm.Door,Status=VehicleStatus.Travelling,Fuel=40,Route=[1,2],RouteIndex=0};
        truck.Cargo[ResourceKind.Food]=16;
        var loaded=new Shipment{Id=sim.State.NextId++,Purpose=ShipmentPurpose.Food,SourceBuildingId=farm.Id,DestinationBuildingId=shop.Id,VehicleId=truck.Id,PickedUp=true,GoodsOwnerAccount="company:4",Cargo=new(){{ResourceKind.Food,16}}};
        truck.ShipmentId=loaded.Id;sim.State.Vehicles.Add(truck);sim.State.Shipments.Add(unloaded);sim.State.Shipments.Add(loaded);
        var wallets=sim.State.Wallets.ToDictionary(kv=>kv.Key,kv=>kv.Value);var before=sim.SaveToJson();
        var quote=sim.QuoteBuildingDemolition(shop.Id);
        Assert.True(quote.Allowed,quote.Message);
        Assert.Equal(baseline.Compensation,quote.Compensation);Assert.Equal(baseline.RemovalCost,quote.RemovalCost);Assert.Equal(baseline.TotalCost,quote.TotalCost);
        Assert.Equal(baseline.Stock.Count,quote.Stock.Count);Assert.Equal(5.0,quote.Stock[ResourceKind.Food],6);
        var kinds=quote.Shipments.ToDictionary(s=>s.ShipmentId,s=>s.Kind);
        Assert.Equal(ShipmentImpactKind.StaysAtOrigin,kinds[unloaded.Id]);
        Assert.Equal(ShipmentImpactKind.OnboardStuck,kinds[loaded.Id]);
        Assert.Equal(20.0,quote.Shipments.Single(s=>s.ShipmentId==unloaded.Id).Quantity,6);
        Assert.Equal("company:4",quote.Shipments.Single(s=>s.ShipmentId==loaded.Id).GoodsOwnerAccount);
        // Reading the preview changes nothing: no wallet, cargo or shipment moves.
        Assert.Equal(before,sim.SaveToJson());
        Assert.All(wallets,kv=>Assert.Equal(kv.Value,sim.State.Wallets[kv.Key]));
        // Confirming pays only the indemnity and removal, never the cargo owner.
        var titularBefore=sim.Balance("company:4");
        Assert.True(sim.DemolishBuilding(shop.Id,quote).Success);
        Assert.Equal(titularBefore,sim.Balance("company:4"));
        Assert.Equal(16,truck.Cargo[ResourceKind.Food]);Assert.False(loaded.Complete);Assert.False(loaded.Cancelled);
        Assert.Equal(20.0,unloaded.Cargo[ResourceKind.Food]);Assert.Equal((int?)farm.Id,unloaded.SourceBuildingId);
        sim.CheckInvariants();
    }

    [Fact] public void Reserved_outgoing_shipment_of_demolished_shop_is_listed_as_lost_and_the_loaded_one_is_not_stock()
    {
        var sim=new CitySimulation(7);
        var shop=Add(sim,BuildingKind.Grocery,"company:3",3);sim.State.Companies.Add(new Company{Id=3,Name="Mercado 3",Buildings=[shop.Id]});sim.State.Wallets["company:3"]=0;
        shop.Stock[ResourceKind.Food]=9;
        var outgoing=new Shipment{Id=sim.State.NextId++,Purpose=ShipmentPurpose.Food,SourceBuildingId=shop.Id,DestinationBuildingId=null,VehicleId=-1,GoodsOwnerAccount="company:3",Cargo=new(){{ResourceKind.Food,4}}};
        sim.State.Shipments.Add(outgoing);
        var quote=sim.QuoteBuildingDemolition(shop.Id);
        Assert.Equal(ShipmentImpactKind.LostReserved,quote.Shipments.Single().Kind);
        Assert.Equal(9.0,quote.Stock[ResourceKind.Food],6);
    }

    [Fact] public void Shipment_change_after_preview_asks_for_a_new_preview_and_charges_nothing()
    {
        var sim=new CitySimulation(7);
        var shop=Add(sim,BuildingKind.Grocery,"company:3",3);sim.State.Companies.Add(new Company{Id=3,Name="Mercado 3",Buildings=[shop.Id]});sim.State.Wallets["company:3"]=0;
        var farm=Add(sim,BuildingKind.Farm,"company:4",4);sim.State.Companies.Add(new Company{Id=4,Name="Campo 4"});sim.State.Wallets["company:4"]=0;
        var unloaded=new Shipment{Id=sim.State.NextId++,Purpose=ShipmentPurpose.Food,SourceBuildingId=farm.Id,DestinationBuildingId=shop.Id,VehicleId=-1,GoodsOwnerAccount="company:3",Cargo=new(){{ResourceKind.Food,20}}};
        sim.State.Shipments.Add(unloaded);
        var reviewed=sim.QuoteBuildingDemolition(shop.Id);
        // The shipment is now carried away: the earlier preview no longer matches.
        var truck=new Vehicle{Id=sim.State.NextId++,OwnerAccount="company:4",Position=farm.Door,Status=VehicleStatus.Travelling,Fuel=40,Route=[1],RouteIndex=0};
        truck.Cargo[ResourceKind.Food]=20;truck.ShipmentId=unloaded.Id;sim.State.Vehicles.Add(truck);unloaded.PickedUp=true;unloaded.VehicleId=truck.Id;
        var before=sim.SaveToJson();
        var stale=sim.DemolishBuilding(shop.Id,reviewed);
        Assert.False(stale.Success);Assert.Equal(CitySimulation.StaleDemolitionPreview,stale.Message);Assert.Equal(before,sim.SaveToJson());
    }
}
