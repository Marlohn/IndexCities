using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    string? ProblemAlertDirectory;

    // Visual review of the problem alerts over completed buildings (owner
    // decision, 2026-10-15, extended with 5 more causes in the same session).
    // Three kinds of evidence, never mixed:
    //  * controlled scenario — exact per-building state set on a city copy that
    //    is never saved, to prove which icon belongs to which real state and
    //    that every icon disappears once the state is fixed;
    //  * new causes scenario — a city copy (never saved) with a dry intake, a
    //    treatment without raw water, a shop with a company but no stock and
    //    municipal buildings without staff, proving each new icon, its
    //    persistence, its night suppression and its clearing;
    //  * real scenario — a city without potable infrastructure really alerts
    //    for water, and the alert really ends after real intake and treatment
    //    buildings are built and start delivering water.
    async void ProblemAlertsSmoke()
    {
        var root=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/problem-alerts"));
        ProblemAlertDirectory=System.IO.Path.Combine(root,"smoke-"+Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(ProblemAlertDirectory);
        var log=new List<string>();
        try{
            Require(DisplayServer.GetName()!="headless","Alertas de problema exigem Godot renderizado");
            _speed=0;_autosaveMinutes=0;_clockBudget=0;
            CitySaves.VerificationDirectory=System.IO.Path.Combine(ProblemAlertDirectory,"saves");
            await ProblemAlertsControlled(log);
            await ProblemAlertsNewCauses(log);
            await ProblemAlertsRealWater(log);
            System.IO.File.WriteAllLines(System.IO.Path.Combine(ProblemAlertDirectory,"log.txt"),log);
            GD.Print("PROBLEM_ALERTS_QA ok "+string.Join(" | ",log)+" directory="+ProblemAlertDirectory);
            GetTree().Quit();
        }catch(Exception error){
            System.IO.File.WriteAllLines(System.IO.Path.Combine(ProblemAlertDirectory!,"log.txt"),log);
            System.IO.File.WriteAllText(System.IO.Path.Combine(ProblemAlertDirectory!,"failure.txt"),error.ToString());
            GD.PushError(error.ToString());GetTree().Quit(1);
        }finally{CitySaves.VerificationDirectory=null;}
    }

    // Four completed houses side by side: one loses its street, one receives
    // too little water, one too little electricity and one stays healthy.
    async Task ProblemAlertsControlled(List<string> log)
    {
        _sim=new CitySimulation(77);
        Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Prefeitura do cenário controlado");
        Require(_sim.PlaceRoad(new(-192,0),new(100,0)).Success,"Rua principal do cenário controlado");
        Require(_sim.PlaceRoad(new(-120,0),new(-120,24)).Success,"Rua do lote que será isolado");
        var works=new List<int>();
        foreach(var at in new[]{new Point(-120,38),new Point(-96,14),new Point(-72,14),new Point(-48,14)}){
            var placed=_sim.PlaceBuilding(BuildingKind.House,at);
            Require(placed.Success,"Casa em "+at+": "+placed.Message);
            works.Add(_sim.State.Buildings.Single(b=>b.Id==placed.Id!.Value).ProjectId);
        }
        await ProblemWaitWorks(works);
        _world.Reset(_sim.State);BuildCatalog();ClearTool();ClearSelection();
        var lots=_sim.State.Buildings.Where(b=>b.Kind==BuildingKind.House).OrderBy(b=>b.Position.X).ToArray();
        var stranded=lots[0];var thirsty=lots[1];var dark=lots[2];var healthy=lots[3];
        _focus=ToV(new(-84,24));_zoom=100;_yaw=45;UpdateCamera();
        // Controlled per-building state on this QA copy (never saved): the
        // simulation recomputes demand and ratios from real capacity every tick,
        // so the forced values are re-applied after each step, like the
        // controlled cases in ServicesSmoke. The healthy baseline makes every
        // lot problem-free, so each icon has exactly one cause.
        void ForceBaseline(){foreach(var b in _sim.State.Buildings.Where(b=>b.Completed&&!b.Removed)){b.WaterDemand=Math.Max(b.WaterDemand,1);b.WaterRatio=1;b.ElectricityDemand=Math.Max(b.ElectricityDemand,1);b.ElectricityRatio=1;}}
        void ForceProblems(){ForceBaseline();thirsty.WaterRatio=.2;dark.ElectricityRatio=.1;}
        log.Add($"controlled stranded={stranded.Id} water={thirsty.Id} power={dark.Id} healthy={healthy.Id}");
        // 1) Healthy city: nothing is drawn, including on the lot about to be cut.
        for(var step=0;step<3;step++){_sim.Advance(60);ForceBaseline();await ProblemPresent(null);}
        await ProblemPresent("controlled-initial");
        Require(string.Join(" ",lots.Select(b=>_world.ProblemAlertIcons(b.Id))).Trim()=="","Cidade em ordem não mostra alerta: "+string.Join(" ",lots.Select(b=>b.Id+"="+(_world.ProblemAlertIcons(b.Id)==""?"clean":_world.ProblemAlertIcons(b.Id)))));
        // 2) One street really cut, one lot receiving too little water and one
        //    too little electricity; the healthy lot keeps showing nothing.
        foreach(var id in _sim.State.Roads.Where(r=>r.Position.X==-120&&r.Position.Z is 20 or 24).Select(r=>r.Id).OrderDescending())
            Require(_sim.DemolishRoad(id).Success,"Corte real da rua do lote "+stranded.Id);
        for(var step=0;step<3;step++){_sim.Advance(60);ForceProblems();await ProblemPresent(null);}
        Require(_world.ProblemAlertIcons(stranded.Id)=="road","Sem acesso viário alerta sem espera: "+_world.ProblemAlertIcons(stranded.Id));
        Require(_world.ProblemAlertIcons(thirsty.Id)=="water","Falta de água alerta depois de persistir: "+_world.ProblemAlertIcons(thirsty.Id));
        Require(_world.ProblemAlertIcons(dark.Id)=="power","Falta de energia alerta depois de persistir: "+_world.ProblemAlertIcons(dark.Id));
        Require(_world.ProblemAlertIcons(healthy.Id)=="","Casa sem problema não mostra nada: "+_world.ProblemAlertIcons(healthy.Id));
        log.Add($"PROBLEM_ALERTS_QA ok building={stranded.Id} alerts={_world.ProblemAlertIcons(stranded.Id)}");
        log.Add($"PROBLEM_ALERTS_QA ok building={thirsty.Id} alerts={_world.ProblemAlertIcons(thirsty.Id)}");
        log.Add($"PROBLEM_ALERTS_QA ok building={dark.Id} alerts={_world.ProblemAlertIcons(dark.Id)}");
        log.Add($"PROBLEM_ALERTS_QA ok building={healthy.Id} alerts={_world.ProblemAlertIcons(healthy.Id)}");
        await ProblemPresent("controlled-alerts");
        _focus=ToV(thirsty.Position);_zoom=45;_yaw=45;UpdateCamera();await ProblemPresent("controlled-alerts-detail");
        // The inspection explanation must match the drawn icon exactly.
        ClearSelection();_selectedBuilding=thirsty.Id;RefreshInspection();await RecoveryFrame();
        Require(RecoveryNodes<Label>(_inspection).Any(label=>label.Text=="Problemas: Falta de água"),"Inspeção acusa o mesmo problema do ícone");
        await RecoveryCapture(System.IO.Path.Combine(ProblemAlertDirectory!,"controlled-inspection.png"),GetViewport());
        log.Add("controlled inspection problems=water");

        // Same real street reconnected and the same ratios restored: everything
        // must clear, proving the icon follows the state and never sticks.
        var reconnect=_sim.PlaceRoad(new(-120,20),new(-120,24));
        Require(reconnect.Success,"Reconexão real da rua: "+reconnect.Message);
        await ProblemWaitWorks([reconnect.Id!.Value]);
        for(var step=0;step<3;step++){_sim.Advance(60);ForceProblems();thirsty.WaterRatio=1;dark.ElectricityRatio=1;await ProblemPresent(null);}
        Require(_world.ProblemAlertIcons(stranded.Id)=="","Alerta de acesso some com a rua reconectada: "+_world.ProblemAlertIcons(stranded.Id));
        Require(_world.ProblemAlertIcons(thirsty.Id)=="","Alerta de água some com o abastecimento: "+_world.ProblemAlertIcons(thirsty.Id));
        Require(_world.ProblemAlertIcons(dark.Id)=="","Alerta de energia some com o fornecimento: "+_world.ProblemAlertIcons(dark.Id));
        await ProblemPresent("controlled-cleared");
        ClearSelection();_selectedBuilding=thirsty.Id;RefreshInspection();await RecoveryFrame();
        Require(!RecoveryNodes<Label>(_inspection).Any(label=>label.Text.Contains("Problema")),"Inspeção não acusa problema onde nada é exibido");
        log.Add("controlled cleared=all inspection-consistent");
    }

    // New problem causes (owner extension, 2026-10-15): a dry intake, a
    // treatment without raw water, municipal buildings without staff, a shop
    // with a company but no stock, and pending municipal maintenance. All on
    // a city copy that is never saved. The empty city has no citizens, so no
    // worker is ever present; water/power demands stay at zero and only the
    // new icons can appear besides the forced ones.
    async Task ProblemAlertsNewCauses(List<string> log)
    {
        _sim=new CitySimulation(91);
        Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Prefeitura do cenário de novas causas");
        // The lake moves with the seed: the main road must reach the shore
        // column, or the shore road would start disconnected and never
        // receive materials.
        var lake=_sim.State.LakeCentre;
        var shoreZ=Math.Round((lake.Z-_sim.State.LakeRadius-18)/4)*4;
        var intakeX=Math.Round(lake.X/4)*4;
        var mainRoad=_sim.PlaceRoad(new(Math.Min(-192,intakeX-24),0),new(Math.Min(248,Math.Max(100,intakeX+24)),0));
        Require(mainRoad.Success,"Rua principal do cenário de novas causas: "+mainRoad.Message);
        await ProblemWaitWorks([mainRoad.Id!.Value]);
        // Shore road first, so the wet intake (negative control) and any hint
        // near the lake adapt to this seed; the dry intake search then avoids
        // that column and any natural source. It reaches shoreZ-10 (still
        // clear of the water) so a viable intake spot is both near the water
        // and within reach of the last street node.
        var shoreRoad=_sim.PlaceRoad(new(intakeX,0),new(intakeX,shoreZ-10));
        Require(shoreRoad.Success,"Rua até a margem no cenário de novas causas: "+shoreRoad.Message);
        var treatment=ProblemPlaceAccessible(BuildingKind.WaterTreatment,
            [new(intakeX+40,20),new(intakeX-40,20),new(intakeX+80,20),new(intakeX-80,20),new(0,20)]);
        var grocery=ProblemPlaceAccessible(BuildingKind.Grocery,
            [new(intakeX-80,16),new(intakeX+80,16),new(intakeX-120,16),new(intakeX+120,16)]);
        var dryIntake=ProblemPlaceDryIntake(intakeX);
        // A shop with a real company but no money and no stock: the company
        // stays active (no debts, only days of complaints), restocking fails
        // gracefully on zero balance, and the stock stays really empty.
        var companyId=_sim.State.NextId++;
        var company=new Company{Id=companyId,Name="Mercado QA "+companyId,Active=true};
        _sim.State.Companies.Add(company);
        _sim.State.Wallets[company.Account]=0;
        grocery.CompanyId=company.Id;grocery.OwnerAccount=company.Account;company.Buildings.Add(grocery.Id);
        log.Add($"newcauses dry={dryIntake.Id} treatment={treatment.Id} shop={grocery.Id} hall={_sim.State.Buildings.Single(b=>b.Kind==BuildingKind.CityHall).Id}");
        await ProblemWaitWorks([shoreRoad.Id!.Value]);
        var wetIntake=await ProblemPlaceNear(BuildingKind.WaterIntake,new(intakeX,shoreZ));
        await ProblemWaitWorks([treatment.ProjectId,grocery.ProjectId,dryIntake.ProjectId,wetIntake.ProjectId]);
        var hall=_sim.State.Buildings.Single(b=>b.Kind==BuildingKind.CityHall);
        _world.Reset(_sim.State);BuildCatalog();ClearTool();ClearSelection();
        _focus=ToV(new(-40,20));_zoom=150;_yaw=45;UpdateCamera();
        // Morning, opening hours for every kind. Reset after aligning the
        // clock so every persistence starts from zero.
        for(var i=0;i<200&&_sim.State.Now.Hour!=8;i++)_sim.Advance(600);
        Require(_sim.State.Now.Hour==8,"Cenário de novas causas às 8h: "+_sim.State.Now.ToString("dd/MM HH:mm"));
        _world.Reset(_sim.State);
        await ProblemPresent("newcauses-baseline");
        Require(_world.ProblemAlertIcons(dryIntake.Id)=="intake","Captação longe da água alerta sem espera: "+_world.ProblemAlertIcons(dryIntake.Id));
        Require(_world.ProblemAlertIcons(wetIntake.Id)=="","Captação perto da água não alerta por localização: "+_world.ProblemAlertIcons(wetIntake.Id));
        // One minute presented: raw-water (20 s) and stock (60 s) appear,
        // staff (300 s) still waits, proving the persistence order.
        _sim.Advance(60);await ProblemPresent(null);
        Require(_world.ProblemAlertIcons(treatment.Id)=="noraw","Tratamento sem água bruta: "+_world.ProblemAlertIcons(treatment.Id));
        Require(_world.ProblemAlertIcons(grocery.Id)=="stock","Mercearia com empresa e sem estoque: "+_world.ProblemAlertIcons(grocery.Id));
        Require(_world.ProblemAlertIcons(hall.Id)=="","Prefeitura ainda sem alerta de equipe (persistência longa): "+_world.ProblemAlertIcons(hall.Id));
        Require(_world.ProblemAlertIcons(dryIntake.Id)=="intake","Captação seca ainda só por localização: "+_world.ProblemAlertIcons(dryIntake.Id));
        Require(_world.ProblemAlertIcons(wetIntake.Id)=="","Captação úmida ainda sem nada: "+_world.ProblemAlertIcons(wetIntake.Id));
        log.Add($"PROBLEM_ALERTS_QA ok building={dryIntake.Id} alerts={_world.ProblemAlertIcons(dryIntake.Id)}");
        log.Add($"PROBLEM_ALERTS_QA ok building={treatment.Id} alerts={_world.ProblemAlertIcons(treatment.Id)}");
        log.Add($"PROBLEM_ALERTS_QA ok building={grocery.Id} alerts={_world.ProblemAlertIcons(grocery.Id)}");
        await ProblemPresent("newcauses-flows");
        // Five minutes presented: no staff anywhere, so every staff icon joins.
        for(var i=0;i<4;i++){_sim.Advance(60);await ProblemPresent(null);}
        Require(_world.ProblemAlertIcons(hall.Id)=="staff","Prefeitura sem funcionários de manhã: "+_world.ProblemAlertIcons(hall.Id));
        Require(_world.ProblemAlertIcons(grocery.Id)=="staff,stock","Mercearia sem equipe e sem estoque: "+_world.ProblemAlertIcons(grocery.Id));
        Require(_world.ProblemAlertIcons(dryIntake.Id)=="intake,staff","Captação seca sem equipe: "+_world.ProblemAlertIcons(dryIntake.Id));
        Require(_world.ProblemAlertIcons(treatment.Id)=="noraw,staff","Tratamento sem água bruta e sem equipe: "+_world.ProblemAlertIcons(treatment.Id));
        Require(_world.ProblemAlertIcons(wetIntake.Id)=="staff","Captação úmida só sem equipe: "+_world.ProblemAlertIcons(wetIntake.Id));
        log.Add($"PROBLEM_ALERTS_QA ok building={hall.Id} alerts={_world.ProblemAlertIcons(hall.Id)}");
        log.Add($"PROBLEM_ALERTS_QA ok building={wetIntake.Id} alerts={_world.ProblemAlertIcons(wetIntake.Id)}");
        _focus=ToV(grocery.Position);_zoom=45;_yaw=45;UpdateCamera();await ProblemPresent("newcauses-flows-detail");
        ClearSelection();_selectedBuilding=grocery.Id;RefreshInspection();await RecoveryFrame();
        Require(RecoveryNodes<Label>(_inspection).Any(label=>label.Text=="Problemas: Sem funcionários · Estoque vazio"),"Inspeção acusa os mesmos problemas dos ícones");
        await RecoveryCapture(System.IO.Path.Combine(ProblemAlertDirectory!,"newcauses-inspection.png"),GetViewport());
        log.Add("newcauses inspection problems=staff+stock");
        // Tooltip: same text as the inspection, only in selection mode (no
        // construction tool). Warps the real OS cursor because the hover reads
        // the same viewport state the player produces.
        ClearTool();
        var balloon=_world.AlertAnchorScreen(grocery.Id,_camera);
        Require(balloon!=null,"Balão com alertas projetado na tela");
        Input.WarpMouse(balloon.Value);await RecoveryFrame();await RecoveryFrame();
        UpdateAlertTooltip(balloon.Value);
        Require(_alertTooltip.Visible,"Tooltip aparece sobre o balão com alertas");
        Require(_alertTooltipLabel.Text=="Mercearia\nProblemas: Sem funcionários · Estoque vazio","Tooltip igual à inspeção: "+_alertTooltipLabel.Text);
        UpdateAlertTooltip(_camera.UnprojectPosition(ToV(grocery.Position,2)));
        Require(_alertTooltip.Visible,"Tooltip aparece sobre o prédio com alertas");
        log.Add("newcauses tooltip=balloon+building");
        await RecoveryCapture(System.IO.Path.Combine(ProblemAlertDirectory!,"newcauses-tooltip.png"),GetViewport());
        _building=BuildingKind.House;
        UpdateAlertTooltip(balloon.Value);
        Require(!_alertTooltip.Visible,"Tooltip some com ferramenta ativa");
        ClearTool();
        UpdateAlertTooltip(_camera.UnprojectPosition(ToV(new Point(0,-100),0)));
        Require(!_alertTooltip.Visible,"Tooltip some longe de alertas");
        log.Add("newcauses tooltip hidden-with-tool-and-away");
        // Staff rule: a hired crew between shifts is normal, not an alert.
        // One resident hire at CityHall on this never-saved copy, then to
        // 16:00: shift 7-15 over, Open until 19h, nobody present, no alert.
        // (The morning asserts above already prove a building with no hires
        // does alert.)
        var hireId=_sim.State.NextId++;
        _sim.State.Citizens.Add(new Citizen{Id=hireId,Name="QA",FamilyId=0,Age=30,WorkplaceId=hall.Id,Shift=0,Position=hall.Door,Resident=true,Alive=true,InsideMap=false,WeeklyDayOff=(int)DayOfWeek.Sunday});
        for(var i=0;i<200&&_sim.State.Now.Hour!=16;i++)_sim.Advance(600);
        Require(_sim.State.Now.Hour==16,"Cenário de novas causas às 16h: "+_sim.State.Now.ToString("dd/MM HH:mm"));
        _sim.Advance(60);await ProblemPresent(null);
        _sim.Advance(60);await ProblemPresent(null);
        Require(_world.ProblemAlertIcons(hall.Id)=="","Prefeitura com contratados fora do turno não alerta às 16h: "+_world.ProblemAlertIcons(hall.Id));
        log.Add("newcauses 16h hired-crew-between-shifts silent");
        // Night: opening-hours kinds go quiet; utilities keep their staff
        // alert because their shifts cover the whole day.
        for(var i=0;i<200&&_sim.State.Now.Hour!=23;i++)_sim.Advance(600);
        Require(_sim.State.Now.Hour==23,"Cenário de novas causas às 23h: "+_sim.State.Now.ToString("dd/MM HH:mm"));
        _sim.Advance(60);await ProblemPresent(null);
        _sim.Advance(60);await ProblemPresent(null);
        Require(_world.ProblemAlertIcons(hall.Id)=="","Prefeitura fora do horário não alerta: "+_world.ProblemAlertIcons(hall.Id));
        Require(_world.ProblemAlertIcons(grocery.Id)=="","Mercearia fechada não alerta: "+_world.ProblemAlertIcons(grocery.Id));
        Require(_world.ProblemAlertIcons(dryIntake.Id).Contains("intake"),"Captação seca segue sem fonte à noite: "+_world.ProblemAlertIcons(dryIntake.Id));
        Require(_world.ProblemAlertIcons(treatment.Id).Contains("noraw"),"Tratamento segue sem água bruta à noite: "+_world.ProblemAlertIcons(treatment.Id));
        log.Add($"PROBLEM_ALERTS_QA ok night hall={_world.ProblemAlertIcons(hall.Id)} shop={_world.ProblemAlertIcons(grocery.Id)} dry={_world.ProblemAlertIcons(dryIntake.Id)} treatment={_world.ProblemAlertIcons(treatment.Id)}");
        // Maintenance: a municipal ratio forced low alerts at once (slow,
        // monotonic condition) and clears as soon as it is restored. Each
        // check presents after advancing the clock, since the icons refresh
        // on simulated time.
        hall.MaintenanceRatio=.5;
        _focus=ToV(hall.Position);_zoom=45;_yaw=45;UpdateCamera();
        _sim.Advance(60);await ProblemPresent("newcauses-maintenance");
        Require(_world.ProblemAlertIcons(hall.Id)=="maintenance","Manutenção pendente: "+_world.ProblemAlertIcons(hall.Id));
        hall.MaintenanceRatio=1;
        _sim.Advance(60);await ProblemPresent(null);
        Require(_world.ProblemAlertIcons(hall.Id)=="","Manutenção em dia não alerta: "+_world.ProblemAlertIcons(hall.Id));
        log.Add("newcauses maintenance appeared-and-cleared");
        // Real goods clear the stock icon; the staff icon keeps waiting for
        // someone present, proving each icon follows its own state.
        for(var i=0;i<200&&_sim.State.Now.Hour!=8;i++)_sim.Advance(600);
        _world.Reset(_sim.State);
        _sim.Advance(60);await ProblemPresent(null);
        Require(_world.ProblemAlertIcons(grocery.Id)=="stock","Mercearia ainda sem estoque de manhã: "+_world.ProblemAlertIcons(grocery.Id));
        grocery.AddStock(ResourceKind.Food,60);
        _sim.Advance(60);await ProblemPresent(null);
        Require(_world.ProblemAlertIcons(grocery.Id)=="","Estoque real apaga o alerta: "+_world.ProblemAlertIcons(grocery.Id));
        log.Add("newcauses stock cleared-by-real-goods");
        _sim.CheckInvariants();
    }

    // First accessible position from the hints, with the same physical
    // validity and real street access the player sees.
    Building ProblemPlaceAccessible(BuildingKind kind,Point[] hints)
    {
        foreach(var at in hints){
            var quote=_sim.QuoteBuilding(kind,at,0);
            if(!quote.PhysicallyValid||!quote.Accessible)continue;
            var placed=_sim.PlaceBuilding(kind,at,0);
            Require(placed.Success,"Colocação de "+Catalog.Definitions[kind].Name+" em "+at+": "+placed.Message);
            return _sim.State.Buildings.Single(b=>b.Id==placed.Id);
        }
        throw new InvalidOperationException("Nenhuma posição acessível para "+Catalog.Definitions[kind].Name);
    }

    // A water intake far from any viable natural source: physically valid
    // with a warning and accessible, but never productive.
    Building ProblemPlaceDryIntake(double shoreX)
    {
        foreach(var z in new[]{14.0,-24.0,-64.0})
            foreach(var x in new[]{-120.0,-104.0,-88.0,-72.0,-56.0,-40.0,-24.0,-8.0,16.0,32.0,48.0,64.0,96.0,120.0}){
                if(Math.Abs(x-shoreX)<24)continue;
                var at=new Point(x,z);
                var quote=_sim.QuoteBuilding(BuildingKind.WaterIntake,at,0);
                if(!quote.PhysicallyValid||!quote.Accessible)continue;
                if(_sim.HasNaturalWaterSource(new Building{Kind=BuildingKind.WaterIntake,Position=at}))continue;
                var placed=_sim.PlaceBuilding(BuildingKind.WaterIntake,at,0);
                Require(placed.Success,"Captação seca em "+at+": "+placed.Message);
                return _sim.State.Buildings.Single(b=>b.Id==placed.Id);
            }
        throw new InvalidOperationException("Nenhuma posição seca acessível para a captação");
    }

    // A real working city: potable water really reaches the occupied houses and
    // no icon is shown. Then a real player action (cutting the street that
    // serves the intake) really stops the water, and reconnecting that street
    // really restores it, so the alert appears and disappears for real.
    async Task ProblemAlertsRealWater(List<string> log)
    {
        _sim=new CitySimulation(42);
        Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Prefeitura do cenário real");
        Require(_sim.PlaceRoad(new(-192,0),new(240,0)).Success,"Rua principal do cenário real");
        var lake=_sim.State.LakeCentre;
        var shoreZ=Math.Round((lake.Z-_sim.State.LakeRadius-18)/4)*4;
        var intakeX=Math.Round(lake.X/4)*4;
        var lakeRoad=_sim.PlaceRoad(new(intakeX,0),new(intakeX,shoreZ-14));
        Require(lakeRoad.Success,"Rua até a captação: "+lakeRoad.Message);
        var works=new List<int>{lakeRoad.Id!.Value};
        foreach(var x in new[]{-120,-104,-88,-72,-56,-40,-24,-8,16,32,48,64}){
            var placed=_sim.PlaceBuilding(BuildingKind.House,new(x,14));
            Require(placed.Success,"Casa em "+x+": "+placed.Message);
            works.Add(_sim.State.Buildings.Single(b=>b.Id==placed.Id!.Value).ProjectId);
        }
        foreach(var x in new[]{100}){
            var placed=_sim.PlaceBuilding(BuildingKind.WindPower,new(x,16));
            Require(placed.Success,"Eólica em "+x+": "+placed.Message);
            works.Add(_sim.State.Buildings.Single(b=>b.Id==placed.Id!.Value).ProjectId);
        }
        // The intake only fits once its street is really built, like a player would.
        await ProblemWaitWorks([lakeRoad.Id!.Value]);
        var intake=await ProblemPlaceNear(BuildingKind.WaterIntake,new(intakeX,shoreZ));
        var treatment=await ProblemPlaceNear(BuildingKind.WaterTreatment,new(intakeX+40,20));
        works.Add(intake.ProjectId);works.Add(treatment.ProjectId);
        await ProblemWaitWorks(works);
        _world.Reset(_sim.State);BuildCatalog();ClearTool();ClearSelection();
        var homes=_sim.State.Buildings.Where(b=>b.Kind==BuildingKind.House).OrderBy(b=>b.Position.X).ToArray();
        _focus=ToV(new(30,14));_zoom=150;_yaw=45;UpdateCamera();
        bool WaterDelivered()=>_sim.State.PotableCapacity>0&&homes.Where(h=>h.FamilyId!=null).ToArray() is {Length:>0} occupied&&occupied.All(h=>h.WaterRatio>=.5);
        // Real families move in and real staff operate the real water chain.
        for(var elapsed=0.0;elapsed<96*3600&&!WaterDelivered();elapsed+=600)_sim.Advance(600);
        var occupied=homes.Where(h=>h.FamilyId!=null).ToArray();
        Require(occupied.Length>0&&WaterDelivered(),"Água potável realmente entregue às casas ocupadas: capacidade "+_sim.State.PotableCapacity.ToString("N0")+" · "+string.Join(" ",occupied.Select(h=>h.WaterRatio.ToString("P0"))));
        for(var step=0;step<3;step++){_sim.Advance(60);await ProblemPresent(null);}
        Require(occupied.All(h=>_world.ProblemAlertIcons(h.Id)==""),"Cidade com água potável não mostra alerta: "+string.Join(" ",occupied.Select(h=>h.Id+":"+_world.ProblemAlertIcons(h.Id))));
        Require(!_world.ProblemAlertIcons(intake.Id).Contains("intake"),"Captação perto da água não alerta por localização: "+_world.ProblemAlertIcons(intake.Id));
        Require(!_world.ProblemAlertIcons(treatment.Id).Contains("noraw"),"Tratamento com água bruta não alerta: "+_world.ProblemAlertIcons(treatment.Id));
        log.Add($"PROBLEM_ALERTS_QA ok buildings={string.Join(",",occupied.Select(h=>h.Id))} alerts=none pop={_sim.Population} potable={_sim.State.PotableCapacity:N0}");
        await ProblemPresent("real-water-healthy");
        await ProblemPresentDetail("real-water-healthy-detail",occupied[0]);

        // Real player action: cut the single street segment that serves the
        // intake. The houses lose their real water supply and alert.
        var intakeNode=_sim.Access(intake)!;
        Require(_sim.DemolishRoad(intakeNode.Id).Success,"Corte real do acesso da captação");
        for(var elapsed=0.0;elapsed<3600&&_sim.State.PotableCapacity>0;elapsed+=60)_sim.Advance(60);
        for(var step=0;step<3;step++){_sim.Advance(60);await ProblemPresent(null);}
        Require(_sim.State.PotableCapacity<=0,"A captação realmente parou: capacidade "+_sim.State.PotableCapacity.ToString("N0"));
        Require(occupied.All(h=>_world.ProblemAlertIcons(h.Id).Contains("water")),"Casas sem água potável alertam: "+string.Join(" ",occupied.Select(h=>h.Id+":"+_world.ProblemAlertIcons(h.Id))));
        Require(occupied.All(h=>h.Cause=="Abastecimento de água insuficiente"),"Causa exibida na inspeção é a mesma do alerta");
        Require(_world.ProblemAlertIcons(treatment.Id).Contains("noraw"),"Tratamento sem água bruta alerta: "+_world.ProblemAlertIcons(treatment.Id));
        Require(_world.ProblemAlertIcons(intake.Id).Contains("road"),"Captação isolada alerta sem acesso: "+_world.ProblemAlertIcons(intake.Id));
        Require(!_world.ProblemAlertIcons(intake.Id).Contains("intake"),"Captação isolada não alerta por localização: "+_world.ProblemAlertIcons(intake.Id));
        log.Add($"PROBLEM_ALERTS_QA ok intake={intake.Id} alerts={_world.ProblemAlertIcons(intake.Id)} treatment={treatment.Id} alerts={_world.ProblemAlertIcons(treatment.Id)}");
        var powerObserved=occupied.Select(h=>_world.ProblemAlertIcons(h.Id).Contains("power")).ToArray();
        log.Add($"PROBLEM_ALERTS_QA ok buildings={string.Join(",",occupied.Select(h=>h.Id))} alerts=water cause=\"Abastecimento de água insuficiente\" potable=0 power={_sim.State.ElectricityCapacity:N0} powerAlertsToo={powerObserved.Count(p=>p)}/{occupied.Length}");
        await ProblemPresent("real-water-shortage");
        await ProblemPresentDetail("real-water-shortage-detail",occupied[0]);

        // Reconnecting the same street restores the real supply and the alert.
        var at=intakeNode.Position;
        var reconnect=_sim.PlaceRoad(new Point((int)at.X,(int)at.Z),new Point((int)at.X,(int)at.Z));
        Require(reconnect.Success,"Reconexão real do acesso da captação: "+reconnect.Message);
        await ProblemWaitWorks([reconnect.Id!.Value]);
        // At least a few presented steps: the icon must clear from the
        // presentation state, not only from the simulation state.
        for(var step=0;step<600;step++){
            _sim.Advance(60);await ProblemPresent(null);
            if(step>=3&&WaterDelivered())break;
        }
        Require(WaterDelivered(),"Água potável realmente restabelecida: capacidade "+_sim.State.PotableCapacity.ToString("N0")+" · "+string.Join(" ",occupied.Select(h=>h.WaterRatio.ToString("P0"))));
        Require(occupied.All(h=>!_world.ProblemAlertIcons(h.Id).Contains("water")),"Alerta de água some quando a água volta: "+string.Join(" ",occupied.Select(h=>h.Id+":"+_world.ProblemAlertIcons(h.Id))));
        ClearSelection();_selectedBuilding=occupied[0].Id;RefreshInspection();await RecoveryFrame();
        Require(!RecoveryNodes<Label>(_inspection).Any(label=>label.Text.Contains("Falta de água")),"Inspeção deixa de acusar problema resolvido");
        log.Add($"real cleared buildings={string.Join(",",occupied.Select(h=>h.Id))} potable={_sim.State.PotableCapacity:N0} houseWater={string.Join(" ",occupied.Select(h=>h.WaterRatio.ToString("P0")))}");
        // Same water family: a building stopped after a persistent shortage
        // keeps the water icon briefly once the supply returns, while its
        // outage counter is still at the closure threshold. Outage forced on
        // this never-saved copy (the real shortage never reaches 72 h).
        treatment.WaterOutageSeconds=_sim.State.Tuning.WaterClosureHours*3600+3600;
        _sim.Advance(60);await ProblemPresent(null);
        Require(_world.ProblemAlertIcons(treatment.Id).Contains("water"),"Parada após falta persistente mantém água: "+_world.ProblemAlertIcons(treatment.Id));
        treatment.WaterOutageSeconds=0;
        _sim.Advance(60);await ProblemPresent(null);
        Require(!_world.ProblemAlertIcons(treatment.Id).Contains("water"),"Contador zerado apaga a água: "+_world.ProblemAlertIcons(treatment.Id));
        log.Add("real outage-tail water-appeared-and-cleared");
        await ProblemPresent("real-water-recovered");
        await ProblemPresentDetail("real-water-recovered-detail",occupied[0]);
    }

    // Builds until the listed works are physically completed. A real city keeps
    // creating maintenance works, so only the requested ids are waited on.
    async Task ProblemWaitWorks(IEnumerable<int> ids,double deadlineSeconds=72*3600)
    {
        var pending=new HashSet<int>(ids);
        var projects=()=>string.Join(",",pending.Select(id=>{var p=_sim.State.Projects.First(x=>x.Id==id);return p.Id+":"+p.Cause;}));
        for(var elapsed=0.0;elapsed<deadlineSeconds&&pending.Count>0;elapsed+=300){
            _sim.Advance(300);
            foreach(var id in pending.ToList())
                if(_sim.State.Projects.First(p=>p.Id==id).Status==WorkStatus.Completed)pending.Remove(id);
        }
        Require(pending.Count==0,"Obras concluídas por execução física: "+projects());
    }

    // Places a building near a hint, requiring the same physical validity, real
    // street access and (for intakes) real natural water source the player sees.
    async Task<Building> ProblemPlaceNear(BuildingKind kind,Point hint)
    {
        var offsets=new[]{new Point(0,0),new Point(4,0),new Point(-4,0),new Point(0,4),new Point(0,-4),new Point(8,0),new Point(-8,0),new Point(0,8),new Point(0,-8),new Point(12,0),new Point(-12,0),new Point(0,12),new Point(0,-12)};
        foreach(var offset in offsets){
            var at=new Point(Math.Round((hint.X+offset.X)/4)*4,Math.Round((hint.Z+offset.Z)/4)*4);
            var quote=_sim.QuoteBuilding(kind,at,0);
            if(!quote.PhysicallyValid||!quote.Accessible)continue;
            var natural=kind!=BuildingKind.WaterIntake||_sim.HasNaturalWaterSource(new Building{Kind=kind,Position=at});
            if(!natural)continue;
            var placed=_sim.PlaceBuilding(kind,at,0);
            Require(placed.Success,"Colocação de "+Catalog.Definitions[kind].Name+" em "+at+": "+placed.Message);
            return _sim.State.Buildings.Single(b=>b.Id==placed.Id);
        }
        throw new InvalidOperationException("Nenhuma posição válida para "+Catalog.Definitions[kind].Name+" perto de "+hint);
    }

    async Task ProblemPresent(string? capture)
    {
        _world.Sync(_sim.State,1,0,_sim);
        await RecoveryFrame();
        if(capture!=null)await RecoveryCapture(System.IO.Path.Combine(ProblemAlertDirectory!,capture+".png"),GetViewport());
    }

    // Frames one building up close and captures it, so a single icon can be
    // checked for shape, colour and readability at the size the player sees.
    async Task ProblemPresentDetail(string capture,Building building)
    {
        _focus=ToV(building.Position);_zoom=42;_yaw=45;UpdateCamera();
        await ProblemPresent(capture);
    }

    // Scripted playtest under the real host clock: a new city is founded and
    // played like a player would, at speed 3, while the alerts are observed.
    async void ProblemAlertsPlaytest()
    {
        var root=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/problem-alerts"));
        ProblemAlertDirectory=System.IO.Path.Combine(root,"playtest-"+Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(ProblemAlertDirectory);
        var log=new List<string>();
        try{
            Require(DisplayServer.GetName()!="headless","Playtest exige Godot renderizado");
            _speed=0;_autosaveMinutes=0;_clockBudget=0;
            CitySaves.VerificationDirectory=System.IO.Path.Combine(ProblemAlertDirectory,"saves");
            _sim=new CitySimulation(42);_world.Reset(_sim.State);BuildCatalog();ClearTool();ClearSelection();RefreshHud();
            log.Add($"playtest start money={_sim.Available}");
            _focus=ToV(new(30,14));_zoom=150;_yaw=45;UpdateCamera();
            Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Prefeitura fundada");
            Require(_sim.PlaceRoad(new(-192,0),new(240,0)).Success,"Rua principal");
            var lake=_sim.State.LakeCentre;
            var shoreZ=Math.Round((lake.Z-_sim.State.LakeRadius-18)/4)*4;
            var intakeX=Math.Round(lake.X/4)*4;
            var lakeRoad=_sim.PlaceRoad(new(intakeX,0),new(intakeX,shoreZ-14));
            Require(lakeRoad.Success,"Rua até a captação: "+lakeRoad.Message);
            await ProblemWaitWorks([lakeRoad.Id!.Value]);
            var houses=new List<Building>();
            foreach(var x in new[]{-120,-104,-88,-72,-56,-40,-24,-8,16,32,48,64}){
                var placed=_sim.PlaceBuilding(BuildingKind.House,new(x,14));
                Require(placed.Success,"Casa em "+x+": "+placed.Message);
                houses.Add(_sim.State.Buildings.Single(b=>b.Id==placed.Id));
            }
            foreach(var x in new[]{100}){
                var placed=_sim.PlaceBuilding(BuildingKind.WindPower,new(x,16));
                Require(placed.Success,"Eólica em "+x+": "+placed.Message);
            }
            var intake=await ProblemPlaceNear(BuildingKind.WaterIntake,new(intakeX,shoreZ));
            var treatment=await ProblemPlaceNear(BuildingKind.WaterTreatment,new(intakeX+40,20));
            var works=houses.Select(h=>h.ProjectId)
                .Concat(_sim.State.Buildings.Where(b=>b.Kind==BuildingKind.WindPower).Select(b=>b.ProjectId))
                .Append(intake.ProjectId).Append(treatment.ProjectId).ToList();
            log.Add($"playtest ordered buildings={works.Count} money={_sim.Available}");
            await ProblemWaitWorks(works);
            log.Add($"playtest completed buildings money={_sim.Available} population={_sim.Population}");
            bool WaterDelivered()=>_sim.State.PotableCapacity>0&&houses.Where(h=>h.FamilyId!=null).ToArray() is {Length:>0} occupied&&occupied.All(h=>h.WaterRatio>=.5);
            (int Road,int Water,int Power,int Other,int Clean) previous=(0,0,0,0,0);
            void Observe(string moment,bool force=false){
                var snapshot=ProblemAlertSnapshot();
                var capacity=_sim.State.Buildings.Where(b=>!b.Removed&&b.Definition.Utility).Select(b=>$"{b.Kind}:{_sim.WorkersPresent(b)}/{b.Definition.Jobs}").ToList();
                if(!force&&snapshot==previous)return;
                var detail=string.Join(" ",_sim.State.Buildings.Where(b=>b.Completed&&!b.Removed).Select(b=>$"{b.Id}:{(_world.ProblemAlertIcons(b.Id)==""?"clean":_world.ProblemAlertIcons(b.Id))}"));
                log.Add($"playtest {moment} road={snapshot.Road} water={snapshot.Water} power={snapshot.Power} other={snapshot.Other} clean={snapshot.Clean} time={_sim.State.Now:dd/MM HH:mm} pop={_sim.Population} potable={_sim.State.PotableCapacity:N0} power={_sim.State.ElectricityCapacity:N0} staff={string.Join(" ",capacity)} | {detail}");
                GD.Print("PROBLEM_ALERTS_PLAYTEST "+moment+" road="+snapshot.Road+" water="+snapshot.Water+" power="+snapshot.Power+" other="+snapshot.Other+" clean="+snapshot.Clean+" time="+_sim.State.Now.ToString("dd/MM HH:mm")+" pop="+_sim.Population+" potable="+_sim.State.PotableCapacity.ToString("N0")+" powerCap="+_sim.State.ElectricityCapacity.ToString("N0"));
                previous=snapshot;
            }
            Observe("before-families",force:true);
            await ProblemPresent("playtest-start");
            // Real host clock at 60x: families move in and real demand appears.
            SetSpeed(3);
            var watch=System.Diagnostics.Stopwatch.StartNew();
            while(watch.Elapsed.TotalSeconds<45){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Observe("real-clock-60x");}
            SetSpeed(0);
            Observe("after-real-clock",force:true);
            // Real city clock in longer quanta, exactly like the host loop does,
            // until the real water chain operates and the city is healthy again.
            for(var step=0;step<200&&!WaterDelivered();step++){_sim.Advance(600);await ProblemPresent(null);Observe("water-chain-starting");}
            Require(WaterDelivered(),"O ciclo real de água potável entrou em operação na cidade");
            for(var step=0;step<3;step++){_sim.Advance(60);await ProblemPresent(null);}
            Require(ProblemAlertSnapshot().Water==0,"Cidade com água potável não mostra alerta");
            Require(ProblemAlertSnapshot().Other==0,"Cidade saudável não mostra nenhum dos outros alertas: "+string.Join(" ",_sim.State.Buildings.Where(b=>b.Completed&&!b.Removed).Select(b=>b.Id+":"+_world.ProblemAlertIcons(b.Id))));
            Observe("city-healthy",force:true);
            await ProblemPresent("playtest-healthy");
            // Player action: cut the street that serves the intake.
            var intakeNode=_sim.Access(intake)!;
            Require(_sim.DemolishRoad(intakeNode.Id).Success,"Corte real do acesso da captação");
            for(var elapsed=0.0;elapsed<3600&&_sim.State.PotableCapacity>0;elapsed+=60)_sim.Advance(60);
            for(var step=0;step<3;step++){_sim.Advance(60);await ProblemPresent(null);}
            Observe("after-cutting-intake-access",force:true);
            Require(ProblemAlertSnapshot().Water>0,"Alertas de água apareceram quando a captação perdeu a rua");
            await ProblemPresent("playtest-water-shortage");
            _focus=ToV(houses[1].Position);_zoom=45;_yaw=45;UpdateCamera();await ProblemPresent("playtest-water-detail");
            ClearSelection();_selectedBuilding=houses[1].Id;RefreshInspection();await RecoveryFrame();
            Require(RecoveryNodes<Label>(_inspection).Any(label=>label.Text=="Problemas: Falta de água"),"Inspeção da casa mostra o mesmo problema do ícone");
            await RecoveryCapture(System.IO.Path.Combine(ProblemAlertDirectory!,"playtest-inspection.png"),GetViewport());
            log.Add("playtest inspection problems=water");
            // The player reconnects the street and the supply is restored.
            var at=intakeNode.Position;
            var reconnect=_sim.PlaceRoad(new Point((int)at.X,(int)at.Z),new Point((int)at.X,(int)at.Z));
            Require(reconnect.Success,"Reconexão real do acesso da captação: "+reconnect.Message);
            await ProblemWaitWorks([reconnect.Id!.Value]);
            // At least a few presented steps: the icon must clear from the
            // presentation state, not only from the simulation state.
            for(var step=0;step<600;step++){
                _sim.Advance(60);await ProblemPresent(null);
                if(step>=3&&WaterDelivered())break;
            }
            Observe("water-recovery",force:true);
            Require(WaterDelivered(),"Água potável realmente restabelecida pela reconexão da rua: capacidade "+_sim.State.PotableCapacity.ToString("N0")+" brutas "+_sim.State.RawWaterCapacity.ToString("N0")+" · "+string.Join(" ",houses.Where(h=>h.FamilyId!=null).Select(h=>h.WaterRatio.ToString("P0"))));
            Require(ProblemAlertSnapshot().Water==0,"Alertas de água somem quando a água volta");
            Require(ProblemAlertSnapshot().Other==0,"Outros alertas somem quando a água volta: "+string.Join(" ",_sim.State.Buildings.Where(b=>b.Completed&&!b.Removed).Select(b=>b.Id+":"+_world.ProblemAlertIcons(b.Id))));
            Observe("after-water-recovery",force:true);
            await ProblemPresent("playtest-recovered");
            _sim.CheckInvariants();
            System.IO.File.WriteAllLines(System.IO.Path.Combine(ProblemAlertDirectory,"log.txt"),log);
            GD.Print("PROBLEM_ALERTS_PLAYTEST_OK waterAlertsAppearedAndCleared buildings="+_sim.State.Buildings.Count(b=>b.Completed&&!b.Removed)+" population="+_sim.Population+" directory="+ProblemAlertDirectory);
            GetTree().Quit();
        }catch(Exception error){
            System.IO.File.WriteAllLines(System.IO.Path.Combine(ProblemAlertDirectory!,"log.txt"),log);
            System.IO.File.WriteAllText(System.IO.Path.Combine(ProblemAlertDirectory!,"failure.txt"),error.ToString());
            GD.PushError(error.ToString());GetTree().Quit(1);
        }finally{CitySaves.VerificationDirectory=null;}
    }

    (int Road,int Water,int Power,int Other,int Clean) ProblemAlertSnapshot()
    {
        int road=0,water=0,power=0,other=0,clean=0;
        foreach(var b in _sim.State.Buildings.Where(b=>b.Completed&&!b.Removed)){
            var icons=_world.ProblemAlertIcons(b.Id);
            if(icons=="")clean++;
            if(icons.Contains("road"))road++;
            if(icons.Contains("water"))water++;
            if(icons.Contains("power"))power++;
            if(icons.Split(',',StringSplitOptions.RemoveEmptyEntries).Any(kind=>kind is "intake" or "noraw" or "staff" or "stock" or "maintenance"))other++;
        }
        return (road,water,power,other,clean);
    }
}
