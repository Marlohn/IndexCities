using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    readonly record struct TerrainTriangle(Vector3 A,Vector3 B,Vector3 C);
    readonly Dictionary<(int,int),List<TerrainTriangle>> _terrainTriangles=new();
    Rect2 _lakeWindow;
    internal double LakeBuildMilliseconds {get;private set;}

    void BuildLakeEnvironment(CityState state)
    {
        var started=System.Diagnostics.Stopwatch.GetTimestamp();_terrainTriangles.Clear();_lakeWindow=default;
        _lakePresentationTime=0;
        if(state.SchemaVersion==1){
            var half=(float)state.Tuning.MapHalfSize;
            var ground=Box(_static,new(half*2,1,half*2),new(0,-.56f,0),"7d9b52");ground.MaterialOverride=GD.Load<Material>("res://Assets/RecoveryEnvironment/Materials/env_grass_soft.tres");
            _lakeMaterial=(ShaderMaterial)GD.Load<ShaderMaterial>("res://Assets/RecoveryEnvironment/Materials/env_lake_water.tres").Duplicate();
            _static.AddChild(new MeshInstance3D{Name="LakeSurface",Mesh=new CylinderMesh{TopRadius=(float)state.LakeRadius,BottomRadius=(float)state.LakeRadius+1,Height=.15f,RadialSegments=64},Position=P(state.LakeCentre,-.04f),MaterialOverride=_lakeMaterial});
        }else{
            using var generator=(GodotObject)GD.Load<GDScript>("res://Assets/RecoveryEnvironment/Lake/Source/ws_lake_d.gd").New();
            var boundary=state.LakeBoundary!.Select(p=>new Vector2((float)p.X,(float)p.Z)).ToArray();
            var parameters=new Godot.Collections.Dictionary{{"map_half",state.Tuning.MapHalfSize}};
            var result=generator.Call("build",new Vector2((float)state.LakeCentre.X,(float)state.LakeCentre.Z),boundary,parameters).AsGodotDictionary();
            if(!result["ok"].AsBool())throw new InvalidOperationException("Malha do lago: "+result["errors"]);
            var ground=result["ground"].As<ArrayMesh>();var bed=result["bed"].As<ArrayMesh>();var water=result["water"].As<ArrayMesh>();
            _static.AddChild(new MeshInstance3D{Name="LakeTerrain",Mesh=ground});
            _static.AddChild(new MeshInstance3D{Name="LakeBed",Mesh=bed});
            _lakeMaterial=(ShaderMaterial)water.SurfaceGetMaterial(0).Duplicate();
            _static.AddChild(new MeshInstance3D{Name="LakeSurface",Mesh=water,MaterialOverride=_lakeMaterial});
            IndexTerrain(ground,1);IndexTerrain(water,0);
            var outer=result["band_outer"].AsVector2Array();var min=new Vector2(outer.Min(v=>v.X),outer.Min(v=>v.Y));var max=new Vector2(outer.Max(v=>v.X),outer.Max(v=>v.Y));_lakeWindow=new Rect2(min,max-min).Grow(2);
        }
        _lakeMaterial.SetShaderParameter("host_time_s",0.0);LakeBuildMilliseconds=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    // Read the rendered triangles, rather than a slightly different analytic
    // height function. Cached spatial bins keep mouse picking local.
    void IndexTerrain(ArrayMesh mesh,int surface)
    {
        var arrays=mesh.SurfaceGetArrays(surface);var vertices=arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();var indices=arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
        for(var i=0;i<indices.Length;i+=3){
            var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];var triangle=new TerrainTriangle(a,b,c);
            for(var x=(int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X))/4);x<=Math.Floor(Math.Max(a.X,Math.Max(b.X,c.X))/4);x++)
                for(var z=(int)Math.Floor(Math.Min(a.Z,Math.Min(b.Z,c.Z))/4);z<=Math.Floor(Math.Max(a.Z,Math.Max(b.Z,c.Z))/4);z++){
                    if(!_terrainTriangles.TryGetValue((x,z),out var bin))_terrainTriangles[(x,z)]=bin=new();bin.Add(triangle);
                }
        }
    }

    internal float VisibleTerrainHeight(Point point)
    {
        if(_terrainTriangles.TryGetValue(((int)Math.Floor(point.X/4),(int)Math.Floor(point.Z/4)),out var triangles))
            foreach(var t in triangles){
                var denominator=(t.B.Z-t.C.Z)*(t.A.X-t.C.X)+(t.C.X-t.B.X)*(t.A.Z-t.C.Z);
                if(Math.Abs(denominator)<1e-10)continue;
                var a=((t.B.Z-t.C.Z)*(point.X-t.C.X)+(t.C.X-t.B.X)*(point.Z-t.C.Z))/denominator;
                var b=((t.C.Z-t.A.Z)*(point.X-t.C.X)+(t.A.X-t.C.X)*(point.Z-t.C.Z))/denominator;
                if(a>=-1e-6&&b>=-1e-6&&a+b<=1.000001)return (float)(a*t.A.Y+b*t.B.Y+(1-a-b)*t.C.Y);
            }
        return -.06f;
    }

    public Point GroundPoint(Vector3 origin,Vector3 direction)
    {
        var t=-origin.Y/direction.Y;var flat=origin+direction*t;
        if(_state.SchemaVersion==1)return new(flat.X,flat.Z);
        if(!_lakeWindow.Grow(2).HasPoint(new(flat.X,flat.Z))){var ground=origin+direction*((-.06f-origin.Y)/direction.Y);return new(ground.X,ground.Z);}
        // Every visible surface in this local basin lies between Y=0 and -0.9.
        // Search in ray order, then refine the first contact with the mesh.
        var end=(-.9f-origin.Y)/direction.Y;var prior=t;
        for(var i=1;i<=64;i++){
            var next=Mathf.Lerp(t,end,i/64f);var p=origin+direction*next;
            if(p.Y<=VisibleTerrainHeight(new(p.X,p.Z))){
                for(var j=0;j<14;j++){var mid=(prior+next)/2;var q=origin+direction*mid;if(q.Y>VisibleTerrainHeight(new(q.X,q.Z)))prior=mid;else next=mid;}
                var hit=origin+direction*((prior+next)/2);return new(hit.X,hit.Z);
            }
            prior=next;
        }
        return new(flat.X,flat.Z);
    }

    bool OnBank(Point centre,double width,double depth,double rotation=0)
    {
        if(_state.SchemaVersion==1)return false;
        foreach(var x in new[]{-width/2,0,width/2})foreach(var z in new[]{-depth/2,0,depth/2}){
            var p=centre+new Point(x*Math.Cos(rotation)+z*Math.Sin(rotation),-x*Math.Sin(rotation)+z*Math.Cos(rotation));
            if(VisibleTerrainHeight(p)<-.075)return true;
        }
        return false;
    }

    void BuildingBankBase(Node3D parent,Building b)
    {
        if(OnBank(b.Position,b.Definition.Width,b.Definition.Depth,b.Rotation)){
            var foundation=Box(parent,new((float)b.Definition.Width,.82f,(float)b.Definition.Depth),new(0,-.415f,0),"a6a08b");foundation.Name="ShoreFoundation";
        }
    }
}
