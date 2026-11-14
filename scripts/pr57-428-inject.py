#!/usr/bin/env python3
"""#428: exact witnesses for the two changed HoldFor traces with stable readers.

Observational, disposable CI checkout only. Composes #426/#420. No cache,
decision replacement, skipped slice, altered ordering or product rule.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-426-inject.py", run_name="__main__")
p = Path("src/IndexCities.Simulation/Mobility.cs")
s = p.read_text()

def once(old, new):
    global s
    assert s.count(old) == 1, (old[:120], s.count(old))
    s = s.replace(old, new, 1)

once(
    "    readonly record struct Pr57426Frame(long Slice,Pr57426Candidate[] Candidates,Pr57426Hold[] Holds);",
    """    readonly record struct Pr57426Frame(long Slice,Pr57426Candidate[] Candidates,Pr57426Hold[] Holds);
    static readonly bool Pr57428Enabled=Environment.GetEnvironmentVariable("PR57_428_HOLD_WITNESS")=="1";
    readonly record struct Pr57428Pending(Pr57426Frame Before,Pr57426Frame After,long PriorDtBits,long DtBits);
    readonly Dictionary<int,Pr57428Pending> _pr57428Pending=new();
    readonly Dictionary<int,long> _pr57428LastDt=new();
    long _pr57428Witnesses;"""
)
once(
    "                        var holdsSame=nowH.SequenceEqual(old.Holds);",
    """                        var holdsSame=nowH.SequenceEqual(old.Holds);
                        if(Pr57428Enabled&&!holdsSame){
                            var priorDt=_pr57428LastDt.TryGetValue(v.Id,out var bits)?bits:long.MinValue;
                            _pr57428Pending[v.Id]=new(old,new(_pr57420Physical,nowC,nowH),
                                priorDt,Pr57420Bits(dt));
                        }"""
)
once(
    "                    _pr57426Last[v.Id]=new(_pr57420Physical,nowC,nowH);",
    """                    _pr57426Last[v.Id]=new(_pr57420Physical,nowC,nowH);
                    if(Pr57428Enabled)_pr57428LastDt[v.Id]=Pr57420Bits(dt);"""
)
once(
    "                            if(reader)_pr57420SameReader++;",
    """                            if(reader)_pr57420SameReader++;
                            if(Pr57428Enabled&&reader&&
                               _pr57428Pending.TryGetValue(v.Id,out var witness)&&
                               witness.After.Slice==_pr57420Physical){
                                _pr57428Witnesses++;
                                var before=witness.Before.Candidates;
                                var after=witness.After.Candidates;
                                var changed=Enumerable.Range(0,Math.Min(before.Length,after.Length))
                                    .Where(i=>!before[i].Equals(after[i]))
                                    .Select(i=>new{index=i,before=before[i],after=after[i]}).ToArray();
                                var hitIds=witness.Before.Holds.Select(h=>h.Id)
                                    .Concat(witness.After.Holds.Select(h=>h.Id)).Distinct().ToArray();
                                var relevant=hitIds.Select(id=>new{
                                    id,before=before.FirstOrDefault(c=>c.Id==id),
                                    after=after.FirstOrDefault(c=>c.Id==id)}).ToArray();
                                Console.WriteLine("PR57_428_WITNESS "+System.Text.Json.JsonSerializer.Serialize(new{
                                    vehicle=v.Id,slice=_pr57420Physical,
                                    priorSlice=witness.Before.Slice,
                                    priorDtBits=witness.PriorDtBits,dtBits=witness.DtBits,
                                    readerBefore=prior,readerAfter=now,
                                    beforeHolds=witness.Before.Holds,afterHolds=witness.After.Holds,
                                    changedCandidates=changed,relevantCandidates=relevant,
                                    candidateCountBefore=before.Length,candidateCountAfter=after.Length
                                }));
                            }"""
)
once(
    "            _pr57420Printed=true;",
    """            if(Pr57428Enabled)Console.WriteLine("PR57_428_SUMMARY witnesses="+_pr57428Witnesses+
                " physical="+_pr57420Physical+
                " observational=True causalClosure=False noDecisionReuse=True");
            _pr57420Printed=true;"""
)
p.write_text(s)
print("PR57_428_INJECTED observational=True noSkippedSlices=True noDecisionReuse=True")
