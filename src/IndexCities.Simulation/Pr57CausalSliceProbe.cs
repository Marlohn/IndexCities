namespace IndexCities.Simulation;

// PR57 #191: bounded, intrusive OBSERVATION ONLY of one authentic GRID
// entry sub-slice. HoldFor edges are a lower bound on physical dependencies;
// lane visits and reservation/pedestrian presence are NOT exact read sets.
// No gameplay decision, scheduler or save consumes this diagnostic.
public sealed partial class CitySimulation
{
    static readonly bool Pr57CausalSliceEnabled =
        Environment.GetEnvironmentVariable("PR57_191_CAUSAL_SLICE") == "1";
    bool _pr57CausalCaptured, _pr57CausalActive;
    double _pr57CausalStartTime, _pr57CausalDuration;
    Dictionary<int, (Pr57DepState State, string? Cause)>? _pr57CausalBefore;
    readonly HashSet<(int Reader, int Other)> _pr57CausalHolds = new();
    readonly HashSet<(int Reader, int Other)> _pr57CausalWinners = new();
    long _pr57CausalVisits, _pr57CausalEligibleVisits, _pr57CausalHoldCalls;
    long _pr57CausalJunctionCalls, _pr57CausalSignalChecks,
        _pr57CausalSignalBoundary, _pr57CausalPedestrianPresence,
        _pr57CausalReservationPresence;
    int _pr57CausalCleanupChanged;

    Dictionary<int, (Pr57DepState State, string? Cause)> Pr57CausalWorld() =>
        State.Vehicles.ToDictionary(v => v.Id, v => (Pr57DepSnapshot(v), v.Cause));

    void Pr57CausalBeginSlice(double dt, double? operationsDt)
    {
        if (!Pr57CausalSliceEnabled || CostProfile is null || _pr57CausalCaptured ||
            _vehicleMovementBatch is null || operationsDt is not null || dt <= 0 ||
            VehicleMovementTime < 3608 || VehicleMovementTime > 3644)
            return;
        _pr57CausalCaptured = true;
        _pr57CausalActive = true;
        _pr57CausalStartTime = VehicleMovementTime;
        _pr57CausalDuration = dt;
        _pr57CausalBefore = Pr57CausalWorld();
        _pr57CausalHolds.Clear();
        _pr57CausalWinners.Clear();
    }

    void Pr57CausalAfterCleanup()
    {
        if (!_pr57CausalActive || _pr57CausalBefore is null) return;
        var now = Pr57CausalWorld();
        _pr57CausalCleanupChanged = now.Count(kv =>
            !_pr57CausalBefore.TryGetValue(kv.Key, out var old) || old != kv.Value)
            + _pr57CausalBefore.Keys.Count(id => !now.ContainsKey(id));
    }

    void Pr57CausalLaneCandidate(Vehicle reader, Vehicle other)
    {
        if (!_pr57CausalActive) return;
        _pr57CausalVisits++;
        if (other.Id != reader.Id && OccupiesLane(other))
            _pr57CausalEligibleVisits++;
    }

    void Pr57CausalHold(Vehicle reader, Vehicle other)
    {
        if (!_pr57CausalActive) return;
        _pr57CausalHoldCalls++;
        _pr57CausalHolds.Add((reader.Id, other.Id));
    }

    void Pr57CausalWinner(Vehicle reader, Vehicle other)
    {
        if (_pr57CausalActive) _pr57CausalWinners.Add((reader.Id, other.Id));
    }

    void Pr57CausalJunction(Vehicle reader, RoadNode junction)
    {
        if (!_pr57CausalActive) return;
        _pr57CausalJunctionCalls++;
        if (junction.Neighbours.Count >= 3 && junction.Control == JunctionControl.Signal)
        {
            _pr57CausalSignalChecks++;
            var before = VehicleMovementTime % 60 < 30;
            var after = (VehicleMovementTime + _pr57CausalDuration) % 60 < 30;
            if (before != after) _pr57CausalSignalBoundary++;
        }
        // These are broad presence checks, not a claim that the exact
        // MayEnterJunction path reached each corresponding predicate.
        if (State.Citizens.Any(c => c.Alive && c.VehicleId == null && c.Moving &&
            c.CrossingNodeId == junction.Id &&
            c.Position.Distance(junction.Position) < State.Tuning.RoadWidth / 2 + 1.5))
            _pr57CausalPedestrianPresence++;
        if (State.Vehicles.Any(o => o.Id != reader.Id &&
            (o.IntersectionClaim == junction.Id ||
             o.ManoeuvreSpaces.Contains(junction.Id))))
            _pr57CausalReservationPresence++;
    }

    void Pr57CausalEndSlice()
    {
        if (!_pr57CausalActive || _pr57CausalBefore is null) return;
        var after = Pr57CausalWorld();
        var changed = after.Where(kv =>
            !_pr57CausalBefore.TryGetValue(kv.Key, out var old) || old != kv.Value)
            .Select(kv => kv.Key).ToHashSet();
        foreach (var id in _pr57CausalBefore.Keys)
            if (!after.ContainsKey(id)) changed.Add(id);
        var changedHoldEdges = _pr57CausalHolds.Count(e => changed.Contains(e.Other));
        var changedWinnerEdges = _pr57CausalWinners.Count(e => changed.Contains(e.Other));
        var affectedReaders = _pr57CausalHolds.Where(e => changed.Contains(e.Other))
            .Select(e => e.Reader).Distinct().Count();
        Console.WriteLine(
            $"PR57_191_CAUSAL_SLICE time={_pr57CausalStartTime:F9} dt={_pr57CausalDuration:F9} " +
            $"vehicles={after.Count} changedVehicles={changed.Count} cleanupChanged={_pr57CausalCleanupChanged} " +
            $"laneVisits={_pr57CausalVisits} eligibleLaneVisits={_pr57CausalEligibleVisits} " +
            $"holdCalls={_pr57CausalHoldCalls} uniqueHoldEdges={_pr57CausalHolds.Count} " +
            $"uniqueWinnerEdges={_pr57CausalWinners.Count} changedHoldEdges={changedHoldEdges} " +
            $"changedWinnerEdges={changedWinnerEdges} affectedReadersLowerBound={affectedReaders} " +
            $"junctionCalls={_pr57CausalJunctionCalls} signalChecks={_pr57CausalSignalChecks} " +
            $"potentialSignalBoundaries={_pr57CausalSignalBoundary} " +
            $"pedestrianPresenceChecks={_pr57CausalPedestrianPresence} " +
            $"reservationPresenceChecks={_pr57CausalReservationPresence} " +
            "scope=ONE_INTRUSIVE_SLICE_HOLD_EDGES_LOWER_BOUND_NOT_EXACT_INVALIDATION");
        _pr57CausalActive = false;
        _pr57CausalBefore = null;
        _pr57CausalHolds.Clear();
        _pr57CausalWinners.Clear();
    }
}
