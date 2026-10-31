namespace IndexCities.Simulation;

// PR57 #550: opt-in, Step-local incremental maintenance of the canonical
// (WaitingSince, Id) actor order. This changes ONLY the sort implementation:
// every vehicle, operation, movement slice and physical predicate still runs.
public sealed partial class CitySimulation
{
    static readonly bool Pr57IncrementalActorOrderEnabled =
        Environment.GetEnvironmentVariable("PR57_INCREMENTAL_ACTOR_ORDER") == "1";
    static readonly bool Pr57IncrementalActorOrderVerify =
        Environment.GetEnvironmentVariable("PR57_INCREMENTAL_ACTOR_ORDER_VERIFY") == "1";
    static readonly bool Pr57IncrementalActorOrderDiag =
        Environment.GetEnvironmentVariable("PR57_INCREMENTAL_ACTOR_ORDER_DIAG") == "1";

    readonly record struct Pr57ActorOrderKey(double WaitingSince, int Id, Vehicle Actor);
    static readonly IComparer<Pr57ActorOrderKey> Pr57ActorOrderComparer =
        Comparer<Pr57ActorOrderKey>.Create(static (a, b) =>
        {
            var c = a.WaitingSince.CompareTo(b.WaitingSince);
            return c != 0 ? c : a.Id.CompareTo(b.Id);
        });

    readonly SortedSet<Pr57ActorOrderKey> _pr57ActorOrder = new(Pr57ActorOrderComparer);
    readonly Dictionary<int, Pr57ActorOrderKey> _pr57ActorOrderById = new();
    long _pr57ActorOrderSteps, _pr57ActorOrderLookups, _pr57ActorOrderBuilds;
    long _pr57ActorOrderScanned, _pr57ActorOrderUpdates, _pr57ActorOrderMembershipChanges;

    void Pr57IncrementalActorOrderBeginStep()
    {
        if (!Pr57IncrementalActorOrderEnabled) return;
        if (Pr57IncrementalActorOrderDiag && _pr57ActorOrderSteps > 0
            && _pr57ActorOrderSteps % 100 == 0)
            Console.WriteLine($"PR57_550_ACTOR_ORDER steps={_pr57ActorOrderSteps} lookups={_pr57ActorOrderLookups} builds={_pr57ActorOrderBuilds} scanned={_pr57ActorOrderScanned} updates={_pr57ActorOrderUpdates} membershipChanges={_pr57ActorOrderMembershipChanges}");
        _pr57ActorOrderSteps++;
        // A public Step boundary admits arbitrary external edits. Never carry
        // this cache across Advance(2), save/load or host commands.
        _pr57ActorOrder.Clear();
        _pr57ActorOrderById.Clear();
    }

    static Vehicle[] Pr57CanonicalActorOrder(IEnumerable<Vehicle> source)
    {
        var ordered = source.ToArray();
        Array.Sort(ordered, static (a, b) =>
        {
            var c = a.WaitingSince.CompareTo(b.WaitingSince);
            return c != 0 ? c : a.Id.CompareTo(b.Id);
        });
        return ordered;
    }

    Vehicle[] Pr57IncrementalOrderedActors(HashSet<int>? batch)
    {
        // No live lane-entry batch means there is only one unsplit physical
        // visit in this Step: no subsequent sort exists to amortize a tree.
        if (_vehicleMovementBatch is null)
            return Pr57CanonicalActorOrder(batch is null
                ? State.Vehicles : State.Vehicles.Where(v => batch.Contains(v.Id)));
        _pr57ActorOrderLookups++;
        // Membership and reference identity are rechecked on EVERY physical
        // slice. This catches additions, removals, replacements and reordering
        // of State.Vehicles, even if Count is unchanged.
        var rebuild = _pr57ActorOrderById.Count != State.Vehicles.Count;
        if (!rebuild)
        {
            foreach (var v in State.Vehicles)
            {
                _pr57ActorOrderScanned++;
                if (!_pr57ActorOrderById.TryGetValue(v.Id, out var key)
                    || !ReferenceEquals(key.Actor, v))
                {
                    rebuild = true;
                    _pr57ActorOrderMembershipChanges++;
                    break;
                }
            }
        }
        if (rebuild)
        {
            _pr57ActorOrderBuilds++;
            _pr57ActorOrder.Clear();
            _pr57ActorOrderById.Clear();
            foreach (var v in State.Vehicles)
            {
                var key = new Pr57ActorOrderKey(v.WaitingSince, v.Id, v);
                if (!_pr57ActorOrderById.TryAdd(v.Id, key))
                {
                    // Duplicate IDs are outside the normal model; the original
                    // Array.Sort remains the authoritative fallback.
                    return Pr57CanonicalActorOrder(batch is null
                        ? State.Vehicles : State.Vehicles.Where(v => batch.Contains(v.Id)));
                }
                _pr57ActorOrder.Add(key);
            }
        }
        else
        {
            foreach (var v in State.Vehicles)
            {
                _pr57ActorOrderScanned++;
                var old = _pr57ActorOrderById[v.Id];
                if (BitConverter.DoubleToInt64Bits(old.WaitingSince)
                    == BitConverter.DoubleToInt64Bits(v.WaitingSince)) continue;
                _pr57ActorOrder.Remove(old);
                var updated = new Pr57ActorOrderKey(v.WaitingSince, v.Id, v);
                _pr57ActorOrder.Add(updated);
                _pr57ActorOrderById[v.Id] = updated;
                _pr57ActorOrderUpdates++;
            }
        }
        // Materialize the canonical frozen actor order BEFORE any actor writes.
        // The batch filter never suppresses physical candidates, only the same
        // actors already excluded by PR57_BATCH_ORDER.
        var result = batch is null
            ? _pr57ActorOrder.Select(k => k.Actor).ToArray()
            : _pr57ActorOrder.Where(k => batch.Contains(k.Id)).Select(k => k.Actor).ToArray();
        if (Pr57IncrementalActorOrderVerify)
        {
            var fresh = Pr57CanonicalActorOrder(batch is null
                ? State.Vehicles : State.Vehicles.Where(v => batch.Contains(v.Id)));
            if (fresh.Length != result.Length
                || !fresh.Zip(result).All(pair => ReferenceEquals(pair.First, pair.Second)))
                throw new InvalidOperationException("PR57_550_ACTOR_ORDER_SHADOW_MISMATCH");
        }
        return result;
    }
}
