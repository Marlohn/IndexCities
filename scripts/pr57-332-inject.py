#!/usr/bin/env python3
"""PR57 #332: selected instance-reader entry census between tow writer hooks.

ARTIFICIAL fixture only. Builds on #331 and observes the exact interval
between the two target publications in exterior delivery. No decisions use
these counters. This is NOT a complete transitive readset or REAL tow replay.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-331-inject.py")

def replace_once(path, old, new):
    p = Path(path)
    s = p.read_text()
    assert s.count(old) == 1, (path, old, s.count(old))
    p.write_text(s.replace(old, new, 1))

partial = Path("src/IndexCities.Simulation/Mobility.TowReadWindow332.Pr57.cs")
assert not partial.exists()
partial.write_text("""namespace IndexCities.Simulation;

// #332: observational only, active in an explicitly artificial testhost.
public sealed partial class CitySimulation
{
    static readonly bool Pr57332Enabled =
        Environment.GetEnvironmentVariable("PR57_332_TOW_WINDOW") == "1";
    bool _pr57332Window;
    long _pr57332Writes;
    readonly long[] _pr57332Reads = new long[7];

    void Pr57332Writer(Vehicle target)
    {
        if (!Pr57332Enabled) return;
        _pr57332Writes++;
        if (_pr57332Writes == 1) _pr57332Window = true;
        if (_pr57332Writes == 2) _pr57332Window = false;
    }

    void Pr57332Read(int site)
    {
        if (Pr57332Enabled && _pr57332Window) _pr57332Reads[site]++;
    }
}
""")

replace_once("src/IndexCities.Simulation/Mobility.PhysicalExposure329.Pr57.cs",
    "    void Pr57330Nonlocal(Vehicle target)\n    {\n",
    "    void Pr57330Nonlocal(Vehicle target)\n    {\n        Pr57332Writer(target);\n")

sites = [
    ("src/IndexCities.Simulation/Mobility.SharedSpace.cs",
     "Vehicle? BodyOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses,bool pr57305Onward=false)\n    {\n", 0),
    ("src/IndexCities.Simulation/Mobility.SharedSpace.cs",
     "Vehicle? TrafficOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses)\n    {\n", 1),
    ("src/IndexCities.Simulation/Mobility.SharedSpace.cs",
     "bool SpaceFree(Vehicle v,RoadNode node,Point entering,bool yieldToTraffic=true)\n    {\n", 2),
    ("src/IndexCities.Simulation/Mobility.SharedSpace.cs",
     "bool CanTurnNow(Vehicle o)\n    {\n", 3),
    ("src/IndexCities.Simulation/Mobility.cs",
     "bool MayEnterJunction(Vehicle v,RoadNode junction)\n    {\n", 5),
    ("src/IndexCities.Simulation/RoadNetwork.cs",
     "List<int> FindRoadRoute(int from,int to,bool construction,HashSet<int>? avoid)\n    {\n", 6),
]
for path, anchor, site in sites:
    replace_once(path, anchor, anchor + f"        Pr57332Read({site});\n")

test = Path("tests/IndexCities.Simulation.Tests/Pr57Physical332Tests.cs")
assert not test.exists()
test.write_text("""using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// #332: narrow ARTIFICIAL fixture, not a complete causal readset.
public sealed class Pr57Physical332Tests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);
    static long Count(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;
    static long[] Reads(CitySimulation sim) =>
        (long[])typeof(CitySimulation).GetField("_pr57332Reads", Private)!.GetValue(sim)!;

    static (CitySimulation Sim, Vehicle Tow, Vehicle Target, Vehicle Reader) Fixture()
    {
        var method = typeof(Pr57Physical331Tests).GetMethod("Fixture",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var tuple = (ValueTuple<CitySimulation,Vehicle,Vehicle,Vehicle>)
            method.Invoke(null, new object[] { "deliver" })!;
        return (tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }

    [Fact]
    public void Exterior_delivery_has_no_selected_physical_reader_between_two_publications()
    {
        var (sim, tow, target, _) = Fixture();
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(2, Count(sim, "_pr57332Writes"));
        Assert.Equal(VehicleStatus.Outside, target.Status);
        var counts = Reads(sim);
        Console.WriteLine("PR57_332_WINDOW writers=2 body={0} traffic={1} space={2} turn={3} penetration=NOT_INSTRUMENTED junction={4} route={5} scope=ARTIFICIAL_SELECTED_INSTANCE_ENTRY_SITES",
            counts[0], counts[1], counts[2], counts[3], counts[5], counts[6]);
        Assert.All(counts, value => Assert.Equal(0, value));
    }

    [Fact]
    public void Interleaved_canonical_body_reader_is_detected_by_positive_control()
    {
        var (sim, _, target, reader) = Fixture();
        reader.Status = VehicleStatus.Travelling;
        reader.Position = target.Position;
        target.Status = VehicleStatus.Travelling;
        Call(sim, "Pr57332Writer", target);
        var poses = new List<(Vehicle Body, Point Position, double Heading)> {
            (reader, reader.Position, reader.Heading)
        };
        typeof(CitySimulation).GetMethod("BodyOnWay", Private, null,
            new[] { typeof(Vehicle), typeof(List<(Vehicle,Point,double)>), typeof(bool) }, null)!
            .Invoke(sim, new object[] { reader, poses, false });
        Call(sim, "Pr57332Writer", target);
        Assert.Equal(2, Count(sim, "_pr57332Writes"));
        Assert.Equal(1, Reads(sim)[0]);
        Console.WriteLine("PR57_332_POSITIVE body=1 scope=ARTIFICIAL_SELECTED_ENTRY_SITES");
    }
}
""")
print("PR57_332_INJECTED sites=6 tests=2 canonicalDecisionsUntouched=True")
