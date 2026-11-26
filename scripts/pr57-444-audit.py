#!/usr/bin/env python3
"""Audit original GitHub Actions #444 ZIP independently of its summary.json.

Usage: python3 scripts/pr57-444-audit.py <original-artifact.zip>
Checks CRC, every public save, pinned hashes, ABBA logs, cadence and metrics.
This is not proof of universal causal closure or a performance improvement.
"""
import hashlib
import json
import re
import statistics
import sys
import zipfile
from decimal import Decimal

EXPECTED = {
    "worksEarlyREAL": ("b7c52ab87928e7eaa93cfc557b9f3dc935d5b17da44820b405990071940f95b5", "686ed27e0cc458d3d8f411a1e308a2dc98e87bf588c12d5dcf1ea787bad1c883", 1, 100),
    "congested14hREAL": ("779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f", "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa", 6, 10800),
}

def field(row, name):
    match = re.search(r"(?<!\w)" + re.escape(name) + r"=([^ ]+)", row)
    if match is None:
        raise AssertionError((name, row))
    return match.group(1)

def inspect(path):
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, "CRC failed"
        names = set(z.namelist())
        summary_paths = [n for n in names if n.endswith("summary.json")]
        assert len(summary_paths) == 1, summary_paths
        summary = json.loads(z.read(summary_paths[0]))
        assert summary["experiment"] == "444_zero_demand_add_opt_in"
        assert summary["optimizationGainAccepted"] is False
        for case, (initial, final, windows, calls) in EXPECTED.items():
            assert case in summary["cases"]
            data = summary["cases"][case]
            assert data["advance2PerProcess"] == calls
            assert data["fullCheckpointsPerProcess"] == windows + 1
            assert len(data["runs"]) == 4
            modes = ("A", "B", "B", "A")
            for checkpoint in range(windows + 1):
                copies = []
                for i, mode in enumerate(modes, 1):
                    suffix = f"{case}/{i}-{mode}/checkpoint-{checkpoint}.json"
                    matches = [n for n in names if n.endswith(suffix)]
                    assert len(matches) == 1, (suffix, matches)
                    copies.append(z.read(matches[0]))
                assert copies.count(copies[0]) == 4, (case, checkpoint, "SAVE_MISMATCH")
                json.loads(copies[0], parse_float=Decimal)
                actual = hashlib.sha256(copies[0]).hexdigest()
                if checkpoint == 0:
                    assert actual == initial, (case, actual)
                if checkpoint == windows:
                    assert actual == final, (case, actual)
            for i, mode in enumerate(modes, 1):
                suffix = f"{case}/{i}-{mode}/full.log"
                matches = [n for n in names if n.endswith(suffix)]
                assert len(matches) == 1, (suffix, matches)
                log = z.read(matches[0]).decode("utf-8")
                assert "INVARIANTS OK" in log and "SAVE " in log
                assert f"STEP_PROFILE steps={calls}" in log
                rows = [x for x in log.splitlines() if x.startswith("WINDOW ")]
                assert len(rows) == windows
                assert [int(field(x, "seconds")) for x in rows] == [200 if windows == 1 else 3600] * windows
                boundaries = [x for x in log.splitlines() if x.startswith("PR57_AB_CHECKPOINT ")]
                assert len(boundaries) == windows
                assert [int(field(x, "window")) for x in boundaries] == list(range(1, windows+1))
                phases = [x for x in log.splitlines() if x.lstrip().startswith("STEP_PHASE ")]
                assert len(phases) == 13
                recorded = data["runs"][i-1]
                assert recorded["mode"] == mode and recorded["run"] == i
                for name, key in (("wallSeconds", "wall"), ("cpuSeconds", "cpu"), ("allocatedMB", "allocMB")):
                    val = sum(float(field(x, key).removesuffix("s")) for x in rows)
                    assert abs(val - recorded[name]) < 1e-6, (case, mode, name, val, recorded[name])
                phase_times = {field(x, "name"): float(field(x, "wall").removesuffix("s")) for x in phases}
                assert phase_times == recorded["phaseSeconds"]
            a = [x for x in data["runs"] if x["mode"] == "A"]
            b = [x for x in data["runs"] if x["mode"] == "B"]
            for metric in ("wallSeconds", "cpuSeconds", "allocatedMB"):
                base = statistics.mean(x[metric] for x in a)
                variant = statistics.mean(x[metric] for x in b)
                print("PR57_444_AUDIT", case, metric, f"A_mean={base:.6f}", f"B_mean={variant:.6f}",
                      f"B_over_A={variant/base:.6f}" if base else "B_over_A=NA")
            print("PR57_444_AUDIT", case, f"calls={calls}", "ABBA=4", f"fullSaves={windows+1}",
                  "parity=EXACT", f"sha256={final}")
        print("PR57_444_AUDIT_COMPLETE crc=OK summary=RECONCILED gainAccepted=NO")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: pr57-444-audit.py <original-artifact.zip>")
    inspect(sys.argv[1])
