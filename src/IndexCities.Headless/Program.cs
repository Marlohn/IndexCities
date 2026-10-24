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
//   profile cycle|grid [same options]  per-step timing of unchanged simulation phases
//   --window=N (scale/profile only): observe N simulated seconds, in 2-second quanta (default 86400)
//   stuck  cycle|grid [same options] [--strand=N]   every 10 min: vehicles stopped 30+ min, stranded on the road, deaths;
//          --strand=N empties the tank of N driving vehicles each day and follows the tow and the queue for 30 min
// Results are measurements, not product rules: see docs/exploration/ritmo-velocidades.md.
Console.OutputEncoding=Encoding.UTF8;
var known=new HashSet<string>{"rows","length","days","cap","signals","seed","growth","warmup","strand","window"};
var opts=new Dictionary<string,string>();foreach(var p in args.Skip(2).Select(a=>a.TrimStart('-').Split('=',2)))opts[p[0]]=p.Length>1?p[1]:"1";
string Opt(string k,string d)=>opts.GetValueOrDefault(k,d);
if(opts.Keys.FirstOrDefault(k=>!known.Contains(k)) is {} unknown){Console.Error.WriteLine("opção desconhecida: --"+unknown);return 2;}
if(args.Length<2||args[0] is not ("scale" or "census" or "stuck" or "profile" or "repro")||args[1] is not ("cycle" or "grid")){Console.Error.WriteLine("uso: scale|census|stuck|profile cycle|grid [--rows=3] [--length=400] [--days=7] [--cap=600] [--signals=1] [--seed=42] [--growth=0] [--warmup=2]");return 2;}
var seed=int.Parse(Opt("seed","42"));var days=int.Parse(Opt("days","7"));var cap=double.Parse(Opt("cap","600"),System.Globalization.CultureInfo.InvariantCulture);var growth=int.Parse(Opt("growth","0"));var warmup=int.Parse(Opt("warmup","2"));
var windowSeconds=int.Parse(Opt("window","86400"));
if(windowSeconds<2||windowSeconds>86400||windowSeconds%2!=0){Console.Error.WriteLine("--window deve estar entre 2 e 86400 segundos e ser múltiplo de 2");return 2;}
if(windowSeconds!=86400&&(args[0]=="census"||args[0]=="stuck")){Console.Error.WriteLine("--window parcial é exclusivo de scale/profile");return 2;}
var sw=Stopwatch.StartNew();CitySimulation sim;string info;
if(args[1]=="cycle"){sim=Fixtures.Cycle(seed);info="cycle (CityCycleTests.City, obras reais)";}
else (sim,info)=Fixtures.Grid(seed,int.Parse(Opt("rows","3")),double.Parse(Opt("length","400"),System.Globalization.CultureInfo.InvariantCulture),Opt("signals","1")=="1",growth>0);
Console.WriteLine($"FIXTURE {info} build={sw.Elapsed.TotalSeconds:F1}s .NET={Environment.Version} cpu={Environment.ProcessorCount} release={!Debugger.IsAttached}");
if(growth>0){sw.Restart();var hours=0;while(Fixtures.AddHouses(sim,growth)>0){sim.Advance(3600);hours++;}Console.WriteLine($"GROWTH {growth} casas/h por {hours} h wall={sw.Elapsed.TotalSeconds:F1}s pop={sim.Population}");}
// PR57-only deterministic replay of the real 5183/5294/5179 queue.
// This is a diagnostic command; it does not change CitySimulation behavior.
if(args[0]=="repro"){
    const double checkpointSecond=3606;
    const double observationSeconds=36;
    sim.Advance(checkpointSecond);
    var snapshot=sim.SaveToJson();
    Console.WriteLine($"PR57_REAL_CHECKPOINT seed={seed} second={sim.State.ElapsedSeconds:F3} {Stats(sim)} bytes={snapshot.Length}");
    foreach(var quantum in new[]{0.1,0.5,2.0}){
        var copy=CitySimulation.FromJson(snapshot);
        if(quantum==2.0)copy.CostProfile=new StepCostProfile(); // profile-only swept-path observations
        var ids=new[]{5183,5294,5179};
        var observed=ids.Select(id=>copy.State.Vehicles.Single(v=>v.Id==id)).ToArray();
        var originals=observed.Select(v=>v.Position).ToArray();
        var travelled=new double[ids.Length];
        var idle=new double[ids.Length];
        var longestIdle=new double[ids.Length];
        double pairedIdle=0,longestPairedIdle=0;
        for(var i=0;i<(int)Math.Round(observationSeconds/quantum);i++){
            var before=observed.Select(v=>v.Position).ToArray();
            copy.Advance(quantum);
            for(var k=0;k<ids.Length;k++){
                var distance=observed[k].Position.Distance(before[k]);
                travelled[k]+=distance;
                idle[k]=observed[k].Status==VehicleStatus.Travelling&&distance<.000001?idle[k]+quantum:0;
                longestIdle[k]=Math.Max(longestIdle[k],idle[k]);
            }
            pairedIdle=idle[0]>0&&idle[1]>0?pairedIdle+quantum:0;
            longestPairedIdle=Math.Max(longestPairedIdle,pairedIdle);
        }
        copy.CheckInvariants();
        for(var k=0;k<ids.Length;k++){
            var v=observed[k];
            Console.WriteLine($"PR57_REAL_VEHICLE quantum={quantum:F1} id={v.Id} firstX={originals[k].X:F6} firstZ={originals[k].Z:F6} lastX={v.Position.X:F6} lastZ={v.Position.Z:F6} net={v.Position.Distance(originals[k]):F6} path={travelled[k]:F6} longestIdle={longestIdle[k]:F3} routeIndex={v.RouteIndex} status={v.Status} waitingSince={v.WaitingSince:F6} cause={v.Cause?.Replace(' ','_')}");
        }
        var endSave=copy.SaveToJson();
        if(CitySimulation.FromJson(endSave).SaveToJson()!=endSave)throw new InvalidOperationException("PR57 checkpoint save/load mismatch");
        Console.WriteLine($"PR57_REAL_RESULT quantum={quantum:F1} seconds={copy.State.ElapsedSeconds-checkpointSecond:F3} longestPairIdle={longestPairedIdle:F3} sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endSave)))} {Stats(copy)}");
    }
    Console.WriteLine("PR57_REAL_INVARIANTS OK");
    return 0;
}
sw.Restart();sim.Advance(warmup*86400);
Console.WriteLine($"WARMUP {warmup}d wall={sw.Elapsed.TotalSeconds:F1}s {Stats(sim)}");
var census=args[0]=="census"?new Census():null;var stuck=args[0]=="stuck"?new Stuck():null;
var profile=args[0]=="profile"?new StepCostProfile():null;sim.CostProfile=profile;
var total=0.0;var gc0=GC.CollectionCount(0);var gc2=GC.CollectionCount(2);var alloc0=GC.GetTotalAllocatedBytes();var done=0;
for(var d=1;d<=days;d++){
    sw.Restart();
    if(stuck!=null){var strand=int.Parse(Opt("strand","0"));var dayEnd=sim.State.ElapsedSeconds+86400;if(strand>0){var forced=new List<int>();while(forced.Count==0&&sim.State.ElapsedSeconds<dayEnd-1800){forced=stuck.Force(sim,strand);if(forced.Count==0)sim.Advance(60);}var since=sim.State.ElapsedSeconds;for(var m=0;m<30&&forced.Count>0;m++){sim.Advance(60);if(m%3==0||m==29||Environment.GetEnvironmentVariable("STUCK_EVERY")=="1")stuck.Watch(sim,forced,since);}}while(sim.State.ElapsedSeconds<dayEnd-1e-6){sim.Advance(Math.Min(600,dayEnd-sim.State.ElapsedSeconds));stuck.Sample(sim);}}else if(census==null)sim.Advance(windowSeconds);else
    {
        var hourTimer=Stopwatch.StartNew();
        var hourCpu=Process.GetCurrentProcess().TotalProcessorTime;
        var hourAlloc=GC.GetTotalAllocatedBytes();
        var hourGc0=GC.CollectionCount(0);
        var hourGc2=GC.CollectionCount(2);
        for(var i=0;i<43200;i++)
        {
            sim.Advance(2);
            census.Sample(sim);
            if((i+1)%1800!=0)continue;
            var cpuNow=Process.GetCurrentProcess().TotalProcessorTime;
            var allocNow=GC.GetTotalAllocatedBytes();
            var gc0Now=GC.CollectionCount(0);
            var gc2Now=GC.CollectionCount(2);
            census.RecordHourCost(hourTimer.Elapsed.TotalSeconds,(cpuNow-hourCpu).TotalSeconds,(allocNow-hourAlloc)/1e6,gc0Now-hourGc0,gc2Now-hourGc2);
            hourTimer.Restart();
            hourCpu=cpuNow;hourAlloc=allocNow;hourGc0=gc0Now;hourGc2=gc2Now;
        }
    }
    var w=sw.Elapsed.TotalSeconds;total+=w;done=d;
    if(windowSeconds==86400)Console.WriteLine($"DAY {d} wall={w:F2}s eff={86400/w:F0}x {Stats(sim)}");
    else Console.WriteLine($"WINDOW {d} seconds={windowSeconds} wall={w:F2}s eff={windowSeconds/w:F0}x {Stats(sim)}");
    stuck?.Report(sim,d);
    if(total>cap){Console.WriteLine($"STOP limite de {cap}s");break;}
}
if(done>0)Console.WriteLine(windowSeconds==86400
    ? $"SUMMARY days={done} perDay={total/done:F2}s eff={86400*done/total:F0}x hours80y={total/done*29220/3600:F0} gen0={GC.CollectionCount(0)-gc0} gen2={GC.CollectionCount(2)-gc2} allocMB/day={(GC.GetTotalAllocatedBytes()-alloc0)/1e6/done:F0} workingSetMB={Process.GetCurrentProcess().WorkingSet64/1e6:F0}"
    : $"SUMMARY windows={done} secondsEach={windowSeconds} perWindow={total/done:F2}s eff={windowSeconds*done/total:F0}x gen0={GC.CollectionCount(0)-gc0} gen2={GC.CollectionCount(2)-gc2} allocMB/window={(GC.GetTotalAllocatedBytes()-alloc0)/1e6/done:F0} workingSetMB={Process.GetCurrentProcess().WorkingSet64/1e6:F0}");
