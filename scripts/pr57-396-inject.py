#!/usr/bin/env python3
"""PR57 #396: classify exact path changes across ALL Unsplit calls, including dt=0.

Apply after #394 and #395 to a disposable checkout. Observational only:
no decisions, order, phases, saves, or Advance(2) cadence are changed.
"""
from pathlib import Path

p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)

patch(
    "    readonly Dictionary<int,Pr57394Path> _pr57394Current=new(),_pr57394Previous=new();",
    """    static readonly bool Pr57396Enabled=Environment.GetEnvironmentVariable("PR57_396_ALL_UNSPLIT")=="1";
    Dictionary<int,Pr57394Path>? _pr57396Last,_pr57396Begin;
    int _pr57396LastKind;
    bool _pr57396Done;
    readonly long[] _pr57396Events=new long[6],_pr57396Route=new long[6],
        _pr57396Site=new long[6],_pr57396Admission=new long[6];
    long _pr57396PhysicalCalls,_pr57396OperationCalls,_pr57396Roster,_pr57396Logs,_pr57396Ticks;
    void Pr57396Compare(Dictionary<int,Pr57394Path>? prior,int category)
    {
        if(prior is null)return;
        if(prior.Count!=State.Vehicles.Count)_pr57396Roster++;
        foreach(var actor in State.Vehicles)
        {
            if(!prior.TryGetValue(actor.Id,out var old)){_pr57396Roster++;continue;}
            bool route=!old.RouteSame(actor),site=!old.SiteSame(actor),
                 admission=!old.AdmissionSame(actor);
            if(!route&&!site&&!admission)continue;
            _pr57396Events[category]++;
            if(route)_pr57396Route[category]++;
            if(site)_pr57396Site[category]++;
            if(admission)_pr57396Admission[category]++;
            if(_pr57396Logs++<80)
                Console.WriteLine($"PR57_396_CHANGE physical={_pr57394Physical} class={category} vehicle={actor.Id} route={route} site={site} admission={admission}");
        }
    }
    readonly Dictionary<int,Pr57394Path> _pr57394Current=new(),_pr57394Previous=new();"""
)
patch(
    "        if(pr57394Sample){\n            _pr57394Samples++;",
    """        // 0=physical, 1=operations. Gap classes 2..5 encode P>P, P>O, O>P, O>O.
        var pr57396Kind=dt>0?0:1;
        var pr57396Track=Pr57396Enabled&&!_pr57396Done&&_pr57394Physical<=12000;
        if(pr57396Track)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            if(_pr57396Last is not null)
                Pr57396Compare(_pr57396Last,2+_pr57396LastKind*2+pr57396Kind);
            _pr57396Begin=Pr57395Capture();
            if(pr57396Kind==0)_pr57396PhysicalCalls++;
            else _pr57396OperationCalls++;
            _pr57396Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        }
        if(pr57394Sample){
            _pr57394Samples++;"""
)
patch(
    "        SyncTowedVehicles();\n        if(Pr57395Enabled&&dt>0",
    """        SyncTowedVehicles();
        if(pr57396Track)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            Pr57396Compare(_pr57396Begin,pr57396Kind);
            _pr57396Last=Pr57395Capture();
            _pr57396LastKind=pr57396Kind;
            _pr57396Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
            if(dt>0&&_pr57394Physical==12000)
            {
                _pr57396Done=true;
                Console.WriteLine($"PR57_396_BOUNDARY physical={_pr57394Physical} physicalCalls={_pr57396PhysicalCalls} operationCalls={_pr57396OperationCalls} events={string.Join(',',_pr57396Events)} route={string.Join(',',_pr57396Route)} site={string.Join(',',_pr57396Site)} admission={string.Join(',',_pr57396Admission)} roster={_pr57396Roster} traces={Math.Min(_pr57396Logs,80)} ticks={_pr57396Ticks} frequency={System.Diagnostics.Stopwatch.Frequency} classes=withinPhysical,withinOperations,gapPP,gapPO,gapOP,gapOO observational=True noSkippedSlices=True");
            }
        }
        if(Pr57395Enabled&&dt>0"""
)
p.write_text(s,encoding="utf-8")
print("PR57_396_INJECTED allUnsplitClasses=True observational=True")
