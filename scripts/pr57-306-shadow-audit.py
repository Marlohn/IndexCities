#!/usr/bin/env python3
"""PR57 #306: audit actor-major candidate projection against canonical #305.
This is a read-only coherence oracle, NOT a causal-closure or speedup claim.
"""
from collections import Counter
from pathlib import Path
import json
import sys

root = Path(sys.argv[1])

def records(lines, prefix):
    return [dict(token.split("=", 1) for token in line.split()[1:] if "=" in token)
            for line in lines if line.startswith(prefix + " ")]

def key(row):
    return row["call"], row["slice"]

pairs = 0
summaries = []
for control, probe in (("control1", "probe1"), ("control2", "probe2")):
    for step in (0, 20, 50, 100):
        c = (root / f"real14hREAL-{control}" / f"checkpoint-{step}.json").read_bytes()
        p = (root / f"real14hREAL-{probe}" / f"checkpoint-{step}.json").read_bytes()
        assert c == p, (control, probe, step)
        json.loads(c)
        pairs += 1
    lines = (root / f"real14hREAL-{probe}.log").read_text().splitlines()
    original = records(lines, "PR57_305_BODY")
    shadow = records(lines, "PR57_306_SHADOW")
    assert len(original) == 528, (probe, len(original))
    assert len(shadow) > 0, probe
    expected = Counter()
    for row in original:
        expected[key(row)] += int(row["visited"])
    actual = Counter()
    for row in shadow:
        assert int(row["vehicles"]) == 98, row
        assert int(row["reads"]) > 0, row
        assert int(row["stale"]) <= int(row["reads"]), row
        assert int(row["staleAfterProcessed"]) <= int(row["stale"]), row
        assert int(row["missing"]) <= int(row["reads"]), row
        assert int(row["refreshes"]) <= int(row["vehicles"]), row
        assert all(float(row[field]) >= 0 for field in
                   ("buildMs", "refreshMs", "readMs", "auditMs")), row
        actual[key(row)] += int(row["reads"])
    assert expected == actual, (probe, expected - actual, actual - expected)
    summary = {
        "arm": probe, "decisions": len(original),
        "candidateReads": sum(int(x["reads"]) for x in shadow),
        "shadowSlices": len(shadow),
        "stale": sum(int(x["stale"]) for x in shadow),
        "staleAfterProcessed": sum(int(x["staleAfterProcessed"]) for x in shadow),
        "missing": sum(int(x["missing"]) for x in shadow),
        "postSync": sum(int(x["postSync"]) for x in shadow),
        "refreshes": sum(int(x["refreshes"]) for x in shadow),
        "buildMs": round(sum(float(x["buildMs"]) for x in shadow), 3),
        "refreshMs": round(sum(float(x["refreshMs"]) for x in shadow), 3),
        "readMs": round(sum(float(x["readMs"]) for x in shadow), 3),
        "auditMs": round(sum(float(x["auditMs"]) for x in shadow), 3),
    }
    print("PR57_306_AUDIT " + json.dumps(summary, sort_keys=True), flush=True)
    summaries.append({k: v for k, v in summary.items() if k not in
                      ("arm", "buildMs", "refreshMs", "readMs", "auditMs")})
assert pairs == 8 and summaries[0] == summaries[1], summaries
print("PR57_306_VERIFIED checkpointPairs=8/8 processes=4 allAdvance2=True "
      "candidateReadParity=True projectionNotCausalClosure=True", flush=True)
