using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only adversarial experiment. It does not change the engine or claim
// that a different list order is a permitted production transformation.
public sealed class Pr57VehicleOrderSensitivity
{
    [Fact]
    public void Real_save_vehicle_order_sensitivity_for_speculative_batching()
    {
        var path = Environment.GetEnvironmentVariable("PR57_CONTROLLED_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_CONTROLLED_CASE") ?? "unspecified";
        var json = File.ReadAllText(path);
        var baseline = CitySimulation.FromJson(json);
        var reordered = CitySimulation.FromJson(json);
        Assert.Equal(baseline.SaveToJson(), reordered.SaveToJson());
        Assert.NotEmpty(reordered.State.Vehicles);

        // Preserve every actor and all properties; change only iteration order.
        reordered.State.Vehicles.Reverse();
        const int steps = 24;
        var differingVehicleSteps = 0;
        var differingCitizenSteps = 0;
        var firstVehicleDifference = -1;
        var firstCitizenDifference = -1;
        for (var step = 0; step < steps; step++)
        {
            baseline.Advance(2);
            reordered.Advance(2);
            // Compare by identity, so the initial list permutation alone
            // cannot create a false positive.
            var a = baseline.State.Vehicles.OrderBy(v => v.Id)
                .Select(v => (v.Id, v.Position, v.Heading, v.Status,
                    v.RouteIndex, v.Fuel, v.Cause, v.WaitingSince)).ToArray();
            var b = reordered.State.Vehicles.OrderBy(v => v.Id)
                .Select(v => (v.Id, v.Position, v.Heading, v.Status,
                    v.RouteIndex, v.Fuel, v.Cause, v.WaitingSince)).ToArray();
            var c = baseline.State.Citizens.OrderBy(v => v.Id)
                .Select(v => (v.Id, v.Position, v.Activity, v.VehicleId,
                    v.HungerMicroseconds, v.HealthUnits)).ToArray();
            var d = reordered.State.Citizens.OrderBy(v => v.Id)
                .Select(v => (v.Id, v.Position, v.Activity, v.VehicleId,
                    v.HungerMicroseconds, v.HealthUnits)).ToArray();
            if (!a.SequenceEqual(b))
            {
                differingVehicleSteps++;
                if (firstVehicleDifference < 0)
                {
                    firstVehicleDifference = step;
                    // Diagnostic only: the first identity-level disagreement, not a
                    // claim that list reordering is a valid engine optimization.
                    var first = a.Zip(b).First(pair => !pair.First.Equals(pair.Second));
                    Console.WriteLine(
                        $"PR57_ORDER_FIRST case={label} step={step} " +
                        $"baseline={first.First} reordered={first.Second} " +
                        $"vehicleCount={a.Length}");
                }
            }
            if (!c.SequenceEqual(d))
            {
                differingCitizenSteps++;
                if (firstCitizenDifference < 0) firstCitizenDifference = step;
            }
        }
        Console.WriteLine(
            $"PR57_ORDER_REAL case={label} steps={steps} " +
            $"vehicles={baseline.State.Vehicles.Count} " +
            $"vehicleDivergentSteps={differingVehicleSteps} " +
            $"citizenDivergentSteps={differingCitizenSteps} " +
            $"firstVehicleDifference={firstVehicleDifference} " +
            $"firstCitizenDifference={firstCitizenDifference} " +
            "scope=permuted-public-vehicle-list notEngineParity=True");
        // Both outcomes are informative: a divergence refutes naive batch
        // reordering; no divergence in 24 Steps is NOT a universal proof.
    }
    // A minimal local-dependence probe, not a proof of safe parallel execution.
    // Each trial starts from the same authentic save and swaps exactly one
    // adjacent pair; all other actors and the full Advance(2) are preserved.
    [Fact]
    public void Real_save_adjacent_vehicle_swap_dependency_probe()
    {
        var path = Environment.GetEnvironmentVariable("PR57_CONTROLLED_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path)) return;
        var label = Environment.GetEnvironmentVariable("PR57_CONTROLLED_CASE") ?? "unspecified";
        var json = File.ReadAllText(path);
        var reference = CitySimulation.FromJson(json);
        // The swap candidates must come from the input save, not from the
        // reference state after Advance(2), which may add/remove vehicles.
        var pairs = reference.State.Vehicles.Count - 1;
        Assert.True(pairs > 0);
        reference.Advance(2);
        var expected = reference.State.Vehicles.OrderBy(v => v.Id)
            .Select(v => (v.Id, v.Position, v.Heading, v.Status,
                v.RouteIndex, v.Fuel, v.Cause, v.WaitingSince)).ToArray();
        var expectedCitizens = reference.State.Citizens.OrderBy(v => v.Id)
            .Select(v => (v.Id, v.Position, v.Activity, v.VehicleId,
                v.HungerMicroseconds, v.HealthUnits)).ToArray();
        var changedVehiclePairs = 0;
        var changedCitizenPairs = 0;
        var firstPair = -1;
        for (var i = 0; i < pairs; i++)
        {
            var trial = CitySimulation.FromJson(json);
            var left = trial.State.Vehicles[i].Id;
            var right = trial.State.Vehicles[i + 1].Id;
            (trial.State.Vehicles[i], trial.State.Vehicles[i + 1]) =
                (trial.State.Vehicles[i + 1], trial.State.Vehicles[i]);
            trial.Advance(2);
            var actual = trial.State.Vehicles.OrderBy(v => v.Id)
                .Select(v => (v.Id, v.Position, v.Heading, v.Status,
                    v.RouteIndex, v.Fuel, v.Cause, v.WaitingSince)).ToArray();
            var citizens = trial.State.Citizens.OrderBy(v => v.Id)
                .Select(v => (v.Id, v.Position, v.Activity, v.VehicleId,
                    v.HungerMicroseconds, v.HealthUnits)).ToArray();
            if (!expected.SequenceEqual(actual))
            {
                changedVehiclePairs++;
                if (firstPair < 0)
                {
                    firstPair = i;
                    Console.WriteLine($"PR57_ADJACENT_FIRST case={label} index={i} leftId={left} rightId={right}");
                    // A projection can hide a causal change in shared state.
                    // Inspect authentic project/shipment state after the same full Step.
                    // Sorting by identity removes only collection-order noise.
                    var referenceProjects = System.Text.Json.JsonSerializer.Serialize(
                        reference.State.Projects.OrderBy(p => p.Id).ToArray());
                    var trialProjects = System.Text.Json.JsonSerializer.Serialize(
                        trial.State.Projects.OrderBy(p => p.Id).ToArray());
                    var referenceShipments = System.Text.Json.JsonSerializer.Serialize(
                        reference.State.Shipments.OrderBy(sh => sh.Id).ToArray());
                    var trialShipments = System.Text.Json.JsonSerializer.Serialize(
                        trial.State.Shipments.OrderBy(sh => sh.Id).ToArray());
                    // Identify the first shared entity whose actual serialized state differs.
                    // This is a diagnostic witness, not a proof of the responsible writer.
                    var firstProject = reference.State.Projects.OrderBy(p => p.Id)
                        .Zip(trial.State.Projects.OrderBy(p => p.Id))
                        .FirstOrDefault(pair =>
                            !String.Equals(
                                System.Text.Json.JsonSerializer.Serialize(pair.First),
                                System.Text.Json.JsonSerializer.Serialize(pair.Second),
                                StringComparison.Ordinal));
                    var firstShipment = reference.State.Shipments.OrderBy(sh => sh.Id)
                        .Zip(trial.State.Shipments.OrderBy(sh => sh.Id))
                        .FirstOrDefault(pair =>
                            !String.Equals(
                                System.Text.Json.JsonSerializer.Serialize(pair.First),
                                System.Text.Json.JsonSerializer.Serialize(pair.Second),
                                StringComparison.Ordinal));
                    Console.WriteLine(
                        $"PR57_ADJACENT_SHARED_FIRST case={label} index={i} " +
                        $"projectId={firstProject.First?.Id.ToString() ?? "none"} " +
                        $"shipmentId={firstShipment.First?.Id.ToString() ?? "none"} " +
                        $"projectReference={System.Text.Json.JsonSerializer.Serialize(firstProject.First)} " +
                        $"projectTrial={System.Text.Json.JsonSerializer.Serialize(firstProject.Second)} " +
                        $"shipmentReference={System.Text.Json.JsonSerializer.Serialize(firstShipment.First)} " +
                        $"shipmentTrial={System.Text.Json.JsonSerializer.Serialize(firstShipment.Second)}");
                    Console.WriteLine(
                        $"PR57_ADJACENT_SHARED case={label} index={i} leftId={left} rightId={right} " +
                        $"projectsEqual={String.Equals(referenceProjects, trialProjects, StringComparison.Ordinal)} " +
                        $"shipmentsEqual={String.Equals(referenceShipments, trialShipments, StringComparison.Ordinal)} " +
                        $"projectCount={reference.State.Projects.Count}/{trial.State.Projects.Count} " +
                        $"shipmentCount={reference.State.Shipments.Count}/{trial.State.Shipments.Count} " +
                        "scope=sorted-identity-shared-state-json notEngineParity=True");
                }
            }
            if (!expectedCitizens.SequenceEqual(citizens)) changedCitizenPairs++;
        }
        Console.WriteLine(
            $"PR57_ADJACENT_REAL case={label} pairs={pairs} " +
            $"vehicleChangedPairs={changedVehiclePairs} citizenChangedPairs={changedCitizenPairs} " +
            $"firstChangedPair={firstPair} scope=single-adjacent-swap-one-full-Advance2 " +
            "notEngineParity=True");
    }

}
