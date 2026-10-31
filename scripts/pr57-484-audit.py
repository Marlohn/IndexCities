#!/usr/bin/env python3
"""PR57 #484: independently audit ORIGINAL partial #483 Action ZIP.

The workflow failed because it compared nondeterministic Stopwatch ticks
between independent processes; this auditor compares deterministic fields only.
No claim of causal closure, cache safety or integrated acceleration.
"""
import hashlib
import json
import re
import sys
import zipfile

EXPECTED_ZIP = 'fe9ead5e10986ab591c38bd3e23b3103d1fbbd0c28a056ec0474c1df2a40bfb8'
EXPECTED_INPUT = '779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'
EXPECTED_FINAL = '9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23'
EXPECTED_COUNTERS = {'calls': 1596, 'visits': 156408, 'scanBytes': 0,
                     'canonicalBytes': 41726768, 'checksum': 97090559671468}
ARMS = ('A1', 'B1', 'B2', 'A2')

def digest(data):
    return hashlib.sha256(data).hexdigest()

def audit(path):
    assert digest(open(path, 'rb').read()) == EXPECTED_ZIP
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        assert set(z.namelist()) == {
            f'{arm}/{name}' for arm in ARMS
            for name in ('checkpoint-0.json', 'checkpoint-1.json', 'full.log')}
        assert 'summary.json' not in z.namelist()  # harness failed after four arms
        samples = []
        windows = {}
        for arm in ARMS:
            start = z.read(f'{arm}/checkpoint-0.json')
            final = z.read(f'{arm}/checkpoint-1.json')
            assert digest(start) == EXPECTED_INPUT
            assert digest(final) == EXPECTED_FINAL and len(final) == 2220348
            state = json.loads(start)
            assert sum(bool(c['Alive']) for c in state['Citizens']) == 121
            assert len(state['Vehicles']) == 98
            log = z.read(f'{arm}/full.log').decode('utf-8')
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            window = [x for x in log.splitlines() if x.startswith('WINDOW ')]
            assert len(window) == 1 and re.search(r'\bseconds=200\b', window[0])
            windows[arm] = window[0]
            lines = [x for x in log.splitlines() if x.startswith('PR57_483_SCAN ')]
            if arm.startswith('A'):
                assert not lines
                continue
            assert len(lines) == 1
            values = {k: int(v) for k, v in re.findall(
                r'([A-Za-z][A-Za-z0-9]*)=(-?\d+)', lines[0])}
            assert all(values[k] == v for k, v in EXPECTED_COUNTERS.items())
            assert values['scanTicks'] > 0 and values['canonicalTicks'] > 0
            scan_ms = float(re.search(r'\bscanMs=(\d+\.\d+)', lines[0]).group(1))
            canonical_ms = float(re.search(r'\bcanonicalMs=(\d+\.\d+)', lines[0]).group(1))
            assert 0 < scan_ms < canonical_ms
            samples.append((scan_ms, canonical_ms))
        assert len(samples) == 2
        # Timers are NOT deterministic and must NOT be compared for equality.
        scan_mean = sum(x for x, _ in samples) / 2
        canonical_mean = sum(y for _, y in samples) / 2
        budget_ms = 100 * (2 * 1000 / 116880)
        result = dict(
            scope='REAL congestionado; 121 vivos, 98 veiculos, 100 Advance(2) por processo',
            original_zip_sha256=EXPECTED_ZIP,
            full_save_parity=True,
            deterministic_counts=EXPECTED_COUNTERS,
            scan_ms_samples=[x for x, _ in samples],
            canonical_ms_samples=[y for _, y in samples],
            scan_ms_mean=round(scan_mean, 6),
            canonical_ms_mean=round(canonical_mean, 6),
            scan_fraction_of_canonical=round(scan_mean / canonical_mean, 6),
            scan_us_per_advance2=round(scan_mean * 10, 3),
            budget_ms_for_100_steps=round(budget_ms, 6),
            scan_floor_vs_target=round(scan_mean / budget_ms, 3),
            canonical_vs_target=round(canonical_mean / budget_ms, 3),
            accepted_gain=False,
            limitations='Read-only O(fleet) scan; excludes comparison, version storage, journal, external invalidators; intrusive timers; no optimization A/B.')
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
        print('PR57_484_INDEPENDENT_SCAN_COST_AUDIT_PASS')

if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-484-audit.py ORIGINAL-483.zip')
    audit(sys.argv[1])
