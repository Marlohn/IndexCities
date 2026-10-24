using IndexCities.Simulation;

namespace IndexCities.Headless;

// Classifies every agent after each 2 s step: idle, moving freely, or held by
// an interaction (pedestrian queue, crossing, lane queue, junction, access).
// The share of free movement bounds what an event-driven mobility could skip.
sealed class Census
{
    long _citizenSteps,_walkFree,_walkQueue,_walkCrossing,_inVehicle,_vehicleSteps,_driveFree,_steps,_walkStarts,_driveStarts;
    long _activeDriveSteps; int _maxWalkers,_maxSimMoving,_maxDrivers,_maxHeld;
    sealed class HourLoad { public long Samples,Walkers,InVehicle,Drivers,Held; public double WallSeconds,CpuSeconds,AllocatedMb; public int Gc0,Gc2; public bool HasCost; }
    readonly List<HourLoad> _hours=new();
    readonly Dictionary<string,long> _driveHeld=new();
    readonly HashSet<int> _walking=new(),_driving=new();
    public void Sample(CitySimulation sim)
    {
        _steps++;
        var walkNow=0;var inVehicleNow=0;var driveNow=0;var heldNow=0;
        foreach(var c in sim.State.Citizens){
            if(!c.Alive)continue;_citizenSteps++;
            if(c.VehicleId!=null){_inVehicle++;inVehicleNow++;_walking.Remove(c.Id);continue;}
            if(!c.Moving){_walking.Remove(c.Id);continue;}
            walkNow++;
            if(_walking.Add(c.Id))_walkStarts++;
            if(c.Activity=="Fila de pedestres")_walkQueue++;else if(c.Activity=="Aguardando travessia")_walkCrossing++;else _walkFree++;
        }
        foreach(var v in sim.State.Vehicles){
            _vehicleSteps++;
            if(v.Status!=VehicleStatus.Travelling){_driving.Remove(v.Id);continue;}
            driveNow++;
            if(_driving.Add(v.Id))_driveStarts++;
            var held=v.Cause.StartsWith("Fila",StringComparison.Ordinal)||v.Cause.StartsWith("Aguardando",StringComparison.Ordinal);
            if(!held){_driveFree++;continue;}
            heldNow++;
            var key=System.Text.RegularExpressions.Regex.Replace(v.Cause,@"\d+","N");
            _driveHeld[key]=_driveHeld.GetValueOrDefault(key)+1;
        }
        _maxWalkers=Math.Max(_maxWalkers,walkNow);
        _maxSimMoving=Math.Max(_maxSimMoving,walkNow+inVehicleNow);
        _maxDrivers=Math.Max(_maxDrivers,driveNow);
        _maxHeld=Math.Max(_maxHeld,heldNow);
        if(driveNow>0)_activeDriveSteps++;
        var hourIndex=(int)((_steps-1)/1800); // 1800 samples = one simulated hour
        while(_hours.Count<=hourIndex)_hours.Add(new HourLoad());
        var h=_hours[hourIndex];h.Samples++;h.Walkers+=walkNow;h.InVehicle+=inVehicleNow;h.Drivers+=driveNow;h.Held+=heldNow;
    }
    // Observational timing only: no simulation state is read or changed here.
    // Each call follows exactly 1,800 two-second samples (one simulated hour).
    public void RecordHourCost(double wallSeconds,double cpuSeconds,double allocatedMb,int gc0,int gc2)
    {
        var h=_hours[^1];
        if(h.Samples!=1800||h.HasCost)throw new InvalidOperationException("Incomplete or duplicate census hour");
        h.WallSeconds=wallSeconds;h.CpuSeconds=cpuSeconds;h.AllocatedMb=allocatedMb;
        h.Gc0=gc0;h.Gc2=gc2;h.HasCost=true;
    }
    public void Print(int days)
    {
        if(days==0)return;var walking=_walkFree+_walkQueue+_walkCrossing;var driving=_driveFree+_driveHeld.Values.Sum();
        string P(long a,long b)=>b==0?"-":(100.0*a/b).ToString("F1")+"%";
        Console.WriteLine($"CENSUS steps={_steps} citizenSteps={_citizenSteps} vehicleSteps={_vehicleSteps}");
        Console.WriteLine($"  PICOS caminhandoSimultaneosMax={_maxWalkers} simsAndandoOuEmVeiculoSimultaneosMax={_maxSimMoving} veiculosEmViagemSimultaneosMax={_maxDrivers} veiculosRetidosSimultaneosMax={_maxHeld}");
        Console.WriteLine($"  CARGA_HORARIA horas={_hours.Count} picoAndandoMedio={_hours.Max(h=>h.Walkers/(double)h.Samples):F2} picoEmVeiculoMedio={_hours.Max(h=>h.InVehicle/(double)h.Samples):F2} picoVeiculosViajandoMedio={_hours.Max(h=>h.Drivers/(double)h.Samples):F2} picoVeiculosRetidosMedio={_hours.Max(h=>h.Held/(double)h.Samples):F2} horasComVeiculoEmViagem={_hours.Count(h=>h.Drivers>0)} passosComVeiculoEmViagem={P(_activeDriveSteps,_steps)}");
        for(var hour=0;hour<_hours.Count;hour++)
        {
            var h=_hours[hour];
            if(!h.HasCost)continue;
            Console.WriteLine($"  HOUR_COST day={hour/24+1} hour={hour%24} wall={h.WallSeconds:F3} cpu={h.CpuSeconds:F3} allocMB={h.AllocatedMb:F1} gc0={h.Gc0} gc2={h.Gc2} avgWalk={h.Walkers/(double)h.Samples:F2} avgDriving={h.Drivers/(double)h.Samples:F2} avgHeld={h.Held/(double)h.Samples:F2}");
        }
        Console.WriteLine($"  SIMs parados (casa/trabalho/loja): {P(_citizenSteps-walking-_inVehicle,_citizenSteps)}  andando: {P(walking,_citizenSteps)}  em veículo: {P(_inVehicle,_citizenSteps)}");
        Console.WriteLine($"  caminhada livre: {P(_walkFree,walking)}  fila de pedestres: {P(_walkQueue,walking)}  aguardando travessia: {P(_walkCrossing,walking)}  caminhadas iniciadas/dia: {_walkStarts/(double)days:F0}");
        Console.WriteLine($"  veículos em viagem: {P(driving,_vehicleSteps)}  livres: {P(_driveFree,driving)}  viagens iniciadas/dia: {_driveStarts/(double)days:F0}");
        foreach(var kv in _driveHeld.OrderByDescending(kv=>kv.Value))Console.WriteLine($"    retido \"{kv.Key}\": {P(kv.Value,driving)}");
    }
}
