using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;

// PR57 #184: retained heap / GC discrimination on authentic checkpoints.
// No invented residents, no altered rules or saves, no time skipping.
// Forced GC occurs OUTSIDE timed Advance(2) windows in retained mode.
if (args.Length != 5 || !File.Exists(args[0]))
    throw new ArgumentException("Expected source.json case plain|retained steps outputDir.");
var source = args[0];
var label = args[1];
var mode = args[2];
if (mode is not ("plain" or "retained"))
    throw new ArgumentException("Mode must be plain or retained.");
if (!int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var steps)
    || steps <= 0 || steps % 4 != 0)
    throw new ArgumentException("Steps must be positive and divisible by four.");
var output = args[4];
Directory.CreateDirectory(output);

static void Warm(string path)
{
    var clone = CitySimulation.FromJson(File.ReadAllText(path));
    clone.Advance(2);
    clone.CheckInvariants();
    var json = clone.SaveToJson();
    if (CitySimulation.FromJson(json).SaveToJson() != json)
        throw new InvalidOperationException("Warm-up save/load mismatch.");
}
Warm(source);
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
var sim = CitySimulation.FromJson(File.ReadAllText(source));
using var process = Process.GetCurrentProcess();
var initialNow = sim.State.Now;
var initialElapsedUnits = sim.State.ElapsedSecondsUnits;
long initialHistory = sim.State.Citizens.Sum(c => (long)c.History.Count);
long lastHistory = initialHistory;
long previousElapsed = initialElapsedUnits;

static string F(double x) => x.ToString("F3", CultureInfo.InvariantCulture);

void Sample(int index)
{
    sim.CheckInvariants();
    if (mode == "retained")
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
    var managed = GC.GetTotalMemory(false);
    var info = GC.GetGCMemoryInfo();
    var history = sim.State.Citizens.Sum(c => (long)c.History.Count);
    if (history < lastHistory)
        throw new InvalidOperationException("Personal history unexpectedly shrank.");
    lastHistory = history;
    if (sim.State.ElapsedSecondsUnits < previousElapsed)
        throw new InvalidOperationException("Elapsed time moved backward.");
    previousElapsed = sim.State.ElapsedSecondsUnits;
    var save = sim.SaveToJson();
    if (CitySimulation.FromJson(save).SaveToJson() != save)
        throw new InvalidOperationException("Full save/load roundtrip mismatch.");
    var file = Path.Combine(output, $"checkpoint-{index}.json");
    File.WriteAllText(file, save);
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(save)));
    Console.WriteLine($"PR57_184_SAMPLE case={label} mode={mode} index={index} " +
        $"elapsedUnits={sim.State.ElapsedSecondsUnits} alive={sim.State.Citizens.Count(c => c.Alive)} " +
        $"citizens={sim.State.Citizens.Count} vehicles={sim.State.Vehicles.Count} " +
        $"buildings={sim.State.Buildings.Count} projects={sim.State.Projects.Count} " +
        $"history={history} ledger={sim.State.Ledger.Count} cityEvents={sim.State.CityEvents.Count} " +
        $"saveBytes={Encoding.UTF8.GetByteCount(save)} managedObservedBytes={managed} " +
        $"heapBytes={info.HeapSizeBytes} fragmentedBytes={info.FragmentedBytes} " +
        $"committedBytes={info.TotalCommittedBytes} workingSetBytes={process.WorkingSet64} " +
        $"privateBytes={process.PrivateMemorySize64} sha256={hash} " +
        $"forcedGC={(mode == "retained" ? 1 : 0)} parity=SAVE_LOAD_INVARIANTS");
}
Sample(0);
var window = steps / 4;
for (var index = 1; index <= 4; index++)
{
    var alloc = GC.GetTotalAllocatedBytes(true);
    var cpu = process.TotalProcessorTime;
    var g0 = GC.CollectionCount(0);
    var g1 = GC.CollectionCount(1);
    var g2 = GC.CollectionCount(2);
    var sw = Stopwatch.StartNew();
    for (var step = 0; step < window; step++)
        sim.Advance(2);
    sw.Stop();
    var cpuMs = (process.TotalProcessorTime - cpu).TotalMilliseconds;
    var allocated = GC.GetTotalAllocatedBytes(true) - alloc;
    var delta0 = GC.CollectionCount(0) - g0;
    var delta1 = GC.CollectionCount(1) - g1;
    var delta2 = GC.CollectionCount(2) - g2;
    Console.WriteLine($"PR57_184_WINDOW case={label} mode={mode} index={index} " +
        $"steps={window} simSeconds={window * 2} wallMs={F(sw.Elapsed.TotalMilliseconds)} " +
        $"cpuMs={F(cpuMs)} allocBytes={allocated} gc0={delta0} gc1={delta1} gc2={delta2} " +
        "scope=ADVANCE_2_ONLY_NO_SAVE_OR_FORCED_GC_IN_TIMED_WINDOW");
    Sample(index);
}
if (sim.State.Now != initialNow.AddSeconds(steps * 2) ||
    sim.State.ElapsedSecondsUnits != initialElapsedUnits + steps * 2_000_000L)
    throw new InvalidOperationException("Advance(2) chronology mismatch.");
Console.WriteLine($"PR57_184_DONE case={label} mode={mode} steps={steps} " +
    $"initialHistory={initialHistory} finalHistory={lastHistory} " +
    "parity=SAVE_LOAD_INVARIANTS cadence=ADVANCE_2");
