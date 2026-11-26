#!/usr/bin/env python3
"""PR57 #449: conservative allocation intersection bound; original #447 ZIP only.

If a measured region allocates R and *all* non-waiting actors together
allocate N, at least max(0,R-N) bytes of that region belong to waiting actors.
No new CI, cross-tab, causal reuse, or speedup is inferred.
"""
import hashlib
import json
import sys
import zipfile
from decimal import Decimal
from pathlib import Path

ZIP_SHA = '89887c3b15105e8ba2860390b5349dd046a365e28629b61970a085b092d17efd'
SAVE_SHA = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'
MEMBERS = {'A/checkpoint-10800.json', 'B/checkpoint-10800.json', 'A/full.log',
           'B/full.log', 'checkpoints.sha256', 'summary-console.log', 'summary.json'}


def rows(log, prefix):
    return [dict(p.split('=', 1) for p in line.split()[1:] if '=' in p)
            for line in log.splitlines() if line.startswith(prefix + ' ')]


def bound(row, keys, nonwaiting):
    # Each printed MB is rounded to 0.01 MB; take a conservative interval.
    value = sum(Decimal(row[k]) for k in keys)
    err = Decimal('0.005') * len(keys)
    lo, hi = (value-err)*1_000_000, (value+err)*1_000_000
    assert lo > nonwaiting
    return {'reportedMB': str(value), 'minimumWaitingBytes': int(lo-nonwaiting),
            'minimumWaitingPct': round(float((lo-nonwaiting)*100/hi), 5)}


def audit(path):
    assert hashlib.sha256(path.read_bytes()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == MEMBERS
        a, b = (z.read(f'{arm}/checkpoint-10800.json') for arm in 'AB')
        assert a == b and len(a) == 2_254_838
        assert hashlib.sha256(a).hexdigest() == SAVE_SHA
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 447 and summary['fullJsonParity']
        logs = {arm: z.read(f'{arm}/full.log').decode() for arm in 'AB'}
        for arm in 'AB':
            assert len(rows(logs[arm], 'PR57_337_HOUR')) == 6
            assert len(rows(logs[arm], 'PR57_ALLOC_PROFILE')) == 252
            assert len(rows(logs[arm], 'PR57_ALLOC_REGIONS')) == 252
            assert len(rows(logs[arm], 'PR57_JUNCTION_ALLOC')) == 252
            assert len(rows(logs[arm], 'PR57_JUNCTION_DETAIL')) == 252
        cohort = rows(logs['B'], 'PR57_447_COHORT')
        assert len(cohort) == 30
        by_kind = [sum(int(c['allocatedBytes']) for c in cohort if int(c['kind']) == k)
                   for k in range(5)]
        assert sum(by_kind) == 30_215_004_968 and by_kind[2] == 30_180_992_744
        nonwaiting = sum(by_kind)-by_kind[2]
        assert nonwaiting == 34_012_224
        p = rows(logs['B'], 'PR57_ALLOC_PROFILE')[-1]
        r = rows(logs['B'], 'PR57_ALLOC_REGIONS')[-1]
        j = rows(logs['B'], 'PR57_JUNCTION_ALLOC')[-1]
        d = rows(logs['B'], 'PR57_JUNCTION_DETAIL')[-1]
        assert all(int(x['calls']) == 75600 for x in (p, r, j, d))
        for prefix, keys in [('PR57_ALLOC_PROFILE', ('preLaneMB','postTurnMB')),
                             ('PR57_ALLOC_REGIONS', ('entryMB','junctionMB'))]:
            control = rows(logs['A'], prefix)[-1]
            shadow = rows(logs['B'], prefix)[-1]
            assert all(abs(Decimal(control[k])-Decimal(shadow[k])) <= Decimal('.02') for k in keys)
        keys = ('manoeuvreMB','areaOwnerMB','bendHolderMB','bendPriorityMB',
                'preferenceMB','turnWayMB','bodyOnWayMB','otherMB')
        assert abs(sum(Decimal(d[k]) for k in keys)-Decimal(j['admissionMB'])) <= Decimal('.04')
        assert abs(Decimal(j['admissionMB'])+Decimal(j['deniedMB'])-Decimal(r['junctionMB'])) <= Decimal('.02')
        assert (int(j['admissionCalls']),int(j['deniedCalls'])) == (323915,323799)
        hot = sum(Decimal(d[k]) for k in ('preferenceMB','turnWayMB','bodyOnWayMB'))
        result = {'sourceZipSha256': ZIP_SHA, 'finalSaveSha256': SAVE_SHA,
                  'advance2PerArm': 10800, 'physicalSlicesInRegions': 75600,
                  'allNonwaitingActorBytes': nonwaiting, 'waitingActorBytes': by_kind[2],
                  'lowerBounds': {
                      'preLane': bound(p, ('preLaneMB',), nonwaiting),
                      'postTurn': bound(p, ('postTurnMB',), nonwaiting),
                      'combined': bound(p, ('preLaneMB','postTurnMB'), nonwaiting),
                      'junction': bound(r, ('junctionMB',), nonwaiting),
                      'entry': bound(r, ('entryMB',), nonwaiting)},
                  'junctionAdmissionCalls': 323915, 'junctionDenials': 323799,
                  'junctionHotThreeMB': str(hot),
                  'junctionHotThreeSharePct': round(float(hot*100/Decimal(j['admissionMB'])),3),
                  'scope': 'intrusive allocated bytes, not CPU or throughput; no 10k extrapolation',
                  'speedupAccepted': False}
        print('PR57_449_BOUND_PASS ' + json.dumps(result, sort_keys=True))


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python3 pr57-449-marginal-bound.py ORIGINAL_447.zip')
    audit(Path(sys.argv[1]))
