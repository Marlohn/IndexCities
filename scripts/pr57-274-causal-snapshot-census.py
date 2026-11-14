#!/usr/bin/env python3
"""PR57 #274: read-only exact JSON census on #273 authentic CI artifact.

Usage: python scripts/pr57-274-causal-snapshot-census.py pr57-273.zip
Source artifact: https://github.com/Marlohn/IndexCities/actions/runs/40661533696/artifacts/12804341685
This does not simulate any actors or prove intermediate-slice causal parity.
"""
import collections
import hashlib
import json
import sys
import zipfile
from decimal import Decimal

if len(sys.argv) != 2:
    raise SystemExit("usage: pr57-274-causal-snapshot-census.py <artifact-273.zip>")

with zipfile.ZipFile(sys.argv[1]) as z:
    paths = [f"{arm}/checkpoint-{n}.json"
             for arm in ("control", "probe1") for n in (0, 1)]
    data = {p: z.read(p) for p in paths}
    for n in (0, 1):
        assert data[f"control/checkpoint-{n}.json"] == data[f"probe1/checkpoint-{n}.json"]
    before, after = [json.loads(data[f"control/checkpoint-{n}.json"], parse_float=Decimal)
                     for n in (0, 1)]
    print("full_json_control_probe_exact", True)
    for n in (0, 1):
        raw = data[f"control/checkpoint-{n}.json"]
        print("checkpoint", n, "sha256", hashlib.sha256(raw).hexdigest())
    print("clock", before["Now"], "->", after["Now"])

    targets = (23, 1453, 1477)
    for kind in ("Vehicles", "Citizens", "Roads", "Buildings", "Shipments"):
        a = {x["Id"]: x for x in before[kind]}
        b = {x["Id"]: x for x in after[kind]}
        changed = {i: {k for k in a[i] if a[i][k] != b[i].get(k)}
                   for i in a.keys() & b.keys()}
        changed = {i: fields for i, fields in changed.items() if fields}
        print(kind, "changed_existing", len(changed),
              "new", len(b.keys() - a.keys()), "removed", len(a.keys() - b.keys()))
        print("changed_fields", dict(sorted(collections.Counter(
            k for fields in changed.values() for k in fields).items())))
        if kind == "Vehicles":
            print("unchanged_target_full_records", [i for i in targets if a[i] == b[i]])
            print("external_pose_changed", sum(
                i not in targets and bool(fields & {"Position", "Heading"})
                for i, fields in changed.items()))
            print("external_admission_related_changed", sum(
                i not in targets and bool(fields & {
                    "Route", "RouteIndex", "DockPosition", "ManoeuvreDock",
                    "ManoeuvreSpaces", "IntersectionClaim", "MovementAdmission",
                    "WaitingSince", "Status", "SitePath", "SitePathIndex",
                    "Cause", "PassengerCar"})
                for i, fields in changed.items()))
        if kind == "Citizens":
            print("crossing_node_changed", sum(
                "CrossingNodeId" in fields for fields in changed.values()))
            print("pedestrian_position_changed", sum(
                "Position" in fields for fields in changed.values()))
