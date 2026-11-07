#!/usr/bin/env python3
"""PR57 #293: conservative physical/route dependency graph from authentic #292 saves.

Observational OFFLINE analysis. Neither a causal graph nor a scheduler. The
12m guard is only one BodyOnWay prefilter, and checkpoints miss transient writes.
"""
import argparse
import collections
import hashlib
import json
import math
import zipfile

SOURCE_SHA256 = "f838c026dfb81f08769b5814cbf3902890cc0ad630e8217ca137adca364c5fcc"
CASES = {"worksREAL": (0, 20, 50, 100),
         "queueREAL": (0, 20, 50, 100),
         "real14hREAL": (0, 20, 50, 100),
         "gridARTIFICIAL": (0, 20, 25, 50)}


def distance(a, b):
    return math.hypot(a["X"] - b["X"], a["Z"] - b["Z"])


def is_bend(node, roads):
    # Mobility.SharedSpace.cs:IsBend; exact shape test, not proof of admission.
    if len(node["Neighbours"]) != 2 or node["Crosswalk"]:
        return False
    a, b = (roads.get(i) for i in node["Neighbours"])
    if a is None or b is None:
        return False
    p = (a["Position"]["X"] - node["Position"]["X"],
         a["Position"]["Z"] - node["Position"]["Z"])
    q = (b["Position"]["X"] - node["Position"]["X"],
         b["Position"]["Z"] - node["Position"]["Z"])
    lp, lq = math.hypot(*p), math.hypot(*q)
    return lp > .01 and lq > .01 and p[0]*q[0] + p[1]*q[1] >= -.5*lp*lq


def component_stats(ids, edges):
    ids = list(ids)
    parent = {i: i for i in ids}

    def root(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for a, b in edges:
        parent[root(a)] = root(b)
    sizes = sorted(collections.Counter(root(i) for i in ids).values(), reverse=True)
    return {"components": len(sizes), "largest": sizes[:5],
            "isolated": sum(size == 1 for size in sizes)}


def census(state):
    roads = {r["Id"]: r for r in state["Roads"]}
    vehicles = state["Vehicles"]
    gateway = [v for v in vehicles if distance(v["Position"], state["Entry"]) < 8]
    # BodyOnWay ignores Outside/Towed bodies. Tow exceptions are NOT modeled.
    active = [v for v in vehicles if v["Status"] not in ("Outside", "Towed")]
    near = set()
    for i, a in enumerate(active):
        for b in active[i+1:]:
            if distance(a["Position"], b["Position"]) < 12 and not (
                distance(a["Position"], state["Entry"]) < 8 and
                distance(b["Position"], state["Entry"]) < 8):
                near.add((min(a["Id"], b["Id"]), max(a["Id"], b["Id"])))
    naive = collections.defaultdict(list)
    admitted = collections.defaultdict(list)
    for v in vehicles:
        if v["Status"] != "Travelling":
            continue
        index, route = v["RouteIndex"], v["Route"]
        if index >= len(route) or route[index] not in roads:
            continue
        node = roads[route[index]]
        if distance(v["Position"], node["Position"]) >= 14:
            continue
        naive[node["Id"]].append(v["Id"])
        # Mobility.cs:sharedAhead, before MayEnterJunction. This is only an
        # eligibility filter; it does not prove a CanTurnNow call occurs.
        if (len(node["Neighbours"]) >= 3 or node["Crosswalk"] or
            (index > 0 and index+1 < len(route) and is_bend(node, roads))):
            admitted[node["Id"]].append(v["Id"])

    def pairs(groups):
        return {tuple(sorted((a, b))) for group in groups.values()
                for i, a in enumerate(group) for b in group[i+1:]}

    naive_edges, admitted_edges = pairs(naive), pairs(admitted)
    return {"vehicles": len(vehicles), "eligible_bodies": len(active),
            "gateway_all": len(gateway),
            "gateway_travelling": sum(v["Status"] == "Travelling" for v in gateway),
            "gateway_passenger_travelling": sum(v["Status"] == "Travelling" and v["PassengerCar"] for v in gateway),
            "near12_edges": len(near), "near12_graph": component_stats((v["Id"] for v in active), near),
            "same_next_node_edges": len(naive_edges),
            "shared_ahead_edges": len(admitted_edges),
            "shared_ahead_groups": {str(k): len(v) for k, v in admitted.items() if len(v)>1},
            "largest_next_node_group": sorted(((len(v), k) for k, v in naive.items()), reverse=True)[:3],
            "_near_edges": near, "_admitted_edges": admitted_edges}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifact_zip")
    args = parser.parse_args()
    with open(args.artifact_zip, "rb") as f:
        digest = hashlib.file_digest(f, "sha256").hexdigest()
    if digest != SOURCE_SHA256:
        raise ValueError(f"Unexpected artifact SHA-256 {digest}")
    with zipfile.ZipFile(args.artifact_zip) as z:
        for case, checkpoints in CASES.items():
            previous = None
            for tick in checkpoints:
                left = z.read(f"{case}-control/checkpoint-{tick}.json")
                right = z.read(f"{case}-probe/checkpoint-{tick}.json")
                if left != right:
                    raise ValueError(f"Parity mismatch {case} {tick}")
                stats = census(json.loads(left))
                near, admitted = stats.pop("_near_edges"), stats.pop("_admitted_edges")
                if previous is not None:
                    stats["near12_edge_changes_since_previous_checkpoint"] = len(near ^ previous[0])
                    stats["shared_ahead_edge_changes_since_previous_checkpoint"] = len(admitted ^ previous[1])
                previous = near, admitted
                print(json.dumps({"case": case, "advance2_calls": tick, **stats}, sort_keys=True))
    # The GRID gateway is a straight road: a route-node graph makes a false
    # 380-vehicle clique, but sharedAhead cannot invoke junction admission there.
    # This assertion is intentionally scoped to the authentic #292 fixture.
    with zipfile.ZipFile(args.artifact_zip) as z:
        grid = census(json.loads(z.read("gridARTIFICIAL-control/checkpoint-0.json")))
    assert grid["gateway_travelling"] == 380
    assert grid["gateway_passenger_travelling"] == 370
    assert grid["same_next_node_edges"] == 72010
    assert grid["shared_ahead_edges"] == 0
    print("PR57_293_VERIFIED parity=16/16 gateway=380 false_node_edges=72010 causal_closure=NOT_PROVEN")


if __name__ == "__main__":
    main()
