#!/usr/bin/env python3
"""PR57 #315: reverse actor-boundary crossing gate events from authentic final saves.

Usage: python3 scripts/pr57-315-gate-endstate-replay.py <#312 ZIP> <#303 ZIP>
This is a CONDITIONAL reconstruction: actor-boundary events omit transient and
external writes, and #303 returns come from a separate deterministic replay.
No motor changes, skipped slices, causal certificate, or throughput claim.
"""
import collections
import hashlib
import json
import math
import re
import sys
import zipfile

EXPECTED = (
    'df97505740901d41c9795756580957cf09d221d8907ad39679b278d71bdeedf1',
    'de77b8789c6ab2b7594d9b196d63846d352ffdb69c1c74caea9b3f9ce64d68ff',
)
assert len(sys.argv) == 3, 'usage: #315.py <#312 ZIP> <#303 ZIP>'
for path, digest in zip(sys.argv[1:], EXPECTED):
    with open(path, 'rb') as f:
        assert hashlib.file_digest(f, 'sha256').hexdigest() == digest, path

FIELDS = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)')

def records(z, name, prefix):
    return [dict(FIELDS.findall(line)) for line in z.read(name).decode().splitlines()
            if line.startswith(prefix + ' ')]

def end_occupants(state):
    roads = {road['Id']: road for road in state['Roads']}
    threshold = state['Tuning']['RoadWidth'] / 2 + 1.5
    occupants = {}
    for c in state['Citizens']:
        node_id = c['CrossingNodeId']
        if not (c['Alive'] and c['VehicleId'] is None and
                c['PathIndex'] < len(c['Path']) and node_id in roads):
            continue
        p, q = c['Position'], roads[node_id]['Position']
        if math.hypot(p['X'] - q['X'], p['Z'] - q['Z']) < threshold:
            occupants[c['Id']] = node_id
    return occupants

def reverse_replay(rows, final):
    # Reverse from a genuine final CityState. For observed actor transitions,
    # after-node must agree with the current reverse state. This does NOT prove
    # there were no unobserved writers between observations.
    active = dict(final)
    flips = []
    redundant = []
    for row in reversed(rows):
        citizen, call = int(row['citizen']), int(row['call'])
        before = int(row['beforeNode']) if row['before'] == 'True' else None
        after = int(row['afterNode']) if row['after'] == 'True' else None
        assert before != after, (call, citizen, before, after)
        assert active.get(citizen) == after, ('final-state mismatch', call, citizen,
                                             after, active.get(citizen))
        after_count = collections.Counter(active.values())
        if before is None:
            active.pop(citizen, None)
        else:
            active[citizen] = before
        before_count = collections.Counter(active.values())
        toggled = [(node, before_count[node], after_count[node])
                   for node in {before, after} - {None}
                   if (before_count[node] > 0) != (after_count[node] > 0)]
        if toggled:
            flips.extend((call, citizen, *change) for change in toggled)
        else:
            redundant.append((call, citizen))
    flips.reverse()
    redundant.reverse()
    return active, flips, redundant

signatures = {}
checkpoint_pairs = 0
cross_experiment_pairs = 0
with zipfile.ZipFile(sys.argv[1]) as new, zipfile.ZipFile(sys.argv[2]) as old:
    for case, end, expected_rows, expected_flips, expected_redundant in (
        ('real14hREAL', 100, 9, 7, 2),
        ('gridARTIFICIAL', 50, 72, 62, 10),
    ):
        signatures[case] = []
        checkpoints = (0, 20, 50, 100) if end == 100 else (0, 20, 25, 50)
        for arm in (1, 2):
            probe = f'{case}-probe{arm}'
            control = f'{case}-control{arm}'
            for step in checkpoints:
                name = f'checkpoint-{step}.json'
                assert new.read(f'{control}/{name}') == new.read(f'{probe}/{name}')
                checkpoint_pairs += 1
                if case == 'real14hREAL':
                    assert old.read(f'{probe}/{name}') == new.read(f'{probe}/{name}')
                    cross_experiment_pairs += 1
            state = json.loads(new.read(f'{probe}/checkpoint-{end}.json'))
            final = end_occupants(state)
            rows = records(new, probe + '.log', 'PR57_312_GATE')
            assert len(rows) == expected_rows
            assert all(int(r['call']) > end - 25 for r in rows)
            initial, flips, redundant = reverse_replay(rows, final)
            assert len(flips) == expected_flips and len(redundant) == expected_redundant
            assert len(initial) == (2 if case == 'real14hREAL' else 0)
            assert len(final) == (1 if case == 'real14hREAL' else 14)
            if case == 'real14hREAL':
                assert initial == {1086: 346, 1490: 804}, initial
                returns = records(old, probe + '.log', 'PR57_303_RETURN')
                assert len(returns) == 600
                assert all(r['allowed'] == '0' for r in returns)
                assert collections.Counter(r['reason'] for r in returns) == {
                    'OnwardBody': 528, 'Pedestrian': 72}
                # Corroboration ACROSS deterministic runs, not a same-run graph.
                for call, node, before, after in (
                    (78, 346, 'Pedestrian', 'OnwardBody'),
                    (91, 466, 'OnwardBody', 'Pedestrian'),
                    (94, 466, 'Pedestrian', 'OnwardBody'),
                ):
                    assert any(f[0] == call and f[2] == node for f in flips)
                    for observed_call, reason in ((call, before), (call + 1, after)):
                        reached = [r for r in returns
                                   if int(r['call']) == observed_call and
                                   int(r['node']) == node]
                        assert len(reached) == 12
                        assert {r['id'] for r in reached} == (
                            {'1243', '1282'} if node == 346 else {'1071', '1380'})
                        assert {r['reason'] for r in reached} == {reason}
            signature = (initial, final, flips, redundant)
            signatures[case].append(signature)
            print('PR57_315_AUDIT ' + json.dumps({
                'case': case, 'arm': arm,
                'individualActorBoundaryFlips': len(rows),
                'conditionalNodeBooleanFlips': len(flips),
                'conditionalCoalescedActorFlips': len(redundant),
                'nodeFlipCalls': sorted({x[0] for x in flips}),
                'inferredInitialOccupants': sorted(initial.items()),
                'canonicalFinalOccupants': sorted(final.items()),
                'crossExperimentActualReturns': 600 if case == 'real14hREAL' else None,
                'scope': 'FINAL_SAVE_ANCHORED_CONDITIONAL_ACTOR_BOUNDARY_ONLY',
            }, sort_keys=True))
        assert signatures[case][0] == signatures[case][1], case

assert checkpoint_pairs == 16 and cross_experiment_pairs == 8
print('PR57_315_VERIFIED zipHashes=2 checkpointParity=16/16 '
      'crossExperimentParity=8/8 armsIdentical=True '
      'REAL=9_TO_7 GRID_ARTIFICIAL=72_TO_62 '
      'EXTERNAL_AND_TRANSIENT_WRITERS_NOT_CLOSED '
      'CROSS_RUN_RETURNS_NOT_SAME_RUN_GRAPH NO_SKIP NO_GAIN')
