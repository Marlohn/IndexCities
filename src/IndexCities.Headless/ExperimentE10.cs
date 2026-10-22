using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using IndexCities.Simulation;

namespace IndexCities.Headless;

// E10: experimento observacional sobre o motor REAL, não um motor de tráfego alternativo.
// Um escalonador-sombra mantém assinaturas de veículo/líder e avalia se seria seguro
// evitar a próxima rechecagem; o motor original continua executando todos os passos.
// Não prevê deslocamentos, não altera o estado, e portanto NÃO comprova speedup.
internal static class ExperimentE10
{
    readonly record struct Stamp(
        double X,double Z,double Heading,VehicleStatus Status,int RouteIndex,
        int DetourCount,int? Claim,int SpaceCount,string Cause,double WaitingSince,
        long FuelUnits,long LoadTimerUnits,bool BrokenDown,TowPhase TowPhase,
        int SitePathIndex,int? TowTargetId,int? TowTruckId)
    {
        internal static Stamp Of(Vehicle v)=>new(
            v.Position.X,v.Position.Z,v.Heading,v.Status,v.RouteIndex,
            v.Detour.Count,v.IntersectionClaim,v.ManoeuvreSpaces.Count,v.Cause,v.WaitingSince,
            v.FuelUnits,v.LoadTimerUnits,v.BrokenDown,v.TowPhase,
            v.SitePathIndex,v.TowTargetId,v.TowTruckId);
    }
    readonly record struct Sleep(int LeaderId,double Deadline,Stamp Vehicle,Stamp Leader);

