#!/usr/bin/env python3
"""PR57 #487: independent audit of original #486 Action ZIP, no CI rerun.

Observational scope only: a zero counter in this 100-Step REAL window does
NOT remove the source-level temporary-heading counterexample from #485.
"""
import hashlib
import json
import re
import sys
import zipfile

EXPECTED_ZIP = 'eb1fa3406a1988c9493dd554de9e3cd8eeda623013b23c8fe78e198fd555a332'
EXPECTED_INPUT = '779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'
EXPECTED_FINAL = '9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23'
ARMS = ('A1', 'B1', 'B2', 'A2')
COUNTERS = ('entries', 'changed', 'nested', 'tempNow', 'tempPose',
            'hookTicks', 'preferenceTicks', 'hookMs', 'preferenceMs')


def digest(data):
    return hashlib.sha256(data).hexdigest()


def audit(path):
    assert digest(open(path, 'rb').read()) == EXPECTED_ZIP
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        expected = {'summary.json'} | {
            f'{arm}/{name}' for arm in ARMS
            for name in ('checkpoint-0.json', 'checkpoint-1.json', 'full.log')
        }
        assert set(z.namelist()) == expected
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 486
        assert summary['allAdvance2'] is True
        assert summary['callsAdvance2PerArm'] == 100
        assert summary['observational'] is True
        assert summary['acceptedGain'] is False
        assert summary['fullSaveByteParity'] is True
        assert summary['inputSha256'] == EXPECTED_INPUT
        assert summary['finalSha256'] == EXPECTED_FINAL
        starts, finals = [], []
        for arm in ARMS:
            start = z.read(f'{arm}/checkpoint-0.json')
            final = z.read(f'{arm}/checkpoint-1.json')
            starts.append(start)
            finals.append(final)
            assert digest(start) == EXPECTED_INPUT
            assert digest(final) == EXPECTED_FINAL
            state = json.loads(start)
            assert sum(bool(c['Alive']) for c in state['Citizens']) == 121
            assert len(state['Vehicles']) == 98
            log = z.read(f'{arm}/full.log').decode('utf-8')
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            window = [line for line in log.splitlines() if line.startswith('WINDOW ')]
            assert len(window) == 1 and re.search(r'\bseconds=200\b', window[0])
            assert window[0] == summary['arms'][arm]['window']
            assert summary['arms'][arm]['finalSha256'] == EXPECTED_FINAL
            rows = [line for line in log.splitlines() if line.startswith('PR57_486_TEMP ')]
            if arm.startswith('A'):
                assert not rows
                assert 'census' not in summary['arms'][arm]
                continue
            assert len(rows) == 1
            assert rows[0] == summary['arms'][arm]['row']
            assert 'observational=True closure=False' in rows[0]
            counts = {key: int(value) for key, value in re.findall(
                r'([A-Za-z][A-Za-z0-9]*)=(-?\d+)', rows[0])}
            assert set(counts) == set(COUNTERS)
            assert all(counts[key] == 0 for key in COUNTERS)
            assert summary['arms'][arm]['census'] == counts
        assert all(raw == starts[0] for raw in starts)
        assert all(raw == finals[0] for raw in finals)
        assert len(finals[0]) == 2220348
        print('PR57_487_INDEPENDENT_TRANSIENT_HEADING_AUDIT_PASS '
              'scope=REAL_121_living_98_vehicles stepsPerArm=100 '
              'ABBA=4 completeSaveParity=True nested=0 tempEntries=0 '
              'sourceCounterexampleStillValid=True acceptedGain=False')


if __name__ == '__main__':
    assert len(sys.argv) == 2, 'usage: pr57-487-audit.py original-486.zip'
    audit(sys.argv[1])
