using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

// Registro único dos cenários de QA do Godot. É a fonte de verdade de quais
// flags existem, o que cada uma verifica, se exige janela real, de que
// cidade/fixture precisa e qual método executa. O roteamento em Main.cs usa
// esta tabela e --qa-list a imprime. Não altera nenhuma regra do jogo.
public partial class Main
{
    public enum QaTrigger { Deferred, Performance, BarCapture }

    public sealed record QaScenario(
        string Name,
        string[] Flags,
        string Verifies,
        bool NeedsWindow,
        string Fixture,
        string? Method,
        QaTrigger Trigger = QaTrigger.Deferred);

    public static readonly QaScenario[] QaScenarios =
    {
        new("--construction-paving-smoke",new[]{"--construction-paving-smoke"},"Caminhões por material, pavimentadora/rolo reais em reta/L, custos, parada causal, cancelamento e save/load",true,"nenhuma (cria cidades; saves isolados em artifacts/verification; modelos Construction obrigatórios na validação final)",nameof(ConstructionPavingSmoke)),
        new("--construction-corner-smoke",new[]{"--construction-corner-smoke"},"Rua em L invertido com casa: caminhão espera fora do trajeto do rolo saindo e segue atrás dele, sem giro no lugar nem sobreposição; rua e casa concluem (quantum 2 s)",true,"nenhuma (cria a cidade CornerCity seed 42; saves isolados em artifacts/verification)",nameof(ConstructionCornerSmoke)),
        new("--performance-smoke",new[]{"--performance-smoke"},"FPS e ms de simulação/apresentação; só métricas, sem asserção de correção",false,"opcional via --fixture= (ignora --review-save-dir antes desta correção)",null,QaTrigger.Performance),
        new("--bar-capture",new[]{"--bar-capture"},"Posiciona a Prefeitura e espera --capture= para capturar a barra; não é teste",true,"nenhuma",null,QaTrigger.BarCapture),
        new("--persistence-smoke",new[]{"--persistence-smoke"},"Round-trip de save/load, esboços, migração legada de relógio e aviso de save de versão antiga",false,"nenhuma (cria a cidade)",nameof(PersistenceSmoke)),
        new("--ui-smoke",new[]{"--ui-smoke"},"Fundação 1 clique, catálogo=10, rua 2 cliques, cancelar e inspeção",false,"nenhuma (cria a cidade)",nameof(UiSmoke)),
        new("--ui-input-smoke",new[]{"--ui-input-smoke"},"Gestos reais de mouse/tecla sobre a GUI",true,"nenhuma (cria a cidade)",nameof(UiInputSmoke)),
        new("--recovery-smoke",new[]{"--recovery-smoke"},"Construção completa por GUI, menus e save/load",true,"nenhuma (cria a cidade)",nameof(RecoverySmoke)),
        new("--road-recovery-smoke",new[]{"--road-recovery-smoke"},"Rua: acesso pela porta, demolição e calçadas",true,"nenhuma (cria a cidade)",nameof(RoadRecoverySmoke)),
        new("--demolition-preview-smoke",new[]{"--demolition-preview-smoke"},"Confirmação de rua: custo, indenização inexistente, total, caixa, retirada imediata, ausência de reembolso, locais isolados/obras/viagens, insuficiência de caixa e cancelamento sem alterar a cidade",true,"nenhuma (cria a cidade)",nameof(DemolitionPreviewSmoke)),
        new("--building-demolition-smoke",new[]{"--building-demolition-smoke"},"Demolir prédio concluído: Prefeitura sem Demolir, prévia com remoção/indenização/titular/total/caixa/famílias, cancelar sem mutação, revalidação antes de cobrar, desocupação assistida com save/load, retirada ao prazo sem casa e mudança antes do prazo com compra real; carga privada embarcada aguarda fora das faixas sem destino apto, persiste no save e é entregue fisicamente à loja da mesma empresa; compra paga antes de carregar permanece reservada na fazenda e retoma sem nova cobrança; baia ocupada e percurso corporal a cada 100 ms, saída física do obstáculo e desembarque só após estacionar",true,"nenhuma (cria a cidade; saves isolados em --review-save-dir)",nameof(BuildingDemolitionSmoke)),
        new("--road-tool-playtest",new[]{"--road-tool-playtest"},"Playtest humano do construtor de ruas; produz a fixture do verify",true,"nenhuma (cria a cidade)",nameof(RoadToolPlaytest)),
        new("--grid-alignment-smoke",new[]{"--grid-alignment-smoke"},"Grade × obra na projeção, folga real de 0,75 m",true,"nenhuma (cria a cidade)",nameof(GridAlignmentSmoke)),
        new("--placement-performance-smoke",new[]{"--placement-performance-smoke"},"Desempenho da faixa e da grade; só métricas/hashes, sem asserção de geometria",false,"nenhuma (fixture sintética)",nameof(PlacementPerformanceSmoke)),
        new("--road-tool-verify",new[]{"--road-tool-verify"},"Concluir obras encomendadas com mouse e conferir graus de nó",false,"cidade preparada por --road-tool-playtest (fixture com as obras encomendadas)",nameof(RoadToolVerifyNative)),
        new("--city-names-smoke",new[]{"--city-names-smoke"},"Nomear/renomear/criar cidade e arquivo único",true,"nenhuma (cria a cidade)",nameof(CityNamesSmoke)),
        new("--finances-smoke",new[]{"--finances-smoke"},"Empréstimos, saldos e atualização periódica",true,"cidade povoada com Prefeitura",nameof(FinancesSmoke)),
        new("--activity-smoke",new[]{"--activity-smoke","--activity-before"},"Menu Pessoas ao vivo, morte e migração",true,"cidade real povoada",nameof(ActivitySmoke)),
        new("--circulation-smoke",new[]{"--circulation-smoke"},"Fila na faixa, gap ≥8 m e consumo de combustível",true,"cidade-checkpoint com veículos 1139/1145/505 e rua 619",nameof(CirculationSmoke)),
        new("--interior-arrival-smoke",new[]{"--interior-arrival-smoke"},"SIM some só dentro de um interior real",true,"cidade real com morador em casa",nameof(InteriorArrivalSmoke)),
        new("--pedestrian-recovery-smoke",new[]{"--pedestrian-before","--pedestrian-recovery-smoke"},"Seis pedestres 'Sem rota' recuperam caminho pela pintura real",true,"cidade-checkpoint com 6 pedestres 'Sem rota' e o cidadão 1175",nameof(PedestrianRecoverySmoke)),
        new("--activity-indicators-smoke",new[]{"--activity-indicators-smoke"},"Indicador de carga real; interior esconde o SIM",true,"cópia isolada indicada por --activity-indicators-save=",nameof(ActivityIndicatorsSmoke)),
        new("--problem-alerts-smoke",new[]{"--problem-alerts-smoke"},"Oito causas de alerta, histerese e inspeção por tooltip",true,"nenhuma (cria a cidade)",nameof(ProblemAlertsSmoke)),
        new("--problem-alerts-playtest",new[]{"--problem-alerts-playtest"},"Playtest scriptado dos alertas sob relógio real",true,"nenhuma (cria a cidade)",nameof(ProblemAlertsPlaytest)),
        new("--services-smoke",new[]{"--services-before","--services-smoke"},"Capacidade, equipe e fatores dos serviços",true,"cidade real com utilitários concluídos",nameof(ServicesSmoke)),
        new("--environment-smoke",new[]{"--environment-smoke"},"Malha de água × simulação e pausa da ondulação",true,"nenhuma (cria a cidade)",nameof(EnvironmentSmoke)),
        new("--lake-recovery-smoke",new[]{"--lake-recovery-smoke"},"Geometria do lago, margem e caso legado",true,"nenhuma (seeds 42/1412/2877/1)",nameof(LakeRecoverySmoke)),
        new("--water-intake-range-smoke",new[]{"--water-intake-range-smoke"},"Faixa válida da captação × regra",true,"nenhuma (cria a cidade)",nameof(WaterIntakeRangeSmoke)),
        new("--work-cause-smoke",new[]{"--work-cause-smoke"},"Toda WorkCause tem frase e ícone no menu de Obras",false,"nenhuma (cria a cidade)",nameof(WorkCauseSmoke)),
        new("--visual-review-smoke",new[]{"--visual-review-smoke"},"Capturas de menus, inspeção e prévias",true,"cópia via --fixture=",nameof(VisualReviewSmoke)),
        new("--real-time-start-smoke",new[]{"--real-time-start-smoke"},"1x = tempo real e obra comprada fisicamente",true,"cidade vazia",nameof(RealTimeStartSmoke)),
        new("--interaction-regressions-smoke",new[]{"--interaction-regressions-smoke"},"Câmera, prévia, fileira e orçamento insuficiente",true,"cidade vazia",nameof(InteractionRegressionSmoke)),
        new("--presentation-smoke",new[]{"--presentation-smoke"},"Interior e carga reais; sem balão de presença",true,"nenhuma (cria a cidade)",nameof(PresentationSmoke)),
        new("--real-clock-smoke",new[]{"--real-clock-smoke"},"Relógio real por velocidade e pausa",true,"nenhuma (cria a cidade)",nameof(RealClockSmoke)),
        new("--traffic-recovery-smoke",new[]{"--traffic-recovery-smoke"},"Caminhões saem da paralisação e a oferta é preservada",true,"cidade-checkpoint via --fixture= com caminhões externos ativos",nameof(TrafficRecoverySmoke)),
        new("--smoke",new[]{"--smoke"},"Invariantes e sincronização do mundo numa cidade vazia",false,"nenhuma (cria a cidade)",nameof(Smoke)),
        new("--municipal-relocation-smoke",new[]{"--municipal-relocation-smoke"},"Mover sede/prédio municipal pela inspeção: destino com o mouse, orçamento, cancelar sem mutação, revalidação, obra física, save/load em transição e no prazo de recuperação, Prefeitura única com gestão liberada, Pátio operando durante a obra, estoque por caminhão e perda residual por falta de espaço",true,"nenhuma (cria a cidade; estoque inicial e excesso de estoque do Pátio injetados como fixtures; saves isolados em artifacts/verification/municipal-relocation)",nameof(MunicipalRelocationSmoke)),
        new("--tow-smoke",new[]{"--tow-smoke"},"Guincho: carro e caminhão sem combustível na via, chegada pela frente, plataforma (CarryAnchor) e guincho pesado (LiftAnchor), entrega no posto, fila desfeita e save/load no meio do reboque",true,"nenhuma (cria a cidade)",nameof(TowSmoke)),
        new("--neighbourhood-smoke",new[]{"--neighbourhood-smoke"},"Continuidade de ruas, conexões e obra dependente",true,"nenhuma (cria a cidade)",nameof(NeighbourhoodSmoke)),
    };

