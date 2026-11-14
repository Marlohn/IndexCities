namespace IndexCities.Simulation;

// PR57 #260: bounded, intrusive read-boundary exposure for three authentic
// persistent-queue junction readers. This is a deliberately BROAD superset
// of candidate invalidators, NOT a complete read/write set or a cache.
// It does not change physics, order, state, saves, or the result of admission.
public sealed partial class CitySimulation
{
    static readonly bool Pr57260Enabled =
        Environment.GetEnvironmentVariable("PR57_260_READ_EXPOSURE") == "1";

    sealed class Pr57260Frame
    {
        public required Dictionary<int, string> Vehicles;
        public required Dictionary<int, bool> Near;
        public required string PedestrianIds;
        public required string PedestrianPoses;
        public required bool PedestrianPresent;
        public required Pr57JunctionExit Reason;
        public required bool Allowed;
        public required int NodeId;
        public required bool SignalHalf;
    }

    sealed class Pr57260Counters
    {
        public long Calls, Intervals, ChangedWorldIntervals, ChangedNearIntervals,
            ChangedBlockerIntervals, ChangedPedestrianMembership,
            ChangedPedestrianPose, ChangedPedestrianPresence, ChangedReason,
            ChangedAllowed, SameObservedInput, ChangedSignalHalf,
            TotalWorldVehiclesChanged, TotalNearVehiclesChanged,
            PedestrianReturns, OnwardBodyReturns, OtherReturns;
    }

    readonly Dictionary<int, Pr57260Frame> _pr57260Prior = new();
    readonly Dictionary<int, Pr57260Counters> _pr57260Counts = new();
    long _pr57260Slices;
    bool _pr57260Active;

    void Pr57260BeginSlice(double dt, double? operationsDt)
    {
        _pr57260Active = Pr57260Enabled && CostProfile is not null &&
            dt > 0 && operationsDt is null && _pr57260Slices < 600;
        if (_pr57260Active) _pr57260Slices++;
    }

