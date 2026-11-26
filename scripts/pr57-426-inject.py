#!/usr/bin/env python3
"""PR57 #426: exact ordered candidate-ID, scalar-input and HoldFor trace census.

Composes #420 only in a disposable CI checkout. Observes the first canonical
lane scan per vehicle/physical slice. The scalar projection is intentionally
incomplete: matching inputs/HoldFor hits cannot prove negative-predicate
closure, admission parity or safe reuse. No decision or slice is skipped.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-420-inject.py", run_name="__main__")
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()
def once(old,new):
    global s
    assert s.count(old)==1,(old[:110],s.count(old))
    s=s.replace(old,new,1)

once(
    "    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,\n",
    """    static readonly bool Pr57426Enabled=Environment.GetEnvironmentVariable("PR57_426_SEMANTIC_SHADOW")=="1";
    readonly record struct Pr57426Candidate(int Id,int Status,long X,long Z,long Heading,
        long Waiting,int TowPhase,int DetourCount,int RouteIndex,int RouteCount,
        bool PassengerCar,int Machine,long DockX,long DockZ);
    readonly record struct Pr57426Hold(int Id,long Distance);
    readonly record struct Pr57426Frame(long Slice,Pr57426Candidate[] Candidates,Pr57426Hold[] Holds);
    readonly record struct Pr57426Compare(long Slice,bool IdsSame,bool ScalarsSame,bool HoldsSame,int Changed);
    readonly Dictionary<int,Pr57426Frame> _pr57426Last=new();
    readonly Dictionary<int,Pr57426Compare> _pr57426Compare=new();
    long _pr57426FirstScans,_pr57426Reads,_pr57426FullLists,_pr57426Pairs,
         _pr57426SameIds,_pr57426SameScalars,_pr57426SameHolds,
         _pr57426StableReader,_pr57426StableReaderStepChanged,
         _pr57426ReaderIds,_pr57426ReaderScalars,_pr57426ReaderHolds,
         _pr57426ChangedIds,_pr57426ChangedScalars,_pr57426ChangedHolds,
         _pr57426ChangedScalarEntries;
    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,
