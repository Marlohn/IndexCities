namespace IndexCities.Simulation;

// PR57 #214: bounded read-site snapshots for candidate dependency falsification.
// Only the first 48 actor/kind pairs encountered in physical slice 37 are
// observed in slices 37-39. Never used by movement decisions.
// Snapshot equality is NOT causal closure: signals, pedestrians, time, roads,
// transient writes, and rejected candidates remain outside this read census.
public sealed partial class CitySimulation
{
    static readonly bool Pr57CandidateReadsEnabled =
        Environment.GetEnvironmentVariable("PR57_214_CANDIDATE_READS") == "1";

    sealed class Pr57CandidateTrace
    {
        public string Self = "";
        public bool Entering;
        public long ReachBits;
        public long TimeBits;
        public int Enumerated;
        public readonly List<(int Id, string State)> Occupiers = new();
        public readonly List<(int Id, string State)> Deep = new();
        public readonly List<Pr57NegativeRow> Negatives = new();
    }

    readonly HashSet<(int Id, int Kind)> _pr57CandidateSelected = new();
    readonly Dictionary<(int Id, int Kind), Pr57CandidateTrace> _pr57CandidatePrior = new();
    readonly Dictionary<(int Id, int Kind), Pr57CandidateTrace> _pr57CandidateCurrent = new();
    (int Id, int Kind) _pr57CandidateKey;
    Pr57CandidateTrace? _pr57CandidateActive;
    bool _pr57CandidateSliceActive;
    long _pr57CandidateCaptured, _pr57CandidateMatched, _pr57CandidateSelfSame;
    long _pr57CandidateLaneSelfSame, _pr57CandidateBendSelfSame;
    long _pr57CandidateEnumeratedSame, _pr57CandidateOccupierIdsSame;
    long _pr57CandidateOccupierStatesSame, _pr57CandidateDeepIdsSame;
    long _pr57CandidateDeepStatesSame, _pr57CandidateAllObservedSame;
    long _pr57CandidateTimeChanged, _pr57CandidateSnapshotCalls;
    long _pr57CandidateSnapshotTicks, _pr57CandidateSnapshotBytes;

    void Pr57CandidateBeginSlice()
    {
        _pr57CandidateSliceActive = Pr57CandidateReadsEnabled && _pr57ExclusiveActive;
        _pr57CandidateActive = null;
        if (!_pr57CandidateSliceActive) return;
        Pr57FieldBeginSlice();
        Pr57FanoutBeginSlice();
        Pr57NegativeBeginSlice();
        if (_pr57ExclusiveSamples == 1)
        {
            _pr57CandidateSelected.Clear();
            _pr57CandidatePrior.Clear();
        }
        _pr57CandidateCurrent.Clear();
        _pr57CandidateCaptured = _pr57CandidateMatched = _pr57CandidateSelfSame = 0;
        _pr57CandidateLaneSelfSame = _pr57CandidateBendSelfSame = 0;
        _pr57CandidateEnumeratedSame = _pr57CandidateOccupierIdsSame = 0;
        _pr57CandidateOccupierStatesSame = _pr57CandidateDeepIdsSame = 0;
        _pr57CandidateDeepStatesSame = _pr57CandidateAllObservedSame = 0;
        _pr57CandidateTimeChanged = _pr57CandidateSnapshotCalls = 0;
        _pr57CandidateSnapshotTicks = _pr57CandidateSnapshotBytes = 0;
    }

    void Pr57CandidateBeginQuery(Vehicle actor, int kind, bool entering, double reach, int enumerated)
    {
        _pr57CandidateActive = null;
        if (!_pr57CandidateSliceActive) return;
        var key = (actor.Id, kind);
        if (_pr57ExclusiveSamples == 1)
        {
            // #217: deterministic ID-stratified sample avoids the first-48 GRID bias.
            // It remains a bounded convenience sample, not a random fleet census.
            if (Pr57NegativeEnabled && State.Vehicles.Count > 300 && (actor.Id & 15) != 0) return;
            if (Pr57NegativeEnabled && State.Vehicles.Count is > 60 and <= 300 && (actor.Id & 3) != 0) return;
            if (!_pr57CandidateSelected.Contains(key) && _pr57CandidateSelected.Count >= 48)
                return;
            _pr57CandidateSelected.Add(key);
        }
        else if (!_pr57CandidateSelected.Contains(key)) return;

        _pr57CandidateKey = key;
        _pr57CandidateActive = new Pr57CandidateTrace
        {
            Self = Pr57MutationSnapshot(actor),
            Entering = entering,
            ReachBits = BitConverter.DoubleToInt64Bits(reach),
            TimeBits = BitConverter.DoubleToInt64Bits(VehicleMovementTime),
            Enumerated = enumerated
        };
        _pr57CandidateCaptured++;
    }

