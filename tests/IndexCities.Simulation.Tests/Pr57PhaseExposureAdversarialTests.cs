using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// #228: ARTIFICIAL adversarial test of the observer, not an alternate engine.
// Canonical lane decisions remain untouched. A later reader can see a changed
// third-party position; a phase-major frozen input cannot assume equality.
public sealed class Pr57PhaseExposureAdversarialTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static object? Call(CitySimulation sim, string method, params object[] args) =>
        typeof(CitySimulation).GetMethod(method, Private)!.Invoke(sim, args);

    static long Count(CitySimulation sim, string field) =>
        (long)typeof(CitySimulation).GetField(field, Private)!.GetValue(sim)!;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Live_s3_candidate_movement_flips_forward_side_gate(bool processedEarlier)
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle
        {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(20, 20), Heading = 0
        };
        var candidate = new Vehicle
        {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(20, 10), Heading = 0
        };
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.AddRange([reader, candidate]);
        Call(sim, "Pr57PhaseExposureBuild");

        // This represents a prior writer, not a real Advance(2) movement.
        candidate.Position = new Point(20, 25);
        if (processedEarlier) Call(sim, "Pr57PhaseExposureAfterVehicle", candidate);
        Call(sim, "Pr57PhaseExposureRead", reader, candidate, true);

        Assert.Equal(1L, Count(sim, "_pr57PhaseChangedThird"));
        Assert.Equal(1L, Count(sim, "_pr57PhaseGateFlips"));
        Assert.Equal(processedEarlier ? 1L : 0L, Count(sim, "_pr57PhaseChangedProcessed"));
        Assert.Equal(processedEarlier ? 0L : 1L, Count(sim, "_pr57PhaseChangedUnprocessed"));

        // The canonical entry frame is intentionally frozen; do not classify
        // it as a current live read of the same vehicle.
        Call(sim, "Pr57PhaseExposureRead", reader, candidate, false);
        Assert.Equal(1L, Count(sim, "_pr57PhaseFrozenEntry"));
        Assert.Equal(1L, Count(sim, "_pr57PhaseChangedThird"));
        Call(sim, "Pr57PhaseExposureEndSlice");
    }

    [Fact]
    public void Changed_reader_self_is_not_a_cross_actor_phase_dependency()
    {
        var sim = new CitySimulation(42);
        var reader = new Vehicle
        {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = new Point(20, 20), Heading = 0
        };
        sim.State.Vehicles.Clear();
        sim.State.Vehicles.Add(reader);
        Call(sim, "Pr57PhaseExposureBuild");
        reader.Position = new Point(20, 21);
        Call(sim, "Pr57PhaseExposureRead", reader, reader, true);
        Assert.Equal(1L, Count(sim, "_pr57PhaseChangedSelf"));
        Assert.Equal(0L, Count(sim, "_pr57PhaseChangedThird"));
        Assert.Equal(0L, Count(sim, "_pr57PhaseGateFlips"));
        Call(sim, "Pr57PhaseExposureEndSlice");
    }
}
