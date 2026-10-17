using System.Diagnostics;
using System.Numerics;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

// Isolated hardware ceiling test, not the game: N cars on ring lanes, each one
// following the car ahead (IDM), 2 s steps. Compares the CPU (scalar and
// vectorised, one core and all cores) with the GPU (waiting for it every step
// or every 30 steps). Each configuration runs several times; the median counts.
const float Dt = 2f, V0 = 12f, T = 1.5f, A = 1f, S0 = 2f, CarLen = 5f, Spacing = 25f;
const float TwoSqrtAB = 2.449489743f; // 2*sqrt(A*B) with B=1.5
const int PerLane = 50;
const float LaneLen = PerLane * Spacing;
const int Steps = 4320; // 1/10 of a game day
const int Runs = 3;
int[] sizes = args.Length > 0 ? args.Select(int.Parse).ToArray() : [1_000, 10_000, 50_000, 200_000];

static void Update(int i, ReadOnlySpan<float> pos, ReadOnlySpan<float> vel, Span<float> posOut, Span<float> velOut)
{
    int lane = i / PerLane, k = i % PerLane, leader = lane * PerLane + (k + 1) % PerLane;
    float gap = pos[leader] - pos[i]; if (gap <= 0) gap += LaneLen; gap -= CarLen; if (gap < 0.1f) gap = 0.1f;
    float v = vel[i], r = v / V0, r2 = r * r;
    float sStar = S0 + v * T + v * (v - vel[leader]) / TwoSqrtAB; if (sStar < S0) sStar = S0;
    float q = sStar / gap;
    float nv = v + A * (1 - r2 * r2 - q * q) * Dt; if (nv < 0) nv = 0;
    float np = pos[i] + nv * Dt; if (np >= LaneLen) np -= LaneLen;
    posOut[i] = np; velOut[i] = nv;
}

// Same arithmetic, Vector<float> wide, over whole lanes [first, last).
static void UpdateLanes(int first, int last, float[] pos, float[] vel, float[] posOut, float[] velOut)
{
    int w = Vector<float>.Count;
    var len = new Vector<float>(LaneLen); var zero = Vector<float>.Zero; var minGap = new Vector<float>(0.1f); var s0 = new Vector<float>(S0);
    for (var lane = first; lane < last; lane++)
    {
        int b = lane * PerLane, k = 0;
        for (; k + w <= PerLane - 1; k += w)
        {
            int i = b + k;
            var p = new Vector<float>(pos, i); var pl = new Vector<float>(pos, i + 1);
            var v = new Vector<float>(vel, i); var vl = new Vector<float>(vel, i + 1);
            var gap = pl - p; gap = Vector.ConditionalSelect(Vector.LessThanOrEqual(gap, zero), gap + len, gap) - new Vector<float>(CarLen); gap = Vector.Max(gap, minGap);
            var r = v / new Vector<float>(V0); var r2 = r * r;
            var sStar = Vector.Max(s0 + v * T + v * (v - vl) / new Vector<float>(TwoSqrtAB), s0);
            var q = sStar / gap;
            var nv = Vector.Max(v + A * (Vector<float>.One - r2 * r2 - q * q) * Dt, zero);
            var np = p + nv * Dt; np = Vector.ConditionalSelect(Vector.GreaterThanOrEqual(np, len), np - len, np);
            np.CopyTo(posOut, i); nv.CopyTo(velOut, i);
        }
        for (; k < PerLane; k++) Update(b + k, pos, vel, posOut, velOut);
    }
}

static void Kernel(Index1D i, ArrayView<float> pos, ArrayView<float> vel, ArrayView<float> posOut, ArrayView<float> velOut)
{
    int lane = i / PerLane, k = i % PerLane, leader = lane * PerLane + (k + 1) % PerLane;
    float gap = pos[leader] - pos[i]; if (gap <= 0) gap += LaneLen; gap -= CarLen; if (gap < 0.1f) gap = 0.1f;
    float v = vel[i], r = v / V0, r2 = r * r;
    float sStar = S0 + v * T + v * (v - vel[leader]) / TwoSqrtAB; if (sStar < S0) sStar = S0;
    float q = sStar / gap;
    float nv = v + A * (1 - r2 * r2 - q * q) * Dt; if (nv < 0) nv = 0;
    float np = pos[i] + nv * Dt; if (np >= LaneLen) np -= LaneLen;
    posOut[i] = np; velOut[i] = nv;
}

