namespace IndexCities.Simulation;

// PR57 #207: observational subdivision of the s6 post-safety region.
// It never decides whether a vehicle may move, and never suppresses a slice.
// Only samples the same three consecutive physical slices as #206.
public sealed partial class CitySimulation
{
    static readonly bool Pr57PoseGuardEnabled =
        Environment.GetEnvironmentVariable("PR57_207_POSE_GUARD") == "1";

    // #208: count SAT calls only while the original s6 guard is evaluating.
    // Thread-local scope follows the synchronous predicate; no decision is cached.
    static readonly bool Pr57BodySatCensusEnabled =
        Environment.GetEnvironmentVariable("PR57_208_BODY_SAT_CENSUS") == "1";
    static readonly bool Pr57PoseForeachEnabled =
        Environment.GetEnvironmentVariable("PR57_210_POSE_FOREACH") == "1";
    static readonly bool Pr57ShipmentReadCensusEnabled =
        Environment.GetEnvironmentVariable("PR57_209_SHIPMENT_READ_CENSUS") == "1";
    [ThreadStatic] static bool _pr57BodySatScope;
    [ThreadStatic] static long _pr57BodySatCalls;
    long _pr57BodySatObserved;
    long _pr57ShipmentLookups, _pr57ShipmentVisits, _pr57ShipmentMatches;
    long _pr57ShipmentListTotal, _pr57ShipmentListMax;
    long _pr57PoseLoopVisits, _pr57PoseLoopNear, _pr57PoseLoopStreet;
    long _pr57PoseLoopEntryExcluded, _pr57PoseLoopTurn, _pr57PoseLoopHits;

    long _pr57PoseCalls, _pr57PoseEligible, _pr57PoseCandidates, _pr57PoseHeadingChanges;
    long _pr57PoseTicks, _pr57PoseBytes;
    long _pr57PoseStartTicks, _pr57PoseStartBytes;
    double _pr57PoseOldHeading;
    bool _pr57PoseActive;

    void Pr57PoseGuardReset()
    {
        _pr57PoseCalls = _pr57PoseEligible = _pr57PoseCandidates = _pr57PoseHeadingChanges = 0;
        _pr57PoseTicks = _pr57PoseBytes = 0;
        _pr57BodySatObserved = 0;
        _pr57ShipmentLookups = _pr57ShipmentVisits = _pr57ShipmentMatches = 0;
        _pr57ShipmentListTotal = _pr57ShipmentListMax = 0;
        _pr57PoseLoopVisits = _pr57PoseLoopNear = _pr57PoseLoopStreet = 0;
        _pr57PoseLoopEntryExcluded = _pr57PoseLoopTurn = _pr57PoseLoopHits = 0;
        _pr57BodySatScope = false;
        _pr57PoseActive = false;
    }

    void Pr57PoseGuardBegin(Vehicle vehicle, double step, double d)
    {
        _pr57PoseActive = Pr57PoseGuardEnabled && _pr57ExclusiveActive;
        if (!_pr57PoseActive) return;
        _pr57PoseCalls++;
        if (step < .00001 && d > .00001) _pr57PoseEligible++;
        _pr57PoseOldHeading = vehicle.Heading;
        _pr57PoseStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57PoseStartBytes = GC.GetAllocatedBytesForCurrentThread();
        if (Pr57BodySatCensusEnabled || Pr57ShipmentReadCensusEnabled)
        {
            _pr57BodySatCalls = 0;
            _pr57BodySatScope = true;
        }
    }

    // Called only after the original ID, status and distance short-circuits.
    // Returning true preserves the original predicate; this is not a blocker count.
    bool Pr57PoseGuardCandidateReached()
    {
        if (_pr57PoseActive) _pr57PoseCandidates++;
        return true;
    }

