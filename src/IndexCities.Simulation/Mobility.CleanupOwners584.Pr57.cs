// PR57 #584: step-local cleanup ownership, experimental and disabled by default.
// Retains the exact canonical cleanup for unsupported writers/list mutations.
// The roster is indexed in State.Vehicles order, not movement priority order.
namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    static readonly bool Pr57CleanupOwnerEnabled=
        Environment.GetEnvironmentVariable("PR57_584_CLEANUP_OWNER")=="1";
    static readonly bool Pr57CleanupOwnerShadow=
        Environment.GetEnvironmentVariable("PR57_584_SHADOW")=="1";
    readonly Dictionary<Vehicle,int> _pr57CleanupIndex=new(ReferenceEqualityComparer.Instance);
    readonly List<int> _pr57CleanupOrder=new();
    bool _pr57CleanupReady,_pr57CleanupFallback,_pr57CleanupInside;
    int _pr57CleanupVehicleCount;
    long _pr57CleanupBuilds,_pr57CleanupSlices,_pr57CleanupSparseSlices;
    long _pr57CleanupSkipped,_pr57CleanupVisited,_pr57CleanupFull;
    long _pr57CleanupMismatches,_pr57CleanupWriterAdds,_pr57CleanupFallbacks;

    static bool Pr57CleanupOwns(Vehicle v)=>
        v.IntersectionClaim!=null||v.IntersectionEnteredAt!=null||
        v.ManoeuvreDock!=null||v.ManoeuvreSpaces.Count>0;

    void Pr57CleanupOwnersBeginStep()
    {
        if(!Pr57CleanupOwnerEnabled)return;
        _pr57CleanupReady=false;
        _pr57CleanupFallback=false;
        _pr57CleanupInside=false;
        _pr57CleanupOrder.Clear();
        _pr57CleanupIndex.Clear();
    }

    // Existing reservation publication callsites update the roster immediately.
    // An unknown vehicle (e.g. a tow inserted during this Step) forces the full
    // canonical pass on all subsequent slices. No ownership crosses a Step.
    void Pr57CleanupOwnerWrite(Vehicle v)
    {
        if(!Pr57CleanupOwnerEnabled||!_pr57CleanupReady||_pr57CleanupFallback||_pr57CleanupInside)return;
        if(!_pr57CleanupIndex.TryGetValue(v,out var index))
        {
            _pr57CleanupFallback=true;
            _pr57CleanupFallbacks++;
            return;
        }
        var at=_pr57CleanupOrder.BinarySearch(index);
        if(Pr57CleanupOwns(v))
        {
            if(at<0){_pr57CleanupOrder.Insert(~at,index);_pr57CleanupWriterAdds++;}
        }
        else if(at>=0)_pr57CleanupOrder.RemoveAt(at);
    }

    void Pr57CleanupOwnersRun()
    {
        if(!Pr57CleanupOwnerEnabled)
        {
            foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}
            return;
        }
        _pr57CleanupSlices++;
        if(!_pr57CleanupReady)
        {
            _pr57CleanupVehicleCount=State.Vehicles.Count;
            for(var i=0;i<State.Vehicles.Count;i++)
            {
                var v=State.Vehicles[i];
                if(!_pr57CleanupIndex.TryAdd(v,i))
                {
                    _pr57CleanupFallback=true;
                    _pr57CleanupFallbacks++;
                    break;
                }
                if(Pr57CleanupOwns(v))_pr57CleanupOrder.Add(i);
            }
            _pr57CleanupReady=true;
            _pr57CleanupBuilds++;
        }
        if(!_pr57CleanupFallback)
        {
            if(State.Vehicles.Count!=_pr57CleanupVehicleCount||
               _pr57CleanupOrder.Any(i=>i<0||i>=State.Vehicles.Count||
                    !_pr57CleanupIndex.TryGetValue(State.Vehicles[i],out var original)||original!=i))
            {
                _pr57CleanupFallback=true;
                _pr57CleanupFallbacks++;
            }
        }
        if(!_pr57CleanupFallback&&Pr57CleanupOwnerShadow)
        {
            // Shadow census happens before any cleanup mutation. A missed
            // writer cannot silently produce a false-negative cleanup.
            var actual=new List<int>();
            for(var i=0;i<State.Vehicles.Count;i++)
                if(Pr57CleanupOwns(State.Vehicles[i]))actual.Add(i);
            if(!actual.SequenceEqual(_pr57CleanupOrder))
            {
                _pr57CleanupMismatches++;
                _pr57CleanupFallback=true;
                _pr57CleanupFallbacks++;
                Console.WriteLine($"PR57_584_SHADOW_MISMATCH step={Steps} expected={actual.Count} tracked={_pr57CleanupOrder.Count}");
            }
        }
        if(_pr57CleanupFallback)
        {
            _pr57CleanupFull++;
            foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}
            return;
        }
        _pr57CleanupSparseSlices++;
        _pr57CleanupSkipped+=State.Vehicles.Count-_pr57CleanupOrder.Count;
        _pr57CleanupVisited+=_pr57CleanupOrder.Count;
        _pr57CleanupInside=true;
        try
        {
            // Original State.Vehicles order is essential: a release may be
            // observed by a later owner in this same cleanup boundary.
            foreach(var index in _pr57CleanupOrder)
            {
                var vehicle=State.Vehicles[index];
                ClearPassedJunction(vehicle);
                ReleaseManoeuvre(vehicle);
            }
        }
        finally{_pr57CleanupInside=false;}
        // Cleanup can only release existing ownership. New ownership is
        // published through the writer hook during the subsequent actor pass.
        for(var i=_pr57CleanupOrder.Count-1;i>=0;i--)
            if(!Pr57CleanupOwns(State.Vehicles[_pr57CleanupOrder[i]]))
                _pr57CleanupOrder.RemoveAt(i);
    }

    public string Pr57584Stats()=>
        $"PR57_584_STATS builds={_pr57CleanupBuilds} slices={_pr57CleanupSlices} sparseSlices={_pr57CleanupSparseSlices} "+
        $"skipped={_pr57CleanupSkipped} visited={_pr57CleanupVisited} full={_pr57CleanupFull} "+
        $"mismatches={_pr57CleanupMismatches} writerAdds={_pr57CleanupWriterAdds} fallbacks={_pr57CleanupFallbacks}";
}
