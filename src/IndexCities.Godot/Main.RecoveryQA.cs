using Godot;
using IndexCities.Simulation;

namespace IndexCities.Game;

public partial class Main
{
    string? _recoveryCaptureDirectory;
    readonly HashSet<string> _recoveryCaptures = new();
    async Task RecoveryCapture(string path, Viewport viewport)
    {
        await RecoveryFrame();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        var image = viewport.GetTexture().GetImage();
        Require(image != null && !image.IsEmpty(), "Viewport possui imagem renderizada para revisão visual");
        var error = image!.SavePng(path);
        Require(error == Error.Ok, "Captura do viewport foi salva: " + path + " / " + error);
        GD.Print("RECOVERY_VIEWPORT_CAPTURE " + path);
    }

    async Task RecoveryCaptureOnce(string name, Viewport viewport)
    {
        if (_recoveryCaptureDirectory != null && _recoveryCaptures.Add(name))
            await RecoveryCapture(System.IO.Path.Combine(_recoveryCaptureDirectory, name + ".png"), viewport);
    }

    static string RecoveryMenuName(string tooltip) => tooltip switch {
        "Finanças e impostos" => "menu-finances", "SIMs e empresas" => "menu-people",
        "Causas, obras e entregas" => "menu-alerts", "Controles" => "menu-controls", _ => "menu-cities"
    };
    // An engine/input integration check. It does not certify human usability.
    // Placement uses viewport events and GUI hit testing, never handler calls.
    async Task RecoveryFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    void RecoveryInput(InputEvent input, Viewport? viewport = null)
    {
        if (viewport is Window window && viewport != GetViewport() && input is InputEventFromWindow windowInput) {
            var target = window;
            while (target.IsEmbedded()) {
                if (input is InputEventMouse embeddedMouse) {
                    embeddedMouse.Position += (Vector2)target.Position;
                    embeddedMouse.GlobalPosition += (Vector2)target.Position;
                }
                target = target.GetParent().GetWindow();
            }
            windowInput.WindowId = target.GetWindowId();
            Input.ParseInputEvent(input);
            if (input is InputEventMouseButton mouse) GD.Print("RECOVERY_GUI_EVENT viewport=" + viewport.GetPath() + " position=" + mouse.Position + " pressed=" + mouse.Pressed + " handled=" + viewport.IsInputHandled());
        }
        else if (viewport != null && viewport != GetViewport()) viewport.PushInput(input, true);
        else Input.ParseInputEvent(input);
    }

    // Returns the pixel actually under the native cursor. The synthetic event uses
    // that same pixel, so hover previews and the following click cannot disagree
    // by the sub-pixel rounding of WarpMouse (an intermittent QA-only divergence).
    async Task<Vector2> RecoveryMove(Vector2 position, Viewport? viewport = null)
    {
        var actual = await RecoveryAlignNativeMouse(position, viewport ?? GetViewport());
        RecoveryInput(new InputEventMouseMotion { Position = actual, GlobalPosition = actual }, viewport);
        await RecoveryFrame();
        return actual;
    }

    async Task<Vector2> RecoveryAlignNativeMouse(Vector2 position, Viewport viewport)
    {
        var modes = new[] { "--ui-input-smoke", "--recovery-smoke", "--visual-review-smoke", "--real-time-start-smoke", "--interaction-regressions-smoke" };
        Require(OS.GetCmdlineUserArgs().Any(modes.Contains), "Foco e cursor nativos são usados somente na janela de QA isolada");
        Require(viewport.GetVisibleRect().HasPoint(position), "Cursor de QA permanece dentro do viewport conhecido");
        var nativeWindow = viewport is Window window ? window : viewport.GetWindow();
        var nativePosition = position;
        while (nativeWindow.IsEmbedded()) { nativePosition += (Vector2)nativeWindow.Position; nativeWindow = nativeWindow.GetParent().GetWindow(); }
        Require(nativeWindow == GetWindow(), "Eventos de QA só podem focar sua própria janela da engine");
        nativeWindow.GrabFocus();
        var focusWatch = System.Diagnostics.Stopwatch.StartNew();
        while (!nativeWindow.HasFocus() && focusWatch.Elapsed.TotalSeconds < 2) await RecoveryFrame();
        Require(nativeWindow.HasFocus(), "A janela conhecida de QA recebeu foco antes de posicionar seu cursor");
        // WarpMouse takes pixels inside the focused manager window, never
        // desktop coordinates. Embedded windows add their local origins.
        nativePosition = nativeWindow.GetFinalTransform() * nativePosition;
        Require(new Rect2(Vector2.Zero, nativeWindow.Size).HasPoint(nativePosition), "Posição física de QA permanece dentro de sua janela da engine");
        Input.WarpMouse(nativePosition);
        await RecoveryFrame();
        var actual = viewport.GetMousePosition();
        // Native cursor updates can lag a rendered frame after focus/layout.
        // Retry only this known window, retaining the same strict position check.
        var cursorWatch = System.Diagnostics.Stopwatch.StartNew();
        while (actual.DistanceTo(position) > 2.5f && cursorWatch.Elapsed.TotalSeconds < 1) {
            Input.WarpMouse(nativePosition);
            await RecoveryFrame();
            actual = viewport.GetMousePosition();
        }
        Require(actual.DistanceTo(position) <= 2.5f, "Cursor nativo e evento de QA concordam no viewport: pedido=" + position + " atual=" + actual + " transformação=" + viewport.GetScreenTransform() + " físico=" + DisplayServer.MouseGetPosition() + " origem=" + nativeWindow.Position + " foco=" + nativeWindow.HasFocus() + " modo=" + Input.MouseMode);
        return actual;
    }

