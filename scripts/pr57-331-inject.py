#!/usr/bin/env python3
"""PR57 #331: adversarial tow-writer test injected only in CI checkout.

Reuses the #330 partial physical signature shadow; invokes the REAL canonical
tow writers on explicitly ARTIFICIAL fixtures. No authoritative decision
consults the shadow. No engine, SPEC or official architecture change.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-330-inject.py")
path = Path("tests/IndexCities.Simulation.Tests/Pr57Physical331Tests.cs")
assert not path.exists(), path
path.write_text(r'''using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// #331: ARTIFICIAL adversarial fixture, REAL canonical tow writers.
// This compares observation at nonlocal writer hooks with an actor-major scan.
// It is NOT a universal mutation journal, physics cache, or throughput claim.
public sealed class Pr57Physical331Tests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);
    static long Count(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;
    static void Set(CitySimulation sim, string name, bool value) =>
        typeof(CitySimulation).GetField(name, Private)!.SetValue(sim, value);
    static int SetCount(CitySimulation sim, string name)
    {
        var value = typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;
        return (int)value.GetType().GetProperty("Count")!.GetValue(value)!;
    }

    static (CitySimulation Sim, Vehicle Tow, Vehicle Target, Vehicle Reader) Fixture(string operation)
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var tow = new Vehicle {
            Id = state.NextId++, External = true,
            Body = operation == "load" ? TruckBody.TowHeavy : TruckBody.Tow,
            Status = operation is "load" or "abort" or "abort-unlinked"
                ? VehicleStatus.Waiting : VehicleStatus.Unloading,
            TowPhase = operation switch {
                "load" => TowPhase.Loading,
                "abort" or "abort-unlinked" => TowPhase.ToVehicle,
                "sync" => TowPhase.Delivering,
                "deliver" => TowPhase.Unloading,
                _ => TowPhase.None
            },
            TowDestination = TowDestination.Exterior,
            LoadTimer = 0, Position = new Point(80, 40),
            Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle {
            Id = state.NextId++, External = true,
            PassengerCar = operation != "load", Body = TruckBody.Cargo,
            Status = operation switch {
                "load" or "resume" => VehicleStatus.Stranded,
                "abort" or "abort-unlinked" => VehicleStatus.Travelling,
                _ => VehicleStatus.Towed
            },
            TowTruckId = operation is "resume" or "abort-unlinked" ? null : tow.Id,
            Position = new Point(20, 20), Heading = 0,
            Cause = "Fixture adversarial de guincho"
        };
        tow.TowTargetId = target.Id;
        var reader = new Vehicle {
            Id = state.NextId++, External = true, Status = VehicleStatus.Outside,
            Position = new Point(900, 900)
        };
        state.Vehicles.AddRange([tow, target, reader]);
        return (sim, tow, target, reader);
    }

    [Theory]
    [InlineData("load", false)]
    [InlineData("load", true)]
    [InlineData("sync", false)]
    [InlineData("sync", true)]
    [InlineData("abort", false)]
    [InlineData("abort", true)]
    [InlineData("abort-unlinked", false)]
    [InlineData("deliver", false)]
    [InlineData("deliver", true)]
    [InlineData("resume", false)]
    [InlineData("resume", true)]
    public void Canonical_nonlocal_tow_writers_publish_target_after_prior_read(
        string operation, bool targetAlreadyProcessed)
    {
        var (sim, tow, target, reader) = Fixture(operation);
        var local = Environment.GetEnvironmentVariable("PR57_330_ACTOR_LOCAL") == "1";
        Set(sim, "_pr57329Active", true);
        Call(sim, "Pr57329Visit", reader, target, false);
        if (targetAlreadyProcessed) Call(sim, "Pr57330AfterActor", target);
        var elapsed = sim.State.ElapsedSeconds;
        switch (operation)
        {
            case "load":
            case "abort":
            case "abort-unlinked":
            case "deliver":
                Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
                break;
            case "sync":
                Call(sim, "SyncTowedVehicles");
                break;
            case "resume":
                Call(sim, "ResumeFromExterior", target);
                break;
        }
        if (operation == "sync") Call(sim, "Pr57329AfterSync");
        else Call(sim, "Pr57330AfterActor", tow);
        Assert.Equal(elapsed, sim.State.ElapsedSeconds);
        var unchanged = operation == "abort-unlinked";
        Assert.Equal(unchanged ? 0 : 1, SetCount(sim, "_pr57329Dirty"));
        Assert.Equal(unchanged ? 0 : 1, SetCount(sim, "_pr57329ChangedIds"));
        // The nested unload -> resume path has TWO canonical target states.
        // The actor-major scan sees only its final state, while the local
        // writer hooks can observe both without a whole-vehicle scan.
        var transitions = Count(sim, "_pr57329ActorTransitions")
                        + Count(sim, "_pr57329SyncTransitions");
        Assert.Equal(unchanged ? 0L : operation == "deliver" && local ? 2L : 1L,
            transitions);
        Assert.Equal(local ? (unchanged ? 0L : operation == "deliver" ? 2L : 1L) : 0L,
            Count(sim, "_pr57330NonlocalChanges"));
        Assert.Equal(local ? (unchanged ? 0L : operation == "deliver" ? 2L : 1L) : 0L,
            Count(sim, "_pr57330NonlocalCalls"));
        if (operation == "load") Assert.Equal(VehicleStatus.Towed, target.Status);
        if (operation == "abort") Assert.Null(target.TowTruckId);
        if (operation == "deliver" || operation == "resume")
            Assert.Equal(VehicleStatus.Outside, target.Status);
    }
}
''')
print("PR57_331_INJECTED adversarialCases=11 canonicalTowWriters=True observationalOnly=True")
