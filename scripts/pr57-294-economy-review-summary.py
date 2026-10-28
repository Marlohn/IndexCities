#!/usr/bin/env python3
"""PR57 #294: verify authentic CI ZIP; scheduled review versus recurring phase.

Observational StepCostProfile phase includes clock/JIT/measurement overhead.
Not an isolated ReviewMarkets benchmark or an accepted speedup.
"""
import argparse
import hashlib
import json
import re
import statistics
import zipfile

SHA256 = "f6b75dea8dde5ebac8bdfd9de1d6eb972452eca5fb36aa5bef85d46f0eeb0302"
CASES = {"worksREAL": 100, "queueREAL": 100, "real14hREAL": 100, "gridARTIFICIAL": 50}


def kv(line):
    return dict(re.findall(r"(\\w+)=([^\\s]+)", line))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifact_zip")
    args = parser.parse_args()
    with open(args.artifact_zip, "rb") as source:
        digest = hashlib.file_digest(source, "sha256").hexdigest()
    assert digest == SHA256, digest
    parity = 0
    with zipfile.ZipFile(args.artifact_zip) as z:
        for case, calls in CASES.items():
            for at in sorted({0, 20, calls // 2, calls}):
                left = z.read(f"{case}-control/checkpoint-{at}.json")
                right = z.read(f"{case}-probe/checkpoint-{at}.json")
                assert left == right, (case, at)
                json.loads(left)
                parity += 1
            for mode in ("control", "probe"):
                lines = z.read(f"{case}-{mode}.log").decode().splitlines()
                finals = [kv(x) for x in lines if x.startswith("PR57_294_FINAL ")]
                assert len(finals) == 1 and int(finals[0]["calls"]) == calls
                assert sum(x.startswith("PR57_294_CHECKPOINT ") for x in lines) == 4
                assert sum(x.startswith("PR57_294_STEP ") for x in lines) == (calls if mode == "probe" else 0)
                if mode != "probe":
                    continue
                rows = [kv(x) for x in lines if x.startswith("PR57_294_STEP ")]
                event = [x for x in rows if x["reviewScheduleChanged"] == "True"]
                regular = [x for x in rows if x["reviewScheduleChanged"] == "False"]
                assert len(event) == (0 if case == "worksREAL" else 1)
                assert all(int(x["completedDelta"]) == 0 for x in rows)
                if event:
                    assert int(event[0]["call"]) == (1 if case == "gridARTIFICIAL" else 4)
                    assert event[0]["dueBefore"] == "True"
                event_ms = sum(float(x["economyMs"]) for x in event)
                regular_ms = sum(float(x["economyMs"]) for x in regular)
                assert abs(event_ms + regular_ms - float(finals[0]["totalEconomyPhaseMs"])) < .001
                vehicle_ms = [float(x["vehiclesMs"]) for x in rows]
                print(json.dumps({
                    "case": case, "calls": calls, "scheduledReviewCalls": len(event),
                    "reviewPhaseMs": round(event_ms, 6),
                    "nonReviewPhaseMs": round(regular_ms, 6),
                    "nonReviewMedianMs": round(statistics.median(float(x["economyMs"]) for x in regular), 6),
                    "scheduledShareOfEconomyPhasePct": round(100 * event_ms / (event_ms + regular_ms), 4),
                    "vehicleFirst20Ms": round(sum(vehicle_ms[:20]), 3),
                    "vehicleRemainingMs": round(sum(vehicle_ms[20:]), 3),
                }, sort_keys=True))
    assert parity == 16
    print(f"PR57_294_VERIFIED sha256={digest} parity=16/16 processes=8 cadence=Advance2")


if __name__ == "__main__":
    main()
