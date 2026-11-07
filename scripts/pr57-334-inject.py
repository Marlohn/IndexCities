#!/usr/bin/env python3
"""PR57 #334: exterior broken-tow branch, canonical reader vs partial shadow.

Only injects in the CI checkout; the canonical simulation never reads this
shadow. The optional hook covers the two Repair exits from StartExteriorService,
not every service writer or every physical field. ARTIFICIAL fixtures only.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-331-inject.py")

path = Path("src/IndexCities.Simulation/Breakdowns.cs")
s = path.read_text()
# The unpaid repair exits early; a hook only at method end misses it.
unpaid = 'v.Cause="Quebrado no exterior; dono sem dinheiro para o conserto, voltando para a vaga";return;'
paid = 'foreach(var c in Occupants(v))c.Activity="Aguardando a revisão do veículo no exterior";'
hook = 'if(Environment.GetEnvironmentVariable("PR57_334_REPAIR_PUBLISH")=="1")Pr57330Nonlocal(v);'
assert s.count(unpaid) == 1, s.count(unpaid)
assert s.count(paid) == 1, s.count(paid)
s = s.replace(unpaid, unpaid.replace('return;', hook + 'return;'), 1)
s = s.replace(paid, paid + hook, 1)
path.write_text(s)

test = Path("tests/IndexCities.Simulation.Tests/Pr57Physical334Tests.cs")
assert not test.exists()
test.write_text(r'''using System.Collections;
using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// #334: ARTIFICIAL fixtures calling REAL canonical delivery and BodyOnWay.
// Neither branch alters authoritative physics; the optional publication only
// updates the #329 observational shadow. Not a complete causal readset.
public sealed class Pr57Physical334Tests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);
    static long Count(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;
    static void Set(CitySimulation sim, string name, bool value) =>
        typeof(CitySimulation).GetField(name, Private)!.SetValue(sim, value);
    static object? Shadow(CitySimulation sim, Vehicle target)
    {
        var map = (IDictionary)typeof(CitySimulation).GetField("_pr57329Last", Private)!.GetValue(sim)!;
        return map[target.Id];
    }
    static object? Canonical(CitySimulation sim, Vehicle target) =>
        typeof(CitySimulation).GetMethod("Pr57329Snapshot",
            BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { target });

    static (CitySimulation Sim, Vehicle Tow, Vehicle Target, Vehicle Reader) Fixture()
    {
        var method = typeof(Pr57Physical331Tests).GetMethod("Fixture",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var tuple = (ValueTuple<CitySimulation,Vehicle,Vehicle,Vehicle>)
            method.Invoke(null, new object[] { "deliver" })!;
        return (tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Broken_exterior_delivery_exposes_missing_writer_and_optional_repair_publication(bool paid)
    {
        var (sim, tow, target, reader) = Fixture();
        var publish = Environment.GetEnvironmentVariable("PR57_334_REPAIR_PUBLISH") == "1";
        Assert.Equal("1", Environment.GetEnvironmentVariable("PR57_330_ACTOR_LOCAL"));
        target.BrokenDown = true;
        target.OwnerAccount = paid ? "global" : "company:nonexistent-334";
        Set(sim, "_pr57329Active", true);
        // The candidate has already been read and processed this sub-slice.
        Call(sim, "Pr57329Visit", reader, target, false);
        Call(sim, "Pr57330AfterActor", target);
        Assert.Equal(0L, Count(sim, "_pr57329ReadTransitions"));

        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(VehicleStatus.Outside, target.Status);
        Assert.Equal(sim.State.Entry, target.Position);
        Assert.Equal(paid ? ServicePhase.AtExterior : ServicePhase.AwaitingReturn,
            target.ServicePhase);
        Assert.Equal(publish ? 2L : 1L, Count(sim, "_pr57330NonlocalCalls"));
        Assert.Equal(publish ? 2L : 1L, Count(sim, "_pr57330NonlocalChanges"));
        // Baseline shadow remains Parked after the first writer hook.
        // The optional repair hook publishes Outside immediately.
        Assert.Equal(publish, Equals(Shadow(sim, target), Canonical(sim, target)));

        // A REAL canonical physical reader visits the target before applying
        // its Outside exclusion, detecting the stale signature on baseline.
        reader.Position = new Point(900, 900);
        var poses = new List<(Vehicle Body, Point Position, double Heading)> {
            (reader, reader.Position, reader.Heading)
        };
        typeof(CitySimulation).GetMethod("BodyOnWay", Private, null,
            new[] { typeof(Vehicle), typeof(List<(Vehicle,Point,double)>), typeof(bool) }, null)!
            .Invoke(sim, new object[] { reader, poses, false });
        Assert.Equal(publish ? 0L : 1L, Count(sim, "_pr57329ReadTransitions"));
        Assert.True(Equals(Shadow(sim, target), Canonical(sim, target)));
        Console.WriteLine(
            $"PR57_334_REPAIR paid={paid} optionalPublish={publish} " +
            $"writerHooks={Count(sim, "_pr57330NonlocalCalls")} " +
            $"staleBeforeReader={!publish} readTransitions={Count(sim, "_pr57329ReadTransitions")} " +
            "scope=ARTIFICIAL_CANONICAL_TOW_AND_BODY_READER_PARTIAL_SHADOW");
    }
}
''')
print("PR57_334_INJECTED repairBranches=2 tests=2 observationalOnly=True")
