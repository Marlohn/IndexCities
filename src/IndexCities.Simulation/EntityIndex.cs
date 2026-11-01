namespace IndexCities.Simulation;

// Id → list position cache over a public, mutable state list. Same result as
// the first-match scan it replaces: a hit is accepted only if that position of
// the live list still holds the Id, and anything else (new list, entities
// added or cleared by tests/QA/loading) rebuilds from the list before
// answering. Ids are unique and never reassigned (CheckInvariants).
sealed class EntityIndex<T>(Func<T,int> idOf) where T:class
{
    readonly Dictionary<int,int> _positions=new();
    List<T>? _list;int _count=-1;
    // PR57 #587: opt-in demand-driven exact miss resolution. A failed lookup
    // used to rebuild the ENTIRE dictionary, even for repeated missing IDs.
    // Scan the live list instead, retaining its first-match order and updating
    // only this key. No negative result is cached: external edits, including
    // same-count replacements and ID changes, remain visible on the next call.
    static readonly bool Pr57ExactMissScan=
        Environment.GetEnvironmentVariable("PR57_587_EXACT_MISS_SCAN")=="1";
    public long Pr57MissScans {get;private set;}
    public long Pr57MissFound {get;private set;}
    public T? Find(List<T> list,int id)
    {
        if(ReferenceEquals(list,_list)&&list.Count==_count)
        {
            if(_positions.TryGetValue(id,out var i)&&idOf(list[i])==id)return list[i];
            if(Pr57ExactMissScan)
            {
                Pr57MissScans++;
                for(var j=0;j<list.Count;j++)
                    if(idOf(list[j])==id)
                    {
                        _positions[id]=j;
                        Pr57MissFound++;
                        return list[j];
                    }
                _positions.Remove(id);
                return null;
            }
        }
        _positions.Clear();_list=list;_count=list.Count;
        for(var j=0;j<list.Count;j++)_positions.TryAdd(idOf(list[j]),j);
        return _positions.TryGetValue(id,out var index)?list[index]:null;
    }
}
