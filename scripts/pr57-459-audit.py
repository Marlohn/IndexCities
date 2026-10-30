#!/usr/bin/env python3
"""PR57 #459: independently audit the ORIGINAL #458 GitHub Actions ZIP.

Run: python3 scripts/pr57-459-audit.py /path/to/original.zip
No engine execution, no synthetic states, no external Python dependencies.
"""
import hashlib
import json
import re
import sys
import zipfile
from collections import defaultdict
from pathlib import Path

EXPECTED_ZIP_SHA256 = '8a96faf4676e6497285be21db845be60c2b48db537f073b3f3bd9888655658c8'
CASES = {
    'economyREAL14h': (1800, 121, 98, 'bf05b6ed3650bdc91fedf5a9c0e66c6c13b7e6de19f04a250a1b561a9c177c1f'),
    'worksREAL': (100, 47, 42, '686ed27e0cc458d3d8f411a1e308a2dc98e87bf588c12d5dcf1ea787bad1c883'),
    'gridARTIFICIAL': (50, 1111, 787, 'f8999ff02c18c7c818ef419e30f63afdcffcbe8a97a171a677bf4ac20f2cfd95'),
}
ARMS = ('A1', 'B1', 'B2', 'A2')

def sha(data):
    return hashlib.sha256(data).hexdigest()

def audit(path):
    raw = Path(path).read_bytes()
    assert sha(raw) == EXPECTED_ZIP_SHA256, ('ZIP_SHA256', sha(raw))
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'CRC_FAIL'
        names = set(z.namelist())
        assert len(names) == 38, ('ZIP_MEMBERS', len(names))
        summary = json.loads(z.read('prepared-458/summary.json'))
        assert summary['experiment'] == 458 and summary['allAdvance2'] is True
        assert set(summary['cases']) == set(CASES)
        for case, (calls, alive, vehicles, expected_final) in CASES.items():
            reported = summary['cases'][case]
            assert reported['callsAdvance2PerArm'] == calls
            assert reported['living'] == alive and reported['vehicles'] == vehicles
            assert reported['fullJsonParity'] is True
            assert reported['finalSha256'] == expected_final
            snapshots = defaultdict(dict)
            for arm in ARMS:
                prefix = f'prepared-458/{case}/{arm}/'
                log = z.read(prefix + 'full.log').decode('utf-8')
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
                if arm.startswith('A'):
                    assert 'PR57_458_FAST_USED' not in log, (case, arm, 'fast gate')
                if case == 'economyREAL14h':
                    assert ('PR57_458_FAST_USED' in log) == arm.startswith('B')
                rows = [x for x in log.splitlines() if x.startswith('WINDOW ')]
                assert len(rows) == 1, (case, arm, 'WINDOW_COUNT')
                row = rows[0]
                assert re.search(rf'\bseconds={calls*2}\b', row), (case, arm, row)
                assert re.search(rf'\balive={alive}\b', row), (case, arm, row)
                assert re.search(rf'\bvehicles={vehicles}\b', row), (case, arm, row)
                for field in ('wall', 'cpu', 'allocMB'):
                    match = re.search(r'\b' + field + r'=([0-9.]+)', row)
                    assert match, (case, arm, field)
                    assert float(match.group(1)) == reported['stats'][arm][field], (case, arm, field)
                files = sorted(n for n in names if n.startswith(prefix + 'checkpoint-') and n.endswith('.json'))
                assert len(files) == reported['checkpointsPerArm'] == 2, (case, arm, 'CHECKPOINTS')
                for name in files:
                    key = name.rsplit('/', 1)[-1]
                    snapshots[key][arm] = z.read(name)
                assert sha(snapshots['checkpoint-0.json'][arm]) == reported['inputSha256']
                assert sha(snapshots['checkpoint-1.json'][arm]) == expected_final
            for checkpoint, by_arm in snapshots.items():
                assert set(by_arm) == set(ARMS), (case, checkpoint, 'ARMS')
                assert len(set(by_arm.values())) == 1, (case, checkpoint, 'BYTE_PARITY')
            print(f'PR57_459_CASE_PASS case={case} Advance2PerArm={calls} '
                  f'checkpointsPerArm={len(snapshots)} finalSha256={expected_final} '
                  f'wallAOverB={reported["ratiosAOverB"]["wall"]:.5f} '
                  f'cpuAOverB={reported["ratiosAOverB"]["cpu"]:.5f}')
    print('PR57_459_AUDIT_PASS zipSha256=' + EXPECTED_ZIP_SHA256)

if __name__ == '__main__':
    assert len(sys.argv) == 2, 'usage: python3 scripts/pr57-459-audit.py ORIGINAL_ZIP'
    audit(sys.argv[1])
