#!/usr/bin/env python3
"""PR57 #401: attribute Route mutations at direct writer sites, not via roster scans.

Apply AFTER #399 and #400 only in a disposable checkout. Route writes remain
canonical and sequential; the opt-in probe compares exact content at three
specific assignment sites. This is NOT an exhaustive mutation journal.
"""
from pathlib import Path

root = Path("src/IndexCities.Simulation")
files = {name: (root / name).read_text(encoding="utf-8")
         for name in ("CitySimulation.cs", "Mobility.cs", "Economy.cs")}

def patch(name, old, new, expected=1):
    count = files[name].count(old)
    assert count == expected, (name, old[:120], count, expected)
    files[name] = files[name].replace(old, new)

patch("CitySimulation.cs",
    '    static readonly bool Pr57400Enabled=Environment.GetEnvironmentVariable("PR57_400_RESTOCK_WRITERS")=="1";',
    """    static readonly bool Pr57401Enabled=Environment.GetEnvironmentVariable("PR57_401_DIRECT_WRITERS")=="1";
    long _pr57401Writes,_pr57401Changed,_pr57401Normal,_pr57401Station,_pr57401Import;
    long _pr57401Targets,_pr57401Ticks;
    void Pr57401RouteWrite(Vehicle v,List<int> route,string site)
    {
        if(!Pr57401Enabled||_pr57399Step>1820){v.Route=route;return;}
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var changed=!v.Route.SequenceEqual(route);
        _pr57401Writes++;
        if(changed)
        {
            _pr57401Changed++;
            if(site=="EnsureFuel.normal")_pr57401Normal++;
            else if(site=="EnsureFuel.station")_pr57401Station++;
            else if(site=="OrderImport")_pr57401Import++;
            else throw new InvalidOperationException("PR57 #401 unknown writer site");
            if(Pr57399Targets.Contains(v.Id))
            {
                _pr57401Targets++;
                Console.WriteLine($"PR57_401_WRITE step={_pr57399Step} phase={_pr57399Phase} physical={_pr57399Physical} vehicle={v.Id} site={site} oldCount={v.Route.Count} newCount={route.Count} changed=True");
            }
        }
        v.Route=route;
        _pr57401Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
    }
    static readonly bool Pr57400Enabled=Environment.GetEnvironmentVariable("PR57_400_RESTOCK_WRITERS")=="1";""")

patch("Mobility.cs",
    "v.Route=normal;v.RouteIndex=0;return true;",
    'Pr57401RouteWrite(v,normal,"EnsureFuel.normal");v.RouteIndex=0;return true;')
patch("Mobility.cs",
    "v.Route=toStation;v.RouteIndex=0;return true;",
    'Pr57401RouteWrite(v,toStation,"EnsureFuel.station");v.RouteIndex=0;return true;')
patch("Economy.cs",
    "vehicle.Route=route;vehicle.RouteIndex=0;vehicle.DockPosition=DockAt(n,destination,false);",
    'Pr57401RouteWrite(vehicle,route,"OrderImport");vehicle.RouteIndex=0;vehicle.DockPosition=DockAt(n,destination,false);')

patch("CitySimulation.cs",
    "        if(Pr57400Enabled&&_pr57399Step==1820)",
    """        if(Pr57401Enabled&&_pr57399Step==1820)
            Console.WriteLine($"PR57_401_SUMMARY steps={_pr57399Step} physical={_pr57399Physical} writes={_pr57401Writes} changed={_pr57401Changed} unchanged={_pr57401Writes-_pr57401Changed} normal={_pr57401Normal} station={_pr57401Station} import={_pr57401Import} targetChanges={_pr57401Targets} ticks={_pr57401Ticks} frequency={System.Diagnostics.Stopwatch.Frequency} observational=True causalClosure=False noSkippedSlices=True");
        if(Pr57400Enabled&&_pr57399Step==1820)""")

for name, source in files.items():
    (root / name).write_text(source, encoding="utf-8")
print("PR57_401_INJECTED directSites=3 exactRouteContent=True observational=True")
