namespace IndexCities.Simulation;

// PR57 #193: one bounded authentic GRID window. Actual selected blockers,
// materialized preference candidates and reached signal readers are different
// classes of evidence; NONE constitutes a complete causal read/write graph.
// Disabled by default; observations never feed physics or persistence.
public sealed partial class CitySimulation
{
    static readonly bool Pr57JunctionReadersEnabled =
        Environment.GetEnvironmentVariable("PR57_193_JUNCTION_READERS") == "1";
    bool _pr57JrActive;
    double _pr57JrTime, _pr57JrDuration;
    Dictionary<int,(Pr57DepState State,string? Cause)>? _pr57JrBefore;
    readonly HashSet<(int Reader,int Other,string Kind)> _pr57JrBlockers = new();
    // #194: first/last ACTUAL selection reads, not a journal of every write.
    // This distinguishes physical changes from Cause-only changes and
    // changes already visible at the read from net changes after the last read.
    readonly Dictionary<(int Reader,int Other,string Kind),
        (Pr57DepState First,string? FirstCause,Pr57DepState Last,string? LastCause,int Reads)>
        _pr57JrSelectedAtRead = new();
    readonly HashSet<(int Reader,int Other)> _pr57JrPreference = new();
    readonly HashSet<(int Reader,int Junction)> _pr57JrSignals = new();
    long _pr57JrAdmissions, _pr57JrRedChecks;

    void Pr57JunctionReadersBeginSlice(double dt,double? operationsDt)
    {
        if (!Pr57JunctionReadersEnabled || CostProfile is null ||
            _vehicleMovementBatch is null || operationsDt is not null ||
            dt <= 0 || VehicleMovementTime < 3610 || VehicleMovementTime > 3613)
            return;
        _pr57JrActive = true;
        _pr57JrTime = VehicleMovementTime;
        _pr57JrDuration = dt;
        _pr57JrBefore = Pr57CausalWorld();
        _pr57JrAdmissions = 0;
        _pr57JrRedChecks = 0;
        _pr57JrBlockers.Clear();
        _pr57JrSelectedAtRead.Clear();
        _pr57JrPreference.Clear();
        _pr57JrSignals.Clear();
    }

    void Pr57JunctionReadersAdmission(Vehicle reader,RoadNode junction)
    {
        if (_pr57JrActive) _pr57JrAdmissions++;
    }

    // These edges identify the ACTUAL object selected by an admission branch.
    // They omit unselected candidates and all other physical readers.
    void Pr57JunctionReadersBlock(Vehicle reader,Vehicle other,string kind)
    {
        if (!_pr57JrActive) return;
        var key = (reader.Id,other.Id,kind);
        _pr57JrBlockers.Add(key);
        var physical = Pr57DepSnapshot(other);
        if (_pr57JrSelectedAtRead.TryGetValue(key,out var prior))
            _pr57JrSelectedAtRead[key] =
                (prior.First,prior.FirstCause,physical,other.Cause,prior.Reads+1);
        else _pr57JrSelectedAtRead[key] =
            (physical,other.Cause,physical,other.Cause,1);
    }

    // The competitors list is materialized by the real preference algorithm.
    // Membership is a read candidate, not proof that each candidate blocked.
    void Pr57JunctionReadersPreferenceCandidates(Vehicle reader,List<Vehicle> others)
    {
        if (!_pr57JrActive) return;
        foreach (var other in others) _pr57JrPreference.Add((reader.Id,other.Id));
    }

    // Invoked at the real signal predicate location, not inferred from presence.
    void Pr57JunctionReadersSignal(Vehicle reader,RoadNode junction)
    {
        if (!_pr57JrActive || junction.Control != JunctionControl.Signal) return;
        _pr57JrSignals.Add((reader.Id,junction.Id));
        var horizontal = Math.Abs(Math.Sin(reader.Heading)) > .7;
        if ((VehicleMovementTime % 60 < 30) != horizontal) _pr57JrRedChecks++;
    }

    void Pr57JunctionReadersEndSlice()
    {
        if (!_pr57JrActive || _pr57JrBefore is null) return;
        var after = Pr57CausalWorld();
        var changed = after.Where(kv =>
            !_pr57JrBefore.TryGetValue(kv.Key,out var old) || old != kv.Value)
            .Select(kv => kv.Key).ToHashSet();
        foreach (var id in _pr57JrBefore.Keys)
            if (!after.ContainsKey(id)) changed.Add(id);
        var changedBlockers = _pr57JrBlockers.Count(e => changed.Contains(e.Other));
        long netPhysical=0,netCauseOnly=0,netUnchanged=0,missing=0;
        long physicalBeforeFirst=0,physicalAfterLast=0,physicalBetweenReads=0;
        long causeBeforeFirst=0,causeAfterLast=0,repeatedEdges=0,totalSelectedReads=0;
        foreach (var (edge,seen) in _pr57JrSelectedAtRead)
        {
            totalSelectedReads += seen.Reads;
            if (seen.Reads>1) repeatedEdges++;
            if (!_pr57JrBefore.TryGetValue(edge.Other,out var start) ||
                !after.TryGetValue(edge.Other,out var end))
            {
                missing++;
                continue;
            }
            if (start.State!=end.State) netPhysical++;
            else if (start.Cause!=end.Cause) netCauseOnly++;
            else netUnchanged++;
            if (start.State!=seen.First) physicalBeforeFirst++;
            if (seen.Last!=end.State) physicalAfterLast++;
            if (seen.First!=seen.Last) physicalBetweenReads++;
            if (start.Cause!=seen.FirstCause) causeBeforeFirst++;
            if (seen.LastCause!=end.Cause) causeAfterLast++;
        }
        var changedPreference = _pr57JrPreference.Count(e => changed.Contains(e.Other));
        var affectedReaders = _pr57JrBlockers.Where(e => changed.Contains(e.Other))
            .Select(e => e.Reader).Distinct().Count();
        Console.WriteLine(
            $"PR57_193_JUNCTION_READERS time={_pr57JrTime:F9} dt={_pr57JrDuration:F9} " +
            $"admissions={_pr57JrAdmissions} changedVehicles={changed.Count} " +
            $"selectedBlockerEdges={_pr57JrBlockers.Count} changedBlockerEdges={changedBlockers} " +
            $"affectedReadersLowerBound={affectedReaders} " +
            $"netPhysicalEdges={netPhysical} netCauseOnlyEdges={netCauseOnly} " +
            $"netUnchangedEdges={netUnchanged} missingEndpointEdges={missing} " +
            $"physicalBeforeFirstRead={physicalBeforeFirst} physicalAfterLastRead={physicalAfterLast} " +
            $"physicalBetweenSelectedReads={physicalBetweenReads} " +
            $"causeBeforeFirstRead={causeBeforeFirst} causeAfterLastRead={causeAfterLast} " +
            $"repeatedSelectedEdges={repeatedEdges} totalSelectedReads={totalSelectedReads} " +
            $"preferenceCandidateEdges={_pr57JrPreference.Count} changedPreferenceEdges={changedPreference} " +
            $"signalReaderJunctionPairs={_pr57JrSignals.Count} redSignalChecks={_pr57JrRedChecks} " +
            "scope=BOUNDED_FIRST_LAST_SELECTED_READS_NET_CHANGES_NOT_WRITE_JOURNAL_NOT_EXACT_INVALIDATION");
        _pr57JrActive = false;
        _pr57JrBefore = null;
    }
}
