using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #233: ARTIFICIAL read-only dock writer/readset probes, no executor.
public sealed class Pr57DockWriterAdversarialTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(CitySimulation sim, string method, params object[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);
    static long Count(CitySimulation sim, string field) =>
        (long)typeof(CitySimulation).GetField(field, Private)!.GetValue(sim)!;

    [Fact]
    public void Public_dock_edit_after_actor_refresh_remains_stale_until_boundary_rebuild()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(50, 50), Heading = 0
        };
        var candidate = new Vehicle {
            Id = sim.State.NextId++, Body = TruckBody.Cargo,
            Status = VehicleStatus.Parked, Position = new Point(50, 60),
            DockPosition = new Point(50, 60), Heading = 0
        };
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([reader, candidate]);
        Call(sim, "Pr57DockWriterBuild");
        Call(sim, "Pr57DockWriterAfterActor", candidate);
        Assert.Equal(10.0, (double)Call(sim, "BodyAcrossLane", reader, candidate)!);
        candidate.DockPosition = new Point(100, 100); // deliberately no hook
        Assert.Null(Call(sim, "BodyAcrossLane", reader, candidate));
        Call(sim, "Pr57DockWriterAfterActor", reader); // wrong actor cannot repair
        Assert.Equal(1L, Count(sim, "_pr57DockWriterStable"));
        Assert.Equal(1L, Count(sim, "_pr57DockWriterStaleThird"));
        Assert.Equal(0L, Count(sim, "_pr57DockWriterJournalTouches"));
        Call(sim, "Pr57DockWriterBuild"); // boundary rebuild catches external edits
        Assert.Null(Call(sim, "BodyAcrossLane", reader, candidate));
        Assert.Equal(1L, Count(sim, "_pr57DockWriterStable"));
        Assert.Equal(0L, Count(sim, "_pr57DockWriterStaleThird"));
    }

    [Fact]
    public void Real_tow_loading_marks_nonlocal_dock_writer_and_journal_repairs_before_next_reader()
    {
        var sim = new CitySimulation(42);
        var tow = new Vehicle {
            Id = sim.State.NextId++, External = true, Body = TruckBody.TowHeavy,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.Loading,
            LoadTimer = 0, Position = new Point(80, 40),
            Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle {
            Id = sim.State.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            Position = new Point(20, 20), DockPosition = new Point(20, 20),
            Heading = 0, Cause = "Sem combustível"
        };
        tow.TowTargetId = target.Id;
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([tow, target]);
        Call(sim, "Pr57DockWriterBuild");
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Null(target.DockPosition);
        Call(sim, "Pr57DockWriterRead", tow, target); // before journal flush: stale
        Assert.Equal(1L, Count(sim, "_pr57DockWriterStaleThird"));
        Call(sim, "Pr57DockWriterAfterActor", tow); // flushes touched target
        Call(sim, "Pr57DockWriterRead", tow, target);
        Assert.Equal(1L, Count(sim, "_pr57DockWriterStable"));
        Assert.True(Count(sim, "_pr57DockWriterJournalTouches") > 0);
        Assert.True(Count(sim, "_pr57DockWriterJournalChanges") > 0);
    }
}
