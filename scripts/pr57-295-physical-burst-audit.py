#!/usr/bin/env python3
"""PR57 #295: offline audit of physical bursts in authentic #294 Advance(2) replay.

Read-only ZIP analysis; no engine execution. Burst threshold (5x late-ten
median) is descriptive, never a scheduler rule. Advance allocations include
all phases; checkpoints miss transient writes.
"""
import argparse
import collections
import hashlib
import json
import re
import statistics
import zipfile

EXPECTED_SHA256 = "f6b75dea8dde5ebac8bdfd9de1d6eb972452eca5fb36aa5bef85d46f0eeb0302"
CASES = {"worksREAL": 100, "queueREAL": 100, "real14hREAL": 100, "gridARTIFICIAL": 50}


def kv(line):
    return dict(re.findall(r"(\w+)=([^\s]+)", line))


def snapshot(z, case, at):
    raw = z.read(f"{case}-control/checkpoint-{at}.json")
    assert raw == z.read(f"{case}-probe/checkpoint-{at}.json"), (case, at)
    return json.loads(raw)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifact_zip")
    args = parser.parse_args()
    with open(args.artifact_zip, "rb") as f:
        digest = hashlib.file_digest(f, "sha256").hexdigest()
    assert digest == EXPECTED_SHA256, f"unexpected artifact: {digest}"
    with zipfile.ZipFile(args.artifact_zip) as z:
        for case, calls in CASES.items():
            probe = [kv(line) for line in z.read(f"{case}-probe.log").decode().splitlines()
                     if line.startswith("PR57_294_STEP ")]
            control = z.read(f"{case}-control.log").decode()
            assert len(probe) == calls and "PR57_294_FINAL " in control
            assert [int(row["call"]) for row in probe] == list(range(1, calls + 1))
            vehicles = [float(row["vehiclesMs"]) for row in probe]
            wall = [float(row["advanceWallMs"]) for row in probe]
            allocations = [int(row["advanceAllocatedBytes"]) for row in probe]
            late_median = statistics.median(vehicles[-10:])
            threshold = 5 * late_median
            bursts = [i for i, value in enumerate(vehicles) if value > threshold]
            checkpoints = sorted({0, 20, calls // 2, calls})
            states = {at: snapshot(z, case, at) for at in checkpoints}
            status = {
                str(at): dict(sorted(collections.Counter(
                    v["Status"] for v in states[at]["Vehicles"]).items()))
                for at in checkpoints
            }
            movement = {}
            for earlier, later in zip(checkpoints, checkpoints[1:]):
                before = {v["Id"]: v for v in states[earlier]["Vehicles"]}
                after = states[later]["Vehicles"]
                movement[f"{earlier}-{later}"] = {
                    "vehiclePositionChanged": sum(
                        v["Position"] != before[v["Id"]]["Position"]
                        for v in after if v["Id"] in before),
                    "vehicleStatusChanged": sum(
                        v["Status"] != before[v["Id"]]["Status"]
                        for v in after if v["Id"] in before),
                }
            print(json.dumps({
                "case": case, "calls": calls, "vehicles": len(states[0]["Vehicles"]),
                "late10VehicleMedianMs": round(late_median, 6),
                "burstThresholdMs": round(threshold, 6),
                "burstCalls": [i + 1 for i in bursts],
                "burstVehicleSharePct": round(
                    100 * sum(vehicles[i] for i in bursts) / sum(vehicles), 4),
                "burstAdvanceWallSharePct": round(
                    100 * sum(wall[i] for i in bursts) / sum(wall), 4),
                "burstAdvanceAllocSharePct": round(
                    100 * sum(allocations[i] for i in bursts) / sum(allocations), 4),
                "vehicleFirst20Ms": round(sum(vehicles[:20]), 3),
                "vehicleAfter20Ms": round(sum(vehicles[20:]), 3),
                "vehicleMedianFirst20Ms": round(statistics.median(vehicles[:20]), 3),
                "vehicleMedianAfter20Ms": round(statistics.median(vehicles[20:]), 3),
                "late10AdvanceWallMeanMs": round(statistics.mean(wall[-10:]), 3),
                "vehicleAllocPearsonExcludingFirstCall": round(
                    statistics.correlation(vehicles[1:], allocations[1:]), 4),
                "vehicleStatuses": status, "checkpointNetChanges": movement,
            }, sort_keys=True))
    print(f"PR57_295_VERIFIED sha256={digest} cases=4 checkpointPairs=16/16 noMotorExecution=True")


if __name__ == "__main__":
    main()
