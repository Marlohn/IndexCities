#!/usr/bin/env python3
"""PR57 #388: independently audit original Actions ZIP and the bounded cross-cohort trace.

Usage: python3 scripts/pr57-388-audit.py artifact.zip [expected_sha256]
Observational lower bound; does not establish counterfactual outcome or acceleration.
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
def rows(data,prefix):
    return [dict(v.split("=",1) for v in line.split()[1:] if "=" in v)
            for line in data.decode().splitlines() if line.startswith(prefix+" ")]

def audit(path,expected=None):
    raw=path.read_bytes()
    sha=hashlib.sha256(raw).hexdigest()
    if expected:
        assert sha==expected.lower(),(sha,expected)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None,"bad CRC"
        expected_names={"summary.json","checkpoints.sha256"} | {
            f"{a}/{f}" for a in ("base","probe")
            for f in ("full.log","checkpoint-10800.json")}
        assert set(z.namelist())==expected_names,set(z.namelist())
        saves=[z.read(f"{a}/checkpoint-10800.json") for a in ("base","probe")]
        assert saves[0]==saves[1] and len(saves[0])==2254838
        assert hashlib.sha256(saves[0]).hexdigest()==SAVE_SHA
        summary=json.loads(z.read("summary.json"))
        assert summary["advance2PerArm"]==10800
        assert summary["sampledPhysicalCalls"]==600
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["acceptedSpeedup"]
        hour_records={}
        traces={}
        for a in ("base","probe"):
            data=z.read(f"{a}/full.log")
            hour=rows(data,"PR57_337_HOUR")
            boundary=rows(data,"PR57_ADVANCE_BOUNDARY")
            final=rows(data,"PR57_337_FINAL")
            order=rows(data,"PR57_387_ORDER")
            trace=rows(data,"PR57_388_CROSS")
            assert len(hour)==6 and len(boundary)==108 and len(final)==1
            assert int(boundary[-1]["calls"])==10800
            assert final[0]["parity"]=="IDENTICAL"
            assert final[0]["sha256"].lower()==SAVE_SHA
            assert all(int(h["living"])==121 for h in hour)
            assert len(order)==len(trace)==(1 if a=="probe" else 0)
            if order:
                d={k:int(order[0][k]) for k in COUNTS}
                assert d["physicalCalls"]==600 and d["crossCohortSelected"]==1
                assert d["selectedAfterPose"]>=d["crossCohortSelected"]
                t=trace[0]
                assert 1<=int(t["call"])<=600
                assert t["observationOnly"]=="True"
                assert t["readerCohortWait"]!=t["blockerCohortWait"]
                assert int(t["reader"])!=int(t["blocker"])
                assert any(t["blockerPre"+k]!=t["blockerPost"+k]
                           for k in ("X","Z","Heading","Status"))
                # In this exact observed straight-west lane, the canonical
                # leader distance uses the POST-writer position, not PRE.
                pre_distance=float(t["readerX"])-float(t["blockerPreX"])
                post_distance=float(t["readerX"])-float(t["blockerPostX"])
                assert abs(post_distance-float(t["leaderDistance"]))<1e-9
                assert abs(post_distance-pre_distance)>2.0
                assert summary["arms"][a]["crossTrace"]==t
                assert {k:int(v) for k,v in summary["arms"][a]["shadow"].items()
                        if k in COUNTS}==d
                traces[a]=t
            hour_records[a]=hour
        for key in ("hour","slices","actors","claimChanges","spaceCountChanges",
                    "living","ledger","history"):
            assert [x[key] for x in hour_records["base"]]==[
                x[key] for x in hour_records["probe"]],key
        return {"zipSha256":sha,"saveSha256":SAVE_SHA,"crossCohortTrace":traces["probe"],
                "preWriterDistanceMeters":pre_distance,
                "postWriterDistanceMeters":post_distance,
                "distanceChangeMeters":post_distance-pre_distance,
                "observational":True,"causalClosure":False,"acceptedSpeedup":False}

if __name__=="__main__":
    if len(sys.argv) not in (2,3):
        raise SystemExit(__doc__)
    print(json.dumps(audit(Path(sys.argv[1]),
        sys.argv[2] if len(sys.argv)==3 else None),indent=2))
