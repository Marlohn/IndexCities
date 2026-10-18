namespace IndexCities.Simulation;

// Orçamento SOMENTE LEITURA para mover um prédio municipal concluído (Prefeitura
// ou serviço municipal). Não executa nada: não escreve, não cancela obra, não
// muda CityHallPlaced e não faz clone/Save temporário do estado. WorkerIds e
// Stock são snapshots de leitura; Construction é a cotação de obra comum no
// destino. A retirada do endereço antigo é cobrada pela área, sem indenização;
// o frete estimado leva o estoque existente ao novo endereço por caminhão real.
public sealed record MunicipalRelocationQuote(int SourceBuildingId,bool Allowed,PlacementQuote? Construction,decimal RemovalCost,decimal TotalCost,List<int> WorkerIds,Dictionary<ResourceKind,double> Stock,double CurrentCapacity,string Message)
{
    public decimal TransferEstimate {get;init;}
    public bool DestinationValid {get;init;}
    public double StockBeyondDestination {get;init;}
    public int PendingPickups {get;init;}
    public int WorksWithLocalReservation {get;init;}
}

public sealed partial class CitySimulation
{
    public MunicipalRelocationQuote QuoteMunicipalRelocation(int sourceBuildingId,Point destination,double rotation=0)
    {
        var source=State.Buildings.FirstOrDefault(b=>b.Id==sourceBuildingId);
        if(source==null)return RejectRelocation(sourceBuildingId,"Prédio inexistente");
        if(source.Removed)return RejectRelocation(sourceBuildingId,"Prédio já removido; não há sede a mudar");
        if(!source.Completed)return RejectRelocation(sourceBuildingId,"Obra incompleta: use Mover pelas regras de obra inacabada");
        if(source.Definition.Private)return RejectRelocation(sourceBuildingId,"Prédio privado: realocação adiada para reavaliação; use Demolir");
        if(State.MunicipalRelocations.FirstOrDefault(r=>r.Active&&(r.SourceBuildingId==source.Id||r.TargetBuildingId==source.Id)) is {} running)return RejectRelocation(sourceBuildingId,"Mudança #"+running.Id+" de "+source.Definition.Name+" já em andamento; acompanhe a obra substituta");
        // A estrutura antiga permanece fisicamente no local durante a obra da
        // substituta. A cotação ignora só o próprio projeto da origem para não
        // acusar duplicidade (inclusive de Prefeitura); a sobreposição com essa
        // estrutura é recusada explicitamente abaixo.
        var placement=QuoteBuilding(source.Kind,destination,rotation,source.ProjectId);
        var candidate=new Building{Kind=source.Kind,Position=destination,Rotation=rotation};
        var removalCost=Money(Math.Max(0,State.Tuning.RoadDemolitionCostPerSquareMetre)*(decimal)(source.Definition.Width*source.Definition.Depth));
        var workers=State.Citizens.Where(c=>c.Alive&&c.WorkplaceId==source.Id).Select(c=>c.Id).ToList();
        var stock=source.Stock.Where(entry=>entry.Value>0).ToDictionary(entry=>entry.Key,entry=>entry.Value);
        var transfer=StockTransferEstimate(source,destination,stock);
        var totalCost=Money(removalCost+placement.Total+transfer);
        var capacity=source.OperationalCapacity;
        var beyond=Math.Max(0,stock.Values.Sum()-source.Definition.Capacity);
        var pickups=State.Shipments.Count(s=>!s.Complete&&!s.PickedUp&&s.SourceBuildingId==source.Id);
        var reservations=IsActiveDepot(source)?State.Projects.Count(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress&&p.LocalReserved.Values.Any(v=>v>.001)):0;
        var overlap=Overlap(candidate,source);
        MunicipalRelocationQuote Result(bool allowed,string message)=>new(sourceBuildingId,allowed,placement,removalCost,totalCost,workers,stock,capacity,message){TransferEstimate=transfer,DestinationValid=placement.PhysicallyValid&&!overlap,StockBeyondDestination=beyond,PendingPickups=pickups,WorksWithLocalReservation=reservations};
        if(overlap)return Result(false,$"O destino fica dentro do próprio prédio existente ({source.Definition.Name} #{source.Id}): a estrutura antiga permanece no local até a retirada; escolha um endereço livre");
        if(!placement.PhysicallyValid)return Result(false,placement.Message);
        if(totalCost>Available)return Result(false,"Caixa livre insuficiente: total "+Cash(totalCost)+" (obra "+Cash(placement.Total)+" + retirada "+Cash(removalCost)+(transfer>0?" + frete do estoque "+Cash(transfer):"")+") e disponível "+Cash(Available));
        return Result(true,DescribeRelocation(source,placement,removalCost,transfer,totalCost,workers,stock,beyond,pickups,reservations));
    }
    static MunicipalRelocationQuote RejectRelocation(int sourceBuildingId,string message)=>new(sourceBuildingId,false,null,0,0,[],new(),0,message);
    bool IsActiveDepot(Building building)=>building.Kind==BuildingKind.WorksDepot&&State.Buildings.FirstOrDefault(b=>b.Kind==BuildingKind.WorksDepot&&b.Completed&&!b.Removed&&!b.OperationClosed)?.Id==building.Id;
    // One external truck trip per load, as the existing municipal transport:
    // entrance → old address (pickup) → new address, freight and a full tank.
    decimal StockTransferEstimate(Building source,Point destination,Dictionary<ResourceKind,double> stock)
    {
        if(stock.Count==0)return 0;
        static double Manhattan(Point a,Point b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Z-b.Z);
        var trips=stock.Values.Sum(quantity=>(int)Math.Ceiling(quantity/150));
        var travel=Manhattan(State.Entry,source.Position)+Manhattan(source.Position,destination);
        return Money(trips*(State.Tuning.FreightBase+(decimal)(travel*2)*State.Tuning.FreightPerMetre+State.Tuning.ImportPrices[ResourceKind.Fuel]*(decimal)State.Tuning.FuelTank));
    }
    string DescribeRelocation(Building source,PlacementQuote placement,decimal removalCost,decimal transfer,decimal totalCost,List<int> workers,Dictionary<ResourceKind,double> stock,double beyond,int pickups,int reservations)
    {
        var parts=new List<string>{"Nova obra de "+source.Definition.Name+" no destino ("+Cash(placement.Total)+") mais a retirada do endereço antigo ("+Cash(removalCost)+")"+(transfer>0?" e o frete estimado do estoque ("+Cash(transfer)+")":"")+": total "+Cash(totalCost)+" comprometido do Caixa, sem indenização por patrimônio municipal"};
        if(!placement.Accessible)parts.Add("o destino é fisicamente válido mas ainda sem acesso viário, como qualquer obra comum que aguarda rua");
        parts.Add(source.Kind==BuildingKind.CityHall?"a Prefeitura mantém as ferramentas de construção e gestão durante a transição":"o prédio antigo continua operando enquanto viável; com a nova estrutura concluída e fisicamente apta, o endereço de trabalho muda de fato e o antigo fica sem equipe, sem capacidade em dobro");
        parts.Add(workers.Count==0?"nenhum emprego vinculado neste momento":workers.Count+" funcionário(s) passam ao novo endereço e reavaliam o emprego pelo novo trajeto; a capacidade acompanha a chegada física deles e cai para quem sair");
        if(stock.Count>0)parts.Add("o estoque existente não é teletransportado: segue por caminhão real ao novo endereço depois da conclusão"+(beyond>0?"; "+beyond.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" acima da capacidade do destino não cabe nele":"")+"; após "+(MunicipalStockRecoveryDeadline/3600).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" h sem progresso por falta de espaço ou rota, a retirada baixa o estoque residual como perda; caminhão ocupado e falta temporária de caixa/combustível pausam o prazo, carregamento real o reinicia; causa, prazo e perda ficam na inspeção");
        if(reservations>0)parts.Add(reservations+" obra(s) com material reservado neste Pátio passam a reservar no novo endereço conforme o estoque chegar");
        if(pickups>0)parts.Add(pickups+" carregamento(s) já a caminho continuam");
        return string.Join("; ",parts)+".";
    }
}
