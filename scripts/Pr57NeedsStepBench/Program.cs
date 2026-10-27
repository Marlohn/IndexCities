using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;

// PR57 #190: exact values read by UpdateNeeds on each real Advance(2).
// The observer is non-serialized and never influences any simulation decision.
// Timings are INTRUSIVE diagnostics, not an optimization A/B.
if (args.Length != 5 || !File.Exists(args[0]))
    throw new ArgumentException("Expected authentic-save.json case control|probe steps outputDir.");
var source = args[0];
var label = args[1];
var mode = args[2];
if (mode is not ("control" or "probe"))
    throw new ArgumentException("Expected control or probe mode.");
if (!int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var steps)
    || steps < 2 || steps % 2 != 0)
    throw new ArgumentException("Steps must be even and >= 2.");
var output = args[4];
Directory.CreateDirectory(output);
var input = File.ReadAllText(source);
var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
var warm = CitySimulation.FromJson(input);
warm.Advance(2);
warm.CheckInvariants();
var sim = CitySimulation.FromJson(input);
var profile = new StepCostProfile();
sim.CostProfile = profile;
var probe = mode == "probe" ? new Pr57NeedsStepProbe() : null;
sim.Pr57NeedsStepProbe = probe;
using var process = Process.GetCurrentProcess();
var initialNow = sim.State.Now;
var initialUnits = sim.State.ElapsedSecondsUnits;
static string F(double value) => value.ToString("F3", CultureInfo.InvariantCulture);
void Save(int index)
{
    sim.CheckInvariants();
    var json = sim.SaveToJson();
    if (CitySimulation.FromJson(json).SaveToJson() != json)
        throw new InvalidOperationException("Full save/load roundtrip mismatch.");
    File.WriteAllText(Path.Combine(output, $"checkpoint-{index}.json"), json);
    Console.WriteLine($"PR57_190_CHECKPOINT case={label} mode={mode} index={index} " +
        $"sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))} " +
        $"alive={sim.State.Citizens.Count(c => c.Alive)} bytes={Encoding.UTF8.GetByteCount(json)} " +
        "parity=SAVE_LOAD_INVARIANTS");
}
Save(0);
var allocatedBefore = GC.GetTotalAllocatedBytes(true);
var cpuBefore = process.TotalProcessorTime;
var timer = Stopwatch.StartNew();
for (var step = 1; step <= steps; step++)
{
    sim.Advance(2);
    if (step == steps / 2)
    {
        timer.Stop();
        Save(1);
        timer.Start();
    }
}
timer.Stop();
var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
var cpuMs = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
Save(2);
if (sim.State.Now != initialNow.AddSeconds(steps * 2)
    || sim.State.ElapsedSecondsUnits != initialUnits + steps * 2_000_000L
    || profile.Steps != steps)
    throw new InvalidOperationException("Advance(2) chronology or profile mismatch.");
Console.WriteLine($"PR57_190_COST case={label} mode={mode} steps={steps} " +
    $"wallMs={F(timer.Elapsed.TotalMilliseconds)} cpuMs={F(cpuMs)} allocBytes={allocated} " +
    $"infraMs={F(profile.Ticks[6] * 1000.0 / Stopwatch.Frequency)} " +
    $"needsMs={F(profile.Ticks[8] * 1000.0 / Stopwatch.Frequency)} " +
    $"vehiclesMs={F(profile.Ticks[0] * 1000.0 / Stopwatch.Frequency)} " +
    "scope=INTRUSIVE_OBSERVATION_NOT_THROUGHPUT_AB");
if (probe is not null)
{
    if (probe.Records.Count != steps)
        throw new InvalidOperationException("Missing needs input Step observations.");
    foreach (var row in probe.Records)
        Console.WriteLine($"PR57_190_STEP case={label} step={row.Step} citizens={row.Citizens} " +
            $"new={row.NewCitizens} missing={row.MissingCitizens} " +
            $"sameHome={row.SameHomeInputs} changedHome={row.ChangedHomeInputs} " +
            $"water={row.ChangedWater} electricity={row.ChangedElectricity} " +
            $"unmet={row.ChangedUnmet} hunger={row.ChangedHunger} " +
            $"pantry={row.ChangedPantry} movement={row.ChangedMovement} " +
            $"other={row.ChangedOtherNeedsInputs}");
    // Step 1 has no previous observation and is excluded from the transition totals.
    var comparable = probe.Records.Skip(1).ToArray();
    long Sum(Func<Pr57NeedsStepProbe.StepRecord, int> pick) => comparable.Sum(r => (long)pick(r));
    Console.WriteLine($"PR57_190_SUMMARY case={label} steps={steps} " +
        $"comparable={Sum(r => r.SameHomeInputs + r.ChangedHomeInputs)} " +
        $"sameHome={Sum(r => r.SameHomeInputs)} changedHome={Sum(r => r.ChangedHomeInputs)} " +
        $"water={Sum(r => r.ChangedWater)} electricity={Sum(r => r.ChangedElectricity)} " +
        $"unmet={Sum(r => r.ChangedUnmet)} hunger={Sum(r => r.ChangedHunger)} " +
        $"pantry={Sum(r => r.ChangedPantry)} movement={Sum(r => r.ChangedMovement)} " +
        $"other={Sum(r => r.ChangedOtherNeedsInputs)} " +
        $"new={Sum(r => r.NewCitizens)} missing={Sum(r => r.MissingCitizens)} " +
        "scope=EXACT_UPDATE_NEEDS_INPUTS_PER_STEP_NOT_SKIP_ELIGIBILITY");
}
Console.WriteLine($"PR57_190_DONE case={label} mode={mode} sourceSha256={sourceHash} " +
    $"steps={steps} cadence=ADVANCE_2 parity=SAVE_LOAD_INVARIANTS");
