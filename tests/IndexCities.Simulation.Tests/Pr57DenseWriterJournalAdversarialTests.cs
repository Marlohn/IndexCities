using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #223: ARTIFICIAL, bounded counterexamples for the #222 dense shadow.
// This exercises the actual tow writers and actual shadow/journal, not an
// alternative traffic algorithm. It cannot prove a universal writer closure.
public sealed class Pr57DenseWriterJournalAdversarialTests
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static object? Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);

    private static long Counter(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;

    private static object Snapshot(Vehicle vehicle) =>
        typeof(CitySimulation).GetMethod("Pr57ExecSnapshot",
            BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [vehicle])!;

    private static object Mirror(CitySimulation sim, Vehicle vehicle)
    {
        var indices = (Dictionary<int, int>)typeof(CitySimulation)
            .GetField("_pr57DenseIndices", Private)!.GetValue(sim)!;
        var values = (Array)typeof(CitySimulation)
            .GetField("_pr57DenseProjection", Private)!.GetValue(sim)!;
        return values.GetValue(indices[vehicle.Id])!;
    }

    [Fact]
    public void Loading_tow_stales_actor_only_mirror_but_known_writer_journal_repairs_before_next_actor()
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var tow = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.TowHeavy,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.Loading,
            LoadTimer = 0, Position = new Point(80, 40), Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            Position = new Point(20, 20), Heading = 0, Cause = "Sem combustível"
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target]);
        Call(sim, "Pr57DenseBuild");
        Assert.Equal(Snapshot(target), Mirror(sim, target));

        var beforeTime = state.ElapsedSeconds;
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(beforeTime, state.ElapsedSeconds);
        Assert.Equal(VehicleStatus.Towed, target.Status);
        Assert.NotEqual(Snapshot(target), Mirror(sim, target));

        // Actor-only refresh would leave the target stale. The existing
        // Pr57DirtyTouch(target) is only a POTENTIAL-writer mark, flushed here.
        Call(sim, "Pr57DenseFinishVehicle", tow);
        Assert.Equal(Snapshot(target), Mirror(sim, target));
        Assert.Equal(1L, Counter(sim, "_pr57DenseJournalChanges"));
        Assert.Equal(0L, Counter(sim, "_pr57DenseExternalChanges"));
        Assert.True(Counter(sim, "_pr57DenseJournalTouches") >= 1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Post_loop_tow_sync_needs_nonlocal_refresh_even_without_elapsed_time(bool passengerCar)
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var tow = new Vehicle
        {
            Id = state.NextId++, External = true,
            Body = passengerCar ? TruckBody.Tow : TruckBody.TowHeavy,
            Status = VehicleStatus.Travelling, TowPhase = TowPhase.Delivering,
            Position = new Point(80, 40), Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle
        {
            Id = state.NextId++, External = true, PassengerCar = passengerCar,
            Body = TruckBody.Cargo, Status = VehicleStatus.Towed,
            TowTruckId = tow.Id, Position = new Point(20, 20), Heading = 0
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target]);
        Call(sim, "Pr57DenseBuild");
        var beforeTime = state.ElapsedSeconds;

        Call(sim, "SyncTowedVehicles");
        Assert.Equal(beforeTime, state.ElapsedSeconds);
        Assert.NotEqual(Snapshot(target), Mirror(sim, target));
        Call(sim, "Pr57DenseFlushJournal");
        Assert.Equal(Snapshot(target), Mirror(sim, target));
        Assert.Equal(1L, Counter(sim, "_pr57DenseJournalChanges"));
        Call(sim, "Pr57DenseAudit", true, -1);
        Assert.Equal(0L, Counter(sim, "_pr57DenseSyncChanges"));
    }

    [Fact]
    public void Public_unmarked_edit_is_detected_only_by_full_audit_not_writer_journal()
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var actor = new Vehicle { Id = state.NextId++, Status = VehicleStatus.Waiting };
        var other = new Vehicle { Id = state.NextId++, Status = VehicleStatus.Waiting };
        state.Vehicles.AddRange([actor, other]);
        Call(sim, "Pr57DenseBuild");
        other.Position = new Point(123, 456);

        Call(sim, "Pr57DenseFinishVehicle", actor);
        Assert.NotEqual(0L, Counter(sim, "_pr57DenseExternalChanges"));
        Assert.Equal(0L, Counter(sim, "_pr57DenseJournalChanges"));
        Assert.Equal(Snapshot(other), Mirror(sim, other));
    }
}
