using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    MeshInstance3D? _placementGrid;
    (double Half,double Step) _placementGridGeometry;

    public void ShowPlacementGrid(CityState state,bool visible,double step)
    {
        var attached=_placementGrid!=null&&GodotObject.IsInstanceValid(_placementGrid)&&_placementGrid.GetParent()==this;
        if(!visible){if(attached)_placementGrid!.Visible=false;return;}
        var geometry=(state.Tuning.MapHalfSize,step);
        if(attached&&geometry==_placementGridGeometry){_placementGrid!.Visible=true;return;}
        if(attached){RemoveChild(_placementGrid!);_placementGrid!.QueueFree();}
        var half=state.Tuning.MapHalfSize;
        var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        // These are alignment lines, not a buildability or access overlay.
        // Construct once per spacing/map change; toggling only changes visibility.
        for(var coordinate=Math.Ceiling(-half/step)*step;coordinate<=half;coordinate+=step){
            mesh.SurfaceAddVertex(new((float)coordinate,.015f,(float)-half));
            mesh.SurfaceAddVertex(new((float)coordinate,.015f,(float)half));
            mesh.SurfaceAddVertex(new((float)-half,.015f,(float)coordinate));
            mesh.SurfaceAddVertex(new((float)half,.015f,(float)coordinate));
        }
        mesh.SurfaceEnd();
        var material=new StandardMaterial3D{AlbedoColor=new Color(.72f,.82f,.74f,.14f),
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded};
        _placementGrid=new MeshInstance3D{Name="PlacementGrid",Mesh=mesh,MaterialOverride=material,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(_placementGrid);_placementGridGeometry=geometry;
    }
}
