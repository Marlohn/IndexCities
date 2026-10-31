#!/usr/bin/env python3
"""PR57 #385: independent audit of original Actions artifact ZIP.

Usage: python3 scripts/pr57-385-audit.py ARTIFACT.zip [expected_sha256]
Only Python stdlib. No timing is treated as an accepted speedup or causal proof.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ARMS=("baseA","probeA","probeB","baseB")
REGIONS=("setup","cleanup","prepare","actorLoop","towSync","post")
SAVE_SHA="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"

def rows(data,prefix):
    return [dict(v.split("=",1) for v in line.split()[1:] if "=" in v)
            for line in data.decode().splitlines() if line.startswith(prefix+" ")]

def audit(path,expected=None):
    digest=hashlib.sha256(path.read_bytes()).hexdigest()
    if expected is not None:
        assert digest==expected.lower(),(digest,expected)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None,"ZIP CRC failure"
        names=set(z.namelist())
        assert names==({"summary.json","checkpoints.sha256"} |
                       {f"{arm}/{f}" for arm in ARMS
                        for f in ("full.log","checkpoint-10800.json")}),names
        data=json.loads(z.read("summary.json"))
        assert data["advance2PerArm"]==10800
        assert data["causalClosure"] is False
        assert data["acceptedSpeedup"] is False
        saves=[z.read(f"{arm}/checkpoint-10800.json") for arm in ARMS]
        assert all(s==saves[0] for s in saves)
        assert len(saves[0])==2254838
        assert hashlib.sha256(saves[0]).hexdigest()==SAVE_SHA
        result={}
        for arm in ARMS:
            log=z.read(f"{arm}/full.log")
            hours=rows(log,"PR57_337_HOUR")
            phases=rows(log,"PR57_384_HOUR")
            segments=rows(log,"PR57_385_SEGMENT")
            boundaries=rows(log,"PR57_ADVANCE_BOUNDARY")
            final=rows(log,"PR57_337_FINAL")
            assert (len(hours),len(phases),len(segments),len(boundaries),len(final))==(6,6,12,108,1)
            assert final[0]["parity"]=="IDENTICAL"
            assert final[0]["sha256"].lower()==SAVE_SHA
            assert int(boundaries[-1]["calls"])==10800
            assert all(int(x["living"])==121 for x in hours)
            summary=data["arms"][arm]
            assert abs(sum(float(x["wallMs"]) for x in hours)-summary["wallMs"])<.005
            assert sum(int(x["totalAllocatedBytes"]) for x in hours)==summary["allocatedBytes"]
            for hour in range(1,7):
                p=next(x for x in phases if int(x["hour"])==hour)
                two=[x for x in segments if int(x["hour"])==hour]
                assert len(two)==2
                assert {x["regime"] for x in two}=={"operational","physical"}
                for x in two:
                    assert x["noSkippedSlices"]=="True" and x["noDecisionReuse"]=="True"
                    assert abs(sum(float(x[f"{k}Ms"]) for k in REGIONS)-float(x["totalMs"]))<.008
                    if arm.startswith("base"):
                        assert x["enabled"]=="False" and int(x["calls"])==0
                    else:
                        assert x["enabled"]=="True" and int(x["calls"])>0
                if arm.startswith("probe"):
                    assert sum(float(x["totalMs"]) for x in two)<float(p["vehicleMs"])+1
                    assert sum(float(x["actorLoopMs"]) for x in two)+1>=float(p["actorMs"])
            if arm.startswith("probe"):
                assert summary["calls"]=={
                    g:sum(int(x["calls"]) for x in segments if x["regime"]==g)
                    for g in ("operational","physical")}
                assert sum(summary["calls"].values())==75619
                regions=summary["regionMs"]
                vehicle=sum(x["vehicleMs"] for x in summary["hours"].values())
                actor=sum(x["actorMs"] for x in summary["hours"].values())
                outside_loop=sum(v for k,v in regions.items() if k!="actorLoop")
                loop_non_actor=regions["actorLoop"]-actor
                outside_unsplit=vehicle-sum(regions.values())
                residual=summary["vehicleNonActorMs"]
                assert min(outside_loop,loop_non_actor,outside_unsplit)>0
                assert abs(outside_loop+loop_non_actor+outside_unsplit-residual)<.025
                result[arm]={"wallMs":summary["wallMs"],"vehicleMs":round(vehicle,3),
                             "actorMs":round(actor,3),"nonActorMs":residual,
                             "outsideActorLoopInsideUnsplitMs":round(outside_loop,3),
                             "insideActorLoopNotActorTimerMs":round(loop_non_actor,3),
                             "vehiclePhaseOutsideUnsplitMs":round(outside_unsplit,3),
                             "nonActorSharesPct":[round(100*x/residual,2)
                               for x in (outside_loop,loop_non_actor,outside_unsplit)],
                             "calls":summary["calls"],"regionsMs":regions}
            else:
                assert all(x==0 for x in summary["calls"].values())
                result[arm]={"wallMs":summary["wallMs"],
                             "nonActorMs":summary["vehicleNonActorMs"]}
        assert data["arms"]["probeA"]["calls"]==data["arms"]["probeB"]["calls"]
        print(json.dumps({"zip_sha256":digest,"crc":"OK","members":len(names),
                          "save_sha256":SAVE_SHA,"save_bytes":len(saves[0]),
                          "arms":result,"accepted_speedup":False},indent=2))

if __name__=="__main__":
    if len(sys.argv) not in (2,3):
        raise SystemExit("usage: pr57-385-audit.py ZIP [sha256]")
    audit(Path(sys.argv[1]),sys.argv[2] if len(sys.argv)==3 else None)
