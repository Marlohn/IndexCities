using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

// Mover prédio municipal concluído (SPEC construcao: Mudança de prédios
// municipais concluídos): escolher o destino com a prévia comum de obra, conferir
// obra, retirada, frete, empregos, estoque e capacidade num orçamento e confirmar.
// Cancelar o orçamento só fecha a janela; a ferramenta continua para outro destino.
public partial class Main
{
    int? _relocatingBuilding;

    void BeginMunicipalRelocation(Building building)
    {
        ClearSelection();ClearTool();HideAlertTooltip();_placementGridVisible=true;
        _relocatingBuilding=building.Id;_building=building.Kind;_rotation=building.Rotation;_inspector.Visible=false;CloseModal();
        SetStatus("Mover "+building.Definition.Name+" · escolha o novo endereço; nada é cobrado antes de confirmar");
    }
    // Placement quote of the current tool: a relocation shows the whole
    // commitment (work + removal + freight) and the destination's validity.
    PlacementQuote PlacementPreviewQuote(BuildingKind kind,Point at)
    {
        if(_relocatingBuilding is {} id){var quote=_sim.QuoteMunicipalRelocation(id,at,_rotation);return new(quote.DestinationValid,quote.Construction?.Accessible??false,quote.TotalCost,quote.Construction?.Materials??new(),quote.Allowed?"Clique para conferir obra, retirada, empregos e estoque antes de confirmar":quote.Message);}
        return _movingProject is {} moving?_sim.QuoteMoveBuilding(moving,at,_rotation):_sim.QuoteBuilding(kind,at,_rotation);
    }
    string? RelocationToolHint()
    {
        if(_relocatingBuilding is not {} id||_sim.State.Buildings.FirstOrDefault(b=>b.Id==id) is not {} source)return null;
        var text="Mover "+source.Definition.Name+" · clique no novo endereço para conferir a mudança · R gira · botão direito cancela";
        if(_hoverPreview&&_modal==null&&GetViewport().GuiGetHoveredControl()==null){var quote=PlacementPreviewQuote(source.Kind,_cursor);text="Mover "+source.Definition.Name+" · total "+Cash(quote.Total)+" · "+QuoteFeedback(quote)+" · R gira · botão direito cancela";}
        return text;
    }
    void ShowMunicipalRelocation(int sourceId,Point destination,double rotation,string feedback="")
    {
        var source=_sim.State.Buildings.FirstOrDefault(b=>b.Id==sourceId);
        var quote=_sim.QuoteMunicipalRelocation(sourceId,destination,rotation);
        var box=Modal(source?.Kind==BuildingKind.CityHall?"Mover sede da Prefeitura":"Mover "+(source?.Definition.Name??"prédio"));_modal!.Name="MunicipalRelocationModal";
        if(feedback.Length>0)Text(box,feedback,16);
        if(quote.Construction==null||!quote.DestinationValid){Text(box,quote.Message);var refused=ModalActions();Button(refused,"× Escolher outro destino",CloseModal);return;}
        Text(box,"Total comprometido: "+Cash(quote.TotalCost),22);
        Text(box,"Nova obra no destino: "+Cash(quote.Construction.Total)+" · materiais, frete e equipe reais"+(quote.Construction.Accessible?"":" · ainda sem acesso viário: a obra aguarda rua"),15);
        Text(box,"Retirada do endereço antigo: "+Cash(quote.RemovalCost)+" · paga quando o prédio antigo sair, sem reembolso",15);
        if(quote.TransferEstimate>0)Text(box,"Frete estimado do estoque: "+Cash(quote.TransferEstimate)+" · caminhão real depois da conclusão",15);
        Text(box,"Indenização: não se aplica a patrimônio municipal",14);
        Text(box,"Caixa disponível: "+Cash(_sim.Available),14);
        Text(box,"Empregos: "+quote.WorkerIds.Count+" funcionário(s) passam ao novo endereço e reavaliam o vínculo pelo novo trajeto",15);
        Text(box,"Capacidade atual: "+quote.CurrentCapacity.ToString("N1")+" · o prédio antigo opera enquanto viável; a troca exige estrutura e acesso aptos, e a capacidade acompanha a chegada da equipe",15);
        if(quote.Stock.Count>0)Text(box,"Estoque: "+string.Join(" • ",quote.Stock.Select(k=>ResourceName(k.Key)+" "+k.Value.ToString("N1")))+(quote.StockBeyondDestination>0?" · "+quote.StockBeyondDestination.ToString("N1")+" não cabe no destino":""),15);
        if(quote.WorksWithLocalReservation>0||quote.PendingPickups>0)Text(box,"Obras com material reservado aqui: "+quote.WorksWithLocalReservation+" · carregamentos a caminho: "+quote.PendingPickups,14);
        Text(box,quote.Message,14);
        if(!quote.Allowed)Text(box,"Não é possível confirmar: "+quote.Message,15);
        var row=ModalActions();
        var confirm=Button(row,"✓ Confirmar mudança",()=>ConfirmMunicipalRelocation(sourceId,destination,rotation,quote));confirm.Disabled=!quote.Allowed;confirm.Name="ConfirmMunicipalRelocation";
        Button(row,"× Cancelar",CloseModal).Name="CancelMunicipalRelocation";
    }
    void ConfirmMunicipalRelocation(int sourceId,Point destination,double rotation,MunicipalRelocationQuote displayed)
    {
        var result=_sim.RelocateMunicipalBuilding(sourceId,destination,rotation,displayed);
        if(!result.Success){ShowMunicipalRelocation(sourceId,destination,rotation,result.Message==CitySimulation.StaleRelocationPreview?"Os dados mudaram enquanto a cidade avançava. Confira o orçamento atualizado antes de confirmar.":result.Message);return;}
        CloseModal();ClearTool();_world.Sync(_sim.State);SetStatus(result.Message);_selectedBuilding=sourceId;RefreshInspection();RefreshHud();
    }
    void MunicipalRelocationInspection(Node parent,Building b)
    {
        if(b.Definition.Private)return;
        var order=_sim.MunicipalRelocationOf(b.Id);
        if(order is {Active:true}){
            var card=Card(parent,order.SourceBuildingId==b.Id?"Mudança em andamento":"Obra substituta da mudança");
            InspectionText(card,()=>order.Stage==MunicipalRelocationStage.Constructing?"Etapa: obra no novo endereço":"Etapa: transferência do estoque e retirada",15);
            InspectionText(card,()=>order.Cause,14);
            InspectionText(card,()=>"Reservado: retirada "+Cash(order.RemovalReserved)+" · frete "+Cash(order.TransferReserved)+" · pago frete "+Cash(order.TransferPaid),14);
            if(order.TargetBuildingId==b.Id&&!b.Completed)Text(card,"Cancelar esta obra encerra a mudança: libera retirada e frete ainda não pagos; gastos efetivos não voltam e o prédio antigo segue como estiver.",14);
            return;
        }
        if(order is {Stage:MunicipalRelocationStage.Completed}&&order.TargetBuildingId==b.Id){var card=Card(parent,"Última mudança");InspectionText(card,()=>order.Cause,14);}
        if(!b.Completed)return;
        var label=b.Kind==BuildingKind.CityHall?"↗ Mover sede":"↗ Mover";
        var tooltip=b.Kind==BuildingKind.CityHall?"Nova sede em outro endereço com obra real; a gestão continua liberada e não surge segunda Prefeitura":"Novo prédio em outro endereço com obra real; o antigo opera durante a obra";
        InspectionButton(parent,()=>label,()=>BeginMunicipalRelocation(b),tooltip).Name="MunicipalRelocationButton";
    }
}
