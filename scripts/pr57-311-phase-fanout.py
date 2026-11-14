#!/usr/bin/env python3
"""PR57 #311: offline causal-fanout bounds from the authentic #310 artifact.

Usage: python3 scripts/pr57-311-phase-fanout.py <unpacked-#310-artifact>
No engine or save mutation. The phase snapshots are net deltas, not a journal.
"""
import hashlib
import pathlib
import re
import sys

root = pathlib.Path(sys.argv[1])
pattern = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)')
fields = ('vehicleChanged', 'bodyChanged', 'routeChanged', 'claimChanged',
          'driverChanged', 'citizenChanged', 'citizenPose', 'citizenVehicle',
          'citizenCrossing', 'roadRevisionChanged')

def read(case, arm):
    base = root / f'{case}-control{arm}'
    probe = root / f'{case}-probe{arm}'
    names = {p.name for p in base.glob('checkpoint-*.json')}
    assert len(names) == 4 and names == {p.name for p in probe.glob('checkpoint-*.json')}
    for name in names:
        assert (base / name).read_bytes() == (probe / name).read_bytes(), (case, arm, name)
    log = (root / f'{case}-probe{arm}.log').read_text()
    rows = [dict(pattern.findall(line)) for line in log.splitlines()
            if line.startswith('PR57_310_PHASE ')]
    assert len(rows) == 75, (case, arm, len(rows))
    by_phase = {}
    for phase in ('vehicles', 'paving', 'citizens'):
        selected = [r for r in rows if r['phase'] == phase]
        assert len(selected) == 25 and len({int(r['call']) for r in selected}) == 25
        by_phase[phase] = selected
    digest = hashlib.sha256((probe / ('checkpoint-100.json' if case == 'real14hREAL'
                                       else 'checkpoint-50.json')).read_bytes()).hexdigest()
    return by_phase, digest

def summarize(rows):
    totals = {k: sum(int(r[k]) if k != 'roadRevisionChanged'
                     else (r[k] == 'True') for r in rows) for k in fields}
    active = {k: sum((int(r[k]) > 0) if k != 'roadRevisionChanged'
                     else (r[k] == 'True') for r in rows) for k in fields}
    return totals, active

for case in ('real14hREAL', 'gridARTIFICIAL'):
    a, hash_a = read(case, 1)
    b, hash_b = read(case, 2)
    assert hash_a == hash_b
    for phase in a:
        assert [(r['call'], *(r[k] for k in fields)) for r in a[phase]] == [
            (r['call'], *(r[k] for k in fields)) for r in b[phase]]
    v, c = a['vehicles'], a['citizens']
    changed = [int(r['call']) for r in v if int(r['vehicleChanged'])]
    crossed = [int(r['call']) for r in c if int(r['citizenCrossing'])]
    quiet_cross = [r for r in c if not int(r['citizenCrossing'])]
    pose_without_crossing = sum(int(r['citizenPose']) for r in quiet_cross)
    print(f'CASE={case} checkpointPairs=8 finalSHA256={hash_a}')
    for phase, rows in a.items():
        totals, active = summarize(rows)
        print(f' PHASE={phase} totals={totals} activeCalls={active}')
    print(f' vehicleChangeCalls={changed} crossingChangeCalls={crossed}')
    print(f' crossingStableCalls={len(quiet_cross)} poseChangesDuringThoseCalls={pose_without_crossing}')
    print(f' vehicleChangeCallsWithoutSameCallCrossing={sorted(set(changed)-set(crossed))}')
    if case == 'real14hREAL':
        assert changed == [76, 79, 92, 95]
        assert pose_without_crossing == 381
        assert sum(int(r['citizenPose']) for r in c) == 641
        assert sum(int(r['citizenCrossing']) for r in c) == 10
    else:
        assert pose_without_crossing == 1791
        assert sum(int(r['citizenPose']) for r in c) == 6191
        assert sum(int(r['citizenVehicle']) for r in v) == 47
        assert sum(int(r['citizenCrossing']) for r in c) == 70
print('CHECKPOINT_PARITY=16/16 repeatedProbeCounts=identical; NO_THROUGHPUT_GAIN_CLAIM')
