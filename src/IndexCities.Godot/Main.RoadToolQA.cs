using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async Task RoadPlacementChecks(string directory)
    {
        var main=_sim;
        _sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
        ClearTool();_world.Reset(_sim.State);BuildCatalog();RefreshHud();
        _focus=new(-160,0,24);_zoom=110;UpdateCamera();
        Require(_managementButtons.All(b=>b.Disabled),"Gestão permanece bloqueada antes da Prefeitura");
        await RecoveryButton(RecoveryRoadButton());
        var before=_sim.SaveToJson();
        await RecoveryMove(_camera.UnprojectPosition(ToV(new(-192,0))));
        Require(_roadTool&&_roadStart==null&&_sim.SaveToJson()==before,"Rua disponível e prévia sem criar obra antes da Prefeitura");
        Require(_world.GetNodeOrNull<Node3D>("RoadFrontageGuide")==null,"Referência azul removida");
        await RecoveryCapture(System.IO.Path.Combine(directory,"road-before-first-click.png"),GetViewport());
        await RecoveryClick(_camera.UnprojectPosition(ToV(new(-192,0))));
        await RecoveryMove(_camera.UnprojectPosition(ToV(new(-192,48))));
        await RecoveryClick(_camera.UnprojectPosition(ToV(new(-192,48))));
        Require(!_sim.State.CityHallPlaced&&_sim.State.Projects.Count==1,"Mouse encomenda rua antes da Prefeitura");
        await RecoveryAdvance(14400);_world.Sync(_sim.State);
        Require(_sim.State.Projects.Single().Status==WorkStatus.Completed,"Rua inicial conclui por logística e pagamentos reais");
        Require(_sim.QuoteRoadDemolition(_sim.State.Roads.First(r=>!r.Highway).Id).Allowed,"Rua pode ser corrigida antes da Prefeitura");
        Choose(BuildingKind.CityHall);
        await RecoveryMove(_camera.UnprojectPosition(ToV(new(-177,24))));
        var hallPosition=_cursor;var hallRotation=_rotation;
        var hallQuote=_sim.QuoteBuilding(BuildingKind.CityHall,hallPosition,hallRotation);
        Require(hallQuote.PhysicallyValid&&hallQuote.Accessible,"Prefeitura encaixa junto à rua construída primeiro");
        await RecoveryCapture(System.IO.Path.Combine(directory,"hall-after-road-preview.png"),GetViewport());
        await RecoveryClick(_camera.UnprojectPosition(ToV(new(-177,24))));
        Require(_sim.State.CityHallPlaced&&!_sim.State.Buildings.Single().Completed,"Prefeitura libera catálogo sem concluir artificialmente");

        foreach(var kind in Enum.GetValues<BuildingKind>())for(var turn=0;turn<4;turn++){
            Choose(kind);_rotation=turn*Math.PI/2;_manualRotation=true;
            var at=Snap(new(-104.4,97.4));var definition=Catalog.Definitions[kind];
            var halfX=(turn%2==0?definition.Width:definition.Depth)/2;
            var halfZ=(turn%2==0?definition.Depth:definition.Width)/2;
            foreach(var edge in new[]{at.X-halfX,at.X+halfX,at.Z-halfZ,at.Z+halfZ})
                Require(Math.Abs(edge/2-Math.Round(edge/2))<.001,"Bordas na malha para "+kind+" rotação "+turn);
        }
        foreach(var kind in new[]{BuildingKind.House,BuildingKind.Grocery}){
            Choose(kind);await RecoveryMove(_camera.UnprojectPosition(ToV(new(-178,52))));
            var quote=_sim.QuoteBuilding(kind,_cursor,_rotation);
            Require(quote.PhysicallyValid&&quote.Accessible,"Encaixe frontal genérico acessível: "+kind);
            await RecoveryCapture(System.IO.Path.Combine(directory,"placement-"+kind+".png"),GetViewport());
        }
        _sim=main;ClearTool();_world.Reset(_sim.State);BuildCatalog();RefreshHud();
        _focus=new(-150,0,18);_zoom=110;UpdateCamera();ChooseRoad();
        Require(_sim.QuoteRoad(new(-176,0),new(-128,0)).PhysicallyValid,"Rua frontal cabe depois de posicionar a Prefeitura pelas bordas");
        foreach(var dx in new[]{-1.5,1.5})foreach(var dz in new[]{-1.5,1.5})
            Require(Snap(new(-104+dx,80+dz))==new Point(-104,80),"Dentro da célula escolhe seu centro");
        await RecoveryMove(_camera.UnprojectPosition(ToV(new(-152,0))));
        await RecoveryCapture(System.IO.Path.Combine(directory,"road-after-hall-preview.png"),GetViewport());

        // Free endpoints straighten small accidental offsets. Explicit points
        // on a pending road retain their exact position instead of moving away.
        _sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
        ClearTool();ChooseRoad();_roadStart=new(-100,96);
        Require(Snap(new(-60,100))==new Point(-60,96)&&Snap(new(-60,104))==new Point(-60,96),"Desvios livres de 4/8 m viram reta");
        Require(Snap(new(-96,136))==new Point(-100,136)&&Snap(new(-92,136))==new Point(-100,136),"Alinhamento funciona nos dois eixos");
        Require(Snap(new(-60,108))==new Point(-60,108),"Braços de 12 m preservam o L");
        Require(_sim.PlaceRoad(new(-48,80),new(-48,144)).Success,"Rua pendente para verificar alvo explícito");
        _roadStart=new(-56,96);
        Require(Snap(new(-48,120))==new Point(-48,120),"Encaixe pendente não é deslocado para forçar reta");
        Require(_sim.PlaceRoad(new(-176,64),new(-112,64)).Success,"Rua pendente para aproximação em T");
        _roadStart=new(-144,96);
        Require(Snap(new(-144,72))==new Point(-144,64),"Ponta próxima alcança o eixo pendente em vez de deixar fresta");
        Require(_sim.QuoteRoad(_roadStart.Value,new(-144,64)).PhysicallyValid,"Encontro previsto pode ser realmente encomendado");
        Require(_sim.PlaceRoad(new(-192,0),new(-192,48)).Success,"Rua pendente para reproduzir faixa dobrada");
        ClearTool();ChooseRoad();_roadStart=new(-208,0);_reverse=false;
        _focus=new(-180,0,40);_zoom=100;UpdateCamera();
        var foldedPixel=_camera.UnprojectPosition(ToV(new(-188,80)));
        await RecoveryMove(foldedPixel);
        Require(_roadEnd==new Point(-188,80),"Reprodução passa ao lado da via, sem alvo explícito na ponta");
        var foldedQuote=_sim.QuoteRoad(_roadStart.Value,_roadEnd!.Value,_reverse);
        Require(!foldedQuote.PhysicallyValid,"Dobra sobre a faixa é recusada mesmo havendo ligações no grafo");
        await RecoveryCapture(System.IO.Path.Combine(directory,"folded-street-rejected.png"),GetViewport());
        var beforeFold=_sim.SaveToJson();await RecoveryClick(foldedPixel);
        Require(_sim.SaveToJson()==beforeFold,"Clique inválido não encomenda obra nem compromete recursos");
        GD.Print("ROAD_TOOL_FOLD_CHECK preview=rejected click=no-mutation pending=checked");
        _sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
        Require(_sim.PlaceRoad(new(-192,0),new(-160,0)).Success,"Rua para reproduzir minidobra na ponta");
        ClearTool();_world.Reset(_sim.State);BuildCatalog();ChooseRoad();
        _focus=new(-168,0,14);_zoom=80;UpdateCamera();
        await RecoveryClick(_camera.UnprojectPosition(ToV(new(-160,0))));
        await RecoveryClick(_camera.UnprojectPosition(ToV(new(-160,8))));
        Require(_roadStart==new Point(-160,8),"Continuação começa na ponta do ramal encomendado");
        var offsetPixel=_camera.UnprojectPosition(ToV(new(-144,8)));
        await RecoveryMove(offsetPixel);
        Require(!_sim.QuoteRoad(_roadStart.Value,_roadEnd!.Value,_reverse).PhysicallyValid,"Continuação reta não oculta curva apertada na ponta");
        await RecoveryCapture(System.IO.Path.Combine(directory,"endpoint-offset-rejected.png"),GetViewport());
        var beforeOffset=_sim.SaveToJson();await RecoveryClick(offsetPixel);
        Require(_sim.SaveToJson()==beforeOffset,"Minidobra na ponta é recusada sem mutação");
        GD.Print("ROAD_TOOL_ENDPOINT_CHECK continuation=checked preview=rejected click=no-mutation");
        _sim=main;ClearTool();_world.Reset(_sim.State);BuildCatalog();
        GD.Print("ROAD_TOOL_PLACEMENT_CHECK road-first=passed building-first=passed kinds=10 rotations=4 blue=removed near-tee=passed small-bends=passed");
    }

    async void RoadToolVerifyNative()
    {
        if(!QaPrecondition(new[]{new Point(-104,32),new Point(-128,32),new Point(-128,48),new Point(-40,44)}.All(point=>_sim.State.Projects.Any(p=>!p.IsMaintenance&&p.Status!=WorkStatus.Cancelled&&p.RoadPoints.Contains(point))),"road-tool-verify","uma cidade preparada por --road-tool-playtest/fixture (obras viárias encomendadas em -104,32 / -128,32 / -128,48 / -40,44)","-- --road-tool-playtest e depois --road-tool-verify com a mesma fixture (--fixture=<native-placement-fixture.json>)"))return;
        try{
            Require(CitySaves.VerificationDirectory!=null,"Verificação usa saves isolados");
            var expected=_sim.State.Projects.Where(p=>!p.IsMaintenance&&p.Status!=WorkStatus.Cancelled).SelectMany(p=>p.RoadPoints).Distinct().ToArray();
            await RecoveryAdvance(14400);
            Require(_sim.State.Projects.All(p=>p.Status==WorkStatus.Completed),"Obras encomendadas com mouse concluíram com logística real");
            Require(expected.All(p=>_sim.State.Roads.Count(r=>r.Position==p)==1),"Cada ponto encomendado existe uma única vez no grafo pronto");
            var crossing=_sim.State.Roads.Single(r=>r.Position==new Point(-104,32));
            var tee=_sim.State.Roads.Single(r=>r.Position==new Point(-128,32));
            Require(crossing.Neighbours.Count==4&&tee.Neighbours.Count==3,"Cruzamento pendente e T pronto resultam em quatro e três ligações reais");
            Require(_sim.State.Roads.Single(r=>r.Position==new Point(-128,48)).Neighbours.Count==3,"Clique na borda da rua pronta produziu T no eixo real");
            var end=_sim.State.Roads.Single(r=>r.Position==new Point(-40,44));
            Require(_sim.FindRoadRoute(tee.Id,end.Id).Count>0,"L/Tab e continuidade por mouse possuem rota real");
            _sim.CheckInvariants();Require(_sim.MonetaryTotal==_sim.State.Tuning.TotalMoney,"Oferta monetária preservada");
            _focus=new(-88,0,76);_zoom=160;UpdateCamera();ClearTool();_world.Sync(_sim.State);
            await RecoveryCapture(ProjectSettings.GlobalizePath("res://../../artifacts/verification/road-tool/native-completed.png"),GetViewport());
            GD.Print("ROAD_TOOL_NATIVE_VERIFIED points="+expected.Length+" cross=4 tee=3 route="+_sim.FindRoadRoute(tee.Id,end.Id).Count+" money=preserved");
            GetTree().Quit();
        }catch(Exception ex){GD.PushError("ROAD_TOOL_NATIVE_FAILED "+ex);GetTree().Quit(1);}
    }

    async void RoadToolPlaytest()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/road-tool"));
        System.IO.Directory.CreateDirectory(directory);
        try{
            Require(DisplayServer.GetName()!="headless","Cenário exige janela Godot");
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"saves");
            _sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
            _cityId=CitySaves.NewId();_world.Reset(_sim.State);BuildCatalog();
            Choose(BuildingKind.CityHall);_focus=new(-150,0,14);_zoom=110;UpdateCamera();
            // The 18 m depth needs an odd centre for both edges to be even.
            var hallPixel=_camera.UnprojectPosition(ToV(new(-150.8,14.8)));
            await RecoveryClick(hallPixel);
            Require(_sim.State.Buildings.Single().Position==new Point(-150,15),"Clique alinha as bordas reais da Prefeitura");
            await RoadPlacementChecks(directory);
            foreach(var (a,b) in new[]{(new Point(-192,0),new Point(-80,0)),(new Point(-128,0),new Point(-128,64)),(new Point(-128,64),new Point(-64,96))})
                Require(_sim.PlaceRoad(a,b).Success,"Rua encomendada pelas regras normais");
            await RecoveryAdvance(14400);_world.Sync(_sim.State);BuildCatalog();
            Require(_sim.State.Projects.All(p=>p.Status==WorkStatus.Completed),"Obras concluídas por materiais, pagamentos e equipe reais");
            var samples=0;
            foreach(var road in _sim.State.Roads)foreach(var crossing in CitySimulation.CrosswalkLocations(road,_sim.State))
                for(var side=-3.24;side<=3.24;side+=.02){
                    var point=crossing.Centre+(crossing.HorizontalRoad?new Point(0,side):new Point(side,0));
                    Require(!_world.OnUnpaintedAsphalt(point),"Pintura girada conserva travessia sobre a área real");samples++;
                }
            GD.Print("ROAD_TOOL_CROSSWALK_CHECK samples="+samples+" unpainted=0");
            _focus=new(-120,0,44);_zoom=125;_yaw=45;UpdateCamera();ChooseRoad();
            foreach(var zoom in new[]{70f,125f,200f}){
                _zoom=zoom;UpdateCamera();
                foreach(var offset in new[]{-5.0,0,5.0})Require(Snap(new(-128+offset,32))==new Point(-128,32),"Encaixe na superfície coincide com o eixo, zoom "+zoom);
                Require(Snap(new(-128+6.1,32)).X==-120,"Fora da superfície conserva ajuste de grade");
            }
            _focus=new(-88,0,76);_zoom=160;UpdateCamera();
            var invalid=_sim.QuoteRoad(new(-120,16),new(-120,48));
            Require(!invalid.PhysicallyValid,"Paralela 8 m afastada não pode fundir calçadas sem conexão");
            async Task Preview(Point p){await RecoveryMove(_camera.UnprojectPosition(ToV(p)));}
            async Task Click(Point p){await Preview(p);await RecoveryClick(_camera.UnprojectPosition(ToV(p)));}
            await Click(new(-128,32));await Preview(new(-80,32));
            Require(_sim.QuoteRoad(_roadStart!.Value,_roadEnd!.Value,_reverse).PhysicallyValid,"T no eixo é válido");
            await RecoveryCapture(System.IO.Path.Combine(directory,"tee-preview.png"),GetViewport());
            await Click(new(-80,32));
            Require(_roadStart==new Point(-80,32),"Continuidade começa na extremidade realmente clicada");
            await Preview(new(-80,64));await RecoveryKey(Key.Tab);
            await Click(new(-80,64));
            Require(_roadStart==new Point(-80,64),"Segundo trecho mantém continuidade");
            await Preview(new(-40,80));await RecoveryKey(Key.Tab);
            Require(_roadEnd==new Point(-40,80),"Prévia em L está livre do HUD e coincide com o ponteiro");
            await RecoveryCapture(System.IO.Path.Combine(directory,"pending-L-preview.png"),GetViewport());
            var stateBeforeCancel=_sim.SaveToJson();var draftStart=_roadStart;var draftEnd=_roadEnd;var draftReverse=_reverse;
            var rightPixel=_camera.UnprojectPosition(ToV(new(-40,80)));
            async Task RightClick(){
                RecoveryInput(new InputEventMouseButton{ButtonIndex=MouseButton.Right,Pressed=true,Position=rightPixel});await RecoveryFrame();
                var yaw=_yaw;
                RecoveryInput(new InputEventMouseMotion{Position=rightPixel,Relative=new(-300,0),ButtonMask=MouseButtonMask.Right});await RecoveryFrame();
                Require(_yaw==yaw,"Movimento relativo anterior ao clique não vira arrasto da câmera");
                RecoveryInput(new InputEventMouseButton{ButtonIndex=MouseButton.Right,Pressed=false,Position=rightPixel});await RecoveryFrame();
            }
            await RightClick();Require(_roadTool&&_roadStart==null&&_roadEnd==null&&_placementGridVisible,"Primeiro direito cancela esboço e mantém rua");
            Require(_sim.SaveToJson()==stateBeforeCancel,"Cancelar esboço preserva obras e recursos");
            var beforeDragYaw=_yaw;
            RecoveryInput(new InputEventMouseButton{ButtonIndex=MouseButton.Right,Pressed=true,Position=rightPixel});await RecoveryFrame();
            RecoveryInput(new InputEventMouseMotion{Position=rightPixel+new Vector2(40,0),Relative=new(40,0),ButtonMask=MouseButtonMask.Right});await RecoveryFrame();
            RecoveryInput(new InputEventMouseButton{ButtonIndex=MouseButton.Right,Pressed=false,Position=rightPixel+new Vector2(40,0)});await RecoveryFrame();
            Require(Math.Abs(_yaw-beforeDragYaw-12)<.01&&_roadTool&&_roadStart==null,"Arrasto real gira câmera e mantém a ferramenta");
            Require(_sim.SaveToJson()==stateBeforeCancel,"Girar a câmera não altera obras");
            _yaw=beforeDragYaw;UpdateCamera();
            await RightClick();Require(!_roadTool&&!_placementGridVisible,"Segundo direito volta à seleção");
            Require(_sim.SaveToJson()==stateBeforeCancel,"Voltar à seleção preserva obras e recursos");
            ChooseRoad();_roadStart=draftStart;_roadEnd=draftEnd;_reverse=draftReverse;_hoverPreview=true;
            await Preview(new(-40,80));
            GD.Print("ROAD_TOOL_CANCEL_CHECK first=keep-road second=select state=preserved");
            Require(_sim.State.Tuning.TotalMoney==_sim.MonetaryTotal,"Oferta monetária preservada");
            _sim.CheckInvariants();TrySaveCity();
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"fixture.json"),_sim.SaveToJson());
            foreach(var p in new[]{new Point(-128,32),new Point(-80,32),new Point(-80,64),new Point(-40,80)})GD.Print("ROAD_TOOL_POINT "+p+" screen="+_camera.UnprojectPosition(ToV(p)));
            GD.Print("ROAD_TOOL_READY "+directory+" · pausado e jogável; saves isolados");
        }catch(Exception ex){GD.PushError("ROAD_TOOL_FAILED "+ex);GetTree().Quit(1);}
    }
}
