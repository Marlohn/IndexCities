#!/usr/bin/env python3
"""Independently audit the original PR57 #445 Actions artifact ZIP.

Usage: python3 scripts/pr57-445-audit.py <artifact.zip>
Only observational, exact public Advance(2) checkpoints; no performance claim.
"""
import hashlib
import json
import re
import sys
import zipfile
from decimal import Decimal

EXPECTED_ZIP = 'ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a'
EXPECTED_SAVES = (
    'b7c52ab87928e7eaa93cfc557b9f3dc935d5b17da44820b405990071940f95b5',
    '686ed27e0cc458d3d8f411a1e308a2dc98e87bf588c12d5dcf1ea787bad1c883',
)

def audit(path):
    raw = open(path, 'rb').read()
    assert hashlib.sha256(raw).hexdigest() == EXPECTED_ZIP
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        names = set(z.namelist())
        assert len(names) == 8, names
        summary = json.loads(z.read('pr57-445/summary.json'))
        assert summary['experiment'] == 445
        assert summary['allAdvance2'] and summary['fullJsonParity']
        assert summary['observationOnly'] and not summary['gainAccepted']
        for n, digest in enumerate(EXPECTED_SAVES):
            a = z.read(f'pr57-445/A/checkpoint-{n}.json')
            b = z.read(f'pr57-445/B/checkpoint-{n}.json')
            assert a == b
            assert hashlib.sha256(a).hexdigest() == digest
            json.loads(a, parse_float=Decimal)
            print('PR57_445_SAVE', n, 'bytes', len(a), 'sha256', digest, 'exactAB', True)
        observed = None
        for arm in ('A', 'B'):
            log = z.read(f'pr57-445/{arm}/full.log').decode('utf-8')
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            assert 'STEP_PROFILE steps=100' in log
            assert len(re.findall(r'^WINDOW ', log, re.M)) == 1
            assert len(re.findall(r'^PR57_AB_CHECKPOINT ', log, re.M)) == 1
            assert len(re.findall(r'^\\s*STEP_PHASE ', log, re.M)) == 13
            rows = re.findall(r'^PR57_445_ACCESS .*', log, re.M)
            assert len(rows) == (1 if arm == 'B' else 0)
            if rows:
                observed = {k: float(v) if k.endswith('Ms') else int(v)
                            for k, v in re.findall(r'(\\w+)=([^ ]+)', rows[0])}
                assert observed['steps'] == 100
                assert observed['matched'] + observed['noProgress'] == observed['dispatch']
                assert observed['sameNonNull'] + observed['sameNull'] + observed['differentAccess'] == observed['matched']
            print('PR57_445_LOG', arm, 'steps=100', 'phases=13', 'invariants=OK')
        assert observed == summary['result']['observed']
        assert summary['result']['A']['sha256'] == EXPECTED_SAVES[1]
        assert summary['result']['B']['sha256'] == EXPECTED_SAVES[1]
        print('PR57_445_AUDIT_PASS', json.dumps(observed, sort_keys=True))

if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-445-audit.py <original-artifact.zip>')
    audit(sys.argv[1])
