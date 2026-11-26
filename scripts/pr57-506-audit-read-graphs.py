#!/usr/bin/env python3
"""Independent audit of PR57 #504/#505 authentic read-graph ABBA ZIPs.

Checks original artifact hashes, CRC, every full save, logs, determinism,
read-graph arithmetic and limitations. Graphs are NOT exact causal closures.
"""
import hashlib
import json
import re
import sys
import zipfile

EXPECTED = {
    504: '4d5ecef0cccb3d54d861baffa985d64dac99de85e563b4d3a0baaec4955dadc2',
    505: '5153ef4e523e66258506b95d04edc335a1cfcdb18385c21044bf39b880fefafa',
}
INPUT = '779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'
OUTPUT = '9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23'


def audit(path, number):
    raw = open(path, 'rb').read()
    digest = hashlib.sha256(raw).hexdigest()
    assert digest == EXPECTED[number], (number, digest)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        assert len(z.namelist()) == 13, len(z.namelist())
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == number and summary['acceptedGain'] is False
        assert summary['fullSaveParity'] and summary['authenticReal']
        arms = ('A1', 'B1', 'B2', 'A2')
        starts = [z.read(f'{a}/checkpoint-0.json') for a in arms]
        ends = [z.read(f'{a}/checkpoint-1.json') for a in arms]
        assert len(set(starts)) == len(set(ends)) == 1
        assert hashlib.sha256(starts[0]).hexdigest() == INPUT
        assert hashlib.sha256(ends[0]).hexdigest() == OUTPUT
        state = json.loads(starts[0])
        assert len(state['Vehicles']) == 98
        assert sum(c['Alive'] for c in state['Citizens']) == 121
        observed = []
        for a in arms:
            log = z.read(f'{a}/full.log').decode()
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            assert 'STEP_PROFILE steps=100 ' in log
            assert len([x for x in log.splitlines() if x.startswith('WINDOW ')]) == 1
            rows = [x for x in log.splitlines() if x.startswith(f'PR57_{number}_READ_GRAPH ')]
            assert rows == summary['arms'][a]['readGraph']
            assert bool(rows) == a.startswith('B')
            parsed = []
            for row in rows:
                fields = {k: int(v) for k, v in re.findall(r'(\w+)=(\d+)', row)}
                assert fields['fleet'] == 98
                assert 0 <= fields['eligibleEdges'] <= fields['rawEdges']
                assert 0 <= fields['eligibleNetChangedEdges'] <= fields['eligibleEdges']
                assert 0 <= fields['rawNetChangedEdges'] <= fields['rawEdges']
                assert 1 <= fields['rawLargest'] <= 98
                assert 1 <= fields['eligibleLargest'] <= 98
                assert 1 <= fields['rawComponents'] <= 98
                assert 1 <= fields['eligibleComponents'] <= 98
                assert fields['rawComponents'] + fields['rawLargest'] <= 99
                assert fields['eligibleComponents'] + fields['eligibleLargest'] <= 99
                parsed.append(fields)
            if parsed:
                assert all(x['sample'] < y['sample'] for x, y in zip(parsed, parsed[1:]))
                observed.append(parsed)
        assert len(observed) == 2 and observed[0] == observed[1]
        rows = observed[0]
        print(f'PR57_{number}_READ_GRAPH original_sha256={digest} samples={len(rows)} '
              f'first={rows[0]["sample"]} last={rows[-1]["sample"]} '
              f'net_changed_observed={sum(r["netChangedVehicles"] > 0 for r in rows)} '
              f'raw_largest_max={max(r["rawLargest"] for r in rows)} '
              f'eligible_largest_max={max(r["eligibleLargest"] for r in rows)} '
              'full_save_parity=True')
    return rows


if __name__ == '__main__':
    assert len(sys.argv) == 3, 'usage: script 504.zip 505.zip'
    a = audit(sys.argv[1], 504)
    b = audit(sys.argv[2], 505)
    print('PR57_506_INDEPENDENT_READ_GRAPH_AUDIT_PASS')
