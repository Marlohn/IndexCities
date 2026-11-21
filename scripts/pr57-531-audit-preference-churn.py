#!/usr/bin/env python3
"""PR57 #531: audit original #530 ZIP; no engine execution or speedup claim.

Usage: python3 scripts/pr57-531-audit-preference-churn.py pr57-530-original.zip
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

DIGEST = '3c0e3e5e5753674bcddbeda027cb836c6fc505a9b58aa2de7e05c24c9113fe44'
CASES = {'congestedREAL121': (100, 121, 98),
         'worksREAL47': (100, 47, 42),
         'gridARTIFICIAL': (50, 0, None)}
ARMS = ('A1', 'B1', 'B2', 'A2')
EXPECTED = {
    'congestedREAL121': (700, 1714, 1532, 0, 12, 2, 2, 1697, 1700, 2, 0),
    'worksREAL47': (125, 35, 0, 0, 17, 0, 0, 2, 18, 0, 0),
    'gridARTIFICIAL': (50, 27, 16, 15, 18, 2, 0, 7, 7, 2, 0),
}
FIELDS = ('slices', 'queries', 'nested', 'passed', 'distinctReaderJunctions',
          'transitions', 'sameReaderTransitions', 'sameReaderQueries',
          'unchangedSets', 'added', 'removed')
WITNESSES = {(1071, 466, 91, 1380), (1243, 346, 159, 1282)}


def main(path):
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == DIGEST
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 37
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 530 and summary['observational'] is True
        assert summary['gainAccepted'] is False
        assert set(summary['cases']) == set(CASES)
        for name, (steps, alive, vehicles) in CASES.items():
            initials, finals, probes = [], [], []
            for arm in ARMS:
                prefix = f'{name}/{arm}/'
                log = z.read(prefix + 'full.log').decode()
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
                assert f'STEP_PROFILE steps={steps} ' in log
                first = z.read(prefix + 'checkpoint-0.json')
                last = z.read(prefix + 'checkpoint-1.json')
                initials.append(first)
                finals.append(last)
                meta = summary['cases'][name]['arms'][arm]
                assert meta['inputSha256'] == hashlib.sha256(first).hexdigest()
                assert meta['finalSha256'] == hashlib.sha256(last).hexdigest()
                lines = [x for x in log.splitlines() if x.startswith('PR57_530_SUM ')]
                witness = [x for x in log.splitlines() if x.startswith('PR57_530_WITNESS ')]
                if arm.startswith('A'):
                    assert not lines and not witness
                    continue
                rows = [{k: int(v) for k, v in re.findall(r'\b(\w+)=(\d+)', x)} for x in lines]
                assert rows and all(rows[i]['slices'] < rows[i + 1]['slices'] for i in range(len(rows) - 1))
                assert all(row['nested'] >= row['passed'] and row['sameReaderTransitions'] <= row['transitions'] for row in rows)
                final = tuple(rows[-1][key] for key in FIELDS)
                assert final == EXPECTED[name], (name, arm, final)
                probes.append((rows, witness))
            assert len(set(initials)) == len(set(finals)) == 1
            assert probes[0] == probes[1], name
            initial = json.loads(initials[0])
            assert sum(bool(c['Alive']) for c in initial['Citizens']) == alive
            if vehicles is not None:
                assert len(initial['Vehicles']) == vehicles
            if name == 'gridARTIFICIAL':
                assert sum(c['Alive'] for c in json.loads(finals[0])['Citizens']) == 108
            print('PR57_531_CASE', name, 'steps', steps, 'lastSample', dict(zip(FIELDS, EXPECTED[name])),
                  'fullSaveParity', True)
            if name == 'congestedREAL121':
                found = set()
                for line in probes[0][1]:
                    m = re.search(r'reader=(\d+) junction=(\d+) slice=(\d+) old= now=(\d+)', line)
                    assert m, line
                    found.add(tuple(map(int, m.groups())))
                assert found == WITNESSES
                end = json.loads(finals[0])
                before_vehicles = {v['Id']: v for v in initial['Vehicles']}
                after_vehicles = {v['Id']: v for v in end['Vehicles']}
                before_citizens = {c['Id']: c for c in initial['Citizens']}
                after_citizens = {c['Id']: c for c in end['Citizens']}
                for reader, junction, slice_no, entrant in sorted(WITNESSES):
                    assert before_vehicles[reader] == after_vehicles[reader]
                    assert before_vehicles[entrant]['Status'] == 'Parked'
                    assert after_vehicles[entrant]['Status'] == 'Travelling'
                    driver = after_vehicles[entrant]['DriverId']
                    assert driver in (1379, 1281)
                    old_history = before_citizens[driver]['History']
                    new_history = after_citizens[driver]['History'][len(old_history):]
                    assert len(new_history) == 1 and 'Vai de carro a Mercearia' in new_history[0]['Description']
                    print('PR57_531_WITNESS', reader, junction, slice_no, entrant,
                          'driver', driver, 'tripHistoryAt', new_history[0]['At'],
                          'firstWriterNotObserved', True)
        print('PR57_531_AUTHENTIC_QUERY_CHURN_AUDIT_PASS')


if __name__ == '__main__':
    assert len(sys.argv) == 2, __doc__
    main(sys.argv[1])
