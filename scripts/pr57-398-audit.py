#!/usr/bin/env python3
"""Offline audit of the ORIGINAL PR57 #398 Action ZIP (no new engine run).

Usage: python3 scripts/pr57-398-audit.py pr57-398-original.zip
The Action's final summary assertion was too strict: 1,800 Steps contain
10,818 physical calls, not 12,000. Its raw logs/saves were uploaded intact.
The probe is fixture-scoped, net-boundary observational evidence, NOT an
optimization or a writer-complete causal certificate.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP = "9dd19c8d7a14420b87e209b512d698878c389c4844f185ff8e6f69eb9388be07"
EXPECTED_SAVE = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
MEMBERS = {"base/checkpoint-10800.json", "probe/checkpoint-10800.json",
           "base/full.log", "probe/full.log", "summary.json", "checkpoints.sha256"}

def rows(lines, prefix):
    return [dict(token.split("=", 1) for token in line.split()[1:] if "=" in token)
            for line in lines if line.startswith(prefix + " ")]

def main(path):
    raw = Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == EXPECTED_ZIP
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
        assert set(archive.namelist()) == MEMBERS
        saved = [archive.read(f"{arm}/checkpoint-10800.json") for arm in ("base", "probe")]
        assert saved[0] == saved[1] and len(saved[0]) == 2254838
        assert hashlib.sha256(saved[0]).hexdigest() == EXPECTED_SAVE
        logs = {arm: archive.read(f"{arm}/full.log").decode().splitlines()
                for arm in ("base", "probe")}
        hours = {}
        metrics = {}
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
        for left, right in zip(hours["base"], hours["probe"]):
            for field in ("living", "ledger", "history", "slices", "actors",
                          "claimChanges", "spaceCountChanges"):
                assert left[field] == right[field], field
        assert not rows(logs["base"], "PR57_398_PHASES")
        phases = rows(logs["probe"], "PR57_398_PHASES")
        changes = rows(logs["probe"], "PR57_398_CHANGE")
        gaps = rows(logs["probe"], "PR57_397_CHANGE")
        assert len(phases) == 1 and len(changes) == 15
        p = phases[0]
        counts = [int(x) for x in p["changes"].split(",")]
        assert counts == [5, 0, 8, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0]
        assert (int(p["steps"]), int(p["physical"]), int(p["captures"]),
                int(p["missingTargetSteps"]), int(p["targetIds"])) == (1800, 10818, 25200, 0, 11)
        assert p["observational"] == "True" and p["noSkippedSlices"] == "True"
        assert p["exactRoute"] == "True" and p["netBoundariesOnly"] == "True"
        gap_po = {(x["vehicle"], int(x["physical"])) for x in gaps
                  if x["class"] == "3" and x["route"] == "True"}
        assert len(gap_po) == 11  # class 3 means physical -> operational
        attributed = {(x["vehicle"], int(x["physical"])) for x in changes
                      if int(x["phase"]) in (2, 11)}
        assert len(attributed) == 10 and attributed <= gap_po
        missing = gap_po - attributed
        assert missing == {("1386", 10830)}
        assert sum(x["phase"] == "0" for x in changes) == 5
        print(json.dumps({
            "zipSha256": EXPECTED_ZIP, "crc": "PASS",
            "saveParity": True, "saveBytes": len(saved[0]),
            "advance2PerArm": 10800, "metrics": metrics,
            "stepPhaseChanges": counts, "attributedGapPO": 10,
            "gapPOTotal": 11, "beyond1800Steps": sorted(missing),
            "phase2CitizenMobility": 8, "phase11MarketFollowups": 2,
            "phase0Vehicle": 5, "actionConclusion": "failure: horizon assertion only",
            "causalClosure": False, "optimizationGainAccepted": False
        }, sort_keys=True))

if __name__ == "__main__":
    assert len(sys.argv) == 2, "usage: pr57-398-audit.py original.zip"
    main(sys.argv[1])
