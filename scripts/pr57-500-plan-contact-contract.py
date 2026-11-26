#!/usr/bin/env python3
"""PR57 #500: source-anchored counterexample for a fused physical-plan view.

A manoeuvre records every in-place intermediate heading in `spaces` but emits
only the final heading into `held`. Therefore an optimization that derives the
shared-space set or its entry direction solely from emitted poses is unsound.
This is a small mathematical counterexample, NOT a city replay, .NET benchmark,
or evidence that the case occurred in the REAL congestion fixture.

Usage: python3 scripts/pr57-500-plan-contact-contract.py [Mobility.SharedSpace.cs]
With a source argument, require the exact source Git blob and reader anchors.
"""
import hashlib
import math
import sys
from pathlib import Path

SOURCE_BLOB = "78c69c5525e2a3b4ff583f8fe486b4dbc82af7a9"


def git_blob(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()


def enters_area(at, heading, node, length, width, clearance):
    # The same strict comparisons and orientation as C# EntersArea.
    dx = abs(at[0] - node[0]); dz = abs(at[1] - node[1])
    s = abs(math.sin(heading)); c = abs(math.cos(heading))
    return dx < clearance + length*s + width*c and dz < clearance + length*c + width*s


def source_contract(path):
    raw = path.read_bytes()
    assert git_blob(raw) == SOURCE_BLOB, "wrong Mobility.SharedSpace.cs revision"
    s = raw.decode("utf-8")
    anchors = (
        "for(var part=0;part<=8;part++)Note(at,facing+turn*part/8);held.Add((o,at,direction));",
        "foreach(var n in near)if(!found.Contains(n)&&EntersArea(o,p,h,n))found.Add(n);",
        "var index=way.FindIndex(p=>EntersArea(v,p.Position,p.Heading,space));var from=index>0?way[index-1].Position:v.Position;",
        "if(poses.Count==0)return null;var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;",
        "if(poses.Any(p=>p.Position.Distance(at)<12&&Penetration(v,p.Position,p.Heading,o,at,o.Heading)>-.1))return o;",
        "if(yieldToTraffic&&TrafficOnWay(v,way) is {})",
        "if(BodyOnWay(v,way) is {} body)",
        "if(!safe)way=WayIn(v,dock,out spaces);",
    )
    for anchor in anchors:
        assert s.count(anchor) == 1, (anchor, s.count(anchor))
    # The order of these consumers is part of the admission decision.
    admit = s[s.index("    bool AdmitManoeuvre("):s.index("    // Only the physical connection dock")]
    ordered = ("ConflictsWithDockWay(v,way)", "foreach(var space in spaces)",
               "TrafficOnWay(v,way)", "BodyOnWay(v,way)")
    indices = [admit.index(a) for a in ordered]
    assert indices == sorted(indices), indices
    print("SOURCE_BLOB_AND_ORDERED_READERS_PASS", SOURCE_BLOB)


def main():
    assert len(sys.argv) <= 2
    if len(sys.argv) == 2:
        source_contract(Path(sys.argv[1]))
    # Hypothetical 8m x 2m body, node at (0,0), centre at (6,6),
    # carriageway half-width 3.3m as in Mobility.BayJunction.cs.
    at = (6.0, 6.0); node = (0.0, 0.0)
    length = 4.0; width = 1.0; clearance = 3.3
    headings = [math.pi * part / 16 for part in range(9)]
    noted = [part for part, h in enumerate(headings)
             if enters_area(at, h, node, length, width, clearance)]
    emitted = [(at, headings[-1])]  # exact in-place-turn emission pattern
    found_from_held = [i for i, (p, h) in enumerate(emitted)
                       if enters_area(p, h, node, length, width, clearance)]
    assert not enters_area(at, headings[0], node, length, width, clearance)
    assert not enters_area(at, headings[-1], node, length, width, clearance)
    assert noted and all(0 < p < 8 for p in noted)
    assert found_from_held == []
    # The original `Note` would add this node to `spaces`, while `FindIndex`
    # over emitted `way` returns -1. These are distinct semantic projections.
    print("intermediate_heading_indices=", noted)
    print("emitted_turn_heading=pi/2 emitted_pose_contact_indices=", found_from_held)
    print("space_recorded_by_Note=True first_emitted_contact_index=-1")
    print("IMPLICATION=retain_separate_ordered_space_contacts_and_emitted_pose_readers")
    print("LIMIT=synthetic_geometry_not_a_REAL_city_occurrence_or_speedup")
    print("PR57_500_PLAN_CONTACT_CONTRACT_PASS")


if __name__ == "__main__":
    main()
