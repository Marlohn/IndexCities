#!/usr/bin/env python3
"""Link authentic #303 junction returns to #312 citizen gate writes (REAL 14h).

Usage: python3 scripts/pr57-312-cross-probe-link.py <#303-zip> <#312-zip>
Cross-run temporal corroboration, NOT an instrumented same-run dependency graph.
"""
import hashlib
import re
import sys
import zipfile

expected_hashes = (
    'de77b8789c6ab2b7594d9b196d63846d352ffdb69c1c74caea9b3f9ce64d68ff',
    'df97505740901d41c9795756580957cf09d221d8907ad39679b278d71bdeedf1',
)
assert len(sys.argv) == 3
for path, digest in zip(sys.argv[1:3], expected_hashes):
    assert hashlib.sha256(open(path, 'rb').read()).hexdigest() == digest, path
kv = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)')
transitions = ((78, '346', 'Pedestrian', 'OnwardBody', ('1243', '1282'), True),
               (91, '466', 'OnwardBody', 'Pedestrian', ('1071', '1380'), False),
               (94, '466', 'Pedestrian', 'OnwardBody', ('1071', '1380'), True))
with zipfile.ZipFile(sys.argv[1]) as old, zipfile.ZipFile(sys.argv[2]) as new:
    for arm in ('1', '2'):
        prefix = f'real14hREAL-probe{arm}'
        for checkpoint in (0, 20, 50, 100):
            name = f'{prefix}/checkpoint-{checkpoint}.json'
            assert old.read(name) == new.read(name), (arm, checkpoint)
        old_rows = [dict(kv.findall(line)) for line in old.read(f'{prefix}.log').decode().splitlines()
                    if line.startswith('PR57_303_RETURN ')]
        new_rows = [dict(kv.findall(line)) for line in new.read(f'{prefix}.log').decode().splitlines()
                    if line.startswith('PR57_312_GATE ')]
        assert len(old_rows) == 600
        for call, node, before, after, readers, position_only in transitions:
            writer = [r for r in new_rows if int(r['call']) == call and r['citizen'] == '1086']
            assert len(writer) == 1, (arm, call, writer)
            w = writer[0]
            assert w['before'] == str(before == 'Pedestrian')
            assert w['after'] == str(after == 'Pedestrian')
            assert w['positionOnly'] == str(position_only)
            assert (w['beforeNode'] == node and w['afterNode'] == node) == position_only
            for current, reason in ((call, before), (call + 1, after)):
                reached = [r for r in old_rows if int(r['call']) == current and r['node'] == node]
                assert len(reached) == 12, (arm, call, current, len(reached))
                assert {r['id'] for r in reached} == set(readers)
                assert {r['reason'] for r in reached} == {reason}
                assert {r['allowed'] for r in reached} == {'0'}
            print(f'arm={arm} citizen=1086 writerCall={call} node={node} '
                  f'positionOnly={position_only} readerCall={call+1} '
                  f'readers={readers} return={before}->{after}')
print('CROSS_RUN_CHECKPOINT_PARITY=8/8 sourceZIPsSHA256=verified '
      'CAUSAL_SCOPE=TEMPORAL_CORROBORATION_NOT_UNIVERSAL_CLOSURE')
