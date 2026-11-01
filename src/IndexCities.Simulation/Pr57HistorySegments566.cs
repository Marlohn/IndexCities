using System.Collections;

namespace IndexCities.Simulation;

// PR57 #566: append-only segmented backing, without compression, duplicate
// canonical Lists, event omission, or changes to the persisted History array.
// Only the Personal writer opts in; any public List access deoptimizes forever.
internal sealed class Pr57HistorySegments : IEnumerable<PersonalEvent>
{
    internal static readonly bool Enabled=Environment.GetEnvironmentVariable("PR57_HISTORY_SEGMENTS")=="1";
    const int MaxBlock=64;
    readonly List<PersonalEvent[]> _sealed=[];
    PersonalEvent[] _tail=new PersonalEvent[4];
    int _tailCount;
    internal int Count {get;private set;}

    internal Pr57HistorySegments(IEnumerable<PersonalEvent> existing)
    {
        foreach(var entry in existing)Append(entry);
    }

    internal void Append(PersonalEvent entry)
    {
        if(_tailCount==_tail.Length)
        {
            if(_tail.Length<MaxBlock)
            {
                var grown=new PersonalEvent[Math.Min(MaxBlock,_tail.Length*2)];
                Array.Copy(_tail,grown,_tailCount);
                _tail=grown;
            }
            else
            {
                _sealed.Add(_tail);
                _tail=new PersonalEvent[MaxBlock];
                _tailCount=0;
            }
        }
        _tail[_tailCount++]=entry;
        Count++;
    }

    public IEnumerator<PersonalEvent> GetEnumerator()
    {
        foreach(var block in _sealed)
            foreach(var entry in block)
                yield return entry;
        for(var i=0;i<_tailCount;i++)
            yield return _tail[i];
    }
    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}
