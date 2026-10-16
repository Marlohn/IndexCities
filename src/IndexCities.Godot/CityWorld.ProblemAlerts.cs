using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    // Problem alerts over completed buildings (owner decision, 2026-10-15,
    // extended with 5 more causes in the same session).
    // Each icon is read from the very state the simulation already uses for
    // those causes (see b.Cause in Infrastructure.cs): exterior access, water
    // received/demand, electricity received/demand, natural water source,
    // raw water capacity, workers present in opening hours, shop stock and
    // municipal maintenance ratio. Nothing is computed per SIM and no
    // consequence changes; thresholds and persistence are presentation
    // calibration only.
    //
    // General rule from SPEC: every new per-building problem cause the
    // simulation detects that asks for a player action must also become a
    // balloon alert. To add one, append a row to ProblemAlertDefs (name,
    // texture, short text, persistence), add its predicate to
    // ProblemAlertActive and drop the matching pictogram in Assets/UI.
    // Informative states (out of hours, looking for a company, operating)
    // never become alerts.
    sealed record ProblemAlertDef(string Name,string Texture,string Text,double PersistSeconds);
    static readonly ProblemAlertDef[] ProblemAlertDefs=
    [
        new("road","problem_road","Sem acesso viário",0),
        new("water","problem_water","Falta de água",ProblemPersistSeconds),
        new("power","problem_energy","Falta de energia",ProblemPersistSeconds),
        new("intake","problem_intake","Captação sem água por perto",0),
        new("noraw","problem_noraw","Tratamento sem água bruta",ProblemPersistSeconds),
        new("staff","problem_staff","Sem funcionários",ProblemStaffPersistSeconds),
        new("stock","problem_stock","Estoque vazio",ProblemStockPersistSeconds),
        new("maintenance","problem_maintenance","Manutenção pendente",0),
    ];
    sealed class ProblemAlert
    {
        public Node3D Anchor=null!;
        public readonly List<(Sprite3D Tile,Sprite3D Icon)> Icons=new();
        // Kinds currently drawn, in order; icons are pooled and reused.
        public readonly List<string> Shown=new();
    }
    readonly Dictionary<int,ProblemAlert> _problemAlerts=new();
    readonly Dictionary<int,Dictionary<string,double>> _problemPersistence=new();
    static readonly Dictionary<string,Texture2D?> ProblemTextures=new();
    const string ProblemTileTexture="problem_alert_tile";
    const double ProblemUpdateSeconds=.5;      // moderate refresh, never per frame
    const double ProblemPersistSeconds=20;     // water/power/raw must persist this long before alerting
    const double ProblemStaffPersistSeconds=300; // staff persists much longer, so shift handovers never flicker
    const double ProblemStockPersistSeconds=60;  // stock flickers on every sale/restock without a short delay
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
    static ProblemAlertDef ProblemKind(string name)=>ProblemAlertDefs.First(kind=>kind.Name==name);

    // Same conditions the simulation uses for the matching b.Cause, read
    // independently so several problems show side by side (the Cause chain
    // itself keeps only the first one). Workforce counts come from a single
    // snapshot per update, never from a scan per building.
    static bool ProblemAlertActive(string name,Building b,CityState state,CitySimulation simulation,Dictionary<int,(int Present,int OnShift,int Hired)> workforce)
    {
        switch(name){
            // "Sem acesso viário": same test the simulation uses for the cause
            // and for real delivery, crew and vehicle routes.
            case "road":return !simulation.HasExteriorAccess(b);
            // Lack of water, plus the "stopped after a persistent shortage"
            // tail: same family, kept while the outage counter is still at
            // the closure threshold after the supply returns. Houses never
            // carry that cause, so they only alert on a live shortage.
            case "water":return b.WaterDemand>0&&b.WaterRatio<.5
                ||b.Kind!=BuildingKind.House&&b.WaterOutageSeconds>=state.Tuning.WaterClosureHours*3600;
            case "power":return b.ElectricityDemand>0&&b.ElectricityRatio<.5&&b.Kind!=BuildingKind.WindPower;
            // "Captação sem água por perto": completed intake far from a viable
            // natural source, the same distance rule the simulation uses.
            case "intake":return b.Kind==BuildingKind.WaterIntake&&!simulation.HasNaturalWaterSource(b);
            // "Falta captação de água bruta": completed treatment with no raw
            // water available at all.
            case "noraw":return b.Kind==BuildingKind.WaterTreatment&&state.RawWaterCapacity<=0;
            // "Sem funcionários presentes no turno": nobody present during
            // opening hours. Utilities run shifts around the clock, so they
            // count as open; houses have no crew; a private shop still
            // looking for a company and a deliberately closed operation are
            // informative states, not staff alerts. Only an actionable
            // shortage alerts: someone is scheduled on shift right now but
            // nobody is present, or nobody is hired at all. A hired crew
            // between shifts (e.g. CityHall 15-19h) is normal, not a problem.
            case "staff":{
                if(b.Kind==BuildingKind.House)return false;
                if(!b.Definition.Utility&&!simulation.BuildingOpen(b))return false;
                if(b.OperationClosed||(b.Definition.Private&&b.CompanyId==null))return false;
                var crew=workforce.GetValueOrDefault(b.Id);
                return crew.Present==0&&(crew.OnShift>0||crew.Hired==0);
            }
            // "Estoque insuficiente": grocery or fuel station with a company,
            // open and really out of the product it sells.
            case "stock":return b.Kind is BuildingKind.Grocery or BuildingKind.FuelStation
                &&simulation.BuildingOpen(b)&&!b.OperationClosed&&b.CompanyId!=null
                &&b.Quantity(b.Kind==BuildingKind.Grocery?ResourceKind.Food:ResourceKind.Fuel)<.1;
            // "Manutenção pendente": municipal installation with reduced
            // capacity from overdue maintenance.
            case "maintenance":return !b.Definition.Private&&b.MaintenanceRatio<.9;
            default:return false;
        }
    }

    void UpdateProblemAlerts(CityState state,CitySimulation simulation,double elapsed)
    {
        var advanced=Math.Max(0,elapsed-_problemElapsedAt);_problemElapsedAt=elapsed;_problemClock+=advanced;
        if(!_problemDirty&&_problemClock<ProblemUpdateSeconds)return;
        var step=_problemClock;_problemClock=0;_problemDirty=false;
        var unit=BadgeUnit();
        // One workforce pass per update for every building, instead of one
        // citizen scan per building.
        var workforce=simulation.WorkforceByBuilding();
        foreach(var b in state.Buildings){
            if(!_problemAlerts.TryGetValue(b.Id,out var alert))continue;
            if(!_problemPersistence.TryGetValue(b.Id,out var accumulated)){accumulated=new();_problemPersistence[b.Id]=accumulated;}
            var shown=new List<string>();
            foreach(var def in ProblemAlertDefs){
                var active=ProblemAlertActive(def.Name,b,state,simulation,workforce);
                if(def.PersistSeconds<=0){
                    // Structural or slow conditions alert at once and clear at once.
                    if(active)shown.Add(def.Name);
                    continue;
                }
                // Asymmetric hysteresis: the shortage must persist before
                // alerting, but the icon leaves shortly after the problem
                // ends. The accumulated time is capped, so a long outage
                // never delays the recovery signal.
                var time=accumulated.GetValueOrDefault(def.Name);
                time=active?Math.Min(def.PersistSeconds,time+step):Math.Max(0,time-step*ProblemRecoverFactor);
                if(time<=0)accumulated.Remove(def.Name);else accumulated[def.Name]=time;
                if(active&&time>=def.PersistSeconds)shown.Add(def.Name);
            }
            if(accumulated.Count==0)_problemPersistence.Remove(b.Id);
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
    public IEnumerable<int> BuildingsWithProblemAlerts()
    {
        foreach(var pair in _problemAlerts)if(pair.Value.Shown.Count>0)yield return pair.Key;
    }
    // Screen position of a drawn alert balloon, for the hover tooltip. Null
    // when the building shows nothing or the anchor is behind the camera.
    public Vector2? AlertAnchorScreen(int buildingId,Camera3D camera)
    {
        if(!_problemAlerts.TryGetValue(buildingId,out var alert)||alert.Shown.Count==0)return null;
        var at=alert.Anchor.GlobalPosition;
        if(camera.IsPositionBehind(at))return null;
        return camera.UnprojectPosition(at);
    }
    public string BuildingProblemText(int buildingId)
    {
        if(!_problemAlerts.TryGetValue(buildingId,out var alert)||alert.Shown.Count==0)return "";
        return "Problemas: "+string.Join(" · ",alert.Shown.Select(name=>ProblemKind(name).Text));
    }
}
