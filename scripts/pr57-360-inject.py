#!/usr/bin/env python3
"""PR57 #360: bounded selected-component shadow, checkout-only; never skips physics."""
from pathlib import Path
base=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
def change(path,old,new):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==1,(path.name,s.count(old),old)
    path.write_text(s.replace(old,new,1),encoding="utf-8")
p=base/"Mobility.ResidualExclusive.Pr57.cs"
change(p,"Pr57ExecBeginSlice();","Pr57ExecBeginSlice();\n        Pr57360Begin();")
change(p,"Pr57DenseBeginVehicle();","Pr57DenseBeginVehicle();\n        Pr57360BeginActor(vehicle);")
change(p,"Pr57DenseFinishVehicle(vehicle);","Pr57DenseFinishVehicle(vehicle);\n        Pr57360EndActor(vehicle);")
# The #359 checkout-only injector runs FIRST; this anchor is created by it.
change(p,"Pr57DenseEndSlice();\n        Pr57359End();",
       "Pr57DenseEndSlice();\n        Pr57360End();\n        Pr57359End();")
p=base/"Mobility.cs"
change(p,"if(blocker is not null)Pr57CausalWinner(v,blocker);",
       "if(blocker is not null)Pr57CausalWinner(v,blocker);\n                Pr57360Edge(v,blocker);")
change(p,"Pr57Stage249FinishBend(pr57BendList.Count,bendLeaders);",
       "Pr57Stage249FinishBend(pr57BendList.Count,bendLeaders);\n                Pr57360Bend(v,bendLeaders);")
p=base/"Mobility.SharedSpace.cs"
change(p,"if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);\n                    return o;",
       "if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);\n                    Pr57360Edge(v,o);\n                    return o;")
out=base/"Mobility.ComponentShadow360.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// Selected winners form only a LOWER BOUND on causal dependencies.
// Negative candidates, transient writes, pedestrian gates, signal changes,
// nested admission contents and all other external writers are NOT closed.
public sealed partial class CitySimulation
{
    static readonly bool Pr57360Enabled =
        Environment.GetEnvironmentVariable("PR57_360_COMPONENT_SHADOW")=="1";
    readonly record struct Pr57360Key(
        Pr57ExecProjection Core,string? Cause,string Route,string Site,
        string Detour,string Spaces,string Corridor,string Push);
    static Pr57360Key Pr57360Capture(Vehicle v)=>new(
        Pr57ExecSnapshot(v),v.Cause,string.Join("|",v.Route),
        string.Join("|",v.SitePath),string.Join("|",v.Detour),
        string.Join("|",v.ManoeuvreSpaces),string.Join("|",v.DockCorridor),
        string.Join("|",v.PushPath));
    readonly record struct Pr57360Citizen(Point Position,bool Moving,int? VehicleId);
    static Pr57360Citizen Pr57360CaptureCitizen(Citizen c)=>new(c.Position,c.Moving,c.VehicleId);
    // Stats: slices, actors, ownChanges, betweenVehicle, betweenCitizen,
    // worldChanges, edges, largestSum, largestMax, observerTicks,
    // deadlineFlips, roadRevisions, syncChanges, componentSum, isolatedSum.
    readonly long[,] _pr57360Stats=new long[2,15];
    readonly Dictionary<int,Pr57360Key> _pr57360Start=new();
    readonly Dictionary<int,Pr57360Key> _pr57360AfterActors=new();
    Dictionary<int,Pr57360Key> _pr57360Previous=new();
    Dictionary<int,Pr57360Citizen> _pr57360PreviousCitizens=new();
    Dictionary<int,bool> _pr57360PreviousDue=new();
    readonly HashSet<(int A,int B)> _pr57360Edges=new();
    bool _pr57360Active;
    int _pr57360Window,_pr57360ActorId,_pr57360PreviousRoad;
    long _pr57360PreviousOrdinal;
    Pr57360Key _pr57360ActorBefore;
    bool Pr57360Due(Vehicle v)=>v.WaitingSince>0 &&
        VehicleMovementTime-v.WaitingSince>=State.Tuning.UTurnWaitSeconds;

