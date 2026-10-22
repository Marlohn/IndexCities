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
var known=new HashSet<string>{"rows","length","days","cap","signals","seed","growth","warmup","strand","e15","e15-verify","e15-checkpoints","e16","e16-interval"};
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
var e15Enabled=Opt("e15","0")=="1";
var e15Checkpoints=Opt("e15-checkpoints","0")=="1";
var e16=Opt("e16","none");
if(e16 is not ("none" or "tow" or "road" or "reload" or "mixed")){Console.Error.WriteLine("invalid e16 mode");return 2;}
var e16Interval=int.Parse(Opt("e16-interval","3600"));
if(e16Interval<=0||3600%e16Interval!=0){Console.Error.WriteLine("e16 interval must divide one hour");return 2;}
sim.E15Enable=e15Enabled;
sim.E15VerifyEvery=int.Parse(Opt("e15-verify","0"));
sw.Restart();sim.Advance(warmup*86400);
Console.WriteLine($"WARMUP {warmup}d wall={sw.Elapsed.TotalSeconds:F1}s {Stats(sim)}");
var census=args[0]=="census"?new Census():null;var stuck=args[0]=="stuck"?new Stuck():null;
var total=0.0;var gc0=GC.CollectionCount(0);var gc2=GC.CollectionCount(2);var alloc0=GC.GetTotalAllocatedBytes();var done=0;
for(var d=1;d<=days;d++){
    sw.Restart();
    if(e16!="none"){
        var e16Forced=false;var e16ForcedIds=new List<int>();var e16Probe=new Stuck();
        var partsPerHour=3600/e16Interval;
        for(var hour=0;hour<24;hour++){
            if((e16=="tow"||e16=="mixed")&&!e16Forced&&hour is >=1 and <=8){
                e16ForcedIds=e16Probe.Force(sim,2);e16Forced=e16ForcedIds.Count>0;
                if(e16Forced)Console.WriteLine($"E16_FORCED day={d} hour={hour} count={e16ForcedIds.Count} ids={string.Join(",",e16ForcedIds)}");
            }
            if((e16=="road"||e16=="mixed")&&hour==3){
                var candidate=sim.State.Roads.Where(r=>!r.Highway&&r.Neighbours.Count>=2).OrderByDescending(r=>r.Neighbours.Count).ThenBy(r=>r.Id).First();
                // Real road node changes reach the same rebuild/invalidation path as road works.
                // Experimental topology perturbation only (not a gameplay action).
                var before=sim.State.Roads.Count;
                var add=typeof(CitySimulation).GetMethod("AddRoad",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                var rebuild=typeof(CitySimulation).GetMethod("RebuildRoadGraph",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                add.Invoke(sim,new object[]{new Point(candidate.Position.X+12,candidate.Position.Z+12),false});
                rebuild.Invoke(sim,new object[]{true});
                Console.WriteLine($"E16_ROAD day={d} hour={hour} roads={before}->{sim.State.Roads.Count}");
            }
            if((e16=="reload"||e16=="mixed")&&hour==8){
                sim=CitySimulation.FromJson(sim.SaveToJson());sim.E15Enable=e15Enabled;sim.E15VerifyEvery=int.Parse(Opt("e15-verify","0"));
                Console.WriteLine($"E16_RELOAD day={d} hour={hour}");
            }
            for(var part=0;part<partsPerHour;part++){
                sim.Advance(e16Interval);
                var snapshot=sim.SaveToJson();
                Console.WriteLine($"E15_CHECKPOINT day={d} hour={hour+1} part={part+1} sha={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)))[..16]}");
            }
        }
        if(e16=="tow"||e16=="mixed")Console.WriteLine($"E16_TOW_COVERAGE forced={e16ForcedIds.Count>0}");
    }else if(stuck!=null){var strand=int.Parse(Opt("strand","0"));var dayEnd=sim.State.ElapsedSeconds+86400;if(strand>0){var forced=new List<int>();while(forced.Count==0&&sim.State.ElapsedSeconds<dayEnd-1800){forced=stuck.Force(sim,strand);if(forced.Count==0)sim.Advance(60);}var since=sim.State.ElapsedSeconds;for(var m=0;m<30&&forced.Count>0;m++){sim.Advance(60);if(m%3==0||m==29||Environment.GetEnvironmentVariable("STUCK_EVERY")=="1")stuck.Watch(sim,forced,since);}}while(sim.State.ElapsedSeconds<dayEnd-1e-6){sim.Advance(Math.Min(600,dayEnd-sim.State.ElapsedSeconds));stuck.Sample(sim);}}else if(census==null&&e15Checkpoints){for(var h=1;h<=24;h++){sim.Advance(3600);var checkpoint=sim.SaveToJson();Console.WriteLine($"E15_CHECKPOINT day={d} hour={h} sha={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(checkpoint)))[..16]}");}}else if(census==null)sim.Advance(86400);else for(var i=0;i<43200;i++){sim.Advance(2);census.Sample(sim);}
    var w=sw.Elapsed.TotalSeconds;total+=w;done=d;
    Console.WriteLine($"DAY {d} wall={w:F2}s eff={86400/w:F0}x {Stats(sim)}");stuck?.Report(sim,d);
    if(total>cap){Console.WriteLine($"STOP limite de {cap}s");break;}
}
if(done>0)Console.WriteLine($"SUMMARY days={done} perDay={total/done:F2}s eff={86400*done/total:F0}x hours80y={total/done*29220/3600:F0} gen0={GC.CollectionCount(0)-gc0} gen2={GC.CollectionCount(2)-gc2} allocMB/day={(GC.GetTotalAllocatedBytes()-alloc0)/1e6/done:F0} workingSetMB={Process.GetCurrentProcess().WorkingSet64/1e6:F0}");
if(e15Enabled)Console.WriteLine(sim.E15Summary());
census?.Print(done);if(stuck!=null)Console.WriteLine($"STUCK SUMMARY guinchos chamados={sim.State.Ledger.Where(m=>m.Category=="Guincho"&&m.To=="global").Select(m=>(m.At,m.EntityId)).Distinct().Count()} rebocando agora={sim.State.Vehicles.Count(v=>v.Status==VehicleStatus.Towed)} sem combustível na via (total)={stuck.StrandedTotal} parados≥30min (máx)={stuck.StuckMax} mortos={sim.State.Citizens.Count(c=>!c.Alive)}");
sw.Restart();var json=sim.SaveToJson();var save=sw.Elapsed.TotalSeconds;sw.Restart();CitySimulation.FromJson(json);
Console.WriteLine($"SAVE {json.Length/1e6:F1}MB save={save:F2}s load={sw.Elapsed.TotalSeconds:F2}s sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))[..16]}");
sim.CheckInvariants();Console.WriteLine("INVARIANTS OK");
return 0;

static string Stats(CitySimulation s){var st=s.State;var alive=st.Citizens.Where(c=>c.Alive&&c.Resident).ToList();return $"pop={s.Population} alive={st.Citizens.Count(c=>c.Alive)} dead={st.Citizens.Count(c=>!c.Alive)} employed={st.Citizens.Count(c=>c.Alive&&c.Resident&&c.WorkplaceId!=null)} moving={st.Citizens.Count(c=>c.Alive&&c.Moving)} vehicles={st.Vehicles.Count} travelling={st.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling)} ledger={st.Ledger.Count} history={st.Citizens.Sum(c=>c.History.Count)} meals={st.Citizens.Sum(c=>c.History.Count(h=>h.Description.StartsWith("Consumiu 1 alimento")))} wages={st.Ledger.Where(l=>l.Category.StartsWith("Salários / ")).Sum(l=>l.Amount):F2} health={(alive.Count==0?0:alive.Average(c=>c.Health)):F6} satisfaction={(alive.Count==0?0:alive.Average(c=>c.Satisfaction)):F6}";}
