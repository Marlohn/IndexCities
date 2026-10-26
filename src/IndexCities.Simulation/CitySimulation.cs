namespace IndexCities.Simulation;

public sealed class StepCostProfile
{
    public static readonly string[] Names = { "vehicles", "paving-returns", "citizen-mobility", "construction", "maintenance", "relocation", "infrastructure", "businesses", "needs", "economy-review", "construction-dispatch", "market-followups", "demolition-finance-loans" };
    public long[] Ticks { get; } = new long[Names.Length];
    public static readonly string[] DetailNames = { "infra-presence", "infra-demands", "infra-electricity", "infra-water", "infra-sewage-pollution", "infra-finalize", "needs-pollution", "needs-citizens" };
    public long[] DetailTicks { get; } = new long[DetailNames.Length];
    public static readonly string[] VehicleDetailNames = { "entry-check", "operations", "entry-events", "movement" };
    public long[] VehicleTicks { get; } = new long[VehicleDetailNames.Length];
    public static readonly string[] VehicleScanNames = { "lane-leader-scan", "bend-leader-scan" };
    public long[] VehicleScanTicks { get; } = new long[VehicleScanNames.Length];
    public long[] VehicleScanVisits { get; } = new long[VehicleScanNames.Length];
    public long[] VehicleScanHits { get; } = new long[VehicleScanNames.Length];
    public long BendPrefilterCandidates { get; internal set; }
    public long BendPrefilterNear { get; internal set; }
    // PR57-only observation: distance of every actual lane HoldFor candidate.
    // No spatial filtering or changes to vehicle movement are performed.
    public long[] LaneHoldDistanceBands { get; } = new long[4]; // <=32m, <=128m, <=512m, >512m
    public long[] LaneLeaderSelectedBands { get; } = new long[4];
    public long[] LaneLeaderPotentialLimitBands { get; } = new long[4];
    public long LaneLeaderFarZeroDistance { get; private set; }
    public double LaneLeaderSelectedMaxDistanceSquared { get; private set; }
    public long LaneHoldEntryFrameHits { get; private set; }
    public double LaneHoldMaxDistanceSquared { get; private set; }
    internal void ObserveLaneHoldDistance(Point from, Point to, bool entryFrame)
    {
        var dx=from.X-to.X;var dz=from.Z-to.Z;
        var distanceSquared=dx*dx+dz*dz;
        var band=distanceSquared<=32*32?0:distanceSquared<=128*128?1:distanceSquared<=512*512?2:3;
        LaneHoldDistanceBands[band]++;
        if(distanceSquared>LaneHoldMaxDistanceSquared)LaneHoldMaxDistanceSquared=distanceSquared;
        if(entryFrame)LaneHoldEntryFrameHits++;
    }
    // Observe the final nearest leader selected by the unchanged full scan.
    // A potential limit is relative to the reachable step BEFORE other geometry/junction restrictions;
    // it is not proof that the candidate caused an actual stop or changed the final state.
    internal void ObserveLaneLeaderSelection(Point from, Point to, double leaderDistance, double reachableStep)
    {
        var dx=from.X-to.X;var dz=from.Z-to.Z;
        var distanceSquared=dx*dx+dz*dz;
        var band=distanceSquared<=32*32?0:distanceSquared<=128*128?1:distanceSquared<=512*512?2:3;
        LaneLeaderSelectedBands[band]++;
        if(distanceSquared>LaneLeaderSelectedMaxDistanceSquared)LaneLeaderSelectedMaxDistanceSquared=distanceSquared;
        if(Math.Max(0,leaderDistance-8)<reachableStep-1e-9)LaneLeaderPotentialLimitBands[band]++;
        if(band==3&&leaderDistance==0)LaneLeaderFarZeroDistance++;
    }
    internal void MarkVehicleScan(int detail, long started, int visits, int hits)
    {
        VehicleScanTicks[detail] += System.Diagnostics.Stopwatch.GetTimestamp() - started;
        VehicleScanVisits[detail] += visits;
        VehicleScanHits[detail] += hits;
    }
    public long VehicleBatchedSteps { get; internal set; }
    public long VehicleUnsplitSteps { get; internal set; }
    public long VehicleSlices { get; internal set; }
    internal void MarkVehicle(int detail, ref long started)
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        VehicleTicks[detail] += now - started;
        started = now;
    }
    public long Steps { get; private set; }
    internal void Mark(int phase, ref long started)
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        Ticks[phase] += now - started;
        started = now;
    }
    internal void MarkDetail(int detail, ref long started)
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        DetailTicks[detail] += now - started;
        started = now;
    }
    internal void FinishStep() => Steps++;
}

