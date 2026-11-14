#!/usr/bin/env python3
"""PR57 #525: no-displacement physical tail allocation census from authentic #522 ZIP.

Observational, retrospective; does not license skipping any vehicle or slice.
Usage: python3 pr57-525-queue-allocation-tail.py pr57-522-original.zip
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

DIGEST = '2561e9a9fe2379ee1f60b28ae45eaac67ceee81f998e69b7d10059e649d8fbd9'
CASES = {'congestedREAL121': 100, 'worksREAL47': 300, 'GRID_ARTIFICIAL': 300}
GROUPS = ('operations', 'nontravelling', 'moved', 'queue', 'junction',
          'waiting', 'stationary-trip', 'other')


def rows(log, prefix):
    return [dict(re.findall(r'([A-Za-z][A-Za-z0-9]*)=([^\s]+)', line))
            for line in log.splitlines() if line.startswith(prefix)]


def snapshot(log, boundary):
    matches = [r for r in rows(log, 'PR57_VEHICLE_COST ')
               if int(r['slices']) == boundary]
    assert len(matches) == len(GROUPS) and {r['group'] for r in matches} == set(GROUPS)
    return {r['group']: r for r in matches}


def difference(earlier, later, group, field):
    return float(later[group][field]) - float(earlier[group][field])


def main(path):
    data = Path(path).read_bytes()
    assert hashlib.sha256(data).hexdigest() == DIGEST
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 37
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 522 and summary['observational'] is True
        assert summary['gainAccepted'] is False
        for case, steps in CASES.items():
            starts, ends = [], []
            for arm in ('A1', 'B1', 'B2', 'A2'):
                prefix = f'{case}/{arm}/'
                start = z.read(prefix + 'checkpoint-0.json')
                end = z.read(prefix + 'checkpoint-1.json')
                starts.append(start)
                ends.append(end)
                log = z.read(prefix + 'full.log').decode('utf-8')
                assert 'INVARIANTS OK' in log
                profile = rows(log, 'STEP_PROFILE ')
                assert len(profile) == 1 and int(profile[0]['steps']) == steps
                arm_summary = summary['cases'][case]['arms'][arm]
                assert hashlib.sha256(start).hexdigest() == arm_summary['initialSha256']
                assert hashlib.sha256(end).hexdigest() == arm_summary['finalSha256']
                if arm.startswith('A'):
                    assert not rows(log, 'PR57_VEHICLE_COST ')
                    continue
                last = snapshot(log, 600 if case == 'congestedREAL121' else 300)
                if case != 'congestedREAL121':
                    alloc = sum(float(r['allocMB']) for r in last.values())
                    queue = sum(float(last[g]['allocMB']) for g in ('queue', 'waiting'))
                    print(f'{case} {arm} finalSlices=300 classifiedAllocMB={alloc:.3f} '
                          f'queueWaitingAllocMB={queue:.3f} sharePct={100*queue/alloc:.2f} '
                          'scope=COMPARATIVE_NOT_CAUSAL')
                    continue
                earlier, later = snapshot(log, 420), snapshot(log, 660)
                moved = difference(earlier, later, 'moved', 'calls')
                moved_bytes = difference(earlier, later, 'moved', 'allocMB')
                assert moved == 0 and moved_bytes == 0
                calls = sum(difference(earlier, later, g, 'calls') for g in ('queue', 'waiting'))
                alloc = sum(difference(earlier, later, g, 'allocMB') for g in ('queue', 'waiting'))
                wall = sum(difference(earlier, later, g, 'ms') for g in ('queue', 'waiting'))
                claim = sum(difference(earlier, later, g, 'claimChanges') for g in ('queue', 'waiting'))
                spaces = sum(difference(earlier, later, g, 'spaceCountChanges') for g in ('queue', 'waiting'))
                assert calls == 5754 and claim == 0 and spaces == 0
                total = sum(difference(earlier, later, g, 'allocMB') for g in GROUPS)
                print(f'{case} {arm} sliceWindow=420..660 publicSteps=100 '
                      f'queueWaitingVisits={int(calls)} movedVisits={int(moved)} '
                      f'queueWaitingAllocMB={alloc:.3f} queueWaitingWallMs={wall:.3f} '
                      f'allocKBPerVisit={1000*alloc/calls:.3f} '
                      f'classifiedDeltaAllocMB={total:.3f} '
                      f'queueWaitingSharePct={100*alloc/total:.2f} '
                      f'claimCountChanges={int(claim)} spaceCountChanges={int(spaces)} '
                      'scope=INTRUSIVE_END_STATE_CLASSES_NOT_CAUSAL_INDEPENDENCE')
            assert all(s == starts[0] for s in starts)
            assert all(e == ends[0] for e in ends)
    print('PR57_525_AUTHENTIC_QUEUE_ALLOCATION_TAIL_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-525-queue-allocation-tail.py pr57-522-original.zip')
    main(sys.argv[1])
