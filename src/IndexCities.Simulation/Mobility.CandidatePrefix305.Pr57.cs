namespace IndexCities.Simulation;

// PR57 #305: opt-in, intrusive census of candidates actually visited by the
// canonical BodyOnWay short-circuit in four authentic REAL 14h junction readers.
// It is not an invalidation journal and never changes the return value.
public sealed partial class CitySimulation
{
    static readonly bool Pr57305Enabled =
        Environment.GetEnvironmentVariable("PR57_305_CANDIDATES") == "1";

    bool Pr57305Target(Vehicle reader) =>
        Pr57305Enabled && _pr57300Calls is >= 76 and <= 100 &&
        reader.Id is 1243 or 1282 or 1071 or 1380;

    void Pr57305Body(Vehicle reader, Vehicle? selected, int poses,
        int visited, int eligible, int nearNegative, int geometry, long started)
    {
        var ticks=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        Console.WriteLine(
            $"PR57_305_BODY call={_pr57300Calls} slice={_pr57302CurrentSlice} " +
            $"id={reader.Id} selected={selected?.Id ?? -1} " +
            $"vehicles={State.Vehicles.Count} poses={poses} visited={visited} " +
            $"eligible={eligible} nearNegative={nearNegative} " +
            $"geometry={geometry} ticks={ticks} " +
            "scope=CANONICAL_PREFIX_ONLY_NOT_CAUSAL_CLOSURE");
    }
}
