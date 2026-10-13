using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    HBoxContainer ModalActions()
    {
        var layout=_modal!.GetChildren().OfType<VBoxContainer>().Single();
        var actions=new HBoxContainer { Name="ModalActions" };actions.AddThemeConstantOverride("separation",8);layout.AddChild(actions);return actions;
    }

    BoxContainer ResponsiveRow(Node parent)
    {
        var window=_modal!;var row=new BoxContainer { Vertical=window.Size.X<620, SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation",8);parent.AddChild(row);
        window.SizeChanged+=()=>{if(GodotObject.IsInstanceValid(row))row.Vertical=window.Size.X<620;};return row;
    }
    static VBoxContainer Card(Node parent, string title, string detail = "")
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color("273b40"), BorderColor = new Color("496269"), BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 12, ContentMarginBottom = 12
        });
        parent.AddChild(panel); var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 8); panel.AddChild(box);
        Text(box, title, 19); if (detail.Length > 0) Text(box, detail, 13); return box;
    }

    static Label Muted(Node parent, string value, int size = 13)
    {
        var label = Text(parent, value, size); label.AddThemeColorOverride("font_color", new Color("b5c8c9")); return label;
    }

    static void Metric(Node parent, string name, string value)
    {
        var box = Card(parent, name); var label = Text(box, value, 26); label.AddThemeColorOverride("font_color", new Color("fff0ba"));
    }

    void ShowCities()
    {
        var box = Modal("Cidades");
        var saves = CitySaves.List().ToList();
        var load = Card(box, "Abrir uma cidade salva", "Cada partida tem seu próprio arquivo. Escolha a cidade que deseja retomar.");
        if (saves.Count == 0) Muted(load, "Nenhuma cidade salva ainda.");
        foreach (var saved in saves) { var copy = saved;var savedRow=ResponsiveRow(load);Button(savedRow, copy.Name + " • " + copy.At.ToString("dd/MM/yyyy HH:mm"), () => LoadCity(copy.Id));var rename=Button(savedRow,"Renomear",()=>ShowCityRename(copy.Id,copy.Name));rename.CustomMinimumSize=new(104,40);rename.SizeFlagsHorizontal=Control.SizeFlags.Fill; }
        var saving = Card(box, "Salvar esta partida", "Cidade atual: " + _sim.State.Name + " · código do mapa: " + _sim.State.Seed);
        Button(saving, "▣ Salvar agora", SaveCity);
        var autosaveRow = new HBoxContainer(); saving.AddChild(autosaveRow);
        Text(autosaveRow, "Salvamento automático · minutos (0 desliga)", 14);
        var autosave = new SpinBox { MinValue = 0, MaxValue = 60, Step = 1, Value = _autosaveMinutes, CustomMinimumSize = new(120, 40) };
        autosaveRow.AddChild(autosave); autosave.ValueChanged += value => _autosaveMinutes = value;
        var creation = Card(box, "Criar outra cidade", "A partida atual será salva antes de abrir uma cidade vazia. O código gera o mapa; ele não carrega um save.");
        Text(creation,"Nome da cidade (opcional)",14);
        var name=new LineEdit { Name="NewCityName", PlaceholderText="Ex.: Vale Verde", CustomMinimumSize=new(0,40), SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };creation.AddChild(name);
        var row = new HBoxContainer(); creation.AddChild(row); Text(row, "Código do mapa", 14);
        var seed = new SpinBox { MinValue = 0, MaxValue = int.MaxValue, Value = 42, CustomMinimumSize = new(140, 40) }; row.AddChild(seed);
        Button(row, "+ Criar", () => NewCity((int)seed.Value,name.Text));
    }

    void ShowCityRename(string id,string previousName)
    {
        var box=Modal("Renomear cidade");Text(box,"O mapa, a partida e o arquivo serão mantidos.",16);
        var name=new LineEdit { Name="RenameCityName", Text=previousName, CustomMinimumSize=new(0,40), SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };box.AddChild(name);
        var feedback=Text(box,"",14);var actions=ModalActions();
        void Rename()
        {
            try {
                var value=CitySaves.NormalizeName(name.Text);if(value.Length==0){feedback.Text="Digite o nome da cidade";return;}
                if(id==_cityId&&!TrySaveCity()){feedback.Text="A partida atual não pôde ser salva. O nome foi mantido.";return;}
                value=CitySaves.Rename(id,value);
                if(id==_cityId)_sim.State.Name=value;
                ShowCities();SetStatus("Cidade renomeada para "+value+" · partida e arquivo mantidos");
            }catch(Exception exception){feedback.Text="Não foi possível renomear: "+exception.Message;}
        }
        var confirm=Button(actions,"Confirmar nome",Rename);confirm.Disabled=string.IsNullOrWhiteSpace(name.Text);
        name.TextChanged+=value=>confirm.Disabled=string.IsNullOrWhiteSpace(value);name.TextSubmitted+=_=>Rename();
        Button(actions,"Cancelar",ShowCities);name.GrabFocus();name.SelectAll();
    }

    void ShowPeople()
    {
        var box = Modal("SIMs e empresas"); var state = _sim.State;
        Text(box, "Moradores · " + _sim.Population, 23);
        foreach (var family in state.Families.Where(f => f.Members.Any(id => state.Citizens.Any(c => c.Id == id && c.Alive)))) {
            var home = state.Buildings.FirstOrDefault(b => b.Id == family.HomeId && !b.Removed);
            var members = state.Citizens.Where(c => c.Alive && family.Members.Contains(c.Id)).ToList();
            var group = Card(box, home == null ? "Família · sem residência na cidade" : "Família · " + home.Definition.Name + " #" + home.Id);
            foreach (var citizen in members) {
                var id = citizen.Id; Button(group, citizen.Name + " · " + citizen.Activity, () => InspectCitizen(id), "Inspecionar pessoa · carteira " + Cash(_sim.Balance(citizen.Account)));
                Muted(group, "Carteira: " + Cash(_sim.Balance(citizen.Account)));
            }
        }
        // Keep agents without a family record inspectable, including external workers.
        foreach (var citizen in state.Citizens.Where(c => c.Alive && !state.Families.Any(f => f.Members.Contains(c.Id)))) {
            var id = citizen.Id; Button(box, citizen.Name + " · " + citizen.Activity + " · carteira " + Cash(_sim.Balance(citizen.Account)), () => InspectCitizen(id));
        }
        Text(box, "Empresas", 23);
        foreach (var company in state.Companies) {
            var group = Card(box, company.Name, company.Active ? "Em atividade" : "Encerrada");
            Text(group, "Caixa: " + Cash(_sim.Balance(company.Account)), 20); Text(group, company.Decision, 14);
            foreach (var id in company.Buildings) {
                var building = state.Buildings.FirstOrDefault(b => b.Id == id && !b.Removed); if (building == null) continue;
                var target = id; Button(group, building.Definition.Name, () => { ClearTool(); _selectedBuilding = target; RefreshInspection(); CloseModal(); });
            }
        }
        var candidates = Card(box, "Candidatos externos", "A possibilidade de chegar depende da decisão e do dinheiro de cada família ou empresa.");
        Text(candidates, "Famílias aguardando: " + state.FamilyCandidates.Count(c => !c.Migrated) + " · empresas aguardando: " + state.CompanyCandidates.Count(c => !c.Entered));
        foreach (var cause in state.FamilyCandidates.Where(c => !c.Migrated).Select(c => c.Cause).Distinct()) Muted(candidates, cause);
    }

    void ShowAlerts()
    {
        var box = Modal("Causas, obras e entregas"); var state = _sim.State;
        var serviceRow = ResponsiveRow(box);
        void Service(string name, double capacity, double demand, string detail)
        {
            var group = Card(serviceRow, name); var value = Text(group, capacity.ToString("N0", Pt) + " / " + demand.ToString("N0", Pt), 23);
            value.AddThemeColorOverride("font_color", new Color(demand > capacity + .5 ? "ffc58a" : "deeedc"));
            Muted(group, "Capacidade / demanda"); Muted(group, detail);
        }
        Service("Água", state.PotableCapacity, state.WaterDemand, "Captação: " + state.RawWaterCapacity.ToString("N1", Pt));
        Service("Energia", state.ElectricityCapacity, state.ElectricityDemand, "Vento: " + state.WindFactor.ToString("P0", Pt));
        Service("Esgoto", state.SewageCapacity, state.SewageDemand, "Sem tratamento: " + state.UntreatedSewage.ToString("N1", Pt));
        Muted(box, "Poluição da fonte natural: " + state.LakePollution.ToString("P1", Pt));
        var projects = state.Projects.Where(p => p.Status != WorkStatus.Completed).ToList();
        Text(box, "Obras · " + projects.Count, 23);
        if (projects.Count == 0) Muted(box, "Nenhuma obra pendente.");
        foreach (var project in projects) {
            var id = project.Id; var building = state.Buildings.FirstOrDefault(b => b.Id == project.BuildingId);
            var group = Card(box, (project.IsMaintenance ? "Manutenção" : building?.Definition.Name ?? "Rua 1+1") + " · " + project.Progress.ToString("P0", Pt));
            Text(group, project.Cause, 16);
            var progress = new ProgressBar { MinValue = 0, MaxValue = 100, Value = project.Progress * 100, ShowPercentage = false, CustomMinimumSize = new(0, 8) }; group.AddChild(progress);
            Muted(group, "Pago: " + Cash(project.CostPaid) + " · compromisso: " + Cash(project.BudgetRemaining));
            Button(group, "Inspecionar obra #" + id, () => { ClearTool(); ClearSelection(); _selectedProject = id; RefreshInspection(); CloseModal(); });
        }
        var shipments = state.Shipments.Where(s => !s.Complete).ToList(); Text(box, "Entregas em trânsito · " + shipments.Count, 23);
        if (shipments.Count == 0) Muted(box, "Nenhuma entrega em trânsito.");
        foreach (var shipment in shipments) {
            var vehicle = state.Vehicles.First(v => v.Id == shipment.VehicleId); var id = vehicle.Id;
            var group = Card(box, "Entrega #" + shipment.Id); Text(group, vehicle.Cause);
            Muted(group, string.Join(" · ", shipment.Cargo.Select(k => ResourceName(k.Key) + " " + k.Value.ToString("N1", Pt))));
            Button(group, "Inspecionar caminhão #" + id, () => { ClearTool(); ClearSelection(); _selectedVehicle = id; RefreshInspection(); CloseModal(); });
        }
        var buildings = Card(box, "Construções");
        foreach (var building in state.Buildings.Where(b => !b.Removed).OrderBy(b => b.Completed ? 1 : 0).ThenBy(b => b.Definition.Private ? 1 : 0)) {
            var id = building.Id; Button(buildings, building.Definition.Name + " · " + building.Cause, () => { ClearTool(); ClearSelection(); _selectedBuilding = id; RefreshInspection(); CloseModal(); });
        }
        var events = Card(box, "História da cidade");
        foreach (var entry in state.CityEvents.TakeLast(20).Reverse()) Muted(events, entry.At.ToString("dd/MM HH:mm") + " · " + entry.Description);
    }

    void ShowFinances()
    {
        var box = Modal("Finanças municipais"); var state = _sim.State;
        var balances = ResponsiveRow(box);
        Metric(balances, "Disponível", Cash(_sim.Available)); Metric(balances, "Reservado", Cash(_sim.Committed)); Metric(balances, "Caixa total", Cash(_sim.CityBalance));
        Muted(box, "O valor reservado já está comprometido com obras. Não é dinheiro livre para novas encomendas.");
        var calendar = Card(box, "Quando o dinheiro entra e sai", "As cobranças dependem de consumo e pagamentos reais; não há receita garantida.");
        Text(calendar, "Água, energia e salários · próxima apuração: " + state.NextDaily.ToString("dd/MM/yyyy HH:mm"), 14);
        Text(calendar, "Impostos sobre imóveis · próxima cobrança: " + state.NextTaxDate.ToString("dd/MM/yyyy HH:mm"), 14);
        Muted(calendar, "Salários essenciais acumulados: " + Cash(_sim.MunicipalCriticalWagesPending) + ". São preservados antes de cobrar empréstimos e outras dívidas.");
        Text(calendar, "Tarifa por consumo · água: " + Cash(state.WaterUnitPrice) + " · energia: " + Cash(state.ElectricityUnitPrice), 14);
        Muted(calendar, "Referência de custo operacional; só consumo efetivo gera conta. Capacidade ociosa não vira tarifa infinita.");
        var municipal = state.Ledger.Where(l => l.From == "city" || l.To == "city").ToList();
        var flows = Card(box, "Entradas e saídas efetivas", "Acumulado dos registros da cidade, desde " + (municipal.Count == 0 ? state.Now : municipal.Min(l => l.At)).ToString("dd/MM/yyyy HH:mm"));
        var table = new GridContainer { Columns = 3, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; table.AddThemeConstantOverride("h_separation", 22); flows.AddChild(table);
        Text(table, "Categoria", 14); Text(table, "Entrou", 14); Text(table, "Saiu", 14);
        foreach (var group in municipal.GroupBy(l => l.Category)) {
            Text(table, group.Key, 14); Text(table, Cash(group.Where(l => l.To == "city").Sum(l => l.Amount)), 14); Text(table, Cash(group.Where(l => l.From == "city").Sum(l => l.Amount)), 14);
        }
        if (municipal.Count == 0) Muted(flows, "Nenhuma transação municipal ainda.");
        var taxes = Card(box, "Impostos patrimoniais anuais (%)", "Cobrança mensal: valor de mercado atual × alíquota anual ÷ 12.");
        var categories = new[] { "Residencial · baixa densidade", "Residencial · alta densidade", "Comercial · baixa densidade", "Comercial · alta densidade", "Industrial · baixa densidade", "Industrial · alta densidade" };
        for (var i = 0; i < 6; i++) {
            var index = i; var row = new HBoxContainer(); taxes.AddChild(row); Text(row, categories[i], 14);
            var rate = new SpinBox { MinValue = 0, MaxValue = 8, Step = .1, Value = (double)state.TaxRates[i] * 100, CustomMinimumSize = new(120, 36) }; row.AddChild(rate);
            rate.ValueChanged += value => _sim.SetTax(index, (decimal)value / 100);
        }
        var borrowing = Card(box, "Empréstimo manual", "Limite de principal em aberto: " + Cash(state.Tuning.LoanLimit) + " · prazo: " + state.Tuning.LoanMonths + " meses");
        Muted(borrowing, "Juros: " + (state.Tuning.LoanMonthlyInterest * 100).ToString("N1", Pt) + "% ao mês sobre o principal restante. Atraso impede novo empréstimo.");
        foreach (var contract in state.Loans.Where(l => l.Principal > 0)) Muted(borrowing, "Contrato #" + contract.Id + " · principal " + Cash(contract.Principal) + " · " + contract.RemainingMonths + " parcelas restantes · próximo vencimento " + contract.NextDue.ToString("dd/MM/yyyy"));
        var loan = new SpinBox { MinValue = .01, MaxValue = (double)state.Tuning.LoanLimit, Step = .01, Value = 10000 }; borrowing.AddChild(loan);
        var feedback = Text(borrowing, "", 14); Button(borrowing, "Contratar empréstimo", () => { var result = _sim.Borrow((decimal)loan.Value); feedback.Text = result.Message; });
        var debts = Card(box, "Dívidas existentes");
        foreach (var debt in state.Obligations.Where(d => d.Outstanding > 0)) Muted(debts, AccountName(debt.Debtor) + " → " + AccountName(debt.Creditor) + " · " + debt.Kind + " · " + Cash(debt.Outstanding));
        if (!state.Obligations.Any(d => d.Outstanding > 0)) Muted(debts, "Nenhuma dívida pendente.");
        var transactions = Card(box, "Últimas transações");
        foreach (var entry in state.Ledger.TakeLast(20).Reverse()) Muted(transactions, entry.At.ToString("dd/MM HH:mm") + " · " + AccountName(entry.From) + " → " + AccountName(entry.To) + " · " + Cash(entry.Amount) + " · " + entry.Reason);
    }

    void ShowHelp()
    {
        var box = Modal("Controles");
        var construction = Card(box, "Construir", "Escolha uma imagem no catálogo. Confira o modelo e o custo antes de clicar. A ferramenta permanece pronta para repetir.");
        Text(construction, "Prédio · clique para encomendar ou arraste para formar uma fileira.", 16);
        Text(construction, "Rua · clique no início e depois no fim. Tab troca o lado do L.", 16);
        Muted(construction, "R gira o prédio · Enter também encomenda a prévia válida · Esc ou clique curto com botão direito cancela a ferramenta. A prévia não gasta dinheiro.");
        Text(construction, "Demolir · escolha a ferramenta no catálogo e clique na rua. Confira orçamento e efeitos antes de confirmar.", 16);
        var camera = Card(box, "Olhar a cidade"); Text(camera, "W A S D ou botão central · mover\nRoda · aproximar\nQ / E ou arrastar com botão direito · girar", 16);
        var time = Card(box, "Tempo e salvamento"); Text(time, "Espaço · pausar\n1 · tempo real\n2 · 15x\n3 · 60x\nF5 · salvar", 16);
        var inspection = Card(box, "Entender o que está acontecendo");
        Text(inspection, "Clique em prédios, pessoas e caminhões para inspecionar. Os painéis mostram causas, dinheiro e entregas reais.", 16);
        Muted(inspection, "As obras precisam de ruas conectadas ao exterior. Casas e negócios não garantem moradores ou compradores. A Prefeitura libera as ferramentas ao confirmar seu projeto. Água e energia dependem das instalações e trabalhadores reais.");
    }
}
