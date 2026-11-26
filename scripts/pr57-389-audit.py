#!/usr/bin/env python3
"""Independent audit of PR57 #389 Actions ZIP; observational, not an optimization.

Usage: python3 scripts/pr57-389-audit.py artifact.zip [zip_sha256]
"""
import hashlib
import json
import math
import sys
import zipfile
from pathlib import Path

SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
INVARIANTS = ("hour", "slices", "actors", "claimChanges", "spaceCountChanges",
              "living", "ledger", "history")


def rows(data, prefix):
    return [dict(kv.split("=", 1) for kv in line.split()[1:] if "=" in kv)
            for line in data.decode().splitlines() if line.startswith(prefix + " ")]


def audit(path, expected_sha=None):
    raw = path.read_bytes()
    sha = hashlib.sha256(raw).hexdigest()
    if expected_sha:
        assert sha == expected_sha.lower(), (sha, expected_sha)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, "CRC failure"
        expected_files = {"summary.json", "checkpoints.sha256"} | {
            f"{arm}/{name}" for arm in ("base", "probe")
            for name in ("full.log", "checkpoint-10800.json")}
        assert set(z.namelist()) == expected_files, set(z.namelist())
        saves = [z.read(f"{arm}/checkpoint-10800.json") for arm in ("base", "probe")]
        assert saves[0] == saves[1] and len(saves[0]) == 2254838
        assert hashlib.sha256(saves[0]).hexdigest() == SAVE_SHA
        summary = json.loads(z.read("summary.json"))
        assert summary["advance2PerArm"] == 10800
        assert summary["sampledPhysicalCalls"] == 600
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["acceptedSpeedup"]
        hour_records = {}
        for arm in ("base", "probe"):
            data = z.read(f"{arm}/full.log")
            hours = rows(data, "PR57_337_HOUR")
            boundaries = rows(data, "PR57_ADVANCE_BOUNDARY")
            final = rows(data, "PR57_337_FINAL")
            order = rows(data, "PR57_387_ORDER")
            cross388 = rows(data, "PR57_388_CROSS")
            binding = rows(data, "PR57_389_BIND")
            cross389 = rows(data, "PR57_389_CROSS")
            assert len(hours) == 6 and len(boundaries) == 108 and len(final) == 1
            assert int(boundaries[-1]["calls"]) == 10800
            assert final[0]["parity"] == "IDENTICAL"
            assert final[0]["sha256"].lower() == SAVE_SHA
            assert all(int(x["living"]) == 121 for x in hours)
            assert len(order) == len(cross388) == len(binding) == len(cross389) == (arm == "probe")
            assert abs(sum(float(h["wallMs"]) for h in hours) - summary["arms"][arm]["wallMs"]) < .001
            assert sum(int(h["totalAllocatedBytes"]) for h in hours) == summary["arms"][arm]["allocatedBytes"]
            hour_records[arm] = hours
            if arm == "probe":
                o, b, c, x = order[0], binding[0], cross389[0], cross388[0]
                assert int(o["physicalCalls"]) == int(b["samplePhysicalCalls"]) == 600
                assert int(o["selectedAfterPose"]) == int(b["selectedAfterPose"]) == 136
                assert int(o["crossCohortSelected"]) == int(b["crossCohort"]) == 1
                assert 0 <= int(b["binding"]) <= 136
                assert 0 <= int(b["crossBinding"]) <= 1
                assert 0 <= int(b["zeroLeaderCap"]) <= 136
                assert c["call"] == x["call"] and c["reader"] == x["reader"] and c["blocker"] == x["blocker"]
                assert c["observationOnly"] == b["observationOnly"] == "True"
                assert c["noCounterfactualDecision"] == "True"
                assert b["noReorder"] == b["noSkippedSlices"] == "True"
                raw_cap = float(c["rawCap"])
                leader_distance = float(c["leaderDistance"])
                pre_forward = float(c["preForwardProjection"])
                assert math.isclose(leader_distance, float(x["leaderDistance"]), abs_tol=1e-12)
                assert math.isclose(float(c["postLeaderCap"]), max(0, leader_distance - 8), abs_tol=1e-12)
                assert math.isclose(float(c["preSameWinnerCap"]), max(0, pre_forward - 8), abs_tol=1e-12)
                assert math.isclose(float(c["postLaneStep"]), min(raw_cap, max(0, leader_distance - 8)), abs_tol=1e-12)
                assert math.isclose(float(c["preSameWinnerStep"]), min(raw_cap, max(0, pre_forward - 8)), abs_tol=1e-12)
                assert (c["binding"] == "True") == (max(0, leader_distance - 8) < raw_cap)
                assert summary["arms"][arm]["laneBinding"] == b
                assert summary["arms"][arm]["crossLaneBinding"] == c
        for k in INVARIANTS:
            assert [h[k] for h in hour_records["base"]] == [h[k] for h in hour_records["probe"]], k
        return {"artifactSha256": sha, "saveSha256": SAVE_SHA,
                "selectedAfterPose": int(b["selectedAfterPose"]),
                "binding": int(b["binding"]), "zeroLeaderCap": int(b["zeroLeaderCap"]),
                "crossCohort": int(b["crossCohort"]), "crossBinding": int(b["crossBinding"]),
                "crossTrace": c, "wallMs": {a: summary["arms"][a]["wallMs"] for a in ("base", "probe")},
                "causalClosure": False, "acceptedSpeedup": False}


if __name__ == "__main__":
    if len(sys.argv) not in (2, 3):
        raise SystemExit(__doc__)
    print(json.dumps(audit(Path(sys.argv[1]), sys.argv[2] if len(sys.argv) == 3 else None), indent=2))
