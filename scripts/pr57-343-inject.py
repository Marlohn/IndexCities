#!/usr/bin/env python3
"""PR57 #343: checkout-only reservation hook vs selected-read audit, REAL 17-20h.

No authoritative decision, actor ordering, clock, persistence, or global slice changes.
The explicit Pr57ReservationWrite sites are NOT a complete mutation journal.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
sim = root / "src/IndexCities.Simulation"


def once(path, old, new):
    source = path.read_text(encoding="utf-8")
    count = source.count(old)
    if count != 1:
        raise RuntimeError(f"{path}: expected one anchor, got {count}: {old[:90]!r}")
    path.write_text(source.replace(old, new, 1), encoding="utf-8")


once(sim / "Pr57JunctionReadersProbe.cs",
     "        Pr57ReservationShadowWrite(vehicle);\n",
     "        Pr57ReservationShadowWrite(vehicle);\n        Pr57343Write(vehicle,site);\n")
once(sim / "Mobility.cs",
     "        var pr57Detail=CostProfile is not null;long pr57DetailStart=0;long pr57DetailTickStart=0;\n",
     "        Pr57343Start();\n        var pr57Detail=CostProfile is not null;long pr57DetailStart=0;long pr57DetailTickStart=0;\n")
once(sim / "Mobility.JunctionReturns.Pr57.cs",
     "        if (reason == Pr57JunctionExit.Pedestrian) Pr57304Walk(reader,junction);\n",
     "        Pr57343Decision(reader);\n        if (reason == Pr57JunctionExit.Pedestrian) Pr57304Walk(reader,junction);\n")
once(sim / "Mobility.JunctionReturns.Pr57.cs",
     "        Pr57Junction247Report(slices);\n",
     "        Pr57Junction247Report(slices);\n        Pr57343Report(slices);\n")

target = sim / "Mobility.ReservationJournal343.Pr57.cs"
if target.exists():
    raise RuntimeError("Refusing to overwrite #343 observer")
target.write_text(r'''namespace IndexCities.Simulation;

// PR57 #343: compare explicit reservation hooks against the canonical fleet
// at four selected real junction returns. The oracle is intentionally intrusive
// and only a read-boundary check: transient writes between reads can escape it.
public sealed partial class CitySimulation
{
    static readonly bool Pr57343Enabled =
        Environment.GetEnvironmentVariable("PR57_343_RESERVATION_JOURNAL") == "1";
    static readonly DateTime Pr57343From = new(2026,1,7,17,0,0);
    static readonly DateTime Pr57343Until = new(2026,1,7,20,0,0);
    readonly Dictionary<Vehicle,(int? Claim,int[] Spaces)> _pr57343Before =
        new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<string,(long Calls,long Changed,long Claim,long Spaces)> _pr57343Sites = new();
    bool _pr57343Started;
    long _pr57343Hooks, _pr57343NoncanonicalHooks, _pr57343HookChanged;
    long _pr57343HookClaim, _pr57343HookSpaces, _pr57343Audits, _pr57343Compared;
    long _pr57343ReadUnhooked, _pr57343ReadClaim, _pr57343ReadSpaces, _pr57343NewRefs;
    string _pr57343FirstUnhooked="none";

    static bool Pr57343Same(int[] previous,List<int> current)
    {
        if(previous.Length!=current.Count)return false;
        for(var i=0;i<previous.Length;i++)if(previous[i]!=current[i])return false;
        return true;
    }
    void Pr57343Start()
    {
        if(!Pr57343Enabled||_pr57343Started||State.Now<Pr57343From||State.Now>=Pr57343Until)return;
        foreach(var v in State.Vehicles)
            _pr57343Before.Add(v,(v.IntersectionClaim,v.ManoeuvreSpaces.ToArray()));
        _pr57343Started=true;
    }
    void Pr57343Write(Vehicle vehicle,string site)
    {
        if(!Pr57343Enabled)return;
        Pr57343Start();
        if(!_pr57343Started||State.Now>=Pr57343Until)return;
        _pr57343Hooks++;
        if(!_pr57343Before.TryGetValue(vehicle,out var prior))
        {
            _pr57343NoncanonicalHooks++;
            return;
        }
        bool claim=prior.Claim!=vehicle.IntersectionClaim;
        bool spaces=!Pr57343Same(prior.Spaces,vehicle.ManoeuvreSpaces);
        var old=_pr57343Sites.GetValueOrDefault(site);
        _pr57343Sites[site]=(old.Calls+1,old.Changed+(claim||spaces?1:0),
            old.Claim+(claim?1:0),old.Spaces+(spaces?1:0));
        if(!claim&&!spaces)return;
        _pr57343HookChanged++;
        if(claim)_pr57343HookClaim++;
        if(spaces)_pr57343HookSpaces++;
        _pr57343Before[vehicle]=(vehicle.IntersectionClaim,vehicle.ManoeuvreSpaces.ToArray());
    }
    void Pr57343Decision(Vehicle reader)
    {
        if(!Pr57343Enabled||!_pr57343Started||State.Now>=Pr57343Until||
           reader.Id is not (1071 or 1243 or 1282 or 1380))return;
        _pr57343Audits++;
        foreach(var vehicle in State.Vehicles)
        {
            _pr57343Compared++;
            if(!_pr57343Before.TryGetValue(vehicle,out var prior))
            {
                _pr57343NewRefs++;
                _pr57343Before[vehicle]=(vehicle.IntersectionClaim,vehicle.ManoeuvreSpaces.ToArray());
                continue;
            }
            bool claim=prior.Claim!=vehicle.IntersectionClaim;
            bool spaces=!Pr57343Same(prior.Spaces,vehicle.ManoeuvreSpaces);
            if(!claim&&!spaces)continue;
            _pr57343ReadUnhooked++;
            if(claim)_pr57343ReadClaim++;
            if(spaces)_pr57343ReadSpaces++;
            if(_pr57343FirstUnhooked=="none")
                _pr57343FirstUnhooked=$"reader:{reader.Id},vehicle:{vehicle.Id},time:{State.Now:O}";
            _pr57343Before[vehicle]=(vehicle.IntersectionClaim,vehicle.ManoeuvreSpaces.ToArray());
        }
    }
    void Pr57343Report(long slices)
    {
        if(!Pr57343Enabled)return;
        Console.WriteLine($"PR57_343_SUMMARY slices={slices} started={_pr57343Started} "+
            $"fleet={_pr57343Before.Count} hooks={_pr57343Hooks} noncanonicalHooks={_pr57343NoncanonicalHooks} "+
            $"hookChanged={_pr57343HookChanged} hookClaim={_pr57343HookClaim} hookSpaces={_pr57343HookSpaces} "+
            $"selectedReadAudits={_pr57343Audits} comparedVehicles={_pr57343Compared} "+
            $"unhookedReadDeltas={_pr57343ReadUnhooked} unhookedClaim={_pr57343ReadClaim} "+
            $"unhookedSpaces={_pr57343ReadSpaces} newReferences={_pr57343NewRefs} "+
            $"firstUnhooked={_pr57343FirstUnhooked} "+
            "scope=EXPLICIT_HOOKS_AND_FOUR_READ_BOUNDARIES_NOT_TRANSIENT_CLOSURE");
        foreach(var (site,values) in _pr57343Sites.OrderBy(x=>x.Key))
            Console.WriteLine($"PR57_343_SITE slices={slices} site={site} calls={values.Calls} "+
                $"changed={values.Changed} claim={values.Claim} spaces={values.Spaces}");
    }
}
''', encoding="utf-8")
print("PR57_343_INJECTED: explicit reservation hooks and exact four-reader fleet read audit")
