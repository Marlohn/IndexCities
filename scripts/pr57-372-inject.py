#!/usr/bin/env python3
"""PR57 #372: writer-side shadow publication and six missing direct list sites.

Checkout-only, applied AFTER #367/#368/#369/#370. Counts immediate effective
canonical scalar/list/reference events and optionally publishes an epoch per
vehicle ID. No reader result reuse, no causal closure, no skipped Step/phase,
no gameplay or persistence changes. Missing external writers remain.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"

def patch(path,old,new,expected=1):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==expected,(path.name,old,s.count(old),expected)
    path.write_text(s.replace(old,new),encoding="utf-8")

journal=root/"Mobility.DirectWriter367.Pr57.cs"
lists=root/"Mobility.DirectList369.Pr57.cs"
nonlocal_file=root/"Mobility.Nonlocal370.Pr57.cs"

patch(journal,"        _pr57367Total++;",
      '        _pr57367Total++;\n        Pr57372Publish(v,"scalar:"+field);')
patch(lists,"        Pr57370Begin();",
      "        Pr57370Begin();\n        Pr57372Begin();")
patch(lists,"        Pr57370Finish();",
      "        Pr57372Finish();\n        Pr57370Finish();")
patch(lists,"        _pr57369Attempts[key]=_pr57369Attempts.GetValueOrDefault(key)+1;",
      '        _pr57369Attempts[key]=_pr57369Attempts.GetValueOrDefault(key)+1;\n'
      '        if(changed&&key.StartsWith("list:",StringComparison.Ordinal))Pr57372Publish(v,key);')
patch(nonlocal_file,
      "        if(same)_pr57370RefEqual++;else _pr57370RefDifferent++;",
      '        if(same)_pr57370RefEqual++;else _pr57370RefDifferent++;\n'
      '        if(!same)Pr57372Publish(v,"ref:"+field);')

# These six direct in-place writers were classified in #371 but missed by
# the 17+11 selected callsites of #369/#370.
sites={
    "BuildingDemolition.Cargo.cs":[("v","Route","Clear()",2)],
    "BuildingDemolition.Execution.cs":[("v","Route","Clear()",1)],
    "Construction.Paving.cs":[("v","Route","Clear()",1)],
    "Economy.cs":[("car","Route","Clear()",1)],
    "Mobility.Bypass.cs":[("v","Detour","RemoveAt(0)",1)],
}
for filename,entries in sites.items():
    path=root/filename
    s=path.read_text(encoding="utf-8")
    for actor,field,call,count in entries:
        old=f"{actor}.{field}.{call};"
        assert s.count(old)==count,(filename,old,s.count(old),count)
        key=f"additional:{filename}:{field}.{call.split('(')[0]}"
        new=(f"{{if(Pr57372Enabled&&{actor}.Pr57368Canonical){{"
             f"var pr57372Before={actor}.{field}.Count;"
             f"{old}Pr57369ListCall({actor},\"{key}\",pr57372Before,"
             f"{actor}.{field}.Count);"
             f"}}else{{{old}}}}}")
        s=s.replace(old,new)
    path.write_text(s,encoding="utf-8")

out=root/"Mobility.WriterFrontier372.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;

namespace IndexCities.Simulation;

// Partial writer-side shadow only. A per-ID epoch does not encode which
// readers depend on that ID, nor pedestrian/road/clock/command invalidation.
public sealed partial class CitySimulation
{
    static readonly bool Pr57372Enabled=
        Environment.GetEnvironmentVariable("PR57_372_WRITER_FRONTIER")=="1";
    static readonly bool Pr57372PublishEnabled=
        Environment.GetEnvironmentVariable("PR57_372_PUBLISH")=="1";
    bool _pr57372Active;
    long _pr57372Events,_pr57372Published,_pr57372Ticks;
    readonly Dictionary<int,long> _pr57372Epoch=new();
    readonly Dictionary<string,long> _pr57372Kinds=new();
    readonly Dictionary<string,long> _pr57372Phases=new();
    readonly Dictionary<int,long> _pr57372PerId=new();
    readonly List<string> _pr57372First=new();

    void Pr57372Begin()
    {
        if(Pr57372Enabled)_pr57372Active=true;
    }
    void Pr57372Publish(Vehicle v,string kind)
    {
        if(!_pr57372Active||!v.Pr57368Canonical)return;
        _pr57372Events++;
        _pr57372Kinds[kind]=_pr57372Kinds.GetValueOrDefault(kind)+1;
        var phase=_pr57367Phase+":"+kind;
        _pr57372Phases[phase]=_pr57372Phases.GetValueOrDefault(phase)+1;
        _pr57372PerId[v.Id]=_pr57372PerId.GetValueOrDefault(v.Id)+1;
        if(_pr57372First.Count<16)
            _pr57372First.Add($"id={v.Id} kind={kind} phase={_pr57367Phase} now={State.Now:O}");
        if(!Pr57372PublishEnabled)return;
        long t=Stopwatch.GetTimestamp();
        _pr57372Epoch[v.Id]=_pr57372Epoch.GetValueOrDefault(v.Id)+1;
        _pr57372Ticks+=Stopwatch.GetTimestamp()-t;
        _pr57372Published++;
    }
    void Pr57372Finish()
    {
        if(!_pr57372Active)return;
        _pr57372Active=false;
        foreach(var (kind,count) in _pr57372Kinds.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_372_FIELD key={kind} events={count}");
        foreach(var (phase,count) in _pr57372Phases.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_372_PHASE key={phase} events={count}");
        foreach(var first in _pr57372First)
            Console.WriteLine("PR57_372_EXAMPLE "+first);
        Console.WriteLine($"PR57_372_SUMMARY enabled={Pr57372Enabled} "+
            $"publish={Pr57372PublishEnabled} events={_pr57372Events} "+
            $"published={_pr57372Published} touched={_pr57372PerId.Count} "+
            $"epochIds={_pr57372Epoch.Count} epochTotal={_pr57372Epoch.Values.Sum()} "+
            $"maxEventsPerId={(_pr57372PerId.Count==0?0:_pr57372PerId.Values.Max())} "+
            $"publishTicks={_pr57372Ticks} tickFrequency={Stopwatch.Frequency} "+
            $"additionalSites=6 onlySelectedWriters=True "+
            "noReaderInvalidation=True noSkippedSlices=True observational=True");
    }
}
''',encoding="utf-8")
print("PR57_372_INJECTED six extra list writers and opt-in per-ID shadow epochs")
