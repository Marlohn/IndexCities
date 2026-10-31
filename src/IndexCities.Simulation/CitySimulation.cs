namespace IndexCities.Simulation;

public sealed class StepCostProfile
{
    public static readonly string[] Names = { "vehicles", "paving-returns", "citizen-mobility", "construction", "maintenance", "relocation", "infrastructure", "businesses", "needs", "economy-review", "construction-dispatch", "market-followups", "demolition-finance-loans" };
    public long[] Ticks { get; } = new long[Names.Length];
    // PR57 #160: opt-in, observational thread-local allocation census.
    // These deltas are not retained heap, state mutations, or causal read/write sets.
    public long[] PhaseAllocatedBytes { get; } = new long[Names.Length];
    public long PhaseAllocationSteps { get; private set; }
    static readonly bool Pr57PhaseAllocEnabled=Environment.GetEnvironmentVariable("PR57_PHASE_ALLOC_PROBE")=="1";
    long _pr57PhaseAllocationBaseline;
    bool _pr57PhaseAllocationTracking;
    internal void BeginAllocationCensus()
    {
        if(!Pr57PhaseAllocEnabled)return;
        _pr57PhaseAllocationBaseline=GC.GetAllocatedBytesForCurrentThread();
        _pr57PhaseAllocationTracking=true;
        PhaseAllocationSteps++;
    }
    internal void ResetAllocationCensusBaseline()
    {
        if(_pr57PhaseAllocationTracking)_pr57PhaseAllocationBaseline=GC.GetAllocatedBytesForCurrentThread();
    }
    // PR57 sampled, intrusive observation; never consulted by gameplay.
    public Pr57PhaseEffects? PhaseEffects { get; set; }
    // PR57 #183: sampled read-only cross-phase presence/crew candidate census.
    public Pr57CrossPhasePresenceProbe? CrossPhasePresence { get; set; }
    // PR57 observational census: counts actual enumeration visits, never controls simulation.
    public long ConstructionOrderProjects, ConstructionProgressProjects, ConstructionReserveProjects;
    // PR57 #282: optional, observation-only project timing and selected-field census.
    public Pr57ConstructionProjectProbe? Pr57ConstructionProjects { get; set; }
    public long ConstructionWorkAccessCalls, ConstructionBuildingAccessCalls;
    public long ConstructionRoadQueries, ConstructionRoadVisits;
    public long ConstructionShipmentQueries, ConstructionShipmentVisits;
    // PR57 intrusive observation only. Net record counts do NOT imply causal inactivity.
    public long ConstructionDispatchActiveSteps, ConstructionDispatchShipmentChangedSteps, ConstructionDispatchShipmentNetDelta;
    public long ConstructionDispatchLedgerChangedSteps, ConstructionDispatchLedgerNetDelta, ConstructionDispatchNoShipmentOrLedgerSteps;
    // PR57 intrusive cross-phase Cause observation; never influences gameplay.
    public long Pr57CauseObserved, Pr57CauseProgressChanged, Pr57CauseIntermediateChanged, Pr57CauseDispatchChanged;
    public long Pr57CauseDispatchUndoProgress, Pr57CauseDispatchReturnProgress;
    // PR57 observational breakdown of the existing dispatch phase; profile only.
    public long Pr57DispatchReserveTicks, Pr57DispatchAccessTicks, Pr57DispatchCrewTicks;
    public long Pr57DispatchReserveCalls, Pr57DispatchAccessCalls, Pr57DispatchCrewCalls;
    public Dictionary<(string From,string To),long> Pr57CauseTransitions { get; } = new();
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
        if(_pr57PhaseAllocationTracking)
        {
            var allocated=GC.GetAllocatedBytesForCurrentThread();
            PhaseAllocatedBytes[phase]+=allocated-_pr57PhaseAllocationBaseline;
            _pr57PhaseAllocationBaseline=allocated;
        }
    }
    internal void MarkDetail(int detail, ref long started)
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        DetailTicks[detail] += now - started;
        started = now;
    }
    internal void FinishStep() { Steps++; _pr57PhaseAllocationTracking=false; }
}

