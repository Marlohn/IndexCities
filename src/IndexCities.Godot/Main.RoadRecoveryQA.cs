using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void RoadRecoverySmoke()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/recovery-v1")), "road-run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6]);
        System.IO.Directory.CreateDirectory(directory);
        try {
            Require(DisplayServer.GetName() != "headless", "Verificação viária exige janela Godot real");
            _speed = 0; _autosaveMinutes = 0;
            CitySaves.VerificationDirectory = System.IO.Path.Combine(directory, "road-saves-" + Guid.NewGuid().ToString("N"));
            _recoveryCaptureDirectory = directory; _recoveryCaptures.Clear();
            await RecoveryFrame();
            await RecoveryPlace(BuildingKind.CityHall, new(-152, 16));
            await RecoveryRoad(new(-192, 0), new(-80, 0), choose: true);
            await RecoveryAdvance(7200);
            _focus = new(-150, 0, 20); _zoom = 110; UpdateCamera();
            await RecoveryButton(RecoveryCatalog(BuildingKind.House));
            await RecoveryPreviewWorldPoint(new(-176,14));
            var doorPosition=_cursor;
            Require(_sim.QuoteBuilding(BuildingKind.House,doorPosition,_rotation) is {PhysicallyValid:true,Accessible:true},"Porta de frente para a rua pronta tem acesso real");
            Require(_world.FindChild("NoRoadWarning",true,false) is Label3D {Visible:false}&&_confirmation.Visible&&_previewSignals[1].Label.Text=="Ligação com o exterior","Frente: prévia e orçamento não avisam falta de acesso");
            await RecoveryCaptureOnce("door-facing-road",GetViewport());
            await RecoveryKey(Key.R);await RecoveryKey(Key.R);
            Require(_cursor==doorPosition&&Math.Abs(_rotation-Math.PI)<.000001,"Duas rotações manuais deixam a porta de costas no mesmo lote");
            var backwardsQuote=_sim.QuoteBuilding(BuildingKind.House,_cursor,_rotation);
            Require(backwardsQuote is {PhysicallyValid:true,Accessible:false},"Porta de costas permanece fisicamente válida, sem acesso real");
            Require(_world.FindChild("NoRoadWarning",true,false) is Label3D {Visible:true}&&_confirmation.Visible&&_previewSignals[1].Label.Text.Contains("obra parada"),"Costas: prévia e orçamento avisam obra parada antes do clique");
            await RecoveryCaptureOnce("door-away-from-road",GetViewport());
            await RecoveryClick(_camera.UnprojectPosition(ToV(_cursor)));
            var backwardsHouse=_sim.State.Buildings.Last();
            Require(backwardsHouse.Position==doorPosition&&Math.Abs(backwardsHouse.Rotation-Math.PI)<.000001,"Clique continua permitindo encomendar prédio de costas para a rua");
            await RecoveryKey(Key.Escape);await RecoveryAdvance(2);
            var backwardsWork=_sim.State.Projects.Single(p=>p.BuildingId==backwardsHouse.Id);
            Require(backwardsWork.Progress==0&&backwardsWork.Cause.Contains("Sem acesso")&&!_sim.State.Shipments.Any(s=>s.ProjectId==backwardsWork.Id),"Obra usa a mesma falta de acesso, sem entrega nem progresso");
            Require(RecoveryNodes<Label3D>(_world).Any(label=>label.IsVisibleInTree()&&label.Text.Contains("SEM RUA: obra parada")),"Canteiro de costas conserva aviso forte visível");
            await RecoveryCaptureOnce("door-away-site",GetViewport());
            Require(_sim.CancelProject(backwardsWork.Id).Success,"Remove somente a obra descartável deste caso de QA");
            GD.Print("ROAD_RECOVERY_DOOR_CHECK front=accessible back=warning quote=warning placement=allowed dispatch=stopped");
            await RecoveryButton(RecoveryRoadButton());
            await RecoveryPreviewWorldPoint(new(-128, 1.5));
            Require(_cursor == new Point(-128, 0), "Cursor a 1,5 m da rua encaixa no nó existente");
            await RecoveryPreviewWorldPoint(new(-128, 6.1));
            Require(_cursor == new Point(-128, 8), "Cursor perto da rua conserva o ponto de grade pretendido, sem atração de 20 pixels");
            await RecoveryWorldPoint(new(-184, 0));
            await RecoveryPreviewWorldPoint(new(-120, 44));
            await RecoveryKey(Key.Tab);
            Require(_reverse, "Tab alterna o L antes de encomendar");
            Require(_world.FindChild("RoadStart", true, false) != null && _world.FindChild("RoadEnd", true, false) != null && _world.FindChild("RoadCorner", true, false) != null, "Início, fim e curva representam a prévia");
            foreach (var marker in new[] { ("RoadStart", new Point(-184, 0)), ("RoadEnd", new Point(-120, 44)), ("RoadCorner", new Point(-184, 44)) }) {
                var node = (Node3D)_world.FindChild(marker.Item1, true, false);
                Require(new Point(node.Position.X, node.Position.Z).Distance(marker.Item2) < .001, "Marcador coincide com posição real: " + marker.Item1);
            }
            var before = _sim.SaveToJson(); var city = _cityId;
            Require(TrySaveCity(), "Esboço viário salva em diretório isolado");
            ClearTool(); LoadCity(city);
            Require(_sim.SaveToJson() == before && _roadStart == new Point(-184, 0) && _roadEnd == new Point(-120, 44) && _reverse, "Retomar preserva prévia e recursos sem criar via");
            await RecoveryWorldPoint(new(-120, 44));
            var corner = _sim.State.Projects.Last();
            Require(corner.RoadPoints.Contains(new(-184, 44)) && corner.Status != WorkStatus.Completed, "Segundo clique encomenda o L exibido como obra real");
            await RecoveryButton(RecoveryCatalog(BuildingKind.House));
            await RecoveryPreviewWorldPoint(new(-144, 60));
            Require(_cursor == new Point(-144, 58) && Math.Abs(_rotation) < .001, "Casa encaixa e orienta pela rua ainda em obra");
            var plannedQuote = _sim.QuoteBuilding(BuildingKind.House, _cursor, _rotation);
            Require(plannedQuote.PhysicallyValid && !plannedQuote.Accessible, "Encaixe futuro conserva aviso sem acesso real");
            Require(_world.FindChild("NoRoadWarning",true,false) is Label3D {Visible:true}&&_confirmation.Visible&&_previewSignals[1].Label.Text.Contains("obra parada"),"Rua em obra mantém aviso na prévia e no orçamento até oferecer acesso real");
            await RecoveryWorldPoint(new(-144, 60));
            var plannedHouse = _sim.State.Buildings.Last();
            Require(!plannedHouse.Completed && !_sim.HasExteriorAccess(plannedHouse), "Casa nasce como obra que aguarda rua utilizável");
            await RecoveryAdvance(7200);
            Require(corner.Status == WorkStatus.Completed, "Materiais e equipe concluíram o L");
            Require(_sim.HasExteriorAccess(plannedHouse), "Somente rua construída concede acesso à casa encaixada");
            foreach (var point in new[] { new Point(-184, 4), new Point(-180, 0), new Point(-184, 40), new Point(-180, 44) })
                Require(!_world.RaisedSidewalkAt(point), "Calçada não cobre braço asfaltado em " + point);
            Require(_world.RaisedSidewalkAt(new(-180, 20)), "A calçada permanece presente junto à rua");
            Require(_world.RaisedSidewalkAt(new(-184, -4.5)), "Lado fechado do T conserva calçada");
            await RecoveryRoad(new(-184, 0), new(-184, -44), choose: true);
            await RecoveryAdvance(7200);
            foreach (var point in new[] { new Point(-184, 4), new Point(-184, -4), new Point(-180, 0), new Point(-188, 0) })
                Require(!_world.RaisedSidewalkAt(point), "Quatro entradas permanecem asfaltadas em " + point);
            var cancelled = _sim.PlaceRoad(new(-160, 80), new(-120, 80));
            Require(cancelled.Success, "Rua isolada encomendável com causa real");
            Choose(BuildingKind.House);
            Require(Snap(new(-144, 90)).Z == 94, "Rua isolada em obra oferece referência espacial");
            Require(_sim.CancelProject(cancelled.Id!.Value).Success, "Cancelar obra não remove rua pronta");
            Require(Snap(new(-144, 90)).Z == 90, "Cancelar obra remove sua referência de encaixe imediatamente");
            await RecoveryKey(Key.Escape);
            await RecoveryPlace(BuildingKind.House, new(-120, 14));
            Require(_sim.HasExteriorAccess(_sim.State.Buildings.Last()), "Encaixe da casa preserva acesso real");
            var unchanged = _sim.SaveToJson();
            _world.Sync(_sim.State); _world.Reset(_sim.State);
            Require(_sim.SaveToJson() == unchanged, "Gerar malha e reconstruir mundo não altera simulação");
            _focus = new(-184, 0, 22); _zoom = 85; UpdateCamera(); ClearTool();
            await RecoveryCaptureOnce("road-recovery-cycle", GetViewport());
            var cameraState = _sim.SaveToJson();
            await RecoveryButton(RecoveryCatalog(BuildingKind.House));
            var mouse = await RecoveryMove(new Vector2(650, 350)); var yaw = _yaw;
            RecoveryInput(new InputEventMouseButton { ButtonIndex=MouseButton.Right, ButtonMask=MouseButtonMask.Right, Pressed=true, Position=mouse }); await RecoveryFrame();
            RecoveryInput(new InputEventMouseMotion { Position=mouse+new Vector2(40,0), Relative=new Vector2(40,0), ButtonMask=MouseButtonMask.Right }); await RecoveryFrame();
            var overHud=RecoveryCatalog(BuildingKind.House).GetGlobalRect().GetCenter();
            RecoveryInput(new InputEventMouseMotion { Position=overHud, Relative=overHud-mouse-new Vector2(40,0), ButtonMask=MouseButtonMask.Right }); await RecoveryFrame();
            RecoveryInput(new InputEventMouseButton { ButtonIndex=MouseButton.Right, Pressed=false, Position=overHud }); await RecoveryFrame();
            Require(Math.Abs(_yaw-yaw-(overHud.X-mouse.X)*.3)<.01 && _building==BuildingKind.House && _sim.SaveToJson()==cameraState, "Arrasto direito continua sobre HUD sem cancelar ferramenta nem mudar cidade");
            GD.Print("ROAD_RECOVERY_CHECK right-drag-over-hud=passed tool=preserved state=preserved");
            RecoveryInput(new InputEventMouseButton { ButtonIndex=MouseButton.Right, Pressed=true, Position=mouse }); await RecoveryFrame();
            RecoveryInput(new InputEventMouseButton { ButtonIndex=MouseButton.Right, Pressed=false, Position=mouse }); await RecoveryFrame();
            Require(_building==null&&!_roadTool, "Clique direito curto cancela ferramenta");
            await RecoveryButton(RecoveryNodes<Button>(_catalog).Single(b=>b.TooltipText.StartsWith("Demolir rua")));
            _focus=new(-128,0,0);_zoom=80;UpdateCamera();
            var target=_sim.State.Roads.Single(r=>r.Position==new Point(-128,0));var demolition=_sim.QuoteRoadDemolition(target.Id);
            var beforeDemolition=_sim.SaveToJson();var balance=_sim.CityBalance;
            await RecoveryPreviewWorldPoint(target.Position);
            Require(_world.FindChild("DemolitionPreview",true,false)!=null, "Trecho de demolição fica destacado antes do clique");
            await RecoveryWorldPoint(target.Position);
            Require(_modal!=null && _sim.SaveToJson()==beforeDemolition, "Clique abre orçamento e consequências sem remover nem cobrar");
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="× Cancelar"));
            Require(_sim.SaveToJson()==beforeDemolition && _demolitionTool, "Cancelar orçamento preserva rua e ferramenta");
            await RecoveryWorldPoint(target.Position);
            var quoted=_sim.QuoteRoadDemolition(target.Id);
            // The newly ordered house dispatches real deliveries while the quote
            // is open. No state, money or cargo is fabricated for this check.
            for(var step=0;step<120&&_sim.QuoteRoadDemolition(target.Id).AffectedVehicleRoutes==quoted.AffectedVehicleRoutes;step++)_sim.Advance(2);
            Require(_sim.QuoteRoadDemolition(target.Id).AffectedVehicleRoutes!=quoted.AffectedVehicleRoutes,"Entregas reais modificam efeitos com o orçamento aberto");
            var advanced=_sim.SaveToJson();var oldModal=_modal;
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="✓ Confirmar remoção"));
            Require(_sim.SaveToJson()==advanced&&_sim.State.Roads.Any(r=>r.Id==target.Id)&&_modal!=oldModal,"Confirmar prévia desatualizada reexibe efeitos sem remover nem cobrar");
            GD.Print("ROAD_RECOVERY_CHECK stale-demolition=refreshed real-delivery-routes="+quoted.AffectedVehicleRoutes+"->"+_sim.QuoteRoadDemolition(target.Id).AffectedVehicleRoutes+" state=preserved payment=none");
            demolition=_sim.QuoteRoadDemolition(target.Id);balance=_sim.CityBalance;
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="✓ Confirmar remoção"));
            Require(_sim.State.Roads.All(r=>r.Id!=target.Id) && _sim.CityBalance==balance-demolition.Cost, "Confirmar remove somente trecho cotado com custo real");
            Require(TrySaveCity(), "Ferramenta Demolir salva no diretório isolado");ClearTool();LoadCity(city);
            Require(_demolitionTool && !_roadTool && _building==null, "Retomar conserva ferramenta sem remover automaticamente");
            var highway=_sim.State.Roads.First(r=>r.Highway);_focus=ToV(highway.Position);UpdateCamera();var highwayState=_sim.SaveToJson();
            await RecoveryWorldPoint(highway.Position);
            Require(_modal==null && _sim.SaveToJson()==highwayState, "Ferramenta preserva rodovia externa");
            var surfaceTimes=RoadSurfacePerformanceFixture();
            var report = new { status = "passed", modelReview = "pending", cornerNodes = corner.RoadPoints.Count, monetaryTotal = _sim.MonetaryTotal, roadSurfaceMilliseconds = _world.RoadSurfaceBuildMilliseconds, synthetic1000NodeSurfaceMilliseconds=surfaceTimes, saveDirectory = CitySaves.DirectoryPath };
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "road-recovery-report.json"), System.Text.Json.JsonSerializer.Serialize(report));
            _sim.CheckInvariants(); GD.Print("INDEXCITIES_ROAD_RECOVERY_OK directory=" + directory); GetTree().Quit();
        } catch (Exception exception) { GD.PushError("Road recovery: " + exception); GetTree().Quit(1); }
        finally { _recoveryCaptureDirectory = null; CitySaves.VerificationDirectory = null; }
    }

    double[] RoadSurfacePerformanceFixture()
    {
        // Synthetic geometry only: measures mesh rebuilding, never gameplay,
        // money, construction, agent throughput, or operation of a real city.
        var state=CitySimulation.FromJson(_sim.SaveToJson()).State;
        state.Roads.Clear();state.Buildings.Clear();state.Projects.Clear();state.Citizens.Clear();state.Vehicles.Clear();
        for(var row=0;row<10;row++)for(var column=0;column<100;column++){
            var id=100000+row*100+column;var neighbours=new List<int>();if(column>0)neighbours.Add(id-1);if(column<99)neighbours.Add(id+1);
            state.Roads.Add(new RoadNode { Id=id, Position=new(-200+column*4,-200+row*20), Neighbours=neighbours });
        }
        var world=new CityWorld { Visible=false };AddChild(world);var times=new List<double>();
        for(var run=0;run<3;run++){state.RoadRevision++;world.Reset(state);times.Add(world.RoadSurfaceBuildMilliseconds);}
        RemoveChild(world);world.QueueFree();return times.ToArray();
    }
}
