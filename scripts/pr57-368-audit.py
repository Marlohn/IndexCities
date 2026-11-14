#!/usr/bin/env python3
"""Independent raw ZIP audit of PR57 #368 (ABBA).

Usage: python3 scripts/pr57-368-audit.py original.zip
The Action's parser expected 1/0 but C# interpolates bool as True/False;
its failure occurred AFTER all four authentic replays and save comparisons.
"""
import hashlib
import sys
import zipfile
from pathlib import Path

p=Path(sys.argv[1]); digest=hashlib.sha256(p.read_bytes()).hexdigest()
assert digest=="9967a2a03b0d054a8ddb030ad855f0dbe6b1b98330000f3146c9784aefcce5d8"
modes=("baselineA","guardedA","guardedB","baselineB")
with zipfile.ZipFile(p) as z:
    assert z.testzip() is None
    expected={"checkpoints.sha256","summary.txt"}|{
        f"{m}/{n}" for m in modes for n in
        ("census.log","full.log","checkpoint-10800.json")}
    assert set(z.namelist())==expected
    saves=[z.read(f"{m}/checkpoint-10800.json") for m in modes]
    assert all(s==saves[0] for s in saves)
    save_sha=hashlib.sha256(saves[0]).hexdigest()
    assert save_sha=="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
    assert len(saves[0])==2254838
    logs={m:z.read(f"{m}/census.log").decode() for m in modes}
    def rows(m,prefix):
        return [dict(t.split("=",1) for t in line.split()[1:] if "=" in t)
                for line in logs[m].splitlines() if line.startswith(prefix+" ")]
    reference=None
    wall={};alloc={}
    for m in modes:
        hours=rows(m,"PR57_337_HOUR")
        assert len(hours)==6 and [int(h["hour"]) for h in hours]==list(range(1,7))
        finals=rows(m,"PR57_337_FINAL")
        assert len(finals)==1 and finals[0]["parity"]=="IDENTICAL"
        guard=rows(m,"PR57_368_GUARD")
        assert len(guard)==1 and guard[0]["enabled"]==str(m.startswith("guarded"))
        summary=rows(m,"PR57_367_SUMMARY")
        assert len(summary)==1
        s=summary[0]
        assert int(s["canonical"])==98 and int(s["writes"])==1852
        assert int(s["noncanonical"])==(0 if m.startswith("guarded") else 1654107)
        fields=rows(m,"PR57_367_FIELD")
        phases=rows(m,"PR57_367_PHASE")
        assert sum(int(f["writes"]) for f in fields)==1852
        assert sum(int(f["writes"]) for f in phases)==1852
        assert len(fields)==int(s["fields"]) and len(phases)==int(s["phaseGroups"])
        projection=tuple(sorted(tuple(sorted(x.items())) for x in group)
                         for group in (fields,phases))
        if reference is None:reference=projection
        else:assert projection==reference
        wall[m]=float(hours[0]["wallMs"])
        alloc[m]=int(hours[0]["totalAllocatedBytes"])
        print("ARM",m,"CANONICAL",s["writes"],"TEMP_CALLBACKS",s["noncanonical"],
              "HOUR1_WALL_MS",wall[m],"HOUR1_ALLOC_BYTES",alloc[m],
              "SIX_HOUR_WALL_MS",round(sum(float(h["wallMs"]) for h in hours),3))
    avg=lambda x:(x[0]+x[1])/2
    baseline=avg([wall["baselineA"],wall["baselineB"]])
    guarded=avg([wall["guardedA"],wall["guardedB"]])
    saved=avg([alloc["baselineA"],alloc["baselineB"]])-avg(
        [alloc["guardedA"],alloc["guardedB"]])
    print("ZIP_SHA256",digest,"SAVE_SHA256",save_sha,"SAVE_BYTES",len(saves[0]))
    print("HOUR1_ALLOC_SAVED_MEAN_BYTES",int(saved))
    print("HOUR1_WALL_BASELINE_MEAN_MS",round(baseline,3),
          "GUARDED_MEAN_MS",round(guarded,3),
          "OBSERVATIONAL_RATIO",round(baseline/guarded,5))
    print("RESULT crc=OK fieldsAndPhases=IDENTICAL saves=IDENTICAL "
          "allFourReplays=COMPLETE workflowFailure=BOOL_PARSER "
          "noIntegratedSpeedupAccepted=True")
