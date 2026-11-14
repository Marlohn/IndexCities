namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 #242: bounded, intrusive observation of the canonical SeekFood.
    // No caches, new decision paths, simulated purchases, or persistent fields.
    static readonly bool Pr57Food242Enabled =
        Environment.GetEnvironmentVariable("PR57_242_FOOD_SEARCH") == "1";
    long _pr57Food242Calls, _pr57Food242ArrivalCalls, _pr57Food242Candidates;
    long _pr57Food242Rejected, _pr57Food242RouteAttempts, _pr57Food242Success;
    long _pr57Food242Failures, _pr57Food242TotalTicks, _pr57Food242RouteTicks;

    public IEnumerable<string> Pr57Food242Report()
    {
        if(!Pr57Food242Enabled)yield break;
        var hz=(double)System.Diagnostics.Stopwatch.Frequency;
        yield return $"PR57_242_FOOD calls={_pr57Food242Calls} arrivalCalls={_pr57Food242ArrivalCalls} " +
            $"visited={_pr57Food242Candidates} rejected={_pr57Food242Rejected} " +
            $"routes={_pr57Food242RouteAttempts} routeSuccess={_pr57Food242Success} " +
            $"failedCalls={_pr57Food242Failures} totalMs={_pr57Food242TotalTicks*1000/hz:F5} " +
            $"routeMs={_pr57Food242RouteTicks*1000/hz:F5} scope=INTRUSIVE_OBSERVATIONAL";
    }
}
