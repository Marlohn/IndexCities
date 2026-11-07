namespace IndexCities.Simulation;

// PR57 #539: opt-in vertical cut of a Step-scoped, shared causal workset.
// No persisted cache: all buckets are emptied at each Step boundary, including
// every public Advance(2). Cargo, completion and ProjectId are read live.
// Only membership is indexed, in original State.Shipments insertion order.
// Direct ProjectId writers publish an epoch; append/shrink is detected by Count.
// Unknown in-place list edits / external concurrent mutations are NOT certified.
public sealed partial class CitySimulation
{
    static readonly bool Pr57SharedStepShipmentsEnabled =
        Environment.GetEnvironmentVariable("PR57_SHARED_STEP_SHIPMENTS") == "1";
    static readonly bool Pr57SharedStepShipmentsDiag =
        Environment.GetEnvironmentVariable("PR57_SHARED_STEP_DIAG") == "1";

    readonly Dictionary<int, List<Shipment>> _pr57SharedStepShipments = new();
    readonly List<List<Shipment>> _pr57SharedStepBuckets = new();
    int _pr57SharedStepUsedBuckets;
    int _pr57SharedStepIndexedCount = -1;
    long _pr57ShipmentProjectEpoch;
    long _pr57SharedStepIndexedEpoch = -1;
    long _pr57SharedStepSteps, _pr57SharedStepBuilds, _pr57SharedStepAppended,
        _pr57SharedStepQueries, _pr57SharedStepReassignments;

    void Pr57SharedStepClearMembership()
    {
        foreach (var bucket in _pr57SharedStepBuckets) bucket.Clear();
        _pr57SharedStepShipments.Clear();
        _pr57SharedStepUsedBuckets = 0;
    }

    void Pr57SharedShipmentsBeginStep()
    {
        if (!Pr57SharedStepShipmentsEnabled) return;
        // Empty pooled lists retain capacity, never Shipment references.
        Pr57SharedStepClearMembership();
        _pr57SharedStepIndexedCount = -1;
        _pr57SharedStepIndexedEpoch = -1;
        _pr57SharedStepSteps++;
    }

    void Pr57SharedShipmentProjectChanged()
    {
        if (!Pr57SharedStepShipmentsEnabled) return;
        _pr57ShipmentProjectEpoch++;
        _pr57SharedStepReassignments++;
    }

    IEnumerable<Shipment> Pr57SharedPendingProjectShipments(int projectId)
    {
        _pr57SharedStepQueries++;
        var count = State.Shipments.Count;
        if (_pr57SharedStepIndexedCount < 0
            || _pr57SharedStepIndexedEpoch != _pr57ShipmentProjectEpoch
            || _pr57SharedStepIndexedCount > count)
        {
            Pr57SharedStepClearMembership();
            _pr57SharedStepIndexedCount = 0;
            _pr57SharedStepIndexedEpoch = _pr57ShipmentProjectEpoch;
            _pr57SharedStepBuilds++;
        }
        // Append-only delta is safe after a complete initial scan. Reassignment
        // forces rebuild above, including inside ProgressConstruction.
        if (_pr57SharedStepIndexedCount < count)
        {
            for (var i = _pr57SharedStepIndexedCount; i < count; i++)
            {
                var shipment = State.Shipments[i];
                if (shipment.ProjectId is not int id) continue;
                if (!_pr57SharedStepShipments.TryGetValue(id, out var group))
                {
                    if (_pr57SharedStepUsedBuckets == _pr57SharedStepBuckets.Count)
                        _pr57SharedStepBuckets.Add(new List<Shipment>());
                    group = _pr57SharedStepBuckets[_pr57SharedStepUsedBuckets++];
                    _pr57SharedStepShipments.Add(id, group);
                }
                group.Add(shipment);
            }
            if (_pr57SharedStepIndexedCount != 0)
                _pr57SharedStepAppended += count - _pr57SharedStepIndexedCount;
            _pr57SharedStepIndexedCount = count;
        }
        return _pr57SharedStepShipments.TryGetValue(projectId, out var shipments)
            ? shipments.Where(s => s.ProjectId == projectId && !s.Complete)
            : Enumerable.Empty<Shipment>();
    }

    void Pr57SharedShipmentsEndStep()
    {
        if (!Pr57SharedStepShipmentsEnabled || !Pr57SharedStepShipmentsDiag
            || _pr57SharedStepSteps % 50 != 0) return;
        Console.WriteLine(
            $"PR57_SHARED_STEP_SUM steps={_pr57SharedStepSteps} builds={_pr57SharedStepBuilds} " +
            $"appends={_pr57SharedStepAppended} queries={_pr57SharedStepQueries} " +
            $"reassigns={_pr57SharedStepReassignments} buckets={_pr57SharedStepUsedBuckets}");
    }
}
