#!/usr/bin/env python3
"""PR57 #370: semantic reference-swap census plus nonlocal Route.Clear writers.

Apply after #367, #368, #369 in a disposable checkout only. The observer
compares complete values only at reference replacement, never at a read.
Eleven Route.Clear callsites in Cars/Breakdowns are observed directly.
No cache, skip, modified gameplay, persistence field or causal certificate.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
model=root/"Model.cs"
journal=root/"Mobility.DirectList369.Pr57.cs"

def replace(path,old,new,expected=1):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==expected,(path.name,old,s.count(old),expected)
    path.write_text(s.replace(old,new),encoding="utf-8")

replace(model,
    "public static Action<Vehicle,string>? Pr57369OnListReplace;",
    "public static Action<Vehicle,string,object?,object?>? Pr57369OnListReplace;")
for field in ("Route","SavedRoute","SitePath","DockCorridor",
              "ManoeuvreSpaces","Detour","MovementAdmission"):
    replace(model,f'cb(this,"{field}");',
                  f'cb(this,"{field}",prior,value);')

replace(journal,
    "void Pr57369Replace(Vehicle v,string field)",
    "void Pr57369Replace(Vehicle v,string field,object? before,object? after)")
replace(journal,
    'Pr57369Record(v,"ref:"+field,true);',
    'Pr57369Record(v,"ref:"+field,true);\n'
    '        if(Pr57370Enabled)Pr57370ObserveReplace(v,field,before,after);')
replace(journal,
    "        _pr57369Active=true;",
    "        _pr57369Active=true;\n        Pr57370Begin();")
replace(journal,
    "        Vehicle.Pr57369OnListReplace=null;\n        _pr57369Active=false;",
    "        Pr57370Finish();\n"
    "        Vehicle.Pr57369OnListReplace=null;\n        _pr57369Active=false;")

# Direct nonlocal route writers missed by #369's three-file scope.
# Exact callsite multiplicity is checked before any mutation of the checkout.
sites={
    "Cars.cs":{"car":1,"v":3},
    "Breakdowns.cs":{"v":4,"t":1,"tow":2},
}
for filename,actors in sites.items():
    path=root/filename
    s=path.read_text(encoding="utf-8")
    for actor,count in actors.items():
        old=f"{actor}.Route.Clear();"
        assert s.count(old)==count,(filename,old,s.count(old),count)
        new=(f"{{if(Pr57370Enabled&&{actor}.Pr57368Canonical){{"
             f"var pr57370Before={actor}.Route.Count;"
             f"{old}Pr57370ListCall({actor},pr57370Before,{actor}.Route.Count);"
             f"}}else{{{old}}}}}")
        s=s.replace(old,new)
    path.write_text(s,encoding="utf-8")

extra=root/"Mobility.Nonlocal370.Pr57.cs"
assert not extra.exists()
extra.write_text(r'''namespace IndexCities.Simulation;

// #370: compares contents only on canonical reference replacement and
// observes 11 nonlocal Route.Clear sites. Does not observe in-place edits
// preserving length, nested mutation, external actors, or global dependencies.
public sealed partial class CitySimulation
{
    static readonly bool Pr57370Enabled=
        Environment.GetEnvironmentVariable("PR57_370_NONLOCAL_WRITERS")=="1";
    bool _pr57370Active;
    long _pr57370RefSwaps,_pr57370RefEqual,_pr57370RefDifferent;
    long _pr57370NonlocalCalls,_pr57370NonlocalCountChanges;
    readonly Dictionary<string,long> _pr57370Fields=new();
    readonly Dictionary<string,long> _pr57370Changed=new();
    readonly Dictionary<string,long> _pr57370Phases=new();

    void Pr57370Begin(){if(Pr57370Enabled)_pr57370Active=true;}

    static bool Pr57370Bits(double a,double b)=>
        BitConverter.DoubleToInt64Bits(a)==BitConverter.DoubleToInt64Bits(b);
    static bool Pr57370Point(Point a,Point b)=>
        Pr57370Bits(a.X,b.X)&&Pr57370Bits(a.Z,b.Z);
    static bool Pr57370List<T>(List<T>? a,List<T>? b,Func<T,T,bool> equal)
    {
        if(ReferenceEquals(a,b))return true;
        if(a==null||b==null||a.Count!=b.Count)return false;
        for(int i=0;i<a.Count;i++)if(!equal(a[i],b[i]))return false;
        return true;
    }
    static bool Pr57370Pose(MovementPose a,MovementPose b)=>
        ReferenceEquals(a,b)||
        a!=null&&b!=null&&Pr57370Point(a.Position,b.Position)&&
        Pr57370Bits(a.Heading,b.Heading);
    static bool Pr57370Admission(LocalMovementAdmission? a,LocalMovementAdmission? b)=>
        ReferenceEquals(a,b)||
        a!=null&&b!=null&&a.EndRouteIndex==b.EndRouteIndex&&
        a.ReachesDock==b.ReachesDock&&
        Pr57370List(a.Route,b.Route,(x,y)=>x==y)&&
        Pr57370List(a.SitePath,b.SitePath,Pr57370Point);

    void Pr57370ObserveReplace(Vehicle v,string field,object? before,object? after)
    {
        if(!_pr57370Active||!v.Pr57368Canonical)return;
        bool same=field switch
        {
            "Route" or "SavedRoute" or "ManoeuvreSpaces" =>
                Pr57370List((List<int>?)before,(List<int>?)after,(x,y)=>x==y),
            "SitePath" or "Detour" =>
                Pr57370List((List<Point>?)before,(List<Point>?)after,Pr57370Point),
            "DockCorridor" =>
                Pr57370List((List<MovementPose>?)before,(List<MovementPose>?)after,Pr57370Pose),
            "MovementAdmission" =>
                Pr57370Admission((LocalMovementAdmission?)before,
                                 (LocalMovementAdmission?)after),
            _=>throw new InvalidOperationException("Unknown physical reference "+field)
        };
        _pr57370RefSwaps++;
        if(same)_pr57370RefEqual++;else _pr57370RefDifferent++;
        _pr57370Fields[field]=_pr57370Fields.GetValueOrDefault(field)+1;
        if(!same)_pr57370Changed[field]=_pr57370Changed.GetValueOrDefault(field)+1;
        string phase=_pr57367Phase+":"+field;
        _pr57370Phases[phase]=_pr57370Phases.GetValueOrDefault(phase)+1;
    }
    void Pr57370ListCall(Vehicle v,int before,int after)
    {
        if(!_pr57370Active||!v.Pr57368Canonical)return;
        _pr57370NonlocalCalls++;
        if(before!=after)_pr57370NonlocalCountChanges++;
        Pr57369ListCall(v,"nonlocal:Route.Clear",before,after);
    }
    void Pr57370Finish()
    {
        if(!_pr57370Active)return;
        _pr57370Active=false;
        foreach(var (field,n) in _pr57370Fields.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_370_FIELD field={field} swaps={n} "+
                $"different={_pr57370Changed.GetValueOrDefault(field)}");
        foreach(var (key,n) in _pr57370Phases.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_370_PHASE key={key} swaps={n}");
        Console.WriteLine($"PR57_370_SUMMARY refSwaps={_pr57370RefSwaps} "+
            $"refContentEqual={_pr57370RefEqual} "+
            $"refContentDifferent={_pr57370RefDifferent} "+
            $"nonlocalAttempts={_pr57370NonlocalCalls} "+
            $"nonlocalLengthChanges={_pr57370NonlocalCountChanges} "+
            "nonlocalSites=11 exactBits=True referenceSwapOnly=True "+
            "sameLengthEditsUnseen=True nestedWritersUnclosed=True "+
            "noSkippedSlices=True observational=True");
    }
}
''',encoding="utf-8")
print("PR57_370_INJECTED semantic reference swaps and 11 nonlocal Route.Clear sites")
