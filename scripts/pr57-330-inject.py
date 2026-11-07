#!/usr/bin/env python3
"""PR57 #330: actor-local versus actor-major physical signature publication.

Runs #329's observational injector first. The actor-local arm replaces only
the expensive all-vehicle AFTER-ACTOR scan with one check of the processed actor,
plus existing canonical tow-target publication sites. The end-of-slice scan
is retained. No authoritative reader uses this shadow, and this is NOT a
complete mutation journal or a proof that any physics can be skipped.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-329-inject.py")

def replace_once(path, old, new):
    p = Path(path)
    s = p.read_text()
    assert s.count(old) == 1, (path, old, s.count(old))
    p.write_text(s.replace(old, new, 1))

replace_once(
    "src/IndexCities.Simulation/Mobility.cs",
    "                Pr57329AfterActor();",
    "                Pr57330AfterActor(v);",
)

physical = Path("src/IndexCities.Simulation/Mobility.PhysicalExposure329.Pr57.cs")
s = physical.read_text()
old = "    void Pr57329AfterActor() => Pr57329ObserveBoundary(false);"
assert s.count(old) == 1
new = """    // #330: only a bounded observational shadow, not canonical writer coverage.
    static readonly bool Pr57330ActorLocal =
        Environment.GetEnvironmentVariable("PR57_330_ACTOR_LOCAL") == "1";
    long _pr57330ActorChecks, _pr57330NonlocalCalls, _pr57330NonlocalChanges;

    bool Pr57330ObserveCandidate(Vehicle candidate)
    {
        if (!_pr57329Active) return false;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var changed = false;
        if (_pr57329Last.TryGetValue(candidate.Id, out var old))
        {
            var now = Pr57329Snapshot(candidate);
            if (old != now)
            {
                _pr57329ActorTransitions++;
                Pr57329MarkChanged(candidate.Id, old, now);
                _pr57329Last[candidate.Id] = now;
                changed = true;
            }
        }
        _pr57329BoundaryTicks += System.Diagnostics.Stopwatch.GetTimestamp() - started;
        return changed;
    }

    void Pr57330AfterActor(Vehicle actor)
    {
        if (!Pr57330ActorLocal) { Pr57329AfterActor(); return; }
        if (!_pr57329Active) return;
        _pr57330ActorChecks++;
        Pr57330ObserveCandidate(actor);
    }

    void Pr57330Nonlocal(Vehicle target)
    {
        if (!Pr57330ActorLocal || !_pr57329Active) return;
        _pr57330NonlocalCalls++;
        if (Pr57330ObserveCandidate(target)) _pr57330NonlocalChanges++;
    }

    void Pr57329AfterActor() => Pr57329ObserveBoundary(false);"""
s = s.replace(old, new, 1)
old = "        _pr57329ChangedIds.Clear();"
assert s.count(old) == 1
s = s.replace(old, old + """
        _pr57330ActorChecks = _pr57330NonlocalCalls = _pr57330NonlocalChanges = 0;""", 1)
old = "        _pr57329Active = false;\n    }\n}"
assert s.count(old) == 1
s = s.replace(old, """        Console.WriteLine(
            $"PR57_330_MODE call={_pr57300Calls} slice={_pr57329Slice} " +
            $"mode={(Pr57330ActorLocal ? "ACTOR_LOCAL" : "ACTOR_MAJOR")} " +
            $"actorChecks={_pr57330ActorChecks} nonlocalCalls={_pr57330NonlocalCalls} " +
            $"nonlocalChanges={_pr57330NonlocalChanges} " +
            "scope=PARTIAL_OBSERVATIONAL_NOT_CANONICAL_JOURNAL");
""" + old, 1)
physical.write_text(s)

tow = Path("src/IndexCities.Simulation/Towing.cs")
s = tow.read_text()
for anchor in ("Pr57307Publish(t);", "Pr57309PublishResume(t);"):
    assert s.count(anchor) == 4, (anchor, s.count(anchor))
    s = s.replace(anchor, anchor + "Pr57330Nonlocal(t);")
tow.write_text(s)
print("PR57_330_INJECTED actorLocalOptional=True nonlocalTowSites=8 canonicalDecisionsUntouched=True")
