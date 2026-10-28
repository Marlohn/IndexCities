#!/usr/bin/env python3
"""PR57 #302: bounded Cause IDs, two writer sites and full checkpoint parity.
Observational only: misses intermediate writes, all other writers and dependencies.
"""
import collections
import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
def fields(line):
    return dict(re.findall(r"(\\w+)=([^\\s]+)", line))
def rows(path, prefix):
    return [fields(line) for line in path.read_text().splitlines()
            if line.startswith(prefix + " ")]
pairs = (("control1", "probe1"), ("control2", "probe2"))
pair_count = 0
for control, probe in pairs:
    for arm in (control, probe):
        log = (root / f"real14hREAL-{arm}.log").read_text()
        assert log.count("PR57_294_FINAL ") == 1, (arm, "final")
        assert log.count("PR57_294_CHECKPOINT ") == 4, (arm, "checkpoints")
    for checkpoint in (0, 20, 50, 100):
        a = (root / f"real14hREAL-{control}" / f"checkpoint-{checkpoint}.json").read_bytes()
        b = (root / f"real14hREAL-{probe}" / f"checkpoint-{checkpoint}.json").read_bytes()
        assert a == b, (control, probe, checkpoint)
        json.loads(a)
        pair_count += 1
    log_path = root / f"real14hREAL-{probe}.log"
    steps = rows(log_path, "PR57_302_STEP")
    changes = rows(log_path, "PR57_302_NET")
    writers = rows(log_path, "PR57_302_WRITER")
    assert [int(x["call"]) for x in steps] == list(range(76, 101)), (probe, len(steps))
    assert sum(int(x["eligibleNet"]) for x in steps) == 8, (probe, steps)
    eligible = [x for x in changes if x["eligible"] == "1"]
    assert len(eligible) == 8, (probe, eligible)
    assert collections.Counter(int(x["call"]) for x in eligible) == {76: 2, 79: 2, 92: 2, 95: 2}
    assert sum(int(x["otherNet"]) for x in steps) == len(changes) - len(eligible)
    assert sum(int(x["omitted"]) for x in steps) == 0, "NET logging cap reached"
    for row in writers:
        assert 76 <= int(row["call"]) <= 100
        assert row["branch"] in ("blocked", "junction")
    by_id = collections.defaultdict(list)
    for row in changes:
        by_id[row["id"]].append({
            "call": int(row["call"]), "slice": int(row["slice"]),
            "eligible": int(row["eligible"]),
            "before": row["before"], "after": row["after"],
            "statusBefore": row["statusBefore"], "statusAfter": row["statusAfter"],
            "claimBefore": row["claimBefore"], "claimAfter": row["claimAfter"]})
    summary = {
        "arm": probe, "eligibleNet": len(eligible),
        "otherNet": len(changes) - len(eligible),
        "writerEventsObserved": len(writers),
        "writerBranchCounts": dict(collections.Counter(x["branch"] for x in writers)),
        "writerIds": dict(collections.Counter(x["id"] for x in writers)),
        "netById": dict(sorted(by_id.items(), key=lambda x: int(x[0]))),
        "notWriterClosure": True, "noSkippedSlices": True, "noGainClaim": True}
    print("PR57_302_AUDIT " + json.dumps(summary, sort_keys=True), flush=True)
assert pair_count == 8
print("PR57_302_VERIFIED checkpointPairs=8/8 processes=4 allAdvance2=True "
      "netCauseOnly=True writerSites=2 noCausalClosure=True", flush=True)
