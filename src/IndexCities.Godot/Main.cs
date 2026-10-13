using Godot;
using IndexCities.Simulation;
using Environment=Godot.Environment;
namespace IndexCities.Game;
public partial class Main:Node3D
{
    CitySimulation _sim=null!;CityWorld _world=null!;Camera3D _camera=null!;DirectionalLight3D _sun=null!;
    CanvasLayer _canvas=null!;HBoxContainer _catalog=null!;Label _clock=null!,_money=null!,_status=null!;
    PanelContainer _inspector=null!,_confirmation=null!;VBoxContainer _inspection=null!,_confirmBody=null!;
    BuildingKind? _building=BuildingKind.CityHall;bool _roadTool,_reverse,_roadFixed,_hoverPreview,_holdLoadedPreview;Point? _anchor,_roadStart,_roadEnd;double _rotation;
    Vector2? _pointerPixel;Point _cursor;Vector3 _focus=new(-144,0,0);float _yaw=45,_zoom=110;int _speed=1;double _clockBudget,_uiAt,_autosaveAt,_autosaveMinutes=5;
    string _cityId="";string? _capturePath;double _captureTimer;int? _selectedBuilding,_selectedCitizen,_selectedVehicle,_selectedRoad,_selectedProject,_movingProject;Window? _modal;
    public override void _Ready()
    {
        GetViewport().Msaa3D=Viewport.Msaa.Msaa4X;
        _sim=new CitySimulation(42);_cityId=CitySaves.NewId();_focus=new((float)_sim.State.Entry.X+112,0,8);_world=new CityWorld();AddChild(_world);
        _sun=new DirectionalLight3D{RotationDegrees=new(-48,-30,0),LightColor=new Color("fff0d0"),LightEnergy=1f,ShadowEnabled=true,DirectionalShadowMaxDistance=650};AddChild(_sun);
        AddChild(new WorldEnvironment{Environment=new Environment{BackgroundMode=Environment.BGMode.Color,BackgroundColor=new Color("b9c9d5"),AmbientLightSource=Environment.AmbientSource.Color,AmbientLightColor=new Color("afc5ce"),AmbientLightEnergy=.32f}});
        _camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=_zoom,Far=2000,Current=true};AddChild(_camera);UpdateCamera();
        BuildHud();_world.Reset(_sim.State);RefreshHud();
        var args=OS.GetCmdlineUserArgs();var capture=args.FirstOrDefault(a=>a.StartsWith("--capture="));if(capture!=null)_capturePath=capture[10..];
        var review=args.FirstOrDefault(a=>a.StartsWith("--review-save-dir="));if(review!=null){var path=System.IO.Path.GetFullPath(review[18..]);var verification=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification"))+System.IO.Path.DirectorySeparatorChar;if(!path.StartsWith(verification,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("A revisão isolada deve guardar saves em artifacts/verification");CitySaves.VerificationDirectory=path;}
        var fixture=args.FirstOrDefault(a=>a.StartsWith("--fixture="));if(fixture!=null){_sim=CitySimulation.FromJson(System.IO.File.ReadAllText(fixture[10..]));_building=null;_speed=0;_focus=new(-40,0,0);_zoom=250;_world.Reset(_sim.State);BuildCatalog();UpdateCamera();RefreshHud();}
        if(args.Contains("--closeup")){_focus=ToV(_sim.State.Buildings.FirstOrDefault(b=>b.Kind==BuildingKind.CityHall&&!b.Removed)?.Position??new(-100,0));_zoom=110;UpdateCamera();}
        if(args.Contains("--inspect-first")){ClearSelection();_selectedBuilding=_sim.State.Buildings.FirstOrDefault(b=>!b.Removed)?.Id;RefreshInspection();}
        if(args.Contains("--performance-smoke")){_performanceSmoke=true;_speed=1;_autosaveMinutes=0;}
        if(args.Contains("--persistence-smoke"))CallDeferred(MethodName.PersistenceSmoke);
        if(args.Contains("--ui-smoke"))CallDeferred(MethodName.UiSmoke);
        if(args.Contains("--ui-input-smoke"))CallDeferred(MethodName.UiInputSmoke);
        if(args.Contains("--recovery-smoke"))CallDeferred(MethodName.RecoverySmoke);
        if(args.Contains("--visual-review-smoke"))CallDeferred(MethodName.VisualReviewSmoke);
        if(args.Contains("--real-time-start-smoke"))CallDeferred(MethodName.RealTimeStartSmoke);
        if(args.Contains("--interaction-regressions-smoke"))CallDeferred(MethodName.InteractionRegressionSmoke);
        if(args.Contains("--smoke"))CallDeferred(MethodName.Smoke);
    }
    public override void _Process(double delta)
    {
        if(_modal==null){var movement=new Vector3((Input.IsPhysicalKeyPressed(Key.D)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)?1:0),0,(Input.IsPhysicalKeyPressed(Key.S)?1:0)-(Input.IsPhysicalKeyPressed(Key.W)?1:0));if(movement!=Vector3.Zero){_holdLoadedPreview=false;_focus+=movement.Rotated(Vector3.Up,Mathf.DegToRad(_yaw))*_zoom*(float)delta*.45f;UpdateCamera();}}
        var simulationStarted=System.Diagnostics.Stopwatch.GetTimestamp();
        _clockBudget+=delta*_sim.State.Tuning.SimulationSecondsPerRealSecond*_sim.State.Tuning.SpeedMultipliers[_speed];
        while(_clockBudget>=2){_world.CaptureMotionBeforeStep(_sim.State);_sim.Advance(2);_clockBudget-=2;}
        var simulationMs=System.Diagnostics.Stopwatch.GetElapsedTime(simulationStarted).TotalMilliseconds;var presentationStarted=System.Diagnostics.Stopwatch.GetTimestamp();
        var hour=(float)_sim.State.Now.TimeOfDay.TotalHours;var daylight=Mathf.Clamp(Mathf.Sin((hour-6)/12*Mathf.Pi),0,1);_sun.LightEnergy=.12f+daylight*.88f;_sun.RotationDegrees=new(-15-daylight*45,-30,0);
        _world.Sync(_sim.State,_speed==0?1:Math.Clamp(_clockBudget/2,0,1));UpdatePreview();RefreshToolHint();_uiAt+=delta;_autosaveAt+=delta;
        if(_uiAt>.4){_uiAt=0;RefreshHud();}
        if(_autosaveMinutes>0&&_autosaveAt>=_autosaveMinutes*60){SaveCity();_autosaveAt=0;}
        if(_performanceSmoke)RecordPerformance(delta,simulationMs,System.Diagnostics.Stopwatch.GetElapsedTime(presentationStarted).TotalMilliseconds);
        if(_capturePath!=null){_captureTimer+=delta;if(_captureTimer>1){var file=_capturePath;_capturePath=null;Capture(file);}}
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if(_modal!=null)return;
        if(input is InputEventMouseMotion motion){_pointerPixel=motion.Position;_holdLoadedPreview=false;_hoverPreview=true;
            if(Input.IsMouseButtonPressed(MouseButton.Middle)){var current=Ground(motion.Position);var previous=Ground(motion.Position-motion.Relative);_focus+=ToV(previous-current);UpdateCamera();}
            _cursor=Snap(Ground(motion.Position));if(_roadTool&&_roadStart!=null&&!_roadFixed){_roadEnd=_cursor;UpdateConfirmation();}
        }
        if(input is InputEventMouseButton mouse&&mouse.Pressed){_pointerPixel=mouse.Position;
            if(mouse.ButtonIndex==MouseButton.WheelUp){_holdLoadedPreview=false;_zoom=Mathf.Clamp(_zoom*.9f,35,500);UpdateCamera();}
            if(mouse.ButtonIndex==MouseButton.WheelDown){_holdLoadedPreview=false;_zoom=Mathf.Clamp(_zoom/.9f,35,500);UpdateCamera();}
            if(mouse.ButtonIndex==MouseButton.Right){ClearTool();return;}
            if(mouse.ButtonIndex==MouseButton.Left){
                _holdLoadedPreview=false;_hoverPreview=true;
                _cursor=Snap(Ground(mouse.Position));
                if(_roadTool){if(_roadStart==null){_roadStart=_cursor;_roadEnd=_cursor;_roadFixed=false;}else{_roadEnd=_cursor;_roadFixed=true;Confirm();}}
                else if(_building!=null){_anchor=_cursor;Confirm();}else SelectAt(Ground(mouse.Position),mouse.Position);
                UpdateConfirmation();
            }
        }
        if(input is InputEventKey key&&key.Pressed&&!key.Echo){switch(key.Keycode){
            case Key.Escape:ClearTool();break;
            case Key.R:_rotation=(_rotation+Math.PI/2)%(Math.PI*2);UpdateConfirmation();break;
            case Key.Tab:_reverse=!_reverse;UpdateConfirmation();break;
            case Key.Enter:if(_building!=null&&_hoverPreview){_anchor=_cursor;Confirm();}else if(_roadTool&&_roadStart!=null&&_roadEnd!=null)Confirm();break;
            case Key.Space:_speed=_speed==0?1:0;break;
            case Key.Key1:_speed=1;break;case Key.Key2:_speed=2;break;case Key.Key3:_speed=3;break;
            case Key.Q:_holdLoadedPreview=false;_yaw-=90;UpdateCamera();break;case Key.E:_holdLoadedPreview=false;_yaw+=90;UpdateCamera();break;case Key.F5:SaveCity();break;
        }}
    }
    static Vector3 ToV(Point p,float y=0)=>new((float)p.X,y,(float)p.Z);
    Point Ground(Vector2 screen){var origin=_camera.ProjectRayOrigin(screen);var direction=_camera.ProjectRayNormal(screen);if(Math.Abs(direction.Y)<.001)return _cursor;var p=origin+direction*(-origin.Y/direction.Y);return new(p.X,p.Z);}
    Point Snap(Point p)
    {
        var step=_sim.State.Tuning.RoadStep/(_building!=null?2:1);var snapped=new Point(Math.Round(p.X/step)*step,Math.Round(p.Z/step)*step);
        if(_roadTool){var near=_sim.State.Roads.Where(r=>r.Position.Distance(p)<=_sim.State.Tuning.RoadWidth/2+_sim.State.Tuning.RoadStep).OrderBy(r=>r.Position.Distance(p)).FirstOrDefault();if(near!=null&&_camera.UnprojectPosition(ToV(near.Position)).DistanceTo(_camera.UnprojectPosition(ToV(p)))<=20)return near.Position;}
        if(_building is {} kind){var definition=Catalog.Definitions[kind];var face=new Point(-Math.Sin(_rotation),-Math.Cos(_rotation));var door=snapped+face*(definition.Depth/2);var road=_sim.State.Roads.Where(r=>!r.Highway&&r.Position.Distance(door)<=_sim.State.Tuning.RoadWidth/2+3).OrderBy(r=>r.Position.Distance(door)).FirstOrDefault();if(road!=null){var gap=Math.Ceiling((definition.Depth/2+_sim.State.Tuning.RoadWidth/2+.75)/step)*step;var horizontal=road.Neighbours.Any(id=>Math.Abs(_sim.State.Roads.First(r=>r.Id==id).Position.X-road.Position.X)>.1);var vertical=road.Neighbours.Any(id=>Math.Abs(_sim.State.Roads.First(r=>r.Id==id).Position.Z-road.Position.Z)>.1);var aligned=horizontal&&Math.Abs(face.Z)>.9?new Point(snapped.X,road.Position.Z-face.Z*gap):vertical&&Math.Abs(face.X)>.9?new Point(road.Position.X-face.X*gap,snapped.Z):snapped;if(aligned.Distance(snapped)<=4&&_sim.QuoteBuilding(kind,aligned,_rotation,_movingProject).PhysicallyValid)return aligned;}}
        return snapped;
    }
    void UpdateCamera(){_camera.Size=_zoom;_camera.Position=_focus+new Vector3(300,330,300).Rotated(Vector3.Up,Mathf.DegToRad(_yaw-45));_camera.LookAt(_focus,Vector3.Up);}
    void ClearSelection(){_selectedBuilding=null;_selectedCitizen=null;_selectedVehicle=null;_selectedRoad=null;_selectedProject=null;}
    void ClearTool(){_movingProject=null;_building=null;_roadTool=false;_anchor=null;_roadStart=null;_roadEnd=null;_roadFixed=false;_hoverPreview=false;_holdLoadedPreview=false;_confirmation.Visible=false;_world.ClearPreview();RefreshHud();}
    void Choose(BuildingKind kind){ClearSelection();ClearTool();_building=kind;_rotation=0;_selectedBuilding=null;_inspector.Visible=false;}
    void ChooseRoad(){if(!_sim.State.CityHallPlaced)return;ClearSelection();ClearTool();_roadTool=true;_inspector.Visible=false;}
    void Confirm()
    {
        var catalogWasUnlocked=_sim.State.CityHallPlaced;
        CommandResult? result=null;if(_building is {} kind&&_anchor is {} p)result=_movingProject is {} moving?_sim.MoveBuildingProject(moving,p,_rotation):_sim.PlaceBuilding(kind,p,_rotation);else if(_roadTool&&_roadStart is {} a&&_roadEnd is {} b)result=_movingProject is {} roadMoving?_sim.MoveRoadProject(roadMoving,a,b,_reverse):_sim.PlaceRoad(a,b,_reverse);
        if(result==null)return;SetStatus(result.Message);_anchor=null;_roadFixed=false;if(result.Success){_hoverPreview=false;_movingProject=null;_roadStart=null;_roadEnd=null;_confirmation.Visible=false;_world.Sync(_sim.State);if(!catalogWasUnlocked&&_sim.State.CityHallPlaced)BuildCatalog();}RefreshHud();
    }
    void UpdatePreview(){if(_building is {} kind){if(_anchor==null){if(!_hoverPreview||_modal!=null||!_holdLoadedPreview&&GetViewport().GuiGetHoveredControl()!=null){_world.ClearPreview();return;}if(!_holdLoadedPreview)_cursor=Snap(Ground(_pointerPixel??GetViewport().GetMousePosition()));}var p=_anchor??_cursor;var q=_movingProject is {} moving?_sim.QuoteMoveBuilding(moving,p,_rotation):_sim.QuoteBuilding(kind,p,_rotation);_world.PreviewBuilding(kind,p,_rotation,q.PhysicallyValid,q.Accessible);}else if(_roadTool&&_roadStart is {} a&&_roadEnd is {} b){if(!_roadFixed&&!_holdLoadedPreview&&_hoverPreview&&_modal==null&&GetViewport().GuiGetHoveredControl()==null){_cursor=Snap(Ground(_pointerPixel??GetViewport().GetMousePosition()));if(b.Distance(_cursor)>.001){b=_cursor;_roadEnd=b;UpdateConfirmation();}}var q=_sim.QuoteRoad(a,b,_reverse,_movingProject);_world.PreviewRoad(_sim.RoadShape(a,b,_reverse),q.PhysicallyValid,_sim.State.Tuning.RoadWidth,q.Accessible);}else _world.ClearPreview();}
    void SelectAt(Point p,Vector2? screen=null){_selectedBuilding=screen is {} pixel?_world.PickBuilding(_camera.ProjectRayOrigin(pixel),_camera.ProjectRayNormal(pixel)):_sim.State.Buildings.Where(b=>!b.Removed&&b.Position.Distance(p)<Math.Max(b.Definition.Width,b.Definition.Depth)*.65).OrderBy(b=>b.Position.Distance(p)).Select(b=>(int?)b.Id).FirstOrDefault();_selectedCitizen=null;_selectedVehicle=null;_selectedRoad=null;_selectedProject=null;
        var c=_sim.State.Citizens.Where(c=>c.Alive&&c.InsideMap&&c.VehicleId==null&&(c.Moving||!_sim.State.Buildings.Any(b=>b.Completed&&!b.Removed&&b.Door.Distance(c.Position)<1))&&c.Position.Distance(p)<3).OrderBy(c=>c.Position.Distance(p)).FirstOrDefault();var v=_sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside&&v.Position.Distance(p)<4).OrderBy(v=>v.Position.Distance(p)).FirstOrDefault();if(c!=null){_selectedCitizen=c.Id;_selectedBuilding=null;}else if(v!=null){_selectedVehicle=v.Id;_selectedBuilding=null;}else if(_selectedBuilding==null){_selectedProject=_sim.State.Projects.Where(project=>project.Status is WorkStatus.Waiting or WorkStatus.InProgress&&project.RoadPoints.Any(point=>point.Distance(p)<6)).Select(project=>(int?)project.Id).FirstOrDefault();if(_selectedProject==null)_selectedRoad=_sim.State.Roads.Where(r=>r.Position.Distance(p)<6).OrderBy(r=>r.Position.Distance(p)).Select(r=>(int?)r.Id).FirstOrDefault();}RefreshInspection();}
    void BeginMove(WorkProject project){ClearSelection();ClearTool();_movingProject=project.Id;if(project.BuildingId is {} id&&_sim.State.Buildings.FirstOrDefault(b=>b.Id==id) is {} building){_building=building.Kind;_rotation=building.Rotation;}else _roadTool=true;_inspector.Visible=false;CloseModal();}
    void SaveCity()=>TrySaveCity();
    bool TrySaveCity(){try{CitySaves.Save(_cityId,new CitySave{CityJson=_sim.SaveToJson(),CameraX=_focus.X,CameraZ=_focus.Z,CameraYaw=_yaw,CameraZoom=_zoom,DraftBuilding=_building,DraftAnchor=_anchor,DraftCursor=_hoverPreview?_cursor:null,DraftRoadStart=_roadStart,DraftRoadEnd=_roadEnd,DraftReverse=_reverse,DraftRotation=_rotation,DraftMoveProject=_movingProject,DraftRoadFixed=_roadFixed,RoadTool=_roadTool,Speed=_speed,AutosaveMinutes=_autosaveMinutes});_autosaveAt=0;SetStatus("Cidade salva");return true;}catch(Exception ex){SetStatus("Falha ao salvar: "+ex.Message);GD.PushError(ex.ToString());return false;}}
    void LoadCity(string id){try{var s=CitySaves.Load(id);var sim=CitySimulation.FromJson(s.CityJson);sim.State.Tuning.SimulationSecondsPerRealSecond=1;sim.State.Tuning.SpeedMultipliers=[0,1,15,60];_sim=sim;_cityId=id;ClearSelection();_focus=new(s.CameraX,0,s.CameraZ);_yaw=s.CameraYaw;_zoom=s.CameraZoom;_building=s.DraftBuilding;_anchor=null;_cursor=s.DraftCursor??s.DraftAnchor??s.DraftRoadEnd??_cursor;_hoverPreview=s.DraftCursor!=null||s.DraftAnchor!=null||s.DraftRoadEnd!=null;_holdLoadedPreview=_hoverPreview;_roadStart=s.DraftRoadStart;_roadEnd=s.DraftRoadEnd;_rotation=s.DraftRotation;_reverse=s.DraftReverse;_movingProject=s.DraftMoveProject;_roadTool=s.RoadTool;_roadFixed=false;_speed=Math.Clamp(s.Speed,0,3);_autosaveMinutes=s.AutosaveMinutes;_clockBudget=0;_autosaveAt=0;_world.Reset(_sim.State);UpdateCamera();BuildCatalog();RefreshHud();UpdateConfirmation();CloseModal();}catch(Exception ex){SetStatus("Falha ao carregar: "+ex.Message);}}
    void NewCity(int seed){if(!TrySaveCity())return;ClearSelection();_sim=new CitySimulation(seed);_cityId=CitySaves.NewId();_clockBudget=0;_autosaveAt=0;_focus=new((float)_sim.State.Entry.X+112,0,8);_yaw=45;_zoom=110;ClearTool();_building=BuildingKind.CityHall;_world.Reset(_sim.State);UpdateCamera();BuildCatalog();RefreshHud();CloseModal();}
    async void Capture(string file){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);var error=GetViewport().GetTexture().GetImage().SavePng(file);GD.Print("INDEXCITIES_CAPTURE "+error+" "+file);GetTree().Quit(error==Error.Ok?0:1);}
    void Smoke(){try{_sim.CheckInvariants();_world.Sync(_sim.State);GD.Print("INDEXCITIES_SMOKE_OK buildings="+_sim.State.Buildings.Count+" roads="+_sim.State.Roads.Count);}catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);return;}GetTree().Quit();}
}
