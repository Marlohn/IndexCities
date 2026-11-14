#!/usr/bin/env python3
"""#498: audit original partial #497 Action ZIP without rerunning its benchmark.

Usage: python3 scripts/pr57-498-audit-exclusive-alloc.py pr57-497-partial.zip
The #497 runner rejected exact B1/B2 byte equality (JIT-sensitive); this audit
requires identical call counts, bounded byte drift, complete JSON save parity,
and exclusive-scope reconciliation. It does NOT turn instrumentation into a
speedup, pose-only allocation estimate, or 10k extrapolation.
"""
import hashlib
import re
import sys
import zipfile

EXPECTED_ZIP="89eb18a37fc30d697562daa556c24d0742000473eeaf1b6d9a7940fde534c7fa"
KINDS=("PenetrationCore","Manoeuvre","TurnWay","BodyOnWay",
       "JunctionPreferenceAllows","EntryPlan","AdmitManoeuvre","SpaceFree",
       "CanTurnNow","ClearOfDockWay","TrafficOnWay","WayIn",
       "PhysicalDockWay","OwnedDockWay","BayEntryHold")
ARMS=("A1","B1","B2","A2")

def read_profile(log):
    totals=[l for l in log.splitlines() if l.startswith("PR57_497_TOTAL ")]
    rows=[l for l in log.splitlines() if l.startswith("PR57_497_KIND ")]
    assert len(totals)==1 and len(rows)==len(KINDS),(len(totals),len(rows))
    def numbers(s):
        return {k:int(v) for k,v in re.findall(r"([A-Za-z][A-Za-z0-9]*)=(\d+)",s)}
    total=numbers(totals[0])
    assert total["calls"]==600 and total["rootCalls"]>0
    data={}
    for line in rows:
        name=re.search(r"\bkind=(\w+)",line).group(1)
        assert name in KINDS and name not in data
        data[name]=numbers(line)
        assert 0<=data[name]["exclusiveBytes"]<=data[name]["inclusiveBytes"]
    assert set(data)==set(KINDS)
    assert sum(v["exclusiveBytes"] for v in data.values())==total["rootBytes"]==total["exclusiveSum"]
    return total,data

def main(path):
    raw=open(path,"rb").read()
    assert hashlib.sha256(raw).hexdigest()==EXPECTED_ZIP,"original artifact digest mismatch"
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        expected={f"congestedREAL121/{arm}/{name}"
                  for arm in ARMS for name in ("checkpoint-0.json","checkpoint-1.json","full.log")}
        assert set(z.namelist())==expected
        for checkpoint,sha in (
            ("checkpoint-0.json","779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f"),
            ("checkpoint-1.json","9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23")):
            saves=[z.read(f"congestedREAL121/{arm}/{checkpoint}") for arm in ARMS]
            assert all(x==saves[0] for x in saves)
            assert hashlib.sha256(saves[0]).hexdigest()==sha
        profiles=[]
        for arm in ARMS:
            log=z.read(f"congestedREAL121/{arm}/full.log").decode("utf-8")
            assert "INVARIANTS OK" in log and "STEP_PROFILE steps=100 " in log
            if arm.startswith("B"):
                profiles.append(read_profile(log))
            else:
                assert "PR57_497_TOTAL " not in log and "PR57_497_KIND " not in log
    (t1,d1),(t2,d2)=profiles
    assert t1["rootCalls"]==t2["rootCalls"]
    assert abs(t1["rootBytes"]-t2["rootBytes"])<=10000
    for name in KINDS:
        assert d1[name]["calls"]==d2[name]["calls"],name
        assert abs(d1[name]["exclusiveBytes"]-d2[name]["exclusiveBytes"])<=10000,name
    print("REAL congested: 121 alive, 98 vehicles, 100 public Advance(2), 600 unsplit invocations")
    print("Full A1/B1/B2/A2 initial and final saves byte-identical; invariants and 100 steps checked")
    print(f"Exclusive measured roots: B1={t1['rootBytes']/1e6:.6f} MB; "
          f"B2={t2['rootBytes']/1e6:.6f} MB; "
          f"drift={t2['rootBytes']-t1['rootBytes']} B (JIT/measurement sensitive)")
    for name in sorted(KINDS,key=lambda k:-d1[k]["exclusiveBytes"]):
        v=d1[name]
        print(f"  {name:25s} calls={v['calls']:7d} "
              f"exclusiveMB={v['exclusiveBytes']/1e6:9.5f} "
              f"inclusiveMB={v['inclusiveBytes']/1e6:9.5f}")
    print("Manoeuvre exclusive is an upper bound for pose-list allocations, "
          "NOT pose-only; PenetrationCore is distinct SAT/LINQ work.")
    print("No integrated acceleration A/B was run; accepted gain=0%.")
    print("PR57_498_INDEPENDENT_EXCLUSIVE_ALLOCATION_AUDIT_PASS")

if __name__=="__main__":
    assert len(sys.argv)==2,__doc__
    main(sys.argv[1])