    internal static int Run(int rows,int length,int seed,int minutes,bool force)
    {
        Console.WriteLine($"E10_SETUP rows={rows} length={length} seed={seed} minutes={minutes} force={force}");
        var timer=Stopwatch.StartNew();
        var (candidate,info)=Fixtures.Grid(seed,rows,length,true);
        Console.WriteLine($"E10_FIXTURE {info} constructionWall={timer.Elapsed.TotalSeconds:F2}s");
        timer.Restart();
        candidate.Advance(86400); // Primeiro dia: faz a cidade crescer, ao contrario da bancada vazia.
        Console.WriteLine($"E10_WARMUP pop={candidate.Population} vehicles={candidate.State.Vehicles.Count} wall={timer.Elapsed.TotalSeconds:F2}s");
        int forced=0;
        if(force)
        {
            var tool=new Stuck();
            for(var i=0;i<60;i++)
            {
                var ids=tool.Force(candidate,1);
                if(ids.Count>0){forced=ids.Count;break;}
                candidate.Advance(2);
            }
            Console.WriteLine($"E10_BREAKDOWN forced={forced} (0 significa sem alvo elegivel)");
        }
        // Comparacao isolada: clones do MESMO save; modo sombra nao recebe atalhos.
        var snapshot=candidate.SaveToJson();
        var baseline=CitySimulation.FromJson(snapshot);
        timer.Restart();
        var steps=minutes*30; // 2 s cada
        for(var i=0;i<steps;i++)baseline.Advance(2);
        var baselineWall=timer.Elapsed.TotalSeconds;
        var baselineHash=Digest(baseline.SaveToJson());
        baseline.CheckInvariants();

        var scheduler=new Dictionary<int,Sleep>();
        long candidates=0,quiet=0,misses=0,leaderChanges=0,timeWakes=0,
            selfWakes=0,newSubscriptions=0,leaderChangedDuringTick=0,
            realVehicleChanges=0,observedTravelling=0;
        var firstMisses=new List<string>();
        var observationTicks=0L;
        timer.Restart();
        for(var step=0;step<steps;step++)
        {
            var now=candidate.State.ElapsedSeconds;
            // Sinal de acordar: mudanca no bloqueador ou prazo maximo de 60 s,
            // adiantado para o prazo atual de meia-volta. Nada dorme alem de prazo.
            var byId=candidate.State.Vehicles.ToDictionary(v=>v.Id);
            var ready=new List<(int VehicleId,int LeaderId,Stamp Vehicle,Stamp Leader)>();
            var instrumentation=Stopwatch.GetTimestamp();
            foreach(var v in candidate.State.Vehicles)
            {
                if(v.Status!=VehicleStatus.Travelling)continue;
                observedTravelling++;
                if(!scheduler.TryGetValue(v.Id,out var sub))
                {
                    var leader=StationaryLeader(v,candidate.State.Vehicles);
                    if(leader is null)continue;
                    var until=Math.Min(now+60,v.WaitingSince+candidate.State.Tuning.UTurnWaitSeconds);
                    if(until<=now+2)continue;
                    scheduler[v.Id]=new Sleep(leader.Id,until,Stamp.Of(v),Stamp.Of(leader));
                    newSubscriptions++;
                    continue;
                }
                if(!byId.TryGetValue(sub.LeaderId,out var blocking)||Stamp.Of(blocking)!=sub.Leader)
                {
                    leaderChanges++;scheduler.Remove(v.Id);continue;
                }
                if(now+2>=sub.Deadline){timeWakes++;scheduler.Remove(v.Id);continue;}
                if(Stamp.Of(v)!=sub.Vehicle){selfWakes++;scheduler.Remove(v.Id);continue;}
                ready.Add((v.Id,sub.LeaderId,sub.Vehicle,sub.Leader));
                candidates++;
            }
            observationTicks+=Stopwatch.GetTimestamp()-instrumentation;
            candidate.Advance(2);
            instrumentation=Stopwatch.GetTimestamp();
            var after=candidate.State.Vehicles.ToDictionary(v=>v.Id);
            foreach(var item in ready)
            {
                if(!after.TryGetValue(item.VehicleId,out var subject)||!after.TryGetValue(item.LeaderId,out var blocking))
                {
                    misses++;scheduler.Remove(item.VehicleId);
                    if(firstMisses.Count<6)firstMisses.Add($"missing-entity step={step} vehicle={item.VehicleId}");
                    continue;
                }
                var vChanged=Stamp.Of(subject)!=item.Vehicle;
                var leaderChanged=Stamp.Of(blocking)!=item.Leader;
                if(vChanged)realVehicleChanges++;
                if(leaderChanged)leaderChangedDuringTick++;
                // Ultra-conservador: ate a alteracao do lider NESTE passo invalida
                // a dispensa; o processamento poderia depender da ordem intratick.
                if(vChanged||leaderChanged)
                {
                    misses++;scheduler.Remove(item.VehicleId);
                    if(firstMisses.Count<6)firstMisses.Add($"step={step} vehicle={item.VehicleId} leader={item.LeaderId} ownChange={vChanged} leaderChange={leaderChanged} cause={subject.Cause}");
                }
                else quiet++;
            }
            observationTicks+=Stopwatch.GetTimestamp()-instrumentation;
        }
        var candidateWall=timer.Elapsed.TotalSeconds;
        candidate.CheckInvariants();
        var candidateHash=Digest(candidate.SaveToJson());
        var equal=baselineHash==candidateHash;
        Console.WriteLine($"E10_RESULT pop={candidate.Population} vehicles={candidate.State.Vehicles.Count} seconds={minutes*60} steps={steps} baselineWall={baselineWall:F3}s shadowWall={candidateWall:F3}s observerWall={observationTicks/(double)Stopwatch.Frequency:F3}s");
        Console.WriteLine($"E10_EVENTS travellingObserved={observedTravelling} subscribed={newSubscriptions} candidateSkippedChecks={candidates} quiet={quiet} invalidWithinTick={misses} leaderDuringTick={leaderChangedDuringTick} changedVehicles={realVehicleChanges} wakeByLeader={leaderChanges} wakeByTime={timeWakes} wakeBySelf={selfWakes}");
        foreach(var s in firstMisses)Console.WriteLine("E10_COUNTEREXAMPLE "+s);
        Console.WriteLine($"E10_SAVES baseline={baselineHash} shadow={candidateHash} identical={equal}");
        Console.WriteLine("E10_LIMIT Uma oportunidade 'quiet' NAO permite pular a simulacao inteira: os demais carros, pedestres, economia, guincho e colisao permanecem em passos reais. O observador usa verificacao retrospectiva; nao e scheduler seguro nem benchmark de ganho integrado.");
        if(!equal){Console.Error.WriteLine("E10_FAIL save divergente embora experimento devesse ser observacional");return 1;}
        Console.WriteLine("E10_OK baseline/shadow causally identical (same original engine)");
        return 0;
    }

    static Vehicle? StationaryLeader(Vehicle v,List<Vehicle> vehicles)
    {
        if(v.WaitingSince<=0||v.Detour.Count>0||v.RouteIndex>=v.Route.Count)return null;
        var si=Math.Sin(v.Heading);var co=Math.Cos(v.Heading);
        Vehicle? best=null;var nearest=8.0;
        foreach(var o in vehicles)
        {
            if(o.Id==v.Id||o.Status is not (VehicleStatus.Travelling or VehicleStatus.Stranded)||o.Status==VehicleStatus.Travelling&&o.WaitingSince<=0)continue;
            if(Math.Cos(o.Heading-v.Heading)<=.5)continue;
            var delta=o.Position-v.Position;
            var ahead=delta.X*si+delta.Z*co;
            if(ahead<=0||ahead>=nearest||Math.Abs(delta.X*co-delta.Z*si)>=2.6)continue;
            best=o;nearest=ahead;
        }
        return best;
    }

    static string Digest(string json)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))[..16];
}