    void Pr57PoseGuardEnd(Vehicle vehicle)
    {
        if (!_pr57PoseActive) return;
        _pr57PoseTicks += System.Diagnostics.Stopwatch.GetTimestamp() - _pr57PoseStartTicks;
        _pr57PoseBytes += GC.GetAllocatedBytesForCurrentThread() - _pr57PoseStartBytes;
        if (Pr57BodySatCensusEnabled || Pr57ShipmentReadCensusEnabled)
        {
            _pr57BodySatObserved += _pr57BodySatCalls;
            _pr57BodySatScope = false;
        }
        if (vehicle.Heading != _pr57PoseOldHeading) _pr57PoseHeadingChanges++;
        _pr57PoseActive = false;
    }

    // #210: opt-in structural counterexample to LINQ Any, not a promoted optimization.
    // Preserve candidate order, short circuits and all original geometric decisions.
    bool Pr57PoseGuardForeach(Vehicle v, double approachHeading)
    {
        foreach (var o in State.Vehicles)
        {
            if (_pr57PoseActive) _pr57PoseLoopVisits++;
            if (o.Id == v.Id || o.Status == VehicleStatus.Outside ||
                o.Position.Distance(v.Position) >= 12) continue;
            Pr57PoseGuardCandidateReached();
            if (_pr57PoseActive) _pr57PoseLoopNear++;
            if (StreetBody(o))
            {
                if (_pr57PoseActive) _pr57PoseLoopStreet++;
                if (VehicleBodiesOverlap(v, v.Position, o))
                {
                    if (_pr57PoseActive) _pr57PoseLoopHits++;
                    return true;
                }
            }
            else if (o.Position.Distance(State.Entry) < 8 &&
                     v.Position.Distance(State.Entry) < 8)
            {
                if (_pr57PoseActive) _pr57PoseLoopEntryExcluded++;
            }
            else
            {
                if (_pr57PoseActive) _pr57PoseLoopTurn++;
                if (TurnsInto(v, approachHeading, o))
                {
                    if (_pr57PoseActive) _pr57PoseLoopHits++;
                    return true;
                }
            }
        }
        return false;
    }

    void Pr57PoseGuardReport()
    {
        if (!Pr57PoseGuardEnabled || !_pr57ExclusiveActive) return;
        var ms = _pr57PoseTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_207_POSE sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
            $"calls={_pr57PoseCalls} eligible={_pr57PoseEligible} geometryCandidates={_pr57PoseCandidates} " +
            $"headingChanges={_pr57PoseHeadingChanges} guardMs={ms:F6} guardMB={_pr57PoseBytes / 1e6:F6} " +
            "scope=s6-pose-guard-only observational=True intrusive=True");
        if (Pr57BodySatCensusEnabled)
            Console.WriteLine(
                $"PR57_208_SAT sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
                $"satCalls={_pr57BodySatObserved} geometryCandidates={_pr57PoseCandidates} " +
                $"guardMB={_pr57PoseBytes / 1e6:F6} " +
                "scope=s6-pose-guard-BodiesOverlap-only observational=True intrusive=True");
        if (Pr57PoseForeachEnabled)
            Console.WriteLine(
                $"PR57_210_LOOP sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
                $"visited={_pr57PoseLoopVisits} near={_pr57PoseLoopNear} street={_pr57PoseLoopStreet} " +
                $"entryExcluded={_pr57PoseLoopEntryExcluded} turn={_pr57PoseLoopTurn} hits={_pr57PoseLoopHits} " +
                $"guardMB={_pr57PoseBytes / 1e6:F6} " +
                "scope=s6-pose-foreach-only observational=True intrusive=True");
        if (Pr57ShipmentReadCensusEnabled)
            Console.WriteLine(
                $"PR57_209_SHIPMENTS sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
                $"lookups={_pr57ShipmentLookups} visits={_pr57ShipmentVisits} matches={_pr57ShipmentMatches} " +
                $"listTotal={_pr57ShipmentListTotal} listMax={_pr57ShipmentListMax} " +
                $"geometryCandidates={_pr57PoseCandidates} guardMB={_pr57PoseBytes / 1e6:F6} " +
                "scope=s6-pose-guard-shipment-Any-only observational=True intrusive=True");
    }
}
