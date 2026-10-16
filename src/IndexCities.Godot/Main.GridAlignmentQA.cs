using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void GridAlignmentSmoke()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/grid-alignment/"));
        var label=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--grid-capture-label="))?[21..]??"after";
        directory=System.IO.Path.Combine(directory,label);
        try {
            Require(DisplayServer.GetName()!="headless","Grade exige viewport renderizado");
            SetProcess(false);
            _speed=0;_autosaveMinutes=0;ClearTool();
            Require(_sim.PlaceRoad(new(-192,0),new(-192,48)).Success,"Rua de referência encomendada pela simulação");
            _world.Sync(_sim.State);_world.ClearPreview();
            _focus=new(-192,0,24);_zoom=66;_canvas.Visible=false;
            var before=_sim.SaveToJson();
            var measurements=new List<object>();
            object Bounds(MeshInstance3D instance) {
                var bounds=instance.GlobalTransform*instance.GetAabb();
                return new {Name=instance.Name.ToString(),Min=new[]{bounds.Position.X,bounds.Position.Y,bounds.Position.Z},Max=new[]{bounds.End.X,bounds.End.Y,bounds.End.Z}};
            }
            var work=_world.GetChildren().OfType<Node3D>().Single(n=>n.Name.ToString().StartsWith("RoadWork"));
            measurements.Add(new {Kind="road-work",Surfaces=RecoveryNodes<MeshInstance3D>(work).Select(Bounds).ToArray()});
            var samples=new List<object>();
            foreach(var yaw in new[]{0,45,180,225}) {
                _yaw=yaw;UpdateCamera();_world.ShowPlacementGrid(_sim.State,true,_sim.State.Tuning.RoadStep);
                var grid=_world.GetNode<MeshInstance3D>("PlacementGrid");
                var vertices=grid.Mesh!.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var cross=vertices.Single(v=>Math.Abs(v.X-(-192.12f))<.001f&&Math.Abs(v.Z-24)<.001f);
                var asphalt=RecoveryNodes<MeshInstance3D>(work).Min(m=>m.GetAabb().End.Y);
                var projected=_camera.UnprojectPosition(cross);
                var reference=_camera.UnprojectPosition(new(cross.X,asphalt,cross.Z));
                var residual=projected.DistanceTo(reference);
                var sidewalk=RecoveryNodes<MeshInstance3D>(work).Max(m=>m.GetAabb().Position.Y);
                var edgePixel=_camera.UnprojectPosition(new(-197.25f,sidewalk,24));
                var origin=_camera.ProjectRayOrigin(edgePixel);var ray=_camera.ProjectRayNormal(edgePixel);
                var hit=origin+ray*((cross.Y-origin.Y)/ray.Y);
                var gap=hit.X+198;
                samples.Add(new {Yaw=yaw,GridY=cross.Y,AsphaltY=asphalt,ResidualPixels=residual,ApparentLeftGapMetres=gap});
                if(label!="before"){
                    Require(residual<.03f,"Grade e obra concordam na projeção: "+residual+" px");
                    Require(Math.Abs(gap-.75f)<.002f,"Borda mantém folga real de 0,75 m em todas as rotações");
                }
                await RecoveryCapture(System.IO.Path.Combine(directory,$"road-work-yaw-{yaw}.png"),GetViewport());
                Require(_sim.SaveToJson()==before,"Rotação e grade preservam o estado lógico");
            }
            measurements.Add(new {Kind="projection",Samples=samples});
            var cachedGrid=_world.GetNode<MeshInstance3D>("PlacementGrid");
            _world.PreviewRoad(_sim.RoadShape(new(-192,0),new(-192,48)),true,_sim.State.Tuning.RoadWidth);
            var preview=RecoveryNodes<MeshInstance3D>(_world).Where(m=>m.MaterialOverride is StandardMaterial3D material&&material.NoDepthTest).ToArray();
            measurements.Add(new {Kind="road-preview",Meshes=preview.Select(Bounds).ToArray()});
            _world.ShowPlacementGrid(_sim.State,true,_sim.State.Tuning.RoadStep);
            Require(_world.GetNode<MeshInstance3D>("PlacementGrid")==cachedGrid,"Prévia móvel reutiliza a grade");
            _world.ShowPlacementGrid(_sim.State,false,_sim.State.Tuning.RoadStep);
            Require(!cachedGrid.Visible,"Ocultar grade preserva a malha");
            _world.ShowPlacementGrid(_sim.State,true,_sim.State.Tuning.RoadStep);
            Require(cachedGrid.Visible,"Reexibir grade reutiliza a malha");
            await RecoveryCapture(System.IO.Path.Combine(directory,"moving-preview.png"),GetViewport());
            _world.ClearPreview();
            await RecoveryAdvance(14400);_world.Sync(_sim.State);
            Require(_sim.State.Projects.Single().Status==WorkStatus.Completed,"Rua pronta por construção real");
            measurements.Add(new {Kind="finished-road",Surfaces=RecoveryNodes<MeshInstance3D>(_world.GetNode<Node3D>("ContinuousRoadSurface")).Select(Bounds).ToArray()});
            _world.ShowPlacementGrid(_sim.State,true,_sim.State.Tuning.RoadStep);
            if(label!="before"){
                var readyGrid=_world.GetNode<MeshInstance3D>("PlacementGrid").Mesh!.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                Require(readyGrid.Any(v=>Math.Abs(v.Y-.041f)<.0001f)&&readyGrid.Any(v=>Math.Abs(v.Y-.191f)<.0001f),"Grade projetada no asfalto e meio-fio prontos");
            }
            await RecoveryCapture(System.IO.Path.Combine(directory,"finished-road.png"),GetViewport());
            _world.PreviewBuilding(BuildingKind.CityHall,new(-177,24),0,true,true);
            _world.ShowPlacementGrid(_sim.State,true,2);
            var buildingPreview=RecoveryNodes<Node3D>(_world).Single(n=>n.Name=="BuildingPreview");
            measurements.Add(new {Kind="building-preview",Meshes=RecoveryNodes<MeshInstance3D>(buildingPreview).Select(Bounds).ToArray()});
            await RecoveryCapture(System.IO.Path.Combine(directory,"building-preview.png"),GetViewport());
            _world.ClearPreview();
            Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-172,24)).Success,"Lote real encomendado para medir canteiro");
            _world.Sync(_sim.State);
            measurements.Add(new {Kind="building-work",Meshes=RecoveryNodes<MeshInstance3D>(_world).Where(m=>m.Mesh is BoxMesh box&&box.Size.Y==.22f).Select(Bounds).ToArray()});
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"measurements.json"),System.Text.Json.JsonSerializer.Serialize(new {TerrainY=_world.VisibleTerrainHeight(new(-192,24)),CameraElevationDegrees=Math.Atan2(330,Math.Sqrt(300*300+300*300))*180/Math.PI,Measurements=measurements},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
            GD.Print("GRID_ALIGNMENT_SMOKE_OK "+directory);GetTree().Quit();
        } catch(Exception error) {GD.PrintErr("GRID_ALIGNMENT_SMOKE_FAIL "+error);GetTree().Quit(1);}
    }
}
