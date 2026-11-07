namespace IndexCities.Simulation;

// PR57 #271: bounded, strictly observational exact-argument census for the
// pure Penetration calculation. One dictionary per PHYSICAL sub-slice;
// no decision, cache, actor order, state, or save reads these observations.
// This is not a dependency closure for any surrounding physical predicate.
public sealed partial class CitySimulation
{
    static readonly bool Pr57271Enabled =
        Environment.GetEnvironmentVariable("PR57_271_PENETRATION_KEYS") == "1";
    static int Pr57271Int(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
    static readonly int Pr57271From = Pr57271Int("PR57_271_FROM", 581);
    static readonly int Pr57271To = Pr57271Int("PR57_271_TO", 600);

    // Vehicle is a reference type. The key includes BOTH references, both exact
    // pose bit patterns, and all four dimensions actually read by Penetration.
    // Thus an unchanged key denotes the same pure calculation, not the same
    // admissibility, reservation, route, clock, pedestrian or future state.
    readonly record struct Pr57271Key(
        Vehicle A, Vehicle B,
        long AX, long AZ, long AH, long BX, long BZ, long BH,
        long AL, long AW, long BL, long BW);

    [ThreadStatic] static Dictionary<Pr57271Key, int>? _pr57271Seen;
    [ThreadStatic] static bool _pr57271Active, _pr57271InActor;
    [ThreadStatic] static int _pr57271Stage;
    [ThreadStatic] static long[]? _pr57271Calls, _pr57271Same, _pr57271Cross;

    int _pr57271PhysicalSlices, _pr57271Observed, _pr57271MaxUnique;
    readonly long[] _pr57271TotalCalls = new long[10];
    readonly long[] _pr57271TotalSame = new long[10];
    readonly long[] _pr57271TotalCross = new long[10];

    void Pr57271BeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57271Active = _pr57271InActor = false;
        if (!Pr57271Enabled || !profiled || dt <= 0 || operationsDt is not null) return;
        _pr57271PhysicalSlices++;
        if (_pr57271PhysicalSlices < Pr57271From ||
            _pr57271PhysicalSlices > Pr57271To) return;
        _pr57271Active = true;
        _pr57271Stage = 9; // outside ordered actor loop
        (_pr57271Seen ??= new Dictionary<Pr57271Key, int>()).Clear();
        _pr57271Calls ??= new long[10];
        _pr57271Same ??= new long[10];
        _pr57271Cross ??= new long[10];
        Array.Clear(_pr57271Calls);
        Array.Clear(_pr57271Same);
        Array.Clear(_pr57271Cross);
    }

    void Pr57271BeginVehicle()
    {
        if (!_pr57271Active) return;
        _pr57271InActor = true;
        _pr57271Stage = 0;
    }

    void Pr57271Mark(int stage)
    {
        if (_pr57271Active && _pr57271InActor)
            _pr57271Stage = stage is >= 0 and <= 8 ? stage : 9;
    }

    void Pr57271FinishVehicle()
    {
        if (!_pr57271Active) return;
        _pr57271InActor = false;
        _pr57271Stage = 9;
    }

    static void Pr57271PenetrationObserve(
        Vehicle a, Point aAt, double aHeading,
        Vehicle b, Point bAt, double bHeading)
    {
        if (!_pr57271Active) return;
        static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);
        var key = new Pr57271Key(
            a, b, Bits(aAt.X), Bits(aAt.Z), Bits(aHeading),
            Bits(bAt.X), Bits(bAt.Z), Bits(bHeading),
            Bits(BodyHalfLength(a)), Bits(BodyHalfWidth(a)),
            Bits(BodyHalfLength(b)), Bits(BodyHalfWidth(b)));
        var stage = _pr57271Stage;
        _pr57271Calls![stage]++;
        if (_pr57271Seen!.TryGetValue(key, out var first))
        {
            if (first == stage) _pr57271Same![stage]++;
            else _pr57271Cross![stage]++;
        }
        else _pr57271Seen.Add(key, stage);
    }

    void Pr57271EndSlice()
    {
        if (!_pr57271Active) return;
        _pr57271Active = _pr57271InActor = false;
        _pr57271Observed++;
        _pr57271MaxUnique = Math.Max(_pr57271MaxUnique, _pr57271Seen!.Count);
        for (var i = 0; i < 10; i++)
        {
            _pr57271TotalCalls[i] += _pr57271Calls![i];
            _pr57271TotalSame[i] += _pr57271Same![i];
            _pr57271TotalCross[i] += _pr57271Cross![i];
        }
        if (_pr57271PhysicalSlices != Pr57271To) return;
        var calls = _pr57271TotalCalls.Sum();
        var same = _pr57271TotalSame.Sum();
        var cross = _pr57271TotalCross.Sum();
        Console.WriteLine(
            $"PR57_271_KEYS slices={_pr57271PhysicalSlices} observed={_pr57271Observed} " +
            $"calls={calls} uniqueWithinSlices={calls - same - cross} " +
            $"sameStageDuplicates={same} crossStageDuplicates={cross} " +
            $"maxUniquePerSlice={_pr57271MaxUnique} " +
            "key=references+bitwisePoses+bitwiseDimensions " +
            "scope=PenetrationOnly observational=True intrusive=True " +
            "surroundingCausalityNotClosed=True noCache=True noSkip=True");
        for (var i = 0; i < 10; i++)
            Console.WriteLine(
                $"PR57_271_STAGE stage={i} calls={_pr57271TotalCalls[i]} " +
                $"sameStageDuplicates={_pr57271TotalSame[i]} " +
                $"crossStageDuplicates={_pr57271TotalCross[i]}");
    }
}
