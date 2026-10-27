namespace IndexCities.Simulation;

// PR57 #230: bounded read-only candidate counterfactual for REAL s3 flips.
// This does not execute a frozen phase, substitute a candidate or skip a slice.
public sealed partial class CitySimulation
{
    static readonly bool Pr57S3CounterEnabled =
        Environment.GetEnvironmentVariable("PR57_230_S3_COUNTERFACTUAL") == "1";

    sealed class Pr57S3CounterRecord
    {
        public double? Frozen, Live;
        public string FrozenPath = "", LivePath = "";
    }

    bool _pr57S3CounterActive;
    readonly Dictionary<int, Vehicle> _pr57S3Frozen = new();
    readonly Dictionary<Pr57PhaseFlipEvent, Pr57S3CounterRecord> _pr57S3Compared = new();
    long _pr57S3CounterFlips, _pr57S3CounterDifferent, _pr57S3CounterLiveMismatch;
    long _pr57S3CounterPotentialNewLeader, _pr57S3CounterMissing;

    static readonly System.Reflection.MethodInfo Pr57S3CloneMethod =
        typeof(object).GetMethod("MemberwiseClone",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

    static Vehicle Pr57S3FrozenCopy(Vehicle source)
    {
        // A shallow object copy keeps scalar state at the slice boundary.
        // Copy mutable collections consulted by the s3 predicate. Other nested
        // reference state remains a documented limitation of this POC.
        var copy = (Vehicle)Pr57S3CloneMethod.Invoke(source, null)!;
        copy.Route = new List<int>(source.Route);
        copy.Detour = new List<Point>(source.Detour);
        copy.SitePath = new List<Point>(source.SitePath);
        copy.ManoeuvreSpaces = new List<int>(source.ManoeuvreSpaces);
        return copy;
    }

    void Pr57S3CounterBeginSlice()
    {
        if (!Pr57S3CounterEnabled || !_pr57PhaseImpactActive) return;
        _pr57S3CounterActive = true;
        _pr57S3Frozen.Clear();
        _pr57S3Compared.Clear();
        _pr57S3CounterFlips = _pr57S3CounterDifferent = 0;
        _pr57S3CounterLiveMismatch = _pr57S3CounterPotentialNewLeader = _pr57S3CounterMissing = 0;
        foreach (var v in State.Vehicles) _pr57S3Frozen[v.Id] = Pr57S3FrozenCopy(v);
    }

    // Mirrors the canonical s3 candidate's read-only gates, in their order.
    // The canonical loop remains the sole owner of HoldFor and all side effects.
    double? Pr57S3CandidateHold(Vehicle reader, Vehicle other,
        bool enteringLane, bool insideMap, LaneAccess access, out string path)
    {
        if (other.Id != reader.Id && (!LeavesDock(reader) || OnCarriageway(reader)) &&
            BodyAcrossLane(reader, other) is {} docked)
        {
            path = "dock";
            return docked;
        }
        if (other.Id == reader.Id || !OccupiesLane(other))
        {
            path = "ineligible";
            return null;
        }
        var relative = other.Position - reader.Position;
        if (insideMap && relative.Distance(new Point(0, 0)) < .1)
        {
            path = "coincident";
            return other.Status == VehicleStatus.Stranded || other.Id < reader.Id ? 0 : null;
        }
        if (enteringLane && BlocksLaneEntry(reader, access, other))
        {
            path = "entry";
            return 0;
        }
        var sin = Math.Sin(reader.Heading);
        var cos = Math.Cos(reader.Heading);
        var forward = relative.X * sin + relative.Z * cos;
        var side = Math.Abs(relative.X * cos - relative.Z * sin);
        var cosDiff = Math.Cos(other.Heading - reader.Heading);
        if (other.Detour.Count > 0 && cosDiff < -.5 && forward > 0 && side < 2.6)
        {
            path = "detour";
            return forward;
        }
        if ((other.TowPhase == TowPhase.Loading ||
             other.TowPhase == TowPhase.Delivering && other.WaitingSince == 0) &&
            cosDiff < -.5 && forward > 0 && side < 1.2)
        {
            path = "tow";
            return forward;
        }
        if (cosDiff > .5 && forward > 0 && side < 2.6)
        {
            var back = -relative.X * Math.Sin(other.Heading) -
                       relative.Z * Math.Cos(other.Heading);
            var backSide = Math.Abs(-relative.X * Math.Cos(other.Heading) +
                                     relative.Z * Math.Sin(other.Heading));
            if (back > 0 && backSide < 2.6 && other.Id > reader.Id &&
                other.Status != VehicleStatus.Stranded)
            {
                path = "tie-break";
                return null;
            }
            path = "leader";
            return forward;
        }
        path = "not-leader";
        return null;
    }

    // Called only when the existing #228 observer has just registered a new
    // changed forward/side gate, before the canonical candidate is evaluated.
    void Pr57S3CounterObserve(Vehicle reader, Vehicle live, bool enteringLane,
        bool insideMap, LaneAccess access)
    {
        if (!_pr57S3CounterActive || _pr57PhaseCurrentFlips.Count == 0) return;
        var flip = _pr57PhaseCurrentFlips[^1];
        if (flip.ReaderId != reader.Id || flip.CandidateId != live.Id ||
            _pr57S3Compared.ContainsKey(flip)) return;
        if (!_pr57S3Frozen.TryGetValue(live.Id, out var frozen))
        {
            _pr57S3CounterMissing++;
            return;
        }
        var liveHold = Pr57S3CandidateHold(reader, live, enteringLane, insideMap, access,
            out var livePath);
        var frozenHold = Pr57S3CandidateHold(reader, frozen, enteringLane, insideMap, access,
            out var frozenPath);
        _pr57S3Compared.Add(flip, new Pr57S3CounterRecord {
            Frozen = frozenHold, Live = liveHold,
            FrozenPath = frozenPath, LivePath = livePath
        });
    }

    void Pr57S3CounterAfterScan(double canonicalLeaderDistance)
    {
        if (!_pr57S3CounterActive) return;
        foreach (var flip in _pr57PhaseCurrentFlips)
        {
            if (!_pr57S3Compared.TryGetValue(flip, out var result)) continue;
            _pr57S3CounterFlips++;
            var changed = result.Frozen != result.Live;
            var mismatch = (result.Live.HasValue != flip.Held) ||
                           (result.Live.HasValue && result.Live.Value != flip.HeldDistance);
            var potential = result.Frozen.HasValue &&
                            result.Frozen.Value < canonicalLeaderDistance &&
                            !flip.Held;
            if (changed) _pr57S3CounterDifferent++;
            if (mismatch) _pr57S3CounterLiveMismatch++;
            if (potential) _pr57S3CounterPotentialNewLeader++;
            Console.WriteLine(
                "PR57_230_S3_COUNTERFACTUAL sample=" + _pr57ExclusiveSamples +
                " reader=" + flip.ReaderId + " candidate=" + flip.CandidateId +
                " frozenPath=" + result.FrozenPath + " livePath=" + result.LivePath +
                " frozenHold=" + (result.Frozen?.ToString("R") ?? "none") +
                " liveHold=" + (result.Live?.ToString("R") ?? "none") +
                " canonicalHold=" + flip.Held +
                " liveMismatch=" + mismatch + " changed=" + changed +
                " potentialNewLeader=" + potential +
                " readerHeldFixed=True frozenNestedReferencesNotProven=True observational=True");
        }
    }

    void Pr57S3CounterEndSlice()
    {
        if (!_pr57S3CounterActive) return;
        Console.WriteLine(
            "PR57_230_S3_SUMMARY sample=" + _pr57ExclusiveSamples +
            " flips=" + _pr57S3CounterFlips +
            " changed=" + _pr57S3CounterDifferent +
            " liveMismatch=" + _pr57S3CounterLiveMismatch +
            " potentialNewLeader=" + _pr57S3CounterPotentialNewLeader +
            " missing=" + _pr57S3CounterMissing +
            " fullPhaseReplay=False nestedReferencesNotFrozen=True observational=True");
        _pr57S3CounterActive = false;
        _pr57S3Frozen.Clear();
        _pr57S3Compared.Clear();
    }
}
