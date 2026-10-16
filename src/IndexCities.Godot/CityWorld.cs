using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;
public partial class CityWorld:Node3D
{
    Node3D _static=null!,_preview=null!;Dictionary<int,Node3D> _buildings=new(),_roads=new(),_people=new(),_trucks=new();Dictionary<int,bool> _finished=new();Dictionary<int,OmniLight3D> _lamps=new();Dictionary<int,MeshInstance3D> _lampGlass=new();Material? _lampOn,_lampOff;Dictionary<int,Label3D> _causes=new();
    readonly Dictionary<int,Label3D> _companySigns=new();readonly Dictionary<int,Node3D> _rotors=new(),_waterStates=new(),_stockStates=new();readonly Dictionary<int,List<(ResourceKind Resource,Node3D Node)>> _stockMarkers=new();readonly List<(Node3D Node,Point Position)> _trees=new();int? _vegetationGeometry;double _lastVisualTime;
    int _roadRevision=-1;string _previewKey="";CityState _state=null!;
    readonly Dictionary<int,Node3D> _roadWorks=new();readonly Dictionary<int,Label3D> _roadWorkLabels=new();int? _roadWorkGeometry;
    BuildingKind? _previewBuildingKind;
    sealed class BuildingPreviewMarker{public Node3D Node=null!;public readonly List<MeshInstance3D> Footprint=new();public Label3D Validity=null!,Warning=null!;public string? Color;}
    readonly List<BuildingPreviewMarker> _previewPool=new();readonly (Point At,bool Valid,bool? Connected)[] _singlePreview=new (Point,bool,bool?)[1];
    readonly Dictionary<int,List<MeshInstance3D>> _buildingPickMeshes=new();
    Node3D? _previewAccess;int? _previewAccessKey;
    readonly List<Rect2> _roadFootprints=new(),_roadCarriageways=new();int? _labelLayoutKey;
    readonly List<Rect2> _workLabelOverlayRects=new();
    public void SetWorkLabelOverlayRects(IEnumerable<Rect2> rectangles){_workLabelOverlayRects.Clear();_workLabelOverlayRects.AddRange(rectangles);}
    readonly List<PreviewRectangle> _crosswalkStripes=new();Node3D? _crosswalkPaint;
    const float RoadAsphaltTop=.03f,RoadSidewalkTop=.18f,RoadVisualLift=.01f;
    const float PlacementPlane=-.06f;
    ShaderMaterial? _lakeMaterial;double _lakePresentationTime;
    sealed record MotionBefore(Point Position,object Path,int Index,List<Point> Remaining);
    sealed class MotionTrace
    {
        public readonly List<Point> Points;public readonly double Length;
        public MotionTrace(List<Point> points){Points=points;for(var i=1;i<points.Count;i++)Length+=points[i-1].Distance(points[i]);}
        public (Point Position,double? Heading) At(double fraction)
        {
            var distance=Length*Math.Clamp(fraction,0,1);
            for(var i=1;i<Points.Count;i++){var a=Points[i-1];var b=Points[i];var segment=a.Distance(b);if(segment<.00001)continue;if(distance<=segment){var delta=b-a;return (a.MoveTowards(b,distance),Math.Atan2(delta.X,delta.Z)+Math.PI);}distance-=segment;}
            return (Points[^1],null);
        }
    }
    readonly Dictionary<int,MotionBefore> _peopleBefore=new(),_vehiclesBefore=new();
    readonly Dictionary<int,MotionTrace> _peopleMotion=new(),_vehicleMotion=new();
    CityState? _motionState;double _motionBeforeAt,_motionBuiltAt=double.NaN;
    public static string Visual(BuildingKind kind)=>kind switch{BuildingKind.CityHall=>"civic.city_hall.a",BuildingKind.WorksDepot=>"civic.works_depot.a",BuildingKind.House=>"residential.small_house.a",BuildingKind.Grocery=>"commercial.grocery_small.a",BuildingKind.Farm=>"agriculture.farm_small.a",BuildingKind.FuelStation=>"commercial.fuel_station.a",BuildingKind.WaterIntake=>"utility.water_intake.a",BuildingKind.WaterTreatment=>"utility.water_treatment.a",BuildingKind.SewageTreatment=>"utility.sewage_treatment.a",_=>"utility.wind_power.a"};
    public void Reset(CityState state)
    {
        _placementGridSurfaces.Clear();
        _waterIntakeRanges.Clear();_waterIntakeRange=null;
        foreach(var child in GetChildren()){RemoveChild(child);child.QueueFree();}_buildings.Clear();_roads.Clear();_people.Clear();_trucks.Clear();_finished.Clear();_lamps.Clear();_lampGlass.Clear();_causes.Clear();_companySigns.Clear();_rotors.Clear();_waterStates.Clear();_stockStates.Clear();_stockMarkers.Clear();_trees.Clear();_vegetationGeometry=null;_lastVisualTime=state.ElapsedSeconds;_roadRevision=-1;_state=state;
        _roadWorks.Clear();_roadWorkLabels.Clear();_roadWorkGeometry=null;_roadSurface=null;_sidewalkSurface.Clear();
        _buildingPickMeshes.Clear();_roadFootprints.Clear();_roadCarriageways.Clear();_crosswalkStripes.Clear();_crosswalkPaint=null;_labelLayoutKey=null;
        ClearActivityBadges();_previewAccess=null;_previewAccessKey=null;
        ClearProblemAlerts();_problemElapsedAt=state.ElapsedSeconds;
        _motionState=null;_peopleBefore.Clear();_vehiclesBefore.Clear();_peopleMotion.Clear();_vehicleMotion.Clear();_motionBuiltAt=double.NaN;
        _static=new Node3D();AddChild(_static);_preview=new Node3D();AddChild(_preview);_previewKey="";_previewBuildingKind=null;_previewPool.Clear();
        var half=(float)state.Tuning.MapHalfSize;BuildLakeEnvironment(state);Box(_static,new(half*2+6,4,half*2+6),new(0,-3,0),"917656");
        var random=new Random(state.Seed^123451);for(var i=0;i<145;i++){var p=new Point((random.NextDouble()*2-1)*(half-10),(random.NextDouble()*2-1)*(half-10));if((state.SchemaVersion==1?p.Distance(state.LakeCentre)<state.LakeRadius+9:LakeGeometry.SignedDistance(state,p)<9)||Math.Abs(p.Z)<10)continue;var tree=Scene("vegetation.tree_broadleaf.a")??FallbackTree();tree.Position=P(p,state.SchemaVersion==1?0:VisibleTerrainHeight(p)+.06f);tree.Scale=Vector3.One*(float)(.6+random.NextDouble()*.45);tree.RotationDegrees=new(0,random.Next(360),0);tree.SetMeta("tree_position",new Vector2((float)p.X,(float)p.Z));_static.AddChild(tree);_trees.Add((tree,p));}
        foreach(var a in new[]{new Vector3(-half,.2f,0),new Vector3(half,.2f,0),new Vector3(0,.2f,-half),new Vector3(0,.2f,half)}){var alongX=Math.Abs(a.Z)>.01;Box(_static,alongX?new(half*2,.12f,.25f):new(.25f,.12f,half*2),a,"dce8b9");}
        Sync(state);
    }
    static Vector3 P(Point p,float y=0)=>new((float)p.X,y,(float)p.Z);
    static readonly Dictionary<(string,float),StandardMaterial3D> Materials=new();
    static StandardMaterial3D Material(string hex,float alpha=1){var key=(hex,alpha);if(!Materials.TryGetValue(key,out var result)){result=new(){AlbedoColor=new Color(hex,alpha),Roughness=.85f,Transparency=alpha<1?BaseMaterial3D.TransparencyEnum.Alpha:BaseMaterial3D.TransparencyEnum.Disabled};Materials[key]=result;}return result;}
    static readonly Dictionary<(string,float),StandardMaterial3D> PreviewMaterials=new();
    static StandardMaterial3D PreviewMaterial(string hex,float alpha=.98f)
    {
        var key=(hex,alpha);if(!PreviewMaterials.TryGetValue(key,out var material)){material=new StandardMaterial3D{AlbedoColor=new Color(hex,alpha),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,NoDepthTest=true,CullMode=BaseMaterial3D.CullModeEnum.Disabled,RenderPriority=alpha<.9f?8:10};PreviewMaterials[key]=material;}return material;
    }
    static MeshInstance3D PreviewBox(Node3D parent,Vector3 size,Vector3 at,StandardMaterial3D material)
    {
        var mesh=new MeshInstance3D{Mesh=new BoxMesh{Size=size},Position=at,MaterialOverride=material,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};parent.AddChild(mesh);return mesh;
    }
    static MeshInstance3D Box(Node3D parent,Vector3 size,Vector3 at,string hex,float alpha=1){var mesh=new MeshInstance3D{Mesh=new BoxMesh{Size=size},Position=at,MaterialOverride=Material(hex,alpha)};parent.AddChild(mesh);return mesh;}
    // Paint variety is presentation only: a stable color per real vehicle.
    static readonly string[] CarPaints=["car_paint","car_paint_red","car_paint_blue","car_paint_white","car_paint_yellow","car_paint_green"];
    static void PaintCar(Node3D car,int id){if(car.GetNodeOrNull<MeshInstance3D>("Body") is not {Mesh:ArrayMesh mesh} body)return;var file="res://Assets/V1/Materials/"+CarPaints[(int)((uint)(id*2654435761u)%CarPaints.Length)]+".tres";if(!ResourceLoader.Exists(file))return;var paint=GD.Load<Material>(file);for(var i=0;i<mesh.GetSurfaceCount();i++)if(mesh.SurfaceGetName(i)=="car_paint")body.SetSurfaceOverrideMaterial(i,paint);}
    static Node3D? Scene(string id){var file="res://Assets/V1/Scenes/"+id+".tscn";return ResourceLoader.Exists(file)?GD.Load<PackedScene>(file)?.Instantiate<Node3D>():null;}
    static Node3D FallbackTree(){var root=new Node3D();Box(root,new(.65f,3,.65f),new(0,1.5f,0),"745541");root.AddChild(new MeshInstance3D{Mesh=new SphereMesh{Radius=2.5f,Height=5,RadialSegments=8,Rings=4},Position=new(0,4,0),MaterialOverride=Material("598745")});return root;}
    static Node3D BuildingVisual(Building b)=>BuildingVisual(b.Kind);
    static Node3D BuildingVisual(BuildingKind kind)
    {
        var definition=Catalog.Definitions[kind];
        var node=Scene(Visual(kind));if(node!=null){var size=node.GetMeta("visual_envelope_size",new Vector3((float)definition.Width,8,(float)definition.Depth)).AsVector3();var scale=Mathf.Min(1,Mathf.Min((float)definition.Width/Math.Max(1,size.X),(float)definition.Depth/Math.Max(1,size.Z)));node.Scale=Vector3.One*scale;return node;}
        node=new Node3D();Box(node,new((float)definition.Width,.12f,(float)definition.Depth),new(0,.06f,0),"c9c7ae");Box(node,new((float)definition.Width*.7f,5,(float)definition.Depth*.65f),new(0,2.5f,0),kind is BuildingKind.WaterIntake or BuildingKind.WaterTreatment or BuildingKind.SewageTreatment?"79a9ad":"ead9b6");Box(node,new((float)definition.Width*.76f,.65f,(float)definition.Depth*.72f),new(0,5.3f,0),"b46643");return node;
    }
    // Called immediately before a real simulation quantum. No state is advanced or modified here.
    public void CaptureMotionBeforeStep(CityState state)
    {
        _motionState=state;_motionBeforeAt=state.ElapsedSeconds;_motionBuiltAt=double.NaN;_peopleBefore.Clear();_vehiclesBefore.Clear();
        foreach(var citizen in state.Citizens.Where(c=>c.Alive&&c.InsideMap&&c.VehicleId==null&&c.Moving))_peopleBefore[citizen.Id]=new(citizen.Position,citizen.Path,citizen.PathIndex,citizen.Path.Skip(citizen.PathIndex).ToList());
        var roads=state.Roads.ToDictionary(r=>r.Id);
        foreach(var vehicle in state.Vehicles.Where(v=>v.Status==VehicleStatus.Travelling)){
            var remaining=new List<Point>();
            for(var i=vehicle.RouteIndex;i<vehicle.Route.Count;i++){
                if(!roads.TryGetValue(vehicle.Route[i],out var node)){remaining.Clear();break;}
                Point previous;if(i>0){if(!roads.TryGetValue(vehicle.Route[i-1],out var preceding)){remaining.Clear();break;}previous=preceding.Position;}else previous=vehicle.Position;
                var delta=i==0&&vehicle.Route.Count>1&&roads.TryGetValue(vehicle.Route[1],out var next)?next.Position-node.Position:node.Position-previous;var length=delta.Distance(new(0,0));
                remaining.Add(length>.01?node.Position+new Point(delta.Z/length*1.65,-delta.X/length*1.65):node.Position);
            }
            _vehiclesBefore[vehicle.Id]=new(vehicle.Position,vehicle.Route,vehicle.RouteIndex,remaining);
        }
    }
    void BuildMotionTraces(CityState state)
    {
        if(_motionBuiltAt==state.ElapsedSeconds)return;_motionBuiltAt=state.ElapsedSeconds;_peopleMotion.Clear();_vehicleMotion.Clear();
        foreach(var citizen in state.Citizens){
            if(!_peopleBefore.TryGetValue(citizen.Id,out var before)||before.Position.Distance(citizen.Position)<.00001)continue;
            var count=ReferenceEquals(before.Path,citizen.Path)?citizen.PathIndex-before.Index:-1;
            // Arrival clears its old path index. Only the old endpoint proves that path was consumed.
            if(before.Remaining.Count>0&&before.Remaining[^1].Distance(citizen.Position)<.00001)count=before.Remaining.Count;
            if(count<0)continue;var points=new List<Point>{before.Position};points.AddRange(before.Remaining.Take(count));points.Add(citizen.Position);_peopleMotion[citizen.Id]=new(points);
        }
        foreach(var vehicle in state.Vehicles){
            if(!_vehiclesBefore.TryGetValue(vehicle.Id,out var before)||before.Position.Distance(vehicle.Position)<.00001||!ReferenceEquals(before.Path,vehicle.Route))continue;
            var count=vehicle.RouteIndex-before.Index;if(count<0||count>before.Remaining.Count)continue;
            var points=new List<Point>{before.Position};points.AddRange(before.Remaining.Take(count));points.Add(vehicle.Position);_vehicleMotion[vehicle.Id]=new(points);
        }
    }
    public void Sync(CityState state,double? interpolation=null,double environmentSeconds=0,CitySimulation? simulation=null)
    {
        if(_lakeMaterial!=null&&environmentSeconds>0){_lakePresentationTime=(_lakePresentationTime+environmentSeconds)%3600;_lakeMaterial.SetShaderParameter("host_time_s",_lakePresentationTime);}
        _state=state;if(_static==null)return;
        var renderTime=interpolation is {} blend&&ReferenceEquals(_motionState,state)&&state.ElapsedSeconds>_motionBeforeAt?_motionBeforeAt+(state.ElapsedSeconds-_motionBeforeAt)*Math.Clamp(blend,0,1):state.ElapsedSeconds;
        var visualSeconds=Math.Max(0,renderTime-_lastVisualTime);_lastVisualTime=renderTime;
        // Exact presentation (pause, load, commands and captures) must not resume an older motion sample.
        if(interpolation==null||interpolation>=1)_motionState=null;
        var interpolate=interpolation is {} fraction&&fraction<1&&ReferenceEquals(_motionState,state)&&state.ElapsedSeconds>_motionBeforeAt;if(interpolate)BuildMotionTraces(state);
        foreach(var b in state.Buildings){if(b.Removed){Remove(_buildings,b.Id);Remove(_causes,b.Id);ForgetVisual(b.Id);continue;}
            if(!_buildings.TryGetValue(b.Id,out var node)||_finished.GetValueOrDefault(b.Id)!=b.Completed){Remove(_buildings,b.Id);node=new Node3D();AddChild(node);node.Position=P(b.Position);node.Rotation=new(0,(float)b.Rotation,0);ForgetVisual(b.Id);if(b.Completed){var visual=BuildingVisual(b);node.AddChild(visual);BindVisual(b.Id,visual);CreateActivityBadge(b,node,visual);CreateProblemAlerts(b,node,visual);}else{Box(node,new((float)b.Definition.Width,.22f,(float)b.Definition.Depth),new(0,.15f,0),"c7ae78");for(var x=-1;x<=1;x+=2)for(var z=-1;z<=1;z+=2)Box(node,new(.3f,3,.3f),new(x*(float)b.Definition.Width/2,1.5f,z*(float)b.Definition.Depth/2),"d7a355");} _buildings[b.Id]=node;_finished[b.Id]=b.Completed;
                BuildingBankBase(node,b);_buildingPickMeshes[b.Id]=MeshesIn(node).ToList();var cause=new Label3D{FontSize=32,PixelSize=.06f,Width=260,AutowrapMode=TextServer.AutowrapMode.WordSmart,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=false,Modulate=new Color("fff5d4"),Position=P(b.Position,b.Completed?10:4),OutlineSize=4};AddChild(cause);Remove(_causes,b.Id);_causes[b.Id]=cause;}
            if(!b.Completed){var project=state.Projects.First(p=>p.Id==b.ProjectId);var stopped=project.Cause!="Construção em andamento"&&!string.IsNullOrWhiteSpace(project.Cause);var text=((int)(project.Progress*100))+"%"+(stopped?"\n"+BriefWorkCause(project.Cause):"");
                var roadless=stopped&&(project.Cause.StartsWith("Sem rua")||project.Cause.StartsWith("Sem acesso"));if(roadless)text=((int)(project.Progress*100))+"%\nSEM RUA: obra parada";
                var label=_causes[b.Id];if(label.Text!=text)label.Text=text;if((label.FontSize==44)!=roadless){label.FontSize=roadless?44:32;label.OutlineSize=roadless?12:4;label.Width=roadless?440:260;label.NoDepthTest=roadless;label.OutlineModulate=new Color(roadless?"5a2a00":"000000");}label.Modulate=new Color(roadless?"ffb02e":stopped?"ffe0a3":"fff5d4");}else{var company=state.Companies.FirstOrDefault(c=>c.Id==b.CompanyId);if(_companySigns.TryGetValue(b.Id,out var label)&&label.Text!=(company?.Name??""))label.Text=company?.Name??"";}
            if(b.Completed){if(_rotors.TryGetValue(b.Id,out var rotor)&&b.OperationalCapacity>.001)rotor.RotateZ((float)(visualSeconds*state.WindFactor*.08));if(_waterStates.TryGetValue(b.Id,out var waterState))waterState.Visible=b.OperationalCapacity>.001&&(b.Kind!=BuildingKind.SewageTreatment||state.SewageDemand>.001);if(_stockStates.TryGetValue(b.Id,out var stockState)){stockState.Visible=_stockMarkers[b.Id].Any(marker=>b.Quantity(marker.Resource)>.001);foreach(var marker in _stockMarkers[b.Id])marker.Node.Visible=b.Quantity(marker.Resource)>.001;}}
        }
        if(_roadRevision!=state.RoadRevision){foreach(var r in _roads.Values){RemoveChild(r);r.QueueFree();}_roads.Clear();_lamps.Clear();_lampGlass.Clear();_roadFootprints.Clear();_roadCarriageways.Clear();_crosswalkStripes.Clear();_roadVisualNodes.Clear();foreach(var road in state.Roads)_roadVisualNodes[road.Id]=road;foreach(var road in state.Roads){var n=RoadVisual(road,state);AddChild(n);_roads[road.Id]=n;}DrawRoadSurface(state);DrawCrosswalkPaint();_roadRevision=state.RoadRevision;}
        SyncRoadWorks(state);
        var night=state.Now.Hour<6||state.Now.Hour>=18;
        foreach(var road in state.Roads){if(_lamps.TryGetValue(road.Id,out var light)){light.Visible=night&&road.ElectricityRatio>.01;light.LightEnergy=(float)road.ElectricityRatio*1.3f;if(_lampGlass.TryGetValue(road.Id,out var glass))glass.MaterialOverride=light.Visible?_lampOn:_lampOff;}if(_roads.TryGetValue(road.Id,out var r)&&r.GetNodeOrNull<MeshInstance3D>("Signal") is {} signal){signal.Visible=road.Control==JunctionControl.Signal&&road.Neighbours.Count>=3;var green=state.ElapsedSeconds%60<30;signal.MaterialOverride=Material(green?"6ca56b":"ba594d");}}
        foreach(var c in state.Citizens){if(!c.Alive||!c.InsideMap||c.VehicleId!=null){Remove(_people,c.Id);continue;}if(!_people.TryGetValue(c.Id,out var n)){n=Scene("agent.sim_pedestrian.a")??new Node3D();n.Name="Citizen"+c.Id;if(n.GetChildCount()==0)Box(n,new(.6f,1.7f,.5f),new(0,.85f,0),"d7a359");AddChild(n);_people[c.Id]=n;}
            var pose=interpolate&&_peopleMotion.TryGetValue(c.Id,out var motion)?motion.At(interpolation!.Value):(c.Position,(double?)null);n.Position=P(pose.Item1,RoadSurfaceAt(pose.Item1));var interior=pose.Item2!=null?null:InteriorBuildingId(state,c,pose.Item1);n.Visible=interior==null;
            var anim=n.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");if(anim!=null){var animation=c.Moving||pose.Item2!=null?"walk":"idle";if(anim.HasAnimation(animation)&&anim.CurrentAnimation!=animation)anim.Play(animation);}if(pose.Item2 is {} heading)n.Rotation=new(0,(float)heading,0);else if(c.Moving){var target=c.Path[c.PathIndex];n.Rotation=new(0,(float)Math.Atan2(c.Position.X-target.X,c.Position.Z-target.Z),0);}}
        foreach(var v in state.Vehicles){if(v.Status==VehicleStatus.Outside){Remove(_trucks,v.Id);continue;}if(!_trucks.TryGetValue(v.Id,out var n)){n=Scene(v.PassengerCar?"vehicle.car_passenger.a":"vehicle.truck_cargo.a")??new Node3D();if(n.GetChildCount()==0){if(v.PassengerCar)Box(n,new(1.8f,1.4f,4.2f),new(0,.75f,0),"5a7fa8");else Box(n,new(2.3f,2.2f,4.5f),new(0,1.4f,0),"b8704b");}else if(v.PassengerCar)PaintCar(n,v.Id);AddChild(n);_trucks[v.Id]=n;}var pose=interpolate&&_vehicleMotion.TryGetValue(v.Id,out var motion)?motion.At(interpolation!.Value):(v.Position,(double?)null);n.Position=P(pose.Item1,RoadSurfaceAt(pose.Item1));n.Rotation=new(0,(float)(pose.Item2??v.Heading+Math.PI),0);}
        SyncBuildingActivity(state,interpolate,interpolation);
        if(simulation!=null)UpdateProblemAlerts(state,simulation,state.ElapsedSeconds);
        var geometry=state.Buildings.Aggregate(state.RoadRevision,(hash,b)=>HashCode.Combine(hash,b.Id,b.Removed));if(_vegetationGeometry!=geometry){_vegetationGeometry=geometry;foreach(var tree in _trees){var p=tree.Position;tree.Node.Visible=!state.Buildings.Any(b=>!b.Removed&&Math.Abs(p.X-b.Position.X)<Math.Max(b.Definition.Width,b.Definition.Depth)/2+4&&Math.Abs(p.Z-b.Position.Z)<Math.Max(b.Definition.Width,b.Definition.Depth)/2+4)&&!state.Roads.Any(r=>r.Position.Distance(p)<state.Tuning.RoadWidth/2+3);}}
        LayoutWorkLabels();
    }
    // This recognizes an interior only from the agent's actual arrival state;
    // standing near somebody else's doorway never makes a pedestrian disappear.
    public static int? InteriorBuildingId(CityState state,Citizen citizen,Point? position=null)
    {
        if(!citizen.Alive||!citizen.InsideMap||citizen.VehicleId!=null||citizen.Moving||citizen.ConstructionProjectId!=null)return null;
        int? id=citizen.Activity=="Em casa"?state.Families.FirstOrDefault(f=>f.Id==citizen.FamilyId)?.HomeId:
            citizen.Activity=="No trabalho"?citizen.WorkplaceId:
            citizen.Purpose==JourneyPurpose.Visit&&citizen.Activity.StartsWith("Visitando família",StringComparison.Ordinal)?citizen.DestinationId:null;
        var building=id is {} target?state.Buildings.FirstOrDefault(b=>b.Id==target&&b.Completed&&!b.Removed):null;
        return building!=null&&building.Door.Distance(position??citizen.Position)<1?building.Id:null;
    }
    public bool IsCitizenVisible(int id)=>_people.TryGetValue(id,out var node)&&node.Visible;
    public Point? VisibleCitizenPosition(int id)=>_people.TryGetValue(id,out var node)&&node.Visible?new Point(node.GlobalPosition.X,node.GlobalPosition.Z):null;
    // Measured once when the building visual is created, including nested mesh
    // transforms and the imported model's actual scale. The marker stays above
    // its roof without drawing through other buildings or changing the lot.
    static float ModelTop(Node3D building,Node3D model)
    {
        var inverse=building.GlobalTransform.AffineInverse();var top=0f;
        foreach(var mesh in MeshesIn(model)){if(mesh.Mesh==null)continue;var bounds=mesh.Mesh.GetAabb();var transform=inverse*mesh.GlobalTransform;
            for(var corner=0;corner<8;corner++)top=Math.Max(top,(transform*bounds.GetEndpoint(corner)).Y);}
        return top;
    }
    void SyncBuildingActivity(CityState state,bool interpolate,double? interpolation)
    {
        _cargoCounts.Clear();
        foreach(var vehicle in state.Vehicles.Where(v=>v.Status==VehicleStatus.Unloading&&v.ShipmentId!=null&&!v.PassengerCar&&v.RefuellingAt==null)){
            var shipment=state.Shipments.FirstOrDefault(s=>s.Id==vehicle.ShipmentId&&!s.Complete&&!s.Cancelled&&s.VehicleId==vehicle.Id);if(shipment==null)continue;
            if(!shipment.Cargo.Any(item=>item.Value>.001)||vehicle.DockPosition is not {} dock||vehicle.Position.Distance(dock)>.1)continue;
            if(!vehicle.External&&!state.Citizens.Any(c=>c.Id==vehicle.DriverId&&c.Alive&&c.Resident&&c.InsideMap&&c.VehicleId==vehicle.Id))continue;
            // Do not display unloading before the interpolated truck reaches its bay.
            if(interpolate&&_vehicleMotion.TryGetValue(vehicle.Id,out var motion)&&motion.At(interpolation!.Value).Position.Distance(vehicle.Position)>.01)continue;
            var id=!shipment.PickedUp?shipment.SourceBuildingId:shipment.DestinationBuildingId??(shipment.ProjectId is {} projectId?state.Projects.FirstOrDefault(p=>p.Id==projectId)?.BuildingId:null);
            if(id is {} buildingId)_cargoCounts[buildingId]=_cargoCounts.GetValueOrDefault(buildingId)+1;
        }
        PresentActivityBadges();
    }
    float RoadSurfaceAt(Point point)
    {
        var at=new Vector2((float)point.X,(float)point.Z);if(_roadCarriageways.Any(rect=>rect.HasPoint(at)))return RoadVisualLift+RoadAsphaltTop;
        return _roadFootprints.Any(rect=>rect.HasPoint(at))?RoadVisualLift+RoadSidewalkTop:0;
    }
    static IEnumerable<MeshInstance3D> MeshesIn(Node node)
    {
        if(node is MeshInstance3D mesh)yield return mesh;
        foreach(var child in node.GetChildren())foreach(var descendant in MeshesIn(child))yield return descendant;
    }
    public int? PickBuilding(Vector3 rayOrigin,Vector3 rayDirection)
    {
        int? selected=null;var nearest=float.PositiveInfinity;
        foreach(var entry in _buildingPickMeshes)foreach(var mesh in entry.Value){
            if(mesh.Mesh==null||!mesh.IsVisibleInTree())continue;var inverse=mesh.GlobalTransform.AffineInverse();var origin=inverse*rayOrigin;var direction=inverse.Basis*rayDirection;
            var bounds=mesh.Mesh.GetAabb();var low=0f;var high=nearest;
            for(var axis=0;axis<3;axis++){
                if(Math.Abs(direction[axis])<.000001f){if(origin[axis]<bounds.Position[axis]||origin[axis]>bounds.End[axis]){high=-1;break;}continue;}
                var a=(bounds.Position[axis]-origin[axis])/direction[axis];var b=(bounds.End[axis]-origin[axis])/direction[axis];if(a>b)(a,b)=(b,a);low=Math.Max(low,a);high=Math.Min(high,b);if(high<low)break;
            }
            if(high>=low&&low<nearest){nearest=low;selected=entry.Key;}
        }
        return selected;
    }
    void LayoutWorkLabels()
    {
        var camera=GetViewport().GetCamera3D();var labels=_causes.Where(pair=>!_finished.GetValueOrDefault(pair.Key)).Select(pair=>pair.Value).Concat(_roadWorkLabels.Values).ToList();if(camera==null){foreach(var label in labels)label.Visible=!string.IsNullOrEmpty(label.Text);return;}
        var previewLabels=_previewPool.SelectMany(marker=>new[]{marker.Validity,marker.Warning}).Where(label=>label.IsVisibleInTree()&&!string.IsNullOrEmpty(label.Text)).ToList();
        var viewport=GetViewport().GetVisibleRect();var key=HashCode.Combine(camera.GlobalTransform,camera.Size,viewport);foreach(var label in labels.Concat(previewLabels))key=HashCode.Combine(key,label.GetInstanceId(),label.GlobalPosition,label.Text,label.FontSize,label.PixelSize,label.GetAabb());
        foreach(var rectangle in _workLabelOverlayRects)key=HashCode.Combine(key,rectangle);
        if(_labelLayoutKey==key)return;_labelLayoutKey=key;
        Rect2 ProjectLabel(Label3D label){
            var bounds=label.GetAabb();var centre=label.GlobalPosition+camera.GlobalBasis.X*bounds.GetCenter().X+camera.GlobalBasis.Y*bounds.GetCenter().Y;
            var a=camera.UnprojectPosition(centre-camera.GlobalBasis.X*bounds.Size.X/2-camera.GlobalBasis.Y*bounds.Size.Y/2);var b=camera.UnprojectPosition(centre+camera.GlobalBasis.X*bounds.Size.X/2+camera.GlobalBasis.Y*bounds.Size.Y/2);
            return new Rect2(new Vector2(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y)),new Vector2(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y))).Grow(4);
        }
        var occupied=_workLabelOverlayRects.Concat(previewLabels.Where(label=>!camera.IsPositionBehind(label.GlobalPosition)).Select(ProjectLabel)).ToList();
        foreach(var label in labels.OrderBy(label=>camera.GlobalPosition.DistanceSquaredTo(label.GlobalPosition))){
            if(string.IsNullOrEmpty(label.Text)||camera.IsPositionBehind(label.GlobalPosition)){label.Visible=false;continue;}
            var rect=ProjectLabel(label);
            label.Visible=viewport.Encloses(rect)&&!occupied.Any(other=>other.Intersects(rect));if(label.Visible)occupied.Add(rect);
        }
    }
    void SyncRoadWorks(CityState state)
    {
        var works=state.Projects.Where(p=>!p.IsMaintenance&&p.RoadPoints.Count>0&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress).ToList();
        var geometry=works.Aggregate(state.RoadRevision,(hash,p)=>HashCode.Combine(hash,p.Id,p.RoadPoints.Count,p.BuiltRoadNodes.Count,p.Status));
        if(_roadWorkGeometry!=geometry){foreach(var id in _roadWorks.Keys.ToList())Remove(_roadWorks,id);_roadWorkLabels.Clear();
            var layout=PlannedRoadLayout();
            foreach(var work in works){var root=new Node3D{Name="RoadWork"+work.Id};AddChild(root);_roadWorks[work.Id]=root;
                var remaining=work.RoadPoints.Where(at=>state.Roads.All(r=>r.Position.Distance(at)>.05)).ToArray();
                DrawRoadDraft(root,remaining,layout,"c7ae78");
                var label=new Label3D{FontSize=32,PixelSize=.06f,Width=360,AutowrapMode=TextServer.AutowrapMode.WordSmart,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,Modulate=new Color("fff5d4"),Position=P(work.Position,3),OutlineSize=4};root.AddChild(label);_roadWorkLabels[work.Id]=label;
            }_roadWorkGeometry=geometry;
        }
        foreach(var work in works){
            var cause=work.Cause.StartsWith("Sem ligação pronta ao exterior",StringComparison.Ordinal)?"Aguarda ligação pronta":work.Cause switch{"Trecho válido; ligação exterior disponível"=>"Ligação exterior disponível","Trecho construído; aguardando ligação ao exterior"=>"Aguarda ligação real","Construção em andamento"=>"Em construção",_=>work.Cause};
            var text=$"Obra de rua #{work.Id}\n{(int)(work.Progress*100)}% • {cause}";if(_roadWorkLabels[work.Id].Text!=text)_roadWorkLabels[work.Id].Text=text;
        }
    }
    void ForgetVisual(int id){_companySigns.Remove(id);_rotors.Remove(id);_waterStates.Remove(id);_stockStates.Remove(id);_stockMarkers.Remove(id);_buildingPickMeshes.Remove(id);_activityBadges.Remove(id);_problemAlerts.Remove(id);_problemPersistence.Remove(id);}
    static string BriefWorkCause(string cause)=>cause switch{
        "Aguardando equipe física no canteiro"=>"Aguardando equipe",
        "Sem orçamento para pagar equipe externa"=>"Sem orçamento para equipe",
        "Equipe externa aguardando pagamento"=>"Equipe sem pagamento",
        "Aguardando entrega física"=>"Aguardando entrega",
        _=>cause.Length>55?cause[..52]+"…":cause};
    void BindVisual(int id,Node3D instance)
    {
        if(instance.FindChild("SignText",true,false) is Label3D label)_companySigns[id]=label;
        if(instance.GetNodeOrNull<Node3D>("Nacelle/Rotor") is {} rotor)_rotors[id]=rotor;
        if(instance.GetNodeOrNull<Node3D>("State_Water") is {} water)_waterStates[id]=water;
        if(instance.GetNodeOrNull<Node3D>("State_Stock") is {} stock){_stockStates[id]=stock;var markers=new List<(ResourceKind,Node3D)>();foreach(var item in new[]{(ResourceKind.Aggregate,"SandGravel"),(ResourceKind.Concrete,"Concrete"),(ResourceKind.Steel,"Steel"),(ResourceKind.Wood,"Wood"),(ResourceKind.Asphalt,"Asphalt")})if(stock.GetNodeOrNull<Node3D>(item.Item2) is {} marker)markers.Add((item.Item1,marker));_stockMarkers[id]=markers;}
    }
    Node3D RoadVisual(RoadNode road,CityState state)
    {
        var n=new Node3D{Position=P(road.Position,RoadVisualLift)};var half=(float)state.Tuning.RoadWidth/2;var step=(float)state.Tuning.RoadStep;var asphalt=half*2*(6.5f/10.5f);
        var horizontal=road.Highway||road.Neighbours.Any(id=>Math.Abs(_roadVisualNodes[id].Position.X-road.Position.X)>.1);var vertical=road.Neighbours.Any(id=>Math.Abs(_roadVisualNodes[id].Position.Z-road.Position.Z)>.1);
        if(!horizontal&&!vertical){var origin=state.Projects.FirstOrDefault(p=>p.BuiltRoadNodes.Contains(road.Id));if(origin!=null){var index=origin.RoadPoints.FindIndex(p=>p.Distance(road.Position)<.05);var adjacent=new List<Point>();if(index>0)adjacent.Add(origin.RoadPoints[index-1]);if(index>=0&&index+1<origin.RoadPoints.Count)adjacent.Add(origin.RoadPoints[index+1]);horizontal=adjacent.Any(p=>Math.Abs(p.X-road.Position.X)>.1);vertical=adjacent.Any(p=>Math.Abs(p.Z-road.Position.Z)>.1);}if(!horizontal&&!vertical)horizontal=true;}
        if(horizontal&&vertical){
            SurfaceRect(road.Position,half*2,half*2,false);SurfaceRect(road.Position,asphalt,asphalt,true);
            var corner=(half*2-asphalt)/2;
            foreach(var alongX in new[]{true,false})foreach(var sign in new[]{-1,1}){
                var opened=road.Neighbours.Any(id=>{var delta=_roadVisualNodes[id].Position-road.Position;return alongX?Math.Abs(delta.X)>.1&&Math.Sign(delta.X)==sign:Math.Abs(delta.Z)>.1&&Math.Sign(delta.Z)==sign;});
                var at=alongX?new Vector3(sign*(asphalt/2+corner/2),0,0):new Vector3(0,0,sign*(asphalt/2+corner/2));var size=alongX?new Vector3(corner,0,asphalt):new Vector3(asphalt,0,corner);
                if(opened)SurfaceRect(road.Position+new Point(at.X,at.Z),size.X,size.Z,true);
            }
        }
        if(horizontal&&!vertical){SurfaceRect(road.Position,step,half*2,false);SurfaceRect(road.Position,step,asphalt,true);}
        if(vertical&&!horizontal){SurfaceRect(road.Position,half*2,step,false);SurfaceRect(road.Position,asphalt,step,true);}
        if(!road.Highway&&road.Id%8==0){var lamp=Scene("road.urban_1x1.a.lamp")??new Node3D();lamp.Position=horizontal&&vertical?new(half-.7f,RoadSidewalkTop,half-.7f):horizontal?new(0,RoadSidewalkTop,half-.7f):new(half-.7f,RoadSidewalkTop,0);lamp.Rotation=new(0,horizontal?0:Mathf.Pi/2,0);n.AddChild(lamp);if(lamp.GetChildCount()==0)Box(lamp,new(.22f,6,.22f),new(0,3,0),"45535a");var source=lamp.GetNodeOrNull<Marker3D>("Candidate_LightSource");var light=new OmniLight3D{Position=source?.Position??new(0,5,0),LightColor=new Color("ffd793"),OmniRange=17,ShadowEnabled=false};lamp.AddChild(light);_lamps[road.Id]=light;if(lamp.GetNodeOrNull<MeshInstance3D>("LampGlass") is {} glass){_lampOn??=GD.Load<Material>("res://Assets/V1/Materials/road_lamp_glass_on.tres");_lampOff??=GD.Load<Material>("res://Assets/V1/Materials/road_lamp_glass_off.tres");_lampGlass[road.Id]=glass;}}
        if(road.Crosswalk&&!road.Highway){
            // Zebra bars run along traffic and repeat across the carriageway.
            // Preserve the 3.9 m crossing band used by pedestrian routing.
            var stripes=Math.Max(1,(int)Math.Round(asphalt/.8f));var pitch=asphalt/stripes;
            foreach(var crossing in CitySimulation.CrosswalkLocations(road,state))for(var i=0;i<stripes;i++){
                var across=(i-(stripes-1)/2f)*pitch;
                CrosswalkStroke(crossing.Centre,crossing.HorizontalRoad?new(3.9f,0,pitch-.1f):new(pitch-.1f,0,3.9f),crossing.HorizontalRoad?new(0,0,across):new(across,0,0));
            }
            n.AddChild(new MeshInstance3D{Name="Signal",Mesh=new BoxMesh{Size=new(.3f,.7f,.3f)},Position=new(-4,3,4),MaterialOverride=Material("6ca56b")});
        }
        return n;
    }
    void CrosswalkStroke(Point road,Vector3 size,Vector3 local)
    {
        var x=(float)road.X+local.X;var z=(float)road.Z+local.Z;_crosswalkStripes.Add(new(x-size.X/2,x+size.X/2,z-size.Z/2,z+size.Z/2));
    }
    void DrawCrosswalkPaint()
    {
        if(_crosswalkPaint!=null){RemoveChild(_crosswalkPaint);_crosswalkPaint.QueueFree();}_crosswalkPaint=new Node3D{Name="CrosswalkPaint"};AddChild(_crosswalkPaint);
        var clipped=new HashSet<PreviewRectangle>();foreach(var stripe in _crosswalkStripes)foreach(var surface in _roadCarriageways){var x=Math.Max(stripe.MinX,surface.Position.X);var z=Math.Max(stripe.MinZ,surface.Position.Y);var endX=Math.Min(stripe.MaxX,surface.End.X);var endZ=Math.Min(stripe.MaxZ,surface.End.Y);if(endX-x>.001f&&endZ-z>.001f)clipped.Add(new(x,endX,z,endZ));}
        if(clipped.Count==0)return;var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);mesh.SurfaceSetNormal(Vector3.Up);var y=RoadVisualLift+RoadAsphaltTop+.008f;
        foreach(var rect in clipped){var a=new Vector3(rect.MinX,y,rect.MinZ);var b=new Vector3(rect.MaxX,y,rect.MinZ);var c=new Vector3(rect.MaxX,y,rect.MaxZ);var d=new Vector3(rect.MinX,y,rect.MaxZ);foreach(var point in new[]{a,b,c,a,c,d})mesh.SurfaceAddVertex(point);}
        mesh.SurfaceEnd();_crosswalkPaint.AddChild(new MeshInstance3D{Mesh=mesh,MaterialOverride=Material("eee9d4"),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
    }
    void SurfaceRect(Point at,float width,float depth,bool carriageway)
    {
        var rect=new Rect2((float)at.X-width/2,(float)at.Z-depth/2,width,depth);(carriageway?_roadCarriageways:_roadFootprints).Add(rect);
    }
    void Remove<T>(Dictionary<int,T> table,int id)where T:Node3D{if(table.Remove(id,out var node)){RemoveChild(node);node.QueueFree();}}
    static void PreviewValidity(Node3D parent,Vector3 at,bool valid){parent.AddChild(new Label3D{Text=valid?"✓":"×",Position=at,FontSize=48,PixelSize=.04f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,OutlineSize=4,Modulate=new Color("fff5d4")});}
    public void ClearPreview(){if(_preview==null||_previewKey==""&&_previewAccess==null)return;foreach(var c in _preview.GetChildren()){_preview.RemoveChild(c);c.QueueFree();}_previewKey="";_previewBuildingKind=null;_previewPool.Clear();_previewAccess=null;_previewAccessKey=null;}
    public void PreviewAccessLinks(IEnumerable<(Point Door,Point Road,bool Connected)> links)
    {
        var entries=links.ToList();var hash=new HashCode();foreach(var link in entries){hash.Add(link.Door);hash.Add(link.Road);hash.Add(link.Connected);}var key=hash.ToHashCode();if(_previewAccessKey==key)return;
        if(_previewAccess!=null){_preview.RemoveChild(_previewAccess);_previewAccess.QueueFree();}_previewAccessKey=key;_previewAccess=new Node3D{Name="PreviewAccessLinks"};_preview.AddChild(_previewAccess);
        foreach(var link in entries){var delta=link.Road-link.Door;var length=(float)delta.Distance(new(0,0));var material=PreviewMaterial(link.Connected?"93cf97":"e6bc64");if(length>.01f){var line=PreviewBox(_previewAccess,new(.45f,.08f,length),P((link.Door+link.Road)*.5,.42f),material);line.Rotation=new(0,(float)Math.Atan2(delta.X,delta.Z),0);}
            foreach(var point in new[]{link.Door,link.Road})_previewAccess.AddChild(new MeshInstance3D{Mesh=new TorusMesh{InnerRadius=.65f,OuterRadius=1f,Rings=16,RingSegments=8},Position=P(point,.44f),MaterialOverride=material,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});}
    }
    static void MakeTransparent(Node node)
    {
        if(node is MeshInstance3D mesh&&mesh.Mesh!=null){
            mesh.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            if(mesh.MaterialOverride is BaseMaterial3D overridden){var copy=(BaseMaterial3D)overridden.Duplicate();copy.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;copy.AlbedoColor=new Color(copy.AlbedoColor,.38f);mesh.MaterialOverride=copy;}
            else for(var i=0;i<mesh.Mesh.GetSurfaceCount();i++){var source=mesh.GetSurfaceOverrideMaterial(i)??mesh.Mesh.SurfaceGetMaterial(i);if(source is BaseMaterial3D original){var copy=(BaseMaterial3D)original.Duplicate();copy.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;copy.AlbedoColor=new Color(copy.AlbedoColor,.38f);mesh.SetSurfaceOverrideMaterial(i,copy);}}
        }
        foreach(var child in node.GetChildren())MakeTransparent(child);
    }
    static void PreviewBoundary(BuildingPreviewMarker marker,Vector3 size,Vector3 position)
    {
        var mesh=new MeshInstance3D{Mesh=new BoxMesh{Size=size},Position=position,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};marker.Node.AddChild(mesh);marker.Footprint.Add(mesh);
    }
    BuildingPreviewMarker NewBuildingPreview(BuildingKind kind,int index)
    {
        var definition=Catalog.Definitions[kind];var width=(float)definition.Width;var depth=(float)definition.Depth;
        var marker=new BuildingPreviewMarker{Node=new Node3D{Name=index==0?"BuildingPreview":"BuildingPreview"+index}};_preview.AddChild(marker.Node);var node=marker.Node;
        var model=BuildingVisual(kind);model.Name="TransparentModel";MakeTransparent(model);node.AddChild(model);
        PreviewBoundary(marker,new(width,.002f,.4f),new(0,0,-depth/2+.2f));PreviewBoundary(marker,new(width,.002f,.4f),new(0,0,depth/2-.2f));
        PreviewBoundary(marker,new(.4f,.002f,depth),new(-width/2+.2f,0,0));PreviewBoundary(marker,new(.4f,.002f,depth),new(width/2-.2f,0,0));
        PreviewBox(node,new(3,.002f,.6f),new(0,PlacementPlane-.021f,-depth/2),PreviewMaterial("fff1a3"));
        PreviewBox(node,new(.7f,.002f,2.4f),new(0,PlacementPlane-.021f,-depth/2-1.6f),PreviewMaterial("fff1a3"));
        var arrow=new ImmediateMesh();arrow.SurfaceBegin(Mesh.PrimitiveType.Triangles);arrow.SurfaceSetNormal(Vector3.Up);arrow.SurfaceAddVertex(new(-1.5f,PlacementPlane-.02f,-depth/2-2));arrow.SurfaceAddVertex(new(0,PlacementPlane-.02f,-depth/2-3.8f));arrow.SurfaceAddVertex(new(1.5f,PlacementPlane-.02f,-depth/2-2));arrow.SurfaceEnd();
        node.AddChild(new MeshInstance3D{Name="EntranceDirection",Mesh=arrow,MaterialOverride=PreviewMaterial("fff1a3"),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
        marker.Validity=new Label3D{Position=new(0,1.5f,-depth/2),FontSize=48,PixelSize=.065f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,RenderPriority=11,OutlineSize=6};node.AddChild(marker.Validity);
        // Válido porém sem rua urbana: aviso inconfundível de obra parada (apenas apresentação).
        marker.Warning=new Label3D{Name="NoRoadWarning",Text="Sem ligação pronta\nobra aguardará acesso",Position=new(0,9,0),FontSize=40,PixelSize=.06f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,RenderPriority=12,OutlineSize=12,Modulate=new Color("ffc247"),OutlineModulate=new Color("4a2800"),Visible=false};node.AddChild(marker.Warning);
        return marker;
    }
    public void PreviewBuilding(BuildingKind kind,Point at,double rotation,bool valid,bool? connected=null){_singlePreview[0]=(at,valid,connected);PreviewBuildings(kind,_singlePreview,rotation);}
    // Desenha N prévias do mesmo tipo reaproveitando os nós; as que sobram ficam ocultas.
    public void PreviewBuildings(BuildingKind kind,IReadOnlyList<(Point At,bool Valid,bool? Connected)> items,double rotation)
    {
        var hash=new HashCode();hash.Add(kind);hash.Add(rotation);foreach(var item in items){hash.Add(item.At);hash.Add(item.Valid);hash.Add(item.Connected);}
        var key="b"+hash.ToHashCode();if(_previewKey==key)return;
        if(_previewPool.Count==0||_previewBuildingKind!=kind){ClearPreview();_previewBuildingKind=kind;}
        while(_previewPool.Count<items.Count)_previewPool.Add(NewBuildingPreview(kind,_previewPool.Count));
        _previewKey=key;
        for(var i=0;i<_previewPool.Count;i++){
            var marker=_previewPool[i];if(i>=items.Count){marker.Node.Visible=false;continue;}
            var (at,valid,connected)=items[i];marker.Node.Visible=true;marker.Node.Position=P(at,.02f);marker.Node.Rotation=new(0,(float)rotation,0);
            foreach(var edge in marker.Footprint)edge.Position=new(edge.Position.X,VisibleTerrainHeight(at)-.021f,edge.Position.Z);
            var color=!valid?"df8577":connected==false?"e6bc64":"93cf97";
            if(marker.Color!=color){marker.Color=color;foreach(var edge in marker.Footprint)edge.MaterialOverride=PreviewMaterial(color);marker.Validity.Text=valid?"✓":"×";marker.Validity.Modulate=new Color(color);}
            marker.Warning.Visible=valid&&connected==false;
        }
    }
    readonly record struct PreviewRectangle(float MinX,float MaxX,float MinZ,float MaxZ);
    void PreviewRoadOutline(List<PreviewRectangle> rectangles,string color)
    {
        var edges=new Dictionary<(bool Vertical,float Line,bool Positive),List<(float Start,float End)>>();
        foreach(var rectangle in rectangles){
            AddEdge(true,rectangle.MinX,rectangle.MinZ,rectangle.MaxZ,false);AddEdge(true,rectangle.MaxX,rectangle.MinZ,rectangle.MaxZ,true);
            AddEdge(false,rectangle.MinZ,rectangle.MinX,rectangle.MaxX,false);AddEdge(false,rectangle.MaxZ,rectangle.MinX,rectangle.MaxX,true);
        }
        foreach(var entry in edges){var ordered=entry.Value.OrderBy(interval=>interval.Start).ToList();if(ordered.Count==0)continue;var start=ordered[0].Start;var end=ordered[0].End;
            for(var i=1;i<=ordered.Count;i++){if(i<ordered.Count&&ordered[i].Start<=end+.001f){end=Math.Max(end,ordered[i].End);continue;}
                var vertical=entry.Key.Vertical;var line=entry.Key.Line+(entry.Key.Positive?-.15f:.15f);PreviewBox(_preview,vertical?new(.3f,.002f,end-start):new(end-start,.002f,.3f),vertical?new(line,PlacementPlane-.001f,(start+end)/2):new((start+end)/2,PlacementPlane-.001f,line),PreviewMaterial(color));if(i<ordered.Count){start=ordered[i].Start;end=ordered[i].End;}}
        }
        void AddEdge(bool vertical,float line,float start,float end,bool positive)
        {
            var segments=new List<(float Start,float End)>{(start,end)};
            foreach(var other in rectangles){var near=vertical?other.MinX:other.MinZ;var far=vertical?other.MaxX:other.MaxZ;var outside=positive?near<line+.001f&&far>line+.001f:near<line-.001f&&far>line-.001f;if(!outside)continue;
                var coveredStart=vertical?other.MinZ:other.MinX;var coveredEnd=vertical?other.MaxZ:other.MaxX;var remainder=new List<(float Start,float End)>();
                foreach(var segment in segments){if(coveredEnd<=segment.Start+.001f||coveredStart>=segment.End-.001f){remainder.Add(segment);continue;}if(coveredStart>segment.Start+.001f)remainder.Add((segment.Start,coveredStart));if(coveredEnd<segment.End-.001f)remainder.Add((coveredEnd,segment.End));}segments=remainder;if(segments.Count==0)break;
            }
            if(!edges.TryGetValue((vertical,line,positive),out var list)){list=new();edges[(vertical,line,positive)]=list;}list.AddRange(segments);
        }
    }
    public void PreviewRoad(IEnumerable<Point> points,bool valid,double width,bool? connected=null,int? ignoredProject=null)
    {
        var p=points.ToList();var key=$"road:{p.FirstOrDefault()}:{p.LastOrDefault()}:{p.Count}:{valid}:{connected}:{width}:{p.ElementAtOrDefault(p.Count/2)}:{_state.RoadRevision}:{_roadWorkGeometry}:{ignoredProject}";if(_previewKey==key)return;ClearPreview();_previewKey=key;
        var color=!valid?"df8577":connected==false?"e6bc64":"93cf97";var rectangles=new List<PreviewRectangle>();
        var layout=PlannedRoadLayout(p,ignoredProject);
        foreach(var point in p){var r=RoadGeometry.Footprint(point,layout,width,_state.Tuning.RoadStep);rectangles.Add(new((float)(point.X-r.Width/2),(float)(point.X+r.Width/2),(float)(point.Z-r.Depth/2),(float)(point.Z+r.Depth/2)));}
        DrawRoadDraft(_preview,p,layout,color);
        PreviewRoadOutline(rectangles,color);
        for(var i=1;i<p.Count;i++){var delta=p[i]-p[i-1];PreviewBox(_preview,Math.Abs(delta.X)>.1?new((float)Math.Abs(delta.X),.002f,.12f):new(.12f,.002f,(float)Math.Abs(delta.Z)),P((p[i]+p[i-1])*.5,PlacementPlane-.001f),PreviewMaterial("fff1a3"));}
        if(p.Count>0){PreviewRoadPoint(p[0],"RoadStart",.8f,"fff1a3");PreviewRoadPoint(p[^1],"RoadEnd",1.05f,color);PreviewValidity(_preview,P(p[^1],1.5f),valid);}
        for(var i=1;i<p.Count-1;i++){var a=p[i]-p[i-1];var b=p[i+1]-p[i];if(Math.Abs(a.X*b.Z-a.Z*b.X)>.01)PreviewRoadPoint(p[i],"RoadCorner",.7f,"fff1a3");}
    }
}
