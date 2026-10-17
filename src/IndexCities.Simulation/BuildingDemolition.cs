namespace IndexCities.Simulation;

public sealed record BuildingDemolitionQuote(int BuildingId,bool Allowed,decimal RemovalCost,decimal Compensation,decimal TotalCost,string? OwnerAccount,string OwnerName,List<int> ResidentIds,List<int> WorkerIds,Dictionary<ResourceKind,double> Stock,double OperationalCapacity,bool WaitsForResidents,string Message)
{
    public List<int> FamilyIds {get;init;}=[];
}

public sealed partial class CitySimulation
{
    public BuildingDemolitionQuote QuoteBuildingDemolition(int buildingId)
    {
        var building=State.Buildings.FirstOrDefault(b=>b.Id==buildingId);
        if(building==null)return Reject(buildingId,"Prédio inexistente");
        if(building.Removed)return Reject(buildingId,"Prédio já removido; não há o que demolir");
        if(!building.Completed)return Reject(buildingId,"Obra incompleta: use Mover ou Cancelar pelas regras vigentes");
        if(building.Kind==BuildingKind.CityHall)return Reject(buildingId,"A Prefeitura concluída oferece somente Mover sede");
        if(building.Demolition is {} order)return Reject(buildingId,"Demolição já confirmada"+(order.CompensationPaid>0?" e indenização paga":"")+"; aguardando a desocupação, sem nova cobrança");
        var families=State.Families.Where(f=>f.HomeId==building.Id).ToList();var members=families.SelectMany(f=>f.Members).ToHashSet();
        var residents=State.Citizens.Where(c=>c.Alive&&c.Resident&&members.Contains(c.Id)).Select(c=>c.Id).ToList();
        var workers=State.Citizens.Where(c=>c.Alive&&c.WorkplaceId==building.Id).Select(c=>c.Id).ToList();
        var stock=building.Stock.Where(entry=>entry.Value>0).ToDictionary(entry=>entry.Key,entry=>entry.Value);
        var ownerAccount=building.OwnerAccount;
        var realOwner=HasRealEconomicOwner(ownerAccount);
        var ownerName=realOwner?ReadableOwnerName(ownerAccount):"";
        var compensation=building.Definition.Private&&realOwner?MarketValue(building):0m;
        var removalCost=Money(Math.Max(0,State.Tuning.RoadDemolitionCostPerSquareMetre)*(decimal)(building.Definition.Width*building.Definition.Depth));
        var totalCost=Money(removalCost+compensation);
        var quote=new BuildingDemolitionQuote(buildingId,true,removalCost,compensation,totalCost,ownerAccount,ownerName,residents,workers,stock,building.OperationalCapacity,residents.Count>0,Describe(building,removalCost,compensation,totalCost,ownerName,residents,workers,stock)){FamilyIds=families.Where(f=>f.Members.Any(residents.Contains)).Select(f=>f.Id).ToList()};
        // A real owner whose wallet cannot receive the money blocks the whole order: nothing is charged or removed.
        if(compensation>0&&!State.Wallets.ContainsKey(ownerAccount!))return quote with{Allowed=false,Message="Conta do titular "+ownerName+" indisponível para receber a indenização; nada será cobrado nem removido"};
        return totalCost>Available?quote with{Allowed=false,Message="Caixa livre insuficiente para a demolição: total "+Cash(totalCost)+" e disponível "+Cash(Available)}:quote;
    }
    static BuildingDemolitionQuote Reject(int buildingId,string message)=>new(buildingId,false,0,0,0,null,"",[],[],new(),0,false,message);
    bool HasRealEconomicOwner(string? account)=>account switch
    {
        var a when a is not null&&a.StartsWith("sim:")=>State.Citizens.Any(c=>c.Account==a),
        var a when a is not null&&a.StartsWith("company:")=>State.Companies.Any(c=>c.Account==a),
        _=>false,
    };
    string Cash(decimal value)=>value.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
    string ReadableOwnerName(string? account)=>account switch
    {
        null=>"",
        var a when a.StartsWith("sim:")=>State.Citizens.FirstOrDefault(c=>c.Account==a)?.Name??a,
        var a when a.StartsWith("company:")=>State.Companies.FirstOrDefault(c=>c.Account==a)?.Name??a,
        var a=>a,
    };
    string Describe(Building building,decimal removalCost,decimal compensation,decimal totalCost,string ownerName,List<int> residents,List<int> workers,Dictionary<ResourceKind,double> stock)
    {
        var parts=new List<string>{"Remoção "+Cash(removalCost)};
        if(compensation>0)parts.Add("indenização de 100% do valor de mercado ("+Cash(compensation)+") a "+ownerName);
        else if(building.Definition.Private)parts.Add("sem proprietário econômico: sem indenização à Reserva");
        else parts.Add("prédio municipal: sem indenização");
        parts.Add("total "+Cash(totalCost)+" do Caixa");
        if(residents.Count>0)parts.Add(residents.Count+" morador(es) aguardam desocupação assistida de até "+VacatingDaysText+" antes da retirada, procurando casa existente à venda que possam pagar");
        if(workers.Count>0)parts.Add(workers.Count+" emprego(s) real(is) afetado(s)");
        if(stock.Count>0)parts.Add("estoque existente no prédio");
        return string.Join("; ",parts)+". A demolição retira o prédio definitivamente, sem reembolso de obra ou materiais e sem garantia de nova moradia.";
    }
}
