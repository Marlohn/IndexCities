#!/usr/bin/env python3
"""PR57 #490: count canonical utility water-filling rounds on authentic REAL saves.

Usage: python3 scripts/pr57-490-authentic-allocation-rounds.py pr57-465-original.zip pr57-445-original.zip

Offline, observational reconstruction; does not run Advance(2), alter state or
claim bitwise .NET parity. The implementation mirrors CapacityAllocation.Allocate
and validates every active building's stored ratio / untreated sewage (1e-12).
"""
import hashlib
import json
import math
import sys
import zipfile

EXPECTED = {
    "465": "370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1",
    "445": "ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a",
}


def distance(p, q):
    dx, dz = p["X"] - q["X"], p["Z"] - q["Z"]
    return math.sqrt(dx * dx + dz * dz)


def allocate(capacity, consumers, sources):
    """Exact branch/order structure of Infrastructure.cs CapacityAllocation.Allocate.

    Python floating point may differ by a few ULPs from .NET; this is why the
    independent persisted-output check below has a stated tolerance.
    """
    demand = [v for _, v in consumers]
    result = [0.0] * len(consumers)
    total = 0.0
    for value in demand:
        total += value
    if total <= 0 or capacity <= 0:
        return result, {"path": "zero", "round_sizes": [], "weighted": 0, "total": total}
    if capacity >= total:
        return demand.copy(), {"path": "sufficient", "round_sizes": [], "weighted": 0, "total": total}
    pending = [i for i, v in enumerate(demand) if v > 0]
    weights = [0.0] * len(consumers)
    for i in pending:
        nearest = min((distance(consumers[i][0], p) for p in sources), default=256.0)
        weights[i] = demand[i] / (1 + nearest / 128)
    weighted = len(pending)
    remaining = capacity
    rounds = []
    while pending and remaining > 0.000001:
        rounds.append(len(pending))
        weight_sum = 0.0
        for i in pending:
            weight_sum += weights[i]
        allocated = 0.0
        for i in pending:
            amount = min(demand[i] - result[i], remaining * weights[i] / weight_sum)
            result[i] += amount
            allocated += amount
        remaining -= allocated
        kept = []
        for i in pending:
            if not (result[i] >= demand[i] - 0.000001):
                kept.append(i)
        pending = kept
        if allocated < 0.000001:
            break
        assert len(rounds) < 10000, "unexpectedly unbounded water-filling"
    return result, {"path": "limited", "round_sizes": rounds, "weighted": weighted, "total": total}


def close(actual, expected, context):
    assert math.isfinite(actual) and math.isfinite(expected), context
    assert abs(actual - expected) <= 1e-12, (context, actual, expected, actual - expected)


