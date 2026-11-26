#!/usr/bin/env python3
"""PR57 #362: split actual lane reads into cheap rejected vs occupancy-eligible.

Checkout-only observational add-on, applied AFTER #359/#360/#361 injectors.
No changes to canonical short-circuits, actor order, effects, or sub-slices.
"""
from pathlib import Path

base = Path(__file__).resolve().parents[1] / "src/IndexCities.Simulation"

def patch(path, old, new):
    text = path.read_text(encoding="utf-8")
    assert text.count(old) == 1, (path.name, text.count(old), old)
    path.write_text(text.replace(old, new, 1), encoding="utf-8")

p = base / "Mobility.NegativeReadGraph361.Pr57.cs"
patch(p, "        return Pr57StageBodyAcross(reader,candidate);",
"""        var answer=Pr57StageBodyAcross(reader,candidate);
        if(Pr57362Active())Pr57362BodyResult(reader,candidate,answer);
        return answer;""")
patch(p, "        return Pr57StageOccupies(candidate);",
"""        var answer=Pr57StageOccupies(candidate);
        if(Pr57362Active())Pr57362OccupiesResult(reader,candidate,answer);
        return answer;""")
patch(base / "Mobility.ResidualExclusive.Pr57.cs",
      "Pr57361End();\n        Pr57360End();",
      "Pr57362End();\n        Pr57361End();\n        Pr57360End();")

out = base / "Mobility.FilteredReadGraph362.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// A returned false is still a read dependency: this only distinguishes
// expensive downstream geometry from cheap negative occupancy predicates.
// The true-only graph is NOT a causal certificate or a safe skip workset.
public sealed partial class CitySimulation
{
    static readonly bool Pr57362Enabled =
        Environment.GetEnvironmentVariable("PR57_362_FILTERED_READ_GRAPH")=="1";
    readonly HashSet<(int A,int B)> _pr57362TrueEdges=new();
    readonly HashSet<(int Reader,int Candidate)> _pr57362TrueDirected=new();
    readonly HashSet<int> _pr57362TrueCandidates=new();
    long _pr57362OccupiesTrue,_pr57362OccupiesFalse;
    long _pr57362BodyPositive,_pr57362BodyNull;
    long _pr57362Passenger,_pr57362Machine,_pr57362Far,_pr57362OtherNull;

    bool Pr57362Active()=>Pr57362Enabled&&Pr57361Active();

    void Pr57362OccupiesResult(Vehicle reader,Vehicle candidate,bool result)
    {
        if(result)
        {
            _pr57362OccupiesTrue++;
            _pr57362TrueCandidates.Add(candidate.Id);
            _pr57362TrueDirected.Add((reader.Id,candidate.Id));
            _pr57362TrueEdges.Add((Math.Min(reader.Id,candidate.Id),
                                  Math.Max(reader.Id,candidate.Id)));
        }
        else _pr57362OccupiesFalse++;
    }

    void Pr57362BodyResult(Vehicle reader,Vehicle candidate,double? result)
    {
        if(result is not null){_pr57362BodyPositive++;return;}
        _pr57362BodyNull++;
        // These are only the first three canonical cheap guards, not an
        // exhaustive attribution of all null results or gateway effects.
        if(candidate.PassengerCar)_pr57362Passenger++;
        else if(candidate.Machine!=PavingMachine.None)_pr57362Machine++;
        else if(candidate.Position.Distance(reader.Position)>20)_pr57362Far++;
        else _pr57362OtherNull++;
    }

    void Pr57362End()
    {
        if(!Pr57362Active())return;
        var components=Pr57361Components(_pr57362TrueEdges);
        var ordinal=_pr57ExclusiveEligibleSlices;
        Console.WriteLine(
            $"PR57_362_SLICE ordinal={ordinal} vehicles={State.Vehicles.Count} "+
            $"occupiesTrue={_pr57362OccupiesTrue} "+
            $"occupiesFalse={_pr57362OccupiesFalse} "+
            $"trueDirected={_pr57362TrueDirected.Count} "+
            $"trueCandidates={_pr57362TrueCandidates.Count} "+
            $"trueEdges={_pr57362TrueEdges.Count} "+
            $"trueLargest={components.Largest} "+
            $"trueComponents={components.Components} "+
            $"trueIsolated={components.Isolated} "+
            $"bodyPositive={_pr57362BodyPositive} bodyNull={_pr57362BodyNull} "+
            $"bodyPassenger={_pr57362Passenger} "+
            $"bodyMachine={_pr57362Machine} bodyFar={_pr57362Far} "+
            $"bodyOtherNull={_pr57362OtherNull} "+
            "negativeStillReads=True onlyTwoLanePredicates=True "+
            "notCausalClosure=True noSkippedSlices=True observational=True");
        _pr57362TrueEdges.Clear();_pr57362TrueDirected.Clear();
        _pr57362TrueCandidates.Clear();
        _pr57362OccupiesTrue=0;_pr57362OccupiesFalse=0;
        _pr57362BodyPositive=0;_pr57362BodyNull=0;
        _pr57362Passenger=0;_pr57362Machine=0;
        _pr57362Far=0;_pr57362OtherNull=0;
    }
}
''', encoding="utf-8")
print("PR57_362_INJECTED filtered lane-read graph, 12 bounded physical slices")
