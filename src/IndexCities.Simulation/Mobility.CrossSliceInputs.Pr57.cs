namespace IndexCities.Simulation;

// PR57 #213: bounded, strictly observational census of repeated input states
// across three consecutive physical sub-slices. It is NOT a dependency closure:
// other vehicles, pedestrians, signals, roads, time, transient writes and
// external mutations may change decisions even when the actor is identical.
// The captured snapshots never participate in simulation decisions.
public sealed partial class CitySimulation
{
    static readonly bool Pr57CrossSliceInputsEnabled =
        Environment.GetEnvironmentVariable("PR57_213_CROSS_SLICE_INPUTS") == "1";

    readonly record struct Pr57CrossSliceQuery(
        string PersistedVehicle, Pr57LaneReadState PhysicalProjection,
        bool Entering, long ReachBits);

    readonly Dictionary<(int Id, int Kind), Pr57CrossSliceQuery> _pr57CrossPrior = new();
    readonly Dictionary<(int Id, int Kind), Pr57CrossSliceQuery> _pr57CrossCurrent = new();
    Dictionary<int, string>? _pr57CrossPriorWorld;
    Dictionary<int, string>? _pr57CrossStartWorld;
    bool _pr57CrossActive;
    long _pr57CrossQueries, _pr57CrossLaneQueries, _pr57CrossBendQueries;
    long _pr57CrossUnique, _pr57CrossWithinRepeat, _pr57CrossRevisited;
    long _pr57CrossFullSame, _pr57CrossPhysicalSame;
    long _pr57CrossFullQuerySame, _pr57CrossPhysicalQuerySame;
    long _pr57CrossPriorStartChanges, _pr57CrossWithinSliceChanges;

    static int Pr57CrossChanged(Dictionary<int, string> before, Dictionary<int, string> after)
    {
        var changed = 0;
        foreach (var (id, value) in after)
            if (!before.TryGetValue(id, out var old) ||
                !string.Equals(old, value, StringComparison.Ordinal)) changed++;
        foreach (var id in before.Keys)
            if (!after.ContainsKey(id)) changed++;
        return changed;
    }

    void Pr57CrossSliceBegin()
    {
        _pr57CrossActive = Pr57CrossSliceInputsEnabled && _pr57ExclusiveActive;
        if (!_pr57CrossActive) return;
        if (_pr57ExclusiveSamples == 1)
        {
            _pr57CrossPrior.Clear();
            _pr57CrossPriorWorld = null;
        }
        _pr57CrossCurrent.Clear();
        _pr57CrossQueries = _pr57CrossLaneQueries = _pr57CrossBendQueries = 0;
        _pr57CrossUnique = _pr57CrossWithinRepeat = _pr57CrossRevisited = 0;
        _pr57CrossFullSame = _pr57CrossPhysicalSame = 0;
        _pr57CrossFullQuerySame = _pr57CrossPhysicalQuerySame = 0;
        _pr57CrossStartWorld = State.Vehicles.ToDictionary(v => v.Id, Pr57MutationSnapshot);
        _pr57CrossPriorStartChanges = _pr57CrossPriorWorld is null ? 0 :
            Pr57CrossChanged(_pr57CrossPriorWorld, _pr57CrossStartWorld);
    }

    // Called immediately before the existing longitudinal/bend scan.
    // The previous slice stores the LAST observed query per actor and kind;
    // the current slice compares its FIRST query to that previous last query.
    void Pr57CrossSliceObserve(Vehicle actor, int kind, bool entering, double reach)
    {
        if (!_pr57CrossActive) return;
        var key = (actor.Id, kind);
        var query = new Pr57CrossSliceQuery(
            Pr57MutationSnapshot(actor), Pr57LaneReadSnapshot(actor),
            entering, BitConverter.DoubleToInt64Bits(reach));
        _pr57CrossQueries++;
        if (kind == 0) _pr57CrossLaneQueries++;
        else _pr57CrossBendQueries++;
        if (_pr57CrossCurrent.ContainsKey(key))
            _pr57CrossWithinRepeat++;
        else
        {
            _pr57CrossUnique++;
            if (_pr57CrossPrior.TryGetValue(key, out var previous))
            {
                _pr57CrossRevisited++;
                var fullSame = string.Equals(
                    previous.PersistedVehicle, query.PersistedVehicle, StringComparison.Ordinal);
                var physicalSame = previous.PhysicalProjection == query.PhysicalProjection;
                var querySame = previous.Entering == query.Entering &&
                    previous.ReachBits == query.ReachBits;
                if (fullSame) _pr57CrossFullSame++;
                if (physicalSame) _pr57CrossPhysicalSame++;
                if (fullSame && querySame) _pr57CrossFullQuerySame++;
                if (physicalSame && querySame) _pr57CrossPhysicalQuerySame++;
            }
        }
        _pr57CrossCurrent[key] = query;
    }

    void Pr57CrossSliceEnd()
    {
        if (!_pr57CrossActive || _pr57CrossStartWorld is null) return;
        var endWorld = State.Vehicles.ToDictionary(v => v.Id, Pr57MutationSnapshot);
        _pr57CrossWithinSliceChanges = Pr57CrossChanged(_pr57CrossStartWorld, endWorld);
        Console.WriteLine(
            $"PR57_213_CROSS sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
            $"worldVehicles={_pr57CrossStartWorld.Count} worldChangedFromPriorStart={_pr57CrossPriorStartChanges} " +
            $"worldChangedWithinSlice={_pr57CrossWithinSliceChanges} queries={_pr57CrossQueries} " +
            $"laneQueries={_pr57CrossLaneQueries} bendQueries={_pr57CrossBendQueries} " +
            $"uniqueActorKind={_pr57CrossUnique} repeatWithinSlice={_pr57CrossWithinRepeat} " +
            $"revisitedFromPriorSlice={_pr57CrossRevisited} fullVehicleSame={_pr57CrossFullSame} " +
            $"physicalProjectionSame={_pr57CrossPhysicalSame} fullVehicleAndQuerySame={_pr57CrossFullQuerySame} " +
            $"physicalAndQuerySame={_pr57CrossPhysicalQuerySame} " +
            "observational=True intrusive=True exactDependencies=False reusableDecisionsProven=False");
        _pr57CrossPrior.Clear();
        foreach (var (key, query) in _pr57CrossCurrent) _pr57CrossPrior[key] = query;
        _pr57CrossPriorWorld = _pr57CrossStartWorld;
        _pr57CrossStartWorld = null;
        _pr57CrossCurrent.Clear();
        _pr57CrossActive = false;
    }
}
