using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void InteriorArrivalSmoke()
    {
        if(!QaPrecondition(_sim.State.Citizens.Any(c=>c.Alive&&c.Resident&&c.InsideMap&&!c.Moving&&c.VehicleId==null&&c.BoardingVehicleId==null&&c.ConstructionProjectId==null&&c.Activity=="Em casa"&&_sim.State.Families.Any(f=>f.Id==c.FamilyId&&f.HomeId!=null)),"interior-arrival-smoke","uma cidade real com um morador em casa numa residência concluída","-- --fixture=<cidade-povoada>"))return;
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/circulation/interior-"+Guid.NewGuid().ToString("N")));
        System.IO.Directory.CreateDirectory(directory);
        try{
            Require(DisplayServer.GetName()!="headless","Verificação de interior exige renderização");
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"cities");
            var originalCount=_sim.State.Citizens.Count;var money=_sim.MonetaryTotal;
            var resident=_sim.State.Citizens.First(c=>c.Alive&&c.Resident&&c.InsideMap&&!c.Moving&&c.VehicleId==null&&c.BoardingVehicleId==null&&c.ConstructionProjectId==null&&c.Activity=="Em casa"&&_sim.State.Families.Any(f=>f.Id==c.FamilyId&&f.HomeId!=null));
            var family=_sim.State.Families.Single(f=>f.Id==resident.FamilyId);
            var home=_sim.State.Buildings.Single(b=>b.Id==family.HomeId&&!b.Removed&&b.Completed);
            // Explicit synthetic arrival fixture using ONE existing resident in
            // a copied real city. No people, vehicles, goods or money are created.
            resident.Position=home.Door+new Point(Math.Cos(home.Rotation),-Math.Sin(home.Rotation))*2.6;
            resident.Path=[home.Door];resident.PathIndex=0;resident.PathRoadRevision=_sim.State.RoadRevision;
            resident.DestinationId=home.Id;resident.Purpose=JourneyPurpose.Home;
            resident.Activity="Voltando para Casa";resident.NextDecisionAt=_sim.State.ElapsedSeconds+3600;
            _focus=ToV(home.Position);_zoom=45;UpdateCamera();_world.Reset(_sim.State);
            _world.CaptureMotionBeforeStep(_sim.State);_sim.Advance(2);
            Require(!resident.Moving&&resident.Activity=="Em casa"&&resident.Position.Distance(home.Door)<.001,"Deslocamento físico chegou à porta");
            _world.Sync(_sim.State,.5);
            Require(_world.VisibleCitizenPosition(resident.Id)!=null,"Interpolação intermediária ainda mostra a chegada");
            await RecoveryCapture(System.IO.Path.Combine(directory,"arrival-halfway.png"),GetViewport());
            _world.Sync(_sim.State,1);
            await RecoveryCapture(System.IO.Path.Combine(directory,"arrival-paused.png"),GetViewport());
            Require(_world.VisibleCitizenPosition(resident.Id)==null,"Fim da interpolação oculta morador realmente dentro, inclusive na pausa");
            for(var frame=0;frame<3;frame++){_world.Sync(_sim.State,1);Require(_world.VisibleCitizenPosition(resident.Id)==null,"Pausa conserva presença interior");}
            Require(_sim.State.Citizens.Count==originalCount&&_sim.MonetaryTotal==money,"Teste não fabrica população ou dinheiro");
            _sim.CheckInvariants();GD.Print("INDEXCITIES_INTERIOR_ARRIVAL_OK citizen="+resident.Id+" fixture=synthetic-arrival-existing-resident directory="+directory);GetTree().Quit();
        }catch(Exception error){System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"failure.txt"),error.ToString());GD.PushError(error.ToString());GetTree().Quit(1);}
    }
}
