using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only adversarial oracle. These temporary external mutations are NOT a
// viable city or a performance fixture; they test the opt-in shipment lookup's
// per-call rebuilding and live predicates against the original full scan.
public sealed class Pr57ProgressShipmentMutationAdversarial
{
    static readonly MethodInfo Progress = typeof(CitySimulation).GetMethod(
        "ProgressConstruction", BindingFlags.Instance | BindingFlags.NonPublic)!;

    static (CitySimulation Sim, int ProjectId, int ShipmentId) Setup()
    {
        var sim = new CitySimulation(42, new() { ExternalTruckCount = 1 });
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success);
        Assert.True(sim.PlaceRoad(new(-192, 0), new(0, 0)).Success);
        for (var i = 0; i < 3600; i++) sim.Advance(2);
        var placed = sim.PlaceBuilding(BuildingKind.House, new(-100, 14));
        Assert.True(placed.Success);
        sim.Advance(2);
        var project = sim.State.Projects.Single(p => p.Id == placed.Id);
        Assert.Equal(WorkStatus.Waiting, project.Status);
        Assert.True(project.Required.Count > 1);
        var pending = Assert.Single(sim.State.Shipments.Where(s =>
            s.ProjectId == project.Id && !s.Complete));
        return (sim, project.Id, pending.Id);
    }

    static void Probe(CitySimulation sim, int projectId, string stage, string expected)
    {
        Progress.Invoke(sim, [2.0]);
        var actual = sim.State.Projects.Single(p => p.Id == projectId).Cause;
        Assert.Equal(expected, actual);
        Console.WriteLine($"PR57_PROGRESS_MUTATION stage={stage} cause={actual}");
    }

    static void Capture(CitySimulation sim, string stage)
    {
        sim.CheckInvariants();
        var json = sim.SaveToJson();
        Assert.Equal(json, CitySimulation.FromJson(json).SaveToJson());
        var dir = Environment.GetEnvironmentVariable("PR57_PROGRESS_MUTATION_DIR");
        if (string.IsNullOrWhiteSpace(dir)) return;
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, stage + ".json"), json);
    }

    [Fact]
    public void External_membership_and_live_cargo_changes_rebuild_between_calls_and_preserve_save()
    {
        var (sim, projectId, shipmentId) = Setup();
        Capture(sim, "before");
        var shipment = sim.State.Shipments.Single(s => s.Id == shipmentId);
        var index = sim.State.Shipments.IndexOf(shipment);
        Probe(sim, projectId, "original", WorkCause.AwaitingPhysicalDelivery);

        shipment.Complete = true;
        Probe(sim, projectId, "complete", WorkCause.MissingMaterials);
        shipment.Complete = false;
        Probe(sim, projectId, "complete-restored", WorkCause.AwaitingPhysicalDelivery);

        shipment.ProjectId = null;
        Probe(sim, projectId, "unassigned", WorkCause.MissingMaterials);
        shipment.ProjectId = projectId + 1_000_000;
        Probe(sim, projectId, "reassigned", WorkCause.MissingMaterials);
        shipment.ProjectId = projectId;
        Probe(sim, projectId, "reassigned-restored", WorkCause.AwaitingPhysicalDelivery);

        // Same count, different object: a cache across Advance(2) would miss it.
        sim.State.Shipments[index] = new Shipment { Id = shipmentId, ProjectId = null };
        Probe(sim, projectId, "same-count-replacement", WorkCause.MissingMaterials);
        sim.State.Shipments[index] = shipment;
        Probe(sim, projectId, "replacement-restored", WorkCause.AwaitingPhysicalDelivery);

        sim.State.Shipments.RemoveAt(index);
        Probe(sim, projectId, "removed", WorkCause.MissingMaterials);
        sim.State.Shipments.Insert(index, shipment);
        Probe(sim, projectId, "inserted", WorkCause.AwaitingPhysicalDelivery);

        shipment.Complete = true;
        var added = new Shipment { Id = sim.State.NextId + 100_000,
            ProjectId = projectId, Complete = false };
        sim.State.Shipments.Add(added);
        Probe(sim, projectId, "appended", WorkCause.AwaitingPhysicalDelivery);
        Assert.True(sim.State.Shipments.Remove(added));
        Probe(sim, projectId, "append-removed", WorkCause.MissingMaterials);
        shipment.Complete = false;
        Probe(sim, projectId, "restored", WorkCause.AwaitingPhysicalDelivery);

        // Live Cargo must change the actual early-crew return decision. This
        // isolated, unsaved state deliberately emulates a malformed old save.
        var synthetic = CitySimulation.FromJson(sim.SaveToJson());
        var project = synthetic.State.Projects.Single(p => p.Id == projectId);
        var load = synthetic.State.Shipments.Single(s => s.Id == shipmentId);
        var truck = synthetic.State.Vehicles.Single(v => v.Id == load.VehicleId);
        truck.Status = VehicleStatus.Parked;
        truck.CrewAboard = true;
        truck.CrewCount = 3;
        project.CrewVehicleId = truck.Id;
        load.Cargo.Clear();
        foreach (var requirement in project.Required)
            load.Cargo[requirement.Key] = requirement.Value;
        Progress.Invoke(synthetic, [2.0]);
        Assert.Equal(truck.Id, project.CrewVehicleId);
        load.Cargo.Clear();
        Progress.Invoke(synthetic, [2.0]);
        Assert.Null(project.CrewVehicleId);
        Console.WriteLine("PR57_PROGRESS_MUTATION cargo-live=True crew-return=True synthetic=True");

        // Resume the genuine state at the host cadence, including save/load.
        Capture(sim, "after-restoration");
        var resumed = CitySimulation.FromJson(sim.SaveToJson());
        for (var i = 0; i < 5; i++)
        {
            sim.Advance(2);
            resumed.Advance(2);
            Assert.Equal(sim.SaveToJson(), resumed.SaveToJson());
        }
        Capture(sim, "after-resume");
    }
}
