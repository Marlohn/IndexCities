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
        if (args.Length is <2 or >3) throw new ArgumentException("hybrid-prepare <seed> <save> [organic100]");
        var seed=int.Parse(args[0], CultureInfo.InvariantCulture);
        var organic=args.Length==3 && args[2]=="organic100";
        if(args.Length==3 && !organic) throw new ArgumentException("unknown workload");
        var sim = organic ? Organic100(seed) : Fixtures.Cycle(seed);
        sim.Advance(86400);
        if(organic && sim.State.Citizens.Count(c=>c.Alive&&c.Resident&&c.InsideMap)<100)
            throw new InvalidOperationException("Organic workload failed to reach 100 living residents; do not label as 100");
        var borrowed = sim.Borrow(100m);
        if (!borrowed.Success) throw new InvalidOperationException("Loan fixture: " + borrowed.Message);
        // Exercise a coincident daily, tax and loan event with actual finance
        // methods. This is a targeted adversarial input, not a calibrated city.
        var due = sim.State.NextDaily;
        sim.State.NextTaxDate = due;
        sim.State.Loans.Last().NextDue = due;
        sim.CheckInvariants();
        File.WriteAllText(args[1], sim.SaveToJson());
        Console.WriteLine($"HYBRID_FIXTURE workload={(organic?"organic100":"cycle")} seed={args[0]} living={sim.State.Citizens.Count(c=>c.Alive)} due={due:O} forced_deadlines=true");
        return 0;
    }

    // Workload recipe reconstructed from PR57 experiment #74. Only ordinary
    // paid commands and elapsed simulation time. No direct population, stock,
    // building completion or catalog edits. This is not a claim of stability.
    static CitySimulation Organic100(int seed)
    {
        var sim = Fixtures.Cycle(seed);
        void Require(CommandResult r)
        {
            if (!r.Success) throw new InvalidOperationException("Organic workload: " + r.Message);
        }
        var roads = new (Point From, Point To)[] {
            (new(-184,0),new(-184,-60)),
            (new(-184,-60),new(-16,-60)),
            (new(-16,-60),new(-16,0)),
            (new(-184,-60),new(-184,-104)),
            (new(-184,-104),new(-16,-104)),
            (new(-184,-104),new(-184,-160)),
            (new(-184,-160),new(228,-160))
        };
        foreach (var (from,to) in roads) Require(sim.PlaceRoad(from,to));
        for (int h=0; h<96 && sim.State.Projects.Any(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress && !p.IsMaintenance); h++)
            sim.Advance(3600);
        int houses=0, jobs=0;
        var rows = new (double Z, double Rotation, double Start, double End)[] {
            (30,Math.PI,-176,216),(-14,Math.PI,-176,216),
            (-74,Math.PI,-168,-28),(-46,0,-168,-28),
            (-118,Math.PI,-168,-28),(-90,0,-168,-28)
        };
        foreach(var (z,rotation,start,end) in rows)
            for(double x=start;x<=end && houses<48;x+=20)
            {
                var p=new Point(x,z);
                var quote=sim.QuoteBuilding(BuildingKind.House,p,rotation);
                if(quote.PhysicallyValid && quote.Accessible && sim.PlaceBuilding(BuildingKind.House,p,rotation).Success) houses++;
            }
        var sites=new List<(BuildingKind Kind,Point Position,double Rotation)>();
        for(int i=0;i<10;i++)sites.Add((BuildingKind.Grocery,new Point(-168+i*20,-145),0));
        for(int i=0;i<10;i++)
        {
            var kind=i<6?BuildingKind.Farm:i<8?BuildingKind.FuelStation:BuildingKind.MechanicShop;
            var z=kind==BuildingKind.Farm?-186:kind==BuildingKind.FuelStation?-178:-176;
            sites.Add((kind,new Point(-164+i*40,z),Math.PI));
        }
        foreach(var (kind,p,rotation) in sites)
        {
            var quote=sim.QuoteBuilding(kind,p,rotation);
            if(quote.PhysicallyValid && quote.Accessible && sim.PlaceBuilding(kind,p,rotation).Success) jobs++;
        }
        Console.WriteLine($"HYBRID_ORGANIC_ORDERS houses={houses} businesses={jobs}");
        for(int h=0;h<96;h++)
        {
            sim.Advance(3600);
            if(h%6==5) Console.WriteLine($"HYBRID_ORGANIC_PROGRESS hour={h+1} living={sim.State.Citizens.Count(c=>c.Alive&&c.Resident&&c.InsideMap)} vehicles={sim.State.Vehicles.Count}");
            if(sim.State.Citizens.Count(c=>c.Alive&&c.Resident&&c.InsideMap)>=100 &&
               sim.State.Buildings.Count(b=>b.CompanyId!=null && b.Completed && !b.Removed)>=5) break;
        }
        sim.CheckInvariants();
        return sim;
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
