#!/usr/bin/env python3
"""Independent PR57 #424 ZIP audit; the CI parser failed after valid replays.

Usage: python3 scripts/pr57-425-audit.py original-424.zip
No GitHub access, .NET or third-party packages required.
"""
import hashlib
import json
import sys
import zipfile

ZIP_SHA = "1c2a5eb75f721c2402f1e9245cc15b81e99fb5eaf691b1c4e8701ee9d619041a"
SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
FILES = {"base/checkpoint-10800.json", "base/full.log", "checkpoints.sha256",
         "shadow/checkpoint-10800.json", "shadow/full.log", "summary.json"}

def rows(lines, prefix):
    return [dict(x.split("=", 1) for x in line.split()[1:] if "=" in x)
            for line in lines if line.startswith(prefix + " ")]

def main(path):
    assert hashlib.sha256(open(path, "rb").read()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == FILES
        assert not z.read("summary.json"), "CI summary should be empty after NameError"
        signatures, saves, arms = [], [], {}
        for arm in ("base", "shadow"):
            lines = z.read(f"{arm}/full.log").decode().splitlines()
            hours = rows(lines, "PR57_337_HOUR")
            boundaries = rows(lines, "PR57_ADVANCE_BOUNDARY")
            final = rows(lines, "PR57_337_FINAL")
            cohorts = rows(lines, "PR57_420_COHORT")
            persistence = rows(lines, "PR57_420_SUMMARY")
            limiter = rows(lines, "PR57_422_SUMMARY")
            horizon = rows(lines, "PR57_424_SUMMARY")
            assert len(hours) == 6 and len(boundaries) == 108 and len(final) == 1
            assert [int(h["hour"]) for h in hours] == list(range(1, 7))
            assert [int(b["calls"]) for b in boundaries] == list(range(100, 10801, 100))
            assert all(b["quantum"] == "2" for b in boundaries)
            assert all(int(h["living"]) == 121 for h in hours)
            assert final[0]["parity"] == "IDENTICAL"
            signatures.append([tuple(h[k] for k in
                ("hour", "slices", "actors", "claimChanges", "spaceCountChanges",
                 "living", "ledger", "history")) for h in hours])
            saves.append(z.read(f"{arm}/checkpoint-10800.json"))
            arms[arm] = {
                "wallMs": round(sum(float(h["wallMs"]) for h in hours), 3),
                "cpuMs": round(sum(float(h["cpuMs"]) for h in hours), 3),
                "allocatedBytes": sum(int(h["totalAllocatedBytes"]) for h in hours)}
            if arm == "base":
                assert not cohorts and not persistence and not limiter and not horizon
            else:
                assert len(cohorts) == 4 and len(persistence) == len(limiter) == len(horizon) == 1
                p, l, h = persistence[0], limiter[0], horizon[0]
                assert sum(int(c["actors"]) for c in cohorts) == 58800
                assert int(p["physical"]) == int(l["physical"]) == int(h["physical"]) == 600
                assert int(p["firstScans"]) == int(h["first"]) == 15874
                assert int(h["remainingEqDt"]) == 15874 and int(h["remainingNeDt"]) == 0
                assert int(h["changed"]) == int(l["readerChanged"]) == 1240
                assert int(l["initial"]) == int(l["remainingChanged"]) == 1240
                assert int(h["dtChanged"]) == int(h["eqDtBoth"]) == 1240
                assert int(h["dtSame"]) == int(h["preConsumedEither"]) == 0
                assert int(p["sameReader"]) - int(p["sameReaderDecision"]) == 1240
                assert int(l["sameLeaderStepChanged"]) == 1240
                assert all(int(l[k]) == 0 for k in
                    ("leader", "stop", "poses", "bend", "site", "crossing", "steer",
                     "unexplained", "dChanged", "speedChanged", "stopEarlyChanged"))
                assert h["observational"] == "True" and h["causalClosure"] == "False"
                assert h["noDecisionReuse"] == "True"
        assert signatures[0] == signatures[1], "intermediate economy/history/event mismatch"
        assert saves[0] == saves[1], "final saves differ"
        assert len(saves[0]) == 2254838
        assert hashlib.sha256(saves[0]).hexdigest() == SAVE_SHA
        print(json.dumps({"audit": "PASS", "zipSha256": ZIP_SHA,
            "saveSha256": SAVE_SHA, "saveBytes": len(saves[0]),
            "advance2PerArm": 10800, "realLiving": 121, "realVehicles": 98,
            "physicalSample": 600, "firstScans": 15874,
            "remainingEqualsDt": 15874, "remainingDiffersDt": 0,
            "stableReaderChanged": 1240, "dtChanged": 1240, "dtSame": 0,
            "preConsumedEither": 0, "intermediateParity": True,
            "finalSavesIdentical": True, "arms": arms,
            "ciFailure": "parser NameError after both completed replays",
            "gainAccepted": False}, sort_keys=True, indent=2))

if __name__ == "__main__":
    assert len(sys.argv) == 2, "usage: pr57-425-audit.py original-424.zip"
    main(sys.argv[1])