    async Task RecoveryClick(Vector2 position, Viewport? viewport = null)
    {
        var actual = await RecoveryMove(position, viewport);
        RecoveryInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true, Position = actual, GlobalPosition = actual }, viewport);
        await RecoveryFrame();
        RecoveryInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = actual, GlobalPosition = actual }, viewport);
        await RecoveryFrame();
    }

    async Task RecoveryKey(Key key, Viewport? viewport = null)
    {
        RecoveryInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true }, viewport);
        await RecoveryFrame();
        RecoveryInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false }, viewport);
        await RecoveryFrame();
    }

    static IEnumerable<T> RecoveryNodes<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren()) {
            if (child is T found) yield return found;
            foreach (var nested in RecoveryNodes<T>(child)) yield return nested;
        }
    }

    async Task RecoveryButton(Button button)
    {
        Require(button.IsVisibleInTree() && !button.Disabled, "Botão disponível: " + button.Text + " / " + button.TooltipText);
        // Menus recém-abertos ainda podem se redimensionar e recentralizar (lista maior,
        // rolagem). Usa o retângulo só depois de ficar igual em quadros consecutivos.
        // O retangulo e local a janela do menu; a propria janela tambem pode mudar de
        // posicao/tamanho, entao ambos precisam ficar estaveis.
        var rectangle = button.GetGlobalRect();
        var menuWindow = button.GetViewport() as Window;
        var placement = menuWindow == null ? new Rect2I() : new Rect2I(menuWindow.Position, menuWindow.Size);
        for (var settle = 0; settle < 30; settle++) {
            await RecoveryFrame();
            var current = button.GetGlobalRect();
            var currentPlacement = menuWindow == null ? new Rect2I() : new Rect2I(menuWindow.Position, menuWindow.Size);
            if (current == rectangle && currentPlacement == placement) break;
            rectangle = current;
            placement = currentPlacement;
        }
        Require(button.GetViewport().GetVisibleRect().HasPoint(rectangle.GetCenter()), "Botão cabe na janela: " + button.Text);
        var viewport = button.GetViewport();
        await RecoveryMove(rectangle.GetCenter(), viewport);
        var hovered = viewport.GuiGetHoveredControl();
        if (viewport != GetViewport()) GD.Print("RECOVERY_GUI_HIT button=" + button.GetPath() + " text=" + button.Text + " rect=" + rectangle + " viewport=" + viewport.GetPath() + " windowId=" + (viewport is Window window ? window.GetWindowId().ToString() : "none") + " embedded=" + (viewport is Window embeddedWindow && embeddedWindow.IsEmbedded()) + " windowPosition=" + (viewport is Window positionedWindow ? positionedWindow.Position.ToString() : "none") + " hovered=" + (hovered?.GetPath().ToString() ?? "none") + " hoveredRect=" + (hovered?.GetGlobalRect().ToString() ?? "none") + " mouseFilter=" + button.MouseFilter + " handleLocally=" + viewport.HandleInputLocally + " guiDisabled=" + viewport.GuiDisableInput + " finalTransform=" + viewport.GetFinalTransform());
        await RecoveryClick(rectangle.GetCenter(), button.GetViewport());
    }

    Button RecoveryCatalog(BuildingKind kind) => RecoveryNodes<Button>(_catalog).Single(button => button.TooltipText.Split('\n')[0] == Catalog.Definitions[kind].Name);
    Button RecoveryRoadButton() => RecoveryNodes<Button>(_catalog).Single(button => button.TooltipText.StartsWith("Rua 1+1", StringComparison.Ordinal));
    Button RecoveryHudButton(string tooltip) => RecoveryNodes<Button>(_canvas).Single(button => button.TooltipText == tooltip);
    Button RecoverySpeedButton(int speed) => _speedButtons.Single(item => item.Speed == speed).Button;
    string RecoveryDiagnostic() => string.Join(" ", new[] { _toolHint.Text, _status.Text }.Concat(RecoveryNodes<Label>(_confirmBody).Select(label => label.Text)));

    async Task RecoveryPreviewWorldPoint(Point point)
    {
        // Camera positioning only keeps this check independent of resolution.
        // The world placement itself still passes through picking and GUI filters.
        _focus = ToV(point);
        _zoom = 110;
        UpdateCamera();
        await RecoveryFrame();
        await RecoveryMove(_camera.UnprojectPosition(ToV(point)));
    }

    async Task RecoveryWorldPoint(Point point) { await RecoveryPreviewWorldPoint(point); await RecoveryClick(_camera.UnprojectPosition(ToV(point))); }

    async Task<WorkProject> RecoveryPlace(BuildingKind kind, Point position, bool reversed = false, bool choose = true)
    {
        if (choose) await RecoveryButton(RecoveryCatalog(kind));
        if (reversed) { await RecoveryKey(Key.R); await RecoveryKey(Key.R); }
        var before = _sim.SaveToJson();
        await RecoveryPreviewWorldPoint(position);
        Require(_cursor.Distance(position) <= 4.01, "Picking posiciona " + kind + " perto do ponto escolhido: " + position + " → " + _cursor);
        var previewPosition = _cursor;
        var previewRotation = _rotation;
        var quote = _sim.QuoteBuilding(kind, previewPosition, _rotation);
        Require(quote.PhysicallyValid, "Encaixe de " + kind + " respeita espaço físico: " + quote.Message);
        if (kind != BuildingKind.CityHall) Require(quote.Accessible, "Encaixe de " + kind + " mantém acesso real à rua: " + quote.Message);
        Require(_sim.SaveToJson() == before, "Prévia de " + kind + " não muda a simulação");
        var preview = _world.FindChild("BuildingPreview", true, false) as Node3D;
        Require(preview != null && preview.Position.DistanceTo(ToV(previewPosition)) < .05f, "Prévia 3D acompanha posição encaixada de " + kind);
        var model = preview!.GetNodeOrNull<Node3D>("TransparentModel");
        Require(model != null && RecoveryNodes<MeshInstance3D>(model).Any(), "Prévia tem o modelo reconhecível de " + kind);
        Require(preview.GetNodeOrNull<MeshInstance3D>("EntranceDirection") != null, "Entrada destacada na prévia de " + kind);
        foreach (var mesh in RecoveryNodes<MeshInstance3D>(model!).Where(mesh => mesh.Mesh != null)) {
            for (var surface = 0; surface < mesh.Mesh!.GetSurfaceCount(); surface++) {
                var material = mesh.MaterialOverride ?? mesh.GetSurfaceOverrideMaterial(surface) ?? mesh.Mesh.SurfaceGetMaterial(surface);
                Require(material is BaseMaterial3D { Transparency: BaseMaterial3D.TransparencyEnum.Alpha } baseMaterial && baseMaterial.AlbedoColor.A < 1, "Modelo da prévia é transparente: " + kind);
            }
        }
        await RecoveryCaptureOnce("building-preview", GetViewport());
        var ids = _sim.State.Projects.Select(project => project.Id).ToHashSet();
        await RecoveryClick(_camera.UnprojectPosition(ToV(position)));
        var project = _sim.State.Projects.Single(project => !ids.Contains(project.Id));
        Require(project.BuildingId != null && ! _sim.State.Buildings.Single(building => building.Id == project.BuildingId).Completed, "Confirmar cria uma obra física de " + kind);
        Require(_sim.State.Buildings.Single(building => building.Id == project.BuildingId).Position == previewPosition, "Obra confirmada mantém a posição mostrada pela prévia de " + kind);
        Require(_anchor == null && _building == kind && Math.Abs(_rotation - previewRotation) < .000001, "Tipo e rotação do prédio continuam disponíveis para repetir");
        return project;
    }

    async Task<WorkProject> RecoveryRoad(Point start, Point end, bool reverse = false, bool choose = false)
    {
        if (choose) await RecoveryButton(RecoveryRoadButton());
        Require(_roadTool, "Ferramenta de rua permanece ativa");
        var before = _sim.SaveToJson();
        await RecoveryWorldPoint(start);
        Require(_roadStart == start, "Primeiro clique inicia a rua");
        Require(_sim.SaveToJson() == before, "Primeiro clique não cria obra");
        _focus = ToV(end); UpdateCamera(); await RecoveryFrame();
        await RecoveryMove(_camera.UnprojectPosition(ToV(end)));
        Require(_roadEnd == end, "Cursor acompanha o fim da rua");
        if (_reverse != reverse) await RecoveryKey(Key.Tab);
        Require(_sim.SaveToJson() == before, "Primeiro clique e prévia não alteram dinheiro ou obras");
        var shape = _sim.RoadShape(start, end, _reverse).Where(point => _sim.State.Roads.All(road => road.Position.Distance(point) > .05)).ToArray();
        await RecoveryCaptureOnce("road-before-order", GetViewport());
        var ids = _sim.State.Projects.Select(project => project.Id).ToHashSet();
        await RecoveryClick(_camera.UnprojectPosition(ToV(end)));
        var project = _sim.State.Projects.Single(project => !ids.Contains(project.Id));
        Require(project.RoadPoints.Count > 0 && project.Status != WorkStatus.Completed, "A rua nasce como obra real");
        Require(project.RoadPoints.SequenceEqual(shape), "Segundo clique encomenda exatamente o traçado que estava na prévia");
        Require(_roadTool && _roadStart == null && _roadEnd == null, "Pode iniciar a próxima rua sem reabrir o catálogo");
        return project;
    }

    async Task RecoveryAdvance(double seconds)
    {
        // Same deterministic simulation processing as normal play, only faster.
        for (double elapsed = 0; elapsed < seconds; elapsed += 3600) {
            _sim.Advance(Math.Min(3600, seconds - elapsed));
            _sim.CheckInvariants();
            await RecoveryFrame();
        }
    }

    async Task RecoveryCloseModal()
    {
        Require(_modal != null && _modal.Visible, "Janela de gestão abriu");
        var close = RecoveryNodes<Button>(_modal!).FirstOrDefault(button => button.TooltipText == "Fechar janela" || button.Text == "Fechar");
        Require(close != null, "Janela possui botão visível para fechar");
        await RecoveryButton(close!);
        Require(_modal == null, "Clique fecha a janela de gestão");
    }

    async void RecoverySmoke()
    {
        var milestones = new List<string>();
        try {
            Require(DisplayServer.GetName() != "headless", "A recuperação deve ser conferida em janela real, sem --headless");
            _speed = 0; _autosaveMinutes = 0;
            var directory = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification"));
            _recoveryCaptureDirectory = System.IO.Path.Combine(directory, "recovery-visual");
            _recoveryCaptures.Clear();
            CitySaves.VerificationDirectory = System.IO.Path.Combine(directory, "recovery-saves-" + Guid.NewGuid().ToString("N"));
            await RecoveryFrame();
            Require(!_sim.State.CityHallPlaced && _managementButtons.All(button => button.Disabled), "Partida começa vazia com gestão bloqueada");
            await RecoveryPlace(BuildingKind.CityHall, new(-152, 16));
            Require(_sim.State.CityHallPlaced && _managementButtons.All(button => !button.Disabled), "Fundação libera gestão sem concluir a Prefeitura");
            Require(Enum.GetValues<BuildingKind>().All(kind => RecoveryCatalog(kind).Icon != null), "Todos os tipos são reconhecíveis por imagem");
            milestones.Add("fundação pela GUI");
            GD.Print("RECOVERY_STAGE founding-complete");

            var first = await RecoveryRoad(new(-192, 0), new(-128, 0), choose: true);
            Require(_world.GetNodeOrNull<Node3D>("RoadWork" + first.Id) != null, "Obra viária confirmada continua visível");
            await RecoveryAdvance(7200);
            Require(first.Status == WorkStatus.Completed, "Primeira rua recebeu materiais e equipe");
            var second = await RecoveryRoad(new(-128, 0), new(240, 0));
            await RecoveryAdvance(14400);
            Require(second.Status == WorkStatus.Completed, "Segunda rua funciona sem reabrir o catálogo");
            Require(_sim.State.Buildings.Single().Completed, "A Prefeitura foi concluída por execução física");

            var beforeInvalid = _sim.SaveToJson();
            await RecoveryWorldPoint(new(-192, 0)); await RecoveryWorldPoint(new(-128, 0));
            Require(_roadStart == new Point(-192, 0), "Rua recusada mantém início para corrigir fim");
            var refused = _sim.QuoteRoad(new(-192, 0), new(-128, 0));
            Require(RecoveryDiagnostic().Contains(refused.Message, StringComparison.Ordinal), "Recusa explica a causa real na interface");
            Require(_sim.SaveToJson() == beforeInvalid, "Segundo clique inválido não altera obra nem caixa");
            await RecoveryKey(Key.Escape);
            Require(_roadStart == null && _roadEnd == null, "Escape limpa a prévia inválida");
            var corner = await RecoveryRoad(new(-184, 0), new(116, 44), reverse: true, choose: true);
            await RecoveryAdvance(7200);
            Require(corner.Status == WorkStatus.Completed, "Rua em L terminou por execução física");
            milestones.Add("ruas retas repetidas, L, recusa causal e cancelamento");
            GD.Print("RECOVERY_STAGE repeat-straight-L-roads-complete");

            var xs = new[] { -128, -108, -88, -68, -48, -28, -8, 12, 32, 52, 72, 92, 112, 132, 152, 172, 192, 212 };
            for (var index = 0; index < xs.Length; index++) await RecoveryPlace(BuildingKind.House, new(xs[index], 14), choose: index == 0);
            foreach (var x in new[] { -160, -140, -120, -100, -80, -60, -40, -20, 0, 20, 40, 60, 80, 100 }) await RecoveryPlace(BuildingKind.House, new(x, 58), choose: false);
            await RecoveryPlace(BuildingKind.WorksDepot, new(-160, -18), reversed: true);
            await RecoveryPlace(BuildingKind.Grocery, new(-124, -16), reversed: true);
            await RecoveryPlace(BuildingKind.FuelStation, new(-88, -20), reversed: true);
            await RecoveryPlace(BuildingKind.Farm, new(-44, -28), reversed: true);
            await RecoveryPlace(BuildingKind.WindPower, new(0, -16), reversed: true);
            await RecoveryPlace(BuildingKind.WindPower, new(120, -16), reversed: true);
            await RecoveryPlace(BuildingKind.WaterTreatment, new(40, -20), reversed: true);
            await RecoveryPlace(BuildingKind.SewageTreatment, new(80, -20), reversed: true);
            var lake = _sim.State.LakeCentre;
            var z = Math.Round((lake.Z - _sim.State.LakeRadius - 30) / 4) * 4;
            var intakeRoad = await RecoveryRoad(new(240, 0), new(132, z), reverse: true, choose: true);
            await RecoveryAdvance(14400);
            Require(intakeRoad.Status == WorkStatus.Completed, "Rua da captação concluída");
            await RecoveryPlace(BuildingKind.WaterIntake, new(144, z + 14));
            milestones.Add("catálogo completo colocado com mouse e confirmação");
            GD.Print("RECOVERY_STAGE full-catalog-placement-complete");

            bool water = false, power = false, produced = false, purchased = false;
            for (var hour = 0; hour < 48; hour++) {
                await RecoveryAdvance(3600);
                water |= _sim.State.PotableCapacity > 0;
                power |= _sim.State.ElectricityCapacity > 0;
                produced |= _sim.State.Buildings.Any(building => building.FoodProduced > 0);
                purchased |= _sim.State.Ledger.Any(entry => entry.Category == "Alimentos");
                if ((hour + 1) % 12 == 0) GD.Print($"RECOVERY_CYCLE hour={hour + 1} complete={_sim.State.Buildings.Count(building => building.Completed)} population={_sim.Population} water={_sim.State.PotableCapacity:N1} power={_sim.State.ElectricityCapacity:N1}");
            }
            Require(_sim.State.Buildings.Where(building => !building.Removed).All(building => building.Completed), "Todas as obras executaram: " + string.Join(" | ", _sim.State.Projects.Where(project => project.Status != WorkStatus.Completed).Select(project => project.Cause)));
            Require(_sim.Population > 0 && _sim.State.Companies.Count > 0, "Pessoas e empresas entraram por processos reais");
            Require(water && power && produced && purchased, "Água, energia, produção e compra de alimentos aconteceram realmente");
            Require(_sim.State.Ledger.Any(entry => entry.Category.StartsWith("Salários", StringComparison.Ordinal)), "Trabalho gerou pagamentos reais");
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "recovery-city.json"), _sim.SaveToJson());
            milestones.Add("cidade operando com pessoas, empresas, serviços e pagamentos reais");
            GD.Print("RECOVERY_STAGE actual-operation-complete");
            _focus = new(-40, 0, 0); _zoom = 250; UpdateCamera();
            await RecoveryCaptureOnce("city", GetViewport());

            await RecoveryKey(Key.Escape);
            var inspected = _sim.State.Buildings.Single(building => building.Kind == BuildingKind.Grocery);
            await RecoveryWorldPoint(inspected.Position);
            Require(_selectedBuilding == inspected.Id && _inspector.Visible, "Clique no modelo abre inspeção da mercearia");
            Require(RecoveryNodes<Label>(_inspection).Any(label => label.Text.Contains(inspected.Cause, StringComparison.Ordinal)), "Inspeção apresenta a causa atual");
            await RecoveryCaptureOnce("inspection", GetViewport());
            foreach (var tooltip in new[] { "Finanças e impostos", "SIMs e empresas", "Causas, obras e entregas", "Controles" }) {
                await RecoveryButton(RecoveryHudButton(tooltip));
                Require(RecoveryNodes<Label>(_modal!).Any(), "Menu contém dados legíveis: " + tooltip);
                await RecoveryCaptureOnce(RecoveryMenuName(tooltip), _modal!);
                await RecoveryCloseModal();
            }
            milestones.Add("inspeção e menus abertos/fechados pela GUI");
            GD.Print("RECOVERY_STAGE inspection-and-menus-complete");

            var firstCity = _cityId;
            await RecoveryButton(RecoveryCatalog(BuildingKind.House));
            await RecoveryPreviewWorldPoint(new(232, 14));
            var savedCursor = _cursor;
            var savedRotation = _rotation;
            var expected = _sim.SaveToJson();
            await RecoveryButton(RecoveryHudButton("Salvar · F5"));
            Require(CitySaves.Load(firstCity).CityJson == expected, "Botão Salvar grava a simulação integral");
            await RecoveryButton(RecoveryHudButton("Cidades e autosave"));
            await RecoveryCaptureOnce("menu-cities", _modal!);
            var create = RecoveryNodes<Button>(_modal!).Single(button => button.Text == "+ Criar" || button.Text == "Criar cidade");
            await RecoveryButton(create);
            Require(_cityId != firstCity && !_sim.State.CityHallPlaced, "Menu cria cidade independente");
            await RecoveryButton(RecoveryHudButton("Cidades e autosave"));
            var savedCity = CitySaves.List().Single(saved => saved.Id == firstCity);
            var savedLabel = savedCity.Name + " • " + savedCity.At.ToString("dd/MM/yyyy HH:mm");
            var load = RecoveryNodes<Button>(_modal!).Single(button => button.Text == savedLabel);
            await RecoveryButton(load);
            Require(_cityId == firstCity && _sim.SaveToJson() == expected && _cursor == savedCursor && _rotation == savedRotation && _building == BuildingKind.House, "Menu retoma cidade e prévia livre exata sem encomendar nada");
            var copy = CitySimulation.FromJson(expected); copy.Advance(120);
            await RecoveryAdvance(120);
            Require(_sim.SaveToJson() == copy.SaveToJson(), "Partida retomada continua de forma determinística");
            milestones.Add("salvar, criar outra cidade, retomar prévia e continuar");
            var report = new { status = "technical-input-check-passed", humanUsabilityReview = "pending", milestones, buildings = _sim.State.Buildings.Count, population = _sim.Population, companies = _sim.State.Companies.Count, monetaryTotal = _sim.MonetaryTotal, savesDirectory = CitySaves.DirectoryPath };
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "recovery-input-report.json"), System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            GD.Print("INDEXCITIES_RECOVERY_OK " + System.Text.Json.JsonSerializer.Serialize(report));
            GetTree().Quit();
        } catch (Exception exception) {
            try {
                var directory = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification"));
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "recovery-failure-city.json"), _sim.SaveToJson());
                var pending = _sim.State.Projects.Where(project => project.Status != WorkStatus.Completed).Select(project => new {
                    project.Id, project.Status, project.BuildingId, project.Progress, project.Cause,
                    project.CostPaid, project.BudgetRemaining, project.Required, project.Delivered,
                    project.LocalReserved, project.CrewCostDue, project.RoadPoints
                }).ToArray();
                var diagnostic = new {
                    status = "technical-input-check-failed", humanUsabilityReview = "pending",
                    milestones, failure = exception.ToString(), _sim.State.Now,
                    buildings = _sim.State.Buildings.Count, population = _sim.Population,
                    _sim.State.RawWaterCapacity, _sim.State.PotableCapacity, _sim.State.ElectricityCapacity,
                    pendingProjects = pending,
                    buildingsState = _sim.State.Buildings.Select(building => new {
                        building.Id, building.Kind, building.Position, building.Rotation, building.Completed,
                        building.ProjectId, building.OwnerAccount, building.CompanyId, building.FamilyId,
                        building.OperationalCapacity, building.WaterRatio, building.ElectricityRatio, building.Cause
                    }).ToArray()
                };
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "recovery-failure-report.json"), System.Text.Json.JsonSerializer.Serialize(diagnostic, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                GD.Print("RECOVERY_FAILURE_PENDING " + System.Text.Json.JsonSerializer.Serialize(pending));
            } catch (Exception diagnosticException) {
                GD.PushError("Falha ao preservar diagnóstico de recuperação: " + diagnosticException);
            }
            GD.PushError("Recovery milestones: " + string.Join("; ", milestones) + "\n" + exception);
            GetTree().Quit(1);
        } finally { CitySaves.VerificationDirectory = null; _recoveryCaptureDirectory = null; }
    }

    async void VisualReviewSmoke()
    {
        try {
            Require(DisplayServer.GetName() != "headless", "A revisão visual exige renderização em uma janela real");
            Require(OS.GetCmdlineUserArgs().Any(argument => argument.StartsWith("--fixture=", StringComparison.Ordinal)), "Revisão visual requer --fixture com estado real da cidade");
            _speed = 0; _autosaveMinutes = 0;
            var before = _sim.SaveToJson();
            _recoveryCaptureDirectory = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/visual-review"));
            _recoveryCaptures.Clear();
            await RecoveryKey(Key.Escape);
            _focus = new(-40, 0, 0); _zoom = 250; UpdateCamera();
            await RecoveryCaptureOnce("city", GetViewport());

            await RecoveryButton(RecoveryCatalog(BuildingKind.House));
            Point? candidate = null;
            foreach (var road in _sim.State.Roads.Where(road => !road.Highway)) {
                foreach (var offset in new[] { new Point(0, 14), new Point(0, -14), new Point(14, 0), new Point(-14, 0) }) {
                    var point = road.Position + offset;
                    var quote = _sim.QuoteBuilding(BuildingKind.House, point, 0);
                    if (quote.PhysicallyValid && quote.Accessible) { candidate = point; break; }
                }
                if (candidate != null) break;
            }
            Require(candidate != null, "Cidade possui espaço para uma prévia de casa acessível");
            await RecoveryPreviewWorldPoint(candidate!.Value);
            Require(_world.FindChild("BuildingPreview", true, false) != null, "GUI posiciona modelo da prévia sem encomendar");
            await RecoveryCaptureOnce("building-preview", GetViewport());
            await RecoveryKey(Key.Escape);

            await RecoveryButton(RecoveryRoadButton());
            (Point Start, Point End)? roadCandidate = null;
            foreach (var road in _sim.State.Roads.Where(road => !road.Highway).OrderByDescending(road => road.Position.X)) {
                foreach (var offset in new[] { new Point(0, -32), new Point(0, 32), new Point(32, 0), new Point(-32, 0) }) {
                    var end = road.Position + offset;
                    var quote = _sim.QuoteRoad(road.Position, end);
                    if (quote.PhysicallyValid && quote.Accessible) { roadCandidate = (road.Position, end); break; }
                }
                if (roadCandidate != null) break;
            }
            Require(roadCandidate != null, "Cidade possui traçado válido para prévia de rua");
            await RecoveryWorldPoint(roadCandidate!.Value.Start);
            await RecoveryPreviewWorldPoint(roadCandidate.Value.End);
            Require(_roadStart != null && _roadEnd != null, "Um clique e movimento exibem a prévia de rua sem encomendar");
            await RecoveryCaptureOnce("road-before-order", GetViewport());
            await RecoveryKey(Key.Escape);

            var building = _sim.State.Buildings.FirstOrDefault(building => building.Kind == BuildingKind.Grocery && !building.Removed) ?? _sim.State.Buildings.First(building => !building.Removed);
            await RecoveryWorldPoint(building.Position);
            Require(_inspector.Visible, "Inspeção aberta pelo mapa");
            await RecoveryCaptureOnce("inspection", GetViewport());
            foreach (var tooltip in new[] { "Finanças e impostos", "SIMs e empresas", "Causas, obras e entregas", "Controles", "Cidades e autosave" }) {
                await RecoveryButton(RecoveryHudButton(tooltip));
                await RecoveryCaptureOnce(RecoveryMenuName(tooltip), _modal!);
                await RecoveryCloseModal();
            }
            Require(_sim.SaveToJson() == before, "Revisão visual não construiu, concluiu, salvou nem mudou a cidade");
            GD.Print("INDEXCITIES_VISUAL_REVIEW_OK directory=" + _recoveryCaptureDirectory);
            GetTree().Quit();
        } catch (Exception exception) {
            GD.PushError("Visual review: " + exception);
            GetTree().Quit(1);
        } finally { _recoveryCaptureDirectory = null; }
    }

    async void RealTimeStartSmoke()
    {
        var directory = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification"));
        var vehicleOrigins = new Dictionary<int, Point>();
        var citizenOrigins = new Dictionary<int, Point>();
        var movedVehicles = new HashSet<int>();
        var movedCitizens = new HashSet<int>();
        var movement = new List<object>();
        var phases = new List<object>();
        try {
            Require(DisplayServer.GetName() != "headless", "Começo com relógio real exige janela da engine");
            Require(_sim.State.Buildings.Count == 0, "Verificação do começo requer cidade inicialmente vazia");
            System.IO.Directory.CreateDirectory(directory);
            CitySaves.VerificationDirectory = System.IO.Path.Combine(directory, "real-time-saves-" + Guid.NewGuid().ToString("N"));
            _recoveryCaptureDirectory = System.IO.Path.Combine(directory, "real-time-start-visual");
            _recoveryCaptures.Clear();
            _autosaveMinutes = 0;
            await RecoveryFrame();
            await RecoveryButton(RecoverySpeedButton(0));
            Require(_speed == 0, "Botão Pausar interrompe relógio durante a colocação");
            var money = _sim.MonetaryTotal;
            var before = new {
                _sim.State.Now, _sim.State.ElapsedSeconds, cityBalance = _sim.CityBalance,
                vehicles = _sim.State.Vehicles.Select(vehicle => new { vehicle.Id, vehicle.Position, vehicle.Status }).ToArray(),
                citizens = _sim.State.Citizens.Select(citizen => new { citizen.Id, citizen.Position, citizen.Activity }).ToArray()
            };
            foreach (var vehicle in _sim.State.Vehicles) vehicleOrigins[vehicle.Id] = vehicle.Position;

            void ObserveMovement()
            {
                foreach (var vehicle in _sim.State.Vehicles) {
                    if (!vehicleOrigins.TryGetValue(vehicle.Id, out var origin)) vehicleOrigins[vehicle.Id] = origin = vehicle.Position;
                    if (origin.Distance(vehicle.Position) > .01 && movedVehicles.Add(vehicle.Id)) movement.Add(new {
                        type = "vehicle", vehicle.Id, from = origin, to = vehicle.Position, vehicle.Status, vehicle.Cause, atSimulationSeconds = _sim.State.ElapsedSeconds
                    });
                }
                foreach (var citizen in _sim.State.Citizens) {
                    if (!citizenOrigins.TryGetValue(citizen.Id, out var origin)) citizenOrigins[citizen.Id] = origin = citizen.Position;
                    if (origin.Distance(citizen.Position) > .01 && movedCitizens.Add(citizen.Id)) movement.Add(new {
                        type = "citizen", citizen.Id, from = origin, to = citizen.Position, citizen.Activity, atSimulationSeconds = _sim.State.ElapsedSeconds
                    });
                }
            }

            async Task WaitFor(string phase, Func<bool> reached)
            {
                var startSimulationSeconds = _sim.State.ElapsedSeconds;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                double nextObservation = 5;
                while (!reached()) {
                    Require(watch.Elapsed.TotalSeconds < 90, "Tempo limite de 90 segundos reais: " + phase);
                    await RecoveryFrame();
                    ObserveMovement();
                    if (watch.Elapsed.TotalSeconds >= nextObservation) {
                        _sim.CheckInvariants();
                        Require(_sim.MonetaryTotal == money, "Oferta monetária permanece constante no relógio normal");
                        GD.Print($"REAL_TIME_START phase={phase} realSeconds={watch.Elapsed.TotalSeconds:F1} simulationSeconds={_sim.State.ElapsedSeconds - startSimulationSeconds:F1} population={_sim.Population} projects=" + string.Join(" | ", _sim.State.Projects.Select(project => $"{project.Id}:{project.Progress:P0}:{project.Cause}")));
                        nextObservation += 5;
                    }
                }
                watch.Stop();
                ObserveMovement();
                var elapsedSimulationSeconds = _sim.State.ElapsedSeconds - startSimulationSeconds;
                Require(elapsedSimulationSeconds > 0 && watch.Elapsed.TotalSeconds > 0, "Relógio do host avançou na fase " + phase);
                phases.Add(new { phase, realSeconds = watch.Elapsed.TotalSeconds, simulationSeconds = elapsedSimulationSeconds, observedSimulationSecondsPerRealSecond = elapsedSimulationSeconds / watch.Elapsed.TotalSeconds });
            }

            async Task NormalClockSample(string phase)
            {
                await RecoveryButton(RecoverySpeedButton(1));
                Require(_speed == 1 && _sim.State.Tuning.SimulationSecondsPerRealSecond * _sim.State.Tuning.SpeedMultipliers[1] == 1, "Controle normal representa um segundo simulado por segundo real");
                var startSimulationSeconds = _sim.State.ElapsedSeconds;
                var startMovements = movement.Count;
                var positionsBefore = _sim.State.Vehicles.Select(vehicle => new { kind = "vehicle", vehicle.Id, vehicle.Position }).Concat(_sim.State.Citizens.Select(citizen => new { kind = "citizen", citizen.Id, citizen.Position })).ToArray();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (watch.Elapsed.TotalSeconds < 4) { await RecoveryFrame(); ObserveMovement(); }
                watch.Stop();
                var simulationSeconds = _sim.State.ElapsedSeconds - startSimulationSeconds;
                Require(simulationSeconds > 0 && Math.Abs(simulationSeconds - watch.Elapsed.TotalSeconds) <= 2.1, "Relógio 1x acompanha tempo real dentro do quantum de dois segundos");
                var positionsAfter = _sim.State.Vehicles.Select(vehicle => new { kind = "vehicle", vehicle.Id, vehicle.Position }).Concat(_sim.State.Citizens.Select(citizen => new { kind = "citizen", citizen.Id, citizen.Position })).ToArray();
                var normalMotion = positionsBefore.Join(positionsAfter, start => start.kind + ":" + start.Id, finish => finish.kind + ":" + finish.Id, (start, finish) => new { start.kind, start.Id, from = start.Position, to = finish.Position, distance = start.Position.Distance(finish.Position), observedNetMetresPerRealSecond = start.Position.Distance(finish.Position) / watch.Elapsed.TotalSeconds }).ToArray();
                if (phase == "normal-clock-with-physical-resident-arrival") Require(normalMotion.Any(item => item.kind == "citizen" && item.distance > .01), "Morador caminhou na amostra de quatro segundos reais a 1x");
                phases.Add(new { phase, speed = 1, realSeconds = watch.Elapsed.TotalSeconds, simulationSeconds, observedSimulationSecondsPerRealSecond = simulationSeconds / watch.Elapsed.TotalSeconds, normalMotion, newMovementEvidence = movement.Skip(startMovements).ToArray() });
                _sim.CheckInvariants();
                Require(_sim.MonetaryTotal == money, "Amostra 1x mantém oferta monetária constante");
            }

            await RecoveryPlace(BuildingKind.CityHall, new(-152, 16));
            var road = await RecoveryRoad(new(-192, 0), new(-120, 0), choose: true);
            await NormalClockSample("normal-clock-before-road-completion");
            Require(movedVehicles.Count > 0, "Caminhões deslocaram-se nos frames normais a 1x");
            await RecoveryButton(RecoverySpeedButton(2));
            Require(_speed == 2, "Botão 15x acelera a obra através do relógio do host");
            await WaitFor("road-physically-completed", () => road.Status == WorkStatus.Completed);
            await RecoveryButton(RecoverySpeedButton(0));
            var houseProject = await RecoveryPlace(BuildingKind.House, new(-120, 14));
            var house = _sim.State.Buildings.Single(building => building.Id == houseProject.BuildingId);
            await RecoveryButton(RecoverySpeedButton(3));
            Require(_speed == 3, "Botão 60x acelera a casa através do relógio do host");
            await WaitFor("house-completed-and-bought", () => house.Completed && house.FamilyId != null);
            await NormalClockSample("normal-clock-with-physical-resident-arrival");
            Require(movedCitizens.Count > 0, "Moradores mudam de posição fisicamente antes de chegar à casa");
            await RecoveryButton(RecoverySpeedButton(3));
            await WaitFor("house-completed-bought-resident-at-home", () => house.Completed && house.FamilyId != null && _sim.State.Citizens.Any(citizen => citizen.Alive && citizen.Resident && citizen.InsideMap && citizen.FamilyId == house.FamilyId && !citizen.Moving && citizen.Position.Distance(house.Door) < .15));
            await RecoveryButton(RecoverySpeedButton(0));
            _sim.CheckInvariants();
            Require(_sim.MonetaryTotal == money && money == _sim.State.Tuning.TotalMoney, "Dinheiro permanece constante antes e depois do começo real");
            Require(movedVehicles.Count > 0 && movedCitizens.Count > 0, "Caminhões e moradores mudaram de posição ao longo dos frames normais");
            Require(_sim.State.Ledger.Any(entry => entry.Category == "Aquisição residencial" && entry.EntityId == house.Id), "Compra da casa tem transferência real identificável");
            await RecoveryKey(Key.Escape);
            _focus = ToV(new(-160, 8)); _zoom = 140; UpdateCamera();
            await RecoveryCaptureOnce("operational-start", GetViewport());
            var report = new {
                status = "normal-host-clock-start-passed", humanUsabilityReview = "pending",
                expectedSimulationSecondsPerRealSecond = _sim.State.Tuning.SimulationSecondsPerRealSecond * _sim.State.Tuning.SpeedMultipliers[1],
                before, phases, movedVehicles = movedVehicles.Order().ToArray(), movedCitizens = movedCitizens.Order().ToArray(), movement,
                after = new {
                    _sim.State.Now, _sim.State.ElapsedSeconds, cityBalance = _sim.CityBalance, monetaryTotal = _sim.MonetaryTotal,
                    roadProject = new { road.Id, road.Status, road.Progress, road.CostPaid, road.Delivered },
                    house = new { house.Id, house.Position, house.Completed, house.OwnerAccount, house.FamilyId, house.Door },
                    vehicles = _sim.State.Vehicles.Select(vehicle => new { vehicle.Id, vehicle.Position, vehicle.Status }).ToArray(),
                    citizens = _sim.State.Citizens.Select(citizen => new { citizen.Id, citizen.Position, citizen.Activity, citizen.Resident, citizen.InsideMap }).ToArray()
                },
                transfers = _sim.State.Ledger.ToArray()
            };
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "real-time-start-report.json"), System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "real-time-start-city.json"), _sim.SaveToJson());
            GD.Print("INDEXCITIES_REAL_TIME_START_OK " + System.Text.Json.JsonSerializer.Serialize(new { phases, population = _sim.Population, monetaryTotal = _sim.MonetaryTotal }));
            GetTree().Quit();
        } catch (Exception exception) {
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "real-time-start-failure-city.json"), _sim.SaveToJson());
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "real-time-start-failure-report.json"), System.Text.Json.JsonSerializer.Serialize(new { status = "normal-host-clock-start-failed", failure = exception.ToString(), phases, movement, _sim.State.ElapsedSeconds, projects = _sim.State.Projects.Select(project => new { project.Id, project.Status, project.Progress, project.Cause }).ToArray() }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            GD.PushError("Real-time start: " + exception);
            GetTree().Quit(1);
        } finally { CitySaves.VerificationDirectory = null; _recoveryCaptureDirectory = null; }
    }

    static Vector3 RecoveryRoofPoint(Node3D building)
    {
        Vector3? highest = null;
        foreach (var instance in RecoveryNodes<MeshInstance3D>(building).Where(instance => instance.IsVisibleInTree() && instance.Mesh != null)) {
            var mesh = instance.Mesh!;
            for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++) {
                if (mesh is ArrayMesh arrayMesh && arrayMesh.SurfaceGetPrimitiveType(surface) != Mesh.PrimitiveType.Triangles) continue;
                var arrays = mesh.SurfaceGetArrays(surface);
                var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var indices = arrays[(int)Mesh.ArrayType.Index].VariantType == Variant.Type.Nil ? Array.Empty<int>() : arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                var count = indices.Length > 0 ? indices.Length : vertices.Length;
                for (var index = 0; index + 2 < count; index += 3) {
                    var a = instance.GlobalTransform * vertices[indices.Length > 0 ? indices[index] : index];
                    var b = instance.GlobalTransform * vertices[indices.Length > 0 ? indices[index + 1] : index + 1];
                    var c = instance.GlobalTransform * vertices[indices.Length > 0 ? indices[index + 2] : index + 2];
                    var normal = (b - a).Cross(c - a);
                    if (normal.LengthSquared() < .000001 || Math.Abs(normal.Normalized().Y) < .5) continue;
                    var point = (a + b + c) / 3;
                    if (highest == null || point.Y > highest.Value.Y) highest = point;
                }
            }
        }
        Require(highest != null && highest.Value.Y > 3, "Há superfície real elevada do prédio para selecionar");
        return highest!.Value;
    }

    async void InteractionRegressionSmoke()
    {
        var milestones = new List<string>();
        var directory = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification"));
        try {
            Require(DisplayServer.GetName() != "headless", "Regressões de interação exigem viewport renderizado real");
            Require(_sim.State.Buildings.Count == 0, "Regressões começam em cidade vazia e isolada");
            _speed = 0; _autosaveMinutes = 0;
            CitySaves.VerificationDirectory = System.IO.Path.Combine(directory, "interaction-saves-" + Guid.NewGuid().ToString("N"));
            _recoveryCaptureDirectory = System.IO.Path.Combine(directory, "interaction-regressions-visual");
            _recoveryCaptures.Clear();
            await RecoveryFrame();
            await RecoveryPlace(BuildingKind.CityHall, new(-152, 16));
            var road = await RecoveryRoad(new(-192, 0), new(-120, 0), choose: true);
            await RecoveryAdvance(7200);
            Require(road.Status == WorkStatus.Completed && _sim.State.Buildings.Single().Completed, "Obras iniciais realmente concluídas para verificar seleção");
            var before = _sim.SaveToJson();

            await RecoveryButton(RecoveryRoadButton());
            await RecoveryWorldPoint(new(-120, 0));
            var screen = _camera.UnprojectPosition(ToV(new(-80, 20)));
            await RecoveryMove(screen);
            var beforeRotation = _roadEnd;
            await RecoveryKey(Key.Q);
            Require(_roadEnd != beforeRotation, "Prévia móvel acompanha rotação de câmera sem mover o mouse");
            var beforeWalking = _roadEnd;
            RecoveryInput(new InputEventKey { Keycode = Key.W, PhysicalKeycode = Key.W, Pressed = true });
            var cameraWatch = System.Diagnostics.Stopwatch.StartNew();
            while (cameraWatch.Elapsed.TotalSeconds < .25) await RecoveryFrame();
            RecoveryInput(new InputEventKey { Keycode = Key.W, PhysicalKeycode = Key.W, Pressed = false });
            await RecoveryFrame();
            Require(_roadEnd != beforeWalking, "Prévia móvel acompanha câmera pelo teclado sem mover o mouse");
            RecoveryInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true, Position = screen, GlobalPosition = screen });
            await RecoveryFrame();
            var shown = _roadEnd;
            var shownShape = _sim.RoadShape(_roadStart!.Value, shown!.Value, _reverse).Where(point => _sim.State.Roads.All(node => node.Position.Distance(point) > .05)).ToArray();
            var shownQuote = _sim.QuoteRoad(_roadStart.Value, shown.Value, _reverse);
            Require(shownQuote.PhysicallyValid && shownQuote.Total <= _sim.Available, "Cenário de câmera mantém traçado executável");
            var projectsBefore = _sim.State.Projects.Select(project => project.Id).ToHashSet();
            await RecoveryClick(screen);
            var orderedRoad = _sim.State.Projects.Single(project => !projectsBefore.Contains(project.Id));
            Require(orderedRoad.RoadPoints.SequenceEqual(shownShape) && orderedRoad.Status != WorkStatus.Completed, "Segundo clique encomenda exatamente o traçado que já estava desenhado depois do zoom");
            await RecoveryCaptureOnce("road-after-camera-movement", GetViewport());
            await RecoveryKey(Key.Escape);
            Require(_sim.State.Projects.Count == projectsBefore.Count + 1 && orderedRoad.Progress == 0, "Só o segundo clique acrescentou a obra real");
            before = _sim.SaveToJson();
            milestones.Add("prévia de rua acompanha rotação, movimento e zoom sem salto na fixação");

            var hall = _sim.State.Buildings.Single();
            // View the asphalt from the front: a road pixel hidden behind a
            // tall model should correctly pick that model instead.
            _yaw = 225;
            await RecoveryWorldPoint(new(-152, 4));
            Require(_selectedBuilding == null && _selectedRoad != null, "Clique na rua fora da Prefeitura seleciona via sem halo do prédio");
            await RecoveryCaptureOnce("road-next-to-building", GetViewport());
            await RecoveryKey(Key.Escape);
            _yaw = 45; _focus = ToV(hall.Position); _zoom = 110; UpdateCamera();
            await RecoveryFrame();
            var visual = _world.GetChildren().OfType<Node3D>().Single(node => node.Position.DistanceTo(ToV(hall.Position)) < .01f && RecoveryNodes<MeshInstance3D>(node).Any(instance => instance.IsVisibleInTree() && instance.Mesh != null));
            var roof = RecoveryRoofPoint(visual);
            var roofScreen = _camera.UnprojectPosition(roof);
            var groundUnderRoof = Ground(roofScreen);
            Require(groundUnderRoof.Distance(hall.Position) > Math.Max(hall.Definition.Width, hall.Definition.Depth) * .65, "Clique elevado está além do antigo círculo de seleção");
            await RecoveryClick(roofScreen);
            Require(_selectedBuilding == hall.Id && _inspector.Visible, "Raio pela superfície real do telhado seleciona a Prefeitura");
            await RecoveryCaptureOnce("building-roof-selection", GetViewport());
            Require(_sim.SaveToJson() == before, "Inspeção não modifica simulação");
            milestones.Add("via adjacente e superfície elevada selecionadas pelo elemento real");

            // Ferramentas fluidas: entrada vira para a rua, R é override manual e a fileira encomenda obras independentes.
            await RecoveryKey(Key.Escape);
            var beforeSnap = _sim.SaveToJson();
            foreach (var kind in new[] { BuildingKind.House, BuildingKind.Grocery }) {
                await RecoveryButton(RecoveryCatalog(kind));
                var free = new Point(-184, kind == BuildingKind.House ? -22 : -24);
                await RecoveryPreviewWorldPoint(free);
                Require(_cursor.Distance(free) <= 8.01 && _sim.QuoteBuilding(kind, _cursor, _rotation) is { PhysicallyValid: true, Accessible: true }, "Encaixe alcança a rua dentro do ajuste permitido, mesmo com a porta livre ainda sem acesso: " + kind);
                Require(_sim.SaveToJson() == beforeSnap, "Encaixe não cria obra, acesso ou compromisso na simulação");
                await RecoveryCaptureOnce("snap-" + kind.ToString().ToLowerInvariant(), GetViewport());
            }
            await RecoveryButton(RecoveryCatalog(BuildingKind.House));
            await RecoveryPreviewWorldPoint(new(-184, -36));
            Require(_cursor.Distance(new(-184, -36)) < .01 && !_sim.QuoteBuilding(BuildingKind.House, _cursor, _rotation).Accessible, "Fora do alcance de ajuste a posição continua livre e sem acesso");
            await RecoveryPreviewWorldPoint(hall.Position);
            var blockedProjects = _sim.State.Projects.Count;
            await RecoveryClick(_camera.UnprojectPosition(ToV(hall.Position)));
            Require(_sim.State.Projects.Count == blockedProjects, "Encaixe preserva o bloqueio físico de prédio existente");
            await RecoveryPreviewWorldPoint(new(-184, -12));
            Require(Math.Abs(_rotation - Math.PI) < .000001 && _sim.QuoteBuilding(BuildingKind.House, _cursor, _rotation) is { PhysicallyValid: true, Accessible: true }, "Casa ao norte da rua vira a entrada para a rua e encaixa com acesso");
            await RecoveryPreviewWorldPoint(new(-184, 12));
            Require(Math.Abs(_rotation) < .000001, "Casa ao sul da rua vira a entrada para a rua");
            await RecoveryKey(Key.R);
            var manual = _rotation;
            await RecoveryMove(_camera.UnprojectPosition(ToV(new(-182, 12))));
            Require(_manualRotation && Math.Abs(_rotation - manual) < .000001, "R vale como rotação manual enquanto o cursor segue perto da rua");
            await RecoveryPreviewWorldPoint(new(-184, -60));
            Require(!_manualRotation, "Sair da proximidade da rua encerra o override manual");
            await RecoveryPreviewWorldPoint(new(-184, -12));
            Require(Math.Abs(_rotation - Math.PI) < .000001, "Voltar à rua retoma a auto-orientação");
            var spacing = Math.Ceiling((Catalog.Definitions[BuildingKind.House].Width + .5) / 2) * 2;
            var rowProjects = _sim.State.Projects.Count;
            var availableBefore = _sim.Available;
            var rowStart = await RecoveryMove(_camera.UnprojectPosition(ToV(new(-184, -22))));
            var startCursor = _cursor;
            RecoveryInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true, Position = rowStart, GlobalPosition = rowStart });
            await RecoveryFrame();
            var rowEnd = await RecoveryMove(_camera.UnprojectPosition(ToV(new(startCursor.X + 2 * spacing, -22))));
            await RecoveryFrame();
            Require(_dragStart != null && _row.Count == 3 && _rowValid == 3 && _toolHint.Text.Contains("×3", StringComparison.Ordinal) && _toolHint.Text.Contains(Cash(_rowTotal), StringComparison.Ordinal), "Arrastar mostra fileira de 3 casas com quantidade e custo total: " + _toolHint.Text);
            Require(_confirmation.Visible && _previewName.Text.Contains("×3") && _previewCost.Text == Cash(_rowTotal), "Resumo visual acompanha quantidade e custo agregado reais da fileira");
            await RecoveryCaptureOnce("building-row-preview", GetViewport());
            RecoveryInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = rowEnd, GlobalPosition = rowEnd });
            await RecoveryFrame();
            var ordered = _sim.State.Projects.Skip(rowProjects).ToList();
            Require(ordered.Count == 3 && ordered.Select(project => project.Position.X).Distinct().Count() == 3 && ordered.All(project => project.BuildingId != null) && _sim.Available < availableBefore, "Soltar encomenda 3 obras de casa independentes: " + ordered.Count);
            Require(ordered.Select(project => _sim.State.Buildings.Single(b => b.Id == project.BuildingId)).All(b => Math.Abs(b.Rotation - Math.PI) < .000001 && !_sim.State.Buildings.Where(o => o.Id != b.Id && !o.Removed).Any(o => o.Position.Distance(b.Position) < 1)), "Casas da fileira ficam alinhadas à rua e sem sobreposição");
            await RecoveryPreviewWorldPoint(new(-100, -70));
            var roadless = _sim.QuoteBuilding(BuildingKind.House, _cursor, _rotation);
            Require(roadless is { PhysicallyValid: true, Accessible: false } && _world.FindChild("NoRoadWarning", true, false) is Label3D { Visible: true }, "Prévia válida sem rua mostra o aviso forte de obra parada");
            await RecoveryCaptureOnce("building-no-road-warning", GetViewport());
            Require(_confirmation.Visible && _previewSignals[1].Label.Text.Contains("obra parada"), "Resumo visual distingue posição válida sem ligação viária");
            await RecoveryClick(_camera.UnprojectPosition(ToV(_cursor)));
            await RecoveryFrame(); await RecoveryFrame();
            await RecoveryCaptureOnce("roadless-site-marker", GetViewport());
            milestones.Add("auto-orientação para a rua e fileira por arrastar de 3 casas; aviso forte sem rua");
            await RecoveryKey(Key.Escape);

            // This is a deliberately poor test city created through the same
            // constructor and monetary distribution, without editing wallets.
            _sim = new CitySimulation(42, new SimulationTuning { CityInitialMoney = 1 });
            _cityId = CitySaves.NewId();
            _world.Reset(_sim.State); BuildCatalog();
            await RecoveryKey(Key.Escape);
            await RecoveryButton(RecoveryCatalog(BuildingKind.CityHall));
            var poorBefore = _sim.SaveToJson();
            await RecoveryPreviewWorldPoint(new(-152, 16));
            var quote = _sim.QuoteBuilding(BuildingKind.CityHall, _cursor, _rotation);
            Require(quote.PhysicallyValid && quote.Total > _sim.Available, "Caso de orçamento tem espaço válido e caixa realmente insuficiente");
            var diagnosis = RecoveryDiagnostic();
            Require(diagnosis.Contains("Saldo insuficiente", StringComparison.Ordinal) && diagnosis.Contains(Cash(_sim.Available), StringComparison.Ordinal) && diagnosis.Contains(Cash(quote.Total - _sim.Available), StringComparison.Ordinal), "Diagnóstico informa caixa disponível e falta real");
            await RecoveryCaptureOnce("insufficient-budget-preview", GetViewport());
            Require(_confirmation.Visible && _previewSignals[2].Label.Text.Contains("Falta " + Cash(quote.Total - _sim.Available)), "Resumo visual mostra a diferença real de saldo sem cobrança");
            await RecoveryClick(_camera.UnprojectPosition(ToV(new(-152, 16))));
            Require(_sim.SaveToJson() == poorBefore && _sim.MonetaryTotal == _sim.State.Tuning.TotalMoney, "Prévia recusada preserva estado e oferta monetária");
            milestones.Add("recusa financeira apresenta causa, disponível e diferença reais");
            var report = new { status = "interaction-regressions-passed", humanUsabilityReview = "pending", milestones, roofPoint = roof, groundUnderRoof, diagnosis };
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "interaction-regressions-report.json"), System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            GD.Print("INDEXCITIES_INTERACTION_REGRESSIONS_OK " + System.Text.Json.JsonSerializer.Serialize(report));
            GetTree().Quit();
        } catch (Exception exception) {
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "interaction-regressions-failure-city.json"), _sim.SaveToJson());
            GD.PushError("Interaction milestones: " + string.Join("; ", milestones) + "\n" + exception);
            GetTree().Quit(1);
        } finally { CitySaves.VerificationDirectory = null; _recoveryCaptureDirectory = null; }
    }
}
