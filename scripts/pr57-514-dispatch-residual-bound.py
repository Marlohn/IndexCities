#!/usr/bin/env python3
"""PR57 #514: bound fleet-only optimization from authentic integrated #493.

Usage: python3 scripts/pr57-514-dispatch-residual-bound.py pr57-493-original.zip

This is an intentionally impossible upper bound, NOT a proposed cache or an
end-to-end optimization. The dispatch residual includes all non-timed work,
including routing, stock, payments, and fleet guards. No per-Step causality
or sustainable 10k/80y performance is inferred from endpoint observations.
"""
import hashlib
import json
from pathlib import Path
import re
import sys
import zipfile

EXPECTED_SHA = 'f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
ARMS = ('A1', 'B1', 'B2', 'A2')
CASES = ('worksREAL47', 'congestedREAL121', 'GRID_ARTIFICIAL')
PHASES = ('vehicles', 'paving-returns', 'citizen-mobility', 'construction',
          'maintenance', 'relocation', 'infrastructure', 'businesses', 'needs',
          'economy-review', 'construction-dispatch', 'market-followups',
          'demolition-finance-loans')
BUDGET_US = 2 * 6 * 3600 / (80 * 365.25 * 86400) * 1e6


def row(pattern, log, name):
    match = re.search(pattern, log, re.M)
    assert match is not None, name
    return match


def audit(path):
    raw = Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == EXPECTED_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 37
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 493
        assert summary['cadence'] == 'one public Advance(2) per quantum'
        assert summary['comparison'] == 'same composed flags, fresh processes ABBA'
        assert summary['observational'] and not summary['gainAccepted']
        for case in CASES:
            meta = summary['cases'][case]
            assert meta['fullJsonParity']
            steps = 50 if case == 'GRID_ARTIFICIAL' else 100
            assert meta['stepsPerArm'] == steps
            results, starts, ends = [], set(), set()
            for arm in ARMS:
                prefix = f'{case}/{arm}/'
                log = z.read(prefix + 'full.log').decode('utf-8')
                assert 'INVARIANTS OK' in log
                assert re.search(rf'^WINDOW 1 seconds={2 * steps}\b', log, re.M)
                total = float(row(r'^STEP_PROFILE steps=(\d+) measuredWall=([0-9.]+)s phases=(\d+)$', log, 'profile')[2])
                assert int(row(r'^STEP_PROFILE steps=(\d+)', log, 'steps')[1]) == steps
                phases = re.findall(r'^  STEP_PHASE name=([^ ]+) wall=([0-9.]+)s share=([0-9.]+)%$', log, re.M)
                assert tuple(name for name, _, _ in phases) == PHASES
                dispatch = float(dict((name, sec) for name, sec, _ in phases)['construction-dispatch'])
                detail = row(r'^PR57_DISPATCH_DETAIL steps=(\d+) reserveCalls=(\d+) reserveWall=([0-9.]+)s accessCalls=(\d+) accessWall=([0-9.]+)s crewCalls=(\d+) crewWall=([0-9.]+)s measuredDispatch=([0-9.]+)s accountedShare=([0-9.]+)%$', log, 'dispatch detail')
                assert int(detail[1]) == steps
                timed = sum(float(detail[i]) for i in (3, 5, 7))
                measured = float(detail[8])
                assert abs(dispatch - measured) <= 0.000501
                residual = measured - timed
                assert residual >= 0 and total > measured
                assert abs(100 * timed / measured - float(detail[9])) < 0.025
                census = row(r'^PR57_CONSTRUCTION_CENSUS steps=(\d+) orderProjects=(\d+) progressProjects=(\d+) reserveProjects=(\d+) workAccess=(\d+) buildingAccess=(\d+)', log, 'census')
                assert int(census[1]) == steps
                productivity = row(r'^PR57_DISPATCH_PRODUCTIVITY activeSteps=(\d+) shipmentChangedSteps=(\d+) shipmentNetDelta=(\d+) ledgerChangedSteps=(\d+) ledgerNetDelta=(\d+) noShipmentOrLedgerSteps=(\d+)', log, 'productivity')
                for i, target in ((0, starts), (1, ends)):
                    data = z.read(prefix + f'checkpoint-{i}.json')
                    digest = hashlib.sha256(data).hexdigest()
                    target.add(digest)
                    assert digest == meta['arms'][arm]['initialSha256' if i == 0 else 'finalSha256']
                results.append(dict(total=total, dispatch=measured, timed=timed,
                                    residual=residual, order=int(census[2]),
                                    progress=int(census[3]), reserve=int(census[4]),
                                    access=int(detail[4]), work_access=int(census[5]), crew=int(detail[6]),
                                    no_shipment_ledger=int(productivity[6]),
                                    active=int(productivity[1])))
            assert len(starts) == len(ends) == 1, case
            if case == 'worksREAL47':
                assert all(r['order'] == r['progress'] == r['access'] == 6800
                           and r['reserve'] == 34000 and r['work_access'] == 13620 and r['crew'] == 6800
                           and r['no_shipment_ledger'] == 91 and r['active'] == 100
                           for r in results)
            else:
                assert all(r['order'] == r['reserve'] == r['crew'] == 0 for r in results)
            # Even the *entire* untimed dispatch residual cannot be attributed
            # to AvailableTruck; this ceiling is intentionally overgenerous.
            fleet_upper = [1 / (1 - r['residual'] / r['total']) for r in results]
            all_dispatch_upper = [1 / (1 - r['dispatch'] / r['total']) for r in results]
            residual_us = [r['residual'] * 1e6 / steps for r in results]
            print(json.dumps({
                'case': case, 'type': 'ARTIFICIAL' if case == 'GRID_ARTIFICIAL' else 'REAL',
                'steps_per_arm': steps, 'full_save_ABBA': True,
                'dispatch_ms_per_100_steps': [round(r['dispatch'] * 1000, 3) for r in results] if steps == 100 else None,
                'timed_reserve_access_crew_share_pct': [round(100 * r['timed'] / r['dispatch'], 2) for r in results],
                'untimed_dispatch_residual_us_per_Advance2': [round(x, 2) for x in residual_us],
                'max_speedup_if_entire_untimed_dispatch_free': [round(x, 4) for x in fleet_upper],
                'max_speedup_if_all_dispatch_free': [round(x, 4) for x in all_dispatch_upper],
                'max_fleet_only_speedup_bound': round(max(fleet_upper), 4) if results[0]['order'] else 1.0,
                'order_project_visits': results[0]['order'],
                'reserve_project_visits': results[0]['reserve'],
                'active_dispatch_steps': results[0]['active'],
                'no_shipment_or_ledger_steps': results[0]['no_shipment_ledger'] if results[0]['active'] else None,
                'note': 'upper bound from intrusive 13-phase profiles, not an observed optimization; no-shipment/ledger does not mean no writes'
            }, sort_keys=True))
    print(f'BUDGET_US_PER_ADVANCE2={BUDGET_US:.6f}')
    print('PR57_514_AUTHENTIC_DISPATCH_RESIDUAL_BOUND_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit('usage: pr57-514-dispatch-residual-bound.py pr57-493-original.zip')
    audit(sys.argv[1])
