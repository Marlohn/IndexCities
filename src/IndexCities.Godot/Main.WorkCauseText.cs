using Godot;
using IndexCities.Simulation;
using System.Collections.Generic;
namespace IndexCities.Game;

// SPEC 2026-10-16 "Problemas de obra em linguagem simples": no menu de Obras,
// cada causa de obra parada/atrasada vira uma frase curta com a ação que
// resolve, no lugar do texto tecnico de diagnostico. Cada frase deriva da
// causa real ja calculada (project.Cause, ver Construction.cs/Maintenance.cs);
// nada e recalculado aqui. As causas sao as constantes de WorkCause, para que
// toda causa gravada pela simulacao tenha frase. O detalhe tecnico continua no
// tooltip do rotulo. O icone reutiliza pictogramas ja existentes em Assets/UI;
// quando nao ha pictograma adequado, a frase fica sem icone. So apresentacao.
public partial class Main
{
    static readonly Dictionary<string,Texture2D?> WorkCauseTextures=new();

    static Texture2D? WorkCauseTexture(string? file)
    {
        if(file==null)return null;
        if(!WorkCauseTextures.TryGetValue(file,out var texture))texture=WorkCauseTextures[file]=
            GD.Load<Texture2D>("res://Assets/UI/"+file+".svg");
        return texture;
    }

    // Texto curto exibido + arquivo de icone (sem extensao) quando existe.
    // Icon==null: a frase fica so com texto (sem pictograma adequado em Assets/UI).
    static (string Text,string? Icon) WorkCauseView(string cause,bool isMaintenance)=>
        WorkCauseViewOrNull(cause,isMaintenance)??(cause,null);

    // null quando a causa nao tem traducao (fallback mostra o texto tecnico).
    static (string Text,string? Icon)? WorkCauseViewOrNull(string cause,bool isMaintenance)=>cause switch{
        // Acesso/rua: sem rota pronta ate o canteiro.
        WorkCause.NoMaterialsAccess => ("Sem caminho pela rua até a obra — ligue a obra à rede que sai da cidade","problem_road"),
        WorkCause.NoRoadAccess => ("Sem rua até a obra — construa uma rua","problem_road"),
        WorkCause.NoUrbanRoad => ("Sem rua até a obra — construa uma rua","problem_road"),
        WorkCause.RoadWithoutExterior => ("Sem ligação ao exterior — conecte a rua","problem_road"),
        WorkCause.RoadWorkAtEntrance => ("Rua em obra na entrada — aguarde a ligação","problem_road"),
        WorkCause.NoReadyExteriorLink => ("Rua sem ligação — conecte a uma via pronta","problem_road"),
        WorkCause.RoadBuiltAwaitingLink => ("Rua pronta sem ligação — conecte ao exterior","problem_road"),
        WorkCause.WorkRouteInterrupted => ("Rota interrompida — reconecte a rua","problem_road"),
        // Transporte e materiais.
        WorkCause.AwaitingExternalTruck => ("Sem caminhão livre — aguarde a entrega","activity_cargo"),
        WorkCause.AwaitingPhysicalDelivery => ("Entrega a caminho — aguarde o caminhão","activity_cargo"),
        WorkCause.MaterialsInTransit => ("Materiais a caminho — aguarde a entrega","activity_cargo"),
        WorkCause.SupplierOutOfMaterial => ("Fornecedor sem material — aguarde reposição","problem_stock"),
        WorkCause.MissingMaterials => ("Falta material — aguardando entrega","problem_stock"),
        WorkCause.SupplierOutOfFuel => ("Fornecedor sem combustível — aguarde reposição","problem_stock"),
        WorkCause.RouteBeyondTruckRange => ("Caminhão não tem combustível para ir e voltar — aproxime a obra da entrada","problem_road"),
        // Equipe.
        WorkCause.AwaitingCrewOnSite => ("Sem equipe no canteiro — aguarde trabalhadores","problem_staff"),
        // Verba da obra. Numa obra comum a acao disponivel e cancelar; numa
        // manutencao nao ha cancelamento (CancelProject recusa manutencao), entao
        // a frase informa que a manutencao e automatica.
        WorkCause.RemainingCommitmentInsufficient => isMaintenance
            ? ("Verba da manutenção esgotada — manutenção automática","problem_maintenance")
            : ("Sem verba restante para a obra — cancele a obra","demolish"),
        WorkCause.CommitmentInsufficientForCrew => isMaintenance
            ? ("Sem verba para trazer a equipe — manutenção automática","problem_maintenance")
            : ("Sem verba para trazer a equipe — cancele a obra","demolish"),
        WorkCause.NoBudgetForExternalCrew => isMaintenance
            ? ("Sem verba para pagar a equipe — manutenção automática","problem_maintenance")
            : ("Sem verba para pagar a equipe — cancele a obra","demolish"),
        WorkCause.ExternalCrewAwaitingPayment => isMaintenance
            ? ("Equipe sem pagamento — manutenção automática","problem_maintenance")
            : ("Equipe sem pagamento — cancele a obra","demolish"),
        // Estados informativos (obra segue normalmente; sem pictograma proprio).
        WorkCause.UnderConstruction => ("Em construção",null),
        WorkCause.ValidStretchExteriorAvailable => ("Trecho válido — enviando material e equipe",null),
        WorkCause.ValidPosition => ("Obra encomendada — aguardando material e equipe",null),
        WorkCause.IntakeNeedsNaturalSource => ("Distante da água — aproxime a captação","problem_intake"),
        // Manutencao.
        WorkCause.MaintenanceAwaitingMaterials => ("Manutenção parada — aguarda material e equipe","problem_maintenance"),
        WorkCause.MaintenanceInProgress => ("Manutenção em andamento","problem_maintenance"),
        WorkCause.MaintenanceEndedRemoved => ("Manutenção encerrada — infraestrutura removida","problem_maintenance"),
        WorkCause.MaintenanceDone => ("Manutenção concluída","problem_maintenance"),
        // Cancelamento.
        WorkCause.Cancelled => ("Cancelada — gastos pagos não voltam","demolish"),
        // Obra concluida (nao aparece no menu; mantida para o QA de cobertura).
        WorkCause.Completed => ("Concluída",null),
        // Causa desconhecida: mantem o texto tecnico (sem inventar traducao).
        _ => null,
    };

