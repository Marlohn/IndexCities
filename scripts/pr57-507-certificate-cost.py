#!/usr/bin/env python3
"""PR57 #507: offline ABBA cost/invalidations of the #505 *observational* graph.

No simulation or optimizer is executed here. The #505 probe uses per-slice
full-vehicle snapshots and HashSets; its overhead is NOT a lower bound on a
purpose-built version journal, and its edges are NOT exact causal dependencies.
"""
import hashlib
import re
import statistics
import sys
import zipfile

EXPECTED = '5153ef4e523e66258506b95d04edc335a1cfcdb18385c21044bf39b880fefafa'
ARMS = ('A1', 'B1', 'B2', 'A2')
GRAPH_PREFIX = 'PR57_505_READ_GRAPH '
FIELDS = ('seconds', 'wall', 'cpu', 'allocMB', 'gc0')

def extract(log):
    window = next(x for x in log.splitlines() if x.startswith('WINDOW 1 '))
    values = {}
    for k in FIELDS:
        match = re.search(r'\b' + k + r'=([0-9.]+)', window)
        assert match, (k, window)
        values[k] = float(match.group(1))
    profile = next(x for x in log.splitlines() if x.startswith('VEHICLE_PROFILE '))
    values['slices'] = int(re.search(r'\bslices=(\d+)', profile).group(1))
    graph = []
    for line in log.splitlines():
        if not line.startswith(GRAPH_PREFIX):
            continue
        graph.append({k: int(v) for k, v in re.findall(r'\b(\w+)=(\d+)', line)})
    return values, graph

def main(path):
    with open(path, 'rb') as f:
        data = f.read()
    digest = hashlib.sha256(data).hexdigest()
    assert digest == EXPECTED, digest
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        assert len(z.namelist()) == 13, z.namelist()
        logs = {arm: z.read(f'{arm}/full.log').decode('utf-8') for arm in ARMS}
        for cp in ('checkpoint-0.json', 'checkpoint-1.json'):
            bodies = [z.read(f'{arm}/{cp}') for arm in ARMS]
            assert all(body == bodies[0] for body in bodies[1:]), cp
    measurements = {arm: extract(logs[arm]) for arm in ARMS}
    assert measurements['A1'][1] == measurements['A2'][1] == []
    b1 = measurements['B1'][1]
    assert b1 == measurements['B2'][1]
    assert len(b1) == 294
    assert all(measurements[arm][0]['slices'] == 618 for arm in ARMS)
    assert all(measurements[arm][0]['seconds'] == 200 for arm in ARMS)
    changed = [r for r in b1 if r['netChangedVehicles'] > 0]
    assert len(changed) == 290
    assert all(r['rawLargest'] == 98 for r in b1)
    assert all(r['rawNetChangedEdges'] > 0 for r in changed)
    assert sum(r['eligibleNetChangedEdges'] == 0 for r in changed) == 207
    a = {k: statistics.mean(measurements[arm][0][k] for arm in ('A1', 'A2')) for k in ('wall', 'cpu', 'allocMB', 'gc0')}
    b = {k: statistics.mean(measurements[arm][0][k] for arm in ('B1', 'B2')) for k in ('wall', 'cpu', 'allocMB', 'gc0')}
    print('PR57_507_ORIGINAL_REAL_GRAPH_CERTIFICATE_COST_PASS')
    print('zip_sha256=', digest, 'arms=ABBA steps_per_arm=100 sampled_slices=600 reported_slices=294')
    for arm in ARMS:
        x = measurements[arm][0]
        print(f'{arm}: wall={x["wall"]:.3f}s cpu={x["cpu"]:.3f}s alloc={x["allocMB"]:.1f}MB gen0={int(x["gc0"])}')
    for k in ('wall', 'cpu', 'allocMB', 'gc0'):
        print(f'{k}: control_mean={a[k]:.4f} probe_mean={b[k]:.4f} excess={b[k]-a[k]:.4f} ratio={b[k]/a[k]:.4f}')
    print('net_writer_active_reported=', len(changed), 'raw_edge_invalidations=', sum(r['rawNetChangedEdges'] > 0 for r in changed))
    print('post_guard_zero_changed_edge_slices=', sum(r['eligibleNetChangedEdges'] == 0 for r in changed))
    for k in ('rawEdges', 'eligibleEdges', 'rawNetChangedEdges', 'eligibleNetChangedEdges', 'netChangedVehicles'):
        xs = [r[k] for r in changed]
        print(f'{k}: min={min(xs)} median={statistics.median(xs):g} max={max(xs)} sum={sum(xs)}')
    print('LIMIT: probe snapshots all vehicles; raw candidates are not exact dependencies; net writes miss transient mutations; no integrated optimization A/B.')

if __name__ == '__main__':
    assert len(sys.argv) == 2, 'usage: python pr57-507-certificate-cost.py pr57-505-read-graph.zip'
    main(sys.argv[1])
