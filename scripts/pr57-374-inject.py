#!/usr/bin/env python3
"""PR57 #374: first-hour REAL writer coalescing + next-slice negative-read fanout.

Checkout-only, applied AFTER #367/#368/#369/#370/#372. The original lane
predicate still runs exactly once, in the same order. No decision reuse,
skipped slices, changed physics or product behavior. This is not causal closure.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1] / 'src/IndexCities.Simulation'

def patch(path, old, new, count=1):
    text = path.read_text(encoding='utf-8')
    assert text.count(old) == count, (path.name, old, text.count(old), count)
    path.write_text(text.replace(old, new), encoding='utf-8')

p = root / 'Mobility.WriterFrontier372.Pr57.cs'
patch(p, '        if(Pr57372Enabled)_pr57372Active=true;',
      '        if(Pr57372Enabled)_pr57372Active=true;\n        Pr57374Begin();')
patch(p, '        _pr57372Events++;',
      '        _pr57372Events++;\n        Pr57374Record(v,kind);')
patch(p, '        _pr57372Active=false;\n        foreach(var (kind,count)',
      '        _pr57372Active=false;\n        Pr57374Finish();\n        foreach(var (kind,count)')

mobility = root / 'Mobility.cs'
patch(mobility, '        Pr57SetupGapStart(dt,operationsDt);',
      '        Pr57374BeginSlice();\n        Pr57SetupGapStart(dt,operationsDt);')
patch(mobility, 'if(other.Id==v.Id||!Pr57StageOccupies(other))',
      'if(other.Id==v.Id||!Pr57374Occupies(v,other))')

out = root / 'Mobility.NextSliceFanout374.Pr57.cs'
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// Partial shadow of *actual* negative OccupiesLane reads on the next physical
// slice after a canonical writer. It does not cover other readers, all writers,
// signals, pedestrians, road edits, clocks, commands, or transitive effects.
public sealed partial class CitySimulation
{
    static readonly bool Pr57374Enabled =
        Environment.GetEnvironmentVariable("PR57_374_NEXT_SLICE_FANOUT")=="1";
    bool _pr57374Active;
    long _pr57374Events,_pr57374PhysicalSlices,_pr57374DirtyReadSlices;
    long _pr57374ReadCalls,_pr57374True,_pr57374False,_pr57374Pairs;
    long _pr57374DirtyCandidatePairs,_pr57374DirtyReadCandidatePairs;
    long _pr57374ExternalEvents,_pr57374MaxReadFanIn;
    readonly HashSet<int> _pr57374PreviousDirty=new();
    readonly HashSet<int> _pr57374CurrentDirty=new();
    readonly HashSet<int> _pr57374ExternalDirty=new();
    readonly HashSet<(int Candidate,int Reader)> _pr57374ReadPairs=new();
    readonly HashSet<(long Slice,int Id)> _pr57374DirtySlicePairs=new();
    readonly HashSet<(long Time,int Id)> _pr57374DirtyAdvancePairs=new();
    readonly Dictionary<long,long> _pr57374EventsPerAdvance=new();
    readonly HashSet<int> _pr57374ReadCandidates=new();

    void Pr57374Begin()
    {
        if(Pr57374Enabled&&_pr57372Active)_pr57374Active=true;
    }
    void Pr57374Record(Vehicle v,string kind)
    {
        if(!_pr57374Active)return;
        _pr57374Events++;
        var now=State.Now.Ticks;
        _pr57374DirtyAdvancePairs.Add((now,v.Id));
        _pr57374EventsPerAdvance[now]=_pr57374EventsPerAdvance.GetValueOrDefault(now)+1;
        if(_pr57367Phase==0)
        {
            _pr57374CurrentDirty.Add(v.Id);
            _pr57374DirtySlicePairs.Add((_pr57374PhysicalSlices,v.Id));
        }
        else
        {
            _pr57374ExternalEvents++;
            _pr57374ExternalDirty.Add(v.Id);
        }
    }
    void Pr57374CloseSlice()
    {
        if(_pr57374PreviousDirty.Count==0)return;
        _pr57374DirtyReadSlices++;
        _pr57374Pairs+=_pr57374ReadPairs.Count;
        _pr57374DirtyCandidatePairs+=_pr57374PreviousDirty.Count;
        _pr57374DirtyReadCandidatePairs+=_pr57374ReadCandidates.Count;
        foreach(var group in _pr57374ReadPairs.GroupBy(p=>p.Candidate))
            _pr57374MaxReadFanIn=Math.Max(_pr57374MaxReadFanIn,group.Count());
    }
    void Pr57374BeginSlice()
    {
        if(!_pr57374Active)return;
        if(_pr57374PhysicalSlices>0)Pr57374CloseSlice();
        _pr57374PhysicalSlices++;
        _pr57374PreviousDirty.Clear();
        _pr57374PreviousDirty.UnionWith(_pr57374CurrentDirty);
        _pr57374PreviousDirty.UnionWith(_pr57374ExternalDirty);
        _pr57374CurrentDirty.Clear();
        _pr57374ExternalDirty.Clear();
        _pr57374ReadPairs.Clear();
        _pr57374ReadCandidates.Clear();
    }
    bool Pr57374Occupies(Vehicle reader,Vehicle candidate)
    {
        bool watch=_pr57374Active&&_pr57374PreviousDirty.Contains(candidate.Id);
        // Exactly one canonical invocation, in the original evaluation order.
        bool answer=Pr57StageOccupies(candidate);
        if(watch)
        {
            _pr57374ReadCalls++;
            if(answer)_pr57374True++;else _pr57374False++;
            _pr57374ReadPairs.Add((candidate.Id,reader.Id));
            _pr57374ReadCandidates.Add(candidate.Id);
        }
        return answer;
    }
    void Pr57374Finish()
    {
        if(!_pr57374Active)return;
        Pr57374CloseSlice();
        _pr57374Active=false;
        var advanceGroups=_pr57374DirtyAdvancePairs.GroupBy(x=>x.Time).ToArray();
        long maxDirtyAdvance=advanceGroups.Length==0?0:advanceGroups.Max(x=>x.Count());
        long maxEventsAdvance=_pr57374EventsPerAdvance.Count==0?0:
            _pr57374EventsPerAdvance.Values.Max();
        Console.WriteLine($"PR57_374_SUMMARY events={_pr57374Events} "+
            $"physicalSlices={_pr57374PhysicalSlices} "+
            $"dirtySlices={_pr57374DirtySlicePairs.Select(x=>x.Slice).Distinct().Count()} "+
            $"dirtySliceIdPairs={_pr57374DirtySlicePairs.Count} "+
            $"dirtyAdvances={advanceGroups.Length} "+
            $"dirtyAdvanceIdPairs={_pr57374DirtyAdvancePairs.Count} "+
            $"maxDirtyIdsPerAdvance={maxDirtyAdvance} "+
            $"maxEventsPerAdvance={maxEventsAdvance} "+
            $"externalEvents={_pr57374ExternalEvents} "+
            $"nextSliceDirtyWindows={_pr57374DirtyReadSlices} "+
            $"nextSliceDirtyCandidatePairs={_pr57374DirtyCandidatePairs} "+
            $"nextSliceReadCandidatePairs={_pr57374DirtyReadCandidatePairs} "+
            $"nextSliceReadPairs={_pr57374Pairs} "+
            $"nextSliceReadCalls={_pr57374ReadCalls} "+
            $"nextSliceTrue={_pr57374True} nextSliceFalse={_pr57374False} "+
            $"maxReadersPerDirtyCandidate={_pr57374MaxReadFanIn} "+
            "scope=PARTIAL_NEXT_SLICE_OCCUPIES_ONLY "+
            "noCausalClosure=True noDecisionReuse=True noSkippedSlices=True observational=True");
    }
}
''', encoding='utf-8')
print('PR57_374_INJECTED bounded coalescing and next-slice occupancy read shadow')
