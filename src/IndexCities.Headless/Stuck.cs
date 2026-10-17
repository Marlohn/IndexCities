using IndexCities.Simulation;

namespace IndexCities.Headless;

// Diagnostic only: samples every 10 simulated minutes and reports vehicles in
// a trip whose position has not changed for 30+ minutes, with what is near them.
sealed class Stuck
{
    readonly Dictionary<int,(Point at,double since)> _still=new();
    readonly HashSet<int> _reported=new();
    public void Sample(CitySimulation sim)
    {
        var now=sim.State.ElapsedSeconds;
        foreach(var v in sim.State.Vehicles){
            if(v.Status is not (VehicleStatus.Travelling or VehicleStatus.Waiting)){_still.Remove(v.Id);continue;}
            if(_still.TryGetValue(v.Id,out var s)&&s.at.Distance(v.Position)<.5)continue;
            _still[v.Id]=(v.Position,now);
        }
    }
    public void Report(CitySimulation sim,int day)
    {
        var now=sim.State.ElapsedSeconds;var st=sim.State;
        var stuck=_still.Where(kv=>now-kv.Value.since>=1800).Select(kv=>st.Vehicles.First(v=>v.Id==kv.Key)).ToList();
        var inStuck=st.Citizens.Count(c=>c.Alive&&c.VehicleId!=null&&stuck.Any(v=>v.Id==c.VehicleId));
        var hungry=st.Citizens.Count(c=>c.Alive&&c.HungerHours>30);
        Console.WriteLine($"STUCK day={day} {sim.State.Now:yyyy-MM-dd HH:mm} parados≥30min={stuck.Count} SIMs dentro deles={inStuck} SIMs com fome>30h={hungry} mortos={st.Citizens.Count(c=>!c.Alive)}");
        foreach(var v in stuck.Where(v=>_reported.Add(v.Id)).Take(30)){
            var near=st.Vehicles.Where(o=>o.Id!=v.Id).OrderBy(o=>o.Position.Distance(v.Position)).Take(2)
                .Select(o=>$"#{o.Id} {o.Status} a {o.Position.Distance(v.Position):F1}m «{o.Cause}» rota {o.RouteIndex}/{o.Route.Count}");
            var node=v.RouteIndex<v.Route.Count?v.Route[v.RouteIndex]:-1;
            Console.WriteLine($"  #{v.Id} {(v.PassengerCar?"carro":"caminhão")} {v.Status} parado há {(now-_still[v.Id].since)/60:F0} min em ({v.Position.X:F1},{v.Position.Z:F1}) rumo {v.Heading*180/Math.PI:F0}° rota {v.RouteIndex}/{v.Route.Count} nó {node} motorista {(v.DriverId?.ToString()??v.ExternalDriverId??"-")} «{v.Cause}»");
            foreach(var n in near)Console.WriteLine($"      perto: {n}");
        }
    }
}
