// PR57 #294: observational per-Advance(2) economy-review phase cost.
// Uses the original 13-phase StepCostProfile and unmodified CitySimulation.
// A changed EconomyNextAt is an observed review schedule update, not a
// claim that all review work is cold/JIT-free or that other phases are inert.
using System.Diagnostics;
using System.Text;
using IndexCities.Simulation;

if (args.Length != 5 || args[2] is not ("control" or "probe") ||
    !int.TryParse(args[3], out var calls) || calls < 25 || calls > 500)
{
    Console.Error.WriteLine("usage: Pr57EconomyReview294 <save.json> <case> control|probe <calls> <output-dir>");
    return 2;
}
var source = File.ReadAllText(args[0], Encoding.UTF8);
var sim = CitySimulation.FromJson(source);
if (sim.SaveToJson() != source)
    throw new InvalidOperationException("Source checkpoint failed exact save/load roundtrip");
sim.CostProfile = new StepCostProfile();
var caseName = args[1];
var mode = args[2];
var trace = mode == "probe";
var output = args[4];
Directory.CreateDirectory(output);
var checkpoints = new HashSet<int> { 20, calls / 2, calls };
var startedProcess = Stopwatch.GetTimestamp();
var allocatedProcess = GC.GetTotalAllocatedBytes();
var cpuProcess = Process.GetCurrentProcess().TotalProcessorTime;
var reviewSteps = 0;
var reviewTicks = 0L;
var nonReviewTicks = 0L;
var totalEconomyTicks = 0L;
var phase = sim.CostProfile;

void Checkpoint(int step)
{
    sim.CheckInvariants();
    var json = sim.SaveToJson();
    File.WriteAllText(Path.Combine(output, $"checkpoint-{step}.json"), json, new UTF8Encoding(false));
    if (CitySimulation.FromJson(json).SaveToJson() != json)
        throw new InvalidOperationException($"Checkpoint {step} failed exact save/load roundtrip");
    Console.WriteLine(FormattableString.Invariant(
        $"PR57_294_CHECKPOINT case={caseName} mode={mode} calls={step} " +
        $"elapsed={sim.State.ElapsedSeconds:F3} nextReview={sim.State.EconomyNextAt:F3} " +
        $"alive={sim.State.Citizens.Count(c => c.Alive)} vehicles={sim.State.Vehicles.Count} " +
        $"completed={sim.State.Projects.Count(p => p.Status == WorkStatus.Completed)} " +
        $"history={sim.State.Citizens.Sum(c => c.History.Count)} " +
        $"ledger={sim.State.Ledger.Count} events={sim.State.CityEvents.Count} " +
        $"bytes={Encoding.UTF8.GetByteCount(json)}"));
}
Checkpoint(0);
for (var i = 1; i <= calls; i++)
{
    var nextBefore = sim.State.EconomyNextAt;
    var elapsedBefore = sim.State.ElapsedSeconds;
    var completedBefore = sim.State.Projects.Count(p => p.Status == WorkStatus.Completed);
    var economyBefore = phase.Ticks[9];
    var vehicleBefore = phase.Ticks[0];
    var citizenBefore = phase.Ticks[2];
    var allocBefore = GC.GetAllocatedBytesForCurrentThread();
    var started = Stopwatch.GetTimestamp();
    sim.Advance(2);
    var wallTicks = Stopwatch.GetTimestamp() - started;
    var economyTicks = phase.Ticks[9] - economyBefore;
    var review = sim.State.EconomyNextAt != nextBefore;
    var completedDelta = sim.State.Projects.Count(p => p.Status == WorkStatus.Completed) - completedBefore;
    if (review) { reviewSteps++; reviewTicks += economyTicks; }
    else nonReviewTicks += economyTicks;
    totalEconomyTicks += economyTicks;
    if (trace)
        Console.WriteLine(FormattableString.Invariant(
            $"PR57_294_STEP case={caseName} call={i} " +
            $"dueBefore={(elapsedBefore + 2 >= nextBefore)} reviewScheduleChanged={review} " +
            $"completedDelta={completedDelta} " +
            $"economyMs={economyTicks * 1000.0 / Stopwatch.Frequency:F6} " +
            $"vehiclesMs={(phase.Ticks[0] - vehicleBefore) * 1000.0 / Stopwatch.Frequency:F6} " +
            $"citizensMs={(phase.Ticks[2] - citizenBefore) * 1000.0 / Stopwatch.Frequency:F6} " +
            $"advanceWallMs={wallTicks * 1000.0 / Stopwatch.Frequency:F6} " +
            $"advanceAllocatedBytes={GC.GetAllocatedBytesForCurrentThread() - allocBefore}"));
    if (checkpoints.Contains(i)) Checkpoint(i);
}
var wall = (Stopwatch.GetTimestamp() - startedProcess) * 1000.0 / Stopwatch.Frequency;
var cpu = (Process.GetCurrentProcess().TotalProcessorTime - cpuProcess).TotalMilliseconds;
Console.WriteLine(FormattableString.Invariant(
    $"PR57_294_FINAL case={caseName} mode={mode} calls={calls} reviewSteps={reviewSteps} " +
    $"reviewPhaseMs={reviewTicks * 1000.0 / Stopwatch.Frequency:F6} " +
    $"nonReviewPhaseMs={nonReviewTicks * 1000.0 / Stopwatch.Frequency:F6} " +
    $"totalEconomyPhaseMs={totalEconomyTicks * 1000.0 / Stopwatch.Frequency:F6} " +
    $"wallMs={wall:F6} cpuMs={cpu:F6} allocatedBytes={GC.GetTotalAllocatedBytes() - allocatedProcess} " +
    $"cadence=Advance2 all13Phases=True exactCheckpoints=True noCache=True noSkippedSlices=True"));
return 0;
