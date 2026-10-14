using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void CirculationSmoke()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/circulation/rendered-"+Guid.NewGuid().ToString("N")));
        try {
            Require(DisplayServer.GetName()!="headless","Circulação exige Godot renderizado");
            var pair=new[]{1139,1145,505}.Select(id=>_sim.State.Vehicles.Single(v=>v.Id==id)).ToArray();
            var blocked=pair.All(v=>v.Cause=="Fila na faixa");
            Require(blocked||pair.Take(2).All(v=>v.Status==VehicleStatus.Travelling&&v.RouteIndex==0),"Fixture real começa com a fila ou a saída original das vagas");
            var observed=blocked?pair:pair.Take(2).ToArray();
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"saves");
            var original=_sim.SaveToJson();var money=_sim.MonetaryTotal;
            var positions=observed.ToDictionary(v=>v.Id,v=>v.Position);var fuel=observed.ToDictionary(v=>v.Id,v=>v.Fuel);
            var road=_sim.State.Roads.Single(r=>r.Id==619);
            // CrossingNodeId is reserved after entering, so a person waiting
            // before the crossing has no reservation yet.
            var crossing=_sim.State.Citizens.Where(c=>c.Activity=="Aguardando travessia"&&c.Position.Distance(road.Position)<12).ToDictionary(c=>c.Id,c=>c.Position);
            Require(!blocked||crossing.Count>0,"Há pedestres reais esperando junto da travessia bloqueada");
            _focus=new(52,0,49);_zoom=65;UpdateCamera();_world.Reset(_sim.State);
            await RecoveryCapture(System.IO.Path.Combine(directory,"queue-before.png"),GetViewport());
            Require(_sim.SaveToJson()==original,"Captura inicial não altera a cidade");
            var bound=2*Math.Max(_sim.State.Tuning.VehicleSpeed,_sim.State.Tuning.HighwaySpeed);
            var minimumGap=double.MaxValue;
            for(var step=0;step<60;step++) {
                var previous=observed.ToDictionary(v=>v.Id,v=>v.Position);
                _world.CaptureMotionBeforeStep(_sim.State);_sim.Advance(2);_world.Sync(_sim.State);
                foreach(var vehicle in observed)Require(vehicle.Position.Distance(previous[vehicle.Id])<=bound+.000001,"Veículo avança dentro da velocidade física, sem salto");
                // Private lot spots are 4.6 m apart; the 8 m road queue applies
                // once both cars have joined the lane, not while still parked.
                if(pair.Take(2).All(v=>v.Status==VehicleStatus.Travelling&&v.RouteIndex>0)&&Math.Cos(pair[0].Heading-pair[1].Heading)>.999999){
                    minimumGap=Math.Min(minimumGap,pair[0].Position.Distance(pair[1].Position));
                    Require(minimumGap>=8-.000001,$"Carros mantêm a distância normal da fila no trecho reto após a entrada: passo={step} gap={minimumGap} A={pair[0].Position} index={pair[0].RouteIndex} B={pair[1].Position} index={pair[1].RouteIndex}");
                }
                await RecoveryFrame();
            }
            Require(minimumGap<double.MaxValue,"Distância entre carros foi conferida na faixa");
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"after-120s-city.json"),_sim.SaveToJson());
            foreach(var vehicle in observed) {
                Require(vehicle.Position.Distance(positions[vehicle.Id])>100,"Veículo completou deslocamento físico: "+vehicle.Id);
                Require(vehicle.Fuel<fuel[vehicle.Id],"Deslocamento consumiu combustível real: "+vehicle.Id);
            }
            Require(crossing.All(person=>_sim.State.Citizens.Single(c=>c.Id==person.Key).Position.Distance(person.Value)>10),"Pedestres retomaram deslocamento físico após a fila");
            Require(_sim.MonetaryTotal==money,"Circulação preserva oferta monetária");_sim.CheckInvariants();
            await RecoveryCapture(System.IO.Path.Combine(directory,"queue-after.png"),GetViewport());
            Require(TrySaveCity(),"Cópia de circulação salva em pasta isolada");
            var restored=CitySimulation.FromJson(CitySaves.Load(_cityId).CityJson);
            Require(restored.SaveToJson()==_sim.SaveToJson(),"Save/load mantém o estado completo");
            _sim.Advance(120);restored.Advance(120);
            Require(restored.SaveToJson()==_sim.SaveToJson(),"Continuação após save/load é idêntica");
            GD.Print("INDEXCITIES_CIRCULATION_OK scenario="+(blocked?"saved-queue":"original-lot-exit")+" vehicles="+observed.Length+" crossingPeople="+crossing.Count+" minimumLaneGap="+minimumGap+" quantum=2 physical=fuel-and-distance money="+money+" save-load=identical directory="+directory);GetTree().Quit();
        } catch(Exception exception) {GD.PushError(exception.ToString());GetTree().Quit(1);}
        finally {CitySaves.VerificationDirectory=null;}
    }
}
