namespace IndexCities.Simulation;

// Textos de causa de obra (WorkProject.Cause). Fonte unica compartilhada entre a
// simulacao, que grava a causa, e a apresentacao, que a traduz em linguagem
// simples. Manter qualquer causa de obra como constante aqui e o que permite o
// QA conferir que toda causa produzida tem uma frase.
public static class WorkCause
{
    public const string NoMaterialsAccess="Sem acesso para materiais/equipe";
    public const string NoRoadAccess="Sem acesso viário";
    public const string NoUrbanRoad="Sem rua urbana: a obra aguardará acesso";
    public const string RoadWithoutExterior="Rua sem ligação ao exterior: a obra aguardará acesso";
    public const string RoadWorkAtEntrance="Rua em obra junto à entrada: aguardando ligação pronta";
    public const string NoReadyExteriorLink="Sem ligação pronta ao exterior: a obra aguardará acesso; ligação planejada não libera entregas";
    public const string RoadBuiltAwaitingLink="Trecho construído; aguardando ligação ao exterior";
    public const string WorkRouteInterrupted="Rota de obra interrompida";
    public const string AwaitingExternalTruck="Aguardando caminhão externo disponível";
    public const string AwaitingPhysicalDelivery="Aguardando entrega física";
    public const string MaterialsInTransit="Materiais em trânsito";
    public const string SupplierOutOfMaterial="Fornecedor externo sem material disponível";
    public const string MissingMaterials="Faltam materiais";
    public const string SupplierOutOfFuel="Fornecedor externo sem combustível";
    public const string AwaitingCrewOnSite="Aguardando equipe física no canteiro";
    public const string RemainingCommitmentInsufficient="Compromisso restante insuficiente";
    public const string CommitmentInsufficientForCrew="Compromisso insuficiente para transportar equipe externa";
    public const string NoBudgetForExternalCrew="Sem orçamento para pagar equipe externa";
    public const string ExternalCrewAwaitingPayment="Equipe externa aguardando pagamento";
    public const string UnderConstruction="Construção em andamento";
    public const string AwaitingPavingMachines="Aguardando pavimentadora e rolo disponíveis";
    public const string PavingMachinesInTransit="Máquinas e operadores em trânsito";
    public const string PavingInProgress="Aplicação e acabamento de asfalto em andamento";
    public const string PavingAwaitingSiteLink="Aguardando ligação física do canteiro";
    public const string PavingOutOfFuel="Máquina de pavimentação sem combustível";
    public const string NoBudgetForPaving="Sem verba restante para máquinas e operadores";
    public const string PavingSiteOccupied="Aguardando espaço ocupado no canteiro";
    public const string PavingInsufficientRange="Autonomia insuficiente para chegar, trabalhar e voltar";
    public const string ValidStretchExteriorAvailable="Trecho válido; ligação exterior disponível";
    public const string ValidPosition="Posição válida";
    public const string IntakeNeedsNaturalSource="A captação precisará alcançar a fonte natural";
    public const string MaintenanceAwaitingMaterials="Manutenção aguardando materiais e equipe reais";
    public const string MaintenanceInProgress="Manutenção em andamento";
    public const string MaintenanceEndedRemoved="Manutenção encerrada: infraestrutura removida; pagamentos e carga preservados";
    public const string MaintenanceDone="Manutenção realizada com materiais e trabalho efetivos";
    public const string Cancelled="Cancelada; gastos pagos e material consumido não voltam";
    public const string Completed="Concluído";

    // Toda causa que a simulacao pode gravar num WorkProject. O QA de apresentacao
    // percorre esta lista e exige uma frase para cada uma.
    public static readonly string[] All={
        NoMaterialsAccess,NoRoadAccess,NoUrbanRoad,RoadWithoutExterior,RoadWorkAtEntrance,
        NoReadyExteriorLink,RoadBuiltAwaitingLink,WorkRouteInterrupted,AwaitingExternalTruck,
        AwaitingPhysicalDelivery,MaterialsInTransit,SupplierOutOfMaterial,MissingMaterials,
        SupplierOutOfFuel,AwaitingCrewOnSite,RemainingCommitmentInsufficient,
        CommitmentInsufficientForCrew,NoBudgetForExternalCrew,ExternalCrewAwaitingPayment,
        UnderConstruction,ValidStretchExteriorAvailable,ValidPosition,IntakeNeedsNaturalSource,
        MaintenanceAwaitingMaterials,MaintenanceInProgress,MaintenanceEndedRemoved,
        MaintenanceDone,Cancelled,Completed,AwaitingPavingMachines,PavingMachinesInTransit,
        PavingInProgress,PavingAwaitingSiteLink,PavingOutOfFuel,NoBudgetForPaving,PavingSiteOccupied,PavingInsufficientRange,
    };
}
