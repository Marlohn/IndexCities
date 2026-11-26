#!/usr/bin/env python3
"""PR57 #291: conservative source-level inventory of projected mobility writers.

Run from a full Git checkout:
    python3 scripts/pr57-291-writer-surface.py

This is a lexical audit of a PINNED source commit, not a runtime write journal,
not a complete C# data-flow analysis, and not a throughput benchmark. It
intentionally includes writes to temporary snapshots and unrelated objects
whose fields share a name; it also misses aliases, indirect calls, initializers,
some list mutations, and all fields outside FIELDS. Review every candidate
before placing a production writer hook.
"""
import argparse
import json
import re
import subprocess

PINNED_REF = "14486133a49896f7f6ce201da592402a6cf7d776"
PREFIX = "src/IndexCities.Simulation/"
FIELDS = (
    "VehicleId", "CrossingNodeId", "BoardingVehicleId", "DriverId",
    "IntersectionClaim", "Position", "Heading", "Route", "RouteIndex",
    "Path", "PathIndex", "DockPosition", "ManoeuvreDock",
)
# Matches property assignments / increments and selected in-place list edits.
# Comparison operators (==, !=, >=, <=) are deliberately excluded.
CANDIDATE = re.compile(
    r"\.(" + "|".join(FIELDS) + r")\s*"
    r"(?:[+*/-]=|(?<![=!<>])=(?!=|>)|\+\+|--|"
    r"\s*\.\s*(?:Clear|Add|AddRange|Remove|RemoveAt|Insert)\s*\()"
)
INITIAL_SCOPE = {
    "Mobility.cs", "Cars.cs", "Breakdowns.cs", "Towing.cs",
    "Economy.cs", "BuildingDemolition.Execution.cs",
    "BuildingDemolition.Cargo.cs",
}
# Manually inspected counterexamples to a journal limited to INITIAL_SCOPE.
ANCHORS = {
    "RoadDemolition.cs": ("vehicle.IntersectionClaim=null", "citizen.CrossingNodeId=null"),
    "Construction.Paving.cs": ("v.IntersectionClaim=null",),
    "MunicipalRelocation.Execution.cs": ("v.Route=route", "v.DockPosition=DockAt"),
    "Construction.Feeding.cs": ("vehicle.Position=vehicle.Position.MoveTowards",),
    "Mobility.SharedSpace.cs": ("v.Heading=Math.Atan2", "v.Heading=heading"),
    "Mobility.LaneEntry.cs": ("snapshot.Route.Clear()",),
}


def git(*args):
    result = subprocess.run(
        ["git", *args], check=True, text=True, encoding="utf-8",
        stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    )
    return result.stdout


def census(ref):
    paths = [
        path for path in git("ls-tree", "-r", "--name-only", ref, "--", PREFIX).splitlines()
        if path.startswith(PREFIX) and path.endswith(".cs")
        and "pr57" not in path.lower()
    ]
    rows = []
    anchor_results = {}
    for path in paths:
        name = path[len(PREFIX):]
        source = git("show", ref + ":" + path)
        hits = []
        for line_number, line in enumerate(source.splitlines(), 1):
            if line.strip().startswith("//"):
                continue
            for match in CANDIDATE.finditer(line):
                hits.append({"line": line_number, "field": match.group(1)})
        if name in ANCHORS:
            anchor_results[name] = {
                anchor: [
                    number for number, line in enumerate(source.splitlines(), 1)
                    if anchor in line and not line.strip().startswith("//")
                ]
                for anchor in ANCHORS[name]
            }
        rows.append({"file": name, "candidate_occurrences": len(hits), "sites": hits})
    total = sum(row["candidate_occurrences"] for row in rows)
    outside = sum(
        row["candidate_occurrences"] for row in rows
        if row["file"] not in INITIAL_SCOPE
    )
    return {
        "source_ref": ref, "scope": PREFIX,
        "files_scanned": len(rows),
        "files_with_candidates": sum(row["candidate_occurrences"] > 0 for row in rows),
        "lexical_candidates": total,
        "outside_seven_named_files": outside,
        "anchor_lines": anchor_results,
        "files": rows,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ref", default=PINNED_REF)
    parser.add_argument("--details", action="store_true",
                        help="include each candidate occurrence and line")
    args = parser.parse_args()
    result = census(args.ref)
    if args.ref == PINNED_REF:
        expected = (40, 20, 348, 78)
        actual = tuple(result[k] for k in (
            "files_scanned", "files_with_candidates",
            "lexical_candidates", "outside_seven_named_files",
        ))
        if actual != expected:
            raise AssertionError(
                "Pinned source census drift: expected %s, got %s" % (expected, actual)
            )
        for name, anchors in result["anchor_lines"].items():
            for anchor, locations in anchors.items():
                if not locations:
                    raise AssertionError("Missing manually checked anchor %s: %s" %
                                         (name, anchor))
    if not args.details:
        for row in result["files"]:
            row.pop("sites")
        result["files"] = [
            row for row in result["files"] if row["candidate_occurrences"]
        ]
    print(json.dumps(result, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
