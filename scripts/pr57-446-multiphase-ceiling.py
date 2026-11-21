#!/usr/bin/env python3
"""PR57 #446: independent, read-only ceiling audit of authentic #444/#445 ZIPs.

Usage: python3 scripts/pr57-446-multiphase-ceiling.py pr57-444.zip pr57-445.zip
No simulation, no altered saves, no optimization or extrapolation to 10k.
"""
import hashlib
import json
import re
import sys
from statistics import mean
from zipfile import ZipFile

EXPECTED = {
    444: "3d60a0b48e15e734e7a71bb7455bb9195f556dfcf2dd7e9344713885d09151d1",
    445: "ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a",
}
PHASE = re.compile(r"^\s*STEP_PHASE name=(\S+) wall=([\d.]+)s", re.M)
DETAIL = re.compile(r"^PR57_DISPATCH_DETAIL (.*)$", re.M)
PROFILE = re.compile(r"^STEP_PROFILE steps=(\d+) measuredWall=([\d.]+)s", re.M)
FIELDS = re.compile(r"(\w+)=([\d.]+)")

def parse(z, name):
    raw = z.read(name).decode("utf-8")
    m = PROFILE.search(raw)
    assert m and int(m.group(1)) == 100, name
    phases = {k: float(v) for k, v in PHASE.findall(raw)}
    assert len(phases) == 13, (name, phases)
    d = DETAIL.search(raw)
    assert d, name
    fields = {k: float(v) for k, v in FIELDS.findall(d.group(1))}
    assert all(fields[x] == 6800 for x in ("accessCalls", "crewCalls"))
    assert fields["reserveCalls"] == 100
    dispatch = fields["measuredDispatch"]
    components = sum(fields[x] for x in ("reserveWall", "accessWall", "crewWall"))
    residual = dispatch - components
    assert residual >= 0 and abs(dispatch - phases["construction-dispatch"]) < .001
    total = float(m.group(2))
    assert abs(total - sum(phases.values())) < .009
    return {
        "wall_profile_s": total,
        "vehicles_s": phases["vehicles"],
        "citizens_s": phases["citizen-mobility"],
        "construction_s": phases["construction"],
        "dispatch_s": dispatch,
        "reserve_s": fields["reserveWall"],
        "access_s": fields["accessWall"],
        "crew_s": fields["crewWall"],
        "dispatch_residual_s": residual,
        "three_helpers_ceiling_x": total / (total - components),
        "construction_plus_dispatch_ceiling_x": total / (total - phases["construction"] - dispatch),
        "construction_dispatch_citizens_ceiling_x": total / (total - phases["construction"] - dispatch - phases["citizen-mobility"]),
    }

def load(path, number):
    assert hashlib.sha256(open(path, "rb").read()).hexdigest() == EXPECTED[number]
    z = ZipFile(path)
    assert z.testzip() is None
    if number == 444:
        prefixes = [f"worksEarlyREAL/{i}-{mode}" for i, mode in enumerate("ABBA", 1)]
    else:
        prefixes = ["pr57-445/A", "pr57-445/B"]
    initial = [z.read(f"{p}/checkpoint-0.json") for p in prefixes]
    final = [z.read(f"{p}/checkpoint-1.json") for p in prefixes]
    assert len(set(initial)) == len(set(final)) == 1
    assert hashlib.sha256(final[0]).hexdigest() == "686ed27e0cc458d3d8f411a1e308a2dc98e87bf588c12d5dcf1ea787bad1c883"
    state = json.loads(initial[0]);assert len(state["Vehicles"]) == 42
    assert sum(bool(c["Alive"]) for c in state["Citizens"]) == 47
    assert sum(p["Status"] in ("Waiting", "InProgress") for p in state["Projects"]) == 68
    return [parse(z, f"{p}/full.log") for p in prefixes]

def main():
    assert len(sys.argv) == 3, "usage: script #444.zip #445.zip"
    a = load(sys.argv[1], 444)
    b = load(sys.argv[2], 445)
    # A arms in #444 are the composed-flag baseline; B arms have the rejected electric no-op.
    control = [a[0], a[3], b[0]]
    keys = control[0].keys()
    result = {"scope": "REAL_WORKS_47_ALIVE_42_VEHICLES_68_OPEN_100_ADVANCE2",
              "baseline": "COMPOSED_FLAGS_444_A_AND_445_A",
              "not_speedup": True, "full_checkpoint_parity": True,
              "all_six_runs": a+b,
              "control_means": {k: mean(row[k] for row in control) for k in keys},
              "control_ranges": {k: [min(row[k] for row in control), max(row[k] for row in control)] for k in keys}}
    print(json.dumps(result, indent=2, sort_keys=True))

if __name__ == "__main__":
    main()