public sealed partial class CitySimulation
{
    public CityState State {get;}
    // PR57-only diagnostic observer; not part of CityState or save.
    public StepCostProfile? CostProfile { get; set; }
    public decimal CityBalance=>Balance("city");
    public decimal Committed=>State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress).Sum(p=>p.BudgetRemaining)+State.MunicipalRelocations.Where(r=>r.Active).Sum(r=>r.RemovalReserved+r.TransferReserved);
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
        State.QuantityStateVersion=2;State.ConstructionStateVersion=1;State.VehicleStateVersion=1;State.SchemaVersion=2;State.LakeBoundary=LakeGeometry.GenerateBoundary(seed,State.LakeCentre,State.LakeRadius);
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
    readonly EntityIndex<Building> _buildingIndex=new(b=>b.Id);readonly EntityIndex<Company> _companyIndex=new(c=>c.Id);readonly EntityIndex<Family> _familyIndex=new(f=>f.Id);
    readonly EntityIndex<Citizen> _citizenIndex=new(c=>c.Id);readonly EntityIndex<Vehicle> _vehicleIndex=new(v=>v.Id);
    Building? BuildingById(int? id)=>id is null?null:_buildingIndex.Find(State.Buildings,id.Value) is {Removed:false} b?b:null;
    Company? CompanyById(int? id)=>id is null?null:_companyIndex.Find(State.Companies,id.Value);
    Family? FamilyById(int? id)=>id is null?null:_familyIndex.Find(State.Families,id.Value);
    Citizen? CitizenById(int? id)=>id is null?null:_citizenIndex.Find(State.Citizens,id.Value);
    Vehicle? VehicleById(int? id)=>id is null?null:_vehicleIndex.Find(State.Vehicles,id.Value);
    // Same contract as Enumerable.First: a missing Id is an inconsistent state.
    Citizen CitizenWithId(int id)=>CitizenById(id)??throw new InvalidOperationException("Sequence contains no matching element");
    public void Advance(double seconds)
    {
        if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
        Pr57BeginBuildingAccessAdvance();
        // A public Advance boundary permits direct external State edits (including tests).
        // Rebuild the opt-in agenda there; internal quanta still use incremental wakeups.
        if(Pr57CitizenAgendaEnabled)_pr57AgendaReady=false;
        // Public Advance boundaries permit arbitrary external State edits.
        _pr57ConstructionWaitAccess.Clear();
        if(Pr57MaintenanceScheduleEnabled)_pr57MaintenanceScheduleReady=false;
        if(Pr57RouteTreeEnabled)
        {
            _pr57RouteTreeParents.Clear();_pr57RouteTreeCalls.Clear();
            _pr57RouteTreeEpoch=_roadEpoch;_pr57RouteTreeActive=true;
            if(!_pr57RouteTreeAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
            {
                Console.WriteLine("PR57_ROUTE_TREE active=True");
                _pr57RouteTreeAnnounced=true;
            }
        }
        else if(!_pr57RouteTreeAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine("PR57_ROUTE_TREE active=False");
            _pr57RouteTreeAnnounced=true;
        }
        _crosswalkBands.Clear();_crosswalkBandsActive=true;BeginWalkRoutes();
        try{var remaining=seconds;while(remaining>1e-8){var dt=Math.Min(Quantum,remaining);Step(dt);remaining-=dt;}}
        finally{_crosswalkBandsActive=false;_crosswalkBands.Clear();EndWalkRoutes();_pr57RouteTreeActive=false;_pr57RouteTreeParents.Clear();_pr57RouteTreeCalls.Clear();Pr57EndBuildingAccessAdvance();}
    }
    // PR57 opt-in: stable living-citizen roster, invalidated on death or population growth.
    // Rebuilt before Step, never while a Step enumerates it. Does not alter CityState/save.
    static readonly bool Pr57LivingRosterEnabled=Environment.GetEnvironmentVariable("PR57_LIVING_ROSTER")=="1";
    readonly List<Citizen> _pr57LivingRoster=[];
    bool _pr57LivingRosterDirty=true,_pr57LivingRosterAnnounced;
    int _pr57LivingRosterCount=-1;
    IEnumerable<Citizen> Pr57LiveCitizens()=>Pr57LivingRosterEnabled?_pr57LivingRoster:State.Citizens.Where(c=>c.Alive);
    IEnumerable<Citizen> Pr57NeedsCandidates()=>Pr57LivingRosterEnabled?_pr57LivingRoster:State.Citizens;
    void Pr57BeginStep()
    {
        _pr57StaffPrepared=false;
        if(!Pr57LivingRosterEnabled)return;
        if(_pr57LivingRosterDirty||_pr57LivingRosterCount!=State.Citizens.Count)
        {
            _pr57LivingRoster.Clear();
            foreach(var c in State.Citizens)if(c.Alive)_pr57LivingRoster.Add(c);
            _pr57LivingRosterCount=State.Citizens.Count;
            _pr57LivingRosterDirty=false;
        }
        if(!_pr57LivingRosterAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine($"PR57_LIVING_ROSTER active=True living={_pr57LivingRoster.Count} citizens={State.Citizens.Count}");
            _pr57LivingRosterAnnounced=true;
        }
    }
    void Step(double dt)
    {
        Pr57WorkAccessBeginStep();
        Pr57BeginStep();
        if(!Pr57LivingRosterEnabled&&!_pr57LivingRosterAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine("PR57_LIVING_ROSTER active=False");
            _pr57LivingRosterAnnounced=true;
        }
        var profile = CostProfile;
        var started = profile is null ? 0L : System.Diagnostics.Stopwatch.GetTimestamp();
        MoveVehicles(dt);
        if(profile is not null)profile.Mark(0,ref started);
        ReturnPavingMachines(dt);
        if(profile is not null)profile.Mark(1,ref started);
        MoveCitizens(dt);
        if(profile is not null)profile.Mark(2,ref started);
        var completedConstruction=ProgressConstruction(dt);
        if(completedConstruction&&Pr57MaintenanceScheduleEnabled)_pr57MaintenanceScheduleReady=false;
        if(profile is not null)profile.Mark(3,ref started);
        UpdateMaintenanceCondition(dt);
        if(profile is not null)profile.Mark(4,ref started);
        ProgressMunicipalRelocations(dt);
        if(profile is not null)profile.Mark(5,ref started);
        UpdateInfrastructure(dt);
        if(profile is not null)profile.Mark(6,ref started);
        OperateBusinesses(dt);
        if(profile is not null)profile.Mark(7,ref started);
        UpdateNeeds(dt);
        if(profile is not null)profile.Mark(8,ref started);
        State.AddElapsedSeconds(dt);State.Now=State.Now.AddSeconds(dt);
        var reviewEconomy=completedConstruction||State.ElapsedSeconds>=State.EconomyNextAt;
        if(reviewEconomy){State.EconomyNextAt=State.ElapsedSeconds+State.Tuning.EconomyInterval;SeekHomesForAllDisplaced();ReviewMarkets();ReviewJobs();ScheduleMaintenance();}
        if(profile is not null)profile.Mark(9,ref started);
        OrderConstruction();
        if(profile is not null)profile.Mark(10,ref started);
        if(reviewEconomy){RetryPurchasesAwaitingDestination();RestockBusinesses();ReviewEmigration();RecoverDebt();RetryCargoAwaitingDestination();ReviewVehicleRevisions();ResumeHeldDeliveries();RefuelIdleShopTows();}
        if(profile is not null)profile.Mark(11,ref started);
        ProgressBuildingDemolitions();
        while(State.Now>=State.NextDaily){DailyFinance();State.NextDaily=State.NextDaily.AddDays(1);}
        while(State.Now>=State.NextTaxDate){PropertyTaxes();State.NextTaxDate=State.NextTaxDate.AddMonths(1);}
        ProcessLoans();
        if(profile is not null)profile.Mark(12,ref started);
        if(profile is not null)profile.FinishStep();
    }
    public void CheckInvariants()
    {
        if(State.QuantityStateVersion!=2)throw new InvalidOperationException("Versão de quantidades do save não suportada");
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
