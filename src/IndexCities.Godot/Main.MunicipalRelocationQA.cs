using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

// Cenário de QA de Mover prédio municipal concluído pela interface real: clique
// no prédio, ação Mover da inspeção, destino escolhido com o mouse na prévia
// comum, orçamento, cancelamento sem mutação, revalidação, confirmação, obra
// física até a conclusão, save/load em transição e transferência do estoque do
// Pátio por caminhão. A cidade é montada pela simulação (fixture); o estoque
// inicial do Pátio é injetado e declarado como fixture.
public partial class Main
{
    async void MunicipalRelocationSmoke()
    {
        try {
            Require(DisplayServer.GetName() != "headless", "Mover prédio municipal exige janela renderizada real");
            _speed = 0; _autosaveMinutes = 0;
            var directory = _reviewSaveDir ?? System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/municipal-relocation"));
            System.IO.Directory.CreateDirectory(directory);
            _recoveryCaptureDirectory = directory; _recoveryCaptures.Clear();
            await RecoveryFrame();

            Require(_sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success, "Prefeitura encomendada na fixture");
            Require(_sim.PlaceRoad(new(-192, 0), new(232, 0)).Success, "Rua conectada ao exterior na fixture");
            await RecoveryAdvance(14400);
            foreach (var x in new[] { -128, -108, -88, -68, -48, -28 }) Require(_sim.PlaceBuilding(BuildingKind.House, new(x, 14)).Success, "Casa da fixture em " + x);
            Require(_sim.PlaceBuilding(BuildingKind.WorksDepot, new(-160, -18), Math.PI).Success, "Pátio de Obras da fixture");
            for (var i = 0; i < 96 && _sim.State.Projects.Any(p => !p.IsMaintenance && p.Status is WorkStatus.Waiting or WorkStatus.InProgress); i++) await RecoveryAdvance(3600);
            var hall = _sim.State.Buildings.Single(b => b.Kind == BuildingKind.CityHall);
            var depot = _sim.State.Buildings.Single(b => b.Kind == BuildingKind.WorksDepot);
            Require(hall.Completed && depot.Completed, "Prefeitura e Pátio concluídos fisicamente");
            for (var i = 0; i < 48 && !_sim.State.Citizens.Any(c => c.WorkplaceId == depot.Id); i++) await RecoveryAdvance(1800);
            Require(_sim.State.Citizens.Any(c => c.WorkplaceId == depot.Id), "Pátio com funcionários reais");
            _world.Sync(_sim.State); BuildCatalog(); RefreshHud();

            // ── Prefeitura: Mover sede ──
            await RecoverySelectBuilding(hall);
            Require(!RecoveryNodes<Button>(_inspection!).Any(b => b.Text.Contains("Demolir")), "Prefeitura concluída não oferece Demolir");
            await RecoveryInspectionButton("↗ Mover sede");
            Require(_relocatingBuilding == hall.Id && _building == BuildingKind.CityHall && !_inspector.Visible, "Ação Mover sede abre a escolha de destino: relocating=" + _relocatingBuilding + " building=" + _building + " inspector=" + _inspector.Visible + " status=" + _status.Text);
            var hallTarget = RelocationDestination(hall);
            var before = _sim.SaveToJson();
            await RecoveryPreviewWorldPoint(hallTarget);
            Require(_sim.SaveToJson() == before && _modal == null, "Prévia de destino sob o cursor não altera a cidade");
            await RecoveryCaptureOnce("hall-destination-hover", GetViewport());
            await RecoveryWorldPoint(hallTarget);
            Require(_modal != null && _modal.Visible && _modal.Name == "MunicipalRelocationModal", "Clique no destino abre o orçamento da mudança: " + RecoveryDiagnostic());
            var quote = _sim.QuoteMunicipalRelocation(hall.Id, hallTarget, _rotation);
            Require(quote.Allowed, "Destino escolhido é válido: " + quote.Message);
            await RecoveryFrame(); await RecoveryFrame();
            var texts = RecoveryNodes<Label>(_modal!).Select(label => label.Text).ToArray();
            bool Has(string fragment) => texts.Any(text => text.Contains(fragment, StringComparison.Ordinal));
            Require(Has("Total comprometido: " + Cash(quote.TotalCost)), "Prévia mostra o total comprometido");
            Require(Has("Nova obra no destino: " + Cash(quote.Construction!.Total)), "Prévia separa a obra");
            Require(Has("Retirada do endereço antigo: " + Cash(quote.RemovalCost)), "Prévia separa a retirada");
            Require(Has("Caixa disponível: " + Cash(_sim.Available)), "Prévia mostra o Caixa");
            Require(Has("Empregos:") && Has("Capacidade atual:"), "Prévia mostra empregos e capacidade");
            Require(Has("gestão"), "Prévia diz que a gestão continua liberada");
            Require(_sim.SaveToJson() == before, "Abrir o orçamento não altera a cidade");
            await RecoveryCaptureOnce("hall-preview", _modal!);
            await RecoveryCaptureOnce("hall-preview-world", GetViewport());
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b => b.Name == "CancelMunicipalRelocation"));
            Require(_modal == null && _sim.SaveToJson() == before && _relocatingBuilding == hall.Id, "Cancelar fecha o orçamento sem mutação e mantém a escolha de destino");

