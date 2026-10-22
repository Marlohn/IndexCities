namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // Experimental broad phase for PedestrianGap. The original precise
    // geometry, iteration order and sequential movement remain authoritative.
    const double PedestrianCellMetres=8;
    readonly Dictionary<(long X,long Z),List<(int Order,Citizen Citizen)>> _pedestrianCells=new();
    readonly Dictionary<int,(long X,long Z)> _pedestrianCellById=new();
    readonly Dictionary<int,int> _pedestrianOrder=new();
    readonly List<(int Order,Citizen Citizen)> _pedestrianQuery=new();
    readonly List<Citizen> _pedestrianQueryCitizens=[];
    bool _pedestrianIndexActive,_pedestrianIndexReady,_pedestrianIndexFallback;

    static bool PedestrianCell(Point p,out (long X,long Z) cell)
    {
        var x=Math.Floor(p.X/PedestrianCellMetres);var z=Math.Floor(p.Z/PedestrianCellMetres);
        if(!double.IsFinite(x)||!double.IsFinite(z)||x<=long.MinValue+4||x>=long.MaxValue-4||z<=long.MinValue+4||z>=long.MaxValue-4)
        {cell=default;return false;}
        cell=((long)x,(long)z);return true;
    }

    void AddPedestrianToCell(Citizen c,(long X,long Z) key)
    {
        if(!_pedestrianCells.TryGetValue(key,out var bucket))_pedestrianCells[key]=bucket=[];
        bucket.Add((_pedestrianOrder[c.Id],c));
        _pedestrianCellById[c.Id]=key;
    }

    void PreparePedestrianIndex()
    {
        _pedestrianIndexReady=true;
        _pedestrianIndexFallback=false;
        _pedestrianCells.Clear();_pedestrianCellById.Clear();_pedestrianOrder.Clear();
        for(var i=0;i<State.Citizens.Count;i++){
            var citizen=State.Citizens[i];_pedestrianOrder[citizen.Id]=i;
            if(!citizen.Alive||!citizen.Moving||citizen.VehicleId!=null)continue;
            if(!PedestrianCell(citizen.Position,out var cell)){_pedestrianIndexFallback=true;return;}
            AddPedestrianToCell(citizen,cell);
        }
    }

    // Called even for continue branches, so a citizen who walks, boards,
    // arrives, or begins a journey is visible to every later citizen in
    // this exact same tick, as in the original full scan.
    void SyncPedestrianIndex(Citizen citizen)
    {
        if(!_pedestrianIndexReady||_pedestrianIndexFallback)return;
        var eligible=citizen.Alive&&citizen.Moving&&citizen.VehicleId==null;
        if(eligible&&!PedestrianCell(citizen.Position,out _)){_pedestrianIndexFallback=true;return;}
        var indexed=_pedestrianCellById.TryGetValue(citizen.Id,out var previous);
        (long X,long Z) next=default;
        if(eligible)PedestrianCell(citizen.Position,out next);
        if(indexed&&eligible&&previous==next)return;
        if(indexed){
            var existing=_pedestrianCells[previous];
            existing.RemoveAll(e=>e.Citizen.Id==citizen.Id);
            _pedestrianCellById.Remove(citizen.Id);
        }
        if(eligible)AddPedestrianToCell(citizen,next);
    }

    List<Citizen> PedestrianCandidates(Point position,double radius)
    {
        if(!_pedestrianIndexActive)return State.Citizens;
        if(!_pedestrianIndexReady)PreparePedestrianIndex();
        if(_pedestrianIndexFallback||!double.IsFinite(radius)||radius<0)return State.Citizens;
        if(!PedestrianCell(new Point(position.X-radius,position.Z-radius),out var low)
            ||!PedestrianCell(new Point(position.X+radius,position.Z+radius),out var high)
            ||high.X-low.X>20||high.Z-low.Z>20)return State.Citizens;

        _pedestrianQuery.Clear();_pedestrianQueryCitizens.Clear();
        for(var x=low.X;x<=high.X;x++)for(var z=low.Z;z<=high.Z;z++)
            if(_pedestrianCells.TryGetValue((x,z),out var bucket))_pedestrianQuery.AddRange(bucket);
        // Preserve State.Citizens order, including exact ties at a merge.
        _pedestrianQuery.Sort((a,b)=>a.Order.CompareTo(b.Order));
        foreach(var entry in _pedestrianQuery)_pedestrianQueryCitizens.Add(entry.Citizen);
        return _pedestrianQueryCitizens;
    }
}
