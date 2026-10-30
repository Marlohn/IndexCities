#!/usr/bin/env python3
"""Independent offline reconciliation of PR57 #395 vs #404 original Actions ZIPs.

Usage: python3 scripts/pr57-405-audit.py pr57-395-original.zip pr57-404-original.zip
Only compares REAL evidence; no engine mutation, benchmark or causal-closure claim.
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

EXPECTED = {
    '395': '1624cc6b926d4288961695aeac04ac814c00e556305add94a34be57d4b0539b2',
    '404': '98faa36381b44d4276349f6e797933e4b56b2ef8356b1d17efd771a8ec3b606c',
    'save': 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa',
}
FILES = {'base/checkpoint-10800.json', 'probe/checkpoint-10800.json',
         'base/full.log', 'probe/full.log', 'checkpoints.sha256', 'summary.json'}

def load(path, key):
    path = Path(path)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    assert digest == EXPECTED[key], (key, digest)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == FILES
        base, probe = (z.read(f'{arm}/checkpoint-10800.json') for arm in ('base', 'probe'))
        assert base == probe
        assert hashlib.sha256(base).hexdigest() == EXPECTED['save']
        return (json.loads(z.read('summary.json')), z.read('probe/full.log').decode(),
                z.read('base/full.log').decode(), len(base))

def rows(text, prefix):
    return [dict(re.findall(r'(\\w+)=([^ ]+)', line[len(prefix):]))
            for line in text.splitlines() if line.startswith(prefix + ' ')]

def main():
    assert len(sys.argv) == 3
    old, old_log, old_base_log, save_bytes = load(sys.argv[1], '395')
    new, new_log, new_base_log, new_save_bytes = load(sys.argv[2], '404')
    assert save_bytes == new_save_bytes == 2254838
    assert old['advance2PerArm'] == new['advance2PerArm'] == 10800
    assert old['scope'] == new['scope'] == 'AUTHENTIC_REAL_121_LIVING_98_VEHICLES_6H'
    assert not old['causalClosure'] and not new['causalClosure']
    assert not old['optimizationGainAccepted'] and not new['optimizationGainAccepted']
    boundary = old['arms']['probe']['boundary395']
    assert int(boundary['insideRoute']) == 4 and int(boundary['outsideRoute']) == 12
    events = rows(new_log, 'PR57_404_WRITE')
    expected = {(136, 655, 43, 0), (232, 695, 93, 0),
                (303, 689, 88, 0), (323, 677, 101, 0)}
    keys = {(int(r['physical']), int(r['vehicle']), int(r['oldCount']), int(r['newCount']))
            for r in events}
    assert keys == expected and len(events) == 4
    assert all(r['site'] == 'Cars.cs:88:clear' and r['phase'] == '0' and r['changed'] == 'True'
               for r in events)
    traces = rows(old_log, 'PR57_395_CHANGE')
    traced = {(int(r['physical']), int(r['vehicle'])) for r in traces
              if r['boundary'] == 'withinUnsplit' and r['route'] == 'True'}
    assert traced == {(136, 655), (232, 695)}
    assert traced <= {(int(r['physical']), int(r['vehicle'])) for r in events}
    summary = rows(new_log, 'PR57_404_SUMMARY')
    assert len(summary) == 1
    s = summary[0]
    assert all(s[k] == v for k, v in {'writes':'4','changed':'4','assigns':'0',
                                     'clears':'4','inserts':'0','observational':'True',
                                     'causalClosure':'False','noSkippedSlices':'True'}.items())
    direct = rows(new_log, 'PR57_401_WRITE')
    assert len(direct) == 12 and all(r['site'] == 'EnsureFuel.normal' for r in direct)
    assert all(r['changed'] == 'True' for r in direct)
    assert not rows(new_base_log, 'PR57_404_WRITE')
    assert not rows(new_base_log, 'PR57_401_WRITE')
    assert 'parity=IDENTICAL' in new_base_log and 'parity=IDENTICAL' in new_log
    result = {'crc': 'OK', 'zip_sha256': {'395':EXPECTED['395'], '404':EXPECTED['404']},
              'save_sha256': EXPECTED['save'], 'save_bytes':save_bytes,
              'advance2_per_arm':10800, 'tests_from_action': '513/513 (workflow log)',
              '395_within_route_aggregate':4, '395_between_route_aggregate':12,
              '404_clear_events':events, '404_direct_ensurefuel_events':len(direct),
              'exact_traced_inside_matches':len(traced), 'untraced_inside_events':2,
              'interpretation':'4/4 aggregate; 2/2 trace identities; no universal causal closure',
              'base_wall_ms':new['arms']['base']['wallMs'],
              'probe_wall_ms':new['arms']['probe']['wallMs'],
              'new_accepted_integrated_gain':0}
    print(json.dumps(result, indent=2, sort_keys=True))

if __name__ == '__main__':
    main()
