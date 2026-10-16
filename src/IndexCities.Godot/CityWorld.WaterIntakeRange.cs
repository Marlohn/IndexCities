using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    // A fine, conservative raster of building CENTRES, not of the footprint.
    // Reset invalidates the cache when the saved geography changes.
    internal const double WaterIntakeRangeCellSize=.5;
    readonly Dictionary<double,MeshInstance3D> _waterIntakeRanges=new();
    MeshInstance3D? _waterIntakeRange;
    internal int WaterIntakeRangeBuilds {get;private set;}

    public void HideWaterIntakeRange(){if(_waterIntakeRange!=null)_waterIntakeRange.Visible=false;}

    public void ShowWaterIntakeRange(CitySimulation simulation,bool selected,double rotation)
    {
        if(!selected){HideWaterIntakeRange();return;}
        if(!_waterIntakeRanges.TryGetValue(rotation,out var range)){
            range=BuildWaterIntakeRange(simulation,rotation);_waterIntakeRanges.Add(rotation,range);AddChild(range);WaterIntakeRangeBuilds++;
        }
        if(_waterIntakeRange!=range)HideWaterIntakeRange();
        _waterIntakeRange=range;range.Visible=true;
    }

    MeshInstance3D BuildWaterIntakeRange(CitySimulation simulation,double rotation)
    {
        var state=simulation.State;var reach=CitySimulation.NaturalWaterSourceReach;var cell=WaterIntakeRangeCellSize;
        var boundary=state.LakeBoundary;
        var minX=(int)Math.Floor(((boundary?.Min(p=>p.X)??state.LakeCentre.X-state.LakeRadius)-reach)/cell);
        var maxX=(int)Math.Ceiling(((boundary?.Max(p=>p.X)??state.LakeCentre.X+state.LakeRadius)+reach)/cell);
        var minZ=(int)Math.Floor(((boundary?.Min(p=>p.Z)??state.LakeCentre.Z-state.LakeRadius)-reach)/cell);
        var maxZ=(int)Math.Ceiling(((boundary?.Max(p=>p.Z)??state.LakeCentre.Z+state.LakeRadius)+reach)/cell);
        var valid=new bool[maxX-minX+1,maxZ-minZ+1];
        for(var x=minX;x<=maxX;x++)for(var z=minZ;z<=maxZ;z++)
            valid[x-minX,z-minZ]=simulation.IsWaterIntakeSite(new(x*cell,z*cell),rotation);
        var vertices=new List<Vector3>();
        for(var x=minX;x<maxX;x++)for(var z=minZ;z<maxZ;z++){
            if(!valid[x-minX,z-minZ]||!valid[x-minX+1,z-minZ]||!valid[x-minX,z-minZ+1]||!valid[x-minX+1,z-minZ+1]
                ||!simulation.IsWaterIntakeSite(new((x+.5)*cell,(z+.5)*cell),rotation))continue;
            var a=Vertex(x,z);var b=Vertex(x+1,z);var c=Vertex(x+1,z+1);var d=Vertex(x,z+1);
            vertices.AddRange([a,c,b,a,d,c]);
        }
        var mesh=new ArrayMesh();
        if(vertices.Count>0){var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=vertices.ToArray();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);}
        return new MeshInstance3D{Name="WaterIntakeRange"+_waterIntakeRanges.Count,Mesh=mesh,Visible=false,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,MaterialOverride=new StandardMaterial3D{
                AlbedoColor=new Color("45df73",.42f),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency=BaseMaterial3D.TransparencyEnum.Alpha,CullMode=BaseMaterial3D.CullModeEnum.Disabled}};
        Vector3 Vertex(int x,int z){var p=new Point(x*cell,z*cell);return P(p,VisibleTerrainHeight(p)+.045f);}
    }
}
