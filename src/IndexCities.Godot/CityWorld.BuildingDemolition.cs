using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    // Destaque do prédio-alvo enquanto a prévia de demolição está aberta, no
    // mesmo tom da prévia de demolição de rua. Só apresentação.
    public void PreviewBuildingDemolition(Building building)
    {
        var key="demolish-building:"+building.Id;if(_previewKey==key)return;ClearPreview();_previewKey=key;
        var definition=building.Definition;
        var box=PreviewBox(_preview,new((float)definition.Width+1,9,(float)definition.Depth+1),P(building.Position,4.5f),PreviewMaterial("df8577",.32f));
        box.Rotation=new(0,(float)building.Rotation,0);box.Name="BuildingDemolitionPreview";PreviewValidity(_preview,P(building.Position,11),false);
    }
}
