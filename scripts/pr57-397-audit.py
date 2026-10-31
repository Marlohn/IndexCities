#!/usr/bin/env python3
"""Independent audit of original PR57 #396/#397 Actions ZIPs (no engine run).

Usage: python3 scripts/pr57-397-audit.py original396.zip original397.zip
Checks SHA/CRC, save parity, host cadence, phase counters and exact net-boundary
classification. This does not prove writer-complete causal closure or speedup.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

EXPECTED = {
    396: ("3faec2d36a35ad1218cbfce9b23b591e5409bf1fecbc38e1f5f13c0fcad07c5a",
          [20,1,14,11,0,0], [4,1,0,11,0,0], [16,0,14,0,0,0]),
    397: ("2b089dd0607dc1e10197bcec67f3460f3877e9aae7937aede32b50b85ab4b971",
          [14,0,0,11,0,0], [0,0,0,11,0,0], [14,0,0,0,0,0]),
}
SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
MEMBERS = {"base/checkpoint-10800.json","probe/checkpoint-10800.json",
           "base/full.log","probe/full.log","summary.json","checkpoints.sha256"}

def rows(lines,prefix):
    return [dict(token.split("=",1) for token in line.split()[1:] if "=" in token)
            for line in lines if line.startswith(prefix+" ")]

def audit(path,n):
    raw=Path(path).read_bytes()
    digest,events,route,admission=EXPECTED[n]
    assert hashlib.sha256(raw).hexdigest()==digest
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist())==MEMBERS
        a=z.read("base/checkpoint-10800.json")
        b=z.read("probe/checkpoint-10800.json")
        assert a==b and hashlib.sha256(a).hexdigest()==SAVE_SHA
        summary=json.loads(z.read("summary.json"))
        assert summary["advance2PerArm"]==10800
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["optimizationGainAccepted"]
        logs={}
        for arm in ("base","probe"):
            lines=z.read(f"{arm}/full.log").decode().splitlines()
            hours=rows(lines,"PR57_337_HOUR")
            boundary=rows(lines,"PR57_ADVANCE_BOUNDARY")
            final=rows(lines,"PR57_337_FINAL")
            assert len(hours)==6 and len(boundary)==108 and len(final)==1
            assert int(boundary[-1]["calls"])==10800 and final[0]["parity"]=="IDENTICAL"
            assert all(int(h["living"])==121 for h in hours)
            assert sum(int(h["totalAllocatedBytes"]) for h in hours)==summary["arms"][arm]["allocatedBytes"]
            logs[arm]=hours
        for left,right in zip(logs["base"],logs["probe"]):
            for key in ("living","ledger","history","slices","actors","claimChanges","spaceCountChanges"):
                assert left[key]==right[key],key
        q=summary["arms"]["probe"][f"boundary{n}"]
        assert int(q["physical"])==12000 and int(q["roster"])==0
        assert q["observational"]=="True" and q["noSkippedSlices"]=="True"
        def numbers(k):return [int(v) for v in q[k].split(",")]
        assert numbers("events")==events and numbers("route")==route
        assert numbers("admission")==admission and numbers("site")==[0]*6
        changes=summary["arms"]["probe"][f"changes{n}"]
        assert int(q["traces"])==len(changes)==sum(events)
        assert not summary["arms"]["base"].get(f"boundary{n}")
        if n==396:
            assert int(q["physicalCalls"])==12000 and int(q["operationCalls"])==1997
            b395=summary["arms"]["probe"]["boundary395"]
            assert (int(b395["inside"]),int(b395["outside"]))==(20,26)
        else:
            # 14 changes previously called "between Unsplit" are in the NEXT
            # physical call's pre-actor cleanup; 11 route changes are true gaps.
            assert numbers("admission")[0]==14 and numbers("route")[3]==11
        print(json.dumps({"experiment":n,"zipSha256":digest,"crc":"PASS",
            "saveParity":True,"saveBytes":len(a),"advance2PerArm":10800,
            "events":events,"route":route,"admission":admission,
            "wallMs":{arm:summary["arms"][arm]["wallMs"] for arm in ("base","probe")},
            "causalClosure":False,"optimizationGainAccepted":False},sort_keys=True))

if __name__=="__main__":
    assert len(sys.argv)==3,"usage: pr57-397-audit.py original396.zip original397.zip"
    audit(sys.argv[1],396)
    audit(sys.argv[2],397)
