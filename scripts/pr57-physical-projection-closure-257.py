#!/usr/bin/env python3
"""PR57 #257: source-anchored, read-only audit of the existing dense physical projection.

Usage: python3 scripts/pr57-physical-projection-closure-257.py
Requires only Python 3 and the repository checkout. Does not run the engine.

This is a *lexical* lower bound on fields and dependencies, NOT a complete
C# call graph, writer inventory, causal closure, benchmark or equivalence proof.
Fail closed when the audited source anchors move; re-audit rather than silently
reporting stale coverage.
"""
from __future__ import annotations

import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SIM = ROOT / "src" / "IndexCities.Simulation"


def source(name: str) -> str:
    return (SIM / name).read_text(encoding="utf-8")


def between(text: str, start: str, end: str) -> str:
    a = text.find(start)
    if a < 0:
        raise ValueError(f"source anchor missing: {start}")
    b = text.find(end, a + len(start))
    if b < 0:
        raise ValueError(f"source anchor missing: {end}")
    return text[a:b]


def body(text: str, signature: str) -> str:
    a = text.find(signature)
    if a < 0:
        raise ValueError(f"method signature missing: {signature}")
    opening = text.find("{", a + len(signature))
    if opening < 0:
        raise ValueError(f"body missing: {signature}")
    depth = 0
    for i in range(opening, len(text)):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return text[opening + 1:i]
    raise ValueError(f"unclosed body: {signature}")


def fields(text: str, variable: str) -> list[str]:
    return sorted(set(re.findall(r"\\b" + re.escape(variable) +
                                 r"\\.([A-Za-z_]\\w*)", text)))


def main() -> None:
    mobility = source("Mobility.cs")
    shared = source("Mobility.SharedSpace.cs")
    entry = source("Mobility.LaneEntry.cs")
    projection = source("Mobility.ExecPath.Pr57.cs")

    lane_scan = between(mobility, "foreach(var liveOther in candidates){",
                        "Pr57StageScanEnd(pr57StageScanStart);")
    bend_scan = between(mobility, "var bendLeaders=pr57BendReuse?",
                        "Pr57Stage249FinishBend")
    body_across = body(shared, "double? BodyAcrossLane(Vehicle v,Vehicle o)")
    blocks_entry = body(entry, "bool BlocksLaneEntry(Vehicle v,LaneAccess access,Vehicle other)")
    snapshot = between(projection,
                       "static Pr57ExecProjection Pr57ExecSnapshot(Vehicle v) => new(",
                       "void Pr57ExecBeginSlice()")

    # The snapshot stores list COUNTS, not contents. 'Id' is also deliberately
    # absent from its record, although the existing dense index maps IDs.
    projected = set(fields(snapshot, "v"))
    sites = {
        "s3_direct": fields(lane_scan, "other"),
        "s4_direct": fields(bend_scan, "o"),
        "body_across_lane": fields(body_across, "o"),
        "blocks_lane_entry": fields(blocks_entry, "other"),
    }
    missing = {site: sorted(set(values) - projected)
               for site, values in sites.items()}
    checks = {
        "entry_frame_distinct_from_live": "Pr57StageEntryFrame(liveOther)" in lane_scan,
        "body_across_is_delegated": "Pr57StageBodyAcross(v,other)" in lane_scan,
        "occupancy_is_delegated": "Pr57StageOccupies(other)" in lane_scan,
        "entry_block_is_delegated": "BlocksEntry(other)" in lane_scan,
        "entry_reads_route_elements": bool(re.search(r"other\\.Route\\s*\\[", blocks_entry)),
        "entry_reads_road_graph": "_roads." in blocks_entry,
        "entry_reads_tuning": "State.Tuning" in blocks_entry,
        "body_reads_dock_or_site_helpers":
            any(name in body_across for name in ("LeavesDock(o)", "OnCarriageway(o)", "OnSitePath(o)")),
        "projection_only_counts_route": "v.Route.Count" in snapshot,
        "projection_only_counts_detour": "v.Detour.Count" in snapshot,
        "projection_only_counts_reservations": "v.ManoeuvreSpaces.Count" in snapshot,
    }
    if not all(checks.values()):
        raise AssertionError("audited path changed; inspect missing dependency anchors")

    result = {
        "experiment": 257,
        "scope": "source-only lexical lower bound, no runtime/throughput claim",
        "sources": [
            "Mobility.cs", "Mobility.SharedSpace.cs",
            "Mobility.LaneEntry.cs", "Mobility.ExecPath.Pr57.cs",
        ],
        "direct_fields": sites,
        "snapshot_source_fields": sorted(projected),
        "direct_fields_absent_from_selected_projection": missing,
        "dependency_flags": checks,
        "conclusion": (
            "The existing #218/#222 selected dense projection is not a complete "
            "drop-in s3/s4 view: Machine and PassengerCar are missing, Id needs "
            "the external index, entry uses a distinct frozen frame, and "
            "BlocksLaneEntry reads route elements, road graph and tuning. "
            "Transitive calls and external writers require separate closure."
        ),
    }
    print(json.dumps(result, ensure_ascii=False, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
