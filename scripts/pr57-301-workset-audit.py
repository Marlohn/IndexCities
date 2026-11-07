#!/usr/bin/env python3
"""PR57 #301: pair #300 boundary counts with #187 partial vehicle-state deltas.
Net unchanged fields are NOT proof of causal independence or writer closure.
"""
import argparse
import json
import re
from pathlib import Path

CASES = {"gridARTIFICIAL": (50, 12), "real14hREAL": (100, 100)}

def fields(line):
    return dict(re.findall(r"(\w+)=([^\s]+)", line))

def inspect_log(path, calls, expected_batched):
    records = {}
    current = None
    batch_count = 0
    for line in path.read_text().splitlines():
        if line.startswith("PR57_300_STEP "):
            row = fields(line)
            call = int(row["call"])
            assert call == len(records) + 1, (path, call)
            assert int(row["changedSlices"]) + int(row["unchangedSlices"]) == int(row["boundaries"])
            records[call] = {"boundary": row, "workset": None}
            current = call
        elif line.startswith("PR57_187_BATCH "):
            assert current is not None and records[current]["workset"] is None, (path, current)
            row = fields(line)
            assert int(row["slices"]) == int(records[current]["boundary"]["boundaries"]), (path, current)
            records[current]["workset"] = row
            batch_count += 1
    assert list(records) == list(range(1, calls + 1)), (path, len(records))
    assert sum(int(v["boundary"]["batched"]) for v in records.values()) == expected_batched
    assert batch_count == expected_batched, (path, batch_count)
    assert all((int(v["boundary"]["batched"]) == 1) == (v["workset"] is not None)
               for v in records.values())
    return records

def summarize(case, arm, records):
    total = {key: 0 for key in ("boundaries", "unchangedSlices", "changedSlices", "changedTouches",
                                "partialChanged", "otherChanged", "causeChanged", "waitingChanged",
                                "claimChanged", "moved", "actualAttempts", "observerMs", "movementMs")}
    final = {key: 0 for key in total}
    final_from = 76 if case == "real14hREAL" else 26
    for call, value in records.items():
        b, w = value["boundary"], value["workset"]
        metrics = {key: int(b[key]) for key in ("boundaries", "unchangedSlices", "changedSlices", "changedTouches")}
        if w:
            for key in ("partialChanged", "otherChanged", "causeChanged", "waitingChanged",
                        "claimChanged", "moved", "actualAttempts"):
                metrics[key] = int(w[key])
            for key in ("observerMs", "movementMs"):
                metrics[key] = float(w[key])
        for key, number in metrics.items():
            total[key] += number
            if call >= final_from:
                final[key] += number
    return {"case": case, "arm": arm, "all": total, "finalWindow": final,
            "finalFrom": final_from, "finalTo": len(records),
            "netOnly": True, "writerClosureProven": False, "causalSkipAllowed": False}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    root = parser.parse_args().directory
    parity = 0
    for case, (calls, batched) in CASES.items():
        for control, probe in (("control1", "probe1"), ("control2", "probe2")):
            control_log = (root / f"{case}-{control}.log").read_text()
            assert "PR57_300_STEP " not in control_log and "PR57_187_BATCH " not in control_log
            for arm in (control, probe):
                log = (root / f"{case}-{arm}.log").read_text()
                assert log.count("PR57_294_FINAL ") == 1, (case, arm)
                assert log.count("PR57_294_CHECKPOINT ") == 4, (case, arm)
            for at in sorted({0, 20, calls // 2, calls}):
                a = (root / f"{case}-{control}" / f"checkpoint-{at}.json").read_bytes()
                b = (root / f"{case}-{probe}" / f"checkpoint-{at}.json").read_bytes()
                assert a == b, (case, control, probe, at)
                json.loads(a)
                parity += 1
            result = summarize(case, probe, inspect_log(root / f"{case}-{probe}.log", calls, batched))
            print("PR57_301_CENSUS " + json.dumps(result, sort_keys=True), flush=True)
    assert parity == 16
    print("PR57_301_VERIFIED checkpointPairs=16/16 processes=8 allAdvance2=True "
          "netFieldsOnly=True canonicalWritersNotProven=True noGainClaim=True", flush=True)

if __name__ == "__main__":
    main()
