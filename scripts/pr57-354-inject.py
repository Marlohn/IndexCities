#!/usr/bin/env python3
"""PR57 #354: checkout-only immediate journal of 17 selected in-place list calls.

Requires #353 injection first. No engine decisions are replaced or skipped.
Coverage is deliberately partial: other call sites, nested object mutations,
pedestrians, roads, signals and clock remain unclosed.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
base=root/"src/IndexCities.Simulation"

def once(s,old,new):
    assert s.count(old)==1,(old,s.count(old))
    return s.replace(old,new,1)

def instrument(filename,sites):
    path=base/filename
    s=path.read_text()
    for actor,field,call,expected in sites:
        old=f"{actor}.{field}.{call};"
        found=s.count(old)
        assert found==expected,(filename,old,found,expected)
        new=(f'{{var pr57354Before={actor}.{field}.Count;'
             f'{old}Pr57354Observe({actor},"{field}.{call.split("(")[0]}",'
             f'pr57354Before,{actor}.{field}.Count);}}')
        s=s.replace(old,new)
    path.write_text(s)

instrument("Mobility.cs",[
    ("v","Route","Clear()",4),
    ("v","Detour","Clear()",1),
    ("v","ManoeuvreSpaces","Add(junction.Id)",2),
])
instrument("Mobility.SharedSpace.cs",[
    ("m","ManoeuvreSpaces","AddRange(needed.Select(node=>node.Id))",1),
    ("v","ManoeuvreSpaces","Clear()",1),
    ("v","DockCorridor","Clear()",2),
    ("v","ManoeuvreSpaces",
     "RemoveAll(id=>!_roads.TryGetValue(id,out var node)||!EntersArea(v,v.Position,v.Heading,node,CarriagewayHalfWidth)&&!rest.Any(p=>EntersArea(v,p.Position,p.Heading,node)))",1),
])
instrument("Towing.cs",[
    ("tow","Detour","Clear()",1),
    ("t","Detour","Clear()",1),
    ("t","Route","Clear()",2),
    ("tow","Route","Insert(0,behind.Id)",1),
])

generated=base/"Mobility.WriterJournal353.Pr57.cs"
s=generated.read_text()
s=once(s,"        Vehicle.Pr57353Write = Pr57353Write;",
       "        Pr57354BeginSlice();\n        Vehicle.Pr57353Write = Pr57353Write;")
s=once(s,"        Vehicle.Pr57353Write = null;",
       "        Pr57354EndSlice();\n        Vehicle.Pr57353Write = null;")
generated.write_text(s)

extra=base/"Mobility.InPlace354.Pr57.cs"
assert not extra.exists()
extra.write_text(r'''namespace IndexCities.Simulation;

// #354: immediate writer-side observation at 17 explicitly audited list call
// sites. Count-changing calls publish through the #353 canonical-reference
// journal; equal-count calls are observed but not published. This does NOT
// capture all in-place mutations or changes that preserve collection length.
public sealed partial class CitySimulation
{
    readonly Dictionary<string,long> _pr57354Sites = new(StringComparer.Ordinal);
    long _pr57354Calls,_pr57354Changed,_pr57354CanonicalCalls,
         _pr57354CanonicalChanged,_pr57354TemporaryCalls,_pr57354TemporaryChanged;

    void Pr57354BeginSlice()
    {
        _pr57354Sites.Clear();
        _pr57354Calls=_pr57354Changed=_pr57354CanonicalCalls=0;
        _pr57354CanonicalChanged=_pr57354TemporaryCalls=_pr57354TemporaryChanged=0;
    }

    void Pr57354Observe(Vehicle vehicle,string field,int before,int after)
    {
        if (!_pr57353Active) return;
        var canonical=_pr57353Canonical?.Contains(vehicle)==true;
        var changed=before!=after;
        _pr57354Calls++;
        if(changed)_pr57354Changed++;
        if(canonical)
        {
            _pr57354CanonicalCalls++;
            if(changed)_pr57354CanonicalChanged++;
        }
        else
        {
            _pr57354TemporaryCalls++;
            if(changed)_pr57354TemporaryChanged++;
        }
        var key=field+":"+(canonical?"canonical":"temporary")+":"+(changed?"changed":"same");
        _pr57354Sites.TryGetValue(key,out var n);
        _pr57354Sites[key]=n+1;
        if(changed)Vehicle.Pr57353Write?.Invoke(vehicle,field);
    }

    void Pr57354EndSlice()
    {
        Console.WriteLine(
            $"PR57_354_INPLACE window={_pr57353Window} attempts={_pr57354Calls} " +
            $"changed={_pr57354Changed} canonicalAttempts={_pr57354CanonicalCalls} " +
            $"canonicalChanged={_pr57354CanonicalChanged} " +
            $"temporaryAttempts={_pr57354TemporaryCalls} " +
            $"temporaryChanged={_pr57354TemporaryChanged} " +
            $"sites={string.Join(",",_pr57354Sites.OrderBy(x=>x.Key).Select(x=>$"{x.Key}:{x.Value}"))} " +
            "scope=17_SELECTED_COUNT_CHANGING_LIST_CALL_SITES " +
            "sameCountContentMutationUnseen=True otherWritersUnclosed=True " +
            "noSkippedSlices=True observational=True intrusive=True");
    }
}
''')
print("PR57_354_INJECTED 17 selected list mutation sites; partial writer coverage")
