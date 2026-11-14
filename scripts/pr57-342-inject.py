#!/usr/bin/env python3
"""PR57 #342: checkout-only journal of canonical Cause/WaitingSince/IntersectionClaim writes.

Captures transient Cause round-trips within actual MayEnterJunction calls.
No decision, ordering, persistence, or global slice is changed.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
sim=root/"src/IndexCities.Simulation"

def once(path,old,new):
    s=path.read_text(encoding="utf-8")
    if s.count(old)!=1:
        raise RuntimeError(f"{path}: expected one anchor, found {s.count(old)}: {old[:70]}")
    path.write_text(s.replace(old,new,1),encoding="utf-8")

model=sim/"Model.cs"
s=model.read_text(encoding="utf-8")
start=s.index("public sealed class Vehicle\n{")
end=s.index("public sealed class Shipment\n{",start)
v=s[start:end]
v=v.replace("public sealed class Vehicle\n{",
    "public sealed class Vehicle\n{\n    // #342 checkout-only; no game reader uses this observer.\n"
    "    public static Action<Vehicle,int,bool>? Pr57342OnWrite;\n",1)
properties=[
    ('public string Cause {get;set;}="No exterior";',
     'private string _pr57342Cause="No exterior";\n'
     '    public string Cause {get=>_pr57342Cause;set{'
     'var changed=!string.Equals(_pr57342Cause,value,StringComparison.Ordinal);'
     '_pr57342Cause=value;Pr57342OnWrite?.Invoke(this,0,changed);}}'),
    ('public double WaitingSince {get;set;}',
     'private double _pr57342WaitingSince;\n'
     '    public double WaitingSince {get=>_pr57342WaitingSince;set{'
     'var changed=BitConverter.DoubleToInt64Bits(_pr57342WaitingSince)!=BitConverter.DoubleToInt64Bits(value);'
     '_pr57342WaitingSince=value;Pr57342OnWrite?.Invoke(this,1,changed);}}'),
    ('public int? IntersectionClaim {get;set;}',
     'private int? _pr57342IntersectionClaim;\n'
     '    public int? IntersectionClaim {get=>_pr57342IntersectionClaim;set{'
     'var changed=_pr57342IntersectionClaim!=value;'
     '_pr57342IntersectionClaim=value;Pr57342OnWrite?.Invoke(this,2,changed);}}'),
]
for old,new in properties:
    if v.count(old)!=1:
        raise RuntimeError(f"Vehicle property anchor not unique: {old}")
    v=v.replace(old,new,1)
model.write_text(s[:start]+v+s[end:],encoding="utf-8")

once(sim/"Mobility.cs",
    '        v.Cause="Aguardando preferência/semáforo/travessia";var area=',
    '        Pr57342Begin(v);\n'
    '        v.Cause="Aguardando preferência/semáforo/travessia";var area=')
once(sim/"Mobility.JunctionReturns.Pr57.cs",
    '        if (reason == Pr57JunctionExit.Pedestrian) Pr57304Walk(reader,junction);',
    '        Pr57342End(reader,reason,allowed);\n'
    '        if (reason == Pr57JunctionExit.Pedestrian) Pr57304Walk(reader,junction);')
once(sim/"Mobility.JunctionReturns.Pr57.cs",
    '        Pr57Junction247Report(slices);',
    '        Pr57Junction247Report(slices);\n        Pr57342Report(slices);')

target=sim/"Mobility.CauseRoundtrip342.Pr57.cs"
if target.exists():
    raise RuntimeError("Refusing to overwrite #342 observer")
target.write_text(r'''namespace IndexCities.Simulation;

// #342: immediate setter observations, not a causal dependency certificate.
// In particular, this does not cover collection mutations, pedestrians, roads,
// clock, transient proxy objects, or all physical state.
public sealed partial class CitySimulation
{
    static readonly bool Pr57342Enabled =
        Environment.GetEnvironmentVariable("PR57_342_CAUSE_ROUNDTRIP")=="1";

    sealed class Pr57342Reader
    {
        public bool Inside;
        public string BeforeCause="";
        public int CurrentChanged, CurrentEqual, CurrentWait, CurrentClaim;
        public long Calls, SameFinal, ChangedFinal, TransientRoundtrips,
            TwoOrMoreChanged, CauseChanged, CauseEqual, WaitWrites, ClaimWrites;
    }
    readonly Dictionary<int,Pr57342Reader> _pr57342Readers=new();
    readonly long[] _pr57342CanonicalChanged=new long[3];
    readonly long[] _pr57342CanonicalEqual=new long[3];
    readonly long[] _pr57342EphemeralChanged=new long[3];
    readonly long[] _pr57342EphemeralEqual=new long[3];
    readonly long[,] _pr57342Reason=new long[18,4]; // calls, roundtrips, changed Cause writes, net changes
    HashSet<Vehicle>? _pr57342Canonical;
    bool _pr57342Started;

    void Pr57342Write(Vehicle v,int field,bool changed)
    {
        if(_pr57342Canonical is null||!_pr57342Canonical.Contains(v))
        {
            (changed?_pr57342EphemeralChanged:_pr57342EphemeralEqual)[field]++;
            return;
        }
        (changed?_pr57342CanonicalChanged:_pr57342CanonicalEqual)[field]++;
        if(!_pr57342Readers.TryGetValue(v.Id,out var s)||!s.Inside)return;
        if(field==0){if(changed)s.CurrentChanged++;else s.CurrentEqual++;}
        if(field==1)s.CurrentWait++;
        if(field==2)s.CurrentClaim++;
    }
    void Pr57342Begin(Vehicle v)
    {
        if(!Pr57342Enabled||State.Now<new DateTime(2026,1,7,17,0,0)||
           v.Id is not (1071 or 1243 or 1282 or 1380))return;
        if(!_pr57342Started)
        {
            _pr57342Canonical=new HashSet<Vehicle>(State.Vehicles,ReferenceEqualityComparer.Instance);
            Vehicle.Pr57342OnWrite=Pr57342Write;
            _pr57342Started=true;
        }
        if(!_pr57342Canonical!.Contains(v))return;
        if(!_pr57342Readers.TryGetValue(v.Id,out var s))
            _pr57342Readers[v.Id]=s=new Pr57342Reader();
        if(s.Inside)throw new InvalidOperationException("#342 nested MayEnterJunction for same reader");
        s.Inside=true;
        s.BeforeCause=v.Cause;
        s.CurrentChanged=s.CurrentEqual=s.CurrentWait=s.CurrentClaim=0;
    }
    void Pr57342End(Vehicle v,Pr57JunctionExit reason,bool allowed)
    {
        if(!_pr57342Readers.TryGetValue(v.Id,out var s)||!s.Inside)return;
        s.Inside=false;
        s.Calls++;
        bool same=string.Equals(s.BeforeCause,v.Cause,StringComparison.Ordinal);
        if(same)s.SameFinal++;else s.ChangedFinal++;
        if(same&&s.CurrentChanged>0)s.TransientRoundtrips++;
        if(s.CurrentChanged>=2)s.TwoOrMoreChanged++;
        s.CauseChanged+=s.CurrentChanged;
        s.CauseEqual+=s.CurrentEqual;
        s.WaitWrites+=s.CurrentWait;
        s.ClaimWrites+=s.CurrentClaim;
        var k=(int)reason;
        _pr57342Reason[k,0]++;
        if(same&&s.CurrentChanged>0)_pr57342Reason[k,1]++;
        _pr57342Reason[k,2]+=s.CurrentChanged;
        if(!same)_pr57342Reason[k,3]++;
    }
    void Pr57342Report(long slices)
    {
        if(!Pr57342Enabled)return;
        Console.WriteLine(
            $"PR57_342_GLOBAL slices={slices} started={_pr57342Started} "+
            $"canonicalChanged={string.Join(",",_pr57342CanonicalChanged)} "+
            $"canonicalEqual={string.Join(",",_pr57342CanonicalEqual)} "+
            $"ephemeralChanged={string.Join(",",_pr57342EphemeralChanged)} "+
            $"ephemeralEqual={string.Join(",",_pr57342EphemeralEqual)} "+
            "scope=IMMEDIATE_THREE_SCALAR_SETTERS_NOT_CAUSAL_CLOSURE");
        foreach(var (id,s) in _pr57342Readers.OrderBy(x=>x.Key))
            Console.WriteLine(
                $"PR57_342_READER slices={slices} id={id} calls={s.Calls} "+
                $"sameFinal={s.SameFinal} changedFinal={s.ChangedFinal} "+
                $"transientRoundtrips={s.TransientRoundtrips} twoOrMoreChanged={s.TwoOrMoreChanged} "+
                $"causeChanged={s.CauseChanged} causeEqual={s.CauseEqual} "+
                $"waitWrites={s.WaitWrites} claimWrites={s.ClaimWrites} "+
                "scope=ACTUAL_MAY_ENTER_JUNCTION_CALL_NO_DECISION_REUSE");
        foreach(var reason in new[]{Pr57JunctionExit.OnwardBody,Pr57JunctionExit.BendBody,Pr57JunctionExit.Pedestrian})
        {
            var k=(int)reason;
            Console.WriteLine(
                $"PR57_342_REASON slices={slices} reason={reason} calls={_pr57342Reason[k,0]} "+
                $"transientRoundtrips={_pr57342Reason[k,1]} "+
                $"causeChanged={_pr57342Reason[k,2]} netCauseChanged={_pr57342Reason[k,3]} "+
                "scope=FOUR_SELECTED_READERS_ONLY");
        }
    }
}
''',encoding="utf-8")
print("PR57_342_INJECTED: three canonical scalar setters and actual junction cause roundtrips")
