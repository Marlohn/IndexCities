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
        var cell=_sim.State.Tuning.RoadStep/(_building!=null?2:1);
        _world.ShowPlacementGrid(_sim.State,_placementGridVisible,cell);
        if(_placementGridButton!=null){
            _placementGridButton.Text=_placementGridVisible?$"▦ Grade: {cell:0.#} m":"▦ Grade [G]";
            _placementGridButton.TooltipText=_building!=null
                ?$"Lotes: bordas alinhadas a {cell:0.#} m · R gira · G oculta a grade\nA prévia mostra o tamanho e o acesso reais."
                :$"Início da rua: ponto no centro da célula de {cell:0.#} m\nRua 1+1: {_sim.State.Tuning.RoadWidth:0.#} m com calçadas · G oculta a grade\nA prévia mostra toda a largura; a grade não indica terreno edificável.";
        }
    }
}
