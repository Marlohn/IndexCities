#!/usr/bin/env python3
"""PR57 #369: first-hour direct list-reference and 17 in-place call-site census.

Apply after #367 and #368 to a disposable Actions checkout only. No change to
committed engine or gameplay. Reference replacement != semantic content change;
count preservation != content preservation. Partial causal coverage by design.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
model=root/"Model.cs"
journal=root/"Mobility.DirectWriter367.Pr57.cs"

def replace(path,old,new,expected=1):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==expected,(path.name,old,s.count(old),expected)
    path.write_text(s.replace(old,new),encoding="utf-8")

# Keep property names/types/JSON shape unchanged. The callback is guarded
# before any allocation and only fires on canonical instances during the hour.
s=model.read_text(encoding="utf-8")
anchor="public sealed class Vehicle\n{"
assert s.count(anchor)==1
head,tail=s.split(anchor,1)
fields={
    "Route":"List<int>",
    "SavedRoute":"List<int>",
    "SitePath":"List<Point>",
    "DockCorridor":"List<MovementPose>",
    "ManoeuvreSpaces":"List<int>",
    "Detour":"List<Point>",
    "MovementAdmission":"LocalMovementAdmission?",
}
for field,kind in fields.items():
    old=f"    public {kind} {field} {{get;set;}}"+("" if field=="MovementAdmission" else "=[];")
    # The list declaration ends with [] as part of the auto-property initializer.
    if field!="MovementAdmission":
        old=f"    public {kind} {field} {{get;set;}}=[];"
    assert tail.count(old)==1,(field,tail.count(old))
    initial="=new();" if field!="MovementAdmission" else ";"
    backing=f"_pr57369{field}"
    new=(f"    private {kind} {backing}{initial}\n"
         f"    public {kind} {field} {{ get=>{backing}; set {{\n"
         f"        var prior={backing}; {backing}=value;\n"
         f"        if(Pr57369OnListReplace is {{ }} cb && Pr57368Canonical && "
         f"!ReferenceEquals(prior,value)) cb(this,\"{field}\");\n"
         f"    }} }}")
    tail=tail.replace(old,new,1)
marker="    public static Action<Vehicle,string,object?,object?>? Pr57367OnWrite;"
assert tail.count(marker)==1
tail=tail.replace(marker,
    "    public static Action<Vehicle,string>? Pr57369OnListReplace;\n"+marker,1)
model.write_text(head+anchor+tail,encoding="utf-8")

# 17 call sites previously identified in #354; now observe active REAL hour
# and all 13 phases with the canonical marker from #368.
sites={
    "Mobility.cs":[
        ("v","Route","Clear()",4),
        ("v","Detour","Clear()",1),
        ("v","ManoeuvreSpaces","Add(junction.Id)",2),
    ],
    "Mobility.SharedSpace.cs":[
        ("m","ManoeuvreSpaces","AddRange(needed.Select(node=>node.Id))",1),
        ("v","ManoeuvreSpaces","Clear()",1),
        ("v","DockCorridor","Clear()",2),
        ("v","ManoeuvreSpaces","RemoveAll(id=>!_roads.TryGetValue(id,out var node)||!EntersArea(v,v.Position,v.Heading,node,CarriagewayHalfWidth)&&!rest.Any(p=>EntersArea(v,p.Position,p.Heading,node)))",1),
    ],
    "Towing.cs":[
        ("tow","Detour","Clear()",1),
        ("t","Detour","Clear()",1),
        ("t","Route","Clear()",2),
        ("tow","Route","Insert(0,behind.Id)",1),
    ],
}
for filename,entries in sites.items():
    path=root/filename
    s=path.read_text(encoding="utf-8")
    for actor,field,call,expected in entries:
        old=f"{actor}.{field}.{call};"
        assert s.count(old)==expected,(filename,old,s.count(old),expected)
        key=f"{field}.{call.split('(')[0]}"
        new=(f"{{if(Pr57369Enabled&&{actor}.Pr57368Canonical){{"
             f"var pr57369Before={actor}.{field}.Count;"
             f"{old}Pr57369ListCall({actor},\"{key}\",pr57369Before,"
             f"{actor}.{field}.Count);"
             f"}}else{{{old}}}}}")
        s=s.replace(old,new)
    path.write_text(s,encoding="utf-8")

replace(journal,
    "        Vehicle.Pr57367OnWrite=Pr57367Write;",
    "        Vehicle.Pr57367OnWrite=Pr57367Write;\n        Pr57369Begin();")
replace(journal,
    "        Vehicle.Pr57367OnWrite=null;",
    "        Pr57369Finish();\n        Vehicle.Pr57367OnWrite=null;")

extra=root/"Mobility.DirectList369.Pr57.cs"
assert not extra.exists()
extra.write_text(r'''namespace IndexCities.Simulation;

// #369: writer-side partial census; no caching or skipped physics.
// Reference replacement does not prove changed list contents. Count equality
// does not prove no in-place mutation. All other writers remain uncovered.
public sealed partial class CitySimulation
{
    static readonly bool Pr57369Enabled=
        Environment.GetEnvironmentVariable("PR57_369_INPLACE_WRITERS")=="1";
    bool _pr57369Active;
    long _pr57369Calls,_pr57369RefEvents,_pr57369CountChanges;
    readonly Dictionary<string,long> _pr57369Attempts=new();
    readonly Dictionary<string,long> _pr57369Changes=new();
    readonly Dictionary<string,HashSet<int>> _pr57369Ids=new();
    readonly Dictionary<string,long> _pr57369Phases=new();
    readonly List<string> _pr57369First=new();

    void Pr57369Begin()
    {
        if(!Pr57369Enabled)return;
        _pr57369Active=true;
        Vehicle.Pr57369OnListReplace=Pr57369Replace;
    }
    void Pr57369Replace(Vehicle v,string field)
    {
        if(!_pr57369Active||!v.Pr57368Canonical)return;
        _pr57369RefEvents++;
        Pr57369Record(v,"ref:"+field,true);
    }
    void Pr57369ListCall(Vehicle v,string field,int before,int after)
    {
        if(!_pr57369Active||!v.Pr57368Canonical)return;
        _pr57369Calls++;
        bool changed=before!=after;
        if(changed)_pr57369CountChanges++;
        Pr57369Record(v,"list:"+field,changed);
    }
    void Pr57369Record(Vehicle v,string key,bool changed)
    {
        _pr57369Attempts[key]=_pr57369Attempts.GetValueOrDefault(key)+1;
        if(changed)_pr57369Changes[key]=_pr57369Changes.GetValueOrDefault(key)+1;
        if(!_pr57369Ids.TryGetValue(key,out var ids))
            _pr57369Ids.Add(key,ids=new HashSet<int>());
        ids.Add(v.Id);
        string phase=_pr57367Phase+":"+key;
        _pr57369Phases[phase]=_pr57369Phases.GetValueOrDefault(phase)+1;
        if(_pr57369First.Count<30)
            _pr57369First.Add($"key={key} id={v.Id} phase={_pr57367Phase} now={State.Now:O} changed={changed}");
    }
    void Pr57369Finish()
    {
        if(!_pr57369Active)return;
        Vehicle.Pr57369OnListReplace=null;
        _pr57369Active=false;
        foreach(var (key,count) in _pr57369Attempts.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_369_FIELD key={key} attempts={count} "+
                $"changed={_pr57369Changes.GetValueOrDefault(key)} ids={_pr57369Ids[key].Count}");
        foreach(var (key,count) in _pr57369Phases.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_369_PHASE key={key} attempts={count}");
        foreach(var first in _pr57369First)
            Console.WriteLine("PR57_369_EXAMPLE "+first);
        Console.WriteLine($"PR57_369_SUMMARY canonical={_pr57367Canonical.Count} "+
            $"inPlaceCalls={_pr57369Calls} countChanges={_pr57369CountChanges} "+
            $"referenceReplacements={_pr57369RefEvents} keys={_pr57369Attempts.Count} "+
            "scope=SEVEN_REFERENCES_AND_17_SELECTED_LIST_CALLS "+
            "sameCountContentUnseen=True otherWritersUnclosed=True "+
            "noSkippedSlices=True observational=True");
    }
}
''',encoding="utf-8")
print("PR57_369_INJECTED seven canonical list references and 17 selected call sites")
