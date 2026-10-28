#!/usr/bin/env python3
"""PR57 #310: audit authentic raw ABBA logs/checkpoints, never change state.
Usage: python3 scripts/pr57-310-phase-fanout-audit.py <extracted-artifact-dir>
"""
import hashlib
import pathlib
import re
import sys

root = pathlib.Path(sys.argv[1])
kv = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)')
keys = ('vehicleChanged', 'bodyChanged', 'routeChanged', 'claimChanged',
        'driverChanged', 'citizenChanged', 'citizenPose', 'citizenVehicle',
        'citizenCrossing', 'roadRevisionChanged')

def parse(case, arm):
    log = (root / f'{case}-probe{arm}.log').read_text()
    rows = [dict(kv.findall(line)) for line in log.splitlines()
            if line.startswith('PR57_310_PHASE ')]
    assert len(rows) == 75, (case, arm, len(rows))
    totals = {}
    for phase in ('vehicles', 'paving', 'citizens'):
        selected = [row for row in rows if row['phase'] == phase]
        assert len(selected) == 25
        assert len({int(row['call']) for row in selected}) == 25
        totals[phase] = {
            key: sum((row[key] == 'True') if key == 'roadRevisionChanged'
                     else int(row[key]) for row in selected)
            for key in keys
        }
    return totals

pairs = 0
for case in ('real14hREAL', 'gridARTIFICIAL'):
    observations = []
    for arm in ('1', '2'):
        control = root / f'{case}-control{arm}'
        probe = root / f'{case}-probe{arm}'
        names = {p.name for p in control.glob('checkpoint-*.json')}
        assert len(names) == 4, (case, arm, names)
        assert names == {p.name for p in probe.glob('checkpoint-*.json')}
        for name in sorted(names):
            assert (control / name).read_bytes() == (probe / name).read_bytes(), (case, arm, name)
            pairs += 1
        totals = parse(case, arm)
        observations.append(totals)
        final = 100 if case == 'real14hREAL' else 50
        digest = hashlib.sha256((probe / f'checkpoint-{final}.json').read_bytes()).hexdigest()
        print(f'PR57_310_VERIFIED case={case} arm={arm} checkpoints={len(names)} finalSHA256={digest}')
        for phase, counts in totals.items():
            print('PR57_310_TOTAL', case, arm, phase,
                  ' '.join(f'{key}={value}' for key, value in counts.items()))
    assert observations[0] == observations[1], (case, 'probe counts differ')
    print('PR57_310_REPEATED_PROBES_IDENTICAL', case)
print('PR57_310_CHECKPOINT_PAIRS', pairs)
