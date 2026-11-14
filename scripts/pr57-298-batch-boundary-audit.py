#!/usr/bin/env python3
"""PR57 #298: read-only batch/queue boundary audit of authentic CI ZIP.

The queue class is assigned by final Cause/Status. No causal independence,
GC attribution, scheduler change or speedup is inferred from these clocks.
"""
import argparse
import hashlib
import json
import re
import statistics
import zipfile

CASES = {'gridARTIFICIAL': 50, 'real14hREAL': 100}

def kv(s):
    return dict(re.findall(r'(\w+)=([^\s]+)', s))

def correlation(a, b):
    return round(statistics.correlation(a, b), 4) if statistics.pstdev(a) and statistics.pstdev(b) else None

def audit(z, case, calls):
    pairs = [('control1', 'probe1'), ('control2', 'probe2')]
    parity = 0
    for control, probe in pairs:
        for at in sorted({0, 20, calls // 2, calls}):
            a = z.read(f'{case}-{control}/checkpoint-{at}.json')
            b = z.read(f'{case}-{probe}/checkpoint-{at}.json')
            assert a == b, (case, control, probe, at)
            json.loads(a)
            parity += 1
    for _, probe in pairs:
        log = z.read(f'{case}-{probe}.log').decode().splitlines()
        phases = {int(v['call']): v for v in map(kv, (x for x in log if x.startswith('PR57_294_STEP ')))}
        steps = {int(v['call']): v for v in map(kv, (x for x in log if x.startswith('PR57_298_STEP ')))}
        groups = {}
        for line in log:
            if line.startswith('PR57_298_CLASS '):
                v = kv(line)
                key = (int(v['call']), int(v['group']))
                assert key not in groups
                groups[key] = v
        assert sorted(steps) == sorted(phases) == list(range(1, calls + 1))
        assert len(groups) == calls * 8
        rows = []
        for i in range(1, calls + 1):
            s, p = steps[i], phases[i]
            g = [groups[(i, j)] for j in range(8)]
            assert int(s['slices']) > 0
            assert all(int(v['slices']) == int(s['slices']) for v in g)
            assert all(int(v[k]) >= 0 for v in g for k in ('evaluations', 'ticks', 'bytes', 'claimChanges', 'spaceCountChanges'))
            frequency = int(s['stopwatchFrequency'])
            assert frequency > 0
            assert abs(sum(int(v['ticks']) for v in g) * 1000 / frequency - float(s['classifiedMs'])) < .001
            assert sum(int(v['bytes']) for v in g) == int(s['classifiedBytes'])
            assert abs(float(s['vehiclesMs']) - float(p['vehiclesMs'])) < .001
            batched = int(g[0]['evaluations']) > 0
            rows.append({'call': i, 'batched': batched,
                         'passes': int(s['slices']), 'motion': int(s['slices']) - int(batched),
                         'vehicleMs': float(s['vehiclesMs']), 'wallMs': float(p['advanceWallMs']),
                         'queueCalls': int(g[3]['evaluations']),
                         'queueMs': int(g[3]['ticks']) * 1000 / frequency,
                         'queueBytes': int(g[3]['bytes']),
                         'classifiedMs': float(s['classifiedMs']),
                         'gc': [int(s[f'gc{j}']) for j in range(3)]})
        assert all(r['motion'] >= 1 for r in rows)
        batched = [r for r in rows if r['batched']]
        result = {
            'case': case, 'probe': probe, 'calls': calls, 'checkpointPairs': 4,
            'batchedCalls': [r['call'] for r in batched],
            'unsplitPasses': sum(r['passes'] for r in rows),
            'motionSlices': sum(r['motion'] for r in rows),
            'maxMotionSlicesPerAdvance': max(r['motion'] for r in rows),
            'queueEvaluations': sum(r['queueCalls'] for r in rows),
            'queueMs': round(sum(r['queueMs'] for r in rows), 3),
            'queueBytes': sum(r['queueBytes'] for r in rows),
            'queuePctClassified': round(100 * sum(r['queueMs'] for r in rows) / sum(r['classifiedMs'] for r in rows), 2),
            'batchedPctVehicleMs': round(100 * sum(r['vehicleMs'] for r in batched) / sum(r['vehicleMs'] for r in rows), 2),
            'batchedPctAdvanceWall': round(100 * sum(r['wallMs'] for r in batched) / sum(r['wallMs'] for r in rows), 2),
            'rPassesVehicleMs': correlation([r['passes'] for r in rows], [r['vehicleMs'] for r in rows]),
            'rQueueCallsVehicleMs': correlation([r['queueCalls'] for r in rows], [r['vehicleMs'] for r in rows]),
            'gcCollections': [sum(r['gc'][j] for r in rows) for j in range(3)],
        }
        print(json.dumps(result, sort_keys=True))
    return parity

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('zip_path')
    ap.add_argument('--sha256', required=True)
    args = ap.parse_args()
    with open(args.zip_path, 'rb') as f:
        digest = hashlib.file_digest(f, 'sha256').hexdigest()
    assert digest == args.sha256, (digest, args.sha256)
    with zipfile.ZipFile(args.zip_path) as z:
        assert sum(audit(z, case, calls) for case, calls in CASES.items()) == 16
    print('PR57_298_VERIFIED sha256=' + digest + ' parity=16/16 processes=8 cadence=Advance2 readOnly=True noGainClaim=True')

if __name__ == '__main__':
    main()
