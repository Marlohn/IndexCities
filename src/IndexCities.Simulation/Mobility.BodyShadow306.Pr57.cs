namespace IndexCities.Simulation;

// PR57 #306: read-only actor-major candidate-side projection for the exact
// BodyOnWay prefix observed in #305. Canonical live Vehicle references still
// decide every gate; no reused decision, skipped actor/slice, or saved state.
// Refresh after the actor is deliberately NOT assumed to cover nonlocal writes.
public sealed partial class CitySimulation
{
    static readonly bool Pr57306Enabled =
        Environment.GetEnvironmentVariable("PR57_306_BODY_SHADOW") == "1";

    readonly record struct Pr57306BodyRow(
        long XBits, long ZBits, long HeadingBits, VehicleStatus Status,
        int? TowTruckId, bool PassengerCar, PavingMachine Machine,
        long HalfLengthBits, long HalfWidthBits, bool Gateway);

    readonly Dictionary<int, int> _pr57306Index = new();
    readonly HashSet<int> _pr57306Processed = new();
    Pr57306BodyRow[]? _pr57306Rows;
    bool _pr57306Active;
    long _pr57306BuildTicks, _pr57306RefreshTicks, _pr57306ReadTicks;
    long _pr57306AuditTicks, _pr57306Reads, _pr57306Stale;
    long _pr57306StaleAfterProcessed, _pr57306Missing;
    long _pr57306Refreshes, _pr57306PostSync, _pr57306Prints;

    Pr57306BodyRow Pr57306Capture(Vehicle o) => new(
        BitConverter.DoubleToInt64Bits(o.Position.X),
        BitConverter.DoubleToInt64Bits(o.Position.Z),
        BitConverter.DoubleToInt64Bits(o.Heading),
        o.Status, o.TowTruckId, o.PassengerCar, o.Machine,
        BitConverter.DoubleToInt64Bits(BodyHalfLength(o)),
        BitConverter.DoubleToInt64Bits(BodyHalfWidth(o)),
        AtGateway(o));

    void Pr57306BeginSlice()
    {
        _pr57306Active = Pr57306Enabled && _pr57300Calls is >= 76 and <= 100;
        if (!_pr57306Active) return;
        Pr57306Build();
    }

    // Test seam: construct the observational projection on a synthetic fixture.
    void Pr57306Build()
    {
        _pr57306Active = true;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57306Index.Clear();
        _pr57306Processed.Clear();
        _pr57306Rows = new Pr57306BodyRow[State.Vehicles.Count];
        for (var i = 0; i < State.Vehicles.Count; i++)
        {
            var o = State.Vehicles[i];
            _pr57306Index.Add(o.Id, i); // preserve canonical State.Vehicles order
            _pr57306Rows[i] = Pr57306Capture(o);
        }
        _pr57306BuildTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57306RefreshTicks = _pr57306ReadTicks = _pr57306AuditTicks = 0;
        _pr57306Reads = _pr57306Stale = _pr57306StaleAfterProcessed = 0;
        _pr57306Missing = _pr57306Refreshes = _pr57306PostSync = _pr57306Prints = 0;
        _pr57307Published = _pr57307PublicationTicks = 0;
    }

    // Called at the exact original candidate visitation, before cheap filters.
    void Pr57306Read(Vehicle reader, Vehicle candidate)
    {
        if (!_pr57306Active) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57306Reads++;
        if (!_pr57306Index.TryGetValue(candidate.Id, out var index))
            _pr57306Missing++;
        else if (_pr57306Rows![index] != Pr57306Capture(candidate))
        {
            _pr57306Stale++;
            if (_pr57306Processed.Contains(candidate.Id))
                _pr57306StaleAfterProcessed++;
            if (_pr57306Prints++ < 4)
                Console.WriteLine($"PR57_306_STALE call={_pr57300Calls} slice={_pr57302CurrentSlice} reader={reader.Id} candidate={candidate.Id} processed={_pr57306Processed.Contains(candidate.Id)} scope=SHADOW_ONLY");
        }
        _pr57306ReadTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57306AfterActor(Vehicle actor)
    {
        if (!_pr57306Active) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_pr57306Index.TryGetValue(actor.Id, out var index))
        {
            _pr57306Rows![index] = Pr57306Capture(actor);
            _pr57306Refreshes++;
        }
        _pr57306Processed.Add(actor.Id);
        _pr57306RefreshTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57306EndSlice()
    {
        if (!_pr57306Active) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        foreach (var o in State.Vehicles)
            if (!_pr57306Index.TryGetValue(o.Id, out var index) ||
                _pr57306Rows![index] != Pr57306Capture(o))
                _pr57306PostSync++;
        _pr57306AuditTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        if (_pr57306Reads > 0 || _pr57306Stale > 0)
        {
            static double Ms(long ticks) =>
                ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(
                $"PR57_306_SHADOW call={_pr57300Calls} slice={_pr57302CurrentSlice} " +
                $"vehicles={State.Vehicles.Count} reads={_pr57306Reads} " +
                $"stale={_pr57306Stale} staleAfterProcessed={_pr57306StaleAfterProcessed} " +
                $"missing={_pr57306Missing} refreshes={_pr57306Refreshes} " +
                $"postSync={_pr57306PostSync} nonlocalPublishes={_pr57307Published} " +
                $"nonlocalMs={Ms(_pr57307PublicationTicks):F6} buildMs={Ms(_pr57306BuildTicks):F6} " +
                $"refreshMs={Ms(_pr57306RefreshTicks):F6} readMs={Ms(_pr57306ReadTicks):F6} " +
                $"auditMs={Ms(_pr57306AuditTicks):F6} " +
                "scope=SELECTED_CANDIDATE_FIELDS_ONLY_NOT_CAUSAL_CLOSURE canonicalDecides=True");
        }
        _pr57306Rows = null;
        _pr57306Index.Clear();
        _pr57306Processed.Clear();
        _pr57306Active = false;
    }
}
