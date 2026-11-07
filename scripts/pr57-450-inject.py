#!/usr/bin/env python3
"""PR57 #450: observational allocation attribution inside junction preference.

The original CanTurnNow and every geometric predicate execute unchanged.
Counts only CanTurnNow calls reached from JunctionPreferenceAllows, and the
four canonical nested stages. The disposable Actions checkout is the only
place where engine code is injected. No cache, no skipped slices, no mutation.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]

def replace_once(path, old, new):
    s = path.read_text(encoding="utf-8")
    n = s.count(old)
    assert n == 1, (str(path), old[:120], n)
    path.write_text(s.replace(old, new, 1), encoding="utf-8")

mob = root / "src/IndexCities.Simulation/Mobility.cs"
replace_once(mob, "&&CanTurnNow(o)).ToList();",
             "&&Pr57450PreferenceCanTurn(o)).ToList();")

shared = root / "src/IndexCities.Simulation/Mobility.SharedSpace.cs"
replace_once(shared, "    bool CanTurnNow(Vehicle o)\n    {", """    static readonly bool Pr57450Enabled =
        Environment.GetEnvironmentVariable("PR57_450_PREFERENCE_ALLOC")=="1";
    bool _pr57450InPreference;
    long _pr57450Calls,_pr57450Bytes,_pr57450Allowed;
    readonly long[] _pr57450StageCalls=new long[4],_pr57450StageBytes=new long[4];
    // All counters are observational. This wrapper executes CanTurnNow exactly once.
    bool Pr57450PreferenceCanTurn(Vehicle o)
    {
        if(!Pr57450Enabled)return CanTurnNow(o);
        var before=GC.GetAllocatedBytesForCurrentThread();
        var previous=_pr57450InPreference;
        _pr57450InPreference=true;
        bool result;
        try{result=CanTurnNow(o);}
        finally{_pr57450InPreference=previous;}
        _pr57450Calls++;
        if(result)_pr57450Allowed++;
        _pr57450Bytes+=GC.GetAllocatedBytesForCurrentThread()-before;
        return result;
    }
    void Pr57450Stage(int stage,long before)
    {
        if(!_pr57450InPreference)return;
        _pr57450StageCalls[stage]++;
        _pr57450StageBytes[stage]+=GC.GetAllocatedBytesForCurrentThread()-before;
    }
    public long[] Pr57450Snapshot()
    {
        var a=new long[13];
        a[0]=_pr57450Calls;a[1]=_pr57450Bytes;a[2]=_pr57450Allowed;
        a[3]=_pr57JunctionDetailCalls[4];a[4]=_pr57JunctionDetailBytes[4];
        for(var i=0;i<4;i++){a[5+2*i]=_pr57450StageCalls[i];a[6+2*i]=_pr57450StageBytes[i];}
        return a;
    }
    bool CanTurnNow(Vehicle o)
    {""")
replace_once(shared, "        var turn=TurnWay(o,out _);\n",
             "        var pr57450Before=_pr57450InPreference?GC.GetAllocatedBytesForCurrentThread():0L;\n"
             "        var turn=TurnWay(o,out _);\n"
             "        if(_pr57450InPreference)Pr57450Stage(0,pr57450Before);\n")
replace_once(shared, "        var body=BodyOnWay(o,turn);\n",
             "        pr57450Before=_pr57450InPreference?GC.GetAllocatedBytesForCurrentThread():0L;\n"
             "        var body=BodyOnWay(o,turn);\n"
             "        if(_pr57450InPreference)Pr57450Stage(1,pr57450Before);\n")
replace_once(shared, "            var way=EntryPlan(o,dock,out _,out _,out var needed);\n",
             "            pr57450Before=_pr57450InPreference?GC.GetAllocatedBytesForCurrentThread():0L;\n"
             "            var way=EntryPlan(o,dock,out _,out _,out var needed);\n"
             "            if(_pr57450InPreference)Pr57450Stage(2,pr57450Before);\n")
replace_once(shared, "                var dockBody=BodyOnWay(o,way);\n",
             "                pr57450Before=_pr57450InPreference?GC.GetAllocatedBytesForCurrentThread():0L;\n"
             "                var dockBody=BodyOnWay(o,way);\n"
             "                if(_pr57450InPreference)Pr57450Stage(3,pr57450Before);\n")

runner = root / "scripts/pr57-actor-regime-337/Program.cs"
replace_once(runner, "    var before = sim.Pr57298VehicleCostSnapshot();",
             "    var pr57450Before=sim.Pr57450Snapshot();\n"
             "    var before = sim.Pr57298VehicleCostSnapshot();")
replace_once(runner, "    var after=sim.Pr57298VehicleCostSnapshot();",
             """    var after=sim.Pr57298VehicleCostSnapshot();
    var pr57450After=sim.Pr57450Snapshot();
    if(Environment.GetEnvironmentVariable("PR57_450_PREFERENCE_ALLOC")=="1")
    {
        var diff=new long[13];
        for(var k=0;k<13;k++){diff[k]=pr57450After[k]-pr57450Before[k];if(diff[k]<0)throw new InvalidOperationException("#450 nonmonotone");}
        var stageBytes=diff[6]+diff[8]+diff[10]+diff[12];
        if(stageBytes>diff[1]||diff[1]>diff[4])
            throw new InvalidOperationException($"#450 allocation boundaries mismatch: stages={stageBytes} turn={diff[1]} preference={diff[4]}");
        Console.WriteLine($"PR57_450_PREFERENCE hour={hour} preferenceCalls={diff[3]} preferenceBytes={diff[4]} canTurnCalls={diff[0]} canTurnBytes={diff[1]} canTurnAllowed={diff[2]} turnWayCalls={diff[5]} turnWayBytes={diff[6]} laneBodyCalls={diff[7]} laneBodyBytes={diff[8]} entryPlanCalls={diff[9]} entryPlanBytes={diff[10]} dockBodyCalls={diff[11]} dockBodyBytes={diff[12]} residualCanTurnBytes={diff[1]-stageBytes} observational=True");
    }""")
print("PR57_450_INJECTED observational=True canonicalCanTurnNow=True")
