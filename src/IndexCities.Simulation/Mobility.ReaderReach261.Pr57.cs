namespace IndexCities.Simulation;

// PR57 #261: bounded observation of the *reached* s0-s8 actor regions and
// selected nested physical readers for one authentic 14h queue vehicle.
// This is not a complete read/write-set, not an invalidation journal, and
// never supplies a movement, reservation, or junction decision.
public sealed partial class CitySimulation
{
    static readonly bool Pr57261Enabled =
        Environment.GetEnvironmentVariable("PR57_261_PATH_REACH") == "1";

    readonly long[] _pr57261StageTicks = new long[9];
    readonly long[] _pr57261StageBytes = new long[9];
    readonly long[] _pr57261StageVisits = new long[9];
    readonly long[] _pr57261NestedCalls = new long[5];
    readonly long[] _pr57261JunctionReasons = new long[18];
    long _pr57261Slices, _pr57261ReaderPasses, _pr57261JunctionCalls;
    bool _pr57261Active, _pr57261InReader;
    int _pr57261Stage;
    long _pr57261StartTicks, _pr57261StartBytes;

    void Pr57261BeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57261Active = Pr57261Enabled && profiled && dt > 0 &&
            operationsDt is null && _pr57261Slices < 600;
        if (_pr57261Active) _pr57261Slices++;
    }

    void Pr57261BeginVehicle(Vehicle v)
    {
        if (!_pr57261Active || v.Id != 1071) return;
        _pr57261InReader = true;
        _pr57261ReaderPasses++;
        _pr57261Stage = 0;
        _pr57261StartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57261StartBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    void Pr57261Mark(int nextStage)
    {
        if (!_pr57261InReader) return;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        _pr57261StageTicks[_pr57261Stage] += now - _pr57261StartTicks;
        _pr57261StageBytes[_pr57261Stage] += bytes - _pr57261StartBytes;
        if (nextStage is >= 1 and <= 8) _pr57261StageVisits[nextStage]++;
        _pr57261Stage = nextStage;
        _pr57261StartTicks = now;
        _pr57261StartBytes = bytes;
    }

    void Pr57261Hit(int kind)
    {
        if (_pr57261InReader) _pr57261NestedCalls[kind]++;
    }

    void Pr57261Junction(Pr57JunctionExit reason, bool allowed)
    {
        if (!_pr57261InReader) return;
        _pr57261JunctionCalls++;
        _pr57261JunctionReasons[(int)reason]++;
    }

    void Pr57261FinishVehicle(Vehicle v)
    {
        if (!_pr57261InReader || v.Id != 1071) return;
        Pr57261Mark(0);
        _pr57261InReader = false;
    }

    void Pr57261EndSlice()
    {
        if (!_pr57261Active) return;
        _pr57261Active = false;
        if (_pr57261Slices != 600) return;
        static double Ms(long ticks) =>
            ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_261_REACH slices={_pr57261Slices} reader=1071 " +
            $"readerPasses={_pr57261ReaderPasses} junctionReturns={_pr57261JunctionCalls} " +
            string.Join(" ", Enumerable.Range(0, 9).Select(i =>
                $"s{i}Visits={_pr57261StageVisits[i]} s{i}Ms={Ms(_pr57261StageTicks[i]):F6} " +
                $"s{i}MB={_pr57261StageBytes[i] / 1e6:F6}")) + " " +
            $"bodyOnWay={_pr57261NestedCalls[0]} " +
            $"trafficOnWay={_pr57261NestedCalls[1]} " +
            $"turnWay={_pr57261NestedCalls[2]} " +
            $"spaceFree={_pr57261NestedCalls[3]} " +
            $"admitManoeuvre={_pr57261NestedCalls[4]} " +
            string.Join(" ", Enum.GetValues<Pr57JunctionExit>().Select(reason =>
                $"{reason}={_pr57261JunctionReasons[(int)reason]}")) +
            " scope=REAL_14H_ONE_READER stageTimesIntrusive=True " +
            "nestedCallsReachedOnly=True noExactReadWriteClosure=True " +
            "noCaching=True noSkippedSlices=True observational=True");
    }
}
