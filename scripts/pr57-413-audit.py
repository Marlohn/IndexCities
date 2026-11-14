#!/usr/bin/env python3
"""#413 independent audit of #412 original GitHub Actions ZIP.

Usage: python3 scripts/pr57-413-audit.py pr57-412-original.zip
Observational only: scalar cross-read parity is not causal closure or a speedup.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ZIP_SHA="b94ca5a318d7da81161e84475bed9a5e5ae8a036127df1c258c78446973f29ae"
SAVE_SHA="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
FILES={"base/checkpoint-10800.json","probe/checkpoint-10800.json",
       "base/full.log","probe/full.log","checkpoints.sha256","summary.json"}

def rows(text,prefix):
    return [dict(item.split("=",1) for item in line.split()[1:] if "=" in item)
            for line in text.splitlines() if line.startswith(prefix+" ")]

def main(path):
    raw=Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest()==ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist())==FILES
        a=z.read("base/checkpoint-10800.json")
        b=z.read("probe/checkpoint-10800.json")
        assert a==b and len(a)==2254838 and hashlib.sha256(a).hexdigest()==SAVE_SHA
        packaged=json.loads(z.read("summary.json"))
        for arm in ("base","probe"):
            log=z.read(f"{arm}/full.log").decode()
            hour=rows(log,"PR57_337_HOUR")
            boundary=rows(log,"PR57_ADVANCE_BOUNDARY")
            final=rows(log,"PR57_337_FINAL")
            p408=rows(log,"PR57_408_SUMMARY")
            p410=rows(log,"PR57_410_SUMMARY")
            p412=rows(log,"PR57_412_SUMMARY")
            witnesses=rows(log,"PR57_412_STALE")+rows(log,"PR57_412_MISMATCH")
            assert len(hour)==6 and len(boundary)==108 and len(final)==1
            assert int(boundary[-1]["calls"])==10800
            assert all(int(h["living"])==121 for h in hour)
            assert final[0]["parity"]=="IDENTICAL" and final[0]["sha256"].lower()==SAVE_SHA
            assert hour[-1]["ledger"]=="4085" and hour[-1]["history"]=="7430"
            assert packaged["arms"][arm]["final"]==final[0]
            assert abs(sum(float(h["wallMs"]) for h in hour)-packaged["arms"][arm]["wallMs"])<.0011
            assert sum(int(h["totalAllocatedBytes"]) for h in hour)==packaged["arms"][arm]["allocatedBytes"]
            if arm=="probe":
                assert len(p408)==len(p410)==len(p412)==1
                p=p412[0];q=p410[0];r=p408[0]
                assert int(p["physical"])==int(r["physical"])==600
                assert int(p["scans"])==int(q["scans"])==int(r["scans"])==16255
                assert int(p["candidateReads"])==int(q["candidates"])==1592990
                assert int(p["ordinary"])==int(q["bindingOrdinary"])==11045
                assert int(p["ordinaryExact"])<=int(p["ordinarySameId"])<=11045
                assert int(p["snapshots"])==int(p["publications"])==58800
                assert int(p["witnesses"])==len(witnesses)
                assert int(p["canonicalStepMismatch"])>=int(q["stepMismatch"])-int(p["directMismatch"])
                assert p["observational"]=="True" and p["causalClosure"]=="False"
                print("PROJECTION",json.dumps(p,sort_keys=True))
            else:
                assert not (p408 or p410 or p412 or witnesses)
            print("ARM",arm,packaged["arms"][arm]["wallMs"],
                  packaged["arms"][arm]["cpuMs"],packaged["arms"][arm]["allocatedBytes"])
    print("PR57_413_AUDIT PASS",ZIP_SHA,SAVE_SHA)

if __name__=="__main__":
    main(sys.argv[1])
