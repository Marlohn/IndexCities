#!/usr/bin/env python3
"""PR57 #495: audit managed allocation pressure from the authentic #493 ZIP.

Observational arithmetic only. No engine mutation, no new benchmark and no
extrapolation to 10k citizens. Source: Actions run 41150158318 artifact 13022328951.
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP = 'f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
# 80 years at 365.25 days/year, 6 real hours, one Advance(2) per quantum.
TARGET_QUANTA_PER_SECOND = 80 * 365.25 * 86400 / (6 * 3600 * 2)
CASES = {'congestedREAL121': (100, 121, 98),
         'worksREAL47': (100, 47, 42),
         'GRID_ARTIFICIAL': (50, None, None)}
ARMS = ('A1', 'B1', 'B2', 'A2')

def fields(line):
    return dict(re.findall(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)', line))

def unique(lines, prefix):
    found = [line.strip() for line in lines if line.strip().startswith(prefix)]
    assert len(found) == 1, (prefix, len(found))
    return fields(found[0])

def audit(path):
    raw = Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == EXPECTED_ZIP, 'original ZIP SHA-256'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'CRC'
        names = set(z.namelist())
        expected = {'summary.json'} | {
            f'{case}/{arm}/{name}'
            for case in CASES for arm in ARMS
            for name in ('checkpoint-0.json', 'checkpoint-1.json', 'full.log')}
        assert names == expected and len(names) == 37, 'artifact members'
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 493
        assert summary['observational'] and not summary['gainAccepted']
        assert summary['cadence'] == 'one public Advance(2) per quantum'
        assert set(summary['cases']) == set(CASES)
        out = {}
        for case, (steps, alive, vehicles) in CASES.items():
            allocs = []
            gc0s = []
            initials, finals = [], []
            movement = []
            profiled = []
            physical = []
            for arm in ARMS:
                root = f'{case}/{arm}/'
                start = z.read(root+'checkpoint-0.json')
                end = z.read(root+'checkpoint-1.json')
                initials.append(start)
                finals.append(end)
                metadata = summary['cases'][case]['arms'][arm]
                assert hashlib.sha256(start).hexdigest() == metadata['initialSha256']
                assert hashlib.sha256(end).hexdigest() == metadata['finalSha256']
                assert len(end) == metadata['finalBytes']
                if alive is not None:
                    state = json.loads(start)
                    assert sum(c['Alive'] for c in state['Citizens']) == alive
                    assert len(state['Vehicles']) == vehicles
                lines = z.read(root+'full.log').decode().splitlines()
                assert 'INVARIANTS OK' in lines
                assert any(line.startswith('SAVE ') for line in lines)
                w = unique(lines, 'WINDOW 1 ')
                p = unique(lines, 'STEP_PROFILE ')
                v = unique(lines, 'VEHICLE_PROFILE ')
                phase = unique(lines, 'STEP_PHASE name=vehicles ')
                assert int(w['seconds']) == steps * 2
                assert int(p['steps']) == steps and int(p['phases']) == 13
                assert int(v['steps']) == steps
                assert metadata['window'] in lines
                assert summary['cases'][case]['fullJsonParity']
                allocs.append(float(w['allocMB']))
                gc0s.append(int(w['gc0']))
                movement.append(float(phase['wall'].rstrip('s')))
                profiled.append(float(p['measuredWall'].rstrip('s')))
                physical.append((int(v['batched']), int(v['unsplit']), int(v['slices'])))
                if case == 'congestedREAL121':
                    alloc = unique(lines, 'PR57_ALLOC_PROFILE calls=600 ')
                    regions = unique(lines, 'PR57_ALLOC_REGIONS calls=600 ')
                    assert int(alloc['calls']) == int(regions['calls']) == 600
                    assert abs(float(alloc['preLaneMB']) + float(alloc['postTurnMB'])
                               - 170.17) < .02
                    assert abs(float(regions['entryMB']) + float(regions['junctionMB'])
                               - 120.85) < .02
                    assert float(alloc['totalMB']) <= float(w['allocMB'])
                    assert physical[-1] == (100, 0, 618)
                    if arm == 'A1':
                        measured = (float(alloc['totalMB']), float(regions['entryMB']),
                                    float(regions['junctionMB']))
                    else:
                        assert measured == (float(alloc['totalMB']), float(regions['entryMB']),
                                            float(regions['junctionMB']))
            assert all(x == initials[0] for x in initials), f'{case}: initial A/B mismatch'
            assert all(x == finals[0] for x in finals), f'{case}: final A/B mismatch'
            assert max(allocs) == min(allocs), f'{case}: rounded allocation A/B mismatch'
            assert max(gc0s) == min(gc0s), f'{case}: gen0 A/B mismatch'
            assert len(set(physical)) == 1, f'{case}: physical counts mismatch'
            out[case] = {'steps': steps, 'allocatedMB': allocs[0], 'gc0': gc0s[0],
                         'physical': physical[0], 'vehicleWallRange': (min(movement), max(movement)),
                         'profiledControl': (profiled[0], movement[0])}
    print('PR57_495_AUTHENTIC_ALLOCATION_BUDGET_PASS')
    print(f'zipSha256={EXPECTED_ZIP} cases=3 arms=12 checkpoints=24 CRC=OK')
    print(f'targetQuantaPerRealSecond={TARGET_QUANTA_PER_SECOND:.6f} '
          f'targetMicrosecondsPerQuantum={1e6/TARGET_QUANTA_PER_SECOND:.6f}')
    for case, row in out.items():
        per_step = row['allocatedMB'] / row['steps']
        # MB means decimal 1e6 bytes in Headless Program.cs, not MiB.
        required_gb_per_sec = per_step * TARGET_QUANTA_PER_SECOND / 1000
        b, u, s = row['physical']
        profile_seconds, vehicle_seconds = row['profiledControl']
        assert profile_seconds >= vehicle_seconds
        free_vehicle_residual_us = (profile_seconds - vehicle_seconds) * 1e6 / row['steps']
        print(f'{case} steps={row["steps"]} allocMB={row["allocatedMB"]:.1f} '
              f'allocMBPerQuantum={per_step:.3f} '
              f'conditionalGBPerSecondAtTarget={required_gb_per_sec:.2f} '
              f'gen0={row["gc0"]} batched={b} unsplit={u} slices={s} '
              f'freeVehicleResidualUsPerQuantum={free_vehicle_residual_us:.1f} '
              f'freeVehicleResidualVsTarget={free_vehicle_residual_us/(1e6/TARGET_QUANTA_PER_SECOND):.1f}x')
    total, entry, junction = measured
    real = out['congestedREAL121']['allocatedMB']
    print(f'congestedREAL121 first600MoveUnsplitCallsAllocatedMB={total:.2f} '
          f'fractionOfWindowAllocation={total/real*100:.2f}% '
          f'entryPlusJunctionMB={entry+junction:.2f} '
          f'fractionOfWindowAllocation={(entry+junction)/real*100:.2f}% '
          f'fractionOfMeasuredMoveUnsplit={(entry+junction)/total*100:.2f}%')
    print(f'congestedVsSmallGRIDAllocationPerQuantum='
          f'{(real/100)/(out["GRID_ARTIFICIAL"]["allocatedMB"]/50):.2f}x '
          'GRID_ARTIFICIAL_NOT_1111_POPULATION')
    print('NOTE: allocation numbers are rounded, intrusive, state-specific; '
          'conditional rates are NOT forecasts, lower bounds or measured bandwidth.')

if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python3 pr57-495-allocation-budget.py pr57-493-original.zip')
    audit(sys.argv[1])
