#!/usr/bin/env python3
"""Independent audit of the original PR57 #386 GitHub Actions artifact ZIP.

Usage: python3 scripts/pr57-386-audit.py artifact.zip [expected_zip_sha256]
No .NET, external packages or benchmark claims. The uploaded summary.json
is empty in run #40962447451 because the workflow's post-replay Python
parser had an indentation error; reconstruct all totals from original logs.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ARMS=("baseA","probeA","probeB","baseB")
SAVE_SHA="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
FIELDS=("total","gate","snapshot","forecast","unsplit","remainder")
COUNTS=("steps","batchedSteps","physicalCalls","operationalCalls","directCalls")

def rows(data,prefix):
    return [dict(v.split("=",1) for v in line.split()[1:] if "=" in v)
            for line in data.decode().splitlines() if line.startswith(prefix+" ")]

def audit(path,expected=None):
    digest=hashlib.sha256(path.read_bytes()).hexdigest()
    if expected:
        assert digest==expected.lower(),(digest,expected)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None,"CRC error"
        names=set(z.namelist())
        assert names==({"summary.json","checkpoints.sha256"} |
                       {f"{a}/{n}" for a in ARMS
                        for n in ("full.log","checkpoint-10800.json")}),names
        assert len(z.read("summary.json"))==0,"Unexpected summary: check parser status"
        saves=[z.read(f"{a}/checkpoint-10800.json") for a in ARMS]
        assert all(s==saves[0] for s in saves)
        assert len(saves[0])==2254838
        assert hashlib.sha256(saves[0]).hexdigest()==SAVE_SHA
        result={}
        for a in ARMS:
            log=z.read(f"{a}/full.log")
            outer=rows(log,"PR57_386_OUTER")
            phase=rows(log,"PR57_384_HOUR")
            seg=rows(log,"PR57_385_SEGMENT")
            hours=rows(log,"PR57_337_HOUR")
            boundary=rows(log,"PR57_ADVANCE_BOUNDARY")
            final=rows(log,"PR57_337_FINAL")
            assert (len(outer),len(phase),len(seg),len(hours),
                    len(boundary),len(final))==(6,6,12,6,108,1)
            assert final[0]["parity"]=="IDENTICAL"
            assert int(boundary[-1]["calls"])==10800
            assert all(int(h["living"])==121 for h in hours)
            assert final[0]["sha256"].lower()==SAVE_SHA
            for o in outer:
                assert o["enabled"]==("True" if a.startswith("probe") else "False")
                assert o["noSkippedSlices"]=="True" and o["noDecisionReuse"]=="True"
                assert abs(sum(float(o[f+"Ms"]) for f in FIELDS[1:])-
                           float(o["totalMs"]))<.01
            counts={k:sum(int(o[k]) for o in outer) for k in COUNTS}
            ms={k:round(sum(float(o[k+"Ms"]) for o in outer),3) for k in FIELDS}
            wall=round(sum(float(h["wallMs"]) for h in hours),3)
            vehicle=round(sum(float(h["vehicleMs"]) for h in phase),3)
            if a.startswith("probe"):
                assert counts=={"steps":10800,"batchedSteps":10800,
                                "physicalCalls":64819,"operationalCalls":10800,
                                "directCalls":0},counts
                assert ms["total"]<=vehicle+10
                assert ms["remainder"]>0
            else:
                assert all(v==0 for v in counts.values())
                assert all(v==0 for v in ms.values())
            result[a]={"wallMs":wall,"vehiclePhaseMs":vehicle,
                       "outerMs":ms,"outerCounts":counts}
        for a in ("probeA","probeB"):
            v=result[a]
            v["outsideUnsplitMs"]=round(v["outerMs"]["total"]-
                                         v["outerMs"]["unsplit"],3)
            v["forecastShareOfOutsidePct"]=round(
                100*v["outerMs"]["forecast"]/v["outsideUnsplitMs"],2)
            v["idealizedOutsideUnsplitSpeedup"]=round(
                v["wallMs"]/(v["wallMs"]-v["outsideUnsplitMs"]),4)
        return {"zipSha256":digest,"saveSha256":SAVE_SHA,"arms":result,
                "causalClosure":False,"acceptedSpeedup":False,
                "workflowParserStatus":"FAILED_AFTER_FOUR_VALID_REPLAYS"}

if __name__=="__main__":
    if len(sys.argv) not in (2,3):
        raise SystemExit(__doc__)
    print(json.dumps(audit(Path(sys.argv[1]),
                           sys.argv[2] if len(sys.argv)==3 else None),indent=2))
