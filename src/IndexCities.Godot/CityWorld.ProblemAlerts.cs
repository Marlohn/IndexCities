using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    // Problem alerts over completed buildings (owner decision, 2026-10-15).
    // Each icon is read from the very state the simulation already uses for
    // those causes: exterior access, water received/demand and electricity
    // received/demand. Nothing is computed per SIM and no consequence changes;
    // thresholds and persistence are presentation calibration only.
    sealed class ProblemAlert
    {
        public Node3D Anchor=null!;
        public readonly List<(Sprite3D Tile,Sprite3D Icon)> Icons=new();
        // Kinds currently drawn, in order; icons are pooled and reused.
        public readonly List<string> Shown=new();
    }
    readonly Dictionary<int,ProblemAlert> _problemAlerts=new();
    readonly Dictionary<int,(double Water,double Power)> _problemPersistence=new();
    // Alert kinds in the fixed priority order: road access, then water, then power.
    static readonly (string Name,string Texture,string Text)[] ProblemKinds=
        [("road","problem_road","Sem acesso viário"),("water","problem_water","Falta de água"),("power","problem_energy","Falta de energia")];
    static readonly Dictionary<string,Texture2D?> ProblemTextures=new();
    const string ProblemTileTexture="problem_alert_tile";
    const double ProblemUpdateSeconds=.5;      // moderate refresh, never per frame
    const double ProblemPersistSeconds=20;     // water/power must persist this long before alerting
    const double ProblemRecoverFactor=5;        // recovery clears the icon this much faster
    const float ProblemTileY=34,ProblemTileStep=76,ProblemMaxHeight=7.2f;
    double _problemClock,_problemElapsedAt;bool _problemDirty=true;

    void CreateProblemAlerts(Building b,Node3D node,Node3D visual)
    {
        var anchor=new Node3D{Name="BuildingProblems"+b.Id,Position=new(0,Math.Min(ModelTop(node,visual)+2.2f,ProblemMaxHeight),-(float)b.Definition.Depth/2-.3f),Visible=false};
        node.AddChild(anchor);
        _problemAlerts[b.Id]=new ProblemAlert{Anchor=anchor};
        _problemDirty=true;
    }
    static Texture2D? ProblemTexture(string file)
    {
        if(!ProblemTextures.TryGetValue(file,out var texture))texture=ProblemTextures[file]=GD.Load<Texture2D>("res://Assets/UI/"+file+".svg");
        return texture;
    }
    static (string Name,string Texture,string Text) ProblemKind(string name)=>ProblemKinds.First(kind=>kind.Name==name);

    void UpdateProblemAlerts(CityState state,CitySimulation simulation,double elapsed)
    {
        var advanced=Math.Max(0,elapsed-_problemElapsedAt);_problemElapsedAt=elapsed;_problemClock+=advanced;
        if(!_problemDirty&&_problemClock<ProblemUpdateSeconds)return;
        var step=_problemClock;_problemClock=0;_problemDirty=false;
        var unit=BadgeUnit();
        foreach(var b in state.Buildings){
            if(!_problemAlerts.TryGetValue(b.Id,out var alert))continue;
            var persistent=_problemPersistence.GetValueOrDefault(b.Id);
            // Exterior access is the same test the simulation uses for the cause
            // "Sem acesso viário" and for real delivery, crew and vehicle routes.
            var road=!simulation.HasExteriorAccess(b);
            var water=b.WaterDemand>0&&b.WaterRatio<.5;
            var power=b.ElectricityDemand>0&&b.ElectricityRatio<.5&&b.Kind!=BuildingKind.WindPower;
            // Asymmetric hysteresis: the shortage must persist before alerting,
            // but the icon leaves shortly after the problem ends. The accumulated
            // time is capped, so a long outage never delays the recovery signal.
            persistent.Water=water?Math.Min(ProblemPersistSeconds,persistent.Water+step):Math.Max(0,persistent.Water-step*ProblemRecoverFactor);
            persistent.Power=power?Math.Min(ProblemPersistSeconds,persistent.Power+step):Math.Max(0,persistent.Power-step*ProblemRecoverFactor);
            if(persistent==(0,0))_problemPersistence.Remove(b.Id);else _problemPersistence[b.Id]=persistent;
            var shown=new List<string>();
            if(road)shown.Add(ProblemKinds[0].Name);
            if(water&&persistent.Water>=ProblemPersistSeconds)shown.Add(ProblemKinds[1].Name);
            if(power&&persistent.Power>=ProblemPersistSeconds)shown.Add(ProblemKinds[2].Name);
            LayoutProblemAlerts(alert,shown,unit);
        }
    }
    void LayoutProblemAlerts(ProblemAlert alert,List<string> shown,float unit)
    {
        while(alert.Icons.Count<shown.Count){
            var index=alert.Icons.Count;
            var tile=new Sprite3D{Name="Tile"+index,Texture=ProblemTexture(ProblemTileTexture),Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,Shaded=false,RenderPriority=20,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,Visible=false};
            var icon=new Sprite3D{Name="Icon"+index,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,Shaded=false,RenderPriority=21,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,Visible=false};
            alert.Anchor.AddChild(tile);alert.Anchor.AddChild(icon);
            alert.Icons.Add((tile,icon));
        }
        for(var i=0;i<alert.Icons.Count;i++){
            var (tile,icon)=alert.Icons[i];var active=i<shown.Count;tile.Visible=icon.Visible=active;
            if(!active)continue;
            var texture=ProblemTexture(ProblemKind(shown[i]).Texture);if(icon.Texture!=texture)icon.Texture=texture;
        }
        alert.Anchor.Visible=shown.Count>0;
        alert.Shown.Clear();alert.Shown.AddRange(shown);
        if(shown.Count==0)return;
        var pixel=unit/2;var start=-(shown.Count-1)*ProblemTileStep/2f;
        for(var i=0;i<shown.Count;i++){
            var shift=start+i*ProblemTileStep;var (tile,icon)=alert.Icons[i];
            tile.PixelSize=icon.PixelSize=pixel;
            tile.Offset=new(shift,ProblemTileY);icon.Offset=new(shift,ProblemTileY);
        }
    }
    void ClearProblemAlerts(){_problemAlerts.Clear();_problemPersistence.Clear();_problemClock=0;_problemDirty=true;}

    // Read-only views of the very state the icons display, so the QA and the
    // building inspection can never disagree with what is drawn.
    public string ProblemAlertIcons(int buildingId)
    {
        if(!_problemAlerts.TryGetValue(buildingId,out var alert))return "";
        return string.Join(",",alert.Shown);
    }
    public string BuildingProblemText(int buildingId)
    {
        if(!_problemAlerts.TryGetValue(buildingId,out var alert)||alert.Shown.Count==0)return "";
        return "Problemas: "+string.Join(" · ",alert.Shown.Select(name=>ProblemKind(name).Text));
    }
}
