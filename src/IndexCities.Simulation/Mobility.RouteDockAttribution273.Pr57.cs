namespace IndexCities.Simulation;

// PR57 #273: bounded, strictly observational attribution of the *real*
// route/dock helper calls for three persistent blocked vehicles. The canonical
// helper is always called once, in the same order and with the same arguments.
// Only first/last 100 physical sub-slices are sampled. No cache, no skipped
// actor, no changed physics, no persisted diagnostic state.
public sealed partial class CitySimulation
{
    static readonly bool Pr57273Enabled =
        Environment.GetEnvironmentVariable("PR57_273_ROUTE_DOCK") == "1";
    static readonly string[] Pr57273Names =
        ["WayOut", "AdmitManoeuvre", "NextSharedOnApproach",
         "HoldBeforeBayJunction", "ClaimHolderPoses", "BayEntryHold"];
    static readonly int[] Pr57273Ids = [1453, 1477, 23];
    // window, vehicle, helper, metric: calls, nonempty/true, ticks, bytes.
    readonly long[,,,] _pr57273Rows = new long[2, 3, 6, 4];
    int _pr57273Slices, _pr57273Window;
    bool _pr57273Sampling;

    void Pr57273BeginSlice(double dt, double? operationsDt)
    {
        _pr57273Sampling = false;
        if (!Pr57273Enabled || dt <= 0 || operationsDt is not null) return;
        _pr57273Slices++;
        _pr57273Window = _pr57273Slices <= 100 ? 0
            : _pr57273Slices is >= 501 and <= 600 ? 1 : -1;
        _pr57273Sampling = _pr57273Window >= 0;
    }

    bool Pr57273Sample(Vehicle vehicle, out int slot)
    {
        slot = vehicle.Id == 1453 ? 0 : vehicle.Id == 1477 ? 1
            : vehicle.Id == 23 ? 2 : -1;
        return _pr57273Sampling && slot >= 0;
    }

    void Pr57273Record(int slot, int helper, long ticks, long bytes, bool positive)
    {
        var window = _pr57273Window;
        _pr57273Rows[window, slot, helper, 0]++;
        if (positive) _pr57273Rows[window, slot, helper, 1]++;
        _pr57273Rows[window, slot, helper, 2] +=
            System.Diagnostics.Stopwatch.GetTimestamp() - ticks;
        _pr57273Rows[window, slot, helper, 3] +=
            GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    List<(Vehicle Body, Point Position, double Heading)> Pr57273WayOut(
        Vehicle v, out List<RoadNode> spaces)
    {
        if (!Pr57273Sample(v, out var slot)) return WayOut(v, out spaces);
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var result = WayOut(v, out spaces);
        Pr57273Record(slot, 0, ticks, bytes, result.Count > 0);
        return result;
    }

    bool Pr57273Admit(Vehicle v,
        List<(Vehicle Body, Point Position, double Heading)> way,
        List<RoadNode> spaces, Point dock, bool yieldToTraffic)
    {
        if (!Pr57273Sample(v, out var slot))
            return AdmitManoeuvre(v, way, spaces, dock, yieldToTraffic);
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        Pr57275Observe(v, slot, 0);
        var result = AdmitManoeuvre(v, way, spaces, dock, yieldToTraffic);
        Pr57273Record(slot, 1, ticks, bytes, result);
        return result;
    }

    bool Pr57273NextShared(Vehicle v, RoadNode node, Point incoming,
        double length, out RoadNode junction, out int index)
    {
        if (!Pr57273Sample(v, out var slot))
            return NextSharedOnApproach(v, node, incoming, length, out junction, out index);
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var result = NextSharedOnApproach(v, node, incoming, length, out junction, out index);
        Pr57273Record(slot, 2, ticks, bytes, result);
        return result;
    }

    bool Pr57273HoldBefore(Vehicle v, int junctionIndex,
        out List<(Vehicle Body, Point Position, double Heading)> exits)
    {
        if (!Pr57273Sample(v, out var slot))
            return HoldBeforeBayJunction(v, junctionIndex, out exits);
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var result = HoldBeforeBayJunction(v, junctionIndex, out exits);
        Pr57273Record(slot, 3, ticks, bytes, result);
        return result;
    }

    List<(Vehicle Body, Point Position, double Heading)> Pr57273Claim(
        Vehicle v, RoadNode junction)
    {
        if (!Pr57273Sample(v, out var slot)) return ClaimHolderPoses(v, junction);
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var result = ClaimHolderPoses(v, junction);
        Pr57273Record(slot, 4, ticks, bytes, result.Count > 0);
        return result;
    }

    List<(Vehicle Body, Point Position, double Heading)> Pr57273BayEntryHold(Vehicle v)
    {
        if (!Pr57273Sample(v, out var slot)) return BayEntryHold(v);
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        Pr57275Observe(v, slot, 1);
        var result = BayEntryHold(v);
        Pr57273Record(slot, 5, ticks, bytes, result.Count > 0);
        return result;
    }

    void Pr57273EndSlice()
    {
        if (!Pr57273Enabled || !_pr57273Sampling) return;
        _pr57273Sampling = false; // EndSlice may be called twice; report once.
        if (_pr57273Slices != 600) return;
        for (var window = 0; window < 2; window++)
            for (var slot = 0; slot < 3; slot++)
            {
                long ticks = 0, bytes = 0, calls = 0;
                for (var helper = 0; helper < 6; helper++)
                {
                    var count = _pr57273Rows[window, slot, helper, 0];
                    var t = _pr57273Rows[window, slot, helper, 2];
                    var b = _pr57273Rows[window, slot, helper, 3];
                    calls += count; ticks += t; bytes += b;
                    Console.WriteLine(
                        $"PR57_273_HELPER window={window} id={Pr57273Ids[slot]} " +
                        $"helper={Pr57273Names[helper]} calls={count} " +
                        $"positive={_pr57273Rows[window, slot, helper, 1]} " +
                        $"ms={t * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F6} " +
                        $"bytes={b} observational=True");
                }
                Console.WriteLine(
                    $"PR57_273_SUM window={window} id={Pr57273Ids[slot]} " +
                    $"calls={calls} inclusiveHelperMs={ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F6} " +
                    $"helperBytes={bytes} slices=100 scope=selectedS1S2Helpers " +
                    "otherS1S2OperationsUnmeasured=True noSkippedSlices=True");
            }
        Pr57275Report();
        Pr57276Report();
    }
}
