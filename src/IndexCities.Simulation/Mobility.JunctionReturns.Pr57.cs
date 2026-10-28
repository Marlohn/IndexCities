namespace IndexCities.Simulation;

// PR57 #246: observe the ACTUAL return sites of MayEnterJunction, not an
// independently re-evaluated predicate. This does not change admission, actor
// ordering, clock, reservations, citizens or persistence. Disabled by default.
public sealed partial class CitySimulation
{
    // #303: targeted actual-return trace for the four #302 Cause-changing actors.
    // Observational only. No decision reuse, scheduling change or skipped slice.
    static readonly bool Pr57303ExitEnabled =
        Environment.GetEnvironmentVariable("PR57_303_JUNCTION_EXITS") == "1";
    enum Pr57JunctionExit
    {
        ExistingManoeuvre, ManoeuvreHolder, AreaOwner, ExistingClaim,
        BendHolder, BendPriority, BendSpace, BendBody, BendAllowed,
        Occupant, Pedestrian, NonIntersection, Signal, Continuing,
        Preference, OnwardBody, OnwardSpace, Allowed
    }

    static readonly bool Pr57JunctionReasonsEnabled =
        Environment.GetEnvironmentVariable("PR57_246_JUNCTION_RETURNS") == "1";
    readonly long[,] _pr57JunctionReasonCounts = new long[2, 18];
    readonly long[] _pr57JunctionPreferenceDenials = new long[5];
    readonly Dictionary<(int Reader, int Junction), (Pr57JunctionExit Reason, bool Allowed)>
        _pr57JunctionPrevious = new();
    readonly long[] _pr57JunctionSameReason = new long[2];
    readonly long[] _pr57JunctionChangedReason = new long[2];
    readonly long[] _pr57JunctionChangedDecision = new long[2];
    readonly long[] _pr57JunctionFirstPair = new long[2];
    long _pr57JunctionObservedCalls, _pr57JunctionObservedDenied;

    bool Pr57JunctionDecision(Vehicle reader, RoadNode junction, Pr57JunctionExit reason, bool allowed)
    {
        if (reason == Pr57JunctionExit.Pedestrian) Pr57304Walk(reader,junction);
        Pr57Cause255Exit(reader,junction,reason,allowed);
        Pr57260Decision(reader,junction,reason,allowed);
        Pr57261Junction(reason,allowed);
        if (Pr57303ExitEnabled && _pr57300Calls is >= 76 and <= 100 &&
            reader.Id is 1243 or 1282 or 1071 or 1380 &&
            junction.Id is 346 or 466)
            Console.WriteLine($"PR57_303_RETURN call={_pr57300Calls} slice={_pr57302CurrentSlice} id={reader.Id} node={junction.Id} reason={reason} allowed={(allowed?1:0)} control={junction.Control} clock={VehicleMovementTime:F9} cause={Uri.EscapeDataString(reader.Cause??"")} scope=ACTUAL_RETURN_NOT_CAUSAL_CLOSURE");
        if (!Pr57JunctionReasonsEnabled || CostProfile is null) return allowed;
        Pr57Junction247Decision(reader,junction,reason);
        var cohort = Pr57State244PersistentIds.Contains(reader.Id) ? 0 : 1;
        _pr57JunctionReasonCounts[cohort, (int)reason]++;
        _pr57JunctionObservedCalls++;
        if (!allowed) _pr57JunctionObservedDenied++;
        var key = (reader.Id, junction.Id);
        if (_pr57JunctionPrevious.TryGetValue(key, out var previous))
        {
            if (previous.Reason == reason) _pr57JunctionSameReason[cohort]++;
            else _pr57JunctionChangedReason[cohort]++;
            if (previous.Allowed != allowed) _pr57JunctionChangedDecision[cohort]++;
        }
        else _pr57JunctionFirstPair[cohort]++;
        _pr57JunctionPrevious[key] = (reason, allowed);
        return allowed;
    }

    bool Pr57JunctionPreferenceDenied(int reason)
    {
        if (Pr57JunctionReasonsEnabled && CostProfile is not null)
            _pr57JunctionPreferenceDenials[reason]++;
        return false;
    }

    void Pr57JunctionReasonReport(long slices)
    {
        if (!Pr57JunctionReasonsEnabled) return;
        Console.WriteLine(
            $"PR57_246_RETURNS slices={slices} calls={_pr57JunctionObservedCalls} " +
            $"denied={_pr57JunctionObservedDenied} allowed={_pr57JunctionObservedCalls - _pr57JunctionObservedDenied} " +
            $"distinctReaderJunctionPairs={_pr57JunctionPrevious.Count} " +
            "scope=actual-return-sites observational=True noReuse=True");
        for (var cohort = 0; cohort < 2; cohort++)
        {
            var name = cohort == 0 ? "real-24-persistent" : "other-vehicles";
            Console.WriteLine(
                $"PR57_246_COHORT slices={slices} cohort={name} " +
                $"firstPairs={_pr57JunctionFirstPair[cohort]} sameReason={_pr57JunctionSameReason[cohort]} " +
                $"changedReason={_pr57JunctionChangedReason[cohort]} changedDecision={_pr57JunctionChangedDecision[cohort]} " +
                string.Join(" ", Enum.GetValues<Pr57JunctionExit>().Select(reason =>
                    $"{reason}={_pr57JunctionReasonCounts[cohort, (int)reason]}")));
        }
        Console.WriteLine(
            $"PR57_246_PREFERENCE slices={slices} signal={_pr57JunctionPreferenceDenials[0]} " +
            $"throughX={_pr57JunctionPreferenceDenials[1]} throughZ={_pr57JunctionPreferenceDenials[2]} " +
            $"rightHandYield={_pr57JunctionPreferenceDenials[3]} rightHandTie={_pr57JunctionPreferenceDenials[4]} " +
            "scope=actual-false-returns-of-JunctionPreferenceAllows");
        Pr57Junction247Report(slices);
    }
}
