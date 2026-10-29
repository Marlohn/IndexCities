#!/usr/bin/env python3
"""Independent offline audit of the authentic PR57 #329 Actions ZIP (stdlib only)."""
from collections import defaultdict
from hashlib import sha256
from pathlib import Path
import re
import sys
from zipfile import ZipFile

EXPECTED_SHA = "d251fffee35172407ca5b7e74f21bcbe2c8d4637f4f987369aa50c019ee2d528"
KEYS = (
    "bodyCalls", "visits", "eligibleVisits", "allEdges", "eligibleEdges",
    "readTransitions", "actorTransitions", "syncTransitions",
    "changedCandidates", "poseChanges", "statusChanges", "towChanges",
    "shapeChanges", "dirtyAllEdges", "dirtyEligibleEdges",
)

def fields(line):
    return dict(re.findall(r"([A-Za-z][A-Za-z0-9]*)=([^\s]+)", line))

def main(path):
    payload = Path(path).read_bytes()
    digest = sha256(payload).hexdigest()
    assert digest == EXPECTED_SHA, ("ZIP SHA-256 mismatch", digest)
    pairs = 0
    with ZipFile(path) as z:
        for case, calls, slices in (
            ("real14hREAL", 100, 150),
            ("gridARTIFICIAL", 50, 5),
        ):
            results = {}
            for arm in ("control1", "probe1", "probe2", "control2"):
                lines = z.read(f"{case}-{arm}.log").decode("utf-8").splitlines()
                finals = [fields(s) for s in lines if s.startswith("PR57_294_FINAL ")]
                assert len(finals) == 1, (case, arm, "final count")
                final = finals[0]
                assert final["cadence"] == "Advance2"
                assert final["all13Phases"] == "True"
                assert final["noSkippedSlices"] == "True"
                assert final["exactCheckpoints"] == "True"
                assert int(final["calls"]) == calls
                ordinals = [
                    int(fields(s)["call"])
                    for s in lines if s.startswith("PR57_294_STEP ")
                ]
                assert ordinals == list(range(1, calls + 1))
                rows = [
                    fields(s) for s in lines if s.startswith("PR57_329_EXPOSURE ")
                ]
                assert len(rows) == (0 if arm.startswith("control") else slices)
                if rows:
                    assert all(
                        r["observational"] == "True"
                        and r["noCausalClosure"] == "True"
                        for r in rows
                    )
                    expected_calls = range(76, 101) if case == "real14hREAL" else range(46, 51)
                    assert set(int(r["call"]) for r in rows) == set(expected_calls)
                results[arm] = {
                    "totals": {k: sum(int(r[k]) for r in rows) for k in KEYS},
                    "rows": rows,
                }
            for i in (1, 2):
                for checkpoint in (0, 20, calls // 2, calls):
                    a = z.read(f"{case}-control{i}/checkpoint-{checkpoint}.json")
                    b = z.read(f"{case}-probe{i}/checkpoint-{checkpoint}.json")
                    assert a == b, (case, i, checkpoint, "full JSON differs")
                    pairs += 1
            assert results["probe1"]["totals"] == results["probe2"]["totals"], case
            for arm in ("probe1", "probe2"):
                totals = results[arm]["totals"]
                print(
                    f"PR57_329_OFFLINE case={case} arm={arm} slices={slices} "
                    + " ".join(f"{k}={totals[k]}" for k in KEYS)
                )
            if case == "gridARTIFICIAL":
                for row in results["probe1"]["rows"]:
                    print(
                        f"PR57_329_GRID_CALL call={row['call']} "
                        f"actorTransitions={row['actorTransitions']} "
                        f"readTransitions={row['readTransitions']} "
                        f"dirtyAllEdges={row['dirtyAllEdges']} "
                        f"dirtyEligibleEdges={row['dirtyEligibleEdges']}"
                    )
    assert pairs == 16
    print(f"PR57_329_OFFLINE_VERIFIED sha256={digest} checkpointPairs={pairs}/16")

if __name__ == "__main__":
    assert len(sys.argv) == 2, "usage: python3 scripts/pr57-329-audit.py pr57-329.zip"
    main(sys.argv[1])
