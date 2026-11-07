#!/usr/bin/env python3
"""Independent offline audit of PR57 #422 original Actions ZIP.

Usage: python3 scripts/pr57-423-audit.py pr57-422.zip
No .NET, GitHub access or third-party packages required.
"""
import hashlib
import json
import sys
import zipfile

EXPECTED_ZIP = "0534bfee9ce1f6f3020dc0847b734bc9d3dd841367f71a0e4d0a4c8d83790388"
EXPECTED_SAVE = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
EXPECTED_FILES = {
    "base/checkpoint-10800.json", "base/full.log", "checkpoints.sha256",
    "shadow/checkpoint-10800.json", "shadow/full.log", "summary.json",
}

def rows(lines, prefix):
    return [dict(token.split("=", 1) for token in line.split()[1:] if "=" in token)
            for line in lines if line.startswith(prefix + " ")]

def main(path):
    with open(path, "rb") as stream:
        zip_sha = hashlib.sha256(stream.read()).hexdigest()
    assert zip_sha == EXPECTED_ZIP, ("ZIP SHA-256 mismatch", zip_sha)
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None, "CRC mismatch"
        assert set(archive.namelist()) == EXPECTED_FILES
        summary = json.loads(archive.read("summary.json"))
        assert summary["scope"] == "AUTHENTIC_REAL_121_LIVING_98_VEHICLES_6H"
        assert summary["advance2PerArm"] == 10800
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["optimizationGainAccepted"]
        saves = []
        baseline_signature = None
        for arm in ("base", "shadow"):
            lines = archive.read(f"{arm}/full.log").decode().splitlines()
            hours = rows(lines, "PR57_337_HOUR")
            boundaries = rows(lines, "PR57_ADVANCE_BOUNDARY")
            finals = rows(lines, "PR57_337_FINAL")
            cohorts = rows(lines, "PR57_420_COHORT")
            persistence = rows(lines, "PR57_420_SUMMARY")
            limiter = rows(lines, "PR57_422_SUMMARY")
            assert len(hours) == 6 and len(boundaries) == 108 and len(finals) == 1
            assert int(boundaries[-1]["calls"]) == 10800
            assert all(int(h["living"]) == 121 for h in hours)
            signature = [(h["hour"],h["living"],h["ledger"],h["history"],h["slices"],h["actors"],
                          h["claimChanges"],h["spaceCountChanges"]) for h in hours]
            if baseline_signature is None:
                baseline_signature = signature
            else:
                assert signature == baseline_signature, "Intermediate economy/history/event mismatch"
            assert finals[0] == summary["arms"][arm]["final"]
            assert finals[0]["parity"] == "IDENTICAL"
            assert cohorts == summary["arms"][arm]["cohorts"]
            assert (persistence[0] if arm == "shadow" else None) == summary["arms"][arm]["persistence"]
            assert (limiter[0] if arm == "shadow" else None) == summary["arms"][arm]["limiter"]
            if arm == "base":
                assert not cohorts and not persistence and not limiter
            else:
                assert len(cohorts) == 4 and len(persistence) == 1
                p = persistence[0]
                assert len(limiter) == 1
                l = limiter[0]
                assert l["observational"] == "True" and l["causalClosure"] == "False"
                assert l["noDecisionReuse"] == "True" and int(l["physical"]) == 600
                assert int(l["readerChanged"]) == int(p["sameReader"]) - int(p["sameReaderDecision"])
                stages = ("leader", "initial", "stop", "poses", "bend", "site", "crossing", "steer", "unexplained")
                assert sum(int(l[k]) for k in stages) == int(l["readerChanged"])
                assert int(l["readerChanged"]) == int(l["readerStepChanged"]) == 1240
                assert int(l["sameLeaderStepChanged"]) == 1240
                assert int(l["initial"]) == int(l["remainingChanged"]) == 1240
                assert all(int(l[k]) == 0 for k in stages if k != "initial")
                assert all(int(l[k]) == 0 for k in ("dChanged", "speedChanged", "stopEarlyChanged"))
                assert int(p["physical"]) == 600
                assert p["observational"] == "True" and p["causalClosure"] == "False"
                assert p["noSkippedSlices"] == "True" and p["noDecisionReuse"] == "True"
                assert int(p["scans"]) >= int(p["firstScans"]) >= int(p["consecutive"])
                assert sum(int(c["actors"]) for c in cohorts) == 58800
                assert sorted(int(c["kind"]) for c in cohorts) == [0, 1, 2, 3]
                assert all(int(c["scalarStable"]) <= int(c["actors"]) for c in cohorts)
                assert int(p["sameReaderDecision"]) <= min(int(p["sameReader"]), int(p["sameDecision"]))
                assert all(int(p[k]) <= int(p["consecutive"]) for k in
                           ("sameReader", "sameBlocker", "sameDistance", "sameStep", "sameDecision"))
            saves.append(archive.read(f"{arm}/checkpoint-10800.json"))
        assert saves[0] == saves[1], "Save mismatch"
        save_sha = hashlib.sha256(saves[0]).hexdigest()
        assert save_sha == EXPECTED_SAVE and save_sha == summary["saveSha256"]
        cohorts = summary["arms"]["shadow"]["cohorts"]
        p = summary["arms"]["shadow"]["persistence"]
        l = summary["arms"]["shadow"]["limiter"]
        freq = int(cohorts[0]["frequency"])
        assert all(int(c["frequency"]) == freq for c in cohorts)
        total_ticks = sum(int(c["ticks"]) for c in cohorts)
        output = {
            "audit": "PASS", "artifactSha256": zip_sha, "saveSha256": save_sha,
            "saveBytes": len(saves[0]), "advance2PerArm": 10800,
            "physicalSample": int(p["physical"]),
            "scans": int(p["scans"]), "consecutiveScans": int(p["consecutive"]),
            "sameReaderAndDecision": int(p["sameReaderDecision"]),
            "sameReaderChanged": int(l["readerChanged"]),
            "initialLimiterChanged": int(l["initial"]),
            "remainingChanged": int(l["remainingChanged"]),
            "stageCounts": {k:int(l[k]) for k in ("leader","initial","stop","poses","bend","site","crossing","steer","unexplained")},
            "actorTimeSampleMs": round(total_ticks * 1000 / freq, 3),
            "cohorts": [
                {"kind": int(c["kind"]), "actors": int(c["actors"]),
                 "scalarStable": int(c["scalarStable"]),
                 "timeMs": round(int(c["ticks"]) * 1000 / freq, 3)}
                for c in cohorts],
            "optimizationGainAccepted": False, "causalClosure": False,
        }
        print(json.dumps(output, sort_keys=True, indent=2))

if __name__ == "__main__":
    assert len(sys.argv) == 2, "usage: pr57-423-audit.py pr57-422.zip"
    main(sys.argv[1])
