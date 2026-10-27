namespace IndexCities.Simulation;

// PR57 #218: bounded observational execution-path census in three consecutive
// physical sub-slices. Tracks only a SELECTED projection of vehicle state and
// the actual stage sequence. Equal projections/paths are not causal equivalence:
// other actors, time, signals, pedestrians, roads, list contents and transient
// writes are NOT included. No scheduling, predicates or saves use this probe.
public sealed partial class CitySimulation
{
    static readonly bool Pr57ExecEnabled =
        Environment.GetEnvironmentVariable("PR57_218_EXEC_PATH") == "1";

    readonly record struct Pr57ExecProjection(
        VehicleStatus Status, Point Position, long HeadingBits,
        int RouteIndex, int RouteCount, int SitePathIndex, int SitePathCount,
        bool SitePathBeforeRoad, int DetourCount, int PushCount,
        int? Claim, int SpaceCount, int CorridorCount, bool HasAdmission,
        Point? Dock, Point? ManoeuvreDock, Point? Holding,
        long WaitingBits, long Fuel, long Burned, long Condition,
        long LoadTimer, int? Driver, int? Shipment, int? Destination,
        TowPhase Tow, int? TowTarget, ServicePhase Service,
        MachinePhase Machine, bool BrokenDown);

    readonly record struct Pr57ExecPrior(
        Pr57ExecProjection Before, Pr57ExecProjection After, ulong Path, ulong PoseHash, long PoseReads);

    readonly Dictionary<int, Pr57ExecPrior> _pr57ExecPrevious = new();
    readonly Dictionary<int, Pr57ExecPrior> _pr57ExecCurrent = new();
    bool _pr57ExecActive, _pr57ExecVehicleActive;
    Pr57ExecProjection _pr57ExecBefore;
    ulong _pr57ExecPath;
    int _pr57ExecMask;
    long _pr57ExecStart;
    long _pr57ExecVisits, _pr57ExecFull, _pr57ExecMoved, _pr57ExecFullStill;
    long _pr57ExecOwnChanged, _pr57ExecCompared, _pr57ExecSameBefore;
    long _pr57ExecSameSinceEnd, _pr57ExecBoundaryChanged;
    long _pr57ExecSamePath, _pr57ExecSamePathBefore;
    long _pr57ExecSamePathBeforeAfter, _pr57ExecFullSameBefore;
    long _pr57ExecTicks, _pr57ExecFullTicks, _pr57ExecRepeatedTicks;
    // PR57 #219: exclusive segments from #206, aggregated ONLY for the
    // repeated full-path cohort. Stage 3/4 contain lane/bend scans; other
    // stages can ALSO contain scans and external reads. Not an Amdahl bound.
    static readonly bool Pr57StageCostEnabled =
        Environment.GetEnvironmentVariable("PR57_219_STAGE_COST") == "1";
    readonly long[] _pr57ExecVehicleStageTicks = new long[9];
    readonly long[] _pr57ExecVehicleStageBytes = new long[9];
    readonly long[] _pr57ExecRepeatedStageTicks = new long[9];
    readonly long[] _pr57ExecRepeatedStageBytes = new long[9];
    long _pr57ExecRepeatedFullCount;
    // #220: bounded fingerprint of actual near-body inputs read by the s6
    // pose guard. This is NOT a complete dependency signature: other systems,
    // shipments, route list contents, time and transient writes are omitted.
    static readonly bool Pr57PoseReadSetEnabled =
        Environment.GetEnvironmentVariable("PR57_220_POSE_READSET") == "1";
    ulong _pr57ExecPoseHash;
    long _pr57ExecPoseReads;
    string? _pr57ExecCauseBefore;
    long _pr57PoseRepeated, _pr57PoseSame, _pr57PoseChanged, _pr57PoseZero;
    long _pr57PoseReadVisits, _pr57PoseSameS6Ticks, _pr57PoseChangedS6Ticks;
    long _pr57PoseCauseValueChanged, _pr57PoseCauseReferenceReplaced;

    static ulong Pr57PoseMix(ulong hash, long value) =>
        unchecked((hash ^ (ulong)value) * 1099511628211UL);

