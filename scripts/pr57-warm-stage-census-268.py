#!/usr/bin/env python3
"""PR57 #268: warm-tail physical-stage census of authentic #267 ABBA artifact.
Usage: python3 scripts/pr57-warm-stage-census-268.py pr57-267.zip
Source: Action #40649831352, artifact #12799088413 (REAL economy, 121 alive).
Intrusive observational clocks; not causal closure or A/B speedups.
"""
import hashlib
import json
import re
import sys
from zipfile import ZipFile

ARMS = ('0-default', '1-tiering-off', '2-tiering-off', '3-default')
INITIAL_SHA = '779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'
FINAL_SHA = '9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23'


def fields(line):
    return dict(re.findall(r'([A-Za-z][A-Za-z0-9]*)=([^\s]+)', line))


def main(path):
    with ZipFile(path) as archive:
        result = {'source': 'PR57_267_ABBA_REAL_14H_200S', 'arms': []}
        for arm in ARMS:
            for index, expected in ((0, INITIAL_SHA), (1, FINAL_SHA)):
                raw = archive.read(f'{arm}/checkpoint-{index}.json')
                assert hashlib.sha256(raw).hexdigest() == expected
            log = archive.read(f'{arm}/run.log').decode('utf-8')
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            assert 'PR57_266_SUM slices=600 cohort=24 passes=14400' in log
            rows = [fields(line) for line in log.splitlines() if line.startswith('PR57_266_ROW ')]
            assert len(rows) == 24 * 6
            assert len({int(r['id']) for r in rows}) == 24
            assert all(int(r['passes']) == 100 and int(r['s8Segments']) == 0 for r in rows)
            warm = [r for r in rows if int(r['window']) == 5]
            cold = [r for r in rows if int(r['window']) == 0]
            assert len(warm) == len(cold) == 24
            stage = [sum(float(r[f's{i}Ms']) for r in warm) for i in range(9)]
            total = sum(float(r['totalMs']) for r in warm)
            assert abs(sum(stage) - total) < 0.00003
            top = sorted(((int(r['id']), float(r['totalMs'])) for r in warm), key=lambda x: -x[1])
            def share(i, ids):
                return round(sum(float(r[f's{i}Ms']) for r in warm if int(r['id']) in ids)
                             / stage[i] * 100, 2)
            result['arms'].append({
                'arm': arm,
                'warmWindow': 5,
                'warmPasses': sum(int(r['passes']) for r in warm),
                'coldCohortMs': round(sum(float(r['totalMs']) for r in cold), 3),
                'warmCohortMs': round(total, 3),
                'warmStageMs': {f's{i}': round(x, 3) for i, x in enumerate(stage)},
                'warmStagePct': {f's{i}': round(x / total * 100, 2) for i, x in enumerate(stage)},
                'warmTop5SharePct': round(sum(x for _, x in top[:5]) / total * 100, 2),
                's1TwoIdsSharePct': share(1, (1453, 1477)),
                's2Id23SharePct': share(2, (23,)),
                's7ThreeIdsSharePct': share(7, (1071, 1243, 1495)),
                'topIds': top[:5],
            })
        print(json.dumps(result, indent=2, sort_keys=True))


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-warm-stage-census-268.py <artifact-267.zip>')
    main(sys.argv[1])
