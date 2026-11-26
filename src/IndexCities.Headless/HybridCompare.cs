using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using IndexCities.Simulation;

namespace IndexCities.Headless;

// Differential test on the ACTUAL simulation, not the independent PR55 model.
// Loan deadlines are deliberately moved in the fixture, never in game code.
static class HybridCompare
{
    public static int Prepare(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("hybrid-prepare <seed> <save>");
        var sim = Fixtures.Cycle(int.Parse(args[0], CultureInfo.InvariantCulture));
        sim.Advance(86400);
        var borrowed = sim.Borrow(100m);
        if (!borrowed.Success) throw new InvalidOperationException("Loan fixture: " + borrowed.Message);
        // Exercise a coincident daily, tax and loan event with actual finance
        // methods. This is a targeted adversarial input, not a calibrated city.
        var due = sim.State.NextDaily;
        sim.State.NextTaxDate = due;
        sim.State.Loans.Last().NextDue = due;
        sim.CheckInvariants();
        File.WriteAllText(args[1], sim.SaveToJson());
        Console.WriteLine($"HYBRID_FIXTURE cycle seed={args[0]} living={sim.State.Citizens.Count(c=>c.Alive)} due={due:O} forced_deadlines=true");
        return 0;
    }

    public static int Run(string[] args)
    {
        if (args.Length != 3 || args[1] is not ("reference" or "calendar"))
            throw new ArgumentException("hybrid-run <save> reference|calendar <output-directory>");
        var candidate = args[1] == "calendar";
        Directory.CreateDirectory(args[2]);
        var sim = CitySimulation.FromJson(File.ReadAllText(args[0]));
        sim.ExperimentalCausalCalendarEnabled = candidate;
        long calls = 0, rebuilds = 0, alloc = 0;
        double wall = 0, cpu = 0;
        int initialAlive = sim.State.Citizens.Count(c=>c.Alive);
        for (int i = 1; i <= 4; i++)
        {
            // External mutation between public Advance calls exercises the
            // invalidation contract, including immediately after a reload.
            if (i == 3)
            {
                var loan = sim.State.Loans.Last();
                if (loan.RemainingMonths <= 0) throw new InvalidOperationException("Loan probe needs another installment");
                loan.NextDue = sim.State.Now.AddSeconds(2);
            }
            var oldCalls = sim.ExperimentalLoanPhaseCalls;
            var oldRebuilds = sim.ExperimentalLoanDeadlineRebuilds;
            using var process = Process.GetCurrentProcess();
            var cpuBefore = process.TotalProcessorTime;
            var allocatedBefore = GC.GetTotalAllocatedBytes(true);
            var timer = Stopwatch.StartNew();
            sim.Advance(21600);
            timer.Stop();
            process.Refresh();
            wall += timer.Elapsed.TotalSeconds;
            cpu += (process.TotalProcessorTime-cpuBefore).TotalSeconds;
            alloc += GC.GetTotalAllocatedBytes(true)-allocatedBefore;
            calls += sim.ExperimentalLoanPhaseCalls-oldCalls;
            rebuilds += sim.ExperimentalLoanDeadlineRebuilds-oldRebuilds;
            sim.CheckInvariants();
            var file = Path.Combine(args[2], $"checkpoint-{i}.json");
            File.WriteAllText(file, sim.SaveToJson());
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
            Console.WriteLine($"HYBRID_CHECKPOINT mode={args[1]} n={i} sha256={hash} alive={sim.State.Citizens.Count(c=>c.Alive)}");
            if (i == 2)
            {
                sim = CitySimulation.FromJson(File.ReadAllText(file));
                sim.ExperimentalCausalCalendarEnabled = candidate;
            }
        }
        if (calls == 0) throw new InvalidOperationException("No real loan callback exercised");
        Console.WriteLine(FormattableString.Invariant($"HYBRID_RESULT mode={args[1]} initialAlive={initialAlive} finalAlive={sim.State.Citizens.Count(c=>c.Alive)} wall={wall:R} cpu={cpu:R} allocatedBytes={alloc} loanPhaseCalls={calls} deadlineRebuilds={rebuilds}"));
        return 0;
    }
}
