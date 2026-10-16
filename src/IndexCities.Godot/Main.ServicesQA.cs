using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void ServicesSmoke()
    {
        if(!QaPrecondition(_sim.State.Buildings.Any(b=>b.Completed&&!b.Removed&&b.Definition.Utility),"services-smoke","uma cidade real com utilitários/serviços concluídos","-- --fixture=<cidade-com-servicos>"))return;
        var baseline=OS.GetCmdlineUserArgs().Contains("--services-before");
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/services-ui/"+(baseline?"before-":"after-")+Guid.NewGuid().ToString("N")));
        try{
            Require(DisplayServer.GetName()!="headless","Serviços exige Godot renderizado");
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"saves");
            var original=_sim.SaveToJson();ShowAlerts();await RecoveryFrame();
            await RecoveryCapture(System.IO.Path.Combine(directory,"services.png"),GetViewport());
            Require(_sim.SaveToJson()==original,"Abrir e capturar serviços não altera a cidade");
            if(!baseline){
                var window=_modal!;var scroll=RecoveryNodes<ScrollContainer>(window).Single();scroll.ScrollVertical=200;
                var services=_sim.State.Buildings.Where(b=>b.Completed&&!b.Removed&&b.Definition.Utility).ToArray();
                Require(services.Length>0,"Fixture contém serviços reais");
                void CheckLabels(){
                    foreach(var building in services){
                        var labels=RecoveryNodes<Label>(window).ToArray();
                        var capacity=labels.Single(l=>l.Name=="ServiceCapacity"+building.Id);
                        Require(capacity.Text.StartsWith(building.OperationalCapacity.ToString("N0",Pt)+" operacional",StringComparison.Ordinal),"Capacidade exibida acompanha o cálculo real");
                        var staff=labels.Single(l=>l.Name=="ServiceStaff"+building.Id);
                        var present=_sim.WorkersPresent(building);var scheduled=_sim.WorkersScheduled(building);
                        Require(staff.Text.StartsWith(present+(present==1?" presente":" presentes")+" · "+scheduled+(scheduled==1?" previsto agora":" previstos agora"),StringComparison.Ordinal),"Presença e escala são distintas e atualizadas");
                        if(building.Kind==BuildingKind.WaterIntake&&!_sim.HasNaturalWaterSource(building))Require(labels.Single(l=>l.Name=="ServiceFactors"+building.Id).Text.Contains("Fora da faixa de captação"),"Fonte inviável é explicada sem esconder equipe/energia");
                    }
                }
                CheckLabels();var expectedBefore=services.ToDictionary(b=>b.Id,b=>(_sim.WorkersScheduled(b),b.OperationalCapacity));
                _sim.Advance(1800);var advanced=_sim.SaveToJson();
                Require(services.Any(b=>expectedBefore[b.Id]!=(_sim.WorkersScheduled(b),b.OperationalCapacity)),"Condições mudaram por avanço normal da cidade");
                await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                Require(_modal==window&&scroll.ScrollVertical==200,"Mesmo painel e rolagem após atualização periódica");CheckLabels();
                Require(_sim.SaveToJson()==advanced,"Atualização visual não altera a simulação");
                await RecoveryCapture(System.IO.Path.Combine(directory,"services-live.png"),GetViewport());
                var target=services.First(b=>b.Kind==BuildingKind.WaterIntake&&!_sim.HasNaturalWaterSource(b));
                var inspect=RecoveryNodes<Button>(window).Single(b=>b.Text=="Inspecionar "+target.Definition.Name+" #"+target.Id);
                inspect.EmitSignal(Godot.Button.SignalName.Pressed);await RecoveryFrame();
                Require(_modal==null&&_selectedBuilding==target.Id,"Inspeção existente continua abrindo a instalação correta");
                Require(_sim.SaveToJson()==advanced,"Inspeção não muda partida");
                await RecoveryCapture(System.IO.Path.Combine(directory,"intake-inspection.png"),GetViewport());
                Require(TrySaveCity(),"Cópia salva em pasta isolada");var restored=CitySimulation.FromJson(CitySaves.Load(_cityId).CityJson);
                Require(restored.SaveToJson()==_sim.SaveToJson(),"Save/load preserva estado integral");
                _sim.Advance(120);restored.Advance(120);Require(restored.SaveToJson()==_sim.SaveToJson(),"Continuação é idêntica");_sim.CheckInvariants();
                // Explicitly controlled UI cases, separate from the real-city evidence above.
                // These states are never saved as a player's city or used as proof of service activity.
                var actualCity=_sim.SaveToJson();_sim=CitySimulation.FromJson(actualCity);
                var treatment=_sim.State.Buildings.First(b=>b.Completed&&!b.Removed&&b.Kind==BuildingKind.WaterTreatment&&_sim.HasExteriorAccess(b));
                var employees=_sim.State.Citizens.Where(c=>c.Alive&&c.Resident&&c.WorkplaceId==treatment.Id).Take(2).ToArray();
                Require(employees.Length==2,"Tratamento do caso controlado tem dois funcionários existentes");
                _sim.State.Now=new DateTime(2026,1,5,8,0,0);_sim.State.LakePollution=0;_sim.State.RawWaterCapacity=40;
                treatment.MaintenanceRatio=1;treatment.ElectricityRatio=1;
                foreach(var employee in employees){employee.Shift=0;employee.WeeklyDayOff=5;employee.Position=treatment.Door;employee.Path=[];employee.VehicleId=null;employee.ConstructionProjectId=null;}
                var controlled=_sim.SaveToJson();ShowAlerts();await RecoveryFrame();var controlledWindow=_modal!;
                var factors=RecoveryNodes<Label>(controlledWindow).Single(l=>l.Name=="ServiceFactors"+treatment.Id);
                Require(_sim.WaterTreatmentPotential(treatment)>40&&factors.Text.Contains("Captação do último cálculo (40) abaixo do potencial atual"),"UI explica água bruta menor que potencial do tratamento com as duas bases temporais explícitas");
                Require(_sim.SaveToJson()==controlled,"Diagnóstico controlado continua sem mutação");
                employees[0].VehicleId=_sim.State.Vehicles.First().Id;
                employees[1].Path=[treatment.Door,treatment.Door+new Point(20,0)];employees[1].PathIndex=0;
                controlled=_sim.SaveToJson();await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                Require(factors.Text.Contains("Em veículo #")&&factors.Text.Contains("Em deslocamento a pé"),"UI distingue estados reais que impedem presença, sem prometer chegada");
                Require(factors.Text.Contains("Sem funcionários no local agora: 0 de "+_sim.WorkersScheduled(treatment)+" previstos no turno presentes. A capacidade depende da chegada"),"Ausência no turno explica diretamente o limite de equipe");
                Require(_sim.SaveToJson()==controlled,"Estados de ausência são apenas lidos");
                var workforce=_sim.State.Citizens.Where(c=>c.Alive&&c.WorkplaceId==treatment.Id).ToArray();
                foreach(var employee in workforce)employee.WorkplaceId=null;
                controlled=_sim.SaveToJson();await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                Require(factors.Text.Contains("Ninguém contratado ainda: a instalação precisa de funcionários no local durante o turno para operar")&&!factors.Text.Contains("A capacidade depende da chegada"),"Sem contratados não sugere que há equipe a caminho");
                Require(_sim.SaveToJson()==controlled,"Diagnóstico sem contratados é apenas leitura");
                foreach(var employee in workforce)employee.WorkplaceId=treatment.Id;
                var shifts=workforce.Select(c=>c.Shift).ToArray();foreach(var employee in workforce)employee.Shift=1;
                controlled=_sim.SaveToJson();await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                Require(factors.Text.Contains("contratados, mas ninguém previsto na escala atual")&&!factors.Text.Contains("A capacidade depende da chegada"),"Fora da escala não sugere deslocamento no turno atual");
                Require(_sim.SaveToJson()==controlled,"Diagnóstico fora da escala é apenas leitura");
                for(var i=0;i<workforce.Length;i++)workforce[i].Shift=shifts[i];
                GD.Print("INDEXCITIES_SERVICES_STAFF_DIAGNOSIS_OK no-hires=true absent-on-shift=true off-shift=true read-only=true");
                var inspectButton=RecoveryNodes<Button>(controlledWindow).Single(b=>b.Text=="Inspecionar "+treatment.Definition.Name+" #"+treatment.Id);
                _sim.State.CityEvents.Add(new PersonalEvent(_sim.State.Now,"Evento sintético controlado para testar atualização do histórico"));
                await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                Require(GodotObject.IsInstanceValid(inspectButton)&&!inspectButton.IsQueuedForDeletion()&&inspectButton.GetParent()!=null,"Novo evento não recria nem libera os botões de serviço");
                var previousButtonId=inspectButton.GetInstanceId();treatment.Completed=false;
                await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                var replacement=RecoveryNodes<Button>(controlledWindow).Single(b=>b.Name=="InspectService"+treatment.Id);
                Require(replacement.GetInstanceId()!=previousButtonId,"Mudança estrutural foi exercitada no caso controlado");
                replacement.EmitSignal(Godot.Button.SignalName.Pressed);await RecoveryFrame();
                Require(_modal==null&&_selectedBuilding==treatment.Id,"Botão reconstruído ainda inspeciona a instalação correta");
                CloseModal();_sim=CitySimulation.FromJson(actualCity);
                GD.Print("INDEXCITIES_SERVICES_CONTROLLED_UI_OK raw-limit=true vehicle-and-walking=true event-keeps-button=true rebuilt-inspection=true synthetic=true");
                GD.Print("INDEXCITIES_SERVICES_UI_OK live=true scroll=200 inspection=true save-load=identical money="+_sim.MonetaryTotal);
            }
            GD.Print((baseline?"BASELINE: sem verificações ":"")+"INDEXCITIES_SERVICES_CAPTURE baseline="+baseline+" money="+_sim.MonetaryTotal+" directory="+directory);
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
        finally{CitySaves.VerificationDirectory=null;}
    }
}
