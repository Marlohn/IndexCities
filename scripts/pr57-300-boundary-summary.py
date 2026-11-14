#!/usr/bin/env python3
"""PR57 #300: verify authentic per-Advance boundary and net-pose observation.
No movement may be skipped; net unchanged state is not causal independence.
"""
import argparse
import json
import re
from pathlib import Path

# Boundaries are only batched NextVehicleMovementEvent calls: 210 GRID.
# The other 38 GRID Advance(2) use one unsplit physical pass each.
CASES = {'gridARTIFICIAL': (50, 12, 210, 26),
         'real14hREAL': (100, 100, 618, 8)}
FIELDS = ('boundaries', 'noEntry', 'full', 'movement', 'projection',
          'entriesSum', 'entriesMin', 'entriesMax', 'uniqueEntries',
          'changedSlices', 'unchangedSlices', 'changedTouches',
          'movedTouches', 'routeTouches', 'statusTouches',
          'blockedChecks', 'projectedCandidates', 'projectedRechecks')

def read_rows(path, calls):
    lines = path.read_text().splitlines()
    rows = []
    for line in lines:
        if not line.startswith('PR57_300_STEP '):
            continue
        row = dict(re.findall(r'(\w+)=([^\s]+)', line))
        assert all(k in row for k in FIELDS), (path, row)
        for key in FIELDS + ('call', 'batched', 'topEntryCount', 'topWinnerCount'):
            row[key] = int(row[key])
        for key in ('schedulerMs', 'movementMs', 'observerMs'):
            row[key] = float(row[key])
            assert row[key] >= 0
        assert row['noEntry'] + row['full'] + row['movement'] + row['projection'] == row['boundaries']
        assert row['changedSlices'] + row['unchangedSlices'] == row['boundaries']
        assert row['movedTouches'] <= row['changedTouches']
        assert row['topEntryCount'] <= row['boundaries']
        assert row['entriesMin'] <= row['entriesMax']
        rows.append(row)
    assert [r['call'] for r in rows] == list(range(1, calls + 1)), (path, len(rows))
    return rows

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('directory', type=Path)
    args = ap.parse_args()
    root = args.directory
    parity = 0
    for case, (calls, batched, boundaries, max_boundaries) in CASES.items():
        for control, probe in (('control1', 'probe1'), ('control2', 'probe2')):
            control_log = (root / f'{case}-{control}.log').read_text()
            probe_log = root / f'{case}-{probe}.log'
            assert 'PR57_300_STEP ' not in control_log
            for arm in (control, probe):
                log = (root / f'{case}-{arm}.log').read_text()
                assert log.count('PR57_294_FINAL ') == 1, (case, arm, 'final')
                assert log.count('PR57_294_CHECKPOINT ') == 4, (case, arm, 'checkpoint')
            for at in sorted({0, 20, calls // 2, calls}):
                a = (root / f'{case}-{control}' / f'checkpoint-{at}.json').read_bytes()
                b = (root / f'{case}-{probe}' / f'checkpoint-{at}.json').read_bytes()
                assert a == b, (case, control, probe, at)
                json.loads(a)
                parity += 1
            rows = read_rows(probe_log, calls)
            assert sum(r['batched'] for r in rows) == batched
            assert sum(r['boundaries'] for r in rows) == boundaries
            assert max(r['boundaries'] for r in rows) == max_boundaries
            batch_rows = [r for r in rows if r['batched']]
            assert all(r['boundaries'] >= 1 for r in batch_rows)
            totals = {k: sum(r[k] for r in rows) for k in
                      ('noEntry', 'full', 'movement', 'projection',
                       'entriesSum', 'changedSlices', 'unchangedSlices',
                       'changedTouches', 'movedTouches', 'routeTouches',
                       'statusTouches', 'blockedChecks',
                       'projectedCandidates', 'projectedRechecks')}
            totals.update(case=case, arm=probe, calls=calls, batched=batched,
                          boundaries=boundaries,
                          singleEntryBatches=sum(r['entriesMax'] == 1 for r in batch_rows),
                          maxEntries=max(r['entriesMax'] for r in rows),
                          maxUniqueEntries=max(r['uniqueEntries'] for r in rows),
                          maxTopEntryCount=max(r['topEntryCount'] for r in rows),
                          schedulerMs=round(sum(r['schedulerMs'] for r in rows), 3),
                          movementMs=round(sum(r['movementMs'] for r in rows), 3),
                          observerMs=round(sum(r['observerMs'] for r in rows), 3))
            print('PR57_300_CENSUS ' + json.dumps(totals, sort_keys=True), flush=True)
    assert parity == 16
    print('PR57_300_VERIFIED checkpointPairs=16/16 processes=8 allAdvance2=True observational=True noGainClaim=True', flush=True)

if __name__ == '__main__':
    main()
