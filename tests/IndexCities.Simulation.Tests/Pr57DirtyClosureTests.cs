using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #173: explicit dirty-ID hooks are checked against complete Vehicle
// snapshots at sampled boundaries. Artificial adversarial fixtures, NOT a
// physical index, gameplay change or proof of complete writer coverage.
public sealed class Pr57DirtyClosureTests
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool Enabled =>
        Environment.GetEnvironmentVariable("PR57_DIRTY_CLOSURE_PROBE") == "1" &&
        Environment.GetEnvironmentVariable("PR57_VEHICLE_MUTATION_AUDIT") == "1";

    static object? Call(CitySimulation sim, string method, params object[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);

    static long Counter(CitySimulation sim, string name) =>
        (long)typeof(CitySimulation).GetField(name, Private)!.GetValue(sim)!;

    static void BeginSample(CitySimulation sim)
    {
        // The observational probe intentionally samples only 1/97 slices.
        for (var i = 0; i < 97; i++) Call(sim, "Pr57MutationBeginSlice");
    }

    [Fact]
    public void Tow_load_and_sync_have_explicit_target_dirty_ids()
    {
        if (!Enabled) return; // dedicated CI runs this with both probes enabled
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
            Position = new Point(20, 20), Heading = 0
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target]);
        BeginSample(sim);
        Assert.Equal(true, Call(sim, "OperateTow", tow, 2.0));
        Assert.Equal(VehicleStatus.Towed, target.Status);
        Call(sim, "Pr57MutationAfterVehicle", tow);
        Assert.Equal(0, Counter(sim, "_pr57DirtyMissed"));
        Assert.Equal(2, Counter(sim, "_pr57DirtyActual"));

        // Simulate the current actor's movement; the noncurrent target must
        // still be marked by SyncTowedVehicles after the ordered pass.
        tow.Position = new Point(100, 40);
        Call(sim, "Pr57DirtyTouch", tow);
        Call(sim, "SyncTowedVehicles");
        Call(sim, "Pr57MutationAfterSync");
        Assert.Equal(0, Counter(sim, "_pr57DirtyMissed"));
        Assert.True(Counter(sim, "_pr57DirtyActual") >= 4);
    }

    [Fact]
    public void Tow_creation_marks_a_new_id_without_scanning_the_roster()
    {
        if (!Enabled) return;
        var sim = new CitySimulation(42);
        sim.State.Vehicles.Clear();
        var actor = new Vehicle { Id = sim.State.NextId++, Status = VehicleStatus.Stranded };
        sim.State.Vehicles.Add(actor);
        BeginSample(sim);
        var created = (Vehicle)Call(sim, "TowTruck", false)!;
        Assert.Contains(sim.State.Vehicles, v => v.Id == created.Id);
        Call(sim, "Pr57MutationAfterVehicle", actor);
        Assert.Equal(1, Counter(sim, "_pr57DirtyNewIds"));
        Assert.Equal(0, Counter(sim, "_pr57DirtyMissed"));
        Call(sim, "Pr57MutationAfterSync");
    }

    [Fact]
    public void Unannounced_external_mutation_is_detected_as_a_miss()
    {
        if (!Enabled) return;
        var sim = new CitySimulation(42);
        sim.State.Vehicles.Clear();
        var actor = new Vehicle { Id = sim.State.NextId++, Position = new Point(10, 10) };
        var other = new Vehicle { Id = sim.State.NextId++, Position = new Point(20, 20) };
        sim.State.Vehicles.AddRange([actor, other]);
        BeginSample(sim);
        other.Position = new Point(21, 20); // no dirty hook, intentionally
        Call(sim, "Pr57MutationAfterVehicle", actor);
        Assert.Equal(1, Counter(sim, "_pr57DirtyMissed"));
        Call(sim, "Pr57MutationAfterSync");
    }
}