    // tier 3: the lane scan passed ID/occupancy checks;
    // tier 9: it also passed coincidence/entry checks and reaches heading/forward;
    // bend: actual bendLeaders after the existing exact filter.
    void Pr57CandidateRead(Vehicle other, int tier)
    {
        if (_pr57CandidateActive is not { } trace || Pr57NegativeEnabled) return;
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var row = (other.Id, Pr57MutationSnapshot(other));
        if (tier == 3) trace.Occupiers.Add(row);
        else trace.Deep.Add(row);
        _pr57CandidateSnapshotTicks += System.Diagnostics.Stopwatch.GetTimestamp() - ticks;
        _pr57CandidateSnapshotBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
        _pr57CandidateSnapshotCalls++;
    }

    static bool Pr57CandidateSame(
        List<(int Id, string State)> a, List<(int Id, string State)> b, bool states)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
            if (a[i].Id != b[i].Id ||
                (states && !string.Equals(a[i].State, b[i].State, StringComparison.Ordinal)))
                return false;
        return true;
    }

    void Pr57CandidateEndQuery()
    {
        if (_pr57CandidateActive is not { } now) return;
        var key = _pr57CandidateKey;
        // Compare the FIRST query of this slice against the LAST query
        // for the same actor/kind in the previous slice.
        if (!_pr57CandidateCurrent.ContainsKey(key) &&
            _pr57CandidatePrior.TryGetValue(key, out var previous))
        {
            _pr57CandidateMatched++;
            var selfSame = string.Equals(previous.Self, now.Self, StringComparison.Ordinal) &&
                           previous.Entering == now.Entering &&
                           previous.ReachBits == now.ReachBits;
            if (selfSame)
            {
                _pr57CandidateSelfSame++;
                if (key.Kind == 0) _pr57CandidateLaneSelfSame++;
                else _pr57CandidateBendSelfSame++;
                if (previous.TimeBits != now.TimeBits) _pr57CandidateTimeChanged++;
                if (previous.Enumerated == now.Enumerated) _pr57CandidateEnumeratedSame++;
                var occupierIds = Pr57CandidateSame(previous.Occupiers, now.Occupiers, false);
                var occupierStates = Pr57CandidateSame(previous.Occupiers, now.Occupiers, true);
                var deepIds = Pr57CandidateSame(previous.Deep, now.Deep, false);
                var deepStates = Pr57CandidateSame(previous.Deep, now.Deep, true);
                if (occupierIds) _pr57CandidateOccupierIdsSame++;
                if (occupierStates) _pr57CandidateOccupierStatesSame++;
                if (deepIds) _pr57CandidateDeepIdsSame++;
                if (deepStates) _pr57CandidateDeepStatesSame++;
                if (previous.Enumerated == now.Enumerated && occupierStates && deepStates)
                    _pr57CandidateAllObservedSame++;
                Pr57FieldCompare(previous.Occupiers, now.Occupiers, 3);
                Pr57FieldCompare(previous.Deep, now.Deep, 9);
                Pr57FanoutCompare(key, previous.Occupiers, now.Occupiers, 3);
                Pr57FanoutCompare(key, previous.Deep, now.Deep, 9);
                Pr57NegativeCompare(key, previous.Negatives, now.Negatives);
            }
        }
        _pr57CandidateCurrent[key] = now;
        _pr57CandidateActive = null;
    }

    void Pr57CandidateEndSlice()
    {
        if (!_pr57CandidateSliceActive) return;
        Pr57FieldEndSlice();
        Pr57FanoutEndSlice();
        Pr57NegativeEndSlice();
        Console.WriteLine(
            $"PR57_214_READS sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
            $"selected={_pr57CandidateSelected.Count} capturedQueries={_pr57CandidateCaptured} " +
            $"revisited={_pr57CandidateMatched} selfAndQuerySame={_pr57CandidateSelfSame} " +
            $"laneSelfSame={_pr57CandidateLaneSelfSame} bendSelfSame={_pr57CandidateBendSelfSame} " +
            $"enumeratedCountSame={_pr57CandidateEnumeratedSame} " +
            $"occupierOrderedIdsSame={_pr57CandidateOccupierIdsSame} " +
            $"occupierOrderedStatesSame={_pr57CandidateOccupierStatesSame} " +
            $"deepOrderedIdsSame={_pr57CandidateDeepIdsSame} " +
            $"deepOrderedStatesSame={_pr57CandidateDeepStatesSame} " +
            $"allObservedSame={_pr57CandidateAllObservedSame} timeChanged={_pr57CandidateTimeChanged} " +
            $"snapshotCalls={_pr57CandidateSnapshotCalls} snapshotTicks={_pr57CandidateSnapshotTicks} " +
            $"snapshotBytes={_pr57CandidateSnapshotBytes} " +
            "sampled=True observational=True intrusive=True exactDependencies=False " +
            "transientWritesCovered=False signalsCovered=False pedestriansCovered=False");
        _pr57CandidatePrior.Clear();
        foreach (var (key, trace) in _pr57CandidateCurrent)
            _pr57CandidatePrior[key] = trace;
        _pr57CandidateCurrent.Clear();
        _pr57CandidateActive = null;
        _pr57CandidateSliceActive = false;
    }
}
