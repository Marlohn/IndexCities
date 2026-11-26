#!/usr/bin/env python3
"""Offline integrity/parity and exact-scalar-writer census for PR57 #367.

Usage: python3 scripts/pr57-367-audit.py <raw-action-artifact.zip>
This checks the Action's raw ZIP, not the workflow's own summary.
"""
import hashlib
import sys
import zipfile
from collections import defaultdict
from pathlib import Path

p=Path(sys.argv[1]); digest=hashlib.sha256(p.read_bytes()).hexdigest()
with zipfile.ZipFile(p) as z:
    assert z.testzip() is None, "ZIP CRC failed"
    expected={"checkpoints.sha256","control/census.log","control/checkpoint-10800.json",
              "control/full.log","probe/census.log","probe/checkpoint-10800.json",
              "probe/full.log","summary.txt"}
    assert set(z.namelist())==expected, (set(z.namelist())^expected)
    final_control=z.read("control/checkpoint-10800.json")
    final_probe=z.read("probe/checkpoint-10800.json")
    assert final_control==final_probe, "final save mismatch"
    save_sha=hashlib.sha256(final_control).hexdigest()
    assert save_sha=="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
    logs={arm:z.read(f"{arm}/census.log").decode() for arm in ("control","probe")}
    def rows(arm,prefix):
        return [dict(t.split("=",1) for t in line.split()[1:] if "=" in t)
                for line in logs[arm].splitlines() if line.startswith(prefix+" ")]
    hours={arm:rows(arm,"PR57_337_HOUR") for arm in ("control","probe")}
    for arm,values in hours.items():
        assert len(values)==6 and [int(x["hour"]) for x in values]==list(range(1,7))
        assert "parity=IDENTICAL" in logs[arm]
    assert not any(line.startswith("PR57_367_") for line in logs["control"].splitlines())
    summary=rows("probe","PR57_367_SUMMARY")
    fields=rows("probe","PR57_367_FIELD")
    phases=rows("probe","PR57_367_PHASE")
    assert len(summary)==1 and fields and phases
    s=summary[0]
    assert int(s["canonical"])==98
    assert int(s["writes"])==sum(int(x["writes"]) for x in fields)
    assert int(s["writes"])==sum(int(x["writes"]) for x in phases)
    assert int(s["fields"])==len(fields) and int(s["phaseGroups"])==len(phases)
    field_counts={x["field"]:int(x["writes"]) for x in fields}
    assert len(field_counts)==len(fields)
    by_field=defaultdict(int)
    for x in phases:
        field,phase=x["key"].rsplit(":",1)
        assert 0<=int(phase)<=13
        by_field[field]+=int(x["writes"])
    assert dict(by_field)==field_counts
    for arm in ("control","probe"):
        assert "PR57_337_FINAL " in logs[arm]
    print("ZIP_SHA256",digest,"SAVE_SHA256",save_sha,"SAVE_BYTES",len(final_control))
    print("CANONICAL_WRITES",s["writes"],"NONCANONICAL_CALLBACKS",s["noncanonical"])
    print("FIELDS",field_counts)
    print("PHASES",{x["key"]:int(x["writes"]) for x in phases})
    for hour,(a,b) in enumerate(zip(hours["control"],hours["probe"]),1):
        print("HOUR",hour,"CONTROL_WALL_MS",a["wallMs"],"PROBE_WALL_MS",b["wallMs"],
              "DELTA_ALLOC_BYTES",int(b["totalAllocatedBytes"])-int(a["totalAllocatedBytes"]))
    print("RESULT parity=IDENTICAL crc=OK counts=CONSISTENT no_speedup_claim=True")
