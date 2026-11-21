#!/usr/bin/env python3
"""PR57 #512: authentic mobility/construction boundary evidence and Amdahl.

Usage: python3 scripts/pr57-512-causal-boundary-audit.py pr57-493-original.zip

Observational: no simulation run or modification. Endpoints show persisted net
changes, not the timing of writers, transient mutations, or per-Step causality.
The C# source anchors cited in the PR comment establish ordered dependencies.
"""
import hashlib
import json
from pathlib import Path
import re
import sys
import zipfile

EXPECTED_ZIP = 'f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
CASES = {'congestedREAL121': (100, False), 'worksREAL47': (100, False),
         'GRID_ARTIFICIAL': (50, True)}
ARMS = ('A1', 'B1', 'B2', 'A2')
HOT = ('vehicles', 'citizen-mobility', 'construction', 'construction-dispatch')
PHASES = ('vehicles', 'paving-returns', 'citizen-mobility', 'construction',
          'maintenance', 'relocation', 'infrastructure', 'businesses', 'needs',
          'economy-review', 'construction-dispatch', 'market-followups',
          'demolition-finance-loans')
BUDGET_US = 2 * 6 * 3600 / (80 * 365.25 * 86400) * 1e6
EXPECTED_REAL = {
    'congestedREAL121': {'vehiclePositions': 9, 'citizenPositions': 41,
                         'projectCauseOnly': 0, 'projectDelivered': 0,
                         'projectProgress': 0, 'newShipments': 1, 'newLedger': 2},
    'worksREAL47': {'vehiclePositions': 12, 'citizenPositions': 15,
                    'projectCauseOnly': 60, 'projectDelivered': 6,
                    'projectProgress': 1, 'newShipments': 13, 'newLedger': 15},
}


def check(ok, msg):
    if not ok:
        raise AssertionError(msg)


def phase_times(log, case, arm, steps):
    match = re.search(r'^STEP_PROFILE steps=(\d+) measuredWall=([0-9.]+)s phases=(\d+)$', log, re.M)
    check(match is not None, f'{case}/{arm}: missing profile')
    check(int(match[1]) == steps and int(match[3]) == 13, f'{case}/{arm}: profile shape')
    check(re.search(rf'^WINDOW 1 seconds={2 * steps}\b', log, re.M), f'{case}/{arm}: cadence')
    rows = re.findall(r'^  STEP_PHASE name=([^ ]+) wall=([0-9.]+)s share=([0-9.]+)%$', log, re.M)
    check(tuple(name for name, _, _ in rows) == PHASES, f'{case}/{arm}: phase order')
    phases = {name: float(seconds) for name, seconds, _ in rows}
    total = float(match[2])
    check(abs(sum(phases.values()) - total) <= .009, f'{case}/{arm}: rounding mismatch')
    hot = sum(phases[p] for p in HOT)
    residual = total - hot
    check(hot > 0 and residual > 0, f'{case}/{arm}: invalid bound')
    return {'share': hot / total, 'ceiling': total / residual,
            'residualUs': residual * 1e6 / steps}


def by_id(rows):
    return {row['Id']: row for row in rows}


def persisted_deltas(initial, final):
    before_v, after_v = by_id(initial['Vehicles']), by_id(final['Vehicles'])
    before_c, after_c = by_id(initial['Citizens']), by_id(final['Citizens'])
    before_p, after_p = by_id(initial['Projects']), by_id(final['Projects'])
    check(before_v.keys() <= after_v.keys() and before_p.keys() <= after_p.keys(),
          'unexpected vehicle/project removal in this window')
    check(before_c.keys() <= after_c.keys(), 'unexpected citizen removal in this window')
    def changed(a, b, field):
        return sum(a[i].get(field) != b[i].get(field) for i in a.keys() & b.keys())
    only_cause = sum({k for k in before_p[i].keys() | after_p[i].keys()
                      if before_p[i].get(k) != after_p[i].get(k)} == {'Cause'}
                     for i in before_p.keys() & after_p.keys())
    return {'vehiclePositions': changed(before_v, after_v, 'Position'),
            'citizenPositions': changed(before_c, after_c, 'Position'),
            'projectCauseOnly': only_cause,
            'projectDelivered': changed(before_p, after_p, 'Delivered'),
            'projectProgress': changed(before_p, after_p, 'ProgressUnits'),
            'newShipments': len(final['Shipments']) - len(initial['Shipments']),
            'newLedger': len(final['Ledger']) - len(initial['Ledger'])}


