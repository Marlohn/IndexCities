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
        var project = sim.State.Projects.Single(p => p.BuildingId == placed.Id);
        Assert.Equal(WorkStatus.Waiting, project.Status);
        Assert.True(project.Required.Count > 1);
        var pending = Assert.Single(sim.State.Shipments, s =>
            s.ProjectId == project.Id && !s.Complete);
        return (sim, project.Id, pending.Id);
    }

    static void Probe(CitySimulation sim, int projectId, string stage, string expected)
    {
        // Reflection bypasses Advance(2), which normally opens the shared
        // shipment step. Start a fresh step here so each external mutation
        // exercises the opt-in index rather than an uninitialized fallback.
        if (Environment.GetEnvironmentVariable("PR57_SHARED_STEP_SHIPMENTS") == "1")
        {
            typeof(CitySimulation).GetMethod("Pr57SharedShipmentsBeginStep",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(sim, null);
        }
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
    public void Shared_step_membership_preserves_order_live_predicates_append_and_epoch_rebuild()
    {
        // Isolated structural oracle, not a performance or full-Advance parity proof.
        // Invoke the PR57 opt-in workset directly to test its causal invalidation
        // contract even when the normal process runs without the opt-in flag.
        if (Environment.GetEnvironmentVariable("PR57_SHARED_STEP_SHIPMENTS") != "1") return;
        var sim = new CitySimulation(42, new() { ExternalTruckCount = 1 });
        var type = typeof(CitySimulation);
        var begin = type.GetMethod("Pr57SharedShipmentsBeginStep",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var query = type.GetMethod("Pr57SharedPendingProjectShipments",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var changed = type.GetMethod("Pr57SharedShipmentProjectChanged",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var a = new Shipment { Id = 910001, ProjectId = 77 };
        var b = new Shipment { Id = 910002, ProjectId = 88 };
        var c = new Shipment { Id = 910003, ProjectId = 77 };
        sim.State.Shipments.AddRange([a, b, c]);
        int[] Read(int projectId) => ((IEnumerable<Shipment>)query.Invoke(sim,
            [projectId])!).Select(s => s.Id).ToArray();

        begin.Invoke(sim, null);
        Assert.Equal(new[] { a.Id, c.Id }, Read(77));
        a.Complete = true;
        Assert.Equal(new[] { c.Id }, Read(77));
        a.Complete = false;
        var d = new Shipment { Id = 910004, ProjectId = 77 };
        sim.State.Shipments.Add(d);
        Assert.Equal(new[] { a.Id, c.Id, d.Id }, Read(77));

        // The production writer must publish the epoch on ProjectId changes.
        // This deliberately calls the same invalidation hook as that writer;
        // arbitrary unannounced external list mutation remains unsupported.
        b.ProjectId = 77;
        changed.Invoke(sim, null);
        Assert.Equal(new[] { a.Id, b.Id, c.Id, d.Id }, Read(77));
        c.ProjectId = 88;
        changed.Invoke(sim, null);
        Assert.Equal(new[] { a.Id, b.Id, d.Id }, Read(77));
        Assert.Equal(new[] { c.Id }, Read(88));
        begin.Invoke(sim, null);
        Assert.Equal(new[] { a.Id, b.Id, d.Id }, Read(77));

        // Step-boundary replacement with unchanged count must not retain stale
        // pooled object references. Mid-Step unannounced edits remain unsupported.
        var replacement = new Shipment { Id = 910005, ProjectId = 88 };
        sim.State.Shipments[0] = replacement;
        begin.Invoke(sim, null);
        Assert.Equal(new[] { b.Id, d.Id }, Read(77));
        Assert.Equal(new[] { replacement.Id, c.Id }, Read(88));

        // Shrinking forces a full rebuild before an append can reuse the bucket.
        sim.State.Shipments.RemoveAt(1);
        Assert.Equal(new[] { d.Id }, Read(77));
        var appended = new Shipment { Id = 910006, ProjectId = 77 };
        sim.State.Shipments.Add(appended);
        Assert.Equal(new[] { d.Id, appended.Id }, Read(77));
        Assert.Equal(new[] { replacement.Id, c.Id }, Read(88));
        Console.WriteLine("PR57_SHARED_STEP_STRUCTURAL order=True live=True append=True epoch=True boundaryReplace=True shrink=True");
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
