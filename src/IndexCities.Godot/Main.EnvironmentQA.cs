using Godot;
namespace IndexCities.Game;

public partial class Main
{
    async void EnvironmentSmoke()
    {
        try {
            Require(DisplayServer.GetName()!="headless","Ambiente exige shader renderizado na GPU");
            _speed=0;_autosaveMinutes=0;var before=_sim.SaveToJson();
            var lake=(MeshInstance3D)_world.FindChild("LakeSurface",true,false);
            var material=(ShaderMaterial)lake.MaterialOverride;
            if(_sim.State.SchemaVersion==1){
                var cylinder=(CylinderMesh)lake.Mesh;
                Require(Math.Abs(cylinder.TopRadius-_sim.State.LakeRadius)<.00001&&Math.Abs(cylinder.Height-.15)<.00001,"Mapa antigo mantém raio e altura da malha existente");
                Require(Math.Abs(lake.Position.X-_sim.State.LakeCentre.X)<.00001&&Math.Abs(lake.Position.Z-_sim.State.LakeCentre.Z)<.00001,"Água antiga permanece sobre a fonte real");
            }else Require(lake.Mesh is ArrayMesh&&lake.Position==Vector3.Zero,"Lago novo usa a malha do contorno salvo, sem transformação");
            _world.Sync(_sim.State,1,1.25);Require(Math.Abs(material.GetShaderParameter("host_time_s").AsDouble()-1.25)<.00001,"Ondulação recebe tempo decorativo explícito");
            _world.Sync(_sim.State,1,0);Require(Math.Abs(material.GetShaderParameter("host_time_s").AsDouble()-1.25)<.00001,"Tempo zero da pausa mantém fase da água");
            Require(_sim.SaveToJson()==before&&_sim.State.Tuning.SpeedMultipliers.SequenceEqual(new double[]{0,1,15,1000}),"Materiais e ondulação não alteram simulação nem velocidades");
            _focus=ToV(_sim.State.LakeCentre);_zoom=110;UpdateCamera();
            var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/recovery-v2/environment"));
            await RecoveryCapture(System.IO.Path.Combine(directory,"lake-integrated.png"),GetViewport());
            _world.Reset(_sim.State);Require(_sim.SaveToJson()==before,"Reconstruir ambiente mantém cidade e recursos");
            _sim.CheckInvariants();
            GD.Print("INDEXCITIES_ENVIRONMENT_OK renderer=Compatibility geometry=preserved simulation=preserved pause=preserved speed=unchanged");GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
}