    // Called only AFTER the original ID/status/distance gates. Hashing follows
    // the actual iteration order, not a reconstructed candidate list.
    void Pr57ExecPoseCandidate(Vehicle other)
    {
        if (!Pr57PoseReadSetEnabled || !_pr57ExecVehicleActive) return;
        var h = _pr57ExecPoseHash;
        h = Pr57PoseMix(h, other.Id);
        h = Pr57PoseMix(h, (int)other.Status);
        h = Pr57PoseMix(h, (int)other.Machine);
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(other.Position.X));
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(other.Position.Z));
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(other.Heading));
        h = Pr57PoseMix(h, other.RouteIndex);
        h = Pr57PoseMix(h, other.Route.Count);
        h = Pr57PoseMix(h, other.SitePathIndex);
        h = Pr57PoseMix(h, other.SitePath.Count);
        h = Pr57PoseMix(h, other.ShipmentId ?? -1);
        h = Pr57PoseMix(h, other.IntersectionClaim ?? -1);
        h = Pr57PoseMix(h, other.ManoeuvreSpaces.Count);
        h = Pr57PoseMix(h, other.DockCorridor.Count);
        h = Pr57PoseMix(h, other.MovementAdmission == null ? 0 : 1);
        h = Pr57PoseMix(h, other.BrokenDown ? 1 : 0);
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(other.WaitingSince));
        _pr57ExecPoseHash = h;
        _pr57ExecPoseReads++;
    }

    static Pr57ExecProjection Pr57ExecSnapshot(Vehicle v) => new(
        v.Status, v.Position, BitConverter.DoubleToInt64Bits(v.Heading),
        v.RouteIndex, v.Route.Count, v.SitePathIndex, v.SitePath.Count,
        v.SitePathBeforeRoad, v.Detour.Count, v.PushPath.Count,
        v.IntersectionClaim, v.ManoeuvreSpaces.Count, v.DockCorridor.Count,
        v.MovementAdmission != null, v.DockPosition, v.ManoeuvreDock,
        v.HoldingPosition, BitConverter.DoubleToInt64Bits(v.WaitingSince),
        v.FuelUnits, v.FuelBurnedLastStepUnits, v.ConditionUnits,
        v.LoadTimerUnits, v.DriverId, v.ShipmentId, v.DestinationBuildingId,
        v.TowPhase, v.TowTargetId, v.ServicePhase, v.MachinePhase, v.BrokenDown);

    void Pr57ExecBeginSlice()
    {
        _pr57ExecActive = Pr57ExecEnabled && _pr57ExclusiveActive;
        if (!_pr57ExecActive) return;
        if (_pr57ExclusiveSamples == 1) _pr57ExecPrevious.Clear();
        _pr57ExecCurrent.Clear();
        _pr57ExecVisits = _pr57ExecFull = _pr57ExecMoved = _pr57ExecFullStill = 0;
        _pr57ExecOwnChanged = _pr57ExecCompared = _pr57ExecSameBefore = 0;
        _pr57ExecSameSinceEnd = _pr57ExecBoundaryChanged = 0;
        _pr57ExecSamePath = _pr57ExecSamePathBefore = 0;
        _pr57ExecSamePathBeforeAfter = _pr57ExecFullSameBefore = 0;
        _pr57ExecTicks = _pr57ExecFullTicks = _pr57ExecRepeatedTicks = 0;
        if (Pr57StageCostEnabled)
        {
            Array.Clear(_pr57ExecRepeatedStageTicks);
            Array.Clear(_pr57ExecRepeatedStageBytes);
            _pr57ExecRepeatedFullCount = 0;
        }
        if (Pr57PoseReadSetEnabled)
        {
            _pr57PoseRepeated = _pr57PoseSame = _pr57PoseChanged = _pr57PoseZero = 0;
            _pr57PoseReadVisits = _pr57PoseSameS6Ticks = _pr57PoseChangedS6Ticks = 0;
            _pr57PoseCauseValueChanged = _pr57PoseCauseReferenceReplaced = 0;
        }
    }

    void Pr57ExecBeginVehicle(Vehicle v)
    {
        if (!_pr57ExecActive) return;
        _pr57ExecVehicleActive = true;
        _pr57ExecBefore = Pr57ExecSnapshot(v);
        if (Pr57PoseReadSetEnabled)
        {
            _pr57ExecPoseHash = 14695981039346656037UL;
            _pr57ExecPoseReads = 0;
            _pr57ExecCauseBefore = v.Cause;
        }
        _pr57ExecPath = 14695981039346656037UL; // deterministic FNV-style stage sequence
        _pr57ExecMask = 0;
        if (Pr57StageCostEnabled)
        {
            Array.Clear(_pr57ExecVehicleStageTicks);
            Array.Clear(_pr57ExecVehicleStageBytes);
        }
        _pr57ExecStart = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    void Pr57ExecMark(int stage)
    {
        if (!_pr57ExecVehicleActive) return;
        _pr57ExecPath = unchecked((_pr57ExecPath ^ (uint)(stage + 1)) * 1099511628211UL);
        _pr57ExecMask |= 1 << stage;
    }

    void Pr57ExecRecordSegment(int stage, long ticks, long bytes)
    {
        if (!Pr57StageCostEnabled || !_pr57ExecVehicleActive) return;
        _pr57ExecVehicleStageTicks[stage] += ticks;
        _pr57ExecVehicleStageBytes[stage] += bytes;
    }

    void Pr57ExecFinishVehicle(Vehicle v)
    {
        if (!_pr57ExecVehicleActive) return;
        _pr57ExecVehicleActive = false;
        var elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - _pr57ExecStart;
        var after = Pr57ExecSnapshot(v);
        var full = (_pr57ExecMask & ((1 << 3) | (1 << 4) | (1 << 5))) ==
                   ((1 << 3) | (1 << 4) | (1 << 5));
        var moved = _pr57ExecBefore.Position != after.Position;
        _pr57ExecVisits++;
        _pr57ExecTicks += elapsed;
        if (full) { _pr57ExecFull++; _pr57ExecFullTicks += elapsed; }
        if (moved) _pr57ExecMoved++;
        else if (full) _pr57ExecFullStill++;
        if (_pr57ExecBefore != after) _pr57ExecOwnChanged++;

        if (_pr57ExecPrevious.TryGetValue(v.Id, out var previous))
        {
            _pr57ExecCompared++;
            var sameBefore = previous.Before == _pr57ExecBefore;
            var sameSinceEnd = previous.After == _pr57ExecBefore;
            var samePath = previous.Path == _pr57ExecPath;
            if (sameBefore) _pr57ExecSameBefore++;
            if (sameSinceEnd) _pr57ExecSameSinceEnd++;
            else _pr57ExecBoundaryChanged++; // includes cleanup, tow sync, other writers
            if (samePath) _pr57ExecSamePath++;
            if (samePath && sameBefore)
            {
                _pr57ExecSamePathBefore++;
                _pr57ExecRepeatedTicks += elapsed;
                if (full)
                {
                    _pr57ExecFullSameBefore++;
                    if (Pr57PoseReadSetEnabled)
                    {
                        _pr57PoseRepeated++;
                        _pr57PoseReadVisits += _pr57ExecPoseReads;
                        if (_pr57ExecPoseReads == 0) _pr57PoseZero++;
                        var samePose = previous.PoseReads == _pr57ExecPoseReads &&
                                       previous.PoseHash == _pr57ExecPoseHash;
                        if (samePose)
                        {
                            _pr57PoseSame++;
                            if (Pr57StageCostEnabled)
                                _pr57PoseSameS6Ticks += _pr57ExecVehicleStageTicks[6];
                        }
                        else
                        {
                            _pr57PoseChanged++;
                            if (Pr57StageCostEnabled)
                                _pr57PoseChangedS6Ticks += _pr57ExecVehicleStageTicks[6];
                        }
                        if (!string.Equals(_pr57ExecCauseBefore, v.Cause,
                                           StringComparison.Ordinal))
                            _pr57PoseCauseValueChanged++;
                        if (!ReferenceEquals(_pr57ExecCauseBefore, v.Cause))
                            _pr57PoseCauseReferenceReplaced++;
                    }
                    if (Pr57StageCostEnabled)
                    {
                        _pr57ExecRepeatedFullCount++;
                        for (var i = 0; i < 9; i++)
                        {
                            _pr57ExecRepeatedStageTicks[i] += _pr57ExecVehicleStageTicks[i];
                            _pr57ExecRepeatedStageBytes[i] += _pr57ExecVehicleStageBytes[i];
                        }
                    }
                }
                if (previous.After == after) _pr57ExecSamePathBeforeAfter++;
            }
        }
        _pr57ExecCurrent[v.Id] = new(_pr57ExecBefore, after, _pr57ExecPath,
            _pr57ExecPoseHash, _pr57ExecPoseReads);
    }

    void Pr57ExecEndSlice()
    {
        if (!_pr57ExecActive) return;
        static double Ms(long ticks) =>
            Math.Round(ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency, 6);
        Console.WriteLine(
            $"PR57_218_EXEC sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
            $"visits={_pr57ExecVisits} fullPath={_pr57ExecFull} moved={_pr57ExecMoved} " +
            $"fullPathNoMove={_pr57ExecFullStill} ownProjectionChanged={_pr57ExecOwnChanged} " +
            $"compared={_pr57ExecCompared} sameBefore={_pr57ExecSameBefore} " +
            $"sameSincePreviousEnd={_pr57ExecSameSinceEnd} boundaryChanged={_pr57ExecBoundaryChanged} " +
            $"sameStagePath={_pr57ExecSamePath} sameStagePathAndBefore={_pr57ExecSamePathBefore} " +
            $"sameStagePathBeforeAfter={_pr57ExecSamePathBeforeAfter} " +
            $"fullPathSameBefore={_pr57ExecFullSameBefore} " +
            $"totalMs={Ms(_pr57ExecTicks)} fullMs={Ms(_pr57ExecFullTicks)} " +
            $"repeatedMs={Ms(_pr57ExecRepeatedTicks)} " +
            "scope=selectedProjectionNotFullInput noExternalClosure=True " +
            "clockNotIncluded=True pathEqualityNotDecisionEquality=True " +
            "observational=True intrusive=True");
        if (Pr57StageCostEnabled)
        {
            var ticks = _pr57ExecRepeatedStageTicks.Sum();
            var bytes = _pr57ExecRepeatedStageBytes.Sum();
            var scanContaining = _pr57ExecRepeatedStageTicks[3] + _pr57ExecRepeatedStageTicks[4];
            Console.WriteLine(
                $"PR57_219_COST sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
                $"repeatedFull={_pr57ExecRepeatedFullCount} totalMs={Ms(ticks)} " +
                $"scanContainingMs={Ms(scanContaining)} remainingStagesMs={Ms(ticks - scanContaining)} " +
                $"stage6Ms={Ms(_pr57ExecRepeatedStageTicks[6])} totalMB={bytes / 1e6:F6} " +
                string.Join(" ", Enumerable.Range(0, 9).Select(i =>
                    $"s{i}Ms={Ms(_pr57ExecRepeatedStageTicks[i])} s{i}MB={_pr57ExecRepeatedStageBytes[i] / 1e6:F6}")) +
                " scope=repeatedFullPathSelectedProjection stage3And4ContainScans=True " +
                "remainingStagesNotScanFree=True externalInvalidatorsNotClosed=True " +
                "observational=True intrusive=True");
        }
        if (Pr57PoseReadSetEnabled)
            Console.WriteLine(
                $"PR57_220_POSE_READSET sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
                $"repeatedFull={_pr57PoseRepeated} sameReadFingerprint={_pr57PoseSame} " +
                $"changedReadFingerprint={_pr57PoseChanged} noNearCandidates={_pr57PoseZero} " +
                $"nearCandidateReads={_pr57PoseReadVisits} sameS6Ms={Ms(_pr57PoseSameS6Ticks)} " +
                $"changedS6Ms={Ms(_pr57PoseChangedS6Ticks)} " +
                $"causeValueChanged={_pr57PoseCauseValueChanged} " +
                $"causeReferenceReplaced={_pr57PoseCauseReferenceReplaced} " +
                "scope=s6-near-body-only fingerprintNotProof=True " +
                "shipmentsClockPedestriansSignalsRoutesOmitted=True " +
                "transientWritesNotTracked=True observational=True intrusive=True");
        _pr57ExecPrevious.Clear();
        foreach (var (id, row) in _pr57ExecCurrent) _pr57ExecPrevious[id] = row;
        _pr57ExecCurrent.Clear();
        _pr57ExecActive = false;
    }
}
