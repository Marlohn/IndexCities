#!/usr/bin/env python3
"""PR57 #368: canonical identity guard before expensive writer callbacks.

Apply AFTER pr57-367-inject.py, to checkout only. Compare baseline #367
versus guarded #368 with identical direct writer accounting. This is not a
causal cache, scheduler, or product change.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "src/IndexCities.Simulation"
model = root / "Model.cs"
journal = root / "Mobility.DirectWriter367.Pr57.cs"

def replace(path, before, after, count=1):
    s = path.read_text(encoding="utf-8")
    assert s.count(before) == count, (path, before, s.count(before))
    path.write_text(s.replace(before, after), encoding="utf-8")

replace(model,
    "    public static Action<Vehicle,string,object?,object?>? Pr57367OnWrite;",
    """    // PR57 #368: nonpersisted, canonical-instance marker; no product state.
    internal bool Pr57368Canonical;
    public static readonly bool Pr57368GuardEnabled =
        Environment.GetEnvironmentVariable("PR57_368_CANONICAL_GUARD") == "1";
    public static Action<Vehicle,string,object?,object?>? Pr57367OnWrite;""")
replace(model,
    "if(Pr57367OnWrite is { } callback&&!Pr57367Same(prior,value))",
    "if(Pr57367OnWrite is { } callback&&(!Pr57368GuardEnabled||Pr57368Canonical)&&!Pr57367Same(prior,value))",
    count=9)
replace(journal,
    "foreach(var v in State.Vehicles)_pr57367Canonical.Add(v);",
    """foreach(var v in State.Vehicles)
        {
            _pr57367Canonical.Add(v);
            v.Pr57368Canonical=true;
        }""")
replace(journal,
    "        Vehicle.Pr57367OnWrite=null;\n        _pr57367Active=false;",
    """        Vehicle.Pr57367OnWrite=null;
        foreach(var v in _pr57367Canonical)v.Pr57368Canonical=false;
        _pr57367Active=false;""")
replace(journal,
    '        Console.WriteLine($"PR57_367_SUMMARY canonical={_pr57367Canonical.Count} "+',
    '        Console.WriteLine($"PR57_368_GUARD enabled={Vehicle.Pr57368GuardEnabled} canonical={_pr57367Canonical.Count}");\n        Console.WriteLine($"PR57_367_SUMMARY canonical={_pr57367Canonical.Count} "+')
print("PR57_368_INJECTED canonical identity guard; opt-in, no skipped slices")
