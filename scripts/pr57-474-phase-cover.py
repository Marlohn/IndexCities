#!/usr/bin/env python3
"""PR57 #474: optimistic whole-phase Amdahl coverage, two authentic REAL runs.

Usage: python3 scripts/pr57-474-phase-cover.py pr57-439-original.zip pr57-445-original.zip
No engine changes, no synthetic population, no claim that any phase is skippable.
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

SOURCES = {
    439: ('0e16ec8569d9ecc203c9a3ba02fc594f4a54097f0cde0f2264e021152dc4f90e', 10800),
    445: ('ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a', 100),
}
# 80 * 365.25 days, 2-second public Advance quantum, 6h wall budget.
BUDGET_US = 6 * 3600 * 1e6 / (80 * 365.25 * 86400 / 2)


def read_verified(path, experiment):
    data = Path(path).read_bytes()
    assert hashlib.sha256(data).hexdigest() == SOURCES[experiment][0]
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        if experiment == 439:
            summary = json.loads(z.read('summary.json'))
            assert summary['advance2PerArm'] == 10800
            assert z.read('base/checkpoint-10800.json') == z.read('shadow/checkpoint-10800.json')
            phases = summary['arms']['base']['phaseMs']
            assert len(phases) == 13
            assert abs(sum(phases.values()) - summary['arms']['base']['phaseTotalMs']) < 0.002
            assert summary['arms']['base']['wallMs'] >= sum(phases.values())
            return phases, 0.0005  # phaseMs rounded to 0.001 ms
        summary = json.loads(z.read('pr57-445/summary.json'))
        assert summary['allAdvance2'] and summary['fullJsonParity']
        assert summary['result']['A']['callsAdvance2'] == 100
        assert z.read('pr57-445/A/checkpoint-0.json') == z.read('pr57-445/B/checkpoint-0.json')
        assert z.read('pr57-445/A/checkpoint-1.json') == z.read('pr57-445/B/checkpoint-1.json')
        log = z.read('pr57-445/A/full.log').decode()
        assert 'STEP_PROFILE steps=100' in log
        pairs = re.findall(r'^\s*STEP_PHASE name=([^ ]+) wall=([0-9.]+)s', log, re.M)
        assert len(pairs) == len(set(name for name, _ in pairs)) == 13
        return {name: float(sec) * 1000 for name, sec in pairs}, 0.5  # printed to 0.001 s


def report(name, phases, steps, rounding_ms):
    ranked = sorted(phases.items(), key=lambda kv: (-kv[1], kv[0]))
    total = sum(phases.values())
    target_ms = BUDGET_US * steps / 1000
    def remaining(k):
        return sum(value for _, value in ranked[k:])
    def lower_remaining(k):
        return sum(max(0, value - rounding_ms) for _, value in ranked[k:])
    # Conservative lower bound on the number of whole phases made *free*.
    impossible = [k for k in range(14) if lower_remaining(k) > target_ms]
    minimum = max(impossible) + 1
    print(f'{name} steps={steps} profiledMs={total:.3f} budgetMs={target_ms:.6f} '
          f'profiledUsPerAdvance={total*1000/steps:.3f} guaranteedMinimumFreePhases={minimum}')
    for k in [0, 1, 2, 3, 4, 5, 6, 7, 8]:
        if k > 13: break
        left = remaining(k)
        print(f'  freeTop={k} remainingMs={left:.3f} speedupCeiling={total/left if left else float("inf"):.3f} '
              f'budgetMultiple={left/target_ms:.3f} lowerRemainingMs={lower_remaining(k):.3f}')
    print('  topOrder=' + ','.join(name for name, _ in ranked))
    return ranked, minimum


def main(a, b):
    assert 17.10 < BUDGET_US < 17.12
    x, rx = read_verified(a, 439)
    y, ry = read_verified(b, 445)
    assert set(x) == set(y)
    topx, mx = report('REAL_CONGESTED_439', x, 10800, rx)
    topy, my = report('REAL_WORKS_445', y, 100, ry)
    assert mx == 7 and my == 8
    assert set(k for k, _ in topx[:7]) & set(k for k, _ in topy[:8]) == {
        'vehicles', 'citizen-mobility', 'infrastructure', 'needs', 'businesses', 'maintenance'}
    print('PR57_474_PHASE_COVER_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 3:
        raise SystemExit('usage: pr57-474-phase-cover.py #439.zip #445.zip')
    main(*sys.argv[1:])