    // Cenário de QA do menu de Obras: a obra sem acesso viario tem causa real e
    // deve aparecer como frase curta com icone, mantendo o texto tecnico no
    // tooltip. Tambem percorre TODAS as causas de obra da simulacao (WorkCause.All)
    // e exige uma frase para cada uma, nas duas variantes (obra e manutencao), para
    // que uma causa nova sem traducao falhe em vez de cair em silencio no fallback.
    // Nao altera a simulacao; apenas le e verifica a apresentacao.
    void WorkCauseSmoke()
    {
        try{
            _speed=0;_autosaveMinutes=0;
            foreach(var cause in WorkCause.All){
                Require(WorkCauseViewOrNull(cause,false)!=null,"Causa de obra sem frase: "+cause);
                Require(WorkCauseViewOrNull(cause,true)!=null,"Causa de manutenção sem frase: "+cause);
            }
            var placed=_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16));
            Require(placed.Success,"Prefeitura encomendada");
            var project=_sim.State.Projects.Single();
            Require(project.Status!=WorkStatus.Completed,"Obra pendente aparece no menu");
            Require(project.Cause==WorkCause.NoUrbanRoad,"Causa real de obra sem rua: "+project.Cause);
            var before=_sim.SaveToJson();
            ShowAlerts();
            var modal=_modal;Require(modal!=null,"Menu de Obras abre");
            var expected=project.Progress.ToString("P0",Pt)+" · Sem rua até a obra — construa uma rua";
            var label=RecoveryNodes<Label>(modal!).Single(l=>l.Name.ToString()=="ProjectCause"+project.Id);
            Require(label.Text==expected,"Frase curta para obra sem rua: "+label.Text);
            Require(label.TooltipText==project.Cause,"Tooltip preserva o texto técnico");
            var icon=RecoveryNodes<TextureRect>(modal!).Single(i=>i.Name.ToString()=="ProjectCauseIcon"+project.Id);
            Require(icon.Visible&&icon.Texture!=null,"Ícone reconhecível de rua");
            Require(WorkCauseView("Causa nova sem tradução",false).Text=="Causa nova sem tradução"
                &&WorkCauseView("Causa nova sem tradução",false).Icon==null,"Causa desconhecida não inventa frase");
            Require(WorkCauseView(WorkCause.Cancelled,false).Icon=="demolish","Cancelada usa ícone de demolição");
            Require(WorkCauseView(WorkCause.RemainingCommitmentInsufficient,true).Text.Contains("manutenção automática")
                &&WorkCauseView(WorkCause.RemainingCommitmentInsufficient,false).Text.Contains("cancele a obra"),
                "Verba de manutenção não manda cancelar; verba de obra manda");
            Require(_sim.SaveToJson()==before,"Abrir o menu não altera a cidade");
            GD.Print("INDEXCITIES_WORK_CAUSE_OK causes="+WorkCause.All.Length+" phrase="+label.Text+" tooltip="+label.TooltipText+" icon=problem_road");
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
}
