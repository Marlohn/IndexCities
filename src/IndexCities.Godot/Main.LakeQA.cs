using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void LakeRecoverySmoke()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/water-integrated/rendered-"+Guid.NewGuid().ToString("N")));
        System.IO.Directory.CreateDirectory(directory);
        try{
            Require(DisplayServer.GetName()!="headless","Lago exige Godot renderizado");_speed=0;_autosaveMinutes=0;
            CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"cities");ClearTool();
            foreach(var seed in new[]{42,1412,2877}){
                _sim=new CitySimulation(seed);var before=_sim.SaveToJson();_world.Reset(_sim.State);BuildCatalog();
                _focus=ToV(_sim.State.LakeCentre);_zoom=110;UpdateCamera();RefreshHud();
                var lake=(MeshInstance3D)_world.FindChild("LakeSurface",true,false);var mesh=(ArrayMesh)lake.Mesh;
                var arrays=mesh.SurfaceGetArrays(0);var vertices=arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();var indices=arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                Require(_sim.State.LakeBoundary!.All(p=>vertices.Any(v=>v.X==(float)p.X&&v.Z==(float)p.Z)),"Todo vértice salvo coincide com a água renderizada");
                var area=0.0;
                for(var i=0;i<indices.Length;i+=3){var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];var centre=(a+b+c)/3;
                    Require(LakeGeometry.SignedDistance(_sim.State,new(centre.X,centre.Z))<.0001,"Triângulo de água dentro do contorno real");
                    area+=Math.Abs((b.X-a.X)*(double)(c.Z-a.Z)-(b.Z-a.Z)*(double)(c.X-a.X))/2;
                }
                var boundary=_sim.State.LakeBoundary!;var polygonArea=Math.Abs(boundary.Select((p,i)=>p.X*boundary[(i+1)%boundary.Count].Z-p.Z*boundary[(i+1)%boundary.Count].X).Sum())/2;
                Require(Math.Abs(area-polygonArea)<.005,"Água cobre toda a área proibida, sem círculo oculto");
                foreach(var p in boundary.Where((_,i)=>i%8==0))foreach(var metres in new[]{-1.0,.1,1.0,4.0,10.0}){
                    var outward=p-_sim.State.LakeCentre;var point=p+outward*(metres/outward.Distance(default));
                    var surface=new Vector3((float)point.X,_world.VisibleTerrainHeight(point),(float)point.Z);
                    var picked=Ground(_camera.UnprojectPosition(surface));Require(picked.Distance(point)<.001,$"Picking coincide com malha: seed={seed} m={metres} target={point} hit={picked} distance={picked.Distance(point)}");
                }
                var material=(ShaderMaterial)lake.MaterialOverride;_world.Sync(_sim.State,1,1.25);Require(Math.Abs(material.GetShaderParameter("host_time_s").AsDouble()-1.25)<.00001,"Água recebe tempo decorativo");
                _world.Sync(_sim.State,1,0);Require(Math.Abs(material.GetShaderParameter("host_time_s").AsDouble()-1.25)<.00001,"Pausa mantém ondulação");
                Require(_sim.SaveToJson()==before,"Ambiente não altera cidade ou recursos");
                await RecoveryCapture(System.IO.Path.Combine(directory,$"lake-{seed}-day.png"),GetViewport());
                GD.Print($"LAKE_GEOMETRY seed={seed} vertices={boundary.Count} area={area:F3} build_ms={_world.LakeBuildMilliseconds:F2}");
            }
            // A real build sequence, with ordinary costs, crews and deliveries.
            _sim=new CitySimulation(42);_world.Reset(_sim.State);_cityId=CitySaves.NewId();
            Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Prefeitura real");
            var west=_sim.State.LakeBoundary!.MinBy(p=>p.X);var north=_sim.State.LakeBoundary!.MinBy(p=>p.Z);
            var house=new Point(Math.Floor((west.X-8-.1)/2)*2,Math.Round(west.Z/2)*2);
            var roadX=Math.Floor((house.X-14)/4)*4;var roadZ=Math.Round(house.Z/4)*4;
            var shoreZ=Math.Floor((north.Z-_sim.State.Tuning.RoadWidth/2-.05)/4)*4;var shoreX=Math.Round((north.X+16)/4)*4;
            Require(_sim.PlaceRoad(new(-192,0),new(roadX,roadZ)).Success,"Rua real até a casa da margem");
            Require(_sim.PlaceRoad(new(roadX,0),new(shoreX,shoreZ),true).Success,"Rua real junto à margem norte");
            var result=_sim.PlaceBuilding(BuildingKind.House,house,Math.PI/2);Require(result.Success,"Casa válida na margem: "+result.Message);
            var houseId=result.Id;_world.Sync(_sim.State);BuildCatalog();RefreshHud();
            _focus=ToV(_sim.State.LakeCentre);_zoom=150;UpdateCamera();await RecoveryCapture(System.IO.Path.Combine(directory,"shore-orders.png"),GetViewport());
            _sim.Advance(14400);_world.Sync(_sim.State);await ToSignal(GetTree().CreateTimer(1.25),SceneTreeTimer.SignalName.Timeout);RefreshHud();
            Require(_sim.State.Projects.All(p=>p.Status==WorkStatus.Completed),"Caminhões e equipes concluíram as obras, sem conclusão artificial");
            var building=_sim.State.Buildings.Single(b=>b.Id==houseId);Require(_sim.HasExteriorAccess(building),"Casa da margem tem acesso real");
            Require(_world.FindChild("ShoreFoundation",true,false)!=null,"Casa da margem possui apoio visual");
            _focus=ToV(house+new Point(8,0));_zoom=55;UpdateCamera();await RecoveryCapture(System.IO.Path.Combine(directory,"house-supported.png"),GetViewport());
            _focus=ToV(new(north.X,shoreZ+8));_zoom=55;UpdateCamera();await RecoveryCapture(System.IO.Path.Combine(directory,"road-supported.png"),GetViewport());
            _placementGridVisible=true;_world.ShowPlacementGrid(_sim.State,true,4);await RecoveryCapture(System.IO.Path.Combine(directory,"shore-grid.png"),GetViewport());
            _placementGridVisible=false;_world.ShowPlacementGrid(_sim.State,false,4);
            var saved=_sim.SaveToJson();SaveCity();LoadCity(_cityId);Require(_sim.SaveToJson()==saved,"Save/load do host preserva toda a cidade e geografia");
            var loaded=CitySimulation.FromJson(saved);_sim.Advance(1200);loaded.Advance(1200);Require(_sim.SaveToJson()==loaded.SaveToJson(),"Continuação após save/load é idêntica");
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"after-city.json"),_sim.SaveToJson());
            var legacyArg=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--legacy-water-fixture="));
            if(legacyArg!=null){
                _sim=CitySimulation.FromJson(System.IO.File.ReadAllText(legacyArg[23..]));var before=_sim.SaveToJson();
                var sources=_sim.State.Buildings.Select(b=>(b.Id,Source:_sim.HasNaturalWaterSource(b))).ToArray();
                _world.Reset(_sim.State);Require(_world.FindChild("LakeSurface",true,false) is MeshInstance3D{Mesh:CylinderMesh},"Partida antiga continua circular");
                SaveCity();LoadCity(_cityId);Require(_sim.SaveToJson()==before&&_sim.State.SchemaVersion==1,"Partida antiga salva sem conversão geográfica");
                Require(sources.All(s=>_sim.HasNaturalWaterSource(_sim.State.Buildings.Single(b=>b.Id==s.Id))==s.Source),"Captação antiga preservada");
                _focus=ToV(_sim.State.LakeCentre);_zoom=110;UpdateCamera();RefreshHud();await RecoveryCapture(System.IO.Path.Combine(directory,"legacy-preserved.png"),GetViewport());
            }
            GD.Print("INDEXCITIES_LAKE_RECOVERY_OK directory="+directory);GetTree().Quit();
        }catch(Exception ex){GD.PushError(ex.ToString());GetTree().Quit(1);}
    }
}