def main(path):
    raw = Path(path).read_bytes()
    check(hashlib.sha256(raw).hexdigest() == EXPECTED_ZIP, 'original ZIP SHA-256')
    with zipfile.ZipFile(path) as z:
        check(z.testzip() is None, 'ZIP CRC')
        check(len(z.namelist()) == 37, 'ZIP member count')
        summary = json.loads(z.read('summary.json'))
        check(summary['experiment'] == 493, 'experiment')
        check(summary['cadence'] == 'one public Advance(2) per quantum', 'cadence')
        check(summary['comparison'] == 'same composed flags, fresh processes ABBA', 'comparison')
        check(set(summary['cases']) == set(CASES), 'cases')
        check(summary['observational'] and not summary['gainAccepted'], 'observational only')
        for case, (steps, artificial) in CASES.items():
            meta = summary['cases'][case]
            check(meta['stepsPerArm'] == steps and meta['artificial'] == artificial
                  and meta['fullJsonParity'], f'{case}: case metadata')
            check(set(meta['arms']) == set(ARMS), f'{case}: arms')
            profiles, deltas, initial_hashes, final_hashes = [], [], set(), set()
            for arm in ARMS:
                prefix = f'{case}/{arm}/'
                log = z.read(prefix + 'full.log').decode('utf-8')
                check('INVARIANTS OK' in log, f'{case}/{arm}: invariants')
                profiles.append(phase_times(log, case, arm, steps))
                record = meta['arms'][arm]
                check(record['probe'] == arm.startswith('B'), f'{case}/{arm}: probe')
                states = []
                for i, hashes in ((0, initial_hashes), (1, final_hashes)):
                    data = z.read(prefix + f'checkpoint-{i}.json')
                    digest = hashlib.sha256(data).hexdigest()
                    check(digest == record['initialSha256' if i == 0 else 'finalSha256'],
                          f'{case}/{arm}: checkpoint digest')
                    hashes.add(digest)
                    states.append(json.loads(data))
                deltas.append(persisted_deltas(*states))
            check(len(initial_hashes) == len(final_hashes) == 1, f'{case}: ABBA full save parity')
            check(all(d == deltas[0] for d in deltas), f'{case}: delta parity')
            if not artificial:
                check(deltas[0] == EXPECTED_REAL[case], f'{case}: unexpected persisted deltas {deltas[0]}')
            shares = [p['share'] * 100 for p in profiles]
            ceilings = [p['ceiling'] for p in profiles]
            residuals = [p['residualUs'] for p in profiles]
            # Printed total and four phase clocks are rounded to 1 ms each.
            # Lower bound subtracts 2.5 ms of possible residual rounding error.
            conservative = min(residuals) - 2500 / steps
            check(conservative > 0, f'{case}: invalid conservative residual')
            print(f'{case} type={"ARTIFICIAL" if artificial else "REAL"} steps={steps} '
                  f'fourPhaseSharePct={min(shares):.2f}..{max(shares):.2f} '
                  f'impossibleCeiling={min(ceilings):.2f}..{max(ceilings):.2f}x '
                  f'otherNinePhasesUsPerAdvance2={min(residuals):.0f}..{max(residuals):.0f} '
                  f'conservativeResidualVsBudget={conservative / BUDGET_US:.1f}x '
                  f'persistedNetDelta={json.dumps(deltas[0], sort_keys=True)} '
                  f'fullSaveABBA=True')
        print(f'BUDGET_US_PER_ADVANCE2={BUDGET_US:.6f}')
        print('NOTE: endpoint deltas do not certify per-Step writer order or independence')
        print('PR57_512_AUTHENTIC_CAUSAL_BOUNDARY_AUDIT_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-512-causal-boundary-audit.py pr57-493-original.zip')
    main(sys.argv[1])
