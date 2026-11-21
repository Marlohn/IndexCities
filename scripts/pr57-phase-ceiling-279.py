#!/usr/bin/env python3
"""PR57 #279: conditional Amdahl ceilings from existing Actions ZIP logs.

Usage: python3 scripts/pr57-phase-ceiling-279.py ARTIFACT.zip [ARTIFACT.zip ...]
No simulation is executed or modified. Logs must contain STEP_PROFILE and
STEP_PHASE from per-call Advance(2) replays.
"""
import argparse
import re
import zipfile
from decimal import Decimal

TARGET_SPS = Decimal("116880")
TARGET_STEP_US = Decimal("2000000") / TARGET_SPS
HEAD = re.compile(r"^STEP_PROFILE steps=(\d+) measuredWall=([0-9.]+)s phases=(\d+)$")
PHASE = re.compile(r"^  STEP_PHASE name=([^ ]+) wall=([0-9.]+)s share=([0-9.]+)%$")


def parse_log(text):
    lines = text.splitlines()
    matches = [(i, HEAD.fullmatch(line)) for i, line in enumerate(lines)]
    matches = [(i, m) for i, m in matches if m]
    if len(matches) != 1:
        raise ValueError(f"expected one STEP_PROFILE, found {len(matches)}")
    pos, match = matches[0]
    steps, wall, count = int(match[1]), Decimal(match[2]), int(match[3])
    phases = {}
    for line in lines[pos + 1:]:
        m = PHASE.fullmatch(line)
        if not m:
            if line.startswith("  STEP_DETAIL") or line.startswith("VEHICLE_PROFILE"):
                break
            continue
        if m[1] in phases:
            raise ValueError(f"duplicate phase {m[1]}")
        phases[m[1]] = Decimal(m[2])
    if len(phases) != count or "vehicles" not in phases:
        raise ValueError(f"incomplete phase list: {len(phases)}/{count}")
    if abs(sum(phases.values()) - wall) > Decimal(".010"):
        raise ValueError("phase sum differs from total by >10ms")
    if wall <= phases["vehicles"] or steps <= 0:
        raise ValueError("invalid phase timings")
    return steps, wall, phases


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("artifacts", nargs="+")
    args = ap.parse_args()
    print(f"Advance(2) target: {TARGET_STEP_US:.4f} us/call; {TARGET_SPS} simulated s/real s")
    print("| ZIP / scenario | Advance(2) calls | Measured total (s) | Vehicles (s) | Nonvehicles (ms/call) | Conditional traffic-only ceiling | Nonvehicle work / 17.1us budget | Dominant nonvehicle phases (ms/call) |")
    print("|---|---:|---:|---:|---:|---:|---:|---|")
    for path in args.artifacts:
        with zipfile.ZipFile(path) as z:
            probes = sorted(n for n in z.namelist() if n.endswith("-probe.log") or n == "probe.log")
            if not probes:
                raise ValueError(f"no probe logs in {path}")
            for name in probes:
                steps, wall, phases = parse_log(z.read(name).decode("utf-8"))
                nonvehicle = wall - phases["vehicles"]
                ms_per_step = nonvehicle * 1000 / steps
                bound = wall / nonvehicle
                budget_multiple = ms_per_step * 1000 / TARGET_STEP_US
                others = sorted(((n, t) for n, t in phases.items() if n != "vehicles"), key=lambda x: -x[1])
                top = ", ".join(f"{n} {t * 1000 / steps:.3f}" for n, t in others[:3])
                label = path.rsplit("/", 1)[-1] + " / " + name.removesuffix("-probe.log")
                print(f"| {label} | {steps} | {wall:.3f} | {phases['vehicles']:.3f} | {ms_per_step:.3f} | {bound:.2f}x | {budget_multiple:.0f}x | {top} |")
    print("Interpretation: zeroing all measured vehicle time, with every other phase unchanged, is an impossible hypothetical. Profiled replays, not optimized A/B, not causal equivalence, and not extrapolations to 10k/80y.")


if __name__ == "__main__":
    main()
