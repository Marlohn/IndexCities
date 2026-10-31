#!/usr/bin/env python3
"""PR57 #361: actual negative lane-read graph vs selected-winner graph.

Checkout-only, observational; depends on #359 then #360 injectors.
No skip, cache, candidate reordering, or substitution of canonical predicates.
"""
from pathlib import Path
base=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
def patch(path,old,new):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==1,(path.name,s.count(old),old)
    path.write_text(s.replace(old,new,1),encoding="utf-8")

patch(base/"Mobility.cs",
      "Pr57StageBodyAcross(v,other) is {} docked",
      "Pr57361BodyAcross(v,other) is {} docked")
patch(base/"Mobility.cs",
      "if(other.Id==v.Id||!Pr57StageOccupies(other))",
      "if(other.Id==v.Id||!Pr57361Occupies(v,other))")
patch(base/"Mobility.ResidualExclusive.Pr57.cs",
      "Pr57DenseEndSlice();\n        Pr57360End();",
      "Pr57DenseEndSlice();\n        Pr57361End();\n        Pr57360End();")
out=base/"Mobility.NegativeReadGraph361.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// Actual invocation graph for two canonical lane-scan predicates, not a
// causal-closure proof. A negative OccupiesLane read is a status dependency;
// BodyAcrossLane can read pose, route, dock and road/gateway data.
// Other physical readers, time, transient writes and external edits omitted.
public sealed partial class CitySimulation
{
    static readonly bool Pr57361Enabled =
        Environment.GetEnvironmentVariable("PR57_361_NEGATIVE_READ_GRAPH")=="1";
    readonly HashSet<(int A,int B)> _pr57361Edges=new();
    readonly HashSet<(int Reader,int Candidate)> _pr57361Directed=new();
    long _pr57361OccupiesCalls,_pr57361DockCalls;

    bool Pr57361Active()
    {
        if(!Pr57361Enabled||!_pr57360Active)return false;
        var ordinal=_pr57ExclusiveEligibleSlices;
        return ordinal>=40001&&ordinal<=40006||ordinal>=50001&&ordinal<=50006;
    }

    void Pr57361Read(Vehicle reader,Vehicle candidate)
    {
        if(!Pr57361Active()||reader.Id==candidate.Id)return;
        _pr57361Directed.Add((reader.Id,candidate.Id));
        _pr57361Edges.Add((Math.Min(reader.Id,candidate.Id),
                           Math.Max(reader.Id,candidate.Id)));
    }

    double? Pr57361BodyAcross(Vehicle reader,Vehicle candidate)
    {
        if(Pr57361Active())
        {
            _pr57361DockCalls++;
            Pr57361Read(reader,candidate);
        }
        return Pr57StageBodyAcross(reader,candidate);
    }

    bool Pr57361Occupies(Vehicle reader,Vehicle candidate)
    {
        if(Pr57361Active())
        {
            _pr57361OccupiesCalls++;
            Pr57361Read(reader,candidate);
        }
        return Pr57StageOccupies(candidate);
    }

    (int Largest,int Components,int Isolated) Pr57361Components(
        IEnumerable<(int A,int B)> edges)
    {
        var parent=State.Vehicles.ToDictionary(v=>v.Id,v=>v.Id);
        int Find(int id)
        {
            var p=parent[id];
            return p==id?id:parent[id]=Find(p);
        }
        foreach(var (a,b) in edges)
        {
            if(!parent.ContainsKey(a)||!parent.ContainsKey(b))continue;
            var pa=Find(a);var pb=Find(b);
            if(pa!=pb)parent[pa]=pb;
        }
        var sizes=parent.Keys.GroupBy(Find).Select(g=>g.Count()).ToArray();
        return (sizes.Length==0?0:sizes.Max(),sizes.Length,
                sizes.Count(x=>x==1));
    }

    void Pr57361End()
    {
        if(!Pr57361Active())return;
        var ordinal=_pr57ExclusiveEligibleSlices;
        var selected=Pr57361Components(_pr57360Edges);
        var read=Pr57361Components(_pr57361Edges);
        var readerGroups=_pr57361Directed.GroupBy(x=>x.Reader).ToArray();
        var candidateGroups=_pr57361Directed.GroupBy(x=>x.Candidate).ToArray();
        var fanout=readerGroups.Length==0?0:readerGroups.Max(g=>g.Count());
        var fanin=candidateGroups.Length==0?0:candidateGroups.Max(g=>g.Count());
        var overlap=_pr57361Edges.Count(e=>_pr57360Edges.Contains(e));
        Console.WriteLine(
            $"PR57_361_SLICE ordinal={ordinal} vehicles={State.Vehicles.Count} "+
            $"occupiesCalls={_pr57361OccupiesCalls} dockCalls={_pr57361DockCalls} "+
            $"directedDistinct={_pr57361Directed.Count} "+
            $"readerCount={readerGroups.Length} candidateCount={candidateGroups.Length} "+
            $"maxReaderFanout={fanout} maxCandidateFanIn={fanin} "+
            $"readEdges={_pr57361Edges.Count} readLargest={read.Largest} "+
            $"readComponents={read.Components} readIsolated={read.Isolated} "+
            $"selectedEdges={_pr57360Edges.Count} "+
            $"selectedLargest={selected.Largest} "+
            $"selectedComponents={selected.Components} "+
            $"selectedIsolated={selected.Isolated} "+
            $"overlapSelected={overlap} "+
            "onlyTwoActualLanePredicates=True negativeStatusRead=True "+
            "notCompleteCausalGraph=True noSkippedSlices=True observational=True");
        _pr57361Edges.Clear();_pr57361Directed.Clear();
        _pr57361OccupiesCalls=0;_pr57361DockCalls=0;
    }
}
''',encoding="utf-8")
print("PR57_361_INJECTED actual negative lane read graph (12 bounded physical slices)")
