namespace IndexCities.Simulation;

// PR57 #596: Step-transient cross-phase worklist, never persisted. The
// infrastructure phase has already visited every completed building and
// evaluated exact worker presence. The immediately following business phase
// consumes the same ordered dependencies, not a cross-Advance snapshot.
public sealed partial class CitySimulation
{
    static readonly bool Pr57PhaseJournalEnabled =
        Environment.GetEnvironmentVariable("PR57_596_PHASE_WORKLIST")=="1";
    static readonly bool Pr57PhaseJournalShadow =
        Environment.GetEnvironmentVariable("PR57_596_SHADOW")=="1";
    readonly List<Building> _pr57PhaseWorklist=[];
    List<Building>? _pr57PhaseJournalBuildingSource;
    List<Citizen>? _pr57PhaseJournalCitizenSource;
    int _pr57PhaseJournalBuildingCount,_pr57PhaseJournalCitizenCount;
    bool _pr57PhaseJournalReady;
    long _pr57PhaseJournalSteps,_pr57PhaseJournalScanned,
        _pr57PhaseJournalSelected,_pr57PhaseJournalSkipped,
        _pr57PhaseJournalFallbacks;

    void Pr57PhaseJournalBegin()
    {
        if(!Pr57PhaseJournalEnabled)return;
        _pr57PhaseWorklist.Clear();
        _pr57PhaseJournalReady=false;
        _pr57PhaseJournalSteps++;
    }

    // This is inside the EXISTING infrastructure building pass; no additional
    // census of buildings is performed. Farms must still run without workers.
    // Non-farms with no present worker have no effect in the business loop;
    // the independent mechanic-shop phase still executes unconditionally.
    void Pr57PhaseJournalRecord(Building b,int present)
    {
        if(!Pr57PhaseJournalEnabled)return;
        _pr57PhaseJournalScanned++;
        if(b.Kind==BuildingKind.Farm||present>0)
        {
            _pr57PhaseWorklist.Add(b);
            _pr57PhaseJournalSelected++;
        }
        else _pr57PhaseJournalSkipped++;
    }

    void Pr57PhaseJournalPublish()
    {
        if(!Pr57PhaseJournalEnabled)return;
        _pr57PhaseJournalBuildingSource=State.Buildings;
        _pr57PhaseJournalCitizenSource=State.Citizens;
        _pr57PhaseJournalBuildingCount=State.Buildings.Count;
        _pr57PhaseJournalCitizenCount=State.Citizens.Count;
        _pr57PhaseJournalReady=true;
    }

    bool Pr57PhaseJournalCanUse()
    {
        if(!Pr57PhaseJournalEnabled)return false;
        if(!_pr57PhaseJournalReady||
           !ReferenceEquals(_pr57PhaseJournalBuildingSource,State.Buildings)||
           !ReferenceEquals(_pr57PhaseJournalCitizenSource,State.Citizens)||
           _pr57PhaseJournalBuildingCount!=State.Buildings.Count||
           _pr57PhaseJournalCitizenCount!=State.Citizens.Count)
        {
            _pr57PhaseJournalFallbacks++;
            _pr57StaffPrepared=false;
            return false;
        }
        if(Pr57PhaseJournalShadow)
        {
            // Intrusive full shadow only: compare actual live workers, including
            // negative dependencies, and exact original building/citizen order.
            var index=0;
            foreach(var b in State.Buildings.Where(b=>b.Completed&&!b.Removed))
            {
                var workers=State.Citizens.Where(c=>WorkerPresentAt(c,b)).ToArray();
                if(b.Kind==BuildingKind.Farm||workers.Length>0)
                {
                    if(index>=_pr57PhaseWorklist.Count||
                       !ReferenceEquals(b,_pr57PhaseWorklist[index++]))
                        throw new InvalidOperationException("PR57 #596 building worklist mismatch");
                }
                if(!_pr57PresentStaff.TryGetValue(b.Id,out var recorded))
                {
                    if(workers.Length!=0)
                        throw new InvalidOperationException("PR57 #596 missing workers");
                }
                else if(!workers.SequenceEqual(recorded))
                    throw new InvalidOperationException("PR57 #596 worker presence/order mismatch");
            }
            if(index!=_pr57PhaseWorklist.Count)
                throw new InvalidOperationException("PR57 #596 excess worklist entries");
        }
        return true;
    }

    void Pr57PhaseJournalEnd()
    {
        if(!Pr57PhaseJournalEnabled)return;
        _pr57PhaseJournalReady=false;
        _pr57PhaseJournalBuildingSource=null;
        _pr57PhaseJournalCitizenSource=null;
        if(_pr57PhaseJournalSteps%100==0)
            Console.WriteLine($"PR57_596_WORKLIST steps={_pr57PhaseJournalSteps} scanned={_pr57PhaseJournalScanned} selected={_pr57PhaseJournalSelected} skipped={_pr57PhaseJournalSkipped} fallbacks={_pr57PhaseJournalFallbacks}");
    }
}
