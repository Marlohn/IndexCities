#!/usr/bin/env python3
"""PR57 #280: conditional three-phase floor and one-time cold-crosswalk correction.

Usage: python3 scripts/pr57-multiphase-cold-ceiling-280.py action-job.log [...]
Input: text GitHub Actions *job* logs (timestamps allowed), not artifact ZIPs.
No simulation or state modification.
"""
import argparse
import re
from decimal import Decimal
from pathlib import Path

TARGET_US = Decimal(2000000) / Decimal(116880)
PROFILE = re.compile(r"STEP_PROFILE steps=(\d+) measuredWall=([0-9.]+)s phases=(\d+)")
PHASE = re.compile(r"STEP_PHASE name=([^ ]+) wall=([0-9.]+)s share=")
COLD = re.compile(r"PR57_239_CITIZEN name=update-crosswalks-cold calls=(\d+) ms=([0-9.]+)")


def parse(text):
    rows = []
    for line in text.splitlines():
        if m := PROFILE.search(line):
            rows.append({"steps": int(m[1]), "wall": Decimal(m[2]),
                         "expected": int(m[3]), "phases": {}, "cold": None})
        elif rows and (m := PHASE.search(line)):
            row = rows[-1]
            if m[1] in row["phases"]:
                raise ValueError(f"duplicate phase: {m[1]}")
            row["phases"][m[1]] = Decimal(m[2])
        elif rows and (m := COLD.search(line)):
            if rows[-1]["cold"] is not None:
                raise ValueError("duplicate cold setup counter")
            rows[-1]["cold"] = (int(m[1]), Decimal(m[2]))
    if not rows:
        raise ValueError("no STEP_PROFILE found")
    for row in rows:
        phases = row["phases"]
        if len(phases) != row["expected"] or row["expected"] != 13:
            raise ValueError(f"expected 13 complete phases; found {len(phases)}")
        if not {"vehicles", "citizen-mobility"} <= phases.keys():
            raise ValueError("missing mandatory phases")
        if abs(sum(phases.values()) - row["wall"]) > Decimal(".010"):
            raise ValueError("rounded phase sum differs by more than 10ms")
        if row["steps"] <= 0:
            raise ValueError("invalid step count")
    return rows


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("job_logs", nargs="+", type=Path)
    args = ap.parse_args()
    print(f"Target: {TARGET_US:.4f} us per Advance(2). Every removal below is an impossible conditional oracle.")
    print("| Log / profile | Steps | Third-largest phase after vehicles/citizens | Residual after zeroing all three (ms/call) | Residual / target | Cold-crosswalk adjusted nonvehicle (ms/call) |")
    print("|---|---:|---|---:|---:|---:|")
    for path in args.job_logs:
        for i, row in enumerate(parse(path.read_text(encoding="utf-8")), 1):
            phases = row["phases"]
            third = max((name for name in phases if name not in ("vehicles", "citizen-mobility")),
                        key=lambda name: phases[name])
            residual = row["wall"] - phases["vehicles"] - phases["citizen-mobility"] - phases[third]
            if residual <= 0:
                raise ValueError("nonpositive residual")
            ms = residual * 1000 / row["steps"]
            multiple = ms * 1000 / TARGET_US
            cold = row["cold"]
            cold_adjusted = "not measured"
            if cold is not None:
                if cold[0] != 1:
                    raise ValueError("cold initialization was not executed exactly once")
                corrected = row["wall"] - phases["vehicles"] - cold[1] / 1000
                if corrected < 0:
                    raise ValueError("invalid cold correction")
                cold_adjusted = f"{corrected * 1000 / row['steps']:.3f}"
            print(f"| {path.name} / {i} | {row['steps']} | {third} | {ms:.3f} | {multiple:.0f}x | {cold_adjusted} |")
    print("Cold correction subtracts one observed setup, not other JIT/GC/restore costs.")
    print("Intrusively profiled phases: not an optimized A/B or a bound on future 10k states.")


if __name__ == "__main__":
    main()