            // Revalidação: o preço do material muda com o orçamento aberto.
            await RecoveryWorldPoint(hallTarget);
            Require(_modal != null, "Orçamento reaberto");
            _sim.State.Tuning.ImportPrices[ResourceKind.Concrete] += 1;
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b => b.Name == "ConfirmMunicipalRelocation"));
            Require(_modal != null && _sim.State.MunicipalRelocations.Count == 0 && _sim.SaveToJson() != before, "Prévia desatualizada é reapresentada sem confirmar");
            Require(RecoveryNodes<Label>(_modal!).Any(l => l.Text.Contains("Os dados mudaram")), "Reapresentação explica a mudança");
            var balance = _sim.CityBalance; var committed = _sim.Committed; var updated = _sim.QuoteMunicipalRelocation(hall.Id, hallTarget, _rotation);
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b => b.Name == "ConfirmMunicipalRelocation"));
            Require(_modal == null && _relocatingBuilding == null, "Confirmação fecha o orçamento e a ferramenta");
            var hallOrder = _sim.State.MunicipalRelocations.Single();
            var newHall = _sim.State.Buildings.Single(b => b.Id == hallOrder.TargetBuildingId);
            Require(!newHall.Completed && !hall.Removed && _sim.CityBalance == balance && _sim.Committed == committed + updated.TotalCost, "Obra substituta encomendada; nada removido e o total fica comprometido");
            Require(_sim.State.CityHallPlaced && RecoveryNodes<Button>(_catalog).Any(b => !b.Disabled && b.TooltipText.StartsWith(Catalog.Definitions[BuildingKind.House].Name)), "Ferramentas de construção continuam liberadas");
            Require(_sim.SetTax(0, .02m).Success, "Gestão continua liberada durante a mudança");
            Require(!_sim.QuoteBuilding(BuildingKind.CityHall, new(200, -60), Math.PI).PhysicallyValid, "Não surge segunda Prefeitura encomendável");
            _inspectionKey = ""; RefreshInspection(); await RecoveryFrame();
            Require(RecoveryNodes<Label>(_inspection!).Any(l => l.Text == "Etapa: obra no novo endereço"), "Inspeção da sede antiga mostra a mudança em andamento");
            Require(!RecoveryNodes<Button>(_inspection!).Any(b => b.Text.Contains("Mover sede")), "Mudança em andamento não oferece novo Mover");
            await RecoveryCaptureOnce("hall-in-progress", GetViewport());

            // Save/load do jogo no meio da obra.
            Require(TrySaveCity(), "Cidade salva no diretório isolado durante a obra");
            var city = _cityId; LoadCity(city); await RecoveryFrame();
            hallOrder = _sim.State.MunicipalRelocations.Single();
            Require(hallOrder.Stage == MunicipalRelocationStage.Constructing && hallOrder.RemovalReserved == updated.RemovalCost, "Etapa e reserva carregadas do save");
            hall = _sim.State.Buildings.Single(b => b.Id == hall.Id); newHall = _sim.State.Buildings.Single(b => b.Id == hallOrder.TargetBuildingId);
            for (var i = 0; i < 48 && hallOrder.Active; i++) await RecoveryAdvance(1800);
            Require(hallOrder.Stage == MunicipalRelocationStage.Completed && newHall.Completed && hall.Removed, "Sede antiga retirada depois da obra física: " + hallOrder.Cause);
            Require(_sim.State.Buildings.Count(b => !b.Removed && b.Kind == BuildingKind.CityHall) == 1 && _sim.State.CityHallPlaced, "Uma única Prefeitura e gestão preservada");
            Require(_sim.State.Ledger.Count(m => m.Category == "Mudança municipal" && m.Reason.StartsWith("Retirada física") && m.EntityId == hall.Id) == 1, "Retirada paga uma única vez depois do save/load");
            Require(_sim.State.CityEvents.Any(e => e.Description.Contains("Mudança de Prefeitura concluída")), "Resultado legível nos eventos");
            _world.Sync(_sim.State); await RecoverySelectBuilding(newHall);
            Require(RecoveryNodes<Label>(_inspection!).Any(l => l.Text.Contains("Mudança de Prefeitura concluída")), "Nova sede mostra a última mudança");
            await RecoveryCaptureOnce("hall-done", GetViewport());

            // ── Pátio de Obras com estoque (fixture) ──
            depot = _sim.State.Buildings.Single(b => b.Id == depot.Id);
            depot.Stock[ResourceKind.Wood] = 30;
            var staff = _sim.State.Citizens.Count(c => c.Alive && c.WorkplaceId == depot.Id);
            await RecoverySelectBuilding(depot);
            await RecoveryInspectionButton("↗ Mover");
            var depotTarget = RelocationDestination(depot);
            await RecoveryWorldPoint(depotTarget);
            Require(_modal != null, "Orçamento da mudança do Pátio: " + RecoveryDiagnostic());
            var depotQuote = _sim.QuoteMunicipalRelocation(depot.Id, depotTarget, _rotation);
            Require(RecoveryNodes<Label>(_modal!).Any(l => l.Text.StartsWith("Frete estimado do estoque: " + Cash(depotQuote.TransferEstimate))), "Prévia mostra o frete do estoque");
            Require(RecoveryNodes<Label>(_modal!).Any(l => l.Text.StartsWith("Estoque: Madeira")), "Prévia mostra o estoque afetado");
            await RecoveryCaptureOnce("depot-preview", _modal!);
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b => b.Name == "ConfirmMunicipalRelocation"));
            var depotOrder = _sim.State.MunicipalRelocations.Single(r => r.SourceBuildingId == depot.Id);
            var newDepot = _sim.State.Buildings.Single(b => b.Id == depotOrder.TargetBuildingId);
            var operatedDuringWork = 0;
            for (var i = 0; i < 400 && depotOrder.Stage == MunicipalRelocationStage.Constructing; i++) {
                Require(!depot.OperationClosed && !depot.Removed && _sim.State.Citizens.Any(c => c.WorkplaceId == depot.Id), "Pátio antigo opera com equipe durante a obra");
                operatedDuringWork++; _sim.Advance(20); _sim.CheckInvariants(); if (i % 10 == 0) await RecoveryFrame();
            }
            Require(depotOrder.Stage != MunicipalRelocationStage.Constructing && operatedDuringWork > 0, "Obra do novo Pátio concluída fisicamente");
            _world.Sync(_sim.State);
            var truck = _sim.State.Vehicles.FirstOrDefault(v => v.ShipmentId is { } sid && depotOrder.TransferShipmentIds.Contains(sid));
            if (truck != null) { _focus = ToV(truck.Position); _zoom = 140; UpdateCamera(); await RecoveryFrame(); await RecoveryCaptureOnce("depot-stock-truck", GetViewport()); }
            for (var i = 0; i < 240 && depotOrder.Active; i++) { _sim.Advance(60); _sim.CheckInvariants(); if (i % 10 == 0) await RecoveryFrame(); }
            Require(depotOrder.Stage == MunicipalRelocationStage.Completed && depot.Removed && newDepot.Completed, "Pátio antigo retirado depois da transferência: " + depotOrder.Cause);
            var moved = _sim.State.Shipments.Where(s => depotOrder.TransferShipmentIds.Contains(s.Id) && s.Complete && !s.Cancelled).Sum(s => s.Cargo.GetValueOrDefault(ResourceKind.Wood));
            var supplied = _sim.State.Shipments.Where(s => s.SourceBuildingId == depot.Id && s.ProjectId != null && s.PickedUp).Sum(s => s.Cargo.GetValueOrDefault(ResourceKind.Wood));
            Require(Math.Abs(moved + supplied - 30) < .01 && depotOrder.StockLost.Count == 0 && depotOrder.TransferShipmentIds.Count > 0, "Madeira levada por caminhão real, sem cópia nem perda: movida " + moved + " usada em obras " + supplied);
            Require(!_sim.State.Citizens.Any(c => c.WorkplaceId == depot.Id) && depotOrder.WorkersContinued + depotOrder.WorkersLeft == staff, "Funcionários reavaliados no novo endereço, sem vínculo duplicado");
            Require(_sim.State.Citizens.Count(c => c.Alive && c.WorkplaceId == newDepot.Id) <= newDepot.Definition.Jobs, "Sem funcionários acima das vagas");
            _world.Sync(_sim.State); await RecoverySelectBuilding(newDepot);
            Require(RecoveryNodes<Label>(_inspection!).Any(l => l.Text.Contains("estoque levado por caminhão")), "Inspeção diagnostica o estoque transferido");
            await RecoveryCaptureOnce("depot-done", GetViewport());

            // Fixture de excesso de estoque: exercita a decisão humana de prazo
            // por falta de espaço, com aviso na prévia e perda persistente.
            newDepot.Stock[ResourceKind.Wood]=100;newDepot.Stock[ResourceKind.Steel]=80;
            await RecoverySelectBuilding(newDepot);await RecoveryInspectionButton("↗ Mover");
            var residualTarget=RelocationDestination(newDepot);await RecoveryWorldPoint(residualTarget);
            Require(_modal!=null&&RecoveryNodes<Label>(_modal!).Any(l=>l.Text.Contains("24 h")&&l.Text.Contains("perda")),"Prévia avisa prazo e perda residual");
            await RecoveryCaptureOnce("residual-preview",_modal!);
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Name=="ConfirmMunicipalRelocation"));
            var residualOrder=_sim.State.MunicipalRelocations.Single(r=>r.SourceBuildingId==newDepot.Id);
            for(var i=0;i<600&&residualOrder.StockRecoveryBlockedSeconds<600;i++)await RecoveryAdvance(60);
            Require(residualOrder.StockRecoveryBlockedSeconds>=600&&!newDepot.Removed&&residualOrder.StockLost.Count==0,"Estoque sem espaço aguarda antes do prazo: "+residualOrder.Cause);
            await RecoverySelectBuilding(newDepot);
            Require(RecoveryNodes<Label>(_inspection!).Any(l=>l.Text.Contains("prazo de perda residual")&&l.Text.Contains("sem espaço")),"Inspeção mostra causa e prazo reais");
            await RecoveryCaptureOnce("residual-waiting",GetViewport());
            var blockedSeconds=residualOrder.StockRecoveryBlockedSeconds;
            Require(TrySaveCity(),"Save isolado durante prazo de recuperação");LoadCity(_cityId);await RecoveryFrame();
            residualOrder=_sim.State.MunicipalRelocations.Single(r=>r.SourceBuildingId==newDepot.Id);
            Require(residualOrder.StockRecoveryBlockedSeconds==blockedSeconds,"Prazo preservado no save/load");
            for(var i=0;i<30&&residualOrder.Active;i++)await RecoveryAdvance(3600);
            Require(residualOrder.Stage==MunicipalRelocationStage.Completed&&residualOrder.StockLost.Values.Sum()>0,"Retirada após prazo baixa resíduo real: "+residualOrder.Cause);
            Require(_sim.State.Ledger.Count(m=>m.Category=="Mudança municipal"&&m.Reason.StartsWith("Retirada física")&&m.EntityId==newDepot.Id)==1,"Retirada residual paga uma vez");
            var residualBuilding=_sim.State.Buildings.Single(b=>b.Id==residualOrder.TargetBuildingId);
            _world.Sync(_sim.State);await RecoverySelectBuilding(residualBuilding);
            Require(RecoveryNodes<Label>(_inspection!).Any(l=>l.Text.Contains("perda do titular city")),"Perda efetiva diagnosticável no novo endereço");
            await RecoveryCaptureOnce("residual-done",GetViewport());

            _sim.CheckInvariants();
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "municipal-relocation-final.json"), _sim.SaveToJson());
            GD.Print("INDEXCITIES_MUNICIPAL_RELOCATION_OK hall-total=" + Cash(updated.TotalCost) + " hall-removal=" + Cash(updated.RemovalCost) + " cancel=no-change stale-revalidated=true save-load=constructing,recovery-deadline depot-wood-moved=" + moved.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " used-by-works=" + supplied.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " workers=" + depotOrder.WorkersContinued + "/" + depotOrder.WorkersLeft + " residual-loss="+residualOrder.StockLost.Values.Sum().ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" directory=" + directory);
            GetTree().Quit();
        } catch (Exception exception) {
            GD.PushError("Municipal relocation: " + exception);
            GetTree().Quit(1);
        } finally { _recoveryCaptureDirectory = null; }
    }
    // Real world click on the building with no tool active opens its inspection.
    async Task RecoverySelectBuilding(Building building)
    {
        ClearTool(); ClearSelection();
        _focus = ToV(building.Position); _zoom = 110; UpdateCamera(); await RecoveryFrame();
        await RecoveryClick(_camera.UnprojectPosition(ToV(building.Position)));
        Require(_selectedBuilding == building.Id && _inspector.Visible, "Clique seleciona " + building.Definition.Name + " #" + building.Id);
    }
    // The inspection lists actions below the details: scroll the panel as the
    // player would, then click the real button.
    async Task RecoveryInspectionButton(string text)
    {
        var button = RecoveryNodes<Button>(_inspection!).Single(b => b.Text == text);
        if (FindScroll(button) is { } scroll) { scroll.EnsureControlVisible(button); await RecoveryFrame(); }
        await RecoveryButton(button);
        static ScrollContainer? FindScroll(Node? node) { for (; node != null; node = node.GetParent()) if (node is ScrollContainer found) return found; return null; }
    }
    // First lot south of the main street, as the cursor would snap to it, where
    // the relocation is allowed with street access.
    Point RelocationDestination(Building source)
    {
        for (var x = 120.0; x <= 220; x += 4) {
            var at = Snap(new Point(x, -(source.Definition.Depth / 2 + 7)));
            var quote = _sim.QuoteMunicipalRelocation(source.Id, at, _rotation);
            if (quote.Allowed && quote.Construction!.Accessible) return at;
        }
        throw new InvalidOperationException("Sem destino acessível para " + source.Definition.Name);
    }
}
