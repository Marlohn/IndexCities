using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;
public partial class Main
{
    void PersistenceSmoke()
    {
        try{
            var artifactRoot=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification"));CitySaves.VerificationDirectory=System.IO.Path.Combine(artifactRoot,"saves-"+Guid.NewGuid().ToString("N"));
            Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Fundação");Require(_sim.PlaceRoad(new(-192,0),new(-80,0)).Success,"Rua física");_sim.Advance(7200);
            _building=BuildingKind.House;_anchor=null;_cursor=new(-120,14);_hoverPreview=true;_rotation=Math.PI/2;_roadStart=null;_roadEnd=null;_roadTool=false;_speed=2;_autosaveMinutes=3;var first=_cityId;var expected=_sim.SaveToJson();var free=_sim.Available;SaveCity();
            _sim=new CitySimulation(99);_cityId=CitySaves.NewId();ClearSelection();_anchor=null;_building=BuildingKind.CityHall;_speed=0;SaveCity();Require(CitySaves.List().Count()==2,"Duas cidades independentes");
            LoadCity(first);Require(_sim.SaveToJson()==expected,"Simulação idêntica após carga");Require(_sim.Available==free,"Prévia não compromete dinheiro");Require(_building==BuildingKind.House&&_anchor==null&&_cursor==new Point(-120,14)&&Math.Abs(_rotation-Math.PI/2)<1e-9,"Prévia livre preservada sem encomenda");Require(_speed==2&&_autosaveMinutes==3,"Preferências preservadas");
            var copy=CitySimulation.FromJson(expected);copy.Advance(120);_sim.Advance(120);Require(copy.SaveToJson()==_sim.SaveToJson(),"Continuação determinística");
            _building=null;_roadTool=true;_roadStart=new(-160,40);_roadEnd=new(-120,80);_cursor=_roadEnd.Value;_hoverPreview=true;_roadFixed=false;_reverse=true;_movingProject=null;var roadState=_sim.SaveToJson();SaveCity();ClearTool();LoadCity(first);Require(_sim.SaveToJson()==roadState&&_roadTool&&_roadStart==new Point(-160,40)&&_roadEnd==new Point(-120,80)&&!_roadFixed&&_reverse,"Esboço em L preservado sem criar rua");
            var legacy=CitySimulation.FromJson(roadState);legacy.State.Tuning.SimulationSecondsPerRealSecond=15;legacy.State.Tuning.SpeedMultipliers=[0,1,4,16];CitySaves.Save(first,new CitySave{CityJson=legacy.SaveToJson(),DraftBuilding=BuildingKind.House,DraftAnchor=new(-120,14),DraftRotation=Math.PI/2});LoadCity(first);Require(_sim.State.Now==legacy.State.Now&&_sim.State.ElapsedSeconds==legacy.State.ElapsedSeconds&&_sim.Available==legacy.Available,"Migrar relógio não altera horário ou recursos");Require(_sim.State.Tuning.SimulationSecondsPerRealSecond==1&&_sim.State.Tuning.SpeedMultipliers.SequenceEqual(new double[]{0,1,15,60}),"Save antigo adota escala humana de tempo");Require(_anchor==null&&_cursor==new Point(-120,14)&&_building==BuildingKind.House,"Âncora antiga vira prévia livre sem obra automática");CitySaves.Save(first,new CitySave{CityJson=roadState,RoadTool=true,DraftRoadStart=new(-160,40),DraftRoadEnd=new(-120,80),DraftRoadFixed=true,DraftReverse=true});LoadCity(first);Require(!_roadFixed&&_roadStart==new Point(-160,40)&&_roadEnd==new Point(-120,80)&&_sim.SaveToJson()==roadState,"Prévia viária antiga fixada vira esboço livre sem encomenda");
            var newWork=_sim.PlaceBuilding(BuildingKind.House,new(-100,14));Require(newWork.Success,"Obra real para verificar prévia de movimento");var project=_sim.State.Projects.Single(p=>p.BuildingId==newWork.Id);BeginMove(project);_cursor=_sim.State.LakeCentre;_hoverPreview=true;var moveState=_sim.SaveToJson();var moveFree=_sim.Available;SaveCity();ClearTool();LoadCity(first);Require(_sim.SaveToJson()==moveState&&_sim.Available==moveFree&&_movingProject==project.Id&&_cursor==_sim.State.LakeCentre,"Prévia de movimento preserva obra e compromisso originais");Require(!_sim.QuoteMoveBuilding(project.Id,_cursor,_rotation).PhysicallyValid,"Carregar não valida destino inválido");
            GD.Print("INDEXCITIES_PERSISTENCE_OK cities=2 free-draft=preserved legacy-draft=migrated clock=1x continuation=identical directory="+CitySaves.DirectoryPath);GetTree().Quit();
        }catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);}finally{CitySaves.VerificationDirectory=null;}
    }
    void UiSmoke()
    {
        try{
            _speed=0;_autosaveMinutes=0;Require(_catalog.GetChildren().OfType<VBoxContainer>().Count()==1,"Somente Prefeitura inicialmente");Require(_managementButtons.All(b=>b.Disabled),"Gestão inicialmente bloqueada");var before=_sim.SaveToJson();
            var hallButton=_catalog.GetChildren().OfType<VBoxContainer>().Single().GetChildren().OfType<Button>().Single();Require(hallButton.Icon!=null,"Prefeitura identificada por imagem");hallButton.EmitSignal(Godot.Button.SignalName.Pressed);Require(_sim.SaveToJson()==before,"Escolher tipo não cria obra nem compromisso");WorldClick(new(-152,16),true);Require(_anchor==null&&_sim.State.Buildings.Single().Position==new Point(-152,16),"Um clique encomenda a obra na posição mostrada");
            Require(_sim.State.CityHallPlaced&&!_sim.State.Buildings.Single().Completed,"Confirmar libera sem concluir obra");Require(_catalog.GetChildren().OfType<VBoxContainer>().Count()==10,"Catálogo funcional inteiro liberado");Require(_catalog.GetChildren().OfType<VBoxContainer>().All(item=>item.GetChildren().OfType<Button>().Single().Icon!=null),"Todos os dez tipos têm imagem reconhecível no catálogo");Require(_managementButtons.All(b=>!b.Disabled),"Gestão liberada imediatamente");
            ChooseRoad();WorldClick(new(-192,0),true);WorldClick(new(-192,0),false);_UnhandledInput(new InputEventMouseMotion{Position=_camera.UnprojectPosition(ToV(new(-80,44)))});Require(_roadStart==new Point(-192,0)&&_roadEnd==new Point(-80,44),"Um clique e movimento desenham rua em L");var roadBefore=_sim.SaveToJson();_UnhandledInput(new InputEventKey{Keycode=Key.Tab,Pressed=true});Require(_reverse,"Tab alterna o L sem construir");_UnhandledInput(new InputEventKey{Keycode=Key.Escape,Pressed=true});Require(_sim.SaveToJson()==roadBefore&&_roadStart==null,"Esc cancela somente o esboço");
            ChooseRoad();WorldClick(new(-192,0),true);WorldClick(new(-192,0),false);WorldClick(new(-80,0),true);WorldClick(new(-80,0),false);Require(_sim.State.Projects.Count==2&&_roadStart==null,"Segundo clique encomenda obra viária real e prepara próxima rua");_sim.Advance(7200);Require(_sim.State.Buildings.Single().Completed,"Prefeitura concluída por entregas e equipe físicas");foreach(var kind in Enum.GetValues<BuildingKind>().Where(k=>k!=BuildingKind.CityHall)){Choose(kind);_rotation=Math.PI;_cursor=Snap(new(-112,-(Catalog.Definitions[kind].Depth/2+7)));var quote=_sim.QuoteBuilding(kind,_cursor,_rotation);Require(quote.PhysicallyValid&&quote.Accessible,"Encaixe acessível de "+kind);if(kind==BuildingKind.WorksDepot)Require(_cursor.Z==-18,"Pátio cabe no encaixe sem sobrepor a rua");}ClearTool();SelectAt(_sim.State.Buildings.Single().Position);Require(_selectedBuilding==_sim.State.Buildings.Single().Id&&_inspector.Visible,"Seleção abre inspeção");_sim.CheckInvariants();GD.Print("INDEXCITIES_UI_OK scope=structural-handler-check founding=one-click road=two-click-order-cancel inspection=selected");GetTree().Quit();
        }catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);}
    }
    async void UiInputSmoke()
    {
        try{
            Require(DisplayServer.GetName()!="headless","O teste de eventos da interface exige uma janela real; execute sem --headless");
            _speed=0;_autosaveMinutes=0;
            await RecoveryFrame();await RecoveryPlace(BuildingKind.CityHall,new(-152,16));Require(_sim.State.CityHallPlaced,"Clique real no botão confirma Prefeitura");
            var first=await RecoveryRoad(new(-192,0),new(-128,0),choose:true);Require(_world.GetNodeOrNull<Node3D>("RoadWork"+first.Id)!=null,"Obra viária ainda não concluída fica visível");
            await RecoveryAdvance(7200);Require(first.Status==WorkStatus.Completed,"Primeira rua construída fisicamente");Require(_world.GetNodeOrNull<Node3D>("RoadWork"+first.Id)==null,"Marcador de obra desaparece quando a via está pronta");var second=await RecoveryRoad(new(-128,0),new(-80,0));await RecoveryAdvance(3600);Require(second.Status==WorkStatus.Completed,"Segunda rua confirmada e construída sem reabrir catálogo");
            var isolated=_sim.State.Roads.First(r=>!r.Highway&&!r.Crosswalk&&r.Id%8!=0&&r.Position.X>-120&&r.Position.X<-90&&r.Neighbours.Count==2);foreach(var id in isolated.Neighbours.ToList())Require(_sim.DemolishRoad(id).Success,"Remoção real dos vizinhos");await RecoveryFrame();Require(isolated.Neighbours.Count==0,"Trecho permanece fisicamente isolado");var visual=_world.GetChildren().OfType<Node3D>().Single(n=>Math.Abs(n.Position.X-isolated.Position.X)<.01&&Math.Abs(n.Position.Z-isolated.Position.Z)<.01);Require(visual.FindChildren("*","MeshInstance3D",true,false).Count>0,"Trecho isolado continua desenhado");_sim.CheckInvariants();GD.Print("INDEXCITIES_UI_INPUT_OK viewport=mouse-and-key gui=buttons roads=two-click-two-completed works=visible-until-completion isolated-road=visible");GetTree().Quit();
        }catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);}
    }
    void WorldClick(Point point,bool pressed)=>_UnhandledInput(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=pressed,Position=_camera.UnprojectPosition(ToV(point))});
    async void RealClockSmoke()
    {
        try {
            Require(DisplayServer.GetName()!="headless","Relógio real exige janela renderizada");
            SetSpeed(0);_autosaveMinutes=0;_clockBudget=0;await RecoveryFrame();
            foreach(var speed in new[]{1,2,3}){
                SetSpeed(speed);var elapsed=_sim.State.ElapsedSeconds;var budget=_clockBudget;var at=_clockAt;var watch=System.Diagnostics.Stopwatch.StartNew();
                while(watch.Elapsed.TotalSeconds<(speed==1?6:2)){System.Threading.Thread.Sleep(180);await RecoveryFrame();}
                SetSpeed(0);var real=(_clockAt-at)/1_000_000.0;var advanced=_sim.State.ElapsedSeconds-elapsed;var represented=advanced+_clockBudget-budget;
                var expected=real*_sim.State.Tuning.SimulationSecondsPerRealSecond*_sim.State.Tuning.SpeedMultipliers[speed];
                GD.Print($"INDEXCITIES_REAL_CLOCK_SAMPLE speed={speed} real={real:F3} simulated={advanced:F3} including-budget={represented:F3}");
                Require(Math.Abs(represented-expected)<.1,"Velocidade conserva tempo real inclusive com frames longos e saldo do passo de 2s");
            }
            var paused=_sim.SaveToJson();System.Threading.Thread.Sleep(400);await RecoveryFrame();
            Require(_sim.SaveToJson()==paused,"Pausa não avança tempo nem altera cidade");
            var pausedBudget=_clockBudget;System.Threading.Thread.Sleep(400);SetSpeed(1);
            Require(_sim.SaveToJson()==paused&&Math.Abs(_clockBudget-pausedBudget)<.000001,"Retomar não cobra o intervalo que estava pausado");
            var begin=_clockAt;var before=_sim.State.ElapsedSeconds;var beforeBudget=_clockBudget;System.Threading.Thread.Sleep(350);SetSpeed(0);
            Require(Math.Abs((_sim.State.ElapsedSeconds-before+_clockBudget-beforeBudget)-(_clockAt-begin)/1_000_000.0)<.001,"Pausar entre frames conserva o tempo ativo anterior");
            GD.Print("INDEXCITIES_REAL_CLOCK_OK long-frames=real-time pause=preserved");GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
    bool _performanceSmoke;int _performanceFrames;ulong _performanceAt;readonly List<double> _frameMs=new(),_simulationMs=new(),_presentationMs=new(),_focusedFrameMs=new(),_engineDeltaMs=new();
    void RecordPerformance(double delta,double simulationMs,double presentationMs)
    {
        var at=Time.GetTicksUsec();var interval=_performanceAt==0?delta*1000:(at-_performanceAt)/1000.0;_performanceAt=at;
        if(++_performanceFrames<=60)return;_frameMs.Add(interval);_engineDeltaMs.Add(delta*1000);if(GetWindow().HasFocus())_focusedFrameMs.Add(interval);_simulationMs.Add(simulationMs);_presentationMs.Add(presentationMs);if(_frameMs.Count<240)return;
        var ordered=_frameMs.Order().ToArray();var metrics=new{frames=_frameMs.Count,engineDeltaMeanMs=_engineDeltaMs.Average(),fps=Engine.GetFramesPerSecond(),focusedFrames=_focusedFrameMs.Count,focusedFrameIntervalMeanMs=_focusedFrameMs.Count>0?(double?)_focusedFrameMs.Average():null,frameIntervalMeanMs=_frameMs.Average(),frameIntervalP95Ms=ordered[(int)Math.Floor((ordered.Length-1)*.95)],simulationMeanMs=_simulationMs.Average(),presentationMeanMs=_presentationMs.Average(),drawCalls=Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),nodes=Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),buildings=_sim.State.Buildings.Count(b=>b.Completed&&!b.Removed),residents=_sim.Population,roads=_sim.State.Roads.Count,renderer=RenderingServer.GetCurrentRenderingMethod(),speed=_speed};
        var file=ProjectSettings.GlobalizePath("res://../../artifacts/verification/game-performance.json");System.IO.File.WriteAllText(file,System.Text.Json.JsonSerializer.Serialize(metrics,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));GD.Print("INDEXCITIES_PERFORMANCE_OK "+System.Text.Json.JsonSerializer.Serialize(metrics));_sim.CheckInvariants();_performanceSmoke=false;GetTree().Quit();
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Verificação do jogo: "+message);}
}
