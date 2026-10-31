#!/usr/bin/env python3
"""Independent PR57 #415 audit of original #414 Actions artifact.

Usage: python3 scripts/pr57-415-audit.py pr57-414-original.zip
Scope: REAL 121 living / 98 vehicles, first 600 physical slices, net boundaries.
No causal closure or optimization claim.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ZIP_SHA = 'b0c12b2903900413ab86470fd71d03af6aca0b2d39ab5714e2ffd697f01c0398'
SAVE_SHA = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'
FILES = {'base/full.log','probe/full.log','base/checkpoint-10800.json',
         'probe/checkpoint-10800.json','checkpoints.sha256','summary.json'}

def rows(log, prefix):
    return [dict(token.split('=',1) for token in line.split()[1:] if '=' in token)
            for line in log.splitlines() if line.startswith(prefix+' ')]

def main(path):
    raw = Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == FILES
        base = z.read('base/checkpoint-10800.json')
        probe = z.read('probe/checkpoint-10800.json')
        assert base == probe and len(base) == 2254838
        assert hashlib.sha256(base).hexdigest() == SAVE_SHA
        summary = json.loads(z.read('summary.json'))
        assert summary['scope'] == 'AUTHENTIC_REAL_121_LIVING_98_VEHICLES_6H'
        assert summary['advance2PerArm'] == 10800
        for arm in ('base','probe'):
            log = z.read(f'{arm}/full.log').decode()
            hours = rows(log,'PR57_337_HOUR')
            boundaries = rows(log,'PR57_ADVANCE_BOUNDARY')
            finals = rows(log,'PR57_337_FINAL')
            p408 = rows(log,'PR57_408_SUMMARY')
            p410 = rows(log,'PR57_410_SUMMARY')
            p412 = rows(log,'PR57_412_SUMMARY')
            p414 = rows(log,'PR57_414_SUMMARY')
            witnesses = rows(log,'PR57_414_WITNESS')
            assert len(hours) == 6 and len(boundaries) == 108 and len(finals) == 1
            assert int(boundaries[-1]['calls']) == 10800
            assert all(int(h['living']) == 121 for h in hours)
            assert hours[-1]['ledger'] == '4085' and hours[-1]['history'] == '7430'
            assert finals[0]['parity'] == 'IDENTICAL'
            assert finals[0]['sha256'].lower() == SAVE_SHA
            packaged = summary['arms'][arm]
            assert packaged['final'] == finals[0]
            assert abs(sum(float(h['wallMs']) for h in hours)-packaged['wallMs']) < .0011
            assert sum(int(h['totalAllocatedBytes']) for h in hours) == packaged['allocatedBytes']
            if arm == 'probe':
                assert len(p408) == len(p410) == len(p412) == len(p414) == 1
                p = p414[0]
                assert p == packaged['census'] and witnesses == packaged['censusWitnesses']
                assert int(p['physical']) == 600
                assert int(p['actors']) == 58800
                assert int(p['checks']) == 58800*97 == 5703600
                assert all(int(p[k]) == 0 for k in ('crossScalar','crossExtra',
                    'crossEither','distinctPerSlice','postSyncScalar','postSyncExtra','witnesses'))
                assert len(witnesses) == 0
                assert int(p412[0]['candidateReads']) == 1592990
                assert int(p412[0]['ordinaryExact']) == 11045
                assert int(p412[0]['staleCross']) == 0
                assert int(p412[0]['staleSelf']) == 446
                assert p['observational'] == 'True' and p['causalClosure'] == 'False'
                assert p['netBoundaryOnly'] == 'True' and p['routeHashWitnessOnly'] == 'True'
                print('CENSUS',json.dumps(p,sort_keys=True))
            else:
                assert not (p408 or p410 or p412 or p414 or witnesses)
            print('ARM',arm,packaged['wallMs'],packaged['cpuMs'],packaged['allocatedBytes'])
    print('PR57_415_AUDIT PASS',ZIP_SHA,SAVE_SHA)

if __name__ == '__main__':
    main(sys.argv[1])