static (float[] p, float[] v) Init(int n)
{
    var p = new float[n]; var v = new float[n];
    for (var i = 0; i < n; i++) { p[i] = (i % PerLane) * Spacing; v[i] = 8f + (i % 7) * 0.3f; }
    return (p, v);
}
static double Median(List<double> xs) { xs.Sort(); return xs[xs.Count / 2]; }
static string Line(string mode, int n, double usPerStep, string extra = "") =>
    $"{mode,-34} carros={n,7}  {usPerStep,9:F1} µs/passo  {usPerStep * 43200 / 1e6,8:F2} s por dia de jogo {extra}";

// Runs `step` Steps times after a warm-up, Runs times; returns the median µs per step and the final positions.
static (double us, float[] final) Measure(int n, Action<float[], float[], float[], float[]> step)
{
    var times = new List<double>(); float[] final = [];
    for (var run = 0; run <= Runs; run++)
    {
        var (p, v) = Init(n); var p2 = new float[n]; var v2 = new float[n];
        var steps = run == 0 ? 300 : Steps; // run 0 only warms up the JIT
        var sw = Stopwatch.StartNew();
        for (var s = 0; s < steps; s++) { step(p, v, p2, v2); (p, p2) = (p2, p); (v, v2) = (v2, v); }
        if (run > 0) { times.Add(sw.Elapsed.TotalMilliseconds * 1000 / Steps); final = p; }
    }
    return (Median(times), final);
}

using var context = Context.Create(b => b.Cuda());
var device = context.Devices.FirstOrDefault(d => d.AcceleratorType == AcceleratorType.Cuda);
Console.WriteLine($"CPU threads={Environment.ProcessorCount} Vector<float>={Vector<float>.Count}  GPU={device?.Name ?? "nenhuma CUDA"}  medidas={Runs} (mediana)");
using var accel = device?.CreateAccelerator(context);
var kernel = accel?.LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>>(Kernel);

foreach (var n in sizes)
{
    var lanes = n / PerLane; var parts = Environment.ProcessorCount;
    var (scalar, reference) = Measure(n, (p, v, po, vo) => { for (var i = 0; i < n; i++) Update(i, p, v, po, vo); });
    Console.WriteLine(Line("CPU escalar 1 núcleo", n, scalar));
    var (vec, vecFinal) = Measure(n, (p, v, po, vo) => UpdateLanes(0, lanes, p, v, po, vo));
    Console.WriteLine(Line("CPU vetorizada 1 núcleo", n, vec, $"(mesmo resultado: {vecFinal.SequenceEqual(reference)})"));
    var (par, parFinal) = Measure(n, (p, v, po, vo) => Parallel.For(0, parts, c => UpdateLanes(lanes * c / parts, lanes * (c + 1) / parts, p, v, po, vo)));
    Console.WriteLine(Line("CPU vetorizada todos os núcleos", n, par, $"(mesmo resultado: {parFinal.SequenceEqual(reference)})"));
    if (accel == null || kernel == null) continue;
    foreach (var batch in new[] { 1, 30 })
    {
        var times = new List<double>(); var back = new float[64]; float[] gpuFinal = [];
        for (var run = 0; run <= Runs; run++)
        {
            var (p, v) = Init(n);
            using var pa = accel.Allocate1D(p); using var va = accel.Allocate1D(v);
            using var pb = accel.Allocate1D<float>(n); using var vb = accel.Allocate1D<float>(n);
            var (pIn, vIn, pOut, vOut) = (pa, va, pb, vb);
            var steps = run == 0 ? 300 : Steps;
            accel.Synchronize();
            var sw = Stopwatch.StartNew();
            for (var s = 0; s < steps; s++)
            {
                kernel((int)n, pIn.View, vIn.View, pOut.View, vOut.View);
                (pIn, pOut) = (pOut, pIn); (vIn, vOut) = (vOut, vIn);
                // Talking to the CPU: wait for the GPU and read a few results back.
                if ((s + 1) % batch == 0) { accel.Synchronize(); pIn.View.AsContiguous().SubView(0, Math.Min(64, n)).CopyToCPU(back.AsSpan(0, Math.Min(64, n))); }
            }
            accel.Synchronize();
            if (run > 0) { times.Add(sw.Elapsed.TotalMilliseconds * 1000 / Steps); gpuFinal = pIn.GetAsArray1D(); }
        }
        var maxDiff = gpuFinal.Zip(reference).Max(x => Math.Abs(x.First - x.Second));
        Console.WriteLine(Line(batch == 1 ? "GPU espera a cada passo" : "GPU espera a cada 30 passos", n, Median(times), $"(diferença máx. de posição contra a CPU: {maxDiff:G3} m)"));
    }
}
