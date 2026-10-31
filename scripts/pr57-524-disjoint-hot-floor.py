#!/usr/bin/env python3
"""PR57 #524: independent cost-floor audit of original #522 ABBA ZIP.

The four hot timing scopes (site, crossing, turn, junction) are sequential,
not nested in one another. Junction *detail* scopes are nested and excluded.
Group attribution to these scopes is NOT recorded: use only set-theoretic bounds.
Usage: python3 pr57-524-disjoint-hot-floor.py pr57-522-original.zip
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

DIGEST = '2561e9a9fe2379ee1f60b28ae45eaac67ceee81f998e69b7d10059e649d8fbd9'
CASES = {'congestedREAL121': (100, 600), 'worksREAL47': (300, 300),
         'GRID_ARTIFICIAL': (300, 300)}
GROUPS = ('operations', 'nontravelling', 'moved', 'queue', 'junction',
          'waiting', 'stationary-trip', 'other')
HOT = ('siteMs', 'crossingMs', 'turnMs', 'junctionMs')


def rows(text, prefix):
    return [dict(re.findall(r'([A-Za-z][A-Za-z0-9]*)=([^\\s]+)', line))
            for line in text.splitlines() if line.startswith(prefix)]


def main(path):
    data = Path(path).read_bytes()
    assert hashlib.sha256(data).hexdigest() == DIGEST
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 37
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 522 and summary['observational'] is True
        assert summary['gainAccepted'] is False
        for case, (steps, boundary) in CASES.items():
            starts, ends = [], []
            for arm in ('A1', 'B1', 'B2', 'A2'):
                base = f'{case}/{arm}/'
                start, end = z.read(base + 'checkpoint-0.json'), z.read(base + 'checkpoint-1.json')
                starts.append(start)
                ends.append(end)
                log = z.read(base + 'full.log').decode('utf-8')
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
                profile = rows(log, 'STEP_PROFILE ')
                assert len(profile) == 1 and int(profile[0]['steps']) == steps
                assert hashlib.sha256(start).hexdigest() == summary['cases'][case]['arms'][arm]['initialSha256']
                assert hashlib.sha256(end).hexdigest() == summary['cases'][case]['arms'][arm]['finalSha256']
                if arm.startswith('A'):
                    assert not rows(log, 'PR57_HOT_TIMING ')
                    continue
                hot = [r for r in rows(log, 'PR57_HOT_TIMING ') if int(r['slices']) == boundary]
                cost = [r for r in rows(log, 'PR57_VEHICLE_COST ') if int(r['slices']) == boundary]
                assert len(hot) == 1 and len(cost) == len(GROUPS)
                assert {r['group'] for r in cost} == set(GROUPS)
                by = {r['group']: float(r['ms']) for r in cost}
                classified = sum(by.values())
                direct = sum(float(hot[0][key]) for key in HOT)
                queue_wait = by['queue'] + by['waiting']
                assert 0 < direct < classified
                # Worst-case all direct hot work belongs to queue/wait; any remainder is outside.
                queue_outside_hot = max(0, queue_wait - direct)
                queue_outside_junction = max(0, queue_wait - float(hot[0]['junctionMs']))
                print(f'{case} {arm} at_slices={boundary} classifiedMs={classified:.3f} '
                      f'queueWaitingMs={queue_wait:.3f} directHotMs={direct:.3f} '
                      f'directHotSharePct={100*direct/classified:.2f} '
                      f'nonHotFloorMs={classified-direct:.3f} '
                      f'queueOutsideAllHotFloorMs={queue_outside_hot:.3f} '
                      f'queueOutsideJunctionFloorMs={queue_outside_junction:.3f} '
                      f'queueOutsideAllHotFloorPct={100*queue_outside_hot/queue_wait:.2f} '
                      f'queueOutsideJunctionFloorPct={100*queue_outside_junction/queue_wait:.2f}')
            assert all(x == starts[0] for x in starts)
            assert all(x == ends[0] for x in ends)
    print('PR57_524_AUTHENTIC_DISJOINT_HOT_FLOOR_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-524-disjoint-hot-floor.py pr57-522-original.zip')
    main(sys.argv[1])
