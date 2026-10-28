namespace IndexCities.Simulation;

// PR57 #253: strictly observational, bounded census of the 24 persistent
// real-queue vehicles. Full persisted Vehicle JSON is compared at boundaries;
// this does NOT detect transient writes, establish a causal read set or skip
// any actor/slice. The extra serialization makes wall times intrusive.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Effect253Enabled =
        Environment.GetEnvironmentVariable("PR57_253_STATE_EFFECTS") == "1";

    readonly record struct Pr57Effect253Sample(
        string Full, Point Position, long HeadingBits, VehicleStatus Status,
        int RouteIndex, long WaitBits, string? Cause, int? Claim,
        string Spaces, long FuelUnits);

    readonly long[] _pr57Effect253Changes = new long[5];
    // 0 between slices, 1 cleanup, 2 earlier actors, 3 own pass, 4 tow sync
    readonly long[] _pr57Effect253OwnFields = new long[9];
    // pose, status, routeIndex, waiting, cause, claim, spaces, fuel, other
    Dictionary<int, Pr57Effect253Sample> _pr57Effect253Pre = new();
    Dictionary<int, Pr57Effect253Sample> _pr57Effect253PostCleanup = new();
    Dictionary<int, Pr57Effect253Sample> _pr57Effect253Before = new();
    Dictionary<int, Pr57Effect253Sample> _pr57Effect253After = new();
    Dictionary<int, Pr57Effect253Sample> _pr57Effect253LastEnd = new();
    Dictionary<int, string>? _pr57Effect253OtherPrior;
    bool _pr57Effect253Active;
    int _pr57Effect253Slices;
    long _pr57Effect253Passes, _pr57Effect253Still, _pr57Effect253Overdue;
    long _pr57Effect253ActorTicks, _pr57Effect253ActorBytes;
    long _pr57Effect253ActorStart, _pr57Effect253ActorAllocStart;
    long _pr57Effect253OtherIntervals, _pr57Effect253OtherChangedIntervals;
    long _pr57Effect253OtherChangedVehicles;

    Pr57Effect253Sample Pr57Effect253Capture(Vehicle v) => new(
        Pr57MutationSnapshot(v), v.Position,
        BitConverter.DoubleToInt64Bits(v.Heading), v.Status, v.RouteIndex,
        BitConverter.DoubleToInt64Bits(v.WaitingSince), v.Cause,
        v.IntersectionClaim, string.Join(",", v.ManoeuvreSpaces), v.FuelUnits);

    Dictionary<int, Pr57Effect253Sample> Pr57Effect253Cohort() =>
        State.Vehicles.Where(v => Pr57State244PersistentIds.Contains(v.Id))
            .ToDictionary(v => v.Id, Pr57Effect253Capture);

    void Pr57Effect253Compare(Pr57Effect253Sample before,
        Pr57Effect253Sample after, int boundary)
    {
        if (string.Equals(before.Full, after.Full, StringComparison.Ordinal)) return;
        _pr57Effect253Changes[boundary]++;
        if (boundary != 3) return;
        var fields = _pr57Effect253OwnFields;
        if (before.Position != after.Position || before.HeadingBits != after.HeadingBits) fields[0]++;
        if (before.Status != after.Status) fields[1]++;
        if (before.RouteIndex != after.RouteIndex) fields[2]++;
        if (before.WaitBits != after.WaitBits) fields[3]++;
        if (!string.Equals(before.Cause, after.Cause, StringComparison.Ordinal)) fields[4]++;
        if (before.Claim != after.Claim) fields[5]++;
        if (!string.Equals(before.Spaces, after.Spaces, StringComparison.Ordinal)) fields[6]++;
        if (before.FuelUnits != after.FuelUnits) fields[7]++;
        if (before.Position == after.Position && before.HeadingBits == after.HeadingBits &&
            before.Status == after.Status && before.RouteIndex == after.RouteIndex &&
            before.WaitBits == after.WaitBits && string.Equals(before.Cause, after.Cause, StringComparison.Ordinal) &&
            before.Claim == after.Claim && string.Equals(before.Spaces, after.Spaces, StringComparison.Ordinal) &&
            before.FuelUnits == after.FuelUnits) fields[8]++;
    }

    void Pr57Effect253BeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57Effect253Active = Pr57Effect253Enabled && profiled &&
            dt > 0 && operationsDt is null && _pr57Effect253Slices < 600 &&
            Pr57State244PersistentIds.Count > 0;
        if (!_pr57Effect253Active) return;
        _pr57Effect253Slices++;
        _pr57Effect253Pre = Pr57Effect253Cohort();
        foreach (var (id, now) in _pr57Effect253Pre)
            if (_pr57Effect253LastEnd.TryGetValue(id, out var prior))
                Pr57Effect253Compare(prior, now, 0);

        // Full-world net changes at ten coarse boundaries only. A stable
        // interval does NOT exclude intermediate writes or external effects.
        if (_pr57Effect253Slices == 1 || (_pr57Effect253Slices - 1) % 60 == 0)
        {
            var world = Pr57MutationWorld();
            if (_pr57Effect253OtherPrior is not null)
            {
                _pr57Effect253OtherIntervals++;
                var changed = 0;
                foreach (var (id, value) in world)
                    if (!Pr57State244PersistentIds.Contains(id) &&
                        (!_pr57Effect253OtherPrior.TryGetValue(id, out var old) ||
                         !string.Equals(old, value, StringComparison.Ordinal))) changed++;
                foreach (var id in _pr57Effect253OtherPrior.Keys)
                    if (!Pr57State244PersistentIds.Contains(id) && !world.ContainsKey(id)) changed++;
                _pr57Effect253OtherChangedVehicles += changed;
                if (changed > 0) _pr57Effect253OtherChangedIntervals++;
            }
            _pr57Effect253OtherPrior = world;
        }
        _pr57Effect253After.Clear();
        _pr57Effect253Before.Clear();
    }

    void Pr57Effect253AfterCleanup()
    {
        if (!_pr57Effect253Active) return;
        _pr57Effect253PostCleanup = Pr57Effect253Cohort();
        foreach (var (id, now) in _pr57Effect253PostCleanup)
            if (_pr57Effect253Pre.TryGetValue(id, out var prior))
                Pr57Effect253Compare(prior, now, 1);
    }

    void Pr57Effect253BeforeActor(Vehicle v)
    {
        if (!_pr57Effect253Active || !Pr57State244PersistentIds.Contains(v.Id)) return;
        var now = Pr57Effect253Capture(v);
        _pr57Effect253Before[v.Id] = now;
        if (_pr57Effect253PostCleanup.TryGetValue(v.Id, out var prior))
            Pr57Effect253Compare(prior, now, 2);
        _pr57Effect253Passes++;
        if (v.WaitingSince > 0 &&
            VehicleMovementTime - v.WaitingSince >= State.Tuning.UTurnWaitSeconds)
            _pr57Effect253Overdue++;
        _pr57Effect253ActorStart = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57Effect253ActorAllocStart = GC.GetAllocatedBytesForCurrentThread();
    }

    void Pr57Effect253AfterActor(Vehicle v)
    {
        if (!_pr57Effect253Active || !Pr57State244PersistentIds.Contains(v.Id)) return;
        _pr57Effect253ActorTicks += System.Diagnostics.Stopwatch.GetTimestamp() - _pr57Effect253ActorStart;
        _pr57Effect253ActorBytes += GC.GetAllocatedBytesForCurrentThread() - _pr57Effect253ActorAllocStart;
        var now = Pr57Effect253Capture(v);
        _pr57Effect253After[v.Id] = now;
        if (_pr57Effect253Before.TryGetValue(v.Id, out var prior))
        {
            Pr57Effect253Compare(prior, now, 3);
            if (prior.Position == now.Position && v.Status == VehicleStatus.Travelling)
                _pr57Effect253Still++;
        }
    }

    void Pr57Effect253EndSlice()
    {
        if (!_pr57Effect253Active) return;
        var now = Pr57Effect253Cohort();
        foreach (var (id, current) in now)
            if (_pr57Effect253After.TryGetValue(id, out var after))
                Pr57Effect253Compare(after, current, 4);
        _pr57Effect253LastEnd = now;
        _pr57Effect253Active = false;
        if (_pr57Effect253Slices != 600) return;
        Console.WriteLine(
            $"PR57_253_STATE_EFFECTS slices={_pr57Effect253Slices} cohort={Pr57State244PersistentIds.Count} " +
            $"actorPasses={_pr57Effect253Passes} stillTravelling={_pr57Effect253Still} " +
            $"waitOverUTurnThreshold={_pr57Effect253Overdue} " +
            $"betweenSliceChanged={_pr57Effect253Changes[0]} cleanupChanged={_pr57Effect253Changes[1]} " +
            $"priorActorChanged={_pr57Effect253Changes[2]} ownPassChanged={_pr57Effect253Changes[3]} " +
            $"towSyncChanged={_pr57Effect253Changes[4]} " +
            $"ownPoseChanged={_pr57Effect253OwnFields[0]} ownStatusChanged={_pr57Effect253OwnFields[1]} " +
            $"ownRouteIndexChanged={_pr57Effect253OwnFields[2]} ownWaitChanged={_pr57Effect253OwnFields[3]} " +
            $"ownCauseChanged={_pr57Effect253OwnFields[4]} ownClaimChanged={_pr57Effect253OwnFields[5]} " +
            $"ownSpacesChanged={_pr57Effect253OwnFields[6]} ownFuelChanged={_pr57Effect253OwnFields[7]} " +
            $"ownOtherFieldsChanged={_pr57Effect253OwnFields[8]} " +
            $"otherVehicleIntervals={_pr57Effect253OtherIntervals} " +
            $"otherVehicleChangedIntervals={_pr57Effect253OtherChangedIntervals} " +
            $"otherVehicleNetChanges={_pr57Effect253OtherChangedVehicles} " +
            $"actorBodyMs={_pr57Effect253ActorTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F4} " +
            $"actorBodyMB={_pr57Effect253ActorBytes / 1e6:F4} " +
            "scope=REAL_QUEUE_BOUNDARY_ONLY fullPersistedVehicleJson=True " +
            "missesTransientAndNonVehicleWriters=True noCausalClosure=True " +
            "observational=True intrusive=True noSkippedSlices=True");
    }
}
