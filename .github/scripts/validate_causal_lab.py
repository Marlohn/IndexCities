#!/usr/bin/env python3
"""Minimum evidence gate, emphatically NOT certification of 10k x 80y."""
import json
import math
import sys
from pathlib import Path

p = Path(sys.argv[1])
if not p.is_file():
    sys.exit("Missing integrated run output JSON")
report = json.loads(p.read_text())
fields = ("seed", "simulated_days", "min_simultaneous_population",
          "active_trips", "economic_transactions", "infrastructure_updates",
          "historical_facts", "physics_collisions_invalid", "conservation_errors")
for key in fields:
    if key not in report:
        sys.exit(f"Missing actual measurement: {key}")
    value = report[key]
    if type(value) not in (int, float) or not math.isfinite(value):
        sys.exit(f"Non-numeric/non-finite measurement: {key}")
if report["seed"] != 42 or report["simulated_days"] < 1 or report["min_simultaneous_population"] < 400:
    sys.exit("Duration/population not representative of the 400-SIM one-day gate")
if any(report[k] <= 0 for k in ("active_trips", "economic_transactions", "infrastructure_updates", "historical_facts")):
    sys.exit("Mobility, economy, infrastructure and history must all be active")
if report["physics_collisions_invalid"] != 0 or report["conservation_errors"] != 0:
    sys.exit("Failed conservation/physics invariants")
print("CausalLab 400-SIM one-day evidence gate passed; NOT a 10k/80-year result.")
