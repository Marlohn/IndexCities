#!/usr/bin/env python3
"""PR57 #287: exact serialized history/ledger footprint in authentic #259 saves.

Usage: python3 scripts/pr57-287-history-footprint.py pr57-259.zip
Optional: --source src/IndexCities.Simulation (checks known append/save paths).
No time-series extrapolation, no performance A/B, no changes to simulation.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path
from zipfile import ZipFile

CASES = {"worksREAL": 150, "queueREAL": 150, "real14hREAL": 100, "gridARTIFICIAL": 50}
EXPECTED_ZIP_SHA256 = "23472d3616d30a93addc97a4adb02d0e807c62ac1943fb814ded1528c550278b"
DECODER = json.JSONDecoder()


def exact_array_bytes(raw: bytes, key: str):
    # System.Text.Json's save is ASCII escaped; measure ORIGINAL bytes, not
    # Python's reserialization (which uses different escaping rules).
    assert raw.isascii(), "Raw byte slicing requires ASCII JSON"
    s = raw.decode("ascii")
    lengths = []
    for match in re.finditer('"' + re.escape(key) + '":', s):
        value, end = DECODER.raw_decode(s, match.end())
        assert isinstance(value, list), (key, type(value))
        lengths.append((end - match.end(), len(value)))
    return lengths


def source_guard(directory: Path):
    city = (directory / "CitySimulation.cs").read_text(encoding="utf-8")
    model = (directory / "Model.cs").read_text(encoding="utf-8")
    persistence = (directory / "Persistence.cs").read_text(encoding="utf-8")
    assert 'c.History.Add(new(State.Now,description))' in city
    assert 'State.CityEvents.Add(new(State.Now,description))' in city
    assert 'State.Ledger.Add(new(State.Now,from,to,amount,category,reason,entity))' in city
    assert 'public List<PersonalEvent> History {get;set;}=[];' in model
    assert 'public List<PersonalEvent> CityEvents {get;set;}=[];' in model
    assert 'JsonSerializer.Serialize(State,SaveOptions)' in persistence
    print("PR57_287_SOURCE_GUARD knownWriters=append-only serializer=full-State "
          "scope=these_paths_only no_global_retention_proof=true")


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("artifact", type=Path)
    ap.add_argument("--source", type=Path)
    args = ap.parse_args()
    zip_hash = hashlib.sha256(args.artifact.read_bytes()).hexdigest()
    assert zip_hash == EXPECTED_ZIP_SHA256, (zip_hash, EXPECTED_ZIP_SHA256)
    if args.source:
        source_guard(args.source)
    with ZipFile(args.artifact) as z:
        for case, steps in CASES.items():
            raw = [z.read(f"{case}-control/checkpoint-{i}.json") for i in (0, 1)]
            for i in (0, 1):
                assert raw[i] == z.read(f"{case}-probe/checkpoint-{i}.json"), (case, i)
            old, new = (json.loads(b) for b in raw)
            assert len(old["Citizens"]) == len(new["Citizens"])
            assert [c["Id"] for c in old["Citizens"]] == [c["Id"] for c in new["Citizens"]]
            assert all(n["History"][:len(o["History"])] == o["History"]
                       for o, n in zip(old["Citizens"], new["Citizens"]))
            for key in ("Ledger", "CityEvents"):
                assert new[key][:len(old[key])] == old[key]
            footprints = []
            for i in (0, 1):
                h = exact_array_bytes(raw[i], "History")
                ledger = exact_array_bytes(raw[i], "Ledger")
                events = exact_array_bytes(raw[i], "CityEvents")
                assert len(h) == len(old["Citizens"]) and len(ledger) == len(events) == 1
                footprints.append((sum(n for n, _ in h), ledger[0][0], events[0][0]))
            history_added = sum(len(n["History"]) - len(o["History"])
                                for o, n in zip(old["Citizens"], new["Citizens"]))
            ledger_added = len(new["Ledger"]) - len(old["Ledger"])
            event_added = len(new["CityEvents"]) - len(old["CityEvents"])
            base, final = footprints
            print("PR57_287_CASE", case, f"steps={steps}",
                  f"alive={sum(c['Alive'] for c in old['Citizens'])}",
                  f"saveBytes={len(raw[0])},{len(raw[1])}",
                  f"historyBytes={base[0]},{final[0]}",
                  f"ledgerBytes={base[1]},{final[1]}",
                  f"cityEventBytes={base[2]},{final[2]}",
                  f"historicalSharePct={100*sum(base)/len(raw[0]):.2f},{100*sum(final)/len(raw[1]):.2f}",
                  f"historyRecordsAdded={history_added}",
                  f"ledgerRecordsAdded={ledger_added}",
                  f"cityEventsAdded={event_added}",
                  "appendOnlyEndpoints=true controlProbeByteParity=true",
                  "sha256=" + ",".join(hashlib.sha256(b).hexdigest() for b in raw))
    print("PR57_287_LIMIT shortWindowsOnly=true historyGrowthRateNotLongHorizonForecast=true "
          "noSaveCpuMeasurement=true noCausalSkip=true noOptimization=true")


if __name__ == "__main__":
    main()
