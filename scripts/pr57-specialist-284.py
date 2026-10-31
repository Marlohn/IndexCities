#!/usr/bin/env python3
"""PR57 #284: endpoint-only first-unmet-resource and specialist-truck census.

Usage: python scripts/pr57-specialist-284.py checkpoint-0.json checkpoint-1.json
Authentic full saves only; no Step-level causal claims or invented supplies.
"""
import argparse
from collections import Counter
from decimal import Decimal
import hashlib
import json
from pathlib import Path

SCALE = Decimal(10) ** 9
BODY = {'Aggregate': 'Dump', 'Asphalt': 'Dump', 'Concrete': 'Mixer',
        'Steel': 'Flatbed', 'Wood': 'Flatbed'}


def load(path):
    data = path.read_bytes()
    return json.loads(data, parse_float=Decimal), hashlib.sha256(data).hexdigest()


def eligible(v):
    return (v['External'] and not v['PassengerCar'] and v['Machine'] == 'None'
            and v['HomeBuildingId'] is None and v['Body'] not in ('Tow', 'TowHeavy'))


def first_unmet(project, shipments):
    for kind, required in project['Required'].items():
        pending = sum((Decimal(s['Cargo'].get(kind, 0)) / SCALE for s in shipments
                       if s['ProjectId'] == project['Id'] and not s['Complete']), Decimal(0))
        remaining = Decimal(str(required)) - Decimal(project['Delivered'].get(kind, 0)) / SCALE - pending
        if remaining > Decimal('0.001'):
            return kind
    return 'none'


def census(state, label, cause_only_ids):
    active = [p for p in state['Projects'] if p['Status'] in ('Waiting', 'InProgress')]
    eligible_trucks = [v for v in state['Vehicles'] if eligible(v)]
    occupied = sum(v['Status'] != 'Outside' for v in eligible_trucks)
    cap = state['Tuning']['ExternalTruckCount']
    available = Counter(v['Body'] for v in eligible_trucks
                        if v['Status'] == 'Outside' and v['ServicePhase'] == 'None' and not v['BrokenDown'])
    first = {p['Id']: first_unmet(p, state['Shipments']) for p in active}
    print('PR57_284_FLEET', label, 'eligible', len(eligible_trucks), 'busy', occupied,
          'capacity', cap, 'availableByBody', json.dumps(dict(sorted(available.items()))))
    print('PR57_284_FIRST_UNMET', label,
          json.dumps(dict(sorted(Counter(first.values()).items()))))
    focus = [p for p in active if p['Id'] in cause_only_ids]
    focus_counts = Counter(first[p['Id']] for p in focus)
    print('PR57_284_CAUSE_ONLY', label, 'projects', len(focus),
          'firstUnmet', json.dumps(dict(sorted(focus_counts.items()))))
    for kind, count in sorted(focus_counts.items()):
        if kind in BODY:
            body = BODY[kind]
            print('PR57_284_GATE', label, 'resource', kind, 'projects', count,
                  'specialistBody', body, 'available', available[body],
                  'globalCapacityReached', occupied >= cap)
    return first


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('before', type=Path)
    ap.add_argument('after', type=Path)
    args = ap.parse_args()
    before, h0 = load(args.before)
    after, h1 = load(args.after)
    print('PR57_284_SHA', h0, h1)
    a = {p['Id']: p for p in before['Projects']}
    b = {p['Id']: p for p in after['Projects']}
    active = {id for id, p in a.items() if p['Status'] in ('Waiting', 'InProgress')}
    assert active <= b.keys(), 'active project missing from endpoint'
    cause_only = {id for id in active if a[id] != b[id] and
                  all(a[id].get(k) == b[id].get(k) for k in a[id].keys() | b[id].keys() if k != 'Cause')}
    print('PR57_284_COHORT', 'activeStart', len(active), 'causeOnlyEndpoint', len(cause_only))
    census(before, 'start', cause_only)
    census(after, 'end', cause_only)
    print('PR57_284_LIMIT', 'Endpoints only; no intra-Step writes, public edits, route/crew checks, or causal skip certificate')


if __name__ == '__main__':
    main()
