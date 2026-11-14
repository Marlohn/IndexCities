#!/usr/bin/env python3
"""#303: match actual MayEnterJunction return sites to #302 net Cause transitions.
No causal closure: only four actors/two nodes and net selected vehicle fields.
"""
import collections
import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
def fields(line):
    return dict(re.findall(r"(\w+)=([^\s]+)", line))
def rows(lines, prefix):
    return [(i, fields(line)) for i, line in enumerate(lines) if line.startswith(prefix + " ")]
def key(x):
    return (x["call"], x["slice"], x["id"])
all_pairs = []
all_summaries = []
for control, probe in (("control1", "probe1"), ("control2", "probe2")):
    for arm in (control, probe):
        lines = (root / f"real14hREAL-{arm}.log").read_text().splitlines()
        assert len(rows(lines, "PR57_294_FINAL")) == 1, arm
        assert len(rows(lines, "PR57_294_CHECKPOINT")) == 4, arm
    for step in (0, 20, 50, 100):
        a = (root / f"real14hREAL-{control}" / f"checkpoint-{step}.json").read_bytes()
        b = (root / f"real14hREAL-{probe}" / f"checkpoint-{step}.json").read_bytes()
        assert a == b, (control, probe, step)
        json.loads(a)
        all_pairs.append((control, probe, step))
    lines = (root / f"real14hREAL-{probe}.log").read_text().splitlines()
    steps = rows(lines, "PR57_302_STEP")
    net = rows(lines, "PR57_302_NET")
    writers = rows(lines, "PR57_302_WRITER")
    exits = rows(lines, "PR57_303_RETURN")
    assert [int(x["call"]) for _, x in steps] == list(range(76, 101))
    assert sum(int(x["eligibleNet"]) for _, x in steps) == 8
    assert sum(int(x["otherNet"]) + int(x["omitted"]) for _, x in steps) == 0
    assert len(net) == len(writers) == 8
    assert collections.Counter(key(x) for _, x in net) == collections.Counter(key(x) for _, x in writers)
    assert all(x["branch"] == "junction" for _, x in writers)
    assert all(x["eligible"] == "1" and x["statusBefore"] == x["statusAfter"] == "Travelling" for _, x in net)
    matched = []
    for i, writer in writers:
        candidates = [(j, x) for j, x in exits if j < i and key(x) == key(writer) and x["node"] == writer["node"]]
        assert candidates, (probe, writer, "no actual return")
        j, actual = candidates[-1]
        matched.append({"call": int(writer["call"]), "slice": int(writer["slice"]),
                        "id": int(writer["id"]), "node": int(writer["node"]),
                        "reason": actual["reason"], "allowed": int(actual["allowed"]),
                        "control": actual["control"], "clock": actual["clock"],
                        "cause": actual["cause"], "before": writer["before"], "after": writer["after"]})
    summary = {"arm": probe, "returnEvents": len(exits),
               "reasonCounts": dict(collections.Counter(x["reason"] for _, x in exits)),
               "matchedNetTransitions": matched, "checkpointPairs": 4,
               "notCausalClosure": True, "noSkippedSlices": True, "noGainClaim": True}
    print("PR57_303_AUDIT " + json.dumps(summary, sort_keys=True), flush=True)
    all_summaries.append([{k:v for k,v in x.items() if k!="clock"} for x in matched])
assert len(all_pairs) == 8
assert all_summaries[0] == all_summaries[1], "different matched decisions across probe processes"
print("PR57_303_VERIFIED checkpointPairs=8/8 processes=4 allAdvance2=True "
      "writerMatches=8/8 eachProbe=True actualReturns=True noCausalClosure=True", flush=True)
