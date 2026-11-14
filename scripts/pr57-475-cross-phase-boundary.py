#!/usr/bin/env python3
"""PR57 #475: read-only cross-phase input invalidation census on authentic #465 saves.

Usage: python3 pr57-475-cross-phase-boundary.py pr57-465-original.zip
No simulated entities are created; this is a saved-state boundary analysis, NOT a
per-phase trace or proof of safe caching. No .NET/Godot engine execution.
"""
import collections
import hashlib
import json
import sys
import zipfile
from datetime import datetime

EXPECTED_SHA = '370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1'
STAGES = [('pre', 'A-pre/checkpoint-1.json', 'A-mid/checkpoint-1.json'),
          ('post', 'A-post/checkpoint-0.json', 'A-post/checkpoint-1.json')]
UTILITY = {'WaterIntake', 'WaterTreatment', 'SewageTreatment', 'WindPower'}
EXTENDED = {'Grocery', 'FuelStation', 'MechanicShop'}


def changed_fields(before, after):
    assert len(before) == len(after)
    b = {x['Id']: x for x in before}
    a = {x['Id']: x for x in after}
    assert b.keys() == a.keys()
    counts = collections.Counter()
    changed = set()
    for key in b:
        fields = set(b[key]) | set(a[key])
        for field in fields:
            if b[key].get(field) != a[key].get(field):
                counts[field] += 1
                changed.add(key)
    return dict(sorted(counts.items(), key=lambda kv: (-kv[1], kv[0]))), len(changed)


def scheduled(c, building, now):
    if not building['Completed'] or not c['Alive'] or not c['Resident']:
        return False
    # C# DateTime.DayOfWeek: Sunday=0, Monday=1, ... Saturday=6.
    day = (now.weekday() + 1) % 7
    off = c['WeeklyDayOff']
    if day == off or day == (off + 1) % 7:
        return False
    hour = now.hour + now.minute / 60 + (now.second + now.microsecond / 1e6) / 3600
    kind = building['Kind']
    shift = c['Shift']
    if kind in UTILITY:
        return (6 <= hour < 14) if shift == 0 else ((14 <= hour < 22) if shift == 1 else (hour >= 22 or hour < 6))
    if kind in EXTENDED:
        return (7 <= hour < 15) if shift == 0 else (14 <= hour < 22)
    return 7 <= hour < 15


def snapshot_summary(s):
    active = {b['Id']: b for b in s['Buildings'] if b['Completed'] and not b['Removed']}
    citizens = [c for c in s['Citizens'] if c['WorkplaceId'] in active]
    now = datetime.fromisoformat(s['Now'])
    sched = {c['Id'] for c in citizens if scheduled(c, active[c['WorkplaceId']], now)}
    return active, sched


def main(path):
    raw = open(path, 'rb').read()
    sha = hashlib.sha256(raw).hexdigest()
    assert sha == EXPECTED_SHA, (sha, EXPECTED_SHA)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        paths = ['A-pre/checkpoint-1.json', 'A-mid/checkpoint-0.json', 'A-mid/checkpoint-1.json',
                 'A-post/checkpoint-0.json', 'A-post/checkpoint-1.json']
        for name in paths:
            assert z.read(name) == z.read(name.replace('A-', 'B-')), f'A/B mismatch: {name}'
        assert z.read(paths[0]) == z.read(paths[1])
        assert z.read(paths[2]) == z.read(paths[3])
        outputs = []
        for name, left, right in STAGES:
            a, b = (json.loads(z.read(p)) for p in (left, right))
            before_active, before_sched = snapshot_summary(a)
            after_active, after_sched = snapshot_summary(b)
            assert before_active.keys() == after_active.keys()
            collections_report = {}
            for kind in ('Citizens', 'Buildings', 'Families', 'Roads', 'Vehicles', 'Projects'):
                changes, changed_entities = changed_fields(a[kind], b[kind])
                collections_report[kind] = {'changed_entities': changed_entities, 'fields': changes}
            assert a['PollutionCells'].keys() == b['PollutionCells'].keys()
            pollution_changes = sum(a['PollutionCells'][k] != b['PollutionCells'][k] for k in a['PollutionCells'])
            home_ids = {f['HomeId'] for f in a['Families'] if f['HomeId'] is not None}
            home_changes = collections.Counter()
            for home_id in home_ids:
                h0, h1 = before_active.get(home_id), after_active.get(home_id)
                if h0 is None or h1 is None:
                    continue
                for field in ('WaterRatio', 'ElectricityRatio', 'WaterDemand', 'ElectricityDemand', 'OperationalCapacity'):
                    if h0[field] != h1[field]:
                        home_changes[field] += 1
            result = {
                'window': name, 'from': a['Now'], 'to': b['Now'],
                'alive': sum(c['Alive'] for c in a['Citizens']),
                'active_buildings': len(before_active), 'homes': len(home_ids),
                'scheduled_before': len(before_sched), 'scheduled_after': len(after_sched),
                'scheduled_membership_flips': len(before_sched ^ after_sched),
                'scheduled_enter': len(after_sched - before_sched),
                'scheduled_leave': len(before_sched - after_sched),
                'pollution_cells': len(a['PollutionCells']), 'pollution_value_changes': pollution_changes,
                'home_field_changes': dict(home_changes),
                'collections': collections_report,
                'ledger_new': len(b['Ledger']) - len(a['Ledger']),
                'history_new': sum(len(y['History'])-len(x['History']) for x,y in zip(a['Citizens'],b['Citizens'])),
                'top_level_changed': sorted(k for k in set(a)|set(b) if a.get(k) != b.get(k) and k not in ('Citizens','Buildings','Families','Roads','Vehicles','Projects','PollutionCells','Ledger')),
            }
            outputs.append(result)
        print(json.dumps({'experiment': 475, 'source_sha256': sha,
                          'scope': 'AUTHENTIC_REAL_MIDNIGHT_TWO_ADVANCE2_BOUNDARIES_NOT_PER_PHASE',
                          'windows': outputs}, indent=2, ensure_ascii=False, sort_keys=True))
        print('PR57_475_BOUNDARY_AUDIT_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python3 pr57-475-cross-phase-boundary.py pr57-465-original.zip')
    main(sys.argv[1])
