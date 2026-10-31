#!/usr/bin/env python3
"""#497: disposable-only exclusive managed allocation attribution.

Run only in a temporary CI checkout. It instruments synchronous method entry/exit
without altering decisions, state, return values, physical slices, or save schema.
The measured bytes include instrumentation/JIT effects and are NOT savings.
"""
from pathlib import Path

ROOT=Path("src/IndexCities.Simulation")
MOB=ROOT/"Mobility.cs"
SHARED=ROOT/"Mobility.SharedSpace.cs"

# Exactly one body of each canonical method is instrumented; expression-bodied
# overloads remain untouched. All scoped methods are synchronous, non-iterator.
TARGETS={
    SHARED:[
        (0,"static double PenetrationCore(Vehicle a,Point aAt,double aHeading,Vehicle b,Point bAt,double bHeading)"),
        (1,"List<(Vehicle Body,Point Position,double Heading)> Manoeuvre(Vehicle o,List<int> route,int from,Point at,double facing,IReadOnlyList<Point> site,bool approach,int minimum,out List<RoadNode> spaces,out int endIndex,Func<Point,double,bool>? extraClear=null,int? through=null)"),
        (2,"List<(Vehicle Body,Point Position,double Heading)> TurnWay(Vehicle o,out List<RoadNode> spaces)"),
        (3,"Vehicle? BodyOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses,bool pr57305Onward=false)"),
        (5,"List<(Vehicle Body,Point Position,double Heading)> EntryPlan(Vehicle v,Point dock,out List<RoadNode> spaces,out LocalMovementAdmission limit,out bool needed)"),
        (6,"bool AdmitManoeuvre(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> way,List<RoadNode> spaces,Point dock,bool yieldToTraffic,LocalMovementAdmission? limit=null)"),
        (7,"bool SpaceFree(Vehicle v,RoadNode node,Point entering,bool yieldToTraffic=true)"),
        (8,"bool CanTurnNow(Vehicle o)"),
        (9,"Func<Point,double,bool> ClearOfDockWay(Vehicle o,List<int> route,Point at,double heading,IReadOnlyList<Point> site)"),
        (10,"Vehicle? TrafficOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses)"),
        (11,"List<(Vehicle Body,Point Position,double Heading)> WayIn(Vehicle v,Point dock,out List<RoadNode> spaces)"),
        (12,"List<(Vehicle Body,Point Position,double Heading)> PhysicalDockWay(Vehicle v,Point dock,bool departure)"),
        (13,"List<(Vehicle Body,Point Position,double Heading)> OwnedDockWay(Vehicle o)"),
        (14,"List<(Vehicle Body,Point Position,double Heading)> BayEntryHold(Vehicle v)"),
    ],
    MOB:[
        (4,"bool JunctionPreferenceAllows(Vehicle v,RoadNode junction)"),
    ],
}
for path,methods in TARGETS.items():
    s=path.read_text()
    for kind,signature in methods:
        anchor=signature+"\n    {"
        assert s.count(anchor)==1,(path,kind,s.count(anchor))
        s=s.replace(anchor,anchor+"\n        using var pr57497Scope=Pr57497Begin("+str(kind)+");",1)
    if path==MOB:
        anchor="        var scanProfile=CostProfile;"
        assert s.count(anchor)==1
        s=s.replace(anchor,anchor+"\n        Pr57497BeginSlice(scanProfile is not null&&_pr57MovementAllocCalls<600);",1)
        anchor="        }\n    }\n    void ReplanVehicle(Vehicle v,Shipment s)"
        assert s.count(anchor)==1
        s=s.replace(anchor,"        }\n        Pr57497EndSlice(_pr57MovementAllocCalls);\n    }\n    void ReplanVehicle(Vehicle v,Shipment s)",1)
    path.write_text(s)

