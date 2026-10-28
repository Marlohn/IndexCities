namespace IndexCities.Simulation;

// PR57 #262: read-only attribution of CanTurnNow reached transitively while
// the canonical reader 1071 evaluates its real 14h junction. No new predicate
// is evaluated, no result is substituted, no cross-step cache is retained.
public sealed partial class CitySimulation
{
    static readonly bool Pr57262Enabled =
        Environment.GetEnvironmentVariable("PR57_262_NESTED_ATTRIBUTION") == "1";

    readonly Dictionary<int, (long Calls, long Allowed)> _pr57262ByCandidate = new();
    long _pr57262CanTurnCalls, _pr57262CanTurnAllowed;

    bool Pr57262CanTurnNowResult(Vehicle candidate, bool allowed)
    {
        if (Pr57262Enabled && _pr57261InReader)
        {
            _pr57262CanTurnCalls++;
            if (allowed) _pr57262CanTurnAllowed++;
            _pr57262ByCandidate.TryGetValue(candidate.Id, out var counts);
            _pr57262ByCandidate[candidate.Id] =
                (counts.Calls + 1, counts.Allowed + (allowed ? 1 : 0));
        }
        return allowed;
    }

    void Pr57262Report()
    {
        if (!Pr57262Enabled) return;
        var onward = _pr57261JunctionReasons[(int)Pr57JunctionExit.OnwardBody];
        var bodyRemainder = _pr57261NestedCalls[0] - onward;
        var turnRemainder = _pr57261NestedCalls[2] - onward;
        Console.WriteLine(
            $"PR57_262_NESTED reader=1071 slices={_pr57261Slices} " +
            $"onwardBodyReturns={onward} " +
            $"turnWayTotal={_pr57261NestedCalls[2]} bodyOnWayTotal={_pr57261NestedCalls[0]} " +
            $"turnWayBeyondDirect={turnRemainder} bodyOnWayBeyondDirect={bodyRemainder} " +
            $"canTurnNowCalls={_pr57262CanTurnCalls} canTurnNowAllowed={_pr57262CanTurnAllowed} " +
            $"canTurnNowDenied={_pr57262CanTurnCalls - _pr57262CanTurnAllowed} " +
            $"distinctCandidates={_pr57262ByCandidate.Count} " +
            $"allRemaindersExplainedByCanTurn={turnRemainder == _pr57262CanTurnCalls && bodyRemainder == _pr57262CanTurnCalls} " +
            "scope=REAL_14H_ONE_READER canonicalNestedReturns=True " +
            "noReadWriteClosure=True noReuse=True observational=True");
        Console.WriteLine("PR57_262_CANDIDATES " +
            string.Join(" ", _pr57262ByCandidate.OrderByDescending(kv => kv.Value.Calls)
                .ThenBy(kv => kv.Key).Select(kv =>
                    $"{kv.Key}:{kv.Value.Calls}:{kv.Value.Allowed}")) +
            " format=id:calls:allowed");
    }
}
