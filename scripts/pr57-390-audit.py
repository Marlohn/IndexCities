#!/usr/bin/env python3
"""Independent audit of original PR57 #390 Actions ZIP.

Usage: python3 scripts/pr57-390-audit.py original.zip [zip_sha256]
Observational census only; no accepted optimization or causal closure.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

SAVE_SHA="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
KEYS=("hour","slices","actors","claimChanges","spaceCountChanges","living","ledger","history")

def rows(data,prefix):
    return [dict(kv.split("=",1) for kv in line.split()[1:] if "=" in kv)
            for line in data.decode().splitlines() if line.startswith(prefix+" ")]

def audit(path,expected=None):
    zip_sha=hashlib.sha256(path.read_bytes()).hexdigest()
    if expected: assert zip_sha==expected.lower()
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        names=set(z.namelist())
        expected_names={"summary.json","checkpoints.sha256"}|{
            f"{arm}/{name}" for arm in ("base","probe")
            for name in ("full.log","checkpoint-10800.json")}
        assert names==expected_names,(names,expected_names)
        saves=[z.read(f"{arm}/checkpoint-10800.json") for arm in ("base","probe")]
        assert saves[0]==saves[1]
        assert hashlib.sha256(saves[0]).hexdigest()==SAVE_SHA
        summary=json.loads(z.read("summary.json"))
        assert summary["advance2PerArm"]==10800 and summary["sampledPhysicalCalls"]==600
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["optimizationGainAccepted"]
        hour_rows={}
        for arm in ("base","probe"):
            data=z.read(f"{arm}/full.log")
            hours=rows(data,"PR57_337_HOUR")
            boundaries=rows(data,"PR57_ADVANCE_BOUNDARY")
            final=rows(data,"PR57_337_FINAL")
            assert len(hours)==6 and len(boundaries)==108 and len(final)==1
            assert int(boundaries[-1]["calls"])==10800
            assert final[0]["parity"]=="IDENTICAL" and final[0]["sha256"].lower()==SAVE_SHA
            assert all(int(h["living"])==121 for h in hours)
            r=summary["arms"][arm]
            assert round(sum(float(h["wallMs"]) for h in hours),3)==r["wallMs"]
            assert round(sum(float(h["cpuMs"]) for h in hours),3)==r["cpuMs"]
            assert sum(int(h["totalAllocatedBytes"]) for h in hours)==r["allocatedBytes"]
            hour_rows[arm]=hours
        for a,b in zip(hour_rows["base"],hour_rows["probe"]):
            assert all(a[k]==b[k] for k in KEYS)
        assert not rows(z.read("base/full.log"),"PR57_390_CELL")
        cells=rows(z.read("probe/full.log"),"PR57_390_CELL")
        assert sorted(int(c["cell"]) for c in cells)==list(range(12))
        for c in cells:
            i=int(c["cell"])
            assert (int(c["waitCohort"]),int(c["enteringLane"]),int(c["outcome"]))==(
                i//6,(i%6)//3,i%3)
            assert int(c["physicalCalls"])==600
            assert c["observationOnly"]==c["noReorder"]==c["noSkippedSlices"]=="True"
            assert 0<=int(c["finalZero"])<=int(c["calls"])
            assert 0<=int(c["rawZero"])<=int(c["calls"])
        freq={int(c["frequency"]) for c in cells}
        assert len(freq)==1 and next(iter(freq))>0
        total=lambda k:sum(int(c[k]) for c in cells)
        lane=summary["arms"]["probe"]["lane"]
        for k in ("calls","candidates","ticks","finalZero","rawZero"):
            assert total(k)==lane[k],k
        assert lane["frequency"]==next(iter(freq))
        assert lane["scanMs"]==round(total("ticks")*1000/lane["frequency"],3)
        outcomes={str(i):sum(int(c["calls"]) for c in cells if int(c["outcome"])==i)
                  for i in range(3)}
        assert outcomes==lane["outcomes"]
        return {"zipSha256":zip_sha,"saveSha256":SAVE_SHA,"saveBytes":len(saves[0]),
                "laneCalls":total("calls"),"laneCandidates":total("candidates"),
                "laneScanMs":lane["scanMs"],"laneOutcomes":outcomes,
                "finalZero":total("finalZero"),
                "waitingActorCalls":sum(int(c["calls"]) for c in cells
                                        if int(c["waitCohort"])==1),
                "bindingWaitingActorCalls":sum(int(c["calls"]) for c in cells
                                        if int(c["waitCohort"])==1 and int(c["outcome"])==2),
                "wallMs":{a:summary["arms"][a]["wallMs"] for a in ("base","probe")},
                "acceptedGain":False}

if __name__=="__main__":
    assert len(sys.argv) in (2,3)
    print(json.dumps(audit(Path(sys.argv[1]),sys.argv[2] if len(sys.argv)==3 else None),
                     sort_keys=True))
