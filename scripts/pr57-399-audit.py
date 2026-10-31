#!/usr/bin/env python3
"""Audit ORIGINAL PR57 #397/#398/#399 Action ZIPs, without rerunning engine.

Usage: python3 scripts/pr57-399-audit.py 397.zip 398.zip 399.zip
Checks byte-identical REAL saves, exact writer-method witnesses, and physical
admission release events. Not a causal-closure or throughput certificate.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ZIP_SHA = {
    397: "2b089dd0607dc1e10197bcec67f3460f3877e9aae7937aede32b50b85ab4b971",
    398: "9dd19c8d7a14420b87e209b512d698878c389c4844f185ff8e6f69eb9388be07",
    399: "c450d8126e9aaf4b0f2486f6dd303cbb03c3f8d9e66725f33b68dda27df911bf",
}
SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
MEMBERS = {"base/checkpoint-10800.json", "probe/checkpoint-10800.json",
           "base/full.log", "probe/full.log", "summary.json", "checkpoints.sha256"}

def rows(lines, prefix):
    return [dict(v.split("=", 1) for v in line.split()[1:] if "=" in v)
            for line in lines if line.startswith(prefix + " ")]

def load(path, experiment):
    raw = Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == ZIP_SHA[experiment], experiment
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == MEMBERS, experiment
        base, probe = [z.read(f"{arm}/checkpoint-10800.json") for arm in ("base", "probe")]
        assert base == probe and len(base) == 2254838
        assert hashlib.sha256(base).hexdigest() == SAVE_SHA
        logs = {arm: z.read(f"{arm}/full.log").decode().splitlines()
                for arm in ("base", "probe")}
        hours, metrics = {}, {}
        for arm, lines in logs.items():
            hours[arm] = rows(lines, "PR57_337_HOUR")
            advance = rows(lines, "PR57_ADVANCE_BOUNDARY")
            final = rows(lines, "PR57_337_FINAL")
            assert len(hours[arm]) == 6 and len(advance) == 108 and len(final) == 1
            assert int(advance[-1]["calls"]) == 10800 and final[0]["parity"] == "IDENTICAL"
            assert all(int(h["living"]) == 121 for h in hours[arm])
            metrics[arm] = {
                "wallMs": round(sum(float(h["wallMs"]) for h in hours[arm]), 3),
                "cpuMs": round(sum(float(h["cpuMs"]) for h in hours[arm]), 3),
                "allocatedBytes": sum(int(h["totalAllocatedBytes"]) for h in hours[arm])
            }
        for a, b in zip(hours["base"], hours["probe"]):
            for key in ("living", "ledger", "history", "slices", "actors",
                        "claimChanges", "spaceCountChanges"):
                assert a[key] == b[key], (experiment, key)
        return logs, metrics

def main(paths):
    logs, metrics = {}, {}
    for experiment, path in zip((397, 398, 399), paths):
        logs[experiment], metrics[experiment] = load(path, experiment)
    cleanup397 = rows(logs[397]["probe"], "PR57_397_CHANGE")
    cleanup397 = {(int(x["physical"]), int(x["vehicle"])) for x in cleanup397
                  if x["class"] == "0" and x["admission"] == "True"}
    releases399 = rows(logs[399]["probe"], "PR57_399_RELEASE")
    release_pairs = {(int(x["physical"]), int(x["vehicle"])) for x in releases399}
    assert len(cleanup397) == len(release_pairs) == 14 and cleanup397 == release_pairs
    assert all(x["directAdmissionNull"] == "True" for x in releases399)
    changes398 = rows(logs[398]["probe"], "PR57_398_CHANGE")
    cross398 = {(x["step"], x["physical"], x["phase"], x["vehicle"])
                for x in changes398 if x["phase"] in ("2", "11")}
    changes399 = rows(logs[399]["probe"], "PR57_399_ROUTE")
    cross399 = {(x["step"], x["physical"], x["phase"], x["vehicle"]) for x in changes399}
    assert len(cross398) == len(cross399) == 10 and cross398 == cross399
    assert sum(x["phase"] == "2" and x["method"] == "TryDrive" for x in changes399) == 8
    assert sum(x["phase"] == "11" and x["method"] == "RestockBusinesses" for x in changes399) == 2
    assert all(x["method"] in ("TryDrive", "RestockBusinesses") for x in changes399)
    methods = rows(logs[399]["probe"], "PR57_399_METHODS")
    cleanup = rows(logs[399]["probe"], "PR57_399_CLEANUP")
    assert len(methods) == len(cleanup) == 1
    m, c = methods[0], cleanup[0]
    assert (int(m["steps"]), int(m["physical"]), int(m["targets"])) == (1800, 10818, 11)
    assert (int(m["tryDriveChanges"]), int(m["routeChanges"])) == (8, 10)
    assert m["methods"] == "RestockBusinesses:2,TryDrive:8"
    assert int(c["physical"]) == 12000 and int(c["directAdmissionClears"]) == 14
    assert all(x["observational"] == "True" and x["noSkippedSlices"] == "True" for x in (m, c))
    assert not rows(logs[399]["base"], "PR57_399_ROUTE")
    assert not rows(logs[399]["base"], "PR57_399_RELEASE")
    print(json.dumps({
        "zipSha256": ZIP_SHA, "crc": "PASS", "saveSha256": SAVE_SHA,
        "saveBytes": 2254838, "advance2PerArm": 10800,
        "cleanupReleaseMatches397": len(release_pairs),
        "crossPhaseRouteMatches398": len(cross399),
        "citizenTryDrive": 8, "marketRestockBusinesses": 2,
        "methodWindowSteps": 1800, "physicalCleanupWindow": 12000,
        "methodCaptureTicks": int(m["ticks"]), "frequency": int(m["frequency"]),
        "metrics": metrics, "causalClosure": False, "optimizationGainAccepted": False
    }, sort_keys=True))

if __name__ == "__main__":
    assert len(sys.argv) == 4, "usage: pr57-399-audit.py 397.zip 398.zip 399.zip"
    main(sys.argv[1:])
