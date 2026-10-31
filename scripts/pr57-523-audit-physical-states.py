#!/usr/bin/env python3
"""PR57 #523: independent audit of the authentic #522 ABBA artifact.

Usage: python3 scripts/pr57-523-audit-physical-states.py pr57-522.zip
Only reads the original ZIP. Never modifies the engine, save, or ZIP.
"""
import hashlib
import json
import re
import sys
import zipfile
from collections import defaultdict
from pathlib import Path

EXPECTED_ZIP_SHA256 = '2561e9a9fe2379ee1f60b28ae45eaac67ceee81f998e69b7d10059e649d8fbd9'
CASES = {'congestedREAL121': 100, 'worksREAL47': 300, 'GRID_ARTIFICIAL': 300}
ARMS = ('A1', 'B1', 'B2', 'A2')
GROUPS = ('operations', 'nontravelling', 'moved', 'queue', 'junction', 'waiting', 'stationary-trip', 'other')


def fields(line):
    return dict(re.findall(r'([A-Za-z][A-Za-z0-9]*)=([^\s]+)', line))


def read_rows(log, prefix):
    return [fields(line) for line in log.splitlines() if line.startswith(prefix)]


def main(path):
    data = Path(path).read_bytes()
    assert hashlib.sha256(data).hexdigest() == EXPECTED_ZIP_SHA256, 'ZIP digest'
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None, 'ZIP CRC'
        assert len(archive.namelist()) == 37, 'ZIP members'
        summary = json.loads(archive.read('summary.json'))
        assert summary['experiment'] == 522 and summary['observational'] is True
        assert summary['gainAccepted'] is False and set(summary['cases']) == set(CASES)
        for case, steps in CASES.items():
            info = summary['cases'][case]
            assert info['stepsPerArm'] == steps and info['fullJsonParity'] is True
            starts, ends, observations = [], [], []
            for arm in ARMS:
                base = f'{case}/{arm}/'
                start = archive.read(base + 'checkpoint-0.json')
                end = archive.read(base + 'checkpoint-1.json')
                starts.append(start)
                ends.append(end)
                log = archive.read(base + 'full.log').decode('utf-8')
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
                assert len(read_rows(log, 'STEP_PROFILE ')) == 1
                assert int(read_rows(log, 'STEP_PROFILE ')[0]['steps']) == steps
                assert len(read_rows(log, 'VEHICLE_PROFILE ')) == 1
                assert hashlib.sha256(start).hexdigest() == info['arms'][arm]['initialSha256']
                assert hashlib.sha256(end).hexdigest() == info['arms'][arm]['finalSha256']
                assert info['arms'][arm]['window'] in log
                if arm.startswith('A'):
                    assert not read_rows(log, 'PR57_VEHICLE_COST ')
                    assert not read_rows(log, 'PR57_HOT_TIMING ')
                    continue
                cost = read_rows(log, 'PR57_VEHICLE_COST ')
                hot = read_rows(log, 'PR57_HOT_TIMING ')
                assert len(cost) % 8 == 0 and len(hot) == (2 if case == 'congestedREAL121' else 1)
                by_slice = defaultdict(dict)
                for row in cost:
                    slice_id = int(row['slices'])
                    assert row['group'] in GROUPS and row['group'] not in by_slice[slice_id]
                    by_slice[slice_id][row['group']] = row
                assert all(set(x) == set(GROUPS) for x in by_slice.values())
                for group in GROUPS:
                    last = (-1, -1.0, -1.0)
                    for slice_id in sorted(by_slice):
                        row = by_slice[slice_id][group]
                        current = (int(row['calls']), float(row['ms']), float(row['allocMB']))
                        assert all(a >= b for a, b in zip(current, last)), (case, arm, slice_id, group)
                        last = current
                latest = by_slice[max(by_slice)]
                counts = {g: int(latest[g]['calls']) for g in GROUPS}
                elapsed = {g: float(latest[g]['ms']) for g in GROUPS}
                assert sum(counts.values()) > 0
                assert 99.8 < sum(float(latest[g]['sharePct']) for g in GROUPS) < 100.2
                observations.append((arm, by_slice, hot, counts, elapsed))
            assert all(x == starts[0] for x in starts), (case, 'initial parity')
            assert all(x == ends[0] for x in ends), (case, 'final parity')
            assert len(observations) == 2
            first, second = observations
            assert first[3] == second[3], (case, 'deterministic group counts')
            assert len(first[2]) == len(second[2])
            for left, right in zip(first[2], second[2]):
                for key in ('slices', 'siteCalls', 'crossingCalls', 'turnCalls', 'junctionCalls'):
                    assert left[key] == right[key], (case, key)
            for arm, series, hot, counts, elapsed in observations:
                total = sum(elapsed.values())
                assert total > 0
                print(f'{case} {arm} classifiedMs={total:.3f} calls={sum(counts.values())} '
                      f'moved={counts["moved"]}/{elapsed["moved"]:.3f}ms '
                      f'queue={counts["queue"]}/{elapsed["queue"]:.3f}ms '
                      f'waiting={counts["waiting"]}/{elapsed["waiting"]:.3f}ms '
                      f'queueWaitingPct={100*(elapsed["queue"]+elapsed["waiting"])/total:.2f}% '
                      f'hotJunctionMs={float(hot[-1]["junctionMs"]):.3f}')
                if case == 'congestedREAL121':
                    before, after = series[420], series[660]
                    moved_delta = int(after['moved']['calls']) - int(before['moved']['calls'])
                    queue_delta = int(after['queue']['calls']) - int(before['queue']['calls'])
                    waiting_delta = int(after['waiting']['calls']) - int(before['waiting']['calls'])
                    queue_ms = float(after['queue']['ms']) - float(before['queue']['ms'])
                    waiting_ms = float(after['waiting']['ms']) - float(before['waiting']['ms'])
                    assert moved_delta == 0 and queue_delta == 4930 and waiting_delta == 824
                    assert int(after['moved']['claimChanges']) == int(before['moved']['claimChanges'])
                    assert int(after['moved']['spaceCountChanges']) == int(before['moved']['spaceCountChanges'])
                    print(f'  cumulative-slice-tail 420->660: movedVisitsDelta={moved_delta} '
                          f'queueVisitsDelta={queue_delta} waitingVisitsDelta={waiting_delta} '
                          f'queueWaitingMsDelta={queue_ms+waiting_ms:.3f} '
                          f'(queue={queue_ms:.3f}, waiting={waiting_ms:.3f})')
    print('PR57_523_INDEPENDENT_PHYSICAL_STATE_AUDIT_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-523-audit-physical-states.py pr57-522.zip')
    main(sys.argv[1])
