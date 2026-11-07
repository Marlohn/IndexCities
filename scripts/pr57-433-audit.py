#!/usr/bin/env python3
"""Independent offline audit of PR57 #432 original Actions ZIP.

Usage: python3 scripts/pr57-433-audit.py original-432.zip
Read-only: no replay, no network, no optimization/causal-closure claim.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP = '69b822f7a0d769604250b3e365a3ef5920e3f6148027531869b1cef408bb18b5'
EXPECTED_SAVE = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'

def rows(lines, prefix):
    return [dict(kv.split('=', 1) for kv in line.split()[1:] if '=' in kv)
            for line in lines if line.startswith(prefix + ' ')]

def main(path):
    data = Path(path).read_bytes()
    assert hashlib.sha256(data).hexdigest() == EXPECTED_ZIP, 'ZIP SHA mismatch'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'ZIP CRC mismatch'
        assert set(z.namelist()) == {
            'base/full.log', 'shadow/full.log', 'base/checkpoint-10800.json',
            'shadow/checkpoint-10800.json', 'checkpoints.sha256', 'summary.json'}
        logs = {a: z.read(f'{a}/full.log').decode().splitlines()
                for a in ('base', 'shadow')}
        saves = {a: z.read(f'{a}/checkpoint-10800.json')
                 for a in ('base', 'shadow')}
        assert saves['base'] == saves['shadow']
        assert len(saves['base']) == 2254838
        assert hashlib.sha256(saves['base']).hexdigest() == EXPECTED_SAVE
        summary = json.loads(z.read('summary.json'))
        assert summary['advance2PerArm'] == 10800
        assert summary['saveSha256'] == EXPECTED_SAVE
        assert summary['observational'] and not summary['causalClosure']
        assert not summary['optimizationGainAccepted']
        signatures = []
        for arm, lines in logs.items():
            hours = rows(lines, 'PR57_337_HOUR')
            boundaries = rows(lines, 'PR57_ADVANCE_BOUNDARY')
            final = rows(lines, 'PR57_337_FINAL')
            assert len(hours) == 6 and len(boundaries) == 108 and len(final) == 1
            assert [int(b['calls']) for b in boundaries] == list(range(100, 10801, 100))
            assert all(b['quantum'] == '2' for b in boundaries)
            assert all(int(h['living']) == 121 for h in hours)
            assert final[0]['parity'] == 'IDENTICAL'
            signatures.append([tuple(h[k] for k in
                ('hour', 'slices', 'actors', 'claimChanges', 'spaceCountChanges',
                 'living', 'ledger', 'history')) for h in hours])
            assert abs(sum(float(h['wallMs']) for h in hours) -
                       summary['arms'][arm]['wallMs']) < .002
            assert sum(int(h['totalAllocatedBytes']) for h in hours) == \
                   summary['arms'][arm]['allocatedBytes']
        assert signatures[0] == signatures[1], 'intermediate economy/history mismatch'
        assert not rows(logs['base'], 'PR57_420_SUMMARY')
        assert not rows(logs['base'], 'PR57_432_SUMMARY')
        cohort = rows(logs['shadow'], 'PR57_420_COHORT')
        persistence = rows(logs['shadow'], 'PR57_420_SUMMARY')
        samples = rows(logs['shadow'], 'PR57_432_SUMMARY')
        stages = rows(logs['shadow'], 'PR57_432_STAGE')
        assert len(cohort) == 4 and len(persistence) == len(samples) == 1
        assert len(stages) == 11 and {int(s['stage']) for s in stages} == set(range(11))
        assert sum(int(c['actors']) for c in cohort) == 58800
        assert int(cohort[1]['actors']) == 16662
        assert int(persistence[0]['physical']) == 600
        assert int(persistence[0]['firstScans']) == 15874
        sample = samples[0]
        assert int(sample['sampledPhysical']) == 60
        assert int(sample['sampledActors']) == 1669
        assert int(sample['blocked']) == 1225
        assert int(sample['poseEligible']) == 0
        assert int(sample['bypassCalls']) == 1105
        assert int(sample['bypassSuccess']) == 0
        assert int(sample['towWaitCalls']) == 1225
        assert sample['noDecisionReuse'] == 'True'
        assert sample['causalClosure'] == 'False'
        visits = {int(s['stage']): int(s['visits']) for s in stages}
        assert visits[0] == visits[1] == visits[10] == 1669
        assert all(visits[i] == 1505 for i in range(2, 9))
        assert visits[9] == 280
        assert visits[9] + int(sample['blocked']) == visits[2]
        ticks = {int(s['stage']): int(s['ticks']) for s in stages}
        total = sum(ticks.values())
        assert all(t > 0 for t in ticks.values())
        assert total >= int(sample['bypassTicks']) + int(sample['towWaitTicks'])
        assert int(sample['frequency']) == 1000000000
        result = {
            'result': 'PASS', 'artifactSha256': EXPECTED_ZIP,
            'saveSha256': EXPECTED_SAVE, 'physicalSlicesSampled': 60,
            'waitingActorsSampled': 1669, 'scans': 1505, 'blocked': 1225,
            'stageTotalMs': round(total / 1e6, 6),
            'stages': {s['name']: {
                'ms': round(int(s['ticks']) / 1e6, 6),
                'percentOfSampledActor': round(int(s['ticks']) * 100 / total, 3),
                'visits': int(s['visits'])} for s in stages},
            'wallMs': {a: summary['arms'][a]['wallMs'] for a in ('base', 'shadow')},
            'causalClosure': False, 'optimizationGainAccepted': False}
        print(json.dumps(result, sort_keys=True))

if __name__ == '__main__':
    main(sys.argv[1])
