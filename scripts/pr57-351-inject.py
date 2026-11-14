#!/usr/bin/env python3
"""#351 checkout-only read-only lane candidate witness; NOT exact causal closure."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
mob=root/"src/IndexCities.Simulation/Mobility.cs"
res=root/"src/IndexCities.Simulation/Mobility.ResidualExclusive.Pr57.cs"
def patch(p,old,new):
 s=p.read_text()
 assert s.count(old)==1,(p,old,s.count(old))
 p.write_text(s.replace(old,new,1))
patch(mob,"Pr57S3CounterAfterScan(leaderDistance);",
 "Pr57S3CounterAfterScan(leaderDistance);\n                Pr57351Observe(v,pr57LaneList,blocker,leaderDistance,step,enteringLane);")
patch(res,"        Pr57PoseGuardReport();",
 "        Pr57PoseGuardReport();\n        Pr57351EndSlice();")
cs=r'''namespace IndexCities.Simulation;
// Selected fields only; no cached result enters the physical engine.
// Missing: transient writes, pedestrian/signal/clock/road/geometry, nested collections.
public sealed partial class CitySimulation {
 static readonly bool Pr57351Enabled=Environment.GetEnvironmentVariable("PR57_351_LANE_WITNESS")=="1";
 sealed record Witness(long Slice,string Self,int Winner,string WinnerState,Dictionary<int,string> Candidates);
 readonly Dictionary<int,Witness> _pr57351Last=new();
 readonly HashSet<int> _pr57351Seen=new();
 long _pr57351Slice=-1, _pr57351Eligible,_pr57351Compared,_pr57351SelfSame,
 _pr57351WinnerSame,_pr57351SetSame,_pr57351AllSame,_pr57351SelfAllSame,
 _pr57351Changes,_pr57351Visits,_pr57351Gap;
 static string Pr57351Key(Vehicle o)=>
  $"@{o.Id}|@{o.Position.X:R},@{o.Position.Z:R}|@{o.Heading:R}|@{o.Status}|"+
  $"@{o.RouteIndex}|@{string.Join(",",o.Route)}|@{string.Join(",",o.Detour)}|"+
  $"@{o.TowPhase}|@{o.TowTargetId}|@{o.TowTruckId}|@{o.WaitingSince:R}|"+
  $"@{o.IntersectionClaim}|@{string.Join(",",o.ManoeuvreSpaces)}|"+
  $"@{o.DockPosition}|@{o.ManoeuvreDock}|@{o.SitePathBeforeRoad}|"+
  $"@{o.SitePathIndex}|@{o.PassengerCar}|@{o.Machine}";
 void Pr57351Observe(Vehicle v,List<Vehicle> candidates,Vehicle? winner,double distance,double step,bool entering) {
  if(!Pr57351Enabled||!_pr57ExclusiveActive||entering||winner is null||
     winner.Status==VehicleStatus.Stranded||distance>8||step>.00001)return;
  var slice=_pr57ExclusiveEligibleSlices;
  if(_pr57351Slice!=slice){_pr57351Slice=slice;_pr57351Seen.Clear();}
  if(!_pr57351Seen.Add(v.Id))return;
  _pr57351Eligible++;_pr57351Visits+=candidates.Count;
  var current=new Dictionary<int,string>(candidates.Count);
  foreach(var o in candidates)current[o.Id]=Pr57351Key(o);
  var self=Pr57351Key(v);var win=Pr57351Key(winner);
  if(_pr57351Last.TryGetValue(v.Id,out var old)) {
   if(old.Slice+1==slice) {
    _pr57351Compared++;
    if(old.Self==self)_pr57351SelfSame++;
    if(old.Winner==winner.Id&&old.WinnerState==win)_pr57351WinnerSame++;
    if(old.Candidates.Count==current.Count&&current.Keys.All(old.Candidates.ContainsKey))_pr57351SetSame++;
    var changed=old.Candidates.Keys.Count(id=>!current.ContainsKey(id));
    foreach(var (id,state) in current)
     if(!old.Candidates.TryGetValue(id,out var before)||before!=state)changed++;
    _pr57351Changes+=changed;
    if(changed==0){_pr57351AllSame++;if(old.Self==self)_pr57351SelfAllSame++;}
   } else _pr57351Gap++;
  }
  _pr57351Last[v.Id]=new(slice,self,winner.Id,win,current);
 }
 void Pr57351EndSlice(){
  if(!Pr57351Enabled||!_pr57ExclusiveActive)return;
  Console.WriteLine(
   $"PR57_351_WITNESS eligibleSlice=@{_pr57ExclusiveEligibleSlices} "+
   $"eligible=@{_pr57351Eligible} compared=@{_pr57351Compared} "+
   $"selfSame=@{_pr57351SelfSame} winnerSame=@{_pr57351WinnerSame} "+
   $"candidateSetSame=@{_pr57351SetSame} allCandidatesSame=@{_pr57351AllSame} "+
   $"selfAllSame=@{_pr57351SelfAllSame} changedCandidates=@{_pr57351Changes} "+
   $"candidateVisits=@{_pr57351Visits} nonconsecutive=@{_pr57351Gap} "+
   "selectedFieldsOnly=True exactReadset=False transientWritesUnseen=True "+
   "clockPedestriansRoadUntracked=True noSkippedSlices=True intrusive=True");
 }
}'''.replace("@{",chr(36)+"{")
out=root/"src/IndexCities.Simulation/Mobility.LaneWitness351.Pr57.cs"
assert not out.exists()
out.write_text(cs)
print("PR57_351_INJECTED observational lane witness")
