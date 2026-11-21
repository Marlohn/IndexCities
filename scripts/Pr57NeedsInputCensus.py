#!/usr/bin/env python3
"""PR57 observational census of needs inputs at persisted checkpoints.

Usage: python3 scripts/Pr57NeedsInputCensus.py retained-184.zip

Reads authentic JSON checkpoints in Action #40451480020's artifact. Intervals
are 600 simulated seconds for REAL scenarios and 100 for GRID. Endpoint
equality is NOT proof of unchanged intermediate values; a changed pollution
cell is NOT necessarily sampled by a particular home. No state is modified.
"""
import argparse
import json
import zipfile

SCENARIOS = ("real12h", "real72h", "queueREAL", "gridARTIFICIAL")


def indexed(rows):
    return {row["Id"]: row for row in rows}


def census(before, after):
    ca, cb = indexed(before["Citizens"]), indexed(after["Citizens"])
    fa, fb = indexed(before["Families"]), indexed(after["Families"])
    ba, bb = indexed(before["Buildings"]), indexed(after["Buildings"])
    homes = {f["HomeId"] for f in fa.values() if f["HomeId"] is not None}
    homes &= set(ba) & set(bb)
    count = {
        "eligible_citizens": 0,
        "eligible_home_water_changed": 0,
        "eligible_home_electricity_changed": 0,
        "eligible_family_pantry_changed": 0,
        "eligible_position_changed": 0,
        "eligible_path_changed": 0,
        "eligible_vehicle_changed": 0,
        "homes": len(homes),
        "homes_water_changed": sum(ba[h]["WaterRatio"] != bb[h]["WaterRatio"] for h in homes),
        "homes_electricity_changed": sum(ba[h]["ElectricityRatio"] != bb[h]["ElectricityRatio"] for h in homes),
    }
    for cid, c in ca.items():
        d = cb.get(cid)
        if not d or not (c["Alive"] and c["Resident"] and c["InsideMap"]
                         and d["Alive"] and d["Resident"] and d["InsideMap"]):
            continue
        count["eligible_citizens"] += 1
        f, g = fa.get(c["FamilyId"]), fb.get(d["FamilyId"])
        h = ba.get(f["HomeId"]) if f and f["HomeId"] is not None else None
        j = bb.get(g["HomeId"]) if g and g["HomeId"] is not None else None
        if h and j:
            count["eligible_home_water_changed"] += h["WaterRatio"] != j["WaterRatio"]
            count["eligible_home_electricity_changed"] += h["ElectricityRatio"] != j["ElectricityRatio"]
        if f and g:
            count["eligible_family_pantry_changed"] += f["PantryUnits"] != g["PantryUnits"]
        count["eligible_position_changed"] += c["Position"] != d["Position"]
        count["eligible_path_changed"] += (c["Path"], c["PathIndex"]) != (d["Path"], d["PathIndex"])
        count["eligible_vehicle_changed"] += c["VehicleId"] != d["VehicleId"]
    p, q = before["PollutionCells"], after["PollutionCells"]
    keys = p.keys() | q.keys()
    count["pollution_cells_total"] = len(keys)
    count["pollution_cells_changed"] = sum(p.get(k) != q.get(k) for k in keys)
    return count


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifact_zip", help="Action #40451480020 artifact ZIP")
    args = parser.parse_args()
    with zipfile.ZipFile(args.artifact_zip) as archive:
        for scenario in SCENARIOS:
            snapshots = [
                json.loads(archive.read(f"{scenario}/plain/checkpoint-{i}.json"))
                for i in range(5)
            ]
            for i in range(4):
                row = census(snapshots[i], snapshots[i + 1])
                row.update(
                    scenario=scenario, interval=i + 1,
                    simulated_seconds=100 if scenario == "gridARTIFICIAL" else 600
                )
                print(json.dumps(row, sort_keys=True, separators=(",", ":")))


if __name__ == "__main__":
    main()
