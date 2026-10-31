using System.Text.Json;
namespace IndexCities.Simulation;

// PR57 #552: opt-in writer publication and full persisted-entity shadow.
// This is NOT a scheduler. No Step, actor, transaction or phase is skipped.
// Phase-boundary net deltas miss mutations undone within a phase; no result
// may be used as a causal certificate until all writers are closed.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Writer552Enabled=Environment.GetEnvironmentVariable("PR57_PHASE_WRITER_JOURNAL")=="1";
    const int Pr57Writer552Sample=32;
    readonly HashSet<int> _pr57Writer552Vehicles=new(),_pr57Writer552Citizens=new(),_pr57Writer552Projects=new();
    readonly long[,] _pr57Writer552Changes=new long[13,3],_pr57Writer552Misses=new long[13,3];
    readonly long[] _pr57Writer552Order=new long[13],_pr57Writer552Touches=new long[3];
    int _pr57Writer552Steps;
    bool _pr57Writer552Active;
    Pr57Writer552Frame? _pr57Writer552Previous;
    sealed class Pr57Writer552Frame
    {
        public Dictionary<int,string> Vehicles {get;}=new(),Citizens {get;}=new(),Projects {get;}=new();
        public List<int> VehicleOrder {get;}=new(),CitizenOrder {get;}=new(),ProjectOrder {get;}=new();
    }
    Pr57Writer552Frame Pr57Writer552Capture()
    {
        var f=new Pr57Writer552Frame();
        foreach(var v in State.Vehicles){f.VehicleOrder.Add(v.Id);f.Vehicles.Add(v.Id,JsonSerializer.Serialize(v,SaveOptions));}
        foreach(var c in State.Citizens){f.CitizenOrder.Add(c.Id);f.Citizens.Add(c.Id,JsonSerializer.Serialize(c,SaveOptions));}
        foreach(var p in State.Projects){f.ProjectOrder.Add(p.Id);f.Projects.Add(p.Id,JsonSerializer.Serialize(p,SaveOptions));}
        return f;
    }
    void Pr57Writer552Begin()
    {
        if(!Pr57Writer552Enabled)return;
        _pr57Writer552Steps++;
        _pr57Writer552Active=_pr57Writer552Steps<=Pr57Writer552Sample;
        if(!_pr57Writer552Active)return;
        _pr57Writer552Vehicles.Clear();_pr57Writer552Citizens.Clear();_pr57Writer552Projects.Clear();
        _pr57Writer552Previous=Pr57Writer552Capture();
    }
    void Pr57Writer552Vehicle(int id){if(_pr57Writer552Active)_pr57Writer552Vehicles.Add(id);}
    void Pr57Writer552Citizen(int id){if(_pr57Writer552Active)_pr57Writer552Citizens.Add(id);}
    void Pr57Writer552Project(int id){if(_pr57Writer552Active)_pr57Writer552Projects.Add(id);}
    static (int Changed,int Missed) Pr57Writer552Compare(Dictionary<int,string> old,Dictionary<int,string> now,HashSet<int> touched)
    {
        var changed=0;var missed=0;
        foreach(var pair in now)
        {
            if(old.TryGetValue(pair.Key,out var previous)&&string.Equals(previous,pair.Value,StringComparison.Ordinal))continue;
            changed++;if(!touched.Contains(pair.Key))missed++;
        }
        foreach(var id in old.Keys)if(!now.ContainsKey(id)){changed++;if(!touched.Contains(id))missed++;}
        return (changed,missed);
    }
    void Pr57Writer552Boundary(int phase)
    {
        if(!_pr57Writer552Active)return;
        var old=_pr57Writer552Previous??throw new InvalidOperationException("PR57 #552 baseline missing");
        var now=Pr57Writer552Capture();
        var counts=new[]{
            Pr57Writer552Compare(old.Vehicles,now.Vehicles,_pr57Writer552Vehicles),
            Pr57Writer552Compare(old.Citizens,now.Citizens,_pr57Writer552Citizens),
            Pr57Writer552Compare(old.Projects,now.Projects,_pr57Writer552Projects)
        };
        for(var type=0;type<3;type++)
        {_pr57Writer552Changes[phase,type]+=counts[type].Changed;_pr57Writer552Misses[phase,type]+=counts[type].Missed;}
        if(!old.VehicleOrder.SequenceEqual(now.VehicleOrder)||!old.CitizenOrder.SequenceEqual(now.CitizenOrder)||!old.ProjectOrder.SequenceEqual(now.ProjectOrder))
            _pr57Writer552Order[phase]++;
        _pr57Writer552Touches[0]+=_pr57Writer552Vehicles.Count;
        _pr57Writer552Touches[1]+=_pr57Writer552Citizens.Count;
        _pr57Writer552Touches[2]+=_pr57Writer552Projects.Count;
        _pr57Writer552Vehicles.Clear();_pr57Writer552Citizens.Clear();_pr57Writer552Projects.Clear();
        _pr57Writer552Previous=now;
    }
    void Pr57Writer552End()
    {
        if(!_pr57Writer552Active)return;
        _pr57Writer552Previous=null;_pr57Writer552Active=false;
        if(_pr57Writer552Steps!=Pr57Writer552Sample)return;
        var fallback=0;
        for(var i=0;i<13;i++)
        {
            if(_pr57Writer552Misses[i,0]+_pr57Writer552Misses[i,1]+_pr57Writer552Misses[i,2]+_pr57Writer552Order[i]>0)fallback++;
            Console.WriteLine($"PR57_552_PHASE name={StepCostProfile.Names[i]} vehicleChanged={_pr57Writer552Changes[i,0]} vehicleUncovered={_pr57Writer552Misses[i,0]} citizenChanged={_pr57Writer552Changes[i,1]} citizenUncovered={_pr57Writer552Misses[i,1]} projectChanged={_pr57Writer552Changes[i,2]} projectUncovered={_pr57Writer552Misses[i,2]} orderChanged={_pr57Writer552Order[i]}");
        }
        Console.WriteLine($"PR57_552_SUM steps={_pr57Writer552Steps} fallbackPhases={fallback} publishedVehicle={_pr57Writer552Touches[0]} publishedCitizen={_pr57Writer552Touches[1]} publishedProject={_pr57Writer552Touches[2]} scope=BOUNDARY_NET_ONLY_NO_CAUSAL_CERTIFICATE_NO_SKIP");
    }
}
