using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;

// #336: authentic REAL 14h save, every Advance(2), no state/rule replacement.
// Allocation/timing probes and per-boundary physical signatures are intrusive
// OBSERVATION. They cannot certify causal independence inside an Advance(2).
if (args.Length != 2 || !File.Exists(args[0]))
    throw new ArgumentException("Expected authentic checkpoint and output directory.");
var input = File.ReadAllBytes(args[0]);
const string inputSha = "779C144F2D6B2B64ECE079F5740BF8A0A473A47286CA40D8B5F6B9DD61AB616F";
const string expectedFinalSha = "E92293FAF61A6EA2AAB7DD0FDAC83C14BD4668F611A126C701DC6705587BB9FA";
if (Convert.ToHexString(SHA256.HashData(input)) != inputSha)
    throw new InvalidOperationException("Unexpected save; never substitute a synthetic city.");
Directory.CreateDirectory(args[1]);
var sim = CitySimulation.FromJson(Encoding.UTF8.GetString(input));
var initialTime = sim.State.Now;
var initialUnits = sim.State.ElapsedSecondsUnits;
using var process = Process.GetCurrentProcess();
Console.WriteLine($"PR57_336_START input={inputSha} now={initialTime:O} vehicles={sim.State.Vehicles.Count} living={sim.State.Citizens.Count(c=>c.Alive)} cadence=ADVANCE_2 hours=6");
for (var hour = 1; hour <= 6; hour++)
{
    var profile = new StepCostProfile();
    sim.CostProfile = profile;
    var cpu0 = process.TotalProcessorTime;
    var alloc0 = GC.GetTotalAllocatedBytes(true);
    var clock = Stopwatch.StartNew();
    long poseChanges = 0, wearChanges = 0, statusChanges = 0;
    long routeChanges = 0, claimChanges = 0, towChanges = 0, newVehicles = 0;
    var poseIds = new HashSet<int>();
    var wearIds = new HashSet<int>();
    var physicalIds = new HashSet<int>();
    for (var i = 1; i <= 1800; i++)
    {
        // Boundary-only observation, after and before canonical Advance(2).
        // These snapshots do not enter the 13 phase clocks below.
        var before = sim.State.Vehicles.Select(Snapshot).ToDictionary(x=>x.Id);
        sim.Advance(2);
        foreach (var vehicle in sim.State.Vehicles)
        {
            var after = Snapshot(vehicle);
            if (!before.TryGetValue(after.Id, out var prior))
            {
                newVehicles++;
                physicalIds.Add(after.Id);
                continue;
            }
            var changed = false;
            if (prior.Position != after.Position || prior.Heading != after.Heading)
            {
                poseChanges++;
                poseIds.Add(after.Id);
                changed = true;
            }
            if (prior.ConditionUnits != after.ConditionUnits)
            {
                wearChanges++;
                wearIds.Add(after.Id);
                changed = true;
            }
            if (prior.Status != after.Status) { statusChanges++; changed = true; }
            if (prior.RouteIndex != after.RouteIndex) { routeChanges++; changed = true; }
            if (prior.IntersectionClaim != after.IntersectionClaim) { claimChanges++; changed = true; }
            if (prior.TowPhase != after.TowPhase || prior.TowTruckId != after.TowTruckId ||
                prior.TowTargetId != after.TowTargetId) { towChanges++; changed = true; }
            if (changed) physicalIds.Add(after.Id);
        }
        var call = (hour-1)*1800+i;
        if (sim.State.ElapsedSecondsUnits != initialUnits+2_000_000L*call ||
            sim.State.Now != initialTime.AddSeconds(2*call))
            throw new InvalidOperationException("Clock/cadence mismatch at call "+call);
    }
    clock.Stop();
    if (profile.Steps != 1800 || profile.PhaseAllocationSteps != 1800)
        throw new InvalidOperationException("Phase census missing Step(2) observations.");
    var totalAlloc = GC.GetTotalAllocatedBytes(true)-alloc0;
    var phaseAlloc = profile.PhaseAllocatedBytes.Sum();
    var phaseMs = profile.Ticks.Sum()*1000.0/Stopwatch.Frequency;
    Console.WriteLine($"PR57_336_HOUR hour={hour} wallMs={clock.Elapsed.TotalMilliseconds:F3} cpuMs={(process.TotalProcessorTime-cpu0).TotalMilliseconds:F3} totalAllocBytes={totalAlloc} phaseAllocBytes={phaseAlloc} phaseMs={phaseMs:F3} phaseSteps={profile.Steps} living={sim.State.Citizens.Count(c=>c.Alive)} vehicles={sim.State.Vehicles.Count} ledger={sim.State.Ledger.Count} history={sim.State.Citizens.Sum(c=>(long)c.History.Count)}");
    Console.WriteLine($"PR57_336_BOUNDARY hour={hour} poseEvents={poseChanges} poseIds={poseIds.Count} wearEvents={wearChanges} wearIds={wearIds.Count} statusEvents={statusChanges} routeEvents={routeChanges} claimEvents={claimChanges} towEvents={towChanges} newVehicles={newVehicles} anyPhysicalIds={physicalIds.Count} scope=NET_PUBLIC_BOUNDARY_NOT_INTRA_STEP");
    for (var p = 0; p < StepCostProfile.Names.Length; p++)
        Console.WriteLine($"PR57_336_PHASE hour={hour} phase={StepCostProfile.Names[p]} ms={profile.Ticks[p]*1000.0/Stopwatch.Frequency:F3} allocatedBytes={profile.PhaseAllocatedBytes[p]}");
}
sim.CostProfile = null;
sim.CheckInvariants();
var finalJson = sim.SaveToJson();
if (CitySimulation.FromJson(finalJson).SaveToJson() != finalJson)
    throw new InvalidOperationException("Save/load mismatch.");
var finalSha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(finalJson)));
File.WriteAllText(Path.Combine(args[1],"checkpoint-10800.json"),finalJson);
Console.WriteLine($"PR57_336_FINAL sha256={finalSha} expected335={expectedFinalSha} parity={(finalSha==expectedFinalSha?"IDENTICAL":"MISMATCH")} scope=AUTHENTIC_REAL_OBSERVATIONAL_NO_THROUGHPUT_AB");
if (finalSha != expectedFinalSha)
    throw new InvalidOperationException("Final save diverged from authentic #335 control.");
static VehicleMark Snapshot(Vehicle v) => new(v.Id,v.Position,v.Heading,v.ConditionUnits,v.Status,
    v.RouteIndex,v.IntersectionClaim,v.TowPhase,v.TowTruckId,v.TowTargetId);
readonly record struct VehicleMark(int Id, Point Position, double Heading, long ConditionUnits,
    VehicleStatus Status, int RouteIndex, int? IntersectionClaim, TowPhase TowPhase,
    int? TowTruckId, int? TowTargetId);
