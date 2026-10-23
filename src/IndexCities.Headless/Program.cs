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
var known=new HashSet<string>{"rows","length","days","cap","signals","seed","growth","warmup","strand","e22","e22-window","e22-adversary","e22-reverse","e23-diff","e23-roadguard","e24-flush-only"};
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
    // The synthetic grid is wider than the V1 map. Give external rescue
    // vehicles fuel autonomy to actually reach a forced stranded truck/car.
    // All three arms use the identical warmed snapshot and the same tuning.
    var stress=Opt("e22-adversary","none");
    if(stress is "tow" or "mixed"){
        sim.State.Tuning.FuelTank*=12;
        Console.WriteLine($"E22_TOW_FIXTURE extendedTank={sim.State.Tuning.FuelTank:F1} cause=large-synthetic-map");
    }
    var initial=sim.SaveToJson();
    var span=int.Parse(Opt("e22-window","7200"));
    if(span<2||span>86400||span%2!=0){Console.Error.WriteLine("e22-window deve ser par de 2 a 86400");return 2;}
    var adversary=Opt("e22-adversary","none");
    if(adversary is not ("none" or "tow" or "junction" or "road" or "mixed"))
    {Console.Error.WriteLine("e22-adversary deve ser none/tow/junction/road/mixed");return 2;}
    // Observe every original 2-second quantum for one minute after each
    // intervention, then return to 10-minute checkpoints. Keep scheduled
    // interventions themselves as explicit checkpoint boundaries.
    var interventionsAt=adversary=="mixed"?new[]{660,1260,1860}:
        adversary=="none"?Array.Empty<int>():new[]{660};
    var boundaries=new List<int>();var progress=0;
    while(progress<span){
        var next=progress+600;
        if(progress<60||interventionsAt.Any(at=>progress>=at&&progress<at+60))
            next=progress+2;
        foreach(var at in interventionsAt)
            if(at>progress)next=Math.Min(next,at);
        progress=Math.Min(span,next);boundaries.Add(progress);
    }
    var reversed=Opt("e22-reverse","0")=="1";
    var order=reversed?new[]{"queue","events","off"}:new[]{"off","events","queue"};
    var interventions=new Dictionary<int,(string Kind,int Id)>();
    var coverage=new Dictionary<string,int>(StringComparer.Ordinal);
    var towDispatchArms=0;var towStrandedArms=0;
    string? ChooseAction(int seconds){
        if(adversary=="none")return null;
        if(adversary!="mixed")return seconds==660?adversary:null;
        return seconds switch{660=>"tow",1260=>"junction",1860=>"road",_=>null};
    }
    (int Id,bool Hit,string Info) Apply(CitySimulation trial,string kind,int? target){
        var st=trial.State;
        if(kind=="tow"){
            // Prefer a real leader with followers, as Stuck.Force does, but
            // select ONE id in reference order and reuse it on all arms.
            var picked=target==null?
                st.Vehicles.Where(v=>v.Status==VehicleStatus.Travelling&&v.Fuel>0&&v.RouteIndex>0&&v.RouteIndex<v.Route.Count
                    &&v.Machine==PavingMachine.None&&v.Body is not (TruckBody.Tow or TruckBody.TowHeavy)&&v.Detour.Count==0)
                .OrderByDescending(v=>st.Roads.Any(n=>n.Position.Distance(v.Position)<=st.Tuning.RoadWidth/2+1))
                .ThenByDescending(v=>v.PassengerCar&&v.DriverId!=null)
                .ThenByDescending(v=>st.Vehicles.Count(o=>o.Id!=v.Id&&o.Status==VehicleStatus.Travelling
                    &&Math.Cos(o.Heading-v.Heading)>.9&&(v.Position-o.Position) is var rel
                    &&rel.X*Math.Sin(v.Heading)+rel.Z*Math.Cos(v.Heading) is >0 and <40
                    &&Math.Abs(rel.X*Math.Cos(v.Heading)-rel.Z*Math.Sin(v.Heading))<2.6))
                .ThenBy(v=>v.Id).FirstOrDefault():
                st.Vehicles.FirstOrDefault(v=>v.Id==target);
            if(picked==null)return(-1,false,"no travelling vehicle to strand");
            picked.Fuel=0;
            // The previous adversary only zeroed Fuel on a car already blocked
            // in traffic. It never attempted a drive, so it never became
            // Stranded and NO tow was called. Fire the existing game's real
            // RunOutOfFuel transition in this synthetic adversarial fixture.
            // This preserves the implementation of tow calling, routing and
            // payments, which all run in subsequent ordinary simulation Steps.
            var outOfFuel=typeof(CitySimulation).GetMethod("RunOutOfFuel",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                ??throw new InvalidOperationException("Missing real RunOutOfFuel transition");
            outOfFuel.Invoke(trial,[picked]);
            return(picked.Id,picked.Status==VehicleStatus.Stranded,
                $"injectedRealRunOutOfFuel id={picked.Id} passengerCar={picked.PassengerCar} driver={picked.DriverId} status={picked.Status}");
        }
        if(kind=="junction"){
            var selected=target==null?
                st.Roads.Where(n=>!n.Highway&&n.Neighbours.Count>=3)
                .OrderByDescending(n=>st.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling&&v.Route.Skip(v.RouteIndex).Contains(n.Id)))
                .ThenBy(n=>n.Id).FirstOrDefault():
                st.Roads.FirstOrDefault(n=>n.Id==target);
            if(selected==null)return(-1,false,"no junction");
            var control=selected.Control==JunctionControl.Signal?JunctionControl.ThroughX:JunctionControl.Signal;
            var result=trial.SetJunction(selected.Id,control);
            return(selected.Id,result.Success,$"junction={selected.Id} control={control} success={result.Success}");
        }
        if(kind=="road"){
            var ordered=target==null?
                st.Roads.Where(n=>!n.Highway)
                .OrderByDescending(n=>st.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling&&v.Route.Skip(v.RouteIndex).Contains(n.Id)))
                .ThenBy(n=>n.Id).FirstOrDefault():
                st.Roads.FirstOrDefault(n=>n.Id==target);
            if(ordered==null)return(-1,false,"no eligible urban road");
            var before=trial.QuoteRoadDemolition(ordered.Id);
            var result=trial.DemolishRoad(ordered.Id);
            return(ordered.Id,result.Success&&before.AffectedVehicleRoutes>0,$"road={ordered.Id} routes={before.AffectedVehicleRoutes} isolated={before.IsolatedBuildingIds.Count} success={result.Success}");
        }
        return(-1,false,"unknown intervention");
    }
    var results=new Dictionary<string,(string[] Hashes,double Seconds,long Alloc,Dictionary<int,string> Snapshots)>();
    foreach(var mode in order)
    {
        var trial=CitySimulation.FromJson(initial);
        trial.E22Mode=mode;
        trial.E23RoadGuardSeconds=double.Parse(Opt("e23-roadguard","0"),System.Globalization.CultureInfo.InvariantCulture);
        trial.E24FlushWithoutPause=Opt("e24-flush-only","0")=="1";
        var hashes=new string[boundaries.Count];var previous=0;
        var snapshots=new Dictionary<int,string>();
        var towStranded=false;var towDispatched=false;
        var initialAlloc=GC.GetTotalAllocatedBytes();
        var clock=Stopwatch.StartNew();
        for(var i=0;i<boundaries.Count;i++)
        {
            trial.Advance(boundaries[i]-previous);previous=boundaries[i];
            if(ChooseAction(previous) is {} kind){
                interventions.TryGetValue(previous,out var prior);
                if(prior.Kind!=null&&prior.Kind!=kind)throw new InvalidOperationException("E22 intervention kind changed");
                var actual=Apply(trial,kind,prior.Kind==null?null:prior.Id);
                if(prior.Kind==null&&actual.Hit)interventions[previous]=(kind,actual.Id);
                if(actual.Hit)coverage[kind]=coverage.GetValueOrDefault(kind)+1;
                Console.WriteLine($"E22_INTERVENTION mode={mode} second={previous} kind={kind} id={actual.Id} applied={actual.Hit} {actual.Info}");
            }
            var towedId=interventions.Values.Where(z=>z.Kind=="tow").Select(z=>z.Id).FirstOrDefault();
            if(towedId>0){
                var victim=trial.State.Vehicles.FirstOrDefault(v=>v.Id==towedId);
                towStranded|=victim?.Status is VehicleStatus.Stranded or VehicleStatus.Towed;
                towDispatched|=victim?.TowTruckId!=null ||
                    trial.State.Vehicles.Any(v=>v.TowTargetId==towedId) ||
                    trial.State.Ledger.Any(l=>l.Category=="Guincho"&&l.EntityId==towedId);
            }
            var currentJson=trial.SaveToJson();
            hashes[i]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(currentJson)))[..16];
            if(Opt("e23-diff","0")=="1"&&previous>=1860&&previous<=3600)
                snapshots[previous]=currentJson;
            if(i<30||i==boundaries.Count-1||i%4==0)
                Console.WriteLine($"E22_CHECKPOINT mode={mode} elapsed={boundaries[i]} sha={hashes[i]}");
        }
        clock.Stop();trial.CheckInvariants();
        results[mode]=(hashes,clock.Elapsed.TotalSeconds,GC.GetTotalAllocatedBytes()-initialAlloc,snapshots);
        if(towStranded)towStrandedArms++;
        if(towDispatched)towDispatchArms++;
        Console.WriteLine($"E22_TOW_COVERAGE mode={mode} stranded={towStranded} dispatched={towDispatched}");
        Console.WriteLine($"E22_DAY mode={mode} simulatedSeconds={span} wall={clock.Elapsed.TotalSeconds:F3} allocatedMB={(GC.GetTotalAllocatedBytes()-initialAlloc)/1e6:F1} sha={hashes[^1]} {Stats(trial)}");
        if(mode!="off")Console.WriteLine(trial.E22Report());
    }
    var original=results["off"];
    foreach(var mode in new[]{"events","queue"})
    {
        var observed=results[mode];var first=-1;
        for(var i=0;i<boundaries.Count;i++)if(original.Hashes[i]!=observed.Hashes[i]){first=boundaries[i];break;}
        Console.WriteLine($"E22_COMPARE mode={mode} exactCheckpoints={first<0} firstDivergentSecond={first} baselineSeconds={original.Seconds:F3} trialSeconds={observed.Seconds:F3} baselineMB={original.Alloc/1e6:F1} trialMB={observed.Alloc/1e6:F1}");
        if(first>=0&&original.Snapshots.TryGetValue(first,out var originalJson)&&observed.Snapshots.TryGetValue(first,out var trialJson)){
            using var baselineDoc=JsonDocument.Parse(originalJson);
            using var candidateDoc=JsonDocument.Parse(trialJson);
            var firstPath=FirstDifference(baselineDoc.RootElement,candidateDoc.RootElement,"state")??"unknown";
            Console.WriteLine($"E23_FIRST_DIFFERENCE mode={mode} second={first} {firstPath}");
        }
    }
    Console.WriteLine($"E22_COVERAGE adversary={adversary} seeds={seed} appliedTow={coverage.GetValueOrDefault("tow")} appliedJunction={coverage.GetValueOrDefault("junction")} appliedRoadWithRoute={coverage.GetValueOrDefault("road")} towStrandedArms={towStrandedArms} towDispatchArms={towDispatchArms} order={string.Join(",",order)}");
    // Coverage must not pass silently. A failed mutation means that stress case
    // was NOT tested, regardless of save equality and CI result.
    var expectedKinds=adversary=="mixed"?new[]{"tow","junction","road"}:
        adversary=="none"?Array.Empty<string>():new[]{adversary};
    return expectedKinds.All(k=>coverage.GetValueOrDefault(k)==3) && (adversary is not ("tow" or "mixed")||towDispatchArms==3&&towStrandedArms==3)?0:3;
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

