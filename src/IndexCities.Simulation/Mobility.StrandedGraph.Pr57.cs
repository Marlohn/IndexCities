namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57-only: an exact, ephemeral directed leader graph for stranded queues.
    // The original HeldInStrandedChain walks the full vehicle roster for each
    // hop and for every query. Here the same nearest-leader predicate is
    // evaluated once per vehicle per immutable decision frame (O(V^2)), then
    // each chain is resolved once (O(V)). Never reuse across a movement commit.
    static readonly bool Pr57StrandedGraphEnabled =
        Environment.GetEnvironmentVariable("PR57_STRANDED_GRAPH") == "1";

    Dictionary<int, Vehicle?>? _pr57StrandedNext;
    Dictionary<int, Vehicle>? _pr57StrandedOriginal;
    Dictionary<int, bool>? _pr57StrandedResolved;
    bool _pr57StrandedGraphAnnounced;

    void Pr57BeginStrandedGraph()
    {
        if (!Pr57StrandedGraphEnabled ||
            !State.Vehicles.Any(v => v.Status == VehicleStatus.Stranded))
            return;

        var next = new Dictionary<int, Vehicle?>(State.Vehicles.Count);
        var original = new Dictionary<int, Vehicle>(State.Vehicles.Count);
        foreach (var v in State.Vehicles) original.Add(v.Id, v);

        foreach (var current in State.Vehicles)
        {
            if (current.Status != VehicleStatus.Travelling ||
                current.WaitingSince <= 0 || current.Detour.Count != 0)
                continue;

            Vehicle? leader = null;
            var nearest = double.MaxValue;
            var sin = Math.Sin(current.Heading);
            var cos = Math.Cos(current.Heading);
            // Exactly the original order, predicates and strict tie rule.
            foreach (var o in State.Vehicles)
            {
                if (o.Id == current.Id || !OccupiesLane(o)) continue;
                var relative = o.Position - current.Position;
                var forward = relative.X * sin + relative.Z * cos;
                var side = Math.Abs(relative.X * cos - relative.Z * sin);
                if (forward > 0 && side < 2.6 &&
                    Math.Cos(o.Heading - current.Heading) > .5 &&
                    forward < nearest)
                {
                    nearest = forward;
                    leader = o;
                }
            }
            next.Add(current.Id, nearest > 8 + .000001 ? null : leader);
        }
        _pr57StrandedNext = next;
        _pr57StrandedOriginal = original;
        _pr57StrandedResolved = new Dictionary<int, bool>(next.Count);
        if (!_pr57StrandedGraphAnnounced &&
            Environment.GetEnvironmentVariable("PR57_GRID_DIAG") == "1")
        {
            Console.WriteLine($"PR57_STRANDED_GRAPH active=True vehicles={State.Vehicles.Count} eligible={next.Count}");
            _pr57StrandedGraphAnnounced = true;
        }
    }

    void Pr57EndStrandedGraph()
    {
        _pr57StrandedNext = null;
        _pr57StrandedOriginal = null;
        _pr57StrandedResolved = null;
    }

    bool Pr57HeldBehindStranded(Vehicle v)
    {
        // EntryFrameVehicle can return a snapshot with the same ID but
        // different fields. Such a snapshot MUST use the original algorithm.
        if (_pr57StrandedNext is null ||
            !_pr57StrandedOriginal!.TryGetValue(v.Id, out var original) ||
            !ReferenceEquals(v, original))
            return HeldInStrandedChain(v, false);

        if (_pr57StrandedResolved!.TryGetValue(v.Id, out var resolved))
            return resolved;

        var visited = new HashSet<int>();
        var path = new List<int>();
        var current = v;
        var result = false;
        // Same upper bound as the original hop loop. A cycle with no stranded
        // leader is false; no order or vehicle state is modified.
        while (path.Count < State.Vehicles.Count &&
               current.Status == VehicleStatus.Travelling &&
               current.WaitingSince > 0 && current.Detour.Count == 0)
        {
            if (_pr57StrandedResolved.TryGetValue(current.Id, out result)) break;
            if (!visited.Add(current.Id)) { result = false; break; }
            path.Add(current.Id);
            if (!_pr57StrandedNext.TryGetValue(current.Id, out var leader) ||
                leader is null) { result = false; break; }
            if (leader.Status == VehicleStatus.Stranded) { result = true; break; }
            current = leader;
        }
        foreach (var id in path) _pr57StrandedResolved[id] = result;
        return result;
    }

    bool Pr57HasLiveLaneEntry()
    {
        Pr57BeginStrandedGraph();
        try { return State.Vehicles.Any(LiveLaneEntry); }
        finally { Pr57EndStrandedGraph(); }
    }

    double Pr57NextMovementEvent(double horizon)
    {
        Pr57BeginStrandedGraph();
        try { return NextVehicleMovementEvent(horizon); }
        finally { Pr57EndStrandedGraph(); }
    }
}
