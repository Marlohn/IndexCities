namespace IndexCities.Simulation;

// Execução de Mover prédio municipal concluído (SPEC construcao: Mudança de
// prédios municipais concluídos). A substituta é uma obra comum no destino; o
// prédio antigo opera até ela ficar pronta e fisicamente apta. Então os funcionários passam ao novo
// endereço e reavaliam o vínculo, o estoque segue por caminhão real e o endereço
// antigo é retirado pagando a remoção reservada. A obra substituta conserva Mover
// e Cancelar das obras inacabadas; cancelar encerra a mudança sem estorno.
public sealed partial class CitySimulation
{
    public const string StaleRelocationPreview="Os dados da mudança mudaram enquanto a cidade avançava; confira a prévia atualizada antes de confirmar";
    // Calibração do prazo curto aprovado: um dia de bloqueio real por espaço/rota.
    public const double MunicipalStockRecoveryDeadline=86400;
    public MunicipalRelocationOrder? MunicipalRelocationOf(int buildingId)=>State.MunicipalRelocations.LastOrDefault(r=>r.SourceBuildingId==buildingId||r.TargetBuildingId==buildingId);
    MunicipalRelocationOrder? ActiveRelocationForProject(int projectId)=>State.MunicipalRelocations.FirstOrDefault(r=>r.Active&&r.TargetProjectId==projectId);
    // Retirada e frete ainda não pagos que Cancelar a obra substituta libera.
    public decimal RelocationReservedFor(int projectId)=>ActiveRelocationForProject(projectId) is {} order?order.RemovalReserved+order.TransferReserved:0;

    public CommandResult RelocateMunicipalBuilding(int sourceBuildingId,Point destination,double rotation=0,MunicipalRelocationQuote? reviewed=null)
    {
        var quote=QuoteMunicipalRelocation(sourceBuildingId,destination,rotation);
        if(!quote.Allowed)return new(false,quote.Message);
        if(reviewed!=null&&!SameRelocationImpact(reviewed,quote))return new(false,StaleRelocationPreview);
        var source=BuildingById(sourceBuildingId)!;
        // The quote validated destination and free cash for construction plus
        // the reserves; PlaceBuilding stops before any mutation if that changed.
        var placed=PlaceBuilding(source.Kind,destination,rotation,source.ProjectId);
        if(!placed.Success)return placed;
        var target=BuildingById(placed.Id)!;
        target.OperationClosed=true;
        var order=new MunicipalRelocationOrder{Id=Id(),Kind=source.Kind,SourceBuildingId=source.Id,TargetBuildingId=target.Id,TargetProjectId=target.ProjectId,OrderedAt=State.Now,RemovalReserved=quote.RemovalCost,TransferReserved=quote.TransferEstimate,Cause="Obra substituta aguardando execução; "+source.Definition.Name+" segue operando no endereço antigo"};
        State.MunicipalRelocations.Add(order);
        Event("Mudança de "+source.Definition.Name+" confirmada: obra no novo endereço ("+Cash(quote.Construction!.Total)+") e retirada"+(quote.TransferEstimate>0?" e frete do estoque":"")+" ("+Cash(quote.RemovalCost+quote.TransferEstimate)+") comprometidos; o prédio antigo continua operando até a conclusão");
        return new(true,State.LastMessage,target.Id);
    }
    // Revalidation before committing money: costs, access, people and stock must
    // match what the player reviewed. Stock tolerates the drift of normal use.
    static bool SameRelocationImpact(MunicipalRelocationQuote a,MunicipalRelocationQuote b)=>a.SourceBuildingId==b.SourceBuildingId&&a.Allowed==b.Allowed&&a.TotalCost==b.TotalCost&&a.RemovalCost==b.RemovalCost&&a.TransferEstimate==b.TransferEstimate
        &&a.Construction?.Total==b.Construction?.Total&&a.Construction?.Accessible==b.Construction?.Accessible&&a.WorkerIds.Order().SequenceEqual(b.WorkerIds.Order())
        &&a.Stock.Count==b.Stock.Count&&a.Stock.All(entry=>b.Stock.TryGetValue(entry.Key,out var other)&&Math.Abs(entry.Value-other)<=Math.Max(1,entry.Value*.05));

