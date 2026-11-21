#!/usr/bin/env python3
"""Independent offline PR57 #466 audit of the original #465 Actions ZIP.

Usage: python3 pr57-466-audit-midnight-zip.py pr57-465-original.zip
No engine changes, simulation, invented residents, or 80-year extrapolation.
"""
import collections
import hashlib
import json
from pathlib import Path
import statistics
import sys
import zipfile

EXPECTED_ZIP_SHA = '370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1'
EXPECTED_INPUT_SHA = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'

def sha(data):
    return hashlib.sha256(data).hexdigest()

def audit(path):
    assert sha(Path(path).read_bytes()) == EXPECTED_ZIP_SHA, 'ZIP differs from GitHub digest'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'ZIP CRC failure'
        assert len(z.namelist()) == 53, 'unexpected artifact member count'
        source = z.read('20h.json')
        assert sha(source) == EXPECTED_INPUT_SHA
        before = json.loads(z.read('A-pre/checkpoint-1.json'))
        midnight = json.loads(z.read('A-mid/checkpoint-1.json'))
        after = json.loads(z.read('A-post/checkpoint-1.json'))
        assert [before['Now'], midnight['Now'], after['Now']] == [
            '2026-01-07T23:59:58', '2026-01-08T00:00:00', '2026-01-08T00:00:02']
        assert before['NextDaily'] == '2026-01-08T00:00:00'
        assert midnight['NextDaily'] == '2026-01-09T00:00:00'
        for stage, parent in [('pre', source), ('mid', z.read('A-pre/checkpoint-1.json')),
                              ('post', z.read('A-mid/checkpoint-1.json'))]:
            a = z.read(f'A-{stage}/checkpoint-1.json')
            b = z.read(f'B-{stage}/checkpoint-1.json')
            assert a == b, ('full JSON A/B mismatch', stage)
            assert z.read(f'A-{stage}/checkpoint-0.json') == parent
            assert z.read(f'B-{stage}/checkpoint-0.json') == parent
            for arm in ('A', 'B'):
                log = z.read(f'{arm}-{stage}/full.log').decode('utf-8')
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
        assert len(before['Citizens']) == len(midnight['Citizens']) == 121
        assert len(before['Vehicles']) == len(midnight['Vehicles']) == 98
        assert sum(bool(c['Alive']) for c in midnight['Citizens']) == 121
        new_history = []
        for old, new in zip(before['Citizens'], midnight['Citizens']):
            assert old['Id'] == new['Id']
            assert new['History'][:len(old['History'])] == old['History']
            new_history.extend(new['History'][len(old['History']):])
        assert len(new_history) == 63
        assert all(e['Description'].startswith('Salário pago ') for e in new_history)
        assert sum(c['WagesAccrued'] > 0 for c in before['Citizens']) == 63
        assert sum(c['WagesAccrued'] > 0 for c in midnight['Citizens']) == 0
        assert midnight['Ledger'][:len(before['Ledger'])] == before['Ledger']
        new_ledger = midnight['Ledger'][len(before['Ledger']):]
        categories = collections.Counter('salary' if x['Category'].startswith('Salários / ')
                                          else x['Category'] for x in new_ledger)
        assert categories == {'salary': 63, 'Água': 99, 'Energia': 99}
        assert midnight['CityEvents'] == before['CityEvents']
        assert len(z.read('A-mid/checkpoint-1.json')) - len(z.read('A-pre/checkpoint-1.json')) == 45133
        assert len(z.read('A-post/checkpoint-1.json')) - len(z.read('A-mid/checkpoint-1.json')) == 10202
        report = json.loads(z.read('audit-summary.json'))
        assert report['fullSaveABParity'] and report['stages']['A-pre_to_A-mid']['history']['salaryEvents'] == 63
        samples = [json.loads(line.split('PR57_465_SAVE_PROBE ', 1)[1])
                   for line in z.read('saveprobe-results.log').decode().splitlines()
                   if 'PR57_465_SAVE_PROBE ' in line]
        assert len(samples) == 20 and all(s['exactRoundtrip'] for s in samples)
        medians = {}
        for label in sorted(set(s['label'] for s in samples)):
            warm = [s for s in samples if s['label'] == label and s['iteration'] > 0]
            assert len(warm) == 4
            medians[label] = {'loadMs': round(statistics.median(s['loadMs'] for s in warm), 3),
                              'saveMs': round(statistics.median(s['saveMs'] for s in warm), 3),
                              'allocatedMB': round(statistics.median(s['allocatedBytes'] for s in warm)/1e6, 3)}
        result = {'sha256': EXPECTED_ZIP_SHA, 'members': len(z.namelist()),
                  'fullABParity': True, 'midnightSalaryHistory': 63,
                  'midnightLedger': dict(categories), 'midnightSaveGrowthBytes': 45133,
                  'postMidnight2sSaveGrowthBytes': 10202, 'warmSaveLoadMedians': medians,
                  'scope': '121 authentic living SIMs; single midnight; no acceleration claim'}
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
        print('PR57_466_INDEPENDENT_MIDNIGHT_AUDIT_PASS')

if __name__ == '__main__':
    assert len(sys.argv) == 2
    audit(sys.argv[1])
