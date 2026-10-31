namespace IndexCities.Simulation;

// PR57 #558: bounded writer-invalidated pair workset for CURRENT body poses only.
// No admission, candidate order, first blocker, projected contact, or sub-slice
// is cached or skipped. Body dimensions are checked on each hit, and all public
// Advance boundaries discard the cache (including after external State edits).
public sealed partial class CitySimulation
{
    static readonly bool Pr57PairBaselineEnabled =
        Environment.GetEnvironmentVariable("PR57_PAIR_BASELINE_558")=="1" &&
        Environment.GetEnvironmentVariable("PR57_CONTACT_EPOCH_557")=="1";
    static readonly bool Pr57PairBaselineVerify =
        Environment.GetEnvironmentVariable("PR57_PAIR_BASELINE_VERIFY")=="1";
    static readonly bool Pr57PairBaselineDiag =
        Environment.GetEnvironmentVariable("PR57_PAIR_BASELINE_DIAG")=="1";
    readonly record struct Pr57PairBaselineEntry(
        long EpochA,long EpochB,long LengthA,long WidthA,long LengthB,long WidthB,
        double Penetration);
    readonly Dictionary<(Vehicle A,Vehicle B),Pr57PairBaselineEntry> _pr57PairBaseline=[];
    long _pr57PairBaselineHits,_pr57PairBaselineMisses,_pr57PairBaselineCalls;
    void Pr57PairBaselineAdvanceBegin()
    {
        if(Pr57PairBaselineEnabled)_pr57PairBaseline.Clear();
    }
    void Pr57PairBaselineAdvanceEnd()
    {
        if(!Pr57PairBaselineEnabled||!Pr57PairBaselineDiag)return;
        if(++_pr57PairBaselineCalls%100==0)
            Console.WriteLine($"PR57_PAIR_BASELINE_558 calls={_pr57PairBaselineCalls} hits={_pr57PairBaselineHits} misses={_pr57PairBaselineMisses} entries={_pr57PairBaseline.Count} scope=CURRENT_POSE_ONLY");
    }
    double Pr57PairBaselineCurrent(Vehicle a,Vehicle b)
    {
        // Do not bypass profiling or the independent penetration memo probe.
        if(!Pr57PairBaselineEnabled||Pr57271Enabled||Pr57272MemoEnabled||CostProfile is not null)
            return Penetration(a,a.Position,a.Heading,b,b.Position,b.Heading);
        var la=BitConverter.DoubleToInt64Bits(BodyHalfLength(a));
        var wa=BitConverter.DoubleToInt64Bits(BodyHalfWidth(a));
        var lb=BitConverter.DoubleToInt64Bits(BodyHalfLength(b));
        var wb=BitConverter.DoubleToInt64Bits(BodyHalfWidth(b));
        // Reference-identity keys avoid aliases when vehicles are removed or
        // replaced. Ordered pairs preserve floating-point operand order.
        var key=(a,b);
        if(_pr57PairBaseline.TryGetValue(key,out var entry)&&
            entry.EpochA==a.Pr57ContactEpoch&&entry.EpochB==b.Pr57ContactEpoch&&
            entry.LengthA==la&&entry.WidthA==wa&&entry.LengthB==lb&&entry.WidthB==wb)
        {
            _pr57PairBaselineHits++;
            if(Pr57PairBaselineVerify)
            {
                var original=Penetration(a,a.Position,a.Heading,b,b.Position,b.Heading);
                if(BitConverter.DoubleToInt64Bits(original)!=BitConverter.DoubleToInt64Bits(entry.Penetration))
                    throw new InvalidOperationException("PR57 #558 current-pose penetration mismatch");
            }
            return entry.Penetration;
        }
        var result=Penetration(a,a.Position,a.Heading,b,b.Position,b.Heading);
        _pr57PairBaselineMisses++;
        // No unbounded pair matrix on 10k exploratory loads. Once full,
        // uncached pairs use the original kernel; no eviction scan.
        if(_pr57PairBaseline.Count<8192)
            _pr57PairBaseline[key]=new(a.Pr57ContactEpoch,b.Pr57ContactEpoch,la,wa,lb,wb,result);
        return result;
    }
}
