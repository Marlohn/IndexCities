#!/usr/bin/env python3
"""PR57 #395: classify exact route/admission changes within vs between physical calls.

Apply AFTER pr57-394-inject.py, only to a disposable checkout. Capture
Route/SitePath/MovementAdmission content at each of the first 12000 physical
Unsplit boundaries, compare exact contents, and keep canonical decisions.
This is a net-boundary witness, NOT a writer-complete causal journal.
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
    """    static readonly bool Pr57395Enabled=Environment.GetEnvironmentVariable("PR57_395_PATH_BOUNDARY")=="1";
    Dictionary<int,Pr57394Path>? _pr57395Last,_pr57395Begin;
    long _pr57395Calls,_pr57395Outside,_pr57395Inside,_pr57395OutsideRoute,
        _pr57395InsideRoute,_pr57395OutsideSite,_pr57395InsideSite,
        _pr57395OutsideAdmission,_pr57395InsideAdmission,
        _pr57395Missing,_pr57395Logs,_pr57395Ticks;
    Dictionary<int,Pr57394Path> Pr57395Capture()
    {
        var result=new Dictionary<int,Pr57394Path>(State.Vehicles.Count);
        foreach(var actor in State.Vehicles)
            result.Add(actor.Id,new Pr57394Path(actor));
        return result;
    }
    void Pr57395Compare(Dictionary<int,Pr57394Path>? before,bool outside)
    {
        if(before is null)return;
        var seen=new HashSet<int>();
        foreach(var actor in State.Vehicles)
        {
            seen.Add(actor.Id);
            if(!before.TryGetValue(actor.Id,out var old))
            {
                _pr57395Missing++;
                continue;
            }
            bool route=!old.RouteSame(actor),site=!old.SiteSame(actor),
                 admission=!old.AdmissionSame(actor);
            if(!route&&!site&&!admission)continue;
            if(outside)
            {
                _pr57395Outside++;
                if(route)_pr57395OutsideRoute++;
                if(site)_pr57395OutsideSite++;
                if(admission)_pr57395OutsideAdmission++;
            }
            else
            {
                _pr57395Inside++;
                if(route)_pr57395InsideRoute++;
                if(site)_pr57395InsideSite++;
                if(admission)_pr57395InsideAdmission++;
            }
            if(_pr57395Logs++<32)
                Console.WriteLine($"PR57_395_CHANGE physical={_pr57394Physical} boundary={(outside?"betweenUnsplit":"withinUnsplit")} vehicle={actor.Id} route={route} site={site} admission={admission}");
        }
        foreach(var id in before.Keys)
            if(!seen.Contains(id))_pr57395Missing++;
    }
    readonly Dictionary<int,Pr57394Path> _pr57394Current=new(),_pr57394Previous=new();"""
)
patch(
    "        var pr57394Sample=Pr57394Enabled&&dt>0&&_pr57394Physical<=12000&&_pr57394Physical%20==0;\n        if(pr57394Sample){",
    """        var pr57394Sample=Pr57394Enabled&&dt>0&&_pr57394Physical<=12000&&_pr57394Physical%20==0;
        if(Pr57395Enabled&&dt>0&&_pr57394Physical<=12000)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            Pr57395Compare(_pr57395Last,true);
            _pr57395Begin=Pr57395Capture();
            _pr57395Calls++;
            _pr57395Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        }
        if(pr57394Sample){"""
)
patch(
    "        SyncTowedVehicles();\n        if(pr57394Sample){",
    """        SyncTowedVehicles();
        if(Pr57395Enabled&&dt>0&&_pr57394Physical<=12000)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            Pr57395Compare(_pr57395Begin,false);
            _pr57395Last=Pr57395Capture();
            _pr57395Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
            if(_pr57394Physical==12000)
                Console.WriteLine($"PR57_395_BOUNDARY physical={_pr57394Physical} calls={_pr57395Calls} inside={_pr57395Inside} outside={_pr57395Outside} insideRoute={_pr57395InsideRoute} outsideRoute={_pr57395OutsideRoute} insideSite={_pr57395InsideSite} outsideSite={_pr57395OutsideSite} insideAdmission={_pr57395InsideAdmission} outsideAdmission={_pr57395OutsideAdmission} rosterChanges={_pr57395Missing} traces={Math.Min(_pr57395Logs,32)} ticks={_pr57395Ticks} frequency={System.Diagnostics.Stopwatch.Frequency} exactContent=True netBoundariesOnly=True observational=True noSkippedSlices=True");
        }
        if(pr57394Sample){"""
)
p.write_text(s,encoding="utf-8")
print("PR57_395_INJECTED exactContent=True withinAndBetweenPhysical=True observational=True")
