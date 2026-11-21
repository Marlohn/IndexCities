#!/usr/bin/env python3
"""PR57 #398: attribute the 11 true physical->operational route gaps to Step phases.

Apply after #394/#395/#396/#397 ONLY in a disposable checkout. Observe exact
Route contents for the 11 vehicle IDs witnessed in #397. Every canonical Step
and every MoveVehiclesUnsplit/Advance(2) remains unchanged. This fixture-scoped
net-boundary witness is NOT a writer-complete journal or an optimization.
"""
from pathlib import Path

p=Path("src/IndexCities.Simulation/CitySimulation.cs")
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)

patch(
    "    static readonly bool Pr57CauseTransitionProbe=Environment.GetEnvironmentVariable(\"PR57_CAUSE_TRANSITION_PROBE\")==\"1\";",
    """    static readonly bool Pr57398Enabled=Environment.GetEnvironmentVariable("PR57_398_ROUTE_PHASES")=="1";
    // Exact IDs from the 11 true gap P>O Route changes in the #397 REAL trace.
    // This is a fixture-specific attribution probe, not an eligibility filter.
    static readonly HashSet<int> Pr57398Targets=new()
        {1487,1380,655,695,1507,596,677,1282,689,1468,1386};
    readonly Dictionary<int,int[]> _pr57398Routes=new();
    readonly long[] _pr57398Changes=new long[14]; // 0..12 Step phases; 13 Step entry
    int _pr57398Step;
    long _pr57398Captures,_pr57398Missing,_pr57398Logs,_pr57398Ticks;
    void Pr57398Boundary(int phase)
    {
        if(!Pr57398Enabled||_pr57398Step>1800)return;
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        var found=0;
        foreach(var vehicle in State.Vehicles)
        {
            if(!Pr57398Targets.Contains(vehicle.Id))continue;
            found++;
            if(_pr57398Routes.TryGetValue(vehicle.Id,out var previous)
                &&!previous.SequenceEqual(vehicle.Route))
            {
                _pr57398Changes[phase]++;
                if(_pr57398Logs++<80)
                    Console.WriteLine($"PR57_398_CHANGE step={_pr57398Step} physical={_pr57394Physical} phase={phase} vehicle={vehicle.Id} previousCount={previous.Length} currentCount={vehicle.Route.Count}");
            }
            _pr57398Routes[vehicle.Id]=vehicle.Route.ToArray();
        }
        if(found!=Pr57398Targets.Count)_pr57398Missing++;
        _pr57398Captures++;
        _pr57398Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
    }
    static readonly bool Pr57CauseTransitionProbe=Environment.GetEnvironmentVariable("PR57_CAUSE_TRANSITION_PROBE")=="1";"""
)
patch(
    "        Pr57WorkAccessBeginStep();\n        Pr57BeginStep();",
    """        Pr57WorkAccessBeginStep();
        Pr57BeginStep();
        if(Pr57398Enabled){_pr57398Step++;Pr57398Boundary(13);}"""
)
for phase in range(13):
    marker=f"        if(profile is not null)profile.Mark({phase},ref started);"
    patch(marker,marker+f"\n        Pr57398Boundary({phase});")
patch(
    "        if(profile is not null)profile.FinishStep();",
    """        if(profile is not null)profile.FinishStep();
        if(Pr57398Enabled&&_pr57398Step==1800)
            Console.WriteLine($"PR57_398_PHASES steps={_pr57398Step} physical={_pr57394Physical} captures={_pr57398Captures} changes={string.Join(',',_pr57398Changes)} missingTargetSteps={_pr57398Missing} traces={Math.Min(_pr57398Logs,80)} ticks={_pr57398Ticks} frequency={System.Diagnostics.Stopwatch.Frequency} targetIds=11 exactRoute=True netBoundariesOnly=True observational=True noSkippedSlices=True");"""
)
p.write_text(s,encoding="utf-8")
print("PR57_398_INJECTED targetedStepPhaseRouteAttribution=True observational=True")
