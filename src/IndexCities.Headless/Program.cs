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
var known=new HashSet<string>{"rows","length","days","cap","signals","seed","growth","warmup","strand","e18"};
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
var e18=Opt("e18","off");if(e18 is not ("off" or "probe" or "compare")){Console.Error.WriteLine("--e18 deve ser off, probe ou compare");return 2;}
if(e18=="probe")sim.E18RouteScopeProbe=true;
if(e18=="compare")
{
    if(args[0]!="scale"){Console.Error.WriteLine("E18_COMPARE requer scale");return 2;}
    var initial=sim.SaveToJson();
    var results=new Dictionary<string,(string[] Checkpoints,string Final,double Seconds,long Alloc)>();
    foreach(var mode in new[]{"original","scoped"})
    {
        var trial=CitySimulation.FromJson(initial);
        trial.E18RouteScopeProbe=mode=="scoped";
        var hourly=new string[24];var allocStart=GC.GetTotalAllocatedBytes();
        var watch=Stopwatch.StartNew();
        for(var hour=0;hour<24;hour++){
            trial.Advance(3600);
            hourly[hour]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trial.SaveToJson())))[..16];
            Console.WriteLine($"E18_CHECKPOINT mode={mode} hour={hour+1} sha={hourly[hour]}");
        }
        watch.Stop();trial.CheckInvariants();
        var allocated=GC.GetTotalAllocatedBytes()-allocStart;
        results[mode]=(hourly,hourly[^1],watch.Elapsed.TotalSeconds,allocated);
        Console.WriteLine($"E18_DAY mode={mode} seconds={watch.Elapsed.TotalSeconds:F3} allocMB={allocated/1e6:F0} sha={hourly[^1]} {Stats(trial)}");
        if(mode=="scoped")Console.WriteLine(trial.E18Summary());
    }
    var a=results["original"];var b=results["scoped"];
    var firstHour=-1;for(var h=0;h<24;h++)if(a.Checkpoints[h]!=b.Checkpoints[h]){firstHour=h+1;break;}
    Console.WriteLine($"E18_COMPARE equal24Hours={firstHour<0} firstDivergentHour={firstHour} baselineSeconds={a.Seconds:F3} scopedSeconds={b.Seconds:F3} baselineAllocMB={a.Alloc/1e6:F0} scopedAllocMB={b.Alloc/1e6:F0}");
    // Locate first 2-second state difference in the opening simulated minute.
    var checkA=CitySimulation.FromJson(initial);var checkB=CitySimulation.FromJson(initial);checkB.E18RouteScopeProbe=true;
    for(var i=1;i<=30;i++){
        checkA.Advance(2);checkB.Advance(2);
        var x=checkA.SaveToJson();var y=checkB.SaveToJson();
        if(x==y)continue;
        var at=0;while(at<Math.Min(x.Length,y.Length)&&x[at]==y[at])at++;
        var start=Math.Max(0,at-90);
        Console.WriteLine($"E18_FIRST_DIFFERENCE seconds={2*i} offset={at} before={x.Substring(start,Math.Min(180,x.Length-start))} after={y.Substring(start,Math.Min(180,y.Length-start))}");
        break;
    }
    return 0; // A causal divergence is an experimental finding, not CI failure.
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
if(sim.E18RouteScopeProbe)Console.WriteLine(sim.E18Summary());
census?.Print(done);if(stuck!=null)Console.WriteLine($"STUCK SUMMARY guinchos chamados={sim.State.Ledger.Where(m=>m.Category=="Guincho"&&m.To=="global").Select(m=>(m.At,m.EntityId)).Distinct().Count()} rebocando agora={sim.State.Vehicles.Count(v=>v.Status==VehicleStatus.Towed)} sem combustível na via (total)={stuck.StrandedTotal} parados≥30min (máx)={stuck.StuckMax} mortos={sim.State.Citizens.Count(c=>!c.Alive)}");
sw.Restart();var json=sim.SaveToJson();var save=sw.Elapsed.TotalSeconds;sw.Restart();CitySimulation.FromJson(json);
Console.WriteLine($"SAVE {json.Length/1e6:F1}MB save={save:F2}s load={sw.Elapsed.TotalSeconds:F2}s sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))[..16]}");
sim.CheckInvariants();Console.WriteLine("INVARIANTS OK");
return 0;

static string Stats(CitySimulation s){var st=s.State;var alive=st.Citizens.Where(c=>c.Alive&&c.Resident).ToList();return $"pop={s.Population} alive={st.Citizens.Count(c=>c.Alive)} dead={st.Citizens.Count(c=>!c.Alive)} employed={st.Citizens.Count(c=>c.Alive&&c.Resident&&c.WorkplaceId!=null)} moving={st.Citizens.Count(c=>c.Alive&&c.Moving)} vehicles={st.Vehicles.Count} travelling={st.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling)} ledger={st.Ledger.Count} history={st.Citizens.Sum(c=>c.History.Count)} meals={st.Citizens.Sum(c=>c.History.Count(h=>h.Description.StartsWith("Consumiu 1 alimento")))} wages={st.Ledger.Where(l=>l.Category.StartsWith("Salários / ")).Sum(l=>l.Amount):F2} health={(alive.Count==0?0:alive.Average(c=>c.Health)):F6} satisfaction={(alive.Count==0?0:alive.Average(c=>c.Satisfaction)):F6}";}
