#!/usr/bin/env python3
"""#505: sample physical read graphs throughout the authentic REAL window.

Patch only the disposable #504-generated C# probe. A read graph is NOT a
causal independence proof. Net persisted differences miss transient writers.
"""
from pathlib import Path
p=Path("src/IndexCities.Simulation/Pr57504ReadGraph.CiOnly.cs")
s=p.read_text()
old="_pr57504Active=Pr57504Enabled&&_pr57504Samples<3&&dt>0&&"
new="_pr57504Active=Pr57504Enabled&&_pr57504Samples<600&&dt>0&&"
assert s.count(old)==1
s=s.replace(old,new)
old='Console.WriteLine($"PR57_504_READ_GRAPH sample='
new='if(changed.Count>0||_pr57504Samples==1||_pr57504Samples%97==0) Console.WriteLine($"PR57_505_READ_GRAPH sample='
assert s.count(old)==1
s=s.replace(old,new)
p.write_text(s)
print("PR57_505_LATE_PHYSICAL_READ_GRAPH_INJECTION_PASS")
