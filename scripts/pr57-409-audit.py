#!/usr/bin/env python3
"""PR57 #409: independent audit of the raw #408 GitHub Actions ZIP.

Usage: python3 scripts/pr57-409-audit.py pr57-408-raw.zip
Reads original logs/checkpoints, not only summary.json. No simulation edits.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP = "d650c18be0aa829cc9c67ac63a78f6512f9bb6caad3b66d1e75f6a903f8355dd"
EXPECTED_SAVE = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"

def records(lines, prefix):
    return [dict(part.split("=", 1) for part in line.split()[1:] if "=" in part)
            for line in lines if line.startswith(prefix + " ")]

def main(path):
    raw = Path(path).read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    assert digest == EXPECTED_ZIP, (digest, EXPECTED_ZIP)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        expected = {"base/checkpoint-10800.json", "base/full.log",
                    "probe/checkpoint-10800.json", "probe/full.log",
                    "checkpoints.sha256", "summary.json"}
        assert set(z.namelist()) == expected, z.namelist()
        saved = [z.read(f"{arm}/checkpoint-10800.json") for arm in ("base", "probe")]
        assert saved[0] == saved[1]
        assert len(saved[0]) == 2254838
        assert hashlib.sha256(saved[0]).hexdigest() == EXPECTED_SAVE
        packaged = json.loads(z.read("summary.json"))
        arms = {}
        for arm in ("base", "probe"):
            lines = z.read(f"{arm}/full.log").decode("utf-8").splitlines()
            hours = records(lines, "PR57_337_HOUR")
            boundaries = records(lines, "PR57_ADVANCE_BOUNDARY")
            final = records(lines, "PR57_337_FINAL")
            cells = records(lines, "PR57_408_CELL")
            holds = records(lines, "PR57_408_HOLD")
            summaries = records(lines, "PR57_408_SUMMARY")
            assert len(hours) == 6 and len(boundaries) == 108 and len(final) == 1
            assert int(boundaries[-1]["calls"]) == 10800
            assert all(int(h["living"]) == 121 for h in hours)
            assert final[0]["parity"] == "IDENTICAL"
            assert final[0]["sha256"].lower() == EXPECTED_SAVE
            assert hours[-1]["ledger"] == "4085" and hours[-1]["history"] == "7430"
            assert packaged["arms"][arm]["final"] == final[0]
            arm_stats = {
                "wallMs": round(sum(float(h["wallMs"]) for h in hours), 3),
                "cpuMs": sum(float(h["cpuMs"]) for h in hours),
                "allocatedBytes": sum(int(h["totalAllocatedBytes"]) for h in hours),
            }
            if arm == "base":
                assert not cells and not holds and not summaries
            else:
                assert len(cells) == 28 and len(holds) == 7 and len(summaries) == 1
                summary = summaries[0]
                assert packaged["arms"][arm]["summary"] == summary
                assert packaged["arms"][arm]["cells"] == cells
                assert packaged["arms"][arm]["holds"] == holds
                assert summary["observational"] == "True"
                assert summary["causalClosure"] == "False"
                assert summary["noSkippedSlices"] == "True"
                assert int(summary["physical"]) == 600
                assert int(summary["scans"]) == 16255
                assert int(summary["candidates"]) == 1592990
                assert int(summary["winners"]) == 13162
                assert int(summary["binding"]) == 11645
                assert sum(int(c["scans"]) for c in cells) == 16255
                assert sum(int(c["scans"]) for c in cells if c["binding"] == "1") == 11645
                assert sum(int(c["scans"]) for c in cells if c["cause"] != "0") == 13162
                def count(**filters):
                    return sum(int(c["scans"]) for c in cells
                               if all(c[k] == str(v) for k, v in filters.items()))
                binding_by_cause = {cause: count(cause=cause, binding=1) for cause in range(7)}
                assert binding_by_cause == {0: 0, 1: 0, 2: 0, 3: 600,
                                            4: 0, 5: 0, 6: 11045}
                assert count(wait=1, binding=1) == 11641
                assert {int(h["cause"]): int(h["attempts"]) for h in holds} == {
                    0: 0, 1: 0, 2: 0, 3: 1200, 4: 0, 5: 0, 6: 70199}
                arm_stats.update({"physicalCalls": 600, "scans": 16255,
                                  "bindingByCause": binding_by_cause,
                                  "waitingBinding": 11641})
            arms[arm] = arm_stats
    print(json.dumps({"status": "PASS", "zipSha256": digest,
                      "saveSha256": EXPECTED_SAVE, "arms": arms,
                      "scope": "REAL 121 living / 98 vehicles; observational, not throughput A/B"},
                     ensure_ascii=False, indent=2))

if __name__ == "__main__":
    assert len(sys.argv) == 2, "Usage: pr57-409-audit.py <raw ZIP>"
    main(sys.argv[1])
