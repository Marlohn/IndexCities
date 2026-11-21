#!/usr/bin/env python3
"""PR57 #322: independent offline audit of #321 per-Advance(2) phase regimes.

Usage: python3 pr57-322-phase-regimes.py pr57-321.zip
Only reads the authentic Actions artifact; never runs/changes the simulation.
"""
import hashlib
import re
import statistics
import sys
import zipfile
from pathlib import Path

EXPECTED = "146786e9d8c3f1fac0ceddb41f0282e49b0b2347381825399ff5dd5d0b90352a"
CASES = {"real14hREAL": 100, "gridARTIFICIAL": 50}
ARMS = ("control1", "shared1", "shared2", "control2")
METRICS = ("advanceWallMs", "vehiclesMs", "citizensMs", "economyMs", "advanceAllocatedBytes")


def fields(line):
    return dict(re.findall(r"([A-Za-z][A-Za-z0-9]*)=([^\s]+)", line))


def mean_window(rows, start, end, metric):
    return statistics.mean(sum(float(r[metric]) for r in run[start - 1:end]) for run in rows)


def main(path):
    blob = Path(path).read_bytes()
    digest = hashlib.sha256(blob).hexdigest()
    assert digest == EXPECTED, f"Unexpected ZIP SHA-256 {digest}"
    pairs = 0
    with zipfile.ZipFile(path) as z:
        for case, calls in CASES.items():
            logs = {}
            for arm in ARMS:
                lines = z.read(f"{case}-{arm}.log").decode().splitlines()
                steps = [fields(s) for s in lines if s.startswith("PR57_294_STEP ")]
                final = [fields(s) for s in lines if s.startswith("PR57_294_FINAL ")]
                assert len(final) == 1 and len(steps) == calls, (case, arm, "cadence")
                assert [int(s["call"]) for s in steps] == list(range(1, calls + 1))
                assert final[0]["cadence"] == "Advance2" and final[0]["all13Phases"] == "True"
                assert final[0]["noSkippedSlices"] == "True" and final[0]["exactCheckpoints"] == "True"
                logs[arm] = steps
            for n in (1, 2):
                for step in (0, 20, calls // 2, calls):
                    a = z.read(f"{case}-control{n}/checkpoint-{step}.json")
                    b = z.read(f"{case}-shared{n}/checkpoint-{step}.json")
                    assert a == b, (case, n, step)
                    pairs += 1
            controls = [logs["control1"], logs["control2"]]
            windows = ([(1, 1), (2, 2), (3, 25), (26, 50), (51, 75), (76, 100)]
                       if calls == 100 else [(1, 1), (2, 2), (3, 10), (11, 25), (26, 50)])
            print(f"CASE {case} calls={calls} controls=2 baseline=COMPOSED_FLAGS")
            for start, end in windows:
                v = {metric: mean_window(controls, start, end, metric) for metric in METRICS}
                wall = v["advanceWallMs"]
                vehicle = v["vehiclesMs"]
                citizen = v["citizensMs"]
                economy = v["economyMs"]
                assert wall > vehicle and wall > vehicle + citizen + economy
                print(f"  {start:3}-{end:<3} wallMs={wall:9.3f} vehiclesPct={vehicle/wall*100:6.2f} "
                      f"citizensPct={citizen/wall*100:6.2f} economyPct={economy/wall*100:6.2f} "
                      f"remainingMs={wall-vehicle:9.3f} "
                      f"zeroVehiclesUpperX={wall/(wall-vehicle):6.2f} "
                      f"allocatedMB={v['advanceAllocatedBytes']/1e6:8.2f}")
            for call in (1, 2, 23, 32) if calls == 50 else (1, 4, 19):
                i = call - 1
                vals = {m: statistics.mean(float(run[i][m]) for run in controls) for m in METRICS}
                print(f"  spike call={call} citizensMs={vals['citizensMs']:.3f} "
                      f"vehiclesMs={vals['vehiclesMs']:.3f} "
                      f"economyMs={vals['economyMs']:.3f} "
                      f"allocatedMB={vals['advanceAllocatedBytes']/1e6:.3f}")
    assert pairs == 16, pairs
    print(f"VERIFIED sha256={digest} checkpointPairs={pairs}/16 controls=4 "
          "probeArms=4 noSkippedSlices=True")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: pr57-322-phase-regimes.py <pr57-321.zip>")
    main(sys.argv[1])
