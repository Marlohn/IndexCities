using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

public sealed class MunicipalRelocationQuoteTests
{
    // Fixture sintética explícita: coloca uma Prefeitura real (libera obras no
    // QuoteBuilding) e injeta prédios municipais concluídos direto no estado, com
    // ProjectId real e único. Não simula obra executada nem partida completa;
    // exercita só o orçamento de leitura. O destino -168,14 é fisicamente válido
    // e fica sem rua pronta nesta cidade, então Accessible=false.
    static readonly Point ValidDestination=new(-168,14);
    static CitySimulation City()
    {
        var sim=new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);
        return sim;
    }
    static Building AddMunicipal(CitySimulation sim,BuildingKind kind,Point position,double rotation=0)
    {
        var building=new Building{Id=sim.State.NextId++,Kind=kind,Position=position,Rotation=rotation,ProjectId=sim.State.NextId++,Completed=true,OwnerAccount="city"};
        sim.State.Buildings.Add(building);return building;
    }
    static void AddWorker(CitySimulation sim,int id,int buildingId)
    {
        sim.State.Citizens.Add(new Citizen{Id=id,Name="SIM "+id,Age=30,Alive=true,WorkplaceId=buildingId});
    }

    [Fact] public void Completed_municipal_depot_is_allowed_at_a_valid_destination()
    {
        var sim=City();var source=AddMunicipal(sim,BuildingKind.WorksDepot,new(220,220));
        AddWorker(sim,901,source.Id);source.Stock[ResourceKind.Wood]=4;source.Stock[ResourceKind.Steel]=0;source.OperationalCapacity=150;
        var before=sim.SaveToJson();
        var quote=sim.QuoteMunicipalRelocation(source.Id,ValidDestination);
        Assert.True(quote.Allowed,quote.Message);
        Assert.NotNull(quote.Construction);Assert.True(quote.Construction!.PhysicallyValid);
        Assert.False(quote.Construction.Accessible);
        Assert.Equal(792m,quote.RemovalCost);
        Assert.Equal(quote.RemovalCost+quote.Construction.Total,quote.TotalCost);
        Assert.Equal(new List<int>{901},quote.WorkerIds);
        Assert.True(quote.Stock.ContainsKey(ResourceKind.Wood));Assert.False(quote.Stock.ContainsKey(ResourceKind.Steel));
        Assert.Equal(150d,quote.CurrentCapacity);
        Assert.Contains("retirada",quote.Message);Assert.Contains("acesso",quote.Message);Assert.Contains("teletransport",quote.Message);
        Assert.Equal(before,sim.SaveToJson());
    }

    [Fact] public void Completed_city_hall_can_move_without_tripping_the_single_hall_rule()
    {
        var sim=City();var hall=sim.State.Buildings.Single(b=>b.Kind==BuildingKind.CityHall&&!b.Removed);hall.Completed=true;
        var quote=sim.QuoteMunicipalRelocation(hall.Id,ValidDestination);
        Assert.True(quote.Allowed,quote.Message);
        Assert.DoesNotContain("já tem uma Prefeitura",quote.Message);
        Assert.Contains("Prefeitura",quote.Message);Assert.Contains("gestão",quote.Message);
        Assert.NotNull(quote.Construction);
    }

    [Fact] public void Private_incomplete_removed_and_unknown_targets_are_rejected()
    {
        var sim=City();
        var house=AddMunicipal(sim,BuildingKind.House,new(220,220));house.OwnerAccount="sim:777";
        var incomplete=AddMunicipal(sim,BuildingKind.WorksDepot,new(230,40));incomplete.Completed=false;
        var removed=AddMunicipal(sim,BuildingKind.WorksDepot,new(-220,220));removed.Removed=true;
        foreach(var id in new[]{house.Id,incomplete.Id,removed.Id,999999})
        {
            var quote=sim.QuoteMunicipalRelocation(id,ValidDestination);
            Assert.False(quote.Allowed);Assert.Null(quote.Construction);Assert.Equal(0m,quote.TotalCost);
            Assert.Empty(quote.WorkerIds);Assert.Empty(quote.Stock);Assert.Equal(0d,quote.CurrentCapacity);
        }
    }

    [Fact] public void Destination_inside_the_source_structure_is_rejected()
    {
        var sim=City();var source=AddMunicipal(sim,BuildingKind.WorksDepot,new(220,220));
        var quote=sim.QuoteMunicipalRelocation(source.Id,source.Position);
        Assert.False(quote.Allowed);Assert.Contains("dentro do próprio prédio",quote.Message);
    }

    [Fact] public void Other_buildings_water_highway_and_out_of_map_destinations_are_rejected()
    {
        var sim=City();var source=AddMunicipal(sim,BuildingKind.WorksDepot,new(220,220));
        AddMunicipal(sim,BuildingKind.WorksDepot,ValidDestination);
        Assert.Contains("Sobreposição",sim.QuoteMunicipalRelocation(source.Id,ValidDestination).Message);
        Assert.Contains("água",sim.QuoteMunicipalRelocation(source.Id,sim.State.LakeCentre).Message);
        Assert.Contains("via",sim.QuoteMunicipalRelocation(source.Id,new(-224,0)).Message);
        Assert.Contains("limite do mapa",sim.QuoteMunicipalRelocation(source.Id,new(-255,40)).Message);
    }

    [Fact] public void Insufficient_free_cash_rejects_considering_construction_and_removal()
    {
        var sim=City();var source=AddMunicipal(sim,BuildingKind.WorksDepot,new(220,220));
        // CityBalance acompanha Committed: Available zera sem violar invariantes.
        sim.State.Wallets["global"]+=sim.CityBalance-sim.Committed;sim.State.Wallets["city"]=sim.Committed;
        var before=sim.SaveToJson();
        var quote=sim.QuoteMunicipalRelocation(source.Id,ValidDestination);
        Assert.False(quote.Allowed);Assert.True(quote.TotalCost>0);Assert.Contains("insuficiente",quote.Message);Assert.Contains("retirada",quote.Message);
        Assert.Equal(before,sim.SaveToJson());
    }

    [Fact] public void Quote_is_read_only_for_save_workers_stock_and_wallets()
    {
        var sim=City();var source=AddMunicipal(sim,BuildingKind.WorksDepot,new(220,220));
        AddWorker(sim,902,source.Id);source.Stock[ResourceKind.Wood]=3;
        var before=sim.SaveToJson();var wallets=sim.State.Wallets.ToDictionary(e=>e.Key,e=>e.Value);var balance=sim.CityBalance;
        var allowed=sim.QuoteMunicipalRelocation(source.Id,ValidDestination);
        var water=sim.QuoteMunicipalRelocation(source.Id,sim.State.LakeCentre);
        var inside=sim.QuoteMunicipalRelocation(source.Id,source.Position);
        var unknown=sim.QuoteMunicipalRelocation(999999,ValidDestination);
        Assert.True(allowed.Allowed);Assert.False(water.Allowed);Assert.False(inside.Allowed);Assert.False(unknown.Allowed);
        Assert.Equal(before,sim.SaveToJson());Assert.Equal(wallets.Count,sim.State.Wallets.Count);
        Assert.All(wallets,e=>Assert.Equal(e.Value,sim.State.Wallets[e.Key]));Assert.Equal(balance,sim.CityBalance);
    }
}
