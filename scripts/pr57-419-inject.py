#!/usr/bin/env python3
"""PR57 #419: combine the exact-site #417 tow hooks with the #418 natural dispatch witness.

Only changes the disposable CI checkout. No production changes are committed.
The fixture is synthetic post-repair; dispatch and consumption use public Advance(2).
"""
import subprocess
from pathlib import Path

subprocess.run(["python3", "scripts/pr57-417-inject.py"], check=True)
subprocess.run(["python3", "scripts/pr57-418-inject.py"], check=True)

# The #417 REAL census prints its summary after 10,800 Steps. This focused
# test ends at Step 20, so shorten only the observational reporting horizon.
path = Path("src/IndexCities.Simulation/CitySimulation.cs")
source = path.read_text()
before = "if(Pr57417Enabled&&_pr57417Step==10800)Pr57417Summary();"
after = "if(Pr57417Enabled&&_pr57417Step==20)Pr57417Summary();"
assert source.count(before) == 1, "unexpected #417 summary callsite"
path.write_text(source.replace(before, after, 1))
print("PR57_419_INJECTED exactSite=True syntheticPostRepair=True productionCommitted=False")
