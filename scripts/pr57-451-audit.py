#!/usr/bin/env python3
"""Audit the original #450 GitHub Actions ZIP; no simulation or mutation."""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ZIP_SHA = '9ec93ba761f3560794dbdae1b81c3486dcb4b9bc84cba26f063dd5a06c8d8257'
SAVE_SHA = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'
MEMBERS = {'A/checkpoint-10800.json', 'B/checkpoint-10800.json', 'A/full.log',
           'B/full.log', 'checkpoints.sha256', 'summary-console.log', 'summary.json'}
KEYS = ('preferenceCalls', 'preferenceBytes', 'canTurnCalls', 'canTurnBytes',
        'canTurnAllowed', 'turnWayCalls', 'turnWayBytes', 'laneBodyCalls',
        'laneBodyBytes', 'entryPlanCalls', 'entryPlanBytes', 'dockBodyCalls',
        'dockBodyBytes', 'residualCanTurnBytes')

def rows(log, prefix):
    return [dict(token.split('=', 1) for token in line.split()[1:] if '=' in token)
            for line in log.splitlines() if line.startswith(prefix + ' ')]

def audit(path):
    assert hashlib.sha256(path.read_bytes()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and set(z.namelist()) == MEMBERS
        a, b = (z.read(f'{arm}/checkpoint-10800.json') for arm in 'AB')
        assert a == b and len(a) == 2_254_838
        assert hashlib.sha256(a).hexdigest() == SAVE_SHA
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 450 and summary['fullJsonParity']
        assert summary['observationOnly'] and not summary['speedupAccepted']
        logs = {arm: z.read(f'{arm}/full.log').decode() for arm in 'AB'}
        obs = rows(logs['B'], 'PR57_450_PREFERENCE')
        assert len(obs) == 6 and not rows(logs['A'], 'PR57_450_PREFERENCE')
        totals = {key: sum(int(row[key]) for row in obs) for key in KEYS}
        assert totals == summary['counters']
        assert totals['canTurnCalls'] == totals['turnWayCalls'] == totals['laneBodyCalls']
        assert totals['canTurnAllowed'] == totals['entryPlanCalls'] == totals['dockBodyCalls'] == 0
        assert totals['canTurnBytes'] == totals['turnWayBytes'] + totals['laneBodyBytes']
        assert totals['residualCanTurnBytes'] == 0
        assert totals['canTurnBytes'] <= totals['preferenceBytes']
        metrics = {}
        for arm in 'AB':
            h = rows(logs[arm], 'PR57_337_HOUR')
            g = rows(logs[arm], 'PR57_337_GROUP')
            final = rows(logs[arm], 'PR57_337_FINAL')
            assert len(h) == 6 and len(g) == 48 and len(final) == 1
            assert final[0]['parity'] == 'IDENTICAL'
            assert final[0]['sha256'].lower() == SAVE_SHA
            assert all(int(x['living']) == 121 for x in h)
            assert sum(int(x['actors']) for x in h) == 7_410_662
            metrics[arm] = {
                'wallMs': round(sum(float(x['wallMs']) for x in h), 3),
                'cpuMs': round(sum(float(x['cpuMs']) for x in h), 3),
                'allocatedBytes': sum(int(x['totalAllocatedBytes']) for x in h),
                'physicalSlices': sum(int(x['slices']) for x in h),
                'historyFinal': int(h[-1]['history']),
                'ledgerFinal': int(h[-1]['ledger'])}
        assert metrics['A']['physicalSlices'] == metrics['B']['physicalSlices']
        assert metrics['A']['historyFinal'] == metrics['B']['historyFinal']
        assert metrics['A']['ledgerFinal'] == metrics['B']['ledgerFinal']
        result = {'zipSha256': ZIP_SHA, 'saveSha256': SAVE_SHA, 'metrics': metrics,
                  'canTurnCalls': totals['canTurnCalls'],
                  'canTurnSharePreferencePct': round(100*totals['canTurnBytes']/totals['preferenceBytes'], 4),
                  'turnWayShareCanTurnPct': round(100*totals['turnWayBytes']/totals['canTurnBytes'], 4),
                  'laneBodyShareCanTurnPct': round(100*totals['laneBodyBytes']/totals['canTurnBytes'], 4),
                  'stageBytes': {k: totals[k] for k in ('preferenceBytes','canTurnBytes','turnWayBytes','laneBodyBytes')},
                  'note': 'intrusive allocation, not throughput gain or proof of avoidability'}
        print('PR57_451_AUDIT_PASS ' + json.dumps(result, sort_keys=True))

if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python3 pr57-451-audit.py ORIGINAL_450.zip')
    audit(Path(sys.argv[1]))
