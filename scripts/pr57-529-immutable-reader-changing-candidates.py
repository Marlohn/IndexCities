#!/usr/bin/env python3
"""PR57 #529: endpoint-only counterexample for preference candidate invalidation.

Usage: python3 scripts/pr57-529-immutable-reader-changing-candidates.py pr57-522-original.zip
No motor execution; no causal inference about intermediate physical sub-slices.
"""
import collections
import hashlib
import json
import math
import re
import sys
import zipfile

EXPECTED_ZIP_SHA256 = "2561e9a9fe2379ee1f60b28ae45eaac67ceee81f998e69b7d10059e649d8fbd9"
SCENARIO = "congestedREAL121"
EXPECTED_READERS = {1071: (466, 1380), 1243: (346, 1282)}


def node(vehicle, roads):
    route, index = vehicle["Route"], vehicle["RouteIndex"]
    if vehicle["Status"] != "Travelling" or not 0 <= index < len(route):
        return None
    return roads.get(route[index])


def distance(a, b):
    return math.hypot(a["X"] - b["X"], a["Z"] - b["Z"])


def candidates(reader, vehicles, roads):
    """Exactly the guard *before* CanTurnNow in JunctionPreferenceAllows.

    We only evaluate readers whose current route node is an intersection;
    callers from other contexts and transient states are not represented.
    """
    junction = node(reader, roads)
    if junction is None or len(junction["Neighbours"]) < 3:
        return None
    return tuple(v["Id"] for v in vehicles
                 if v["Id"] != reader["Id"]
                 and v["Status"] == "Travelling"
                 and 0 <= v["RouteIndex"] < len(v["Route"])
                 and v["Route"][v["RouteIndex"]] == junction["Id"]
                 and distance(v["Position"], junction["Position"]) < 14)


def snapshot(zip_file, arm, checkpoint):
    raw = zip_file.read(f"{SCENARIO}/{arm}/checkpoint-{checkpoint}.json")
    return raw, json.loads(raw)


def main(path):
    assert hashlib.sha256(open(path, "rb").read()).hexdigest() == EXPECTED_ZIP_SHA256, "unexpected artifact"
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, "ZIP CRC failure"
        assert len(z.namelist()) == 37, "unexpected ZIP member count"
        for arm in ("A1", "B1", "B2", "A2"):
            log = z.read(f"{SCENARIO}/{arm}/full.log").decode("utf-8")
            assert "INVARIANTS OK" in log, f"missing invariant confirmation: {arm}"
            assert "VEHICLE_PROFILE steps=100" in log, f"wrong Step count: {arm}"
            assert re.search(r"SAVE .*sha256=[0-9A-F]{64}", log), f"missing final save hash: {arm}"
        snapshots = {}
        for cp in (0, 1):
            raw, state = snapshot(z, "A1", cp)
            for arm in ("A2", "B1", "B2"):
                assert snapshot(z, arm, cp)[0] == raw, f"checkpoint mismatch: {arm}/{cp}"
            snapshots[cp] = state
        before, after = snapshots[0], snapshots[1]
        assert len(before["Vehicles"]) == len(after["Vehicles"]) == 98
        assert before["ElapsedSecondsUnits"] == 547200000000
        assert after["ElapsedSecondsUnits"] == 547400000000
        assert before["Roads"] == after["Roads"]
        roads = {r["Id"]: r for r in before["Roads"]}
        vehicles0 = {v["Id"]: v for v in before["Vehicles"]}
        vehicles1 = {v["Id"]: v for v in after["Vehicles"]}
        assert vehicles0.keys() == vehicles1.keys()

        eligible = {}
        for cp, state in snapshots.items():
            vehicles = state["Vehicles"]
            eligible[cp] = {v["Id"]: candidates(v, vehicles, roads)
                            for v in vehicles if candidates(v, vehicles, roads) is not None}
        print("snapshot_candidate_guard", {cp: {"readers": len(d), "ordered_pairs": sum(len(v) for v in d.values()),
             "controls": dict(collections.Counter(roads[node(vehicles0[i] if cp == 0 else vehicles1[i], roads)["Id"]]["Control"]
                                                  for i, ids in d.items() for _ in ids))}
             for cp, d in eligible.items()})

        # The two existing stopped readers keep *every* persisted vehicle field,
        # not just pose/route/heading; newly arriving vehicles change their guard.
        for reader_id, (junction_id, entrant_id) in EXPECTED_READERS.items():
            reader0, reader1 = vehicles0[reader_id], vehicles1[reader_id]
            assert reader0 == reader1, f"reader {reader_id} mutated"
            assert node(reader0, roads)["Id"] == junction_id
            assert roads[junction_id]["Control"] == "ThroughZ"
            assert eligible[0][reader_id] == ()
            assert eligible[1][reader_id] == (entrant_id,)
            entrant0, entrant1 = vehicles0[entrant_id], vehicles1[entrant_id]
            assert entrant0["Status"] == "Parked" and entrant1["Status"] == "Travelling"
            assert node(entrant1, roads)["Id"] == junction_id
            assert distance(entrant1["Position"], roads[junction_id]["Position"]) < 14
            assert entrant0["Position"] != entrant1["Position"]
            print("immutable_reader_guard_change", json.dumps({
                "reader": reader_id, "junction": junction_id, "control": "ThroughZ",
                "old_candidates": list(eligible[0][reader_id]), "new_candidates": list(eligible[1][reader_id]),
                "entrant": entrant_id, "entrant_status": [entrant0["Status"], entrant1["Status"]],
                "entrant_distance_final_m": round(distance(entrant1["Position"], roads[junction_id]["Position"]), 6),
                "reader_all_persisted_fields_equal": True,
            }, sort_keys=True))

        # This is deliberately *not* a CanTurnNow result. It is a guard-level
        # invalidation witness and does not reconstruct any intermediate slice.
        print("PR57_529_AUTHENTIC_IMMUTABLE_READER_GUARD_CHANGE_PASS")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: pr57-529-immutable-reader-changing-candidates.py artifact.zip")
    main(sys.argv[1])