"""
)
once(
    "                var leaderDistance=double.MaxValue;Vehicle? blocker=null;",
    """                var pr57426First=Pr57426Enabled&&pr57420Sample&&
                    (!_pr57420Seen.TryGetValue(v.Id,out var pr57426Seen)||pr57426Seen!=_pr57420Physical);
                List<Pr57426Candidate>? pr57426Candidates=pr57426First?new(98):null;
                List<Pr57426Hold>? pr57426Holds=pr57426First?new(8):null;
                var leaderDistance=double.MaxValue;Vehicle? blocker=null;"""
)
once(
    "void HoldFor(Vehicle other,double distance){Pr57PhaseImpactHold(other,distance);",
    "void HoldFor(Vehicle other,double distance){if(pr57426First)pr57426Holds!.Add(new(other.Id,Pr57420Bits(distance)));Pr57PhaseImpactHold(other,distance);"
)
once(
    "                    var other=enteringLane?Pr57StageEntryFrame(liveOther):liveOther;",
    """                    var other=enteringLane?Pr57StageEntryFrame(liveOther):liveOther;
                    if(pr57426First){
                        var pr57426Dock=other.DockPosition;
                        pr57426Candidates!.Add(new(other.Id,(int)other.Status,
                            Pr57420Bits(other.Position.X),Pr57420Bits(other.Position.Z),
                            Pr57420Bits(other.Heading),Pr57420Bits(other.WaitingSince),
                            (int)other.TowPhase,other.Detour.Count,other.RouteIndex,
                            other.Route.Count,other.PassengerCar,(int)other.Machine,
                            pr57426Dock.HasValue?Pr57420Bits(pr57426Dock.Value.X):long.MinValue,
                            pr57426Dock.HasValue?Pr57420Bits(pr57426Dock.Value.Z):long.MinValue));
                    }"""
)
once(
    "                Pr57ScanLane(pr57LaneList);\n                Pr57CandidateEndQuery();",
    """                Pr57ScanLane(pr57LaneList);
                if(pr57426First){
                    var nowC=pr57426Candidates!.ToArray();
                    var nowH=pr57426Holds!.ToArray();
                    _pr57426FirstScans++;
                    _pr57426Reads+=nowC.Length;
                    if(pr57LaneList.Count==State.Vehicles.Count)_pr57426FullLists++;
                    if(_pr57426Last.TryGetValue(v.Id,out var old)&&old.Slice==_pr57420Physical-1){
                        _pr57426Pairs++;
                        var idsSame=nowC.Length==old.Candidates.Length;
                        var scalarsSame=idsSame;
                        var changed=idsSame?0:Math.Max(nowC.Length,old.Candidates.Length);
                        if(idsSame)for(var i=0;i<nowC.Length;i++){
                            if(nowC[i].Id!=old.Candidates[i].Id)idsSame=false;
                            if(!nowC[i].Equals(old.Candidates[i])){scalarsSame=false;changed++;}
                        }
                        var holdsSame=nowH.SequenceEqual(old.Holds);
                        if(idsSame)_pr57426SameIds++;
                        if(scalarsSame)_pr57426SameScalars++;
                        if(holdsSame)_pr57426SameHolds++;
                        _pr57426Compare[v.Id]=new(_pr57420Physical,idsSame,scalarsSame,holdsSame,changed);
                    }
                    _pr57426Last[v.Id]=new(_pr57420Physical,nowC,nowH);
                }
                Pr57CandidateEndQuery();"""
)
once(
    "                            if(reader&&blockerSame&&distanceSame&&stepSame)_pr57420SameReaderDecision++;",
    """                            if(reader&&blockerSame&&distanceSame&&stepSame)_pr57420SameReaderDecision++;
                            if(Pr57426Enabled&&reader&&_pr57426Compare.TryGetValue(v.Id,out var comp)&&comp.Slice==_pr57420Physical){
                                _pr57426StableReader++;
                                if(comp.IdsSame)_pr57426ReaderIds++;
                                if(comp.ScalarsSame)_pr57426ReaderScalars++;
                                if(comp.HoldsSame)_pr57426ReaderHolds++;
                                if(!stepSame){
                                    _pr57426StableReaderStepChanged++;
                                    if(comp.IdsSame)_pr57426ChangedIds++;
                                    if(comp.ScalarsSame)_pr57426ChangedScalars++;
                                    if(comp.HoldsSame)_pr57426ChangedHolds++;
                                    _pr57426ChangedScalarEntries+=comp.Changed;
                                }
                            }"""
)
once(
    "            _pr57420Printed=true;",
    """            _pr57420Printed=true;
            if(Pr57426Enabled)
                Console.WriteLine("PR57_426_SUMMARY physical="+_pr57420Physical+
                    " first="+_pr57426FirstScans+" reads="+_pr57426Reads+
                    " fullLists="+_pr57426FullLists+" pairs="+_pr57426Pairs+
                    " sameIds="+_pr57426SameIds+" sameScalars="+_pr57426SameScalars+
                    " sameHolds="+_pr57426SameHolds+" stableReader="+_pr57426StableReader+
                    " readerIds="+_pr57426ReaderIds+" readerScalars="+_pr57426ReaderScalars+
                    " readerHolds="+_pr57426ReaderHolds+
                    " changedStep="+_pr57426StableReaderStepChanged+
                    " changedIds="+_pr57426ChangedIds+
                    " changedScalars="+_pr57426ChangedScalars+
                    " changedHolds="+_pr57426ChangedHolds+
                    " changedScalarEntries="+_pr57426ChangedScalarEntries+
                    " observational=True causalClosure=False noDecisionReuse=True");"""
)
p.write_text(s)
print("PR57_426_INJECTED observational=True noSkippedSlices=True noDecisionReuse=True")
