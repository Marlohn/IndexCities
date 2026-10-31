namespace IndexCities.Simulation;

// PR57 #263: bounded, read-only census of every reached CanTurnNow call in
// the authentic economic queue. Counts actual blocking bodies returned by the
// canonical predicate and inclusive timing of its real nested work. Never
// supplies a decision, mutates a vehicle, or persists a cache.
public sealed partial class CitySimulation
{
    static readonly bool Pr57263Enabled =
        Environment.GetEnvironmentVariable("PR57_263_ALL_CAN_TURN") == "1";

    readonly long[] _pr57263ReasonCalls = new long[4]; // route-end, lane-body, dock-body, allowed
    readonly long[] _pr57263ReasonTicks = new long[4];
    readonly long[] _pr57263StageTicks = new long[4]; // TurnWay, lane BodyOnWay, EntryPlan, dock BodyOnWay
    readonly long[] _pr57263StageCalls = new long[4];
    readonly Dictionary<int, (long Calls, long Allowed, long Ticks)> _pr57263Candidates = new();
    readonly Dictionary<int, long> _pr57263Blockers = new();
    long _pr57263TotalTicks, _pr57263Calls;

    long Pr57263Begin() =>
        Pr57263Enabled && _pr57261Active
            ? System.Diagnostics.Stopwatch.GetTimestamp()
            : 0L;

    void Pr57263Stage(int kind, long start)
    {
        if (start == 0) return;
        _pr57263StageTicks[kind] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57263StageCalls[kind]++;
    }

    bool Pr57263Finish(Vehicle candidate, long start, int reason, int? blockerId, bool allowed)
    {
        // Preserve the #262 observer and the canonical return even if #263 is off.
        var result = Pr57262CanTurnNowResult(candidate, allowed);
        if (start == 0) return result;
        var ticks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57263Calls++;
        _pr57263TotalTicks += ticks;
        _pr57263ReasonCalls[reason]++;
        _pr57263ReasonTicks[reason] += ticks;
        _pr57263Candidates.TryGetValue(candidate.Id, out var row);
        _pr57263Candidates[candidate.Id] =
            (row.Calls + 1, row.Allowed + (allowed ? 1 : 0), row.Ticks + ticks);
        if (blockerId is {} id)
        {
            _pr57263Blockers.TryGetValue(id, out var hits);
            _pr57263Blockers[id] = hits + 1;
        }
        return result;
    }

    void Pr57263Report()
    {
        if (!Pr57263Enabled) return;
        static double Ms(long ticks) => ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        var attributed = _pr57263StageTicks.Sum();
        Console.WriteLine(
            $"PR57_263_FANOUT slices={_pr57261Slices} calls={_pr57263Calls} " +
            $"allowed={_pr57263ReasonCalls[3]} denied={_pr57263Calls - _pr57263ReasonCalls[3]} " +
            $"distinctCandidates={_pr57263Candidates.Count} distinctBodyBlockers={_pr57263Blockers.Count} " +
            $"inclusiveMs={Ms(_pr57263TotalTicks):F6} " +
            $"turnWayCalls={_pr57263StageCalls[0]} turnWayMs={Ms(_pr57263StageTicks[0]):F6} " +
            $"laneBodyCalls={_pr57263StageCalls[1]} laneBodyMs={Ms(_pr57263StageTicks[1]):F6} " +
            $"entryPlanCalls={_pr57263StageCalls[2]} entryPlanMs={Ms(_pr57263StageTicks[2]):F6} " +
            $"dockBodyCalls={_pr57263StageCalls[3]} dockBodyMs={Ms(_pr57263StageTicks[3]):F6} " +
            $"unattributedMs={Ms(_pr57263TotalTicks - attributed):F6} " +
            $"routeEnd={_pr57263ReasonCalls[0]} laneBody={_pr57263ReasonCalls[1]} " +
            $"dockBody={_pr57263ReasonCalls[2]} allowedReason={_pr57263ReasonCalls[3]} " +
            "scope=REAL_14H_ALL_REACHED_CALLS timingsIntrusive=True " +
            "unattributedIncludesProbeOverhead=True noExactReadWriteClosure=True noReuse=True");
        Console.WriteLine("PR57_263_CANDIDATES " +
            string.Join(" ", _pr57263Candidates.OrderByDescending(kv => kv.Value.Ticks)
                .ThenBy(kv => kv.Key).Take(15).Select(kv =>
                    $"{kv.Key}:{kv.Value.Calls}:{kv.Value.Allowed}:{Ms(kv.Value.Ticks):F6}")) +
            " format=id:calls:allowed:inclusiveMs top=15");
        Console.WriteLine("PR57_263_BLOCKERS " +
            string.Join(" ", _pr57263Blockers.OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key).Take(15).Select(kv =>
                    $"{kv.Key}:{kv.Value}")) +
            " format=id:returns top=15");
    }
}