    void Pr57260Decision(Vehicle reader, RoadNode junction,
        Pr57JunctionExit reason, bool allowed)
    {
        if (!_pr57260Active ||
            reader.Id is not (1071 or 1243 or 1495)) return;

        var id = reader.Id;
        if (!_pr57260Counts.TryGetValue(id, out var counts))
            _pr57260Counts[id] = counts = new Pr57260Counters();
        counts.Calls++;
        if (reason == Pr57JunctionExit.Pedestrian) counts.PedestrianReturns++;
        else if (reason == Pr57JunctionExit.OnwardBody) counts.OnwardBodyReturns++;
        else counts.OtherReturns++;

        // The original method has already selected its canonical exit. This
        // observer deliberately reads again; it never supplies a decision.
        var world = State.Vehicles.Where(v => v.Id != id)
            .ToDictionary(v => v.Id, Pr57MutationSnapshot);
        var near = State.Vehicles.Where(v => v.Id != id)
            .ToDictionary(v => v.Id,
                v => v.Position.Distance(junction.Position) < 20);
        var walkers = State.Citizens.Where(c => c.Alive &&
            c.VehicleId == null && c.Moving && c.CrossingNodeId == junction.Id &&
            c.Position.Distance(junction.Position) <
                State.Tuning.RoadWidth / 2 + 1.5).ToList();
        var pedestrianIds = string.Join(",", walkers.Select(c => c.Id));
        var pedestrianPoses = string.Join(";", walkers.Select(c =>
            $"{c.Id}:{BitConverter.DoubleToInt64Bits(c.Position.X)}:" +
            $"{BitConverter.DoubleToInt64Bits(c.Position.Z)}"));
        var current = new Pr57260Frame
        {
            Vehicles = world, Near = near, PedestrianIds = pedestrianIds,
            PedestrianPoses = pedestrianPoses,
            PedestrianPresent = walkers.Count > 0, Reason = reason,
            Allowed = allowed, NodeId = junction.Id,
            SignalHalf = VehicleMovementTime % 60 < 30
        };

        if (_pr57260Prior.TryGetValue(id, out var previous))
        {
            counts.Intervals++;
            long changed = 0, changedNear = 0;
            foreach (var (otherId, value) in world)
            {
                if (previous.Vehicles.TryGetValue(otherId, out var old) &&
                    string.Equals(old, value, StringComparison.Ordinal)) continue;
                changed++;
                if (near.GetValueOrDefault(otherId) ||
                    previous.Near.GetValueOrDefault(otherId)) changedNear++;
            }
            foreach (var otherId in previous.Vehicles.Keys)
            {
                if (world.ContainsKey(otherId)) continue;
                changed++;
                if (previous.Near.GetValueOrDefault(otherId)) changedNear++;
            }
            counts.TotalWorldVehiclesChanged += changed;
            counts.TotalNearVehiclesChanged += changedNear;
            if (changed > 0) counts.ChangedWorldIntervals++;
            if (changedNear > 0) counts.ChangedNearIntervals++;

            // These are the previously observed selected body IDs from #247,
            // NOT a proof that no other body can become the blocker.
            var blocker = id switch { 1071 => 1511, 1243 => 636, _ => 23 };
            if (!string.Equals(previous.Vehicles.GetValueOrDefault(blocker),
                    world.GetValueOrDefault(blocker), StringComparison.Ordinal))
                counts.ChangedBlockerIntervals++;

            if (previous.PedestrianIds != pedestrianIds)
                counts.ChangedPedestrianMembership++;
            if (previous.PedestrianPoses != pedestrianPoses)
                counts.ChangedPedestrianPose++;
            if (previous.PedestrianPresent != current.PedestrianPresent)
                counts.ChangedPedestrianPresence++;
            if (previous.Reason != reason) counts.ChangedReason++;
            if (previous.Allowed != allowed) counts.ChangedAllowed++;
            if (previous.SignalHalf != current.SignalHalf)
                counts.ChangedSignalHalf++;

            if (changed == 0 &&
                previous.PedestrianPoses == pedestrianPoses &&
                previous.NodeId == junction.Id &&
                previous.SignalHalf == current.SignalHalf)
                counts.SameObservedInput++;
        }
        _pr57260Prior[id] = current;
    }

    void Pr57260EndSlice()
    {
        if (!_pr57260Active) return;
        _pr57260Active = false;
        if (_pr57260Slices != 600) return;
        foreach (var (id, c) in _pr57260Counts.OrderBy(kv => kv.Key))
            Console.WriteLine(
                $"PR57_260_READ_EXPOSURE id={id} slices={_pr57260Slices} " +
                $"calls={c.Calls} intervals={c.Intervals} " +
                $"worldChangedIntervals={c.ChangedWorldIntervals} " +
                $"near20mChangedIntervals={c.ChangedNearIntervals} " +
                $"selectedBlockerChangedIntervals={c.ChangedBlockerIntervals} " +
                $"pedMembershipChanged={c.ChangedPedestrianMembership} " +
                $"pedPoseChanged={c.ChangedPedestrianPose} " +
                $"pedPresenceChanged={c.ChangedPedestrianPresence} " +
                $"reasonChanged={c.ChangedReason} allowedChanged={c.ChangedAllowed} " +
                $"signalHalfChanged={c.ChangedSignalHalf} " +
                $"sameObservedInputIntervals={c.SameObservedInput} " +
                $"worldVehicleChanges={c.TotalWorldVehiclesChanged} " +
                $"near20mVehicleChanges={c.TotalNearVehiclesChanged} " +
                $"pedestrianReturns={c.PedestrianReturns} " +
                $"onwardBodyReturns={c.OnwardBodyReturns} otherReturns={c.OtherReturns} " +
                "scope=three-real-queue-readers fullVehicleJsonBetweenReads=True " +
                "pedestrianCandidatesAtExit=True notExactReadSet=True " +
                "missesTransientWritesAndClockDependencies=True " +
                "observational=True intrusive=True noReuse=True");
    }
}
