using Godot;
using IndexCities.Simulation;
using Environment=Godot.Environment;
namespace IndexCities.Game;
public partial class Main:Node3D
{
    CitySimulation _sim=null!;CityWorld _world=null!;Camera3D _camera=null!;DirectionalLight3D _sun=null!;
    CanvasLayer _canvas=null!;HBoxContainer _catalog=null!;Label _clock=null!,_money=null!,_status=null!;
    PanelContainer _inspector=null!,_confirmation=null!;VBoxContainer _inspection=null!,_confirmBody=null!;
    BuildingKind? _building=BuildingKind.CityHall;bool _roadTool,_reverse,_roadFixed,_hoverPreview,_holdLoadedPreview;Point? _anchor,_roadStart,_roadEnd;double _rotation;bool _manualRotation;Point? _dragStart;readonly List<(Point At,PlacementQuote Quote)> _row=new();decimal _rowTotal;int _rowValid;
    Vector2? _pointerPixel;Point _cursor;Vector3 _focus=new(-144,0,0);float _yaw=45,_zoom=110;int _speed=1;double _clockBudget,_uiAt,_autosaveAt,_autosaveMinutes=5;
    (Vector3 Focus,float Yaw,float Zoom)? _captureCamera;
    ulong _frameAt,_clockAt;string _cityId="";string? _capturePath;string? _reviewSaveDir;double _captureTimer;int? _selectedBuilding,_selectedCitizen,_selectedVehicle,_selectedRoad,_selectedProject,_movingProject;Window? _modal;
    public override void _Ready()
    {
        var args=OS.GetCmdlineUserArgs();
        if(OS.IsDebugBuild()&&args.Contains("--qa-list")){PrintQaList();SetProcess(false);SetProcessInput(false);SetProcessUnhandledInput(false);GetTree().Quit(0);return;}
        if(!EnsureImportCachePresent())return;
        GetViewport().Msaa3D=Viewport.Msaa.Msaa4X;
        _sim=new CitySimulation(42);_cityId=CitySaves.NewId();_focus=new((float)_sim.State.Entry.X+112,0,8);_world=new CityWorld();AddChild(_world);
        _sun=new DirectionalLight3D{RotationDegrees=new(-48,-30,0),LightColor=new Color("fff0d0"),LightEnergy=1f,ShadowEnabled=true,DirectionalShadowMaxDistance=650};AddChild(_sun);
        AddChild(new WorldEnvironment{Environment=new Environment{BackgroundMode=Environment.BGMode.Color,BackgroundColor=new Color("b9c9d5"),AmbientLightSource=Environment.AmbientSource.Color,AmbientLightColor=new Color("afc5ce"),AmbientLightEnergy=.32f}});
        _camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=_zoom,Far=2000,Current=true};AddChild(_camera);UpdateCamera();
        BuildHud();_world.Reset(_sim.State);RefreshHud();
        var capture=args.FirstOrDefault(a=>a.StartsWith("--capture="));if(capture!=null)_capturePath=capture[10..];
        var review=args.FirstOrDefault(a=>a.StartsWith("--review-save-dir="));if(review!=null){var path=System.IO.Path.GetFullPath(review[18..]);var verification=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification"))+System.IO.Path.DirectorySeparatorChar;if(!path.StartsWith(verification,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("A revisão isolada deve guardar saves em artifacts/verification");_reviewSaveDir=path;CitySaves.VerificationDirectory=path;}
        var fixture=args.FirstOrDefault(a=>a.StartsWith("--fixture="));if(fixture!=null){_sim=CitySimulation.FromJson(System.IO.File.ReadAllText(fixture[10..]));_building=null;_speed=0;_focus=new(-40,0,0);_zoom=250;_world.Reset(_sim.State);BuildCatalog();UpdateCamera();RefreshHud();}
        if(args.Contains("--closeup")){_focus=ToV(_sim.State.Buildings.FirstOrDefault(b=>b.Kind==BuildingKind.CityHall&&!b.Removed)?.Position??new(-100,0));_zoom=110;UpdateCamera();}
        // Capture-only camera framing: --focus=x,z[,zoom]
        if(args.FirstOrDefault(a=>a.StartsWith("--focus=")) is {} focusArg){var parts=focusArg[8..].Split(',').Select(s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray();_focus=new(parts[0],0,parts[1]);if(parts.Length>2)_zoom=parts[2];UpdateCamera();}
        if(args.Contains("--inspect-first")){ClearSelection();_selectedBuilding=_sim.State.Buildings.FirstOrDefault(b=>!b.Removed)?.Id;RefreshInspection();}
        // Roteamento dos cenários de QA a partir do registro único (Main.QARegistry.cs).
        // Só é ativado em builds de editor/debug; em build release as flags são
        // ignoradas e o jogo abre normal. Nenhuma regra do jogo depende disto.
        if(OS.IsDebugBuild())foreach(var scenario in QaScenarios){
            if(!scenario.Flags.Any(args.Contains))continue;
            switch(scenario.Trigger){
                case QaTrigger.Performance:EnablePerformanceSmoke(args);break;
                case QaTrigger.BarCapture:EnableBarCapture();break;
                default:CallDeferred(scenario.Method!);break;
            }
        }
        if(_capturePath!=null)_captureCamera=(_focus,_yaw,_zoom);
        _placementGridVisible=_building!=null||_roadTool;
        _clockAt=_frameAt=Time.GetTicksUsec();
    }
    public override void _Process(double delta)
    {
        var frameAt=Time.GetTicksUsec();var frameSeconds=_frameAt==0?delta:(frameAt-_frameAt)/1_000_000.0;_frameAt=frameAt;
        if(_capturePath!=null&&_captureCamera is {} capture){_focus=capture.Focus;_yaw=capture.Yaw;_zoom=capture.Zoom;UpdateCamera();}
        else if(_modal==null&&GetWindow().HasFocus()){var movement=new Vector3((Input.IsPhysicalKeyPressed(Key.D)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)?1:0),0,(Input.IsPhysicalKeyPressed(Key.S)?1:0)-(Input.IsPhysicalKeyPressed(Key.W)?1:0));if(movement!=Vector3.Zero){_holdLoadedPreview=false;_focus+=movement.Rotated(Vector3.Up,Mathf.DegToRad(_yaw))*_zoom*(float)delta*.45f;UpdateCamera();}}
        var simulationStarted=System.Diagnostics.Stopwatch.GetTimestamp();
        AdvanceRealClock(frameAt);
        var simulationMs=System.Diagnostics.Stopwatch.GetElapsedTime(simulationStarted).TotalMilliseconds;var presentationStarted=System.Diagnostics.Stopwatch.GetTimestamp();
        var hour=(float)_sim.State.Now.TimeOfDay.TotalHours;var daylight=Mathf.Clamp(Mathf.Sin((hour-6)/12*Mathf.Pi),0,1);_sun.LightEnergy=.12f+daylight*.88f;_sun.RotationDegrees=new(-15-daylight*45,-30,0);
        _world.SetWorkLabelOverlayRects(_workLabelOccluders.Where(control=>control.IsVisibleInTree()).Select(control=>control.GetGlobalRect()));
        _world.Sync(_sim.State,_speed==0?1:Math.Clamp(_clockBudget/2,0,1),_speed==0?0:frameSeconds,_sim);UpdatePreview();_world.ShowWaterIntakeRange(_sim,_building==BuildingKind.WaterIntake,_rotation);RefreshToolHint();UpdateVersionHud();_uiAt+=frameSeconds;_autosaveAt+=frameSeconds;
        if(_uiAt>.4){_uiAt=0;RefreshHud();}
        if(_autosaveMinutes>0&&_autosaveAt>=_autosaveMinutes*60){SaveCity();_autosaveAt=0;}
        if(_performanceSmoke)RecordPerformance(delta,simulationMs,System.Diagnostics.Stopwatch.GetElapsedTime(presentationStarted).TotalMilliseconds);
        if(_capturePath!=null){_captureTimer+=frameSeconds;if(_captureTimer>1){var file=_capturePath;_capturePath=null;Capture(file);}}
    }
    void AdvanceRealClock(ulong at)
    {
        // Godot may clamp delta on slow frames. Execute every simulation step
        // belonging to this wall-clock interval at its actual selected speed.
        var elapsed=_clockAt==0?0:(at-_clockAt)/1_000_000.0;_clockAt=at;
        _clockBudget+=elapsed*_sim.State.Tuning.SimulationSecondsPerRealSecond*_sim.State.Tuning.SpeedMultipliers[_speed];
        while(_clockBudget>=2){_world.CaptureMotionBeforeStep(_sim.State);_sim.Advance(2);_clockBudget-=2;}
    }
    void SetSpeed(int speed)
    {
        speed=Math.Clamp(speed,0,3);if(speed==_speed)return;
        AdvanceRealClock(Time.GetTicksUsec());_speed=speed;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if(_modal!=null)return;
        if(RightCameraInput(input))return;
        if(input is InputEventMouseMotion motion){_pointerPixel=motion.Position;_holdLoadedPreview=false;_hoverPreview=true;
            if(Input.IsMouseButtonPressed(MouseButton.Middle)){var current=Ground(motion.Position);var previous=Ground(motion.Position-motion.Relative);_focus+=ToV(previous-current);UpdateCamera();}
            _cursor=Snap(Ground(motion.Position));if(_roadTool&&_roadStart is {} beginning&&!_roadFixed){_roadEnd=_cursor;if(_cursor.Distance(beginning)<.01&&_sim.QuoteRoad(beginning,_cursor,_reverse,_movingProject).Materials.Values.All(amount=>amount==0))_roadEnd=null;UpdateConfirmation();}
            UpdateAlertTooltip(motion.Position);
        }
        if(input is InputEventMouseButton{ButtonIndex:MouseButton.Left,Pressed:false} released)ReleaseLeft(released.Position);
        if(input is InputEventMouseButton mouse&&mouse.Pressed){_pointerPixel=mouse.Position;
            if(mouse.ButtonIndex==MouseButton.WheelUp){_holdLoadedPreview=false;_zoom=Mathf.Clamp(_zoom*.9f,35,500);UpdateCamera();}
            if(mouse.ButtonIndex==MouseButton.WheelDown){_holdLoadedPreview=false;_zoom=Mathf.Clamp(_zoom/.9f,35,500);UpdateCamera();}
            if(mouse.ButtonIndex==MouseButton.Left){
                _holdLoadedPreview=false;_hoverPreview=true;
                _cursor=Snap(Ground(mouse.Position));
                if(_demolitionTool)DemolishAt(Ground(mouse.Position));
                else if(_roadTool){if(_roadStart==null){_roadStart=_cursor;_roadEnd=_cursor;_roadFixed=false;}else{_roadEnd=_cursor;_roadFixed=true;Confirm();}}
                else if(_building!=null){if(_sim.State.CityHallPlaced&&_movingProject==null)_dragStart=_cursor;else{_anchor=_cursor;Confirm();}}else SelectAt(Ground(mouse.Position),mouse.Position);
                UpdateConfirmation();
            }
        }
        if(input is InputEventKey key&&key.Pressed&&!key.Echo){switch(key.Keycode){
            case Key.Escape:ClearTool();break;
            case Key.R:if(_dragStart!=null)break;_manualRotation=true;_rotation=(_rotation+Math.PI/2)%(Math.PI*2);UpdateConfirmation();break;
            case Key.Tab:_reverse=!_reverse;UpdateConfirmation();break;
            case Key.G:TogglePlacementGrid();break;
            case Key.Enter:if(_building!=null&&_hoverPreview){_anchor=_cursor;Confirm();}else if(_roadTool&&_roadStart!=null&&_roadEnd!=null)Confirm();break;
            case Key.Space:SetSpeed(_speed==0?1:0);break;
            case Key.Key1:SetSpeed(1);break;case Key.Key2:SetSpeed(2);break;case Key.Key3:SetSpeed(3);break;
            case Key.Q:_holdLoadedPreview=false;_yaw-=90;UpdateCamera();break;case Key.E:_holdLoadedPreview=false;_yaw+=90;UpdateCamera();break;case Key.F5:SaveCity();break;
        }}
    }
    static Vector3 ToV(Point p,float y=0)=>new((float)p.X,y,(float)p.Z);
    Point Ground(Vector2 screen){var origin=_camera.ProjectRayOrigin(screen);var direction=_camera.ProjectRayNormal(screen);if(Math.Abs(direction.Y)<.001)return _cursor;return _world.GroundPoint(origin,direction);}
    Point Snap(Point p)
    {
        var step=_sim.State.Tuning.RoadStep/2;
        if(_roadTool)return SnapRoadTool(p,_sim.SnapRoad(p));
        if(_building is {} kind){
            var rotation=_rotation;var tolerance=4.0;
            // Auto-orientação: perto de rua urbana a entrada vira para a rua; R vale como manual até sair da proximidade; durante o arrasto a rotação fica travada.
            if(_dragStart==null){var gap=Catalog.Definitions[kind].Depth/2+FrontageOffset(step);var near=PlacementRoadGuides().Where(r=>r.Position.Distance(p)<=gap+8).OrderBy(r=>r.Position.Distance(p)).FirstOrDefault();
                if(near==null)_manualRotation=false;else if(!_manualRotation){rotation=_rotation=FacePlacementGuide(near,p);tolerance=8;}}
            var snapped=SnapBuildingBounds(kind,p,rotation,step);
            return AlignToRoad(kind,snapped,rotation,step,tolerance)??snapped;
        }
        return _sim.SnapRoad(p);
    }
    Point? AlignToRoad(BuildingKind kind,Point snapped,double rotation,double step,double tolerance)
    {
        var definition=Catalog.Definitions[kind];var face=new Point(-Math.Round(Math.Sin(rotation)),-Math.Round(Math.Cos(rotation)));var door=snapped+face*(definition.Depth/2);
        var guide=PlacementRoadGuides().Where(r=>r.Position.Distance(door)<=_sim.State.Tuning.RoadWidth/2+3+tolerance).MinBy(r=>r.Position.Distance(door));if(guide==null)return null;
        var gap=definition.Depth/2+FrontageOffset(step);
        var aligned=guide.Horizontal&&Math.Abs(face.Z)>.9?new Point(snapped.X,guide.Position.Z-face.Z*gap):guide.Vertical&&Math.Abs(face.X)>.9?new Point(guide.Position.X-face.X*gap,snapped.Z):snapped;
        var candidate=new Building{Kind=kind,Position=aligned,Rotation=rotation};
        // Near a guide the frontage line is the only cursor reference: an invalid spot stays there and the quote shows why,
        // instead of falling back to the plain grid, which can sit ~1 m inside the (future) road bed.
        return aligned.Distance(snapped)<=tolerance&&guide.Position.Distance(candidate.Door)<=_sim.State.Tuning.RoadWidth/2+3?aligned:null;
    }
    // Fileira por arrastar: prévias lado a lado ao longo da rua, espaçamento mínimo fisicamente válido; menos de 2 posições = clique simples.
    void ComputeRow()
    {
        _row.Clear();_rowTotal=0;_rowValid=0;if(_dragStart is not {} start||_building is not {} kind)return;
        var definition=Catalog.Definitions[kind];var step=_sim.State.Tuning.RoadStep/2;var alongX=Math.Abs(Math.Sin(_rotation))<.1;var delta=alongX?_cursor.X-start.X:_cursor.Z-start.Z;
        var spacing=Math.Ceiling((definition.Width+.5)/step)*step;var count=(int)(Math.Abs(delta)/spacing)+1;if(count<2)return;var direction=Math.Sign(delta);
        for(var i=0;i<count;i++){var raw=i==0?start:alongX?new Point(start.X+direction*i*spacing,start.Z):new Point(start.X,start.Z+direction*i*spacing);var at=i==0?start:AlignToRoad(kind,raw,_rotation,step,4)??raw;var quote=_sim.QuoteBuilding(kind,at,_rotation);_row.Add((at,quote));if(quote.PhysicallyValid){_rowValid++;_rowTotal+=quote.Total;}}
    }
    public override void _Input(InputEvent input){if(input is InputEventMouseButton{ButtonIndex:MouseButton.Left,Pressed:false} released)ReleaseLeft(released.Position);if(_rightPress!=null&&(input is InputEventMouseMotion||input is InputEventMouseButton{ButtonIndex:MouseButton.Right,Pressed:false})){RightCameraInput(input);GetViewport().SetInputAsHandled();}}
    void ReleaseLeft(Vector2 position)
    {
        if(_dragStart is not {} start||_building is not {} kind){_dragStart=null;return;}
        _pointerPixel=position;_cursor=Snap(Ground(position));ComputeRow();var row=_row.ToList();var total=_rowTotal;var valid=_rowValid;_dragStart=null;_row.Clear();
        if(row.Count<2){_anchor=start;Confirm();UpdateConfirmation();return;}
        if(valid==0){SetStatus("Nenhuma posição válida na fileira");return;}
        if(total>_sim.Available){SetStatus("Saldo insuficiente · disponível "+Cash(_sim.Available)+" · falta "+Cash(total-_sim.Available));return;}
        var ordered=0;var last="";foreach(var item in row.Where(i=>i.Quote.PhysicallyValid)){var result=_sim.PlaceBuilding(kind,item.At,_rotation);last=result.Message;if(result.Success)ordered++;}
        SetStatus(ordered==valid?$"{ordered} obras de {Catalog.Definitions[kind].Name} encomendadas · custo {Cash(total)}":$"{ordered} de {valid} obras encomendadas · "+last);_hoverPreview=false;_world.Sync(_sim.State);RefreshHud();
    }
    void UpdateCamera(){_camera.Size=_zoom;_camera.Position=_focus+new Vector3(300,330,300).Rotated(Vector3.Up,Mathf.DegToRad(_yaw-45));_camera.LookAt(_focus,Vector3.Up);if(_pointerPixel is {} pixel)UpdateAlertTooltip(pixel);}
    void ClearSelection(){_selectedBuilding=null;_selectedCitizen=null;_selectedVehicle=null;_selectedRoad=null;_selectedProject=null;}
    void ClearTool(){if(_building!=null||_roadTool)_placementGridVisible=false;_rightPress=null;_rightDragging=false;_demolitionTool=false;_dragStart=null;_row.Clear();_manualRotation=false;_movingProject=null;_building=null;_roadTool=false;_anchor=null;_roadStart=null;_roadEnd=null;_roadFixed=false;_hoverPreview=false;_holdLoadedPreview=false;_confirmation.Visible=false;_world.ClearPreview();_world.HideWaterIntakeRange();RefreshHud();}
    void Choose(BuildingKind kind){ClearSelection();ClearTool();HideAlertTooltip();_building=kind;_placementGridVisible=true;_rotation=0;_manualRotation=false;_selectedBuilding=null;_inspector.Visible=false;}
    void ChooseRoad(){ClearSelection();ClearTool();HideAlertTooltip();_roadTool=true;_placementGridVisible=true;_inspector.Visible=false;}
    void Confirm()
    {
        var catalogWasUnlocked=_sim.State.CityHallPlaced;
        CommandResult? result=null;if(_building is {} kind&&_anchor is {} p)result=_movingProject is {} moving?_sim.MoveBuildingProject(moving,p,_rotation):_sim.PlaceBuilding(kind,p,_rotation);else if(_roadTool&&_roadStart is {} a&&_roadEnd is {} b)result=_movingProject is {} roadMoving?_sim.MoveRoadProject(roadMoving,a,b,_reverse):_sim.PlaceRoad(a,b,_reverse);
        if(result==null)return;SetStatus(result.Message);_anchor=null;_roadFixed=false;if(result.Success){var nextStart=_roadTool&&_movingProject==null?_roadEnd:null;_hoverPreview=false;_movingProject=null;_roadStart=nextStart;_roadEnd=null;_confirmation.Visible=false;_world.Sync(_sim.State);if(!catalogWasUnlocked&&_sim.State.CityHallPlaced)BuildCatalog();}RefreshHud();
    }
    void UpdatePreview()
    {
        RefreshPlacementGrid();
        if(_demolitionTool){if(_hoverPreview&&_modal==null&&GetViewport().GuiGetHoveredControl()==null&&RoadUnderPointer(Ground(_pointerPixel??GetViewport().GetMousePosition())) is {Highway:false} target)_world.PreviewDemolition(target);else _world.ClearPreview();return;}
        if (_building is {} kind) {
            if (_dragStart is {} dragStart) {
                ComputeRow();
                if (_row.Count >= 2) {
                    _world.PreviewBuildings(kind, _row.Select(r => (r.At, r.Quote.PhysicallyValid, (bool?)r.Quote.Accessible)).ToList(), _rotation);
                    PreviewBuildingAccess(kind, _row.Select(r => (r.At, r.Quote.PhysicallyValid, r.Quote.Accessible)));
                    return;
                }
                var single = _sim.QuoteBuilding(kind, dragStart, _rotation);
                _world.PreviewBuilding(kind, dragStart, _rotation, single.PhysicallyValid, single.Accessible);
                PreviewBuildingAccess(kind, [(dragStart, single.PhysicallyValid, single.Accessible)]);
                return;
            }
            if (_anchor == null) {
                if (!_hoverPreview || _modal != null || !_holdLoadedPreview && GetViewport().GuiGetHoveredControl() != null) { _world.ClearPreview(); return; }
                if (!_holdLoadedPreview) _cursor = Snap(Ground(_pointerPixel ?? GetViewport().GetMousePosition()));
            }
            var p = _anchor ?? _cursor;
            var q = _movingProject is {} moving ? _sim.QuoteMoveBuilding(moving, p, _rotation) : _sim.QuoteBuilding(kind, p, _rotation);
            _world.PreviewBuilding(kind, p, _rotation, q.PhysicallyValid, q.Accessible);
            PreviewBuildingAccess(kind, [(p, q.PhysicallyValid, q.Accessible)]);
        } else if (_roadTool && _roadStart is {} a && _roadEnd is {} b) {
            if (!_roadFixed && !_holdLoadedPreview && _hoverPreview && _modal == null && GetViewport().GuiGetHoveredControl() == null) {
                _cursor = Snap(Ground(_pointerPixel ?? GetViewport().GetMousePosition()));
                if (b.Distance(_cursor) > .001) { b = _cursor; _roadEnd = b; UpdateConfirmation(); }
            }
            var q = _sim.QuoteRoad(a, b, _reverse, _movingProject);
            if(a.Distance(b)<.01&&q.Materials.Values.All(amount=>amount==0)){_roadEnd=null;_world.PreviewRoad([a],true,_sim.State.Tuning.RoadWidth,false);UpdateConfirmation();return;}
            _world.PreviewRoad(_sim.RoadShape(a, b, _reverse), q.PhysicallyValid, _sim.State.Tuning.RoadWidth, q.Accessible,_movingProject);
        } else if(_roadTool&&_roadStart is {} start)_world.PreviewRoad([start],true,_sim.State.Tuning.RoadWidth,false);
        else if(_roadTool&&_hoverPreview&&_modal==null&&GetViewport().GuiGetHoveredControl()==null){
            _cursor=Snap(Ground(_pointerPixel??GetViewport().GetMousePosition()));
            var quote=_sim.QuoteRoad(_cursor,_cursor,false,_movingProject);
            _world.PreviewRoad([_cursor],quote.PhysicallyValid||quote.Materials.Values.All(amount=>amount==0),_sim.State.Tuning.RoadWidth,quote.Accessible);
        }
        else _world.ClearPreview();
    }
    void SelectAt(Point p,Vector2? screen=null){_selectedBuilding=screen is {} pixel?_world.PickBuilding(_camera.ProjectRayOrigin(pixel),_camera.ProjectRayNormal(pixel)):_sim.State.Buildings.Where(b=>!b.Removed&&b.Position.Distance(p)<Math.Max(b.Definition.Width,b.Definition.Depth)*.65).OrderBy(b=>b.Position.Distance(p)).Select(b=>(int?)b.Id).FirstOrDefault();_selectedCitizen=null;_selectedVehicle=null;_selectedRoad=null;_selectedProject=null;
        var c=_sim.State.Citizens.Select(citizen=>(Citizen:citizen,Position:_world.VisibleCitizenPosition(citizen.Id))).Where(item=>item.Position is {} at&&at.Distance(p)<3).OrderBy(item=>item.Position!.Value.Distance(p)).Select(item=>item.Citizen).FirstOrDefault();var v=_sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside&&v.Position.Distance(p)<4).OrderBy(v=>v.Position.Distance(p)).FirstOrDefault();if(c!=null){_selectedCitizen=c.Id;_selectedBuilding=null;}else if(v!=null){_selectedVehicle=v.Id;_selectedBuilding=null;}else if(_selectedBuilding==null){_selectedProject=_sim.State.Projects.Where(project=>project.Status is WorkStatus.Waiting or WorkStatus.InProgress&&project.RoadPoints.Any(point=>point.Distance(p)<6)).Select(project=>(int?)project.Id).FirstOrDefault();if(_selectedProject==null)_selectedRoad=_sim.State.Roads.Where(r=>r.Position.Distance(p)<6).OrderBy(r=>r.Position.Distance(p)).Select(r=>(int?)r.Id).FirstOrDefault();}RefreshInspection();}
    void BeginMove(WorkProject project){ClearSelection();ClearTool();HideAlertTooltip();_placementGridVisible=true;_movingProject=project.Id;if(project.BuildingId is {} id&&_sim.State.Buildings.FirstOrDefault(b=>b.Id==id) is {} building){_building=building.Kind;_rotation=building.Rotation;}else _roadTool=true;_inspector.Visible=false;CloseModal();}
    void SaveCity()=>TrySaveCity();
    bool TrySaveCity(){try{CitySaves.Save(_cityId,new CitySave{CityJson=_sim.SaveToJson(),CameraX=_focus.X,CameraZ=_focus.Z,CameraYaw=_yaw,CameraZoom=_zoom,DraftBuilding=_building,DraftAnchor=_anchor,DraftCursor=_hoverPreview?_cursor:null,DraftRoadStart=_roadStart,DraftRoadEnd=_roadEnd,DraftReverse=_reverse,DraftRotation=_rotation,DraftMoveProject=_movingProject,DraftRoadFixed=_roadFixed,RoadTool=_roadTool,DemolitionTool=_demolitionTool,Speed=_speed,AutosaveMinutes=_autosaveMinutes});_autosaveAt=0;SetStatus("Cidade salva");return true;}catch(Exception ex){SetStatus("Falha ao salvar: "+ex.Message);GD.PushError(ex.ToString());return false;}}
    void LoadCity(string id){try{var s=CitySaves.Load(id);var sim=CitySimulation.FromJson(s.CityJson);sim.State.Tuning.SimulationSecondsPerRealSecond=1;sim.State.Tuning.SpeedMultipliers=[0,1,30,1000];_sim=sim;_cityId=id;ClearSelection();_focus=new(s.CameraX,0,s.CameraZ);_yaw=s.CameraYaw;_zoom=s.CameraZoom;_building=s.DraftBuilding;_anchor=null;_cursor=s.DraftCursor??s.DraftAnchor??s.DraftRoadEnd??_cursor;_hoverPreview=s.DraftCursor!=null||s.DraftAnchor!=null||s.DraftRoadEnd!=null;_holdLoadedPreview=_hoverPreview;_roadStart=s.DraftRoadStart;_roadEnd=s.DraftRoadEnd;_rotation=s.DraftRotation;_reverse=s.DraftReverse;_movingProject=s.DraftMoveProject;_roadTool=s.RoadTool;_placementGridVisible=_building!=null||_roadTool;_demolitionTool=s.DemolitionTool;_rightPress=null;_rightDragging=false;_roadFixed=false;_speed=Math.Clamp(s.Speed,0,3);_autosaveMinutes=s.AutosaveMinutes;_clockBudget=0;_autosaveAt=0;_world.Reset(_sim.State);UpdateCamera();BuildCatalog();RefreshHud();UpdateConfirmation();CloseModal();SetStatus(_sim.State.Name+" carregada");_clockAt=_frameAt=Time.GetTicksUsec();}catch(Exception ex){SetStatus("Falha ao carregar: "+ex.Message);}}
    void NewCity(int seed,string? name=null){string cityName;try{cityName=CitySaves.NormalizeName(name??"");}catch(ArgumentException exception){SetStatus(exception.Message);return;}if(!TrySaveCity())return;ClearSelection();_sim=new CitySimulation(seed);if(cityName.Length>0)_sim.State.Name=cityName;_cityId=CitySaves.NewId();_clockBudget=0;_autosaveAt=0;_focus=new((float)_sim.State.Entry.X+112,0,8);_yaw=45;_zoom=110;ClearTool();_building=BuildingKind.CityHall;_placementGridVisible=true;_world.Reset(_sim.State);UpdateCamera();BuildCatalog();RefreshHud();CloseModal();SetStatus(_sim.State.Name+" criada · a partida anterior foi salva");_clockAt=_frameAt=Time.GetTicksUsec();}
    async void Capture(string file){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);var error=GetViewport().GetTexture().GetImage().SavePng(file);GD.Print("INDEXCITIES_CAPTURE "+error+" "+file);GetTree().Quit(error==Error.Ok?0:1);}
    void Smoke(){try{_sim.CheckInvariants();_world.Sync(_sim.State);GD.Print("INDEXCITIES_SMOKE_OK buildings="+_sim.State.Buildings.Count+" roads="+_sim.State.Roads.Count);}catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);return;}GetTree().Quit();}
}
