using Godot;
namespace IndexCities.Game;

// SPEC 2026-10-16 "Problemas de obra em linguagem simples": cada frase curta
// deriva da causa real já calculada em Construction.cs (sem recalcular nada).
// O texto técnico original continua acessível no tooltip do rótulo.
// Símbolos restritos aos já usados na interface (⚠ · ●) para não depender de
// cobertura de emoji da fonte — ver PreviewSignal.cs. Só apresentação.
public partial class Main
{
    static string WorkCauseShort(string cause) => cause switch {
        "Sem acesso para materiais/equipe" => "⚠ Sem acesso à obra — ligue uma rua",
        "Sem acesso viário" => "⚠ Sem rua até a obra — construa uma rua",
        "Sem rua urbana: a obra aguardará acesso" => "⚠ Sem rua até a obra — construa uma rua",
        "Rua sem ligação ao exterior: a obra aguardará acesso" => "⚠ Sem ligação ao exterior — conecte a rua",
        "Rua em obra junto à entrada: aguardando ligação pronta" => "⚠ Rua em obra na entrada — aguarde a ligação",
        "Sem ligação pronta ao exterior: a obra aguardará acesso; ligação planejada não libera entregas" => "⚠ Rua sem ligação — conecte a uma via pronta",
        "Trecho construído; aguardando ligação ao exterior" => "⚠ Rua pronta sem ligação — conecte ao exterior",
        "Rota de obra interrompida" => "⚠ Rota interrompida — reconecte a rua",
        "Aguardando caminhão externo disponível" => "⚠ Sem caminhão livre — aguarde a entrega",
        "Fornecedor externo sem material disponível" => "⚠ Fornecedor sem material — aguarde reposição",
        "Fornecedor externo sem combustível" => "⚠ Fornecedor sem combustível — aguarde",
        "Faltam materiais" => "⚠ Falta material — aguardando entrega",
        "Aguardando entrega física" => "● Entrega a caminho — aguarde o caminhão",
        "Materiais em trânsito" => "● Materiais a caminho — aguarde a entrega",
        "Aguardando equipe física no canteiro" => "⚠ Sem equipe no canteiro — aguarde trabalhadores",
        "Compromisso restante insuficiente" => "⚠ Verba da obra esgotada — cancele a obra",
        "Compromisso insuficiente para transportar equipe externa" => "⚠ Sem verba p/ trazer equipe — cancele a obra",
        "Sem orçamento para pagar equipe externa" => "⚠ Sem verba p/ a equipe — cancele a obra",
        "Equipe externa aguardando pagamento" => "⚠ Equipe sem pagamento — cancele a obra",
        "Construção em andamento" => "● Em construção",
        "Trecho válido; ligação exterior disponível" => "● Trecho válido — enviando material e equipe",
        "Posição válida" => "● Obra encomendada — enviando material e equipe",
        "A captação precisará alcançar a fonte natural" => "● Válida — captação precisa alcançar a água",
        "Manutenção aguardando materiais e equipe reais" => "⚠ Manutenção parada — aguarda material e equipe",
        "Manutenção em andamento" => "● Manutenção em andamento",
        "Cancelada; gastos pagos e material consumido não voltam" => "× Cancelada — gastos pagos não voltam",
        _ => cause,
    };
}
