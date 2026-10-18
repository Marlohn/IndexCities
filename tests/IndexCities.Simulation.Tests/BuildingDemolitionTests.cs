using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class BuildingDemolitionTests
{
    // Ciclo integrado: Prefeitura, rua, casa construída fisicamente e família
    // migrante que compra a casa e chega pelas regras normais da simulação.
    static (CitySimulation Sim,Building House,Family Family) OccupiedCity(int seed=42)
    {
        var sim=new CitySimulation(seed);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(7200);
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(placed.Success,placed.Message);
        var house=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        for(var i=0;i<96&&!(house.Completed&&sim.State.Families.Any(f=>f.HomeId==house.Id));i++)sim.Advance(1800);
        var family=sim.State.Families.Single(f=>f.HomeId==house.Id);Assert.NotNull(house.OwnerAccount);
        return (sim,house,family);
    }
    static decimal Paid(CitySimulation sim,string category,int buildingId)=>sim.State.Ledger.Where(m=>m.Category==category&&m.EntityId==buildingId).Sum(m=>m.Amount);
    static Building Add(CitySimulation sim,BuildingKind kind,string? owner=null)
    {
        var building=new Building{Id=sim.State.NextId++,Kind=kind,Position=new(220,220),Completed=true,OwnerAccount=owner};sim.State.Buildings.Add(building);return building;
    }

    [Fact] public void Occupied_house_without_alternative_pays_once_waits_and_survives_save_load()
    {
        var (sim,house,family)=OccupiedCity();var owner=house.OwnerAccount!;
        sim.State.Obligations.Add(new Obligation{Id=sim.State.NextId++,Debtor=owner,Creditor="city",Outstanding=12,Kind="Imposto",BuildingId=house.Id,Due=sim.State.Now});
        var quote=sim.QuoteBuildingDemolition(house.Id);Assert.True(quote.Allowed,quote.Message);Assert.True(quote.WaitsForResidents);Assert.Single(quote.FamilyIds);
        var city=sim.CityBalance;var ownerBefore=sim.Balance(owner);var total=sim.MonetaryTotal;
        var result=sim.DemolishBuilding(house.Id,quote);Assert.True(result.Success,result.Message);
        Assert.Equal(ownerBefore+quote.Compensation,sim.Balance(owner));Assert.Equal(city-quote.TotalCost,sim.CityBalance);Assert.Equal(total,sim.MonetaryTotal);
        Assert.False(house.Removed);Assert.NotNull(house.Demolition);Assert.Equal(owner,house.OwnerAccount);Assert.Equal(family.Id,house.FamilyId);
        Assert.False(sim.QuoteBuildingDemolition(house.Id).Allowed);Assert.False(sim.DemolishBuilding(house.Id).Success);
        // Pendência persiste no save sem repetir pagamentos.
        sim.Advance(3600);var loaded=CitySimulation.FromJson(sim.SaveToJson());var reloaded=loaded.State.Buildings.Single(b=>b.Id==house.Id);
        Assert.NotNull(reloaded.Demolition);Assert.Equal(quote.Compensation,reloaded.Demolition!.CompensationPaid);
        loaded.Advance(86400+3600);loaded.CheckInvariants();
        Assert.True(reloaded.Removed);Assert.Null(reloaded.OwnerAccount);Assert.Null(reloaded.FamilyId);
        var lost=loaded.State.Families.Single(f=>f.Id==family.Id);Assert.Null(lost.HomeId);
        Assert.Equal(quote.Compensation,Paid(loaded,"Indenização",house.Id));Assert.Equal(quote.RemovalCost,Paid(loaded,"Demolição",house.Id));
        Assert.Contains(loaded.State.Obligations,o=>o.Debtor==owner&&o.BuildingId==house.Id&&o.Kind=="Imposto");
        Assert.DoesNotContain(loaded.State.Ledger,m=>m.Category=="Imposto imobiliário"&&m.EntityId==house.Id&&m.At>loaded.State.Now.AddHours(-1));
        Assert.All(lost.Members.Select(id=>loaded.State.Citizens.Single(c=>c.Id==id)),c=>Assert.Contains(c.History,h=>h.Description.StartsWith("Perdeu a moradia")));
    }
    [Fact] public void Occupied_house_with_vacant_home_for_sale_moves_family_and_is_removed_before_deadline()
    {
        var (sim,house,family)=OccupiedCity();
        // Sem novos migrantes, a casa vaga permanece à venda para a família deslocada.
        foreach(var candidate in sim.State.FamilyCandidates)candidate.Migrated=true;
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-60,14));Assert.True(placed.Success,placed.Message);var vacant=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        for(var i=0;i<96&&!vacant.Completed;i++)sim.Advance(1800);Assert.True(vacant.Completed);Assert.Null(vacant.OwnerAccount);
        var owner=house.OwnerAccount!;var global=sim.Balance("global");var total=sim.MonetaryTotal;
        var quote=sim.QuoteBuildingDemolition(house.Id);var price=sim.MarketValue(vacant);
        Assert.True(sim.DemolishBuilding(house.Id,quote).Success);
        var deadline=house.Demolition!.DeadlineSeconds;
        for(var i=0;i<4&&!house.Removed;i++)sim.Advance(1800);
        Assert.True(house.Removed);Assert.True(sim.State.ElapsedSeconds<deadline);Assert.Equal(vacant.Id,family.HomeId);Assert.Equal(family.Id,vacant.FamilyId);
        Assert.StartsWith("sim:",vacant.OwnerAccount);Assert.Contains(family.Members,id=>vacant.OwnerAccount=="sim:"+id);
        Assert.Equal(1,house.Demolition.FamiliesRelocated);Assert.Equal(global+quote.RemovalCost+price,sim.Balance("global"));Assert.Equal(total,sim.MonetaryTotal);
        Assert.Equal(quote.Compensation,Paid(sim,"Indenização",house.Id));Assert.Equal(owner,sim.State.Ledger.Single(m=>m.Category=="Indenização").To);
        sim.CheckInvariants();
    }
    [Fact] public void Insufficient_cash_invalid_wallet_and_stale_preview_change_nothing()
    {
        var sim=new CitySimulation(7);var house=Add(sim,BuildingKind.House,"company:3");sim.State.Companies.Add(new Company{Id=3,Name="Mercado 3"});
        var before=sim.SaveToJson();var quote=sim.QuoteBuildingDemolition(house.Id);
        Assert.False(quote.Allowed);Assert.Contains("indisponível",quote.Message);Assert.False(sim.DemolishBuilding(house.Id).Success);Assert.Equal(before,sim.SaveToJson());
        sim.State.Wallets["company:3"]=0;sim.State.Wallets["global"]+=sim.CityBalance;sim.State.Wallets["city"]=0;before=sim.SaveToJson();
        Assert.False(sim.DemolishBuilding(house.Id).Success);Assert.Equal(before,sim.SaveToJson());
        sim.State.Wallets["city"]=sim.State.Wallets["global"];sim.State.Wallets["global"]=0;var reviewed=sim.QuoteBuildingDemolition(house.Id);Assert.True(reviewed.Allowed,reviewed.Message);
        sim.State.Citizens.Add(new Citizen{Id=sim.State.NextId++,Name="Eva",FamilyId=99,Age=30,WorkplaceId=house.Id});before=sim.SaveToJson();
        var stale=sim.DemolishBuilding(house.Id,reviewed);Assert.False(stale.Success);Assert.Equal(CitySimulation.StaleDemolitionPreview,stale.Message);Assert.Equal(before,sim.SaveToJson());
    }
    [Fact] public void City_hall_is_refused_and_municipal_service_is_removed_with_workers_and_stock()
    {
        var sim=new CitySimulation(7);var hall=Add(sim,BuildingKind.CityHall);var before=sim.SaveToJson();
        Assert.False(sim.DemolishBuilding(hall.Id).Success);Assert.Equal(before,sim.SaveToJson());
        var depot=Add(sim,BuildingKind.WorksDepot);depot.Stock[ResourceKind.Wood]=5;var worker=new Citizen{Id=sim.State.NextId++,Name="Lia",FamilyId=98,Age=30,WorkplaceId=depot.Id,WagesAccrued=3};sim.State.Citizens.Add(worker);sim.State.Wallets[worker.Account]=0;
        var global=sim.Balance("global");var quote=sim.QuoteBuildingDemolition(depot.Id);Assert.Equal(0m,quote.Compensation);
        Assert.True(sim.DemolishBuilding(depot.Id,quote).Success);
        Assert.True(depot.Removed);Assert.Empty(depot.Stock);Assert.Null(worker.WorkplaceId);Assert.Equal(3m,sim.Balance(worker.Account));Assert.Equal(0m,worker.WagesAccrued);
        Assert.Equal(global+quote.RemovalCost,sim.Balance("global"));sim.CheckInvariants();
    }
    [Fact] public void Private_business_pays_company_ends_jobs_and_loses_stock_once()
    {
        var sim=new CitySimulation(7);var shop=Add(sim,BuildingKind.Grocery,"company:3");shop.CompanyId=3;shop.Stock[ResourceKind.Food]=20;
        var company=new Company{Id=3,Name="Mercado 3",Buildings=[shop.Id]};sim.State.Companies.Add(company);sim.State.Wallets[company.Account]=0;
        var worker=new Citizen{Id=sim.State.NextId++,Name="Bruno",FamilyId=97,Age=30,WorkplaceId=shop.Id};sim.State.Citizens.Add(worker);sim.State.Wallets[worker.Account]=0;
        var quote=sim.QuoteBuildingDemolition(shop.Id);Assert.True(quote.Compensation>0);var total=sim.MonetaryTotal;
        Assert.True(sim.DemolishBuilding(shop.Id,quote).Success);
        Assert.True(shop.Removed);Assert.Equal(quote.Compensation,sim.Balance(company.Account));Assert.Empty(company.Buildings);Assert.True(company.Active);
        Assert.Null(shop.CompanyId);Assert.Null(shop.OwnerAccount);Assert.Null(worker.WorkplaceId);Assert.Empty(shop.Stock);Assert.Equal(total,sim.MonetaryTotal);
        Assert.Contains("estoque perdido",sim.State.LastMessage);sim.CheckInvariants();
    }
    [Fact] public void Cargo_in_transit_to_demolished_shop_stays_on_truck_with_its_titular_across_save_load()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);sim.Advance(7200);
        Assert.True(sim.PlaceBuilding(BuildingKind.House,new(-100,14)).Success);var placed=sim.PlaceBuilding(BuildingKind.Grocery,new(-40,16));Assert.True(placed.Success,placed.Message);
        var shop=sim.State.Buildings.Single(b=>b.Id==placed.Id);Shipment? shipment=null;
        for(var i=0;i<6000&&shipment==null;i++){sim.Advance(30);shipment=sim.State.Shipments.FirstOrDefault(s=>s.DestinationBuildingId==shop.Id&&!s.Complete&&s.PickedUp&&s.Purpose==ShipmentPurpose.Food);}
        Assert.NotNull(shipment);var truck=sim.State.Vehicles.Single(v=>v.Id==shipment!.VehicleId);var cargo=truck.Cargo.ToDictionary(k=>k.Key,k=>k.Value);var titular=shipment!.GoodsOwnerAccount;
        Assert.True(sim.DemolishBuilding(shop.Id).Success);Assert.True(shop.Removed);
        for(var i=0;i<60;i++)sim.Advance(60);
        var loaded=CitySimulation.FromJson(sim.SaveToJson());for(var i=0;i<60;i++)loaded.Advance(60);loaded.CheckInvariants();
        var keptShipment=loaded.State.Shipments.Single(s=>s.Id==shipment.Id);var keptTruck=loaded.State.Vehicles.Single(v=>v.Id==truck.Id);
        Assert.False(keptShipment.Complete);Assert.False(keptShipment.Cancelled);Assert.Equal(titular,keptShipment.GoodsOwnerAccount);Assert.Equal(cargo,keptTruck.Cargo);
        Assert.Equal("Destino indisponível: carga preservada",keptTruck.Cause);
    }
    [Fact] public void Vacant_private_building_without_owner_is_removed_without_reserve_indemnity()
    {
        var sim=new CitySimulation(7);var house=Add(sim,BuildingKind.House);var global=sim.Balance("global");var quote=sim.QuoteBuildingDemolition(house.Id);
        Assert.True(sim.DemolishBuilding(house.Id,quote).Success);Assert.True(house.Removed);
        Assert.Equal(global+quote.RemovalCost,sim.Balance("global"));Assert.Equal(0m,Paid(sim,"Indenização",house.Id));
    }
    [Fact] public void Local_order_not_yet_loaded_for_a_demolished_shop_stays_at_the_farm()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);sim.Advance(7200);
        sim.State.Companies.Add(new Company{Id=900,Name="Campo 900"});sim.State.Wallets["company:900"]=0;sim.State.Companies.Add(new Company{Id=901,Name="Mercado 901"});sim.State.Wallets["company:901"]=0;
        var farm=new Building{Id=sim.State.NextId++,Kind=BuildingKind.Farm,Position=new(-40,26),Completed=true,OwnerAccount="company:900",CompanyId=900};farm.Stock[ResourceKind.Food]=30;sim.State.Buildings.Add(farm);
        var shop=new Building{Id=sim.State.NextId++,Kind=BuildingKind.Grocery,Position=new(40,14),Completed=true,OwnerAccount="company:901",CompanyId=901};sim.State.Buildings.Add(shop);
        Assert.NotNull(sim.Access(farm));Assert.NotNull(sim.Access(shop));
        var truck=new Vehicle{Id=sim.State.NextId++,OwnerAccount="company:900",HomeBuildingId=farm.Id,Position=farm.Door,Status=VehicleStatus.Unloading,LoadTimer=1,Fuel=40};
        var order=new Shipment{Id=sim.State.NextId++,Purpose=ShipmentPurpose.Food,SourceBuildingId=farm.Id,DestinationBuildingId=shop.Id,VehicleId=truck.Id,PickedUp=false,GoodsOwnerAccount="company:901",Cargo=new(){{ResourceKind.Food,20}}};
        var driver=new Citizen{Id=sim.State.NextId++,Name="Lia 900",FamilyId=sim.State.NextId++,Resident=true,InsideMap=true,Age=30,WorkplaceId=farm.Id,VehicleId=truck.Id,Position=truck.Position};sim.State.Citizens.Add(driver);sim.State.Wallets[driver.Account]=0;sim.State.Families.Add(new Family{Id=driver.FamilyId,Members=[driver.Id]});
        truck.DriverId=driver.Id;truck.ShipmentId=order.Id;sim.State.Vehicles.Add(truck);sim.State.Shipments.Add(order);
        Assert.True(sim.DemolishBuilding(shop.Id).Success);Assert.True(shop.Removed);
        for(var i=0;i<30;i++)sim.Advance(60);sim.CheckInvariants();
        Assert.True(order.Complete&&order.Cancelled);Assert.Null(truck.ShipmentId);Assert.Empty(truck.Cargo);Assert.Equal(30,farm.Quantity(ResourceKind.Food),3);
    }
    [Fact] public void Saves_without_demolition_data_load_with_defaults_and_stay_unchanged()
    {
        var sim=new CitySimulation(7);Add(sim,BuildingKind.House);var json=sim.SaveToJson();
        Assert.DoesNotContain("\"Demolition\"",json);Assert.Contains("\"AssistedVacatingDays\":1,",json);
        var loaded=CitySimulation.FromJson(json.Replace("\"AssistedVacatingDays\":1,",""));
        Assert.Equal(1,loaded.State.Tuning.AssistedVacatingDays);Assert.All(loaded.State.Buildings,b=>Assert.Null(b.Demolition));Assert.Equal(json,loaded.SaveToJson());
    }
}
