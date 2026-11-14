#!/usr/bin/env python3
"""PR57 #305: audit canonical BodyOnWay candidate-prefix census against #304.
Counters describe the evaluated prefix, not all possible invalidators.
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
    return (row["call"], row["slice"], row["id"])

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
    actual = records(lines, "PR57_305_BODY")
    selected = records(lines, "PR57_304_BODY")
    exits = records(lines, "PR57_303_RETURN")
    assert len(exits) == 600, (probe, len(exits))
    assert len(selected) == 528, (probe, len(selected))
    assert len(actual) == len(selected), (probe, len(actual), len(selected))
    assert Counter((key(x), x["selected"]) for x in actual) == Counter(
        (key(x), x["blocker"]) for x in selected), "selected blockers differ"
    assert all(0 < int(x["visited"]) <= int(x["vehicles"]) and
               0 < int(x["eligible"]) <= int(x["visited"]) and
               0 <= int(x["nearNegative"]) <= int(x["eligible"]) and
               int(x["geometry"]) > 0 and int(x["ticks"]) > 0 for x in actual)
    by_reader = {}
    for vid in sorted({x["id"] for x in actual}):
        rows = [x for x in actual if x["id"] == vid]
        by_reader[vid] = {
            "calls": len(rows),
            "visited": sum(int(x["visited"]) for x in rows),
            "eligible": sum(int(x["eligible"]) for x in rows),
            "nearNegative": sum(int(x["nearNegative"]) for x in rows),
            "geometry": sum(int(x["geometry"]) for x in rows),
            "ticks": sum(int(x["ticks"]) for x in rows),
            "minVisited": min(int(x["visited"]) for x in rows),
            "maxVisited": max(int(x["visited"]) for x in rows),
            "totalVehicles": sorted({int(x["vehicles"]) for x in rows}),
        }
    normalized = {k: {x: y for x, y in v.items() if x != "ticks"}
                  for k, v in by_reader.items()}
    print("PR57_305_AUDIT " + json.dumps({
        "arm": probe, "byReader": by_reader,
        "total": len(actual),
        "notCausalClosure": True, "noSkippedSlices": True, "noGainClaim": True
    }, sort_keys=True), flush=True)
    summaries.append(normalized)
assert pairs == 8 and summaries[0] == summaries[1], "probe census differs"
print("PR57_305_VERIFIED checkpointPairs=8/8 processes=4 allAdvance2=True "
      "prefixCensus=1056 noCausalClosure=True", flush=True)
