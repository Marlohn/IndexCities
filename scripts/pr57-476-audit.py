#!/usr/bin/env python3
"""PR57 #476: independently audit original workflow ZIP (not a performance A/B).

Usage: python3 scripts/pr57-476-audit.py pr57-476-original.zip
All snapshots are authentic. Net serialized changes are NOT causal read sets.
"""
import hashlib
import json
import re
import sys
import zipfile

EXPECTED = "84bdf5c01fe95ad710589924e16414449ae37d7705c4bda170ce51abb4af359c"
CASES = {
    "worksREAL68": (20, 47, 42, "b7c52ab87928e7eaa93cfc557b9f3dc935d5b17da44820b405990071940f95b5"),
    "midnightREAL121": (2, 121, 98, "e31ad0aa321c8aa4272dd400e83575c5a600677ecb8a83879287c654eff1ce92"),
}


def fields(line):
    return dict(re.findall(r"([A-Za-z][A-Za-z0-9]*)=([^ ]+)", line))


def main(path):
    raw = open(path, "rb").read()
    assert hashlib.sha256(raw).hexdigest() == EXPECTED
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        summary = json.loads(z.read("summary.json"))
        assert summary["experiment"] == 476 and summary["allAdvance2"]
        assert summary["observationOnly"] and not summary["gainAccepted"]
        assert set(summary["cases"]) == set(CASES)
        result = {}
        for case, (steps, alive, vehicles, source_sha) in CASES.items():
            assert summary["cases"][case]["inputSha256"] == source_sha
            snapshots = {}
            for arm in ("A", "B"):
                prefix = f"{case}/{arm}/"
                before = z.read(prefix + "checkpoint-0.json")
                after = z.read(prefix + "checkpoint-1.json")
                assert hashlib.sha256(before).hexdigest() == source_sha
                state = json.loads(before)
                assert sum(bool(c["Alive"]) for c in state["Citizens"]) == alive
                assert len(state["Vehicles"]) == vehicles
                assert summary["cases"][case]["arms"][arm]["advance2"] == steps
                assert hashlib.sha256(after).hexdigest() == summary["cases"][case]["arms"][arm]["sha256"]
                assert len(after) == summary["cases"][case]["arms"][arm]["bytes"]
                log = z.read(prefix + "full.log").decode()
                assert "INVARIANTS OK" in log and "SAVE " in log
                snapshots[arm] = (before, after)
                if arm == "A":
                    assert "PR57_PHASE_EFFECT name=" not in log
                    continue
                rows = json.loads(z.read(prefix + "phase-rows.json"))
                assert len(rows["netFieldRows"]) == len(rows["presenceRows"]) == 13
                assert f"PR57_PHASE_EFFECT_SUMMARY samples={steps} " in log
                assert f"PR57_183_PRESENCE_SUMMARY samples={steps} " in log
                assert f"STEP_PROFILE steps={steps} " in log
                for line in rows["netFieldRows"] + rows["presenceRows"]:
                    assert line in log
                phase = {fields(line)["name"]: fields(line) for line in rows["netFieldRows"]}
                presence = {fields(line)["name"]: fields(line) for line in rows["presenceRows"]}
                assert set(phase) == set(presence) and len(phase) == 13
                assert all(int(v["changed"]) + int(v["unchanged"]) == steps for v in phase.values())
                result[case] = {
                    "steps": steps,
                    "net_changed_steps": {k: int(v["changed"]) for k, v in phase.items()},
                    "worker_membership_changes": {k: int(v["workerMemberChanges"]) for k, v in presence.items()},
                    "phase_fields": {k: v.get("topFields", "") for k, v in phase.items()},
                    "project_citizen_fields": {k: v.get("nestedFields", "") for k, v in phase.items()},
                    "net_revisited": {k: v["revisitedEarlierPhase"] for k, v in phase.items()},
                }
            assert snapshots["A"][0] == snapshots["B"][0]
            assert snapshots["A"][1] == snapshots["B"][1]
        print(json.dumps({"zip_sha256": EXPECTED, "entries": len(z.namelist()),
                          "scope": "NET_FIELDS_AND_WORKER_PRESENCE_NOT_CAUSAL_READ_SETS",
                          "cases": result}, indent=2, ensure_ascii=False, sort_keys=True))
    print("PR57_476_INTRASTEP_AUDIT_PASS")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: pr57-476-audit.py pr57-476-original.zip")
    main(sys.argv[1])
