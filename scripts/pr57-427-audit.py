#!/usr/bin/env python3
"""Independent offline audit of original PR57 #426 Actions ZIP (no third-party deps).

Usage: python3 scripts/pr57-427-audit.py original-426.zip
"""
import hashlib
import json
import sys
import zipfile

ZIP_SHA = '2ed69cda3e304d8380b71226290f0ba87faa37513506041044d5fad738f065eb'
SAVE_SHA = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'
FILES = {'base/checkpoint-10800.json', 'base/full.log', 'checkpoints.sha256',
         'shadow/checkpoint-10800.json', 'shadow/full.log', 'summary.json'}


def rows(lines, prefix):
    return [dict(x.split('=', 1) for x in line.split()[1:] if '=' in x)
            for line in lines if line.startswith(prefix + ' ')]


def main(path):
    assert hashlib.sha256(open(path, 'rb').read()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == FILES
        j = json.loads(z.read('summary.json'))
        assert j['advance2PerArm'] == 10800
        assert j['observational'] and not j['causalClosure'] and not j['optimizationGainAccepted']
        signatures, saves, arms = [], [], {}
        for arm in ('base', 'shadow'):
            lines = z.read(f'{arm}/full.log').decode().splitlines()
            hours, bounds, final = (rows(lines, x) for x in
                ('PR57_337_HOUR', 'PR57_ADVANCE_BOUNDARY', 'PR57_337_FINAL'))
            p, s = rows(lines, 'PR57_420_SUMMARY'), rows(lines, 'PR57_426_SUMMARY')
            assert len(hours) == 6 and len(bounds) == 108 and len(final) == 1
            assert [int(b['calls']) for b in bounds] == list(range(100, 10801, 100))
            assert all(b['quantum'] == '2' for b in bounds)
            assert all(int(h['living']) == 121 for h in hours)
            assert final[0]['parity'] == 'IDENTICAL'
            signatures.append([tuple(h[k] for k in
                ('hour', 'slices', 'actors', 'claimChanges', 'spaceCountChanges',
                 'living', 'ledger', 'history')) for h in hours])
            saves.append(z.read(f'{arm}/checkpoint-10800.json'))
            arms[arm] = {
                'wallMs': round(sum(float(h['wallMs']) for h in hours), 3),
                'cpuMs': sum(float(h['cpuMs']) for h in hours),
                'allocatedBytes': sum(int(h['totalAllocatedBytes']) for h in hours)}
            assert arms[arm] == {k: j['arms'][arm][k] for k in arms[arm]}
            if arm == 'base':
                assert not p and not s
            else:
                assert len(p) == len(s) == 1
                p, s = p[0], s[0]
                assert s['observational'] == 'True' and s['causalClosure'] == 'False'
                assert s['noDecisionReuse'] == 'True'
                assert int(s['physical']) == int(p['physical']) == 600
                assert int(s['first']) == int(p['firstScans']) == 15874
                assert int(s['pairs']) == int(p['consecutive']) == 15843
                assert int(s['stableReader']) == int(p['sameReader']) == 15000
                assert int(s['changedStep']) == int(p['sameReader']) - int(p['sameReaderDecision']) == 1240
                assert int(s['fullLists']) == int(s['first'])
                assert int(s['sameIds']) == int(s['pairs'])
                assert int(s['readerIds']) == int(s['stableReader'])
                assert int(s['readerScalars']) == int(s['sameScalars']) == 8037
                assert int(s['readerHolds']) == 14998
                assert int(s['changedIds']) == int(s['changedHolds']) == 1240
                assert int(s['changedScalars']) == 495
                assert int(s['changedScalarEntries']) == 2442
                assert int(s['reads']) == 1555652
                assert j['arms'][arm]['semantic'] == s
        assert signatures[0] == signatures[1], 'intermediate economy/history/event mismatch'
        assert saves[0] == saves[1] and len(saves[0]) == 2254838
        assert hashlib.sha256(saves[0]).hexdigest() == SAVE_SHA == j['saveSha256']
        print(json.dumps({'audit': 'PASS', 'zipSha256': ZIP_SHA, 'saveSha256': SAVE_SHA,
            'realLiving': 121, 'realVehicles': 98, 'advance2PerArm': 10800,
            'physicalSample': 600, 'candidateReads': 1555652,
            'candidateIdsStable': '15843/15843',
            'stableReaderCandidateScalars': '8037/15000',
            'stableReaderHoldTraces': '14998/15000',
            'dtDrivenStepChanges': 1240,
            'changedStepCandidateScalarsStable': 495,
            'changedStepHoldTracesStable': 1240,
            'intermediateParity': True, 'finalSavesIdentical': True,
            'gainAccepted': False, 'arms': arms}, sort_keys=True, indent=2))


if __name__ == '__main__':
    assert len(sys.argv) == 2, 'usage: pr57-427-audit.py original-426.zip'
    main(sys.argv[1])
