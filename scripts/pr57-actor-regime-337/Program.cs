using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;

// #337: actor-boundary cost census, not an executor or proof of write independence.
// Requires PR57_VEHICLE_COST_PROBE=1. The original engine executes every slice.
if (args.Length != 2 || !File.Exists(args[0]))
    throw new ArgumentException("Expected authentic REAL checkpoint and output directory.");
const string expectedInput = "779C144F2D6B2B64ECE079F5740BF8A0A473A47286CA40D8B5F6B9DD61AB616F";
const string expectedFinal = "E92293FAF61A6EA2AAB7DD0FDAC83C14BD4668F611A126C701DC6705587BB9FA";
if (Environment.GetEnvironmentVariable("PR57_VEHICLE_COST_PROBE") != "1")
    throw new InvalidOperationException("Actor census flag must be enabled.");
var input = File.ReadAllBytes(args[0]);
if (Convert.ToHexString(SHA256.HashData(input)) != expectedInput)
    throw new InvalidOperationException("Only the pinned authentic REAL checkpoint is permitted.");
Directory.CreateDirectory(args[1]);
var sim = CitySimulation.FromJson(Encoding.UTF8.GetString(input));
sim.CostProfile = new StepCostProfile();
var initialUnits = sim.State.ElapsedSecondsUnits;
var initialTime = sim.State.Now;
using var process = Process.GetCurrentProcess();
string[] groups = ["operations","nontravelling","moved","queue","junction","waiting","stationary-trip","other"];
Console.WriteLine($"PR57_337_START sha={expectedInput} now={initialTime:O} living={sim.State.Citizens.Count(c=>c.Alive)} vehicles={sim.State.Vehicles.Count} scope=AUTHENTIC_REAL_ACTOR_BOUNDARY_INTRUSIVE");
for (var hour=1;hour<=6;hour++)
{
    var before = sim.Pr57298VehicleCostSnapshot();
    var cpu0 = process.TotalProcessorTime;
    var alloc0 = GC.GetTotalAllocatedBytes(true);
    var clock = Stopwatch.StartNew();
    for (var i=1;i<=1800;i++)
    {
        sim.Advance(2);
        var call=(hour-1)*1800+i;
        if (sim.State.ElapsedSecondsUnits!=initialUnits+2_000_000L*call ||
            sim.State.Now!=initialTime.AddSeconds(2*call))
            throw new InvalidOperationException("Clock/cadence mismatch at "+call);
    }
    clock.Stop();
    var after=sim.Pr57298VehicleCostSnapshot();
    if (after.Length!=41 || before.Length!=41)
        throw new InvalidOperationException("Unexpected #298 snapshot layout.");
    var slices=after[0]-before[0];
    long actorEvaluations=0, actorBytes=0, claimChanges=0, spaceChanges=0;
    for (var g=0;g<8;g++)
    {
        var k=1+5*g;
        var calls=after[k]-before[k];
        var ticks=after[k+1]-before[k+1];
        var bytes=after[k+2]-before[k+2];
        var claims=after[k+3]-before[k+3];
        var spaces=after[k+4]-before[k+4];
        actorEvaluations+=calls; actorBytes+=bytes; claimChanges+=claims; spaceChanges+=spaces;
        Console.WriteLine($"PR57_337_GROUP hour={hour} group={groups[g]} calls={calls} actorMs={ticks*1000.0/Stopwatch.Frequency:F3} allocatedBytes={bytes} claimChanges={claims} spaceCountChanges={spaces}");
    }
    if(slices!=12600 || actorEvaluations!=slices*sim.State.Vehicles.Count)
        throw new InvalidOperationException($"Incomplete actor census: slices={slices} actors={actorEvaluations}");
    Console.WriteLine($"PR57_337_HOUR hour={hour} slices={slices} actors={actorEvaluations} actorAllocatedBytes={actorBytes} claimChanges={claimChanges} spaceCountChanges={spaceChanges} wallMs={clock.Elapsed.TotalMilliseconds:F3} cpuMs={(process.TotalProcessorTime-cpu0).TotalMilliseconds:F3} totalAllocatedBytes={GC.GetTotalAllocatedBytes(true)-alloc0} living={sim.State.Citizens.Count(c=>c.Alive)} ledger={sim.State.Ledger.Count} history={sim.State.Citizens.Sum(c=>(long)c.History.Count)} scope=POST_ACTOR_NET_CHANGES_NOT_WRITER_JOURNAL");
}
sim.CostProfile=null;
sim.CheckInvariants();
var finalJson=sim.SaveToJson();
if(CitySimulation.FromJson(finalJson).SaveToJson()!=finalJson)
    throw new InvalidOperationException("Save/load roundtrip mismatch.");
var sha=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(finalJson)));
File.WriteAllText(Path.Combine(args[1],"checkpoint-10800.json"),finalJson);
Console.WriteLine($"PR57_337_FINAL sha256={sha} expected335336={expectedFinal} parity={(sha==expectedFinal?"IDENTICAL":"MISMATCH")} scope=OBSERVATIONAL_NO_THROUGHPUT_AB");
if(sha!=expectedFinal)throw new InvalidOperationException("Canonical final checkpoint diverged.");
