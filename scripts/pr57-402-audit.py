#!/usr/bin/env python3
"""Independent offline audit of original PR57 #401 Actions ZIP.

No simulation execution or GitHub access. Confirms the direct Route writer witness
against the method-boundary witnesses from the *same* REAL replay, including the
extra phase-0 write missed by #399. The probe is observational, not causal closure.

Usage: python3 scripts/pr57-402-audit.py path/to/pr57-401-original.zip
"""
import hashlib
import json
import re
import sys
import zipfile
from collections import Counter
from pathlib import Path

EXPECTED_ZIP = '9bb372c703da22c4989917208c5972aff58165557fa1ba21968a85b7bc36ba57'
EXPECTED_SAVE = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'
FILES = {
    'base/checkpoint-10800.json', 'base/full.log', 'checkpoints.sha256',
    'probe/checkpoint-10800.json', 'probe/full.log', 'summary.json',
}

def require(condition, message):
    if not condition:
        raise AssertionError(message)

def parse_records(text, prefix):
    result = []
    for line in text.splitlines():
        if not line.startswith(prefix + ' '):
            continue
        parts = dict(re.findall(r'(\w+)=([^ ]+)', line[len(prefix):]))
        result.append(parts)
    return result

def key(r):
    return int(r['step']), int(r['physical']), int(r['vehicle'])

def main(path):
    path = Path(path)
    zip_sha = hashlib.sha256(path.read_bytes()).hexdigest()
    require(zip_sha == EXPECTED_ZIP, f'Unexpected original ZIP sha256: {zip_sha}')
    with zipfile.ZipFile(path) as z:
        require(z.testzip() is None, 'CRC failure')
        require(set(z.namelist()) == FILES, f'Unexpected ZIP entries: {z.namelist()}')
        summary = json.loads(z.read('summary.json'))
        base = z.read('base/full.log').decode()
        probe = z.read('probe/full.log').decode()
        a = z.read('base/checkpoint-10800.json')
        b = z.read('probe/checkpoint-10800.json')
        require(a == b, 'End-to-end save bytes differ')
        require(hashlib.sha256(a).hexdigest() == EXPECTED_SAVE, 'Unexpected canonical save')
        require('parity=IDENTICAL' in base and 'parity=IDENTICAL' in probe,
                'Missing explicit baseline/probe parity')
        require('PR57_401_WRITE' not in base, 'Direct probe active in control')
        direct = parse_records(probe, 'PR57_401_WRITE')
        methods = parse_records(probe, 'PR57_399_ROUTE')
        restock = parse_records(probe, 'PR57_400_ROUTE')
        dsum = parse_records(probe, 'PR57_401_SUMMARY')
        msum = parse_records(probe, 'PR57_399_METHODS')
        rsum = parse_records(probe, 'PR57_400_SUMMARY')
        cleanup = parse_records(probe, 'PR57_399_CLEANUP')
        require(len(dsum) == len(msum) == len(rsum) == len(cleanup) == 1,
                'Missing or duplicated probe summaries')
        ds, ms, rs, cs = dsum[0], msum[0], rsum[0], cleanup[0]
        require(len(direct) == 12 and all(r['changed'] == 'True' for r in direct),
                'Direct changes must be 12')
        require(len(methods) == 11 and len(restock) == 2,
                'Method-boundary witness count changed')
        require(len(set(map(key, direct))) == len(direct), 'Duplicate direct event keys')
        require(len(set(map(key, methods))) == len(methods), 'Duplicate method event keys')
        direct_keys = set(map(key, direct))
        method_keys = set(map(key, methods))
        require(method_keys <= direct_keys, 'Method-level write not seen at direct site')
        missing = sorted(direct_keys - method_keys)
        require(missing == [(26, 159, 1487)], f'Unexpected direct-only events: {missing}')
        phase_zero = next(r for r in direct if key(r) == missing[0])
        require(phase_zero['phase'] == '0' and phase_zero['site'] == 'EnsureFuel.normal'
                and phase_zero['oldCount'] == phase_zero['newCount'] == '12',
                'Extra writer does not match the same-length phase-0 route rewrite')
        require(Counter(r['site'] for r in direct) == {'EnsureFuel.normal': 12},
                'Direct site attribution differs')
        require(Counter(r['method'] for r in methods) ==
                {'TryDrive': 9, 'RestockBusinesses': 2}, 'Method attribution differs')
        require(all(r['method'] == 'OrderLocalFood' for r in restock),
                'Unexpected restock submethod')
        require(set(map(key, restock)) <= method_keys, 'Restock method keys mismatch')
        for k, v in {'writes': '14', 'changed': '12', 'unchanged': '2',
                     'normal': '12', 'station': '0', 'import': '0',
                     'targetChanges': '12', 'steps': '1820',
                     'observational': 'True', 'causalClosure': 'False',
                     'noSkippedSlices': 'True'}.items():
            require(ds.get(k) == v, f'401 summary {k}={ds.get(k)} != {v}')
        require(ms['routeChanges'] == '11' and rs['changes'] == '2',
                'Other probe summaries inconsistent')
        require(cs['directAdmissionClears'] == '14' and cs['physical'] == '12000',
                'Cleanup witness inconsistent')
        require(summary['advance2PerArm'] == 10800 and
                summary['scope'] == 'AUTHENTIC_REAL_121_LIVING_98_VEHICLES_6H',
                'Wrong replay scope')
        require(summary['arms']['probe']['direct401']['changed'] == '12',
                'summary.json disagrees with raw logs')
        require(len(summary['arms']['probe']['directChanges401']) == 12,
                'summary.json omitted direct events')
        require({key(r) for r in summary['arms']['probe']['directChanges401']} == direct_keys,
                'summary.json keys disagree with raw log')
        require(summary['optimizationGainAccepted'] is False and
                summary['causalClosure'] is False, 'Mislabelled probe')
        result = {
            'artifact_sha256': zip_sha,
            'crc': 'OK', 'files': len(FILES),
            'save_sha256': EXPECTED_SAVE, 'saves_identical': True,
            'advance2_per_arm': 10800,
            'direct_writes': int(ds['writes']),
            'direct_changes': len(direct),
            'direct_unchanged': int(ds['unchanged']),
            'method_changes': len(methods),
            'restock_changes': len(restock),
            'direct_only_event': {'step': 26, 'phase': 0,
                                  'physical': 159, 'vehicle': 1487,
                                  'site': phase_zero['site'],
                                  'old_count': 12, 'new_count': 12},
            'direct_sites': dict(Counter(r['site'] for r in direct)),
            'direct_probe_timed_ms': round(int(ds['ticks']) * 1000 /
                                           int(ds['frequency']), 6),
            'direct_probe_scope': '3 sites; not exhaustive; not A/B throughput',
            'new_accepted_integrated_gain': 0,
        }
        print(json.dumps(result, indent=2, ensure_ascii=False))

if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit('Usage: pr57-402-audit.py <original #401 artifact.zip>')
    main(sys.argv[1])
