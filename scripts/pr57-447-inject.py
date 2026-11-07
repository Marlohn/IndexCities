#!/usr/bin/env python3
"""PR57 #447: pre-actor physical cohort allocation census; disposable CI checkout.

Observational only. Uses the existing #298 per-actor Stopwatch and
GC.GetAllocatedBytesForCurrentThread boundaries. No new per-actor allocation,
no changed physical predicates, no skipped slice, no state/decision reuse.
The pre-state cohort is intentionally distinct from #337's post-Cause groups.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
mob=root/"src/IndexCities.Simulation/Mobility.cs"
s=mob.read_text(encoding="utf-8")

def once(old,new):
    global s
    count=s.count(old)
    assert count==1,("Mobility anchor",old[:120],count)
    s=s.replace(old,new,1)

once("    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)\n    {",
"""    static readonly bool Pr57447Enabled=Environment.GetEnvironmentVariable("PR57_447_COHORT_ALLOC")=="1";
    // 0=dt<=0 operations; 1=nontravelling; 2=travelling waiting at entry;
    // 3=travelling without waiting; 4=detour/site/tow special.
    readonly long[] _pr57447Calls=new long[5],_pr57447Ticks=new long[5],_pr57447Bytes=new long[5];
    public long[] Pr57447CohortSnapshot()
    {
        var values=new long[15];
        for(var k=0;k<5;k++){
            values[3*k]=_pr57447Calls[k];
            values[3*k+1]=_pr57447Ticks[k];
            values[3*k+2]=_pr57447Bytes[k];
        }
        return values;
    }
    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)
    {""")

once("            var pr57CostOldSpaces=pr57CostProbe?v.ManoeuvreSpaces.Count:0;\n            try{",
"""            var pr57CostOldSpaces=pr57CostProbe?v.ManoeuvreSpaces.Count:0;
            var pr57447Cohort=Pr57447Enabled&&pr57CostProbe?
                (dt<=0?0:v.Status!=VehicleStatus.Travelling?1:
                 v.Detour.Count>0||v.SitePathBeforeRoad||v.TowPhase!=TowPhase.None?4:
                 v.WaitingSince>0?2:3):-1;
            try{""")

once("                    var pr57Allocated=GC.GetAllocatedBytesForCurrentThread()-pr57CostStartBytes;\n                    var pr57Moved=",
"""                    var pr57Allocated=GC.GetAllocatedBytesForCurrentThread()-pr57CostStartBytes;
                    if(pr57447Cohort>=0){
                        _pr57447Calls[pr57447Cohort]++;
                        _pr57447Ticks[pr57447Cohort]+=pr57ElapsedTicks;
                        _pr57447Bytes[pr57447Cohort]+=pr57Allocated;
                    }
                    var pr57Moved=""")

mob.write_text(s,encoding="utf-8")

runner=root/"scripts/pr57-actor-regime-337/Program.cs"
s=runner.read_text(encoding="utf-8")

def once_runner(old,new):
    global s
    count=s.count(old)
    assert count==1,("Runner anchor",old[:120],count)
    s=s.replace(old,new,1)

once_runner("var initialTime = sim.State.Now;",
"""var initialTime = sim.State.Now;
var pr57447Enabled=Environment.GetEnvironmentVariable("PR57_447_COHORT_ALLOC")=="1";""")
once_runner("    var before = sim.Pr57298VehicleCostSnapshot();",
"""    var pre447=pr57447Enabled?sim.Pr57447CohortSnapshot():null;
    var before = sim.Pr57298VehicleCostSnapshot();""")
once_runner("    var after=sim.Pr57298VehicleCostSnapshot();",
"""    var after=sim.Pr57298VehicleCostSnapshot();
    var post447=pr57447Enabled?sim.Pr57447CohortSnapshot():null;""")
once_runner("    long actorEvaluations=0, actorBytes=0, claimChanges=0, spaceChanges=0;",
"""    long actorEvaluations=0, actorBytes=0, actorTicks=0, claimChanges=0, spaceChanges=0;""")
once_runner("        actorEvaluations+=calls; actorBytes+=bytes; claimChanges+=claims; spaceChanges+=spaces;",
"""        actorEvaluations+=calls; actorBytes+=bytes; actorTicks+=ticks;
        claimChanges+=claims; spaceChanges+=spaces;""")
once_runner("    // #155 also counts extra zero-dt operations; never assume 7 calls per Advance(2).",
"""    if(pr57447Enabled){
        if(pre447 is null||post447 is null||pre447.Length!=15||post447.Length!=15)
            throw new InvalidOperationException("#447 missing pre-cohort counters");
        long cohortCalls=0,cohortBytes=0,cohortTicks=0;
        string[] labels=["operations","nontravelling","travelling-waiting","travelling-active","special"];
        for(var k=0;k<5;k++){
            var i=3*k;
            var calls=post447[i]-pre447[i];
            var ticks=post447[i+1]-pre447[i+1];
            var bytes=post447[i+2]-pre447[i+2];
            if(calls<0||ticks<0||bytes<0)
                throw new InvalidOperationException("#447 nonmonotone cohort counter");
            cohortCalls+=calls;cohortTicks+=ticks;cohortBytes+=bytes;
            Console.WriteLine($"PR57_447_COHORT hour={hour} kind={k} label={labels[k]} calls={calls} ticks={ticks} allocatedBytes={bytes}");
        }
        if(cohortCalls!=actorEvaluations||cohortBytes!=actorBytes||cohortTicks!=actorTicks)
            throw new InvalidOperationException($"#447 cohort reconciliation calls={cohortCalls}/{actorEvaluations} bytes={cohortBytes}/{actorBytes} ticks={cohortTicks}/{actorTicks}");
        Console.WriteLine($"PR57_447_HOUR hour={hour} calls={cohortCalls} allocatedBytes={cohortBytes} ticks={cohortTicks} preState=True observational=True sameActorBoundariesAs337=True");
    }
    // #155 also counts extra zero-dt operations; never assume 7 calls per Advance(2).""")
runner.write_text(s,encoding="utf-8")
print("PR57_447_INJECTED observational=True noNewPerActorGCReads=True preState=True")
