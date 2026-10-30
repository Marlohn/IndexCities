#!/usr/bin/env python3
"""PR57 #404: opt-in direct Vehicle.Route assignment/Clear/Insert witness.

Apply AFTER #399, #400 and #401 in a disposable checkout. Only wraps canonical
source sites (not temporary LaneEntry snapshots); exact Route operations remain
sequential and unchanged. This is a targeted writer census, NOT causal closure.
"""
from pathlib import Path
import re

root = Path("src/IndexCities.Simulation")
files = sorted(p for p in root.glob("*.cs")
               if "Pr57" not in p.name and "PR57" not in p.name
               and p.name not in ("CitySimulation.cs", "Mobility.LaneEntry.cs"))
patterns = [
    ("insert", re.compile(r'\b([A-Za-z_]\w*)\.Route\.Insert\(\s*0\s*,\s*behind\.Id\s*\)')),
    ("clear", re.compile(r'\b([A-Za-z_]\w*)\.Route\.Clear\(\)')),
    ("assign", re.compile(r'\b([A-Za-z_]\w*)\.Route\s*=\s*(?!=)([^;]+);')),
]
counts = {kind: 0 for kind, _ in patterns}
sites = []
for path in files:
    source = path.read_text(encoding="utf-8")
    # Compute original line numbers before replacing, then apply edits in reverse.
    edits = []
    for kind, pattern in patterns:
        for match in pattern.finditer(source):
            line = source.count("\n", 0, match.start()) + 1
            name = match.group(1)
            site = f"{path.name}:{line}:{kind}"
            if kind == "clear":
                replacement = f'Pr57404RouteClear({name},"{site}")'
            elif kind == "insert":
                replacement = f'Pr57404RouteInsert({name},0,behind.Id,"{site}")'
            else:
                replacement = f'Pr57404RouteAssign({name},{match.group(2)},"{site}");'
            edits.append((match.start(), match.end(), replacement, kind, site))
    edits.sort(key=lambda x: x[0])
    for left, right in zip(edits, edits[1:]):
        assert left[1] <= right[0], ("overlapping Route rewrites", path, left, right)
    for start, end, replacement, kind, site in reversed(edits):
        source = source[:start] + replacement + source[end:]
        counts[kind] += 1
        sites.append(site)
    if edits:
        path.write_text(source, encoding="utf-8")

p = root / "CitySimulation.cs"
source = p.read_text(encoding="utf-8")
anchor = '    static readonly bool Pr57401Enabled=Environment.GetEnvironmentVariable("PR57_401_DIRECT_WRITERS")=="1";'
assert source.count(anchor) == 1
helper = """    static readonly bool Pr57404Enabled=Environment.GetEnvironmentVariable("PR57_404_ROUTE_WRITERS")=="1";
    long _pr57404Writes,_pr57404Changes,_pr57404Assigns,_pr57404Clears,_pr57404Inserts,_pr57404Logs;
    void Pr57404Record(Vehicle v,string site,bool changed,int before,int after)
    {
        _pr57404Writes++;
        if(!changed)return;
        _pr57404Changes++;
        if(_pr57404Logs++<200)
            Console.WriteLine($"PR57_404_WRITE step={_pr57399Step} phase={_pr57399Phase} physical={_pr57399Physical} vehicle={v.Id} site={site} oldCount={before} newCount={after} changed=True");
    }
    void Pr57404RouteAssign(Vehicle v,List<int> route,string site)
    {
        if(!Pr57404Enabled||_pr57399Step>1820||!Pr57399Targets.Contains(v.Id)){v.Route=route;return;}
        var previous=v.Route.ToArray();
        v.Route=route;
        _pr57404Assigns++;
        Pr57404Record(v,site,!previous.SequenceEqual(v.Route),previous.Length,v.Route.Count);
    }
    void Pr57404RouteClear(Vehicle v,string site)
    {
        if(!Pr57404Enabled||_pr57399Step>1820||!Pr57399Targets.Contains(v.Id)){v.Route.Clear();return;}
        var before=v.Route.Count;
        v.Route.Clear();
        _pr57404Clears++;
        Pr57404Record(v,site,before>0,before,0);
    }
    void Pr57404RouteInsert(Vehicle v,int index,int node,string site)
    {
        if(!Pr57404Enabled||_pr57399Step>1820||!Pr57399Targets.Contains(v.Id)){v.Route.Insert(index,node);return;}
        var before=v.Route.Count;
        v.Route.Insert(index,node);
        _pr57404Inserts++;
        Pr57404Record(v,site,true,before,v.Route.Count);
    }
"""
source = source.replace(anchor, helper + anchor)
summary_anchor = '        if(Pr57401Enabled&&_pr57399Step==1820)'
assert source.count(summary_anchor) == 1
source = source.replace(summary_anchor,
    '        if(Pr57404Enabled&&_pr57399Step==1820)\n'
    '            Console.WriteLine($"PR57_404_SUMMARY steps={_pr57399Step} physical={_pr57399Physical} writes={_pr57404Writes} changed={_pr57404Changes} assigns={_pr57404Assigns} clears={_pr57404Clears} inserts={_pr57404Inserts} logs={_pr57404Logs} observational=True causalClosure=False noSkippedSlices=True");\n'
    + summary_anchor)
p.write_text(source, encoding="utf-8")
assert counts["assign"]>20 and counts["clear"]>10 and counts["insert"]>=1, counts
print("PR57_404_INJECTED", counts, "sites", len(sites),
      "observational=True canonicalRouteOnly=True")
