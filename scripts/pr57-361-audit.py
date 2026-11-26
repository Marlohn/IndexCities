#!/usr/bin/env python3
"""Independent offline audit of PR57 #361 original Actions ZIP.

Usage: python3 scripts/pr57-361-audit.py pr57-361-original.zip
Requires only Python stdlib; does not run or alter the simulation.
"""
from hashlib import sha256
from pathlib import Path
from zipfile import ZipFile
import sys

assert len(sys.argv) == 2, "usage: pr57-361-audit.py <original-actions.zip>"
archive = Path(sys.argv[1])
print("zip_sha256", sha256(archive.read_bytes()).hexdigest())
with ZipFile(archive) as z:
    assert z.testzip() is None, "ZIP CRC failure"
    names = set(z.namelist())
    expected = {
        "control/full.log", "probe/full.log",
        "control/census.log", "probe/census.log",
        "control/checkpoint-10800.json", "probe/checkpoint-10800.json",
        "checkpoints.sha256", "summary.txt",
    }
    assert names == expected, (names - expected, expected - names)
    def read(name):
        return z.read(name)
    def rows(mode, prefix):
        return [
            dict(part.split("=", 1) for part in line.split()[1:] if "=" in part)
            for line in read(f"{mode}/full.log").decode().splitlines()
            if line.startswith(prefix + " ")
        ]
    control = read("control/checkpoint-10800.json")
    probe = read("probe/checkpoint-10800.json")
    assert control == probe
    assert len(control) == 2254838, len(control)
    print("checkpoint_sha256", sha256(control).hexdigest(), "bytes", len(control))
    for mode in ("control", "probe"):
        assert len(rows(mode, "PR57_337_HOUR")) == 6
        assert len(rows(mode, "PR57_345_PHASE")) == 78
        assert rows(mode, "PR57_337_FINAL")[0]["parity"] == "IDENTICAL"
        assert len(rows(mode, "PR57_206_RESIDUAL")) == 1200
        assert len(rows(mode, "PR57_244_STATE")) == 6000
        assert len(rows(mode, "PR57_359_GROUP")) == 10
        windows = rows(mode, "PR57_359_WINDOW")
        assert [int(x["start"]) for x in windows] == [40001, 50001]
        assert all(int(x["slices"]) == 600 and int(x["movedActors"]) == 0
                   for x in windows)
    assert not rows("control", "PR57_361_SLICE")
    samples = rows("probe", "PR57_361_SLICE")
    expected_ordinals = list(range(40001, 40007)) + list(range(50001, 50007))
    assert [int(x["ordinal"]) for x in samples] == expected_ordinals
    for x in samples:
        n = int(x["vehicles"])
        assert n == 98
        assert int(x["occupiesCalls"]) > 0
        assert int(x["directedDistinct"]) > 0
        assert 0 <= int(x["readLargest"]) <= n
        assert 0 <= int(x["selectedLargest"]) <= n
        assert int(x["readComponents"]) + int(x["readEdges"]) >= n
        assert int(x["selectedComponents"]) + int(x["selectedEdges"]) >= n
        assert int(x["overlapSelected"]) <= min(
            int(x["selectedEdges"]), int(x["readEdges"]))
        assert x["notCompleteCausalGraph"] == "True"
        print("PR57_361_AUDITED", " ".join(f"{k}={v}" for k, v in x.items()))
    print("PR57_361_SUMMARY",
          "sampleSlices", len(samples),
          "readLargestMin", min(int(x["readLargest"]) for x in samples),
          "readLargestMax", max(int(x["readLargest"]) for x in samples),
          "selectedLargestMin", min(int(x["selectedLargest"]) for x in samples),
          "selectedLargestMax", max(int(x["selectedLargest"]) for x in samples),
          "maxCandidateFanIn", max(int(x["maxCandidateFanIn"]) for x in samples),
          "checkpointByteIdentical=True noSkippedSlices=True",
          "REAL121=True GRID=NOT_TESTED",
          "noCausalClosure=True acceptedIntegratedSpeedup=False")
