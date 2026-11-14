using System.Diagnostics;

namespace IndexCities.Simulation;

// PR57 #183: intrusive, read-only, sampled causal-candidate census.
// Exact workplace presence uses the engine's WorkerPresentAt predicate.
// Local construction crew and external crew snapshots are only candidates:
// they do NOT establish road access, a causal read set, or safe phase skipping.
public sealed class Pr57CrossPhasePresenceProbe
{
    readonly record struct LocalCandidate(bool Moving,int? VehicleId,Point Position);
    readonly record struct ExternalCandidate(int? VehicleId,VehicleStatus? Status,bool Aboard,Point? Position);
    readonly record struct ProjectResult(long ProgressUnits,string Cause,WorkStatus Status);
    readonly record struct BuildingResult(double WaterDemand,double ElectricityDemand,double OperationalCapacity,double WaterRatio,double ElectricityRatio);
    sealed class Snapshot
    {
        public Dictionary<int,HashSet<int>> Workers {get;}=new();
        public Dictionary<int,Dictionary<int,LocalCandidate>> Local {get;}=new();
        public Dictionary<int,ExternalCandidate> External {get;}=new();
        public Dictionary<int,ProjectResult> Projects {get;}=new();
        public Dictionary<int,BuildingResult> Buildings {get;}=new();
        public int RoadRevision,RoadCount;
        public long ClockUnits;
    }
    static readonly HashSet<int> EmptyWorkers=new();
    static readonly Dictionary<int,LocalCandidate> EmptyLocal=new();
    readonly int _every;
    long _steps,_ticks,_citizenVisits,_buildingVisits,_projectVisits;
    readonly long[] _memberChanges=new long[StepCostProfile.Names.Length];
    readonly long[] _siteChanges=new long[StepCostProfile.Names.Length];
    readonly long[] _localChanges=new long[StepCostProfile.Names.Length];
    readonly long[] _externalChanges=new long[StepCostProfile.Names.Length];
    readonly long[] _roadChanges=new long[StepCostProfile.Names.Length];
    readonly long[] _clockChanges=new long[StepCostProfile.Names.Length];
    readonly HashSet<int> _earlyWorksites=new(),_earlyProjects=new();
    Snapshot? _previous;
    long _phase3Projects,_phase3Overlap,_phase6Buildings,_phase6Overlap;
    public long Samples {get;private set;}
    public Pr57CrossPhasePresenceProbe(int every)
    {
        if(every<1)throw new ArgumentOutOfRangeException(nameof(every));
        _every=every;
    }
    Snapshot Capture(CitySimulation sim)
    {
        var state=sim.State;
        var result=new Snapshot{RoadRevision=state.RoadRevision,RoadCount=state.Roads.Count,ClockUnits=state.ElapsedSecondsUnits};
        var buildings=state.Buildings.Where(b=>b.Completed&&!b.Removed).ToDictionary(b=>b.Id);
        _buildingVisits+=buildings.Count;
        foreach(var b in buildings.Values)
        {
            result.Workers[b.Id]=new HashSet<int>();
            result.Buildings[b.Id]=new(b.WaterDemand,b.ElectricityDemand,b.OperationalCapacity,b.WaterRatio,b.ElectricityRatio);
        }
        foreach(var c in state.Citizens)
        {
            if(!c.Alive)continue;
            _citizenVisits++;
            if(c.WorkplaceId is int workplace&&buildings.TryGetValue(workplace,out var b)&&sim.WorkerPresentAt(c,b))
                result.Workers[workplace].Add(c.Id);
            if(c.ConstructionProjectId is int project)
            {
                if(!result.Local.TryGetValue(project,out var members))result.Local[project]=members=new();
                members[c.Id]=new(c.Moving,c.VehicleId,c.Position);
            }
        }
        var vehicles=state.Vehicles.ToDictionary(v=>v.Id);
        foreach(var p in state.Projects)
        {
            _projectVisits++;
            result.Projects[p.Id]=new(p.ProgressUnits,p.Cause,p.Status);
            if(p.Status is not (WorkStatus.Waiting or WorkStatus.InProgress))continue;
            var v=p.CrewVehicleId is int id&&vehicles.TryGetValue(id,out var found)?found:null;
            result.External[p.Id]=new(p.CrewVehicleId,v?.Status,v?.CrewAboard??false,v?.Position);
        }
        return result;
    }
    public bool Begin(CitySimulation sim)
    {
        var selected=_steps++%_every==0;
        if(!selected)return false;
        var start=Stopwatch.GetTimestamp();
        _earlyWorksites.Clear();_earlyProjects.Clear();
        _previous=Capture(sim);
        Samples++;
        _ticks+=Stopwatch.GetTimestamp()-start;
        return true;
    }
    static bool Diff<T>(Dictionary<int,T> before,Dictionary<int,T> after,int id)
    {
        var had=before.TryGetValue(id,out var old);
        var has=after.TryGetValue(id,out var current);
        return had!=has||(had&&!EqualityComparer<T>.Default.Equals(old!,current!));
    }
    public void Observe(CitySimulation sim,int phase)
    {
        if(_previous is null)throw new InvalidOperationException("PR57 #183 missing baseline");
        var start=Stopwatch.GetTimestamp();
        var before=_previous;
        var after=Capture(sim);
        foreach(var id in before.Workers.Keys.Union(after.Workers.Keys))
        {
            var old=before.Workers.GetValueOrDefault(id)??EmptyWorkers;
            var current=after.Workers.GetValueOrDefault(id)??EmptyWorkers;
            if(old.SetEquals(current))continue;
            _siteChanges[phase]++;
            _memberChanges[phase]+=old.Except(current).Count()+current.Except(old).Count();
            if(phase<=2)_earlyWorksites.Add(id);
        }
        foreach(var id in before.Local.Keys.Union(after.Local.Keys))
        {
            var old=before.Local.GetValueOrDefault(id)??EmptyLocal;
            var current=after.Local.GetValueOrDefault(id)??EmptyLocal;
            foreach(var citizenId in old.Keys.Union(current.Keys))
                if(Diff(old,current,citizenId))
                {
                    _localChanges[phase]++;
                    if(phase<=2)_earlyProjects.Add(id);
                }
        }
        foreach(var id in before.External.Keys.Union(after.External.Keys))
            if(Diff(before.External,after.External,id))
            {
                _externalChanges[phase]++;
                if(phase<=2)_earlyProjects.Add(id);
            }
        if(phase==3)
            foreach(var id in before.Projects.Keys.Union(after.Projects.Keys))
                if(Diff(before.Projects,after.Projects,id))
                {
                    _phase3Projects++;
                    if(_earlyProjects.Contains(id))_phase3Overlap++;
                }
        if(phase==6)
            foreach(var id in before.Buildings.Keys.Union(after.Buildings.Keys))
                if(Diff(before.Buildings,after.Buildings,id))
                {
                    _phase6Buildings++;
                    if(_earlyWorksites.Contains(id))_phase6Overlap++;
                }
        if(before.RoadRevision!=after.RoadRevision||before.RoadCount!=after.RoadCount)_roadChanges[phase]++;
        if(before.ClockUnits!=after.ClockUnits)_clockChanges[phase]++;
        _previous=phase==StepCostProfile.Names.Length-1?null:after;
        _ticks+=Stopwatch.GetTimestamp()-start;
    }
    public IEnumerable<string> ReportLines()
    {
        yield return $"PR57_183_PRESENCE_SUMMARY samples={Samples} intervalSteps={_every} observedSteps={_steps} " +
            $"probeMs={_ticks*1000.0/Stopwatch.Frequency:F3} citizenVisits={_citizenVisits} " +
            $"buildingVisits={_buildingVisits} projectVisits={_projectVisits} " +
            $"phase3ProjectChanges={_phase3Projects} phase3CandidateOverlap={_phase3Overlap} " +
            $"phase6BuildingChanges={_phase6Buildings} phase6WorksiteOverlap={_phase6Overlap} " +
            "scope=EXACT_WORKPLACE_MEMBERSHIP_AND_CANDIDATE_CREW_STATE_NOT_CAUSAL_READ_SETS";
        for(var i=0;i<StepCostProfile.Names.Length;i++)
            yield return $"PR57_183_PRESENCE_PHASE name={StepCostProfile.Names[i]} " +
                $"workerMemberChanges={_memberChanges[i]} workerSiteChanges={_siteChanges[i]} " +
                $"localCrewCandidateChanges={_localChanges[i]} externalCrewCandidateChanges={_externalChanges[i]} " +
                $"roadRevisionOrCountChanges={_roadChanges[i]} clockChanges={_clockChanges[i]}";
    }
}
