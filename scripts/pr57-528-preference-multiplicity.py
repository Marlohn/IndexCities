#!/usr/bin/env python3
"""PR57 #528: cross-audit #462, #497 and #522 on the same authentic REAL save.

Usage: python3 scripts/pr57-528-preference-multiplicity.py ZIP462 ZIP497 ZIP522

All comparisons are of independent, intrusive, observational runs, not an
integrated optimization A/B. Physical invocation boundaries include dt=0.
"""
import hashlib
import json
import re
import sys
import zipfile
from decimal import Decimal as D
from pathlib import Path

INPUTS = (
    ('462', 'feebd310b101740c83ced56246cbb91d765a9c22978fece8dd7f98747f92b128', 20),
    ('497', '89eb18a37fc30d697562daa556c24d0742000473eeaf1b6d9a7940fde534c7fa', 12),
    ('522', '2561e9a9fe2379ee1f60b28ae45eaac67ceee81f998e69b7d10059e649d8fbd9', 37),
)
PREFIX = {'462': 'all-canturn-462/economyREAL14h',
          '497': 'congestedREAL121', '522': 'congestedREAL121'}
ARMS = {'462': ('A', 'B'), '497': ('A1', 'B1', 'B2', 'A2'),
        '522': ('A1', 'B1', 'B2', 'A2')}
KV = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^\s]+)')


def records(log, prefix):
    return [dict(KV.findall(line)) for line in log.splitlines()
            if line.startswith(prefix + ' ')]


def one(log, prefix, **filters):
    matches = [r for r in records(log, prefix)
               if all(r.get(k) == str(v) for k, v in filters.items())]
    assert len(matches) == 1, (prefix, filters, len(matches))
    return matches[0]


def main(paths):
    zips = {}
    try:
        for (name, digest, members), path in zip(INPUTS, paths):
            assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == digest, name
            z = zipfile.ZipFile(path)
            assert z.testzip() is None and len(z.namelist()) == members, name
            zips[name] = z
        initial, final = [], []
        logs = {}
        for name, z in zips.items():
            per_arm = []
            for arm in ARMS[name]:
                base = f'{PREFIX[name]}/{arm}/'
                checkpoints = [z.read(base+f'checkpoint-{i}.json') for i in (0, 1)]
                per_arm.append(checkpoints)
                log = z.read(base+'full.log').decode('utf-8')
                assert 'INVARIANTS OK' in log, (name, arm)
                assert re.search(r'^WINDOW 1 seconds=200 ', log, re.M), (name, arm)
                if name != '462':
                    assert re.search(r'^STEP_PROFILE steps=100 ', log, re.M), (name, arm)
                logs[name, arm] = log
            assert all(pair == per_arm[0] for pair in per_arm[1:]), name
            initial.append(per_arm[0][0]); final.append(per_arm[0][1])
        assert len(set(initial)) == len(set(final)) == 1
        assert hashlib.sha256(initial[0]).hexdigest() == '779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'
        assert hashlib.sha256(final[0]).hexdigest() == '9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23'
        summary = json.loads(zips['522'].read('summary.json'))
        assert summary['experiment'] == 522 and summary['observational'] is True
        assert summary['gainAccepted'] is False
        for arm in ARMS['522']:
            meta = summary['cases']['congestedREAL121']['arms'][arm]
            assert meta['initialSha256'] == hashlib.sha256(initial[0]).hexdigest()
            assert meta['finalSha256'] == hashlib.sha256(final[0]).hexdigest()
        fanout = records(logs['462', 'B'], 'PR57_462_SUM')
        assert len(fanout) == 718
        assert [int(r['slices']) for r in fanout] == list(range(1, 719))
        assert all(int(r['repeats']) == 0 and int(r['spaceFreeCalls']) == 0
                   for r in fanout)
        assert int(fanout[-1]['calls']) == 1596
        first_nested = int(one(logs['462', 'B'], 'PR57_462_SUM', slices=300)['calls'])
        total_nested = int(one(logs['462', 'B'], 'PR57_462_SUM', slices=600)['calls'])
        second_nested = total_nested-first_nested
        assert (first_nested, second_nested, total_nested) == (516, 708, 1224)
        for arm in ('B1', 'B2'):
            scoped = one(logs['497', arm], 'PR57_497_KIND', kind='CanTurnNow')
            pref_scope = one(logs['497', arm], 'PR57_497_KIND', kind='JunctionPreferenceAllows')
            assert int(scoped['calls']) == total_nested
            assert int(pref_scope['calls']) == 1406
            assert int(pref_scope['inclusiveBytes'])-int(pref_scope['exclusiveBytes']) == int(scoped['inclusiveBytes'])
            assert int(pref_scope['exclusiveBytes']) < int(pref_scope['inclusiveBytes'])/100
            a = one(logs['522', arm], 'PR57_JUNCTION_DETAIL', calls=300)
            b = one(logs['522', arm], 'PR57_JUNCTION_DETAIL', calls=600)
            p1, p2 = int(a['preferenceCalls']), int(b['preferenceCalls'])-int(a['preferenceCalls'])
            m1, m2 = D(a['preferenceMB']), D(b['preferenceMB'])-D(a['preferenceMB'])
            assert (p1, p2) == (696, 710)
            assert (m1, m2) == (D('13.35'), D('19.42'))
            k1, k2 = m1*1000/first_nested, m2*1000/second_nested
            # Exact arithmetic identity for these rounded inclusive MB counters.
            multiplicity = m1*(D(second_nested)/first_nested-1)
            residual = (m2-m1)-multiplicity
            print(f'{arm} early300 preferenceCalls={p1} nestedCanTurnNow={first_nested} '
                  f'nestedPerPreference={D(first_nested)/p1:.6f} preferenceMB={m1} '
                  f'inclusiveKBperNested={k1:.3f}')
            print(f'{arm} late300 preferenceCalls={p2} nestedCanTurnNow={second_nested} '
                  f'nestedPerPreference={D(second_nested)/p2:.6f} preferenceMB={m2} '
                  f'inclusiveKBperNested={k2:.3f}')
            print(f'{arm} preferenceMBDelta={m2-m1:.3f} '
                  f'callMultiplicityTermMB={multiplicity:.3f} '
                  f'callMultiplicitySharePct={100*multiplicity/(m2-m1):.2f} '
                  f'perNestedInclusiveTermMB={residual:.3f} '
                  f'perNestedInclusiveGrowthPct={100*(k2/k1-1):.2f} '
                  f'preferenceExclusiveTotalBytes={pref_scope["exclusiveBytes"]} '
                  f'canTurnNowInclusiveTotalBytes={scoped["inclusiveBytes"]}')
        print('PR57_528_AUTHENTIC_CROSS_ARTIFACT_MULTIPLICITY_PASS')
    finally:
        for z in zips.values():
            z.close()


if __name__ == '__main__':
    assert len(sys.argv) == 4, __doc__
    main(sys.argv[1:])
