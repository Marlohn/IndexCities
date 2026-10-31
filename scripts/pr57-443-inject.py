#!/usr/bin/env python3
"""PR57 #443: observational cell-churn and IEEE-754 delta-replay falsification.

Injected only in a disposable CI checkout. Every canonical pollution update,
home sample, Step, event and save remains unchanged. The shadow delta result
is never used for gameplay. This is not a throughput A/B.
"""
from pathlib import Path

def patch(path, pairs):
    p = Path(path)
    s = p.read_text()
    for old, new in pairs:
        assert s.count(old) == 1, (path, old[:110], s.count(old))
        s = s.replace(old, new, 1)
    p.write_text(s)

probe = r'''
    // #443: observational only. The shadow never publishes a value.
    public long[] Pr57443Census = new long[12];
    readonly Dictionary<long,decimal> _pr57443PreviousCells = new();
    readonly Dictionary<Point,double> _pr57443PreviousRaw = new();
    long[] _pr57443PreviousKeys = [];
    double[] _pr57443PreviousValues = [];
    int _pr57443PreviousCount;
    bool _pr57443Seen;
    void Pr57443ObservePollution(List<Point> points, Dictionary<Point,double> actual)
    {
        Pr57443Census[0]++;
        var sameLayout = _pr57443Seen && _pr57443PreviousCount == _cellCount;
        if (sameLayout)
            for (var j=0;j<_cellCount;j++)
                if (_pr57443PreviousKeys[j]!=_cellKeys[j]) { sameLayout=false; break; }
        var changedDecimal=0;
        foreach (var pair in State.PollutionCells)
            if (!_pr57443PreviousCells.TryGetValue(pair.Key,out var old) || old!=pair.Value)
                changedDecimal++;
        foreach (var key in _pr57443PreviousCells.Keys)
            if (!State.PollutionCells.ContainsKey(key)) changedDecimal++;
        if (_pr57443Seen)
        {
            Pr57443Census[1]++;
            Pr57443Census[2]+=State.PollutionCells.Count;
            Pr57443Census[3]+=changedDecimal;
            if (changedDecimal==0) Pr57443Census[6]++;
            if (!sameLayout) Pr57443Census[7]++;
            if (sameLayout)
            {
                if (changedDecimal==_cellCount) Pr57443Census[5]++;
                for (var j=0;j<_cellCount;j++)
                    if (BitConverter.DoubleToInt64Bits(_pr57443PreviousValues[j]) !=
                        BitConverter.DoubleToInt64Bits(_cellValues[j]))
                        Pr57443Census[4]++;
            }
        }
        var nextRaw = new Dictionary<Point,double>(points.Count);
        for (var home=0;home<points.Count;home++)
        {
            var point=points[home];
            var factors=FactorsFor(point).Factors;
            double raw=0;
            for (var j=0;j<_cellCount;j++) raw+=_cellValues[j]*factors[j];
            var canonical= Math.Clamp(raw,0,2);
            if (BitConverter.DoubleToInt64Bits(canonical)!=BitConverter.DoubleToInt64Bits(actual[point]))
                throw new InvalidOperationException("PR57 #443 observer does not match canonical sample");
            if (sameLayout && _pr57443PreviousRaw.TryGetValue(point,out var previousRaw))
            {
                // Mathematically valid incremental sum, but different IEEE-754
                // operation order from the canonical left-to-right dot product.
                double delta=previousRaw;
                for (var j=0;j<_cellCount;j++)
                    delta+=(_cellValues[j]-_pr57443PreviousValues[j])*factors[j];
                Pr57443Census[9]++;
                if (BitConverter.DoubleToInt64Bits(Math.Clamp(delta,0,2))!=
                    BitConverter.DoubleToInt64Bits(canonical))
                {
                    Pr57443Census[10]++;
                    if (Pr57443Census[11]==0)
                    {
                        Pr57443Census[11]=Pr57443Census[0];
                        Console.WriteLine($"PR57_443_EXAMPLE step={Pr57443Census[0]} homeIndex={home} canonicalBits={BitConverter.DoubleToInt64Bits(canonical):X16} deltaBits={BitConverter.DoubleToInt64Bits(Math.Clamp(delta,0,2)):X16}");
                    }
                }
            }
            nextRaw[point]=raw;
            Pr57443Census[8]++;
        }
        _pr57443PreviousRaw.Clear();
        foreach (var pair in nextRaw) _pr57443PreviousRaw.Add(pair.Key,pair.Value);
        _pr57443PreviousCells.Clear();
        foreach (var pair in State.PollutionCells) _pr57443PreviousCells.Add(pair.Key,pair.Value);
        if (_pr57443PreviousKeys.Length<_cellCount)
        {
            Array.Resize(ref _pr57443PreviousKeys,_cellCount);
            Array.Resize(ref _pr57443PreviousValues,_cellCount);
        }
        Array.Copy(_cellKeys,_pr57443PreviousKeys,_cellCount);
        Array.Copy(_cellValues,_pr57443PreviousValues,_cellCount);
        _pr57443PreviousCount=_cellCount;
        _pr57443Seen=true;
    }
'''
patch("src/IndexCities.Simulation/Pollution.cs", [
    ("    const double PollutionCellSize=32;", "    const double PollutionCellSize=32;\n"+probe)
])
patch("src/IndexCities.Simulation/Economy.cs", [
    ("            SampledPollution(_homePoints,pollution);",
     "            SampledPollution(_homePoints,pollution);\n            Pr57443ObservePollution(_homePoints,pollution);")
])
patch("scripts/pr57-actor-regime-337/Program.cs", [
    ("    var before = sim.Pr57298VehicleCostSnapshot();",
     "    var before = sim.Pr57298VehicleCostSnapshot();\n    var pr57443Before=sim.Pr57443Census.ToArray();"),
    ("    var after=sim.Pr57298VehicleCostSnapshot();",
     """    var after=sim.Pr57298VehicleCostSnapshot();
    string[] pr57443Names={"steps","comparableSteps","cellsObserved","decimalCellChanges","doubleCellChanges","allCellsChangedSteps","unchangedSteps","layoutChanges","homeSamples","deltaComparisons","deltaBitwiseMismatches","firstMismatchStep"};
    for(var k=0;k<pr57443Names.Length;k++)
        Console.WriteLine($"PR57_443_CENSUS hour={hour} metric={pr57443Names[k]} value={sim.Pr57443Census[k]-pr57443Before[k]}");""")
])
print("PR57_443_INJECTED observational=True noDeltaPublication=True everyAdvance2=True")
