using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void WaterIntakeRangeSmoke()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/water-intake-range/rendered-"+Guid.NewGuid().ToString("N")));
        try{
            Require(DisplayServer.GetName()!="headless","Faixa exige Godot renderizado");_speed=0;_autosaveMinutes=0;
            foreach(var seed in new[]{42,1412,2877,1}){
                ClearTool();_sim=new CitySimulation(seed);
                if(seed==1){_sim.State.SchemaVersion=1;_sim.State.LakeBoundary=null;}
                Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Prefeitura encomendada por comando normal");
                _world.Reset(_sim.State);BuildCatalog();RefreshHud();Choose(BuildingKind.WaterIntake);
                _focus=ToV(_sim.State.LakeCentre);_zoom=150;UpdateCamera();
                var saved=_sim.SaveToJson();var firstBuild=_world.WaterIntakeRangeBuilds;var meshes=new List<ArrayMesh>();
                for(var turn=0;turn<4;turn++){
                    _rotation=turn*Math.PI/2;_manualRotation=true;var started=System.Diagnostics.Stopwatch.GetTimestamp();
                    _world.ShowWaterIntakeRange(_sim,true,_rotation);
                    while(!_world.GetChildren().OfType<MeshInstance3D>().Any(n=>n.Name.ToString().StartsWith("WaterIntakeRange")&&n.Visible)){
                        Require(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds<30,"Faixa fica pronta sem bloquear frames");
                        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);_world.ShowWaterIntakeRange(_sim,true,_rotation);
                    }
                    var range=_world.GetChildren().OfType<MeshInstance3D>().Single(n=>n.Name.ToString().StartsWith("WaterIntakeRange")&&n.Visible);
                    var mesh=(ArrayMesh)range.Mesh;meshes.Add(mesh);Require(mesh.GetSurfaceCount()==1,"Faixa não vazia");
                    var vertices=mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                    var shown=new HashSet<(int,int)>();var checkedPoints=0;
                    for(var i=0;i<vertices.Length;i+=6){
                        var a=vertices[i];var c=vertices[i+1];var b=vertices[i+2];var d=vertices[i+4];
                        shown.Add(((int)Math.Round(a.X/CityWorld.WaterIntakeRangeCellSize),(int)Math.Round(a.Z/CityWorld.WaterIntakeRangeCellSize)));
                        // Verify actual rendered triangle interiors, not just the sampler's inputs.
                        foreach(var v in new[]{a,b,c,d,(a+b+c)/3,(a+c+d)/3,(a+c)/2}){
                            var point=new Point(v.X,v.Z);Require(_sim.IsWaterIntakeSite(point,_rotation),"Ponto verde passa na geometria compartilhada");checkedPoints++;
                            if(i%600==0){var quote=_sim.QuoteBuilding(BuildingKind.WaterIntake,point,_rotation);
                                Require(quote.PhysicallyValid&&_sim.HasNaturalWaterSource(new Building{Kind=BuildingKind.WaterIntake,Position=point,Rotation=_rotation}),"Ponto verde passa no quote e tem fonte eficaz");}
                        }
                    }
                    var invalid=0;var valid=0;var cell=CityWorld.WaterIntakeRangeCellSize;var centre=_sim.State.LakeCentre;
                    // Cell centres outside the valid geography must never be painted.
                    for(var x=(int)Math.Floor(centre.X-90);x<centre.X+90;x++)for(var z=(int)Math.Floor(centre.Z-90);z<centre.Z+90;z++){
                        var point=new Point(x+.25,z+.25);var inside=shown.Contains(((int)Math.Floor(point.X/cell),(int)Math.Floor(point.Z/cell)));
                        if(!_sim.IsWaterIntakeSite(point,_rotation)){Require(!inside,"Ponto inválido fora da faixa");invalid++;}else valid++;
                    }
                    Require(invalid>0&&valid>0,"Amostra cobre interior e exterior");
                    _cursor=new(vertices[0].X,vertices[0].Z);_hoverPreview=true;_holdLoadedPreview=true;UpdatePreview();
                    if(seed==42&&turn<2)await RecoveryCapture(System.IO.Path.Combine(directory,$"intake-selected-rotation-{turn}.png"),GetViewport());
                    var builds=_world.WaterIntakeRangeBuilds;
                    for(var frame=0;frame<20;frame++)_world.ShowWaterIntakeRange(_sim,true,_rotation);
                    Require(_world.WaterIntakeRangeBuilds==builds,"Atualização não recalcula a malha");
                    GD.Print($"WATER_INTAKE_RANGE seed={seed} rotation={turn} cells={vertices.Length/6} inside_points={checkedPoints} outside_points={invalid} elapsed_ms={System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds:F1}");
                }
                Require(_world.WaterIntakeRangeBuilds==firstBuild+4,"Uma malha por rotação");
                _rotation=0;_world.ShowWaterIntakeRange(_sim,true,_rotation);
                Require(_world.GetChildren().OfType<MeshInstance3D>().Any(n=>n.Visible&&n.Mesh==meshes[0]),"Retorno reutiliza malha da primeira rotação");
                Choose(BuildingKind.House);
                Require(!_world.GetChildren().OfType<MeshInstance3D>().Any(n=>n.Name.ToString().StartsWith("WaterIntakeRange")&&n.Visible),"Troca de ferramenta remove a faixa imediatamente");
                Require(_sim.SaveToJson()==saved,"Orientação visual não altera a simulação");
            }
            GD.Print("INDEXCITIES_WATER_INTAKE_RANGE_OK directory="+directory);GetTree().Quit();
        }catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);}
    }
}
