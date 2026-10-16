using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    // Short explanation of the problem alerts under the cursor: building name
    // plus the very text the inspection shows. Presentation only, fed by the
    // same icon state; evaluated on mouse motion (and camera moves reusing
    // the last pointer), never per frame, reusing the selection raycast.
    PanelContainer _alertTooltip=null!;Label _alertTooltipLabel=null!;
    const float AlertBalloonHoverRadius=64;

    void BuildAlertTooltip()
    {
        _alertTooltip=Panel(_canvas,new(0,0),new(280,0));
        _alertTooltip.Visible=false;_alertTooltip.MouseFilter=Control.MouseFilterEnum.Ignore;
        _alertTooltipLabel=Text(_alertTooltip,"",15);_alertTooltipLabel.MouseFilter=Control.MouseFilterEnum.Ignore;
    }
    void HideAlertTooltip(){if(_alertTooltip!=null)_alertTooltip.Visible=false;}

    bool AlertTooltipAllowed()=>_modal==null&&_building==null&&!_roadTool&&!_demolitionTool&&_movingProject==null&&GetViewport().GuiGetHoveredControl()==null;

    int? AlertBalloonUnder(Vector2 screen)
    {
        int? best=null;var nearest=float.PositiveInfinity;
        foreach(var id in _world.BuildingsWithProblemAlerts()){
            var at=_world.AlertAnchorScreen(id,_camera);
            if(at is not {} point)continue;
            var distance=screen.DistanceTo(point);
            if(distance<AlertBalloonHoverRadius&&distance<nearest){nearest=distance;best=id;}
        }
        return best;
    }

    void UpdateAlertTooltip(Vector2 screen)
    {
        _pointerPixel=screen;
        if(_alertTooltip==null||!AlertTooltipAllowed()){HideAlertTooltip();return;}
        // Same picking the click selection uses; the balloon floats above the
        // entrance, so its screen neighbourhood counts as a hover too.
        var id=_world.PickBuilding(_camera.ProjectRayOrigin(screen),_camera.ProjectRayNormal(screen))??AlertBalloonUnder(screen);
        if(id is not {} bid||_sim.State.Buildings.FirstOrDefault(b=>b.Id==bid&&!b.Removed) is not {} b||_world.ProblemAlertIcons(bid)==""){HideAlertTooltip();return;}
        var text=b.Definition.Name+"\n"+_world.BuildingProblemText(bid);
        if(_alertTooltipLabel.Text!=text)_alertTooltipLabel.Text=text;
        var view=GetViewport().GetVisibleRect().Size;
        var at=screen+new Vector2(18,26);
        _alertTooltip.Position=new(Math.Max(8,Math.Min(at.X,view.X-320)),Math.Max(8,Math.Min(at.Y,view.Y-110)));
        _alertTooltip.Visible=true;
    }
}
