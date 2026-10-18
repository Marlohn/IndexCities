using IndexCities.Simulation;

namespace IndexCities.Headless;

// Diagnostic only: samples every 10 simulated minutes and reports vehicles in a
// trip that have not moved for 30+ minutes, vehicles stranded on the road and
// what is near them. Measurement, not a product rule.
sealed class Stuck
{
    readonly Dictionary<int,(Point at,double since)> _still=new();
    readonly HashSet<int> _reported=new();
    readonly HashSet<int> _stranded=new();
    public int StrandedTotal=>_stranded.Count;
    public int StuckMax {get;private set;}
    public void Sample(CitySimulation sim)
    {
        var now=sim.State.ElapsedSeconds;
        foreach(var v in sim.State.Vehicles){
            if(v.Status==VehicleStatus.Stranded&&_stranded.Add(v.Id))Console.WriteLine($"  STRANDED #{v.Id} {(v.PassengerCar?"carro":"caminhão")}{(v.External?" externo":"")} {sim.State.Now:yyyy-MM-dd HH:mm} em ({v.Position.X:F1},{v.Position.Z:F1}) «{v.Cause}»");
            if(v.Status is not (VehicleStatus.Travelling or VehicleStatus.Waiting)){_still.Remove(v.Id);continue;}
            if(_still.TryGetValue(v.Id,out var s)&&s.at.Distance(v.Position)<.5)continue;
            _still[v.Id]=(v.Position,now);
        }
    }
    public void Report(CitySimulation sim,int day)
    {
        var now=sim.State.ElapsedSeconds;var st=sim.State;
        var stuck=_still.Where(kv=>now-kv.Value.since>=1800).Select(kv=>st.Vehicles.First(v=>v.Id==kv.Key)).ToList();StuckMax=Math.Max(StuckMax,stuck.Count);
        var inStuck=st.Citizens.Count(c=>c.Alive&&c.VehicleId!=null&&stuck.Any(v=>v.Id==c.VehicleId));
        var hungry=st.Citizens.Count(c=>c.Alive&&c.HungerHours>30);
        Console.WriteLine($"STUCK day={day} {st.Now:yyyy-MM-dd HH:mm} parados≥30min={stuck.Count} SIMs dentro deles={inStuck} SIMs com fome>30h={hungry} mortos={st.Citizens.Count(c=>!c.Alive)} sem combustível na via={st.Vehicles.Count(v=>v.Status==VehicleStatus.Stranded)} (total {_stranded.Count})");
        foreach(var v in stuck.Where(v=>_reported.Add(v.Id)).Take(30)){
            var near=st.Vehicles.Where(o=>o.Id!=v.Id).OrderBy(o=>o.Position.Distance(v.Position)).Take(2)
                .Select(o=>$"#{o.Id} {o.Status} a {o.Position.Distance(v.Position):F1}m «{o.Cause}» rota {o.RouteIndex}/{o.Route.Count}");
            var node=v.RouteIndex<v.Route.Count?v.Route[v.RouteIndex]:-1;
            Console.WriteLine($"  #{v.Id} {(v.PassengerCar?"carro":"caminhão")} {v.Status} parado há {(now-_still[v.Id].since)/60:F0} min em ({v.Position.X:F1},{v.Position.Z:F1}) rumo {v.Heading*180/Math.PI:F0}° rota {v.RouteIndex}/{v.Route.Count} nó {node} motorista {(v.DriverId?.ToString()??v.ExternalDriverId??"-")} «{v.Cause}»");
            foreach(var n in near)Console.WriteLine($"      perto: {n}");
        }
    }
    // --strand=N: empties the tank of N vehicles driving with someone right behind,
    // so the tow and the queue behind them can be watched (measurement only).
    public List<int> Force(CitySimulation sim,int count)
    {
        var st=sim.State;
        int Behind(Vehicle v)=>st.Vehicles.Count(o=>o.Id!=v.Id&&o.Status==VehicleStatus.Travelling&&Math.Cos(o.Heading-v.Heading)>.9&&(v.Position-o.Position) is var r&&r.X*Math.Sin(v.Heading)+r.Z*Math.Cos(v.Heading) is >0 and <40&&Math.Abs(r.X*Math.Cos(v.Heading)-r.Z*Math.Sin(v.Heading))<2.6);
        var picked=st.Vehicles.Where(v=>v.Status==VehicleStatus.Travelling&&v.WaitingSince==0&&v.Machine==PavingMachine.None&&v.Body is not (TruckBody.Tow or TruckBody.TowHeavy)&&v.RouteIndex>0&&v.RouteIndex<v.Route.Count&&v.Detour.Count==0&&Math.Abs(v.Position.X)<st.Tuning.MapHalfSize-20&&Behind(v)>0).OrderByDescending(Behind).ThenBy(v=>v.Id).Take(count).ToList();
        foreach(var v in picked){v.Fuel=0;Console.WriteLine($"  FORCE #{v.Id} {(v.PassengerCar?"carro":"caminhão")}{(v.External?" externo":"")} {st.Now:HH:mm} em ({v.Position.X:F1},{v.Position.Z:F1}) com {Behind(v)} veículo(s) logo atrás; ocupantes={st.Citizens.Count(c=>c.VehicleId==v.Id)}");}
        return picked.Select(v=>v.Id).ToList();
    }
    public void Watch(CitySimulation sim,List<int> ids,double since)
    {
        var st=sim.State;
        foreach(var id in ids){
            var v=st.Vehicles.First(x=>x.Id==id);var tow=st.Vehicles.FirstOrDefault(x=>x.TowTargetId==id);
            // Queue: vehicles held (waiting) within 60 m behind it, same lane and heading.
            var queue=v.Status==VehicleStatus.Stranded?st.Vehicles.Count(o=>o.Id!=id&&o.WaitingSince>0&&o.Status==VehicleStatus.Travelling&&Math.Cos(o.Heading-v.Heading)>.9&&(v.Position-o.Position) is var r&&r.X*Math.Sin(v.Heading)+r.Z*Math.Cos(v.Heading) is >0 and <60&&Math.Abs(r.X*Math.Cos(v.Heading)-r.Z*Math.Sin(v.Heading))<2.6):0;
            if(tow!=null&&Environment.GetEnvironmentVariable("STUCK_EVERY")=="1")Console.WriteLine($"      alvo h={v.Heading*180/Math.PI:F0} claim={v.IntersectionClaim} guincho doca={tow.DockPosition} rota guincho={string.Join(",",tow.Route.Skip(Math.Max(0,tow.RouteIndex-1)).Take(8).Select(n=>st.Roads.First(r=>r.Id==n) is var r?$"{n}({r.Position.X:F0},{r.Position.Z:F0})/{r.Neighbours.Count}":""))} perto do alvo: {string.Join("; ",st.Vehicles.Where(o=>o.Id!=id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(v.Position)<14).Select(o=>$"#{o.Id} {o.Status} ({o.Position.X:F1},{o.Position.Z:F1}) h={o.Heading*180/Math.PI:F0} claim={o.IntersectionClaim} w={o.WaitingSince:F0} «{o.Cause}» dest={o.Route.LastOrDefault()}"))}");
            if(tow!=null&&Environment.GetEnvironmentVariable("STUCK_EVERY")=="1")foreach(var o in st.Vehicles.Where(o=>o.Id!=tow.Id&&o.Status!=VehicleStatus.Outside).OrderBy(o=>o.Position.Distance(tow.Position)).Take(2))Console.WriteLine($"      perto: #{o.Id} {o.Status} ({o.Position.X:F1},{o.Position.Z:F1}) h={o.Heading*180/Math.PI:F0} rota {o.RouteIndex}/{o.Route.Count} esperando desde {o.WaitingSince:F0} (agora {st.ElapsedSeconds:F0}) claim={o.IntersectionClaim} «{o.Cause}»");
            Console.WriteLine($"    t+{(st.ElapsedSeconds-since)/60,3:F0}min #{id} {v.Status} fila atrás={queue} guincho={(tow==null?"-":$"#{tow.Id} {tow.TowPhase} {tow.Status} em ({tow.Position.X:F1},{tow.Position.Z:F1}) rota {tow.RouteIndex}/{tow.Route.Count} «{tow.Cause}»")} «{v.Cause}» ocupantes={st.Citizens.Count(c=>c.VehicleId==id)}");
        }
    }
}
