#!/usr/bin/env python3
"""Audit PR57 #312 actor-boundary crossing-gate observer artifact.

Usage: python3 scripts/pr57-312-crossing-writer-audit.py <unpacked-artifact-dir>
This validates exact checkpoint parity and repeatability; it does not certify
causal closure, per-segment writes, or any throughput gain.
"""
import hashlib
import pathlib
import re
import sys

root = pathlib.Path(sys.argv[1])
kv = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)')
keys = ('pose', 'crossing', 'moving', 'vehicle', 'gateFlips',
        'sameNodeGateFlips', 'positionOnlyGateFlips')
pairs = 0
for case, end, start in (('real14hREAL', 100, 76), ('gridARTIFICIAL', 50, 26)):
    observations = []
    final_hashes = []
    for arm in ('1', '2'):
        control = root / f'{case}-control{arm}'
        probe = root / f'{case}-probe{arm}'
        names = {p.name for p in control.glob('checkpoint-*.json')}
        assert len(names) == 4, (case, arm, names)
        assert names == {p.name for p in probe.glob('checkpoint-*.json')}
        for name in sorted(names):
            assert (control / name).read_bytes() == (probe / name).read_bytes(), (case, arm, name)
            pairs += 1
        final_hashes.append(hashlib.sha256((probe / f'checkpoint-{end}.json').read_bytes()).hexdigest())
        log = (root / f'{case}-probe{arm}.log').read_text()
        rows = [dict(kv.findall(line)) for line in log.splitlines()
                if line.startswith('PR57_312_SUM ')]
        events = [dict(kv.findall(line)) for line in log.splitlines()
                  if line.startswith('PR57_312_GATE ')]
        assert len(rows) == 25, (case, arm, len(rows))
        assert [int(row['call']) for row in rows] == list(range(start, end + 1))
        totals = {key: sum(int(row[key]) for row in rows) for key in keys}
        assert totals['positionOnlyGateFlips'] <= totals['sameNodeGateFlips'] <= totals['gateFlips']
        assert len(events) == totals['gateFlips'], (case, arm, len(events), totals)
        assert sum(e['positionOnly'] == 'True' for e in events) == totals['positionOnlyGateFlips']
        observations.append(([(row['call'], *(row[key] for key in keys)) for row in rows],
                             [(e['call'], e['citizen'], e['beforeNode'], e['afterNode'],
                               e['before'], e['after'], e['positionOnly']) for e in events]))
        print(f'CASE={case} arm={arm} checkpointPairs=4 total={totals} finalSHA256={final_hashes[-1]}')
    assert observations[0] == observations[1], (case, 'observer count/event mismatch')
    assert final_hashes[0] == final_hashes[1], (case, 'final state mismatch')
print(f'CHECKPOINT_PARITY={pairs}/16 repeatedObserverCounts=identical NO_THROUGHPUT_GAIN_CLAIM')
