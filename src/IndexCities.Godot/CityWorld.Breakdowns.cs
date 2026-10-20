using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

// Presentation of breakdowns and of the mechanic shop (SPEC transporte-circulacao,
// "Quebras, guincho local e oficina"; asset contracts in Assets/Breakdown/README.md
// and Assets/Workshop/README.md). Everything here only reads the simulation.
public partial class CityWorld
{
    const string HazardLightsScene="res://Assets/Breakdown/Scenes/prop.hazard_lights.a.tscn",SmokeScene="res://Assets/Breakdown/Scenes/prop.breakdown_smoke.a.tscn",TriangleScene="res://Assets/Breakdown/Scenes/prop.warning_triangle.a.tscn";
    // Ground distance between the vehicle's rear lights and the warning triangle.
    const float TriangleBehindRear=1.5f;
    sealed record ShopVisual(Node3D Root,Node3D? Seat,AnimationPlayer? Lift,AnimationPlayer? Gate);
    readonly Dictionary<int,ShopVisual> _shopVisuals=new();

    static Node3D? Prop(string file)=>ResourceLoader.Exists(file)?GD.Load<PackedScene>(file)?.Instantiate<Node3D>():null;
    static HashSet<int> VehiclesBeingLoaded(CityState state)=>state.Vehicles.Where(t=>t.TowPhase==TowPhase.Loading&&t.TowTargetId!=null).Select(t=>t.TowTargetId!.Value).ToHashSet();

    // Broken on the road (until its tow starts loading it): hazard lights, open
    // hood, smoke and the triangle behind it. On a shop bay in service: open hood.
    void SyncBreakdownParts(Node3D n,Vehicle v,bool beingLoaded)
    {
        var stranded=v.BrokenDown&&v.Status==VehicleStatus.Stranded&&!beingLoaded;
        var serviced=v.ServicePhase==ServicePhase.InService&&v.ServiceBay!=null&&v.PushPath.Count==0;
        // Nothing shown and nothing to show: the common case costs one lookup.
        var first=!n.HasMeta("hood_open");if(!first&&!stranded&&!serviced&&!n.GetMeta("hood_open").AsBool())return;
        SetHood(n,stranded||serviced,first);
        var hazards=n.GetNodeOrNull<Node3D>("BreakdownHazards");
        if(stranded&&hazards==null&&Prop(HazardLightsScene) is {} lights){
            lights.Name="BreakdownHazards";
            foreach(var corner in new[]{"FL","FR","RL","RR"})if(n.GetNodeOrNull<Node3D>("Hazard"+corner) is {} marker&&lights.GetNodeOrNull<Node3D>("Light"+corner) is {} light)light.Transform=marker.Transform;
            n.AddChild(lights);
        }
        else if(!stranded&&hazards!=null)Discard(hazards);
        var smoke=n.GetNodeOrNull<Node3D>("BreakdownSmoke");
        // The particles are in world space: the origin goes on before entering the tree.
        if(stranded&&smoke==null&&n.GetNodeOrNull<Node3D>("HoodSmoke") is {} vent&&Prop(SmokeScene) is {} puff){puff.Name="BreakdownSmoke";puff.Transform=vent.Transform;n.AddChild(puff);}
        else if(!stranded&&smoke!=null)Discard(smoke);
        var triangle=n.GetNodeOrNull<Node3D>("BreakdownTriangle");
        if(stranded&&triangle==null&&Prop(TriangleScene) is {} sign){
            var rear=new[]{"HazardRL","HazardRR"}.Select(name=>n.GetNodeOrNull<Node3D>(name)?.Position.Z??0).DefaultIfEmpty(0).Max();
            sign.Name="BreakdownTriangle";sign.Position=new(0,0,rear+TriangleBehindRear);n.AddChild(sign);
        }
        else if(!stranded&&triangle!=null)Discard(triangle);
    }
    static void Discard(Node node){node.GetParent()?.RemoveChild(node);node.QueueFree();}
    // hood_open plays once on the change; a vehicle drawn already open (new visual,
    // loaded city) shows the final pose.
    static void SetHood(Node3D n,bool open,bool first)
    {
        if(!first&&n.GetMeta("hood_open").AsBool()==open)return;n.SetMeta("hood_open",open);
        if(n.GetNodeOrNull<AnimationPlayer>("AnimationPlayer") is not {} player||!player.HasAnimation("hood_open"))return;
        if(open){player.Play("hood_open");if(first)player.Seek(player.CurrentAnimationLength,true);}
        else if(!first)player.PlayBackwards("hood_open");
    }

