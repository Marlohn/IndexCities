using System.Reflection;
using System.Text.Json;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #172: test-only, slice-local *full Vehicle* mirror. This deliberately
// uses JSON to detect every persisted Vehicle field, not a production index.
// Refreshes are explicit and validation scans the full world; neither is a
// performance candidate or proof that all physical writers are covered.
public sealed class Pr57SliceMirrorAdversarialTests
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private sealed class Mirror
    {
        private readonly Dictionary<int, string> _snapshots = new();

        public Mirror(IEnumerable<Vehicle> vehicles)
        {
            foreach (var v in vehicles) Refresh(v);
        }

        public void Refresh(Vehicle v) => _snapshots[v.Id] = JsonSerializer.Serialize(v);

        public bool Matches(IReadOnlyCollection<Vehicle> vehicles) =>
            _snapshots.Count == vehicles.Count &&
            vehicles.All(v => _snapshots.TryGetValue(v.Id, out var old) &&
                string.Equals(old, JsonSerializer.Serialize(v), StringComparison.Ordinal));
    }

    private static object? Call(CitySimulation sim, string name, params object[] args) =>
        typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args);

    [Fact]
    public void Tow_load_and_post_loop_sync_require_refresh_of_the_noncurrent_target()
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();

        var tow = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.TowHeavy,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.Loading,
            LoadTimer = 0, Position = new Point(80, 40), Heading = Math.PI / 2,
            Fuel = 40
        };
        var target = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            Position = new Point(20, 20), Heading = 0, Cause = "Sem combustível"
        };
        var nextActor = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Travelling, Position = new Point(80, 60)
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target, nextActor]);
        var mirror = new Mirror(state.Vehicles);

        // This is the actual OperateTow branch called in MoveVehiclesUnsplit,
        // not a direct invocation of LoadTowed.
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(VehicleStatus.Towed, target.Status);
        Assert.False(mirror.Matches(state.Vehicles));
        mirror.Refresh(tow); // The current-actor-only strategy is still wrong.
        Assert.False(mirror.Matches(state.Vehicles));
        mirror.Refresh(target); // Explicit nonlocal writer closure for THIS path.
        Assert.True(mirror.Matches(state.Vehicles));

        // SyncTowedVehicles runs after the ordered vehicle pass. A fresh
        // current-actor snapshot does not update the carried vehicle's pose.
        tow.Position = new Point(100, 40);
        tow.Heading = 0;
        mirror.Refresh(tow);
        Call(sim, "SyncTowedVehicles");
        Assert.NotEqual(tow.Position, new Point(80, 40));
        Assert.False(mirror.Matches(state.Vehicles));
        mirror.Refresh(target);
        Assert.True(mirror.Matches(state.Vehicles));

        // A public mutable State edit between Advance(2) calls cannot be
        // invalidated by an actor-only cache across calls. Rebuild at boundary.
        nextActor.Position = new Point(81, 60);
        Assert.False(mirror.Matches(state.Vehicles));
        Assert.True(new Mirror(state.Vehicles).Matches(state.Vehicles));
    }

    [Fact]
    public void New_vehicle_id_requires_insertion_even_when_ordered_actor_roster_is_unchanged()
    {
        var sim = new CitySimulation(42);
        sim.State.Vehicles.Clear();
        var first = new Vehicle { Id = sim.State.NextId++, External = true, Status = VehicleStatus.Stranded };
        sim.State.Vehicles.Add(first);
        var orderedAtEntry = sim.State.Vehicles.ToArray();
        var mirror = new Mirror(sim.State.Vehicles);
        var spawned = new Vehicle { Id = sim.State.NextId++, External = true, Body = TruckBody.Tow };
        sim.State.Vehicles.Add(spawned);

        // The actual Advance(2) -> CallTow insertion is proven separately by
        // TowingTests.Pr57_171; this isolates the mirror's membership invariant.
        Assert.Single(orderedAtEntry);
        Assert.Equal(2, sim.State.Vehicles.Count);
        Assert.False(mirror.Matches(sim.State.Vehicles));
        mirror.Refresh(spawned);
        Assert.True(mirror.Matches(sim.State.Vehicles));
    }
}
