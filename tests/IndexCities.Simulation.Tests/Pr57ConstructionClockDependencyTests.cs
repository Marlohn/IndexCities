using System.Reflection;
using System.Text.Json;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #175: a causal read dependency may change between Step phases even
// when no Citizen, Building, Project or Shipment field is written. The clock
// advances after ProgressConstruction and before OrderConstruction.
public sealed class Pr57ConstructionClockDependencyTests
{
    [Fact]
    public void Crew_shift_eligibility_changes_at_clock_boundary_without_entity_mutation()
    {
        var sim = new CitySimulation(175);
        var depot = new Building
        {
            Id = 100_001,
            Kind = BuildingKind.WorksDepot,
            Completed = true,
            Position = new Point(0, 0)
        };
        var worker = new Citizen
        {
            Id = 100_002,
            WorkplaceId = depot.Id,
            Resident = true,
            Alive = true,
            WeeklyDayOff = 0, // Sunday/Monday off; Thursday is a workday.
            Shift = 0,
            Position = depot.Door
        };
        sim.State.Buildings.Add(depot);
        sim.State.Citizens.Add(worker);
        var shiftActive = typeof(CitySimulation).GetMethod(
            "ShiftActive", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool Active() => (bool)shiftActive.Invoke(sim, new object[] { worker })!;

        // Thursday, 2026-01-01. Ordinary depot shifts are 07:00-15:00.
        sim.State.Now = new DateTime(2026, 1, 1, 6, 59, 58);
        Assert.False(Active());
        var citizens = JsonSerializer.Serialize(sim.State.Citizens);
        var buildings = JsonSerializer.Serialize(sim.State.Buildings);
        var projects = JsonSerializer.Serialize(sim.State.Projects);
        var shipments = JsonSerializer.Serialize(sim.State.Shipments);

        sim.State.Now = sim.State.Now.AddSeconds(2);
        Assert.True(Active());
        Assert.Equal(citizens, JsonSerializer.Serialize(sim.State.Citizens));
        Assert.Equal(buildings, JsonSerializer.Serialize(sim.State.Buildings));
        Assert.Equal(projects, JsonSerializer.Serialize(sim.State.Projects));
        Assert.Equal(shipments, JsonSerializer.Serialize(sim.State.Shipments));

        sim.State.Now = new DateTime(2026, 1, 1, 14, 59, 58);
        Assert.True(Active());
        sim.State.Now = sim.State.Now.AddSeconds(2);
        Assert.False(Active());
        Assert.Equal(citizens, JsonSerializer.Serialize(sim.State.Citizens));
        Assert.Equal(buildings, JsonSerializer.Serialize(sim.State.Buildings));
    }
}
