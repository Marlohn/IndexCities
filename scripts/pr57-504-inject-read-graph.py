#!/usr/bin/env python3
"""PR57 #504: disposable CI-only four-reader candidate graph. Not exact causality."""
from pathlib import Path
root=Path("src/IndexCities.Simulation")
shared=root/"Mobility.SharedSpace.cs"
mob=root/"Mobility.cs"
def within(s,start,end,old,new):
    a=s.index(start);b=s.index(end,a+len(start));part=s[a:b]
    assert part.count(old)==1,(start,old,part.count(old))
    return s[:a]+part.replace(old,new,1)+s[b:]
s=shared.read_text()
for start,end,kind,guard in [
 ("    Vehicle? BodyOnWay(","    Vehicle? TrafficOnWay(",0,
  "if(o.Id==v.Id||o.Id==v.TowTargetId||o.TowTruckId==v.Id||o.Status is VehicleStatus.Outside or VehicleStatus.Towed||o.Position.Distance(v.Position)>reach||AtGateway(o)&&AtGateway(v))continue;"),
 ("    Vehicle? TrafficOnWay(","    List<(Vehicle Body,Point Position,double Heading)> TurnWay(",1,
  "if(o.Id==v.Id||o.Id==v.TowTargetId||o.Status!=VehicleStatus.Travelling||o.WaitingSince>0||Leaving(o)||o.Machine!=PavingMachine.None||o.Position.Distance(v.Position)>seconds*State.Tuning.HighwaySpeed+20)continue;"),
 ("    bool SpaceFree(","    bool AdmitManoeuvre(",2,
  "if(o.Id==v.Id||o.Id==v.TowTargetId||o.TowTruckId==v.Id||Receives(o,v))continue;"),
]:
    s=within(s,start,end,"foreach(var o in State.Vehicles){",
        f"foreach(var o in State.Vehicles){{Pr57504Read(v,o,{kind},false);")
    s=within(s,start,end,guard,guard+f"Pr57504Read(v,o,{kind},true);")
shared.write_text(s)
s=mob.read_text()
anchor="        Pr57Man248BeginSlice(dt,operationsDt,scanProfile is not null);"
assert s.count(anchor)==1
s=s.replace(anchor,anchor+"\n        Pr57504BeginSlice(dt,operationsDt);",1)
anchor="        // A towed vehicle moves only with its tow (platform or trailing a heavy tow)."
assert s.count(anchor)==1
s=s.replace(anchor,"        Pr57504EndSlice();\n"+anchor,1)
s=within(s,"    bool JunctionPreferenceAllows(","    // The body v queues behind",
    "var competitors=State.Vehicles.Where(o=>",
    "var competitors=State.Vehicles.Where(o=>Pr57504Read(v,o,3,false)).Where(o=>")
s=within(s,"    bool JunctionPreferenceAllows(","    // The body v queues behind",
    "Pr57JunctionReadersPreferenceCandidates(v,competitors);",
    "Pr57JunctionReadersPreferenceCandidates(v,competitors);foreach(var o in competitors)Pr57504Read(v,o,3,true);")
