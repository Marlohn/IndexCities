#!/usr/bin/env python3
"""PR57 #296: read-only audit of authentic CI physical vehicle-class census.

The opt-in group clocks are intrusive and classify by *final* state/cause.
Groups are not exact causal dependency classes, and snapshots are cumulative.
No cache, scheduler, speedup, or 10k extrapolation is inferred here.
"""
import argparse
import collections
import hashlib
import json
import re
import zipfile

CASES = {'gridARTIFICIAL': 50, 'real14hREAL': 100}
GROUPS = {'operations', 'nontravelling', 'moved', 'queue', 'junction',
          'waiting', 'stationary-trip', 'other'}


def kv(line):
    return dict(re.findall(r'(\w+)=([^\s]+)', line))


def read_lines(z, case, mode):
    return z.read(f'{case}-{mode}.log').decode('utf-8').splitlines()


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('zip_path')
    ap.add_argument('--sha256', required=True)
    args = ap.parse_args()
    with open(args.zip_path, 'rb') as f:
        digest = hashlib.file_digest(f, 'sha256').hexdigest()
    assert digest == args.sha256, (digest, args.sha256)
    with zipfile.ZipFile(args.zip_path) as z:
        parity = 0
        for case, calls in CASES.items():
            for at in sorted({0, 20, calls // 2, calls}):
                left = z.read(f'{case}-control/checkpoint-{at}.json')
                right = z.read(f'{case}-probe/checkpoint-{at}.json')
                assert left == right, (case, at)
                json.loads(left)
                parity += 1
            control = read_lines(z, case, 'control')
            probe = read_lines(z, case, 'probe')
            assert not any(x.startswith('PR57_VEHICLE_COST ') for x in control)
            steps = [kv(x) for x in probe if x.startswith('PR57_294_STEP ')]
            assert len(steps) == calls
            assert [int(x['call']) for x in steps] == list(range(1, calls + 1))
            snapshots = collections.defaultdict(dict)
            for line in probe:
                if line.startswith('PR57_VEHICLE_COST '):
                    r = kv(line)
                    slices = int(r['slices'])
                    group = r['group']
                    assert group in GROUPS and group not in snapshots[slices]
                    snapshots[slices][group] = {
                        'calls': int(r['calls']), 'ms': float(r['ms']),
                        'allocMB': float(r['allocMB']),
                        'claimChanges': int(r['claimChanges']),
                        'spaceCountChanges': int(r['spaceCountChanges']),
                    }
            assert snapshots and all(set(x) == GROUPS for x in snapshots.values())
            assert sorted(snapshots) == list(range(60, max(snapshots) + 1, 60)), sorted(snapshots)
            previous = {g: {k: 0 for k in ('calls', 'ms', 'allocMB', 'claimChanges', 'spaceCountChanges')}
                        for g in GROUPS}
            totals = collections.defaultdict(float)
            windows = []
            for slices in sorted(snapshots):
                start = slices - 59
                end = slices
                group_delta = {
                    g: {k: snapshots[slices][g][k] - previous[g][k] for k in previous[g]}
                    for g in GROUPS
                }
                for g in GROUPS:
                    previous[g] = snapshots[slices][g]
                    totals[g] += group_delta[g]['ms']
                class_ms = sum(d['ms'] for d in group_delta.values())
                dominant = max(GROUPS, key=lambda g: group_delta[g]['ms'])
                windows.append({
                    'slices': f'{start}-{end}',
                    'classTimedMs': round(class_ms, 3), 'dominant': dominant,
                    'dominantClassPct': round(100 * group_delta[dominant]['ms'] / class_ms, 2) if class_ms else 0,
                    'groupMs': {g: round(group_delta[g]['ms'], 3) for g in sorted(GROUPS)},
                    'groupCalls': {g: group_delta[g]['calls'] for g in sorted(GROUPS)},
                    'groupAllocMB': {g: round(group_delta[g]['allocMB'], 3) for g in sorted(GROUPS)},
                })
            regions = [kv(x) for x in probe if x.startswith('PR57_ALLOC_REGIONS ')]
            assert len(regions) == max(snapshots) // 300, (case, len(regions))
            final_control = [kv(x) for x in control if x.startswith('PR57_294_FINAL ')]
            final_probe = [kv(x) for x in probe if x.startswith('PR57_294_FINAL ')]
            assert len(final_control) == len(final_probe) == 1
            print(json.dumps({
                'case': case, 'calls': calls, 'sliceWindows': windows,
                'advanceWindows': [
                    {'calls': f'{i+1}-{min(i+10,calls)}',
                     'vehiclePhaseMs': round(sum(float(x['vehiclesMs']) for x in steps[i:i+10]), 3),
                     'advanceAllocatedMB': round(sum(int(x['advanceAllocatedBytes']) for x in steps[i:i+10]) / 1e6, 3)}
                    for i in range(0,calls,10)],
                'reportedSlices': max(snapshots),
                'classTotalMs': {g: round(totals[g], 3) for g in sorted(GROUPS)},
                'classTotalCalls': {g: snapshots[max(snapshots)][g]['calls'] for g in sorted(GROUPS)},
                'classTotalAllocMB': {g: snapshots[max(snapshots)][g]['allocMB'] for g in sorted(GROUPS)},
                'queueCallsPerReportedSlice': round(snapshots[max(snapshots)]['queue']['calls'] / max(snapshots), 3),
                'classTotalPct': {g: round(100 * totals[g] / sum(totals.values()), 2) for g in sorted(GROUPS)},
                'regionSnapshots': regions,
                'controlWallMs': float(final_control[0]['wallMs']),
                'probeWallMs': float(final_probe[0]['wallMs']),
                'controlCpuMs': float(final_control[0]['cpuMs']),
                'probeCpuMs': float(final_probe[0]['cpuMs']),
            }, sort_keys=True))
        assert parity == 8
    print(f'PR57_296_VERIFIED sha256={digest} parity=8/8 processes=4 cadence=Advance2 noMotorExecution=True')


if __name__ == '__main__':
    main()
