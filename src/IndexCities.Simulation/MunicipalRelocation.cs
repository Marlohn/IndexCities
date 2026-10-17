namespace IndexCities.Simulation;

// Orçamento SOMENTE LEITURA para mover um prédio municipal concluído (Prefeitura
// ou serviço municipal). Não executa nada: não escreve, não cancela obra, não
// muda CityHallPlaced e não faz clone/Save temporário do estado. WorkerIds e
// Stock são snapshots de leitura; Construction é a cotação de obra comum no
// destino. A retirada do endereço antigo é cobrada pela área, sem indenização.
public sealed record MunicipalRelocationQuote(int SourceBuildingId,bool Allowed,PlacementQuote? Construction,decimal RemovalCost,decimal TotalCost,List<int> WorkerIds,Dictionary<ResourceKind,double> Stock,double CurrentCapacity,string Message);

public sealed partial class CitySimulation
{
    public MunicipalRelocationQuote QuoteMunicipalRelocation(int sourceBuildingId,Point destination,double rotation=0)
    {
        var source=State.Buildings.FirstOrDefault(b=>b.Id==sourceBuildingId);
        if(source==null)return RejectRelocation(sourceBuildingId,"Prédio inexistente");
        if(source.Removed)return RejectRelocation(sourceBuildingId,"Prédio já removido; não há sede a mudar");
        if(!source.Completed)return RejectRelocation(sourceBuildingId,"Obra incompleta: use Mover pelas regras de obra inacabada");
        if(source.Definition.Private)return RejectRelocation(sourceBuildingId,"Prédio privado: realocação adiada para reavaliação; use Demolir");
        // A estrutura antiga permanece fisicamente no local durante a obra da
        // substituta. A cotação ignora só o próprio projeto da origem para não
        // acusar duplicidade (inclusive de Prefeitura); a sobreposição com essa
        // estrutura é recusada explicitamente abaixo.
        var placement=QuoteBuilding(source.Kind,destination,rotation,source.ProjectId);
        var candidate=new Building{Kind=source.Kind,Position=destination,Rotation=rotation};
        var removalCost=Money(Math.Max(0,State.Tuning.RoadDemolitionCostPerSquareMetre)*(decimal)(source.Definition.Width*source.Definition.Depth));
        var totalCost=Money(removalCost+placement.Total);
        var workers=State.Citizens.Where(c=>c.Alive&&c.WorkplaceId==source.Id).Select(c=>c.Id).ToList();
        var stock=source.Stock.Where(entry=>entry.Value>0).ToDictionary(entry=>entry.Key,entry=>entry.Value);
        var capacity=source.OperationalCapacity;
        if(Overlap(candidate,source))return new(sourceBuildingId,false,placement,removalCost,totalCost,workers,stock,capacity,$"O destino fica dentro do próprio prédio existente ({source.Definition.Name} #{source.Id}): a estrutura antiga permanece no local até a retirada; escolha um endereço livre");
        if(!placement.PhysicallyValid)return new(sourceBuildingId,false,placement,removalCost,totalCost,workers,stock,capacity,placement.Message);
        if(totalCost>Available)return new(sourceBuildingId,false,placement,removalCost,totalCost,workers,stock,capacity,"Caixa livre insuficiente: total "+Cash(totalCost)+" (obra "+Cash(placement.Total)+" + retirada "+Cash(removalCost)+") e disponível "+Cash(Available));
        return new(sourceBuildingId,true,placement,removalCost,totalCost,workers,stock,capacity,Describe(source,placement,removalCost,totalCost,workers,stock));
    }
    static MunicipalRelocationQuote RejectRelocation(int sourceBuildingId,string message)=>new(sourceBuildingId,false,null,0,0,[],new(),0,message);
    string Describe(Building source,PlacementQuote placement,decimal removalCost,decimal totalCost,List<int> workers,Dictionary<ResourceKind,double> stock)
    {
        var parts=new List<string>{"Nova obra de "+source.Definition.Name+" no destino ("+Cash(placement.Total)+") mais a retirada do endereço antigo ("+Cash(removalCost)+"): total "+Cash(totalCost)+" do Caixa, sem indenização por patrimônio municipal"};
        if(!placement.Accessible)parts.Add("o destino é fisicamente válido mas ainda sem acesso viário, como qualquer obra comum que aguarda rua");
        parts.Add(workers.Count==0?"nenhum emprego vinculado neste momento":workers.Count+" emprego(s) real(is) podem ser interrompidos até a nova estrutura operar");
        parts.Add("capacidade atual "+source.OperationalCapacity.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" e "+(stock.Count>0?"o estoque existente":"o estoque atual")+" não são teletransportados: seguem transporte físico possível, com perda residual real");
        parts.Add(source.Kind==BuildingKind.CityHall?"a Prefeitura mantém as ferramentas de construção e gestão durante a transição":"a operação antiga pode cessar quando a estrutura ou o acesso precisar ser liberado");
        return string.Join("; ",parts)+".";
    }
}
