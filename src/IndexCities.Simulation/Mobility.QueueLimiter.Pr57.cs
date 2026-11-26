namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 #165: sampled, strictly observational attribution of the first zero-step
    // gate. Does not cache, skip a slice, alter a decision, or survive a save.
    // Instrumented profile only; never used by production movement.
    static readonly bool Pr57QueueLimiterEnabled =
        Environment.GetEnvironmentVariable("PR57_QUEUE_LIMITER_PROBE") == "1";
    readonly long[] _pr57QueueLimiterAttempts = new long[10];
    readonly long[] _pr57QueueLimiterFila = new long[10];
    readonly long[] _pr57QueueLimiterLeader = new long[10];
    readonly long[] _pr57QueueLimiterEntry = new long[10];
    readonly long[] _pr57QueueLimiterLaneTicks = new long[10];
    readonly long[] _pr57QueueLimiterBendTicks = new long[10];

    void Pr57QueueLimiterObserve(int stage, string? cause, long laneTicks,
        long bendTicks, bool leader, bool entering)
    {
        // 0=unknown, 1=longitudinal leader, 2=stop line, 3=reserved poses,
        // 4=bend bodies, 5=site, 6=pedestrian crossing, 7=turn recovery,
        // 8=initial speed/remaining gate, 9=junction admission denial.
        // "First" is the first observed zero within the original sequential
        // predicates; later recovery can change the outcome.
        if(stage<0||stage>=_pr57QueueLimiterAttempts.Length)stage=0;
        _pr57QueueLimiterAttempts[stage]++;
        if(cause?.StartsWith("Fila",StringComparison.Ordinal)==true)
            _pr57QueueLimiterFila[stage]++;
        if(leader)_pr57QueueLimiterLeader[stage]++;
        if(entering)_pr57QueueLimiterEntry[stage]++;
        _pr57QueueLimiterLaneTicks[stage]+=laneTicks;
        _pr57QueueLimiterBendTicks[stage]+=bendTicks;
    }

    void Pr57QueueLimiterPrint(long slices)
    {
        string[] stages=["unknown","leader","stop-line","reserved-poses",
            "bend","site","crossing","turn","initial","junction"];
        for(var i=0;i<stages.Length;i++)
        {
            var laneMs=_pr57QueueLimiterLaneTicks[i]*1000.0/System.Diagnostics.Stopwatch.Frequency;
            var bendMs=_pr57QueueLimiterBendTicks[i]*1000.0/System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(
                $"PR57_QUEUE_LIMITER slices={slices} stage={stages[i]} attempts={_pr57QueueLimiterAttempts[i]} " +
                $"fila={_pr57QueueLimiterFila[i]} leader={_pr57QueueLimiterLeader[i]} " +
                $"entry={_pr57QueueLimiterEntry[i]} laneMs={laneMs:F3} bendMs={bendMs:F3}");
        }
    }
}
