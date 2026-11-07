#!/usr/bin/env python3
"""PR57 #367: direct scalar writer journal, opt-in in CI checkout only.

Replaces selected Vehicle auto-properties with equivalent accessors solely in
the checkout. Reports effective setter writes on canonical vehicle instances,
including transient writes and priority timestamps. Does NOT cover in-place
lists, nested object mutations, external road/citizen/signal writers, or prove
causal closure. Does not skip any simulation phase or Advance(2).
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
model=root/"Model.cs"
engine=root/"CitySimulation.cs"

def replace(path,old,new,expected=1):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==expected,(path.name,old,s.count(old))
    path.write_text(s.replace(old,new),encoding="utf-8")

fields={
    "Position":"Point",
    "Heading":"double",
    "Status":"VehicleStatus",
    "RouteIndex":"int",
    "DockPosition":"Point?",
    "WaitingSince":"double",
    "IntersectionClaim":"int?",
    "IntersectionEnteredAt":"double?",
    "PassengerCar":"bool",
}
# Use exact bit comparisons for physical doubles, including signed zero.
helpers="""    // PR57 #367 checkout-only instrumentation; no persisted or product field.
    public static Action<Vehicle,string,object?,object?>? Pr57367OnWrite;
    static bool Pr57367Same(double a,double b)=>
        BitConverter.DoubleToInt64Bits(a)==BitConverter.DoubleToInt64Bits(b);
    static bool Pr57367Same(double? a,double? b)=>
        a.HasValue==b.HasValue&&(!a.HasValue||Pr57367Same(a.Value,b!.Value));
    static bool Pr57367Same(Point a,Point b)=>
        Pr57367Same(a.X,b.X)&&Pr57367Same(a.Z,b.Z);
    static bool Pr57367Same(Point? a,Point? b)=>
        a.HasValue==b.HasValue&&(!a.HasValue||Pr57367Same(a.Value,b!.Value));
    static bool Pr57367Same<T>(T a,T b)=>EqualityComparer<T>.Default.Equals(a,b);
"""
replace(model,"public sealed class Vehicle\n{","public sealed class Vehicle\n{\n"+helpers)
def replace_vehicle(old,new):
    s=model.read_text(encoding="utf-8")
    start=s.index("public sealed class Vehicle\n{")
    stop=s.find("\npublic sealed class ",start+len("public sealed class Vehicle\n{"))
    if stop<0: stop=len(s)
    body=s[start:stop]
    assert body.count(old)==1,("Vehicle",old,body.count(old))
    model.write_text(s[:start]+body.replace(old,new)+s[stop:],encoding="utf-8")

for name,kind in fields.items():
    old=f"    public {kind} {name} {{get;set;}}"
    backing="_pr57367"+name
    new=(f"    private {kind} {backing};\n"
         f"    public {kind} {name} {{ get=>{backing}; set {{\n"
         f"        var prior={backing}; {backing}=value;\n"
         f"        if(Pr57367OnWrite is {{ }} callback&&!Pr57367Same(prior,value))\n"
         f"            callback(this,\"{name}\",prior,value);\n"
         f"    }} }}")
    replace_vehicle(old,new)

replace(engine,"        Pr57WorkAccessBeginStep();",
               "        Pr57367Begin();\n        Pr57WorkAccessBeginStep();")
for phase in range(1,14):
    old=f"        if(Pr57Writer289Enabled)_pr57Writer289Phase={phase};"
    # Phase 13 occurs twice in the original Step.
    replace(engine,old,old+f"\n        if(_pr57367Active)_pr57367Phase={phase};",
            expected=2 if phase==13 else 1)

out=root/"Mobility.DirectWriter367.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// #367 observes immediate *effective* scalar setter writes across all phases,
// on canonical vehicles only. Does not infer writer closure or reusable results.
public sealed partial class CitySimulation
{
    static readonly bool Pr57367Enabled=
        Environment.GetEnvironmentVariable("PR57_367_DIRECT_WRITERS")=="1";
    static readonly DateTime Pr57367Start=new(2026,1,7,14,0,0);
    static readonly DateTime Pr57367Stop=new(2026,1,7,15,0,0);
    bool _pr57367Active,_pr57367Done;
    int _pr57367Phase;
    long _pr57367Total,_pr57367Noncanonical;
    readonly HashSet<Vehicle> _pr57367Canonical=new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<string,long> _pr57367Counts=new();
    readonly Dictionary<string,HashSet<int>> _pr57367Ids=new();
    readonly Dictionary<string,long> _pr57367Phases=new();
    readonly List<string> _pr57367First=new();
    void Pr57367Begin()
    {
        if(!Pr57367Enabled||_pr57367Done)return;
        if(State.Now>=Pr57367Stop)
        {
            if(_pr57367Active)Pr57367Finish();
            return;
        }
        if(_pr57367Active){_pr57367Phase=0;return;}
        if(State.Now<Pr57367Start)return;
        _pr57367Active=true;
        _pr57367Phase=0;
        foreach(var v in State.Vehicles)_pr57367Canonical.Add(v);
        Vehicle.Pr57367OnWrite=Pr57367Write;
    }
    void Pr57367Write(Vehicle v,string field,object? oldValue,object? newValue)
    {
        if(!_pr57367Active)return;
        if(!_pr57367Canonical.Contains(v)){_pr57367Noncanonical++;return;}
        _pr57367Total++;
        _pr57367Counts[field]=_pr57367Counts.GetValueOrDefault(field)+1;
        if(!_pr57367Ids.TryGetValue(field,out var ids))
            _pr57367Ids.Add(field,ids=new HashSet<int>());
        ids.Add(v.Id);
        var phase=field+":"+_pr57367Phase;
        _pr57367Phases[phase]=_pr57367Phases.GetValueOrDefault(phase)+1;
        if(_pr57367First.Count<40)
            _pr57367First.Add($"field={field} id={v.Id} phase={_pr57367Phase} "+
                $"before={oldValue} after={newValue} now={State.Now:O}");
    }
    void Pr57367Finish()
    {
        Vehicle.Pr57367OnWrite=null;
        _pr57367Active=false;
        _pr57367Done=true;
        foreach(var (field,count) in _pr57367Counts.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_367_FIELD field={field} writes={count} "+
                $"unique={_pr57367Ids[field].Count} scope=CANONICAL_SCALAR_SETTERS");
        foreach(var (key,count) in _pr57367Phases.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_367_PHASE key={key} writes={count}");
        foreach(var row in _pr57367First)
            Console.WriteLine("PR57_367_EXAMPLE "+row);
        Console.WriteLine($"PR57_367_SUMMARY canonical={_pr57367Canonical.Count} "+
            $"writes={_pr57367Total} noncanonical={_pr57367Noncanonical} "+
            $"fields={_pr57367Counts.Count} phaseGroups={_pr57367Phases.Count} "+
            "hour=14to15 exactBits=True inPlaceListsUnseen=True "+
            "externalWritersUnseen=True noSkippedSlices=True observational=True");
    }
}
''',encoding="utf-8")
print("PR57_367_INJECTED direct canonical scalar setter journal")
