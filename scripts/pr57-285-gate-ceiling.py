#!/usr/bin/env python3
"""PR57 #285: conservative upper bound on shared specialist-truck gate in REAL works.

Usage: python scripts/pr57-285-gate-ceiling.py checkpoint-0.json checkpoint-1.json worksEarlyREAL-probe.log
Endpoint census + intrusively measured project times; NOT a causal skip certificate or throughput A/B.
"""
import argparse
from collections import Counter
from decimal import Decimal
import hashlib
import json
import re
from pathlib import Path

SCALE = Decimal(10) ** 9
BODY = {'Aggregate': 'Dump', 'Asphalt': 'Dump', 'Concrete': 'Mixer',
        'Steel': 'Flatbed', 'Wood': 'Flatbed'}


def first_unmet(p, shipments):
    for kind, required in p['Required'].items():
        pending = sum((Decimal(s['Cargo'].get(kind, 0)) / SCALE for s in shipments
                       if s['ProjectId'] == p['Id'] and not s['Complete']), Decimal(0))
        if Decimal(str(required)) - Decimal(p['Delivered'].get(kind, 0)) / SCALE - pending > Decimal('.001'):
            return kind
    return 'none'


def number(log, pattern):
    match = re.search(pattern, log, re.MULTILINE)
    if not match:
        raise ValueError('Missing log marker: ' + pattern)
    return Decimal(match.group(1))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('before', type=Path)
    parser.add_argument('after', type=Path)
    parser.add_argument('probe_log', type=Path)
    args = parser.parse_args()
    raw = [path.read_bytes() for path in (args.before, args.after)]
    a, b = [json.loads(data, parse_float=Decimal) for data in raw]
    projects = {p['Id']: p for p in a['Projects'] if p['Status'] in ('Waiting', 'InProgress')}
    end = {p['Id']: p for p in b['Projects']}
    assert len(projects) == 68 and projects.keys() <= end.keys()
    cause_only = {pid for pid, p in projects.items() if p != end[pid] and
                  all(p.get(k) == end[pid].get(k) for k in p.keys() | end[pid].keys() if k != 'Cause')}
    assert len(cause_only) == 60 and 904 not in cause_only
    for label, state in (('start', a), ('end', b)):
        trucks = [v for v in state['Vehicles'] if v['External'] and not v['PassengerCar'] and
                  v['Machine'] == 'None' and v['HomeBuildingId'] is None and
                  v['Body'] not in ('Tow', 'TowHeavy')]
        busy = sum(v['Status'] != 'Outside' for v in trucks)
        available = Counter(v['Body'] for v in trucks if v['Status'] == 'Outside' and
                            v['ServicePhase'] == 'None' and not v['BrokenDown'])
        first = Counter(first_unmet(next(p for p in state['Projects'] if p['Id'] == pid), state['Shipments'])
                        for pid in cause_only)
        print('PR57_285_GATE', label, 'causeOnly', len(cause_only), 'firstUnmet', dict(first),
              'availableByBody', dict(sorted(available.items())), 'busy', busy,
              'capacity', state['Tuning']['ExternalTruckCount'])
        if label == 'end':
            assert first == {'Aggregate': 60} and available['Dump'] == 0 and available['Cargo'] == 1
            assert busy < state['Tuning']['ExternalTruckCount']
    log = args.probe_log.read_text()
    total = {}
    excluded = {}
    for phase in ('order', 'progress'):
        total[phase] = number(log, rf'^PR57_282_SUM phase={phase} .*?wallMs=([0-9.]+)')
        excluded[phase] = number(log, rf'^PR57_282_PROJECT phase={phase} id=904 .*?wallMs=([0-9.]+)')
    wall_ms = number(log, r'^STEP_PROFILE steps=100 measuredWall=([0-9.]+)s') * 1000
    upper = sum((total[p] - excluded[p] for p in total), Decimal(0))
    fraction = upper / wall_ms
    print('PR57_285_UPPER_BOUND', 'projectCohort', len(cause_only),
          'orderUpperMs', total['order'] - excluded['order'],
          'progressUpperMs', total['progress'] - excluded['progress'],
          'combinedUpperMs', upper, 'measuredWallMs', wall_ms,
          'fractionUpper', round(fraction, 6), 'speedupUpper', round(1 / (1 - fraction), 5))
    print('PR57_285_INPUT_SHA256', *(hashlib.sha256(data).hexdigest() for data in raw))
    print('PR57_285_LIMIT', 'Upper bound subtracts only project 904, includes 7 other projects; '
          'instrumented project timings and Step wall, no within-Step proof, no A/B, no 10k extrapolation')


if __name__ == '__main__':
    main()
