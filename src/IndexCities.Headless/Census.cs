using IndexCities.Simulation;

namespace IndexCities.Headless;

// Classifies every agent after each 2 s step: idle, moving freely, or held by
// an interaction (pedestrian queue, crossing, lane queue, junction, access).
// The share of free movement bounds what an event-driven mobility could skip.
sealed class Census
{
    long _citizenSteps,_walkFree,_walkQueue,_walkCrossing,_inVehicle,_vehicleSteps,_driveFree,_steps,_walkStarts,_driveStarts;
    readonly Dictionary<string,long> _driveHeld=new();
    readonly HashSet<int> _walking=new(),_driving=new();
    public void Sample(CitySimulation sim)
    {
        _steps++;
        foreach(var c in sim.State.Citizens){
            if(!c.Alive)continue;_citizenSteps++;
            if(c.VehicleId!=null){_inVehicle++;_walking.Remove(c.Id);continue;}
            if(!c.Moving){_walking.Remove(c.Id);continue;}
            if(_walking.Add(c.Id))_walkStarts++;
            if(c.Activity=="Fila de pedestres")_walkQueue++;else if(c.Activity=="Aguardando travessia")_walkCrossing++;else _walkFree++;
        }
        foreach(var v in sim.State.Vehicles){
            _vehicleSteps++;
            if(v.Status!=VehicleStatus.Travelling){_driving.Remove(v.Id);continue;}
            if(_driving.Add(v.Id))_driveStarts++;
            var held=v.Cause.StartsWith("Fila",StringComparison.Ordinal)||v.Cause.StartsWith("Aguardando",StringComparison.Ordinal);
            if(!held){_driveFree++;continue;}
            var key=System.Text.RegularExpressions.Regex.Replace(v.Cause,@"\d+","N");
            _driveHeld[key]=_driveHeld.GetValueOrDefault(key)+1;
        }
    }
    public void Print(int days)
    {
        if(days==0)return;var walking=_walkFree+_walkQueue+_walkCrossing;var driving=_driveFree+_driveHeld.Values.Sum();
        string P(long a,long b)=>b==0?"-":(100.0*a/b).ToString("F1")+"%";
        Console.WriteLine($"CENSUS steps={_steps} citizenSteps={_citizenSteps} vehicleSteps={_vehicleSteps}");
        Console.WriteLine($"  SIMs parados (casa/trabalho/loja): {P(_citizenSteps-walking-_inVehicle,_citizenSteps)}  andando: {P(walking,_citizenSteps)}  em veículo: {P(_inVehicle,_citizenSteps)}");
        Console.WriteLine($"  caminhada livre: {P(_walkFree,walking)}  fila de pedestres: {P(_walkQueue,walking)}  aguardando travessia: {P(_walkCrossing,walking)}  caminhadas iniciadas/dia: {_walkStarts/(double)days:F0}");
        Console.WriteLine($"  veículos em viagem: {P(driving,_vehicleSteps)}  livres: {P(_driveFree,driving)}  viagens iniciadas/dia: {_driveStarts/(double)days:F0}");
        foreach(var kv in _driveHeld.OrderByDescending(kv=>kv.Value))Console.WriteLine($"    retido \"{kv.Key}\": {P(kv.Value,driving)}");
    }
}
