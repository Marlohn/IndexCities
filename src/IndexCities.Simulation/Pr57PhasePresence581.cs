namespace IndexCities.Simulation;

// PR57 #581: Step-local publication from the existing ordered MoveCitizens visit.
// No state, reference, or certificate survives an Advance(2) boundary.
// Only the workforce consumers immediately following construction/relocation
// can reuse it; every known intervening writer invalidates to canonical scans.
public sealed partial class CitySimulation
{
    static readonly bool Pr57PhasePresenceEnabled =
        Environment.GetEnvironmentVariable("PR57_PHASE_PRESENCE")=="1";
    static readonly bool Pr57PhasePresenceShadow =
        Environment.GetEnvironmentVariable("PR57_PHASE_PRESENCE_SHADOW")=="1";
    bool _pr57PhasePresenceReady,_pr57PhasePresenceUsed;
    long _pr57PhasePresenceCalls,_pr57PhasePresenceHits,_pr57PhasePresenceFallbacks;
    long _pr57PhasePresenceInvalidations,_pr57PhasePresenceCaptured;

    void Pr57PhasePresenceBegin()
    {
        _pr57PhasePresenceUsed=false;
        // Other experimental execution modes may skip or reorder the citizen
        // traversal. Their canonical paths remain untouched by this POC.
        _pr57PhasePresenceReady=Pr57PhasePresenceEnabled
            &&!Pr57CitizenAgendaEnabled&&!Pr57LivingRosterEnabled
            &&!Pr57StaffPhaseFusionEnabled;
        if(!_pr57PhasePresenceReady)return;
        foreach(var list in _staff.Values)list.Clear();
        foreach(var list in _pr57PresentStaff.Values)list.Clear();
        _pr57StaffPrepared=false;
    }

    void Pr57PhasePresenceCapture(Citizen citizen)
    {
        if(!_pr57PhasePresenceReady||!citizen.Alive)return;
        _pr57PhasePresenceCaptured++;
        if(citizen.WorkplaceId is not int site)return;
        if(!_staff.TryGetValue(site,out var staff))_staff[site]=staff=[];
        staff.Add(citizen);
        var building=BuildingById(site);
        if(building==null||!WorkerPresentAt(citizen,building))return;
        if(!_pr57PresentStaff.TryGetValue(site,out var present))
            _pr57PresentStaff[site]=present=[];
        present.Add(citizen);
    }

    void Pr57PhasePresenceEnd()
    {
        if(_pr57PhasePresenceReady)_pr57StaffPrepared=true;
    }

    void Pr57PhasePresenceInvalidate()
    {
        if(!_pr57PhasePresenceReady)return;
        _pr57PhasePresenceInvalidations++;
        _pr57PhasePresenceReady=false;
        _pr57StaffPrepared=false;
    }

    static bool Pr57SameCitizens(List<Citizen>? actual,List<Citizen>? expected)
    {
        if((actual?.Count??0)!=(expected?.Count??0))return false;
        if(expected==null)return true;
        for(var i=0;i<expected.Count;i++)
            if(!ReferenceEquals(actual![i],expected[i]))return false;
        return true;
    }

    // Expensive shadow runs separately from the unprofiled A/B.
    // Compare both membership and citizen order, not just aggregate counts.
    void Pr57PhasePresenceVerify(Dictionary<int,Building> active)
    {
        var staff=new Dictionary<int,List<Citizen>>();
        var present=new Dictionary<int,List<Citizen>>();
        foreach(var citizen in State.Citizens)
        {
            if(!citizen.Alive||citizen.WorkplaceId is not int site)continue;
            if(!staff.TryGetValue(site,out var workers))staff[site]=workers=[];
            workers.Add(citizen);
            if(!active.TryGetValue(site,out var building)||!WorkerPresentAt(citizen,building))continue;
            if(!present.TryGetValue(site,out var onSite))present[site]=onSite=[];
            onSite.Add(citizen);
        }
        foreach(var (site,workers) in staff)
            if(!_staff.TryGetValue(site,out var published)||!Pr57SameCitizens(published,workers))
                throw new InvalidOperationException("PR57_PHASE_PRESENCE staff shadow mismatch at "+site);
        foreach(var (site,workers) in _staff)
            if(workers.Count>0&&!staff.ContainsKey(site))
                throw new InvalidOperationException("PR57_PHASE_PRESENCE extra staff at "+site);
        foreach(var site in active.Keys)
        {
            _pr57PresentStaff.TryGetValue(site,out var published);
            present.TryGetValue(site,out var expected);
            if(!Pr57SameCitizens(published,expected))
                throw new InvalidOperationException("PR57_PHASE_PRESENCE presence shadow mismatch at "+site);
        }
    }

    bool Pr57PhasePresenceTryUse(Dictionary<int,Building> active,Dictionary<int,int> counts)
    {
        if(!Pr57PhasePresenceEnabled)return false;
        _pr57PhasePresenceCalls++;
        if(!_pr57PhasePresenceReady)
        {
            _pr57PhasePresenceFallbacks++;
            return false;
        }
        if(Pr57PhasePresenceShadow)Pr57PhasePresenceVerify(active);
        foreach(var (site,workers) in _pr57PresentStaff)
            if(counts.ContainsKey(site))counts[site]=workers.Count;
        _pr57PhasePresenceUsed=true;
        _pr57PhasePresenceHits++;
        if(Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1"
            &&(_pr57PhasePresenceCalls==1||_pr57PhasePresenceCalls%100==0))
            Console.WriteLine($"PR57_PHASE_PRESENCE used=True calls={_pr57PhasePresenceCalls} hits={_pr57PhasePresenceHits} fallbacks={_pr57PhasePresenceFallbacks} invalidations={_pr57PhasePresenceInvalidations} captured={_pr57PhasePresenceCaptured}");
        return true;
    }
}
