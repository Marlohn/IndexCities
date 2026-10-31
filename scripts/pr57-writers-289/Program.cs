// PR57 #289: authentic host-cadence append census with phase-cost context.
// Observational only: every Advance(2) and every canonical state mutation executes.
using System.Diagnostics;
using System.Globalization;
using System.Text;
using IndexCities.Simulation;

if(args.Length!=5 || args[2] is not ("control" or "probe") ||
   !int.TryParse(args[3],out var calls) || calls<25 || calls>500)
{
    Console.Error.WriteLine("usage: Pr57Writers289 <save.json> <case> control|probe <calls> <output-dir>");
    return 2;
}
var original=File.ReadAllText(args[0],Encoding.UTF8);
var sim=CitySimulation.FromJson(original);
sim.CostProfile=new StepCostProfile();
var caseName=args[1];
var mode=args[2];
var root=args[4];
Directory.CreateDirectory(root);
if(!string.Equals(original,sim.SaveToJson(),StringComparison.Ordinal))
    throw new InvalidOperationException("Source checkpoint not byte-identical after roundtrip");
void Checkpoint(int at)
{
    var json=sim.SaveToJson();
    var path=Path.Combine(root,$"checkpoint-{at}.json");
    File.WriteAllText(path,json,new UTF8Encoding(false));
    if(CitySimulation.FromJson(json).SaveToJson()!=json)
        throw new InvalidOperationException($"Checkpoint {at} does not roundtrip");
    var histories=sim.State.Citizens.Sum(x=>x.History.Count);
    Console.WriteLine(FormattableString.Invariant(
        $"PR57_289_CHECKPOINT case={caseName} mode={mode} calls={at} citizens={sim.State.Citizens.Count} alive={sim.State.Citizens.Count(x=>x.Alive)} historyRecords={histories} ledgerRecords={sim.State.Ledger.Count} cityEventRecords={sim.State.CityEvents.Count} bytes={Encoding.UTF8.GetByteCount(json)}"));
    foreach(var line in sim.Pr57Writer289Lines(at))Console.WriteLine(line);
}
Checkpoint(0);
var checkpoints=new HashSet<int>{20,calls/2,calls};
long[] previousPhase=new long[StepCostProfile.Names.Length];
var previous=0;
long windowWallTicks=0,windowAllocated=0;
var cpuStart=Process.GetCurrentProcess().TotalProcessorTime;
for(var i=1;i<=calls;i++)
{
    var allocatedBefore=GC.GetAllocatedBytesForCurrentThread();
    var started=Stopwatch.GetTimestamp();
    sim.Advance(2);
    windowWallTicks+=Stopwatch.GetTimestamp()-started;
    windowAllocated+=GC.GetAllocatedBytesForCurrentThread()-allocatedBefore;
    if(!checkpoints.Contains(i))continue;
    var cpu=Process.GetCurrentProcess().TotalProcessorTime-cpuStart;
    Console.WriteLine(FormattableString.Invariant(
        $"PR57_289_WINDOW case={caseName} mode={mode} from={previous} to={i} quantum=2 wallMs={windowWallTicks*1000.0/Stopwatch.Frequency:F4} cpuMs={cpu.TotalMilliseconds:F4} allocatedBytes={windowAllocated}"));
    for(var phase=0;phase<StepCostProfile.Names.Length;phase++)
    {
        var ticks=sim.CostProfile.Ticks[phase]-previousPhase[phase];
        previousPhase[phase]=sim.CostProfile.Ticks[phase];
        Console.WriteLine(FormattableString.Invariant(
            $"PR57_289_PHASE case={caseName} mode={mode} from={previous} to={i} name={StepCostProfile.Names[phase]} ms={ticks*1000.0/Stopwatch.Frequency:F4}"));
    }
    Checkpoint(i);
    previous=i;
    windowWallTicks=0;
    windowAllocated=0;
    cpuStart=Process.GetCurrentProcess().TotalProcessorTime;
}
Console.WriteLine($"PR57_289_FINAL case={caseName} mode={mode} calls={calls} fullSaveRoundtrip=True cadence=Advance2 observational=True");
return 0;
