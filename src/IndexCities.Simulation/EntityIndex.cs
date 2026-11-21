namespace IndexCities.Simulation;

// Id → list position cache over a public, mutable state list. Same result as
// the first-match scan it replaces: a hit is accepted only if that position of
// the live list still holds the Id, and anything else (new list, entities
// added or cleared by tests/QA/loading) rebuilds from the list before
// answering. Ids are unique and never reassigned (CheckInvariants).
sealed class EntityIndex<T>(Func<T,int> idOf) where T:class
{
    readonly Dictionary<int,int> _positions=new();
    // PR57 isolated opt-in: only within a synchronous vehicle phase, memoize
    // repeated ABSENT IDs. A regular lookup must still rebuild on a miss.
    // Clear on every index rebuild, count change, and phase exit. The phase
    // never admits external State edits, and entity IDs cannot be reassigned.
    readonly HashSet<int> _pr57AbsentIds=new();
    bool _pr57AbsentWindow;
    List<T>? _list;int _count=-1;

    public void Pr57BeginAbsentWindow()
    {
        _pr57AbsentIds.Clear();
        _pr57AbsentWindow=true;
    }
    public void Pr57EndAbsentWindow()
    {
        _pr57AbsentWindow=false;
        _pr57AbsentIds.Clear();
    }

    public T? Find(List<T> list,int id)
    {
        if(ReferenceEquals(list,_list)&&list.Count==_count)
        {
            if(_positions.TryGetValue(id,out var cached)&&idOf(list[cached])==id)return list[cached];
            if(_pr57AbsentWindow&&_pr57AbsentIds.Contains(id))return null;
        }
        // Rebuild on every other miss, including the first miss of a new phase,
        // so external modifications at the public Advance boundary are visible.
        _pr57AbsentIds.Clear();
        _positions.Clear();_list=list;_count=list.Count;
        for(var j=0;j<list.Count;j++)_positions.TryAdd(idOf(list[j]),j);
        if(_positions.TryGetValue(id,out var i))return list[i];
        if(_pr57AbsentWindow)_pr57AbsentIds.Add(id);
        return null;
    }
}
