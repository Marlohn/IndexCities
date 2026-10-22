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
var known=new HashSet<string>{"rows","length","days","cap","signals","seed","growth","warmup","strand","e20","e20-window"};
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
var e20=Opt("e20","off");
if(e20 is not ("off" or "sleep" or "compare")){Console.Error.WriteLine("e20 deve ser off/sleep/compare");return 2;}
sim.E20SleepBlockedCars=e20=="sleep";
if(e20=="compare")
{
    var initial=sim.SaveToJson();
    var window=int.Parse(Opt("e20-window","86400"));
    if(window<2||window>86400||window%2!=0){Console.Error.WriteLine("e20-window deve ser multiplo par entre 2 e 86400");return 2;}
    var intervals=window==86400?24:Math.Min(12,window/60);
    intervals=Math.Max(1,intervals);
    var slices=window/intervals;
    // checkpoints must preserve even 2-second simulation quanta
    if(slices%2!=0){intervals=1;slices=window;}
    var compared=new Dictionary<string,(string[] Sha,double Seconds,long Alloc)>();
    foreach(var mode in new[]{"off","sleep"}){
        var trial=CitySimulation.FromJson(initial);trial.E20SleepBlockedCars=mode=="sleep";
        var points=new string[intervals];var alloc=GC.GetTotalAllocatedBytes();
        var sw20=Stopwatch.StartNew();var progressed=0;
        for(var i=0;i<intervals;i++){
            var step=i==intervals-1?window-progressed:slices;
            trial.Advance(step);progressed+=step;
            points[i]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trial.SaveToJson())))[..16];
            Console.WriteLine($"E20_CHECKPOINT mode={mode} time={progressed} sha={points[i]}");
        }
        sw20.Stop();trial.CheckInvariants();
        compared[mode]=(points,sw20.Elapsed.TotalSeconds,GC.GetTotalAllocatedBytes()-alloc);
        Console.WriteLine($"E20_DAY mode={mode} simSeconds={window} wall={sw20.Elapsed.TotalSeconds:F3} allocatedMB={(GC.GetTotalAllocatedBytes()-alloc)/1e6:F1} sha={points[^1]} {Stats(trial)}");
        if(mode=="sleep")Console.WriteLine(trial.E20Summary());
    }
    var a=compared["off"];var z=compared["sleep"];var first=-1;
    for(var i=0;i<intervals;i++)if(a.Sha[i]!=z.Sha[i]){first=i+1;break;}
    Console.WriteLine($"E20_COMPARE sameCheckpoints={first<0} firstDivergent={first} originalSeconds={a.Seconds:F3} sleepingSeconds={z.Seconds:F3} originalMB={a.Alloc/1e6:F1} sleepingMB={z.Alloc/1e6:F1}");
    // An intentionally incomplete dependency model is a falsification trial.
    return 0;
}
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
if(sim.E20SleepBlockedCars)Console.WriteLine(sim.E20Summary());
census?.Print(done);if(stuck!=null)Console.WriteLine($"STUCK SUMMARY guinchos chamados={sim.State.Ledger.Where(m=>m.Category=="Guincho"&&m.To=="global").Select(m=>(m.At,m.EntityId)).Distinct().Count()} rebocando agora={sim.State.Vehicles.Count(v=>v.Status==VehicleStatus.Towed)} sem combustível na via (total)={stuck.StrandedTotal} parados≥30min (máx)={stuck.StuckMax} mortos={sim.State.Citizens.Count(c=>!c.Alive)}");
sw.Restart();var json=sim.SaveToJson();var save=sw.Elapsed.TotalSeconds;sw.Restart();CitySimulation.FromJson(json);
Console.WriteLine($"SAVE {json.Length/1e6:F1}MB save={save:F2}s load={sw.Elapsed.TotalSeconds:F2}s sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))[..16]}");
sim.CheckInvariants();Console.WriteLine("INVARIANTS OK");
return 0;

static string Stats(CitySimulation s){var st=s.State;var alive=st.Citizens.Where(c=>c.Alive&&c.Resident).ToList();return $"pop={s.Population} alive={st.Citizens.Count(c=>c.Alive)} dead={st.Citizens.Count(c=>!c.Alive)} employed={st.Citizens.Count(c=>c.Alive&&c.Resident&&c.WorkplaceId!=null)} moving={st.Citizens.Count(c=>c.Alive&&c.Moving)} vehicles={st.Vehicles.Count} travelling={st.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling)} ledger={st.Ledger.Count} history={st.Citizens.Sum(c=>c.History.Count)} meals={st.Citizens.Sum(c=>c.History.Count(h=>h.Description.StartsWith("Consumiu 1 alimento")))} wages={st.Ledger.Where(l=>l.Category.StartsWith("Salários / ")).Sum(l=>l.Amount):F2} health={(alive.Count==0?0:alive.Average(c=>c.Health)):F6} satisfaction={(alive.Count==0?0:alive.Average(c=>c.Satisfaction)):F6}";}
