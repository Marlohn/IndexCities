using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

// Cenário de QA da ordem Demolir de prédio concluído pela interface real:
// prévia, cancelamento sem mutação, revalidação, pendência de desocupação,
// avanço real até a retirada, save/load e mudança para casa existente à venda.
public partial class Main
{
    async void BuildingDemolitionSmoke()
    {
        try {
            Require(DisplayServer.GetName() != "headless", "A ordem Demolir exige janela renderizada real");
            _speed = 0; _autosaveMinutes = 0;
            var directory = _reviewSaveDir ?? System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/building-demolition"));
            System.IO.Directory.CreateDirectory(directory);
            _recoveryCaptureDirectory = directory; _recoveryCaptures.Clear();
            await RecoveryFrame();

            Require(_sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success, "Prefeitura encomendada na fixture");
            Require(_sim.PlaceRoad(new(-192, 0), new(100, 0)).Success, "Rua conectada ao exterior na fixture");
            await RecoveryAdvance(7200);
            var hall = _sim.State.Buildings.Single(b => b.Kind == BuildingKind.CityHall);
            Require(hall.Completed, "Prefeitura concluída fisicamente");
            var placed = _sim.PlaceBuilding(BuildingKind.House, new(-100, 14));
            Require(placed.Success, "Casa encomendada junto à rua");
            var house = _sim.State.Buildings.Single(b => b.Id == placed.Id);
            for (var i = 0; i < 48 && !_sim.State.Families.Any(f => f.HomeId == house.Id); i++) await RecoveryAdvance(1800);
            var family = _sim.State.Families.Single(f => f.HomeId == house.Id);
            var owner = house.OwnerAccount!;
            Require(owner.StartsWith("sim:"), "Família migrante comprou a casa com recursos reais");
            _focus = ToV(house.Position); _zoom = 90; UpdateCamera();

            // Prefeitura concluída não oferece Demolir.
            ClearSelection(); _selectedBuilding = hall.Id; _inspectionKey = ""; RefreshInspection(); await RecoveryFrame();
            Require(!RecoveryNodes<Button>(_inspection!).Any(b => b.Text.Contains("Demolir")), "Prefeitura concluída não mostra Demolir");

            ClearSelection(); _selectedBuilding = house.Id; _inspectionKey = ""; RefreshInspection(); await RecoveryFrame();
            var action = RecoveryNodes<Button>(_inspection!).Single(b => b.Text == "× Demolir com indenização");
            var quote = _sim.QuoteBuildingDemolition(house.Id);
            Require(quote.Allowed && quote.Compensation > 0 && quote.WaitsForResidents, "Orçamento real com indenização e moradores");
            var before = _sim.SaveToJson();
            action.EmitSignal(Godot.Button.SignalName.Pressed);
            Require(_modal != null && _modal.Visible, "Prévia abre a partir da ação na inspeção");
            await RecoveryFrame(); await RecoveryFrame(); await RecoveryFrame();
            var texts = RecoveryNodes<Label>(_modal!).Select(label => label.Text).ToArray();
            bool Has(string fragment) => texts.Any(text => text.Contains(fragment, StringComparison.Ordinal));
            Require(Has("Alvo: Casa #" + house.Id), "Prévia identifica o alvo");
            Require(Has("Custo de remoção: " + Cash(quote.RemovalCost)), "Prévia separa o custo de remoção");
            Require(Has("Indenização (100% do valor de mercado): " + Cash(quote.Compensation) + " → " + quote.OwnerName), "Prévia mostra indenização e titular");
            Require(Has("Total que sai do Caixa: " + Cash(quote.TotalCost)), "Prévia mostra o total");
            Require(Has("Caixa livre: " + Cash(_sim.Available)), "Prévia mostra Caixa livre e compromissos");
            Require(Has("⌂ 1 família(s), " + quote.ResidentIds.Count + " morador(es)"), "Prévia agrega famílias e moradores");
            Require(Has("Não há casa garantida"), "Prévia não promete nova moradia");
            Require(Has("Sem reembolso de obra ou materiais"), "Prévia avisa ausência de reembolso");
            Require(_sim.SaveToJson() == before, "Abrir a prévia não altera a cidade");
            Require(_world.FindChild("BuildingDemolitionPreview", true, false) != null, "Prédio-alvo destacado no mundo durante a prévia");
            await RecoveryCaptureOnce("building-demolition-preview", _modal!);
            await RecoveryCaptureOnce("building-demolition-preview-world", GetViewport());
            RecoveryNodes<Button>(_modal!).Single(b => b.Text == "× Cancelar").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            Require(_modal == null && _sim.SaveToJson() == before, "Cancelar fecha a prévia sem alterar dinheiro nem a cidade");
            await RecoveryFrame();
            Require(_world.FindChild("BuildingDemolitionPreview", true, false) == null, "Cancelar remove o destaque do alvo");

            // Revalidação: o valor muda depois da prévia; confirmar reapresenta sem cobrar.
            ShowBuildingDemolition(house.Id); await RecoveryFrame();
            house.WaterRatio = house.WaterRatio < .5 ? house.WaterRatio + .5 : house.WaterRatio - .5; var changed = _sim.QuoteBuildingDemolition(house.Id);
            Require(changed.Compensation != quote.Compensation, "Valor de mercado mudou depois da prévia");
            var cityBefore = _sim.CityBalance; var ownerBefore = _sim.Balance(owner); var total = _sim.MonetaryTotal;
            RecoveryNodes<Button>(_modal!).Single(b => b.Text == "✓ Confirmar demolição").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            Require(_modal != null && _sim.CityBalance == cityBefore && house.Demolition == null, "Prévia desatualizada é reapresentada sem cobrança");
            Require(RecoveryNodes<Label>(_modal!).Any(l => l.Text.Contains(CitySimulation.StaleDemolitionPreview)), "Reapresentação explica a mudança");
            Require(RecoveryNodes<Label>(_modal!).Any(l => l.Text.Contains(Cash(changed.Compensation))), "Prévia reapresentada traz o novo valor");
            RecoveryNodes<Button>(_modal!).Single(b => b.Text == "✓ Confirmar demolição").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            Require(_modal == null && house.Demolition != null && !house.Removed, "Confirmação abre a desocupação assistida");
            Require(_sim.Balance(owner) == ownerBefore + changed.Compensation && _sim.CityBalance == cityBefore - changed.TotalCost && _sim.MonetaryTotal == total, "Indenização paga ao titular real e dinheiro conservado");

            _inspectionKey = ""; RefreshInspection(); await RecoveryFrame();
            var pending = RecoveryNodes<Label>(_inspection!).Select(l => l.Text).ToArray();
            Require(pending.Any(t => t.Contains("Indenização paga: " + Cash(changed.Compensation))), "Inspeção mostra indenização paga");
            Require(pending.Any(t => t.Contains("Famílias ainda no imóvel: 1 de 1")), "Inspeção mostra famílias aguardando");
            Require(!RecoveryNodes<Button>(_inspection!).Any(b => b.Text.Contains("Demolir")), "Pendência não oferece nova demolição/indenização");
            await RecoveryCaptureOnce("building-demolition-pending-inspection", GetViewport());

            // Save/load com a pendência, sem repetir pagamentos.
            var pendingSave = System.IO.Path.Combine(directory, "building-demolition-pending.json");
            System.IO.File.WriteAllText(pendingSave, _sim.SaveToJson());
            _sim = CitySimulation.FromJson(System.IO.File.ReadAllText(pendingSave)); _world.Reset(_sim.State);
            house = _sim.State.Buildings.Single(b => b.Id == house.Id);
            Require(house.Demolition is { CompensationPaid: > 0 }, "Pendência carregada do save");
            ClearSelection(); _selectedBuilding = house.Id; _inspectionKey = ""; RefreshInspection();

            // Avanço real até o fim do prazo: sem casa à venda, a família fica sem moradia.
            await RecoveryAdvance(_sim.State.Tuning.AssistedVacatingDays * 86400 + 3600);
            Require(house.Removed && _sim.State.Families.Single(f => f.Id == family.Id).HomeId == null, "Ao fim do prazo a casa é retirada e a família fica sem moradia");
            Require(_sim.State.Ledger.Count(m => m.Category == "Indenização" && m.EntityId == house.Id) == 1, "Indenização paga uma única vez após save/load e avanço");
            for (var i = 0; i < 6; i++) await RecoveryFrame();
            Require(_world.PickBuilding(_camera.ProjectRayOrigin(_camera.UnprojectPosition(ToV(house.Position))), _camera.ProjectRayNormal(_camera.UnprojectPosition(ToV(house.Position)))) != house.Id, "Prédio retirado some do mundo renderizado");
            Require(_sim.State.CityEvents.Any(e => e.Description.Contains("Demolição concluída") && e.Description.Contains("ficaram sem moradia") && e.Description.Contains("indenização")), "Resultado real legível: retirada, indenização e família sem moradia");
            RefreshHud(); await RecoveryFrame();
            Require(!_inspector.Visible, "Inspeção do prédio retirado é fechada");
            await RecoveryCaptureOnce("building-demolition-after-deadline", GetViewport());

            // Moradia alternativa real: sem novos migrantes, a casa vaga à venda é comprada pela família deslocada.
            var second = _sim.PlaceBuilding(BuildingKind.House, new(-60, 14)); Require(second.Success, "Segunda casa encomendada");
            var occupied = _sim.State.Buildings.Single(b => b.Id == second.Id);
            for (var i = 0; i < 48 && !_sim.State.Families.Any(f => f.HomeId == occupied.Id); i++) await RecoveryAdvance(1800);
            var moving = _sim.State.Families.Single(f => f.HomeId == occupied.Id);
            foreach (var candidate in _sim.State.FamilyCandidates) candidate.Migrated = true;
            var third = _sim.PlaceBuilding(BuildingKind.House, new(-20, 14)); Require(third.Success, "Casa vaga encomendada");
            var vacant = _sim.State.Buildings.Single(b => b.Id == third.Id);
            for (var i = 0; i < 48 && !vacant.Completed; i++) await RecoveryAdvance(1800);
            Require(vacant.Completed && vacant.OwnerAccount == null, "Casa vaga concluída e à venda");
            _focus = ToV(occupied.Position); UpdateCamera();
            ClearSelection(); _selectedBuilding = occupied.Id; _inspectionKey = ""; RefreshInspection(); await RecoveryFrame();
            RecoveryNodes<Button>(_inspection!).Single(b => b.Text == "× Demolir com indenização").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            RecoveryNodes<Button>(_modal!).Single(b => b.Text == "✓ Confirmar demolição").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            var deadline = occupied.Demolition!.DeadlineSeconds;
            for (var i = 0; i < 4 && !occupied.Removed; i++) await RecoveryAdvance(1800);
            Require(occupied.Removed && _sim.State.ElapsedSeconds < deadline, "Família comprou casa existente e a retirada ocorreu antes do prazo");
            Require(moving.HomeId == vacant.Id && vacant.OwnerAccount is { } bought && moving.Members.Any(id => bought == "sim:" + id), "Compra real pela família deslocada");
            for (var i = 0; i < 6; i++) await RecoveryFrame();
            Require(_sim.State.CityEvents.Any(e => e.Description.Contains("mudaram-se comprando outra casa")), "Resultado real legível: família mudou-se antes do prazo");
            RefreshHud(); await RecoveryFrame();
            await RecoveryCaptureOnce("building-demolition-family-moved", GetViewport());

            // Remessas afetadas na prévia: loja com carga reservada, carga não carregada para ela e carga já embarcada.
            _sim.State.Companies.Add(new Company { Id = 901, Name = "Campo 901" }); _sim.State.Wallets["company:901"] = 0;
            var shop = new Building { Id = _sim.State.NextId++, Kind = BuildingKind.Grocery, Position = new(40, 14), Completed = true };
            shop.Stock[ResourceKind.Food] = 9;
            _sim.State.Buildings.Add(shop);
            var waiting = new Shipment { Id = _sim.State.NextId++, Purpose = ShipmentPurpose.Food, DestinationBuildingId = shop.Id, VehicleId = -1, GoodsOwnerAccount = "company:901", Cargo = new() { { ResourceKind.Food, 20 } } };
            var reserved = new Shipment { Id = _sim.State.NextId++, Purpose = ShipmentPurpose.Food, SourceBuildingId = shop.Id, VehicleId = -1, GoodsOwnerAccount = "company:901", Cargo = new() { { ResourceKind.Food, 4 } } };
            var onboard = new Shipment { Id = _sim.State.NextId++, Purpose = ShipmentPurpose.Food, DestinationBuildingId = shop.Id, PickedUp = true, VehicleId = _sim.State.NextId, GoodsOwnerAccount = "company:901", Cargo = new() { { ResourceKind.Food, 16 } } };
            var carrier = new Vehicle { Id = onboard.VehicleId, OwnerAccount = "company:901", Position = new(40, 20), Status = VehicleStatus.Travelling, Fuel = 40, ShipmentId = onboard.Id };
            carrier.Cargo[ResourceKind.Food] = 16;
            _sim.State.Vehicles.Add(carrier); _sim.State.Shipments.Add(waiting); _sim.State.Shipments.Add(reserved); _sim.State.Shipments.Add(onboard);
            var beforeShipments = _sim.SaveToJson();
            ShowBuildingDemolition(shop.Id); await RecoveryFrame(); await RecoveryFrame();
            var shipmentTexts = RecoveryNodes<Label>(_modal!).Select(label => label.Text).ToArray();
            bool Shows(string fragment) => shipmentTexts.Any(text => text.Contains(fragment, StringComparison.Ordinal));
            Require(Shows("Remessas ainda não carregadas para este prédio: 1 ficam na origem"), "Prévia mostra a remessa não carregada que fica na origem");
            Require(Shows("Remessas de saída ainda não carregadas: 1 cancelada(s)"), "Prévia mostra a remessa de saída que é cancelada");
            Require(Shows("Carga já embarcada para este prédio: 1 caminhão(ões)"), "Prévia distingue carga já embarcada");
            Require(Shows("Sem reembolso nem frete novo") && Shows("busca outro prédio da mesma empresa"), "Prévia explica destino da mesma empresa, sem reembolso nem frete");
            Require(Shows("Estoque perdido na retirada: " + ResourceName(ResourceKind.Food) + " " + (9.0).ToString("N1")), "Estoque perdido não inclui carga embarcada");
            Require(_sim.SaveToJson() == beforeShipments, "Abrir a prévia com remessas não altera a cidade");
            await RecoveryCaptureOnce("building-demolition-shipments-preview", _modal!);
            RecoveryNodes<Button>(_modal!).Single(b => b.Text == "× Cancelar").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            Require(_modal == null && _sim.SaveToJson() == beforeShipments, "Cancelar a prévia com remessas não muda carga, titular nem dinheiro");
            // Os dados fictícios da prévia de remessas saem da cidade antes da fase com caminhão real.
            _sim.State.Vehicles.Remove(carrier); _sim.State.Shipments.RemoveAll(x => x.Id == waiting.Id || x.Id == reserved.Id || x.Id == onboard.Id); _sim.State.Buildings.Remove(shop); _world.Reset(_sim.State);

            // Carga privada real embarcada: importação paga pela empresa, loja demolida pela interface,
            // espera segura sem destino apto, save/load e retomada física quando surge espaço na loja irmã.
            var firm = new Company { Id = _sim.State.NextId++, Name = "Mercado QA", Active = false }; _sim.State.Companies.Add(firm);
            _sim.State.Wallets[firm.Account] = 6000; _sim.State.Wallets["global"] -= 6000;
            Building FirmShop(double x) { var b = new Building { Id = _sim.State.NextId++, Kind = BuildingKind.Grocery, Position = new(x, 14), Completed = true, OperationalCapacity = 1, OwnerAccount = firm.Account, CompanyId = firm.Id }; _sim.State.Buildings.Add(b); firm.Buildings.Add(b.Id); return b; }
            var doomed = FirmShop(80); var sister = FirmShop(60); sister.Stock[ResourceKind.Food] = sister.Definition.Capacity;
            var import = typeof(CitySimulation).GetMethod("OrderImport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            Require((bool)import.Invoke(_sim, [doomed, ResourceKind.Food, 30d, firm.Account])!, "Importação real paga pela empresa titular");
            var cargoShipment = _sim.State.Shipments.Last(x => x.DestinationBuildingId == doomed.Id); var truck = _sim.State.Vehicles.Single(v => v.Id == cargoShipment.VehicleId);
            var cargoAmount = truck.Cargo[ResourceKind.Food];
            for (var i = 0; i < 600 && !(truck.Status == VehicleStatus.Travelling && truck.RouteIndex > 2); i++) { _sim.Advance(1); if (i % 10 == 0) await RecoveryFrame(); }
            Require(truck.Status == VehicleStatus.Travelling, "Caminhão em viagem com a carga");
            _focus = ToV(doomed.Position); UpdateCamera();
            ClearSelection(); _selectedBuilding = doomed.Id; _inspectionKey = ""; RefreshInspection(); await RecoveryFrame();
            RecoveryNodes<Button>(_inspection!).Single(b => b.Text == "× Demolir com indenização").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame(); await RecoveryFrame();
            var cargoTexts = RecoveryNodes<Label>(_modal!).Select(label => label.Text).ToArray();
            Require(cargoTexts.Any(t => t.Contains("Carga já embarcada para este prédio: 1 caminhão(ões)") && t.Contains("titular Mercado QA") && t.Contains("nenhum apto agora")), "Prévia mostra carga embarcada, titular e ausência de destino apto sem prometer");
            await RecoveryCaptureOnce("building-demolition-cargo-preview", _modal!);
            var firmBefore = _sim.Balance(firm.Account); var cargoQuote = _sim.QuoteBuildingDemolition(doomed.Id); var money = _sim.MonetaryTotal;
            RecoveryNodes<Button>(_modal!).Single(b => b.Text == "✓ Confirmar demolição").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            Require(doomed.Removed && _sim.Balance(firm.Account) == firmBefore + cargoQuote.Compensation, "Loja retirada; só a indenização entra na empresa");
            Require(_sim.State.CityEvents.Any(e => e.Description.Contains("mantêm titular e quantidade")), "Resultado explica a carga embarcada");
            for (var i = 0; i < 400 && !(truck.Status == VehicleStatus.Waiting && truck.HoldingPosition is { } spot && truck.Position.Distance(spot) < .1); i++) { _sim.Advance(5); if (i % 10 == 0) await RecoveryFrame(); }
            Require(truck.Status == VehicleStatus.Waiting && truck.Cause.Contains("fora das faixas"), "Sem destino apto, o caminhão aguarda fora das faixas: " + truck.Cause);
            Require(truck.Cargo[ResourceKind.Food] == cargoAmount && cargoShipment.GoodsOwnerAccount == firm.Account && !cargoShipment.Complete, "Carga, titular e remessa preservados na espera");
            Require(!_sim.State.Buildings.Any(b => b.Kind == BuildingKind.WorksDepot && b.Stock.Values.Sum() > 0), "Nenhum Pátio recebe a carga privada");
            _focus = ToV(truck.Position); _zoom = 60; UpdateCamera();
            ClearSelection(); _selectedVehicle = truck.Id; _inspectionKey = ""; RefreshInspection(); for (var i = 0; i < 4; i++) await RecoveryFrame();
            await RecoveryCaptureOnce("building-demolition-cargo-waiting", GetViewport());
            var waitingSave = System.IO.Path.Combine(directory, "building-demolition-cargo-waiting.json");
            System.IO.File.WriteAllText(waitingSave, _sim.SaveToJson());
            _sim = CitySimulation.FromJson(System.IO.File.ReadAllText(waitingSave)); _world.Reset(_sim.State);
            truck = _sim.State.Vehicles.Single(v => v.Id == truck.Id); cargoShipment = _sim.State.Shipments.Single(x => x.Id == cargoShipment.Id); sister = _sim.State.Buildings.Single(b => b.Id == sister.Id);
            Require(truck.HoldingPosition != null && truck.Cargo[ResourceKind.Food] == cargoAmount, "Espera carregada do save com carga e posição");
            // Espaço real na loja irmã: a próxima revisão econômica retoma a entrega física.
            sister.Stock[ResourceKind.Food] = 0;
            for (var i = 0; i < 2000 && !cargoShipment.Complete; i++) { _sim.Advance(2); if (i % 25 == 0) await RecoveryFrame(); }
            Require(cargoShipment.Complete && Math.Abs(sister.Quantity(ResourceKind.Food) - cargoAmount) < 1e-6, "Carga entregue fisicamente na loja da mesma empresa: " + truck.Cause);
            Require(_sim.Balance(firm.Account) == firmBefore + cargoQuote.Compensation && _sim.MonetaryTotal == money, "Retomada sem reembolso, frete ou taxa nova");
            _focus = ToV(sister.Position); UpdateCamera(); ClearSelection(); _selectedBuilding = sister.Id; _inspectionKey = ""; RefreshInspection(); for (var i = 0; i < 4; i++) await RecoveryFrame();
            Require(_sim.State.CityEvents.Any(e => e.Description.Contains("segue a Mercearia #" + sister.Id + " da mesma empresa")), "Resultado legível do redirecionamento");
            RefreshHud(); await RecoveryFrame();
            await RecoveryCaptureOnce("building-demolition-cargo-delivered", GetViewport());

            // Caminhão local com motorista SIM: estaciona junto ao terreno, o motorista desembarca e segue a rotina;
            // a retomada exige destino apto e um trabalhador da empresa que caminha e embarca de verdade.
            var farmFirm = new Company { Id = _sim.State.NextId++, Name = "Campo QA" }; _sim.State.Companies.Add(farmFirm);
            _sim.State.Wallets[farmFirm.Account] = 5000; _sim.State.Wallets["global"] -= 5000;
            var farm = new Building { Id = _sim.State.NextId++, Kind = BuildingKind.Farm, Position = new(-40, -28), Rotation = Math.PI, Completed = true, OperationalCapacity = 1, OwnerAccount = farmFirm.Account, CompanyId = farmFirm.Id, SalaryOffer = 80 };
            farm.Stock[ResourceKind.Food] = 60; _sim.State.Buildings.Add(farm); farmFirm.Buildings.Add(farm.Id);
            firm = _sim.State.Companies.Single(c => c.Id == firm.Id);
            var localTarget = FirmShop(20); sister.Stock[ResourceKind.Food] = sister.Definition.Capacity;
            var dockAt = typeof(CitySimulation).GetMethod("DockAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var farmTruck = new Vehicle { Id = _sim.State.NextId++, OwnerAccount = farmFirm.Account, HomeBuildingId = farm.Id, Fuel = _sim.State.Tuning.FuelTank, Status = VehicleStatus.Parked, Cause = "Na fazenda" };
            farmTruck.Position = (IndexCities.Simulation.Point)dockAt.Invoke(_sim, [_sim.Access(farm)!, farm, false])!; _sim.State.Vehicles.Add(farmTruck); _world.Reset(_sim.State);
            for (var i = 0; i < 200 && !(_sim.State.Now.Hour is >= 8 and < 12); i++) await RecoveryAdvance(1800);
            var driverSim = _sim.State.Citizens.First(c => c.Id == _sim.State.Families.Single(f => f.Id == moving.Id).Members[0]);
            driverSim.WorkplaceId = farm.Id; driverSim.Shift = 0; driverSim.WeeklyDayOff = ((int)_sim.State.Now.DayOfWeek + 3) % 7; driverSim.Path.Clear(); driverSim.PathIndex = 0; driverSim.Position = farm.Door;
            var localOrder = typeof(CitySimulation).GetMethod("OrderLocalFood", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            Require((bool)localOrder.Invoke(_sim, [farm, localTarget, 20d])!, "Pedido local real da loja à fazenda");
            var localShipment = _sim.State.Shipments.Last(x => x.VehicleId == farmTruck.Id);
            for (var i = 0; i < 3000 && !(localShipment.PickedUp && farmTruck.Status == VehicleStatus.Travelling); i++) { _sim.Advance(1); if (i % 20 == 0) await RecoveryFrame(); }
            Require(localShipment.PickedUp && farmTruck.DriverId == driverSim.Id, "Caminhão local carregado com motorista SIM a bordo");
            var localMoney = _sim.MonetaryTotal; var firmLocal = _sim.Balance(firm.Account);
            _focus = ToV(localTarget.Position); UpdateCamera();
            ClearSelection(); _selectedBuilding = localTarget.Id; _inspectionKey = ""; RefreshInspection(); await RecoveryFrame();
            RecoveryNodes<Button>(_inspection!).Single(b => b.Text == "× Demolir com indenização").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame(); await RecoveryFrame();
            Require(RecoveryNodes<Label>(_modal!).Any(l => l.Text.Contains("motorista local desembarca")), "Prévia explica a liberação do motorista local");
            var localQuote = _sim.QuoteBuildingDemolition(localTarget.Id);
            RecoveryNodes<Button>(_modal!).Single(b => b.Text == "✓ Confirmar demolição").EmitSignal(Godot.Button.SignalName.Pressed);
            await RecoveryFrame();
            for (var i = 0; i < 2000 && farmTruck.Status != VehicleStatus.Parked; i++) { _sim.Advance(1); if (i % 20 == 0) await RecoveryFrame(); }
            Require(farmTruck.Status == VehicleStatus.Parked && farmTruck.DriverId == null && driverSim.VehicleId == null && farmTruck.Cause.Contains("junto ao terreno"), "Caminhão local estacionado junto ao terreno e motorista liberado: " + farmTruck.Cause);
            Require(farmTruck.Cargo[ResourceKind.Food] == 20 && localShipment.GoodsOwnerAccount == firm.Account && !localShipment.Complete, "Carga e titular preservados sem motorista");
            for (var i = 0; i < 120; i++) { _sim.Advance(1); if (i % 10 == 0) await RecoveryFrame(); }
            _focus = ToV(farmTruck.Position); _zoom = 50; UpdateCamera();
            ClearSelection(); _selectedVehicle = farmTruck.Id; _inspectionKey = ""; RefreshInspection(); RefreshHud(); for (var i = 0; i < 4; i++) await RecoveryFrame();
            await RecoveryCaptureOnce("building-demolition-local-parked", GetViewport());
            sister.Stock[ResourceKind.Food] = 0; var localBoarded = false;
            for (var i = 0; i < 60000 && !localShipment.Complete; i++) { _sim.Advance(5); localBoarded |= farmTruck.DriverId is { } d && _sim.State.Citizens.Single(c => c.Id == d).VehicleId == farmTruck.Id; if (i % 200 == 0) await RecoveryFrame(); }
            Require(localShipment.Complete && localBoarded && Math.Abs(sister.Quantity(ResourceKind.Food) - 20) < 1e-6, "Retomada com motorista real embarcado e entrega física: " + farmTruck.Cause);
            Require(_sim.Balance(firm.Account) == firmLocal + localQuote.Compensation && _sim.MonetaryTotal == localMoney, "Sem reembolso, frete ou taxa na retomada local");

            _sim.CheckInvariants();
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "building-demolition-final.json"), _sim.SaveToJson());
            GD.Print("INDEXCITIES_BUILDING_DEMOLITION_OK compensation=" + Cash(changed.Compensation) + " removal=" + Cash(changed.RemovalCost) + " stale-revalidated=true cancel=no-change deadline-homeless=true moved-before-deadline=true cargo-waited-safely=true cargo-resumed-same-company=true local-driver-released=true directory=" + directory);
            GetTree().Quit();
        } catch (Exception exception) {
            GD.PushError("Building demolition: " + exception);
            GetTree().Quit(1);
        } finally { _recoveryCaptureDirectory = null; }
    }
}
