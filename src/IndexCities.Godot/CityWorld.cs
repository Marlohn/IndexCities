using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;
public partial class CityWorld:Node3D
{
    Node3D _static=null!,_preview=null!;Dictionary<int,Node3D> _buildings=new(),_roads=new(),_people=new(),_trucks=new();Dictionary<int,bool> _finished=new();Dictionary<int,OmniLight3D> _lamps=new();Dictionary<int,MeshInstance3D> _lampGlass=new();Material? _lampOn,_lampOff;Dictionary<int,Label3D> _causes=new();
    readonly Dictionary<int,Label3D> _companySigns=new();readonly Dictionary<int,Node3D> _rotors=new(),_waterStates=new(),_stockStates=new();readonly Dictionary<int,List<(ResourceKind Resource,Node3D Node)>> _stockMarkers=new();readonly List<(Node3D Node,Point Position)> _trees=new();int? _vegetationGeometry;double _lastVisualTime;
    int _roadRevision=-1;string _previewKey="";CityState _state=null!;
    readonly Dictionary<int,Node3D> _roadWorks=new();readonly Dictionary<int,Label3D> _roadWorkLabels=new();int? _roadWorkGeometry;
    Node3D? _previewBuildingNode;BuildingKind? _previewBuildingKind;bool? _previewBuildingValid;string? _previewBuildingColor;
    readonly List<MeshInstance3D> _previewFootprint=new();Label3D? _previewValidity;
    readonly Dictionary<int,List<MeshInstance3D>> _buildingPickMeshes=new();
    readonly List<Rect2> _roadFootprints=new(),_roadCarriageways=new();int? _labelLayoutKey;
    readonly List<PreviewRectangle> _crosswalkStripes=new();Node3D? _crosswalkPaint;
    const float RoadAsphaltTop=.03f,RoadSidewalkTop=.18f,RoadVisualLift=.01f;
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
        foreach(var child in GetChildren()){RemoveChild(child);child.QueueFree();}_buildings.Clear();_roads.Clear();_people.Clear();_trucks.Clear();_finished.Clear();_lamps.Clear();_lampGlass.Clear();_causes.Clear();_companySigns.Clear();_rotors.Clear();_waterStates.Clear();_stockStates.Clear();_stockMarkers.Clear();_trees.Clear();_vegetationGeometry=null;_lastVisualTime=state.ElapsedSeconds;_roadRevision=-1;_state=state;
        _roadWorks.Clear();_roadWorkLabels.Clear();_roadWorkGeometry=null;
        _buildingPickMeshes.Clear();_roadFootprints.Clear();_roadCarriageways.Clear();_crosswalkStripes.Clear();_crosswalkPaint=null;_labelLayoutKey=null;
        _motionState=null;_peopleBefore.Clear();_vehiclesBefore.Clear();_peopleMotion.Clear();_vehicleMotion.Clear();_motionBuiltAt=double.NaN;
        _static=new Node3D();AddChild(_static);_preview=new Node3D();AddChild(_preview);_previewKey="";_previewBuildingNode=null;_previewBuildingKind=null;_previewBuildingValid=null;_previewBuildingColor=null;_previewFootprint.Clear();_previewValidity=null;
        var half=(float)state.Tuning.MapHalfSize;Box(_static,new(half*2,1,half*2),new(0,-.56f,0),"7d9b52");Box(_static,new(half*2+6,4,half*2+6),new(0,-3,0),"917656");
        var water=new MeshInstance3D{Mesh=new CylinderMesh{TopRadius=(float)state.LakeRadius,BottomRadius=(float)state.LakeRadius+1,Height=.15f,RadialSegments=64},Position=P(state.LakeCentre,-.04f),MaterialOverride=Material("6bb3bd")};_static.AddChild(water);
        var random=new Random(state.Seed^123451);for(var i=0;i<145;i++){var p=new Point((random.NextDouble()*2-1)*(half-10),(random.NextDouble()*2-1)*(half-10));if(p.Distance(state.LakeCentre)<state.LakeRadius+9||Math.Abs(p.Z)<10)continue;var tree=Scene("vegetation.tree_broadleaf.a")??FallbackTree();tree.Position=P(p);tree.Scale=Vector3.One*(float)(.6+random.NextDouble()*.45);tree.RotationDegrees=new(0,random.Next(360),0);tree.SetMeta("tree_position",new Vector2((float)p.X,(float)p.Z));_static.AddChild(tree);_trees.Add((tree,p));}
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
    static void Box(Node3D parent,Vector3 size,Vector3 at,string hex,float alpha=1){parent.AddChild(new MeshInstance3D{Mesh=new BoxMesh{Size=size},Position=at,MaterialOverride=Material(hex,alpha)});}
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
    public void Sync(CityState state,double? interpolation=null)
    {
        _state=state;if(_static==null)return;
        var renderTime=interpolation is {} blend&&ReferenceEquals(_motionState,state)&&state.ElapsedSeconds>_motionBeforeAt?_motionBeforeAt+(state.ElapsedSeconds-_motionBeforeAt)*Math.Clamp(blend,0,1):state.ElapsedSeconds;
        var visualSeconds=Math.Max(0,renderTime-_lastVisualTime);_lastVisualTime=renderTime;
        // Exact presentation (pause, load, commands and captures) must not resume an older motion sample.
        if(interpolation==null||interpolation>=1)_motionState=null;
        var interpolate=interpolation is {} fraction&&fraction<1&&ReferenceEquals(_motionState,state)&&state.ElapsedSeconds>_motionBeforeAt;if(interpolate)BuildMotionTraces(state);
        foreach(var b in state.Buildings){if(b.Removed){Remove(_buildings,b.Id);Remove(_causes,b.Id);ForgetVisual(b.Id);continue;}
            if(!_buildings.TryGetValue(b.Id,out var node)||_finished.GetValueOrDefault(b.Id)!=b.Completed){Remove(_buildings,b.Id);node=new Node3D();AddChild(node);node.Position=P(b.Position);node.Rotation=new(0,(float)b.Rotation,0);ForgetVisual(b.Id);if(b.Completed){var visual=BuildingVisual(b);node.AddChild(visual);BindVisual(b.Id,visual);}else{Box(node,new((float)b.Definition.Width,.22f,(float)b.Definition.Depth),new(0,.15f,0),"c7ae78");for(var x=-1;x<=1;x+=2)for(var z=-1;z<=1;z+=2)Box(node,new(.3f,3,.3f),new(x*(float)b.Definition.Width/2,1.5f,z*(float)b.Definition.Depth/2),"d7a355");} _buildings[b.Id]=node;_finished[b.Id]=b.Completed;
                _buildingPickMeshes[b.Id]=MeshesIn(node).ToList();var cause=new Label3D{FontSize=32,PixelSize=.06f,Width=260,AutowrapMode=TextServer.AutowrapMode.WordSmart,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=false,Modulate=new Color("fff5d4"),Position=P(b.Position,b.Completed?10:4),OutlineSize=4};AddChild(cause);Remove(_causes,b.Id);_causes[b.Id]=cause;}
            if(!b.Completed){var project=state.Projects.First(p=>p.Id==b.ProjectId);var stopped=project.Cause!="Construção em andamento"&&!string.IsNullOrWhiteSpace(project.Cause);var text=((int)(project.Progress*100))+"%"+(stopped?"\n"+BriefWorkCause(project.Cause):"");if(_causes[b.Id].Text!=text)_causes[b.Id].Text=text;_causes[b.Id].Modulate=new Color(stopped?"ffe0a3":"fff5d4");}else{var company=state.Companies.FirstOrDefault(c=>c.Id==b.CompanyId);if(_companySigns.TryGetValue(b.Id,out var label)&&label.Text!=(company?.Name??""))label.Text=company?.Name??"";_causes[b.Id].Text=b.WaterDemand>0&&b.WaterRatio<.9?"◉":b.ElectricityDemand>0&&b.ElectricityRatio<.9?"ϟ":"";}
            if(b.Completed){if(_rotors.TryGetValue(b.Id,out var rotor)&&b.OperationalCapacity>.001)rotor.RotateZ((float)(visualSeconds*state.WindFactor*.08));if(_waterStates.TryGetValue(b.Id,out var waterState))waterState.Visible=b.OperationalCapacity>.001&&(b.Kind!=BuildingKind.SewageTreatment||state.SewageDemand>.001);if(_stockStates.TryGetValue(b.Id,out var stockState)){stockState.Visible=_stockMarkers[b.Id].Any(marker=>b.Quantity(marker.Resource)>.001);foreach(var marker in _stockMarkers[b.Id])marker.Node.Visible=b.Quantity(marker.Resource)>.001;}}
        }
        if(_roadRevision!=state.RoadRevision){foreach(var r in _roads.Values){RemoveChild(r);r.QueueFree();}_roads.Clear();_lamps.Clear();_lampGlass.Clear();_roadFootprints.Clear();_roadCarriageways.Clear();_crosswalkStripes.Clear();foreach(var road in state.Roads){var n=RoadVisual(road,state);AddChild(n);_roads[road.Id]=n;}DrawCrosswalkPaint();_roadRevision=state.RoadRevision;}
        SyncRoadWorks(state);
        var night=state.Now.Hour<6||state.Now.Hour>=18;
        foreach(var road in state.Roads){if(_lamps.TryGetValue(road.Id,out var light)){light.Visible=night&&road.ElectricityRatio>.01;light.LightEnergy=(float)road.ElectricityRatio*1.3f;if(_lampGlass.TryGetValue(road.Id,out var glass))glass.MaterialOverride=light.Visible?_lampOn:_lampOff;}if(_roads.TryGetValue(road.Id,out var r)&&r.GetNodeOrNull<MeshInstance3D>("Signal") is {} signal){signal.Visible=road.Control==JunctionControl.Signal&&road.Neighbours.Count>=3;var green=state.ElapsedSeconds%60<30;signal.MaterialOverride=Material(green?"6ca56b":"ba594d");}}
        foreach(var c in state.Citizens){if(!c.Alive||!c.InsideMap||c.VehicleId!=null){Remove(_people,c.Id);continue;}if(!_people.TryGetValue(c.Id,out var n)){n=Scene("agent.sim_pedestrian.a")??new Node3D();if(n.GetChildCount()==0)Box(n,new(.6f,1.7f,.5f),new(0,.85f,0),"d7a359");AddChild(n);_people[c.Id]=n;}
            var pose=interpolate&&_peopleMotion.TryGetValue(c.Id,out var motion)?motion.At(interpolation!.Value):(c.Position,(double?)null);n.Position=P(pose.Item1,RoadSurfaceAt(pose.Item1));n.Visible=c.Moving||!state.Buildings.Any(b=>b.Completed&&!b.Removed&&b.Door.Distance(pose.Item1)<1);
            var anim=n.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");if(anim!=null){var animation=c.Moving||pose.Item2!=null?"walk":"idle";if(anim.HasAnimation(animation)&&anim.CurrentAnimation!=animation)anim.Play(animation);}if(pose.Item2 is {} heading)n.Rotation=new(0,(float)heading,0);else if(c.Moving){var target=c.Path[c.PathIndex];n.Rotation=new(0,(float)Math.Atan2(c.Position.X-target.X,c.Position.Z-target.Z),0);}}
        foreach(var v in state.Vehicles){if(v.Status==VehicleStatus.Outside){Remove(_trucks,v.Id);continue;}if(!_trucks.TryGetValue(v.Id,out var n)){n=Scene("vehicle.truck_cargo.a")??new Node3D();if(n.GetChildCount()==0){Box(n,new(2.3f,2.2f,4.5f),new(0,1.4f,0),"b8704b");}AddChild(n);_trucks[v.Id]=n;}var pose=interpolate&&_vehicleMotion.TryGetValue(v.Id,out var motion)?motion.At(interpolation!.Value):(v.Position,(double?)null);n.Position=P(pose.Item1,RoadSurfaceAt(pose.Item1));n.Rotation=new(0,(float)(pose.Item2??v.Heading+Math.PI),0);}
        var geometry=state.Buildings.Aggregate(state.RoadRevision,(hash,b)=>HashCode.Combine(hash,b.Id,b.Removed));if(_vegetationGeometry!=geometry){_vegetationGeometry=geometry;foreach(var tree in _trees){var p=tree.Position;tree.Node.Visible=!state.Buildings.Any(b=>!b.Removed&&Math.Abs(p.X-b.Position.X)<Math.Max(b.Definition.Width,b.Definition.Depth)/2+4&&Math.Abs(p.Z-b.Position.Z)<Math.Max(b.Definition.Width,b.Definition.Depth)/2+4)&&!state.Roads.Any(r=>r.Position.Distance(p)<state.Tuning.RoadWidth/2+3);}}
        LayoutWorkLabels();
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
        var viewport=GetViewport().GetVisibleRect();var key=HashCode.Combine(camera.GlobalTransform,camera.Size,viewport);foreach(var label in labels)key=HashCode.Combine(key,label.GetInstanceId(),label.GlobalPosition,label.Text,label.FontSize,label.PixelSize,label.GetAabb());
        if(_labelLayoutKey==key)return;_labelLayoutKey=key;var occupied=new List<Rect2>();
        foreach(var label in labels.OrderBy(label=>camera.GlobalPosition.DistanceSquaredTo(label.GlobalPosition))){
            if(string.IsNullOrEmpty(label.Text)||camera.IsPositionBehind(label.GlobalPosition)){label.Visible=false;continue;}
            var bounds=label.GetAabb();var centre=label.GlobalPosition+camera.GlobalBasis.X*bounds.GetCenter().X+camera.GlobalBasis.Y*bounds.GetCenter().Y;
            var a=camera.UnprojectPosition(centre-camera.GlobalBasis.X*bounds.Size.X/2-camera.GlobalBasis.Y*bounds.Size.Y/2);var b=camera.UnprojectPosition(centre+camera.GlobalBasis.X*bounds.Size.X/2+camera.GlobalBasis.Y*bounds.Size.Y/2);
            var rect=new Rect2(new Vector2(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y)),new Vector2(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y))).Grow(4);
            label.Visible=rect.Intersects(viewport)&&!occupied.Any(other=>other.Intersects(rect));if(label.Visible)occupied.Add(rect);
        }
    }
    void SyncRoadWorks(CityState state)
    {
        var works=state.Projects.Where(p=>!p.IsMaintenance&&p.RoadPoints.Count>0&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress).ToList();
        var geometry=works.Aggregate(state.RoadRevision,(hash,p)=>HashCode.Combine(hash,p.Id,p.RoadPoints.Count,p.BuiltRoadNodes.Count,p.Status));
        if(_roadWorkGeometry!=geometry){foreach(var id in _roadWorks.Keys.ToList())Remove(_roadWorks,id);_roadWorkLabels.Clear();
            foreach(var work in works){var root=new Node3D{Name="RoadWork"+work.Id};AddChild(root);_roadWorks[work.Id]=root;
                for(var i=0;i<work.RoadPoints.Count;i++){var at=work.RoadPoints[i];if(state.Roads.Any(r=>r.Position.Distance(at)<.05))continue;Box(root,RoadCellSize(work.RoadPoints,i,(float)state.Tuning.RoadWidth,(float)state.Tuning.RoadStep),P(at,.18f),"c7ae78",.45f);}
                var label=new Label3D{FontSize=32,PixelSize=.075f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Modulate=new Color("fff5d4"),Position=P(work.Position,3),OutlineSize=4};root.AddChild(label);_roadWorkLabels[work.Id]=label;
            }_roadWorkGeometry=geometry;
        }
        foreach(var work in works){var text=$"Obra de rua #{work.Id}\n{(int)(work.Progress*100)}% • {work.Cause}";if(_roadWorkLabels[work.Id].Text!=text)_roadWorkLabels[work.Id].Text=text;}
    }
    Vector3 RoadCellSize(IReadOnlyList<Point> points,int i,float width,float step)
    {
        var at=points[i];var adjacent=new List<Point>();if(i>0)adjacent.Add(points[i-1]);if(i+1<points.Count)adjacent.Add(points[i+1]);if(adjacent.Count==0)adjacent.AddRange(_state.Roads.Where(r=>r.Position.Distance(at)<=step+.05).Select(r=>r.Position));
        var horizontal=adjacent.Any(p=>Math.Abs(p.X-at.X)>.1);var vertical=adjacent.Any(p=>Math.Abs(p.Z-at.Z)>.1);return horizontal&&vertical?new(width,.05f,width):vertical?new(width,.05f,step):new(step,.05f,width);
    }
    void ForgetVisual(int id){_companySigns.Remove(id);_rotors.Remove(id);_waterStates.Remove(id);_stockStates.Remove(id);_stockMarkers.Remove(id);_buildingPickMeshes.Remove(id);}
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
        var horizontal=road.Highway||road.Neighbours.Any(id=>Math.Abs(state.Roads.First(r=>r.Id==id).Position.X-road.Position.X)>.1);var vertical=road.Neighbours.Any(id=>Math.Abs(state.Roads.First(r=>r.Id==id).Position.Z-road.Position.Z)>.1);
        if(!horizontal&&!vertical){var origin=state.Projects.FirstOrDefault(p=>p.BuiltRoadNodes.Contains(road.Id));if(origin!=null){var index=origin.RoadPoints.FindIndex(p=>p.Distance(road.Position)<.05);var adjacent=new List<Point>();if(index>0)adjacent.Add(origin.RoadPoints[index-1]);if(index>=0&&index+1<origin.RoadPoints.Count)adjacent.Add(origin.RoadPoints[index+1]);horizontal=adjacent.Any(p=>Math.Abs(p.X-road.Position.X)>.1);vertical=adjacent.Any(p=>Math.Abs(p.Z-road.Position.Z)>.1);}if(!horizontal&&!vertical)horizontal=true;}
        if(horizontal&&vertical){
            SurfaceRect(road.Position,half*2,half*2,false);SurfaceRect(road.Position,asphalt,asphalt,true);
            Box(n,new(half*2,.02f,half*2),new(0,.01f,0),"bdb7ab");Box(n,new(asphalt,.01f,asphalt),new(0,RoadAsphaltTop-.005f,0),"4a4e54");
            var corner=(half*2-asphalt)/2;for(var x=-1;x<=1;x+=2)for(var z=-1;z<=1;z+=2)Box(n,new(corner,RoadSidewalkTop,corner),new(x*(asphalt/2+corner/2),RoadSidewalkTop/2,z*(asphalt/2+corner/2)),"bdb7ab");
            foreach(var alongX in new[]{true,false})foreach(var sign in new[]{-1,1}){
                var opened=road.Neighbours.Any(id=>{var delta=state.Roads.First(r=>r.Id==id).Position-road.Position;return alongX?Math.Abs(delta.X)>.1&&Math.Sign(delta.X)==sign:Math.Abs(delta.Z)>.1&&Math.Sign(delta.Z)==sign;});
                var at=alongX?new Vector3(sign*(asphalt/2+corner/2),0,0):new Vector3(0,0,sign*(asphalt/2+corner/2));var size=alongX?new Vector3(corner,0,asphalt):new Vector3(asphalt,0,corner);
                size.Y=opened?.01f:RoadSidewalkTop;at.Y=opened?RoadAsphaltTop-.005f:RoadSidewalkTop/2;Box(n,size,at,opened?"4a4e54":"bdb7ab");
                if(opened)SurfaceRect(road.Position+new Point(at.X,at.Z),size.X,size.Z,true);
            }
        }
        if(horizontal&&!vertical){SurfaceRect(road.Position,step,half*2,false);SurfaceRect(road.Position,step,asphalt,true);if(!road.Highway&&Scene("road.urban_1x1.a.straight") is {} straight){FitRoadStrip(straight,step,half*2,false);n.AddChild(straight);}else RoadStrip(n,step,half*2,asphalt,false,road.Id%3==0);}
        if(vertical&&!horizontal){SurfaceRect(road.Position,half*2,step,false);SurfaceRect(road.Position,asphalt,step,true);if(!road.Highway&&Scene("road.urban_1x1.a.straight") is {} upright){FitRoadStrip(upright,step,half*2,true);n.AddChild(upright);}else RoadStrip(n,step,half*2,asphalt,true,road.Id%3==0);}
        if(!road.Highway&&road.Id%8==0){var lamp=Scene("road.urban_1x1.a.lamp")??new Node3D();lamp.Position=horizontal&&vertical?new(half-.7f,RoadSidewalkTop,half-.7f):horizontal?new(0,RoadSidewalkTop,half-.7f):new(half-.7f,RoadSidewalkTop,0);lamp.Rotation=new(0,horizontal?0:Mathf.Pi/2,0);n.AddChild(lamp);if(lamp.GetChildCount()==0)Box(lamp,new(.22f,6,.22f),new(0,3,0),"45535a");var source=lamp.GetNodeOrNull<Marker3D>("Candidate_LightSource");var light=new OmniLight3D{Position=source?.Position??new(0,5,0),LightColor=new Color("ffd793"),OmniRange=17,ShadowEnabled=false};lamp.AddChild(light);_lamps[road.Id]=light;if(lamp.GetNodeOrNull<MeshInstance3D>("LampGlass") is {} glass){_lampOn??=GD.Load<Material>("res://Assets/V1/Materials/road_lamp_glass_on.tres");_lampOff??=GD.Load<Material>("res://Assets/V1/Materials/road_lamp_glass_off.tres");_lampGlass[road.Id]=glass;}}
        if(road.Crosswalk&&!road.Highway){if(horizontal&&vertical){foreach(var id in road.Neighbours){var arm=state.Roads.First(r=>r.Id==id).Position-road.Position;var alongX=Math.Abs(arm.X)>.1;var at=(alongX?Math.Sign(arm.X):Math.Sign(arm.Z))*(half-.7f);for(var i=-2;i<=2;i++)CrosswalkStroke(road.Position,alongX?new(.7f,0,asphalt):new(asphalt,0,.7f),alongX?new(at+i*.8f,0,0):new(0,0,at+i*.8f));}}else for(var i=-2;i<=2;i++)CrosswalkStroke(road.Position,horizontal?new(.7f,0,asphalt):new(asphalt,0,.7f),horizontal?new(i,0,0):new(0,0,i));n.AddChild(new MeshInstance3D{Name="Signal",Mesh=new BoxMesh{Size=new(.3f,.7f,.3f)},Position=new(-4,3,4),MaterialOverride=Material("6ca56b")});}
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
    static void RoadStrip(Node3D parent,float length,float width,float asphalt,bool vertical,bool dash)
    {
        Vector3 Size(float x,float y,float z)=>vertical?new(z,y,x):new(x,y,z);Vector3 Position(float x,float y,float z)=>vertical?new(z,y,x):new(x,y,z);
        Box(parent,Size(length,.02f,width),Position(0,.01f,0),"bdb7ab");Box(parent,Size(length,.01f,asphalt),Position(0,RoadAsphaltTop-.005f,0),"4a4e54");
        var sidewalk=(width-asphalt)/2;foreach(var side in new[]{-1,1})Box(parent,Size(length,RoadSidewalkTop,sidewalk),Position(0,RoadSidewalkTop/2,side*(asphalt/2+sidewalk/2)),"bdb7ab");
        if(dash)Box(parent,Size(Math.Min(2,length),.008f,.14f),Position(0,RoadAsphaltTop+.008f,0),"e0b23d");
    }
    static void FitRoadStrip(Node3D strip,float length,float width,bool vertical){var size=strip.GetMeta("visual_envelope_size",new Vector3(10.5f,.18f,10.5f)).AsVector3();strip.Scale=new(length/Math.Max(.01f,size.X),1,width/Math.Max(.01f,size.Z));if(vertical)strip.Rotation=new(0,Mathf.Pi/2,0);}
    void Remove<T>(Dictionary<int,T> table,int id)where T:Node3D{if(table.Remove(id,out var node)){RemoveChild(node);node.QueueFree();}}
    static void PreviewValidity(Node3D parent,Vector3 at,bool valid){parent.AddChild(new Label3D{Text=valid?"✓":"×",Position=at,FontSize=48,PixelSize=.04f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,OutlineSize=4,Modulate=new Color("fff5d4")});}
    public void ClearPreview(){if(_preview==null||_previewKey=="")return;foreach(var c in _preview.GetChildren()){_preview.RemoveChild(c);c.QueueFree();}_previewKey="";_previewBuildingNode=null;_previewBuildingKind=null;_previewBuildingValid=null;_previewBuildingColor=null;_previewFootprint.Clear();_previewValidity=null;}
    static void MakeTransparent(Node node)
    {
        if(node is MeshInstance3D mesh&&mesh.Mesh!=null){
            mesh.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            if(mesh.MaterialOverride is BaseMaterial3D overridden){var copy=(BaseMaterial3D)overridden.Duplicate();copy.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;copy.AlbedoColor=new Color(copy.AlbedoColor,.38f);mesh.MaterialOverride=copy;}
            else for(var i=0;i<mesh.Mesh.GetSurfaceCount();i++){var source=mesh.GetSurfaceOverrideMaterial(i)??mesh.Mesh.SurfaceGetMaterial(i);if(source is BaseMaterial3D original){var copy=(BaseMaterial3D)original.Duplicate();copy.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;copy.AlbedoColor=new Color(copy.AlbedoColor,.38f);mesh.SetSurfaceOverrideMaterial(i,copy);}}
        }
        foreach(var child in node.GetChildren())MakeTransparent(child);
    }
    void PreviewBoundary(Node3D parent,Vector3 size,Vector3 position)
    {
        var mesh=new MeshInstance3D{Mesh=new BoxMesh{Size=size},Position=position,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};parent.AddChild(mesh);_previewFootprint.Add(mesh);
    }
    public void PreviewBuilding(BuildingKind kind,Point at,double rotation,bool valid,bool? connected=null)
    {
        var key=$"{kind}:{at}:{rotation}:{valid}:{connected}";if(_previewKey==key)return;
        if(_previewBuildingNode==null||_previewBuildingKind!=kind){
            ClearPreview();var definition=Catalog.Definitions[kind];var width=(float)definition.Width;var depth=(float)definition.Depth;
            var marker=new Node3D{Name="BuildingPreview"};_preview.AddChild(marker);_previewBuildingNode=marker;_previewBuildingKind=kind;
            var model=BuildingVisual(kind);model.Name="TransparentModel";MakeTransparent(model);marker.AddChild(model);
            PreviewBoundary(marker,new(width,.12f,.4f),new(0,.25f,-depth/2+.2f));PreviewBoundary(marker,new(width,.12f,.4f),new(0,.25f,depth/2-.2f));
            PreviewBoundary(marker,new(.4f,.12f,depth),new(-width/2+.2f,.25f,0));PreviewBoundary(marker,new(.4f,.12f,depth),new(width/2-.2f,.25f,0));
            PreviewBox(marker,new(3,.14f,.6f),new(0,.35f,-depth/2),PreviewMaterial("fff1a3"));
            PreviewBox(marker,new(.7f,.12f,2.4f),new(0,.28f,-depth/2-1.6f),PreviewMaterial("fff1a3"));
            var arrow=new ImmediateMesh();arrow.SurfaceBegin(Mesh.PrimitiveType.Triangles);arrow.SurfaceSetNormal(Vector3.Up);arrow.SurfaceAddVertex(new(-1.5f,.28f,-depth/2-2));arrow.SurfaceAddVertex(new(0,.28f,-depth/2-3.8f));arrow.SurfaceAddVertex(new(1.5f,.28f,-depth/2-2));arrow.SurfaceEnd();
            marker.AddChild(new MeshInstance3D{Name="EntranceDirection",Mesh=arrow,MaterialOverride=PreviewMaterial("fff1a3"),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
            _previewValidity=new Label3D{Position=new(0,1.5f,-depth/2),FontSize=48,PixelSize=.065f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,RenderPriority=11,OutlineSize=6};marker.AddChild(_previewValidity);
        }
        _previewKey=key;_previewBuildingNode.Position=P(at,.02f);_previewBuildingNode.Rotation=new(0,(float)rotation,0);
        var color=!valid?"df8577":connected==false?"e6bc64":"93cf97";
        if(_previewBuildingValid!=valid||_previewBuildingColor!=color){_previewBuildingValid=valid;_previewBuildingColor=color;foreach(var edge in _previewFootprint)edge.MaterialOverride=PreviewMaterial(color);_previewValidity!.Text=valid?"✓":"×";_previewValidity.Modulate=new Color(color);}
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
                var vertical=entry.Key.Vertical;var line=entry.Key.Line+(entry.Key.Positive?-.15f:.15f);PreviewBox(_preview,vertical?new(.3f,.05f,end-start):new(end-start,.05f,.3f),vertical?new(line,.34f,(start+end)/2):new((start+end)/2,.34f,line),PreviewMaterial(color));if(i<ordered.Count){start=ordered[i].Start;end=ordered[i].End;}}
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
    public void PreviewRoad(IEnumerable<Point> points,bool valid,double width,bool? connected=null)
    {
        var p=points.ToList();var key=$"road:{p.FirstOrDefault()}:{p.LastOrDefault()}:{p.Count}:{valid}:{connected}:{width}:{p.ElementAtOrDefault(p.Count/2)}";if(_previewKey==key)return;ClearPreview();_previewKey=key;
        var color=!valid?"df8577":connected==false?"e6bc64":"93cf97";var rectangles=new List<PreviewRectangle>();
        for(var i=0;i<p.Count;i++){var size=RoadCellSize(p,i,(float)width,(float)_state.Tuning.RoadStep);var position=P(p[i],.25f);PreviewBox(_preview,size,position,PreviewMaterial(color,.48f));rectangles.Add(new(position.X-size.X/2,position.X+size.X/2,position.Z-size.Z/2,position.Z+size.Z/2));}
        PreviewRoadOutline(rectangles,color);if(p.Count>0)PreviewValidity(_preview,P(p[^1],1.5f),valid);
    }
}
