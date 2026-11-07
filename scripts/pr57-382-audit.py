#!/usr/bin/env python3
"""PR57 #382: independent offline audit of the original Actions ZIP.

Usage: python3 scripts/pr57-382-audit.py ZIP [expected_zip_sha256]
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

def rows(data, prefix):
    return [dict(item.split("=",1) for item in line.split()[1:] if "=" in item)
            for line in data.decode().splitlines() if line.startswith(prefix+" ")]

def audit(path, expected=None):
    digest=hashlib.sha256(path.read_bytes()).hexdigest()
    if expected: assert digest==expected.lower(),(digest,expected)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        assert set(z.namelist())=={
            "checkpoints.sha256","summary.json",
            "control/census.log","control/full.log","control/checkpoint-10800.json",
            "regime/census.log","regime/full.log","regime/checkpoint-10800.json"}
        data=json.loads(z.read("summary.json"))
        saves=[z.read(f"{arm}/checkpoint-10800.json") for arm in ("control","regime")]
        assert saves[0]==saves[1] and len(saves[0])==2254838
        save_sha=hashlib.sha256(saves[0]).hexdigest()
        assert save_sha=="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
        assert data["causalClosure"] is False and data["acceptedSpeedup"] is False
        for arm in ("control","regime"):
            census=z.read(f"{arm}/census.log")
            final=rows(census,"PR57_337_FINAL")
            assert len(final)==1 and final[0]["parity"]=="IDENTICAL"
            assert final[0]["sha256"].lower()==save_sha
            hours=rows(census,"PR57_337_HOUR")
            assert len(hours)==6 and all(int(x["living"])==121 for x in hours)
            assert abs(sum(float(x["wallMs"]) for x in hours)-data["arms"][arm]["wallMs"])<.005
            assert sum(int(x["totalAllocatedBytes"]) for x in hours)==data["arms"][arm]["allocatedBytes"]
            assert len(rows(census,"PR57_377_SUMMARY"))==1
            assert len(rows(census,"PR57_379_SUMMARY"))==1
            assert len(rows(census,"PR57_381_SUMMARY"))==(1 if arm=="regime" else 0)
            assert len(rows(census,"PR57_382_SUMMARY"))==(2 if arm=="regime" else 0)
        census=z.read("regime/census.log")
        cost=rows(census,"PR57_382_SUMMARY")
        assert {x["regime"] for x in cost}=={"operational","physical"}
        for x in cost:
            assert x==data["cost"][x["regime"]]
            assert all(x[k]=="True" for k in
                       ("noCausalClosure","noDecisionReuse","noSkippedSlices","observational"))
            stages=("setup","cleanup","prepare","actor","towSync","post")
            assert sum(int(x[f"{k}Bytes"]) for k in stages)==int(x["allocatedBytes"])
            assert abs(sum(float(x[f"{k}Ms"]) for k in stages)-float(x["totalMs"]))<.02
        assert int(data["cost"]["operational"]["calls"])==84
        assert int(data["cost"]["physical"]["calls"])==516
        assert int(data["cost"]["operational"]["operationsNonNull"])==84
        assert int(data["cost"]["physical"]["operationsNonNull"])==0
        bound=rows(census,"PR57_382_BOUND")
        assert len(bound)==1 and bound[0]==data["bound"]
        total=sum(float(x["totalMs"]) for x in cost)
        share=100*float(data["cost"]["operational"]["totalMs"])/total
        assert abs(share-float(bound[0]["operationsPct"]))<.002
        assert int(bound[0]["measuredCalls"])==600
        print(json.dumps({"zip_sha256":digest,"crc":"OK","files":len(z.namelist()),
            "save_sha256":save_sha,"save_bytes":len(saves[0]),"arms":data["arms"],
            "cost":data["cost"],"operations_pct_of_unsplit":round(share,3),
            "max_unsplit_gain_if_operations_free":round(1/(1-share/100),4),
            "accepted_integrated_speedup":False},indent=2))

if __name__=="__main__":
    if len(sys.argv) not in (2,3): raise SystemExit("usage: audit.py ZIP [expected_sha256]")
    audit(Path(sys.argv[1]),sys.argv[2] if len(sys.argv)==3 else None)
