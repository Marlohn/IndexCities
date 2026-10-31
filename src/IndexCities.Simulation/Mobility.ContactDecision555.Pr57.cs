namespace IndexCities.Simulation;

// PR57 #555: decision-oriented narrowphase. Every query is recomputed from
// current bodies and hypothetical poses, in original candidate order.
// A separating axis can disprove penetration > limit without calculating
// remaining axes. No blocker, pose, contact or verdict is cached.
public sealed partial class CitySimulation
{
    static readonly bool Pr57ContactDecision555Enabled =
        Environment.GetEnvironmentVariable("PR57_CONTACT_DECISION_555") == "1";
    static readonly bool Pr57ContactDecision555Verify =
        Environment.GetEnvironmentVariable("PR57_CONTACT_DECISION_VERIFY") == "1";
    static readonly bool Pr57ContactDecision555Diag =
        Environment.GetEnvironmentVariable("PR57_GRID_DIAG") == "1";
    static int _pr57ContactDecision555Announced;

    static bool Pr57ContactExceeds555(
        Vehicle a, Point aAt, double aHeading,
        Vehicle b, Point bAt, double bHeading, double limit)
    {
        // Existing intrusive SAT probes and memo instrumentation retain their
        // canonical call paths; the production flag remains off by default.
        if (!Pr57ContactDecision555Enabled || Pr57271Enabled || Pr57272MemoEnabled)
            return Penetration(a, aAt, aHeading, b, bAt, bHeading) > limit;

        if (Pr57ContactDecision555Diag &&
            System.Threading.Interlocked.Exchange(ref _pr57ContactDecision555Announced, 1) == 0)
            Console.WriteLine("PR57_555_CONTACT_DECISION used=True scope=LIVE_BOOLEAN_NARROWPHASE");

        static double Dot(Point x, Point y) => x.X * y.X + x.Z * y.Z;
        // Identical trigonometry, separating-axis order and arithmetic to
        // PenetrationCore; the original scalar minimum remains the fallback.
        var af = new Point(Math.Sin(aHeading), Math.Cos(aHeading));
        var ar = new Point(af.Z, -af.X);
        var bf = new Point(Math.Sin(bHeading), Math.Cos(bHeading));
        var br = new Point(bf.Z, -bf.X);
        var d = bAt - aAt;
        ReadOnlySpan<Point> axes = stackalloc Point[] { af, ar, bf, br };
        var exceeds = true;
        foreach (var x in axes)
        {
            var depth = BodyHalfLength(a) * Math.Abs(Dot(af, x)) +
                        BodyHalfWidth(a) * Math.Abs(Dot(ar, x)) +
                        BodyHalfLength(b) * Math.Abs(Dot(bf, x)) +
                        BodyHalfWidth(b) * Math.Abs(Dot(br, x)) -
                        Math.Abs(Dot(d, x));
            // !(depth > limit) propagates NaN as a false comparison, as does
            // Enumerable.Min(axes) > limit in the original implementation.
            if (!(depth > limit)) { exceeds = false; break; }
        }
        if (Pr57ContactDecision555Verify)
        {
            var canonical = Penetration(a, aAt, aHeading, b, bAt, bHeading) > limit;
            if (canonical != exceeds)
                throw new InvalidOperationException("PR57 #555 contact decision divergence");
        }
        return exceeds;
    }
}
