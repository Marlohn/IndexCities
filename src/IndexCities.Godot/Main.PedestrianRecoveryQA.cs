using Godot;
using IndexCities.Simulation;
using System.Text.Json;
namespace IndexCities.Game;

public partial class Main
{
    async void PedestrianRecoverySmoke()
    {
        if(!QaPrecondition(_sim.State.Citizens.Count(c=>c.Alive&&c.Activity.StartsWith("Sem rota",StringComparison.Ordinal))==6&&_sim.State.Citizens.Any(c=>c.Id==1175),"pedestrian-recovery-smoke","uma cidade-checkpoint com exatamente 6 pedestres 'Sem rota' e o cidadão 1175","-- --fixture=<caminho-do-checkpoint>"))return;
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/pedestrian-recovery/rendered-"+Guid.NewGuid().ToString("N")));
        System.IO.Directory.CreateDirectory(directory);
        try {
            Require(DisplayServer.GetName()!="headless","Cenário pedonal exige Godot renderizado");
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"cities");
            var original=_sim.SaveToJson();var money=_sim.MonetaryTotal;
            var walkers=_sim.State.Citizens.Where(c=>c.Alive&&c.Activity.StartsWith("Sem rota",StringComparison.Ordinal)).ToArray();
            Require(walkers.Length==6,"Cópia original contém os seis pedestres sem rota");
            var origins=walkers.ToDictionary(c=>c.Id,c=>c.Position);
            var queries=new List<object>();var routes=new List<List<Point>>();var unavailable=0;
            foreach(var person in walkers) {
                var family=_sim.State.Families.FirstOrDefault(f=>f.Id==person.FamilyId);
                foreach(var target in new[]{(Name:"home",Id:family?.HomeId),(Name:"work",Id:person.WorkplaceId),(Name:"lastDestination",Id:person.DestinationId)}) {
                    var building=_sim.State.Buildings.FirstOrDefault(b=>b.Id==target.Id&&!b.Removed);
                    if(building==null)continue;
                    var route=_sim.PedestrianRoute(person.Position,building);if(route.Count==0)unavailable++;else routes.Add(route);
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
                GD.Print($"BASELINE: sem verificações INDEXCITIES_PEDESTRIAN_BASELINE queries={queries.Count} unavailable={unavailable} citizens={walkers.Length} state=unchanged directory={directory}");GetTree().Quit();return;
            }
            Require(unavailable==0,"Destinos conectados possuem caminho pedonal físico");
            // Every queried walk and every walk taken is checked against the
            // asphalt and paint drawn on screen, not the simulation's geometry.
            foreach(var route in routes){var exempt=RenderedOffCrossing(route[0]);var bad=RenderedCrossingViolation(route,ref exempt);Require(bad==null,"Consulta atravessa asfalto fora da faixa desenhada: "+bad);Require(ThroughBuilding(route) is null,"Consulta atravessa um prédio: "+ThroughBuilding(route));}
            // The oracle must reject the walk the independent review found (F1):
            // it crossed the street end at X=-192, beside the real paint.
            var reviewed=new List<Point>{new(-188,-4.5),new(-188,-4.55),new(-192,-4.55),new(-192,6.6),new(-232.55,6.6),new(-232.55,16),new(-232.375,17.25),new(-235,18)};
            var reviewedStart=RenderedOffCrossing(reviewed[0]);Require(RenderedCrossingViolation(reviewed,ref reviewedStart)!=null,"Oráculo desenhado reconhece a travessia F1 fora da faixa");
            var recovering=walkers.ToDictionary(c=>c.Id,c=>RenderedOffCrossing(c.Position));var checkedPaths=walkers.ToDictionary(c=>c.Id,_=>(List<Point>?)null);var walksChecked=0;
            var furthest=walkers.ToDictionary(c=>c.Id,c=>0.0);var departureCaptured=false;
            for(var step=0;step<600;step++) {
                var previous=walkers.ToDictionary(c=>c.Id,c=>c.Position);
                _world.CaptureMotionBeforeStep(_sim.State);_sim.Advance(2);
                foreach(var person in walkers) {
                    if(person.VehicleId==null)Require(person.Position.Distance(previous[person.Id])<=_sim.State.Tuning.WalkingSpeed*2+.001,"Pedestre respeita velocidade física: "+person.Id);
                    furthest[person.Id]=Math.Max(furthest[person.Id],person.Position.Distance(origins[person.Id]));
                    if(person.VehicleId!=null)continue;
                    if(person.Path.Count>0&&!ReferenceEquals(person.Path,checkedPaths[person.Id])){var exempt=recovering[person.Id];var bad=RenderedCrossingViolation(person.Path,ref exempt);Require(bad==null,"Caminho do pedestre "+person.Id+" atravessa fora da faixa desenhada: "+bad);Require(ThroughBuilding(person.Path) is null,"Caminho do pedestre "+person.Id+" atravessa um prédio: "+ThroughBuilding(person.Path));checkedPaths[person.Id]=person.Path;walksChecked++;}
                    var off=RenderedOffCrossing(person.Position);if(recovering[person.Id]&&!off)recovering[person.Id]=false;
                    Require(recovering[person.Id]||!off,"Pedestre "+person.Id+" sobre asfalto fora da faixa desenhada em "+person.Position);
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
            GD.Print($"INDEXCITIES_PEDESTRIAN_RECOVERY_OK queries={queries.Count} unavailable={unavailable} movingCitizens={furthest.Count(p=>p.Value>10)} walksCheckedAgainstPaint={walksChecked} minutes=20 save-load=identical directory={directory}");GetTree().Quit();
        }catch(Exception error){System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"failure.txt"),error.ToString());GD.PushError(error.ToString());GetTree().Quit(1);}
        finally {CitySaves.VerificationDirectory=null;}
    }

