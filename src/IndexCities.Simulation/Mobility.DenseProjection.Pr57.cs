namespace IndexCities.Simulation;

// PR57 #222: bounded observational dense mirror of the selected #218 physical
// projection. Updating the current actor is O(1); a full O(N) audit after
// EVERY actor detects non-local NET writes and measures their verification
// cost. The audit repairs ONLY this mirror. This is not a causal closure:
// list contents, roads, signals, pedestrians, clock, transient writes and
// other systems are omitted. Never used by movement, scheduling or saves.
public sealed partial class CitySimulation
{
    static readonly bool Pr57DenseEnabled =
        Environment.GetEnvironmentVariable("PR57_222_DENSE_PROJECTION") == "1";
    Vehicle[]? _pr57DenseVehicles;
    Pr57ExecProjection[]? _pr57DenseProjection;
    readonly Dictionary<int, int> _pr57DenseIndices = new();
    readonly HashSet<int> _pr57DenseExternalIds = new();
    bool _pr57DenseActive;
    long _pr57DenseBuildTicks, _pr57DenseBuildBytes;
    long _pr57DenseSelfTicks, _pr57DenseSelfBytes, _pr57DenseSelfChanges;
    long _pr57DenseAuditTicks, _pr57DenseAuditBytes, _pr57DenseAudits;
    long _pr57DenseCompared, _pr57DenseExternalChanges, _pr57DenseSyncChanges;
    long _pr57DenseSelfMisses, _pr57DenseUntracked, _pr57DenseCountChanges;
    long _pr57DenseProcessed, _pr57DensePostRepairMismatches;

    // The first ordered actor starts after the normal cleanup of reservations.
    void Pr57DenseBeginVehicle()
    {
        if (!Pr57DenseEnabled || !_pr57ExclusiveActive || _pr57DenseActive) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57DenseActive = true;
        _pr57DenseVehicles = State.Vehicles.ToArray();
        _pr57DenseProjection = new Pr57ExecProjection[_pr57DenseVehicles.Length];
        _pr57DenseIndices.Clear();
        _pr57DenseExternalIds.Clear();
        _pr57DenseSelfTicks = _pr57DenseSelfBytes = _pr57DenseSelfChanges = 0;
        _pr57DenseAuditTicks = _pr57DenseAuditBytes = _pr57DenseAudits = 0;
        _pr57DenseCompared = _pr57DenseExternalChanges = _pr57DenseSyncChanges = 0;
        _pr57DenseSelfMisses = _pr57DenseUntracked = _pr57DenseCountChanges = 0;
        _pr57DenseProcessed = _pr57DensePostRepairMismatches = 0;
        for (var i = 0; i < _pr57DenseVehicles.Length; i++)
        {
            var v = _pr57DenseVehicles[i];
            _pr57DenseIndices[v.Id] = i;
            _pr57DenseProjection[i] = Pr57ExecSnapshot(v);
        }
        _pr57DenseBuildTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57DenseBuildBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57DenseFinishVehicle(Vehicle actor)
    {
        if (!_pr57DenseActive || _pr57DenseProjection == null) return;
        _pr57DenseProcessed++;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        if (_pr57DenseIndices.TryGetValue(actor.Id, out var index))
        {
            var now = Pr57ExecSnapshot(actor);
            if (_pr57DenseProjection[index] != now) _pr57DenseSelfChanges++;
            _pr57DenseProjection[index] = now;
        }
        else _pr57DenseUntracked++;
        _pr57DenseSelfTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57DenseSelfBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
        Pr57DenseAudit(false, actor.Id);
    }

    void Pr57DenseAudit(bool afterSync, int actorId)
    {
        if (!_pr57DenseActive || _pr57DenseProjection == null) return;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57DenseAudits++;
        if (State.Vehicles.Count != _pr57DenseProjection.Length) _pr57DenseCountChanges++;
        foreach (var v in State.Vehicles)
        {
            if (!_pr57DenseIndices.TryGetValue(v.Id, out var index))
            {
                _pr57DenseUntracked++;
                continue;
            }
            _pr57DenseCompared++;
            var canonical = Pr57ExecSnapshot(v);
            if (_pr57DenseProjection[index] == canonical) continue;
            _pr57DenseProjection[index] = canonical; // shadow only
            if (afterSync) _pr57DenseSyncChanges++;
            else if (v.Id == actorId) _pr57DenseSelfMisses++;
            else
            {
                _pr57DenseExternalChanges++;
                _pr57DenseExternalIds.Add(v.Id);
            }
        }
        _pr57DenseAuditTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57DenseAuditBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57DenseEndSlice()
    {
        if (!Pr57DenseEnabled || !_pr57ExclusiveActive) return;
        if (!_pr57DenseActive)
        {
            Console.WriteLine($"PR57_222_DENSE sample={_pr57ExclusiveSamples} active=False vehicles=0");
            return;
        }
        // Called after SyncTowedVehicles, including non-local tow mutations.
        Pr57DenseAudit(true, -1);
        foreach (var v in State.Vehicles)
            if (_pr57DenseIndices.TryGetValue(v.Id, out var index) &&
                _pr57DenseProjection![index] != Pr57ExecSnapshot(v))
                _pr57DensePostRepairMismatches++;
        static double Ms(long ticks) =>
            Math.Round(ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency, 6);
        Console.WriteLine(
            $"PR57_222_DENSE sample={_pr57ExclusiveSamples} active=True " +
            $"vehicles={_pr57DenseVehicles!.Length} processed={_pr57DenseProcessed} " +
            $"buildMs={Ms(_pr57DenseBuildTicks)} buildMB={_pr57DenseBuildBytes / 1e6:F6} " +
            $"selfMs={Ms(_pr57DenseSelfTicks)} selfMB={_pr57DenseSelfBytes / 1e6:F6} " +
            $"selfChanged={_pr57DenseSelfChanges} " +
            $"auditMs={Ms(_pr57DenseAuditTicks)} auditMB={_pr57DenseAuditBytes / 1e6:F6} " +
            $"audits={_pr57DenseAudits} compared={_pr57DenseCompared} " +
            $"nonLocalChanges={_pr57DenseExternalChanges} nonLocalIds={_pr57DenseExternalIds.Count} " +
            $"syncChanges={_pr57DenseSyncChanges} ownMisses={_pr57DenseSelfMisses} " +
            $"untracked={_pr57DenseUntracked} populationDifferences={_pr57DenseCountChanges} " +
            $"postRepairMismatches={_pr57DensePostRepairMismatches} " +
            "scope=selectedVehicleProjection afterCanonicalCleanup=True " +
            "fullAuditEveryVehicle=True O_N2_Audit=True " +
            "transientWritesAndListContentsOmitted=True observational=True intrusive=True");
        _pr57DenseActive = false;
        _pr57DenseVehicles = null;
        _pr57DenseProjection = null;
        _pr57DenseIndices.Clear();
        _pr57DenseExternalIds.Clear();
    }
}
