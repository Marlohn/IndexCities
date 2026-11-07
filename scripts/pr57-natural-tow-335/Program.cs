using System.Diagnostics;
using System.Security.Cryptography;
using IndexCities.Simulation;

// #335: authentic economy REAL 14h. No synthetic actors or altered rules.
// Tow phases are observed only after each public Advance(2); no intra-call proof.
if(args.Length!=2||!File.Exists(args[0]))throw new ArgumentException("Expected authentic REAL save and output directory.");
var input=File.ReadAllBytes(args[0]);
var inputHash=Convert.ToHexString(SHA256.HashData(input));
const string expectedHash="779C144F2D6B2B64ECE079F5740BF8A0A473A47286CA40D8B5F6B9DD61AB616F";
if(inputHash!=expectedHash)throw new InvalidOperationException("Unexpected input; no substituted city.");
Directory.CreateDirectory(args[1]);
var sim=CitySimulation.FromJson(System.Text.Encoding.UTF8.GetString(input));
var initialUnits=sim.State.ElapsedSecondsUnits;
var initialNow=sim.State.Now;
const int maxCalls=10800; // Six simulated hours, two seconds each.
using var process=Process.GetCurrentProcess();
var cpu0=process.TotalProcessorTime;
var bytes0=GC.GetTotalAllocatedBytes(true);
var clock=Stopwatch.StartNew();
int? towId=null,targetId=null;
TowPhase? previousPhase=null;
int events=0,calls=0;
bool phaseEnded=false;
void Checkpoint(int call,string reason)
{
    sim.CheckInvariants();
    var json=sim.SaveToJson();
    if(CitySimulation.FromJson(json).SaveToJson()!=json)throw new InvalidOperationException("Save/load mismatch at "+call);
    File.WriteAllText(Path.Combine(args[1],$"checkpoint-{call:D5}.json"),json);
    var hash=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
    Console.WriteLine($"PR57_335_CHECKPOINT call={call} reason={reason} sha256={hash} parity=INVARIANTS_SAVE_LOAD");
}
if(sim.State.Vehicles.Any(v=>v.TowPhase!=TowPhase.None||v.TowTruckId!=null||v.Status==VehicleStatus.Towed))
    throw new InvalidOperationException("Expected no active tow in the authentic initial checkpoint.");
Console.WriteLine($"PR57_335_START sha256={inputHash} now={initialNow:O} vehicles={sim.State.Vehicles.Count} maxCalls={maxCalls} cadence=ADVANCE_2");
for(var call=1;call<=maxCalls;call++)
{
    sim.Advance(2);
    calls=call;
    if(sim.State.ElapsedSecondsUnits!=initialUnits+2_000_000L*call||sim.State.Now!=initialNow.AddSeconds(2*call))
        throw new InvalidOperationException("Clock/cadence mismatch at "+call);
    if(towId==null)
    {
        var first=sim.State.Vehicles.Where(v=>v.TowPhase!=TowPhase.None).OrderBy(v=>v.Id).FirstOrDefault();
        if(first!=null)
        {
            towId=first.Id;targetId=first.TowTargetId;previousPhase=first.TowPhase;events++;
            Console.WriteLine($"PR57_335_TOW call={call} tow={towId} target={targetId} phase={first.TowPhase} kind=FIRST_BOUNDARY");
            Checkpoint(call,"first-tow");
        }
    }
    else
    {
        var tow=sim.State.Vehicles.FirstOrDefault(v=>v.Id==towId);
        var target=sim.State.Vehicles.FirstOrDefault(v=>v.Id==targetId);
        var phase=tow?.TowPhase??TowPhase.None;
        if(phase!=previousPhase)
        {
            previousPhase=phase;events++;
            Console.WriteLine($"PR57_335_TOW call={call} tow={towId} target={targetId} phase={phase} targetStatus={target?.Status} broken={target?.BrokenDown} kind=BOUNDARY_TRANSITION");
            Checkpoint(call,"tow-phase");
        }
        if(phase==TowPhase.None){phaseEnded=true;break;}
    }
    if(call%1800==0)
        Console.WriteLine($"PR57_335_PROGRESS call={call} activeTow={sim.State.Vehicles.Count(v=>v.TowPhase!=TowPhase.None)} broken={sim.State.Vehicles.Count(v=>v.BrokenDown)} vehicles={sim.State.Vehicles.Count}");
}
Checkpoint(calls,"final");
clock.Stop();
Console.WriteLine($"PR57_335_SUMMARY calls={calls} simulatedSeconds={2*calls} firstTow={towId?.ToString()??"NONE"} target={targetId?.ToString()??"NONE"} phaseEnded={phaseEnded} boundaryEvents={events} wallMs={clock.Elapsed.TotalMilliseconds:F3} cpuMs={(process.TotalProcessorTime-cpu0).TotalMilliseconds:F3} allocatedBytes={GC.GetTotalAllocatedBytes(true)-bytes0} now={sim.State.Now:O} scope=OBSERVATIONAL_AUTHENTIC_REAL_NO_THROUGHPUT_AB");
