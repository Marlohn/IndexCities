#!/usr/bin/env python3
"""PR57 #346: observe exclusive actor stages in three authentic REAL jam windows.

Checkout-only injection. Reuses the canonical #206 nine-stage stopwatch and
#244 state grouping; does not change vehicle decisions, writers, or sub-slices.
The late samples are physical sub-slice ordinals, not exact clock boundaries.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
path = root / "src/IndexCities.Simulation/Mobility.ResidualExclusive.Pr57.cs"
source = path.read_text(encoding="utf-8")
old = "if (_pr57ExclusiveEligibleSlices < 37 || _pr57ExclusiveEligibleSlices > 39) return;"
new = """if (!((_pr57ExclusiveEligibleSlices >= 40001 && _pr57ExclusiveEligibleSlices <= 40003)
            || (_pr57ExclusiveEligibleSlices >= 50001 && _pr57ExclusiveEligibleSlices <= 50003)
            || (_pr57ExclusiveEligibleSlices >= 65001 && _pr57ExclusiveEligibleSlices <= 65003))) return;"""
assert source.count(old) == 1, "Unexpected #206 sampling anchor; do not silently patch"
assert "scope=ordered-vehicle-loop only" in source
assert "PR57_244_STATE" in source
path.write_text(source.replace(old, new, 1), encoding="utf-8")
print("PR57_346_INJECTED: 9 late physical slices, observational stage census only")
