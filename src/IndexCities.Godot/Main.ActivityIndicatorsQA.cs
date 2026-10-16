using System.Text.Json;
using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    // Visual review of the real cargo badge and of citizens disappearing only
    // inside a real interior (IA-017). The presence balloon was removed on
    // 2026-10-15 and replaced by the problem alerts in Main.ProblemAlertsQA.cs.
    // Nothing is fabricated: counts are compared with the trucks the simulation
    // already has; the save is never written.
    async void ActivityIndicatorsSmoke()
    {
        var args=OS.GetCmdlineUserArgs();
        if(!QaPrecondition(args.Any(a=>a.StartsWith("--activity-indicators-save=")),"activity-indicators-smoke","uma cópia isolada indicada por --activity-indicators-save= (save sob artifacts/verification/activity-indicators)","-- --activity-indicators-save=<copia-isolada>"))return;
        var root=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/activity-indicators"));
        var phase=args.FirstOrDefault(a=>a.StartsWith("--activity-indicators-phase="))?[28..]??"after";
        var directory=System.IO.Path.Combine(root,phase+"-"+Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        try {
            Require(DisplayServer.GetName()!="headless","Indicadores exigem Godot renderizado");
            var source=System.IO.Path.GetFullPath(args.First(a=>a.StartsWith("--activity-indicators-save="))[27..]);
            Require(source.StartsWith(root+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Save de revisão é uma cópia isolada");
            _speed=0;_autosaveMinutes=0;_clockBudget=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"saves");
            _sim=CitySimulation.FromJson(JsonSerializer.Deserialize<CitySave>(System.IO.File.ReadAllText(source))!.CityJson);
            _world.Reset(_sim.State);BuildCatalog();ClearTool();ClearSelection();RefreshHud();
            var log=new List<string>();
            void Check(string moment) {
                var state=_sim.State;var before=_sim.SaveToJson();_world.Sync(state);
                Require(_sim.SaveToJson()==before,"Apresentação não altera o estado ("+moment+")");
                var walkers=state.Citizens.Where(c=>c.Alive&&c.InsideMap&&c.VehicleId==null).ToList();
                var inside=walkers.Select(c=>CityWorld.InteriorBuildingId(state,c)).Where(id=>id!=null).GroupBy(id=>id!.Value).ToDictionary(g=>g.Key,g=>g.Count());
                Require(_world.CitizenNodeCount==walkers.Count,"Um nó físico por SIM a pé ("+moment+")");
                Require(_world.VisibleCitizenCount==walkers.Count-inside.Values.Sum(),"Somente quem está num interior real some ("+moment+")");
                foreach(var b in state.Buildings.Where(b=>b.Completed&&!b.Removed)){
                    var shown=_world.ActivityIndicator(b.Id);
                    Require(shown.Visible==(shown.Cargo>0),"Indicador de carga oculto sem caminhão real no edifício "+b.Id+" ("+moment+")");
                    if(shown.Cargo>0)Require(state.Vehicles.Count(v=>v.Status==VehicleStatus.Unloading&&!v.PassengerCar&&v.ShipmentId is {} sid&&state.Shipments.Any(s=>s.Id==sid&&(s.PickedUp?s.DestinationBuildingId==b.Id||state.Projects.Any(p=>p.Id==s.ProjectId&&p.BuildingId==b.Id):s.SourceBuildingId==b.Id)))>=shown.Cargo,"Carga mostrada corresponde a caminhão real em operação no edifício "+b.Id);
                }
            }
            Check("load");
            var state=_sim.State;
            var interior=state.Citizens.Select(c=>CityWorld.InteriorBuildingId(state,c)).Where(id=>id!=null).GroupBy(id=>id!.Value).ToDictionary(g=>g.Key,g=>g.Count());
            var occupied=state.Buildings.Where(b=>b.Completed&&!b.Removed&&interior.ContainsKey(b.Id)).ToList();
            var house=occupied.Where(b=>b.Kind==BuildingKind.House).OrderByDescending(b=>interior[b.Id]).First();
            var tall=occupied.Where(b=>b.Kind!=BuildingKind.House).OrderByDescending(b=>_world.BuildingModelTop(b.Id)).First();
            var empty=state.Buildings.FirstOrDefault(b=>b.Completed&&!b.Removed&&!interior.ContainsKey(b.Id)&&b.Kind==BuildingKind.House);
            log.Add($"house={house.Id} people={interior[house.Id]} top={_world.BuildingModelTop(house.Id):0.0}");
            log.Add($"tall={tall.Id} kind={tall.Kind} people={interior[tall.Id]} top={_world.BuildingModelTop(tall.Id):0.0}");
            log.Add($"empty={(empty?.Id.ToString()??"none")} shown={(empty==null?"-":_world.ActivityIndicator(empty.Id).ToString())}");
            async Task Views(string name,Building target){
                foreach(var zoom in new[]{35f,80f,170f})foreach(var yaw in new[]{45f,135f,225f}){
                    _focus=ToV(target.Position);_zoom=zoom;_yaw=yaw;UpdateCamera();
                    await RecoveryCapture(System.IO.Path.Combine(directory,$"{name}-z{zoom:0}-y{yaw:0}.png"),GetViewport());
                }
            }
            await Views("house",house);await Views("tall",tall);
            if(empty!=null){_focus=ToV(empty.Position);_zoom=35;_yaw=45;UpdateCamera();await RecoveryCapture(System.IO.Path.Combine(directory,"empty-house.png"),GetViewport());}
            // Real cargo: advance the copy (never saved) until a truck is actually
            // unloading at a dock, checking every step for false positives.
            int? cargoBuilding=null;var travellingSeen=0;
            var trigger="natural";
            for(var step=0;step<2400&&cargoBuilding==null;step++){
                // If no delivery happens naturally within the first 20 minutes, make
                // the existing maintenance rule due now (as PresentationSmoke does);
                // the order, truck, cargo and travel are then all real simulation.
                if(step==600){var hall=_sim.State.Buildings.First(b=>b.Kind==BuildingKind.CityHall&&b.Completed&&!b.Removed);hall.MaintenanceDueAt=_sim.State.ElapsedSeconds-1;_sim.State.EconomyNextAt=_sim.State.ElapsedSeconds;trigger="maintenance-due-fixture building="+hall.Id;}
                _sim.Advance(2);Check("step "+step);
                travellingSeen+=_sim.State.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling&&v.ShipmentId!=null);
                cargoBuilding=_sim.State.Buildings.FirstOrDefault(b=>_world.ActivityIndicator(b.Id).Cargo>0)?.Id;
            }
            log.Add("cargo-trigger="+trigger);log.Add($"cargo-building={(cargoBuilding?.ToString()??"none")} travelling-observations={travellingSeen} at={_sim.State.Now:dd/MM HH:mm}");
            if(cargoBuilding is {} cargoId){
                var b=_sim.State.Buildings.Single(x=>x.Id==cargoId);log.Add("cargo-indicator="+_world.ActivityIndicator(cargoId));
                foreach(var zoom in new[]{35f,90f}){_focus=ToV(b.Position);_zoom=zoom;_yaw=45;UpdateCamera();await RecoveryCapture(System.IO.Path.Combine(directory,$"cargo-z{zoom:0}.png"),GetViewport());}
            }
            _focus=ToV(house.Position);_zoom=260;_yaw=45;UpdateCamera();await RecoveryCapture(System.IO.Path.Combine(directory,"overview-z260.png"),GetViewport());
            _sim.CheckInvariants();
            System.IO.File.WriteAllLines(System.IO.Path.Combine(directory,"log.txt"),log);
            GD.Print("INDEXCITIES_ACTIVITY_INDICATORS_OK phase="+phase+" "+string.Join(" | ",log)+" directory="+directory);
            GetTree().Quit();
        } catch(Exception error){System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"failure.txt"),error.ToString());GD.PushError(error.ToString());GetTree().Quit(1);}
        finally{CitySaves.VerificationDirectory=null;}
    }
}
