#!/usr/bin/env python3
"""Audit the original #355 GitHub Actions ZIP without rerunning the replay.

Usage: python3 scripts/pr57-355-audit.py artifact.zip
Checks ZIP integrity, selected original raw log records, and byte-for-byte
checkpoint equality. Does not claim causal closure or throughput improvement.
"""
import hashlib
import re
import sys
import zipfile
from pathlib import Path

assert len(sys.argv) == 2, "usage: pr57-355-audit.py artifact.zip"
path = Path(sys.argv[1])
zip_sha = hashlib.sha256(path.read_bytes()).hexdigest()
with zipfile.ZipFile(path) as z:
    assert z.testzip() is None, "corrupt ZIP entry"
    names = set(z.namelist())
    for required in ("probe/full.log", "control/full.log", "probe/checkpoint-10800.json", "control/checkpoint-10800.json"):
        assert required in names, required
    probe = z.read("probe/full.log").decode()
    control = z.read("control/full.log").decode()
    checkpoints = [z.read(f"{mode}/checkpoint-10800.json") for mode in ("control", "probe")]
    assert checkpoints[0] == checkpoints[1], "checkpoint mismatch"
    checkpoint_sha = hashlib.sha256(checkpoints[0]).hexdigest()
    def records(text, prefix):
        result = []
        for line in text.splitlines():
            if not line.startswith(prefix + " "):
                continue
            result.append({key: value for key, value in re.findall(r"([A-Za-z][A-Za-z0-9]*)=([^\s]+)", line)})
        return result
    outcomes = records(probe, "PR57_355_OUTPUT")
    assert not records(control, "PR57_355_OUTPUT")
    assert len(outcomes) == 2, outcomes
    assert [int(x["window"]) for x in outcomes] == [40001, 50001]
    assert len(records(probe, "PR57_351_WITNESS")) == 6
    assert len(records(probe, "PR57_353_JOURNAL")) == 2
    assert len(records(probe, "PR57_354_INPLACE")) == 2
    assert len(records(probe, "PR57_337_HOUR")) == 6
    assert len(records(control, "PR57_337_HOUR")) == 6
    assert "parity=IDENTICAL" in probe and "parity=IDENTICAL" in control
    for row in outcomes:
        assert int(row["eligible"]) == 60
        assert int(row["compared"]) == 40
        for field in ("winnerSame", "distanceBitsSame", "stepBitsSame", "readerSame", "allSame", "clockChanged"):
            assert int(row[field]) == 40, (row["window"], field, row[field])
        assert int(row["entryChanged"]) == 0
        assert row["canonicalScanAlwaysRuns"] == "True"
        assert row["notCausalClosure"] == "True"
    print(f"PR57_355_AUDITED zipSha256={zip_sha} zipCRC=OK "
          f"windows=2 pairs=80 exactOutputsSame=80 clockChanged=80 "
          f"checkpointBytes={len(checkpoints[0])} checkpointSha256={checkpoint_sha} "
          "byteIdentical=True noSkippedSlices=True observational=True")
