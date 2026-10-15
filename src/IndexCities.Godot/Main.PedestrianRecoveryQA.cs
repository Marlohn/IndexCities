using Godot;
using IndexCities.Simulation;
using System.Text.Json;
namespace IndexCities.Game;

public partial class Main
{
    async void PedestrianRecoverySmoke()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/pedestrian-recovery/rendered-"+Guid.NewGuid().ToString("N")));
        System.IO.Directory.CreateDirectory(directory);
        try {
            Require(DisplayServer.GetName()!="headless","Cenário pedonal exige Godot renderizado");
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"cities");
            var original=_sim.SaveToJson();var money=_sim.MonetaryTotal;
            var walkers=_sim.State.Citizens.Where(c=>c.Alive&&c.Activity.StartsWith("Sem rota",StringComparison.Ordinal)).ToArray();
            Require(walkers.Length==6,"Cópia original contém os seis pedestres sem rota");
            var origins=walkers.ToDictionary(c=>c.Id,c=>c.Position);
            var queries=new List<object>();var unavailable=0;
            foreach(var person in walkers) {
                var family=_sim.State.Families.FirstOrDefault(f=>f.Id==person.FamilyId);
                foreach(var target in new[]{(Name:"home",Id:family?.HomeId),(Name:"work",Id:person.WorkplaceId),(Name:"lastDestination",Id:person.DestinationId)}) {
                    var building=_sim.State.Buildings.FirstOrDefault(b=>b.Id==target.Id&&!b.Removed);
                    if(building==null)continue;
                    var route=_sim.PedestrianRoute(person.Position,building);if(route.Count==0)unavailable++;
                    queries.Add(new{person.Id,person.Activity,target=target.Name,building=building.Id,origin=person.Position,door=building.Door,points=route});
                }
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"route-queries.json"),JsonSerializer.Serialize(queries,new JsonSerializerOptions{WriteIndented=true}));
            Require(_sim.SaveToJson()==original,"Consultas de caminho não alteram a partida");
            _focus=ToV(walkers.First(c=>c.Id==1175).Position);_zoom=38;UpdateCamera();_world.Reset(_sim.State);
            ClearSelection();_selectedCitizen=1175;RefreshInspection();RefreshHud();
            await RecoveryCapture(System.IO.Path.Combine(directory,"original-position.png"),GetViewport());
            var before=OS.GetCmdlineUserArgs().Contains("--pedestrian-before");
            if(before) {
                GD.Print($"INDEXCITIES_PEDESTRIAN_BASELINE queries={queries.Count} unavailable={unavailable} citizens={walkers.Length} state=unchanged directory={directory}");GetTree().Quit();return;
            }
            Require(unavailable==0,"Destinos conectados possuem caminho pedonal físico");
            var furthest=walkers.ToDictionary(c=>c.Id,c=>0.0);var departureCaptured=false;
            for(var step=0;step<600;step++) {
                var previous=walkers.ToDictionary(c=>c.Id,c=>c.Position);
                _world.CaptureMotionBeforeStep(_sim.State);_sim.Advance(2);
                foreach(var person in walkers) {
                    if(person.VehicleId==null)Require(person.Position.Distance(previous[person.Id])<=_sim.State.Tuning.WalkingSpeed*2+.001,"Pedestre respeita velocidade física: "+person.Id);
                    furthest[person.Id]=Math.Max(furthest[person.Id],person.Position.Distance(origins[person.Id]));
                }
                if(step%5==0)_world.Sync(_sim.State);
                if(!departureCaptured&&furthest[1175]>1.5&&furthest[1175]<10) {
                    _world.Sync(_sim.State);RefreshInspection();
                    await RecoveryCapture(System.IO.Path.Combine(directory,"physical-departure.png"),GetViewport());departureCaptured=true;
                }
                if(step%30==0)await RecoveryFrame();
            }
            _world.Sync(_sim.State);RefreshHud();RefreshInspection();
            await RecoveryCapture(System.IO.Path.Combine(directory,"after-20-minutes.png"),GetViewport());
            _focus=ToV(walkers.First(c=>c.Id==1175).Position);_zoom=48;UpdateCamera();
            await RecoveryCapture(System.IO.Path.Combine(directory,"actual-destination.png"),GetViewport());
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"movement.json"),JsonSerializer.Serialize(walkers.Select(c=>new{c.Id,origin=origins[c.Id],c.Position,c.Activity,c.DestinationId,furthest=furthest[c.Id]}),new JsonSerializerOptions{WriteIndented=true}));
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"after-city.json"),_sim.SaveToJson());
            Require(furthest.All(pair=>pair.Value>10),"Os seis pedestres retomam deslocamento pelas rotinas normais");
            Require(walkers.All(c=>!c.Moving&&_sim.State.Buildings.Any(b=>b.Id==c.DestinationId&&!b.Removed&&c.Position.Distance(b.Door)<.001)),"Os seis pedestres chegam fisicamente à porta do destino");
            Require(_sim.MonetaryTotal==money,"Oferta monetária preservada");_sim.CheckInvariants();
            Require(TrySaveCity(),"Save isolado gravado");
            var restored=CitySimulation.FromJson(CitySaves.Load(_cityId).CityJson);
            Require(restored.SaveToJson()==_sim.SaveToJson(),"Save/load integral idêntico");
            _sim.Advance(120);restored.Advance(120);
            Require(restored.SaveToJson()==_sim.SaveToJson(),"Continuação de dois minutos idêntica após load");
            GD.Print($"INDEXCITIES_PEDESTRIAN_RECOVERY_OK queries={queries.Count} unavailable={unavailable} movingCitizens={furthest.Count(p=>p.Value>10)} minutes=20 save-load=identical directory={directory}");GetTree().Quit();
        }catch(Exception error){System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"failure.txt"),error.ToString());GD.PushError(error.ToString());GetTree().Quit(1);}
        finally {CitySaves.VerificationDirectory=null;}
    }
}
