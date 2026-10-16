using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void ActivitySmoke()
    {
        if(!QaPrecondition(_sim.Population>0,"activity-smoke","uma cidade real povoada","-- --fixture=<cidade-povoada>"))return;
        var baseline=OS.GetCmdlineUserArgs().Contains("--activity-before");
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/activity-ui/"+(baseline?"before-":"after-")+Guid.NewGuid().ToString("N")));
        try {
            Require(DisplayServer.GetName()!="headless"&&_sim.Population>0,"Atividade exige cidade real povoada e Godot renderizado");
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"saves");
            var before=_sim.SaveToJson();ShowPeople();await RecoveryFrame();var window=_modal!;
            var scroll=RecoveryNodes<ScrollContainer>(window).Single();
            var original=_sim.State.Citizens.Where(c=>c.Alive).ToDictionary(c=>c.Id,c=>c.Activity);
            Citizen? changed=null;
            for(var step=0;step<300&&changed==null;step++) {
                _sim.Advance(2);
                changed=_sim.State.Citizens.FirstOrDefault(c=>c.Alive&&original.TryGetValue(c.Id,out var activity)&&c.Activity!=activity);
            }
            Require(changed!=null,"Uma atividade mudou por simulação normal em até 600 segundos");
            var person=changed!;var expected=person.Name+" · "+person.Activity;
            var old=person.Name+" · "+original[person.Id];
            var beforeRefresh=RecoveryNodes<Button>(window).First(b=>b.Text.StartsWith(person.Name+" · ",StringComparison.Ordinal));
            scroll.ScrollVertical=(int)(beforeRefresh.GlobalPosition.Y-scroll.GlobalPosition.Y+scroll.ScrollVertical);
            var offset=scroll.ScrollVertical;
            await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
            var current=RecoveryNodes<Button>(window).First(b=>b.Text.StartsWith(person.Name+" · ",StringComparison.Ordinal));
            var stale=current.Text!=expected&&current.Text!=expected+" · carteira "+Cash(_sim.Balance(person.Account));
            Require(_modal==window&&scroll.ScrollVertical==offset,"Atualização preserva modal e rolagem");
            if(!baseline)Require(!stale,"Pessoas acompanha a atividade real sem reabrir");
            RefreshHud();await RecoveryCapture(System.IO.Path.Combine(directory,"people.png"),GetViewport());
            GD.Print("INDEXCITIES_ACTIVITY_OBSERVATION person="+person.Id+" previous="+old+" actual="+expected+" displayed="+current.Text+" stale="+stale);
            var authoritative=_sim.SaveToJson();
            current.EmitSignal(Godot.Button.SignalName.Pressed);await RecoveryFrame();
            Require(_selectedCitizen==person.Id&&_modal==null,"A pessoa permanece inspecionável pela ação existente");
            Require(_sim.SaveToJson()==authoritative,"Inspeção não muda estado real");
            _focus=ToV(person.Position);_zoom=85;UpdateCamera();
            await RecoveryCapture(System.IO.Path.Combine(directory,"citizen.png"),GetViewport());
            Citizen? eventPerson=null;string? checkpoint=null;
            for(var step=0;step<900;step++) {
                var histories=_sim.State.Citizens.ToDictionary(c=>c.Id,c=>c.History.Count);
                var idBefore=_sim.State.NextId;var previousState=_sim.SaveToJson();
                _sim.Advance(2);
                if(idBefore==_sim.State.NextId)eventPerson=_sim.State.Citizens.FirstOrDefault(c=>c.Alive&&histories.TryGetValue(c.Id,out var count)&&c.History.Count>count);
                if(eventPerson!=null){checkpoint=previousState;break;}
            }
            Require(eventPerson!=null,"Evento pessoal real ocorreu sem mudança de IDs em até 1800 segundos");
            // Reload the real preceding checkpoint, inspect, then replay the same
            // two-second step. No citizen fields or events are manufactured.
            var eventId=eventPerson!.Id;_sim=CitySimulation.FromJson(checkpoint!);_inspectionKey="";
            person=_sim.State.Citizens.First(c=>c.Id==eventId);InspectCitizen(eventId);await RecoveryFrame();
            _sim.Advance(2);
            var latest=person.History.Last();await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
            var historyText=latest.At.ToString("dd/MM HH:mm")+" "+latest.Description;
            var staleHistory=!RecoveryNodes<Label>(_inspection).Any(l=>l.Text==historyText);
            if(!baseline)Require(!staleHistory,"Histórico acompanha evento real com inspeção aberta");
            authoritative=_sim.SaveToJson();RefreshInspection();await RecoveryFrame();
            Require(_sim.SaveToJson()==authoritative,"Atualização visual não altera simulação");
            await RecoveryCapture(System.IO.Path.Combine(directory,"citizen-history.png"),GetViewport());
            Require(TrySaveCity(),"Partida de teste salva no diretório isolado");
            Require(CitySimulation.FromJson(CitySaves.Load(_cityId).CityJson).SaveToJson()==_sim.SaveToJson(),"Save/load mantém o estado completo");
            ClearSelection();RefreshInspection();await RecoveryFrame();
            if(!baseline) {
                ShowPeople();await RecoveryFrame();var deathWindow=_modal!;
                var deathScroll=RecoveryNodes<ScrollContainer>(deathWindow).Single();deathScroll.ScrollVertical=200;
                var deathOffset=deathScroll.ScrollVertical;Require(deathOffset>0,"Reconstrução é exercitada com rolagem não zero");
                var dying=_sim.State.Citizens.First(c=>c.Alive&&c.Resident&&c.InsideMap);
                // Explicitly synthetic UI edge case, never gameplay evidence:
                // force imminent starvation only in this isolated test state.
                dying.Health=.000001;dying.HungerHours=100;
                _sim.State.Families.First(f=>f.Id==dying.FamilyId).Pantry=0;
                _sim.Advance(2);await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                Require(!dying.Alive,"Morte ocorreu pela rotina real de necessidades no caso sintético");
                Require(_modal==deathWindow&&deathScroll.ScrollVertical==deathOffset,"Reconstrução após morte preserva janela e rolagem");
                Require(!RecoveryNodes<Button>(deathWindow).Any(b=>b.Text.StartsWith(dying.Name+" · ",StringComparison.Ordinal)),"Pessoa morta deixa a lista");
                Require(_sim.MonetaryTotal==20000000,"Caso sintético não cria dinheiro");
                CloseModal();await RecoveryFrame();
                GD.Print("INDEXCITIES_ACTIVITY_DEATH_UI_OK synthetic=true scroll="+deathOffset+" menu=same removed=true");
                // A separate city exercises a new family appearing through real
                // construction and migration while the same menu stays open.
                _sim=new CitySimulation(42);_world.Reset(_sim.State);
                Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Fundação real do cenário de migração");
                Require(_sim.PlaceRoad(new(-192,0),new(-80,0)).Success,"Rua real do cenário de migração");
                _sim.Advance(7200);ShowPeople();await RecoveryFrame();var growingWindow=_modal!;
                Require(_sim.Population==0,"Cenário começa sem moradores");
                Require(_sim.PlaceBuilding(BuildingKind.House,new(-120,14)).Success,"Casa com orçamento e obra reais");
                _sim.Advance(7200);await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                var newcomer=_sim.State.Citizens.First(c=>c.Alive&&c.Resident&&c.InsideMap);
                Require(_modal==growingWindow&&RecoveryNodes<Button>(growingWindow).Any(b=>b.Text.StartsWith(newcomer.Name+" · ",StringComparison.Ordinal)),"Família migrante aparece no mesmo menu");
                Require(_sim.MonetaryTotal==20000000,"Cenário de migração preserva dinheiro existente");
                CloseModal();await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
                GD.Print("INDEXCITIES_ACTIVITY_MIGRATION_OK population="+_sim.Population+" menu=same closed=clean");
            }
            GD.Print((baseline?"BASELINE: sem verificações ":"")+"INDEXCITIES_ACTIVITY_"+(baseline?"BEFORE":"OK")+" stalePeople="+stale+" staleHistory="+staleHistory+" money="+_sim.MonetaryTotal+" directory="+directory);GetTree().Quit();
        } catch(Exception exception) { GD.PushError(exception.ToString());GetTree().Quit(1); }
        finally {CitySaves.VerificationDirectory=null;}
    }
}
