#!/usr/bin/env python3
"""Independently audit the original PR57 #370 artifact ZIP.

Usage: python3 scripts/pr57-370-audit.py path/to/original.zip
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

EXPECTED_SAVE="e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
EXPECTED_FILES={
    "checkpoints.sha256","summary.txt",
    "control/census.log","control/full.log","control/checkpoint-10800.json",
    "probe/census.log","probe/full.log","probe/checkpoint-10800.json",
}

def records(data,prefix):
    return [dict(part.split("=",1) for part in line.split()[1:] if "=" in part)
            for line in data.splitlines() if line.startswith(prefix+" ")]

def one(data,prefix):
    result=records(data,prefix)
    assert len(result)==1,(prefix,len(result))
    return result[0]

def total(rows,key):
    return sum(int(row[key]) for row in rows)

def main(path):
    zip_hash=hashlib.sha256(Path(path).read_bytes()).hexdigest()
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None,"ZIP CRC failure"
        assert set(z.namelist())==EXPECTED_FILES,z.namelist()
        log={arm:z.read(f"{arm}/census.log").decode("utf-8")
             for arm in ("control","probe")}
        saves={arm:z.read(f"{arm}/checkpoint-10800.json")
               for arm in ("control","probe")}
        assert saves["control"]==saves["probe"]
        assert len(saves["control"])==2254838
        assert hashlib.sha256(saves["control"]).hexdigest()==EXPECTED_SAVE
        assert z.read("checkpoints.sha256").decode().lower().count(EXPECTED_SAVE)==2
        for arm in ("control","probe"):
            hours=records(log[arm],"PR57_337_HOUR")
            assert len(hours)==6
            assert [int(x["hour"]) for x in hours]==list(range(1,7))
            assert all(int(x["living"])==121 for x in hours)
            assert one(log[arm],"PR57_337_FINAL")["parity"]=="IDENTICAL"
            assert int(one(log[arm],"PR57_367_SUMMARY")["writes"])==1852
            assert int(one(log[arm],"PR57_367_SUMMARY")["noncanonical"])==0
            assert one(log[arm],"PR57_368_GUARD")["enabled"].lower() in ("true","1")
            assert int(one(log[arm],"PR57_369_SUMMARY")["referenceReplacements"])==55
        for key in ("slices","actors","living","ledger","history",
                    "claimChanges","spaceCountChanges"):
            assert [x[key] for x in records(log["control"],"PR57_337_HOUR")]==[
                x[key] for x in records(log["probe"],"PR57_337_HOUR")]
        for prefix in ("PR57_367_FIELD","PR57_367_PHASE"):
            assert sorted(records(log["control"],prefix),key=str)==sorted(
                records(log["probe"],prefix),key=str)
        a=one(log["control"],"PR57_369_SUMMARY")
        b=one(log["probe"],"PR57_369_SUMMARY")
        assert (int(a["inPlaceCalls"]),int(a["countChanges"]))==(100947,13)
        assert (int(b["inPlaceCalls"]),int(b["countChanges"]))==(100952,17)
        assert not records(log["control"],"PR57_370_SUMMARY")
        s=one(log["probe"],"PR57_370_SUMMARY")
        assert {k:int(s[k]) for k in (
            "refSwaps","refContentEqual","refContentDifferent",
            "nonlocalAttempts","nonlocalLengthChanges","nonlocalSites")}=={
            "refSwaps":55,"refContentEqual":4,"refContentDifferent":51,
            "nonlocalAttempts":5,"nonlocalLengthChanges":4,"nonlocalSites":11}
        fields=records(log["probe"],"PR57_370_FIELD")
        assert total(fields,"swaps")==55 and total(fields,"different")==51
        assert total(records(log["probe"],"PR57_370_PHASE"),"swaps")==55
        extra=[r for r in records(log["probe"],"PR57_369_FIELD")
               if r["key"]=="list:nonlocal:Route.Clear"]
        assert len(extra)==1
        assert (int(extra[0]["attempts"]),int(extra[0]["changed"]))==(5,4)
        assert total(records(log["probe"],"PR57_369_PHASE"),"attempts")==(
            int(b["inPlaceCalls"])+int(b["referenceReplacements"]))
        report={
            "zip_sha256":zip_hash,"zip_crc":"ok","members":len(z.namelist()),
            "save_sha256":EXPECTED_SAVE,"save_bytes":len(saves["control"]),
            "hours_per_arm":6,"canonical_scalar_writes_per_arm":1852,
            "reference_swaps":55,"reference_content_equal":4,
            "reference_content_different":51,
            "nonlocal_route_clear_calls":5,"nonlocal_route_length_changes":4,
            "reference_by_field":{x["field"]:{
                "swaps":int(x["swaps"]),"different":int(x["different"])}
                for x in fields},
            "hour1_wall_ms":{arm:float(records(log[arm],"PR57_337_HOUR")[0]["wallMs"])
                             for arm in log},
            "hour1_allocated_bytes":{arm:int(
                records(log[arm],"PR57_337_HOUR")[0]["totalAllocatedBytes"])
                for arm in log},
        }
        print(json.dumps(report,indent=2,sort_keys=True))

if __name__=="__main__":
    assert len(sys.argv)==2,"usage: pr57-370-audit.py raw.zip"
    main(sys.argv[1])
