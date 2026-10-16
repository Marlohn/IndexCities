using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    bool _demolitionTool;
    Button? _demolitionButton;
    bool? _demolitionStyleState;
    Vector2? _rightPress;
    bool _rightDragging;
    float _rightTravel;
    Vector2 _rightLast;

    void ConfirmRoadDemolition(int roadId,RoadDemolitionQuote displayed,Node feedback)
    {
        var current=_sim.QuoteRoadDemolition(roadId);
        if(current.Allowed!=displayed.Allowed||current.Cost!=displayed.Cost||current.InterruptedWorks!=displayed.InterruptedWorks||current.AffectedVehicleRoutes!=displayed.AffectedVehicleRoutes||!current.IsolatedBuildingIds.Order().SequenceEqual(displayed.IsolatedBuildingIds.Order())) {
            ShowRoadDemolition(roadId,"Os efeitos mudaram enquanto a cidade avançava. Confira o orçamento atualizado antes de confirmar.");return;
        }
        var result=_sim.DemolishRoad(roadId);
        if(result.Success){ClearSelection();CloseModal();RefreshHud();}else Text(feedback,result.Message);
    }

    bool RightCameraInput(InputEvent input)
    {
        if(input is InputEventMouseButton { ButtonIndex:MouseButton.Right } button) {
            if(button.Pressed){_rightPress=button.Position;_rightLast=button.Position;_rightDragging=false;_rightTravel=0;return true;}
            if(_rightPress==null)return false;
            var dragged=_rightDragging;_rightPress=null;_rightDragging=false;
            if(!dragged){
                if(_roadTool&&_roadStart!=null){
                    _roadStart=null;_roadEnd=null;_roadFixed=false;_hoverPreview=false;_holdLoadedPreview=false;
                    _confirmation.Visible=false;_world.ClearPreview();RefreshToolHint();
                }else ClearTool();
            }return true;
        }
        if(input is InputEventMouseMotion motion&&_rightPress!=null) {
            // A delayed relative event can include travel before the button
            // press. Only actual positions after the press belong to this drag.
            var movement=motion.Position-_rightLast;_rightLast=motion.Position;
            _rightTravel+=movement.Length();
            if(_rightTravel>5)_rightDragging=true;
            if(_rightDragging){_holdLoadedPreview=false;_yaw+=movement.X*.3f;UpdateCamera();_pointerPixel=motion.Position;UpdatePreview();}
            return true;
        }
        return false;
    }

    RoadNode? RoadUnderPointer(Point position)=>_sim.State.Roads.Where(r=>r.Position.Distance(position)<=_sim.State.Tuning.RoadWidth/2+_sim.State.Tuning.RoadStep/2).MinBy(r=>r.Position.Distance(position));
    void ChooseDemolition()
    {
        ClearSelection();ClearTool();_demolitionTool=true;_inspector.Visible=false;RefreshToolHint();
    }
    void DemolishAt(Point position)
    {
        var road=RoadUnderPointer(position);
        if(road==null){SetStatus("Aponte um trecho de rua para ver o orçamento de remoção");return;}
        var quote=_sim.QuoteRoadDemolition(road.Id);
        if(!quote.Allowed){SetStatus(quote.Message);return;}
        ShowRoadDemolition(road.Id);
    }
}
