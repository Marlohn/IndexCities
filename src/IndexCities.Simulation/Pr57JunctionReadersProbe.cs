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
    Dictionary<int,(Pr57DepState State,string? Cause)>? _pr57JrAfterCleanup;
    long _pr57JrCandidateEvaluations;
    // #196: bounded stage-gate census: visits to short-circuit predicate tails,
    // NOT a complete physical read graph. The observer always returns true.
    readonly long[] _pr57JrScanVisits = new long[4]; // manoeuvre, holder, bend-priority, occupant
    readonly long[] _pr57JrGateVisits = new long[7]; // spaces, pose, holds, turn, pose, claim, pose
    readonly HashSet<(int Reader,int Other)> _pr57JrPosePairs = new();
    readonly HashSet<int> _pr57JrCandidateIds = new();
    readonly HashSet<(int Reader,int Other)> _pr57JrCandidatePairs = new();
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
        _pr57JrAfterCleanup = null;
        _pr57JrCandidateEvaluations = 0;
        Array.Clear(_pr57JrScanVisits);
        Array.Clear(_pr57JrGateVisits);
        _pr57JrPosePairs.Clear();
        _pr57JrCandidateIds.Clear();
        _pr57JrCandidatePairs.Clear();
        _pr57JrAdmissions = 0;
        _pr57JrRedChecks = 0;
        _pr57JrBlockers.Clear();
        _pr57JrSelectedAtRead.Clear();
        _pr57JrPreference.Clear();
        _pr57JrSignals.Clear();
        Pr57ReservationBegin();
    }

    // #195: snapshot after the real all-vehicle claim/manoeuvre cleanup.
    void Pr57JunctionReadersAfterCleanup()
    {
        if (_pr57JrActive) _pr57JrAfterCleanup = Pr57CausalWorld();
        Pr57ReservationAfterCleanup();
    }

    // Counts real predicate evaluations, including misses; always returns true.
    bool Pr57JunctionReadersCandidate(Vehicle reader,Vehicle other,int scan)
    {
        if (_pr57JrActive)
        {
            _pr57JrCandidateEvaluations++;
            _pr57JrScanVisits[scan]++;
            _pr57JrCandidateIds.Add(other.Id);
            _pr57JrCandidatePairs.Add((reader.Id,other.Id));
        }
        return true;
    }

    // Placed immediately before an existing field-dependent predicate tail.
    // It does not itself inspect the field, and does not count the reads in
    // earlier guards (tow links, status, route, Receives) or other algorithms.
    bool Pr57JunctionReadersGate(Vehicle reader,Vehicle other,int gate)
    {
        if (_pr57JrActive)
        {
            _pr57JrGateVisits[gate]++;
            if (gate is 1 or 4 or 6) _pr57JrPosePairs.Add((reader.Id,other.Id));
        }
        return true;
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
        long selectedChangedDuringCleanup=0,selectedChangedAfterCleanupBeforeFirst=0;
        long selectedUnchangedAfterCleanup=0,selectedMissingAfterCleanup=0;
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
            if (_pr57JrAfterCleanup is null || !_pr57JrAfterCleanup.TryGetValue(edge.Other,out var clean))
                selectedMissingAfterCleanup++;
            else
            {
                if (start.State!=clean.State) selectedChangedDuringCleanup++;
                if (clean.State!=seen.First) selectedChangedAfterCleanupBeforeFirst++;
                else selectedUnchangedAfterCleanup++;
            }
            if (seen.Last!=end.State) physicalAfterLast++;
            if (seen.First!=seen.Last) physicalBetweenReads++;
            if (start.Cause!=seen.FirstCause) causeBeforeFirst++;
            if (seen.LastCause!=end.Cause) causeAfterLast++;
        }
        var changedPreference = _pr57JrPreference.Count(e => changed.Contains(e.Other));
        var changedCandidateIds = _pr57JrCandidateIds.Count(id => changed.Contains(id));
        var changedPosePairs = _pr57JrPosePairs.Count(e => changed.Contains(e.Other));
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
            $"selectedChangedDuringCleanup={selectedChangedDuringCleanup} " +
            $"selectedChangedAfterCleanupBeforeFirst={selectedChangedAfterCleanupBeforeFirst} " +
            $"selectedUnchangedAfterCleanup={selectedUnchangedAfterCleanup} " +
            $"selectedMissingAfterCleanup={selectedMissingAfterCleanup} " +
            $"candidateEvaluations={_pr57JrCandidateEvaluations} " +
            $"scanManoeuvre={_pr57JrScanVisits[0]} scanHolder={_pr57JrScanVisits[1]} " +
            $"scanBendPriority={_pr57JrScanVisits[2]} scanOccupant={_pr57JrScanVisits[3]} " +
            $"gateManoeuvreSpaces={_pr57JrGateVisits[0]} gateManoeuvrePose={_pr57JrGateVisits[1]} " +
            $"gateHolderHolds={_pr57JrGateVisits[2]} gateBendTurn={_pr57JrGateVisits[3]} " +
            $"gateBendPose={_pr57JrGateVisits[4]} gateOccupantClaim={_pr57JrGateVisits[5]} " +
            $"gateOccupantPose={_pr57JrGateVisits[6]} poseReaderPairs={_pr57JrPosePairs.Count} " +
            $"changedPoseReaderPairs={changedPosePairs} " +
            $"candidateDistinctVehicles={_pr57JrCandidateIds.Count} " +
            $"candidateReaderPairs={_pr57JrCandidatePairs.Count} " +
            $"changedCandidateVehicles={changedCandidateIds} " +
            $"repeatedSelectedEdges={repeatedEdges} totalSelectedReads={totalSelectedReads} " +
            $"preferenceCandidateEdges={_pr57JrPreference.Count} changedPreferenceEdges={changedPreference} " +
            $"signalReaderJunctionPairs={_pr57JrSignals.Count} redSignalChecks={_pr57JrRedChecks} " +
            "scope=BOUNDED_FIRST_LAST_SELECTED_READS_NET_CHANGES_NOT_WRITE_JOURNAL_NOT_EXACT_INVALIDATION");
        Pr57ReservationEnd();
        _pr57JrActive = false;
        _pr57JrBefore = null;
        _pr57JrAfterCleanup = null;
    }

    // #197: observational, bounded to ONE real sub-slice. Snapshots after
    // cleanup, after each processed vehicle (including third-party mutations),
    // and after tow sync. These are NET writes between boundaries, NOT a
    // complete transient write journal or a cache-invalidation proof.
    static readonly bool Pr57ReservationWritersEnabled =
        Environment.GetEnvironmentVariable("PR57_197_RESERVATION_WRITERS") == "1";
    bool _pr57ReservationWritersActive, _pr57ReservationWritersDone;
    Dictionary<int,(int? Claim,string Spaces)>? _pr57ReservationPrevious;
    long _pr57ReservationTicks, _pr57ReservationSnapshots, _pr57ReservationChecks;
    long _pr57ReservationVehiclePasses, _pr57ReservationChanges, _pr57ReservationClaimChanges;
    long _pr57ReservationSpaceChanges, _pr57ReservationNonlocal, _pr57ReservationNewIds;
    long _pr57ReservationRemovedIds, _pr57ReservationCleanupChanges, _pr57ReservationSyncChanges;
    long _pr57ReservationWriterPassesWithChanges, _pr57ReservationMaxChangesPerPass;
    readonly HashSet<int> _pr57ReservationChangedIds = new();
    readonly HashSet<int> _pr57ReservationWriters = new();
    (int Claims,int Entries,int Unique) Pr57ReservationOccupancy()
    {
        var claims=0;var entries=0;var spaces=new HashSet<int>();
        foreach(var v in State.Vehicles)
        {
            if(v.IntersectionClaim is {} claim){claims++;spaces.Add(claim);}
            foreach(var id in v.ManoeuvreSpaces){entries++;spaces.Add(id);}
        }
        return (claims,entries,spaces.Count);
    }
    (int Claims,int Entries,int Unique) _pr57ReservationInitialOccupancy;
    static (int? Claim,string Spaces) Pr57ReservationState(Vehicle v)=>
        (v.IntersectionClaim,string.Join(",",v.ManoeuvreSpaces));
    Dictionary<int,(int? Claim,string Spaces)> Pr57ReservationSnapshot()=>
        State.Vehicles.ToDictionary(v=>v.Id,Pr57ReservationState);
    // #198: bounded event-hook POC. Explicit callsites only, never feeds decisions.
    // This is intentionally NOT universal coverage of public Vehicle mutations.
    static readonly bool Pr57ReservationHooksEnabled =
        Environment.GetEnvironmentVariable("PR57_198_RESERVATION_HOOKS") == "1";
    readonly Dictionary<int,(int? Claim,string Spaces)> _pr57ReservationHookLast = new();
    readonly HashSet<int> _pr57ReservationHookSinceCapture = new();
    long _pr57ReservationHookCalls, _pr57ReservationHookChangedWrites;
    long _pr57ReservationHookMissedBoundaryChanges, _pr57ReservationHookTicks;
    void Pr57ReservationWrite(Vehicle vehicle,string site)
    {
        if(!_pr57ReservationWritersActive||!Pr57ReservationHooksEnabled)return;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57ReservationHookCalls++;
        var state=Pr57ReservationState(vehicle);
        if(!_pr57ReservationHookLast.TryGetValue(vehicle.Id,out var prior)||prior!=state)
        {
            _pr57ReservationHookLast[vehicle.Id]=state;
            _pr57ReservationHookSinceCapture.Add(vehicle.Id);
            _pr57ReservationHookChangedWrites++;
        }
        _pr57ReservationHookTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
    }
    void Pr57ReservationBegin()
    {
        if(!_pr57JrActive||!Pr57ReservationWritersEnabled||_pr57ReservationWritersDone||
            _pr57JrTime<3611.05||_pr57JrTime>=3611.06)return;
        _pr57ReservationWritersActive=true;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57ReservationPrevious=Pr57ReservationSnapshot();
        if(Pr57ReservationHooksEnabled)
        {
            _pr57ReservationHookLast.Clear();
            foreach(var (id,state) in _pr57ReservationPrevious)
                _pr57ReservationHookLast.Add(id,state);
            _pr57ReservationHookSinceCapture.Clear();
        }
        _pr57ReservationTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        _pr57ReservationSnapshots=1;
        _pr57ReservationChecks=_pr57ReservationPrevious.Count;
        _pr57ReservationInitialOccupancy=Pr57ReservationOccupancy();
    }
    void Pr57ReservationCapture(int writer,string phase)
    {
        if(!_pr57ReservationWritersActive||_pr57ReservationPrevious is null)return;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var current=Pr57ReservationSnapshot();
        _pr57ReservationSnapshots++;
        _pr57ReservationChecks+=current.Count;
        var changes=0;
        foreach(var (id,now) in current)
        {
            if(!_pr57ReservationPrevious.TryGetValue(id,out var old))
            {
                _pr57ReservationNewIds++;changes++;_pr57ReservationChangedIds.Add(id);
                continue;
            }
            if(old==now)continue;
            changes++;_pr57ReservationChangedIds.Add(id);
            if(Pr57ReservationHooksEnabled&&!_pr57ReservationHookSinceCapture.Contains(id))
                _pr57ReservationHookMissedBoundaryChanges++;
            if(old.Claim!=now.Claim)_pr57ReservationClaimChanges++;
            if(old.Spaces!=now.Spaces)_pr57ReservationSpaceChanges++;
            if(writer>=0&&id!=writer)_pr57ReservationNonlocal++;
        }
        foreach(var id in _pr57ReservationPrevious.Keys)
            if(!current.ContainsKey(id))
            {
                _pr57ReservationRemovedIds++;changes++;_pr57ReservationChangedIds.Add(id);
            }
        if(changes>0)
        {
            if(writer>=0)_pr57ReservationWriters.Add(writer);
            if(phase=="cleanup")_pr57ReservationCleanupChanges+=changes;
            if(phase=="sync")_pr57ReservationSyncChanges+=changes;
            if(phase=="vehicle")_pr57ReservationWriterPassesWithChanges++;
            _pr57ReservationMaxChangesPerPass=Math.Max(_pr57ReservationMaxChangesPerPass,changes);
        }
        _pr57ReservationChanges+=changes;
        _pr57ReservationPrevious=current;
        _pr57ReservationHookSinceCapture.Clear();
        _pr57ReservationTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
    }
    void Pr57ReservationAfterCleanup()=>Pr57ReservationCapture(-1,"cleanup");
    void Pr57ReservationAfterVehicle(Vehicle v)
    {
        if(!_pr57ReservationWritersActive)return;
        _pr57ReservationVehiclePasses++;
        Pr57ReservationCapture(v.Id,"vehicle");
    }
    void Pr57ReservationAfterSync()=>Pr57ReservationCapture(-2,"sync");
    void Pr57ReservationEnd()
    {
        if(!_pr57ReservationWritersActive)return;
        var occupancy=Pr57ReservationOccupancy();
        var elapsedMs=1000.0*_pr57ReservationTicks/System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_197_RESERVATION_WRITERS time={_pr57JrTime:F9} dt={_pr57JrDuration:F9} " +
            $"vehiclePasses={_pr57ReservationVehiclePasses} snapshots={_pr57ReservationSnapshots} " +
            $"checks={_pr57ReservationChecks} netChanges={_pr57ReservationChanges} " +
            $"claimChanges={_pr57ReservationClaimChanges} spaceChanges={_pr57ReservationSpaceChanges} " +
            $"nonlocalChanges={_pr57ReservationNonlocal} newIds={_pr57ReservationNewIds} " +
            $"removedIds={_pr57ReservationRemovedIds} cleanupChanges={_pr57ReservationCleanupChanges} " +
            $"syncChanges={_pr57ReservationSyncChanges} writerPassesWithChanges={_pr57ReservationWriterPassesWithChanges} " +
            $"distinctWriters={_pr57ReservationWriters.Count} distinctChangedIds={_pr57ReservationChangedIds.Count} " +
            $"maxChangesPerPass={_pr57ReservationMaxChangesPerPass} " +
            $"initialClaims={_pr57ReservationInitialOccupancy.Claims} initialEntries={_pr57ReservationInitialOccupancy.Entries} " +
            $"initialUniqueSpaces={_pr57ReservationInitialOccupancy.Unique} " +
            $"finalClaims={occupancy.Claims} finalEntries={occupancy.Entries} finalUniqueSpaces={occupancy.Unique} " +
            $"candidateEvaluations={_pr57JrCandidateEvaluations} observerMs={elapsedMs:F3} " +
            $"hookCalls={_pr57ReservationHookCalls} hookChangedWrites={_pr57ReservationHookChangedWrites} " +
            $"hookMissedBoundaryChanges={_pr57ReservationHookMissedBoundaryChanges} " +
            $"hookMs={1000.0*_pr57ReservationHookTicks/System.Diagnostics.Stopwatch.Frequency:F3} " +
            "scope=ONE_SLICE_NET_BOUNDARY_WRITES_NOT_TRANSIENT_JOURNAL_NOT_AB");
        _pr57ReservationWritersActive=false;
        _pr57ReservationWritersDone=true;
        _pr57ReservationPrevious=null;
    }
}