    void Pr57360Begin()
    {
        if(!Pr57360Enabled||!_pr57ExclusiveActive)return;
        var t=System.Diagnostics.Stopwatch.GetTimestamp();
        var ordinal=_pr57ExclusiveEligibleSlices;
        if(!((ordinal>=40001&&ordinal<=40600)||(ordinal>=50001&&ordinal<=50600)))return;
        _pr57360Window=ordinal<=40600?0:1;
        var w=_pr57360Window;
        _pr57360Active=true;
        _pr57360Stats[w,0]++;
        _pr57360Start.Clear();_pr57360AfterActors.Clear();_pr57360Edges.Clear();
        foreach(var v in State.Vehicles)
        {
            var key=Pr57360Capture(v);
            _pr57360Start[v.Id]=key;
            if(_pr57360PreviousOrdinal!=ordinal-1)continue;
            if(!_pr57360Previous.TryGetValue(v.Id,out var prior)||prior!=key)
                _pr57360Stats[w,3]++;
            if(_pr57360PreviousDue.TryGetValue(v.Id,out var due)&&due!=Pr57360Due(v))
                _pr57360Stats[w,10]++;
        }
        if(_pr57360PreviousOrdinal==ordinal-1)
        {
            foreach(var c in State.Citizens)
                if(!_pr57360PreviousCitizens.TryGetValue(c.Id,out var prior)||
                   prior!=Pr57360CaptureCitizen(c))_pr57360Stats[w,4]++;
            if(_pr57360PreviousRoad!=State.RoadRevision)_pr57360Stats[w,11]++;
        }
        _pr57360Stats[w,9]+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
    }
    void Pr57360BeginActor(Vehicle v)
    {
        if(!_pr57360Active)return;
        var t=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57360ActorId=v.Id;_pr57360ActorBefore=Pr57360Capture(v);
        _pr57360Stats[_pr57360Window,9]+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
    }
    void Pr57360EndActor(Vehicle v)
    {
        if(!_pr57360Active)return;
        var t=System.Diagnostics.Stopwatch.GetTimestamp();
        var w=_pr57360Window;
        if(_pr57360ActorId!=v.Id)throw new InvalidOperationException("PR57_360 actor mismatch");
        var after=Pr57360Capture(v);
        _pr57360Stats[w,1]++;
        if(_pr57360ActorBefore!=after)_pr57360Stats[w,2]++;
        _pr57360AfterActors[v.Id]=after;
        _pr57360Stats[w,9]+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
    }
    void Pr57360Edge(Vehicle reader,Vehicle? blocker)
    {
        if(!_pr57360Active||blocker is null||reader.Id==blocker.Id)return;
        var t=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57360Edges.Add((Math.Min(reader.Id,blocker.Id),Math.Max(reader.Id,blocker.Id)));
        _pr57360Stats[_pr57360Window,9]+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
    }
    void Pr57360Bend(Vehicle reader,IEnumerable<Vehicle> candidates)
    {
        if(!_pr57360Active)return;
        foreach(var candidate in candidates)Pr57360Edge(reader,candidate);
    }
    void Pr57360End()
    {
        if(!_pr57360Active)return;
        var t=System.Diagnostics.Stopwatch.GetTimestamp();
        var w=_pr57360Window;
        var end=State.Vehicles.ToDictionary(v=>v.Id,Pr57360Capture);
        foreach(var (id,key) in end)
        {
            if(!_pr57360Start.TryGetValue(id,out var before)||before!=key)_pr57360Stats[w,5]++;
            if(!_pr57360AfterActors.TryGetValue(id,out var after)||after!=key)_pr57360Stats[w,12]++;
        }
        var parent=State.Vehicles.ToDictionary(v=>v.Id,v=>v.Id);
        int Find(int id)
        {
            var p=parent[id];
            return p==id?id:parent[id]=Find(p);
        }
        foreach(var (a,b) in _pr57360Edges)
            if(parent.ContainsKey(a)&&parent.ContainsKey(b))
            {
                var pa=Find(a);var pb=Find(b);
                if(pa!=pb)parent[pa]=pb;
            }
        var sizes=State.Vehicles.GroupBy(v=>Find(v.Id)).Select(g=>g.Count()).ToArray();
        var largest=sizes.Length==0?0:sizes.Max();
        _pr57360Stats[w,6]+=_pr57360Edges.Count;
        _pr57360Stats[w,7]+=largest;
        _pr57360Stats[w,8]=Math.Max(_pr57360Stats[w,8],largest);
        _pr57360Stats[w,13]+=sizes.Length;
        _pr57360Stats[w,14]+=sizes.Count(x=>x==1);
        _pr57360Previous=end;
        _pr57360PreviousCitizens=State.Citizens.ToDictionary(c=>c.Id,Pr57360CaptureCitizen);
        _pr57360PreviousDue=State.Vehicles.ToDictionary(v=>v.Id,Pr57360Due);
        _pr57360PreviousRoad=State.RoadRevision;
        _pr57360PreviousOrdinal=_pr57ExclusiveEligibleSlices;
        _pr57360Active=false;
        _pr57360Stats[w,9]+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
        if(_pr57ExclusiveEligibleSlices!=40600&&_pr57ExclusiveEligibleSlices!=50600)return;
        var n=(double)_pr57360Stats[w,0];
        var hz=(double)System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_360_WINDOW start={(w==0?40001:50001)} slices={n:F0} " +
            $"actors={_pr57360Stats[w,1]} ownNetChanges={_pr57360Stats[w,2]} " +
            $"betweenSliceVehicleChanges={_pr57360Stats[w,3]} " +
            $"betweenSliceCitizenChanges={_pr57360Stats[w,4]} " +
            $"worldNetChanges={_pr57360Stats[w,5]} " +
            $"selectedUniqueEdges={_pr57360Stats[w,6]} " +
            $"largestSelectedComponentMean={_pr57360Stats[w,7]/n:F5} " +
            $"largestSelectedComponentMax={_pr57360Stats[w,8]} " +
            $"selectedComponentMean={_pr57360Stats[w,13]/n:F5} " +
            $"isolatedMean={_pr57360Stats[w,14]/n:F5} " +
            $"waitingDeadlineFlips={_pr57360Stats[w,10]} " +
            $"roadRevisionChanges={_pr57360Stats[w,11]} " +
            $"postActorSyncChanges={_pr57360Stats[w,12]} " +
            $"observerMs={_pr57360Stats[w,9]*1000.0/hz:F6} " +
            "graphLowerBound=True noCausalClosure=True noSkippedSlices=True " +
            "missesTransientAndNestedWriters=True observational=True");
    }
}
''',encoding="utf-8")
print("PR57_360_INJECTED observational selected-winner component shadow")
