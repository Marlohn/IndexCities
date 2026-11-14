#!/usr/bin/env python3
"""Independently audit PR57 #391/#392 original GitHub Actions ZIP artifacts.

Usage: python3 scripts/pr57-392-audit.py artifact.zip expected_sha256
Observational subset only; no causal certificate or accepted speedup.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

def rows(log, prefix):
    return [dict(part.split("=",1) for part in line.split()[1:] if "=" in part)
            for line in log.splitlines() if line.startswith(prefix+" ")]

def main():
    assert len(sys.argv)==3, __doc__
    raw=Path(sys.argv[1]).read_bytes()
    digest=hashlib.sha256(raw).hexdigest()
    assert digest==sys.argv[2], (digest,sys.argv[2])
    with zipfile.ZipFile(sys.argv[1]) as z:
        assert z.testzip() is None
        names=z.namelist()
        def member(name):
            found=[n for n in names if n==name or n.endswith("/"+name)]
            assert len(found)==1,(name,found)
            return z.read(found[0])
        summary=json.loads(member("summary.json"))
        assert summary["advance2PerArm"]==10800
        assert summary["sampledPhysicalCalls"]==600
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["optimizationGainAccepted"]
        saves=[member(f"{arm}/checkpoint-10800.json") for arm in ("base","probe")]
        assert saves[0]==saves[1]
        out={}
        for arm in ("base","probe"):
            log=member(f"{arm}/full.log").decode()
            hours=rows(log,"PR57_337_HOUR")
            boundary=rows(log,"PR57_ADVANCE_BOUNDARY")
            final=rows(log,"PR57_337_FINAL")
            shadow=rows(log,"PR57_391_SHADOW")
            assert len(hours)==6 and len(boundary)==108 and len(final)==1
            assert int(boundary[-1]["calls"])==10800
            assert final[0]["parity"]=="IDENTICAL"
            assert all(int(h["living"])==121 for h in hours)
            assert len(shadow)==(1 if arm=="probe" else 0)
            arm_summary=summary["arms"][arm]
            assert arm_summary["wallMs"]==round(sum(float(h["wallMs"]) for h in hours),3)
            assert arm_summary["cpuMs"]==round(sum(float(h["cpuMs"]) for h in hours),3)
            assert arm_summary["allocatedBytes"]==sum(int(h["totalAllocatedBytes"]) for h in hours)
            out[arm]={k:arm_summary[k] for k in ("wallMs","cpuMs","allocatedBytes")}
            if shadow:
                s=shadow[0]
                assert s["observational"]=="True" and s["noDecisionReuse"]=="True"
                assert s["noSkippedSlices"]=="True" and int(s["physicalCalls"])==600
                assert int(s["missing"])==0
                assert int(s["candidateReads"])>0
                assert int(s["staleReads"])>=len(rows(log,"PR57_391_STALE"))
                assert int(s["frequency"])>0
                assert arm_summary["shadow"]["staleReads"]==int(s["staleReads"])
                ms=(int(s["buildTicks"])+int(s["publishTicks"]))*1000/int(s["frequency"])
                assert abs(ms-arm_summary["shadow"]["maintenanceMs"])<=0.001
                out["shadow"]={k:int(v) for k,v in s.items() if v.isdecimal()}
                out["shadow"]["maintenanceMs"]=round(ms,3)
                split=rows(log,"PR57_392_SPLIT")
                if split:
                    assert len(split)==1
                    x=split[0]
                    assert int(x["physicalCalls"])==600
                    assert int(x["selfStale"])+int(x["crossStale"])==int(s["staleReads"])
                    assert int(x["crossStale"])>=len(rows(log,"PR57_392_CROSS"))
                    assert arm_summary["split"]["crossStale"]==int(x["crossStale"])
                    out["split"]={k:int(v) for k,v in x.items() if v.isdecimal()}
        print(json.dumps({"artifactSha256":digest,"zipMembers":len(names),
              "saveSha256":hashlib.sha256(saves[0]).hexdigest(),
              "saveBytes":len(saves[0]),"arms":out,"acceptedGain":False},
              sort_keys=True,indent=2))
if __name__=="__main__":
    main()
