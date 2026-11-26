#!/usr/bin/env python3
"""PR57 read-only census of authentic GitHub Actions checkpoint ZIPs.

Run: python3 scripts/pr57-real-depot-stock-census.py real-growth.zip legal-workplaces.zip reservation-census.zip
Never modifies saves or simulates shipments; a zero-stock observation is not proof of long-term impossibility.
"""
import argparse
import hashlib
import json
import pathlib
import zipfile


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def material_units(stock):
    """Support persisted ResourceStock as flat map or {'Units': {...}}."""
    if not isinstance(stock, dict):
        raise ValueError(f"unexpected ResourceStock: {type(stock).__name__}")
    units = stock.get("Units", stock)
    if not isinstance(units, dict):
        raise ValueError("unexpected ResourceStock.Units")
    return {str(k): int(v) for k, v in units.items()}


def census(paths):
    rows = []
    archives = []
    for path in paths:
        archive = pathlib.Path(path)
        data = archive.read_bytes()
        archives.append({"archive": archive.name, "sha256": digest(data)})
        with zipfile.ZipFile(archive) as z:
            for member in sorted(z.namelist()):
                if not member.endswith(".json") or "checkpoint-" not in member:
                    continue
                raw = z.read(member)
                state = json.loads(raw)
                depots = [b for b in state["Buildings"]
                          if b["Kind"] == "WorksDepot" and b.get("Completed")
                          and not b.get("Removed") and not b.get("OperationClosed")]
                active = [p for p in state["Projects"]
                          if p["Status"] in ("Waiting", "InProgress")]
                pending = [s for s in state["Shipments"]
                           if not s["Complete"] and s.get("ProjectId") is not None]
                stocks = [material_units(b["Stock"]) for b in depots]
                positive = [{k: v for k, v in stock.items() if v > 0} for stock in stocks]
                rows.append({"archive": archive.name, "checkpoint": member,
                             "sha256": digest(raw), "now": state["Now"],
                             "depots": len(depots), "positive_depot_units": positive,
                             "active_projects": len(active),
                             "pending_project_shipments": len(pending),
                             "has_positive_depot_stock": any(positive)})
    if not rows:
        raise ValueError("no authentic checkpoint-*.json files found")
    unique = {r["sha256"]: r for r in rows}
    return {"archives": archives, "checkpoints": len(rows),
            "unique_checkpoints": len(unique),
            "with_completed_operating_depot": sum(r["depots"] > 0 for r in rows),
            "with_active_projects": sum(r["active_projects"] > 0 for r in rows),
            "with_pending_project_shipments": sum(r["pending_project_shipments"] > 0 for r in rows),
            "with_positive_depot_stock": sum(r["has_positive_depot_stock"] for r in rows),
            "positive_stock_and_active_projects": sum(r["has_positive_depot_stock"] and r["active_projects"] > 0 for r in rows),
            "positive_stock_and_pending_project_shipments": sum(r["has_positive_depot_stock"] and r["pending_project_shipments"] > 0 for r in rows),
            "max_active_projects": max(r["active_projects"] for r in rows),
            "max_pending_project_shipments": max(r["pending_project_shipments"] for r in rows),
            "rows": rows}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archives", nargs="+", help="original Actions artifact ZIPs")
    args = parser.parse_args()
    print(json.dumps(census(args.archives), indent=2, ensure_ascii=False))
