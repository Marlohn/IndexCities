namespace IndexCities.Simulation;

// E15: exploratory executor. Never enabled by the game or saved in CityState.
// The "local" mode deliberately challenges equivalence; a matching end-state
// on a fixture is evidence, not a proof of causal independence.
public sealed partial class CitySimulation
{
    public bool E15BatchScoped { get; set; }
    public bool E15LocalTime { get; set; }

    long _e15Outer, _e15Partitioned, _e15FullCohorts, _e15DeferredVehicleSteps;
    long _e15LocalVehicleSteps, _e15TotalEligibleVehicleSteps, _e15Substeps;

    public string E15Summary() =>
        $"E15_RESULT outer={_e15Outer} partitioned={_e15Partitioned} fullCohorts={_e15FullCohorts} " +
        $"deferredVehicleSteps={_e15DeferredVehicleSteps} localVehicleSteps={_e15LocalVehicleSteps} " +
        $"totalEligibleVehicleSteps={_e15TotalEligibleVehicleSteps} substeps={_e15Substeps} " +
        "(local mode is an unproven ablation, not a gameplay algorithm)";

    // An initial conservative *candidate* dependency closure. It does not claim
    // to prove causal independence: indirect reservations, RNG, same-tick ordering
    // and geometry beyond endpoints are explicitly falsified by save checkpoints.
    HashSet<int> E15Cohort(IReadOnlyList<Vehicle> active, double dt)
    {
        var selected = new HashSet<int>();
        foreach (var v in active)
            if (TryLaneEntry(v, out _)) selected.Add(v.Id);

        var maxTravel = Math.Max(State.Tuning.VehicleSpeed, State.Tuning.HighwaySpeed) * dt;
        var guard = 32 + maxTravel * 2;

        // Connect all nearby actively moving vehicles, transitively. This is
        // intentionally broader than just the immediate road-entry neighbor.
        // Still insufficient to establish equivalence: that is the experiment.
        bool expanded;
        do {
            expanded = false;
            foreach (var candidate in active) {
                if (selected.Contains(candidate.Id)) continue;
                foreach (var member in active) {
                    if (!selected.Contains(member.Id)) continue;
                    if (candidate.Position.Distance(member.Position) > guard) continue;
                    selected.Add(candidate.Id);
                    expanded = true;
                    break;
                }
            }
        } while (expanded);
        return selected;
    }

    void MoveVehiclesE15(double dt)
    {
        if (!State.Vehicles.Any(LiveLaneEntry)) {
            MoveVehiclesUnsplit(dt);
            return;
        }

        _e15Outer++;
        _vehicleMovementBatch = [];
        _vehicleMovementOffset = 0;
        try {
            // Exactly one operational pass at the original 2-second boundary.
            // Only physical movement is eligible for substep localization.
            MoveVehiclesUnsplit(0, dt);
            var all = _vehicleMovementBatch;
            var active = State.Vehicles.Where(v => all.Contains(v.Id)).ToArray();
            var selected = E15Cohort(active, dt);
            var local = active.Where(v => selected.Contains(v.Id)).Select(v => v.Id).ToHashSet();
            var remote = active.Where(v => !selected.Contains(v.Id)).Select(v => v.Id).ToHashSet();

            _e15TotalEligibleVehicleSteps += active.Length;
            _e15LocalVehicleSteps += local.Count;
            if (remote.Count > 0) {
                _e15Partitioned++;
                _e15DeferredVehicleSteps += remote.Count;
            } else _e15FullCohorts++;

            _vehicleMovementBatch = local;
            while (dt - _vehicleMovementOffset > .000001) {
                var remaining = dt - _vehicleMovementOffset;
                _vehicleMovementClock = State.ElapsedSeconds + _vehicleMovementOffset;
                _entryFrameBuffer.Clear();
                foreach (var v in State.Vehicles.Where(v => OccupiesLane(v) && !TryLaneEntry(v, out _)))
                    _entryFrameBuffer.Add(v.Id, EntrySnapshot(v));
                _entryFrame = _entryFrameBuffer;
                var slice = NextVehicleMovementEvent(remaining);
                MoveVehiclesUnsplit(slice);
                _vehicleMovementOffset += slice;
                _e15Substeps++;
            }

            // The isolated cohort is moved once at the outer boundary. This can
            // be wrong if a dependency escapes the estimated guard; hourly/full
            // save hashes are mandatory and any divergence disqualifies the mode.
            if (remote.Count > 0) {
                _entryFrame = null;
                _entryFrameBuffer.Clear();
                _vehicleMovementClock = null;
                _vehicleMovementOffset = 0;
                _vehicleMovementBatch = remote;
                MoveVehiclesUnsplit(dt);
            }
        } finally {
            _vehicleMovementBatch = null;
            _entryFrame = null;
            _entryFrameBuffer.Clear();
            _vehicleMovementClock = null;
            _vehicleMovementOffset = 0;
        }
    }
}
