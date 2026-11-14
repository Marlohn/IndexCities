#!/usr/bin/env python3
"""PR57 #400: distinguish REAL RestockBusinesses route writers and extend #399 to 1820 Steps.

Apply AFTER scripts/pr57-399-inject.py, only in a disposable checkout.
Observe exact routes of the eleven #397 witness vehicles at the two canonical
RestockBusinesses call sites. Record whether the call returned success; a failed
OrderLocalFood can still have changed a route via EnsureFuel. This is a targeted
witness probe, NOT a full mutation journal or a causal-closure certificate.
"""
from pathlib import Path

root = Path("src/IndexCities.Simulation")
files = {
    name: (root / name).read_text(encoding="utf-8")
    for name in ("CitySimulation.cs", "Economy.cs")
}

def patch(name, old, new, expected=1):
    actual = files[name].count(old)
    assert actual == expected, (name, old[:100], actual, expected)
    files[name] = files[name].replace(old, new)

patch("CitySimulation.cs",
    '    static readonly bool Pr57399Enabled=Environment.GetEnvironmentVariable("PR57_399_WRITER_METHODS")=="1";',
    """    static readonly bool Pr57400Enabled=Environment.GetEnvironmentVariable("PR57_400_RESTOCK_WRITERS")=="1";
    readonly Dictionary<int,int[]> _pr57400Before=new();
    long _pr57400Captures,_pr57400Checks,_pr57400Changes,_pr57400Local,_pr57400Import,_pr57400Failed,_pr57400Ticks;
    bool Pr57400Observe(string method,Func<bool> original)
    {
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57400Before.Clear();
        foreach(var v in State.Vehicles)
            if(Pr57399Targets.Contains(v.Id))_pr57400Before.Add(v.Id,v.Route.ToArray());
        _pr57400Captures++;
        var result=original();
        _pr57400Checks++;
        foreach(var v in State.Vehicles)
        {
            if(!Pr57399Targets.Contains(v.Id))continue;
            if(_pr57400Before.TryGetValue(v.Id,out var before)&&before.SequenceEqual(v.Route))continue;
            _pr57400Changes++;
            if(method=="OrderLocalFood")_pr57400Local++;else _pr57400Import++;
            if(!result)_pr57400Failed++;
            if(_pr57400Changes<=40)
                Console.WriteLine($"PR57_400_ROUTE step={_pr57399Step} physical={_pr57399Physical} method={method} vehicle={v.Id} beforeCount={(_pr57400Before.TryGetValue(v.Id,out var prior)?prior.Length:-1)} afterCount={v.Route.Count} success={result}");
        }
        _pr57400Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        return result;
    }
    bool Pr57400OrderLocalFood(Building farm,Building shop,double amount)
    {
        if(!Pr57400Enabled||_pr57399Step>1820)return OrderLocalFood(farm,shop,amount);
        return Pr57400Observe("OrderLocalFood",()=>OrderLocalFood(farm,shop,amount));
    }
    bool Pr57400OrderImport(Building destination,ResourceKind resource,double requested,string account)
    {
        if(!Pr57400Enabled||_pr57399Step>1820)return OrderImport(destination,resource,requested,account);
        return Pr57400Observe("OrderImport",()=>OrderImport(destination,resource,requested,account));
    }
    static readonly bool Pr57399Enabled=Environment.GetEnvironmentVariable("PR57_399_WRITER_METHODS")=="1";""")

patch("CitySimulation.cs", "_pr57399Step>1800", "_pr57399Step>1820", expected=2)
patch("CitySimulation.cs", "_pr57399Step<=1800", "_pr57399Step<=1820")
patch("CitySimulation.cs",
    "        if(Pr57399Enabled&&_pr57399Step==1800)",
    """        if(Pr57400Enabled&&_pr57399Step==1820)
            Console.WriteLine($"PR57_400_SUMMARY steps={_pr57399Step} physical={_pr57399Physical} captures={_pr57400Captures} checks={_pr57400Checks} changes={_pr57400Changes} local={_pr57400Local} import={_pr57400Import} failed={_pr57400Failed} ticks={_pr57400Ticks} frequency={System.Diagnostics.Stopwatch.Frequency} observational=True noSkippedSlices=True");
        if(Pr57399Enabled&&_pr57399Step==1820)""")

patch("Economy.cs",
    "if(local!=null&&OrderLocalFood(local,b,amount))continue;",
    "if(local!=null&&Pr57400OrderLocalFood(local,b,amount))continue;")
patch("Economy.cs",
    "            OrderImport(b,resource,amount,account);",
    "            Pr57400OrderImport(b,resource,amount,account);")

for name, source in files.items():
    (root / name).write_text(source, encoding="utf-8")
print("PR57_400_INJECTED submethods=True witnessHorizonSteps=1820 observational=True")