    // Drawn asphalt outside drawn paint. The highway has no paint; it is
    // accepted only where an urban road branches off it (as before).
    bool RenderedOffCrossing(Point p)
    {
        if(!_world.OnUnpaintedAsphalt(p))return false;
        var half=_sim.State.Tuning.RoadWidth/2;var asphalt=_sim.State.Tuning.RoadWidth*6.5/10.5;
        return !_sim.State.Roads.Any(r=>r.Highway&&Math.Abs(p.X-r.Position.X)<=half-.7+1.95&&Math.Abs(p.Z-r.Position.Z)<=asphalt/2&&r.Neighbours.Select(id=>_sim.State.Roads.First(o=>o.Id==id)).Any(o=>!o.Highway&&Math.Abs(o.Position.Z-r.Position.Z)>.1));
    }

    // A sample strictly inside a building footprint (5 cm inset); doors lie on
    // the edge, so walks may start or end there.
    string? ThroughBuilding(IReadOnlyList<Point> route)
    {
        foreach(var (a,b) in route.Zip(route.Skip(1))){
            var count=Math.Max(1,(int)Math.Ceiling(a.Distance(b)/.25));
            for(var k=0;k<=count;k++){var p=a+(b-a)*(k/(double)count);foreach(var lot in _sim.State.Buildings.Where(l=>!l.Removed)){var d=p-lot.Position;var x=d.X*Math.Cos(lot.Rotation)-d.Z*Math.Sin(lot.Rotation);var z=d.X*Math.Sin(lot.Rotation)+d.Z*Math.Cos(lot.Rotation);if(Math.Abs(x)<lot.Definition.Width/2-.05&&Math.Abs(z)<lot.Definition.Depth/2-.05)return $"{p} no prédio {lot.Id}";}}
        }
        return null;
    }

    // First segment with more than 5 cm off the crossings, sampled every 5 cm.
    // A walk that starts on asphalt (the stranded positions) is exempt until it
    // first leaves it.
    string? RenderedCrossingViolation(IReadOnlyList<Point> route,ref bool recovering)
    {
        for(var i=1;i<route.Count;i++){
            var a=route[i-1];var b=route[i];var count=Math.Max(1,(int)Math.Ceiling(a.Distance(b)/.05));var bad=0.0;
            for(var k=1;k<=count;k++){var p=a+(b-a)*((k-.5)/count);var off=RenderedOffCrossing(p);if(recovering){if(off)continue;recovering=false;}if(off)bad+=a.Distance(b)/count;}
            if(bad>.05)return $"{a} -> {b}: {bad:0.##} m";
        }
        return null;
    }
}
