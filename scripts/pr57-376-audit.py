#!/usr/bin/env python3
"""PR57 #376: independently audit original Actions ZIP, no simulation mutation.

Usage: python3 scripts/pr57-376-audit.py /path/to/pr57-376-original.zip
Counts are from a bounded PARTIAL gate; no causal closure or speedup claim.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ZIP_SHA = "1672cf5a62cf72d62cad5cc164fc15f76157ca9f64258f267b590a6c4d91ffce"
SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"

def rows(log, prefix):
    return [dict(piece.split("=", 1) for piece in line.split()[1:] if "=" in piece)
            for line in log.splitlines() if line.startswith(prefix+" ")]

def one(log, prefix):
    found = rows(log, prefix)
    assert len(found)==1, (prefix, len(found))
    return found[0]

def audit(path):
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        expected={"checkpoints.sha256","summary.txt"}
        expected|={f"{arm}/{name}" for arm in ("control","shadow")
                   for name in ("checkpoint-10800.json","census.log","full.log")}
        assert set(z.namelist())==expected
        saves=[z.read(f"{arm}/checkpoint-10800.json") for arm in ("control","shadow")]
        assert saves[0]==saves[1] and len(saves[0])==2254838
        assert hashlib.sha256(saves[0]).hexdigest()==SAVE_SHA
        logs={arm:z.read(f"{arm}/census.log").decode()
              for arm in ("control","shadow")}
        hours={}
        for arm,log in logs.items():
            h=rows(log,"PR57_337_HOUR")
            assert len(h)==6 and [int(x["hour"]) for x in h]==list(range(1,7))
            assert all(int(x["living"])==121 for x in h)
            assert one(log,"PR57_337_FINAL")["parity"]=="IDENTICAL"
            assert int(one(log,"PR57_367_SUMMARY")["writes"])==1852
            assert int(one(log,"PR57_369_SUMMARY")["countChanges"])==17
            assert int(one(log,"PR57_370_SUMMARY")["refContentDifferent"])==51
            assert int(one(log,"PR57_372_SUMMARY")["events"])==1920
            hours[arm]=dict(first_wall_ms=float(h[0]["wallMs"]),
                            total_wall_ms=round(sum(float(x["wallMs"]) for x in h),3),
                            first_alloc_bytes=int(h[0]["totalAllocatedBytes"]),
                            total_alloc_bytes=sum(int(x["totalAllocatedBytes"]) for x in h))
        a=one(logs["control"],"PR57_374_SUMMARY")
        b=one(logs["shadow"],"PR57_374_SUMMARY")
        assert a==b and int(a["events"])==1920
        assert not rows(logs["control"],"PR57_376_SUMMARY")
        s=one(logs["shadow"],"PR57_376_SUMMARY")
        expected_counts=dict(physicalSlices=600,readCalls=1364887,
                             uniqueReadPairs=1327930,writerEvents=1914,
                             uniqueWriters=879,externalEvents=27,
                             writersWithReaders=835,invalidatedReaderPairs=20036,
                             maxReadersPerWriter=29,registrationTicks=74433843,
                             propagationTicks=503294,tickFrequency=1000000000)
        assert {k:int(s[k]) for k in expected_counts}==expected_counts
        assert s["noCausalClosure"]=="True" and s["noDecisionReuse"]=="True"
        assert s["noSkippedSlices"]=="True" and s["observational"]=="True"
        print(json.dumps(dict(zip_sha256=ZIP_SHA,crc="ok",members=len(expected),
            save_sha256=SAVE_SHA,save_parity="byte-identical",
            control=hours["control"],shadow=hours["shadow"],
            first_hour_extra_alloc_bytes=hours["shadow"]["first_alloc_bytes"]-hours["control"]["first_alloc_bytes"],
            first_hour_wall_delta_ms=round(hours["shadow"]["first_wall_ms"]-hours["control"]["first_wall_ms"],3),
            partial_gate_counts=expected_counts,
            direct_registration_ms=round(expected_counts["registrationTicks"]*1000/expected_counts["tickFrequency"],3),
            direct_propagation_ms=round(expected_counts["propagationTicks"]*1000/expected_counts["tickFrequency"],3),
            note="Intrusive partial reverse-read graph, not causal closure or a throughput A/B"),indent=2))
if __name__=="__main__":
    if len(sys.argv)!=2:raise SystemExit(__doc__)
    audit(sys.argv[1])
