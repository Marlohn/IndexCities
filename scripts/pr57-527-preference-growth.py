#!/usr/bin/env python3
"""PR57 #527: independent offline audit of the authentic #522 ZIP.
Usage: python3 pr57-527-preference-growth.py pr57-522-original.zip
No simulator modification, skipped slices or causal equivalence claim.
"""
import hashlib
import json
import re
import sys
import zipfile
from decimal import Decimal as D
from pathlib import Path

DIGEST = '2561e9a9fe2379ee1f60b28ae45eaac67ceee81f998e69b7d10059e649d8fbd9'
CASES = {'congestedREAL121': 100, 'worksREAL47': 300, 'GRID_ARTIFICIAL': 300}
KV = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^\s]+)')


def rows(log, prefix):
    return [dict(KV.findall(line)) for line in log.splitlines()
            if line.startswith(prefix + ' ')]


def row(log, prefix, boundary, key='calls'):
    found = [r for r in rows(log, prefix) if int(r[key]) == boundary]
    assert len(found) == 1, (prefix, boundary, len(found))
    return found[0]


def sample(log, boundary):
    d = row(log, 'PR57_JUNCTION_DETAIL', boundary)
    t = row(log, 'PR57_HOT_TIMING', boundary, 'slices')
    j = row(log, 'PR57_JUNCTION_ALLOC', boundary)
    fields = ['preferenceCalls', 'preferenceMB', 'turnWayCalls', 'turnWayMB',
              'bodyOnWayCalls', 'bodyOnWayMB']
    return {**{k: D(d[k]) for k in fields},
            'preferenceMs': D(t['preferenceMs']),
            'admissionCalls': D(j['admissionCalls']),
            'deniedCalls': D(j['deniedCalls']),
            'admissionMB': D(j['admissionMB'])}


def subtract(a, b):
    return {k: b[k] - a[k] for k in b}


def verify(z):
    assert z.testzip() is None and len(z.namelist()) == 37
    summary = json.loads(z.read('summary.json'))
    assert summary['experiment'] == 522 and summary['observational'] is True
    assert summary['gainAccepted'] is False
    for case, steps in CASES.items():
        starts, ends = [], []
        for arm in ('A1', 'B1', 'B2', 'A2'):
            base = f'{case}/{arm}/'
            start, end = (z.read(base + f'checkpoint-{i}.json') for i in (0, 1))
            starts.append(start)
            ends.append(end)
            meta = summary['cases'][case]['arms'][arm]
            assert hashlib.sha256(start).hexdigest() == meta['initialSha256']
            assert hashlib.sha256(end).hexdigest() == meta['finalSha256']
            log = z.read(base + 'full.log').decode('utf-8')
            assert 'INVARIANTS OK' in log
            assert len(row(log, 'STEP_PROFILE', steps, 'steps')) > 0
        assert len(set(starts)) == len(set(ends)) == 1


def main(path):
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == DIGEST
    with zipfile.ZipFile(path) as z:
        verify(z)
        allocations = []
        for arm in ('B1', 'B2'):
            log = z.read(f'congestedREAL121/{arm}/full.log').decode('utf-8')
            first = sample(log, 300)
            second = subtract(first, sample(log, 600))
            allocations.append((first, second))
            for name, r in (('first300', first), ('next300', second)):
                count = r['preferenceCalls']
                print(f'{arm} {name}: preferenceCalls={count} preferenceMB={r["preferenceMB"]} '
                      f'KBperCall={1000*r["preferenceMB"]/count:.3f} '
                      f'preferenceMs={r["preferenceMs"]} '
                      f'usPerCall={1000*r["preferenceMs"]/count:.2f} '
                      f'admissions={r["admissionCalls"]} denied={r["deniedCalls"]} '
                      f'denialPct={100*r["deniedCalls"]/r["admissionCalls"]:.2f} '
                      f'preferenceShareAdmissionPct={100*r["preferenceMB"]/r["admissionMB"]:.2f} '
                      f'turnWayKBperCall={1000*r["turnWayMB"]/r["turnWayCalls"]:.3f} '
                      f'bodyOnWayKBperCall={1000*r["bodyOnWayMB"]/r["bodyOnWayCalls"]:.3f}')
            ratio = (second['preferenceMB']/second['preferenceCalls']) / (
                first['preferenceMB']/first['preferenceCalls'])
            assert ratio > D('1.4')
            assert second['preferenceCalls'] < first['preferenceCalls']*D('1.04')
            print(f'{arm} preferenceAllocationPerCallGrowthPct={(ratio-1)*100:.2f}')
        # Two independent instrumented processes: allocation and call counts
        # deterministic, wall timers intentionally NOT asserted equal.
        for a, b in zip(*allocations):
            for field in a:
                if field != 'preferenceMs':
                    assert a[field] == b[field], field
    print('PR57_527_AUTHENTIC_PREFERENCE_PER_CALL_GROWTH_PASS')


if __name__ == '__main__':
    assert len(sys.argv) == 2, __doc__
    main(sys.argv[1])
