namespace IndexCities.Simulation;

// PR57 #225: deliberately intrusive, opt-in, read-only census of SELECTED
// physical projection changes between consecutive canonical vehicle stages.
// This does NOT close external dependencies (routes/list contents, roads,
// signals, pedestrians, clock, transient writes, negative reads, editor).
// No simulation predicate, cache, save, order or decision consumes this data.
public sealed partial class CitySimulation
{
    static readonly bool Pr57StageCoherenceEnabled =
        Environment.GetEnvironmentVariable("PR57_225_STAGE_COHERENCE") == "1";

    Vehicle? _pr57StageCoherenceActor;
    Vehicle[]? _pr57StageCoherenceFleet;
    Pr57ExecProjection[]? _pr57StageCoherenceValues;
    int _pr57StageCoherencePrevious;
    readonly long[] _pr57StageCoherenceTransitions = new long[8];
    readonly long[] _pr57StageCoherenceStable = new long[8];
    readonly long[] _pr57StageCoherenceSelf = new long[8];
    readonly long[] _pr57StageCoherenceOther = new long[8];
    long _pr57StageCoherenceBuilds, _pr57StageCoherenceComparisons;
    long _pr57StageCoherencePosition, _pr57StageCoherenceHeading;
    long _pr57StageCoherenceStatus, _pr57StageCoherenceRoute;
    long _pr57StageCoherenceClaim, _pr57StageCoherenceOtherFields;
    long _pr57StageCoherencePopulation, _pr57StageCoherenceOrder;
    long _pr57StageCoherenceBuildTicks, _pr57StageCoherenceAuditTicks;
    long _pr57StageCoherenceBuildBytes, _pr57StageCoherenceAuditBytes;

    void Pr57StageCoherenceBeginSlice()
    {
        if (!Pr57StageCoherenceEnabled) return;
        Array.Clear(_pr57StageCoherenceTransitions);
        Array.Clear(_pr57StageCoherenceStable);
        Array.Clear(_pr57StageCoherenceSelf);
        Array.Clear(_pr57StageCoherenceOther);
        _pr57StageCoherenceBuilds = _pr57StageCoherenceComparisons = 0;
        _pr57StageCoherencePosition = _pr57StageCoherenceHeading = 0;
        _pr57StageCoherenceStatus = _pr57StageCoherenceRoute = 0;
        _pr57StageCoherenceClaim = _pr57StageCoherenceOtherFields = 0;
        _pr57StageCoherencePopulation = _pr57StageCoherenceOrder = 0;
        _pr57StageCoherenceBuildTicks = _pr57StageCoherenceAuditTicks = 0;
        _pr57StageCoherenceBuildBytes = _pr57StageCoherenceAuditBytes = 0;
        _pr57StageCoherenceActor = null;
        _pr57StageCoherenceFleet = null;
        _pr57StageCoherenceValues = null;
        _pr57StageCoherencePrevious = 0;
    }

    void Pr57StageCoherenceBeginVehicle(Vehicle actor)
    {
        if (!Pr57StageCoherenceEnabled) return;
        _pr57StageCoherenceActor = actor;
        _pr57StageCoherenceFleet = null;
        _pr57StageCoherenceValues = null;
        _pr57StageCoherencePrevious = 0;
    }

    void Pr57StageCoherenceMark(int nextStage)
    {
        if (!Pr57StageCoherenceEnabled || _pr57StageCoherenceActor is null) return;
        if (nextStage == 3)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            _pr57StageCoherenceFleet = State.Vehicles.ToArray();
            _pr57StageCoherenceValues = new Pr57ExecProjection[_pr57StageCoherenceFleet.Length];
            for (var i = 0; i < _pr57StageCoherenceFleet.Length; i++)
                _pr57StageCoherenceValues[i] = Pr57ExecSnapshot(_pr57StageCoherenceFleet[i]);
            _pr57StageCoherenceBuilds++;
            _pr57StageCoherenceBuildTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
            _pr57StageCoherenceBuildBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
            _pr57StageCoherencePrevious = 3;
            return;
        }