    static void PrintQaList()
    {
        GD.Print("CENÁRIOS DE QA (" + QaScenarios.Length + ") · passe as flags depois de --");
        GD.Print("flag | verifica | janela real | cidade/fixture | método");
        foreach(var scenario in QaScenarios)
            GD.Print(string.Join(" | ", string.Join(" ", scenario.Flags), scenario.Verifies, scenario.NeedsWindow ? "sim" : "não", scenario.Fixture, scenario.Method ?? "(embutido)"));
    }

    // Pré-condição de fixture/IDs fixos: falha com UMA mensagem clara em vez de
    // exceção crua de Single/First. Devolve false já encerrando o cenário.
    bool QaPrecondition(bool ok, string scenario, string needs, string command)
    {
        if(ok)return true;
        GD.PrintErr($"INDEXCITIES_QA_PRECONDITION: {scenario} precisa de {needs}; rode {command}");
        GetTree().Quit(1);
        return false;
    }

    void EnablePerformanceSmoke(string[] args)
    {
        _performanceSmoke=true;_speed=1;_autosaveMinutes=0;
        var speedArg=args.FirstOrDefault(a=>a.StartsWith("--performance-speed="));
        if(speedArg!=null&&(!int.TryParse(speedArg[20..],out _speed)||_speed is <1 or >3))throw new ArgumentException("Performance usa somente velocidades aprovadas 1, 2 e 3");
    }

    void EnableBarCapture()
    {
        _speed=0;_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16));_world.Reset(_sim.State);BuildCatalog();RefreshHud();
    }
}
