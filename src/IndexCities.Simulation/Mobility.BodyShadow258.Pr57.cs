namespace IndexCities.Simulation;

// PR57 #258: bounded, read-only full BodyAcrossLane candidate projection.
// Actor-major refresh is not a causal closure: nonlocal tow writes, public edits,
// road edits, and transient writes can stale the mirror. Canonical physics alone
// decides every hold. No cross-Advance cache, changed save, or skipped slice.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Body258Enabled =
        Environment.GetEnvironmentVariable("PR57_258_BODY_SHADOW") == "1";

    readonly record struct Pr57Body258Projection(
        Point Position, long HeadingBits, VehicleStatus Status,
        bool PassengerCar, PavingMachine Machine, Point? Dock,
        bool Gateway, bool Leaving, bool OnCarriageway, bool OnSitePath,
        double HalfLength, double HalfWidth);

    Vehicle[]? _pr57Body258Vehicles;
    Pr57Body258Projection[]? _pr57Body258Rows;
    readonly Dictionary<int,int> _pr57Body258Index = new();
    bool _pr57Body258Active;
    long _pr57Body258BuildTicks, _pr57Body258RefreshTicks, _pr57Body258ShadowTicks;
    long _pr57Body258AuditTicks, _pr57Body258Compared, _pr57Body258Samples;
    long _pr57Body258Matched, _pr57Body258Mismatched, _pr57Body258Frozen;
    long _pr57Body258Missing, _pr57Body258Positive, _pr57Body258ShadowPositive;
    long _pr57Body258SelfMismatch, _pr57Body258ThirdMismatch, _pr57Body258AuditChanged;

    Pr57Body258Projection Pr57Body258Capture(Vehicle o) => new(
        o.Position, BitConverter.DoubleToInt64Bits(o.Heading),
        o.Status, o.PassengerCar, o.Machine, o.DockPosition,
        AtGateway(o), LeavesDock(o), OnCarriageway(o), OnSitePath(o),
        BodyHalfLength(o), BodyHalfWidth(o));

    // This is the complete candidate-side scalar read of BodyAcrossLane.
    // The reader stays live. Road/tuning dependencies are resolved at capture,
    // not assumed immutable: output mismatches expose stale snapshots.
    double? Pr57Body258Evaluate(Vehicle v, Pr57Body258Projection o)
    {
        if (o.PassengerCar || o.Machine != PavingMachine.None ||
            o.Position.Distance(v.Position) > 20 ||
            o.Gateway && AtGateway(v)) return null;
        var docked =
            o.Status is VehicleStatus.Unloading or VehicleStatus.Parked &&
                o.Dock is {} at && at.Distance(o.Position) < .1 ||
            o.Leaving && !o.OnCarriageway || o.OnSitePath;
        if (!docked) return null;
        var relative = o.Position - v.Position;
        var forward = relative.X * Math.Sin(v.Heading) +
                      relative.Z * Math.Cos(v.Heading);
        if (forward <= 0) return null;
        var side = Math.Abs(relative.X * Math.Cos(v.Heading) -
                            relative.Z * Math.Sin(v.Heading));
        var turn = BitConverter.Int64BitsToDouble(o.HeadingBits) - v.Heading;
        return side < o.HalfLength * Math.Abs(Math.Sin(turn)) +
                      o.HalfWidth * Math.Abs(Math.Cos(turn)) + BodyHalfWidth(v)
            ? forward : null;
    }

    static bool Pr57Body258Same(double? a, double? b) =>
        a.HasValue == b.HasValue &&
        (!a.HasValue || BitConverter.DoubleToInt64Bits(a.Value) ==
                        BitConverter.DoubleToInt64Bits(b!.Value));

    void Pr57Body258BeginSlice()
    {
        _pr57Body258Active = Pr57Body258Enabled && _pr57ExclusiveActive;
        if (!_pr57Body258Active) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57Body258Vehicles = State.Vehicles.ToArray();
        _pr57Body258Rows = new Pr57Body258Projection[_pr57Body258Vehicles.Length];
        _pr57Body258Index.Clear();
        for (var i = 0; i < _pr57Body258Vehicles.Length; i++)
        {
            var o = _pr57Body258Vehicles[i];
            _pr57Body258Index.Add(o.Id, i);
            _pr57Body258Rows[i] = Pr57Body258Capture(o);
        }
        _pr57Body258BuildTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57Body258RefreshTicks = _pr57Body258ShadowTicks = _pr57Body258AuditTicks = 0;
        _pr57Body258Compared = _pr57Body258Samples = _pr57Body258Matched = 0;
        _pr57Body258Mismatched = _pr57Body258Frozen = _pr57Body258Missing = 0;
        _pr57Body258Positive = _pr57Body258ShadowPositive = 0;
        _pr57Body258SelfMismatch = _pr57Body258ThirdMismatch = _pr57Body258AuditChanged = 0;
    }

    void Pr57Body258Observe(Vehicle actor, Vehicle other, double? canonical)
    {
        if (!_pr57Body258Active) return;
        if (!_pr57Body258Index.TryGetValue(other.Id, out var index))
        {
            _pr57Body258Missing++;
            return;
        }
        // Lane entry intentionally uses a frozen entryFrame, not the live actor.
        if (!ReferenceEquals(_pr57Body258Vehicles![index], other))
        {
            _pr57Body258Frozen++;
            return;
        }
        var sample = (_pr57Body258Compared & 63) == 0;
        var start = sample ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        var predicted = Pr57Body258Evaluate(actor, _pr57Body258Rows![index]);
        if (sample)
        {
            _pr57Body258ShadowTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
            _pr57Body258Samples++;
        }
        _pr57Body258Compared++;
        if (canonical.HasValue) _pr57Body258Positive++;
        if (predicted.HasValue) _pr57Body258ShadowPositive++;
        if (Pr57Body258Same(canonical, predicted)) _pr57Body258Matched++;
        else
        {
            _pr57Body258Mismatched++;
            if (actor.Id == other.Id) _pr57Body258SelfMismatch++;
            else _pr57Body258ThirdMismatch++;
            if (_pr57Body258Mismatched <= 4)
                Console.WriteLine($"PR57_258_MISMATCH actor={actor.Id} other={other.Id} " +
                    $"canonical={canonical} shadow={predicted} " +
                    $"self={actor.Id == other.Id} scope=READ_ONLY");
        }
    }

    void Pr57Body258AfterActor(Vehicle actor)
    {
        if (!_pr57Body258Active || _pr57Body258Rows is null) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_pr57Body258Index.TryGetValue(actor.Id, out var index))
            _pr57Body258Rows[index] = Pr57Body258Capture(actor);
        _pr57Body258RefreshTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57Body258EndSlice()
    {
        if (!_pr57Body258Active) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        // One O(N) post-sync audit, never a per-read/per-actor O(N) census.
        // Detects only net differences in the selected derived predicate inputs.
        foreach (var o in State.Vehicles)
            if (!_pr57Body258Index.TryGetValue(o.Id, out var index) ||
                _pr57Body258Rows![index] != Pr57Body258Capture(o))
                _pr57Body258AuditChanged++;
        _pr57Body258AuditTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        static double Ms(long ticks) =>
            ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_258_BODY_SHADOW compared={_pr57Body258Compared} matched={_pr57Body258Matched} " +
            $"mismatched={_pr57Body258Mismatched} selfMismatch={_pr57Body258SelfMismatch} " +
            $"thirdMismatch={_pr57Body258ThirdMismatch} frozenFrame={_pr57Body258Frozen} " +
            $"missing={_pr57Body258Missing} canonicalPositive={_pr57Body258Positive} " +
            $"shadowPositive={_pr57Body258ShadowPositive} buildMs={Ms(_pr57Body258BuildTicks):F6} " +
            $"refreshMs={Ms(_pr57Body258RefreshTicks):F6} " +
            $"sampledShadowMs={Ms(_pr57Body258ShadowTicks):F6} sampledCalls={_pr57Body258Samples} " +
            $"postSyncNetDifferences={_pr57Body258AuditChanged} auditMs={Ms(_pr57Body258AuditTicks):F6} " +
            "scope=BODY_ACROSS_LANE_ONLY actorMajor=True canonicalDecides=True " +
            "perQueryOracle=True transientWritesNotCovered=True observational=True intrusive=True");
        _pr57Body258Active = false;
        _pr57Body258Vehicles = null;
        _pr57Body258Rows = null;
        _pr57Body258Index.Clear();
    }
}
