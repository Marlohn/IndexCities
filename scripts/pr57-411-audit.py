#!/usr/bin/env python3
"""#411: independent audit of #410 original GitHub Actions ZIP, raw logs and saves.

Usage: python3 scripts/pr57-411-audit.py pr57-410-original.zip
Observational claim only, not a physics/causality proof.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ZIP_SHA = '14d394b5f4b395f665db81082446c49b93b83431b8de7679ef82c3a5d09fd91d'
SAVE_SHA = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'
FILES = {'base/checkpoint-10800.json', 'probe/checkpoint-10800.json',
         'base/full.log', 'probe/full.log', 'checkpoints.sha256', 'summary.json'}

def rows(log, prefix):
    return [dict(part.split('=', 1) for part in line.split()[1:] if '=' in part)
            for line in log.splitlines() if line.startswith(prefix + ' ')]

def main(path):
    raw = Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == FILES
        a = z.read('base/checkpoint-10800.json')
        b = z.read('probe/checkpoint-10800.json')
        assert a == b and len(a) == 2254838
        assert hashlib.sha256(a).hexdigest() == SAVE_SHA
        packaged = json.loads(z.read('summary.json'))
        for arm in ('base', 'probe'):
            log = z.read(f'{arm}/full.log').decode()
            hours = rows(log, 'PR57_337_HOUR')
            boundaries = rows(log, 'PR57_ADVANCE_BOUNDARY')
            final = rows(log, 'PR57_337_FINAL')
            cells = rows(log, 'PR57_408_CELL')
            holds = rows(log, 'PR57_408_HOLD')
            baseline = rows(log, 'PR57_408_SUMMARY')
            shadow = rows(log, 'PR57_410_SUMMARY')
            witnesses = rows(log, 'PR57_410_WITNESS')
            assert len(hours) == 6 and len(boundaries) == 108 and len(final) == 1
            assert int(boundaries[-1]['calls']) == 10800
            assert all(int(h['living']) == 121 for h in hours)
            assert final[0]['parity'] == 'IDENTICAL' and final[0]['sha256'].lower() == SAVE_SHA
            assert hours[-1]['ledger'] == '4085' and hours[-1]['history'] == '7430'
            assert packaged['arms'][arm]['final'] == final[0]
            assert abs(sum(float(h['wallMs']) for h in hours)-packaged['arms'][arm]['wallMs']) < .0011
            assert sum(int(h['totalAllocatedBytes']) for h in hours) == packaged['arms'][arm]['allocatedBytes']
            if arm == 'base':
                assert not cells and not holds and not baseline and not shadow and not witnesses
                continue
            assert len(cells) == 28 and len(holds) == 7 and len(baseline) == len(shadow) == 1
            old, new = baseline[0], shadow[0]
            assert packaged['arms'][arm]['shadow'] == new
            assert packaged['arms'][arm]['witnesses'] == witnesses
            assert int(old['physical']) == 600
            assert int(old['scans']) == int(new['scans']) == sum(int(c['scans']) for c in cells) == 16255
            assert int(old['candidates']) == int(new['candidates']) == 1592990
            assert int(old['binding']) == sum(int(c['scans']) for c in cells if c['binding']=='1') == 11645
            assert int(new['bindingOrdinary']) == sum(int(c['scans']) for c in cells if c['cause']=='6' and c['binding']=='1') == 11045
            assert int(new['bindingOrdinarySameId']) == int(new['bindingOrdinaryExact']) == 11045
            assert int(new['geometry']) == next(int(h['attempts']) for h in holds if h['cause']=='6') == 70199
            assert int(new['stepMismatch']) == int(new['missedBinding']) == 600
            assert int(new['falseBinding']) == 0
            assert len(witnesses) == int(new['witnesses']) == 16
            assert all(w['cause']=='3' and w['canonicalStep']=='0' and w['scalarId']=='-1' for w in witnesses)
            assert new['observational']=='True' and new['causalClosure']=='False'
    print('PR57_411_AUDIT PASS zip_sha='+ZIP_SHA+' save_sha='+SAVE_SHA+
          ' ordinary_exact=11045/11045 missed_entry=600/600 step_mismatch=600'
          ' scope=REAL_121_98_600_PHYSICAL_SLICES_NOT_CAUSAL_CLOSURE')

if __name__ == '__main__':
    assert len(sys.argv) == 2
    main(sys.argv[1])
