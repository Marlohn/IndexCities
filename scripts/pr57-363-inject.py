#!/usr/bin/env python3
"""PR57 #363: observe actual vehicle status/occupancy and pose setter writes.

Checkout-only. Run after #359/#360/#361/#362 injectors.
No engine decisions, short-circuits, slices or saves are replaced.
A setter hook is not a complete causal journal (lists, external phases, etc.).
"""
from pathlib import Path

base=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"

def patch(path,old,new):
    text=path.read_text(encoding="utf-8")
    assert text.count(old)==1,(str(path),old,text.count(old))
    path.write_text(text.replace(old,new,1),encoding="utf-8")

model=base/"Model.cs"
source=model.read_text(encoding="utf-8")
head,tail=source.split("public sealed class Vehicle\n{",1)
assert "Pr57363Write" not in tail
properties=[
    ("Status","VehicleStatus",0,"old!=value",
     "Pr57363Occupancy(old,PassengerCar)!=Pr57363Occupancy(value,PassengerCar)"),
    ("PassengerCar","bool",1,"old!=value",
     "Pr57363Occupancy(Status,old)!=Pr57363Occupancy(Status,value)"),
    ("Position","Point",2,
     "BitConverter.DoubleToInt64Bits(old.X)!=BitConverter.DoubleToInt64Bits(value.X)||BitConverter.DoubleToInt64Bits(old.Z)!=BitConverter.DoubleToInt64Bits(value.Z)",
     "false"),
    ("Heading","double",3,
     "BitConverter.DoubleToInt64Bits(old)!=BitConverter.DoubleToInt64Bits(value)",
     "false"),
]
for name,typ,kind,changed,flipped in properties:
    old=f"public {typ} {name} {{get;set;}}"
    field="_pr57363"+name
    new=(f"private {typ} {field};\n    "
         f"public {typ} {name} {{get=>{field};set{{var old={field};{field}=value;"
         f"if(Pr57363Write is {{}} observe)observe(this,{kind},{changed},{flipped});}}}}")
    assert tail.count(old)==1,(name,tail.count(old))
    tail=tail.replace(old,new,1)
tail=("    public static Action<Vehicle,int,bool,bool>? Pr57363Write;\n"
      "    internal static bool Pr57363Occupancy(VehicleStatus status,bool passenger)=>\n"
      "        status is VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded||\n"
      "        status==VehicleStatus.Towed&&!passenger;\n"+tail)
model.write_text(head+"public sealed class Vehicle\n{\n"+tail,encoding="utf-8")

p=base/"Mobility.ResidualExclusive.Pr57.cs"
patch(p,"Pr57360Begin();","Pr57360Begin();\n        Pr57363Begin();")
patch(p,"Pr57362End();\n        Pr57361End();",
      "Pr57363End();\n        Pr57362End();\n        Pr57361End();")

out=base/"Mobility.OccupancyWriters363.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// #363: write-side *observational* census in two 600-slice REAL windows.
// The occupancy predicate is identical to Towing.cs/OccupiesLane and is
// asserted against that canonical predicate on the first slice of each window.
// This is NOT a complete writer journal or a safe scheduling certificate.
public sealed partial class CitySimulation
{
    static readonly bool Pr57363Enabled =
        Environment.GetEnvironmentVariable("PR57_363_OCCUPANCY_WRITES")=="1";
    readonly HashSet<Vehicle> _pr57363Canonical =
        new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
    // [window, field: status/passenger/position/heading, kind: canonical
    // assignments/canonical effective changes/temporary effective changes]
    readonly long[,,] _pr57363Stats=new long[2,4,3];
    readonly long[] _pr57363Flips=new long[2];
    readonly HashSet<int> _pr57363FlippedIds=new();
    long _pr57363SliceFlips,_pr57363SliceChanges,_pr57363SliceAssignments;
    long _pr57363SliceTempChanges;
    int _pr57363Window;
    bool _pr57363Active;