    ShopVisual? ShopVisualOf(int? shopId)
    {
        if(shopId is not {} id||!_buildings.TryGetValue(id,out var node)||!IsInstanceValid(node))return null;
        if(_shopVisuals.TryGetValue(id,out var cached)&&IsInstanceValid(cached.Root)&&cached.Root.GetParent()==node)return cached;
        var root=node.GetChildren().OfType<Node3D>().FirstOrDefault(child=>child.FindChild("LiftPlatform",true,false)!=null);if(root==null)return null;
        var lift=root.FindChild("LiftPlatform",true,false) as Node3D;
        var visual=new ShopVisual(root,lift?.GetNodeOrNull<Node3D>("ServiceBay0"),lift?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer"),root.GetNodeOrNull<AnimationPlayer>("Gate/AnimationPlayer"));
        _shopVisuals[id]=visual;return visual;
    }
    // The real car on the lift bay rides the platform (asset contract: parented to
    // LiftPlatform/ServiceBay0 with an identity local transform).
    Node3D? LiftSeat(Vehicle v)=>v.ServiceBay==0&&v.Status==VehicleStatus.Parked&&v.PushPath.Count==0&&v.ServicePhase is ServicePhase.InService or ServicePhase.AwaitingReturn?ShopVisualOf(v.ServiceShopId)?.Seat:null;
    void Seat(Node3D n,Node3D parent){if(n.GetParent()==parent)return;n.GetParent()?.RemoveChild(n);parent.AddChild(n);}

    // The lift rises while a vehicle is in service on it and comes down when the
    // service ends. The gate of the floor bay is open while a vehicle moves
    // through it, and during opening hours while one stands inside; closed otherwise.
    void SyncShopVisuals(CityState state)
    {
        foreach(var b in state.Buildings){
            if(b.Kind!=BuildingKind.MechanicShop||!b.Completed||b.Removed||ShopVisualOf(b.Id) is not {} shop)continue;
            var assigned=state.Vehicles.Where(v=>v.ServiceShopId==b.Id).ToList();
            var liftUp=assigned.Any(v=>v.ServiceBay==0&&v.ServicePhase==ServicePhase.InService&&v.PushPath.Count==0&&v.Status==VehicleStatus.Parked);
            bool AtFloorBay(Vehicle v,double front){var local=Local(b,v.Position);return Math.Abs(local.X-FloorBayX)<1.6&&local.Z>front&&local.Z<b.Definition.Depth/2;}
            var moving=state.Vehicles.Any(v=>v.PushPath.Count>0&&v.Status==VehicleStatus.Parked&&AtFloorBay(v,-3)||v.PushPath.Count>0&&v.ServiceShopId==b.Id&&v.ServiceBay==1);
            var inside=assigned.Any(v=>v.Status==VehicleStatus.Parked&&AtFloorBay(v,BayThresholdZ));
            Animate(shop.Lift,"lift_up","lift_down",liftUp);
            Animate(shop.Gate,"gate_open","gate_close",moving||inside&&CitySimulation.OpenAt(b.Kind,state.Now));
        }
    }
    // Floor bay (ServiceBay1) and facade line of the shop model, in lot coordinates.
    const double FloorBayX=-1.45,BayThresholdZ=-.3;
    static Point Local(Building b,Point p){var d=p-b.Position;return new(d.X*Math.Cos(b.Rotation)-d.Z*Math.Sin(b.Rotation),d.X*Math.Sin(b.Rotation)+d.Z*Math.Cos(b.Rotation));}
    static void Animate(AnimationPlayer? player,string on,string off,bool state)
    {
        if(player==null)return;var shown=player.HasMeta("state")?player.GetMeta("state").AsBool():(bool?)null;if(shown==state)return;player.SetMeta("state",state);
        var name=state?on:off;if(!player.HasAnimation(name))return;player.Play(name);
        // First sight (new visual, loaded city): the final pose, without replaying the motion.
        if(shown==null)player.Seek(player.CurrentAnimationLength,true);
    }
    // QA: whether a vehicle is drawn on the lift, and the lift and gate states shown.
    public bool VehicleOnLift(int id)=>_trucks.TryGetValue(id,out var n)&&n.GetParent()?.Name=="ServiceBay0";
    public (bool? LiftUp,bool? GateOpen) ShopPresentation(int shopId)=>ShopVisualOf(shopId) is {} shop?(shop.Lift?.HasMeta("state")==true?shop.Lift.GetMeta("state").AsBool():null,shop.Gate?.HasMeta("state")==true?shop.Gate.GetMeta("state").AsBool():null):(null,null);
    public (bool Hood,bool Hazards,bool Smoke,bool Triangle) BreakdownParts(int id)=>_trucks.TryGetValue(id,out var n)?(n.HasMeta("hood_open")&&n.GetMeta("hood_open").AsBool(),n.GetNodeOrNull("BreakdownHazards")!=null,n.GetNodeOrNull("BreakdownSmoke")!=null,n.GetNodeOrNull("BreakdownTriangle")!=null):(false,false,false,false);
}
