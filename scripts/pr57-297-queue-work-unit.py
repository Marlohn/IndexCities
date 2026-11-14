#!/usr/bin/env python3
"""PR57 #297: audit per-evaluation cost of the #296 queue class.

Read-only, authentic CI artifact; not a scheduler, cache or benchmark A/B.
Vehicle class is assigned from final Cause/Status, not a causal predicate.
A 60-slice snapshot can fall *inside* an Advance(2), so it cannot be
assigned exactly to a per-call phase timer or a single spike.
"""
import argparse
import collections
import hashlib
import json
import re
import zipfile

EXPECTED_CASES = {'gridARTIFICIAL': 50, 'real14hREAL': 100}
GROUPS = {'operations', 'nontravelling', 'moved', 'queue', 'junction',
          'waiting', 'stationary-trip', 'other'}
FIELDS = ('calls', 'ms', 'allocMB', 'claimChanges', 'spaceCountChanges')


def kv(line):
    return dict(re.findall(r'(\w+)=([^\s]+)', line))


def audit_case(z, case, calls):
    parity = 0
    for at in sorted({0, 20, calls // 2, calls}):
        a = z.read(f'{case}-control/checkpoint-{at}.json')
        b = z.read(f'{case}-probe/checkpoint-{at}.json')
        assert a == b, (case, at, 'checkpoint mismatch')
        json.loads(a)
        parity += 1
    control = z.read(f'{case}-control.log').decode().splitlines()
    probe = z.read(f'{case}-probe.log').decode().splitlines()
    assert not any(x.startswith('PR57_VEHICLE_COST ') for x in control)
    assert len([x for x in control if x.startswith('PR57_294_FINAL ')]) == 1
    assert len([x for x in probe if x.startswith('PR57_294_FINAL ')]) == 1
    steps = [kv(x) for x in probe if x.startswith('PR57_294_STEP ')]
    assert len(steps) == calls
    assert [int(x['call']) for x in steps] == list(range(1, calls + 1))

    snapshots = collections.defaultdict(dict)
    inside_advance = {}
    completed = 0
    for line in probe:
        if line.startswith('PR57_294_STEP '):
            completed = int(kv(line)['call'])
        elif line.startswith('PR57_VEHICLE_COST '):
            v = kv(line)
            s = int(v['slices'])
            group = v['group']
            assert group in GROUPS and group not in snapshots[s]
            snapshots[s][group] = {f: float(v[f]) for f in FIELDS}
            inside_advance.setdefault(s, completed + 1)
            assert inside_advance[s] == completed + 1
    assert snapshots
    assert all(set(groups) == GROUPS for groups in snapshots.values())
    assert sorted(snapshots) == list(range(60, max(snapshots) + 1, 60))

    prior = {g: {f: 0.0 for f in FIELDS} for g in GROUPS}
    windows = []
    for s in sorted(snapshots):
        current = snapshots[s]
        delta = {g: {f: current[g][f] - prior[g][f] for f in FIELDS}
                 for g in GROUPS}
        assert all(x >= -1e-6 for g in GROUPS for x in delta[g].values())
        prior = current
        q = delta['queue']
        total = sum(delta[g]['ms'] for g in GROUPS)
        assert q['calls'] > 0 and total > 0
        windows.append({
            'slices': f'{s-59}-{s}',
            'snapshotDuringAdvance': inside_advance[s],
            'queueEvaluations': int(q['calls']),
            'queueEvaluationsPerSlice': round(q['calls'] / 60, 3),
            'queueTimedMs': round(q['ms'], 3),
            'queueMicrosecondsPerEvaluation': round(q['ms'] * 1000 / q['calls'], 3),
            'queueAllocatedMB': round(q['allocMB'], 3),
            'queueAllocatedBytesPerEvaluation': round(q['allocMB'] * 1e6 / q['calls'], 1),
            'queuePctClassifiedTime': round(q['ms'] * 100 / total, 2),
            'movedEvaluations': int(delta['moved']['calls']),
        })
    qcount = [w['queueEvaluationsPerSlice'] for w in windows]
    qus = [w['queueMicrosecondsPerEvaluation'] for w in windows]
    return {
        'case': case, 'advanceCalls': calls,
        'sampledPhysicalSlices': max(snapshots),
        'sampledWindows': len(windows),
        'queueEvaluationsPerSliceRange': [min(qcount), max(qcount)],
        'queueMicrosecondsPerEvaluationRange': [min(qus), max(qus)],
        'queuePerEvaluationTimeRatioMaxMin': round(max(qus) / min(qus), 3),
        'windows': windows,
        'parityPairs': parity,
        'limits': '60-slice windows, cumulative intrusive class clocks; snapshot inside Advance; no per-call attribution or full causal closure',
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('zip_path')
    parser.add_argument('--sha256', required=True)
    args = parser.parse_args()
    with open(args.zip_path, 'rb') as f:
        digest = hashlib.file_digest(f, 'sha256').hexdigest()
    assert digest == args.sha256, (digest, args.sha256)
    with zipfile.ZipFile(args.zip_path) as z:
        results = [audit_case(z, case, calls) for case, calls in EXPECTED_CASES.items()]
    assert sum(x['parityPairs'] for x in results) == 8
    for result in results:
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    print(f'PR57_297_VERIFIED sha256={digest} parity=8/8 processes=4 cadence=Advance2 '
          'source=CI296 readOnly=True noMotorRun=True noGainClaim=True')


if __name__ == '__main__':
    main()