        if (nextStage >= 4 && nextStage <= 7 &&
            _pr57StageCoherencePrevious == nextStage - 1 &&
            _pr57StageCoherenceFleet is { } fleet &&
            _pr57StageCoherenceValues is { } values)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            var from = nextStage - 1;
            _pr57StageCoherenceTransitions[from]++;
            var changed = false;
            if (State.Vehicles.Count != fleet.Length)
            {
                _pr57StageCoherencePopulation++;
                changed = true;
            }
            for (var i = 0; i < fleet.Length; i++)
            {
                if (i < State.Vehicles.Count &&
                    !ReferenceEquals(fleet[i], State.Vehicles[i]))
                {
                    _pr57StageCoherenceOrder++;
                    changed = true;
                }
                var now = Pr57ExecSnapshot(fleet[i]);
                var old = values[i];
                _pr57StageCoherenceComparisons++;
                if (old != now)
                {
                    changed = true;
                    if (fleet[i].Id == _pr57StageCoherenceActor.Id)
                        _pr57StageCoherenceSelf[from]++;
                    else _pr57StageCoherenceOther[from]++;
                    if (old.Position != now.Position) _pr57StageCoherencePosition++;
                    if (old.HeadingBits != now.HeadingBits) _pr57StageCoherenceHeading++;
                    if (old.Status != now.Status) _pr57StageCoherenceStatus++;
                    if (old.RouteIndex != now.RouteIndex ||
                        old.RouteCount != now.RouteCount ||
                        old.SitePathIndex != now.SitePathIndex ||
                        old.SitePathCount != now.SitePathCount) _pr57StageCoherenceRoute++;
                    if (old.Claim != now.Claim || old.SpaceCount != now.SpaceCount ||
                        old.CorridorCount != now.CorridorCount) _pr57StageCoherenceClaim++;
                    if (old.Position == now.Position && old.HeadingBits == now.HeadingBits &&
                        old.Status == now.Status && old.RouteIndex == now.RouteIndex &&
                        old.RouteCount == now.RouteCount && old.SitePathIndex == now.SitePathIndex &&
                        old.SitePathCount == now.SitePathCount && old.Claim == now.Claim &&
                        old.SpaceCount == now.SpaceCount && old.CorridorCount == now.CorridorCount)
                        _pr57StageCoherenceOtherFields++;
                }
                values[i] = now;
            }
            if (!changed) _pr57StageCoherenceStable[from]++;
            _pr57StageCoherenceAuditTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
            _pr57StageCoherenceAuditBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
            _pr57StageCoherencePrevious = nextStage;
            return;
        }

        // Early exit, a new route iteration, or an unobserved stage breaks the
        // comparison chain; never report it as a stable transition.
        _pr57StageCoherencePrevious = 0;
        _pr57StageCoherenceFleet = null;
        _pr57StageCoherenceValues = null;
    }

    void Pr57StageCoherenceFinishVehicle()
    {
        if (!Pr57StageCoherenceEnabled) return;
        _pr57StageCoherenceActor = null;
        _pr57StageCoherenceFleet = null;
        _pr57StageCoherenceValues = null;
        _pr57StageCoherencePrevious = 0;
    }

    void Pr57StageCoherenceEndSlice()
    {
        if (!Pr57StageCoherenceEnabled) return;
        static double Ms(long ticks) =>
            Math.Round(ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency, 6);
        Console.WriteLine(
            $"PR57_225_STAGE_COHERENCE sample={_pr57ExclusiveSamples} " +
            $"builds={_pr57StageCoherenceBuilds} compared={_pr57StageCoherenceComparisons} " +
            string.Join(" ", Enumerable.Range(3, 4).Select(i =>
                $"s{i}to{i + 1}={_pr57StageCoherenceTransitions[i]} " +
                $"stable{i}={_pr57StageCoherenceStable[i]} " +
                $"self{i}={_pr57StageCoherenceSelf[i]} " +
                $"nonlocal{i}={_pr57StageCoherenceOther[i]}")) + " " +
            $"position={_pr57StageCoherencePosition} heading={_pr57StageCoherenceHeading} " +
            $"status={_pr57StageCoherenceStatus} route={_pr57StageCoherenceRoute} " +
            $"reservation={_pr57StageCoherenceClaim} otherFields={_pr57StageCoherenceOtherFields} " +
            $"populationChanges={_pr57StageCoherencePopulation} orderChanges={_pr57StageCoherenceOrder} " +
            $"buildMs={Ms(_pr57StageCoherenceBuildTicks)} auditMs={Ms(_pr57StageCoherenceAuditTicks)} " +
            $"buildMB={_pr57StageCoherenceBuildBytes / 1e6:F6} " +
            $"auditMB={_pr57StageCoherenceAuditBytes / 1e6:F6} " +
            "scope=selectedProjectionOnly transientWritesInvisible=True " +
            "externalDependenciesUnclosed=True noRuntimeReuse=True " +
            "observational=True intrusive=True");
    }
}
