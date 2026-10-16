using Godot;
using IndexCities.Simulation;
using System.Collections.Generic;
namespace IndexCities.Game;

// SPEC 2026-10-16 "Problemas de obra em linguagem simples": no menu de Obras,
// cada causa de obra parada/atrasada vira uma frase curta com a ação que
// resolve, no lugar do texto tecnico de diagnostico. Cada frase deriva da
// causa real ja calculada (project.Cause, ver Construction.cs/Maintenance.cs);
// nada e recalculado aqui. O detalhe tecnico continua no tooltip do rotulo.
// O icone reutiliza pictogramas ja existentes em Assets/UI; quando nao ha
// pictograma proprio, usa-se um simbolo simples como prefixo. So apresentacao.
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
    // Icon==null: o proprio texto ja comeca com um simbolo simples ("⚠", "●").
    static (string Text,string? Icon) WorkCauseView(string cause)=>cause switch{
        // Acesso/rua: sem rota pronta ate o canteiro.
        "Sem acesso para materiais/equipe" => ("Sem acesso à obra — ligue uma rua","problem_road"),
        "Sem acesso viário" => ("Sem rua até a obra — construa uma rua","problem_road"),
        "Sem rua urbana: a obra aguardará acesso" => ("Sem rua até a obra — construa uma rua","problem_road"),
        "Rua sem ligação ao exterior: a obra aguardará acesso" => ("Sem ligação ao exterior — conecte a rua","problem_road"),
        "Rua em obra junto à entrada: aguardando ligação pronta" => ("Rua em obra na entrada — aguarde a ligação","problem_road"),
        "Sem ligação pronta ao exterior: a obra aguardará acesso; ligação planejada não libera entregas" => ("Rua sem ligação — conecte a uma via pronta","problem_road"),
        "Trecho construído; aguardando ligação ao exterior" => ("Rua pronta sem ligação — conecte ao exterior","problem_road"),
        "Rota de obra interrompida" => ("Rota interrompida — reconecte a rua","problem_road"),
        // Transporte e materiais.
        "Aguardando caminhão externo disponível" => ("Sem caminhão livre — aguarde a entrega","activity_cargo"),
        "Aguardando entrega física" => ("Entrega a caminho — aguarde o caminhão","activity_cargo"),
        "Materiais em trânsito" => ("Materiais a caminho — aguarde a entrega","activity_cargo"),
        "Fornecedor externo sem material disponível" => ("Fornecedor sem material — aguarde reposição","problem_stock"),
        "Faltam materiais" => ("Falta material — aguardando entrega","problem_stock"),
        "Fornecedor externo sem combustível" => ("Fornecedor sem combustível — aguarde","problem_energy"),
        // Equipe.
        "Aguardando equipe física no canteiro" => ("Sem equipe no canteiro — aguarde trabalhadores","problem_staff"),
        // Verba da obra: acao e cancelar; sem pictograma proprio.
        "Compromisso restante insuficiente" => ("⚠ Verba da obra esgotada — cancele a obra",null),
        "Compromisso insuficiente para transportar equipe externa" => ("⚠ Sem verba p/ trazer equipe — cancele a obra",null),
        "Sem orçamento para pagar equipe externa" => ("⚠ Sem verba p/ a equipe — cancele a obra",null),
        "Equipe externa aguardando pagamento" => ("⚠ Equipe sem pagamento — cancele a obra",null),
        // Estados informativos (obra segue normalmente).
        "Construção em andamento" => ("● Em construção",null),
        "Trecho válido; ligação exterior disponível" => ("● Trecho válido — enviando material e equipe",null),
        "Posição válida" => ("● Obra encomendada — enviando material e equipe",null),
        "A captação precisará alcançar a fonte natural" => ("Distante da água — aproxime a captação","problem_intake"),
        // Manutencao.
        "Manutenção aguardando materiais e equipe reais" => ("Manutenção parada — aguarda material e equipe","problem_maintenance"),
        "Manutenção em andamento" => ("Manutenção em andamento","problem_maintenance"),
        "Manutenção encerrada: infraestrutura removida; pagamentos e carga preservados" => ("Manutenção encerrada — infraestrutura removida","problem_maintenance"),
        "Manutenção realizada com materiais e trabalho efetivos" => ("Manutenção concluída","problem_maintenance"),
        // Cancelamento.
        "Cancelada; gastos pagos e material consumido não voltam" => ("Cancelada — gastos pagos não voltam","demolish"),
        // Causa desconhecida: mantem o texto tecnico (sem inventar traducao).
        _ => (cause,null),
    };

    // Cenário de QA do menu de Obras: a obra sem acesso viario tem causa real e
    // deve aparecer como frase curta com icone, mantendo o texto tecnico no
    // tooltip. Nao altera a simulacao; apenas le e verifica a apresentacao.
    void WorkCauseSmoke()
    {
        try{
            _speed=0;_autosaveMinutes=0;
            var placed=_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16));
            Require(placed.Success,"Prefeitura encomendada");
            var project=_sim.State.Projects.Single();
            Require(project.Status!=WorkStatus.Completed,"Obra pendente aparece no menu");
            Require(project.Cause=="Sem rua urbana: a obra aguardará acesso","Causa real de obra sem rua: "+project.Cause);
            var before=_sim.SaveToJson();
            ShowAlerts();
            var modal=_modal;Require(modal!=null,"Menu de Obras abre");
            var expected=((int)(project.Progress*100))+"% · Sem rua até a obra — construa uma rua";
            var label=RecoveryNodes<Label>(modal!).Single(l=>l.Name.ToString()=="ProjectCause"+project.Id);
            Require(label.Text==expected,"Frase curta para obra sem rua: "+label.Text);
            Require(label.TooltipText==project.Cause,"Tooltip preserva o texto técnico");
            var icon=RecoveryNodes<TextureRect>(modal!).Single(i=>i.Name.ToString()=="ProjectCauseIcon"+project.Id);
            Require(icon.Visible&&icon.Texture!=null,"Ícone reconhecível de rua");
            Require(WorkCauseView("Causa nova sem tradução").Text=="Causa nova sem tradução"
                &&WorkCauseView("Causa nova sem tradução").Icon==null,"Causa desconhecida não inventa frase");
            Require(WorkCauseView("Cancelada; gastos pagos e material consumido não voltam").Icon=="demolish","Cancelada usa ícone de demolição");
            Require(_sim.SaveToJson()==before,"Abrir o menu não altera a cidade");
            GD.Print("INDEXCITIES_WORK_CAUSE_OK phrase="+label.Text+" tooltip="+label.TooltipText+" icon=problem_road");
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
}
