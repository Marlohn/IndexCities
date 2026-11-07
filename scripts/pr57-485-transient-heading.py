#!/usr/bin/env python3
"""PR57 #485: synthetic SAT witness for a transient physical writer.

The SAT arithmetic and BodyOnWay threshold follow Mobility.SharedSpace.cs.
This is NOT a replay, not a cache, and not a .NET or throughput benchmark.
When invoked with a repository root, assert the exact production call chain.
"""
import math
import sys
from pathlib import Path


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1]


def penetration(a_at, a_heading, b_at, b_heading, a_length=2.5,
                a_width=1.0, b_length=2.5, b_width=1.0):
    af = (math.sin(a_heading), math.cos(a_heading))
    ar = (af[1], -af[0])
    bf = (math.sin(b_heading), math.cos(b_heading))
    br = (bf[1], -bf[0])
    d = (b_at[0] - a_at[0], b_at[1] - a_at[1])
    return min(
        a_length * abs(dot(af, axis)) + a_width * abs(dot(ar, axis))
        + b_length * abs(dot(bf, axis)) + b_width * abs(dot(br, axis))
        - abs(dot(d, axis))
        for axis in (af, ar, bf, br)
    )


def body_on_way_guard(candidate_heading):
    # A hypothetical actor advances north 0.5 m; a candidate is at (3, 4).
    # Both rectangles have half-length 2.5 and half-width 1.0 m.
    now = penetration((0, 0), 0, (3, 4), candidate_heading)
    next_pose = penetration((0, 0.5), 0, (3, 4), candidate_heading)
    limit = max(-0.1, now + 0.001)
    return now, next_pose, limit, next_pose > limit


def assert_production_chain(root):
    base = Path(root) / 'src/IndexCities.Simulation'
    shared = (base / 'Mobility.SharedSpace.cs').read_text(encoding='utf-8')
    mobility = (base / 'Mobility.cs').read_text(encoding='utf-8')
    audit = (base / 'Mobility.MutationAudit.Pr57.cs').read_text(encoding='utf-8')
    assert 'var heading=v.Heading;v.Heading=Math.Atan2(entering.X,entering.Z);' in shared
    assert 'var allowed=JunctionPreferenceAllows(v,node);v.Heading=heading;' in shared
    assert 'o.Position.Distance(junction.Position)<14&&CanTurnNow(o)).ToList()' in mobility
    assert 'var body=BodyOnWay(o,turn);' in shared
    assert 'foreach(var o in State.Vehicles){' in shared
    assert 'var now=Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading)' in shared
    assert 'if(Penetration(v,p.Position,p.Heading,o,o.Position,o.Heading)>limit)' in shared
    assert mobility.count('SpaceFree(v,space,space.Position-from,false)') == 2
    assert 'Pr57DirtyTouch(actor);' in audit
    assert 'Pr57MutationAfterVehicle(v);' in mobility
    # A boundary-only observer cannot see the temporary heading during a nested read.
    temporary = shared.split('var heading=v.Heading;v.Heading=Math.Atan2(', 1)[1].split('v.Heading=heading;', 1)[0]
    assert 'Pr57DirtyTouch(' not in temporary
    print('PR57_485_SOURCE_CHAIN_PASS nestedReader=True temporaryHeading=True '
          'boundaryHookAfterActor=True directTouchInTemporarySpan=False')


def main():
    if len(sys.argv) > 2:
        raise SystemExit('usage: pr57-485-transient-heading.py [repo-root]')
    if len(sys.argv) == 2:
        assert_production_chain(sys.argv[1])
    before = body_on_way_guard(0.0)
    during = body_on_way_guard(math.pi / 2)
    after = body_on_way_guard(0.0)
    assert before == after
    assert before[3] is False and during[3] is True
    assert before[:3] == (-1.0, -1.0, -0.1)
    assert abs(during[0] - (-0.5)) < 1e-12
    assert abs(during[1] - 0.0) < 1e-12
    print(f'PR57_485_SYNTHETIC_SAT_PASS before={before} during={during} after={after} '
          'scope=LOCAL_PREDICATE_ONLY_NOT_AUTHENTIC_REPLAY')


if __name__ == '__main__':
    main()