static string? FirstDifference(JsonElement a,JsonElement b,string path)
{
    if(a.ValueKind!=b.ValueKind)return path+" type "+a.ValueKind+" -> "+b.ValueKind;
    if(a.ValueKind==JsonValueKind.Object)
    {
        foreach(var field in a.EnumerateObject()){
            if(!b.TryGetProperty(field.Name,out var counterpart))return path+"."+field.Name+" missing";
            var sub=FirstDifference(field.Value,counterpart,path+"."+field.Name);
            if(sub!=null)return sub;
        }
        return null;
    }
    if(a.ValueKind==JsonValueKind.Array)
    {
        if(a.GetArrayLength()!=b.GetArrayLength())return path+$" count {a.GetArrayLength()} -> {b.GetArrayLength()}";
        for(var i=0;i<a.GetArrayLength();i++)
        {
            // Show vehicle identity with the array index for traceability.
            var id=a[i].ValueKind==JsonValueKind.Object&&a[i].TryGetProperty("Id",out var tag)?
                "(id="+tag.GetRawText()+")":"";
            var sub=FirstDifference(a[i],b[i],path+"["+i+"]"+id);
            if(sub!=null)return sub;
        }
        return null;
    }
    if(a.GetRawText()!=b.GetRawText())return path+" original="+a.GetRawText()[..Math.Min(130,a.GetRawText().Length)]+" candidate="+b.GetRawText()[..Math.Min(130,b.GetRawText().Length)];
    return null;
}
