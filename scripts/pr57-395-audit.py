#!/usr/bin/env python3
"""Independent audit of original PR57 #395 GitHub Actions ZIP (no engine execution).

Usage: python3 scripts/pr57-395-audit.py /path/to/original-artifact.zip
Verifies CRC, raw save identity, replay boundaries, event counts and #394/#395
observations. This is NOT proof of causal closure or a throughput A/B.
"""
import hashlib
import json
import sys
import zipfile
from collections import Counter
from pathlib import Path

def rows(lines, prefix):
    return [dict(token.split("=", 1) for token in line.split()[1:] if "=" in token)
            for line in lines if line.startswith(prefix + " ")]

def main(path):
    raw=Path(path).read_bytes()
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, "CRC mismatch"
        expected={"base/checkpoint-10800.json","probe/checkpoint-10800.json",
                  "base/full.log","probe/full.log","summary.json","checkpoints.sha256"}
        assert set(z.namelist())==expected, z.namelist()
        a=z.read("base/checkpoint-10800.json")
        b=z.read("probe/checkpoint-10800.json")
        assert a==b, "canonical save parity failed"
        sha=hashlib.sha256(a).hexdigest()
        assert sha=="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
        summary=json.loads(z.read("summary.json"))
        assert summary["advance2PerArm"]==10800
        assert summary["boundaryCalls395"]==12000
        assert summary["sampledPhysicalCalls"]==600
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["optimizationGainAccepted"]
        logs={}
        for arm in ("base","probe"):
            lines=z.read(f"{arm}/full.log").decode("utf-8").splitlines()
            hours=rows(lines,"PR57_337_HOUR")
            boundary=rows(lines,"PR57_ADVANCE_BOUNDARY")
            final=rows(lines,"PR57_337_FINAL")
            assert len(hours)==6 and len(boundary)==108 and len(final)==1
            assert int(boundary[-1]["calls"])==10800
            assert final[0]["parity"]=="IDENTICAL"
            assert all(int(h["living"])==121 for h in hours)
            assert summary["arms"][arm]["allocatedBytes"]==sum(
                int(h["totalAllocatedBytes"]) for h in hours)
            logs[arm]={k:rows(lines,k) for k in
                ("PR57_337_HOUR","PR57_394_PATH","PR57_395_BOUNDARY","PR57_395_CHANGE")}
        for left,right in zip(logs["base"]["PR57_337_HOUR"],logs["probe"]["PR57_337_HOUR"]):
            for key in ("living","ledger","history","slices","actors","claimChanges","spaceCountChanges"):
                assert left[key]==right[key],(key,left[key],right[key])
        assert not logs["base"]["PR57_394_PATH"]
        assert not logs["base"]["PR57_395_BOUNDARY"]
        assert not logs["base"]["PR57_395_CHANGE"]
        shadow=logs["probe"]["PR57_394_PATH"]
        boundary=logs["probe"]["PR57_395_BOUNDARY"]
        changes=logs["probe"]["PR57_395_CHANGE"]
        assert len(shadow)==len(boundary)==1
        s=shadow[0]; q=boundary[0]
        assert int(s["physical"])==int(q["physical"])==12000
        assert int(s["samples"])==600 and int(s["betweenSamples"])==18
        assert int(s["reads"])==1538012 and int(s["cross"])==0
        assert int(q["calls"])==12000 and int(q["rosterChanges"])==0
        assert int(q["inside"])==20 and int(q["outside"])==26
        assert int(q["insideRoute"])==4 and int(q["outsideRoute"])==12
        assert int(q["insideAdmission"])==16 and int(q["outsideAdmission"])==14
        assert int(q["insideSite"])==int(q["outsideSite"])==0
        assert q["netBoundariesOnly"]=="True" and q["observational"]=="True"
        assert int(q["traces"])==len(changes)==32
        assert summary["arms"]["probe"]["boundary395"]["inside"]==int(q["inside"])
        assert summary["arms"]["probe"]["boundary395"]["outside"]==int(q["outside"])
        print(json.dumps({"artifactSha256":hashlib.sha256(raw).hexdigest(),
              "crc":"PASS","members":len(expected),"saveBytes":len(a),"saveSha256":sha,
              "replayAdvance2PerArm":10800,"hoursPerArm":6,
              "sampledPhysical":600,"allPhysicalBoundaries":12000,
              "withinUnsplitNet":int(q["inside"]),"betweenMeasuredBoundariesNet":int(q["outside"]),
              "routeInside":4,"routeOutside":12,"admissionInside":16,"admissionOutside":14,
              "siteInside":0,"siteOutside":0,"rosterChanges":0,
              "loggedEvents":len(changes),"logClasses":dict(Counter(x["boundary"] for x in changes)),
              "sameSaves":True,"observational":True,"causalClosure":False},
              sort_keys=True))
if __name__=="__main__":
    assert len(sys.argv)==2,"Usage: pr57-395-audit.py original.zip"
    main(sys.argv[1])
