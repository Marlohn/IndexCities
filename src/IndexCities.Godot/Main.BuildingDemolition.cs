using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

// Ordem Demolir de prédio concluído: prévia com custos, beneficiário e afetados
// reais, confirmação que revalida antes de cobrar, e andamento na inspeção.
public partial class Main
{
    int? _buildingDemolitionTarget;
    bool BuildingDemolitionPreview()
    {
        if(_buildingDemolitionTarget is not {} id)return false;
        if(_modal==null||_sim.State.Buildings.FirstOrDefault(b=>b.Id==id&&!b.Removed) is not {} building){_buildingDemolitionTarget=null;_world.ClearPreview();return false;}
        _world.PreviewBuildingDemolition(building);return true;
    }
    string VacatingWindow=>_sim.State.Tuning.AssistedVacatingDays.ToString("0.##",Pt)+" dia(s)";
    void ShowBuildingDemolition(int buildingId,string feedback="")
    {
        var s=_sim.State;var building=s.Buildings.First(b=>b.Id==buildingId);var quote=_sim.QuoteBuildingDemolition(buildingId);
        var box=Modal("Demolir "+building.Definition.Name);_buildingDemolitionTarget=buildingId;if(feedback.Length>0)Text(box,"⚠ "+feedback,16);
        Text(box,"Alvo: "+building.Definition.Name+" #"+building.Id+" • "+building.Position.X.ToString("N0")+", "+building.Position.Z.ToString("N0"),18);
        if(quote.TotalCost>0){
            var costs=Card(box,"Custos");
            Text(costs,"Custo de remoção: "+Cash(quote.RemovalCost),16);
            Text(costs,quote.Compensation>0?"Indenização (100% do valor de mercado): "+Cash(quote.Compensation)+" → "+quote.OwnerName
                :building.Definition.Private?"Indenização: sem proprietário econômico; nada vai à Reserva Global"
                :"Indenização: não se aplica a prédio municipal",16);
            Text(costs,"Total que sai do Caixa: "+Cash(quote.TotalCost),22);
            Text(costs,"Caixa livre: "+Cash(_sim.Available)+" • já comprometido em obras: "+Cash(_sim.Committed),14);
            if(quote.Compensation>0)Text(costs,"A indenização é paga ao titular no instante da confirmação, uma única vez.",14);
        }
        var effects=Card(box,"Consequências");
        if(quote.WaitsForResidents){
            var homes=Text(effects,"⌂ "+quote.FamilyIds.Count+" família(s), "+quote.ResidentIds.Count+" morador(es): a retirada aguarda a desocupação assistida de até "+VacatingWindow+".",16);
            homes.TooltipText="Moradores: "+string.Join(", ",quote.ResidentIds.Select(id=>s.Citizens.First(c=>c.Id==id).Name));homes.MouseFilter=Control.MouseFilterEnum.Stop;
            Text(effects,"A família procura casa existente à venda que consiga pagar com o próprio dinheiro. Não há casa garantida: ao fim do prazo, quem não se mudou fica sem moradia.",14);
        }else Text(effects,"Retirada imediata após confirmar.",16);
        if(quote.WorkerIds.Count>0)Text(effects,"⚒ "+quote.WorkerIds.Count+" emprego(s) encerrado(s) na retirada; salários devidos são pagos ou ficam como dívida do empregador.",14);
        if(quote.Stock.Count>0)Text(effects,"▣ Estoque perdido na retirada: "+string.Join(" • ",quote.Stock.Select(k=>ResourceName(k.Key)+" "+k.Value.ToString("N1"))),14);
        if(!building.Definition.Private&&building.Definition.Capacity>0)Text(effects,"⚠ Capacidade de serviço perdida: "+quote.OperationalCapacity.ToString("N1")+" atual",14);
        Text(effects,"Sem reembolso de obra ou materiais; sem promessa de nova moradia ou de continuidade da empresa.",14);
        if(!quote.Allowed)Text(box,"⚠ "+quote.Message+(quote.TotalCost>_sim.Available?" — falta "+Cash(quote.TotalCost-_sim.Available):""),16);
        var row=ModalActions();var confirm=Button(row,"✓ Confirmar demolição",()=>ConfirmBuildingDemolition(buildingId,quote,box));confirm.Disabled=!quote.Allowed;Button(row,"× Cancelar",CloseModal);
    }
    void ConfirmBuildingDemolition(int buildingId,BuildingDemolitionQuote displayed,Node feedback)
    {
        var result=_sim.DemolishBuilding(buildingId,displayed);
        if(!result.Success){if(result.Message==CitySimulation.StaleDemolitionPreview||_sim.QuoteBuildingDemolition(buildingId) is {Allowed:false})ShowBuildingDemolition(buildingId,result.Message);else Text(feedback,result.Message);return;}
        CloseModal();if(_sim.State.Buildings.First(b=>b.Id==buildingId).Removed)ClearSelection();_inspectionKey="";RefreshInspection();RefreshHud();
    }
    // Status of a confirmed order goes at the top of the inspection; the action at the end.
    void BuildingDemolitionInspection(Node parent,Building building,bool status)
    {
        if(status){if(building.Demolition is not {} order)return;
            var card=Card(parent,"Demolição confirmada");
            Text(card,order.CompensationPaid>0?"Indenização paga: "+Cash(order.CompensationPaid)+" a "+order.BeneficiaryName:"Sem indenização a pagar",14);
            Text(card,"Remoção paga: "+Cash(order.RemovalCostPaid),14);
            InspectionText(card,()=>"⌂ Famílias ainda no imóvel: "+_sim.VacatingFamilies(building).Count+" de "+order.FamiliesAtOrder+" • mudaram-se: "+order.FamiliesRelocated,16);
            InspectionText(card,()=>"Prazo restante: "+Math.Max(0,(order.DeadlineSeconds-_sim.State.ElapsedSeconds)/3600).ToString("N1")+" h; depois, quem não se mudou fica sem moradia e o prédio é retirado",14);
            return;
        }
        if(building.Demolition!=null||!building.Completed||building.Kind==BuildingKind.CityHall)return;
        InspectionButton(parent,()=>_sim.QuoteBuildingDemolition(building.Id).Compensation>0?"× Demolir com indenização":"× Demolir",()=>ShowBuildingDemolition(building.Id),"Mostra custos, beneficiário e afetados antes de confirmar");
    }
}
