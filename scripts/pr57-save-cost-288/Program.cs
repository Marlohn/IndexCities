// PR57 #288 — isolated, disposable serialization-cost counterfactual.
// No simulation Step, product save format, or persistence implementation is modified.
// The stripped state is NEVER passed off as a valid product save.
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IndexCities.Simulation;

if (args.Length != 3 || args[1] is not ("ABBA" or "BAAB") ||
    !int.TryParse(args[2], out var repetitions) || repetitions is < 2 or > 30)
{
    Console.Error.WriteLine("usage: Pr57SaveCost288 <authentic-checkpoint.json> ABBA|BAAB <repetitions:2..30>");
    return 2;
}
var path = args[0];
var order = args[1];
var original = File.ReadAllText(path, Encoding.UTF8);
var options = new JsonSerializerOptions { WriteIndented = false };
options.Converters.Add(new JsonStringEnumConverter());

var sim = CitySimulation.FromJson(original);
var full = sim.State;
var fullJson = JsonSerializer.Serialize(full, options);
if (!string.Equals(original, fullJson, StringComparison.Ordinal) ||
    !string.Equals(original, sim.SaveToJson(), StringComparison.Ordinal))
    throw new InvalidOperationException("Full checkpoint did not roundtrip byte-for-byte; abort comparison.");

var stripped = JsonSerializer.Deserialize<CityState>(original, options)
    ?? throw new InvalidOperationException("Cannot create disposable state copy.");
var historyRecords = stripped.Citizens.Sum(c => c.History.Count);
var ledgerRecords = stripped.Ledger.Count;
var cityEventRecords = stripped.CityEvents.Count;
foreach (var citizen in stripped.Citizens) citizen.History.Clear();
stripped.Ledger.Clear();
stripped.CityEvents.Clear();
var strippedJson = JsonSerializer.Serialize(stripped, options);
if (stripped.Citizens.Any(c => c.History.Count != 0) ||
    stripped.Ledger.Count != 0 || stripped.CityEvents.Count != 0)
    throw new InvalidOperationException("Counterfactual copy retained historical arrays.");
if (!string.Equals(original, sim.SaveToJson(), StringComparison.Ordinal))
    throw new InvalidOperationException("Original state changed while creating the disposable copy.");

var sourceSha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original)));
var strippedSha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(strippedJson)));
Console.WriteLine(FormattableString.Invariant(
    $"PR57_288_SOURCE path={path} order={order} reps={repetitions} citizens={full.Citizens.Count} alive={full.Citizens.Count(c => c.Alive)} historyRecords={historyRecords} ledgerRecords={ledgerRecords} cityEventRecords={cityEventRecords} fullBytes={Encoding.UTF8.GetByteCount(original)} strippedBytes={Encoding.UTF8.GetByteCount(strippedJson)} fullSha256={sourceSha} strippedSha256={strippedSha} fullByteRoundtrip=True counterfactualOnly=True"));

static long Serialize(CityState state, JsonSerializerOptions options)
    => JsonSerializer.Serialize(state, options).Length;

static long Deserialize(string json, JsonSerializerOptions options)
{
    var state = JsonSerializer.Deserialize<CityState>(json, options)
        ?? throw new InvalidOperationException("Null state from serializer.");
    return state.Citizens.Count + state.Ledger.Count + state.CityEvents.Count;
}

static void Measure(string op, string mode, string order, int block, int count, Func<long> action)
{
    // Warm the exact call path before measuring; a forced collection happens
    // outside the timed section and is not an optimization of the simulation.
    for (var i = 0; i < 3; i++) GC.KeepAlive(action());
    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    var beforeAlloc = GC.GetTotalAllocatedBytes(precise: true);
    var beforeCpu = Process.GetCurrentProcess().TotalProcessorTime;
    var gc0 = GC.CollectionCount(0);
    var gc2 = GC.CollectionCount(2);
    var sw = Stopwatch.StartNew();
    long checksum = 0;
    for (var i = 0; i < count; i++) checksum += action();
    sw.Stop();
    var cpuMs = (Process.GetCurrentProcess().TotalProcessorTime - beforeCpu).TotalMilliseconds;
    var allocBytes = GC.GetTotalAllocatedBytes(precise: true) - beforeAlloc;
    Console.WriteLine(FormattableString.Invariant(
        $"PR57_288_RESULT op={op} mode={mode} order={order} block={block} reps={count} wallMsPerOp={sw.Elapsed.TotalMilliseconds / count:F5} cpuMsPerOp={cpuMs / count:F5} allocBytesPerOp={allocBytes / (double)count:F1} gc0={GC.CollectionCount(0) - gc0} gc2={GC.CollectionCount(2) - gc2} checksum={checksum}"));
}

// ABBA/BAAB mitigates order bias, not the difference between .NET GC states.
// Every mode uses the SAME CityState schema and JsonSerializerOptions.
for (var block = 0; block < order.Length; block++)
{
    var mode = order[block] == 'A' ? "full" : "without-history";
    var state = mode == "full" ? full : stripped;
    var json = mode == "full" ? original : strippedJson;
    Measure("serialize", mode, order, block, repetitions, () => Serialize(state, options));
    Measure("deserialize", mode, order, block, repetitions, () => Deserialize(json, options));
}

// Public persistence costs are measured separately: SaveToJson validates
// invariants/geometry and FromJson performs schema/version checks.
// They are NOT compared with the stripped state as if it were a valid save.
Measure("public-SaveToJson", "full-only", order, 4, repetitions, () => sim.SaveToJson().Length);
Measure("public-FromJson", "full-only", order, 5, repetitions,
    () => CitySimulation.FromJson(original).State.Citizens.Count);

if (!string.Equals(original, sim.SaveToJson(), StringComparison.Ordinal))
    throw new InvalidOperationException("Canonical state changed during read-only probe.");
Console.WriteLine("PR57_288_PARITY originalSaveUnchanged=True noAdvance=True noProductSaveChanged=True " +
                  "notIntegratedThroughput=True not80YearProjection=True");
return 0;