public sealed partial class CitySimulation
{
    public CityState State {get;}
    // PR57 #167: opt-in observational upper bound on public Advance(2)
    // boundary work versus the complete Step(2), including every physical slice.
    // Never used to change scheduling, state, or save behavior.
    static readonly bool Pr57AdvanceBoundaryProbe =
        Environment.GetEnvironmentVariable("PR57_ADVANCE_BOUNDARY_PROBE")=="1";
    bool _pr57BoundaryActive;
    long _pr57BoundaryCalls,_pr57BoundaryAdvanceTicks,_pr57BoundaryStepTicks;
    long _pr57BoundaryAdvanceBytes,_pr57BoundaryStepBytes;
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
        var pr57Writer289Start=Pr57Writer289Start(out var pr57Writer289Bytes);
        State.Ledger.Add(new(State.Now,from,to,amount,category,reason,entity));
        Pr57Writer289End(2,pr57Writer289Start,pr57Writer289Bytes);return true;
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
    void Personal(Citizen c,string description)
    {
        var pr57Writer289Start=Pr57Writer289Start(out var pr57Writer289Bytes);
        c.History.Add(new(State.Now,description));c.Decision=description;
        Pr57Writer289End(0,pr57Writer289Start,pr57Writer289Bytes);
    }
    void Event(string description)
    {
        var pr57Writer289Start=Pr57Writer289Start(out var pr57Writer289Bytes);
        State.CityEvents.Add(new(State.Now,description));State.LastMessage=description;
        Pr57Writer289End(1,pr57Writer289Start,pr57Writer289Bytes);
    }
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
        _pr57BoundaryActive=Pr57AdvanceBoundaryProbe&&seconds==Quantum;
        var pr57BoundaryStart=_pr57BoundaryActive?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var pr57BoundaryBytesStart=_pr57BoundaryActive?GC.GetAllocatedBytesForCurrentThread():0L;
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
        finally{_crosswalkBandsActive=false;_crosswalkBands.Clear();EndWalkRoutes();_pr57RouteTreeActive=false;_pr57RouteTreeParents.Clear();_pr57RouteTreeCalls.Clear();Pr57EndBuildingAccessAdvance();Pr57ClearSharedNodeGrid();}
        if(_pr57BoundaryActive)
        {
            _pr57BoundaryAdvanceTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57BoundaryStart;
            _pr57BoundaryAdvanceBytes+=GC.GetAllocatedBytesForCurrentThread()-pr57BoundaryBytesStart;
            _pr57BoundaryCalls++;
            _pr57BoundaryActive=false;
            if(_pr57BoundaryCalls%100==0)
            {
                double Ms(long ticks)=>ticks*1000.0/System.Diagnostics.Stopwatch.Frequency;
                Console.WriteLine($"PR57_ADVANCE_BOUNDARY calls={_pr57BoundaryCalls} quantum=2 " +
                    $"advanceMs={Ms(_pr57BoundaryAdvanceTicks):F3} stepMs={Ms(_pr57BoundaryStepTicks):F3} " +
                    $"outsideStepMs={Ms(_pr57BoundaryAdvanceTicks-_pr57BoundaryStepTicks):F3} " +
                    $"advanceAllocMB={_pr57BoundaryAdvanceBytes/1e6:F3} stepAllocMB={_pr57BoundaryStepBytes/1e6:F3} " +
                    $"outsideStepAllocMB={(_pr57BoundaryAdvanceBytes-_pr57BoundaryStepBytes)/1e6:F3}");
            }
        }
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
    static readonly bool Pr57CauseTransitionProbe=Environment.GetEnvironmentVariable("PR57_CAUSE_TRANSITION_PROBE")=="1";
    void Step(double dt)
    {
        if(Pr57Writer289Enabled)_pr57Writer289Phase=0;
        var pr57BoundaryStepStart=_pr57BoundaryActive?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var pr57BoundaryStepBytesStart=_pr57BoundaryActive?GC.GetAllocatedBytesForCurrentThread():0L;
        Pr57WorkAccessBeginStep();
        Pr57BeginStep();
        Pr57PhaseBuildingsBeginStep();
        Pr57SharedShipmentsBeginStep();
        if(!Pr57LivingRosterEnabled&&!_pr57LivingRosterAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine("PR57_LIVING_ROSTER active=False");
            _pr57LivingRosterAnnounced=true;
        }
        var profile = CostProfile;
        var started = profile is null ? 0L : System.Diagnostics.Stopwatch.GetTimestamp();
        // Sample full persisted state at phase boundaries only in a diagnostic profile.
        // The sample never influences simulation decisions, and is excluded from phase timing.
        var effects=profile?.PhaseEffects;
        var sampleEffects=effects is not null&&effects.ShouldSample();
        if(sampleEffects)
        {
            effects!.Begin(System.Text.Json.JsonSerializer.Serialize(State,SaveOptions));
            started=System.Diagnostics.Stopwatch.GetTimestamp();
        }
        var presence=profile?.CrossPhasePresence;
        var samplePresence=presence is not null&&presence.Begin(this);
        if(samplePresence)started=System.Diagnostics.Stopwatch.GetTimestamp();
        // Observational allocation boundaries; all 13 phases still execute.
        profile?.BeginAllocationCensus();
        // PR57 opt-in: one exact conservative node grid for all movement
        // sub-slices of this Step; discard at the public Advance boundary.
        Pr57PrepareSharedNodeGrid();
        Pr57310Begin();
        // PR57 #541: geometry-only, Step-scoped reuse. All vehicle motion and
        // physical admission still execute in their original causal order.
        Pr57TurnWayWorksetBegin();
        try { MoveVehicles(dt); }
        finally { Pr57TurnWayWorksetEnd(); }
        Pr57310Boundary("vehicles");
        if(profile is not null)profile.Mark(0,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=1;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,0,ref started);
        if(samplePresence){presence!.Observe(this,0);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        ReturnPavingMachines(dt);
        Pr57310Boundary("paving");
        if(profile is not null)profile.Mark(1,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=2;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,1,ref started);
        if(samplePresence){presence!.Observe(this,1);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        MoveCitizens(dt);
        Pr57310Boundary("citizens");
        if(profile is not null)profile.Mark(2,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=3;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,2,ref started);
        if(samplePresence){presence!.Observe(this,2);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        // Observe persisted project diagnostics at causal boundaries, profile only.
        var pr57CauseBeforeProgress=profile is not null&&Pr57CauseTransitionProbe
            ?State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress).ToDictionary(p=>p.Id,p=>p.Cause):null;
        if(pr57CauseBeforeProgress is not null)started=System.Diagnostics.Stopwatch.GetTimestamp();
        var completedConstruction=ProgressConstruction(dt);
        var pr57CauseAfterProgress=pr57CauseBeforeProgress is null?null:State.Projects.ToDictionary(p=>p.Id,p=>p.Cause);
        if(pr57CauseAfterProgress is not null)started=System.Diagnostics.Stopwatch.GetTimestamp();
        if(completedConstruction&&Pr57MaintenanceScheduleEnabled)_pr57MaintenanceScheduleReady=false;
        if(profile is not null)profile.Mark(3,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=4;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,3,ref started);
        if(samplePresence){presence!.Observe(this,3);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        UpdateMaintenanceCondition(dt);
        if(profile is not null)profile.Mark(4,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=5;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,4,ref started);
        if(samplePresence){presence!.Observe(this,4);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        ProgressMunicipalRelocations(dt);
        if(profile is not null)profile.Mark(5,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=6;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,5,ref started);
        if(samplePresence){presence!.Observe(this,5);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        UpdateInfrastructure(dt);
        if(profile is not null)profile.Mark(6,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=7;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,6,ref started);
        if(samplePresence){presence!.Observe(this,6);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        OperateBusinesses(dt);
        if(profile is not null)profile.Mark(7,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=8;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,7,ref started);
        if(samplePresence){presence!.Observe(this,7);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        UpdateNeeds(dt);
        if(profile is not null)profile.Mark(8,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=9;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,8,ref started);
        if(samplePresence){presence!.Observe(this,8);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        State.AddElapsedSeconds(dt);State.Now=State.Now.AddSeconds(dt);
        var reviewEconomy=completedConstruction||State.ElapsedSeconds>=State.EconomyNextAt;
        if(reviewEconomy){State.EconomyNextAt=State.ElapsedSeconds+State.Tuning.EconomyInterval;SeekHomesForAllDisplaced();ReviewMarkets();ReviewJobs();ScheduleMaintenance();}
        if(profile is not null)profile.Mark(9,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=10;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,9,ref started);
        if(samplePresence){presence!.Observe(this,9);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        var pr57CauseBeforeDispatch=pr57CauseBeforeProgress is null?null:State.Projects.ToDictionary(p=>p.Id,p=>p.Cause);
        if(pr57CauseBeforeDispatch is not null)started=System.Diagnostics.Stopwatch.GetTimestamp();
        // PR57 profile-only productivity census around the unchanged dispatcher.
        var pr57DispatchActive=profile is not null&&State.Projects.Any(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress);
        var pr57ShipmentsBefore=profile is null?0:State.Shipments.Count;
        var pr57LedgerBefore=profile is null?0:State.Ledger.Count;
        OrderConstruction();
        if(pr57DispatchActive)
        {
            var shipmentsDelta=State.Shipments.Count-pr57ShipmentsBefore;
            var ledgerDelta=State.Ledger.Count-pr57LedgerBefore;
            profile!.ConstructionDispatchActiveSteps++;
            if(shipmentsDelta!=0){profile.ConstructionDispatchShipmentChangedSteps++;profile.ConstructionDispatchShipmentNetDelta+=shipmentsDelta;}
            if(ledgerDelta!=0){profile.ConstructionDispatchLedgerChangedSteps++;profile.ConstructionDispatchLedgerNetDelta+=ledgerDelta;}
            if(shipmentsDelta==0&&ledgerDelta==0)profile.ConstructionDispatchNoShipmentOrLedgerSteps++;
        }
        if(pr57CauseBeforeDispatch is not null)
        {
            foreach(var p in State.Projects)
            {
                if(!pr57CauseBeforeProgress!.TryGetValue(p.Id,out var before)
                    ||!pr57CauseAfterProgress!.TryGetValue(p.Id,out var afterProgress)
                    ||!pr57CauseBeforeDispatch.TryGetValue(p.Id,out var beforeDispatch))continue;
                var afterDispatch=p.Cause;
                profile!.Pr57CauseObserved++;
                if(!string.Equals(before,afterProgress,StringComparison.Ordinal))profile.Pr57CauseProgressChanged++;
                if(!string.Equals(afterProgress,beforeDispatch,StringComparison.Ordinal))profile.Pr57CauseIntermediateChanged++;
                if(!string.Equals(beforeDispatch,afterDispatch,StringComparison.Ordinal))
                {
                    profile.Pr57CauseDispatchChanged++;
                    var key=(beforeDispatch??"<null>",afterDispatch??"<null>");
                    profile.Pr57CauseTransitions[key]=profile.Pr57CauseTransitions.GetValueOrDefault(key)+1;
                }
                if(!string.Equals(before,afterProgress,StringComparison.Ordinal)
                    &&string.Equals(before,afterDispatch,StringComparison.Ordinal))profile.Pr57CauseDispatchUndoProgress++;
                if(!string.Equals(beforeDispatch,afterDispatch,StringComparison.Ordinal)
                    &&string.Equals(afterProgress,afterDispatch,StringComparison.Ordinal))profile.Pr57CauseDispatchReturnProgress++;
            }
            started=System.Diagnostics.Stopwatch.GetTimestamp();
        }
        if(profile is not null)profile.Mark(10,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=11;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,10,ref started);
        if(samplePresence){presence!.Observe(this,10);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        if(reviewEconomy){RetryPurchasesAwaitingDestination();RestockBusinesses();ReviewEmigration();RecoverDebt();RetryCargoAwaitingDestination();ReviewVehicleRevisions();ResumeHeldDeliveries();RefuelIdleShopTows();}
        if(profile is not null)profile.Mark(11,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=12;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,11,ref started);
        if(samplePresence){presence!.Observe(this,11);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        ProgressBuildingDemolitions();
        while(State.Now>=State.NextDaily){DailyFinance();State.NextDaily=State.NextDaily.AddDays(1);}
        while(State.Now>=State.NextTaxDate){PropertyTaxes();State.NextTaxDate=State.NextTaxDate.AddMonths(1);}
        ProcessLoans();
        if(profile is not null)profile.Mark(12,ref started);
        if(Pr57Writer289Enabled)_pr57Writer289Phase=13;
        if(sampleEffects)Pr57ObservePhaseEffect(effects!,12,ref started);
        if(samplePresence){presence!.Observe(this,12);profile?.ResetAllocationCensusBaseline();started=System.Diagnostics.Stopwatch.GetTimestamp();}
        Pr57SharedShipmentsEndStep();
        Pr57PhaseBuildingsEndStep();
        if(profile is not null)profile.FinishStep();
        if(Pr57Writer289Enabled)_pr57Writer289Phase=13;
        if(_pr57BoundaryActive)
        {
            _pr57BoundaryStepTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57BoundaryStepStart;
            _pr57BoundaryStepBytes+=GC.GetAllocatedBytesForCurrentThread()-pr57BoundaryStepBytesStart;
        }
    }
    // Intrusive PR57 observation only: serialize the same persisted CityState shape
    // between phases, never SaveToJson (intermediate states may be transient).
    void Pr57ObservePhaseEffect(Pr57PhaseEffects effects,int phase,ref long started)
    {
        effects.Observe(phase,System.Text.Json.JsonSerializer.Serialize(State,SaveOptions));
        CostProfile?.ResetAllocationCensusBaseline();
        started=System.Diagnostics.Stopwatch.GetTimestamp();
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
