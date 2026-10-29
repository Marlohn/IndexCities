#!/usr/bin/env python3
"""Independent offline audit of PR57 #387 original Actions ZIP (stdlib only).
Usage: python3 scripts/pr57-387-audit.py artifact.zip [expected_zip_sha256]
This checks the observational replay, NOT causal closure or a speedup.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

SAVE_SHA="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
COUNTS=("physicalCalls","poseWriters","otherWriters","laneCandidateReads",
        "liveReadsAfterPose","crossCohortReads","selectedAfterPose",
        "crossCohortSelected")
INVARIANTS=("hour","slices","actors","claimChanges","spaceCountChanges",
            "living","ledger","history")

def rows(data,prefix):
    return [dict(x.split("=",1) for x in line.split()[1:] if "=" in x)
            for line in data.decode().splitlines() if line.startswith(prefix+" ")]

def audit(path,expected=None):
    raw=path.read_bytes()
    digest=hashlib.sha256(raw).hexdigest()
    if expected is not None:
        assert digest==expected.lower(),(digest,expected)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None,"CRC failure"
        expected_files={"summary.json","checkpoints.sha256"} | {
            f"{arm}/{name}" for arm in ("base","probe")
            for name in ("full.log","checkpoint-10800.json")}
        assert set(z.namelist())==expected_files
        saves=[z.read(f"{a}/checkpoint-10800.json") for a in ("base","probe")]
        assert saves[0]==saves[1] and len(saves[0])==2254838
        assert hashlib.sha256(saves[0]).hexdigest()==SAVE_SHA
        summary=json.loads(z.read("summary.json"))
        assert summary["advance2PerArm"]==10800
        assert summary["sampledPhysicalCalls"]==600
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["acceptedSpeedup"]
        hours={}
        result={}
        for arm in ("base","probe"):
            data=z.read(f"{arm}/full.log")
            hour=rows(data,"PR57_337_HOUR")
            boundary=rows(data,"PR57_ADVANCE_BOUNDARY")
            final=rows(data,"PR57_337_FINAL")
            order=rows(data,"PR57_387_ORDER")
            assert len(hour)==6 and len(boundary)==108 and len(final)==1
            assert int(boundary[-1]["calls"])==10800
            assert final[0]["parity"]=="IDENTICAL"
            assert final[0]["sha256"].lower()==SAVE_SHA
            assert all(int(x["living"])==121 for x in hour)
            assert len(order)==(1 if arm=="probe" else 0)
            hours[arm]=hour
            if arm=="probe":
                d={k:int(order[0][k]) for k in COUNTS}
                assert d["physicalCalls"]==600
                assert order[0]["noReorder"]=="True"
                assert order[0]["noSkippedSlices"]=="True"
                assert all(v>=0 for v in d.values())
                assert d["crossCohortReads"]<=d["liveReadsAfterPose"]<=d["laneCandidateReads"]
                assert d["crossCohortSelected"]<=d["selectedAfterPose"]
                result=d
            assert abs(sum(float(x["wallMs"]) for x in hour)-
                       summary["arms"][arm]["wallMs"])<.001
            assert sum(int(x["totalAllocatedBytes"]) for x in hour)==summary["arms"][arm]["allocatedBytes"]
        for key in INVARIANTS:
            assert [x[key] for x in hours["base"]]==[x[key] for x in hours["probe"]],key
        assert {k:int(v) for k,v in summary["arms"]["probe"]["shadow"].items()
                if k in COUNTS}==result
        return {"zipSha256":digest,"saveSha256":SAVE_SHA,
                "first600PhysicalCalls":result,
                "wallMs":{a:summary["arms"][a]["wallMs"] for a in ("base","probe")},
                "causalClosure":False,"acceptedSpeedup":False,
                "interpretation":"candidate enumeration and selected-winner lower bounds, not causal equivalence"}

if __name__=="__main__":
    if len(sys.argv) not in (2,3):
        raise SystemExit(__doc__)
    print(json.dumps(audit(Path(sys.argv[1]),
                           sys.argv[2] if len(sys.argv)==3 else None),indent=2))
