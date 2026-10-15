using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    sealed class ActivityBadge
    {
        public Node3D Anchor=null!;
        public (Sprite3D Pill,Sprite3D Icon,Label3D Count) Cargo;
        public int ShownCargo=-1;public float Unit;
    }
    readonly Dictionary<int,ActivityBadge> _activityBadges=new();
    readonly Dictionary<int,int> _cargoCounts=new();
    // Badge artwork is authored at 2x: one design pixel = two texture pixels.
    const float BadgeTailToCenter=41,BadgeBodyCenter=49,BadgeIconX=-28,BadgeCountX=-4;
    const float BadgeMaxHeight=5f;

    // Only real loading/unloading keeps an information badge over the entrance.
    // The presence balloon was removed (IA-017 human review, 2026-10-15) and
    // replaced by the problem alerts in CityWorld.ProblemAlerts.cs.
    void CreateActivityBadge(Building b,Node3D node,Node3D visual)
    {
        // Anchored in front of the entrance (local -Z is the door side), low
        // enough to belong to the door even on tall models like wind turbines.
        var anchor=new Node3D{Name="BuildingActivity"+b.Id,Position=new(0,Math.Min(ModelTop(node,visual)+.5f,BadgeMaxHeight),-(float)b.Definition.Depth/2-.3f),Visible=false};
        node.AddChild(anchor);
        _activityBadges[b.Id]=new ActivityBadge{Anchor=anchor,Cargo=BadgePart(anchor,"Cargo","activity_cargo")};
    }
    static (Sprite3D,Sprite3D,Label3D) BadgePart(Node3D anchor,string name,string icon)
    {
        Sprite3D Sprite(string part,string texture,int priority){var sprite=new Sprite3D{Name=name+part,Texture=GD.Load<Texture2D>("res://Assets/UI/"+texture+".svg"),Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,Shaded=false,RenderPriority=priority,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,Visible=false};anchor.AddChild(sprite);return sprite;}
        var pill=Sprite("Pill","activity_badge",20);var glyph=Sprite("Icon",icon,21);
        var count=new Label3D{Name=name+"Count",FontSize=44,OutlineSize=4,Modulate=Colors.White,OutlineModulate=new Color("20323a"),Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,RenderPriority=23,OutlineRenderPriority=22,HorizontalAlignment=HorizontalAlignment.Left,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,Visible=false};
        anchor.AddChild(count);return (pill,glyph,count);
    }
    // Constant on-screen size (0.85 screen px per design px) up to zoom 100;
    // beyond that it shrinks with the map so a full city view stays readable.
    float BadgeUnit()
    {
        var viewport=GetViewport();var camera=viewport?.GetCamera3D();if(camera==null)return .05f;
        var height=Math.Max(1,viewport!.GetVisibleRect().Size.Y);var size=camera.Projection==Camera3D.ProjectionType.Orthogonal?camera.Size:110;
        return size/height*.85f*Mathf.Pow(Mathf.Min(1,100/size),.7f);
    }
    void PresentActivityBadges()
    {
        var unit=BadgeUnit();
        foreach(var (id,badge) in _activityBadges){
            var cargo=_cargoCounts.GetValueOrDefault(id);
            if(cargo==badge.ShownCargo&&Math.Abs(unit-badge.Unit)<=badge.Unit*.01f)continue;
            badge.ShownCargo=cargo;badge.Unit=unit;
            badge.Anchor.Visible=cargo>0;
            LayoutBadgePart(badge.Cargo,cargo,0,unit);
        }
    }
    static void LayoutBadgePart((Sprite3D Pill,Sprite3D Icon,Label3D Count) part,int value,float shift,float unit)
    {
        var shown=value>0;part.Pill.Visible=part.Icon.Visible=part.Count.Visible=shown;if(!shown)return;
        var pixel=unit/2;var text=value.ToString();if(part.Count.Text!=text)part.Count.Text=text;
        part.Pill.PixelSize=part.Icon.PixelSize=part.Count.PixelSize=pixel;
        part.Pill.Offset=new(shift,BadgeTailToCenter);
        part.Icon.Offset=new(shift+BadgeIconX,BadgeBodyCenter);
        part.Count.Offset=new(shift+BadgeCountX,BadgeBodyCenter);
    }
    void ClearActivityBadges(){_activityBadges.Clear();_cargoCounts.Clear();}

    // Read-only view of the real cargo step, for QA.
    public (int Cargo,bool Visible) ActivityIndicator(int buildingId)
    {
        if(!_activityBadges.TryGetValue(buildingId,out var badge))return (0,false);
        return (badge.Cargo.Count.Visible?int.Parse(badge.Cargo.Count.Text):0,badge.Anchor.Visible);
    }
    public int VisibleCitizenCount=>_people.Values.Count(n=>n.Visible);
    public int CitizenNodeCount=>_people.Count;
    public float BuildingModelTop(int buildingId)=>_buildings.TryGetValue(buildingId,out var node)&&node.GetChildCount()>0&&node.GetChild(0) is Node3D model?ModelTop(node,model):0;
}
