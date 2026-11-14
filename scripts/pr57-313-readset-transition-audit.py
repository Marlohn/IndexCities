#!/usr/bin/env python3
"""#313: bounded actual-return readset versus actor-boundary gate writers.

Usage: python3 scripts/pr57-313-readset-transition-audit.py <#303 ZIP> <#312 ZIP>
No motor mutation, no causal-closure claim. Correlation across deterministic runs.
"""
import collections
import hashlib
import re
import sys
import zipfile

assert len(sys.argv) == 3, 'usage: #313.py <#303.zip> <#312.zip>'
EXPECTED = (
    'de77b8789c6ab2b7594d9b196d63846d352ffdb69c1c74caea9b3f9ce64d68ff',
    'df97505740901d41c9795756580957cf09d221d8907ad39679b278d71bdeedf1',
)
for path, expected in zip(sys.argv[1:], EXPECTED):
    assert hashlib.sha256(open(path, 'rb').read()).hexdigest() == expected, path
FIELDS = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)')
TRANSITIONS = {79: ('346', 'Pedestrian', 'OnwardBody', '78', '1086', 'True'),
               92: ('466', 'OnwardBody', 'Pedestrian', '91', '1086', 'False'),
               95: ('466', 'Pedestrian', 'OnwardBody', '94', '1086', 'True')}

with zipfile.ZipFile(sys.argv[1]) as old, zipfile.ZipFile(sys.argv[2]) as new:
    for arm in (1, 2):
        prefix = f'real14hREAL-probe{arm}'
        for checkpoint in (0, 20, 50, 100):
            name = f'{prefix}/checkpoint-{checkpoint}.json'
            assert old.read(name) == new.read(name), (arm, name)
        rows = [dict(FIELDS.findall(line)) for line in old.read(prefix+'.log').decode().splitlines()
                if line.startswith('PR57_303_RETURN ')]
        writers = [dict(FIELDS.findall(line)) for line in new.read(prefix+'.log').decode().splitlines()
                   if line.startswith('PR57_312_GATE ')]
        assert len(rows) == 600 and len(writers) == 9, (arm, len(rows), len(writers))
        assert {r['allowed'] for r in rows} == {'0'}
        by_call = collections.defaultdict(list)
        by_pair = collections.defaultdict(lambda: collections.defaultdict(list))
        for r in rows:
            call = int(r['call'])
            pair = (r['id'], r['node'])
            by_call[call].append(r)
            by_pair[pair][call].append(r)
        assert set(by_call) == set(range(76, 101))
        assert all(len(v) == 24 for v in by_call.values())
        assert len(by_pair) == 4
        assert all(len(entries) == 6 for calls in by_pair.values() for entries in calls.values())
        assert all(len({r['reason'] for r in entries}) == 1
                   for calls in by_pair.values() for entries in calls.values())
        changes = collections.defaultdict(set)
        for (reader, node), calls in by_pair.items():
            assert set(calls) == set(range(76, 101))
            for call in range(77, 101):
                before = calls[call-1][0]['reason']
                after = calls[call][0]['reason']
                if before != after:
                    changes[call].add((reader, node, before, after))
        assert set(changes) == set(TRANSITIONS), changes
        for call, (node, before, after, writer_call, citizen, position_only) in TRANSITIONS.items():
            selected = changes[call]
            assert len(selected) == 2 and {p[1:] for p in selected} == {(node, before, after)}
            source = [w for w in writers if w['call'] == writer_call and w['citizen'] == citizen]
            assert len(source) == 1, (arm, call, source)
            w = source[0]
            assert w['positionOnly'] == position_only
            assert w['before'] == str(before == 'Pedestrian')
            assert w['after'] == str(after == 'Pedestrian')
            assert node in (w['beforeNode'], w['afterNode'])
            print(f'arm={arm} writerCall={writer_call} readerCall={call} node={node} '
                  f'citizen={citizen} positionOnly={position_only} '
                  f'readerFanout=2 returnsPerReader=6 reason={before}->{after} '
                  'admissionsAllowed=0')
        reasons = collections.Counter(r['reason'] for r in rows)
        assert reasons == {'OnwardBody': 528, 'Pedestrian': 72}
        print(f'arm={arm} reads=600 pairs=4 calls=25 readsPerCall=24 '
              f'reasons={dict(reasons)} writerGateFlips=9 '
              f'reasonChangesAcrossCalls={sum(map(len,changes.values()))} '
              'allowedTransitions=0 withinCallReasonChanges=0')
print('SOURCE_ZIP_SHA256=verified CROSS_RUN_CHECKPOINT_PARITY=8/8 '
      'CAUSAL_SCOPE=BOUNDED_TEMPORAL_CORROBORATION_NOT_FULL_INVALIDATION '
      'NO_SAFE_SKIP_OR_THROUGHPUT_CLAIM')
