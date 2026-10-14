using Godot;
namespace IndexCities.Game;

public partial class Main
{
    bool _placementGridVisible;
    Button? _placementGridButton;

    void TogglePlacementGrid()
    {
        _placementGridVisible=!_placementGridVisible;
        RefreshPlacementGrid();
    }

    void RefreshPlacementGrid()
    {
        _world.ShowPlacementGrid(_sim.State,_placementGridVisible,
            _sim.State.Tuning.RoadStep/(_building!=null?2:1));
        if(_placementGridButton!=null)
            _placementGridButton.Text=_placementGridVisible?"▦ Grade: ligada":"▦ Grade [G]";
    }
}