mob.write_text(s)
probe=r'''namespace IndexCities.Simulation;
// #504 observational, disposable only. Negative candidate reads and net endpoint
// changes are not a complete causal dependency or writer journal.
public sealed partial class CitySimulation
{
    static readonly bool Pr57504Enabled=
        Environment.GetEnvironmentVariable("PR57_504_READ_GRAPH")=="1";
    bool _pr57504Active;
    int _pr57504Samples;
    readonly HashSet<(int Reader,int Other)> _pr57504Raw=new();
    readonly HashSet<(int Reader,int Other)> _pr57504Eligible=new();
    readonly long[] _pr57504RawCalls=new long[4];
    readonly long[] _pr57504EligibleCalls=new long[4];
    Dictionary<int,string>? _pr57504Before;
    void Pr57504BeginSlice(double dt,double? operationsDt)
    {
        _pr57504Active=Pr57504Enabled&&_pr57504Samples<3&&dt>0&&
            operationsDt is null&&_vehicleMovementBatch is not null;
        if(!_pr57504Active)return;
        _pr57504Samples++;
        _pr57504Raw.Clear();_pr57504Eligible.Clear();
        Array.Clear(_pr57504RawCalls);Array.Clear(_pr57504EligibleCalls);
        _pr57504Before=State.Vehicles.ToDictionary(v=>v.Id,Pr57MutationSnapshot);
    }
    bool Pr57504Read(Vehicle reader,Vehicle other,int kind,bool eligible)
    {
        if(_pr57504Active)
        {
            if(eligible){_pr57504EligibleCalls[kind]++;_pr57504Eligible.Add((reader.Id,other.Id));}
            else{_pr57504RawCalls[kind]++;_pr57504Raw.Add((reader.Id,other.Id));}
        }
        return true;
    }
    static (int Components,int Largest) Pr57504Components(int[] ids,
        HashSet<(int Reader,int Other)> edges)
    {
        var parent=ids.ToDictionary(id=>id,id=>id);
        int Root(int id)
        {
            while(parent[id]!=id){parent[id]=parent[parent[id]];id=parent[id];}
            return id;
        }
        foreach(var (reader,other) in edges)
        {
            if(!parent.ContainsKey(reader)||!parent.ContainsKey(other))continue;
            var a=Root(reader);var b=Root(other);
            if(a!=b)parent[b]=a;
        }
        var groups=new Dictionary<int,int>();
        foreach(var id in ids){var r=Root(id);groups[r]=groups.GetValueOrDefault(r)+1;}
        return (groups.Count,groups.Values.DefaultIfEmpty(0).Max());
    }
    void Pr57504EndSlice()
    {
        if(!_pr57504Active||_pr57504Before is null)return;
        var after=State.Vehicles.ToDictionary(v=>v.Id,Pr57MutationSnapshot);
        var changed=new HashSet<int>();
        foreach(var (id,state) in after)
            if(!_pr57504Before.TryGetValue(id,out var prior)||prior!=state)changed.Add(id);
        foreach(var id in _pr57504Before.Keys)if(!after.ContainsKey(id))changed.Add(id);
        var ids=State.Vehicles.Select(v=>v.Id).ToArray();
        var raw=Pr57504Components(ids,_pr57504Raw);
        var eligible=Pr57504Components(ids,_pr57504Eligible);
        var rawChanged=_pr57504Raw.Count(p=>changed.Contains(p.Other));
        var eligibleChanged=_pr57504Eligible.Count(p=>changed.Contains(p.Other));
        Console.WriteLine($"PR57_504_READ_GRAPH sample={_pr57504Samples} fleet={ids.Length} "+
            $"rawEdges={_pr57504Raw.Count} rawComponents={raw.Components} rawLargest={raw.Largest} "+
            $"eligibleEdges={_pr57504Eligible.Count} eligibleComponents={eligible.Components} "+
            $"eligibleLargest={eligible.Largest} netChangedVehicles={changed.Count} "+
            $"rawNetChangedEdges={rawChanged} eligibleNetChangedEdges={eligibleChanged} "+
            $"bodyRaw={_pr57504RawCalls[0]} bodyEligible={_pr57504EligibleCalls[0]} "+
            $"trafficRaw={_pr57504RawCalls[1]} trafficEligible={_pr57504EligibleCalls[1]} "+
            $"spaceRaw={_pr57504RawCalls[2]} spaceEligible={_pr57504EligibleCalls[2]} "+
            $"preferenceRaw={_pr57504RawCalls[3]} preferenceEligible={_pr57504EligibleCalls[3]} "+
            "scope=FOUR_READERS_ENUMERATED_VS_GUARD_ELIGIBLE_NET_ENDPOINT_WRITES_ONLY "+
            "notExactDependencies=True notParallelSafety=True intrusive=True");
        _pr57504Active=false;_pr57504Before=null;
    }
}
'''
path=root/"Pr57504ReadGraph.CiOnly.cs"
assert not path.exists()
path.write_text(probe)
print("PR57_504_DISPOSABLE_INJECTION_PASS")
