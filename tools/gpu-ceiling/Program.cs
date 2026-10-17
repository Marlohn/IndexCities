using System.Diagnostics;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

// Isolated hardware ceiling test, not the game: N cars on ring lanes, each one
// following the car ahead (IDM), 2 s steps. Compares one CPU core, all CPU
// cores and the GPU, talking to the GPU every step or once per 30 steps.
const float Dt = 2f, V0 = 12f, T = 1.5f, A = 1f, S0 = 2f, CarLen = 5f, Spacing = 25f;
const float TwoSqrtAB = 2.449489743f; // 2*sqrt(A*B) with B=1.5
const int PerLane = 50;
const float LaneLen = PerLane * Spacing;
const int Steps = 4320; // 1/10 of a game day
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
static string Line(string mode, int n, double usPerStep) =>
    $"{mode,-28} carros={n,7}  {usPerStep,9:F1} µs/passo  {usPerStep * 43200 / 1e6,8:F2} s por dia de jogo (só este movimento)";

using var context = Context.Create(b => b.Cuda());
var device = context.Devices.FirstOrDefault(d => d.AcceleratorType == AcceleratorType.Cuda);
Console.WriteLine($"CPU threads={Environment.ProcessorCount}  GPU={device?.Name ?? "nenhuma CUDA"}");
Console.WriteLine($"Meta: 1 dia de jogo em 2,5–4,9 s para TUDO (movimento + decisões + economia). Passos por dia: 43.200.");
using var accel = device?.CreateAccelerator(context);
var kernel = accel?.LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>>(Kernel);

foreach (var n in sizes)
{
    // One CPU core.
    {
        var (p, v) = Init(n); var p2 = new float[n]; var v2 = new float[n];
        var sw = Stopwatch.StartNew();
        for (var s = 0; s < Steps; s++) { for (var i = 0; i < n; i++) Update(i, p, v, p2, v2); (p, p2) = (p2, p); (v, v2) = (v2, v); }
        Console.WriteLine(Line("CPU 1 núcleo", n, sw.Elapsed.TotalMilliseconds * 1000 / Steps));
    }
    // All CPU cores, one parallel loop per step (barrier between steps).
    {
        var (p, v) = Init(n); var p2 = new float[n]; var v2 = new float[n];
        var chunk = Math.Max(1, n / (Environment.ProcessorCount * 4));
        var sw = Stopwatch.StartNew();
        for (var s = 0; s < Steps; s++)
        {
            var (pi, vi, po, vo) = (p, v, p2, v2);
            Parallel.For(0, (n + chunk - 1) / chunk, c => { var end = Math.Min(n, (c + 1) * chunk); for (var i = c * chunk; i < end; i++) Update(i, pi, vi, po, vo); });
            (p, p2) = (p2, p); (v, v2) = (v2, v);
        }
        Console.WriteLine(Line("CPU todos os núcleos", n, sw.Elapsed.TotalMilliseconds * 1000 / Steps));
    }
    if (accel == null || kernel == null) continue;
    foreach (var batch in new[] { 1, 30 })
    {
        var (p, v) = Init(n);
        using var pa = accel.Allocate1D(p); using var va = accel.Allocate1D(v);
        using var pb = accel.Allocate1D<float>(n); using var vb = accel.Allocate1D<float>(n);
        var back = new float[64];
        // Warm-up (JIT compile of the kernel).
        kernel((int)n, pa.View, va.View, pb.View, vb.View); accel.Synchronize();
        var sw = Stopwatch.StartNew();
        var (pIn, vIn, pOut, vOut) = (pa, va, pb, vb);
        for (var s = 0; s < Steps; s++)
        {
            kernel((int)n, pIn.View, vIn.View, pOut.View, vOut.View);
            (pIn, pOut) = (pOut, pIn); (vIn, vOut) = (vOut, vIn);
            // Talking to the CPU: wait for the GPU and read a few results back.
            if ((s + 1) % batch == 0) { accel.Synchronize(); pIn.View.AsContiguous().SubView(0, Math.Min(64, n)).CopyToCPU(back.AsSpan(0, Math.Min(64, n))); }
        }
        accel.Synchronize();
        Console.WriteLine(Line(batch == 1 ? "GPU conversando a cada passo" : "GPU conversando a cada 30", n, sw.Elapsed.TotalMilliseconds * 1000 / Steps));
    }
}
