#!/usr/bin/env python3
"""Independent offline audit of PR57 #428 original Actions artifact ZIP.

Usage: python3 scripts/pr57-429-audit.py original-428.zip
"""
import hashlib
import json
import struct
import sys
import zipfile

ZIP_SHA = "3498c53370c4cb5c3e900b84da2c7d5166f3428bf408a2ae615d989084320e95"
SAVE_SHA = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
FILES = {"base/checkpoint-10800.json", "base/full.log", "checkpoints.sha256",
         "shadow/checkpoint-10800.json", "shadow/full.log", "summary.json"}

def rows(lines, prefix):
    return [dict(x.split("=", 1) for x in line.split()[1:] if "=" in x)
            for line in lines if line.startswith(prefix + " ")]

def as_double(bits):
    return struct.unpack(">d", struct.pack(">q", bits))[0]

def main(path):
    assert hashlib.sha256(open(path, "rb").read()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == FILES
        summary = json.loads(z.read("summary.json"))
        assert summary["advance2PerArm"] == 10800
        assert summary["observational"] and not summary["causalClosure"]
        assert not summary["optimizationGainAccepted"]
        signatures, saves = [], []
        for arm in ("base", "shadow"):
            lines = z.read(f"{arm}/full.log").decode().splitlines()
            hours, boundaries, final = (rows(lines, x) for x in
                ("PR57_337_HOUR", "PR57_ADVANCE_BOUNDARY", "PR57_337_FINAL"))
            assert len(hours) == 6 and len(boundaries) == 108 and len(final) == 1
            assert [int(b["calls"]) for b in boundaries] == list(range(100, 10801, 100))
            assert all(b["quantum"] == "2" for b in boundaries)
            assert all(int(h["living"]) == 121 for h in hours)
            assert final[0]["parity"] == "IDENTICAL"
            signatures.append([tuple(h[k] for k in
                ("hour", "slices", "actors", "claimChanges", "spaceCountChanges",
                 "living", "ledger", "history")) for h in hours])
            saves.append(z.read(f"{arm}/checkpoint-10800.json"))
            a = summary["arms"][arm]
            assert a["wallMs"] == round(sum(float(h["wallMs"]) for h in hours), 3)
            assert a["cpuMs"] == sum(float(h["cpuMs"]) for h in hours)
            assert a["allocatedBytes"] == sum(int(h["totalAllocatedBytes"]) for h in hours)
            witnesses = [json.loads(line.split(" ", 1)[1]) for line in lines
                         if line.startswith("PR57_428_WITNESS ")]
            assert a["witnesses"] == witnesses
            if arm == "base":
                assert not witnesses and not rows(lines, "PR57_428_SUMMARY")
            else:
                assert len(witnesses) == 2
                assert rows(lines, "PR57_428_SUMMARY")[0]["witnesses"] == "2"
                semantic = rows(lines, "PR57_426_SUMMARY")
                assert len(semantic) == 1 and semantic[0]["readerHolds"] == "14998"
                assert semantic[0]["stableReader"] == "15000"
                assert [(w["vehicle"], w["priorSlice"], w["slice"]) for w in witnesses] == [
                    (1174, 322, 323), (1174, 323, 324)]
                for w in witnesses:
                    assert w["candidateCountBefore"] == w["candidateCountAfter"] == 98
                    assert len(w["changedCandidates"]) == 1
                    assert w["changedCandidates"][0]["before"]["Id"] == 677
                    assert w["changedCandidates"][0]["after"]["Id"] == 677
                    b, n = w["readerBefore"], w["readerAfter"]
                    assert all(b[k] == n[k] for k in
                        ("Node", "Entry", "RouteIndex", "Waiting", "X", "Z", "Heading"))
                    assert b["Blocker"] == n["Blocker"] == 487
                    assert b["Distance"] == n["Distance"] == b["Step"] == n["Step"] == 0
                    assert [h["Id"] for h in w["beforeHolds"] if h["Id"] != 677] == [487, 1107]
                    assert [h["Id"] for h in w["afterHolds"] if h["Id"] != 677] == [487, 1107]
                assert [sum(h["Id"] == 677 for h in w["beforeHolds"])
                        for w in witnesses] == [0, 1]
                assert [sum(h["Id"] == 677 for h in w["afterHolds"])
                        for w in witnesses] == [1, 0]
                assert as_double(witnesses[0]["afterHolds"][1]["Distance"]) > 200
        assert signatures[0] == signatures[1]
        assert saves[0] == saves[1] and len(saves[0]) == 2254838
        assert hashlib.sha256(saves[0]).hexdigest() == SAVE_SHA == summary["saveSha256"]
        print(json.dumps({"audit": "PASS", "zipSha256": ZIP_SHA, "saveSha256": SAVE_SHA,
            "witnesses": [[1174, 322, 323], [1174, 323, 324]],
            "changedCandidate": 677, "unchangedWinner": 487,
            "intermediateParity": True, "finalSavesIdentical": True,
            "gainAccepted": False}, sort_keys=True))

if __name__ == "__main__":
    assert len(sys.argv) == 2
    main(sys.argv[1])
