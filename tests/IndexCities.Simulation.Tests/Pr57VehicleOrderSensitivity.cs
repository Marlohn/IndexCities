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
                if (firstVehicleDifference < 0) firstVehicleDifference = step;
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
}
