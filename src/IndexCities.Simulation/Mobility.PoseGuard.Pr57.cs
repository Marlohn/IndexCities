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
    static readonly bool Pr57NoCaptureStreetBodyEnabled =
        Environment.GetEnvironmentVariable("PR57_211_NO_CAPTURE_STREETBODY") == "1";
    static readonly bool Pr57ShipmentReadCensusEnabled =
        Environment.GetEnvironmentVariable("PR57_209_SHIPMENT_READ_CENSUS") == "1";
    [ThreadStatic] static bool _pr57BodySatScope;
    [ThreadStatic] static long _pr57BodySatCalls;
    long _pr57BodySatObserved;
    long _pr57ShipmentLookups, _pr57ShipmentVisits, _pr57ShipmentMatches;
    long _pr57ShipmentListTotal, _pr57ShipmentListMax;
    long _pr57PoseLoopVisits, _pr57PoseLoopNear, _pr57PoseLoopStreet;
    long _pr57PoseLoopEntryExcluded, _pr57PoseLoopTurn, _pr57PoseLoopHits;
    long _pr57StreetBodyReads, _pr57StreetBodyBytes;

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
        _pr57StreetBodyReads = _pr57StreetBodyBytes = 0;
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
    bool Pr57PoseGuardCandidateReached(Vehicle other)
    {
        if (_pr57PoseActive) _pr57PoseCandidates++;
        Pr57ExecPoseCandidate(other); // #220: bounded read-only fingerprint
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

    // #211: distinguish a captured shipment predicate display-class allocation
    // from real shipment reads. Isolated to the already opt-in #210 pose foreach.
    // The alternate path preserves the exact original short-circuit order.
    bool Pr57StreetBodyNoCapture(Vehicle o)
    {
        if (o.Machine != PavingMachine.None && o.Status == VehicleStatus.Parked) return true;
        if (OnSitePath(o)) return true;
        if (o.Machine != PavingMachine.None || o.Status != VehicleStatus.Unloading ||
            o.ShipmentId is not {} id) return false;
        foreach (var shipment in State.Shipments)
            if (shipment.Id == id && shipment.ReceivingMachineId != null) return true;
        return false;
    }

    bool Pr57PoseStreetBody(Vehicle o)
    {
        if (!_pr57PoseActive)
            return Pr57NoCaptureStreetBodyEnabled ? Pr57StreetBodyNoCapture(o) : StreetBody(o);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = Pr57NoCaptureStreetBodyEnabled ? Pr57StreetBodyNoCapture(o) : StreetBody(o);
        _pr57StreetBodyBytes += GC.GetAllocatedBytesForCurrentThread() - before;
        _pr57StreetBodyReads++;
        return result;
    }

    // #210: opt-in structural counterexample to LINQ Any, not a promoted optimization.
    // Preserve candidate order, short circuits and all original geometric decisions.
    bool Pr57PoseGuardForeach(Vehicle v, double approachHeading)
    {
        foreach (var o in State.Vehicles)
        {
            if (_pr57PoseActive) _pr57PoseLoopVisits++;
            if (o.Id == v.Id || o.Status == VehicleStatus.Outside ||
                !(o.Position.Distance(v.Position) < 12)) continue;
            Pr57PoseGuardCandidateReached(o);
            if (_pr57PoseActive) _pr57PoseLoopNear++;
            if (Pr57PoseStreetBody(o))
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

    // #226: read-only, slice-local shadow of ONLY the first three s6 pose
    // guard gates (ID, status, position). The canonical predicate always runs.
    // This is NOT a reusable s6 decision: street bodies, routes, clock, signals,
    // pedestrian and transient mutations remain outside this projection.
    static readonly bool Pr57S6GateShadowEnabled =
        Environment.GetEnvironmentVariable("PR57_226_S6_GATE_SHADOW") == "1";
    readonly Dictionary<int, (VehicleStatus Status, Point Position)> _pr57S6GateShadow = new();
    bool _pr57S6GateShadowActive;
    long _pr57S6GateVisits, _pr57S6GateStale, _pr57S6GateDisagree;
    long _pr57S6GateMissing, _pr57S6GateRefreshes, _pr57S6GateNew;
    long _pr57S6GateBuildTicks, _pr57S6GateRefreshTicks;
    long _pr57S6GateBuildBytes, _pr57S6GateRefreshBytes;

    void Pr57S6GateShadowBeginSlice()
    {
        _pr57S6GateShadowActive = Pr57S6GateShadowEnabled && _pr57ExclusiveActive;
        if (!_pr57S6GateShadowActive) return;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57S6GateShadow.Clear();
        _pr57S6GateVisits = _pr57S6GateStale = _pr57S6GateDisagree = 0;
        _pr57S6GateMissing = _pr57S6GateRefreshes = _pr57S6GateNew = 0;
        _pr57S6GateRefreshTicks = _pr57S6GateRefreshBytes = 0;
        foreach (var other in State.Vehicles)
            _pr57S6GateShadow.Add(other.Id, (other.Status, other.Position));
        _pr57S6GateBuildTicks = System.Diagnostics.Stopwatch.GetTimestamp() - started;
        _pr57S6GateBuildBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    // Always returns true, so the original LINQ Any short-circuit and candidate
    // order are unchanged. Observe BEFORE the canonical negative gates.
    bool Pr57S6GateShadowObserve(Vehicle reader, Vehicle other)
    {
        if (!_pr57S6GateShadowActive) return true;
        _pr57S6GateVisits++;
        if (!_pr57S6GateShadow.TryGetValue(other.Id, out var prior))
        {
            _pr57S6GateMissing++;
            return true;
        }
        if (prior.Status != other.Status || prior.Position != other.Position)
            _pr57S6GateStale++;
        var shadow = other.Id != reader.Id &&
                     prior.Status != VehicleStatus.Outside &&
                     prior.Position.Distance(reader.Position) < 12;
        var live = other.Id != reader.Id &&
                   other.Status != VehicleStatus.Outside &&
                   other.Position.Distance(reader.Position) < 12;
        if (shadow != live) _pr57S6GateDisagree++;
        return true;
    }

    // A processed actor may change its own pose. Refresh only that actor;
    // unmarked third-party writes are intentionally NOT repaired.
    void Pr57S6GateShadowAfterVehicle(Vehicle actor)
    {
        if (!_pr57S6GateShadowActive) return;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        if (!_pr57S6GateShadow.ContainsKey(actor.Id)) _pr57S6GateNew++;
        _pr57S6GateShadow[actor.Id] = (actor.Status, actor.Position);
        _pr57S6GateRefreshes++;
        _pr57S6GateRefreshTicks += System.Diagnostics.Stopwatch.GetTimestamp() - started;
        _pr57S6GateRefreshBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57S6GateShadowEndSlice()
    {
        if (!_pr57S6GateShadowActive) return;
        // This final O(N) oracle is AFTER SyncTowedVehicles, and is not used
        // to fix any decision or to claim complete transient-write coverage.
        long finalStale = 0, finalMissing = 0;
        foreach (var other in State.Vehicles)
        {
            if (!_pr57S6GateShadow.TryGetValue(other.Id, out var prior))
                finalMissing++;
            else if (prior.Status != other.Status || prior.Position != other.Position)
                finalStale++;
        }
        static double Ms(long ticks) =>
            ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_226_S6_GATE sample={_pr57ExclusiveSamples} " +
            $"vehicles={_pr57S6GateShadow.Count} visits={_pr57S6GateVisits} " +
            $"staleVisits={_pr57S6GateStale} gateDisagreements={_pr57S6GateDisagree} " +
            $"missingVisits={_pr57S6GateMissing} refreshes={_pr57S6GateRefreshes} " +
            $"newActors={_pr57S6GateNew} postSyncStale={finalStale} " +
            $"postSyncMissing={finalMissing} buildMs={Ms(_pr57S6GateBuildTicks):F6} " +
            $"refreshMs={Ms(_pr57S6GateRefreshTicks):F6} " +
            $"buildMB={_pr57S6GateBuildBytes / 1e6:F6} " +
            $"refreshMB={_pr57S6GateRefreshBytes / 1e6:F6} " +
            "scope=ID_STATUS_POSITION_GATE_ONLY canonicalAlwaysRuns=True " +
            "thirdPartyAndTransientWritersUnclosed=True observational=True intrusive=True");
        _pr57S6GateShadow.Clear();
        _pr57S6GateShadowActive = false;
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
                $"PR57_211_STREETBODY sample={_pr57ExclusiveSamples} calls={_pr57StreetBodyReads} " +
                $"bytes={_pr57StreetBodyBytes} noCapture={Pr57NoCaptureStreetBodyEnabled} " +
                "scope=s6-pose-street-body-only observational=True intrusive=True");
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
