#!/usr/bin/env python3
"""PR57 #520: authentic regime-dependent entry-event scheduler ceiling.

Usage: python3 pr57-520-entry-scheduler-ceiling.py pr57-493-original.zip
This is an offline audit of intrusive .NET profiles, not a speedup or a new CI.
It never assumes an unsplit vehicle is stationary or omits a physical Step.
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

ZIP_SHA = 'f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
CASES = {'congestedREAL121': (100, (121, 121), (98, 98), 'REAL congestionada'),
         'worksREAL47': (100, (47, 47), (42, 42), 'REAL 68 obras'),
         'GRID_ARTIFICIAL': (50, (0, 108), (12, 83), 'GRID artificial pequeno; 34 cidadãos no mapa ao final')}
ARMS = ('A1', 'B1', 'B2', 'A2')

def line(log, prefix):
    found = [s.strip() for s in log.splitlines() if s.strip().startswith(prefix)]
    assert len(found) == 1, (prefix, len(found))
    return found[0]

def numeric(s, key, kind=float):
    m = re.search(r'(?<!\w)' + re.escape(key) + r'=([0-9.]+)', s)
    assert m, (key, s)
    return kind(m.group(1))

def audit(path):
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 37
        summary = json.loads(z.read('summary.json'))
        assert summary, 'missing summary'
        for case, (steps, alive, fleet, label) in CASES.items():
            sha = [set(), set()]
            rows = []
            for arm in ARMS:
                checkpoints = []
                for i in (0, 1):
                    raw = z.read(f'{case}/{arm}/checkpoint-{i}.json')
                    sha[i].add(hashlib.sha256(raw).hexdigest())
                    checkpoints.append(json.loads(raw))
                before, after = checkpoints
                assert (len(before['Vehicles']), len(after['Vehicles'])) == fleet
                assert tuple(sum(c['Alive'] for c in state['Citizens']) for state in checkpoints) == alive, case
                if case == 'GRID_ARTIFICIAL':
                    assert sum(c['InsideMap'] for c in after['Citizens']) == 34
                assert 'INVARIANTS OK' in (log := z.read(f'{case}/{arm}/full.log').decode())
                profile = line(log, 'STEP_PROFILE steps=')
                vp = line(log, 'VEHICLE_PROFILE steps=')
                movement = line(log, 'VEHICLE_DETAIL name=movement ')
                events = line(log, 'VEHICLE_DETAIL name=entry-events ')
                vehicle = line(log, 'STEP_PHASE name=vehicles ')
                assert numeric(profile, 'steps', int) == steps
                assert numeric(vp, 'steps', int) == steps
                batched = numeric(vp, 'batched', int)
                unsplit = numeric(vp, 'unsplit', int)
                slices = numeric(vp, 'slices', int)
                assert batched + unsplit == steps
                assert (batched == 0) == (slices == 0)
                total = numeric(profile, 'measuredWall')
                vehicle_s = numeric(vehicle, 'wall')
                movement_s = numeric(movement, 'wall')
                event_s = numeric(events, 'wall')
                assert 0 <= event_s <= vehicle_s <= total
                assert 0 <= movement_s <= vehicle_s
                # The observed timings are rounded to milliseconds in the logs.
                # Removing all entry-event time is an impossible optimistic oracle.
                upper = total / (total - event_s) if total > event_s else float('inf')
                rows.append((batched, unsplit, slices, total, vehicle_s, movement_s, event_s, upper))
            assert len(sha[0]) == len(sha[1]) == 1, (case, 'ABBA save mismatch')
            assert len({r[:3] for r in rows}) == 1, (case, 'different physical slices')
            print(json.dumps({
                'case': label, 'Advance2_per_arm': steps, 'ABBA_full_saves_equal': True,
                'batched_steps': rows[0][0], 'unsplit_steps': rows[0][1],
                'physical_subslices': rows[0][2],
                'vehicle_wall_s_range': [min(r[4] for r in rows), max(r[4] for r in rows)],
                'movement_wall_s_range': [min(r[5] for r in rows), max(r[5] for r in rows)],
                'entry_events_wall_s_range': [min(r[6] for r in rows), max(r[6] for r in rows)],
                'entry_events_fraction_of_total_pct_range': [round(min(r[6]/r[3] for r in rows)*100, 3), round(max(r[6]/r[3] for r in rows)*100, 3)],
                'oracle_entry_events_free_speedup_range': [round(min(r[7] for r in rows), 5), round(max(r[7] for r in rows), 5)],
                'scope': 'intrusive phase profiles; not an A/B or universal bound; GRID is artificial',
            }, sort_keys=True, ensure_ascii=False))
    print('PR57_520_AUTHENTIC_ENTRY_SCHEDULER_CEILING_PASS')

if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit('usage: pr57-520-entry-scheduler-ceiling.py pr57-493-original.zip')
    audit(sys.argv[1])
