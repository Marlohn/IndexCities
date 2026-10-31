namespace IndexCities.Simulation;

// PR57 #548: a Step-local, writer-boundary workset shared by infrastructure,
// business operation and the later economy review. This is deliberately a
// reversible experiment, not a change to the CityState or public save format.
public sealed partial class CitySimulation
{
    static readonly bool Pr57PhaseBuildingWorksetEnabled=
        Environment.GetEnvironmentVariable("PR57_PHASE_BUILDING_WORKSET")=="1";
    static readonly bool Pr57PhaseBuildingWorksetVerify=
        Environment.GetEnvironmentVariable("PR57_PHASE_BUILDING_WORKSET_VERIFY")=="1";

    bool _pr57PhaseBuildingsReady;
    int _pr57PhaseBuildingsCount;

    void Pr57PhaseBuildingsBeginStep()=>_pr57PhaseBuildingsReady=false;
    void Pr57PhaseBuildingsEndStep()=>_pr57PhaseBuildingsReady=false;

    // UpdateInfrastructure has already materialized the exact original-order
    // Completed && !Removed roster after construction and relocations.
    // Subsequent consumers always read live mutable fields (owner, company,
    // operation, capacity, etc.), never a copied decision.
    void Pr57PhaseBuildingsPublish()
    {
        if(!Pr57PhaseBuildingWorksetEnabled)return;
        _pr57PhaseBuildingsCount=State.Buildings.Count;
        _pr57PhaseBuildingsReady=true;
    }

    IEnumerable<Building> Pr57PhaseActiveBuildings()
    {
        if(!Pr57PhaseBuildingWorksetEnabled||!_pr57PhaseBuildingsReady||
           _pr57PhaseBuildingsCount!=State.Buildings.Count)
            return State.Buildings.Where(b=>b.Completed&&!b.Removed);

        // Diagnostic shadow compares identity and order, not merely counts.
        // Any in-place Completed/Removed writer between phase boundaries
        // is a hard failure until it has an explicit invalidation publication.
        if(Pr57PhaseBuildingWorksetVerify)
        {
            var index=0;
            foreach(var building in State.Buildings)
            {
                if(!building.Completed||building.Removed)continue;
                if(index>=_active.Count||!ReferenceEquals(_active[index],building))
                    throw new InvalidOperationException("PR57_PHASE_BUILDINGS_CAUSAL_MISMATCH");
                index++;
            }
            if(index!=_active.Count)
                throw new InvalidOperationException("PR57_PHASE_BUILDINGS_CAUSAL_MISMATCH");
        }
        return _active;
    }
}
