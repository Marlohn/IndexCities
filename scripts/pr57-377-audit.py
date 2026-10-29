#!/usr/bin/env python3
"""PR57 #377: audit original Actions ZIP, verify partial bitset/HashSet parity.

Usage: python3 scripts/pr57-377-audit.py <original-377.zip>
This does not establish causal closure or a throughput gain.
"""
import hashlib
import json
import statistics
import sys
import zipfile
from pathlib import Path

ZIP_SHA = "de76a362bd90ca0b327e7918e155eac1c0da5f0b1194ee081d4630e63a0fc4b5"
SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
ARMS = ("hash-a", "bit-a", "bit-b", "hash-b", "both")
KEYS = ("physicalSlices", "readCalls", "uniqueReadPairs", "writerEvents",
        "uniqueWriters", "externalEvents", "writersWithReaders",
        "invalidatedReaderPairs", "maxReadersPerWriter")
EXPECTED = (600, 1364887, 1327930, 1914, 879, 27, 835, 20036, 29)

def rows(log, prefix):
    return [dict(x.split("=", 1) for x in line.split()[1:] if "=" in x)
            for line in log.splitlines() if line.startswith(prefix + " ")]

def one(log, prefix):
    found = rows(log, prefix)
    assert len(found) == 1, (prefix, len(found))
    return found[0]

def audit(path):
    p = Path(path)
    assert hashlib.sha256(p.read_bytes()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(p) as z:
        assert z.testzip() is None
        expected_members = {"checkpoints.sha256", "summary.txt"} | {
            f"{a}/{f}" for a in ARMS
            for f in ("checkpoint-10800.json", "census.log", "full.log")}
        assert set(z.namelist()) == expected_members
        saves = [z.read(f"{a}/checkpoint-10800.json") for a in ARMS]
        assert all(x == saves[0] for x in saves)
        assert len(saves[0]) == 2254838
        assert hashlib.sha256(saves[0]).hexdigest() == SAVE_SHA
        summary = json.loads(z.read("summary.txt"))
        assert summary["saveSha256"] == SAVE_SHA
        assert tuple(summary["expectedCounters"][k] for k in KEYS) == EXPECTED
        data = {}
        for arm in ARMS:
            log = z.read(f"{arm}/census.log").decode()
            assert one(log, "PR57_337_FINAL")["parity"] == "IDENTICAL"
            assert int(one(log, "PR57_367_SUMMARY")["writes"]) == 1852
            assert int(one(log, "PR57_369_SUMMARY")["countChanges"]) == 17
            assert int(one(log, "PR57_370_SUMMARY")["refContentDifferent"]) == 51
            assert int(one(log, "PR57_372_SUMMARY")["events"]) == 1920
            assert int(one(log, "PR57_374_SUMMARY")["events"]) == 1920
            hours = rows(log, "PR57_337_HOUR")
            assert len(hours) == 6
            assert [int(h["hour"]) for h in hours] == list(range(1, 7))
            assert all(int(h["living"]) == 121 for h in hours)
            hashrows = rows(log, "PR57_376_SUMMARY")
            bitrows = rows(log, "PR57_377_SUMMARY")
            assert len(hashrows) == (1 if arm.startswith("hash") or arm == "both" else 0)
            assert len(bitrows) == (1 if arm.startswith("bit") or arm == "both" else 0)
            for row in hashrows + bitrows:
                assert tuple(int(row[k]) for k in KEYS) == EXPECTED
                assert all(row[k] == "True" for k in
                           ("noCausalClosure", "noDecisionReuse", "noSkippedSlices", "observational"))
            chosen = bitrows[0] if arm.startswith("bit") or arm == "both" else hashrows[0]
            actual = dict(firstHourWallMs=float(hours[0]["wallMs"]),
                          firstHourCpuMs=float(hours[0]["cpuMs"]),
                          firstHourAllocBytes=int(hours[0]["totalAllocatedBytes"]),
                          sixHourWallMs=round(sum(float(h["wallMs"]) for h in hours), 3),
                          sixHourCpuMs=round(sum(float(h["cpuMs"]) for h in hours), 3),
                          sixHourAllocBytes=sum(int(h["totalAllocatedBytes"]) for h in hours),
                          registrationMs=round(int(chosen["registrationTicks"])*1000 /
                                               int(chosen["tickFrequency"]), 3),
                          propagationMs=round(int(chosen["propagationTicks"])*1000 /
                                              int(chosen["tickFrequency"]), 3))
            assert actual == summary["arms"][arm], (arm, actual, summary["arms"][arm])
            data[arm] = actual
        def mean(field, pair):
            return statistics.mean(data[a][field] for a in pair)
        h = ("hash-a", "hash-b")
        b = ("bit-a", "bit-b")
        metrics = {}
        for field in ("registrationMs", "propagationMs", "firstHourAllocBytes",
                      "sixHourWallMs", "sixHourCpuMs", "sixHourAllocBytes"):
            hv, bv = mean(field, h), mean(field, b)
            metrics[field] = dict(hashMean=hv, bitMean=bv,
                                  bitMinusHash=bv-hv, bitOverHash=bv/hv)
        print(json.dumps(dict(zipSha256=ZIP_SHA, crc="ok", members=len(expected_members),
                              saveSha256=SAVE_SHA, fiveSaves="byte-identical",
                              counters=dict(zip(KEYS, EXPECTED)), arms=data,
                              comparison=metrics,
                              warning="partial gate, intrusive, no causal closure, no integrated gain"), indent=2))

if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    audit(sys.argv[1])
