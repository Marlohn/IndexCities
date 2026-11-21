#!/usr/bin/env python3
"""#379 shadow: separate partial gate edge churn by reader presence."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
p=root/"Mobility.BitsetReader377.Pr57.cs"
s=p.read_text()
for old,new in (
 ("        Pr57378Compare();","        Pr57378Compare();\n        Pr57379Compare();"),
 ("        Pr57378Finish();","        Pr57378Finish();\n        Pr57379Finish();"),
):
 assert s.count(old)==1,(old,s.count(old))
 s=s.replace(old,new)
p.write_text(s)
out=root/"Mobility.ReaderPresence379.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;
using System.Numerics;
namespace IndexCities.Simulation;
// Only the partial Occupies gate. Presence = >=1 candidate read in a slice.
// Does not establish eligibility, causal closure or safe reuse.
public sealed partial class CitySimulation
{
 static readonly bool Pr57379Enabled=Environment.GetEnvironmentVariable("PR57_379_PRESENCE_SHADOW")=="1";
 long _pr57379Pairs,_pr57379Changed,_pr57379PresenceChanged,_pr57379OnlyPresence,_pr57379WithoutPresence;
 long _pr57379Churn,_pr57379ToggledChurn,_pr57379PersistentChurn,_pr57379Entries,_pr57379Exits;
 long _pr57379PrevActive,_pr57379NowActive,_pr57379Zero,_pr57379OneToTen,_pr57379ElevenToThirty,_pr57379OverThirty;
 long _pr57379MaxActive,_pr57379MaxToggled,_pr57379MaxPersistent,_pr57379Ticks;
 void Pr57379Compare()
 {
  if(!Pr57379Enabled||!_pr57377Active)return;
  long slice=_pr57374PhysicalSlices;
  if(slice<3||slice>Pr57377SliceLimit+1)return;
  long started=Stopwatch.GetTimestamp();
  ulong oldLo=0,oldHi=0,newLo=0,newHi=0;
  foreach(var p in _pr57377Previous){oldLo|=p.Value.Lo;oldHi|=p.Value.Hi;}
  foreach(var p in _pr57377Current){newLo|=p.Value.Lo;newHi|=p.Value.Hi;}
  ulong toggleLo=oldLo^newLo,toggleHi=oldHi^newHi;
  int entries=BitOperations.PopCount(newLo&~oldLo)+BitOperations.PopCount(newHi&~oldHi);
  int exits=BitOperations.PopCount(oldLo&~newLo)+BitOperations.PopCount(oldHi&~newHi);
  int prev=BitOperations.PopCount(oldLo)+BitOperations.PopCount(oldHi);
  int now=BitOperations.PopCount(newLo)+BitOperations.PopCount(newHi);
  long toggled=0,persistent=0;
  foreach(var p in _pr57377Previous)
  {
   var other=_pr57377Current.GetValueOrDefault(p.Key);
   ulong lo=p.Value.Lo^other.Lo,hi=p.Value.Hi^other.Hi;
   toggled+=BitOperations.PopCount(lo&toggleLo)+BitOperations.PopCount(hi&toggleHi);
   persistent+=BitOperations.PopCount(lo&~toggleLo)+BitOperations.PopCount(hi&~toggleHi);
  }
  foreach(var p in _pr57377Current)
  {
   if(_pr57377Previous.ContainsKey(p.Key))continue;
   toggled+=BitOperations.PopCount(p.Value.Lo&toggleLo)+BitOperations.PopCount(p.Value.Hi&toggleHi);
   persistent+=BitOperations.PopCount(p.Value.Lo&~toggleLo)+BitOperations.PopCount(p.Value.Hi&~toggleHi);
  }
  long churn=toggled+persistent;
  _pr57379Pairs++;_pr57379Churn+=churn;_pr57379ToggledChurn+=toggled;_pr57379PersistentChurn+=persistent;
  _pr57379Entries+=entries;_pr57379Exits+=exits;_pr57379PrevActive+=prev;_pr57379NowActive+=now;
  _pr57379MaxActive=Math.Max(_pr57379MaxActive,now);
  _pr57379MaxToggled=Math.Max(_pr57379MaxToggled,toggled);
  _pr57379MaxPersistent=Math.Max(_pr57379MaxPersistent,persistent);
  if(now==0)_pr57379Zero++;
  else if(now<=10)_pr57379OneToTen++;
  else if(now<=30)_pr57379ElevenToThirty++;
  else _pr57379OverThirty++;
  if(entries+exits>0)_pr57379PresenceChanged++;
  if(churn>0)
  {
   _pr57379Changed++;
   if(persistent==0)_pr57379OnlyPresence++;
   if(entries+exits==0)_pr57379WithoutPresence++;
  }
  _pr57379Ticks+=Stopwatch.GetTimestamp()-started;
 }
 void Pr57379Finish()
 {
  if(!Pr57379Enabled)return;
  if(_pr57379Pairs!=599||_pr57379Churn!=_pr57378Added+_pr57378Removed||
     _pr57379Changed!=_pr57378Changed)
   throw new InvalidOperationException("#379/#378 edge XOR mismatch");
  Console.WriteLine($"PR57_379_SUMMARY transitions={_pr57379Pairs} changed={_pr57379Changed} "+
   $"presenceChanged={_pr57379PresenceChanged} onlyPresence={_pr57379OnlyPresence} "+
   $"withoutPresence={_pr57379WithoutPresence} edgeChurn={_pr57379Churn} "+
   $"toggledChurn={_pr57379ToggledChurn} persistentChurn={_pr57379PersistentChurn} "+
   $"entries={_pr57379Entries} exits={_pr57379Exits} "+
   $"prevActiveSum={_pr57379PrevActive} nowActiveSum={_pr57379NowActive} "+
   $"zeroActive={_pr57379Zero} oneToTen={_pr57379OneToTen} "+
   $"elevenToThirty={_pr57379ElevenToThirty} overThirty={_pr57379OverThirty} "+
   $"maxActive={_pr57379MaxActive} maxToggled={_pr57379MaxToggled} "+
   $"maxPersistent={_pr57379MaxPersistent} comparisonTicks={_pr57379Ticks} "+
   $"tickFrequency={Stopwatch.Frequency} partialGate=True noCausalClosure=True "+
   "noDecisionReuse=True noSkippedSlices=True observational=True");
 }
}
''')
print("PR57_379_INJECTED partial gate reader-presence XOR shadow")
