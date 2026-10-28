using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;

// PR57 #252: isolated .NET persistence-cost observation on genuine saved cities.
// Every simulated quantum calls Advance(2); saving never replaces simulation.
if (args.Length != 4 || !File.Exists(args[0]))
    throw new ArgumentException("Usage: <authentic-save.json> <steps> <save-every-N-steps-or-0> <output.json>");
var steps = int.Parse(args[1], CultureInfo.InvariantCulture);
var saveEvery = int.Parse(args[2], CultureInfo.InvariantCulture);
if (steps <= 0 || saveEvery < 0 || saveEvery > steps)
    throw new ArgumentOutOfRangeException(nameof(args), "Invalid step count or save interval.");
var source = File.ReadAllText(args[0], Encoding.UTF8);
var simulation = CitySimulation.FromJson(source);
var now = simulation.State.Now;
var alive = simulation.State.Citizens.Count(c => c.Alive);
var vehicles = simulation.State.Vehicles.Count;
var history = simulation.State.Citizens.Sum(c => c.History.Count);
var ledger = simulation.State.Ledger.Count;

// JIT/warmup outside all measurements; never mutate the measured state.
var warm = CitySimulation.FromJson(source);
warm.Advance(2);
var warmSave = warm.SaveToJson();
_ = CitySimulation.FromJson(warmSave);
const int serializationRepeats = 6;
var initialSave = Timed(() =>
{
    for (var i = 0; i < serializationRepeats; i++) _ = simulation.SaveToJson();
});
var initialLoad = Timed(() =>
{
    for (var i = 0; i < serializationRepeats; i++) _ = CitySimulation.FromJson(source);
});

long periodicTicks = 0;
long periodicBytes = 0;
var periodicCount = 0;
var loop = Timed(() =>
{
    for (var i = 0; i < steps; i++)
    {
        simulation.Advance(2);
        if (saveEvery > 0 && (i + 1) % saveEvery == 0)
        {
            var started = Stopwatch.GetTimestamp();
            var snapshot = simulation.SaveToJson();
            periodicTicks += Stopwatch.GetTimestamp() - started;
            periodicBytes += Encoding.UTF8.GetByteCount(snapshot);
            periodicCount++;
        }
    }
});
if (simulation.State.Now != now.AddSeconds(2.0 * steps))
    throw new InvalidOperationException("Advance(2) chronology changed.");
if (simulation.State.Citizens.Count(c => c.Alive) != alive)
    throw new InvalidOperationException("Alive count changed unexpectedly in short observation.");
simulation.CheckInvariants();
var final = simulation.SaveToJson();
var restored = CitySimulation.FromJson(final);
restored.CheckInvariants();
if (restored.SaveToJson() != final)
    throw new InvalidOperationException("Final save/load/save JSON roundtrip mismatch.");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
File.WriteAllText(args[3], final, new UTF8Encoding(false));
var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(final))).ToLowerInvariant();
static string F(double value) => value.ToString("F3", CultureInfo.InvariantCulture);
Console.WriteLine($"PR57_252_METRIC source={Path.GetFileName(args[0])} steps={steps} quantum=2 interval={saveEvery} " +
    $"alive={alive} vehicles={vehicles} historyStart={history} historyEnd={simulation.State.Citizens.Sum(c => c.History.Count)} " +
    $"ledgerStart={ledger} ledgerEnd={simulation.State.Ledger.Count} " +
    $"saveRepeats={serializationRepeats} saveBenchWallMs={F(initialSave.WallMs)} saveBenchCpuMs={F(initialSave.CpuMs)} saveBenchAllocBytes={initialSave.AllocBytes} " +
    $"loadBenchWallMs={F(initialLoad.WallMs)} loadBenchCpuMs={F(initialLoad.CpuMs)} loadBenchAllocBytes={initialLoad.AllocBytes} " +
    $"loopWallMs={F(loop.WallMs)} loopCpuMs={F(loop.CpuMs)} loopAllocBytes={loop.AllocBytes} " +
    $"autosaves={periodicCount} periodicSaveWallMs={F(periodicTicks * 1000.0 / Stopwatch.Frequency)} periodicBytes={periodicBytes} " +
    $"finalBytes={Encoding.UTF8.GetByteCount(final)} finalSha256={digest} invariant=PASS roundtrip=PASS");
static (double WallMs, double CpuMs, long AllocBytes) Timed(Action action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var allocated = GC.GetAllocatedBytesForCurrentThread();
    var cpu = Process.GetCurrentProcess().TotalProcessorTime;
    var sw = Stopwatch.StartNew();
    action();
    sw.Stop();
    return (sw.Elapsed.TotalMilliseconds,
        (Process.GetCurrentProcess().TotalProcessorTime - cpu).TotalMilliseconds,
        GC.GetAllocatedBytesForCurrentThread() - allocated);
}
