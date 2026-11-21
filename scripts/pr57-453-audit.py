#!/usr/bin/env python3
"""Offline original-ZIP audit of #452 three-fixture ABBA; no engine execution."""
import hashlib
import json
import re
import statistics
import sys
import zipfile
from pathlib import Path

ZIP_SHA = '3a04391334af5c3af3a7244199ab15d41fb3bebf35ec96f11ae2b9878aee0026'
CASES = {'economyREAL14h': (121, 98, 100), 'worksREAL': (47, 42, 100),
         'gridARTIFICIAL': (1111, 787, 50)}
MODES = ('A1', 'B1', 'B2', 'A2')

def audit(path):
    assert hashlib.sha256(path.read_bytes()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 38
        summary = json.loads(z.read('sat-stack-452/summary.json'))
        assert summary['experiment'] == 452 and summary['allAdvance2']
        assert set(summary['cases']) == set(CASES)
        result = {}
        for case, (living, vehicles, calls) in CASES.items():
            record = summary['cases'][case]
            assert (record['living'], record['vehicles'], record['callsAdvance2PerArm']) == (living, vehicles, calls)
            initial, final = [], []
            stats = {}
            for mode in MODES:
                prefix = f'sat-stack-452/{case}/{mode}/'
                before = z.read(prefix + 'checkpoint-0.json')
                after = z.read(prefix + 'checkpoint-1.json')
                log = z.read(prefix + 'full.log').decode()
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
                assert hashlib.sha256(before).hexdigest() == record['sourceSha256']
                assert hashlib.sha256(after).hexdigest() == record['finalSha256']
                assert len([x for x in log.splitlines() if x.startswith('WINDOW ')]) == 1
                assert len([x for x in log.splitlines() if x.startswith('PR57_AB_CHECKPOINT ')]) == 1
                assert f'WINDOW 1 seconds={2*calls} ' in log
                line = next(x for x in log.splitlines() if x.startswith('WINDOW '))
                values = {}
                for key in ('wall', 'cpu', 'allocMB'):
                    match = re.search(r'(?:^| )' + key + r'=([-0-9.]+)', line)
                    assert match, (case, mode, key)
                    values[key] = float(match.group(1))
                    assert values[key] == record['stats'][mode][key]
                stats[mode] = values
                initial.append(before)
                final.append(after)
            assert len(set(initial)) == len(set(final)) == 1
            means = {arm: {k: statistics.mean(stats[m][k] for m in modes)
                           for k in ('wall', 'cpu', 'allocMB')}
                     for arm, modes in (('A', ('A1', 'A2')), ('B', ('B1', 'B2')))}
            for arm in 'AB':
                for key in ('wall', 'cpu', 'allocMB'):
                    assert means[arm][key] == record['means'][arm][key]
                    assert round(means['A'][key]/means['B'][key], 5) == record['ratiosAOverB'][key]
            result[case] = {'callsAdvance2PerArm': calls, 'fullJsonParity': True,
                            'means': means, 'ratiosAOverB': record['ratiosAOverB'],
                            'finalSha256': record['finalSha256']}
        print('PR57_453_AUDIT_PASS ' + json.dumps({'zipSha256': ZIP_SHA, 'cases': result}, sort_keys=True))

if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python3 pr57-453-audit.py ORIGINAL_452.zip')
    audit(Path(sys.argv[1]))
