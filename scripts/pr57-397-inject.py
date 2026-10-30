#!/usr/bin/env python3
"""PR57 #397: distinguish true inter-call path changes from Unsplit's own cleanup.

Apply after #394/#395/#396 only in disposable checkout. Exact Route/SitePath/
MovementAdmission contents, first 12000 physical calls; observational only.
"""
from pathlib import Path
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)

patch(
    "        public bool Same(Vehicle v)=>RouteSame(v)&&SiteSame(v)&&AdmissionSame(v);",
    """        public bool RouteSame(Pr57394Path other)=>route.SequenceEqual(other.route);
        public bool SiteSame(Pr57394Path other)=>sitePath.SequenceEqual(other.sitePath);
        public bool AdmissionSame(Pr57394Path other)=>
            hasAdmission==other.hasAdmission&&endIndex==other.endIndex&&
            reachesDock==other.reachesDock&&admissionRoute.SequenceEqual(other.admissionRoute)&&
            admissionSite.SequenceEqual(other.admissionSite);
        public bool Same(Vehicle v)=>RouteSame(v)&&SiteSame(v)&&AdmissionSame(v);"""
)
patch(
    "    static readonly bool Pr57396Enabled=Environment.GetEnvironmentVariable(\"PR57_396_ALL_UNSPLIT\")==\"1\";",
    """    static readonly bool Pr57397Enabled=Environment.GetEnvironmentVariable("PR57_397_ENTRY_CLEANUP")=="1";
    Dictionary<int,Pr57394Path>? _pr57397Entry;
    readonly long[] _pr57397Events=new long[6],_pr57397Route=new long[6],
        _pr57397Site=new long[6],_pr57397Admission=new long[6];
    long _pr57397Roster,_pr57397Logs,_pr57397Ticks;
    void Pr57397Diff(Dictionary<int,Pr57394Path>? before,
                      Dictionary<int,Pr57394Path>? after,int category)
    {
        if(before is null||after is null)return;
        if(before.Count!=after.Count)_pr57397Roster++;
        foreach(var pair in after)
        {
            if(!before.TryGetValue(pair.Key,out var old)){_pr57397Roster++;continue;}
            bool route=!old.RouteSame(pair.Value),site=!old.SiteSame(pair.Value),
                 admission=!old.AdmissionSame(pair.Value);
            if(!route&&!site&&!admission)continue;
            _pr57397Events[category]++;
            if(route)_pr57397Route[category]++;
            if(site)_pr57397Site[category]++;
            if(admission)_pr57397Admission[category]++;
            if(_pr57397Logs++<80)
                Console.WriteLine($"PR57_397_CHANGE physical={_pr57394Physical} class={category} vehicle={pair.Key} route={route} site={site} admission={admission}");
        }
    }
    static readonly bool Pr57396Enabled=Environment.GetEnvironmentVariable("PR57_396_ALL_UNSPLIT")=="1";"""
)
patch(
    "    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)\n    {\n        Pr57SetupGapStart(dt,operationsDt);",
    """    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)
    {
        // True method entry, BEFORE ClearPassedJunction/ReleaseManoeuvre.
        var pr57397Kind=dt>0?0:1;
        var pr57397Track=Pr57397Enabled&&!_pr57396Done&&_pr57394Physical<12000;
        if(pr57397Track)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            _pr57397Entry=Pr57395Capture();
            _pr57397Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        }
        Pr57SetupGapStart(dt,operationsDt);"""
)
patch(
    "        // 0=physical, 1=operations. Gap classes 2..5 encode P>P, P>O, O>P, O>O.",
    """        if(pr57397Track)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            // 0/1: net changes during this call's pre-actor cleanup.
            // 2..5: net changes from PREVIOUS call end to TRUE current entry.
            if(_pr57396Last is not null)
                Pr57397Diff(_pr57396Last,_pr57397Entry,2+_pr57396LastKind*2+pr57397Kind);
            Pr57397Diff(_pr57397Entry,Pr57395Capture(),pr57397Kind);
            _pr57397Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        }
        // 0=physical, 1=operations. Gap classes 2..5 encode P>P, P>O, O>P, O>O."""
)
patch(
    "        if(Pr57395Enabled&&dt>0&&_pr57394Physical<=12000)\n        {\n            var start=System.Diagnostics.Stopwatch.GetTimestamp();\n            Pr57395Compare(_pr57395Begin,false);",
    """        if(pr57397Track&&dt>0&&_pr57394Physical==12000)
            Console.WriteLine($"PR57_397_BOUNDARY physical={_pr57394Physical} events={string.Join(',',_pr57397Events)} route={string.Join(',',_pr57397Route)} site={string.Join(',',_pr57397Site)} admission={string.Join(',',_pr57397Admission)} roster={_pr57397Roster} traces={Math.Min(_pr57397Logs,80)} ticks={_pr57397Ticks} frequency={System.Diagnostics.Stopwatch.Frequency} classes=cleanupPhysical,cleanupOperations,trueGapPP,trueGapPO,trueGapOP,trueGapOO observational=True noSkippedSlices=True");
        if(Pr57395Enabled&&dt>0&&_pr57394Physical<=12000)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            Pr57395Compare(_pr57395Begin,false);"""
)
p.write_text(s,encoding="utf-8")
print("PR57_397_INJECTED trueEntryVsCleanup=True observational=True")
