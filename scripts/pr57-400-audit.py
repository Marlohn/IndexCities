#!/usr/bin/env python3
"""Independently audit ORIGINAL PR57 #400 Action ZIP; no engine replay.

Usage: python3 scripts/pr57-400-audit.py pr57-400-original.zip
Validates SHA/CRC, REAL Advance(2) boundaries, exact route writer witnesses,
admission cleanup, intermediate invariants, and byte-identical final saves.
Does not certify causal closure or a throughput gain.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ZIP_SHA = "00d279be14187318766c20829660c4a774f81a94f3c1eab60c5d5a7e250b5cf7"
SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
MEMBERS = {
    "base/full.log", "probe/full.log", "base/checkpoint-10800.json",
    "probe/checkpoint-10800.json", "summary.json", "checkpoints.sha256"
}

def rows(lines, prefix):
    return [
        dict(part.split("=", 1) for part in line.split()[1:] if "=" in part)
        for line in lines if line.startswith(prefix + " ")
    ]

def main(path):
    raw = Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == MEMBERS
        base = z.read("base/checkpoint-10800.json")
        probe = z.read("probe/checkpoint-10800.json")
        assert base == probe and len(base) == 2254838
        assert hashlib.sha256(base).hexdigest() == SAVE_SHA
        logs = {
            arm: z.read(f"{arm}/full.log").decode().splitlines()
            for arm in ("base", "probe")
        }
        metrics = {}
        for arm, lines in logs.items():
            hours = rows(lines, "PR57_337_HOUR")
            advances = rows(lines, "PR57_ADVANCE_BOUNDARY")
            finals = rows(lines, "PR57_337_FINAL")
            assert len(hours) == 6 and len(advances) == 108 and len(finals) == 1
            assert advances[-1]["calls"] == "10800" and finals[0]["parity"] == "IDENTICAL"
            assert all(int(h["living"]) == 121 for h in hours)
            metrics[arm] = {
                "wallMs": round(sum(float(h["wallMs"]) for h in hours), 3),
                "cpuMs": round(sum(float(h["cpuMs"]) for h in hours), 3),
                "allocatedBytes": sum(int(h["totalAllocatedBytes"]) for h in hours)
            }
        for a, b in zip(rows(logs["base"], "PR57_337_HOUR"),
                        rows(logs["probe"], "PR57_337_HOUR")):
            for key in ("living", "ledger", "history", "slices", "actors",
                        "claimChanges", "spaceCountChanges"):
                assert a[key] == b[key], key
        for prefix in ("PR57_399_ROUTE", "PR57_399_RELEASE", "PR57_400_ROUTE"):
            assert not rows(logs["base"], prefix)
        lines = logs["probe"]
        routes = rows(lines, "PR57_399_ROUTE")
        restock = rows(lines, "PR57_400_ROUTE")
        releases = rows(lines, "PR57_399_RELEASE")
        assert len(routes) == 11 and len(restock) == 2 and len(releases) == 14
        assert sum(x["method"] == "TryDrive" for x in routes) == 9
        assert sum(x["method"] == "RestockBusinesses" for x in routes) == 2
        assert {
            (x["step"], x["physical"], x["vehicle"]) for x in restock
        } == {
            (x["step"], x["physical"], x["vehicle"])
            for x in routes if x["method"] == "RestockBusinesses"
        }
        assert all(x["method"] == "OrderLocalFood" and x["success"] == "True"
                   for x in restock)
        assert [(x["vehicle"], x["step"], x["physical"])
                for x in routes if x["vehicle"] == "1386"] == [
                    ("1386", "1802", "10830")
                ]
        s400 = rows(lines, "PR57_400_SUMMARY")
        s399 = rows(lines, "PR57_399_METHODS")
        cleanup = rows(lines, "PR57_399_CLEANUP")
        assert len(s400) == len(s399) == len(cleanup) == 1
        assert tuple(s400[0][key] for key in
                     ("changes", "local", "import", "failed", "captures", "checks")) == (
                         "2", "2", "0", "0", "10", "10"
                     )
        assert tuple(s399[0][key] for key in
                     ("routeChanges", "tryDriveChanges", "physical", "steps")) == (
                         "11", "9", "10939", "1820"
                     )
        assert cleanup[0]["directAdmissionClears"] == "14"
        assert len({(x["physical"], x["vehicle"]) for x in releases}) == 14
        assert all(x["directAdmissionNull"] == "True" for x in releases)
        assert all(x["observational"] == "True" and x["noSkippedSlices"] == "True"
                   for x in (s399[0], s400[0], cleanup[0]))
        summary = json.loads(z.read("summary.json"))
        assert summary["arms"]["probe"]["restock400"]["changes"] == "2"
        print(json.dumps({
            "zipSha256": ZIP_SHA, "crc": "PASS", "saveSha256": SAVE_SHA,
            "saveBytes": len(base), "advance2PerArm": 10800, "living": 121,
            "routeChanges": 11, "tryDrive": 9, "restock": 2,
            "restockOrderLocalFood": 2, "restockOrderImport": 0,
            "restockFailed": 0, "eleventhVehicle": 1386,
            "eleventhStep": 1802, "eleventhPhysical": 10830,
            "admissionReleases": 14, "metrics": metrics,
            "causalClosure": False, "optimizationGainAccepted": False
        }, sort_keys=True))

if __name__ == "__main__":
    assert len(sys.argv) == 2, "usage: pr57-400-audit.py ORIGINAL.zip"
    main(sys.argv[1])
