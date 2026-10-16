using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void PlacementPerformanceSmoke()
    {
        try {
            SetProcess(false);_speed=0;_autosaveMinutes=0;ClearTool();
            _sim=new CitySimulation(42);_world.Reset(_sim.State);
            for(var turn=0;turn<4;turn++){
                var rotation=turn*Math.PI/2;var started=System.Diagnostics.Stopwatch.GetTimestamp();
                _world.ShowWaterIntakeRange(_sim,true,rotation);
                var call=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var maxCall=call;
                while(!_world.GetChildren().OfType<MeshInstance3D>().Any(n=>n.Name.ToString().StartsWith("WaterIntakeRange")&&n.Visible)){
                    Require(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds<30,"Raster termina");
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    var frame=System.Diagnostics.Stopwatch.GetTimestamp();_world.ShowWaterIntakeRange(_sim,true,rotation);
                    maxCall=Math.Max(maxCall,System.Diagnostics.Stopwatch.GetElapsedTime(frame).TotalMilliseconds);
                }
                var water=_world.GetChildren().OfType<MeshInstance3D>().Single(n=>n.Name.ToString().StartsWith("WaterIntakeRange")&&n.Visible);
                var points=water.Mesh!.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var waterHash=System.Security.Cryptography.SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(points.AsSpan()));
                GD.Print($"PLACEMENT_PERF water rotation={turn} call_ms={call:F3} max_main_ms={maxCall:F3} ready_ms={System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds:F3} sha256={Convert.ToHexString(waterHash)}");
                _world.HideWaterIntakeRange();
            }
            // A disposable presentation fixture: 240 physical road nodes, linked
            // in 12 rows. No construction, money or simulation is advanced here.
            var id=10000;
            for(var row=0;row<12;row++)for(var col=0;col<20;col++){
                var road=new RoadNode{Id=id++,Position=new(-180+col*8,-180+row*16)};
                if(col>0){road.Neighbours.Add(road.Id-1);_sim.State.Roads[^1].Neighbours.Add(road.Id);}
                _sim.State.Roads.Add(road);
            }
            _sim.State.RoadRevision++;_world.Sync(_sim.State);
            foreach(var step in new[]{8.0,2.0,8.0}){
                var started=System.Diagnostics.Stopwatch.GetTimestamp();_world.ShowPlacementGrid(_sim.State,true,step);
                var buildMs=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var mesh=_world.GetNode<MeshInstance3D>("PlacementGrid").Mesh!;
                var vertices=mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var hash=System.Security.Cryptography.SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(vertices.AsSpan()));
                GD.Print($"PLACEMENT_PERF grid roads={_sim.State.Roads.Count} step={step} build_ms={buildMs:F3} vertices={vertices.Length} sha256={Convert.ToHexString(hash)}");
            }
            GD.Print("PLACEMENT_PERFORMANCE_SMOKE_OK");GetTree().Quit();
        }catch(Exception error){GD.PushError(error.ToString());GetTree().Quit(1);}
    }
}
