#!/usr/bin/env python3
"""PR57 #465: exact-JSON history/ledger audit across a REAL midnight.

Input is a 20h authentic checkpoint from #439 and six fresh .NET replay
checkpoints (A/B, 23:59:58, 00:00:00, 00:00:02). No synthetic entities.
This script audits files; it does not run the simulation or extrapolate.
"""
import collections
import datetime as dt
import hashlib
import json
from pathlib import Path
import sys

def sha(raw):
    return hashlib.sha256(raw).hexdigest()

def arrays(raw, key):
    source = raw.decode("utf-8")
    needle = '"' + key + '":'
    decoder = json.JSONDecoder()
    cursor = 0
    out = []
    while (pos := source.find(needle, cursor)) >= 0:
        start = pos + len(needle)
        val, end = decoder.raw_decode(source, start)
        assert isinstance(val, list), key
        out.append((val, len(source[start:end].encode("utf-8"))))
        cursor = end
    return out

def difference(before, after, key, expected):
    old = arrays(before, key)
    new = arrays(after, key)
    assert len(old) == len(new) == expected, (key, len(old), len(new))
    delta = []
    bytes_growth = 0
    changed = 0
    for (o, old_bytes), (n, new_bytes) in zip(old, new):
        assert n[:len(o)] == o, (key, "not append-only")
        if len(n) != len(o):
            changed += 1
        delta.extend(n[len(o):])
        bytes_growth += new_bytes - old_bytes
    return {
        "entriesBefore": sum(len(x[0]) for x in old),
        "entriesAfter": sum(len(x[0]) for x in new),
        "newEntries": len(delta),
        "changedOwners": changed,
        "rawJsonBytesDelta": bytes_growth,
        "newDescriptions": collections.Counter(
            str(x.get("Description", "")) for x in delta if isinstance(x, dict)
        ).most_common(12),
        "newKinds": collections.Counter(
            str(x.get("Kind", "")) for x in delta if isinstance(x, dict)
        ).most_common(12),
        "salaryEvents": sum(
            str(x.get("Description", "")).startswith("Salário pago ")
            for x in delta if isinstance(x, dict)
        ),
    }

def main(root):
    root = Path(root)
    files = {"start": root / "20h.json"}
    for arm in ("A", "B"):
        for stage in ("pre", "mid", "post"):
            files[f"{arm}-{stage}"] = root / f"{arm}-{stage}" / "checkpoint-1.json"
    raw = {key: path.read_bytes() for key, path in files.items()}
    assert sha(raw["start"]) == "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
    for stage in ("pre", "mid", "post"):
        assert raw[f"A-{stage}"] == raw[f"B-{stage}"], ("A/B full save mismatch", stage)
    snapshots = {k: json.loads(v) for k, v in raw.items()}
    expected = {
        "start": "2026-01-07T20:00:00",
        "pre": "2026-01-07T23:59:58",
        "mid": "2026-01-08T00:00:00",
        "post": "2026-01-08T00:00:02",
    }
    for key, state in snapshots.items():
        stage = key.split("-")[-1] if "-" in key else "start"
        assert state["Now"] == expected[stage], (key, state["Now"])
        assert sum(bool(c["Alive"]) for c in state["Citizens"]) == 121
        assert len(state["Vehicles"]) == 98
        expected_next = "2026-01-08T00:00:00" if stage in ("start", "pre") else "2026-01-09T00:00:00"
        assert state["NextDaily"] == expected_next, (key, state["NextDaily"])
        assert [c["Id"] for c in state["Citizens"]] == [c["Id"] for c in snapshots["start"]["Citizens"]]
    stages = [("start", "A-pre"), ("A-pre", "A-mid"), ("A-mid", "A-post")]
    output = {
        "experiment": 465,
        "fixture": "AUTHENTIC_REAL_121_LIVING_98_VEHICLES",
        "allAdvance2": True,
        "newDotNetProcesses": 6,
        "fullSaveABParity": True,
        "inputSha256": sha(raw["start"]),
        "stages": {},
        "limitations": "REAL 4h+4s midnight replay, not 80 years or 10k. A/B identical control flags, not an acceleration test.",
    }
    for left, right in stages:
        before, after = raw[left], raw[right]
        b, a = snapshots[left], snapshots[right]
        key = left + "_to_" + right
        output["stages"][key] = {
            "from": b["Now"], "to": a["Now"],
            "seconds": int((dt.datetime.fromisoformat(a["Now"]) - dt.datetime.fromisoformat(b["Now"])).total_seconds()),
            "saveBytesBefore": len(before), "saveBytesAfter": len(after),
            "saveBytesDelta": len(after) - len(before),
            "sha256After": sha(after),
            "wagesAccruedPositiveBefore": sum(float(c.get("WagesAccrued", 0)) > 0 for c in b["Citizens"]),
            "wagesAccruedPositiveAfter": sum(float(c.get("WagesAccrued", 0)) > 0 for c in a["Citizens"]),
            "history": difference(before, after, "History", 121),
            "ledger": difference(before, after, "Ledger", 1),
            "cityEvents": difference(before, after, "CityEvents", 1),
        }
    mid = output["stages"]["A-pre_to_A-mid"]
    assert mid["seconds"] == 2
    assert output["stages"]["start_to_A-pre"]["seconds"] == 14398
    assert output["stages"]["A-mid_to_A-post"]["seconds"] == 2
    (root / "audit-summary.json").write_text(json.dumps(output, ensure_ascii=False, indent=2, sort_keys=True) + "\n")
    print(json.dumps(output, ensure_ascii=False, sort_keys=True))
    print("PR57_465_REAL_MIDNIGHT_AUDIT_PASS")

if __name__ == "__main__":
    assert len(sys.argv) == 2, "usage: pr57-465-midnight-history-audit.py ARTIFACT_ROOT"
    main(sys.argv[1])
