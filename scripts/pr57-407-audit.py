#!/usr/bin/env python3
"""Independent offline audit of PR57 #406 original REAL Action artifact.

Usage: python3 scripts/pr57-407-audit.py pr57-406-original.zip
No engine mutation, no benchmark, no causal-closure or optimization claim.
"""
import collections
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

ZIP_SHA = "d3bd93276bba18a1493fc62898ea82d982f30cb50bded6fbf4fa92e07a4060db"
SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
FILES = {
    "base/checkpoint-10800.json", "probe/checkpoint-10800.json",
    "base/full.log", "probe/full.log", "checkpoints.sha256", "summary.json",
}

def rows(log, prefix):
    return [
        dict(re.findall(r"(\w+)=([^ ]+)", line[len(prefix):]))
        for line in log.splitlines() if line.startswith(prefix + " ")
    ]

def main():
    assert len(sys.argv) == 2
    data = Path(sys.argv[1]).read_bytes()
    assert hashlib.sha256(data).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(sys.argv[1]) as z:
        assert z.testzip() is None and set(z.namelist()) == FILES
        a = z.read("base/checkpoint-10800.json")
        b = z.read("probe/checkpoint-10800.json")
        assert a == b and len(a) == 2254838
        assert hashlib.sha256(a).hexdigest() == SAVE_SHA
        summary = json.loads(z.read("summary.json"))
        logs = {arm: z.read(arm + "/full.log").decode()
                for arm in ("base", "probe")}
    assert summary["scope"] == "AUTHENTIC_REAL_121_LIVING_98_VEHICLES_6H"
    assert summary["advance2PerArm"] == 10800
    assert summary["observational"] and not summary["causalClosure"]
    assert not summary["optimizationGainAccepted"]
    for arm, log in logs.items():
        assert len(rows(log, "PR57_337_HOUR")) == 6
        assert all(int(r["living"]) == 121
                   for r in rows(log, "PR57_337_HOUR"))
        assert len(rows(log, "PR57_ADVANCE_BOUNDARY")) == 108
        assert int(rows(log, "PR57_ADVANCE_BOUNDARY")[-1]["calls"]) == 10800
        assert len(rows(log, "PR57_337_FINAL")) == 1
        assert rows(log, "PR57_337_FINAL")[0]["parity"] == "IDENTICAL"
        assert len(rows(log, "PR57_406_SUMMARY")) == (1 if arm == "probe" else 0)
    probe = logs["probe"]
    f = rows(probe, "PR57_406_SUMMARY")[0]
    r404 = rows(probe, "PR57_404_SUMMARY")[0]
    r401 = rows(probe, "PR57_401_SUMMARY")[0]
    writes = rows(probe, "PR57_406_WRITE")
    clears = rows(probe, "PR57_404_WRITE")
    directs = rows(probe, "PR57_401_WRITE")
    assert int(f["steps"]) == 1820 and int(f["physical"]) == 10939
    assert int(f["slices"]) == 10939
    assert int(f["dockReads"]) == 197023
    assert int(f["uniqueReaderCandidatePerSlice"]) == 196981
    assert int(f["readerOrdinalOverflow"]) == 0
    assert int(f["routeChanges"]) == 16 == len(writes)
    assert int(r404["changed"]) == 4 == len(clears)
    assert int(r401["changed"]) == 12 == len(directs)
    assert all(x["site"] == "EnsureFuel.normal" for x in directs)
    assert all(x["site"] == "Cars.cs:88:clear" for x in clears)
    assert {(int(x["physical"]), int(x["vehicle"])) for x in clears} == {
        (136,655), (232,695), (303,689), (323,677)}
    assert collections.Counter(int(x["readersLastTwoPhysical"]) for x in writes) == {
        0:15, 5:1}
    nonzero = [x for x in writes if int(x["readersLastTwoPhysical"]) > 0]
    assert len(nonzero) == 1
    assert (nonzero[0]["step"],nonzero[0]["vehicle"],
            nonzero[0]["site"]) == ("904","1468","EnsureFuel.normal")
    assert int(f["changesWithDockReaders"]) == 1
    assert int(f["potentialNotices"]) == 5
    assert int(f["maxWriterFanout"]) == 5
    assert int(f["maxCandidateReadersPerSlice"]) == 5
    assert int(f["frequency"]) > 0
    assert not rows(logs["base"], "PR57_406_WRITE")
    assert not rows(logs["base"], "PR57_404_WRITE")
    assert not rows(logs["base"], "PR57_401_WRITE")
    result = {
        "audit": "PASS", "zip_sha256": ZIP_SHA, "save_sha256": SAVE_SHA,
        "save_bytes": len(a), "advance2_per_arm": 10800,
        "real_living": 121, "real_vehicles": 98,
        "physical_slices": int(f["slices"]),
        "dock_reads": int(f["dockReads"]),
        "unique_reader_candidate_slices": int(f["uniqueReaderCandidatePerSlice"]),
        "effective_route_writes": len(writes),
        "writer_sources": dict(collections.Counter(x["site"] for x in writes)),
        "potential_dock_reader_notifications": 5,
        "nonzero_writer": nonzero[0],
        "dock_read_registration_ms_intrusive":
            int(f["readTicks"]) * 1000 / int(f["frequency"]),
        "route_publication_ms_intrusive":
            int(f["publishTicks"]) * 1000 / int(f["frequency"]),
        "base_wall_ms": summary["arms"]["base"]["wallMs"],
        "probe_wall_ms": summary["arms"]["probe"]["wallMs"],
        "base_allocated_bytes": summary["arms"]["base"]["allocatedBytes"],
        "probe_allocated_bytes": summary["arms"]["probe"]["allocatedBytes"],
        "scope": "dock-body after early guard only; last two physical slices",
        "causal_closure": False, "new_accepted_integrated_gain": 0,
    }
    print(json.dumps(result, indent=2, sort_keys=True))

if __name__ == "__main__":
    main()
