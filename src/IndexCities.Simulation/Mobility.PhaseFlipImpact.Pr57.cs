namespace IndexCities.Simulation;

// PR57 #229: bounded, observational attribution of the REAL s3 gate flips
// found by #228. No phase-major execution, candidate substitution or cache.
public sealed partial class CitySimulation
{
    static readonly bool Pr57PhaseImpactEnabled =
        Environment.GetEnvironmentVariable("PR57_229_PHASE_FLIP_IMPACT") == "1";

    sealed class Pr57PhaseFlipEvent
    {
        public int ReaderId, CandidateId;
        public Pr57PhaseExposureSnapshot Initial, AtRead;
        public bool Held;
        public double HeldDistance;
    }

    bool _pr57PhaseImpactActive;
    readonly Dictionary<int, Pr57PhaseExposureSnapshot> _pr57PhaseBeforeActor = new();
    readonly Dictionary<int, Pr57PhaseExposureSnapshot> _pr57PhaseAfterActor = new();
    readonly List<Pr57PhaseFlipEvent> _pr57PhaseCurrentFlips = new();
    long _pr57PhaseImpactFlips, _pr57PhaseImpactHeld, _pr57PhaseImpactWinners;
    long _pr57PhaseImpactBeforeOwn, _pr57PhaseImpactDuringOwn, _pr57PhaseImpactAfterOwn;

    void Pr57PhaseImpactBeginSlice()
    {
        if (!Pr57PhaseImpactEnabled || !_pr57PhaseActive) return;
        _pr57PhaseImpactActive = true;
        _pr57PhaseBeforeActor.Clear();
        _pr57PhaseAfterActor.Clear();
        _pr57PhaseCurrentFlips.Clear();
        _pr57PhaseImpactFlips = _pr57PhaseImpactHeld = _pr57PhaseImpactWinners = 0;
        _pr57PhaseImpactBeforeOwn = _pr57PhaseImpactDuringOwn = _pr57PhaseImpactAfterOwn = 0;
    }

    void Pr57PhaseImpactBeforeActor(Vehicle v)
    {
        if (_pr57PhaseImpactActive) _pr57PhaseBeforeActor[v.Id] = Pr57PhaseSnapshot(v);
    }

    void Pr57PhaseImpactAfterActor(Vehicle v)
    {
        if (_pr57PhaseImpactActive) _pr57PhaseAfterActor[v.Id] = Pr57PhaseSnapshot(v);
    }

    void Pr57PhaseImpactBeforeScan()
    {
        if (_pr57PhaseImpactActive) _pr57PhaseCurrentFlips.Clear();
    }

    // Invoked only when #228 observes a changed necessary forward/side gate.
    void Pr57PhaseImpactFlip(Vehicle reader, Vehicle candidate,
        Pr57PhaseExposureSnapshot initial, Pr57PhaseExposureSnapshot atRead)
    {
        if (!_pr57PhaseImpactActive) return;
        _pr57PhaseCurrentFlips.Add(new Pr57PhaseFlipEvent {
            ReaderId = reader.Id, CandidateId = candidate.Id,
            Initial = initial, AtRead = atRead
        });
    }

    void Pr57PhaseImpactHold(Vehicle other, double distance)
    {
        if (!_pr57PhaseImpactActive || _pr57PhaseCurrentFlips.Count == 0) return;
        foreach (var flip in _pr57PhaseCurrentFlips)
            if (flip.CandidateId == other.Id) {
                flip.Held = true;
                flip.HeldDistance = distance;
            }
    }

    void Pr57PhaseImpactAfterScan(Vehicle reader, Vehicle? blocker,
        double leaderDistance, double nominalStep)
    {
        if (!_pr57PhaseImpactActive) return;
        foreach (var flip in _pr57PhaseCurrentFlips) {
            var beforeKnown = _pr57PhaseBeforeActor.TryGetValue(flip.CandidateId, out var before);
            var afterKnown = _pr57PhaseAfterActor.TryGetValue(flip.CandidateId, out var after);
            var changedBeforeOwn = beforeKnown && before != flip.Initial;
            var changedDuringOwn = beforeKnown && afterKnown && before != after;
            var changedAfterOwn = afterKnown && after != flip.AtRead;
            var won = blocker?.Id == flip.CandidateId;
            _pr57PhaseImpactFlips++;
            if (flip.Held) _pr57PhaseImpactHeld++;
            if (won) _pr57PhaseImpactWinners++;
            if (changedBeforeOwn) _pr57PhaseImpactBeforeOwn++;
            if (changedDuringOwn) _pr57PhaseImpactDuringOwn++;
            if (changedAfterOwn) _pr57PhaseImpactAfterOwn++;
            Console.WriteLine(
                $"PR57_229_PHASE_FLIP sample={_pr57ExclusiveSamples} " +
                $"reader={reader.Id} candidate={flip.CandidateId} " +
                $"candidateProcessed={_pr57PhaseProcessed.Contains(flip.CandidateId)} " +
                $"beforeKnown={beforeKnown} afterKnown={afterKnown} " +
                $"changedBeforeOwn={changedBeforeOwn} changedDuringOwn={changedDuringOwn} " +
                $"changedAfterOwn={changedAfterOwn} " +
                $"initialX={flip.Initial.Position.X:R} initialZ={flip.Initial.Position.Z:R} " +
                $"readX={flip.AtRead.Position.X:R} readZ={flip.AtRead.Position.Z:R} " +
                $"held={flip.Held} heldDistance={flip.HeldDistance:R} " +
                $"won={won} blocker={blocker?.Id ?? -1} " +
                $"leaderDistance={leaderDistance:R} nominalStep={nominalStep:R} " +
                "gateIsNecessaryNotSufficient=True writerAttributionIsNetOnly=True " +
                "canonicalAlwaysDecides=True");
        }
        _pr57PhaseCurrentFlips.Clear();
    }

    void Pr57PhaseImpactEndSlice()
    {
        if (!_pr57PhaseImpactActive) return;
        Console.WriteLine(
            $"PR57_229_PHASE_IMPACT sample={_pr57ExclusiveSamples} " +
            $"flips={_pr57PhaseImpactFlips} held={_pr57PhaseImpactHeld} " +
            $"winners={_pr57PhaseImpactWinners} changedBeforeOwn={_pr57PhaseImpactBeforeOwn} " +
            $"changedDuringOwn={_pr57PhaseImpactDuringOwn} changedAfterOwn={_pr57PhaseImpactAfterOwn} " +
            "netSnapshotsOnly=True noPhaseBatching=True observational=True");
        _pr57PhaseImpactActive = false;
        _pr57PhaseBeforeActor.Clear();
        _pr57PhaseAfterActor.Clear();
        _pr57PhaseCurrentFlips.Clear();
    }
}
