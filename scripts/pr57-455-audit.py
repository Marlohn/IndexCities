#!/usr/bin/env python3
"""Independent offline audit of original PR57 #454 Action ZIP; no engine mutation."""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP = '814dfb42933745e5c4ca73bfb2aef92211430650e6e597b6c68a16050b097b38'
EXPECTED = {
    'economyREAL14h': (1800, 121, 98, 12618, 40980, 0, 1),
    'worksREAL': (100, 47, 42, 128, 0, 0, 0),
    'gridARTIFICIAL': (50, 1111, 787, 50, 0, 0, 0),
}

def sha(raw):
    return hashlib.sha256(raw).hexdigest()

def audit(path):
    raw = Path(path).read_bytes()
    assert sha(raw) == EXPECTED_ZIP, ('ZIP SHA mismatch', sha(raw))
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'ZIP CRC failure'
        names = z.namelist()
        assert len(names) == 20, len(names)
        summary = json.loads(z.read('fanout-454/summary.json'))
        assert summary['experiment'] == 454 and summary['allAdvance2'] is True
        assert set(summary['cases']) == set(EXPECTED)
        assert len([x for x in names if x.endswith('/full.log')]) == 6
        assert len([x for x in names if x.endswith('.json')]) == 13
        for name, (calls, alive, vehicles, slices, positive_calls, repeats, maxfanout) in EXPECTED.items():
            case = summary['cases'][name]
            assert case['callsAdvance2PerArm'] == calls
            assert (case['living'], case['vehicles']) == (alive, vehicles)
            assert case['fullJsonParity'] is True
            base = 'fanout-454/' + name + '/'
            starts = [z.read(base + arm + '/checkpoint-0.json') for arm in ('A', 'B')]
            finals = [z.read(base + arm + '/checkpoint-1.json') for arm in ('A', 'B')]
            assert starts[0] == starts[1] and finals[0] == finals[1]
            assert sha(starts[0]) == case['inputSha256']
            assert sha(finals[0]) == case['finalSha256']
            for arm in ('A', 'B'):
                log = z.read(base + arm + '/full.log').decode('utf-8')
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
                window = [x for x in log.splitlines() if x.startswith('WINDOW ')]
                assert window == [case['arms'][arm]['window']]
                assert re.search(r'\bseconds=' + str(calls * 2) + r'\b', window[0])
                lines = [x for x in log.splitlines() if x.startswith('PR57_454_SUM ')]
                if arm == 'A':
                    assert not lines
                    continue
                assert len(lines) == slices, (name, len(lines), slices)
                prev = None
                for i, line in enumerate(lines, 1):
                    d = {k:int(v) for k,v in re.findall(r'([A-Za-z][A-Za-z0-9]*)=([0-9]+)', line)}
                    assert d['slices'] == i
                    for p in ('zero', 'positive'):
                        assert d[p+'Calls'] == d[p+'Unique'] + d[p+'Repeats']
                        assert d[p+'Flips'] <= d[p+'Repeats']
                        assert d[p+'Allowed'] <= d[p+'Calls']
                    if prev:
                        for key in ('zeroCalls','positiveCalls','zeroUnique','positiveUnique',
                                    'zeroRepeats','positiveRepeats','zeroFlips','positiveFlips',
                                    'zeroAllowed','positiveAllowed','zeroRepeatSlices',
                                    'positiveRepeatSlices','maxCallsPerSlice','maxFanout'):
                            assert d[key] >= prev[key], (name, i, key)
                    prev = d
                assert prev == case['arms']['B']['census']
                assert lines[-1] == case['arms']['B']['lastRawCensus']
                assert (prev['positiveCalls'], prev['positiveRepeats'], prev['maxFanout']) == (positive_calls, repeats, maxfanout)
                assert prev['positiveFlips'] == 0 and prev['positiveAllowed'] == 0
                print('PR57_455_CASE', name, 'callsAdvance2PerArm', calls,
                      'slices', slices, 'canonicalCanTurnCalls', positive_calls,
                      'withinSliceRepeats', repeats, 'maxFanout', maxfanout,
                      'fullSaveSha256', case['finalSha256'])
    print('PR57_455_AUDIT_PASS zipSha256=' + EXPECTED_ZIP + ' members=20 fullJsonParity=3/3')

if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python pr57-455-audit.py original-pr57-454.zip')
    audit(sys.argv[1])
