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
    public T? Find(List<T> list,int id)
    {
        if(ReferenceEquals(list,_list)&&list.Count==_count&&_positions.TryGetValue(id,out var i)&&idOf(list[i])==id)return list[i];
        _positions.Clear();_list=list;_count=list.Count;
        for(var j=0;j<list.Count;j++)_positions.TryAdd(idOf(list[j]),j);
        return _positions.TryGetValue(id,out i)?list[i]:null;
    }
}
