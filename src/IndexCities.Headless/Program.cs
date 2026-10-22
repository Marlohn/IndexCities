using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Headless;
using IndexCities.Simulation;

// Headless measurements of the simulation core (no Godot). Usage:
//   scale  cycle|grid [--rows=3] [--length=400] [--days=7] [--cap=600] [--signals=1] [--seed=42] [--growth=0]
//          --growth=N (grid only): houses appear N per simulated hour until all are placed
//          --warmup=N days simulated before measuring (default 2)
//   census cycle|grid [same options]   per-step movement census (free vs. waiting)
//   stuck  cycle|grid [same options] [--strand=N]   every 10 min: vehicles stopped 30+ min, stranded on the road, deaths;
//          --strand=N empties the tank of N driving vehicles each day and follows the tow and the queue for 30 min
// Results are measurements, not product rules: see docs/exploration/ritmo-velocidades.md.
Console.OutputEncoding=Encoding.UTF8;
var known=new HashSet<string>{"rows","length","days","cap","signals","seed","growth","warmup","strand","e22","e22-window"};
var opts=new Dictionary<string,string>();foreach(var p in args.Skip(2).Select(a=>a.TrimStart('-').Split('=',2)))opts[p[0]]=p.Length>1?p[1]:"1";
string Opt(string k,string d)=>opts.GetValueOrDefault(k,d);
if(opts.Keys.FirstOrDefault(k=>!known.Contains(k)) is {} unknown){Console.Error.WriteLine("opção desconhecida: --"+unknown);return 2;}
if(args.Length<2||args[0] is not ("scale" or "census" or "stuck")||args[1] is not ("cycle" or "grid")){Console.Error.WriteLine("uso: scale|census|stuck cycle|grid [--rows=3] [--length=400] [--days=7] [--cap=600] [--signals=1] [--seed=42] [--growth=0] [--warmup=2]");return 2;}
var seed=int.Parse(Opt("seed","42"));var days=int.Parse(Opt("days","7"));var cap=double.Parse(Opt("cap","600"),System.Globalization.CultureInfo.InvariantCulture);var growth=int.Parse(Opt("growth","0"));var warmup=int.Parse(Opt("warmup","2"));
var sw=Stopwatch.StartNew();CitySimulation sim;string info;
if(args[1]=="cycle"){sim=Fixtures.Cycle(seed);info="cycle (CityCycleTests.City, obras reais)";}
else (sim,info)=Fixtures.Grid(seed,int.Parse(Opt("rows","3")),double.Parse(Opt("length","400"),System.Globalization.CultureInfo.InvariantCulture),Opt("signals","1")=="1",growth>0);
Console.WriteLine($"FIXTURE {info} build={sw.Elapsed.TotalSeconds:F1}s .NET={Environment.Version} cpu={Environment.ProcessorCount} release={!Debugger.IsAttached}");
if(growth>0){sw.Restart();var hours=0;while(Fixtures.AddHouses(sim,growth)>0){sim.Advance(3600);hours++;}Console.WriteLine($"GROWTH {growth} casas/h por {hours} h wall={sw.Elapsed.TotalSeconds:F1}s pop={sim.Population}");}
sw.Restart();sim.Advance(warmup*86400);
Console.WriteLine($"WARMUP {warmup}d wall={sw.Elapsed.TotalSeconds:F1}s {Stats(sim)}");
var e22=Opt("e22","off");
if(e22 is not ("off" or "events" or "queue" or "compare")){Console.Error.WriteLine("e22 deve ser off/events/queue/compare");return 2;}
if(e22=="compare")
{
    var initial=sim.SaveToJson();
    var span=int.Parse(Opt("e22-window","7200"));
    if(span<2||span>86400||span%2!=0){Console.Error.WriteLine("e22-window deve ser par de 2 a 86400");return 2;}
    var boundaries=new List<int>();var progress=0;
    while(progress<Math.Min(span,60)){progress+=2;boundaries.Add(progress);}
    while(progress<span){progress=Math.Min(span,progress+600);boundaries.Add(progress);}
    var results=new Dictionary<string,(string[] Hashes,double Seconds,long Alloc)>();
    foreach(var mode in new[]{"off","events","queue"})
    {
        var trial=CitySimulation.FromJson(initial);
        trial.E22Mode=mode;
        var hashes=new string[boundaries.Count];var previous=0;
        var initialAlloc=GC.GetTotalAllocatedBytes();
        var clock=Stopwatch.StartNew();
        for(var i=0;i<boundaries.Count;i++)
        {
            trial.Advance(boundaries[i]-previous);previous=boundaries[i];
            hashes[i]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trial.SaveToJson())))[..16];
            if(i<30||i==boundaries.Count-1||i%4==0)
                Console.WriteLine($"E22_CHECKPOINT mode={mode} elapsed={boundaries[i]} sha={hashes[i]}");
        }
        clock.Stop();trial.CheckInvariants();
        results[mode]=(hashes,clock.Elapsed.TotalSeconds,GC.GetTotalAllocatedBytes()-initialAlloc);
        Console.WriteLine($"E22_DAY mode={mode} simulatedSeconds={span} wall={clock.Elapsed.TotalSeconds:F3} allocatedMB={(GC.GetTotalAllocatedBytes()-initialAlloc)/1e6:F1} sha={hashes[^1]} {Stats(trial)}");
        if(mode!="off")Console.WriteLine(trial.E22Report());
    }
    var original=results["off"];
    foreach(var mode in new[]{"events","queue"})
    {
        var observed=results[mode];var first=-1;
        for(var i=0;i<boundaries.Count;i++)if(original.Hashes[i]!=observed.Hashes[i]){first=boundaries[i];break;}
        Console.WriteLine($"E22_COMPARE mode={mode} exactCheckpoints={first<0} firstDivergentSecond={first} baselineSeconds={original.Seconds:F3} trialSeconds={observed.Seconds:F3} baselineMB={original.Alloc/1e6:F1} trialMB={observed.Alloc/1e6:F1}");
    }
    return 0; // experimental falsification: divergence MUST be reported, not a CI failure.
}
sim.E22Mode=e22;
var census=args[0]=="census"?new Census():null;var stuck=args[0]=="stuck"?new Stuck():null;
var total=0.0;var gc0=GC.CollectionCount(0);var gc2=GC.CollectionCount(2);var alloc0=GC.GetTotalAllocatedBytes();var done=0;
for(var d=1;d<=days;d++){
    sw.Restart();
    if(stuck!=null){var strand=int.Parse(Opt("strand","0"));var dayEnd=sim.State.ElapsedSeconds+86400;if(strand>0){var forced=new List<int>();while(forced.Count==0&&sim.State.ElapsedSeconds<dayEnd-1800){forced=stuck.Force(sim,strand);if(forced.Count==0)sim.Advance(60);}var since=sim.State.ElapsedSeconds;for(var m=0;m<30&&forced.Count>0;m++){sim.Advance(60);if(m%3==0||m==29||Environment.GetEnvironmentVariable("STUCK_EVERY")=="1")stuck.Watch(sim,forced,since);}}while(sim.State.ElapsedSeconds<dayEnd-1e-6){sim.Advance(Math.Min(600,dayEnd-sim.State.ElapsedSeconds));stuck.Sample(sim);}}else if(census==null)sim.Advance(86400);else for(var i=0;i<43200;i++){sim.Advance(2);census.Sample(sim);}
    var w=sw.Elapsed.TotalSeconds;total+=w;done=d;
    Console.WriteLine($"DAY {d} wall={w:F2}s eff={86400/w:F0}x {Stats(sim)}");stuck?.Report(sim,d);
    if(total>cap){Console.WriteLine($"STOP limite de {cap}s");break;}
}
if(done>0)Console.WriteLine($"SUMMARY days={done} perDay={total/done:F2}s eff={86400*done/total:F0}x hours80y={total/done*29220/3600:F0} gen0={GC.CollectionCount(0)-gc0} gen2={GC.CollectionCount(2)-gc2} allocMB/day={(GC.GetTotalAllocatedBytes()-alloc0)/1e6/done:F0} workingSetMB={Process.GetCurrentProcess().WorkingSet64/1e6:F0}");
if(sim.E22Mode!="off")Console.WriteLine(sim.E22Report());
census?.Print(done);if(stuck!=null)Console.WriteLine($"STUCK SUMMARY guinchos chamados={sim.State.Ledger.Where(m=>m.Category=="Guincho"&&m.To=="global").Select(m=>(m.At,m.EntityId)).Distinct().Count()} rebocando agora={sim.State.Vehicles.Count(v=>v.Status==VehicleStatus.Towed)} sem combustível na via (total)={stuck.StrandedTotal} parados≥30min (máx)={stuck.StuckMax} mortos={sim.State.Citizens.Count(c=>!c.Alive)}");
sw.Restart();var json=sim.SaveToJson();var save=sw.Elapsed.TotalSeconds;sw.Restart();CitySimulation.FromJson(json);
Console.WriteLine($"SAVE {json.Length/1e6:F1}MB save={save:F2}s load={sw.Elapsed.TotalSeconds:F2}s sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))[..16]}");
sim.CheckInvariants();Console.WriteLine("INVARIANTS OK");
return 0;

static string Stats(CitySimulation s){var st=s.State;var alive=st.Citizens.Where(c=>c.Alive&&c.Resident).ToList();return $"pop={s.Population} alive={st.Citizens.Count(c=>c.Alive)} dead={st.Citizens.Count(c=>!c.Alive)} employed={st.Citizens.Count(c=>c.Alive&&c.Resident&&c.WorkplaceId!=null)} moving={st.Citizens.Count(c=>c.Alive&&c.Moving)} vehicles={st.Vehicles.Count} travelling={st.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling)} ledger={st.Ledger.Count} history={st.Citizens.Sum(c=>c.History.Count)} meals={st.Citizens.Sum(c=>c.History.Count(h=>h.Description.StartsWith("Consumiu 1 alimento")))} wages={st.Ledger.Where(l=>l.Category.StartsWith("Salários / ")).Sum(l=>l.Amount):F2} health={(alive.Count==0?0:alive.Average(c=>c.Health)):F6} satisfaction={(alive.Count==0?0:alive.Average(c=>c.Satisfaction)):F6}";}
