#!/usr/bin/env python3
"""PR57 #482 independent audit of ORIGINAL #481 Actions artifact ZIP.

Validates deterministic counters, original #480 censo, full-save A/B parity,
monotonic per-slice rows and SHA/CRC. No inference of causal cache safety.
"""
import hashlib
import json
import re
import sys
import zipfile

EXPECTED_ZIP = '7213dcfd485f31d287a6bc1aee5cb77311feb2db50eb337c0e986dc4f66eba32'
EXPECTED_INPUT = '779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'
EXPECTED_FINAL = '9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23'
EXPECTED = {
    'slices':718, 'calls':1596, 'pairs':1336, 'sameActor':1330,
    'samePoses':1336, 'sameCandidates':905, 'sameObserved':902,
    'sameBlocker':1336, 'sameTime':0, 'sameVisitCount':1336,
    'allVisits':97156, 'matchedVisits':49948,
    'sameReadActor':1336, 'sameReadCandidates':920, 'sameEntry':1336,
    'sameReadObserved':920, 'fullChangedReadSame':18, 'readChanged':416,
    'readMatchedVisits':51288,
}

def digest(data): return hashlib.sha256(data).hexdigest()

def audit(path):
    assert digest(open(path,'rb').read()) == EXPECTED_ZIP
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        assert set(z.namelist()) == {'summary.json'} | {
            f'{arm}/{name}' for arm in ('A1','B1','B2','A2')
            for name in ('checkpoint-0.json','checkpoint-1.json','full.log')}
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 481
        assert summary['allAdvance2'] and summary['callsAdvance2PerArm'] == 100
        assert summary['inputSha256'] == EXPECTED_INPUT
        assert summary['finalSha256'] == EXPECTED_FINAL
        assert summary['fullSaveByteParity'] and not summary['acceptedGain']
        previous = None
        for arm in ('A1','B1','B2','A2'):
            assert digest(z.read(f'{arm}/checkpoint-0.json')) == EXPECTED_INPUT
            assert digest(z.read(f'{arm}/checkpoint-1.json')) == EXPECTED_FINAL
            state = json.loads(z.read(f'{arm}/checkpoint-0.json'))
            assert sum(bool(c['Alive']) for c in state['Citizens']) == 121
            assert len(state['Vehicles']) == 98
            log = z.read(f'{arm}/full.log').decode()
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            windows = [line for line in log.splitlines() if line.startswith('WINDOW ')]
            assert len(windows) == 1 and re.search(r'\bseconds=200\b',windows[0])
            assert windows[0] == summary['arms'][arm]['window']
            rows = [line for line in log.splitlines() if line.startswith('PR57_481_SUM ')]
            if arm.startswith('A'):
                assert not rows
                continue
            assert len(rows) == 718
            parsed = [{k:int(v) for k,v in re.findall(r'([A-Za-z][A-Za-z0-9]*)=(\d+)',line)} for line in rows]
            assert [row['slices'] for row in parsed] == list(range(1,719))
            for key in EXPECTED:
                assert all(key in row for row in parsed)
                if key != 'slices':
                    assert all(a[key] <= b[key] for a,b in zip(parsed,parsed[1:])),key
            final = parsed[-1]
            assert final == EXPECTED
            assert final == summary['arms'][arm]['census']
            assert rows[-1] == summary['arms'][arm]['lastRow']
            assert final['sameReadObserved'] + final['readChanged'] == final['pairs']
            assert final['sameReadObserved'] - final['sameObserved'] == final['fullChangedReadSame']
            assert final['sameReadObserved'] <= min(final['sameReadActor'],final['sameReadCandidates'],final['samePoses'],final['sameEntry'])
            if previous is not None: assert parsed == previous
            previous = parsed
    print(json.dumps({
        'zip_sha256':EXPECTED_ZIP,'pairs':1336,'full_equal':902,'read_equal':920,
        'full_changed_but_read_equal':18,'read_changed':416,
        'read_equal_percent':round(100*920/1336,3),
        'read_changed_percent':round(100*416/1336,3),
        'read_matched_visits':51288,'accepted_gain':False,
    },sort_keys=True))
    print('PR57_482_INDEPENDENT_READ_PROJECTION_AUDIT_PASS')

if __name__ == '__main__':
    if len(sys.argv) != 2: raise SystemExit('usage: pr57-482-audit.py ORIGINAL-481.zip')
    audit(sys.argv[1])
