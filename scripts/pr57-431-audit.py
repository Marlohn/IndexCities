#!/usr/bin/env python3
"""Independent offline audit of PR57 #430 original Actions artifact.

Usage: python3 scripts/pr57-431-audit.py original-430.zip
No network, no replay, no claim of optimization or causal closure.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP = '3c2c23881993fc87426b309e41099803e80d65d9fee905201ddf4c869a10a068'
EXPECTED_SAVE = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'

def rows(lines, prefix):
    return [dict(kv.split('=', 1) for kv in line.split()[1:] if '=' in kv)
            for line in lines if line.startswith(prefix + ' ')]

def main(path):
    data = Path(path).read_bytes()
    assert hashlib.sha256(data).hexdigest() == EXPECTED_ZIP, 'artifact ZIP SHA mismatch'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'CRC mismatch'
        assert set(z.namelist()) == {
            'base/full.log', 'shadow/full.log', 'base/checkpoint-10800.json',
            'shadow/checkpoint-10800.json', 'checkpoints.sha256', 'summary.json'
        }
        logs = {arm: z.read(f'{arm}/full.log').decode().splitlines()
                for arm in ('base', 'shadow')}
        saves = {arm: z.read(f'{arm}/checkpoint-10800.json')
                 for arm in ('base', 'shadow')}
        assert saves['base'] == saves['shadow']
        assert len(saves['base']) == 2254838
        assert hashlib.sha256(saves['base']).hexdigest() == EXPECTED_SAVE
        summary = json.loads(z.read('summary.json'))
        assert summary['saveSha256'] == EXPECTED_SAVE
        assert summary['advance2PerArm'] == 10800
        assert summary['optimizationGainAccepted'] is False
        signatures = []
        for arm, lines in logs.items():
            hours = rows(lines, 'PR57_337_HOUR')
            boundaries = rows(lines, 'PR57_ADVANCE_BOUNDARY')
            final = rows(lines, 'PR57_337_FINAL')
            assert len(hours) == 6 and len(boundaries) == 108 and len(final) == 1
            assert [int(x['calls']) for x in boundaries] == list(range(100, 10801, 100))
            assert all(x['quantum'] == '2' for x in boundaries)
            assert all(int(x['living']) == 121 for x in hours)
            assert final[0]['parity'] == 'IDENTICAL'
            signatures.append([tuple(h[k] for k in
                ('hour','slices','actors','claimChanges','spaceCountChanges','living','ledger','history'))
                for h in hours])
            assert abs(sum(float(h['wallMs']) for h in hours) - summary['arms'][arm]['wallMs']) < .002
            assert sum(int(h['totalAllocatedBytes']) for h in hours) == summary['arms'][arm]['allocatedBytes']
        assert signatures[0] == signatures[1], 'intermediate economy/history mismatch'
        assert not rows(logs['base'], 'PR57_430_SUMMARY')
        assert not rows(logs['base'], 'PR57_420_SUMMARY')
        cohort = rows(logs['shadow'], 'PR57_420_COHORT')
        persistence = rows(logs['shadow'], 'PR57_420_SUMMARY')
        sample = rows(logs['shadow'], 'PR57_430_SUMMARY')
        stages = rows(logs['shadow'], 'PR57_430_STAGE')
        assert len(cohort) == 4 and len(persistence) == len(sample) == 1
        assert sum(int(x['actors']) for x in cohort) == 98 * 600
        assert int(cohort[1]['actors']) == 16662
        assert int(persistence[0]['physical']) == 600
        assert int(persistence[0]['firstScans']) == 15874
        assert len(stages) == 5 and {int(s['stage']) for s in stages} == {1, 2, 4, 5, 9}
        s = sample[0]
        assert int(s['sampledPhysical']) == 60 and int(s['sampledActors']) == 1669
        assert s['noDecisionReuse'] == 'True' and s['causalClosure'] == 'False'
        counts = {int(x['stage']): int(x['candidates']) for x in stages}
        ticks = {int(x['stage']): int(x['ticks']) for x in stages}
        assert sum(counts.values()) == int(s['laneCalls']) * 98 == 147490
        assert sum(ticks.values()) <= int(s['laneTicks'])
        total = int(s['actorTicks'])
        lane = int(s['laneTicks'])
        bend = int(s['bendTicks'])
        assert 0 < lane + bend < total
        freq = int(s['frequency'])
        result = {
            'result': 'PASS', 'artifactSha256': EXPECTED_ZIP,
            'saveSha256': EXPECTED_SAVE, 'physicalSlicesSampled': 60,
            'waitingActorsSampled': int(s['sampledActors']),
            'laneScans': int(s['laneCalls']), 'laneCandidates': sum(counts.values()),
            'candidateBuckets': counts,
            'actorMs': total * 1000 / freq,
            'laneMs': lane * 1000 / freq,
            'bendMs': bend * 1000 / freq,
            'otherMs': (total-lane-bend) * 1000 / freq,
            'laneFractionOfSampledActor': lane / total,
            'bendFractionOfSampledActor': bend / total,
            'otherFractionOfSampledActor': (total-lane-bend) / total,
            'stage2FractionOfLane': ticks[2] / lane,
            'stage2FractionOfActor': ticks[2] / total,
            'controlWallMs': summary['arms']['base']['wallMs'],
            'probeWallMs': summary['arms']['shadow']['wallMs'],
            'interpretation': 'intrusive nested timings; not causal closure or an A/B optimization'
        }
        print(json.dumps(result, sort_keys=True, indent=2))

if __name__ == '__main__':
    assert len(sys.argv) == 2, 'usage: pr57-431-audit.py original-430.zip'
    main(sys.argv[1])
