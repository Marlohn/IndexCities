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
            Line(new(coordinate,-half),new(coordinate,half));
            Line(new(-half,coordinate),new(half,coordinate));
        }
        mesh.SurfaceEnd();
        var material=new StandardMaterial3D{AlbedoColor=new Color(.72f,.82f,.74f,.14f),
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded};
        _placementGrid=new MeshInstance3D{Name="PlacementGrid",Mesh=mesh,MaterialOverride=material,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(_placementGrid);_placementGridGeometry=geometry;
        void Line(Point a,Point b){
            var alongX=a.Z==b.Z;var fixedCoordinate=alongX?a.Z:a.X;
            if(state.SchemaVersion==1||fixedCoordinate<(alongX?_lakeWindow.Position.Y:_lakeWindow.Position.X)||fixedCoordinate>(alongX?_lakeWindow.End.Y:_lakeWindow.End.X)){Segment(a,b);return;}
            var start=Math.Max(-half,alongX?_lakeWindow.Position.X:_lakeWindow.Position.Y);var end=Math.Min(half,alongX?_lakeWindow.End.X:_lakeWindow.End.Y);
            Point At(double v)=>alongX?new(v,fixedCoordinate):new(fixedCoordinate,v);
            var prior=At(start);Segment(a,prior);
            for(var v=start+1;v<end;v++){var next=At(v);Segment(prior,next);prior=next;}Segment(prior,At(end));Segment(At(end),b);
        }
        void Segment(Point a,Point b){
            mesh.SurfaceAddVertex(P(a,state.SchemaVersion==1?.015f:VisibleTerrainHeight(a)+.075f));
            mesh.SurfaceAddVertex(P(b,state.SchemaVersion==1?.015f:VisibleTerrainHeight(b)+.075f));
        }
    }
}
