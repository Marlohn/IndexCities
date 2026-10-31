namespace IndexCities.Simulation;

// PR57 #545: writer-driven, slice-local logical dependency index.
// This is NOT a collision broadphase or a cached physical decision. It indexes
// only the current next-road membership used by two junction readers; every
// candidate's live state, BodyOnWay/CanTurnNow, priority and signal are still
// evaluated in the original State.Vehicles order.
public sealed partial class CitySimulation
{
    static readonly bool Pr57WriterJunctionEnabled =
        Environment.GetEnvironmentVariable("PR57_WRITER_JUNCTION_INDEX") == "1";
    static readonly bool Pr57WriterJunctionVerify =
        Environment.GetEnvironmentVariable("PR57_WRITER_JUNCTION_VERIFY") == "1";
    static readonly bool Pr57WriterJunctionDiag =
        Environment.GetEnvironmentVariable("PR57_WRITER_JUNCTION_DIAG") == "1";

    readonly Dictionary<int, SortedSet<int>> _pr57WriterJunctionBuckets = new();
    readonly Dictionary<Vehicle, int> _pr57WriterJunctionOrdinals =
        new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<Vehicle, int?> _pr57WriterJunctionMembership =
        new(ReferenceEqualityComparer.Instance);
    Vehicle? _pr57WriterJunctionCurrentActor;
    bool _pr57WriterJunctionActive;
    int _pr57WriterJunctionVehicleCount;
    long _pr57WriterJunctionSlices, _pr57WriterJunctionReads,
         _pr57WriterJunctionUpdates, _pr57WriterJunctionFallbacks,
         _pr57WriterJunctionIndexedCandidates;

    static int? Pr57WriterJunctionNext(Vehicle v) =>
        v.RouteIndex >= 0 && v.RouteIndex < v.Route.Count
            ? v.Route[v.RouteIndex] : null;

    void Pr57WriterJunctionAdd(int key, int ordinal)
    {
        if (!_pr57WriterJunctionBuckets.TryGetValue(key, out var bucket))
            _pr57WriterJunctionBuckets.Add(key, bucket = new SortedSet<int>());
        bucket.Add(ordinal);
    }

    void Pr57WriterJunctionBegin(double dt, double? operationsDt)
    {
        _pr57WriterJunctionActive = false;
        _pr57WriterJunctionCurrentActor = null;
        _pr57WriterJunctionBuckets.Clear();
        _pr57WriterJunctionOrdinals.Clear();
        _pr57WriterJunctionMembership.Clear();
        // Only the ordinary movement pass, not operations/replanning or tow
        // handoffs. Those may publish changes to another actor mid-reader.
        if (!Pr57WriterJunctionEnabled || CostProfile is not null ||
            dt <= 0 || operationsDt is not null ||
            State.Vehicles.Any(v => v.TowPhase != TowPhase.None ||
                v.Status == VehicleStatus.Towed || v.TowTruckId is not null ||
                v.TowTargetId is not null))
            return;
        _pr57WriterJunctionVehicleCount = State.Vehicles.Count;
        for (var i = 0; i < State.Vehicles.Count; i++)
        {
            var v = State.Vehicles[i];
            _pr57WriterJunctionOrdinals.Add(v, i);
            var key = Pr57WriterJunctionNext(v);
            _pr57WriterJunctionMembership.Add(v, key);
            if (key is int node) Pr57WriterJunctionAdd(node, i);
        }
        _pr57WriterJunctionActive = true;
        _pr57WriterJunctionSlices++;
    }

    void Pr57WriterJunctionBeforeActor(Vehicle v)
    {
        if (_pr57WriterJunctionActive) _pr57WriterJunctionCurrentActor = v;
    }

    // A writer publishes its changed logical next-node membership before the
    // next actor's reads. Refresh also runs immediately before a read because
    // the current actor may change its own route within its movement quantum.
    void Pr57WriterJunctionRefresh(Vehicle v)
    {
        if (!_pr57WriterJunctionActive) return;
        if (!_pr57WriterJunctionOrdinals.TryGetValue(v, out var ordinal) ||
            ordinal >= State.Vehicles.Count ||
            !ReferenceEquals(State.Vehicles[ordinal], v))
        {
            _pr57WriterJunctionActive = false;
            _pr57WriterJunctionFallbacks++;
            return;
        }
        var next = Pr57WriterJunctionNext(v);
        var prior = _pr57WriterJunctionMembership[v];
        if (next == prior) return;
        if (prior is int old && _pr57WriterJunctionBuckets.TryGetValue(old, out var bucket))
        {
            bucket.Remove(ordinal);
            if (bucket.Count == 0) _pr57WriterJunctionBuckets.Remove(old);
        }
        if (next is int node) Pr57WriterJunctionAdd(node, ordinal);
        _pr57WriterJunctionMembership[v] = next;
        _pr57WriterJunctionUpdates++;
    }

    void Pr57WriterJunctionAfterActor(Vehicle v)
    {
        Pr57WriterJunctionRefresh(v);
        _pr57WriterJunctionCurrentActor = null;
    }

    IEnumerable<Vehicle> Pr57WriterJunctionCandidates(RoadNode junction)
    {
        if (!_pr57WriterJunctionActive) return State.Vehicles;
        if (State.Vehicles.Count != _pr57WriterJunctionVehicleCount)
        {
            _pr57WriterJunctionActive = false;
            _pr57WriterJunctionFallbacks++;
            return State.Vehicles;
        }
        if (_pr57WriterJunctionCurrentActor is not null)
            Pr57WriterJunctionRefresh(_pr57WriterJunctionCurrentActor);
        if (!_pr57WriterJunctionActive) return State.Vehicles;
        IEnumerable<Vehicle> selected = _pr57WriterJunctionBuckets.TryGetValue(junction.Id, out var bucket)
            ? bucket.Select(i => State.Vehicles[i])
            : Array.Empty<Vehicle>();
        if (Pr57WriterJunctionVerify)
        {
            // The oracle checks membership AND original order, including
            // in-place route edits. It never replaces the canonical decision.
            var indexed = selected.ToArray();
            var canonical = State.Vehicles.Where(v => Pr57WriterJunctionNext(v) == junction.Id).ToArray();
            if (!indexed.SequenceEqual(canonical, ReferenceEqualityComparer.Instance))
                throw new InvalidOperationException("PR57_545_WRITER_JUNCTION_MEMBERSHIP_MISMATCH");
            selected = indexed;
        }
        _pr57WriterJunctionReads++;
        if (_pr57WriterJunctionBuckets.TryGetValue(junction.Id, out var current))
            _pr57WriterJunctionIndexedCandidates += current.Count;
        return selected;
    }

    void Pr57WriterJunctionEnd()
    {
        _pr57WriterJunctionActive = false;
        _pr57WriterJunctionCurrentActor = null;
        _pr57WriterJunctionBuckets.Clear();
        _pr57WriterJunctionOrdinals.Clear();
        _pr57WriterJunctionMembership.Clear();
        if (Pr57WriterJunctionDiag && _pr57WriterJunctionSlices > 0 &&
            _pr57WriterJunctionSlices % 100 == 0)
            Console.WriteLine($"PR57_545_WRITER_JUNCTION slices={_pr57WriterJunctionSlices} reads={_pr57WriterJunctionReads} candidates={_pr57WriterJunctionIndexedCandidates} updates={_pr57WriterJunctionUpdates} fallback={_pr57WriterJunctionFallbacks}");
    }
}
