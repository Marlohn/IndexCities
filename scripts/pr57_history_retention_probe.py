#!/usr/bin/env python3
"""PR57 offline, lossless history-size probe for authentic Action checkpoint ZIPs.

No simulation, synthetic residents, altered save, or performance claim. The
packed representation is an analytical candidate, never loaded by the engine.
"""
import argparse
import collections
import gzip
import json
import re
import zipfile


def compact_json(value):
    return json.dumps(value, ensure_ascii=False, separators=(",", ":")).encode("utf-8")


def history_size(citizens):
    histories = [c["History"] for c in citizens]
    descriptions = list(dict.fromkeys(event["Description"] for row in histories for event in row))
    ids = {description: i for i, description in enumerate(descriptions)}
    packed = {
        "descriptions": descriptions,
        "histories": [[[event["At"], ids[event["Description"]]] for event in row] for row in histories],
    }
    reconstructed = [[{"At": timestamp, "Description": descriptions[index]} for timestamp, index in row]
                     for row in packed["histories"]]
    assert reconstructed == histories, "lossless history roundtrip failed"
    original = compact_json(histories)
    candidate = compact_json(packed)
    return len(original), len(candidate), len(gzip.compress(original, compresslevel=6)), len(gzip.compress(candidate, compresslevel=6)), len(descriptions)


def examine(archive):
    with zipfile.ZipFile(archive) as z:
        paths = sorted((path for path in z.namelist() if re.search(r"/checkpoint-\d+\.json$", path)),
                       key=lambda path: (path.split("/")[0], int(re.search(r"checkpoint-(\d+)", path).group(1))))
        if not paths:
            raise ValueError("no checkpoint-N.json found")
        previous = {}
        for path in paths:
            raw = z.read(path)
            state = json.loads(raw)
            citizens = state["Citizens"]
            events = [event for citizen in citizens for event in citizen["History"]]
            # Check each individual history is chronological, and earlier
            # checkpoints are a prefix for surviving IDs. Never assume that
            # the population is constant or that the replay is sustainable.
            for citizen in citizens:
                dates = [event["At"] for event in citizen["History"]]
                assert dates == sorted(dates), (path, citizen["Id"], "chronology")
            group = path.split("/")[0]
            old = previous.get(group, {})
            for citizen in citizens:
                if citizen["Id"] in old:
                    prior = old[citizen["Id"]]
                    assert citizen["History"][:len(prior)] == prior, (path, citizen["Id"], "append-only")
            previous[group] = {c["Id"]: c["History"] for c in citizens}
            raw_hist, packed_hist, zipped_hist, zipped_packed, unique = history_size(citizens)
            print(json.dumps({
                "archive": archive, "checkpoint": path, "elapsed_s": state["ElapsedSecondsUnits"] / 1_000_000,
                "living": sum(c["Alive"] for c in citizens), "citizens": len(citizens),
                "personal_events": len(events), "unique_descriptions": unique,
                "ledger": len(state["Ledger"]), "city_events": len(state["CityEvents"]),
                "save_bytes": len(raw), "history_bytes": raw_hist,
                "packed_history_bytes": packed_hist, "gzip_history_bytes": zipped_hist,
                "gzip_packed_history_bytes": zipped_packed, "roundtrip": "exact",
                "chronology": "monotonic", "previous_checkpoint": "prefix"
            }, ensure_ascii=False, separators=(",", ":")))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archives", nargs="+", help="Action artifact ZIP(s), not modified")
    args = parser.parse_args()
    for archive in args.archives:
        examine(archive)
