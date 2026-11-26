#!/usr/bin/env python3
"""PR57 #265: endpoint-only queue geometry, not causal invalidation.

Run: python3 scripts/pr57-cohort-corridors-265.py pr57-264.zip
Source: Action #40642980982, artifact #12796541232. Python stdlib only.
"""
import json
import math
import re
import sys
from collections import defaultdict
from decimal import Decimal
from zipfile import ZipFile


def point(v):
    p = v['Position']
    return float(p['X']), float(p['Z'])


def gap(a, b):
    return math.dist(point(a), point(b))


def corridor(v):
    x, z = point(v)
    h = float(v['Heading'])
    if abs(math.sin(h)) > .999 and abs(math.cos(h)) < .05:
        return ('E-W', round(z, 2), x)
    if abs(math.cos(h)) > .999 and abs(math.sin(h)) < .05:
        return ('N-S', round(x, 2), z)
    return ('off-axis', v['Id'], 0)


def main(path):
    with ZipFile(path) as z:
        def read(name):
            return json.loads(z.read(name), parse_float=Decimal)
        b, a = read('control/checkpoint-0.json'), read('control/checkpoint-1.json')
        for n in (0, 1):
            assert z.read(f'control/checkpoint-{n}.json') == z.read(f'probe/checkpoint-{n}.json')
        log = z.read('probe.log').decode()
    before, after = ({v['Id']: v for v in s['Vehicles']} for s in (b, a))
    assert before.keys() == after.keys()
    stable = ('Position', 'Heading', 'WaitingSince', 'Cause', 'RouteIndex')
    ids = sorted(i for i, v in before.items() if v['Status'] == 'Travelling'
                 and all(v.get(k) == after[i].get(k) for k in stable))
    assert len(ids) == 24 and all(before[i] == after[i] for i in ids)
    changed = sorted(i for i in before if before[i] != after[i] and i not in ids)
    corridors = defaultdict(list)
    for i in ids:
        axis, lateral, longitudinal = corridor(before[i])
        corridors[(axis, lateral)].append((longitudinal, i))
    chains = []
    for (axis, lateral), members in sorted(corridors.items()):
        members.sort()
        if axis == 'off-axis':
            continue
        run = [members[0][1]]
        for (prev, _), (cur, i) in zip(members, members[1:]):
            if abs((cur - prev) - 8) > .05:
                chains.append(run)
                run = []
            run.append(i)
        chains.append(run)
    near = {str(i): [j for j in changed if min(gap(before[i], before[j]),
                                               gap(before[i], after[j])) <= 20]
            for i in ids}
    match = re.search(r'^PR57_264_COHORT .*$', log, re.M)
    assert match
    metrics = dict(re.findall(r'(\w+)=([^\s]+)', match.group()))
    assert (int(metrics['slices']), int(metrics['persistentPasses']),
            int(metrics['persistentMoved']), int(metrics['persistentCauseChanges'])) == (600, 14400, 0, 14)
    print(json.dumps({
        'scope': 'REAL_14H_200S_ENDPOINTS_ONLY_NOT_CAUSAL_CLOSURE',
        'alive': sum(bool(c['Alive']) for c in b['Citizens']),
        'vehicles': len(before), 'cohortIds': ids,
        'corridors': {f'{axis}@{lat}': [i for _, i in sorted(v)]
                      for (axis, lat), v in sorted(corridors.items())},
        'eightMetreChains': [c for c in chains if len(c) >= 2],
        'distinctNextRoadNodes': len({before[i]['Route'][before[i]['RouteIndex']]
                                      for i in ids if before[i]['RouteIndex'] < len(before[i]['Route'])}),
        'otherVehiclesNetChanged': changed,
        'cohortNearNetChangedVehicleWithin20mAtEitherEndpoint': {i: v for i, v in near.items() if v},
        'cohortWithNoSuchEndpointNeighbour': sum(not v for v in near.values()),
        'cohortFullStateIdenticalAcrossEndpoints': len(ids),
        'transientCauseChangesWithin600Slices': int(metrics['persistentCauseChanges']),
        'cohortActorMsIntrusive': float(metrics['persistentMs']),
        'warning': 'Neither 8m chains nor 20m endpoint neighbourhoods close the live causal read/write set.'
    }, sort_keys=True, indent=2))


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-cohort-corridors-265.py <artifact-264.zip>')
    main(sys.argv[1])
