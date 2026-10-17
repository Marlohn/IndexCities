namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    public CityState State {get;}
    public decimal CityBalance=>Balance("city");
    public decimal Committed=>State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress).Sum(p=>p.BudgetRemaining);
    public decimal Available=>Math.Max(0,CityBalance-Committed);
    public decimal MonetaryTotal=>State.Wallets.Values.Sum();
    public IReadOnlyList<Building> Buildings=>State.Buildings;
    public double Quantum=>2;
    public int Population=>State.Citizens.Count(c=>c.Alive&&c.Resident&&c.InsideMap);
    public CitySimulation(int seed,SimulationTuning? tuning=null)
    {
        State=new(){Seed=seed,Name="Cidade "+seed,Tuning=tuning??new(),RandomState=(ulong)(uint)seed+0x9E3779B97F4A7C15UL};
        if(State.Tuning.CityInitialMoney>State.Tuning.TotalMoney||State.Tuning.TotalMoney<=0)throw new ArgumentException("Distribuição monetária inválida");
        State.WaterUnitPrice=State.Tuning.WaterUnitPrice;State.ElectricityUnitPrice=State.Tuning.ElectricityUnitPrice;
        State.Entry=new(-State.Tuning.MapHalfSize,0);
        State.LakeCentre=new(State.Tuning.MapHalfSize*.64+(NextRandom()-.5)*24,State.Tuning.MapHalfSize*.38+(NextRandom()-.5)*24);
        State.LakeRadius=28+NextRandom()*12;
        State.ConstructionStateVersion=1;State.SchemaVersion=2;State.LakeBoundary=LakeGeometry.GenerateBoundary(seed,State.LakeCentre,State.LakeRadius);
        State.Wallets["global"]=State.Tuning.TotalMoney;State.Wallets["city"]=0;
        Transfer("global","city",State.Tuning.CityInitialMoney,"Capital inicial","Distribuição da oferta monetária",null);
        State.NextDaily=State.Now.Date.AddDays(1);State.NextTaxDate=State.Now.Date.AddMonths(1);
        foreach(var k in Enum.GetValues<ResourceKind>())State.OutsideStock[k]=k is ResourceKind.Food or ResourceKind.Fuel?100000:1000000;
        for(var x=State.Entry.X;x<=State.Entry.X+64+.01;x+=State.Tuning.RoadStep)AddRoad(new(x,0),true);
        var truckBodies=new[]{TruckBody.Dump,TruckBody.Dump,TruckBody.Dump,TruckBody.Mixer,TruckBody.Flatbed,TruckBody.Cargo};
        for(var i=0;i<State.Tuning.ExternalTruckCount;i++)State.Vehicles.Add(new(){Id=Id(),Body=truckBodies[i%truckBodies.Length],External=true,ExternalDriverId="motorista externo "+(i+1),Status=VehicleStatus.Outside,Position=State.Entry});
        for(var i=0;i<State.Tuning.ExternalFamilyCandidates;i++)State.FamilyCandidates.Add(new(){Id=i+1,Adults=NextRandom()<.55?2:1,Capital=Money(State.Tuning.MigrantCapitalMinimum+(decimal)NextRandom()*(State.Tuning.MigrantCapitalMaximum-State.Tuning.MigrantCapitalMinimum)),RiskTolerance=.35+NextRandom()*.65});
        for(var i=0;i<State.Tuning.ExternalCompanyCandidates;i++)State.CompanyCandidates.Add(new(){Id=i+1,Capital=Money(State.Tuning.CompanyCapitalMinimum+(decimal)NextRandom()*(State.Tuning.CompanyCapitalMaximum-State.Tuning.CompanyCapitalMinimum)),RiskTolerance=.25+NextRandom()*.75});
        RebuildRoadGraph();CheckInvariants();
    }
    public CitySimulation(CityState state){State=state;LakeGeometry.Validate(state);RebuildRoadGraph(false);CheckInvariants();}
    int Id()=>State.NextId++;
    double NextRandom(){var x=State.RandomState;x^=x>>12;x^=x<<25;x^=x>>27;State.RandomState=x;return (x*2685821657736338717UL>>11)*(1.0/(1UL<<53));}
    public decimal Balance(string account)=>State.Wallets.GetValueOrDefault(account);
    static decimal Money(decimal x)=>decimal.Round(x,2,MidpointRounding.AwayFromZero);
    decimal Spendable(string account)=>account=="city"?Available:Balance(account);
    bool Transfer(string from,string to,decimal amount,string category,string reason,int? entity)
    {
        amount=Money(amount);if(amount<0)throw new InvalidOperationException("Pagamento negativo");
        if(amount==0||from==to)return true;
        if(!State.Wallets.ContainsKey(from)||!State.Wallets.ContainsKey(to)||Balance(from)<amount)return false;
        State.Wallets[from]-=amount;State.Wallets[to]+=amount;
        State.Ledger.Add(new(State.Now,from,to,amount,category,reason,entity));return true;
    }
    decimal Pay(string from,string to,decimal due,string category,string reason,int? entity,bool preserveEssentials=false)
    {
        var funds=Spendable(from);
        if(preserveEssentials&&from.StartsWith("sim:"))funds=Math.Max(0,funds-20);
        var amount=Money(Math.Min(Math.Max(0,funds),due));Transfer(from,to,amount,category,reason,entity);return amount;
    }
    void Owe(string from,string to,decimal due,string kind,int? building,int? loanId=null,decimal principal=0)
    {
        due=Money(due);if(due<=0)return;
        State.Obligations.Add(new(){Id=Id(),Debtor=from,Creditor=to,Outstanding=due,Kind=kind,BuildingId=building,Due=State.Now,LoanId=loanId,PrincipalOutstanding=Money(Math.Min(due,principal))});
    }
    void Personal(Citizen c,string description){c.History.Add(new(State.Now,description));c.Decision=description;}
    void Event(string description){State.CityEvents.Add(new(State.Now,description));State.LastMessage=description;}
    Building? BuildingById(int? id)=>id is null?null:State.Buildings.FirstOrDefault(b=>b.Id==id&&!b.Removed);
    Company? CompanyById(int? id)=>id is null?null:State.Companies.FirstOrDefault(c=>c.Id==id);
    Family? FamilyById(int? id)=>id is null?null:State.Families.FirstOrDefault(f=>f.Id==id);
    public void Advance(double seconds)
    {
        if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
        var remaining=seconds;while(remaining>1e-8){var dt=Math.Min(Quantum,remaining);Step(dt);remaining-=dt;}
    }
    void Step(double dt)
    {
        MoveVehicles(dt);ReturnPavingMachines(dt);MoveCitizens(dt);var completedConstruction=ProgressConstruction(dt);UpdateMaintenanceCondition(dt);UpdateInfrastructure(dt);OperateBusinesses(dt);UpdateNeeds(dt);
        State.ElapsedSeconds+=dt;State.Now=State.Now.AddSeconds(dt);
        // Construction is physical work, not a periodic market decision. Dispatch
        // available vehicles and crews on the next simulated step, including when
        // a vehicle returns or a newly completed street restores access.
        var reviewEconomy=completedConstruction||State.ElapsedSeconds>=State.EconomyNextAt;
        if(reviewEconomy){State.EconomyNextAt=State.ElapsedSeconds+State.Tuning.EconomyInterval;ReviewMarkets();ReviewJobs();ScheduleMaintenance();}
        OrderConstruction();
        if(reviewEconomy){RestockBusinesses();ReviewEmigration();RecoverDebt();}
        ProgressBuildingDemolitions(reviewEconomy);
        while(State.Now>=State.NextDaily){DailyFinance();State.NextDaily=State.NextDaily.AddDays(1);}
        while(State.Now>=State.NextTaxDate){PropertyTaxes();State.NextTaxDate=State.NextTaxDate.AddMonths(1);}
        ProcessLoans();
    }
    public void CheckInvariants()
    {
        if(State.SchemaVersion is not (1 or 2))throw new InvalidOperationException("Versão de save não suportada");
        if(State.SchemaVersion==2&&State.LakeBoundary is not {Count:>=8})throw new InvalidOperationException("Contorno do lago ausente");
        if(State.Wallets.Values.Any(x=>x<0)||MonetaryTotal!=State.Tuning.TotalMoney)throw new InvalidOperationException("Oferta monetária ou saldo inválido");
        if(Committed>CityBalance)throw new InvalidOperationException("Compromissos maiores que o Caixa");
        if(State.Buildings.Any(b=>b.Stock.Values.Any(x=>x<-.000001))||State.OutsideStock.Values.Any(x=>x<-.000001))throw new InvalidOperationException("Estoque negativo");
        if(State.Vehicles.Any(v=>v.Fuel<-.000001||v.Cargo.Values.Any(x=>x<-.000001)))throw new InvalidOperationException("Combustível/carga negativos");
        if(State.Projects.Any(p=>p.BudgetRemaining<0||p.Delivered.Any(x=>x.Value>p.Required.GetValueOrDefault(x.Key)+.000001)))throw new InvalidOperationException("Obra inconsistente");
        if(State.Citizens.Select(c=>c.Id).Distinct().Count()!=State.Citizens.Count)throw new InvalidOperationException("SIM duplicado");
        if(State.Buildings.Where(b=>b.CompanyId!=null).Any(b=>b.OwnerAccount!="company:"+b.CompanyId))throw new InvalidOperationException("Titularidade empresarial inválida");
        foreach(var f in State.Families)if(f.Members.Any(id=>State.Citizens.All(c=>c.Id!=id||c.FamilyId!=f.Id)))throw new InvalidOperationException("Família inconsistente");
    }
    public CommandResult SetTax(int category,decimal rate)
    {
        if(!State.CityHallPlaced)return new(false,"Confirme o projeto da Prefeitura para liberar a gestão");
        if(category<0||category>=6||rate<0||rate>.08m)return new(false,"Alíquota fora dos limites de calibração");State.TaxRates[category]=rate;Event("Alíquota alterada; afeta cobranças e decisões econômicas reais");return new(true,State.LastMessage);
    }
    public CommandResult Borrow(decimal amount)
    {
        if(!State.CityHallPlaced)return new(false,"Confirme o projeto da Prefeitura para liberar a gestão");
        amount=Money(amount);if(amount<=0||amount>State.Tuning.LoanLimit||State.Loans.Sum(l=>l.Principal)+amount>State.Tuning.LoanLimit)return new(false,"Limite de crédito insuficiente");
        if(State.Obligations.Any(o=>o.Debtor=="city"&&o.Creditor=="global"&&o.Kind=="Empréstimo"&&o.Outstanding>0))return new(false,"Há parcelas vencidas: crédito indisponível");
        if(!Transfer("global","city",amount,"Empréstimo","Crédito municipal solicitado pelo jogador",null))return new(false,"Reserva Global insuficiente");
        State.Loans.Add(new(){Id=Id(),Principal=amount,ScheduledPrincipal=amount,MonthlyPrincipal=Money(amount/State.Tuning.LoanMonths),Rate=State.Tuning.LoanMonthlyInterest,RemainingMonths=State.Tuning.LoanMonths,NextDue=State.Now.AddMonths(1)});Event("Empréstimo contratado com dinheiro existente");return new(true,State.LastMessage);
    }
}