    void ProgressMunicipalRelocations(double dt)
    {
        if(!State.MunicipalRelocations.Any(r=>r.Active))return;
        foreach(var order in State.MunicipalRelocations.Where(r=>r.Active).ToList()){
            var source=BuildingById(order.SourceBuildingId);var target=BuildingById(order.TargetBuildingId);var project=State.Projects.FirstOrDefault(p=>p.Id==order.TargetProjectId);
            if(order.Stage==MunicipalRelocationStage.Constructing){
                if(target==null||project==null||project.Status==WorkStatus.Cancelled){CloseRelocationAfterCancel(order);continue;}
                target.OperationClosed=true;
                if(!target.Completed){order.Cause="Obra substituta "+project.Progress.ToString("P0",System.Globalization.CultureInfo.InvariantCulture)+": "+project.Cause+(source==null?"; o endereço antigo já não existe":"; "+source.Definition.Name+" segue operando no endereço antigo");continue;}
                if(!HasExteriorAccess(target)||target.MaintenanceRatio<=.05||target.Kind==BuildingKind.WaterIntake&&!HasNaturalWaterSource(target)){
                    order.Cause="Obra substituta concluída, mas ainda sem estrutura/acesso aptos"+(target.Kind==BuildingKind.WaterIntake&&!HasNaturalWaterSource(target)?"; captação sem fonte natural viável":"")+"; a instituição e seus funcionários permanecem no endereço antigo enquanto viável";continue;
                }
                BeginRelocationTransfer(order,source,target);
            }
            ProgressRelocationTransfer(order,source,target,dt);
        }
    }
    void CloseRelocationAfterCancel(MunicipalRelocationOrder order)
    {
        var released=order.RemovalReserved+order.TransferReserved;
        order.RemovalReserved=0;order.TransferReserved=0;order.Stage=MunicipalRelocationStage.Cancelled;order.FinishedAt=State.Now;
        order.Cause="Obra substituta cancelada: mudança encerrada; "+Cash(released)+" de retirada/frete reservados liberados; gastos já pagos não voltam e o prédio de origem segue no estado real em que estiver";
        Event("Mudança de "+Catalog.Definitions[order.Kind].Name+" encerrada pelo cancelamento da obra substituta; reservas liberadas, pagamentos efetivos preservados");
    }
    void EndRelocationOfCancelledWork(WorkProject project){if(ActiveRelocationForProject(project.Id) is {} order)CloseRelocationAfterCancel(order);}
    // Mover the substitute work keeps the relocation: same cancel-and-order rules
    // of an unfinished work, relinked to the new work instead of a separate one.
    CommandResult MoveRelocationWork(MunicipalRelocationOrder order,WorkProject project,Point position,double rotation)
    {
        var source=BuildingById(order.SourceBuildingId);
        if(source!=null&&Overlap(new Building{Kind=order.Kind,Position=position,Rotation=rotation},source))return new(false,$"O destino fica dentro do prédio antigo ({source.Definition.Name} #{source.Id}), que permanece até a retirada; escolha um endereço livre");
        CancelWork(project);
        var placed=PlaceBuilding(order.Kind,position,rotation,source?.ProjectId);
        if(!placed.Success){CloseRelocationAfterCancel(order);return new(false,"Obra substituta cancelada e não recolocada: "+placed.Message);}
        var target=BuildingById(placed.Id)!;order.TargetBuildingId=target.Id;order.TargetProjectId=target.ProjectId;
        target.OperationClosed=true;
        Event("Obra substituta de "+target.Definition.Name+" movida; a mudança continua com retirada e frete ainda reservados; pagamentos efetivos da obra anterior preservados");
        return new(true,State.LastMessage,target.Id);
    }
    void BeginRelocationTransfer(MunicipalRelocationOrder order,Building? source,Building target)
    {
        order.Stage=MunicipalRelocationStage.Transferring;order.BlockedSince=-1;target.OperationClosed=false;
        if(source==null)return;
        // Same institution: the job keeps its salary; the old address stops
        // operating now, so capacity, vacancies and wages never exist twice.
        target.SalaryOffer=source.SalaryOffer;source.OperationClosed=true;source.OperationalCapacity=0;
        foreach(var c in State.Citizens.Where(c=>c.Alive&&c.WorkplaceId==source.Id).ToList())ReevaluateAfterRelocation(c,order,source,target);
        // Reservations pointed at stock that is now leaving; works reserve again
        // from the new address as the stock physically arrives there.
        if(source.Kind==BuildingKind.WorksDepot)foreach(var p in State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress))p.LocalReserved.Clear();
        Event(target.Definition.Name+" concluída no novo endereço: "+order.WorkersContinued+" funcionário(s) continuam, "+order.WorkersLeft+" saíram após reavaliar"+(source.Stock.Values.Any(v=>v>.0001)?"; estoque segue por caminhão antes da retirada do endereço antigo":"; retirada do endereço antigo"));
    }
    // The individual re-evaluation already approved for a change of work address:
    // a route from home is required and a clearly better real vacancy may win.
    void ReevaluateAfterRelocation(Citizen c,MunicipalRelocationOrder order,Building source,Building target)
    {
        c.WorkplaceId=target.Id;var name=target.Definition.Name;
        var home=BuildingById(FamilyById(c.FamilyId)?.HomeId);var from=home==null?null:Access(home);var to=Access(target);
        if(home!=null&&(from==null||to==null||FindRoadRoute(from.Id,to.Id).Count==0)){SettleWages(c,target);c.WorkplaceId=null;order.WorkersLeft++;Personal(c,"Saiu do emprego: o novo endereço de "+name+" não tem trajeto viável desde a moradia");return;}
        if(home!=null&&JobOptions(home).FirstOrDefault(b=>b.Id!=target.Id) is {} better&&JobScore(home,better)>JobScore(home,target)+target.SalaryOffer*.15m){
            SettleWages(c,target);c.WorkplaceId=better.Id;c.Shift=State.Citizens.Count(x=>x.Alive&&x.WorkplaceId==better.Id&&x.Id!=c.Id)%(better.Definition.Utility?3:better.Kind is BuildingKind.Grocery or BuildingKind.FuelStation?2:1);c.NextDecisionAt=State.ElapsedSeconds;
            order.WorkersLeft++;Personal(c,"Reavaliou o emprego na mudança de "+name+": trocou por "+better.Definition.Name+" (salário e trajeto melhores)");return;
        }
        order.WorkersContinued++;Personal(c,"Reavaliou o emprego na mudança de "+name+": continua no novo endereço");
        var idleAtOld=!c.Moving&&c.Position.Distance(source.Door)<3;var headingToOld=c.Moving&&c.DestinationId==source.Id;
        if(c.InsideMap&&c.VehicleId==null&&c.BoardingVehicleId==null&&c.ConstructionProjectId==null&&(idleAtOld||headingToOld)&&ShiftActive(c))StartJourney(c,target,JourneyPurpose.Work);
    }
    void ProgressRelocationTransfer(MunicipalRelocationOrder order,Building? source,Building? target,double dt)
    {
        if(source==null){FinishRelocation(order,null,target);return;}
        if(target!=null)RedirectCargoBoundFor(order,source,target);
        var transfers=State.Shipments.Where(s=>order.TransferShipmentIds.Contains(s.Id)&&!s.Complete).ToList();
        var pickups=State.Shipments.Where(s=>!s.Complete&&!s.PickedUp&&s.SourceBuildingId==source.Id).ToList();
        var inbound=State.Shipments.Count(s=>!s.Complete&&s.DestinationBuildingId==source.Id);
        double Free(ResourceKind k)=>source.Quantity(k)-pickups.Sum(s=>s.Cargo.GetValueOrDefault(k));
        var remaining=source.Stock.Keys.Where(k=>Free(k)>.001).ToList();
        if(remaining.Count==0&&transfers.Count==0&&pickups.Count==0&&inbound==0){FinishRelocation(order,source,target);return;}
        var stock=source.Stock.Values.Sum();
        var progressed=order.LastRecoveryStock>=0&&stock<order.LastRecoveryStock-.0001;
        order.LastRecoveryStock=stock;
        if(progressed)order.StockRecoveryBlockedSeconds=0;
        string? blocked=RelocationRouteBlock(source,target);var dispatched=false;
        if(target==null)blocked="Novo endereço indisponível para receber o estoque";
        else if(blocked==null)foreach(var k in remaining){
            var space=target.Definition.Capacity-target.Stock.Values.Sum()-State.Shipments.Where(s=>!s.Complete&&s.DestinationBuildingId==target.Id).Sum(s=>s.Cargo.Values.Sum());
            if(space<.001){blocked="Novo endereço sem espaço de estoque";break;}
            var refusal=DispatchRelocationStock(order,source,target,k,Math.Min(Free(k),space));
            if(refusal==null)dispatched=true;else blocked=refusal;
        }
        var stockText=string.Join(", ",source.Stock.Where(e=>e.Value>.0001).Select(e=>ResourceLabel(e.Key)+" "+e.Value.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)));
        // Pending deliveries reserve space but are not a permanent lack of room.
        // Only actual full storage or a missing physical road counts; vehicle,
        // cash and fuel waits pause the clock. A real pickup resets it.
        var deadlineCause=stock>.001?(RelocationRouteBlock(source,target)??(target!=null&&target.Definition.Capacity-target.Stock.Values.Sum()<.001?"Novo endereço sem espaço de estoque":null)):null;
        if(deadlineCause!=null&&!progressed)order.StockRecoveryBlockedSeconds+=dt;
        order.BlockedSince=deadlineCause==null?-1:State.ElapsedSeconds-order.StockRecoveryBlockedSeconds;
        if(deadlineCause!=null&&order.StockRecoveryBlockedSeconds>=MunicipalStockRecoveryDeadline){FinishRelocation(order,source,target);return;}
        var trips=State.Shipments.Count(s=>order.TransferShipmentIds.Contains(s.Id)&&!s.Complete);
        var action=deadlineCause??blocked??(trips>0||dispatched?"Transferindo estoque por caminhão real: "+trips+" viagem(ns) em curso":pickups.Count+inbound>0?"Aguardando carregamentos já a caminho do endereço antigo":"Aguardando saída para o estoque");
        order.Cause=action+(stockText.Length>0?"; ainda no endereço antigo: "+stockText:"")+"; retirada aguarda recuperação; prazo de perda residual: "+(Math.Max(0,MunicipalStockRecoveryDeadline-order.StockRecoveryBlockedSeconds)/3600).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" h de bloqueio por espaço/rota"+(deadlineCause==null?" (pausado; caminhão, caixa e combustível não contam)":"; risco de perda do estoque residual")+(inbound>0?"; preservando cargas ainda a caminho do endereço antigo":"");
    }
    string? RelocationRouteBlock(Building source,Building? target)
    {
        if(target==null)return "Novo endereço indisponível para receber o estoque";
        var from=Access(source);var to=Access(target);
        if(from==null||to==null||!HasExteriorAccess(source)||!HasExteriorAccess(target))return "Sem acesso viário entre os dois endereços";
        return FindRoadRoute(from.Id,to.Id).Count==0?"Sem rota viável entre os dois endereços":null;
    }
    // Cargo already loaded for the old address (a redirected municipal load)
    // continues physically to the new one instead of stopping at a closed door.
    void RedirectCargoBoundFor(MunicipalRelocationOrder order,Building source,Building target)
    {
        var from=Access(source);var to=Access(target);if(from==null||to==null||!HasExteriorAccess(target))return;
        foreach(var s in State.Shipments.Where(s=>!s.Complete&&!s.Cancelled&&s.PickedUp&&s.ProjectId==null&&s.DestinationBuildingId==source.Id)){
            var v=State.Vehicles.FirstOrDefault(x=>x.Id==s.VehicleId&&x.ShipmentId==s.Id);
            // Finish the existing road leg first. Never replace the next node in
            // mid-segment or interrupt a detour/refuelling/load operation.
            if(v==null||v.Status!=VehicleStatus.Unloading||v.RefuellingAt!=null||v.Detour.Count>0||v.RouteIndex<v.Route.Count||v.DockPosition is not {} dock||v.Position.Distance(dock)>.1||dock.Distance(DockAt(from,source,false))>.1)continue;
            var reserved=State.Shipments.Where(x=>x.Id!=s.Id&&!x.Complete&&x.DestinationBuildingId==target.Id).Sum(x=>x.Cargo.Values.Sum());
            if(v.Cargo.Values.Sum()>target.Definition.Capacity-target.Stock.Values.Sum()-reserved+.001)continue;
            var route=FindRoadRoute(from.Id,to.Id);var back=FindRoadRoute(to.Id,EntryNode().Id);
            if(route.Count==0||back.Count==0||v.Fuel<(RouteLength(route)+RouteLength(back)+v.Position.Distance(from.Position))*FuelRate(v)+4)continue;
            var fee=Money(State.Tuning.FreightBase+(decimal)RouteLength(route)*State.Tuning.FreightPerMetre);
            var reservedFee=Math.Min(fee,order.TransferReserved);
            if(fee-reservedFee>Available||!Transfer("city","global",fee,"Mudança municipal","Frete e combustível: redirecionamento físico de carga ao novo endereço",source.Id))continue;
            order.TransferReserved-=reservedFee;order.TransferPaid+=fee;s.PricePaid+=fee;
            s.DestinationBuildingId=target.Id;s.Destination=to.Position;v.Route=route;v.RouteIndex=0;v.DockPosition=DockAt(to,target,false);v.Status=VehicleStatus.Travelling;v.Cause="Carga municipal seguindo ao novo endereço de "+target.Definition.Name;
        }
    }
    // One real external truck per load, paid like the municipal transport:
    // first from the freight reserved at confirmation, then from free cash.
    string? DispatchRelocationStock(MunicipalRelocationOrder order,Building source,Building target,ResourceKind k,double amount)
    {
        if(amount<=.001)return null;
        var vehicle=AvailableTruck(k);if(vehicle==null)return "Aguardando caminhão externo disponível para "+ResourceLabel(k);
        var from=Access(source);var to=Access(target);if(from==null||to==null||!HasExteriorAccess(source)||!HasExteriorAccess(target))return "Sem acesso viário entre os dois endereços";
        var entry=EntryNode();var route=FindRoadRoute(entry.Id,from.Id);var leg=FindRoadRoute(from.Id,to.Id);var back=FindRoadRoute(to.Id,entry.Id);
        if(route.Count==0||leg.Count==0||back.Count==0)return "Sem rota viável entre os dois endereços";
        var travel=RouteLength(route)+RouteLength(leg);
        if((travel+RouteLength(back))*FuelRate(vehicle)+4>State.Tuning.FuelTank)return "Rota da mudança além da autonomia do caminhão";
        var fuel=Math.Max(0,State.Tuning.FuelTank-vehicle.Fuel);if(State.OutsideStock.GetValueOrDefault(ResourceKind.Fuel)<fuel)return "Fornecedor sem combustível para o caminhão";
        var fee=Money(State.Tuning.FreightBase+(decimal)(travel*2)*State.Tuning.FreightPerMetre+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]);
        var reserved=Math.Min(fee,order.TransferReserved);if(fee-reserved>Available)return "Caixa livre insuficiente para o frete do estoque";
        if(!Transfer("city","global",fee,"Mudança municipal","Frete e combustível do estoque de "+source.Definition.Name+" ao novo endereço",source.Id))return "Caixa insuficiente para o frete do estoque";
        order.TransferReserved-=reserved;order.TransferPaid+=fee;State.OutsideStock[ResourceKind.Fuel]-=fuel;vehicle.Fuel+=fuel;
        amount=Math.Min(amount,vehicle.CargoCapacity);
        var shipment=new Shipment{Id=Id(),Purpose=ShipmentPurpose.Maintenance,SourceBuildingId=source.Id,DestinationBuildingId=target.Id,Destination=to.Position,VehicleId=vehicle.Id,Cargo=new(){{k,amount}},PricePaid=fee,PickedUp=false,GoodsOwnerAccount=source.OwnerAccount??"city"};
        State.Shipments.Add(shipment);order.TransferShipmentIds.Add(shipment.Id);
        vehicle.ShipmentId=shipment.Id;vehicle.Cargo=new();vehicle.Route=route;vehicle.RouteIndex=0;vehicle.DockPosition=DockAt(from,source,false);vehicle.Status=VehicleStatus.Travelling;vehicle.Position=State.Entry;vehicle.Cause="Mudança de "+source.Definition.Name+": buscando estoque no endereço antigo";
        return null;
    }
    static string ResourceLabel(ResourceKind kind)=>kind switch{ResourceKind.Aggregate=>"Areia e brita",ResourceKind.Concrete=>"Concreto",ResourceKind.Steel=>"Aço",ResourceKind.Wood=>"Madeira",ResourceKind.Asphalt=>"Asfalto",ResourceKind.Food=>"Alimentos",_=>"Combustível"};
    void FinishRelocation(MunicipalRelocationOrder order,Building? source,Building? target)
    {
        var name=Catalog.Definitions[order.Kind].Name;
        if(source!=null){
            // Loaded inbound cargo is outside the structure. Retire its old
            // destination through the existing municipal recovery flow, leaving
            // the body, ownership and current road leg untouched. Arrival or
            // ordinary replanning will recover/park it instead of unloading into
            // the retired building; it is never included in residual loss.
            foreach(var s in State.Shipments.Where(s=>!s.Complete&&s.PickedUp&&s.ProjectId==null&&s.GoodsOwnerAccount=="city"&&s.DestinationBuildingId==source.Id))s.Cancelled=true;
            // Loads not yet picked up stay inside the structure and are lost with it.
            foreach(var s in State.Shipments.Where(s=>!s.Complete&&!s.PickedUp&&s.SourceBuildingId==source.Id)){
                s.Cancelled=true;
                if(s.ProjectId is {} pid&&State.Projects.FirstOrDefault(p=>p.Id==pid) is {} p)foreach(var kv in s.Cargo)p.LocalReserved[kv.Key]=Math.Max(0,p.LocalReserved.GetValueOrDefault(kv.Key)-kv.Value);
            }
            foreach(var c in State.Citizens.Where(c=>c.WorkplaceId==source.Id)){SettleWages(c,source);c.WorkplaceId=null;Personal(c,"Vínculo encerrado: endereço antigo de "+name+" retirado");}
            order.StockLost=source.Stock.Where(e=>e.Value>.001).ToDictionary(e=>e.Key,e=>e.Value);
            if(order.RemovalReserved>0&&!Transfer("city","global",order.RemovalReserved,"Mudança municipal","Retirada física do endereço antigo de "+name+"; sem reembolso de obra ou materiais",source.Id))throw new InvalidOperationException("Retirada reservada sem Caixa");
            order.RemovalPaid+=order.RemovalReserved;
            source.Stock.Clear();source.OperationalCapacity=0;source.OperationClosed=true;source.Removed=true;source.Cause="Retirado: "+name+" mudou-se para o novo endereço";
            RetireMaintenanceWithoutTargets();UpdateCrosswalks();
        }
        order.RemovalReserved=0;order.TransferReserved=0;order.BlockedSince=-1;order.Stage=MunicipalRelocationStage.Completed;order.FinishedAt=State.Now;
        var moved=State.Shipments.Where(s=>order.TransferShipmentIds.Contains(s.Id)&&s.Complete&&!s.Cancelled).SelectMany(s=>s.Cargo).GroupBy(e=>e.Key).ToDictionary(g=>g.Key,g=>g.Sum(e=>e.Value));
        static string Amounts(Dictionary<ResourceKind,double> stock)=>string.Join(", ",stock.Select(e=>ResourceLabel(e.Key)+" "+e.Value.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)));
        var parts=new List<string>{"Mudança de "+name+" concluída"+(target==null?"":" no novo endereço #"+target.Id)};
        parts.Add(source==null?"endereço antigo já não existia: nada a retirar":"retirada do endereço antigo paga ("+Cash(order.RemovalPaid)+")");
        if(order.TransferPaid>0)parts.Add("frete do estoque "+Cash(order.TransferPaid));
        if(moved.Count>0)parts.Add("estoque levado por caminhão: "+Amounts(moved));
        if(order.StockLost.Count>0)parts.Add("resíduo de estoque baixado uma única vez após "+(MunicipalStockRecoveryDeadline/3600)+" h sem progresso por falta de espaço/rota: "+Amounts(order.StockLost)+"; perda do titular "+(source?.OwnerAccount??"city")+"; causa anterior: "+order.Cause);
        parts.Add(order.WorkersContinued+" funcionário(s) continuaram e "+order.WorkersLeft+" saíram após reavaliar");
        order.Cause=string.Join("; ",parts);Event(order.Cause);
    }
}
