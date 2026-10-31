#!/usr/bin/env python3
"""PR57 #314: bounded per-node coalescing audit of authentic #312 gate events.

Usage: python3 scripts/pr57-314-gate-coalescing.py <#312 artifact ZIP>
No motor mutation. An observed citizen's active interval is tracked only after
its entry was seen; unobserved initial occupants/writers are NOT inferred.
"""
import collections
import hashlib
import re
import sys
import zipfile

EXPECTED_SHA256 = 'df97505740901d41c9795756580957cf09d221d8907ad39679b278d71bdeedf1'
assert len(sys.argv) == 2, 'usage: pr57-314-gate-coalescing.py <#312 ZIP>'
with open(sys.argv[1], 'rb') as f:
    assert hashlib.file_digest(f, 'sha256').hexdigest() == EXPECTED_SHA256

FIELDS = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)')
CASES = (('real14hREAL', 100, 9, 2), ('gridARTIFICIAL', 50, 72, 10))

with zipfile.ZipFile(sys.argv[1]) as archive:
    for case, end, expected_events, expected_overlap in CASES:
        arm_signatures = []
        for arm in (1, 2):
            prefix = f'{case}-probe{arm}'
            control = f'{case}-control{arm}'
            for checkpoint in (0, 20, 50, 100) if end == 100 else (0, 20, 25, 50):
                name = f'checkpoint-{checkpoint}.json'
                assert archive.read(f'{prefix}/{name}') == archive.read(f'{control}/{name}')
            rows = [dict(FIELDS.findall(line)) for line in archive.read(prefix + '.log').decode().splitlines()
                    if line.startswith('PR57_312_GATE ')]
            assert len(rows) == expected_events, (case, arm, len(rows))
            # The log is in canonical citizen processing order. A witnessed entry
            # starts a known-active interval, closed by a witnessed exit.
            known = collections.defaultdict(set)
            overlaps = []
            unmatched_exits = []
            for row in rows:
                call, citizen = int(row['call']), int(row['citizen'])
                assert end - 24 <= call <= end
                assert row['before'] != row['after']
                if row['before'] == 'True':
                    node = row['beforeNode']
                    others = sorted(known[node] - {citizen})
                    if others:
                        overlaps.append((call, citizen, int(node), 'exit', tuple(others)))
                    if citizen in known[node]:
                        known[node].remove(citizen)
                    else:
                        unmatched_exits.append((call, citizen, node))
                if row['after'] == 'True':
                    node = row['afterNode']
                    others = sorted(known[node] - {citizen})
                    if others:
                        overlaps.append((call, citizen, int(node), 'entry', tuple(others)))
                    known[node].add(citizen)
            assert len(overlaps) == expected_overlap, (case, arm, overlaps)
            arm_signatures.append((rows, overlaps, unmatched_exits))
            print(f'case={case} arm={arm} individualFlips={len(rows)} '
                  f'otherObservedActive={len(overlaps)} '
                  f'nodesWithOverlap={len(set(x[2] for x in overlaps))} '
                  f'unmatchedInitialExits={len(unmatched_exits)} '
                  f'overlapEvents={overlaps}')
        assert arm_signatures[0] == arm_signatures[1], (case, 'nonrepeatable')
print('ZIP_SHA256=verified CHECKPOINT_PARITY=16/16 '
      'PROBES=identical SCOPE=OBSERVED_ACTOR_INTERVALS_ONLY '
      'EXTERNAL_WRITERS_AND_INITIAL_OCCUPANTS_NOT_CLOSED '
      'NO_SKIP_NO_CAUSAL_CERTIFICATE_NO_THROUGHPUT_GAIN')