    void Pr57363Begin()
    {
        if(!Pr57363Enabled||!_pr57360Active)return;
        var ordinal=_pr57ExclusiveEligibleSlices;
        if(!((ordinal>=40001&&ordinal<=40600)||
             (ordinal>=50001&&ordinal<=50600)))return;
        if(_pr57363Active||Vehicle.Pr57363Write is not null)
            throw new InvalidOperationException("PR57 #363 overlapping writer probe");
        _pr57363Window=ordinal<=40600?0:1;
        _pr57363Canonical.Clear();
        foreach(var v in State.Vehicles)_pr57363Canonical.Add(v);
        if(ordinal is 40001 or 50001)
            foreach(var v in State.Vehicles)
                if(OccupiesLane(v)!=Vehicle.Pr57363Occupancy(v.Status,v.PassengerCar))
                    throw new InvalidOperationException("PR57 #363 occupancy predicate mismatch");
        _pr57363FlippedIds.Clear();
        _pr57363SliceFlips=_pr57363SliceChanges=0;
        _pr57363SliceAssignments=_pr57363SliceTempChanges=0;
        _pr57363Active=true;
        Vehicle.Pr57363Write=Pr57363Observe;
    }

    void Pr57363Observe(Vehicle v,int field,bool changed,bool flipped)
    {
        var w=_pr57363Window;
        if(_pr57363Canonical.Contains(v))
        {
            _pr57363Stats[w,field,0]++;
            _pr57363SliceAssignments++;
            if(changed)
            {
                _pr57363Stats[w,field,1]++;
                _pr57363SliceChanges++;
            }
            if(flipped)
            {
                if(!changed||field>1)
                    throw new InvalidOperationException("PR57 #363 invalid occupancy flip");
                _pr57363Flips[w]++;
                _pr57363SliceFlips++;
                _pr57363FlippedIds.Add(v.Id);
            }
        }
        else if(changed)
        {
            _pr57363Stats[w,field,2]++;
            _pr57363SliceTempChanges++;
        }
    }

    void Pr57363End()
    {
        if(!_pr57363Active)return;
        var ordinal=_pr57ExclusiveEligibleSlices;
        Vehicle.Pr57363Write=null;
        _pr57363Active=false;
        if(ordinal is >=40001 and <=40006 or >=50001 and <=50006)
        {
            // #361 contains actual read pairs only in these 12 slices.
            // Exposure is not a measured invalidation or causal closure.
            var exposed=_pr57361Directed.Count(p=>
                _pr57363FlippedIds.Contains(p.Candidate));
            Console.WriteLine(
                $"PR57_363_SLICE ordinal={ordinal} canonicalAssignments={_pr57363SliceAssignments} "+
                $"canonicalEffective={_pr57363SliceChanges} tempEffective={_pr57363SliceTempChanges} "+
                $"occupancyFlips={_pr57363SliceFlips} uniqueFlipped={_pr57363FlippedIds.Count} "+
                $"readEdgesExposed={exposed} negativeReadsNotSafeToSkip=True "+
                "noSkippedSlices=True observational=True");
        }
        if(ordinal!=40600&&ordinal!=50600)return;
        var w=_pr57363Window;
        string[] names=["status","passenger","position","heading"];
        var fields=new List<string>();
        long assignments=0,effective=0,temp=0;
        for(var k=0;k<4;k++)
        {
            var a=_pr57363Stats[w,k,0];
            var c=_pr57363Stats[w,k,1];
            var t=_pr57363Stats[w,k,2];
            assignments+=a;effective+=c;temp+=t;
            fields.Add($"{names[k]}Assign={a} {names[k]}Changed={c} {names[k]}TempChanged={t}");
        }
        Console.WriteLine(
            $"PR57_363_WINDOW start={(w==0?40001:50001)} end={ordinal} slices=600 "+
            $"canonicalAssignments={assignments} canonicalEffective={effective} "+
            $"canonicalNoops={assignments-effective} tempEffective={temp} "+
            $"occupancyFlips={_pr57363Flips[w]} "+string.Join(" ",fields)+" "+
            "scope=FOUR_SETTERS_DURING_PHYSICAL_SLICES_ONLY "+
            "externalWritersUnseen=True nestedMutationsUnseen=True "+
            "notCausalClosure=True noSkippedSlices=True observational=True");
    }
}
''',encoding="utf-8")
print("PR57_363_INJECTED occupancy/pose setter census; no decision reuse")
