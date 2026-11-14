namespace IndexCities.Simulation;

// PR57 #272: opt-in exploratory memo of a PURE, exact-argument calculation,
// not of physical decisions. A fresh dictionary is cleared for every canonical
// physical sub-slice. All vehicle, pedestrian, reservation and temporal phases
// still execute. Disabled by default; no persisted state or cross-Advance cache.
public sealed partial class CitySimulation
{
    static readonly bool Pr57272MemoEnabled =
        Environment.GetEnvironmentVariable("PR57_272_PENETRATION_MEMO") == "1";
    static readonly int Pr57272ReportAt = Pr57271Int("PR57_272_REPORT_AT", 600);
    [ThreadStatic] static Dictionary<Pr57271Key, double>? _pr57272Memo;
    [ThreadStatic] static bool _pr57272MemoActive;
    [ThreadStatic] static long _pr57272SliceHits, _pr57272SliceMisses;
    int _pr57272Slices, _pr57272PeakKeys;
    long _pr57272TotalHits, _pr57272TotalMisses;

    // Same bitwise key as the #271 observer: both vehicle object identities,
    // six pose coordinates/angles and the four dimensions Penetration reads.
    // Other vehicle state is irrelevant to this pure SAT calculation ONLY.
    static Pr57271Key Pr57272Key(
        Vehicle a, Point aAt, double aHeading,
        Vehicle b, Point bAt, double bHeading)
    {
        static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);
        return new Pr57271Key(
            a, b, Bits(aAt.X), Bits(aAt.Z), Bits(aHeading),
            Bits(bAt.X), Bits(bAt.Z), Bits(bHeading),
            Bits(BodyHalfLength(a)), Bits(BodyHalfWidth(a)),
            Bits(BodyHalfLength(b)), Bits(BodyHalfWidth(b)));
    }

    void Pr57272BeginSlice(double dt, double? operationsDt)
    {
        _pr57272MemoActive = false;
        if (!Pr57272MemoEnabled || dt <= 0 || operationsDt is not null) return;
        _pr57272Slices++;
        (_pr57272Memo ??= new Dictionary<Pr57271Key, double>()).Clear();
        _pr57272SliceHits = _pr57272SliceMisses = 0;
        _pr57272MemoActive = true;
    }

    void Pr57272EndSlice()
    {
        if (!_pr57272MemoActive) return;
        _pr57272MemoActive = false;
        _pr57272TotalHits += _pr57272SliceHits;
        _pr57272TotalMisses += _pr57272SliceMisses;
        _pr57272PeakKeys = Math.Max(_pr57272PeakKeys, _pr57272Memo!.Count);
        _pr57272Memo.Clear();
        if (_pr57272Slices == Pr57272ReportAt)
            Console.WriteLine(
                $"PR57_272_MEMO slices={_pr57272Slices} hits={_pr57272TotalHits} " +
                $"misses={_pr57272TotalMisses} peakKeys={_pr57272PeakKeys} " +
                "scope=purePenetrationExactArgumentsOnly optIn=True " +
                "perPhysicalSlice=True noCrossAdvanceCache=True " +
                "noSkippedActors=True noSkippedSlices=True");
    }
}