probe=r'''namespace IndexCities.Simulation;

// #497 temporary CI-only allocation attribution. NOT a production optimization.
public sealed partial class CitySimulation
{
    static readonly bool Pr57497Enabled=
        Environment.GetEnvironmentVariable("PR57_497_ALLOC_SPLIT")=="1";
    [ThreadStatic] static bool _pr57497Active;
    [ThreadStatic] static int _pr57497Depth;
    [ThreadStatic] static long[]? _pr57497Child;
    [ThreadStatic] static long[]? _pr57497Exclusive;
    [ThreadStatic] static long[]? _pr57497Inclusive;
    [ThreadStatic] static long[]? _pr57497Counts;
    [ThreadStatic] static long _pr57497RootBytes;
    [ThreadStatic] static long _pr57497RootCalls;
    static readonly string[] Pr57497Names={
        "PenetrationCore","Manoeuvre","TurnWay","BodyOnWay",
        "JunctionPreferenceAllows","EntryPlan","AdmitManoeuvre",
        "SpaceFree","CanTurnNow","ClearOfDockWay","TrafficOnWay",
        "WayIn","PhysicalDockWay","OwnedDockWay","BayEntryHold"
    };
    readonly struct Pr57497Scope : IDisposable
    {
        readonly bool _active;
        readonly int _kind,_depth;
        readonly long _start;
        public Pr57497Scope(int kind,int depth,long start)
        { _active=true;_kind=kind;_depth=depth;_start=start; }
        public void Dispose()
        {
            if(!_active)return;
            var total=GC.GetAllocatedBytesForCurrentThread()-_start;
            if(_pr57497Depth!=_depth)throw new InvalidOperationException("PR57_497_SCOPE_ORDER");
            var child=_pr57497Child![_depth];
            if(child>total)throw new InvalidOperationException("PR57_497_NEGATIVE_EXCLUSIVE");
            _pr57497Exclusive![_kind]+=total-child;
            _pr57497Inclusive![_kind]+=total;
            _pr57497Counts![_kind]++;
            _pr57497Depth--;
            if(_pr57497Depth>0)_pr57497Child[_pr57497Depth]+=total;
            else {_pr57497RootBytes+=total;_pr57497RootCalls++;}
        }
    }
    static Pr57497Scope Pr57497Begin(int kind)
    {
        if(!Pr57497Enabled||!_pr57497Active)return default;
        if(_pr57497Depth>=255)throw new InvalidOperationException("PR57_497_DEPTH");
        var depth=++_pr57497Depth;
        _pr57497Child![depth]=0;
        return new Pr57497Scope(kind,depth,GC.GetAllocatedBytesForCurrentThread());
    }
    void Pr57497BeginSlice(bool eligible)
    {
        if(!Pr57497Enabled)return;
        if(_pr57497Active||_pr57497Depth!=0)throw new InvalidOperationException("PR57_497_SLICE_NESTED");
        if(_pr57MovementAllocCalls==0)
        {
            _pr57497Child=new long[256];
            _pr57497Exclusive=new long[Pr57497Names.Length];
            _pr57497Inclusive=new long[Pr57497Names.Length];
            _pr57497Counts=new long[Pr57497Names.Length];
            _pr57497RootBytes=0;_pr57497RootCalls=0;
        }
        _pr57497Active=eligible;
    }
    void Pr57497EndSlice(long calls)
    {
        if(!Pr57497Enabled)return;
        if(_pr57497Depth!=0)throw new InvalidOperationException("PR57_497_UNBALANCED");
        _pr57497Active=false;
        if(calls!=600)return;
        long total=0;
        for(var i=0;i<Pr57497Names.Length;i++)total+=_pr57497Exclusive![i];
        if(total!=_pr57497RootBytes)throw new InvalidOperationException("PR57_497_EXCLUSIVE_RECONCILIATION");
        Console.WriteLine("PR57_497_TOTAL calls="+calls+" rootCalls="+_pr57497RootCalls+
            " rootBytes="+_pr57497RootBytes+" exclusiveSum="+total+
            " note=INTRUSIVE_METHOD_SCOPES_NOT_POSE_ONLY_NOT_ACCELERATION");
        for(var i=0;i<Pr57497Names.Length;i++)
            Console.WriteLine("PR57_497_KIND kind="+Pr57497Names[i]+" calls="+_pr57497Counts![i]+
                " inclusiveBytes="+_pr57497Inclusive![i]+
                " exclusiveBytes="+_pr57497Exclusive![i]);
    }
}
'''
path=ROOT/"Pr57497ExclusiveAlloc.CiOnly.cs"
assert not path.exists()
path.write_text(probe)
print("PR57_497_DISPOSABLE_INJECTION_PASS methods=15")