if(profile is not null){
    var ticks=profile.Ticks.Sum();var measured=ticks/(double)Stopwatch.Frequency;
    Console.WriteLine($"STEP_PROFILE steps={profile.Steps} measuredWall={measured:F3}s phases={StepCostProfile.Names.Length}");
    for(var i=0;i<StepCostProfile.Names.Length;i++)
        Console.WriteLine($"  STEP_PHASE name={StepCostProfile.Names[i]} wall={profile.Ticks[i]/(double)Stopwatch.Frequency:F3}s share={(ticks==0?0:100.0*profile.Ticks[i]/ticks):F2}%");
    for(var i=0;i<StepCostProfile.DetailNames.Length;i++){
        var parent=profile.Ticks[i<6?6:8];
        Console.WriteLine($"  STEP_DETAIL name={StepCostProfile.DetailNames[i]} wall={profile.DetailTicks[i]/(double)Stopwatch.Frequency:F3}s parentShare={(parent==0?0:100.0*profile.DetailTicks[i]/parent):F2}%");
    }
    Console.WriteLine($"VEHICLE_PROFILE steps={profile.VehicleBatchedSteps+profile.VehicleUnsplitSteps} batched={profile.VehicleBatchedSteps} unsplit={profile.VehicleUnsplitSteps} slices={profile.VehicleSlices} avgSlicesPerBatchedStep={(profile.VehicleBatchedSteps==0?0:(double)profile.VehicleSlices/profile.VehicleBatchedSteps):F3}");
    for(var i=0;i<StepCostProfile.VehicleDetailNames.Length;i++)
        Console.WriteLine($"  VEHICLE_DETAIL name={StepCostProfile.VehicleDetailNames[i]} wall={profile.VehicleTicks[i]/(double)Stopwatch.Frequency:F3}s parentShare={(profile.Ticks[0]==0?0:100.0*profile.VehicleTicks[i]/profile.Ticks[0]):F2}%");
    for(var i=0;i<StepCostProfile.VehicleScanNames.Length;i++)
        Console.WriteLine($"  VEHICLE_SCAN name={StepCostProfile.VehicleScanNames[i]} wall={profile.VehicleScanTicks[i]/(double)Stopwatch.Frequency:F3}s visits={profile.VehicleScanVisits[i]} hits={profile.VehicleScanHits[i]} hitRate={(profile.VehicleScanVisits[i]==0?0:100.0*profile.VehicleScanHits[i]/profile.VehicleScanVisits[i]):F4}% shareOfMovement={(profile.VehicleTicks[3]==0?0:100.0*profile.VehicleScanTicks[i]/profile.VehicleTicks[3]):F2}%");
    var laneHits=profile.LaneHoldDistanceBands.Sum();
    if(laneHits!=profile.VehicleScanHits[0])throw new InvalidOperationException("Lane hold distance count differs from actual HoldFor count");
    Console.WriteLine($"LANE_HOLD_RADIUS hits={laneHits} within32={profile.LaneHoldDistanceBands[0]} between32and128={profile.LaneHoldDistanceBands[1]} between128and512={profile.LaneHoldDistanceBands[2]} beyond512={profile.LaneHoldDistanceBands[3]} maxDistanceMeters={Math.Sqrt(profile.LaneHoldMaxDistanceSquared):F2} entryFrameHits={profile.LaneHoldEntryFrameHits}");
    var selected=profile.LaneLeaderSelectedBands.Sum();var limited=profile.LaneLeaderPotentialLimitBands.Sum();
    if(selected>laneHits||limited>selected)throw new InvalidOperationException("Lane leader selection accounting inconsistent");
    Console.WriteLine($"LANE_LEADER_WINNERS selected={selected} within32={profile.LaneLeaderSelectedBands[0]} between32and128={profile.LaneLeaderSelectedBands[1]} between128and512={profile.LaneLeaderSelectedBands[2]} beyond512={profile.LaneLeaderSelectedBands[3]} potentialLimits={limited} limitWithin32={profile.LaneLeaderPotentialLimitBands[0]} limit32to128={profile.LaneLeaderPotentialLimitBands[1]} limit128to512={profile.LaneLeaderPotentialLimitBands[2]} limitBeyond512={profile.LaneLeaderPotentialLimitBands[3]} farZeroDistance={profile.LaneLeaderFarZeroDistance} maxSelectedDistanceMeters={Math.Sqrt(profile.LaneLeaderSelectedMaxDistanceSquared):F2}");
}
census?.Print(done);if(stuck!=null)Console.WriteLine($"STUCK SUMMARY guinchos chamados={sim.State.Ledger.Where(m=>m.Category=="Guincho"&&m.To=="global").Select(m=>(m.At,m.EntityId)).Distinct().Count()} rebocando agora={sim.State.Vehicles.Count(v=>v.Status==VehicleStatus.Towed)} sem combustível na via (total)={stuck.StrandedTotal} parados≥30min (máx)={stuck.StuckMax} mortos={sim.State.Citizens.Count(c=>!c.Alive)}");
sw.Restart();var json=sim.SaveToJson();var save=sw.Elapsed.TotalSeconds;sw.Restart();CitySimulation.FromJson(json);
Console.WriteLine($"SAVE {json.Length/1e6:F1}MB save={save:F2}s load={sw.Elapsed.TotalSeconds:F2}s sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))}");
sim.CheckInvariants();Console.WriteLine("INVARIANTS OK");
return 0;

static string Stats(CitySimulation s){var st=s.State;var alive=st.Citizens.Where(c=>c.Alive&&c.Resident).ToList();return $"pop={s.Population} alive={st.Citizens.Count(c=>c.Alive)} dead={st.Citizens.Count(c=>!c.Alive)} employed={st.Citizens.Count(c=>c.Alive&&c.Resident&&c.WorkplaceId!=null)} moving={st.Citizens.Count(c=>c.Alive&&c.Moving)} vehicles={st.Vehicles.Count} travelling={st.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling)} ledger={st.Ledger.Count} history={st.Citizens.Sum(c=>c.History.Count)} meals={st.Citizens.Sum(c=>c.History.Count(h=>h.Description.StartsWith("Consumiu 1 alimento")))} wages={st.Ledger.Where(l=>l.Category.StartsWith("Salários / ")).Sum(l=>l.Amount):F2} health={(alive.Count==0?0:alive.Average(c=>c.Health)):F6} satisfaction={(alive.Count==0?0:alive.Average(c=>c.Satisfaction)):F6}";}
