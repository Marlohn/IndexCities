using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
var known=new HashSet<string>{"rows","length","days","cap","signals","seed","growth","warmup","strand","e15","e15-checkpoints"};
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
var e15=Opt("e15","off");if(e15 is not ("off" or "active" or "local" or "compare" or "diagnose" or "semantic")){Console.Error.WriteLine("e15 deve ser off, active ou local");return 2;}
sim.E15BatchScoped=e15 is "active" or "local";sim.E15LocalTime=e15=="local";
var e15Checkpoints=Opt("e15-checkpoints","0")=="1";
Console.WriteLine($"E15_MODE {e15} checkpoints={e15Checkpoints}");
// An experiment-only differential harness: fork three identical warmed states,
// compare each simulated hour, and run the full day (including economics/needs).
// This is separate from the normal scale command path and never used in Godot.
// First-difference probe on identical saves; diagnostic-only branch.
// Compare every gameplay field on full-hour snapshots, allowing only
// tiny floating-point numerical noise. The baseline and trial receive
// exactly the same commands/save and each simulate one integrated day.
if(e15=="semantic")
{
    var initialJson=sim.SaveToJson();
    var reference=CitySimulation.FromJson(initialJson);
    var trial=CitySimulation.FromJson(initialJson);
    trial.E15BatchScoped=true;trial.E15LocalTime=true;
    long totalFloatDifferences=0;int substantiveHours=0;
    for(var hour=1;hour<=24;hour++)
    {
        reference.Advance(3600);trial.Advance(3600);
        var before=reference.SaveToJson();var after=trial.SaveToJson();
        using var l=JsonDocument.Parse(before);using var rr=JsonDocument.Parse(after);
        var first=new List<string>();long floating=0;double maxFloatingDelta=0;
        void Compare(JsonElement a,JsonElement b,string path)
        {
            if(first.Count>=12)return;
            if(a.ValueKind!=b.ValueKind){first.Add(path+": kind "+a.ValueKind+" / "+b.ValueKind);return;}
            switch(a.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach(var pair in a.EnumerateObject())
                    {
                        if(!b.TryGetProperty(pair.Name,out var rhs)){first.Add(path+"."+pair.Name+": missing");continue;}
                        Compare(pair.Value,rhs,path+"."+pair.Name);
                    }
                    break;
                case JsonValueKind.Array:
                    if(a.GetArrayLength()!=b.GetArrayLength()){first.Add(path+": array count "+a.GetArrayLength()+" / "+b.GetArrayLength());break;}
                    for(var i=0;i<a.GetArrayLength();i++)Compare(a[i],b[i],path+"["+i+"]");
                    break;
                case JsonValueKind.Number:
                    if(a.GetRawText()==b.GetRawText())break;
                    if(a.TryGetInt64(out var ia)&&b.TryGetInt64(out var ib)&&ia==ib)break;
                    if(a.TryGetUInt64(out var ua)&&b.TryGetUInt64(out var ub)&&ua==ub)break;
                    var asText=a.GetRawText();var bsText=b.GetRawText();
                    if(asText.All(char.IsDigit)&&bsText.All(char.IsDigit)){first.Add(path+": integer "+asText+" / "+bsText);break;}
                    var x=a.GetDouble();var y=b.GetDouble();var diff=Math.Abs(x-y);
                    // Distinguish machine-level float drift from gameplay changes;
                    // large money, time and fixed-point changes remain detectable.
                    if(double.IsFinite(diff)&&diff<=1e-9){floating++;maxFloatingDelta=Math.Max(maxFloatingDelta,diff);}
                    else first.Add(path+": "+a.GetRawText()+" / "+b.GetRawText());
                    break;
                default:
                    if(a.GetRawText()!=b.GetRawText())first.Add(path+": "+a.GetRawText().Substring(0,Math.Min(80,a.GetRawText().Length))+" / "+b.GetRawText().Substring(0,Math.Min(80,b.GetRawText().Length)));
                    break;
            }
        }
        Compare(l.RootElement,rr.RootElement,"$");
        totalFloatDifferences+=floating;if(first.Count>0)substantiveHours++;
        Console.WriteLine($"E15_SEMANTIC hour={hour} equalJson={before==after} substantiveDifferences={first.Count} floatingNoiseCount={floating} maxFloatingNoise={maxFloatingDelta:G17}");
        foreach(var x in first.Take(6))Console.WriteLine("E15_SUBSTANTIVE "+x);
    }
    Console.WriteLine($"E15_SEMANTIC_SUMMARY substantiveHours={substantiveHours} floatingNoiseCount={totalFloatDifferences}");
    return 0;
}
if(e15=="diagnose")
{
    var initialJson=sim.SaveToJson();
    var control=CitySimulation.FromJson(initialJson);
    var experiment=CitySimulation.FromJson(initialJson);
    experiment.E15BatchScoped=true;experiment.E15LocalTime=true;
    for(var tick=1;tick<=1800;tick++)
    {
        control.Advance(2);
        experiment.Advance(2);
        var reference=control.SaveToJson();
        var altered=experiment.SaveToJson();
        if(reference==altered)continue;
        var first=0;while(first<Math.Min(reference.Length,altered.Length)&&reference[first]==altered[first])first++;
        var start=Math.Max(0,first-100);
        Console.WriteLine($"E15_FIRST_DIFFERENCE step={tick} timeSeconds={tick*2} firstByte={first} " +
            $"referenceLength={reference.Length} alteredLength={altered.Length}");
        Console.WriteLine("E15_REF "+reference.Substring(start,Math.Min(280,reference.Length-start)));
        Console.WriteLine("E15_NEW "+altered.Substring(start,Math.Min(280,altered.Length-start)));
        Console.WriteLine($"E15_RNG ref={control.State.RandomState} new={experiment.State.RandomState}");
        foreach(var v in control.State.Vehicles)
        {
            var n=experiment.State.Vehicles.FirstOrDefault(x=>x.Id==v.Id);
            if(n==null){Console.WriteLine($"E15_VEHICLE ref={v.Id} missing");continue;}
            if(v.Position!=n.Position||v.Heading!=n.Heading||v.Status!=n.Status||v.RouteIndex!=n.RouteIndex||v.FuelUnits!=n.FuelUnits||v.WaitingSince!=n.WaitingSince||v.Cause!=n.Cause)
            {
                Console.WriteLine($"E15_VEHICLE id={v.Id} ref=({v.Position}, {v.Heading}, {v.Status}, route={v.RouteIndex}, fuel={v.FuelUnits}, wait={v.WaitingSince}, cause={v.Cause})");
                Console.WriteLine($"E15_VEHICLE id={n.Id} new=({n.Position}, {n.Heading}, {n.Status}, route={n.RouteIndex}, fuel={n.FuelUnits}, wait={n.WaitingSince}, cause={n.Cause})");
                break;
            }
        }
        return 0;
    }
    Console.WriteLine("E15_NO_DIFFERENCE within first 3600 simulated seconds");
    return 1;
}
if(e15=="compare")
{
    if(args[0]!="scale"){Console.Error.WriteLine("e15 compare exige scale");return 2;}
    var initial=sim.SaveToJson();
    var reports=new Dictionary<string,(string[] Hours,string Final,double Seconds,long Alloc)>();
    foreach(var mode in new[]{"off","active","local"})
    {
        var candidate=CitySimulation.FromJson(initial);
        candidate.E15BatchScoped=mode!="off";
        candidate.E15LocalTime=mode=="local";
        var checkpoints=new string[24];
        var allocatedAt=GC.GetTotalAllocatedBytes();
        var timer=Stopwatch.StartNew();
        for(var hour=0;hour<24;hour++)
        {
            candidate.Advance(3600);
            checkpoints[hour]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(candidate.SaveToJson())))[..16];
            Console.WriteLine($"E15_CHECKPOINT mode={mode} hour={hour+1} sha={checkpoints[hour]}");
        }
        timer.Stop();
        candidate.CheckInvariants();
        var totalAlloc=GC.GetTotalAllocatedBytes()-allocatedAt;
        reports[mode]=(checkpoints,checkpoints[^1],timer.Elapsed.TotalSeconds,totalAlloc);
        Console.WriteLine($"E15_DAY mode={mode} wall={timer.Elapsed.TotalSeconds:F3}s allocMB={totalAlloc/1e6:F0} sha={checkpoints[^1]} {Stats(candidate)}");
        if(mode!="off")Console.WriteLine(candidate.E15Summary());
        Console.WriteLine($"E15_INVARIANTS mode={mode} OK");
    }
    foreach(var mode in new[]{"active","local"})
    {
        var baseline=reports["off"].Hours;var other=reports[mode].Hours;
        var first=-1;for(var i=0;i<other.Length;i++)if(other[i]!=baseline[i]){first=i+1;break;}
        Console.WriteLine($"E15_COMPARE mode={mode} sameHourly={first<0} firstDivergentHour={first} " +
            $"baseSeconds={reports["off"].Seconds:F3} newSeconds={reports[mode].Seconds:F3} " +
            $"baseAllocMB={reports["off"].Alloc/1e6:F0} newAllocMB={reports[mode].Alloc/1e6:F0}");
    }
    // "active" must be an exact differential control; "local" may fail and
    // will then be reported as falsified, not used for production.
    return reports["off"].Hours.SequenceEqual(reports["active"].Hours)?0:1;
}
var census=args[0]=="census"?new Census():null;var stuck=args[0]=="stuck"?new Stuck():null;
var total=0.0;var gc0=GC.CollectionCount(0);var gc2=GC.CollectionCount(2);var alloc0=GC.GetTotalAllocatedBytes();var done=0;
for(var d=1;d<=days;d++){
    sw.Restart();
    if(stuck!=null){var strand=int.Parse(Opt("strand","0"));var dayEnd=sim.State.ElapsedSeconds+86400;if(strand>0){var forced=new List<int>();while(forced.Count==0&&sim.State.ElapsedSeconds<dayEnd-1800){forced=stuck.Force(sim,strand);if(forced.Count==0)sim.Advance(60);}var since=sim.State.ElapsedSeconds;for(var m=0;m<30&&forced.Count>0;m++){sim.Advance(60);if(m%3==0||m==29||Environment.GetEnvironmentVariable("STUCK_EVERY")=="1")stuck.Watch(sim,forced,since);}}while(sim.State.ElapsedSeconds<dayEnd-1e-6){sim.Advance(Math.Min(600,dayEnd-sim.State.ElapsedSeconds));stuck.Sample(sim);}}else if(census==null&&e15Checkpoints){for(var h=1;h<=24;h++){sim.Advance(3600);var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sim.SaveToJson())))[..16];Console.WriteLine($"E15_CHECKPOINT day={d} hour={h} sha={hash}");}}else if(census==null)sim.Advance(86400);else for(var i=0;i<43200;i++){sim.Advance(2);census.Sample(sim);}
    var w=sw.Elapsed.TotalSeconds;total+=w;done=d;
    Console.WriteLine($"DAY {d} wall={w:F2}s eff={86400/w:F0}x {Stats(sim)}");stuck?.Report(sim,d);
    if(total>cap){Console.WriteLine($"STOP limite de {cap}s");break;}
}
if(done>0)Console.WriteLine($"SUMMARY days={done} perDay={total/done:F2}s eff={86400*done/total:F0}x hours80y={total/done*29220/3600:F0} gen0={GC.CollectionCount(0)-gc0} gen2={GC.CollectionCount(2)-gc2} allocMB/day={(GC.GetTotalAllocatedBytes()-alloc0)/1e6/done:F0} workingSetMB={Process.GetCurrentProcess().WorkingSet64/1e6:F0}");
if(e15!="off")Console.WriteLine(sim.E15Summary());
census?.Print(done);if(stuck!=null)Console.WriteLine($"STUCK SUMMARY guinchos chamados={sim.State.Ledger.Where(m=>m.Category=="Guincho"&&m.To=="global").Select(m=>(m.At,m.EntityId)).Distinct().Count()} rebocando agora={sim.State.Vehicles.Count(v=>v.Status==VehicleStatus.Towed)} sem combustível na via (total)={stuck.StrandedTotal} parados≥30min (máx)={stuck.StuckMax} mortos={sim.State.Citizens.Count(c=>!c.Alive)}");
sw.Restart();var json=sim.SaveToJson();var save=sw.Elapsed.TotalSeconds;sw.Restart();CitySimulation.FromJson(json);
Console.WriteLine($"SAVE {json.Length/1e6:F1}MB save={save:F2}s load={sw.Elapsed.TotalSeconds:F2}s sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))[..16]}");
sim.CheckInvariants();Console.WriteLine("INVARIANTS OK");
return 0;

static string Stats(CitySimulation s){var st=s.State;var alive=st.Citizens.Where(c=>c.Alive&&c.Resident).ToList();return $"pop={s.Population} alive={st.Citizens.Count(c=>c.Alive)} dead={st.Citizens.Count(c=>!c.Alive)} employed={st.Citizens.Count(c=>c.Alive&&c.Resident&&c.WorkplaceId!=null)} moving={st.Citizens.Count(c=>c.Alive&&c.Moving)} vehicles={st.Vehicles.Count} travelling={st.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling)} ledger={st.Ledger.Count} history={st.Citizens.Sum(c=>c.History.Count)} meals={st.Citizens.Sum(c=>c.History.Count(h=>h.Description.StartsWith("Consumiu 1 alimento")))} wages={st.Ledger.Where(l=>l.Category.StartsWith("Salários / ")).Sum(l=>l.Amount):F2} health={(alive.Count==0?0:alive.Average(c=>c.Health)):F6} satisfaction={(alive.Count==0?0:alive.Average(c=>c.Satisfaction)):F6}";}
