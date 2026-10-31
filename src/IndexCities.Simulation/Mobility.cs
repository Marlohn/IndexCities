namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57-only allocation census for authentic congested checkpoint replays.
    // Observation is enabled exclusively by CostProfile; it never affects movement or saves.
    // PR57 #155: intrusive, observational per-vehicle cost classification.
    // Disabled in normal gameplay and in plain wall runs. No scheduler reads these counters.
    static readonly bool Pr57VehicleCostProbeEnabled=Environment.GetEnvironmentVariable("PR57_VEHICLE_COST_PROBE")=="1";
    readonly long[] _pr57VehicleCostTicks=new long[8];
    readonly long[] _pr57VehicleCostCalls=new long[8];
    readonly long[] _pr57VehicleCostBytes=new long[8];
    readonly long[] _pr57VehicleCostClaimChanges=new long[8];
    readonly long[] _pr57VehicleCostSpaceCountChanges=new long[8];
    readonly long[] _pr57MovementAllocBytes=new long[11]; // total, setup, lane, bend, site, crossing, turn, operations, pre-lane, post-turn, finalizer
    // PR57 observational split of preLane/postTurn. Indexes are never read by physics.
    readonly long[] _pr57RegionAllocBytes=new long[8]; // route, geometry, shared, entry, pose, blocked, junction, drive
    // PR57 observation only: split the zero-step branch, including paths that break/continue.
    readonly long[] _pr57BlockedAllocBytes=new long[6]; // cause, tow-approach, bypass, tow-exit, tow-reroute, tow-wait
    readonly long[] _pr57BlockedCalls=new long[6];
    // PR57-only observation: full junction admission including early break/continue.
    readonly long[] _pr57JunctionAllocBytes=new long[2]; // MayEnterJunction, post-denial handling
    readonly long[] _pr57JunctionCalls=new long[3]; // MayEnterJunction, denied, bay-held before admission
    // PR57 observation only: allocations by predicate inside MayEnterJunction.
    readonly long[] _pr57JunctionDetailBytes=new long[7]; // manoeuvre, area owner, bend holder, bend priority, preference, way, body
    readonly long[] _pr57JunctionDetailCalls=new long[7];
    // PR57 opt-in diagnostic only: timings are observational and not persisted.
    // Categories are nested; their wall times must not be added together.
    static readonly bool Pr57HotTiming=Environment.GetEnvironmentVariable("PR57_HOT_TIMING")=="1";
    readonly long[] _pr57DirectMovementTicks=new long[4]; // site, crossing, turn, junction
    readonly long[] _pr57DirectMovementCalls=new long[4];
    readonly long[] _pr57JunctionDetailTicks=new long[7]; // same regions as JunctionDetailBytes
    long _pr57MovementAllocCalls;
    // Shared empty candidate list: read-only, never populated by a movement scan.
    static readonly List<Vehicle> Pr57EmptyBendList=[];
    // PR57-only bounded diagnostics for the two previously identified junction winners.
    // Never consulted by movement, and never persisted.
    readonly Dictionary<int,int> _laneBlockProbeCounts=new();
    // PR57 trace of real checkpoint 12; profiler-only and never read by physics.
    readonly Dictionary<int,long> _pr57TargetTraceCounts=new();
    readonly Dictionary<int,Dictionary<int,long>> _laneBlockProbeLeaders=new();
    int _laneEntryOriginLastSecond=-1; // PR57: bounded, observational, never serialized
    bool _pr57CausalHorizonAnnounced; // diagnostic only; no persisted state
    bool _pedestrianCrossingsChecked;
    // PR57 #224: opt-in, observational classification of the actual citizen loop.
    // No state is skipped; category is captured BEFORE the canonical actor body.
    static readonly bool Pr57CitizenStateCostEnabled =
        Environment.GetEnvironmentVariable("PR57_224_CITIZEN_STATE_COST") == "1";
    readonly long[] _pr57CitizenStateCalls = new long[4];
    readonly long[] _pr57CitizenStateTicks = new long[4];
    long _pr57CitizenSetupTicks, _pr57CitizenLoopTicks;
    // PR57 #239: bounded, observational breakdown; never consulted by gameplay.
    static readonly bool Pr57Citizen239Enabled =
        Environment.GetEnvironmentVariable("PR57_239_CITIZEN_PHASES") == "1";
    readonly long[] _pr57Citizen239Ticks = new long[7];
    readonly long[] _pr57Citizen239Calls = new long[7];
    void Pr57Citizen239Record(int stage,long start)
    {
        _pr57Citizen239Ticks[stage] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57Citizen239Calls[stage]++;
    }
    public IEnumerable<string> Pr57Citizen239Report()
    {
        if (!Pr57Citizen239Enabled) yield break;
        string[] names = ["update-crosswalks-cold","refresh-crosswalks","pedestrian-grid-rebuild",
            "citizen-routine-total","seek-food","family-adults-shopper","start-journey"];
        var hz = (double)System.Diagnostics.Stopwatch.Frequency;
        for (var i=0;i<names.Length;i++)
            yield return $"PR57_239_CITIZEN name={names[i]} calls={_pr57Citizen239Calls[i]} ms={_pr57Citizen239Ticks[i]*1000/hz:F5} scope=INTRUSIVE_OBSERVATIONAL";
    }
    // PR57 #240: observational walking-physics cost split; never read by gameplay.
    static readonly bool Pr57Walk240Enabled =
        Environment.GetEnvironmentVariable("PR57_240_WALK_SPLIT") == "1";
    readonly long[] _pr57Walk240Ticks = new long[4];
    readonly long[] _pr57Walk240Calls = new long[4];
    long _pr57Walk240Visits, _pr57Walk240Segments, _pr57Walk240Replans;
    long _pr57Walk240CrosswalkCandidates, _pr57Walk240CitizenCandidates, _pr57Walk240VehicleCandidates;
    void Pr57Walk240Record(int stage,long start)
    {
        _pr57Walk240Ticks[stage] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57Walk240Calls[stage]++;
    }
    // PR57 #241: attribute walking arrivals by purpose and timed nested calls.
    // Observation only; the original methods remain the sole decision makers.
    static readonly bool Pr57Arrive241Enabled =
        Environment.GetEnvironmentVariable("PR57_241_ARRIVAL_SPLIT") == "1";
    readonly Dictionary<JourneyPurpose,(long Calls,long Ticks)> _pr57Arrive241Purposes = new();
    readonly long[] _pr57Arrive241SubTicks = new long[3];
    readonly long[] _pr57Arrive241SubCalls = new long[3];
    void Pr57Arrive241Purpose(JourneyPurpose purpose,long start)
    {
        _pr57Arrive241Purposes.TryGetValue(purpose,out var row);
        _pr57Arrive241Purposes[purpose]=(row.Calls+1,row.Ticks+System.Diagnostics.Stopwatch.GetTimestamp()-start);
    }
    void Pr57Arrive241Sub(int stage,long start)
    {
        _pr57Arrive241SubTicks[stage]+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        _pr57Arrive241SubCalls[stage]++;
    }
    bool Pr57Arrive241Buy(Citizen c,Building b)
    {
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var result=TryBuyFood(c,b);
        Pr57Arrive241Sub(0,start);
        return result;
    }
    bool Pr57Arrive241Seek(Citizen c,int excluded)
    {
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var result=SeekFood(c,excluded);
        Pr57Arrive241Sub(1,start);
        return result;
    }
    bool Pr57Arrive241Journey(Citizen c,Building b,JourneyPurpose purpose)
    {
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var result=StartJourney(c,b,purpose);
        Pr57Arrive241Sub(2,start);
        return result;
    }
    public IEnumerable<string> Pr57Arrive241Report()
    {
        if(!Pr57Arrive241Enabled)yield break;
        var hz=(double)System.Diagnostics.Stopwatch.Frequency;
        foreach(var row in _pr57Arrive241Purposes.OrderBy(x=>x.Key))
            yield return $"PR57_241_ARRIVAL purpose={row.Key} calls={row.Value.Calls} ms={row.Value.Ticks*1000/hz:F5} scope=INTRUSIVE_OBSERVATIONAL";
        string[] names=["try-buy-food","seek-food","start-journey"];
        for(var i=0;i<names.Length;i++)
            yield return $"PR57_241_NESTED name={names[i]} calls={_pr57Arrive241SubCalls[i]} ms={_pr57Arrive241SubTicks[i]*1000/hz:F5} scope=INTRUSIVE_OBSERVATIONAL";
    }
    public IEnumerable<string> Pr57Walk240Report()
    {
        if (!Pr57Walk240Enabled) yield break;
        string[] names = ["crossing-reached","pedestrian-gap","crossing-occupancy","citizen-arrived"];
        var hz = (double)System.Diagnostics.Stopwatch.Frequency;
        for (var i=0;i<names.Length;i++)
            yield return $"PR57_240_WALK name={names[i]} calls={_pr57Walk240Calls[i]} ms={_pr57Walk240Ticks[i]*1000/hz:F5} scope=INTRUSIVE_OBSERVATIONAL";
        yield return $"PR57_240_WALK_SUM visits={_pr57Walk240Visits} segments={_pr57Walk240Segments} replans={_pr57Walk240Replans} crosswalkCandidates={_pr57Walk240CrosswalkCandidates} citizenCandidates={_pr57Walk240CitizenCandidates} vehicleCandidates={_pr57Walk240VehicleCandidates} crossingGrid={Pr57CrosswalkGridEnabled} pedestrianGrid={_pr57PedestrianGridActive}";
    }
    public IEnumerable<string> Pr57CitizenStateCostReport()
    {
        if (!Pr57CitizenStateCostEnabled) yield break;
        string[] names = ["aboard", "walking", "stationary-due", "stationary-not-due"];
        var hz = (double)System.Diagnostics.Stopwatch.Frequency;
        var total = _pr57CitizenStateTicks.Sum();
        yield return $"PR57_224_CITIZEN_SUM setupMs={_pr57CitizenSetupTicks * 1000 / hz:F4} loopMs={_pr57CitizenLoopTicks * 1000 / hz:F4} actorMs={total * 1000 / hz:F4} scope=INTRUSIVE_ALL_ACTORS_NO_SKIP";
        for (var i = 0; i < names.Length; i++)
            yield return $"PR57_224_CITIZEN_STATE name={names[i]} calls={_pr57CitizenStateCalls[i]} actorMs={_pr57CitizenStateTicks[i] * 1000 / hz:F4} actorShare={(total == 0 ? 0 : 100.0 * _pr57CitizenStateTicks[i] / total):F3}";
    }
    bool ShiftActive(Citizen c)=>BuildingById(c.WorkplaceId) is {} b&&ShiftActiveIn(c,b);
    bool ShiftActiveIn(Citizen c,Building b)
    {
        if(!b.Completed||!c.Alive||!c.Resident)return false;
        var day=(int)State.Now.DayOfWeek;if(day==c.WeeklyDayOff||day==(c.WeeklyDayOff+1)%7)return false;
        var hour=State.Now.TimeOfDay.TotalHours;
        if(b.Kind is BuildingKind.WaterIntake or BuildingKind.WaterTreatment or BuildingKind.SewageTreatment or BuildingKind.WindPower)return c.Shift switch{0=>hour>=6&&hour<14,1=>hour>=14&&hour<22,_=>hour>=22||hour<6};
        return b.Kind is BuildingKind.Grocery or BuildingKind.FuelStation or BuildingKind.MechanicShop?(c.Shift==0?hour>=7&&hour<15:hour>=14&&hour<22):hour>=7&&hour<15;
    }
    bool Open(Building b)=>OpenAt(b.Kind,State.Now);
    public static bool OpenAt(BuildingKind kind,DateTime now)=>kind is BuildingKind.Grocery or BuildingKind.FuelStation or BuildingKind.MechanicShop?now.Hour>=7&&now.Hour<22:now.Hour>=6&&now.Hour<19;
    // Read-only opening hours for presentation (problem alerts over buildings).
    // Same rule the simulation already uses for building causes; no behaviour change.
    public bool BuildingOpen(Building building)=>Open(building);
    public bool WorkerPresentAt(Citizen c,Building b)=>c.Alive&&c.WorkplaceId==b.Id&&c.VehicleId==null&&c.ConstructionProjectId==null&&!c.Moving&&c.Position.Distance(b.Door)<3&&ShiftActive(c);
    public int WorkersPresent(Building b)=>State.Citizens.Count(c=>WorkerPresentAt(c,b));
    // Single-pass workforce counts per building for presentation (problem
    // alerts): present at the door on shift, scheduled with an active shift,
    // and hired. Read-only; same rules as WorkerPresentAt/ShiftActiveIn, no
    // behaviour change.
    public Dictionary<int,(int Present,int OnShift,int Hired)> WorkforceByBuilding()
    {
        var buildings=State.Buildings.Where(b=>b.Completed&&!b.Removed).ToDictionary(b=>b.Id);
        var result=buildings.Keys.ToDictionary(id=>id,_=>(Present:0,OnShift:0,Hired:0));
        foreach(var c in State.Citizens){
            if(c.WorkplaceId is not {} site||!buildings.TryGetValue(site,out var building))continue;
            var row=result[site];
            if(c.Alive)row.Hired++;
            if(ShiftActiveIn(c,building))row.OnShift++;
            if(WorkerPresentAt(c,building))row.Present++;
            result[site]=row;
        }
        return result;
    }
    List<Point> CitizenRoute(Citizen citizen,Building destination)
    {
        // Replanning beside the destination door must not take the walker to
        // the default sidewalk, across the road and back to that same door.
        var access=Access(destination);
        if(citizen.Position.Distance(destination.Door)<3&&access!=null&&!CrossesLane(citizen.Position,destination.Door,access))return [citizen.Position,destination.Door];
        return PedestrianRoute(citizen.Position,destination);
    }
    bool StartJourney(Citizen c,Building destination,JourneyPurpose purpose)
    {
        Pr57CitizenAgendaWake(c);
        if(TryDrive(c,destination,purpose))return true;
        var path=CitizenRoute(c,destination);if(path.Count==0){c.Activity="Sem rota para "+destination.Definition.Name;c.Decision="Acesso viário ou travessia indisponível";c.PathRoadRevision=State.RoadRevision;c.NextDecisionAt=State.ElapsedSeconds+300;return false;}
        Pr57320ReplacePath(c,path,"startJourney");Pr57320ResetIndex(c,"startJourney");c.PathRoadRevision=State.RoadRevision;c.Purpose=purpose;c.DestinationId=destination.Id;c.Activity=purpose switch{JourneyPurpose.Work=>"Indo trabalhar",JourneyPurpose.Home=>"Voltando para casa",JourneyPurpose.Shopping=>"Indo comprar alimentos",JourneyPurpose.Arriving=>"Chegando à cidade",JourneyPurpose.Visit=>"Indo visitar outra família",JourneyPurpose.Stroll=>"Passeando a pé",_=>"Indo ao canteiro"};Personal(c,c.Activity+" em "+destination.Definition.Name);return true;
    }
    void MoveCitizens(double dt)
    {
        Pr57312Begin();
        Pr57317Begin();
        Pr57319Begin();
        Pr57320Begin();
        Pr57Transitions290Begin(2);
        var pr57StateCost = CostProfile is not null && Pr57CitizenStateCostEnabled;
        var pr57SetupStart = pr57StateCost ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        var pr57Citizen239 = Pr57Citizen239Enabled && CostProfile is not null;
        var pr57Walk240 = Pr57Walk240Enabled && CostProfile is not null;
        if(!Pr57CitizenAgendaEnabled&&!_pr57AgendaAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1"){Console.WriteLine("PR57_CITIZEN_AGENDA active=False");_pr57AgendaAnnounced=true;}
        // Refresh derived crossings once for an old save before retrying routes.
        // Loading itself still preserves the serialized state exactly.
        if(!_pedestrianCrossingsChecked){
            var t=pr57Citizen239?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            UpdateCrosswalks();_pedestrianCrossingsChecked=true;
            if(pr57Citizen239)Pr57Citizen239Record(0,t);
        }
        var refreshStart=pr57Citizen239?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        RefreshCrosswalks();
        if(pr57Citizen239)Pr57Citizen239Record(1,refreshStart);
        _pr57PedestrianGridActive=_pr57PedestrianGridEnabled;
        if(_pr57PedestrianGridActive){
            var gridStart=pr57Citizen239?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            Pr57RebuildPedestrianGrid();
            if(pr57Citizen239)Pr57Citizen239Record(2,gridStart);
        }
        if(!_pr57PedestrianGridAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1"){
            Console.WriteLine($"PR57_PEDESTRIAN_GRID active={_pr57PedestrianGridActive} citizens={State.Citizens.Count} cellMetres=8");
            _pr57PedestrianGridAnnounced=true;
        }
        if (pr57StateCost) _pr57CitizenSetupTicks += System.Diagnostics.Stopwatch.GetTimestamp() - pr57SetupStart;
        var pr57LoopStart = pr57StateCost ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        foreach(var c in Pr57CitizenAgendaEnabled?Pr57CitizenAgendaCandidates():Pr57LiveCitizens()){
            var pr57StateClass = pr57StateCost
                ? (c.VehicleId is not null ? 0 : c.Moving ? 1 : State.ElapsedSeconds >= c.NextDecisionAt ? 2 : 3)
                : -1;
            var pr57ActorStart = pr57StateCost ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
            var pr57312Before = Pr57312Capture(c);
            try{
            if(!c.Moving&&c.PathRoadRevision!=State.RoadRevision&&(c.Activity.StartsWith("Sem rota",StringComparison.Ordinal)||c.Activity=="Viagem interrompida: rota removida"))c.NextDecisionAt=Math.Min(c.NextDecisionAt,State.ElapsedSeconds);
            if(c.ConstructionProjectId!=null&&ShiftActive(c)){c.AddWorkSecondsToday(dt);AccrueWage(c,dt);}
            if(c.VehicleId is {} vid){var v=VehicleById(vid);if(v!=null){Pr57317SetPosition(c,v.Position,"aboard");c.InsideMap=InMap(v.Position);}continue;}
            if(c.Moving){
                if(pr57Walk240)_pr57Walk240Visits++;
                if(c.Purpose==JourneyPurpose.Boarding)c.PathRoadRevision=State.RoadRevision;
                if(c.PathRoadRevision!=State.RoadRevision&&c.Purpose==JourneyPurpose.Construction&&c.ConstructionProjectId is {} projectId){if(pr57Walk240)_pr57Walk240Replans++;var project=State.Projects.First(p=>p.Id==projectId);var access=WorkAccess(project);var replacement=access==null?new List<Point>():PedestrianPath(c.Position,CrewPedestrianSite(access),access);Pr57320ReplacePath(c,replacement,"constructionReplan");Pr57320ResetIndex(c,"constructionReplan");c.PathRoadRevision=State.RoadRevision;if(replacement.Count==0){c.Activity="Equipe aguardando rota de obra";continue;}}
                var journeyDestination=BuildingById(c.DestinationId);
                var closeDoorDetour=journeyDestination!=null&&c.Purpose!=JourneyPurpose.Service&&c.Position.Distance(journeyDestination.Door)<3&&c.Path.Skip(c.PathIndex).Any(point=>point.Distance(journeyDestination.Door)>3)&&Access(journeyDestination) is {} doorAccess&&!CrossesLane(c.Position,journeyDestination.Door,doorAccess);
                if((c.PathRoadRevision!=State.RoadRevision||closeDoorDetour)&&c.Purpose!=JourneyPurpose.Construction){
                    if(pr57Walk240)_pr57Walk240Replans++;
                    var dest=journeyDestination;if(dest!=null){var replacement=CitizenRoute(c,dest);if(replacement.Count==0){c.Activity="Viagem interrompida: rota removida";Pr57320ClearPath(c,"noRoute");c.PathRoadRevision=State.RoadRevision;c.NextDecisionAt=State.ElapsedSeconds+300;continue;}Pr57320ReplacePath(c,replacement,"destinationReplan");Pr57320ResetIndex(c,"destinationReplan");c.PathRoadRevision=State.RoadRevision;}
                }
                var distance=State.Tuning.WalkingSpeed*dt;
                while(distance>0&&c.PathIndex<c.Path.Count){if(pr57Walk240)_pr57Walk240Segments++;var target=c.Path[c.PathIndex];var d=c.Position.Distance(target);
                    if(c.CrossingNodeId is {} old&&(!_roads.TryGetValue(old,out var marked)||c.Position.Distance(marked.Position)>State.Tuning.RoadWidth+1))Pr57317SetCrossing(c,null,"expire");
                    // A long sidewalk segment can pass several future crossings.
                    // Only negotiate the crossing reached by this physical step,
                    // and preserve an ongoing crossing before nearby alternatives.
                    var nextPosition=c.Position.MoveTowards(target,Math.Min(distance,d));
                    var crossingStart=pr57Walk240?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                    var crossing=CrossingReached(c,nextPosition);
                    if(pr57Walk240){
                        Pr57Walk240Record(0,crossingStart);
                        if(!Pr57CrosswalkGridEnabled)_pr57Walk240CrosswalkCandidates+=_crosswalks.Count;
                    }
                    if(crossing!=null&&c.CrossingNodeId!=crossing.Id){
                        // A vehicle already stopped in a queue does not advance onto the
                        // walker; only one entering or passing the crossing does. This
                        // breaks the circular wait between queues and waiting walkers.
                        var occupancyStart=pr57Walk240?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                        var occupied=State.Vehicles.Any(v=>v.Status==VehicleStatus.Travelling&&(v.IntersectionClaim==crossing.Id||v.WaitingSince==0&&v.Position.Distance(crossing.Position)<3.5));
                        if(pr57Walk240){
                            Pr57Walk240Record(2,occupancyStart);
                            _pr57Walk240VehicleCandidates+=State.Vehicles.Count;
                        }
                        if(occupied){c.Activity="Aguardando travessia";break;}Pr57317SetCrossing(c,crossing.Id,"acquire");Pr57Crossing292Acquire(c,crossing.Id);
                    }
                    var heading=d>.001?(target-c.Position)*(1/d):new Point(0,0);
                    var gapStart=pr57Walk240?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                    var gap=PedestrianGap(c,target,d,heading,Math.Min(distance,d)+.7);
                    if(pr57Walk240){
                        Pr57Walk240Record(1,gapStart);
                        if(!_pr57PedestrianGridActive)_pr57Walk240CitizenCandidates+=State.Citizens.Count;
                    }
                    var travelled=Math.Min(distance,Math.Min(d,Math.Max(0,gap-.7)));if(travelled<.000001&&d>.001){c.Activity="Fila de pedestres";break;}Pr57317SetPosition(c,c.Position.MoveTowards(target,travelled),"walk");distance-=travelled;if(d<=travelled+.00001)Pr57319AdvancePathIndex(c);else break;
                }
                c.InsideMap=InMap(c.Position);
                if(!c.Moving){
                    Pr57317SetCrossing(c,null,"arrive");
                    var arrivalStart=pr57Walk240?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                    CitizenArrived(c);
                    if(pr57Walk240)Pr57Walk240Record(3,arrivalStart);
                }
                continue;
            }
            if(State.ElapsedSeconds<c.NextDecisionAt)continue;c.NextDecisionAt=State.ElapsedSeconds+60;
            CitizenRoutine(c);
            }finally{
                Pr57312After(c, pr57312Before);
                if(_pr57PedestrianGridActive)Pr57UpdatePedestrianGrid(c);
                Pr57CitizenAgendaSchedule(c);
                if (pr57StateCost)
                {
                    _pr57CitizenStateCalls[pr57StateClass]++;
                    _pr57CitizenStateTicks[pr57StateClass] += System.Diagnostics.Stopwatch.GetTimestamp() - pr57ActorStart;
                }
            }
        }
        if (pr57StateCost) _pr57CitizenLoopTicks += System.Diagnostics.Stopwatch.GetTimestamp() - pr57LoopStart;
        Pr57Transitions290End(2);
        Pr57312End();
        Pr57320End();
        Pr57319End();
        Pr57317End();
    }
    // Crosswalk nodes in State.Roads order. Rebuilt at the start of every
    // MoveCitizens call and whenever the road list or crossings change.
    readonly List<RoadNode> _crosswalks=[];int _crosswalksEpoch=-1;int _roadEpoch;
    // PR57-only POC: index crossing candidates by position rather than scan
    // every crossing for every walking segment. Rebuild lazily per Step;
    // no cache persists across public Advance boundaries or road mutations.
    static readonly bool Pr57CrosswalkGridEnabled=Environment.GetEnvironmentVariable("PR57_CROSSWALK_GRID")=="1";
    const double Pr57CrosswalkCellMetres=16;
    readonly Dictionary<(int X,int Z),List<int>> _pr57CrosswalkCells=new();
    readonly List<int> _pr57CrosswalkCandidates=[];
    readonly HashSet<int> _pr57CrosswalkSeen=new();
    bool _pr57CrosswalkGridReady,_pr57CrosswalkGridAnnounced,_pr57CrosswalkGridEngaged;
    void RefreshCrosswalks()
    {
        _crosswalks.Clear();foreach(var n in State.Roads)if(n.Crosswalk)_crosswalks.Add(n);
        _crosswalksEpoch=_roadEpoch;_pr57CrosswalkGridReady=false;
        if(!_pr57CrosswalkGridAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine($"PR57_CROSSWALK_GRID active={Pr57CrosswalkGridEnabled}");
            _pr57CrosswalkGridAnnounced=true;
        }
    }
    void Pr57EnsureCrosswalkGrid()
    {
        if(_pr57CrosswalkGridReady)return;
        foreach(var bucket in _pr57CrosswalkCells.Values)bucket.Clear();
        for(var i=0;i<_crosswalks.Count;i++)
        {
            var p=_crosswalks[i].Position;
            var key=((int)Math.Floor(p.X/Pr57CrosswalkCellMetres),(int)Math.Floor(p.Z/Pr57CrosswalkCellMetres));
            if(!_pr57CrosswalkCells.TryGetValue(key,out var bucket))_pr57CrosswalkCells[key]=bucket=[];
            bucket.Add(i);
        }
        _pr57CrosswalkGridReady=true;
        if(!_pr57CrosswalkGridEngaged&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine($"PR57_CROSSWALK_GRID built=True nodes={_crosswalks.Count}");
            _pr57CrosswalkGridEngaged=true;
        }
    }
    void Pr57CollectCrosswalkCandidates(Point point,double reach)
    {
        var minX=(int)Math.Floor((point.X-reach)/Pr57CrosswalkCellMetres);
        var maxX=(int)Math.Floor((point.X+reach)/Pr57CrosswalkCellMetres);
        var minZ=(int)Math.Floor((point.Z-reach)/Pr57CrosswalkCellMetres);
        var maxZ=(int)Math.Floor((point.Z+reach)/Pr57CrosswalkCellMetres);
        for(var x=minX;x<=maxX;x++)for(var z=minZ;z<=maxZ;z++)
            if(_pr57CrosswalkCells.TryGetValue((x,z),out var bucket))
                foreach(var index in bucket)if(_pr57CrosswalkSeen.Add(index))_pr57CrosswalkCandidates.Add(index);
    }
    // Crossing predicate and tie-breaking remain identical. Bounding cells
    // include all nodes that could pass the original endpoint-distance gate.
    RoadNode? CrossingReached(Citizen c,Point nextPosition)
    {
        if(_crosswalksEpoch!=_roadEpoch)RefreshCrosswalks();
        if(Pr57CrosswalkGridEnabled)
        {
            Pr57EnsureCrosswalkGrid();
            _pr57CrosswalkCandidates.Clear();_pr57CrosswalkSeen.Clear();
            var reach=State.Tuning.RoadWidth+1;
            Pr57CollectCrosswalkCandidates(c.Position,reach);
            Pr57CollectCrosswalkCandidates(nextPosition,reach);
            _pr57CrosswalkCandidates.Sort(); // preserve State.Roads order for ties
        }
        RoadNode? best=null;var bestKey=0;var bestDistance=0.0;
        var count=Pr57CrosswalkGridEnabled?_pr57CrosswalkCandidates.Count:_crosswalks.Count;
        for(var i=0;i<count;i++)
        {
            var n=_crosswalks[Pr57CrosswalkGridEnabled?_pr57CrosswalkCandidates[i]:i];
            if(!(n.Position.Distance(c.Position)<State.Tuning.RoadWidth+1||n.Position.Distance(nextPosition)<State.Tuning.RoadWidth+1)||!CrossesLane(c.Position,nextPosition,n))continue;
            var key=n.Id==c.CrossingNodeId?0:1;var distance=n.Position.Distance(c.Position);
            if(best==null||key<bestKey||key==bestKey&&distance.CompareTo(bestDistance)<0){best=n;bestKey=key;bestDistance=distance;}
        }
        return best;
    }
    // Distance to the nearest walker ahead in the same lane (double.MaxValue if
    // none). The caller only compares it with `reach` (step length + 0.7 m
    // clearance). A walker more than reach+1.3 m away and within 0.55 m of the
    // lane is more than `reach` ahead, so skipping it never changes the step:
    // the result differs only when both values already exceed `reach`.
    double PedestrianGap(Citizen c,Point target,double d,Point heading,double reach)
    {
        if(_pr57PedestrianGridActive)return Pr57IndexedPedestrianGap(c,target,d,heading,reach);
        var gap=double.MaxValue;var radius=reach+1.3;radius*=radius;
        foreach(var other in State.Citizens){
            if(other.Id==c.Id||!other.Alive||!other.Moving||other.VehicleId!=null)continue;
            var relative=other.Position-c.Position;if(relative.X*relative.X+relative.Z*relative.Z>radius)continue;
            var otherTarget=other.Path[other.PathIndex];var otherDelta=otherTarget-other.Position;var otherLength=otherDelta.Distance(new(0,0));var alignment=otherLength<.001?1:(heading.X*otherDelta.X+heading.Z*otherDelta.Z)/otherLength;
            // At a shared turning point, two converging headings can
            // each see the other ahead. Only the closer walker leads
            // that merge; stable identity resolves an exact tie.
            var sharedTurn=otherTarget.Distance(target)<.05;var precedes=!sharedTurn||otherLength<d-.001||Math.Abs(otherLength-d)<=.001&&other.Id<c.Id;
            var ahead=relative.X*heading.X+relative.Z*heading.Z;var side=Math.Abs(relative.X*heading.Z-relative.Z*heading.X);
            if(precedes&&ahead>.001&&side<.55&&alignment>.5&&ahead<gap)gap=ahead;
        }
        return gap;
    }
    void CitizenArrived(Citizen c)
    {
        var pr57Arrive241=Pr57Arrive241Enabled && CostProfile is not null;
        var arrivalPurpose=pr57Arrive241?c.Purpose:default;
        var arrivalStart=pr57Arrive241?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        try {
        if(c.Purpose==JourneyPurpose.Boarding){var vehicle=State.Vehicles.FirstOrDefault(v=>v.Id==c.BoardingVehicleId&&v.DriverId==c.Id);if(vehicle!=null&&vehicle.Position.Distance(c.Position)<.1){c.VehicleId=vehicle.Id;c.Activity=vehicle.PassengerCar?"Dirigindo carro particular":"Embarcou para entrega real";if(vehicle.PassengerCar){vehicle.Status=VehicleStatus.Travelling;vehicle.Cause="Em viagem";}}else {c.Activity="Veículo indisponível para embarque";if(State.Vehicles.FirstOrDefault(v=>v.Id==c.BoardingVehicleId&&v.PassengerCar&&v.DriverId==c.Id) is {} reserved)reserved.DriverId=null;}c.BoardingVehicleId=null;Pr57320ClearPath(c,"boardingArrived");Pr57320ResetIndex(c,"boardingArrived");return;}
        if(c.Purpose==JourneyPurpose.Leaving){c.Resident=false;c.InsideMap=false;c.Activity="Residindo no exterior";c.WorkplaceId=null;Personal(c,"Saiu fisicamente da cidade; dinheiro, patrimônio e dívidas persistem");return;}
        if(c.Purpose==JourneyPurpose.Construction){c.Activity="Trabalhando no canteiro";return;}
        var destination=BuildingById(c.DestinationId);if(destination==null){c.Activity="Destino indisponível";return;}
        // A driver waiting for a revision stays at the place by the shop reception.
        c.Position=c.Purpose==JourneyPurpose.Service&&destination.Kind==BuildingKind.MechanicShop&&c.Path.Count>0?c.Path[^1]:destination.Door;Pr57320ClearPath(c,"arrived");Pr57320ResetIndex(c,"arrived");
        if(c.Purpose==JourneyPurpose.Arriving){c.Resident=true;c.InsideMap=true;c.Activity="Em casa";Personal(c,"Chegou à residência adquirida");}
        else if(c.Purpose==JourneyPurpose.Work)c.Activity="No trabalho";
        else if(c.Purpose==JourneyPurpose.Home){if(FamilyById(c.FamilyId) is {} family){family.PantryUnits=checked(family.PantryUnits+c.BagFoodUnits);c.BagFood=0;}c.Activity="Em casa";}
        else if(c.Purpose==JourneyPurpose.Shopping){
            if(pr57Arrive241?Pr57Arrive241Buy(c,destination):TryBuyFood(c,destination)){
                if(BuildingById(FamilyById(c.FamilyId)?.HomeId) is {} home){
                    if(pr57Arrive241)Pr57Arrive241Journey(c,home,JourneyPurpose.Home);
                    else StartJourney(c,home,JourneyPurpose.Home);
                }
            }
            else if(!(pr57Arrive241?Pr57Arrive241Seek(c,destination.Id):SeekFood(c,destination.Id))&&BuildingById(FamilyById(c.FamilyId)?.HomeId) is {} home){
                if(pr57Arrive241)Pr57Arrive241Journey(c,home,JourneyPurpose.Home);
                else StartJourney(c,home,JourneyPurpose.Home);
            }
        }
        else if(c.Purpose==JourneyPurpose.Visit)c.Activity="Visitando família em "+destination.Definition.Name;
        else if(c.Purpose==JourneyPurpose.Service)c.Activity="Aguardando o veículo na "+destination.Definition.Name+" "+destination.Id;
        else if(c.Purpose==JourneyPurpose.Stroll){if(BuildingById(FamilyById(c.FamilyId)?.HomeId) is {} home&&(pr57Arrive241?Pr57Arrive241Journey(c,home,JourneyPurpose.Home):StartJourney(c,home,JourneyPurpose.Home)))c.Activity="Voltando do passeio";}
        } finally {
            if(pr57Arrive241)Pr57Arrive241Purpose(arrivalPurpose,arrivalStart);
        }
    }
    void CitizenRoutine(Citizen c)
    {
        var pr57Citizen239 = Pr57Citizen239Enabled && CostProfile is not null;
        var routineStart=pr57Citizen239?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        try {
        if(!c.Resident||!c.InsideMap)return;
        // SPEC 8A: the driver waits at the shop door until the revision is done.
        if(c.Purpose==JourneyPurpose.Service&&WaitingForService(c))return;
        var family=FamilyById(c.FamilyId);var home=BuildingById(family?.HomeId);var job=BuildingById(c.WorkplaceId);
        if(c.ConstructionProjectId!=null){if(ShiftActive(c))return;var p=State.Projects.FirstOrDefault(p=>p.Id==c.ConstructionProjectId);p?.LocalCrew.Remove(c.Id);c.ConstructionProjectId=null;}
        if(job!=null&&ShiftActive(c)){
            if(c.Position.Distance(job.Door)>3){
                var journeyStart=pr57Citizen239?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                StartJourney(c,job,JourneyPurpose.Work);
                if(pr57Citizen239)Pr57Citizen239Record(6,journeyStart);
            }else c.Activity="No trabalho";return;
        }
        if(home==null){c.Activity="Sem moradia";return;}
        var adultsStart=pr57Citizen239?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var adults=family!.Members.Select(id=>CitizenWithId(id)).Where(x=>x.Alive&&x.Age>=18&&x.Resident).ToList();
        var existingShopper=adults.Any(x=>x.Id!=c.Id&&(x.Purpose==JourneyPurpose.Shopping&&x.Moving||x.BagFood>0));
        if(pr57Citizen239)Pr57Citizen239Record(5,adultsStart);
        if(family.Pantry<family.Members.Count*1.5&&c.Age>=18&&!existingShopper&&State.Now.Hour>=7&&State.Now.Hour<21){
            c.TriedShops.Clear();
            var foodStart=pr57Citizen239?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var foodFound=SeekFood(c);
            if(pr57Citizen239)Pr57Citizen239Record(4,foodStart);
            if(foodFound)return;
        }
        if(c.Purpose==JourneyPurpose.Visit&&c.OutingUntil>State.ElapsedSeconds&&c.Position.Distance(home.Door)>3){
            // A failed food search updates its diagnosis, but does not make an
            // arrived visitor leave the actual interior or end the visit.
            if(BuildingById(c.DestinationId) is {Completed:true} host&&c.Position.Distance(host.Door)<1)c.Activity="Visitando família em "+host.Definition.Name;
            return;
        }
        if(c.Position.Distance(home.Door)<=3&&c.Age>=18&&FreeTime(c)&&NextRandom()<State.Tuning.OutingChancePerMinute&&TryOuting(c,home))return;
        if(c.Position.Distance(home.Door)>3){
            var journeyStart=pr57Citizen239?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            StartJourney(c,home,JourneyPurpose.Home);
            if(pr57Citizen239)Pr57Citizen239Record(6,journeyStart);
        }else c.Activity="Em casa";
        } finally {
            if(pr57Citizen239)Pr57Citizen239Record(3,routineStart);
        }
    }
    bool SeekFood(Citizen c,int? excluded=null)
    {
        // #242: observation only; preserve the lazy sorted enumeration, its
        // candidate order, every StartJourney attempt, and all state writes.
        var probe=Pr57Food242Enabled&&CostProfile is not null;
        var start=probe?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        if(probe){_pr57Food242Calls++;if(excluded!=null)_pr57Food242ArrivalCalls++;}
        try
        {
            if(excluded!=null&&!c.TriedShops.Contains(excluded.Value))c.TriedShops.Add(excluded.Value);
            // Choose by the family's real total cost: shelf price for what it needs plus
            // a calibrated round-trip cost; a shop that cannot fill the basket costs
            // another trip elsewhere.
            var family=FamilyById(c.FamilyId);var wanted=Math.Max(1,(family?.Members.Count??1)*State.Tuning.PurchaseFoodPerPerson-(family?.Pantry??0));
            decimal TripCost(Building shop)=>(decimal)(shop.Position.Distance(c.Position)*2)*State.Tuning.ShoppingTripCostPerMetre;
            foreach(var shop in State.Buildings.Where(b=>b.Kind==BuildingKind.Grocery&&b.Completed&&!b.Removed&&b.CompanyId!=null&&!c.TriedShops.Contains(b.Id)).OrderBy(b=>b.SalePrice*(decimal)wanted+TripCost(b)*(b.Quantity(ResourceKind.Food)>=wanted?1:2)).ThenBy(b=>b.Id)){
                if(probe)_pr57Food242Candidates++;
                if(!Open(shop)||shop.OperationalCapacity<=0||shop.Quantity(ResourceKind.Food)<.5){
                    if(probe)_pr57Food242Rejected++;
                    continue;
                }
                if(probe)_pr57Food242RouteAttempts++;
                var routeStart=probe?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                var started=StartJourney(c,shop,JourneyPurpose.Shopping);
                if(probe){
                    _pr57Food242RouteTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-routeStart;
                    if(started)_pr57Food242Success++;
                }
                if(started)return true;
            }
            c.Activity="Alimentos indisponíveis ou inacessíveis";c.Decision="Não existe loja operante, estoque e rota viáveis";
            if(probe)_pr57Food242Failures++;
            return false;
        }
        finally{if(probe)_pr57Food242TotalTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;}
    }
    // A crew dock lies along the curb, away from its access node: driving to that
    // node's lane point is a long diagonal whose body ends in the opposite lane.
    // The way back then starts at the arrival street's node nearest the body whose
    // route leaves in the direction the truck faces (a pull-out, never a U-turn).
    RoadNode? CrewDockForwardStart(Vehicle v)
    {
        var facing=Forward(v.Heading);static double Dot(Point a,Point b)=>a.X*b.X+a.Z*b.Z;
        if(Math.Abs(Dot(_roads[v.Route[^1]].Position-v.Position,facing))<=4)return null;
        foreach(var node in v.Route.Skip(Math.Max(0,v.Route.Count-4)).Where(_roads.ContainsKey).Select(id=>_roads[id]).OrderBy(r=>r.Position.Distance(v.Position))){
            if(Dot(node.Position-v.Position,facing)< -.5)continue;
            var route=FindRoadRoute(node.Id,EntryNode().Id,ConstructionTrip(v));
            if(route.Count>1&&Dot(_roads[route[1]].Position-node.Position,facing)>0)return node;
        }
        return null;
    }
    void ReturnExternal(Vehicle v)
    {
        PrepareConstructionSiteDeparture(v);
        v.ConstructionAccess=ConstructionTrip(v);
        v.ShipmentId=null;
        var atKnownBay=v.DockPosition is {} dock&&v.Position.Distance(dock)<.1&&v.Route.Count>0&&v.RouteIndex>=v.Route.Count&&_roads.ContainsKey(v.Route[^1]);var n=atKnownBay?CrewDockForwardStart(v)??_roads[v.Route[^1]]:NearestRoad(v.Position);if(n==null||!atKnownBay&&n.Position.Distance(v.Position)>State.Tuning.RoadWidth/2+3){v.Status=VehicleStatus.Waiting;v.Cause="Sem via para voltar ao exterior";return;}
        var route=FindRoadRoute(n.Id,EntryNode().Id,ConstructionTrip(v));if(route.Count==0){v.Status=VehicleStatus.Waiting;v.Cause="Conexão externa interrompida";return;}
        v.Route=route;v.RouteIndex=0;v.DockPosition=null;v.Status=VehicleStatus.Travelling;v.DestinationBuildingId=null;v.ShipmentId=null;v.Cause="Retorno ao exterior";
    }
    // PR57 #554: experimental dense primitive lane kernel with exact writer repair. It performs every
    // original candidate decision in the same order, but avoids the PR57
    // observational wrapper chain. Unknown diagnostic/algorithm flags force
    // the canonical path; no cached state survives a scan or a Step.
    // #554: slice-local dense trigonometry, ordered actor write-through and
    // bitwise lazy repair before every read, including nonlocal heading writes.
    // No decisions, actors or slices are cached or skipped.
    sealed class Pr57DenseTrigFrame554
    {
        readonly Vehicle[] _actors;
        readonly long[] _bits;
        readonly double[] _sin;
        readonly double[] _cos;
        readonly Dictionary<Vehicle,int> _slots=new(ReferenceEqualityComparer.Instance);
        public Pr57DenseTrigFrame554(List<Vehicle> vehicles)
        {
            _actors=vehicles.ToArray();_bits=new long[_actors.Length];
            _sin=new double[_actors.Length];_cos=new double[_actors.Length];
            for(var i=0;i<_actors.Length;i++){_slots.Add(_actors[i],i);Store(i,_actors[i]);}
        }
        void Store(int i,Vehicle v)
        {
            _bits[i]=BitConverter.DoubleToInt64Bits(v.Heading);
            _sin[i]=Math.Sin(v.Heading);_cos[i]=Math.Cos(v.Heading);
        }
        public void Refresh(Vehicle v)
        {
            if(_slots.TryGetValue(v,out var i)&&_bits[i]!=BitConverter.DoubleToInt64Bits(v.Heading))Store(i,v);
        }
        public void Read(int i,Vehicle v,out double sin,out double cos)
        {
            if((uint)i>=(uint)_actors.Length||!ReferenceEquals(_actors[i],v))
            {sin=Math.Sin(v.Heading);cos=Math.Cos(v.Heading);return;}
            if(_bits[i]!=BitConverter.DoubleToInt64Bits(v.Heading))Store(i,v);
            sin=_sin[i];cos=_cos[i];
        }
    }
    static readonly bool Pr57DenseLane554Enabled =
        Environment.GetEnvironmentVariable("PR57_DENSE_TRIG_554") == "1";
    static readonly bool Pr57DenseLane554Safe =
        Environment.GetEnvironmentVariables().Keys.Cast<object>()
            .Select(k => k.ToString() ?? "")
            .Where(k => k.StartsWith("PR57_", StringComparison.Ordinal))
            .All(k => k is "PR57_DENSE_TRIG_554" or "PR57_GRID_DIAG"
                or "PR57_HOST_QUANTUM" or "PR57_AB_CHECKPOINT"
                or "PR57_SAVE_CAPTURE_DIR" or "PR57_SITE_OWNER_ROSTER"
                or "PR57_BUILDING_WORK_ACCESS" or "PR57_DISPATCH_SHIPMENT_LOOKUP"
                or "PR57_ORDER_SHIPMENT_INDEX" or "PR57_PROGRESS_SHIPMENT_LOOKUP"
                or "PR57_SINGLE_CREW_ASSIGNMENT" or "PR57_SAFE_SITE_BUFFER"
                or "PR57_236_POSE_NOOP_SKIP" or "PR57_PARALLEL_NEEDS"
                or "PR57_NEEDS_SINGLE_PASS" or "PR57_SHARED_STEP_SHIPMENTS"
                or "PR57_SHARED_STEP_DIAG");
    bool _pr57DenseLane554Announced;
    // #237: same bitwise predicate as #236, factored for adversarial tests.
    static bool Pr57PoseHeadingNoop(double current,double approach)=>
        BitConverter.DoubleToInt64Bits(current)==BitConverter.DoubleToInt64Bits(approach);
    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)
    {
        Pr57SetupGapStart(dt,operationsDt);
        Pr57Transitions290Begin(dt<=0?0:1);
        Pr57PrepareSiteOwnerRoster();
        Pr57CausalBeginSlice(dt,operationsDt);
        Pr57JunctionCensusBeginSlice(dt,operationsDt);
        Pr57JunctionReadersBeginSlice(dt,operationsDt);
        var scanProfile=CostProfile;
        Pr57Effect253BeginSlice(dt,operationsDt,scanProfile is not null);
        Pr57260BeginSlice(dt,operationsDt);
        Pr57ExclusiveBeginSlice(dt,operationsDt,scanProfile is not null);
        Pr57Man248BeginSlice(dt,operationsDt,scanProfile is not null);
        Pr57PairBeginSlice(dt,operationsDt,scanProfile is not null);
        Pr57StageBeginSlice(dt,operationsDt,scanProfile is not null);
        Pr57Stage249BeginSlice(dt,operationsDt,scanProfile is not null);
        // #236: state-based POC. When restoring the approach heading is bitwise
        // identical to the current heading, the read-only pose scan cannot
        // affect the canonical state. Do not skip any vehicle or sub-slice.
        var pr57PoseNoopSkip=Environment.GetEnvironmentVariable("PR57_236_POSE_NOOP_SKIP")=="1";
        var pr57PoseNoopEligible=0;
        var pr57PoseNoopSkipped=0;
        var pr57CostProbe=scanProfile is not null&&Pr57VehicleCostProbeEnabled;
        var pr57LimiterProbe=scanProfile is not null&&Pr57QueueLimiterEnabled&&dt>0&&_pr57MovementAllocCalls%37==0;
        var pr57DepProbe=scanProfile is not null&&Pr57BlockDepProbeEnabled&&dt>0;
        var pr57IdleProbe=scanProfile is not null&&Pr57IdleProbeEnabled;
        var pr57AllocCallStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
        if(pr57DepProbe)Pr57DepBeginSlice();
        if(Pr57LaneReadWriteEnabled&&scanProfile is not null)Pr57LaneReadBeginSlice();
        // Release a passed junction even while a vehicle waits at the next one.
        // Carriageway half-width (3.3 m) + truck half-length (3.6 m) covers the
        // actual rear of the truck; the former 9 m reservation could overlap
        // nearby junctions and make opposite queues hold each other forever.
        foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}
        Pr57Effect253AfterCleanup();
        Pr57CausalAfterCleanup();
        Pr57JunctionReadersAfterCleanup();
        if(pr57DepProbe)Pr57DepAfterCleanup();
        if(Pr57MutationAuditEnabled&&scanProfile is not null)Pr57MutationBeginSlice();
        Pr57CrossSliceBegin();
        Pr57CandidateBeginSlice();
        Pr57S6GateShadowBeginSlice();
        Pr57PhaseExposureBeginSlice();
        Pr57PhaseImpactBeginSlice();
        Pr57S3CounterBeginSlice();
        Pr57ActorShadowBeginSlice();
        Pr57DockWriterBeginSlice();
        Pr57Body258BeginSlice();
        Pr57Crossing259BeginSlice();
        // Same order as OrderBy(WaitingSince).ThenBy(Id): Ids are unique, so the
        // key is a total order and any sort yields the same sequence.
        // PR57 isolated A/B: during a batched movement slice, the loop below
        // skips every ID outside _vehicleMovementBatch before any vehicle work.
        // Sort only that batch while retaining the exact WaitingSince/Id order
        // among all vehicles that can execute. The all-vehicle junction cleanup
        // above and every physical candidate scan remain unchanged.
        var pr57BatchOrder=Environment.GetEnvironmentVariable("PR57_BATCH_ORDER")=="1"&&operationsDt is null?_vehicleMovementBatch:null;
        var ordered=pr57BatchOrder is null?State.Vehicles.ToArray():State.Vehicles.Where(v=>pr57BatchOrder.Contains(v.Id)).ToArray();
        Array.Sort(ordered,static (a,b)=>a.WaitingSince.CompareTo(b.WaitingSince) is var c and not 0?c:a.Id.CompareTo(b.Id));
        // PR57 isolated A/B: parked passenger cars are not lane occupants and
        // BodyAcrossLane explicitly excludes passenger cars. Build a stable
        // original-order candidate list once per movement slice.
        // Keep entry-frame occupants even if their live state has changed.
        var pr57ActiveScan=Environment.GetEnvironmentVariable("PR57_ACTIVE_SCAN")=="1";
        var pr57SpatialScan=Environment.GetEnvironmentVariable("PR57_SPATIAL_SCAN")=="1";
        // PR57: experimental allocation-light, exact-order spatial bitsets (off by default).
        var pr57BitsetScan=Environment.GetEnvironmentVariable("PR57_BITSET_SCAN")=="1";
        // PR57 isolated A/B: far leaders cannot restrict this quantum's
        // distance, but blocked decisions still recover the exact full winner.
        // All physical predicates and all global movement slices are retained.
        var pr57CausalHorizon=Environment.GetEnvironmentVariable("PR57_CAUSAL_HORIZON")=="1";
        // PR57 structural POC: exact, slice-local ordered axis sweep, never a
        // finite-distance lane cutoff. Tow handoffs conservatively use full scans.
        var pr57AxisSweep=Environment.GetEnvironmentVariable("PR57_AXIS_SWEEP")=="1"
            &&!State.Vehicles.Any(o=>o.TowPhase!=TowPhase.None||o.Status==VehicleStatus.Towed||o.TowTruckId!=null);
        _pr57AxisSweepDirty=false;
        var pr57LaneGrid=pr57BitsetScan||pr57CausalHorizon;
        var pr57Bvh=Pr57DynamicBvhEnabled&&!pr57SpatialScan&&!pr57LaneGrid&&!pr57ActiveScan;
        if(pr57Bvh){
            if(_pr57VehicleBvh is null||!_pr57VehicleBvh.Matches(State.Vehicles))
                _pr57VehicleBvh=new Pr57VehicleBvh(State.Vehicles);
            else _pr57VehicleBvh.Refresh();
            if(!_pr57BvhAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1"){
                Console.WriteLine("PR57_DYNAMIC_BVH active=True");
                _pr57BvhAnnounced=true;
            }
        }
        var pr57FastScan=Environment.GetEnvironmentVariable("PR57_FAST_SCAN")=="1";
        // Independent A/B switch: isolate heading reuse from bend broad-phase.
        var pr57BendPrefilter=pr57FastScan&&Environment.GetEnvironmentVariable("PR57_BEND_PREFILTER")!="0";
        // PR57 A/B: a zero step cannot be shortened by bend bodies. When the
        // current heading already equals the approach heading, the only other
        // bend-scan effect (restoring that heading) is also a no-op.
        var pr57ZeroBendSkip=Environment.GetEnvironmentVariable("PR57_ZERO_BEND_SKIP")=="1";
        // PR57 A/B: reuse bend-leader storage across movement iterations, preserving the full predicate and order.
        var pr57BendReuse=pr57BitsetScan&&!pr57BendPrefilter&&Environment.GetEnvironmentVariable("PR57_BEND_REUSE")=="1";
        // PR57 A/B: the vehicle does not change during a leader enumeration;
        // hoist its dock eligibility check outside the per-candidate loop.
        var pr57DockGateHoist=pr57BitsetScan&&Environment.GetEnvironmentVariable("PR57_DOCK_GATE_HOIST")=="1";
        // PR57 A/B: conservative prefilter for BodyAcrossLane only (its exact 20m test remains).
        var pr57DockAabb=pr57BitsetScan&&Environment.GetEnvironmentVariable("PR57_DOCK_AABB")=="1";
        // PR57 A/B: reuse exact longitudinal/lateral projections already computed by the conservative gate.
        var pr57GateReuse=pr57BitsetScan&&Environment.GetEnvironmentVariable("PR57_GATE_REUSE")=="1";
        // PR57 A/B: exact-order bitset cells intersecting the full, conservative
        // lane corridor, including the 20m dock-body exception (off by default).
        var pr57CorridorScan=pr57BitsetScan&&Environment.GetEnvironmentVariable("PR57_CORRIDOR_SCAN")=="1";
        // PR57 A/B: separate the exact 20m dock-body neighborhood from the
        // 2.6m forward-lane corridor; their union retains original vehicle order.
        var pr57SplitCorridor=pr57CorridorScan&&Environment.GetEnvironmentVariable("PR57_SPLIT_CORRIDOR")=="1";
        var pr57ScanCandidates=pr57ActiveScan
            ?State.Vehicles.Where(o=>!o.PassengerCar||o.Status!=VehicleStatus.Parked||_entryFrame?.ContainsKey(o.Id)==true).ToList()
            :State.Vehicles;
        var pr57ScanIds=pr57ActiveScan?pr57ScanCandidates.Select(o=>o.Id).ToHashSet():null;
        var pr57VehicleOrder=pr57ActiveScan||pr57SpatialScan||pr57LaneGrid?State.Vehicles.Select((o,i)=>new{o.Id,Index=i}).ToDictionary(x=>x.Id,x=>x.Index):null;
        // PR57 spatial A/B: both indexes are per movement slice, updated after
        // each processed vehicle. Exact lane/bend predicates still decide every
        // collision. Bitset enumeration follows original State.Vehicles order,
        // without per-query sorting, per-cell lists, or a distance cutoff.
        const double pr57CellWidth=32;
        static (int X,int Z) Pr57Cell(Point p)=>((int)Math.Floor(p.X/pr57CellWidth),(int)Math.Floor(p.Z/pr57CellWidth));
        var pr57Cells=pr57SpatialScan?new Dictionary<(int X,int Z),List<Vehicle>>():null;
        var pr57BitsetCells=pr57LaneGrid?new Dictionary<(int X,int Z),ulong[]>():null;
        var pr57BitsetWords=(State.Vehicles.Count+63)/64;
        var pr57BitsetScratch=pr57LaneGrid?new ulong[pr57BitsetWords]:null;
        var pr57BitsetResults=pr57LaneGrid?new List<Vehicle>(64):null;
        var pr57BendLeadersReuse=pr57BendReuse?new List<Vehicle>(32):null;
        var pr57SpatialIds=pr57SpatialScan||pr57LaneGrid?pr57ScanCandidates.Select(o=>o.Id).ToHashSet():null;
        var pr57WorldExtent=(pr57SpatialScan||pr57BitsetScan)&&pr57ScanCandidates.Count>0
            ?pr57ScanCandidates.Max(o=>Math.Max(Math.Abs(o.Position.X),Math.Abs(o.Position.Z))):0;
        void Pr57Add(Vehicle o){
            var key=Pr57Cell(o.Position);
            if(pr57BitsetCells is not null){
                if(!pr57BitsetCells.TryGetValue(key,out var bits))pr57BitsetCells[key]=bits=new ulong[pr57BitsetWords];
                var index=pr57VehicleOrder![o.Id];
                bits[index/64]|=1UL<<(index%64);
            }else{
                if(!pr57Cells!.TryGetValue(key,out var list))pr57Cells[key]=list=new();
                list.Add(o);
            }
        }
        void Pr57Remove(Vehicle o,(int X,int Z) key){
            if(pr57BitsetCells is not null){
                var bits=pr57BitsetCells[key];
                var index=pr57VehicleOrder![o.Id];
                bits[index/64]&=~(1UL<<(index%64));
                if(bits.All(word=>word==0))pr57BitsetCells.Remove(key);
            }else{
                var list=pr57Cells![key];
                list.Remove(o);
                if(list.Count==0)pr57Cells.Remove(key);
            }
        }
        if(pr57Cells is not null||pr57BitsetCells is not null)
            foreach(var o in pr57ScanCandidates)Pr57Add(o);
        List<Vehicle> Pr57Query(double x0,double x1,double z0,double z1){
            var first=Pr57Cell(new Point(x0,z0));var last=Pr57Cell(new Point(x1,z1));
            if(pr57BitsetCells is not null){
                // A very long diagonal corridor is cheaper to scan in full.
                // This is a conservative fallback, never a physical cutoff.
                if(((long)last.X-first.X+1)*((long)last.Z-first.Z+1)>256)
                    return pr57ScanCandidates;
                var found=pr57BitsetResults!;
                found.Clear();
                var scratch=pr57BitsetScratch!;
                Array.Clear(scratch);
                for(var x=first.X;x<=last.X;x++)for(var z=first.Z;z<=last.Z;z++)
                    if(pr57BitsetCells.TryGetValue((x,z),out var bits))
                        for(var i=0;i<scratch.Length;i++)scratch[i]|=bits[i];
                for(var i=0;i<scratch.Length;i++){
                    var word=scratch[i];
                    while(word!=0){
                        var bit=System.Numerics.BitOperations.TrailingZeroCount(word);
                        found.Add(State.Vehicles[i*64+bit]);
                        word&=word-1;
                    }
                }
                return found;
            }
            var oldFound=new List<Vehicle>();
            for(var x=first.X;x<=last.X;x++)for(var z=first.Z;z<=last.Z;z++)
                if(pr57Cells!.TryGetValue((x,z),out var cell))oldFound.AddRange(cell);
            oldFound.Sort((a,b)=>pr57VehicleOrder![a.Id].CompareTo(pr57VehicleOrder[b.Id]));
            return oldFound;
        }
        List<Vehicle> Pr57LaneCorridorCandidates(Vehicle v,double reach){
            // All ordinary leaders require lateral distance <2.6m; the
            // BodyAcrossLane exception requires Euclidean distance <=20m.
            // A 32m cell's center differs by at most 16m in each axis.
            // Expand BOTH projections by the exact cell support plus a
            // floating-point margin; never cap the number of vehicles.
            var sin=Math.Sin(v.Heading);var cos=Math.Cos(v.Heading);
            var margin=(pr57SplitCorridor?2.6:20)+pr57CellWidth/2*(Math.Abs(sin)+Math.Abs(cos))+1e-7;
            // A dock body is relevant only within 20m, regardless of heading.
            // Include every cell intersecting that disk, even behind the vehicle.
            var dockCellRadius=20+pr57CellWidth/Math.Sqrt(2)+1e-7;
            var dockCellRadiusSquared=dockCellRadius*dockCellRadius;
            var scratch=pr57BitsetScratch!;
            Array.Clear(scratch);
            foreach(var (cell,bits) in pr57BitsetCells!){
                var dx=(cell.X+0.5)*pr57CellWidth-v.Position.X;
                var dz=(cell.Z+0.5)*pr57CellWidth-v.Position.Z;
                // Either the exact forward-lane strip or the full dock-body
                // disk can matter. Conservative cell support prevents false
                // negatives; exact physics still runs in State.Vehicles order.
                var mayHoldDock=pr57SplitCorridor&&dx*dx+dz*dz<=dockCellRadiusSquared;
                if(!mayHoldDock){
                    if(Math.Abs(dx*cos-dz*sin)>margin)continue;
                    var forward=dx*sin+dz*cos;
                    if(forward< -margin||forward>reach+margin)continue;
                }
                for(var i=0;i<scratch.Length;i++)scratch[i]|=bits[i];
            }
            var found=pr57BitsetResults!;
            found.Clear();
            for(var i=0;i<scratch.Length;i++){
                var word=scratch[i];
                while(word!=0){
                    var bit=System.Numerics.BitOperations.TrailingZeroCount(word);
                    found.Add(State.Vehicles[i*64+bit]);
                    word&=word-1;
                }
            }
            return found;
        }
        List<Vehicle> Pr57LaneCandidates(Vehicle v,bool entering,double maxStep){
            if(pr57CausalHorizon&&!entering){
                // A normal leader affecting this step has forward <= maxStep+8
                // and lateral <2.6m. Dock blockers are within 20m. The square
                // encloses both without excluding any possible exact hit.
                var forward=maxStep+8;
                var radius=Math.Max(20,Math.Sqrt(forward*forward+2.6*2.6))+1e-6;
                var nearby=Pr57Query(v.Position.X-radius,v.Position.X+radius,
                                     v.Position.Z-radius,v.Position.Z+radius);
                if(!_pr57CausalHorizonAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1"){
                    Console.WriteLine($"PR57_CAUSAL_HORIZON used=True candidates={nearby.Count} total={State.Vehicles.Count} radius={radius:F3}");
                    _pr57CausalHorizonAnnounced=true;
                }
                return nearby;
            }
            // Entry admission uses a speed-dependent corridor: never spatially filter it.
            if(pr57Bvh&&!entering)return _pr57VehicleBvh!.Lane(v);
            if(!(pr57SpatialScan||pr57BitsetScan)||entering)return pr57ScanCandidates;
            // The leader ray extends beyond all indexed positions, with 20m
            // clearance for the existing BodyAcrossLane special case.
            var reach=2*(pr57WorldExtent+Math.Max(Math.Abs(v.Position.X),Math.Abs(v.Position.Z)))+40;
            if(pr57CorridorScan)return Pr57LaneCorridorCandidates(v,reach);
            var end=new Point(v.Position.X+Math.Sin(v.Heading)*reach,v.Position.Z+Math.Cos(v.Heading)*reach);
            return Pr57Query(Math.Min(v.Position.X,end.X)-20,Math.Max(v.Position.X,end.X)+20,
                             Math.Min(v.Position.Z,end.Z)-20,Math.Max(v.Position.Z,end.Z)+20);
        }
        var pr57AxisIndex=pr57AxisSweep?new Pr57AxisSweepIndex(State.Vehicles,
            Environment.GetEnvironmentVariable("PR57_AXIS_SWEEP_VERIFY")=="1"):null;
        if(pr57AxisIndex is {Valid:false})pr57AxisIndex=null;
        if(pr57AxisIndex is not null&&!_pr57AxisSweepAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1"){
            Console.WriteLine($"PR57_AXIS_SWEEP active=True vehicles={State.Vehicles.Count}");
            _pr57AxisSweepAnnounced=true;
        }
        if(scanProfile is not null)_pr57MovementAllocBytes[1]+=GC.GetAllocatedBytesForCurrentThread()-pr57AllocCallStart;
        Pr57SetupGapEnd(ordered.Length,pr57ScanCandidates.Count);
        Pr57DenseTrigFrame554? pr57DenseTrig554=null;
        foreach(var v in ordered){
            Pr57Effect253BeforeActor(v);
            Pr57PhaseImpactBeforeActor(v);
            Pr57ExclusiveBeginVehicle(v);
            var pr57IdleStart=pr57IdleProbe?Pr57IdleSnapshot(v):default;
            if(pr57IdleProbe)Pr57BlockBeginVehicle(v);
            long pr57IdleLaneVisits=0,pr57IdleBendVisits=0;
            var pr57OldPosition=pr57AxisIndex is null?default:v.Position;
            var pr57OldCell=pr57SpatialScan||pr57LaneGrid?Pr57Cell(v.Position):default;
            // PR57 observation only: account for the residual outside the six measured calls.
            // Stages: 0=vehicle operations, 1=route/entry preparation, 2=post-safety hold,
            // 3=between existing measured calls. All counters are disabled without profiling.
            var pr57VehicleAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
            var pr57VehicleAllocStage=0;
            // Capture before operations; the timer stops before existing finally diagnostics.
            // These measurements are deliberately intrusive and never used for wall A/B.
            var pr57CostStartTicks=pr57CostProbe?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var pr57CostStartBytes=pr57CostProbe?GC.GetAllocatedBytesForCurrentThread():0L;
            var pr57CostOldPosition=pr57CostProbe?v.Position:default;
            var pr57CostOldClaim=pr57CostProbe?v.IntersectionClaim:null;
            var pr57CostOldSpaces=pr57CostProbe?v.ManoeuvreSpaces.Count:0;
            try{
            // PR57: isolate whether the lateral entrant already obstructs the through lane
            // when it starts travelling, or reaches that pose during admission.
            if(scanProfile is not null&&v.Id==5183){
                var probeTime=State.ElapsedSeconds+_vehicleMovementOffset;
                var probeSecond=(int)Math.Floor(probeTime);
                if(probeSecond>=3550&&probeSecond<=3620&&probeSecond!=_laneEntryOriginLastSecond&&
                   (probeSecond%10==0||probeSecond>=3595)){
                    _laneEntryOriginLastSecond=probeSecond;
                    LaneAccess probeAccess=default;
                    var probeEligible=TryLaneEntry(v,out probeAccess);
                    var probe5294=State.Vehicles.FirstOrDefault(o=>o.Id==5294);
                    var probe5179=State.Vehicles.FirstOrDefault(o=>o.Id==5179);
                    var blocked5294=probeEligible&&probe5294 is not null&&BlocksLaneEntry(v,probeAccess,EntryFrameVehicle(probe5294));
                    var blocked5179=probeEligible&&probe5179 is not null&&BlocksLaneEntry(v,probeAccess,EntryFrameVehicle(probe5179));
                    // Observational only: inspect the same full first-node swept
                    // geometry already used for truck manoeuvre admission.
                    // No traffic priority, position or event is modified.
                    LaneAccess otherAccess=default;
                    var otherEntryEligible=probe5294 is not null&&TryLaneEntry(probe5294,out otherAccess);
                    var firstNodeSweepsOverlap=false;
                    var firstNodeSweepPairs=0;
                    if(probeSecond==3608&&probeEligible&&probe5294 is {Status:VehicleStatus.Travelling}){
                        var entrantWay=Manoeuvre(v,v.Route,0,v.Position,v.Heading,[],false,int.MaxValue,out _,out _,through:0);
                        var otherWay=Manoeuvre(probe5294,probe5294.Route,0,probe5294.Position,probe5294.Heading,[],false,int.MaxValue,out _,out _,through:0);
                        foreach(var p in entrantWay)foreach(var q in otherWay){
                            if(p.Position.Distance(q.Position)<12&&BodiesOverlap(v,p.Position,p.Heading,probe5294,q.Position,q.Heading))
                                firstNodeSweepPairs++;
                        }
                        firstNodeSweepsOverlap=firstNodeSweepPairs>0;
                        // Inspect the *same* sampled body sweeps beyond one node.
                        // Pure observation: the first node alone missed the real hold.
                        foreach(var through in new[]{1,2,4}){
                            var extendedEntrant=Manoeuvre(v,v.Route,0,v.Position,v.Heading,[],false,int.MaxValue,out _,out _,through:through);
                            var extendedOther=Manoeuvre(probe5294,probe5294.Route,0,probe5294.Position,probe5294.Heading,[],false,int.MaxValue,out _,out _,through:through);
                            var conflictPairs=0;
                            foreach(var a in extendedEntrant)foreach(var b in extendedOther)
                                if(a.Position.Distance(b.Position)<12&&BodiesOverlap(v,a.Position,a.Heading,probe5294,b.Position,b.Heading))conflictPairs++;
                            Console.WriteLine($"PR57_ENTRY_SWEEP second={probeTime:F6} throughIndex={through} firstNodePairs={firstNodeSweepPairs} extendedPairs={conflictPairs} entrantPoses={extendedEntrant.Count} otherPoses={extendedOther.Count}");
                        }
                    }
                    var crossingNow=false;var crossingNext=false;
                    if(probe5294 is not null&&probe5294.Position.Distance(v.Position)<20&&
                       TryMovementTarget(probe5294,out _,out var probeTarget,out _,out _,out _)){
                        var towards=probeTarget-probe5294.Position;
                        var heading=towards.Distance(default)>.00001?Math.Atan2(towards.X,towards.Z):probe5294.Heading;
                        crossingNow=BodiesOverlap(probe5294,probe5294.Position,heading,v,v.Position,v.Heading);
                        crossingNext=BodiesOverlap(probe5294,probe5294.Position.MoveTowards(probeTarget,.5),heading,v,v.Position,v.Heading);
                    }
                    Console.WriteLine($"LANE_ENTRY_ORIGIN time={probeTime:F6} entrant=5183 status={v.Status} cause={v.Cause?.Replace(' ','_')} x={v.Position.X:F6} z={v.Position.Z:F6} heading={v.Heading:F6} routeIndex={v.RouteIndex} waitingSince={v.WaitingSince:F6} eligible={probeEligible} fromSide={probeAccess.FromSide:F6} toSide={probeAccess.ToSide:F6} blockedBy5294={blocked5294} blockedBy5179={blocked5179} crossingNow={crossingNow} crossingNext05={crossingNext} leader5294X={probe5294?.Position.X:F6} leader5294Z={probe5294?.Position.Z:F6} leader5294Status={probe5294?.Status} leader5294EntryEligible={otherEntryEligible} leader5294FromSide={otherAccess.FromSide:F6} leader5294ToSide={otherAccess.ToSide:F6} firstNodeSweepsOverlap={firstNodeSweepsOverlap} firstNodeSweepPairs={firstNodeSweepPairs}");
                }
            }
            // A bypass belongs to the trip in progress: a stopped, parked or
            // replanned-elsewhere vehicle does not resume old waypoints.
            if(v.Detour.Count>0&&v.Status!=VehicleStatus.Travelling)v.Detour.Clear();
            if(_vehicleMovementBatch==null||operationsDt!=null){
                var operationTime=operationsDt??dt;
                v.FuelBurnedLastStep=0;
                if(v.Status==VehicleStatus.Towed)continue;
                // Lot manoeuvres, service queues, exterior service and calls for parked broken vehicles.
                if((v.ServicePhase!=ServicePhase.None||v.ServiceShopId!=null||v.PushPath.Count>0||v.Status==VehicleStatus.Outside)&&TendService(v,operationTime))continue;
                if(IsTow(v)&&OperateTow(v,operationTime))continue;
                if(!v.External&&v.Status is VehicleStatus.Travelling or VehicleStatus.Unloading or VehicleStatus.Waiting){var actualDriver=CitizenById(v.DriverId) is {} c&&c.Alive&&(v.PassengerCar||IsTow(v)||c.Resident&&c.InsideMap)&&c.VehicleId==v.Id?c:null;if(actualDriver==null){v.Cause=v.PassengerCar||v.ShipmentId==null?IsTow(v)?"Guincho aguardando o motorista embarcar":"Aguardando motorista embarcar":"Sem motorista local presente; carga e viagem preservadas";continue;}}
                if(!v.PassengerCar&&v.DriverId is {} driver&&CitizenById(driver) is {} citizen&&v.Status is VehicleStatus.Travelling or VehicleStatus.Unloading){citizen.Position=v.Position;citizen.AddWorkSecondsToday(operationTime);AccrueWage(citizen,operationTime);citizen.Activity=IsTow(v)?"Dirigindo o guincho da oficina":"Conduzindo entrega real";}
                if(v.Status==VehicleStatus.Unloading){v.AddLoadTimer(-(operationTime));if(v.LoadTimer<=0)VehicleArrived(v);continue;}
                if(v.Machine!=PavingMachine.None&&v.Status==VehicleStatus.Waiting){ReplanPavingMachine(v);continue;}
                if(v.PassengerCar&&v.Status==VehicleStatus.Waiting){ReplanCar(v);continue;}
                if(v.Status==VehicleStatus.Stranded){TendStranded(v);continue;}
                if(v.Status==VehicleStatus.Waiting){if(v.ShipmentId is {} si&&State.Shipments.FirstOrDefault(s=>s.Id==si) is {} shipment){if(shipment.Cancelled)RedirectCancelled(v,shipment);else ReplanVehicle(v,shipment);}else if(v.External)ReturnExternal(v);else if(v.HomeBuildingId!=null&&v.DestinationBuildingId==v.HomeBuildingId)ReturnLocal(v);continue;}
                if(v.Status!=VehicleStatus.Travelling)continue;
                if(v.Route.Count==0||v.Route.Any(id=>!_roads.ContainsKey(id))){v.Status=VehicleStatus.Waiting;v.Cause="Rota interrompida; aguarda ligação viária";continue;}
                _vehicleMovementBatch?.Add(v.Id);
            }else if(!_vehicleMovementBatch.Contains(v.Id)||v.Status!=VehicleStatus.Travelling)continue;
            // #187: count actual physical candidates after the existing eligibility guard.
            if(Pr57SliceWorksetEnabled&&scanProfile is not null&&dt>0&&
               _vehicleMovementBatch is not null&&operationsDt is null)
                _pr57_187MotionAttempts++;
            var remaining=dt;
            while(remaining>.000001&&v.Status==VehicleStatus.Travelling){
                Pr57ExclusiveMark(1);
                if(pr57IdleProbe)Pr57BlockMarkStage(1);
                if(scanProfile is not null){
                    var allocNow=GC.GetAllocatedBytesForCurrentThread();
                    if(pr57VehicleAllocStage==0)_pr57MovementAllocBytes[7]+=allocNow-pr57VehicleAllocStart;
                    else if(pr57VehicleAllocStage==1)_pr57MovementAllocBytes[8]+=allocNow-pr57VehicleAllocStart;
                    else if(pr57VehicleAllocStage==2)_pr57MovementAllocBytes[9]+=allocNow-pr57VehicleAllocStart;
                    pr57VehicleAllocStart=allocNow;
                    pr57VehicleAllocStage=1;
                }
                var pr57RegionStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                // The way back from a works-site dock is admitted whole (Mobility.SharedSpace).
                if(v.SitePathBeforeRoad&&(AtSiteDock(v)||v.ManoeuvreDock!=null)&&Pr57273WayOut(v,out var siteSpaces) is var siteWay&&!Pr57273Admit(v,siteWay,siteSpaces,v.ManoeuvreDock??v.SitePath[0],true)){if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;}
                if(v.SitePathBeforeRoad){if(!MoveConstructionSitePath(v,ref remaining,2))break;v.SitePathBeforeRoad=false;v.SitePath=[];v.SitePathIndex=0;}
                if(_vehicleMovementBatch!=null)_vehicleMovementClock=State.ElapsedSeconds+_vehicleMovementOffset+dt-remaining;
                if(v.Detour.Count>0){MoveAlongDetour(v,ref remaining);ClearPassedJunction(v);continue;}
                // An approach already begun keeps its existing physical backing-out
                // path when road traffic obstructs it; admission cannot prevent that recovery.
                if(OnAsphaltApproach(v)&&YieldAsphaltApproach(v,ref remaining,2))break;
                if(v.RouteIndex>=v.Route.Count){if(!v.PassengerCar&&!IsTow(v)&&v.RefuellingAt==null&&v.Machine==PavingMachine.None&&v.SitePathIndex==0&&v.DockPosition is {} entryDock&&v.Position.Distance(entryDock)>.01&&!DockAdmitted(v,entryDock)&&Pr57273BayEntryHold(v).Count>0){HeldCargoBayBlocked(v,entryDock);if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;}if(v.Machine==PavingMachine.None&&v.SitePathIndex<v.SitePath.Count){if(!MoveConstructionSitePath(v,ref remaining,2))break;}var carSpot=v.PassengerCar&&v.RefuellingAt==null;if(v.DockPosition is {} dock&&v.Position.Distance(dock)>.01){if(!carSpot&&HeldCargoBayBlocked(v,dock))break;
                    // A machine has no bay admission: its slide across the lane waits for any body on it.
                    if(v.Machine!=PavingMachine.None&&MachineDockSlideBlocked(v,dock)){v.Cause="Fila para acesso de carga";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;}var blocked=!carSpot&&State.Vehicles.Any(o=>o.Id!=v.Id&&!(o.PassengerCar&&o.Status==VehicleStatus.Parked)&&o.Status is VehicleStatus.Unloading or VehicleStatus.Parked&&o.Position.Distance(dock)<4);if(blocked){v.Cause="Fila para acesso de carga";break;}var distance=v.Position.Distance(dock);var moved=Math.Min(distance,State.Tuning.VehicleSpeed*remaining);if(v.Fuel<moved*FuelRate(v)){RunOutOfFuel(v);break;}if(v.TowPhase==TowPhase.ToVehicle&&!PickupLaneClear(v,dock)){v.Cause="Guincho aguardando a faixa livre para encostar";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;}var toDock=dock-v.Position;if(v.PassengerCar&&distance>.01)v.Heading=Math.Atan2(toDock.X,toDock.Z);v.Position=v.Position.MoveTowards(dock,moved);Wear(v,moved);v.AddFuel(-(moved*FuelRate(v)));v.AddFuelBurnedLastStep(moved*FuelRate(v));remaining-=moved/State.Tuning.VehicleSpeed;if(moved<distance-.01)break;}if(v.TowPhase!=TowPhase.None){TowReachedDock(v);break;}if(v.ServicePhase==ServicePhase.ToService){ServiceArrived(v);break;}if(carSpot){CarReachedSpot(v);break;}v.Status=VehicleStatus.Unloading;v.LoadTimer=15;v.IntersectionClaim=null;v.Cause=v.RefuellingAt!=null?"Abastecendo combustível":v.ShipmentId is {} shipmentId&&State.Shipments.Any(s=>s.Id==shipmentId&&!s.PickedUp)?"Carregando na origem":"Descarregando no destino";break;}
                if(scanProfile is not null){_pr57RegionAllocBytes[0]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;pr57RegionStart=GC.GetAllocatedBytesForCurrentThread();}
                var node=_roads[v.Route[v.RouteIndex]];if(node.ConstructionOnly&&!ConstructionTrip(v)){v.Status=VehicleStatus.Waiting;v.Cause="Rua ainda em obra: tráfego normal aguarda conclusão";break;}if(!node.Highway&&node.MaintenanceRatio<=.05){v.Status=VehicleStatus.Waiting;v.Cause="Via sem capacidade por manutenção não realizada";break;}var previous=v.RouteIndex>0?_roads[v.Route[v.RouteIndex-1]].Position:v.Position;
                var delta=v.RouteIndex==0&&v.Route.Count>1?_roads[v.Route[1]].Position-node.Position:node.Position-previous;var len=delta.Distance(new(0,0));var target=node.Position;
                if(len>.01)target+=new Point(delta.Z/len*1.65,-delta.X/len*1.65);
                target+=InnerCornerOffset(v.Route,v.RouteIndex,node,delta,len);
                var d=v.Position.Distance(target);var speed=(node.Highway?State.Tuning.HighwaySpeed:State.Tuning.VehicleSpeed)*Math.Max(.15,node.MaintenanceRatio);
                // Already on this lane line past its point (e.g. a U-turn lands just
                // beyond it): that point is reached, rather than turning back to it.
                if(v.RouteIndex>0&&v.RouteIndex+1<v.Route.Count&&len>.01&&d>.001&&Math.Abs(((v.Position.X-target.X)*delta.Z-(v.Position.Z-target.Z)*delta.X)/len)<.05&&
                    ((v.Position.X-target.X)*delta.X+(v.Position.Z-target.Z)*delta.Z)/len>0){v.RouteIndex++;continue;}
                // Leaving a bay or dock is admitted whole, or the truck stays there (Mobility.SharedSpace).
                if(LeavesDock(v)&&(v.ManoeuvreDock!=null||!OnCarriageway(v))&&Pr57273WayOut(v,out var exitSpaces) is var exitWay&&!Pr57273Admit(v,exitWay,exitSpaces,v.ManoeuvreDock??v.Position,true)){if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;}
                // The next junction is awaited at its stop line (also while the claim of
                // the one just passed is held, if it waits past it), and a delivery to a
                // busy bay just beyond it waits before it (Mobility.BayJunction).
                if(scanProfile is not null){_pr57RegionAllocBytes[1]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;pr57RegionStart=GC.GetAllocatedBytesForCurrentThread();}
                Pr57ExclusiveMark(2);
                var stopEarly=0.0;var bayHold=false;List<(Vehicle Body,Point Position,double Heading)>? crossingPoses=null;
                if(node.Neighbours.Count<3&&!node.Crosswalk&&Pr57273NextShared(v,node,delta,len,out var crossing,out var crossingIndex)&&v.IntersectionClaim!=crossing.Id){
                    if(WaitsClearOfPassedClaim(v,node,crossing,delta,len,target,d)){
                        stopEarly=StopLineBefore(v,node,crossing,delta,len);
                        if(Pr57273HoldBefore(v,crossingIndex,out var exits)){bayHold=true;crossingPoses=exits;stopEarly=Math.Max(stopEarly,.001);}
                        if(d<=stopEarly+.001){
                            if(bayHold){v.Cause="Fila para acesso de carga";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;}
                            if(stopEarly>0){v.RouteIndex++;continue;}
                        }
                    }
                    // The holder's remaining way (a machine leaving onto the lane just
                    // before the junction, say) is kept clear along the whole approach,
                    // also while the claim of the junction just passed is still held.
                    var turning=Pr57273Claim(v,crossing);if(turning.Count>0)(crossingPoses??=[]).AddRange(turning);
                }
                // Near its bay, a delivery not yet admitted keeps out of the dock corridor
                // and of the way out of the truck that owns it (Mobility.SharedSpace).
                if(scanProfile is not null){_pr57RegionAllocBytes[2]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;pr57RegionStart=GC.GetAllocatedBytesForCurrentThread();}
                var entryHeld=EntersBay(v)&&Pr57273BayEntryHold(v) is {Count:>0} entryHold?entryHold:null;
                if(entryHeld!=null)(crossingPoses??=[]).AddRange(entryHeld);
                var approachHeading=v.Heading;
                var facing=target-v.Position;if(d>.01)v.Heading=Math.Atan2(facing.X,facing.Z);
                // The exterior gateway is a staging origin (all external trucks
                // start there). A waiting cargo outside the map must not prevent
                // another real truck leaving to repair its broken road route.
                var insideMap=InMap(v.Position);
                double RoadSide(Point p)=>((p.X-node.Position.X)*delta.Z-(p.Z-node.Position.Z)*delta.X)/len;
                var fromSide=len>.01?RoadSide(v.Position):0;var toSide=len>.01?RoadSide(target):0;
                var access=new LaneAccess(node,delta,len,target,d,speed,fromSide,toSide);
                var enteringLane=v.PassengerCar&&v.RouteIndex==0&&insideMap&&len>.01&&Math.Abs(fromSide-toSide)>.1&&!AlreadyFollowingLane(v,node,approachHeading);
                bool BlocksEntry(Vehicle other)=>enteringLane&&Pr57StageBlocksEntry(v,access,other);
                // Apply the existing ID tie-break to the whole coincidence case.
                // A tiny positive forward projection must not make its winner
                // yield back to the other vehicle and freeze both indefinitely.
                var leaderDistance=double.MaxValue;Vehicle? blocker=null;
                var laneCandidateHits=0;
                void HoldFor(Vehicle other,double distance){Pr57PhaseImpactHold(other,distance);Pr57CausalHold(v,other);if(scanProfile is not null){laneCandidateHits++;scanProfile.ObserveLaneHoldDistance(v.Position,other.Position,enteringLane);}if(distance<leaderDistance){leaderDistance=distance;blocker=other;}}
                // A/B only: hoist invariant heading trigonometry for the original-order scan.
                var pr57Sin=pr57FastScan?Math.Sin(v.Heading):0;
                var pr57Cos=pr57FastScan?Math.Cos(v.Heading):0;
                var pr57DockGate=pr57DockGateHoist?(!LeavesDock(v)||OnCarriageway(v)):false;
                if(scanProfile is not null){
                    _pr57MovementAllocBytes[8]+=GC.GetAllocatedBytesForCurrentThread()-pr57VehicleAllocStart;
                    pr57VehicleAllocStage=3;
                }
                if(scanProfile is not null){_pr57RegionAllocBytes[3]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;pr57RegionStart=GC.GetAllocatedBytesForCurrentThread();}
                Pr57ExclusiveMark(3);
                var laneScanStart=scanProfile is null?0L:System.Diagnostics.Stopwatch.GetTimestamp();
                long pr57QueueLaneTicks=0,pr57QueueBendTicks=0;
                var pr57LaneAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                var pr57LaneList=pr57AxisIndex is not null&&!_pr57AxisSweepDirty
                    &&pr57AxisIndex.Count==State.Vehicles.Count
                    ?pr57AxisIndex.Lane(v,enteringLane)??Pr57LaneCandidates(v,enteringLane,speed*remaining)
                    :Pr57LaneCandidates(v,enteringLane,speed*remaining);
                if(pr57AxisIndex is not null&&!_pr57AxisSweepUsedAnnounced
                    &&pr57LaneList.Count<State.Vehicles.Count&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1"){
                    Console.WriteLine($"PR57_AXIS_SWEEP used=True candidates={pr57LaneList.Count} total={State.Vehicles.Count}");
                    _pr57AxisSweepUsedAnnounced=true;
                }
                void Pr57ScanLane(IEnumerable<Vehicle> candidates){
                // Whole-candidate production kernel: no radius/axis broadphase,
                // no snapshot, no reordering, no skipped actor or sub-slice.
                // The fallback retains all diagnostic observers and experimental
                // alternate algorithms. In particular, entry-frame candidates
                // are deliberately left to the original path.
                if(Pr57DenseLane554Enabled&&Pr57DenseLane554Safe&&scanProfile is null
                    &&!enteringLane&&!pr57ActiveScan&&!pr57SpatialScan
                    &&!pr57LaneGrid&&!pr57Bvh&&!pr57AxisSweep&&!pr57FastScan
                    &&ReferenceEquals(candidates,State.Vehicles))
                {
                    pr57DenseTrig554 ??= new Pr57DenseTrigFrame554(State.Vehicles);
                    var actorSin=Math.Sin(v.Heading);
                    var actorCos=Math.Cos(v.Heading);
                    var denseIndex=0;
                    if(!_pr57DenseLane554Announced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
                    {
                        Console.WriteLine("PR57_554_DENSE_TRIG used=True scope=full-canonical-order-no-observers");
                        _pr57DenseLane554Announced=true;
                    }
                    foreach(var other in State.Vehicles)
                    {
                        pr57DenseTrig554.Read(denseIndex++,other,out var otherSin,out var otherCos);
                        // Exactly the same physical predicates, winner tie-breaks,
                        // and short-circuit ordering as the canonical scan below.
                        if(other.Id!=v.Id&&(!LeavesDock(v)||OnCarriageway(v))&&
                           BodyAcrossLane(v,other) is {} docked)
                        {
                            HoldFor(other,docked);
                            continue;
                        }
                        if(other.Id==v.Id||!OccupiesLane(other))continue;
                        var relative=other.Position-v.Position;
                        if(insideMap&&relative.Distance(new(0,0))<.1)
                        {
                            if(other.Status==VehicleStatus.Stranded||other.Id<v.Id)HoldFor(other,0);
                            continue;
                        }
                        // enteringLane is false by construction; BlocksEntry is false.
                        if(other.Detour.Count>0&&Math.Cos(other.Heading-v.Heading)<-.5)
                        {
                            var ahead=relative.X*actorSin+relative.Z*actorCos;
                            var across=Math.Abs(relative.X*actorCos-relative.Z*actorSin);
                            if(ahead>0&&across<2.6){HoldFor(other,ahead);continue;}
                        }
                        var forward=relative.X*actorSin+relative.Z*actorCos;
                        var side=Math.Abs(relative.X*actorCos-relative.Z*actorSin);
                        if((other.TowPhase==TowPhase.Loading||
                            other.TowPhase==TowPhase.Delivering&&other.WaitingSince==0)&&
                            Math.Cos(other.Heading-v.Heading)<-.5&&forward>0&&side<1.2)
                        {
                            HoldFor(other,forward);
                            continue;
                        }
                        if(Math.Cos(other.Heading-v.Heading)>.5&&forward>0&&side<2.6)
                        {
                            var back=-relative.X*otherSin-relative.Z*otherCos;
                            var backSide=Math.Abs(-relative.X*otherCos+relative.Z*otherSin);
                            if(back>0&&backSide<2.6&&other.Id>v.Id&&other.Status!=VehicleStatus.Stranded)continue;
                            HoldFor(other,forward);
                        }
                    }
                    return;
                }
                var pr57StageScanStart=Pr57StageScanBegin(v);
                foreach(var liveOther in candidates){
                    if(pr57IdleProbe)pr57IdleLaneVisits++;
                    Pr57StageMark(0);
                    var other=enteringLane?Pr57StageEntryFrame(liveOther):liveOther;
                    var pr57FlipBefore=_pr57S3CounterActive?_pr57PhaseCurrentFlips.Count:0;
                    if(_pr57PhaseActive)Pr57PhaseExposureRead(v,other,ReferenceEquals(other,liveOther));
                    if(_pr57S3CounterActive&&_pr57PhaseCurrentFlips.Count>pr57FlipBefore)
                        Pr57S3CounterObserve(v,other,enteringLane,insideMap,access);
                    Pr57ActorShadowRead(v,other,ReferenceEquals(other,liveOther));
                    Pr57PairBeforeLaneCandidate(v,other,enteringLane);
                    Pr57CausalLaneCandidate(v,other);
                    if(Pr57NegativeEnabled)Pr57NegativeBeginCandidate(other);
                    // A truck stopped at a dock with its body across this lane (a street bay)
                    // is queued behind like a leader, keeping the queue gap (Mobility.SharedSpace).
                    if(other.Id!=v.Id&&(pr57DockGateHoist?pr57DockGate:(!LeavesDock(v)||OnCarriageway(v)))&&
                       (!pr57DockAabb||(!other.PassengerCar&&other.Machine==PavingMachine.None&&
                         Math.Abs(other.Position.X-v.Position.X)<=20.00000001&&Math.Abs(other.Position.Z-v.Position.Z)<=20.00000001))&&
                       Pr57StageBodyAcross(v,other) is {} docked){Pr57StageMark(1);if(Pr57NegativeEnabled)Pr57NegativeStage(1);HoldFor(other,docked);continue;}
                    if(other.Id==v.Id||!Pr57StageOccupies(other)){Pr57StageMark(2);if(Pr57NegativeEnabled)Pr57NegativeStage(2);continue;}
                    Pr57StageMark(3);
                    Pr57Stage249Lane(other,3);
                    if(Pr57NegativeEnabled)Pr57NegativeStage(3);
                    if(_pr57CandidateActive is not null)Pr57CandidateRead(other,3);
                    var relative=other.Position-v.Position;
                    if(insideMap&&relative.Distance(new(0,0))<.1){Pr57StageMark(4);if(other.Status==VehicleStatus.Stranded||other.Id<v.Id)HoldFor(other,0);continue;}
                    // Entry occupancy is measured along the ROAD axis, including
                    // the opposite lane, rather than the diagonal lot heading.
                    if(BlocksEntry(other)){Pr57StageMark(5);HoldFor(other,0);continue;}
                    Pr57StageMark(6);
                    // PR57 bitset A/B: the detour, opposing tow, and normal
                    // leader paths below all require forward > 0 and side < 2.6.
                    // Reject impossible candidates before the costly heading cosine;
                    // the exact predicates and original iteration order are unchanged.
                    var pr57GateForward=0.0;var pr57GateSide=0.0;
                    if(pr57BitsetScan){
                        var gateSin=pr57FastScan?pr57Sin:Math.Sin(v.Heading);
                        var gateCos=pr57FastScan?pr57Cos:Math.Cos(v.Heading);
                        pr57GateForward=relative.X*gateSin+relative.Z*gateCos;
                        if(!(pr57GateForward>0)){Pr57StageMark(7);continue;}
                        pr57GateSide=Math.Abs(relative.X*gateCos-relative.Z*gateSin);
                        if(!(pr57GateSide<2.6)){Pr57StageMark(8);continue;}
                    }
                    Pr57StageMark(9);
                    Pr57Stage249Lane(other,9);
                    if(_pr57CandidateActive is not null)Pr57CandidateRead(other,9);
                    var pr57CosDiff=pr57FastScan?Math.Cos(other.Heading-v.Heading):0;
                    // An overtaker committed to this lane: oncoming traffic waits for it.
                    if(other.Detour.Count>0&&(pr57FastScan?pr57CosDiff:Math.Cos(other.Heading-v.Heading))<-.5){var ahead=relative.X*(pr57FastScan?pr57Sin:Math.Sin(v.Heading))+relative.Z*(pr57FastScan?pr57Cos:Math.Cos(v.Heading));var across=Math.Abs(relative.X*(pr57FastScan?pr57Cos:Math.Cos(v.Heading))-relative.Z*(pr57FastScan?pr57Sin:Math.Sin(v.Heading)));if(ahead>0&&across<2.6){HoldFor(other,ahead);continue;}}
                    var forward=pr57GateReuse?pr57GateForward:relative.X*(pr57FastScan?pr57Sin:Math.Sin(v.Heading))+relative.Z*(pr57FastScan?pr57Cos:Math.Cos(v.Heading));
                    var side=pr57GateReuse?pr57GateSide:Math.Abs(relative.X*(pr57FastScan?pr57Cos:Math.Cos(v.Heading))-relative.Z*(pr57FastScan?pr57Sin:Math.Sin(v.Heading)));
                    // A tow facing it in its own lane while loading, or leaving with the load,
                    // holds it like a leader: having come from behind, it turns its rear to the
                    // vehicle, facing the queue. A tow that is itself held does not (it may wait
                    // on this very queue). Oncoming traffic in the other lane is 3.3 m aside.
                    if((other.TowPhase==TowPhase.Loading||other.TowPhase==TowPhase.Delivering&&other.WaitingSince==0)&&(pr57FastScan?pr57CosDiff:Math.Cos(other.Heading-v.Heading))< -.5&&forward>0&&side<1.2){HoldFor(other,forward);continue;}
                    if((pr57FastScan?pr57CosDiff:Math.Cos(other.Heading-v.Heading))>.5&&forward>0&&side<2.6){
                        // Side by side with converging headings (adjacent lot
                        // spaces), each is "ahead" of the other: only the higher
                        // ID yields, as in the coincidence case above.
                        var back=-relative.X*Math.Sin(other.Heading)-relative.Z*Math.Cos(other.Heading);
                        var backSide=Math.Abs(-relative.X*Math.Cos(other.Heading)+relative.Z*Math.Sin(other.Heading));
                        if(back>0&&backSide<2.6&&other.Id>v.Id&&other.Status!=VehicleStatus.Stranded)continue;
                        HoldFor(other,forward);
                    }
                }
                Pr57StageScanEnd(pr57StageScanStart);
                }
                Pr57CrossSliceObserve(v,0,enteringLane,speed*remaining);
                Pr57CandidateBeginQuery(v,0,enteringLane,speed*remaining,pr57LaneList.Count);
                Pr57Stage249BeginLane(pr57LaneList.Count);
                if(Pr57MutationAuditEnabled&&scanProfile is not null)Pr57MutationBeforeLeaderScan(v);
                if(Pr57LaneReadWriteEnabled&&scanProfile is not null)
                    Pr57LaneReadBeforeScan(v,enteringLane,speed*remaining,pr57LaneList.Count);
                Pr57PhaseImpactBeforeScan();
                Pr57ScanLane(pr57LaneList);
                Pr57CandidateEndQuery();
                if(blocker is not null)Pr57CausalWinner(v,blocker);
                if(scanProfile is not null){
                    scanProfile.MarkVehicleScan(0,laneScanStart,pr57LaneList.Count,laneCandidateHits);
                    if(blocker is not null)scanProfile.ObserveLaneLeaderSelection(v.Position,blocker.Position,leaderDistance,Math.Min(d,speed*remaining));
                    _pr57MovementAllocBytes[2]+=GC.GetAllocatedBytesForCurrentThread()-pr57LaneAllocStart;
                }
                if(pr57LimiterProbe)pr57QueueLaneTicks=System.Diagnostics.Stopwatch.GetTimestamp()-laneScanStart;
                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));
                Pr57S3CounterAfterScan(leaderDistance);
                Pr57PhaseImpactAfterScan(v,blocker,leaderDistance,step);
                var pr57QueueFirstLimit=pr57LimiterProbe&&step<.00001?(leaderDistance<=8?1:8):0;
                if(stopEarly>0)step=Math.Min(step,Math.Max(0,d-stopEarly));
                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=2;
                if(crossingPoses!=null)step=ClearOfPoses(v,target,step,crossingPoses);
                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=3;
                // At a bend the vehicle ahead in the same general direction can sit
                // beyond the straight-lane strip above; its body still bounds this
                // step and this turn. Opposite and parallel lanes are not involved.
                Pr57ExclusiveMark(4);
                var bendScanStart=scanProfile is null?0L:System.Diagnostics.Stopwatch.GetTimestamp();
                var pr57BendAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                var pr57BendList=pr57ZeroBendSkip&&step==0&&v.Heading==approachHeading?Pr57EmptyBendList
                    :pr57AxisIndex is not null&&!_pr57AxisSweepDirty&&pr57AxisIndex.Count==State.Vehicles.Count
                        ?pr57AxisIndex.Bend(v,step)
                    :pr57Bvh?_pr57VehicleBvh!.Bend(v,step)
                    :pr57SpatialScan||pr57BitsetScan?Pr57Query(v.Position.X-step-12,v.Position.X+step+12,v.Position.Z-step-12,v.Position.Z+step+12):pr57ScanCandidates;
                Pr57CrossSliceObserve(v,1,false,step);
                Pr57CandidateBeginQuery(v,1,false,step,pr57BendList.Count);
                if(pr57IdleProbe)pr57IdleBendVisits+=pr57BendList.Count;
                // A/B only: cheap conservative AABB and exact radius rejection BEFORE
                // heading/Ahead/body tests. The old order and all original exact predicates
                // remain in the control path; no finite lane-leader cutoff is introduced.
                var bendLeaders=pr57BendReuse?pr57BendLeadersReuse!:pr57BendPrefilter?new List<Vehicle>():pr57BendList.Where(o=>o.Id!=v.Id&&o.Status is VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded&&Math.Cos(o.Heading-v.Heading)>.5&&Ahead(v,o)>0&&
                    Math.Abs((o.Position.X-v.Position.X)*Math.Cos(v.Heading)-(o.Position.Z-v.Position.Z)*Math.Sin(v.Heading))>=2.6&&o.Position.Distance(v.Position)<=step+12&&
                    !(o.Position.Distance(State.Entry)<8&&v.Position.Distance(State.Entry)<8)).ToList();
                if(pr57BendReuse){
                    // Same short-circuit predicate as the control LINQ Where; no new spatial cutoff.
                    bendLeaders.Clear();
                    foreach(var o in pr57BendList)
                        if(o.Id!=v.Id&&o.Status is VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded&&Math.Cos(o.Heading-v.Heading)>.5&&Ahead(v,o)>0&&
                           Math.Abs((o.Position.X-v.Position.X)*Math.Cos(v.Heading)-(o.Position.Z-v.Position.Z)*Math.Sin(v.Heading))>=2.6&&o.Position.Distance(v.Position)<=step+12&&
                           !(o.Position.Distance(State.Entry)<8&&v.Position.Distance(State.Entry)<8))bendLeaders.Add(o);
                }
                else if(pr57BendPrefilter){
                    var reach=step+12;
                    var near=0;
                    foreach(var o in pr57BendList){
                        if(o.Id==v.Id||o.Status is not (VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded))continue;
                        // 1e-8 m margin prevents boundary rejection from rounding.
                        if(Math.Abs(o.Position.X-v.Position.X)>reach+1e-8||Math.Abs(o.Position.Z-v.Position.Z)>reach+1e-8)continue;
                        if(!(o.Position.Distance(v.Position)<=reach))continue;
                        near++;
                        if(Math.Cos(o.Heading-v.Heading)>.5&&Ahead(v,o)>0&&
                            Math.Abs((o.Position.X-v.Position.X)*Math.Cos(v.Heading)-(o.Position.Z-v.Position.Z)*Math.Sin(v.Heading))>=2.6&&
                            !(o.Position.Distance(State.Entry)<8&&v.Position.Distance(State.Entry)<8))bendLeaders.Add(o);
                    }
                    if(scanProfile is not null){scanProfile.BendPrefilterCandidates+=pr57BendList.Count;scanProfile.BendPrefilterNear+=near;}
                }
                Pr57Stage249FinishBend(pr57BendList.Count,bendLeaders);
                if(_pr57CandidateActive is not null)
                    foreach(var candidate in bendLeaders)Pr57CandidateRead(candidate,9);
                Pr57CandidateEndQuery();
                if(scanProfile is not null){scanProfile.MarkVehicleScan(1,bendScanStart,pr57BendList.Count,bendLeaders.Count);_pr57MovementAllocBytes[3]+=GC.GetAllocatedBytesForCurrentThread()-pr57BendAllocStart;}
                if(pr57LimiterProbe)pr57QueueBendTicks=System.Diagnostics.Stopwatch.GetTimestamp()-bendScanStart;
                if(bendLeaders.Count>0){
                    if(bendLeaders.Any(o=>TurnsInto(v,approachHeading,o))){v.Heading=approachHeading;step=0;}
                    else step=ClearOfPoses(v,target,step,bendLeaders.Where(o=>!VehicleBodiesOverlap(v,v.Position,o)).Select(o=>(o,o.Position,o.Heading)).ToList());
                }
                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=4;
                Pr57ExclusiveMark(5);
                var pr57SiteAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                var pr57SiteHotStart=Pr57HotTiming&&scanProfile is not null?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                var free=step;step=SafeSiteTravel(v,target,step,true);
                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=5;
                if(pr57SiteHotStart!=0){_pr57DirectMovementTicks[0]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57SiteHotStart;_pr57DirectMovementCalls[0]++;}
                if(scanProfile is not null)_pr57MovementAllocBytes[4]+=GC.GetAllocatedBytesForCurrentThread()-pr57SiteAllocStart;
                var afterSiteStep=step; // observação PR57: antes da verificação de travessia
                var pr57CrossingAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                var pr57CrossingHotStart=Pr57HotTiming&&scanProfile is not null?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                if(node.Neighbours.Count<3&&!node.Crosswalk)step=ClearOfCrossingAhead(v,target,step);
                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=6;
                if(pr57CrossingHotStart!=0){_pr57DirectMovementTicks[1]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57CrossingHotStart;_pr57DirectMovementCalls[1]++;}
                if(scanProfile is not null)_pr57MovementAllocBytes[5]+=GC.GetAllocatedBytesForCurrentThread()-pr57CrossingAllocStart;
                var afterCrossingStep=step; // observação PR57: sem alterar o passo
                var headingBeforeBlockProbe=v.Heading;
                var pr57TurnAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                var pr57TurnHotStart=Pr57HotTiming&&scanProfile is not null?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                var steer=step<.00001&&free>.00001?TurnClearance(v,approachHeading,free,access):null;
                if(pr57TurnHotStart!=0){_pr57DirectMovementTicks[2]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57TurnHotStart;_pr57DirectMovementCalls[2]++;}
                if(scanProfile is not null)_pr57MovementAllocBytes[6]+=GC.GetAllocatedBytesForCurrentThread()-pr57TurnAllocStart;
                if(steer is {} bend){v.Heading=bend.Heading;target=bend.To;step=bend.Distance;}
                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=7;
                if(scanProfile is not null){
                    pr57VehicleAllocStart=GC.GetAllocatedBytesForCurrentThread();
                    pr57VehicleAllocStage=2;
                }
                pr57RegionStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                Pr57ExclusiveMark(6);
                // Like a machine, a blocked vehicle keeps its verified pose rather
                // than turning in place into a parked machine's or site truck's body,
                // or into any other body that pose was clear of.
                Pr57PoseGuardBegin(v,step,d);
                // Only the possible assignment v.Heading=approachHeading is a
                // canonical effect of this predicate. Compare the exact double
                // representation (including signed zero), not approximate angles.
                var pr57PoseNoop=pr57PoseNoopSkip&&
                    Pr57PoseHeadingNoop(v.Heading,approachHeading);
                if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&step<.00001&&d>.00001){
                    pr57PoseNoopEligible++;
                    if(pr57PoseNoop)pr57PoseNoopSkipped++;
                }
                if(step<.00001&&d>.00001&&!pr57PoseNoop&&
                    (Pr57PoseForeachEnabled?Pr57PoseGuardForeach(v,approachHeading):
                    State.Vehicles.Any(o=>Pr57S6GateShadowObserve(v,o)&&o.Id!=v.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(v.Position)<12&&
                    Pr57PoseGuardCandidateReached(o)&&
                    (StreetBody(o)?VehicleBodiesOverlap(v,v.Position,o):!(o.Position.Distance(State.Entry)<8&&v.Position.Distance(State.Entry)<8)&&TurnsInto(v,approachHeading,o)))))v.Heading=approachHeading;
                Pr57PoseGuardEnd(v);
                if(scanProfile is not null&&Environment.GetEnvironmentVariable("PR57_TARGET_BLOCK_TRACE")=="1"&&(v.Id is 1107 or 487 or 1174)){
                    _pr57TargetTraceCounts.TryGetValue(v.Id,out var count);
                    count++;_pr57TargetTraceCounts[v.Id]=count;
                    if(count<=3||count%250==0)
                        Console.WriteLine($"PR57_TARGET_HOLD vehicle={v.Id} sample={count} time={VehicleMovementTime:F6} node={node.Id} route={v.RouteIndex} leaderId={blocker?.Id} leaderDistance={leaderDistance:F6} nominalDistance={d:F6} freeBeforeSafety={free:F6} afterSite={afterSiteStep:F6} afterCrossing={afterCrossingStep:F6} finalStep={step:F6} enteringLane={enteringLane} stopEarly={stopEarly:F6} bayHold={bayHold} entryHeld={entryHeld!=null} waitingSince={v.WaitingSince:F6} claim={v.IntersectionClaim} cause={v.Cause?.Replace(' ','_')}");
                }
                if(scanProfile is not null&&(_laneObservedWinner?.Id==v.Id&&(v.Id==3949||v.Id==5179)||_laneObservedWinner?.Id==5179&&v.Id==5294)&&step<.00001&&d>.00001){
                    _laneBlockProbeCounts.TryGetValue(v.Id,out var blockedCount);
                    blockedCount++;_laneBlockProbeCounts[v.Id]=blockedCount;
                    if(!_laneBlockProbeLeaders.TryGetValue(v.Id,out var leaders))_laneBlockProbeLeaders[v.Id]=leaders=new();
                    var leaderId=blocker?.Id??-1;
                    leaders.TryGetValue(leaderId,out var leaderCount);leaders[leaderId]=leaderCount+1;
                    if(blockedCount<=3||blockedCount%1000==0){
                        var top=string.Join(";",leaders.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).Take(3).Select(x=>$"{x.Key}:{x.Value}"));
                        var blockerDistance=blocker is null?-1:blocker.Position.Distance(v.Position);
                        // Observação geométrica somente na amostra impressa: o primeiro passo de 0,5m
                        // segue os mesmos candidatos físicos, sem tocar no estado da simulação.
                        var firstPose=v.Position.MoveTowards(target,Math.Min(.5,Math.Max(0,free)));
                        var selfNearby=v.Id!=5294?"n/a":string.Join(";",State.Vehicles.Where(o=>o.Id!=v.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(v.Position)<=14)
                            .OrderBy(o=>o.Position.Distance(v.Position)).ThenBy(o=>o.Id).Take(8)
                            .Select(o=>$"{o.Id}:{o.Position.Distance(v.Position):F2}:{o.Status}:{o.Cause?.Replace(' ','_')}"));
                        var siteBodyOverlaps=v.Id!=5294?"n/a":string.Join(";",State.Vehicles.Where(o=>o.Id!=v.Id&&o.Status!=VehicleStatus.Outside&&o.Machine!=PavingMachine.None&&o.Position.Distance(firstPose)<12&&
                            BodiesOverlap(v,firstPose,headingBeforeBlockProbe,o,o.Position,o.Heading)).Select(o=>o.Id));
                        var crossingBodyOverlaps=v.Id!=5294?"n/a":string.Join(";",State.Vehicles.Where(o=>o.Id!=v.Id&&InMap(o.Position)&&LaneTraffic(o)&&
                            (o.Position.X-v.Position.X)*Math.Sin(headingBeforeBlockProbe)+(o.Position.Z-v.Position.Z)*Math.Cos(headingBeforeBlockProbe)>0&&
                            (o.IntersectionClaim!=null||Math.Abs(Math.Cos(o.Heading-headingBeforeBlockProbe))<=.5)&&o.Position.Distance(v.Position)<=afterSiteStep+12&&
                            BodiesOverlap(v,firstPose,headingBeforeBlockProbe,o,o.Position,o.Heading)).Select(o=>o.Id));
                        var entrant5183=v.Id==5294?State.Vehicles.FirstOrDefault(o=>o.Id==5183):null;
                        LaneAccess entrantAccess=default;
                        var entrantEligible=entrant5183 is not null&&TryLaneEntry(entrant5183,out entrantAccess);
                        var entrantBlockers=!entrantEligible?"n/a":string.Join(";",State.Vehicles.Where(o=>o.Id!=entrant5183!.Id&&OccupiesLane(o)&&
                            BlocksLaneEntry(entrant5183,entrantAccess,EntryFrameVehicle(o))).OrderBy(o=>o.Id).Take(12).Select(o=>o.Id));
                        // Read-only snapshot of the immediate leader's surroundings, sampled at most every 1000 blocked slices.
                        var nearby=blocker is null?"none":string.Join(";",State.Vehicles.Where(o=>o.Id!=blocker.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(blocker.Position)<=14)
                            .OrderBy(o=>o.Position.Distance(blocker.Position)).ThenBy(o=>o.Id).Take(6)
                            .Select(o=>$"{o.Id}:{o.Position.Distance(blocker.Position):F2}:{o.Status}:{o.Cause?.Replace(' ','_')}"));
                        Console.WriteLine($"LANE_BLOCK_PROBE vehicle={v.Id} sample={blockedCount} time={VehicleMovementTime:F6} node={node.Id} routeIndex={v.RouteIndex} targetDistance={d:F6} blockerId={leaderId} blockerStatus={blocker?.Status} blockerDistance={blockerDistance:F6} blockerWait={(blocker?.WaitingSince??0):F6} blockerRouteIndex={blocker?.RouteIndex} blockerCause={blocker?.Cause?.Replace(' ','_')} blockerClaim={blocker?.IntersectionClaim} blockerX={blocker?.Position.X:F6} blockerZ={blocker?.Position.Z:F6} vehicleCauseBefore={v.Cause?.Replace(' ','_')} nearby={nearby} leaderDistance={leaderDistance:F6} freeBeforeSafety={free:F6} afterSite={afterSiteStep:F6} afterCrossing={afterCrossingStep:F6} finalStep={step:F6} selfNearby={selfNearby} siteBodyOverlaps={siteBodyOverlaps} crossingBodyOverlaps={crossingBodyOverlaps} entrant5183Eligible={entrantEligible} entrant5183Blockers={entrantBlockers} entrant5183Wait={entrant5183?.WaitingSince:F6} entrant5183Route={entrant5183?.RouteIndex} stopEarly={stopEarly:F6} bayHold={bayHold} entryHeld={entryHeld!=null} enteringLane={enteringLane} crossingBodies={crossingPoses?.Count??0} bendLeaders={bendLeaders.Count} waitingSince={v.WaitingSince:F6} blockerTop3={top}");
                    }
                }
                if(scanProfile is not null){_pr57RegionAllocBytes[4]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;pr57RegionStart=GC.GetAllocatedBytesForCurrentThread();}
                try{
                    if(pr57IdleProbe)Pr57BlockMarkStage(2);
                    if(step<.00001&&d>.00001){
                        // A distant leader is irrelevant to the travelled step
                        // but can still determine a wait cause or tow decision.
                        // Recover the exact original full-scan winner if needed.
                        if(pr57CausalHorizon&&!enteringLane&&blocker is null)
                            Pr57ScanLane(State.Vehicles);
                        var allocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                        // #254 observation: record the exact writer only when its label changes.
                        var pr57302BlockedPrior=Pr57302CauseIdsEnabled&&_pr57300Calls is >=76 and <=100?v.Cause:null;
                        var pr57Cause254Prior=Pr57Cause254Enabled&&_pr57Effect253Active?v.Cause:null;
                        v.Cause=blocker?.Status==VehicleStatus.Stranded?$"Aguardando veículo {blocker.Id} {(blocker.BrokenDown?"quebrado":"sem combustível")} na via":bayHold||entryHeld!=null?"Fila para acesso de carga":enteringLane?"Aguardando espaço para acessar a faixa":blocker!=null&&HoldsJunctionAhead(blocker,node)?(IsBend(node)?"Aguardando liberação da curva pelo veículo ":"Aguardando liberação do cruzamento pelo veículo ")+blocker.Id:"Fila na faixa";
                        if(Pr57Cause254Enabled&&_pr57Effect253Active)
                            Pr57Cause254Blocked(v,pr57Cause254Prior,blocker,leaderDistance,enteringLane,bayHold,entryHeld!=null,node.Id);
                        if(Pr57302CauseIdsEnabled)Pr57302Writer(v,"blocked",pr57302BlockedPrior,node.Id);
                        if(pr57LimiterProbe)Pr57QueueLimiterObserve(pr57QueueFirstLimit,v.Cause,pr57QueueLaneTicks,pr57QueueBendTicks,blocker!=null,enteringLane);
                        if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;
                        if(pr57IdleProbe)Pr57BlockObserve(v,blocker,node,enteringLane,bayHold,entryHeld!=null);
                        if(pr57DepProbe)Pr57DepBlocked(v,blocker);
                        if(scanProfile is not null){_pr57BlockedAllocBytes[0]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[0]++;}
                        if(v.TowPhase==TowPhase.ToVehicle){
                            allocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                            var recovered=TryTowContraflow(v)||TryTowFromBehind(v);
                            if(scanProfile is not null){_pr57BlockedAllocBytes[1]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[1]++;}
                            if(recovered)continue;
                        }
                        if(!enteringLane&&blocker!=null&&blocker.Id!=v.TowTargetId){
                            allocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                            var bypassed=TryBypassStopped(v,blocker);
                            if(scanProfile is not null){_pr57BlockedAllocBytes[2]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[2]++;}
                            if(bypassed)continue;
                        }
                        if(blocker!=null&&TowExitHolds(blocker)){
                            allocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                            var turned=TryUTurn(v,blocker);
                            if(scanProfile is not null){_pr57BlockedAllocBytes[3]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[3]++;}
                            if(turned)continue;
                        }
                        if(blocker!=null){
                            allocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                            var turned=TowHeldTooLong(v,blocker)&&TryUTurn(v,blocker,TowClosedRoads(v));
                            if(scanProfile is not null){_pr57BlockedAllocBytes[4]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[4]++;}
                            if(turned){CheckTowFuel(v);continue;}
                        }
                        allocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                        TowWaitsWithoutAccess(v);
                        if(scanProfile is not null){_pr57BlockedAllocBytes[5]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[5]++;}
                        break;
                    }
                }finally{
                    // A break/continue above previously escaped the postTurn region marker.
                    if(scanProfile is not null)_pr57RegionAllocBytes[5]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;
                }
                if(pr57IdleProbe)Pr57BlockMarkStage(3);
                // Entering is admitted whole too: a delivery near its bay takes no junction
                // or bend before its entry is admitted.
                pr57RegionStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                Pr57ExclusiveMark(7);
                // Include early break/continue in the measured admission region. The
                // finally block is observational; it never changes the decision.
                try{
                    var sharedAhead=node.Neighbours.Count>=3||node.Crosswalk||TurnsAtBend(v,v.RouteIndex);
                    if(sharedAhead&&!Holds(v,node.Id)&&entryHeld!=null){
                        if(scanProfile is not null)_pr57JunctionCalls[2]++;
                        v.Cause="Fila para acesso de carga";if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;break;
                    }
                    var junctionAllowed=true;
                    if(sharedAhead){
                        var pr57AdmissionStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                        if(scanProfile is not null)_pr57JunctionCalls[0]++;
                        var pr57JunctionHotStart=Pr57HotTiming&&scanProfile is not null?System.Diagnostics.Stopwatch.GetTimestamp():0L;
                        var pr57302JunctionPrior=Pr57302CauseIdsEnabled&&_pr57300Calls is >=76 and <=100?v.Cause:null;
                        try{Pr57CausalJunction(v,node);Pr57JunctionCensusObserve(v,node);Pr57JunctionReadersAdmission(v,node);junctionAllowed=MayEnterJunction(v,node);}
                        finally{
                            if(Pr57302CauseIdsEnabled)Pr57302Writer(v,"junction",pr57302JunctionPrior,node.Id);
                            if(pr57JunctionHotStart!=0){_pr57DirectMovementTicks[3]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57JunctionHotStart;_pr57DirectMovementCalls[3]++;}
                            if(scanProfile is not null)_pr57JunctionAllocBytes[0]+=GC.GetAllocatedBytesForCurrentThread()-pr57AdmissionStart;
                        }
                    }
                    if(sharedAhead&&!junctionAllowed){
                        if(pr57LimiterProbe)Pr57QueueLimiterObserve(9,v.Cause,pr57QueueLaneTicks,pr57QueueBendTicks,false,false);
                        if(pr57IdleProbe)Pr57BlockMarkStage(4);
                        var pr57DeniedStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                        if(scanProfile is not null)_pr57JunctionCalls[1]++;
                        try{
                            if(v.WaitingSince==0)v.WaitingSince=VehicleMovementTime;
                            // SPEC: held at a junction by a vehicle stopped there (out of fuel), it
                            // turns back after the same minutes as behind it in the lane.
                            if(v.TowPhase==TowPhase.ToVehicle&&(TryTowContraflow(v)||TryTowFromBehind(v)))continue;
                            if(VehicleMovementTime-v.WaitingSince>=State.Tuning.UTurnWaitSeconds&&JunctionHeldByStranded(v,node) is {} stranded&&TryUTurn(v,stranded))continue;
                            if(IsTow(v)&&TowHeldTooLong(v,null)&&JunctionHeldByStranded(v,node) is {} holder&&TryUTurn(v,holder,[..TowClosedRoads(v),node.Id])){CheckTowFuel(v);continue;}
                            TowWaitsWithoutAccess(v);break;
                        }finally{if(scanProfile is not null)_pr57JunctionAllocBytes[1]+=GC.GetAllocatedBytesForCurrentThread()-pr57DeniedStart;}
                    }
                }finally{
                    if(scanProfile is not null)_pr57RegionAllocBytes[6]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;
                }
                pr57RegionStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                Pr57ExclusiveMark(8);
                if(v.Fuel<=.000001){RunOutOfFuel(v);break;}
                if(pr57IdleProbe)Pr57BlockMarkStage(5);
                step=Math.Min(step,v.Fuel/FuelRate(v));v.Position=v.Position.MoveTowards(target,step);v.AddFuel(-(step*FuelRate(v)));v.FuelUnits=Math.Max(0,v.FuelUnits);v.AddFuelBurnedLastStep(step*FuelRate(v));remaining-=step/speed;v.WaitingSince=0;v.Cause="Em viagem";
                ClearPassedJunction(v);
                var advanced=!bayHold&&(d<=step+.001||stopEarly>0&&d-step<=stopEarly+.001);
                if(steer==null&&advanced)v.RouteIndex++;
                // Wear for the distance driven; a vehicle in use may break down here.
                if(scanProfile is not null)_pr57RegionAllocBytes[7]+=GC.GetAllocatedBytesForCurrentThread()-pr57RegionStart;
                if(DriveWear(v,step)||steer==null&&!advanced)break;
            }
            }finally{
                pr57DenseTrig554?.Refresh(v);
                Pr57Effect253AfterActor(v);
                Pr57ExclusiveFinishVehicle(v);
                Pr57S6GateShadowAfterVehicle(v);
                if(pr57CostProbe){
                    var pr57ElapsedTicks=System.Diagnostics.Stopwatch.GetTimestamp()-pr57CostStartTicks;
                    var pr57Allocated=GC.GetAllocatedBytesForCurrentThread()-pr57CostStartBytes;
                    var pr57Moved=v.Position.Distance(pr57CostOldPosition)>1e-7;
                    var pr57Cause=v.Cause;
                    // 0 operations; 1 nontravelling; 2 moved; 3 lane/bay queue;
                    // 4 crossing/junction; 5 other waiting; 6 stationary "in trip"; 7 other.
                    var pr57Group=dt<=0?0:v.Status!=VehicleStatus.Travelling?1:pr57Moved?2:
                        pr57Cause?.StartsWith("Fila",StringComparison.Ordinal)==true?3:
                        pr57Cause?.Contains("cruzamento",StringComparison.OrdinalIgnoreCase)==true||
                        pr57Cause?.Contains("curva",StringComparison.OrdinalIgnoreCase)==true||
                        pr57Cause?.Contains("semáforo",StringComparison.OrdinalIgnoreCase)==true?4:
                        pr57Cause?.StartsWith("Aguardando",StringComparison.Ordinal)==true?5:
                        pr57Cause=="Em viagem"?6:7;
                    _pr57VehicleCostTicks[pr57Group]+=pr57ElapsedTicks;
                    _pr57VehicleCostBytes[pr57Group]+=pr57Allocated;
                    _pr57VehicleCostCalls[pr57Group]++;
                    if(v.IntersectionClaim!=pr57CostOldClaim)_pr57VehicleCostClaimChanges[pr57Group]++;
                    if(v.ManoeuvreSpaces.Count!=pr57CostOldSpaces)_pr57VehicleCostSpaceCountChanges[pr57Group]++;
                }
                if(scanProfile is not null){
                    var allocNow=GC.GetAllocatedBytesForCurrentThread();
                    if(pr57VehicleAllocStage==0)_pr57MovementAllocBytes[7]+=allocNow-pr57VehicleAllocStart;
                    else if(pr57VehicleAllocStage==1)_pr57MovementAllocBytes[8]+=allocNow-pr57VehicleAllocStart;
                    else if(pr57VehicleAllocStage==2)_pr57MovementAllocBytes[9]+=allocNow-pr57VehicleAllocStart;
                }
                if(pr57IdleProbe){
                    Pr57IdleObserve(v,pr57IdleStart,pr57IdleLaneVisits,pr57IdleBendVisits);
                    Pr57BlockFinishVehicle(v,pr57IdleStart,dt,operationsDt);
                    Pr57DependencyBoundary(v);
                }
                var pr57FinalizeAllocStart=scanProfile is null?0L:GC.GetAllocatedBytesForCurrentThread();
                // If an initially parked car starts moving during this pass, it
                // becomes eligible for subsequent scans in original vehicle order.
                if(pr57ScanIds is not null&&(!v.PassengerCar||v.Status!=VehicleStatus.Parked)&&pr57ScanIds.Add(v.Id)){
                    var next=pr57ScanCandidates.FindIndex(o=>pr57VehicleOrder![o.Id]>pr57VehicleOrder[v.Id]);
                    if(next<0)pr57ScanCandidates.Add(v);else pr57ScanCandidates.Insert(next,v);
                }
                if(pr57Cells is not null||pr57BitsetCells is not null){
                    if(pr57SpatialIds!.Contains(v.Id)){
                        var newCell=Pr57Cell(v.Position);
                        if(newCell!=pr57OldCell){
                            Pr57Remove(v,pr57OldCell);
                            Pr57Add(v);
                        }
                    }else if(pr57ScanIds?.Contains(v.Id)==true){
                        pr57SpatialIds.Add(v.Id);Pr57Add(v);
                    }
                    pr57WorldExtent=Math.Max(pr57WorldExtent,Math.Max(Math.Abs(v.Position.X),Math.Abs(v.Position.Z)));
                }
                if(pr57AxisIndex is not null&&!_pr57AxisSweepDirty
                    &&pr57AxisIndex.Count==State.Vehicles.Count)pr57AxisIndex.Update(v,pr57OldPosition);
                if(pr57Bvh)_pr57VehicleBvh!.Update(v);
                if(Pr57MutationAuditEnabled&&scanProfile is not null)Pr57MutationAfterVehicle(v);
                Pr57ReservationAfterVehicle(v);
                Pr57PhaseExposureAfterVehicle(v);
                Pr57ActorShadowAfterActor(v);
                Pr57DockWriterAfterActor(v);
                Pr57Body258AfterActor(v);
                Pr57PhaseImpactAfterActor(v);
                if(scanProfile is not null)_pr57MovementAllocBytes[10]+=GC.GetAllocatedBytesForCurrentThread()-pr57FinalizeAllocStart;
            }
        }
        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36&&_pr57MovementAllocCalls<39)
            Console.WriteLine($"PR57_236_POSE_NOOP sample={_pr57MovementAllocCalls-35} eligible={pr57PoseNoopEligible} skipped={pr57PoseNoopSkipped} evaluated={pr57PoseNoopEligible-pr57PoseNoopSkipped} scope=BITWISE_HEADING_NOOP");
        // A towed vehicle moves only with its tow (platform or trailing a heavy tow).
        SyncTowedVehicles();
        Pr57Transitions290End(dt<=0?0:1);
        Pr57Effect253EndSlice();
        Pr57260EndSlice();
        Pr57ReservationAfterSync();
        Pr57CrossSliceEnd();
        Pr57CandidateEndSlice();
        Pr57S6GateShadowEndSlice();
        Pr57S3CounterEndSlice();
        Pr57ActorShadowEndSlice();
        Pr57DockWriterEndSlice();
        Pr57Body258EndSlice();
        Pr57Crossing259EndSlice();
        Pr57PhaseImpactEndSlice();
        Pr57PhaseExposureEndSlice();
        Pr57ExclusiveEndSlice();
        Pr57PairEndSlice();
        Pr57StageEndSlice();
        Pr57CausalEndSlice();
        Pr57JunctionCensusEndSlice();
        Pr57JunctionReadersEndSlice();
        Pr57Man248EndSlice();
        Pr57Stage249EndSlice();
        if(Pr57MutationAuditEnabled&&scanProfile is not null)Pr57MutationAfterSync();
        if(Pr57LaneReadWriteEnabled&&scanProfile is not null)Pr57LaneReadEndSlice();
        if(pr57DepProbe)Pr57DepEndSlice();
        if(pr57IdleProbe)Pr57IdleProbeTick();
        _pr57SiteOwnerRoster=null; // no index may survive this movement slice
        if(pr57Bvh)_pr57VehicleBvh!.Refresh();
        if(scanProfile is not null){
            _pr57MovementAllocBytes[0]+=GC.GetAllocatedBytesForCurrentThread()-pr57AllocCallStart;
            if(++_pr57MovementAllocCalls%300==0){
                var b=_pr57MovementAllocBytes;
                var other=b[0]-b[1]-b[2]-b[3]-b[4]-b[5]-b[6]-b[7]-b[8]-b[9]-b[10];
                var region=_pr57RegionAllocBytes;
                Console.WriteLine($"PR57_ALLOC_REGIONS calls={_pr57MovementAllocCalls} routeMB={region[0]/1e6:F2} geometryMB={region[1]/1e6:F2} sharedMB={region[2]/1e6:F2} entryMB={region[3]/1e6:F2} poseMB={region[4]/1e6:F2} blockedMB={region[5]/1e6:F2} junctionMB={region[6]/1e6:F2} driveMB={region[7]/1e6:F2}");
                var blocked=_pr57BlockedAllocBytes;var counts=_pr57BlockedCalls;
                Console.WriteLine($"PR57_BLOCKED_ALLOC calls={_pr57MovementAllocCalls} causeMB={blocked[0]/1e6:F2} causeCalls={counts[0]} towApproachMB={blocked[1]/1e6:F2} towApproachCalls={counts[1]} bypassMB={blocked[2]/1e6:F2} bypassCalls={counts[2]} towExitMB={blocked[3]/1e6:F2} towExitCalls={counts[3]} towRerouteMB={blocked[4]/1e6:F2} towRerouteCalls={counts[4]} towWaitMB={blocked[5]/1e6:F2} towWaitCalls={counts[5]}");
                var junctionBytes=_pr57JunctionAllocBytes;var junctionCalls=_pr57JunctionCalls;
                Console.WriteLine($"PR57_JUNCTION_ALLOC calls={_pr57MovementAllocCalls} admissionMB={junctionBytes[0]/1e6:F2} admissionCalls={junctionCalls[0]} deniedMB={junctionBytes[1]/1e6:F2} deniedCalls={junctionCalls[1]} bayHeldCalls={junctionCalls[2]} fullRegionMB={region[6]/1e6:F2}");
                var detailBytes=_pr57JunctionDetailBytes;var detailCalls=_pr57JunctionDetailCalls;
                Console.WriteLine($"PR57_JUNCTION_DETAIL calls={_pr57MovementAllocCalls} manoeuvreMB={detailBytes[0]/1e6:F2} manoeuvreCalls={detailCalls[0]} areaOwnerMB={detailBytes[1]/1e6:F2} areaOwnerCalls={detailCalls[1]} bendHolderMB={detailBytes[2]/1e6:F2} bendHolderCalls={detailCalls[2]} bendPriorityMB={detailBytes[3]/1e6:F2} bendPriorityCalls={detailCalls[3]} preferenceMB={detailBytes[4]/1e6:F2} preferenceCalls={detailCalls[4]} turnWayMB={detailBytes[5]/1e6:F2} turnWayCalls={detailCalls[5]} bodyOnWayMB={detailBytes[6]/1e6:F2} bodyOnWayCalls={detailCalls[6]} otherMB={(junctionBytes[0]-detailBytes.Sum())/1e6:F2}");
                if(Pr57HotTiming){
                    double Ms(long ticks)=>ticks*1000.0/System.Diagnostics.Stopwatch.Frequency;
                    var hot=_pr57DirectMovementTicks;var calls=_pr57DirectMovementCalls;var detail=_pr57JunctionDetailTicks;
                    Console.WriteLine($"PR57_HOT_TIMING slices={_pr57MovementAllocCalls} siteMs={Ms(hot[0]):F3} siteCalls={calls[0]} crossingMs={Ms(hot[1]):F3} crossingCalls={calls[1]} turnMs={Ms(hot[2]):F3} turnCalls={calls[2]} junctionMs={Ms(hot[3]):F3} junctionCalls={calls[3]} ownerMs={Ms(detail[0]):F3} areaMs={Ms(detail[1]):F3} bendHolderMs={Ms(detail[2]):F3} bendPriorityMs={Ms(detail[3]):F3} preferenceMs={Ms(detail[4]):F3} turnWayMs={Ms(detail[5]):F3} bodyOnWayMs={Ms(detail[6]):F3}");
                }
                Console.WriteLine($"PR57_ALLOC_PROFILE calls={_pr57MovementAllocCalls} totalMB={b[0]/1e6:F2} setupMB={b[1]/1e6:F2} laneMB={b[2]/1e6:F2} bendMB={b[3]/1e6:F2} siteMB={b[4]/1e6:F2} crossingMB={b[5]/1e6:F2} turnMB={b[6]/1e6:F2} operationsMB={b[7]/1e6:F2} preLaneMB={b[8]/1e6:F2} postTurnMB={b[9]/1e6:F2} finalizeMB={b[10]/1e6:F2} otherMB={other/1e6:F2}");
                Pr57JunctionReasonReport(_pr57MovementAllocCalls);
                Pr57Man248Report(_pr57MovementAllocCalls);
            }
            if(Pr57QueueLimiterEnabled&&_pr57MovementAllocCalls%100==0)Pr57QueueLimiterPrint(_pr57MovementAllocCalls);
            if(pr57CostProbe&&_pr57MovementAllocCalls%60==0){
                string[] names=["operations","nontravelling","moved","queue","junction","waiting","stationary-trip","other"];
                var totalTicks=_pr57VehicleCostTicks.Sum();
                for(var i=0;i<names.Length;i++){
                    var ticks=_pr57VehicleCostTicks[i];
                    Console.WriteLine($"PR57_VEHICLE_COST slices={_pr57MovementAllocCalls} group={names[i]} calls={_pr57VehicleCostCalls[i]} ms={ticks*1000.0/System.Diagnostics.Stopwatch.Frequency:F3} sharePct={(totalTicks==0?0:100.0*ticks/totalTicks):F2} allocMB={_pr57VehicleCostBytes[i]/1e6:F3} claimChanges={_pr57VehicleCostClaimChanges[i]} spaceCountChanges={_pr57VehicleCostSpaceCountChanges[i]}");
                }
            }
        }
    }
    void ReplanVehicle(Vehicle v,Shipment s)
    {
        var start=NearestRoad(v.Position);if(start==null||start.Position.Distance(v.Position)>State.Tuning.RoadWidth/2+3)return;
        // A demolished origin/destination keeps the truck waiting with its cargo and titular preserved.
        var endpoint=BuildingById(!s.PickedUp?s.SourceBuildingId:s.DestinationBuildingId);
        if(AwaitsRemovedDestination(s)){if(v.HoldingPosition==null)HandleRemovedCargoDestination(v,s);return;}
        if(s.ProjectId==null&&endpoint==null){v.Cause="Destino indisponível: carga preservada";return;}
        var target=s.ProjectId is {} pId&&s.PickedUp?WorkAccess(State.Projects.First(p=>p.Id==pId)):endpoint==null?null:Access(endpoint);
        if(target==null)return;
        // SPEC transporte-circulacao: a replanned departure also needs fuel for the trip.
        if(!EnsureFuel(v,target.Id))return;v.Status=VehicleStatus.Travelling;
    }
    bool CrossesLane(Point from,Point to,RoadNode n)
    {
        var a=from-n.Position;var b=to-n.Position;
        var horizontal=n.Neighbours.Select(id=>_roads[id].Position-n.Position).Where(p=>Math.Abs(p.X)>.1).Select(p=>p.X).Append(0).ToArray();
        var vertical=n.Neighbours.Select(id=>_roads[id].Position-n.Position).Where(p=>Math.Abs(p.Z)>.1).Select(p=>p.Z).Append(0).ToArray();
        // A corner's incoming carriageway ends at the bend. Its infinite axis
        // must not be mistaken for road beyond that end while walking the curb.
        bool Cross(double first,double second,double alongFirst,double alongSecond,double[] extent)=>extent.Length>1&&Math.Min(alongFirst,alongSecond)<=extent.Max()+3.3&&Math.Max(alongFirst,alongSecond)>=extent.Min()-3.3&&((Math.Abs(first)<3.3||Math.Abs(second)<3.3)||Math.Sign(first)!=Math.Sign(second))&&Math.Abs(first-second)>.05;
        return Cross(a.Z,b.Z,a.X,b.X,horizontal)||Cross(a.X,b.X,a.Z,b.Z,vertical);
    }
    bool JunctionPreferenceAllows(Vehicle v,RoadNode junction)
    {
        var horizontal=Math.Abs(Math.Sin(v.Heading))>.7;
        if(junction.Control==JunctionControl.Signal&&((VehicleMovementTime%60<30)!=horizontal))return Pr57JunctionPreferenceDenied(0);
        var competitors=State.Vehicles.Where(o=>o.Id!=v.Id&&o.Status==VehicleStatus.Travelling&&o.RouteIndex<o.Route.Count&&o.Route[o.RouteIndex]==junction.Id&&o.Position.Distance(junction.Position)<14&&CanTurnNow(o)).ToList();
        Pr57JunctionReadersPreferenceCandidates(v,competitors);
        if(junction.Control==JunctionControl.ThroughX&&!horizontal&&competitors.Any(o=>Math.Abs(Math.Sin(o.Heading))>.7))return Pr57JunctionPreferenceDenied(1);
        if(junction.Control==JunctionControl.ThroughZ&&horizontal&&competitors.Any(o=>Math.Abs(Math.Cos(o.Heading))>.7))return Pr57JunctionPreferenceDenied(2);
        if(junction.Control==JunctionControl.RightHand){
            bool YieldsRight(Vehicle a,IEnumerable<Vehicle> others){var right=new Point(-Math.Cos(a.Heading),Math.Sin(a.Heading));return others.Any(o=>o.Id!=a.Id&&(o.Position-junction.Position) is var p&&p.X*right.X+p.Z*right.Z>2);}
            if(YieldsRight(v,competitors)){
                // SPEC: when everyone approaching yields to someone on the right,
                // the one waiting longest crosses (fixed ID tie-break), then the
                // right-hand rule applies again.
                var waiting=competitors.Append(v).ToList();
                if(!waiting.All(a=>YieldsRight(a,waiting)))return Pr57JunctionPreferenceDenied(3);
                double Since(Vehicle a)=>a.WaitingSince>0?a.WaitingSince:double.MaxValue;
                if(waiting.OrderBy(Since).ThenBy(a=>a.Id).First().Id!=v.Id)return Pr57JunctionPreferenceDenied(4);
            }
        }
        return true;
    }
    // The body v queues behind still holds the junction or bend v is about to
    // take (or one of its adjacent area): the wait is for that crossing.
    bool HoldsJunctionAhead(Vehicle body,RoadNode node)=>SharedNode(node)&&(Holds(body,node.Id)||
        _junctionAreas.GetValueOrDefault(node.Id) is {} area&&(body.IntersectionClaim is {} claim&&area.Contains(claim)||body.ManoeuvreSpaces.Any(area.Contains)));
    bool MayEnterJunction(Vehicle v,RoadNode junction)
    {
        var pr57Detail=CostProfile is not null;long pr57DetailStart=0;long pr57DetailTickStart=0;
        v.Cause="Aguardando preferência/semáforo/travessia";var area=_junctionAreas.GetValueOrDefault(junction.Id);
        // A space of an admitted manoeuvre (Mobility.SharedSpace): its holder goes
        // on, everyone else waits for it. Elsewhere in the junction area, only a
        // holder close to this crossing reserves it, as for claims (NearbyAreaOwner).
        if(v.ManoeuvreSpaces.Contains(junction.Id)){ReserveJunction(v,junction,area);return Pr57JunctionDecision(v,junction,Pr57JunctionExit.ExistingManoeuvre,true);}
        pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var manoeuvre=State.Vehicles.FirstOrDefault(o=>Pr57JunctionReadersCandidate(v,o,0)&&o.Id!=v.Id&&o.Id!=v.TowTargetId&&o.TowTruckId!=v.Id&&!Receives(o,v)&&Pr57JunctionReadersGate(v,o,0)&&(o.ManoeuvreSpaces.Contains(junction.Id)||area!=null&&o.ManoeuvreSpaces.Any(area.Contains)&&Pr57JunctionReadersGate(v,o,1)&&o.Position.Distance(junction.Position)<9));
        if(pr57Detail){_pr57JunctionDetailBytes[0]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[0]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[0]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
        if(manoeuvre is not null){Pr57JunctionReadersBlock(v,manoeuvre,"manoeuvre");v.Cause=(IsBend(junction)?"Aguardando liberação da curva pelo veículo ":"Aguardando liberação do cruzamento pelo veículo ")+manoeuvre.Id;return Pr57JunctionDecision(v,junction,Pr57JunctionExit.ManoeuvreHolder,false);}
        var continuing=area!=null&&v.IntersectionClaim is {} held&&area.Contains(held);
        if(area!=null){
            pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var owner=NearbyAreaOwner(area,junction,v);
            if(pr57Detail){_pr57JunctionDetailBytes[1]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[1]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[1]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
            if(owner!=null&&owner.Id!=v.Id&&owner.Id!=v.TowTargetId){Pr57JunctionReadersBlock(v,owner,"areaOwner");v.Cause="Aguardando liberação do cruzamento pelo veículo "+owner.Id;return Pr57JunctionDecision(v,junction,Pr57JunctionExit.AreaOwner,false);}
        }
        if(v.IntersectionClaim==junction.Id)return Pr57JunctionDecision(v,junction,Pr57JunctionExit.ExistingClaim,true);
        if(IsBend(junction)){
            // SPEC "Curva simples, um de cada vez": no direction has preference; the
            // first to arrive (waiting longest, fixed ID tie-break) turns. It takes
            // the bend together with every space it crosses up to the first place
            // its body can wait clear of them, with that way free of bodies.
            pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var holder=State.Vehicles.FirstOrDefault(o=>Pr57JunctionReadersCandidate(v,o,1)&&o.Id!=v.Id&&o.Id!=v.TowTargetId&&o.TowTruckId!=v.Id&&!Receives(o,v)&&Pr57JunctionReadersGate(v,o,2)&&Holds(o,junction.Id));
            if(pr57Detail){_pr57JunctionDetailBytes[2]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[2]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[2]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
            if(holder is not null){Pr57JunctionReadersBlock(v,holder,"bendHolder");v.Cause="Aguardando liberação da curva pelo veículo "+holder.Id;return Pr57JunctionDecision(v,junction,Pr57JunctionExit.BendHolder,false);}
            double Arrived(Vehicle a)=>a.WaitingSince>0?a.WaitingSince:VehicleMovementTime;
            pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var first=State.Vehicles.Where(o=>Pr57JunctionReadersCandidate(v,o,2)&&o.Id!=v.TowTargetId&&o.Status==VehicleStatus.Travelling&&!Leaving(o)&&o.RouteIndex<o.Route.Count&&o.Route[o.RouteIndex]==junction.Id&&Pr57JunctionReadersGate(v,o,3)&&TurnsAtBend(o,o.RouteIndex)&&Pr57JunctionReadersGate(v,o,4)&&o.Position.Distance(junction.Position)<11&&(o.Id==v.Id||CanTurnNow(o)))
                .Append(v).Distinct().OrderBy(Arrived).ThenBy(o=>o.Id).First();
            if(pr57Detail){_pr57JunctionDetailBytes[3]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[3]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[3]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
            if(first.Id!=v.Id){Pr57JunctionReadersBlock(v,first,"bendPriority");v.Cause="Aguardando liberação da curva pelo veículo "+first.Id;return Pr57JunctionDecision(v,junction,Pr57JunctionExit.BendPriority,false);}
            pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var way=TurnWay(v,out var spaces);
            if(pr57Detail){_pr57JunctionDetailBytes[5]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[5]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[5]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
            foreach(var space in spaces.Where(s=>s.Id!=junction.Id)){var index=way.FindIndex(p=>EntersArea(v,p.Position,p.Heading,space));var from=index>0?way[index-1].Position:v.Position;if(!SpaceFree(v,space,space.Position-from,false))return Pr57JunctionDecision(v,junction,Pr57JunctionExit.BendSpace,false);}
            pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var bendBody=BodyOnWay(v,way);
            if(pr57Detail){_pr57JunctionDetailBytes[6]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[6]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[6]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
            if(bendBody is not null){Pr57JunctionReadersBlock(v,bendBody,"bendBody");v.Cause="Fila na faixa";return Pr57JunctionDecision(v,junction,Pr57JunctionExit.BendBody,false);}
            ReserveJunction(v,junction,null);RememberLaneManoeuvre(v,spaces);if(!v.ManoeuvreSpaces.Contains(junction.Id)){v.ManoeuvreSpaces.Add(junction.Id);Pr57ReservationWrite(v,"bend-add");}return Pr57JunctionDecision(v,junction,Pr57JunctionExit.BendAllowed,true);
        }
        if(area==null&&junction.Neighbours.Count>=3&&State.Vehicles.FirstOrDefault(o=>Pr57JunctionReadersCandidate(v,o,3)&&o.Id!=v.Id&&o.Id!=v.TowTargetId&&Pr57JunctionReadersGate(v,o,5)&&o.IntersectionClaim==junction.Id&&Pr57JunctionReadersGate(v,o,6)&&o.Position.Distance(junction.Position)<9) is {} occupant){Pr57JunctionReadersBlock(v,occupant,"occupant");v.Cause="Aguardando liberação do cruzamento pelo veículo "+occupant.Id;return Pr57JunctionDecision(v,junction,Pr57JunctionExit.Occupant,false);}
        // Only a walker physically at this crossing holds traffic back.
        if(Pr57Crossing259Check(junction))return Pr57JunctionDecision(v,junction,Pr57JunctionExit.Pedestrian,false);
        // A straight loading access/crosswalk is not a road intersection.
        // Applying right-hand priority there can make the road vehicle yield
        // forever to a truck that is itself waiting for the road to clear.
        if(junction.Neighbours.Count<3)return Pr57JunctionDecision(v,junction,Pr57JunctionExit.NonIntersection,true);
        var horizontal=Math.Abs(Math.Sin(v.Heading))>.7;
        Pr57JunctionCensusSignalReached(v,junction);
        Pr57JunctionReadersSignal(v,junction);
        if(junction.Control==JunctionControl.Signal&&((VehicleMovementTime%60<30)!=horizontal))return Pr57JunctionDecision(v,junction,Pr57JunctionExit.Signal,false);
        // A vehicle already admitted to shared pavement must be able to leave
        // it before another occupant/approach takes the same physical space.
        if(continuing){ReserveJunction(v,junction,area);return Pr57JunctionDecision(v,junction,Pr57JunctionExit.Continuing,true);}
        pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var preferenceAllows=JunctionPreferenceAllows(v,junction);
        if(pr57Detail){_pr57JunctionDetailBytes[4]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[4]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[4]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
        if(!preferenceAllows)return Pr57JunctionDecision(v,junction,Pr57JunctionExit.Preference,false);
        // When its way out of the junction runs straight into another junction or
        // bend, with no place between where its body could wait clear of both, it
        // takes them together (ARCHITECTURE "conjunto até a espera segura").
        pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var onward=TurnWay(v,out var crossed);
        if(pr57Detail){_pr57JunctionDetailBytes[5]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[5]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[5]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
        // Even a single shared space needs a clear way through it. Taking the
        // junction while a docked body blocks the exit prevents that body from
        // acquiring the very junction it needs to leave.
        pr57DetailStart=pr57Detail?GC.GetAllocatedBytesForCurrentThread():0L;pr57DetailTickStart=pr57Detail&&Pr57HotTiming?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var onwardBody=BodyOnWay(v,onward,Pr57305Enabled);
        if(pr57Detail){_pr57JunctionDetailBytes[6]+=GC.GetAllocatedBytesForCurrentThread()-pr57DetailStart;_pr57JunctionDetailCalls[6]++;if(pr57DetailTickStart!=0)_pr57JunctionDetailTicks[6]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57DetailTickStart;}
        if(onwardBody is not null){Pr57JunctionReadersBlock(v,onwardBody,"onwardBody");v.Cause="Fila na faixa";return Pr57JunctionDecision(v,junction,Pr57JunctionExit.OnwardBody,false);}
        if(crossed.Any(s=>s.Id!=junction.Id)){
            foreach(var space in crossed.Where(s=>s.Id!=junction.Id)){var index=onward.FindIndex(p=>EntersArea(v,p.Position,p.Heading,space));var from=index>0?onward[index-1].Position:v.Position;if(!SpaceFree(v,space,space.Position-from,false))return Pr57JunctionDecision(v,junction,Pr57JunctionExit.OnwardSpace,false);}
        }
        // A claim obtained at the stop line is not released on approach merely
        // because the body has not yet entered its square. Keep its local plan.
        RememberLaneManoeuvre(v,crossed);if(!v.ManoeuvreSpaces.Contains(junction.Id)){v.ManoeuvreSpaces.Add(junction.Id);Pr57ReservationWrite(v,"crossing-add");}
        ReserveJunction(v,junction,area);return Pr57JunctionDecision(v,junction,Pr57JunctionExit.Allowed,true);
    }
    void VehicleArrived(Vehicle v)
    {
        if(PavingMachineArrived(v))return;
        if(v.RefuellingAt is {} stationId&&BuildingById(stationId) is {} station&&Access(station) is {} stationAccess&&v.Position.Distance(DockAt(stationAccess,station,false))<3){Refuel(v,stationId);return;}
        var s=v.ShipmentId==null?null:State.Shipments.FirstOrDefault(s=>s.Id==v.ShipmentId);
        if(s==null){if(v.External&&v.Position.Distance(State.Entry)<8){v.Position=State.Entry;v.Status=VehicleStatus.Outside;v.CrewAboard=false;v.CrewCount=0;v.ConstructionAccess=false;v.Route.Clear();v.IntersectionClaim=null;v.Cause="Disponível no exterior";}else {v.Status=VehicleStatus.Parked;ReleaseDriver(v);if(IsTow(v)&&BuildingById(v.HomeBuildingId) is {Kind:BuildingKind.MechanicShop} shop&&v.Position.Distance(ShopTowSpace(shop))<.5){v.Heading=TowBaseHeading(shop);v.Cause="Guincho da oficina disponível";}}return;}
        if(s.Complete&&v.CrewAboard){v.Status=VehicleStatus.Parked;v.Cause="Equipe externa no canteiro";return;}
        if(s.Cancelled){RedirectCancelled(v,s);return;}
        if(!s.PickedUp&&s.SourceBuildingId is {} sourceId){var source=BuildingById(sourceId);if(source==null||s.Cargo.Any(kv=>source.Quantity(kv.Key)<kv.Value)){v.Status=VehicleStatus.Waiting;v.Cause="Carga reservada indisponível na origem";return;}foreach(var kv in s.Cargo.Units)source.AddStockUnits(kv.Key,-kv.Value);if(source.Kind==BuildingKind.Farm)source.FoodSoldTodayUnits=checked(source.FoodSoldTodayUnits+s.Cargo.Units.GetValueOrDefault(ResourceKind.Food));v.Cargo=new(s.Cargo);s.PickedUp=true;
            if(s.ProjectId is {} pi&&State.Projects.FirstOrDefault(p=>p.Id==pi) is {} project)foreach(var kv in s.Cargo)project.LocalReserved.Units[kv.Key]=Math.Max(0,checked(project.LocalReserved.Units.GetValueOrDefault(kv.Key)-s.Cargo.Units[kv.Key]));
            var target=s.ProjectId is {} pId?WorkAccess(State.Projects.First(p=>p.Id==pId)):Access(BuildingById(s.DestinationBuildingId)!);var start=NearestRoad(v.Position);if(start==null||target==null){v.Status=VehicleStatus.Waiting;v.Cause="Destino sem via";return;}if(!EnsureFuel(v,target.Id)){v.Status=VehicleStatus.Waiting;return;}if(v.RefuellingAt==null)v.DockPosition=DockAt(target,BuildingById(s.DestinationBuildingId),v.CrewAboard);v.Status=VehicleStatus.Travelling;if(v.RefuellingAt==null&&s.ReceivingMachineId!=null)ConfigureAsphaltApproach(v,s);return;
        }
        if(s.ReceivingMachineId is {} machineId){var receiver=VehicleById(machineId);if(receiver is not {MachinePhase:MachinePhase.OnSite,FeedingReady:true,CrewAboard:true}||receiver.CrewCount<1||receiver.Fuel<=0||receiver.MachineProjectId!=s.ProjectId||receiver.Position.Distance(v.Position)>7||Math.Cos(receiver.Heading-v.Heading)<.95){v.LoadTimer=2;v.Status=VehicleStatus.Unloading;v.Cause="Aguardando pavimentadora real para descarregar";return;}}
        if(s.ProjectId is {} id&&s.Purpose is ShipmentPurpose.Construction or ShipmentPurpose.Maintenance){var p=State.Projects.First(p=>p.Id==id);if(p.Status==WorkStatus.Cancelled){s.Cancelled=true;RedirectCancelled(v,s);return;}if(RetireMaintenanceWithoutTargets(p))return;foreach(var kv in v.Cargo)p.Delivered.AddUnits(kv.Key,v.Cargo.Units[kv.Key]);v.Cargo.Clear();s.Complete=true;
            if(v.CrewAboard){v.Status=VehicleStatus.Parked;v.IntersectionClaim=null;v.Cause="Equipe externa no canteiro";}else ReturnExternal(v);return;
        }
        if(s.Purpose==ShipmentPurpose.Vehicle){s.Complete=true;v.External=false;v.ExternalDriverId=null;v.Status=VehicleStatus.Parked;v.ShipmentId=null;v.Cause="Caminhão adquirido; aguarda motorista SIM";
            // The shop's tow is set at its own space (it may arrive at the street side after a detour).
            if(IsTow(v)&&BuildingById(v.HomeBuildingId) is {Kind:BuildingKind.MechanicShop} shop){v.Cause="Guincho da oficina disponível";if(v.Position.Distance(ShopTowSpace(shop))>.5)v.PushPath=[ShopTowSpace(shop)];v.PushHeading=TowBaseHeading(shop);if(v.PushPath.Count==0)v.Heading=TowBaseHeading(shop);}
            return;}
        if(AwaitsRemovedDestination(s)){HandleRemovedCargoDestination(v,s);return;}
        var dest=BuildingById(s.DestinationBuildingId);if(dest==null){v.Status=VehicleStatus.Waiting;v.Cause="Destino indisponível: carga preservada";return;}
        var atBay=State.Vehicles.Any(o=>o.Id<v.Id&&o.Status==VehicleStatus.Unloading&&o.ShipmentId is {} other&&State.Shipments.Any(x=>x.Id==other&&x.DestinationBuildingId==dest.Id));
        if(atBay){v.Status=VehicleStatus.Unloading;v.LoadTimer=10;v.Cause="Fila física de descarga";return;}
        var space=Math.Max(0,dest.Definition.Capacity-dest.Stock.Values.Sum());var quantity=v.Cargo.Values.Sum();if(quantity>space+.001){v.Status=VehicleStatus.Unloading;v.LoadTimer=30;v.Cause="Estoque de destino sem capacidade";return;}
        foreach(var kv in v.Cargo.Units)dest.AddStockUnits(kv.Key,kv.Value);v.Cargo.Clear();s.Complete=true;
        if(v.External)ReturnExternal(v);else ReturnLocal(v);
    }
    void ReturnLocal(Vehicle v)
    {
        var source=BuildingById(v.HomeBuildingId);var start=NearestRoad(v.Position);var target=source==null?null:Access(source);if(start==null||target==null){v.Status=VehicleStatus.Waiting;v.Cause="Sem acesso para retorno";return;}v.ShipmentId=null;v.DestinationBuildingId=source!.Id;
        // SPEC transporte-circulacao: the way back is a departure too; it refuels
        // at a station first when the tank does not cover it, or waits at the bay.
        if(!EnsureFuel(v,target.Id)){if(FindRoadRoute(start.Id,target.Id).Count==0)v.Cause="Sem rota para retorno";v.Route.Clear();v.RouteIndex=0;v.Status=VehicleStatus.Waiting;return;}
        if(v.RefuellingAt==null)v.DockPosition=DockAt(target,source,false);v.Status=VehicleStatus.Travelling;
    }
    void ReleaseDriver(Vehicle v){if(v.DriverId is {} id&&CitizenById(id) is {} c){Pr57CitizenAgendaWake(c);c.VehicleId=null;c.Position=v.Position;var workplace=BuildingById(c.WorkplaceId);if(workplace!=null)StartJourney(c,workplace,JourneyPurpose.Work);}v.DriverId=null;}
    void RedirectCancelled(Vehicle v,Shipment s)
    {
        PrepareConstructionSiteDeparture(v);
        if(!s.PickedUp){s.Complete=true;v.Cargo.Clear();v.CrewAboard=false;ReturnExternal(v);return;}
        // A crew-only trip has no paid cargo to store or redirect. Keep the real
        // crew aboard its truck and send it home by the remaining road network.
        if(v.Cargo.Values.All(quantity=>quantity==0)){s.Complete=true;v.HoldingPosition=null;ReturnExternal(v);return;}
        // Works and the Pátio only take municipal material; private cargo is never appropriated.
        if(PrivateCargo(s)){s.Cancelled=false;if(AwaitsRemovedDestination(s))HandleRemovedCargoDestination(v,s);else {v.Status=VehicleStatus.Waiting;v.Cause="Carga privada de "+ReadableOwnerName(s.GoodsOwnerAccount)+" preservada; não vai ao Pátio municipal";}return;}
        if(TryRedirectMunicipalCargo(v,s))return;
        var depot=State.Buildings.FirstOrDefault(b=>b.Kind==BuildingKind.WorksDepot&&b.Completed&&!b.Removed&&Access(b)!=null&&b.Stock.Values.Sum()+v.Cargo.Values.Sum()<=b.Definition.Capacity);
        var start=NearestRoad(v.Position);var target=depot==null?null:Access(depot);var route=start==null||target==null?new List<int>():FindRoadRoute(start.Id,target.Id);
        if(depot==null||route.Count==0){ParkCancelledCargo(v);return;}
        v.HoldingPosition=null;s.Cancelled=false;s.ProjectId=null;Pr57SharedShipmentProjectChanged();s.Purpose=ShipmentPurpose.Maintenance;s.DestinationBuildingId=depot.Id;s.Destination=depot.Door;v.CrewAboard=false;v.CrewCount=0;v.Route=route;v.RouteIndex=0;v.DockPosition=DockAt(target!,depot,false);v.Status=VehicleStatus.Travelling;v.Cause="Redirecionando carga municipal ao Pátio";
    }
    // Roadside spot beside the highway, outside the lanes and away from buildings and
    // spots already reserved by other waiting trucks.
    (RoadNode Road,Point Position)? HoldingSlot(Vehicle vehicle)
    {
        var candidates=State.Roads.Where(r=>r.Highway).OrderBy(r=>r.Position.Distance(State.Entry)).Where((road,index)=>index>=2&&index%2==0).Select(road=>(Road:road,Position:road.Position+new Point(0,-State.Tuning.RoadWidth/2-1.5)));
        var slot=candidates.FirstOrDefault(candidate=>State.Buildings.All(b=>b.Removed||!Inside(b,candidate.Position,4))&&State.Vehicles.All(other=>other.Id==vehicle.Id||other.HoldingPosition is not {} reserved||reserved.Distance(candidate.Position)>=8));
        return slot.Road==null?null:slot;
    }
    void ParkCancelledCargo(Vehicle vehicle)
    {
        var slot=HoldingSlot(vehicle)??default;
        var start=NearestRoad(vehicle.Position);if(slot.Road==null||start==null){vehicle.Status=VehicleStatus.Waiting;vehicle.Cause="Carga municipal aguardando estoque; não há espaço físico de espera";return;}
        vehicle.HoldingPosition=slot.Position;if(vehicle.Position.Distance(slot.Position)<.1){vehicle.Status=VehicleStatus.Waiting;vehicle.Cause="Carga municipal estacionada fora das faixas; aguarda estoque ou obra compatível";return;}
        var route=FindRoadRoute(start.Id,slot.Road.Id);if(route.Count==0){vehicle.Status=VehicleStatus.Waiting;vehicle.Cause="Sem rota física para estacionar carga preservada";return;}
        vehicle.Route=route;vehicle.RouteIndex=0;vehicle.DockPosition=slot.Position;vehicle.Status=VehicleStatus.Travelling;vehicle.Cause="Transportando carga cancelada para espaço real de espera";
    }
    bool TryRedirectMunicipalCargo(Vehicle vehicle,Shipment shipment)
    {
        if(shipment.GoodsOwnerAccount!="city"||vehicle.Cargo.Count==0)return false;var start=NearestRoad(vehicle.Position);if(start==null||start.Position.Distance(vehicle.Position)>State.Tuning.RoadWidth/2+3)return false;
        foreach(var project in State.Projects.Where(p=>p.Id!=shipment.ProjectId&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress).OrderBy(p=>p.Id)){
            var compatible=vehicle.Cargo.All(k=>k.Value<=project.Required.GetValueOrDefault(k.Key)-project.Delivered.GetValueOrDefault(k.Key)-project.LocalReserved.GetValueOrDefault(k.Key)-State.Shipments.Where(s=>s.Id!=shipment.Id&&s.ProjectId==project.Id&&!s.Complete).Sum(s=>s.Cargo.GetValueOrDefault(k.Key))+.001);if(!compatible)continue;
            var destination=WorkAccess(project);if(destination==null||!_workExteriorConnected.Contains(destination.Id))continue;var route=FindRoadRoute(start.Id,destination.Id,true);if(route.Count==0)continue;var returnTrip=FindRoadRoute(destination.Id,EntryNode().Id,true);if(returnTrip.Count==0||vehicle.Fuel<(RouteLength(route)+RouteLength(returnTrip)+20)*State.Tuning.FuelPerMetre)continue;
            var fee=State.Tuning.FreightBase+(decimal)RouteLength(route)*State.Tuning.FreightPerMetre;if(!ProjectPay(project,fee,project.IsMaintenance?MaintenanceCategory(project):"Transporte municipal","Redirecionamento físico de material municipal já pago"))continue;
            vehicle.HoldingPosition=null;shipment.OriginalProjectId??=shipment.ProjectId;shipment.ProjectId=project.Id;Pr57SharedShipmentProjectChanged();shipment.Cancelled=false;shipment.Purpose=project.IsMaintenance?ShipmentPurpose.Maintenance:ShipmentPurpose.Construction;shipment.DestinationBuildingId=project.BuildingId;shipment.Destination=destination.Position;shipment.PricePaid+=Money(fee);
            if(vehicle.CrewAboard&&project.CrewVehicleId==null&&project.LocalCrew.Count==0)project.CrewVehicleId=vehicle.Id;else {vehicle.CrewAboard=false;vehicle.CrewCount=0;}vehicle.Route=route;vehicle.RouteIndex=0;vehicle.DockPosition=DockAt(destination,BuildingById(project.BuildingId),vehicle.CrewAboard);vehicle.Status=VehicleStatus.Travelling;vehicle.Cause="Carga municipal redirecionada fisicamente à obra "+project.Id;return true;
        }return false;
    }
    Point DockAt(RoadNode road,Building? building,bool crew)
    {
        var normal=road.Highway||road.Neighbours.Any(id=>Math.Abs(_roads[id].Position.X-road.Position.X)>.1)?new Point(0,1):new Point(1,0);var delta=building==null?normal:building.Door-road.Position;var length=delta.Distance(new(0,0));var side=crew?normal*(delta.X*normal.X+delta.Z*normal.Z<0?-1:1):length<.01?normal:delta*(1/length);
        var dock=road.Position+side*(State.Tuning.RoadWidth/2+1.5)+(crew?new Point(side.Z,-side.X)*8.5:default);
        if(!crew)return dock;
        var along=new Point(side.Z,-side.X);var heading=Math.Atan2(along.X,along.Z);
        // Same curb and local access, nearest compatible position first. This
        // uses lot geometry, never SpotFree or the cars' choice of parking.
        for(var shift=0.0;shift<=State.Tuning.RoadWidth*2;shift+=.5)foreach(var sign in shift==0?new[]{1}:new[]{1,-1}){
            // Fit between the real carriageway and lot, rather than forcing the
            // old centre offset through the facade of a closely set-back house.
            // SPEC: the truck may occupy part of its own lane during the work.
            // Its centre stays at or beyond that carriageway's outer edge.
            for(var inset=0.0;State.Tuning.RoadWidth/2+1.5-inset>=CarriagewayHalfWidth;inset+=.25){
                var candidate=dock+along*(shift*sign)-side*inset;
                if(CrewDockClear(candidate,heading,road))return candidate;
            }
        }
        // No compatible location: keep the requested access as an intention.
        // BayEntryHold refuses its admission, so the body stays in a safe place.
        return dock;
    }

    // A machine ending its road route slides from the lane to its bay like a
    // truck, but without a bay admission: a body on that slide keeps it waiting.
    bool MachineDockSlideBlocked(Vehicle v,Point dock)
    {
        var slide=new List<(Point Position,double Heading)>();SamplePoses(slide,v.Position,dock,v.Heading);
        return BodyOnWay(v,slide.Select(p=>(v,p.Position,p.Heading)).ToList())!=null;
    }

    readonly Dictionary<(Point,double,int),bool> _crewDockGeometry=[];
    (int,int,int) _crewDockGeometryStamp;
    bool CrewDockClear(Point dock,double heading,RoadNode? access=null)
    {
        var stamp=WalkStamp;if(_crewDockGeometryStamp!=stamp){_crewDockGeometry.Clear();_crewDockGeometryStamp=stamp;}
        var key=(dock,heading,access?.Id??-1);if(!_crewDockGeometry.TryGetValue(key,out var clear))_crewDockGeometry[key]=clear=CrewDockGeometryClear(dock,heading,access);
        return clear;
    }
    bool CrewDockGeometryClear(Point dock,double heading,RoadNode? access)
    {
        var truck=new Vehicle();var car=new Vehicle{PassengerCar=true};
        if(!InMap(dock,BodyHalfLength(truck))||State.Roads.Any(n=>(SharedNode(n)||n.Crosswalk)&&EntersArea(truck,dock,heading,n)))return false;
        // SPEC construcao: part of its own lane only. Lengthwise along its own
        // street the centre stays at or beyond that carriageway's outer edge, so
        // the body never reaches the centre line; any other street, such as the
        // cross street of a T, keeps its whole carriageway clear.
        var lengthwiseX=Math.Abs(Math.Sin(heading))>.7;var ownCurb=State.Tuning.RoadWidth/2+1.5+.1;
        double Lateral(RoadNode n)=>Math.Abs(lengthwiseX?dock.Z-n.Position.Z:dock.X-n.Position.X);
        bool OwnStreet(RoadNode n)=>!n.Highway&&!SharedNode(n)&&!n.Crosswalk&&Lateral(n)<=ownCurb&&
            n.Neighbours.All(id=>lengthwiseX?Math.Abs(_roads[id].Position.Z-n.Position.Z)<.01:Math.Abs(_roads[id].Position.X-n.Position.X)<.01);
        if(State.Roads.Any(n=>OwnStreet(n)&&Lateral(n)<CarriagewayHalfWidth-.001)||BayBodyBlocksRoad(dock,lengthwiseX,BodyHalfLength(truck),BodyHalfWidth(truck),OwnStreet))return false;
        bool OffLots(Point at,double h)
        {
            var forward=new Point(Math.Sin(h),Math.Cos(h));var right=new Point(forward.Z,-forward.X);
            foreach(var b in State.Buildings.Where(b=>!b.Removed&&b.Position.Distance(at)<30)){
                var bf=new Point(Math.Sin(b.Rotation),Math.Cos(b.Rotation));var br=new Point(bf.Z,-bf.X);var delta=b.Position-at;
                bool Separated(Point axis){double Dot(Point a,Point c)=>a.X*c.X+a.Z*c.Z;return Math.Abs(Dot(delta,axis))>=BodyHalfLength(truck)*Math.Abs(Dot(forward,axis))+BodyHalfWidth(truck)*Math.Abs(Dot(right,axis))+b.Definition.Depth/2*Math.Abs(Dot(bf,axis))+b.Definition.Width/2*Math.Abs(Dot(br,axis))+.1;}
                if(!new[]{forward,right,bf,br}.Any(Separated))return false;
                if(LotParkingGeometry(b).Any(s=>BodiesOverlap(truck,at,h,car,s.Position,s.Heading)))return false;
            }
            return true;
        }
        if(!OffLots(dock,heading))return false;
        if(access==null)return true;
        // Leaving, the lane model turns the truck in place toward a lane point of
        // its access, either way along the street: that real turn must not sweep
        // a facade or a lot's parking space either.
        var axis=lengthwiseX?new Point(Math.Sign(access.Position.X-dock.X),0):new Point(0,Math.Sign(access.Position.Z-dock.Z));
        if(axis.Distance(default)<.5)return true;
        var facing=Math.Atan2(axis.X,axis.Z);var across=new Point(axis.Z,-axis.X)*1.65;
        foreach(var lane in new[]{access.Position+across,access.Position-across}){
            var turn=Math.IEEERemainder(Math.Atan2(lane.X-dock.X,lane.Z-dock.Z)-facing,2*Math.PI);
            for(var part=1;part<=8;part++)if(!OffLots(dock,facing+turn*part/8))return false;
        }
        return true;
    }
    // SPEC populacao: an emigrating family that cannot refuel enough leaves the car
    // parked at the station (off the pump) and walks back home, where it waits and
    // reconsiders; its home was kept until the car actually left the city.
    void ReturnEmigrantHome(Vehicle v)
    {
        Family? family=v.DriverId is int driverId&&CitizenById(driverId) is {} driver?FamilyById(driver.FamilyId):null;
        v.TripPurpose=JourneyPurpose.Home;v.RefuellingAt=null;v.SavedRoute=[];
        if(family!=null){family.Emigrating=false;v.DestinationBuildingId=family.HomeId;}
        // Back home by car when the tank allows it (a small margin covers the lot
        // manoeuvres), so the car is there for the next try.
        if(family!=null&&BuildingById(family.HomeId) is {} home&&Access(home) is {} access&&FreeSpot(home,family,v) is {} spot&&NearestRoad(v.Position,false) is {} start&&FindRoadRoute(start.Id,access.Id) is {Count:>0} route&&v.Fuel>=RouteLength(route)*FuelRate(v)+.3){
            v.ParkingSpot=spot.Key;v.DockPosition=spot.Position;v.Route=route;v.RouteIndex=0;v.Status=VehicleStatus.Travelling;v.Cause="Família voltando para casa: sem dinheiro ou combustível para emigrar";
            foreach(var id in v.Passengers.Append(v.DriverId!.Value)){var c=CitizenWithId(id);c.Purpose=JourneyPurpose.Home;c.DestinationId=home.Id;c.Activity="Voltando para casa de carro";}
            return;
        }
        // Not even enough to get home: the car stays parked at the station (off the pump).
        v.Status=VehicleStatus.Parked;LeaveCar(v,"Carro parado no posto: sem combustível nem para voltar; família volta a pé para casa");
    }
    void Refuel(Vehicle v,int stationId)
    {
        var leaving=v.PassengerCar&&v.TripPurpose==JourneyPurpose.Leaving;
        var station=BuildingById(stationId);if(station==null||station.OperationalCapacity<=0||station.CompanyId==null||station.Quantity(ResourceKind.Fuel)<=0){if(leaving){ReturnEmigrantHome(v);return;}v.LoadTimer=60;v.Status=VehicleStatus.Unloading;v.Cause="Posto sem operação/estoque";return;}
        var amount=Math.Min(State.Tuning.FuelTank-v.Fuel,Math.Min(station.Quantity(ResourceKind.Fuel),(double)(Balance(v.OwnerAccount)/station.SalePrice)));
        // An emigrating family never leaves with less than the exit needs.
        if(leaving&&v.Fuel+amount<RouteLength(v.SavedRoute)*FuelRate(v)+1){ReturnEmigrantHome(v);return;}
        if(amount<=.001){
            // A private car that cannot pay stays parked at the station, still
            // usable once the family has money; it is not stranded on the lane.
            if(v.PassengerCar){v.RefuellingAt=null;v.SavedRoute=[];v.Status=VehicleStatus.Parked;LeaveCar(v,"Carro parado no posto: sem dinheiro para abastecer");return;}
            v.RefuellingAt=null;v.SavedRoute=[];v.Route.Clear();v.RouteIndex=0;v.Status=VehicleStatus.Waiting;v.Cause="Sem dinheiro/combustível para abastecer";return;}
        var price=Money((decimal)amount*station.SalePrice);if(!Transfer(v.OwnerAccount,"company:"+station.CompanyId,price,"Combustível","Abastecimento físico no posto",v.Id))return;
        station.AddStock(ResourceKind.Fuel,-amount);v.AddFuel(amount);
        // Stock or money can run short after the stop was chosen: never leave without
        // the rest of the trip. A car stays parked at the station.
        if(!leaving&&v.SavedRoute.Count>0&&v.Fuel<RouteLength(v.SavedRoute)*FuelRate(v)+(v.PassengerCar?1:4)){
            if(v.PassengerCar){v.RefuellingAt=null;v.SavedRoute=[];v.Status=VehicleStatus.Parked;LeaveCar(v,"Carro parado no posto: combustível do posto ou dinheiro não bastam para a viagem");return;}
            // Off the pump bay: the truck waits and replans its refuelling (Waiting).
            v.RefuellingAt=null;v.SavedRoute=[];v.Route.Clear();v.RouteIndex=0;v.Status=VehicleStatus.Waiting;v.Cause="Abastecido em parte; combustível do posto ou dinheiro não bastam para a viagem";return;}
        v.RefuellingAt=null;v.Route=v.SavedRoute;v.SavedRoute=[];if(v.Route.Count>0){var shipment=State.Shipments.FirstOrDefault(s=>s.Id==v.ShipmentId);var destination=BuildingById(shipment?.PickedUp==false?shipment.SourceBuildingId:shipment?.DestinationBuildingId??v.HomeBuildingId);v.DockPosition=DockAt(_roads[v.Route[^1]],destination,false);}if(v.PassengerCar&&SpotByKey(v.ParkingSpot) is {} carSpot)v.DockPosition=carSpot.Position;if(v.PassengerCar&&v.TripPurpose==JourneyPurpose.Leaving)v.DockPosition=State.Entry;
        // A revision trip goes on to the shop's street side; the shop's tow to its own space.
        if(v.ServicePhase==ServicePhase.ToService)v.DockPosition=BuildingById(v.ServiceShopId) is {} serviceShop?ServiceDock(serviceShop):State.Entry;
        else if(IsTow(v)&&!v.External&&v.TowPhase==TowPhase.None&&BuildingById(v.HomeBuildingId) is {Kind:BuildingKind.MechanicShop} towShop)v.DockPosition=ShopTowSpace(towShop);
        v.RouteIndex=0;v.Status=VehicleStatus.Travelling;v.Cause="Abastecido com combustível real";
    }
    bool EnsureFuel(Vehicle v,int destination)
    {
        var start=NearestRoad(v.Position);if(start==null)return false;var normal=FindRoadRoute(start.Id,destination,ConstructionTrip(v));if(normal.Count==0)return false;
        var need=RouteLength(normal)*FuelRate(v)+(v.PassengerCar?1:4);if(v.Fuel>=need){v.Route=normal;v.RouteIndex=0;return true;}
        foreach(var station in State.Buildings.Where(b=>b.Kind==BuildingKind.FuelStation&&b.Completed&&b.OperationalCapacity>0&&b.Quantity(ResourceKind.Fuel)>0&&Access(b)!=null).OrderBy(b=>b.Position.Distance(v.Position))){var n=Access(station)!;var toStation=FindRoadRoute(start.Id,n.Id);if(toStation.Count==0||RouteLength(toStation)*FuelRate(v)+(v.PassengerCar?1:0)>v.Fuel)continue;var onward=FindRoadRoute(n.Id,destination,ConstructionTrip(v));if(onward.Count==0)continue;
            // SPEC transporte-circulacao: the stop must cover the rest of the trip,
            // with stock the station actually has and money the owner actually has.
            var atStation=v.Fuel-RouteLength(toStation)*FuelRate(v);var purchase=RouteLength(onward)*FuelRate(v)+(v.PassengerCar?1:4)-atStation;
            if(purchase>0&&(purchase>State.Tuning.FuelTank-atStation||station.Quantity(ResourceKind.Fuel)<purchase||Balance(v.OwnerAccount)<Money((decimal)purchase*station.SalePrice)))continue;
            v.SavedRoute=onward;v.RefuellingAt=station.Id;v.DockPosition=DockAt(n,station,false);v.Route=toStation;v.RouteIndex=0;return true;}
        v.Cause="Combustível insuficiente; não há posto viável no alcance";return false;
    }
}
