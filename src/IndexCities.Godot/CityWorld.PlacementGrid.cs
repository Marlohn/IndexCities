using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    MeshInstance3D? _placementGrid;
    (double Half,double Step,int Revision) _placementGridGeometry;
    readonly List<(MeshInstance3D Mesh,Rect2[] Cells,float Top)> _placementGridSurfaces=new();

    public void ShowPlacementGrid(CityState state,bool visible,double step)
    {
        var attached=_placementGrid!=null&&GodotObject.IsInstanceValid(_placementGrid)&&_placementGrid.GetParent()==this;
        if(!visible){if(attached)_placementGrid!.Visible=false;return;}
        var geometry=(state.Tuning.MapHalfSize,step,state.RoadRevision);
        if(attached&&geometry==_placementGridGeometry){_placementGrid!.Visible=true;return;}
        if(attached){RemoveChild(_placementGrid!);_placementGrid!.QueueFree();}
        var half=state.Tuning.MapHalfSize;
        _placementGridSurfaces.RemoveAll(s=>!GodotObject.IsInstanceValid(s.Mesh)||!s.Mesh.IsInsideTree());
        var surfaces=_placementGridSurfaces.Where(s=>s.Mesh.IsVisibleInTree()).SelectMany(s=>s.Cells.Select(cell=>(Cell:cell,s.Top))).ToArray();
        var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        // These are alignment lines, not a buildability or access overlay.
        // Rebuild only for spacing, map or physical roads; moving a plan and
        // toggling visibility retain the same mesh.
        var interval=step;var major=state.Tuning.RoadStep*4;var offset=state.Tuning.RoadStep/2;
        // Road nodes remain unchanged. Put cell boundaries half a road step
        // away, so the existing nearest-node snap selects the hovered cell.
        // Building lines subdivide those same cells and match lot edges.
        for(var coordinate=Math.Ceiling((-half-offset)/interval)*interval+offset;coordinate<=half;coordinate+=interval){
            mesh.SurfaceSetColor(new Color(.72f,.82f,.74f,Math.Abs((coordinate-offset)%major)<.01?.16f:.1f));
            Line(new(coordinate,-half),new(coordinate,half));
            Line(new(-half,coordinate),new(half,coordinate));
        }
        if(step>=state.Tuning.RoadStep){
            mesh.SurfaceSetColor(new Color(.72f,.82f,.74f,.15f));
            for(var x=Math.Ceiling(-half/interval)*interval;x<=half;x+=interval)
                for(var z=Math.Ceiling(-half/interval)*interval;z<=half;z+=interval){
                    Segment(new(x-.12,z),new(x+.12,z));Segment(new(x,z-.12),new(x,z+.12));
                }
        }
        mesh.SurfaceEnd();
        var material=new StandardMaterial3D{AlbedoColor=Colors.White,VertexColorUseAsAlbedo=true,
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,RenderPriority=9};
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
            var alongX=a.Z==b.Z;var start=alongX?a.X:a.Z;var end=alongX?b.X:b.Z;
            var fixedCoordinate=alongX?a.Z:a.X;
            var patches=surfaces.Where(s=>fixedCoordinate>=(alongX?s.Cell.Position.Y:s.Cell.Position.X)&&fixedCoordinate<=(alongX?s.Cell.End.Y:s.Cell.End.X)
                &&(alongX?s.Cell.End.X:s.Cell.End.Y)>start&&(alongX?s.Cell.Position.X:s.Cell.Position.Y)<end).ToArray();
            var cuts=patches.SelectMany(s=>new[]{(double)(alongX?s.Cell.Position.X:s.Cell.Position.Y),(double)(alongX?s.Cell.End.X:s.Cell.End.Y)})
                .Where(v=>v>start&&v<end).Append(start).Append(end).Distinct().Order().ToArray();
            for(var i=1;i<cuts.Length;i++){
                var middle=(cuts[i-1]+cuts[i])/2;
                var top=patches.Where(s=>middle>=(alongX?s.Cell.Position.X:s.Cell.Position.Y)&&middle<=(alongX?s.Cell.End.X:s.Cell.End.Y)).Select(s=>(float?)s.Top).Max();
                Vertex(cuts[i-1],top);Vertex(cuts[i],top);
            }
            void Vertex(double value,float? top){
                var at=alongX?new Point(value,fixedCoordinate):new Point(fixedCoordinate,value);
                // Follow the actual surface in X/Z. A 1 mm separation only
                // prevents depth fighting; never lift an alignment plane.
                mesh.SurfaceAddVertex(P(at,(top??VisibleTerrainHeight(at))+.001f));
            }
        }
    }
}
