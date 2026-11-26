#!/usr/bin/env python3
"""PR57 #511: authentic three-regime Amdahl ceiling for a four-phase workset.

Usage: python3 pr57-511-crossphase-amdahl.py pr57-493-original.zip

Reuses the original #493 ABBA ZIP, does not benchmark an optimization. The
four-phase set is maintenance + infrastructure + businesses + needs. Even if
all its observed work became free, this is only a per-window, intrusive
upper bound. It is not a proof of causal interchangeability, nor a 10k forecast.
"""
import hashlib
import json
from pathlib import Path
import re
import sys
import zipfile

EXPECTED_SHA256 = 'f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
PHASES = ('maintenance', 'infrastructure', 'businesses', 'needs')
CASES = {'congestedREAL121': (100, False), 'worksREAL47': (100, False),
         'GRID_ARTIFICIAL': (50, True)}
ARMS = ('A1', 'B1', 'B2', 'A2')
BUDGET_US = 2 * 6 * 3600 / (80 * 365.25 * 86400) * 1e6


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def parse_profile(log, expected_steps, case, arm):
    step = re.search(r'^STEP_PROFILE steps=(\d+) measuredWall=([0-9.]+)s phases=(\d+)$',
                     log, re.M)
    require(step is not None, f'{case}/{arm}: STEP_PROFILE missing')
    steps, total, count = int(step[1]), float(step[2]), int(step[3])
    require(steps == expected_steps and count == 13, f'{case}/{arm}: steps/phases')
    require(re.search(rf'^WINDOW 1 seconds={expected_steps * 2}\b', log, re.M),
            f'{case}/{arm}: wrong simulated horizon')
    phase_rows = re.findall(r'^  STEP_PHASE name=([^ ]+) wall=([0-9.]+)s share=([0-9.]+)%$', log, re.M)
    require(len(phase_rows) == 13, f'{case}/{arm}: 13 phase rows expected')
    phases = {name: float(seconds) for name, seconds, _ in phase_rows}
    require(len(phases) == 13 and all(name in phases for name in PHASES + ('vehicles',)),
            f'{case}/{arm}: missing or duplicate phase')
    # Each printed phase is rounded to 1ms. The total is also rounded.
    require(abs(sum(phases.values()) - total) <= 0.009,
            f'{case}/{arm}: printed phases do not reconcile')
    four = sum(phases[name] for name in PHASES)
    require(0 < four < total, f'{case}/{arm}: invalid Amdahl share')
    return dict(total=total, four=four, share=four / total,
                ceiling=total / (total - four),
                residual_us=(total - four) * 1e6 / steps,
                residual_vs_budget=(total - four) * 1e6 / steps / BUDGET_US,
                vehicle=phases['vehicles'], citizen=phases['citizen-mobility'])


def main(path):
    data = Path(path).read_bytes()
    require(hashlib.sha256(data).hexdigest() == EXPECTED_SHA256, 'ZIP SHA256 mismatch')
    with zipfile.ZipFile(path) as z:
        require(z.testzip() is None, 'ZIP CRC mismatch')
        names = set(z.namelist())
        require(len(names) == 37, f'ZIP members: {len(names)} != 37')
        summary = json.loads(z.read('summary.json'))
        require(summary['experiment'] == 493, 'wrong experiment')
        require(summary['cadence'] == 'one public Advance(2) per quantum', 'cadence')
        require(summary['comparison'] == 'same composed flags, fresh processes ABBA', 'comparison')
        require(set(summary['cases']) == set(CASES), 'case mismatch')
        require(not summary['gainAccepted'] and summary['observational'], 'not an observational baseline')
        for case, (steps, artificial) in CASES.items():
            meta = summary['cases'][case]
            require(meta['stepsPerArm'] == steps and meta['artificial'] == artificial
                    and meta['fullJsonParity'], f'{case}: metadata mismatch')
            require(set(meta['arms']) == set(ARMS), f'{case}: arms')
            profiles = []
            initial, final = set(), set()
            for arm in ARMS:
                prefix = f'{case}/{arm}/'
                log = z.read(prefix + 'full.log').decode('utf-8')
                require('INVARIANTS OK' in log, f'{case}/{arm}: invariants')
                require(log.startswith('FIXTURE '), f'{case}/{arm}: fixture')
                profiles.append(parse_profile(log, steps, case, arm))
                record = meta['arms'][arm]
                for index, hashes in ((0, initial), (1, final)):
                    raw = z.read(prefix + f'checkpoint-{index}.json')
                    digest = hashlib.sha256(raw).hexdigest()
                    require(digest == record['initialSha256' if index == 0 else 'finalSha256'],
                            f'{case}/{arm}: checkpoint hash')
                    hashes.add(digest)
                require(record['probe'] == arm.startswith('B'), f'{case}/{arm}: probe arm')
            require(len(initial) == len(final) == 1, f'{case}: ABBA save parity')
            # No pooled wall time: bounds are calculated separately per process.
            ceilings = [x['ceiling'] for x in profiles]
            residual = [x['residual_vs_budget'] for x in profiles]
            print(f'{case} label={"ARTIFICIAL" if artificial else "REAL"} '
                  f'stepsPerArm={steps} fourPhaseSharePct='
                  f'{min(x["share"] for x in profiles)*100:.2f}..'
                  f'{max(x["share"] for x in profiles)*100:.2f} '
                  f'optimisticCeiling={min(ceilings):.4f}..{max(ceilings):.4f}x '
                  f'nonFourPhaseResidualUsPerStep='
                  f'{min(x["residual_us"] for x in profiles):.0f}..'
                  f'{max(x["residual_us"] for x in profiles):.0f} '
                  f'residualVsTarget={min(residual):.0f}..{max(residual):.0f}x '
                  f'fullJsonParity=True')
            require(max(ceilings) < 1.12, f'{case}: unexpected ceiling')
        print(f'BUDGET_US_PER_ADVANCE2={BUDGET_US:.6f}')
        print('PR57_511_AUTHENTIC_CROSSPHASE_AMDAHL_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: pr57-511-crossphase-amdahl.py pr57-493-original.zip')
    main(sys.argv[1])
