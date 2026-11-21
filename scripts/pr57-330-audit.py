#!/usr/bin/env python3
"""PR57 #330: audit same-run actor-major versus actor-local observational shadows.

No equivalence of a future cache is asserted. Both arms execute canonical
Advance(2), and complete checkpoints must match byte for byte.
"""
from pathlib import Path
import re
import statistics
import sys

FIELDS = (
    "bodyCalls", "visits", "eligibleVisits", "allEdges", "eligibleEdges",
    "readTransitions", "actorTransitions", "syncTransitions",
    "changedCandidates", "poseChanges", "statusChanges", "towChanges",
    "shapeChanges", "dirtyAllEdges", "dirtyEligibleEdges",
)
READS = FIELDS[:5]
ARMS = ("scan1", "local1", "local2", "scan2")

def parse(line):
    return dict(re.findall(r"([A-Za-z][A-Za-z0-9]*)=([^\s]+)", line))

def main(root):
    root = Path(root)
    paired = 0
    for case, calls, slices in (("real14hREAL", 100, 150), ("gridARTIFICIAL", 50, 5)):
        final, totals, modes = {}, {}, {}
        for arm in ARMS:
            lines = (root / f"{case}-{arm}.log").read_text().splitlines()
            f = [parse(x) for x in lines if x.startswith("PR57_294_FINAL ")]
            assert len(f) == 1, (case, arm, "final")
            final[arm] = f[0]
            assert f[0]["cadence"] == "Advance2"
            assert f[0]["all13Phases"] == f[0]["noSkippedSlices"] == "True"
            assert f[0]["exactCheckpoints"] == "True"
            assert int(f[0]["calls"]) == calls
            steps = [parse(x) for x in lines if x.startswith("PR57_294_STEP ")]
            assert [int(x["call"]) for x in steps] == list(range(1, calls + 1))
            rows = [parse(x) for x in lines if x.startswith("PR57_329_EXPOSURE ")]
            mode = [parse(x) for x in lines if x.startswith("PR57_330_MODE ")]
            assert len(rows) == len(mode) == slices, (case, arm, len(rows), len(mode))
            assert [(x["call"], x["slice"]) for x in rows] == [
                (x["call"], x["slice"]) for x in mode
            ]
            expected = "ACTOR_LOCAL" if arm.startswith("local") else "ACTOR_MAJOR"
            assert all(x["mode"] == expected for x in mode), (case, arm, "wrong mode")
            assert all(x["observational"] == x["noCausalClosure"] == "True" for x in rows)
            assert set(int(x["call"]) for x in rows) == set(
                range(76, 101) if case == "real14hREAL" else range(46, 51)
            )
            if arm.startswith("local"):
                assert sum(int(x["actorChecks"]) for x in mode) > 0
            else:
                assert all(int(x["actorChecks"]) == 0 for x in mode)
            totals[arm] = {k: sum(int(x[k]) for x in rows) for k in FIELDS}
            totals[arm].update({
                "boundaryMs": sum(float(x["boundaryMs"]) for x in rows),
                "visitMs": sum(float(x["visitMs"]) for x in rows),
            })
            modes[arm] = {
                k: sum(int(x[k]) for x in mode)
                for k in ("actorChecks", "nonlocalCalls", "nonlocalChanges")
            }
        for i in (1, 2):
            for n in (0, 20, calls // 2, calls):
                a = (root / f"{case}-scan{i}" / f"checkpoint-{n}.json").read_bytes()
                b = (root / f"{case}-local{i}" / f"checkpoint-{n}.json").read_bytes()
                assert a == b, (case, i, n, "full checkpoint mismatch")
                paired += 1
        for first, second in (("scan1", "scan2"), ("local1", "local2")):
            assert all(totals[first][k] == totals[second][k] for k in FIELDS), (
                case, first, second, "non-deterministic counters"
            )
        assert all(totals["scan1"][k] == totals["local1"][k] for k in READS), (
            case, "canonical read visits differ"
        )
        for arm in ARMS:
            print("PR57_330_CENSUS", f"case={case}", f"arm={arm}",
                  *(f"{k}={totals[arm][k]}" for k in FIELDS),
                  *(f"{k}={totals[arm][k]:.6f}" for k in ("visitMs", "boundaryMs")),
                  *(f"{k}={modes[arm][k]}" for k in modes[arm]))
        for key in FIELDS[5:]:
            scan = totals["scan1"][key]
            local = totals["local1"][key]
            print(f"PR57_330_COMPARISON case={case} metric={key} "
                  f"actorMajor={scan} actorLocal={local} equal={scan == local}")
        for metric in ("wallMs", "cpuMs", "allocatedBytes"):
            scan = statistics.mean(float(final[f"scan{i}"][metric]) for i in (1, 2))
            local = statistics.mean(float(final[f"local{i}"][metric]) for i in (1, 2))
            print(f"PR57_330_FULL case={case} metric={metric} "
                  f"actorMajor={scan:.3f} actorLocal={local:.3f} "
                  "intrusive=True notIntegratedSpeedup=True")
        print(f"PR57_330_PARITY case={case} checkpointPairs=8/8")
    assert paired == 16
    print("PR57_330_VERIFIED checkpointPairs=16/16 processes=8 "
          "cadence=Advance2 all13Phases=True noSkippedSlices=True")

if __name__ == "__main__":
    assert len(sys.argv) == 2, "usage: python3 scripts/pr57-330-audit.py artifacts/physical-330"
    main(sys.argv[1])
