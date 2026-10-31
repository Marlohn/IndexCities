#!/usr/bin/env python3
"""PR57 #521: bounded movement residual after removing both measured leader scans.

Usage: python3 scripts/pr57-521-movement-residual.py pr57-493-original.zip
Offline analysis of intrusive profiles from 12 independent .NET processes.
No skipped actor, simulated speedup, code change, or inference about 10k SIMs.
"""
import hashlib
import json
import re
import statistics
import sys
import zipfile
from pathlib import Path

ZIP_SHA = 'f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
ARMS = ('A1', 'B1', 'B2', 'A2')
CASES = {
    'congestedREAL121': (100, 121, 98, 'REAL congestionada'),
    'worksREAL47': (100, 47, 42, 'REAL 68 obras'),
    'GRID_ARTIFICIAL': (50, 108, 83, 'GRID artificial pequeno'),
}
TARGET_US_PER_STEP = (80 * 365.25 * 86400 / 2 / (6 * 3600)) ** -1 * 1e6


def get_line(lines, prefix):
    rows = [s.strip() for s in lines if s.strip().startswith(prefix)]
    assert len(rows) == 1, (prefix, len(rows))
    return rows[0]


def val(row, key, cast=float):
    match = re.search(r'(?<!\\w)' + re.escape(key) + r'=([0-9.]+)', row)
    assert match, (key, row)
    return cast(match.group(1))


def audit(path):
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == ZIP_SHA, 'original ZIP SHA'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 37, 'ZIP CRC/members'
        assert json.loads(z.read('summary.json'))['experiment'] == 493
        for case, (steps, alive, fleet, label) in CASES.items():
            initials, finals, arms = set(), set(), []
            for arm in ARMS:
                before = z.read(f'{case}/{arm}/checkpoint-0.json')
                after = z.read(f'{case}/{arm}/checkpoint-1.json')
                initials.add(hashlib.sha256(before).hexdigest())
                finals.add(hashlib.sha256(after).hexdigest())
                states = (json.loads(before), json.loads(after))
                assert len(states[1]['Vehicles']) == fleet
                if case != 'GRID_ARTIFICIAL':
                    assert all(sum(c['Alive'] for c in s['Citizens']) == alive for s in states)
                else:
                    assert sum(c['Alive'] for c in states[1]['Citizens']) == alive
                lines = z.read(f'{case}/{arm}/full.log').decode().splitlines()
                assert 'INVARIANTS OK' in lines
                total = val(get_line(lines, 'STEP_PROFILE steps='), 'measuredWall')
                assert val(get_line(lines, 'STEP_PROFILE steps='), 'steps', int) == steps
                vehicle = val(get_line(lines, 'STEP_PHASE name=vehicles '), 'wall')
                movement = val(get_line(lines, 'VEHICLE_DETAIL name=movement '), 'wall')
                lane = get_line(lines, 'VEHICLE_SCAN name=lane-leader-scan ')
                bend = get_line(lines, 'VEHICLE_SCAN name=bend-leader-scan ')
                lane_wall, bend_wall = val(lane, 'wall'), val(bend, 'wall')
                lane_visits, bend_visits = val(lane, 'visits', int), val(bend, 'visits', int)
                assert lane_visits == bend_visits
                batch = get_line(lines, 'VEHICLE_PROFILE steps=')
                assert val(batch, 'steps', int) == steps
                batched, unsplit, slices = (val(batch, k, int) for k in ('batched', 'unsplit', 'slices'))
                assert batched + unsplit == steps
                assert 0 <= lane_wall + bend_wall < movement <= vehicle <= total
                scan_free = total - lane_wall - bend_wall
                movement_non_scan = movement - lane_wall - bend_wall
                arms.append(dict(total=total, vehicle=vehicle, movement=movement,
                                 lane=lane_wall, bend=bend_wall, scans=lane_wall + bend_wall,
                                 non_scan=movement_non_scan, scan_free=scan_free,
                                 speedup=total / scan_free, visits=lane_visits,
                                 batched=batched, unsplit=unsplit, slices=slices))
            assert len(initials) == len(finals) == 1, (case, 'full A/B saves mismatch')
            assert len({(r['visits'], r['batched'], r['unsplit'], r['slices']) for r in arms}) == 1
            def bounds(key, factor=1, digits=3):
                return [round(min(r[key] for r in arms)*factor, digits),
                        round(max(r[key] for r in arms)*factor, digits)]
            row = arms[0]
            print(json.dumps(dict(
                case=label, steps_per_arm=steps, alive_final=alive, vehicles_final=fleet,
                ABBA_full_initial_and_final_save_parity=True,
                batched_steps=row['batched'], unsplit_steps=row['unsplit'],
                entry_subslices=row['slices'],
                lane_and_bend_candidate_visits_each=row['visits'],
                movement_calls_without_operations=row['unsplit']+row['slices'],
                candidate_visits_per_movement_call=round(row['visits']/(row['unsplit']+row['slices']), 1),
                lane_plus_bend_scan_wall_ms_range=bounds('scans', 1000),
                movement_non_scan_wall_ms_range=bounds('non_scan', 1000),
                movement_non_scan_us_per_Advance2_range=bounds('non_scan', 1e6/steps, 1),
                scan_share_of_movement_pct_range=[round(min(r['scans']/r['movement'] for r in arms)*100, 2),
                                                  round(max(r['scans']/r['movement'] for r in arms)*100, 2)],
                oracle_both_scans_free_integrated_speedup_range=bounds('speedup', 1, 4),
                oracle_both_scans_free_total_us_per_Advance2_range=bounds('scan_free', 1e6/steps, 1),
                target_us_per_Advance2=round(TARGET_US_PER_STEP, 4),
                scope='INTRUSIVE profiles, rounded milliseconds; impossible zero-cost scans, not actual gain; no 10k extrapolation',
            ), ensure_ascii=False, sort_keys=True))
    print('PR57_521_AUTHENTIC_MOVEMENT_RESIDUAL_PASS')

if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    audit(sys.argv[1])
