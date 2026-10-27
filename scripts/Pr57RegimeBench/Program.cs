using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;

// PR57 #186: intrusive observation of the first GRID 400 simulated seconds.
// All 200 Advance(2) calls execute. No gameplay, scheduling or save changes.
// The phase allocation census is thread-local and enabled by PR57_PHASE_ALLOC_PROBE=1.
if (args.Length != 2 || !File.Exists(args[0]))
    throw new ArgumentException("Expected authentic-grid-checkpoint.json output-directory.");
if (Environment.GetEnvironmentVariable("PR57_PHASE_ALLOC_PROBE") != "1")
    throw new InvalidOperationException("PR57_PHASE_ALLOC_PROBE=1 is required.");
var source = args[0];
var output = args[1];
Directory.CreateDirectory(output);
var input = File.ReadAllText(source);
var inputHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
if (!inputHash.Equals("79F84B21A9A0DE9BB0BF5D07F9F93852C3DF43DC7B1A71B6C0F956BF648745B9",
    StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Unexpected GRID fixture: do not substitute another city.");
var warm = CitySimulation.FromJson(input);
warm.Advance(2);
warm.CheckInvariants();
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
var sim = CitySimulation.FromJson(input);
var profile = new StepCostProfile();
sim.CostProfile = profile;
using var process = Process.GetCurrentProcess();
var initialUnits = sim.State.ElapsedSecondsUnits;
var initialNow = sim.State.Now;
var previousSteps = 0;
var previousBytes = GC.GetTotalAllocatedBytes(true);
var previousCpu = process.TotalProcessorTime;
var previousTicks = Stopwatch.GetTimestamp();
var previousPhase = new long[StepCostProfile.Names.Length];
var previousHistory = sim.State.Citizens.Sum(c => (long)c.History.Count);
var previousVehicleTicks = new long[StepCostProfile.VehicleDetailNames.Length];
var previousDetailTicks = new long[StepCostProfile.DetailNames.Length];
var previousSlices = 0L;
var marks = new HashSet<int> { 1, 2, 5, 10, 25, 50, 75, 100, 150, 200 };
static string N(double n) => n.ToString("F3", CultureInfo.InvariantCulture);
void Save(int index)
{
    sim.CheckInvariants();
    var json = sim.SaveToJson();
    if (CitySimulation.FromJson(json).SaveToJson() != json)
        throw new InvalidOperationException("Save/load mismatch.");
    File.WriteAllText(Path.Combine(output, $"checkpoint-{index}.json"), json);
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    Console.WriteLine($"PR57_186_CHECKPOINT step={index} sha256={hash} bytes={Encoding.UTF8.GetByteCount(json)} parity=SAVE_LOAD_INVARIANTS");
}
Save(0);
for (var step = 1; step <= 200; step++)
{
    sim.Advance(2);
    if (!marks.Contains(step)) continue;
    var now = Stopwatch.GetTimestamp();
    var cpu = process.TotalProcessorTime;
    var bytes = GC.GetTotalAllocatedBytes(true);
    var phaseParts = new List<string>();
    long sumPhase = 0;
    for (var i = 0; i < StepCostProfile.Names.Length; i++)
    {
        var delta = profile.PhaseAllocatedBytes[i] - previousPhase[i];
        previousPhase[i] = profile.PhaseAllocatedBytes[i];
        sumPhase += delta;
        phaseParts.Add($"{StepCostProfile.Names[i]}:{delta}");
    }
    var vehicleParts = new List<string>();
    for (var i = 0; i < StepCostProfile.VehicleDetailNames.Length; i++)
    {
        var delta = profile.VehicleTicks[i] - previousVehicleTicks[i];
        previousVehicleTicks[i] = profile.VehicleTicks[i];
        vehicleParts.Add($"{StepCostProfile.VehicleDetailNames[i]}:{N(delta * 1000.0 / Stopwatch.Frequency)}");
    }
    var detailParts = new List<string>();
    for (var i = 0; i < StepCostProfile.DetailNames.Length; i++)
    {
        var delta = profile.DetailTicks[i] - previousDetailTicks[i];
        previousDetailTicks[i] = profile.DetailTicks[i];
        detailParts.Add($"{StepCostProfile.DetailNames[i]}:{N(delta * 1000.0 / Stopwatch.Frequency)}");
    }
    var history = sim.State.Citizens.Sum(c => (long)c.History.Count);
    var vehicles = sim.State.Vehicles;
    var citizens = sim.State.Citizens;
    var elapsedMs = (now - previousTicks) * 1000.0 / Stopwatch.Frequency;
    var totalAllocated = bytes - previousBytes;
    Console.WriteLine($"PR57_186_SEGMENT fromStep={previousSteps} toStep={step} simSeconds={(step-previousSteps)*2} " +
        $"wallMs={N(elapsedMs)} cpuMs={N((cpu-previousCpu).TotalMilliseconds)} " +
        $"allocatedBytes={totalAllocated} phaseAllocatedBytes={sumPhase} " +
        $"historyAdded={history-previousHistory} travelling={vehicles.Count(v => v.Status == VehicleStatus.Travelling)} " +
        $"routeNodes={vehicles.Sum(v => (long)v.Route.Count)} " +
        $"citizenPaths={citizens.Count(c => c.Path.Count > 0)} " +
        $"pathNodes={citizens.Sum(c => (long)c.Path.Count)} " +
        $"vehicleSlices={profile.VehicleSlices-previousSlices} " +
        $"phaseBytes={string.Join(",", phaseParts)} vehicleMs={string.Join(",", vehicleParts)} " +
        $"infraNeedsMs={string.Join(",", detailParts)} " +
        "scope=INTRUSIVE_THREAD_LOCAL_PHASE_ALLOCATION_NOT_THROUGHPUT_AB");
    previousSteps = step;
    previousTicks = Stopwatch.GetTimestamp();
    previousCpu = process.TotalProcessorTime;
    previousBytes = GC.GetTotalAllocatedBytes(true);
    previousHistory = history;
    previousSlices = profile.VehicleSlices;
    if (step % 50 == 0) Save(step);
}
if (sim.State.Now != initialNow.AddSeconds(400) ||
    sim.State.ElapsedSecondsUnits != initialUnits + 400_000_000L ||
    profile.Steps != 200 || profile.PhaseAllocationSteps != 200)
    throw new InvalidOperationException("Incomplete Advance(2) or phase allocation census.");
Console.WriteLine($"PR57_186_DONE steps=200 inputSha256={inputHash} cadence=ADVANCE_2 parity=SAVE_LOAD_INVARIANTS");
