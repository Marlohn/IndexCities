#!/usr/bin/env python3
"""Independent audit of original PR57 #372 Actions ZIP (not a benchmark runner).

Usage: python3 scripts/pr57-372-audit.py path/to/pr57-372-raw.zip
Checks CRC, original artifact digest, ABBA raw logs, event accounting,
intermediate hourly counters and byte-for-byte final saves.
"""
from pathlib import Path
import hashlib
import json
import sys
import zipfile

ARTIFACT_SHA="9a8fb9ed2f86762cb07ddbc543859490876aae2fcfe2f31d08f1c15071d0e1be"
SAVE_SHA="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
ARMS=("baseA","publishA","publishB","baseB")

def records(log,prefix):
    return [dict(x.split("=",1) for x in line.split()[1:] if "=" in x)
            for line in log.splitlines() if line.startswith(prefix+" ")]

def one(log,prefix):
    rows=records(log,prefix)
    assert len(rows)==1,(prefix,len(rows))
    return rows[0]

def audit(path):
    raw=Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest()==ARTIFACT_SHA,"original ZIP SHA mismatch"
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None,"ZIP CRC mismatch"
        expected={"summary.txt","checkpoints.sha256"}
        expected|={f"{arm}/{name}" for arm in ARMS
                   for name in ("full.log","census.log","checkpoint-10800.json")}
        assert set(z.namelist())==expected,(len(z.namelist()),set(z.namelist())^expected)
        saves=[z.read(f"{arm}/checkpoint-10800.json") for arm in ARMS]
        assert all(save==saves[0] for save in saves)
        assert len(saves[0])==2254838
        assert hashlib.sha256(saves[0]).hexdigest()==SAVE_SHA
        assert z.read("checkpoints.sha256").decode().lower().count(SAVE_SHA)==4
        logs={arm:z.read(f"{arm}/census.log").decode() for arm in ARMS}
        first=None
        report=[]
        for arm in ARMS:
            log=logs[arm]
            hours=records(log,"PR57_337_HOUR")
            assert len(hours)==6
            assert [int(h["hour"]) for h in hours]==list(range(1,7))
            assert all(int(h["living"])==121 for h in hours)
            assert one(log,"PR57_337_FINAL")["parity"]=="IDENTICAL"
            scalar=one(log,"PR57_367_SUMMARY")
            lists=one(log,"PR57_369_SUMMARY")
            refs=one(log,"PR57_370_SUMMARY")
            summary=one(log,"PR57_372_SUMMARY")
            assert int(scalar["writes"])==1852
            assert int(scalar["noncanonical"])==0
            assert int(lists["countChanges"])==17
            assert int(refs["refContentDifferent"])==51
            assert int(summary["events"])==1920
            assert int(summary["events"])==(
                int(scalar["writes"])+int(lists["countChanges"])+
                int(refs["refContentDifferent"]))
            assert int(summary["additionalSites"])==6
            assert int(summary["touched"])==10
            assert int(summary["maxEventsPerId"])==421
            assert not any("additional:" in x["key"]
                           for x in records(log,"PR57_372_FIELD"))
            publish=arm.startswith("publish")
            assert summary["publish"].lower()==str(publish).lower()
            assert int(summary["published"])==(1920 if publish else 0)
            assert int(summary["epochIds"])==(10 if publish else 0)
            assert int(summary["epochTotal"])==(1920 if publish else 0)
            assert sum(int(x["events"]) for x in records(log,"PR57_372_FIELD"))==1920
            assert sum(int(x["events"]) for x in records(log,"PR57_372_PHASE"))==1920
            snapshot={prefix:sorted(tuple(sorted(x.items())) for x in records(log,prefix))
                      for prefix in ("PR57_367_FIELD","PR57_367_PHASE",
                                     "PR57_369_FIELD","PR57_369_PHASE",
                                     "PR57_370_FIELD","PR57_370_PHASE",
                                     "PR57_372_FIELD","PR57_372_PHASE")}
            if first is None:first=snapshot
            else:assert snapshot==first,"writer counts differ between arms"
            report.append(dict(arm=arm,events=int(summary["events"]),
                               published=int(summary["published"]),
                               epoch_ids=int(summary["epochIds"]),
                               publish_ms=round(int(summary["publishTicks"])*1000/
                                                int(summary["tickFrequency"]),4),
                               wall_ms=round(sum(float(h["wallMs"]) for h in hours),3),
                               cpu_ms=round(sum(float(h["cpuMs"]) for h in hours),3),
                               allocated_bytes=sum(int(h["totalAllocatedBytes"])
                                                   for h in hours)))
        result=dict(artifact_sha256=ARTIFACT_SHA,crc="ok",members=len(z.namelist()),
                    save_sha256=SAVE_SHA,save_bytes=len(saves[0]),
                    save_parity="four byte-identical",arms=report)
        print(json.dumps(result,indent=2,sort_keys=True))

if __name__=="__main__":
    assert len(sys.argv)==2,"usage: pr57-372-audit.py original.zip"
    audit(sys.argv[1])
