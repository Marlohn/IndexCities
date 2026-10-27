using System.Reflection;
using System.Text.Json;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #176: project-local invalidation misses a shared resource dependency.
// This is a bounded synthetic causal counterexample, not a throughput test.
public sealed class Pr57ConstructionSharedReservationTests
{
    private static (CitySimulation Sim, WorkProject First, WorkProject Second) Fixture(
        double pendingForFirst, bool complete = false, bool reverseInsertion = false)
    {
        var sim = new CitySimulation(176);
        var depot = new Building
        {
            Id = 100_001,
            Kind = BuildingKind.WorksDepot,
            Completed = true,
            Position = new Point(0, 0)
        };
        depot.AddStock(ResourceKind.Wood, 10);
        sim.State.Buildings.Add(depot);
        var first = new WorkProject
        {
            Id = 100_002,
            Status = WorkStatus.Waiting,
            Required = new() { [ResourceKind.Wood] = 8 }
        };
        var second = new WorkProject
        {
            Id = 100_003,
            Status = WorkStatus.Waiting,
            Required = new() { [ResourceKind.Wood] = 8 }
        };
        if (reverseInsertion)
        {
            sim.State.Projects.Add(second);
            sim.State.Projects.Add(first);
        }
        else
        {
            sim.State.Projects.Add(first);
            sim.State.Projects.Add(second);
        }
        if (pendingForFirst > 0)
        {
            sim.State.Shipments.Add(new Shipment
            {
                Id = 100_004,
                ProjectId = first.Id,
                Complete = complete,
                Cargo = new() { [ResourceKind.Wood] = pendingForFirst }
            });
        }
        return (sim, first, second);
    }

    private static void Reserve(CitySimulation sim)
    {
        var method = typeof(CitySimulation).GetMethod(
            "ReserveLocalMaterials", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(sim, null);
    }

    [Fact]
    public void Other_projects_pending_shipment_changes_my_reservation_without_changing_my_inputs()
    {
        var baseline = Fixture(0);
        var pending = Fixture(4);
        var delivered = Fixture(4, complete: true);

        // Same second project, depot, clock, project ordering and required quantity.
        // Only the *first* project's in-flight shipment differs.
        var secondBefore = JsonSerializer.Serialize(baseline.Second);
        Assert.Equal(secondBefore, JsonSerializer.Serialize(pending.Second));
        Assert.Equal(secondBefore, JsonSerializer.Serialize(delivered.Second));
        Assert.Equal(baseline.Sim.State.Now, pending.Sim.State.Now);
        Assert.Equal(baseline.Sim.State.Now, delivered.Sim.State.Now);

        Reserve(baseline.Sim);
        Reserve(pending.Sim);
        Reserve(delivered.Sim);

        Assert.Equal(8.0, baseline.First.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(2.0, baseline.Second.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(4.0, pending.First.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(6.0, pending.Second.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(8.0, delivered.First.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(2.0, delivered.Second.LocalReserved.GetValueOrDefault(ResourceKind.Wood));

        // Reservation is stable on an unchanged repeat; this is not an
        // artificial duplication of stock or a claim about a viable city.
        Reserve(pending.Sim);
        Assert.Equal(4.0, pending.First.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(6.0, pending.Second.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(10.0,
            pending.First.LocalReserved.GetValueOrDefault(ResourceKind.Wood) +
            pending.Second.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
    }

    [Fact]
    public void Reservation_uses_project_id_priority_not_list_insertion_order()
    {
        var reversed = Fixture(0, reverseInsertion: true);
        Reserve(reversed.Sim);
        Assert.Equal(8.0, reversed.First.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
        Assert.Equal(2.0, reversed.Second.LocalReserved.GetValueOrDefault(ResourceKind.Wood));
    }
}
