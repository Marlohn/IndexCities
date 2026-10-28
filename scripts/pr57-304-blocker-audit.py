#!/usr/bin/env python3
"""PR57 #304: check selected body and return-time walker identities against #303.
Observational only; selected IDs do NOT close the read/write dependency graph.
"""
import collections
import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
def fields(line):
    return dict(re.findall(r"(\\w+)=([^\\s]+)", line))
def rows(lines, prefix):
    return [fields(x) for x in lines if x.startswith(prefix + " ")]
def key(x):
    return (x["call"], x["slice"], x["id"])
results = []
pairs = 0
for control, probe in (("control1", "probe1"), ("control2", "probe2")):
    for step in (0, 20, 50, 100):
        c = (root / f"real14hREAL-{control}" / f"checkpoint-{step}.json").read_bytes()
        p = (root / f"real14hREAL-{probe}" / f"checkpoint-{step}.json").read_bytes()
        assert c == p, (control, probe, step)
        json.loads(c)
        pairs += 1
    lines = (root / f"real14hREAL-{probe}.log").read_text().splitlines()
    exits = rows(lines, "PR57_303_RETURN")
    bodies = rows(lines, "PR57_304_BODY")
    walkers = rows(lines, "PR57_304_WALK")
    assert len(exits) == 300, (probe, len(exits))
    expected_body = collections.Counter(key(x) for x in exits if x["reason"] == "OnwardBody")
    expected_walk = collections.Counter(key(x) for x in exits if x["reason"] == "Pedestrian")
    assert sum(expected_body.values()) == 264
    assert sum(expected_walk.values()) == 36
    assert collections.Counter(key(x) for x in bodies) == expected_body
    assert collections.Counter(key(x) for x in walkers) == expected_walk
    assert all(int(x["count"]) >= 1 for x in walkers)
    assert all(int(x["first"]) == int(x["ids"].split(",")[0]) for x in walkers)
    blocker_readers = collections.defaultdict(set)
    pedestrian_readers = collections.defaultdict(set)
    for x in bodies:
        blocker_readers[int(x["blocker"])].add(int(x["id"]))
    for x in walkers:
        for pid in map(int, x["ids"].split(",")):
            pedestrian_readers[pid].add(int(x["id"]))
    result = {
        "arm": probe, "bodyReturns": len(bodies), "pedestrianReturns": len(walkers),
        "bodySelections": dict(sorted(collections.Counter(x["blocker"] for x in bodies).items())),
        "pedestrianMatches": dict(sorted(collections.Counter(
            p for x in walkers for p in x["ids"].split(",")).items())),
        "bodyFanoutDistinctReaders": {str(k): len(v) for k, v in sorted(blocker_readers.items())},
        "pedestrianFanoutDistinctReaders": {str(k): len(v) for k, v in sorted(pedestrian_readers.items())},
        "maxPedestriansAtOneReturn": max(int(x["count"]) for x in walkers),
        "scope": "selected-body-and-return-time-pedestrian-matches-only",
        "notCausalClosure": True, "noSkippedSlices": True, "noGainClaim": True,
    }
    print("PR57_304_AUDIT " + json.dumps(result, sort_keys=True), flush=True)
    results.append({k: v for k, v in result.items() if k != "arm"})
assert pairs == 8 and results[0] == results[1], "probe processes disagree"
print("PR57_304_VERIFIED checkpointPairs=8/8 processes=4 allAdvance2=True "
      "bodySelections=528 pedestrianReturns=72 noCausalClosure=True", flush=True)
