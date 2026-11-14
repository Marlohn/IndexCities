namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 #298: observational boundary snapshot of the existing #155
    // per-actor class clocks. No decisions, state, caches or scheduling read it.
    // A class is defined by final actor Cause/Status, not causal independence.
    // Layout: [physical slices, 8 x (evaluations, ticks, allocated bytes,
    // intersection-claim changes, manoeuvre-space-count changes)].
    public long[] Pr57298VehicleCostSnapshot()
    {
        var snapshot = new long[1 + 8 * 5];
        snapshot[0] = _pr57MovementAllocCalls;
        for (var g = 0; g < 8; g++)
        {
            var k = 1 + g * 5;
            snapshot[k] = _pr57VehicleCostCalls[g];
            snapshot[k + 1] = _pr57VehicleCostTicks[g];
            snapshot[k + 2] = _pr57VehicleCostBytes[g];
            snapshot[k + 3] = _pr57VehicleCostClaimChanges[g];
            snapshot[k + 4] = _pr57VehicleCostSpaceCountChanges[g];
        }
        return snapshot;
    }
}
