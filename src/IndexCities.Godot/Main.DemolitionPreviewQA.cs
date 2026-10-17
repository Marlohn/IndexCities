using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

// Cenário de QA da confirmação de demolição de RUA. Só lê o RoadDemolitionQuote
// existente e a interface: não altera regra, custo, seleção nem revalidação.
public partial class Main
{
    async void DemolitionPreviewSmoke()
    {
        try {
            Require(DisplayServer.GetName() != "headless", "A confirmação de demolição de rua exige janela renderizada real");
            _speed = 0; _autosaveMinutes = 0;
            var directory = _reviewSaveDir ?? System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification"));
            System.IO.Directory.CreateDirectory(directory);
            _recoveryCaptureDirectory = directory; _recoveryCaptures.Clear();
            await RecoveryFrame();

            Require(_sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success, "Prefeitura encomendada na fixture");
            Require(_sim.PlaceRoad(new(-192, 0), new(-80, 0)).Success, "Rua conectada ao exterior na fixture");
            await RecoveryAdvance(7200);
            Require(_sim.State.Buildings.Single(b => b.Kind == BuildingKind.CityHall).Completed, "Prefeitura concluída fisicamente");
            var houseResult = _sim.PlaceBuilding(BuildingKind.House, new(-100, 14));
            Require(houseResult.Success, "Casa da fixture encomendada junto à rua");
            var house = _sim.State.Buildings.Single(b => b.Id == houseResult.Id);
            await RecoveryAdvance(7200);
            Require(_sim.HasExteriorAccess(house), "Casa tem acesso exterior antes do corte");

            var target = _sim.State.Roads.Single(r => r.Position == new Point(-132, 0));
            var quote = _sim.QuoteRoadDemolition(target.Id);
            Require(quote.Allowed, "Corte de rua urbana admitido pelo orçamento da simulação");
            Require(quote.IsolatedBuildingIds.Contains(house.Id), "Corte isola a casa da fixture (quote existente)");
            var before = _sim.SaveToJson(); var balance = _sim.CityBalance; var monetary = _sim.MonetaryTotal;

            ShowRoadDemolition(target.Id);
            Require(_modal != null && _modal.Visible, "Confirmação de rua abre sem remover nem cobrar");
            await RecoveryFrame(); await RecoveryFrame(); await RecoveryFrame();
            var texts = RecoveryNodes<Label>(_modal!).Select(label => label.Text).ToArray();
            bool Has(string fragment) => texts.Any(text => text.Contains(fragment, StringComparison.Ordinal));
            Require(Has("Custo de remoção: " + Cash(quote.Cost)), "Modal informa o custo de remoção real");
            Require(Has("Indenização: não se aplica a esta rua municipal"), "Modal declara que não há indenização para rua municipal");
            Require(Has("Total: " + Cash(quote.Cost)), "Modal informa o total que sairá do Caixa");
            Require(Has("Caixa disponível: " + Cash(_sim.Available)), "Modal mostra a disponibilidade real de Caixa");
            Require(Has("Retirada imediata após confirmar"), "Modal explica que a retirada é imediata");
            Require(Has("Sem reembolso de dinheiro ou materiais"), "Modal avisa a ausência de reembolso de dinheiro e materiais");
            Require(Has(quote.Message), "Modal mantém o texto condicional de perda de acesso do orçamento");
            Require(Has("Construções que perderão conexão exterior: " + quote.IsolatedBuildingIds.Count), "Modal lista a contagem de locais isolados do orçamento");
            Require(Has(house.Definition.Name + " • " + house.Position.X.ToString("N0") + ", " + house.Position.Z.ToString("N0")), "Modal identifica a construção isolada pelo nome e posição");
            Require(Has("Obras com acesso interrompido: " + quote.InterruptedWorks + " • rotas atuais pelo trecho: " + quote.AffectedVehicleRoutes), "Modal resume obras e viagens afetadas pelo orçamento");
            Require(_sim.SaveToJson() == before, "Abrir a confirmação não altera a cidade");

            var confirm = RecoveryNodes<Button>(_modal!).Single(b => b.Text == "✓ Confirmar remoção");
            Require(!confirm.Disabled, "Com Caixa suficiente, confirmar fica disponível");
            Require(confirm.GetViewport().GetVisibleRect().HasPoint(confirm.GetGlobalRect().GetCenter()), "Botão confirmar permanece visível, sem corte");
            var cancel = RecoveryNodes<Button>(_modal!).Single(b => b.Text == "× Cancelar");
            Require(cancel.GetViewport().GetVisibleRect().HasPoint(cancel.GetGlobalRect().GetCenter()), "Botão cancelar permanece visível, sem corte");
            await RecoveryCaptureOnce("road-demolition-preview", _modal!);

            cancel.EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            Require(_modal == null, "Cancelar fecha a confirmação");
            Require(_sim.SaveToJson() == before && _sim.CityBalance == balance && _sim.MonetaryTotal == monetary && _sim.State.Roads.Any(r => r.Id == target.Id), "Cancelar não altera dinheiro, rede nem a cidade");

            var city = _sim.CityBalance; var global = _sim.Balance("global");
            _sim.State.Wallets["global"] += city; _sim.State.Wallets["city"] = 0;
            ShowRoadDemolition(target.Id);
            Require(_modal != null, "Confirmação reabre para conferir insuficiência de Caixa");
            await RecoveryFrame(); await RecoveryFrame();
            var lowTexts = RecoveryNodes<Label>(_modal!).Select(label => label.Text).ToArray();
            Require(lowTexts.Any(text => text.Contains("Caixa livre insuficiente", StringComparison.Ordinal)), "Insuficiência de Caixa explica a causa e o quanto falta");
            Require(RecoveryNodes<Button>(_modal!).Single(b => b.Text == "✓ Confirmar remoção").Disabled, "Caixa insuficiente desabilita confirmar");
            RecoveryNodes<Button>(_modal!).Single(b => b.Text == "× Cancelar").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            _sim.State.Wallets["city"] = city; _sim.State.Wallets["global"] = global;
            Require(_sim.SaveToJson() == before, "Restaurar o Caixa devolve a cidade ao estado original");

            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "road-demolition-preview-city.json"), before);
            _sim.CheckInvariants();
            GD.Print("INDEXCITIES_DEMOLITION_PREVIEW_OK cost=" + Cash(quote.Cost) + " isolated=" + quote.IsolatedBuildingIds.Count + " works=" + quote.InterruptedWorks + " routes=" + quote.AffectedVehicleRoutes + " no-refund=true immediate=true cancel=no-change directory=" + directory);
            GetTree().Quit();
        } catch (Exception exception) {
            GD.PushError("Demolition preview: " + exception);
            GetTree().Quit(1);
        } finally { _recoveryCaptureDirectory = null; }
    }
}
