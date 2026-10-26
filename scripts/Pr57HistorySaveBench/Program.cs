using System.Diagnostics;
using System.Globalization;
using System.Text;
using IndexCities.Simulation;

// PR57 #166: isolated observation of authentic persisted states. No game
// changes, synthetic entities, time skipping, profiler or caching.
if (args.Length != 1 || !File.Exists(args[0]))
    throw new ArgumentException("Expected exactly one authentic checkpoint JSON path.");
var input = File.ReadAllText(args[0], Encoding.UTF8);
var baseline = CitySimulation.FromJson(input);
var beforeNow = baseline.State.Now;
var liveBefore = baseline.State.Citizens.Count(c => c.Alive);
var historyBefore = baseline.State.Citizens.Sum(c => c.History.Count);
var ledgerBefore = baseline.State.Ledger.Count;

// Warm all three public paths outside the timed regions.
var warm = baseline.SaveToJson();
var warmed = CitySimulation.FromJson(warm);
warmed.Advance(2);
const int repeats = 20;
string latestSave = "";
var save = Timed(() =>
{
    for (var i = 0; i < repeats; i++)
        latestSave = baseline.SaveToJson();
});
if (string.IsNullOrEmpty(latestSave) || baseline.State.Now != beforeNow)
    throw new InvalidOperationException("Save unexpectedly mutated the simulation.");

CitySimulation? latestLoad = null;
var load = Timed(() =>
{
    for (var i = 0; i < repeats; i++)
        latestLoad = CitySimulation.FromJson(input);
});
if (latestLoad is null || latestLoad.State.Citizens.Sum(c => c.History.Count) != historyBefore)
    throw new InvalidOperationException("Load did not preserve personal history.");

var simulation = CitySimulation.FromJson(input);
const int steps = 300;
var advance = Timed(() =>
{
    for (var i = 0; i < steps; i++)
        simulation.Advance(2);
});
if (simulation.State.Now != beforeNow.AddSeconds(steps * 2))
    throw new InvalidOperationException("Host quantum chronology mismatch.");
simulation.CheckInvariants();
var roundtrip = CitySimulation.FromJson(simulation.SaveToJson());
if (roundtrip.State.Citizens.Sum(c => c.History.Count) != simulation.State.Citizens.Sum(c => c.History.Count))
    throw new InvalidOperationException("Post-Advance save/load lost personal events.");

static (double WallMs, double CpuMs, long AllocBytes, int Gen0, int Gen1, int Gen2) Timed(Action action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var alloc = GC.GetAllocatedBytesForCurrentThread();
    var cpu = Process.GetCurrentProcess().TotalProcessorTime;
    var g0 = GC.CollectionCount(0);
    var g1 = GC.CollectionCount(1);
    var g2 = GC.CollectionCount(2);
    var sw = Stopwatch.StartNew();
    action();
    sw.Stop();
    return (sw.Elapsed.TotalMilliseconds,
        (Process.GetCurrentProcess().TotalProcessorTime - cpu).TotalMilliseconds,
        GC.GetAllocatedBytesForCurrentThread() - alloc,
        GC.CollectionCount(0) - g0, GC.CollectionCount(1) - g1, GC.CollectionCount(2) - g2);
}
static string F(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

Console.WriteLine($"PR57_HISTORY_SAVE checkpoint={Path.GetFileName(args[0])} inputBytes={Encoding.UTF8.GetByteCount(input)} " +
    $"aliveBefore={liveBefore} aliveAfter={simulation.State.Citizens.Count(c => c.Alive)} " +
    $"historyBefore={historyBefore} historyAfter={simulation.State.Citizens.Sum(c => c.History.Count)} " +
    $"ledgerBefore={ledgerBefore} ledgerAfter={simulation.State.Ledger.Count} " +
    $"saveRepeats={repeats} saveWallMs={F(save.WallMs)} saveCpuMs={F(save.CpuMs)} saveAllocBytes={save.AllocBytes} saveGC0={save.Gen0} saveGC1={save.Gen1} saveGC2={save.Gen2} " +
    $"loadRepeats={repeats} loadWallMs={F(load.WallMs)} loadCpuMs={F(load.CpuMs)} loadAllocBytes={load.AllocBytes} loadGC0={load.Gen0} loadGC1={load.Gen1} loadGC2={load.Gen2} " +
    $"steps={steps} simulatedSeconds={steps * 2} advanceWallMs={F(advance.WallMs)} advanceCpuMs={F(advance.CpuMs)} advanceAllocBytes={advance.AllocBytes} advanceGC0={advance.Gen0} advanceGC1={advance.Gen1} advanceGC2={advance.Gen2} " +
    "hostQuantum=2 parity=save-load-invariants observational=True");
