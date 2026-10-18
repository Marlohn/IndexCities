using System.Reflection;
using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// SPEC construcao (decisões humanas de 2026-10-18): carga privada já embarcada para um
// prédio demolido busca outro prédio apto da mesma empresa titular, ou aguarda em local
// seguro; família deslocada tem a primeira oportunidade de compra antes de migrantes.
public sealed class BuildingDemolitionCargoTests
{
    static CitySimulation Street()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(7200);
        return sim;
    }
    // Real company with real money taken from the Global Reserve, so the total is unchanged.
    static Company AddCompany(CitySimulation sim,int id,decimal money)
    {
        var company=new Company{Id=id,Name="Mercado "+id,Active=false};sim.State.Companies.Add(company);
        sim.State.Wallets[company.Account]=money;sim.State.Wallets["global"]-=money;return company;
    }
    static Building Shop(CitySimulation sim,Company? company,Point position)
    {
        var shop=new Building{Id=sim.State.NextId++,Kind=BuildingKind.Grocery,Position=position,Completed=true,OperationalCapacity=1,OwnerAccount=company?.Account,CompanyId=company?.Id};
        sim.State.Buildings.Add(shop);company?.Buildings.Add(shop.Id);return shop;
    }
    // The existing commercial import: paid by the titular, a real external truck with fuel and cargo.
    static (Vehicle Truck,Shipment Shipment) Import(CitySimulation sim,Building shop,double amount)
    {
        var order=typeof(CitySimulation).GetMethod("OrderImport",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Assert.True((bool)order.Invoke(sim,[shop,ResourceKind.Food,amount,shop.OwnerAccount!])!);
        var shipment=sim.State.Shipments.Last(s=>s.DestinationBuildingId==shop.Id);var truck=sim.State.Vehicles.Single(v=>v.Id==shipment.VehicleId);
        return (truck,shipment);
    }
    static void TravelSome(CitySimulation sim,Vehicle truck)
    {
        for(var i=0;i<600&&!(truck.Status==VehicleStatus.Travelling&&truck.RouteIndex>2);i++)sim.Advance(1);
        Assert.Equal(VehicleStatus.Travelling,truck.Status);
    }

    [Fact] public void Onboard_cargo_goes_physically_to_another_apt_shop_of_the_same_company_without_money_moving()
    {
        var sim=Street();
        var company=AddCompany(sim,900,5000);var rival=AddCompany(sim,901,0);
        var target=Shop(sim,company,new(-20,14));var sister=Shop(sim,company,new(60,14));
        // A closer shop of another company with room is never used: titular stays the same.
        var other=Shop(sim,rival,new(20,14));
        var depot=new Building{Id=sim.State.NextId++,Kind=BuildingKind.WorksDepot,Position=new(-60,14),Completed=true,OperationalCapacity=1};sim.State.Buildings.Add(depot);
        var (truck,shipment)=Import(sim,target,30);var cargo=truck.Cargo[ResourceKind.Food];
        TravelSome(sim,truck);
        var quote=sim.QuoteBuildingDemolition(target.Id);
        var impact=quote.Shipments.Single(s=>s.ShipmentId==shipment.Id);
        Assert.Equal(ShipmentImpactKind.OnboardSeeksSameCompany,impact.Kind);Assert.Contains("#"+sister.Id,impact.AlternativeDestination);
        var titular=sim.Balance(company.Account);var ledger=sim.State.Ledger.Count(m=>m.From==company.Account||m.To==company.Account);var total=sim.MonetaryTotal;
        Assert.True(sim.DemolishBuilding(target.Id,quote).Success);
        Assert.True(target.Removed);Assert.Equal((int?)sister.Id,shipment.DestinationBuildingId);Assert.Equal(VehicleStatus.Travelling,truck.Status);
        Assert.Contains("mantêm titular e quantidade",sim.State.LastMessage);
        var fuel=truck.Fuel;
        for(var i=0;i<3600&&!shipment.Complete;i++){sim.Advance(2);if(i%100==0)sim.CheckInvariants();}
        Assert.True(shipment.Complete,truck.Cause);Assert.False(shipment.Cancelled);
        Assert.Equal(cargo,sister.Quantity(ResourceKind.Food),6);Assert.Equal(0,other.Quantity(ResourceKind.Food));Assert.Equal(0,depot.Stock.Values.Sum());
        Assert.Equal("company:900",shipment.GoodsOwnerAccount);Assert.True(truck.Fuel<fuel);
        // The titular also owns the demolished shop: only its indemnity moves, once.
        Assert.Equal(titular+quote.Compensation,sim.Balance(company.Account));Assert.Equal(ledger+1,sim.State.Ledger.Count(m=>m.From==company.Account||m.To==company.Account));
        Assert.Equal(total,sim.MonetaryTotal);sim.CheckInvariants();
    }

    [Fact] public void Without_apt_destination_cargo_waits_off_the_lanes_survives_save_load_and_resumes_when_room_appears()
    {
        var sim=Street();
        var company=AddCompany(sim,900,5000);var rival=AddCompany(sim,901,0);
        var target=Shop(sim,company,new(-20,14));
        // Same company, but full, and another one without road access: neither is apt.
        var full=Shop(sim,company,new(60,14));full.Stock[ResourceKind.Food]=full.Definition.Capacity;
        var isolated=Shop(sim,company,new(220,220));
        var other=Shop(sim,rival,new(20,14));
        var depot=new Building{Id=sim.State.NextId++,Kind=BuildingKind.WorksDepot,Position=new(-60,14),Completed=true,OperationalCapacity=1};sim.State.Buildings.Add(depot);
        var (truck,shipment)=Import(sim,target,30);var cargo=truck.Cargo[ResourceKind.Food];
        TravelSome(sim,truck);
        var quote=sim.QuoteBuildingDemolition(target.Id);Assert.Null(quote.Shipments.Single().AlternativeDestination);
        var titular=sim.Balance(company.Account);var total=sim.MonetaryTotal;
        Assert.True(sim.DemolishBuilding(target.Id,quote).Success);
        for(var i=0;i<3600&&!(truck.Status==VehicleStatus.Waiting&&truck.HoldingPosition is {} h&&truck.Position.Distance(h)<.1);i++)sim.Advance(2);
        Assert.Equal(VehicleStatus.Waiting,truck.Status);Assert.NotNull(truck.HoldingPosition);Assert.Contains("fora das faixas",truck.Cause);
        // The waiting spot is beside the road, outside the carriageway.
        var nearest=sim.State.Roads.MinBy(r=>r.Position.Distance(truck.Position))!;Assert.True(nearest.Position.Distance(truck.Position)>=sim.State.Tuning.RoadWidth/2);
        Assert.False(shipment.Complete);Assert.False(shipment.Cancelled);Assert.Equal((int?)target.Id,shipment.DestinationBuildingId);
        Assert.Equal(cargo,truck.Cargo[ResourceKind.Food]);Assert.Equal(cargo,shipment.Cargo[ResourceKind.Food]);Assert.Equal("company:900",shipment.GoodsOwnerAccount);
        Assert.Equal(0,other.Quantity(ResourceKind.Food));Assert.Equal(0,depot.Stock.Values.Sum());Assert.Equal(0,isolated.Quantity(ResourceKind.Food));
        Assert.Equal(titular+quote.Compensation,sim.Balance(company.Account));
        // Waiting persists across save/load with body, cargo and titular.
        var loaded=CitySimulation.FromJson(sim.SaveToJson());
        var held=loaded.State.Vehicles.Single(v=>v.Id==truck.Id);var heldShipment=loaded.State.Shipments.Single(s=>s.Id==shipment.Id);
        Assert.Equal(truck.Position,held.Position);Assert.Equal(truck.HoldingPosition,held.HoldingPosition);Assert.Equal(cargo,held.Cargo[ResourceKind.Food]);Assert.Equal("company:900",heldShipment.GoodsOwnerAccount);
        loaded.Advance(loaded.State.Tuning.EconomyInterval+10);
        Assert.Equal(VehicleStatus.Waiting,held.Status);Assert.Equal(truck.Position,held.Position);
        // Real room appears in the same company's shop: the next economy review resumes delivery.
        var room=loaded.State.Buildings.Single(b=>b.Id==full.Id);room.Stock[ResourceKind.Food]=0;
        for(var i=0;i<4000&&!heldShipment.Complete;i++){loaded.Advance(2);if(i%200==0)loaded.CheckInvariants();}
        Assert.True(heldShipment.Complete,held.Cause);Assert.Equal(cargo,room.Quantity(ResourceKind.Food),6);
        Assert.Equal(0,loaded.State.Buildings.Single(b=>b.Id==depot.Id).Stock.Values.Sum());
        Assert.Equal(titular+quote.Compensation,loaded.Balance(company.Account));Assert.Equal(total,loaded.MonetaryTotal);loaded.CheckInvariants();
    }

    // Uma casa vaga, uma família deslocada e um migrante que podem pagar: a família deslocada compra primeiro.
    static (CitySimulation Sim,Building House,Family Family,Building Vacant) DisplacedAndVacant()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(100,0)).Success);
        sim.Advance(7200);
        var placed=sim.PlaceBuilding(BuildingKind.House,new(-100,14));Assert.True(placed.Success,placed.Message);
        var house=sim.State.Buildings.Single(b=>b.Id==placed.Id);
        for(var i=0;i<96&&!(house.Completed&&sim.State.Families.Any(f=>f.HomeId==house.Id));i++)sim.Advance(1800);
        var family=sim.State.Families.Single(f=>f.HomeId==house.Id);
        var waiting=sim.State.FamilyCandidates.Where(c=>!c.Migrated).ToList();foreach(var c in waiting)c.Migrated=true;
        var second=sim.PlaceBuilding(BuildingKind.House,new(-60,14));Assert.True(second.Success,second.Message);var vacant=sim.State.Buildings.Single(b=>b.Id==second.Id);
        for(var i=0;i<96&&!vacant.Completed;i++)sim.Advance(1800);Assert.True(vacant.Completed);Assert.Null(vacant.OwnerAccount);
        foreach(var c in waiting)c.Migrated=false;
        return (sim,house,family,vacant);
    }
    static ExternalFamilyCandidate EligibleMigrant(CitySimulation sim,Building vacant)
    {
        var migrant=sim.State.FamilyCandidates.First(c=>!c.Migrated);migrant.Capital=sim.MarketValue(vacant)+5000;migrant.RiskTolerance=1;
        foreach(var c in sim.State.FamilyCandidates.Where(c=>c.Id!=migrant.Id))c.Migrated=true;
        return migrant;
    }
    // Moves the family's money aside (to the Global Reserve and back) so it cannot buy at confirmation.
    static decimal Park(CitySimulation sim,Family family)
    {
        decimal parked=0;foreach(var id in family.Members){var account="sim:"+id;var money=sim.State.Wallets[account];sim.State.Wallets[account]=0;sim.State.Wallets["global"]+=money;parked+=money;}
        return parked;
    }
    static void Restore(CitySimulation sim,Family family,decimal money){var account="sim:"+family.Members[0];sim.State.Wallets["global"]-=money;sim.State.Wallets[account]+=money;}
    static void UntilEconomyReview(CitySimulation sim){var next=sim.State.EconomyNextAt;for(var i=0;i<4000&&sim.State.EconomyNextAt<=next;i++)sim.Advance(1);}

    [Fact] public void Displaced_family_buys_the_only_vacant_house_before_an_eligible_migrant_in_the_same_review()
    {
        var (sim,house,family,vacant)=DisplacedAndVacant();var migrant=EligibleMigrant(sim,vacant);
        var quote=sim.QuoteBuildingDemolition(house.Id);
        var money=Park(sim,family);var families=sim.State.Families.Count;
        Assert.True(sim.DemolishBuilding(house.Id,quote).Success);Assert.Null(vacant.OwnerAccount);
        // Only now the family has its own money again; the market review decides.
        money+=Park(sim,family);Restore(sim,family,money);
        var price=sim.MarketValue(vacant);var total=sim.MonetaryTotal;
        UntilEconomyReview(sim);
        Assert.Equal(family.Id,vacant.FamilyId);Assert.Equal(vacant.Id,family.HomeId);Assert.Contains(family.Members,id=>vacant.OwnerAccount=="sim:"+id);
        Assert.False(migrant.Migrated);Assert.Equal(families,sim.State.Families.Count);
        Assert.Contains(sim.State.Ledger,m=>m.Category=="Aquisição residencial"&&m.EntityId==vacant.Id&&m.From==vacant.OwnerAccount&&m.To=="global"&&m.Amount>=price*.9m);
        Assert.Equal(total,sim.MonetaryTotal);sim.CheckInvariants();
    }

    [Fact] public void Displaced_family_that_cannot_pay_gets_no_reservation_and_the_migrant_buys_the_house()
    {
        var (sim,house,family,vacant)=DisplacedAndVacant();var migrant=EligibleMigrant(sim,vacant);
        var quote=sim.QuoteBuildingDemolition(house.Id);
        Park(sim,family);
        Assert.True(sim.DemolishBuilding(house.Id,quote).Success);
        // The owner got the indemnity at confirmation; move it aside too so the family really cannot pay.
        Park(sim,family);
        UntilEconomyReview(sim);
        Assert.True(migrant.Migrated);Assert.NotNull(vacant.FamilyId);Assert.NotEqual(family.Id,vacant.FamilyId);Assert.Equal(house.Id,family.HomeId);
        sim.CheckInvariants();
    }
}