def inspect(state):
    active = [b for b in state["Buildings"] if b["Completed"] and not b["Removed"]]
    streets = [r for r in state["Roads"] if not r["Highway"]]
    assert len({b["Id"] for b in active}) == len(active)
    assert len({r["Id"] for r in streets}) == len(streets)
    # All authentic checkpoints in this probe are at night. The clock advances
    # AFTER UpdateInfrastructure; this restriction avoids assuming the current
    # Now hour is the same as the phase's hour at a day/night boundary.
    hour = int(state["Now"][11:13])
    assert hour >= 18 or hour < 6, state["Now"]
    wind = [b["Position"] for b in active if b["Kind"] == "WindPower"]
    water_sources = [b["Position"] for b in active if b["Kind"] == "WaterTreatment"]
    sewage_sources = [b["Position"] for b in active if b["Kind"] == "SewageTreatment"]
    # The fixture's sources must have exterior access; parity checks below
    # independently reject the reconstruction if this assumption is false.
    electric = [(b["Position"], b["ElectricityDemand"]) for b in active]
    electric += [(r["Position"], 0.0 if r["ConstructionOnly"] else
                  0.12 * (r["MaintenanceRatioUnits"] / 1_000_000_000_000)) for r in streets]
    water = [(b["Position"], b["WaterDemand"]) for b in active]
    sewage = [(b["Position"], b["SewageProduced"]) for b in active]
    inputs = [
        ("electric", state["ElectricityCapacity"], electric, wind),
        ("water", state["PotableCapacity"], water, water_sources),
        ("sewage", state["SewageCapacity"], sewage, sewage_sources),
    ]
    results = {}
    for kind, capacity, consumers, sources in inputs:
        out, meta = allocate(capacity, consumers, sources)
        for i, b in enumerate(active):
            if kind == "electric":
                observed = b["ElectricityRatio"]
                predicted = 0.0 if b["ElectricityDemand"] <= 0 else out[i] / b["ElectricityDemand"]
            elif kind == "water":
                observed = b["WaterRatio"]
                predicted = 0.0 if b["WaterDemand"] <= 0 else out[i] / b["WaterDemand"]
            else:
                observed = b["UntreatedSewage"]
                predicted = max(0.0, b["SewageProduced"] - out[i])
            close(predicted, observed, (kind, b["Id"]))
        if kind == "electric":
            for i, road in enumerate(streets):
                demand = consumers[len(active) + i][1]
                predicted = 0.0 if demand <= 0 else out[len(active) + i] / demand
                close(predicted, road["ElectricityRatio"], (kind, "road", road["Id"]))
            close(sum(b["ElectricityDemand"] for b in active) + sum(v for _, v in electric[len(active):]), state["ElectricityDemand"], "electric total")
        elif kind == "water":
            close(meta["total"], state["WaterDemand"], "water total")
        else:
            close(meta["total"], state["SewageDemand"], "sewage total")
        results[kind] = {
            "consumers": len(consumers), "sources": len(sources),
            "positive": sum(v > 0 for _, v in consumers),
            "path": meta["path"], "rounds": meta["round_sizes"],
            "loop_visits": 3 * sum(meta["round_sizes"]),
            "weighted": meta["weighted"],
        }
    return results


def load_archive(path, label):
    digest = hashlib.sha256(open(path, "rb").read()).hexdigest()
    assert digest == EXPECTED[label], (path, digest)
    archive = zipfile.ZipFile(path)
    assert archive.testzip() is None, "ZIP CRC failure"
    return archive


def main():
    assert len(sys.argv) == 3, "usage: probe.py pr57-465-original.zip pr57-445-original.zip"
    z465 = load_archive(sys.argv[1], "465")
    z445 = load_archive(sys.argv[2], "445")
    # Original A/B controls are byte-identical for every replay checkpoint.
    for segment in ("pre", "mid", "post"):
        for i in (0, 1):
            a = z465.read(f"A-{segment}/checkpoint-{i}.json")
            b = z465.read(f"B-{segment}/checkpoint-{i}.json")
            assert a == b, (segment, i)
    for i in (0, 1):
        assert z445.read(f"pr57-445/A/checkpoint-{i}.json") == z445.read(f"pr57-445/B/checkpoint-{i}.json")
    cases = [
        ("REAL-congested-20h", z465.read("20h.json")),
        ("REAL-congested-235958", z465.read("A-mid/checkpoint-0.json")),
        ("REAL-congested-000000", z465.read("A-mid/checkpoint-1.json")),
        ("REAL-congested-000002", z465.read("A-post/checkpoint-1.json")),
        ("REAL-works-220000", z445.read("pr57-445/A/checkpoint-0.json")),
        ("REAL-works-220320", z445.read("pr57-445/A/checkpoint-1.json")),
    ]
    max_rounds = {"electric": 0, "water": 0, "sewage": 0}
    for name, raw in cases:
        state = json.loads(raw)
        result = inspect(state)
        for kind, metrics in result.items():
            max_rounds[kind] = max(max_rounds[kind], len(metrics["rounds"]))
            print(json.dumps({"case": name, "now": state["Now"], "utility": kind, **metrics},
                             separators=(",", ":"), sort_keys=True))
    print("MAX_ROUNDS", json.dumps(max_rounds, sort_keys=True))
    print("PR57_490_AUTHENTIC_ALLOCATION_ROUNDS_PASS")


if __name__ == "__main__":
    main()
