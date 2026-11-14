#!/usr/bin/env python3
"""PR57 #378: exact edge-set churn of the bounded #377 partial reader graph.

Applied after #377 in a disposable checkout. Shadow-only, no reuse, no skips.
Compares complete per-candidate bitmasks across 599 consecutive physical slices.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "src/IndexCities.Simulation"
p = root / "Mobility.BitsetReader377.Pr57.cs"
s = p.read_text(encoding="utf-8")

def patch(old, new):
    global s
    assert s.count(old) == 1, (old, s.count(old))
    s = s.replace(old, new)

patch("        if(_pr57374PhysicalSlices>Pr57377SliceLimit)\n        {",
      "        Pr57378Compare();\n        if(_pr57374PhysicalSlices>Pr57377SliceLimit)\n        {")
patch("    void Pr57377Finish()\n    {\n        if(!_pr57377Active)return;",
      "    void Pr57377Finish()\n    {\n        if(!_pr57377Active)return;\n        Pr57378Finish();")
p.write_text(s, encoding="utf-8")

out = root / "Mobility.ReaderChurn378.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;
using System.Numerics;

namespace IndexCities.Simulation;

// Observational XOR of complete PARTIAL OccupiesLane reader masks.
// Does not observe all predicates or external invalidators; no causal certificate.
public sealed partial class CitySimulation
{
    static readonly bool Pr57378Enabled =
        Environment.GetEnvironmentVariable("PR57_378_CHURN_SHADOW") == "1";
    long _pr57378Transitions, _pr57378Changed, _pr57378Added, _pr57378Removed;
    long _pr57378Stable, _pr57378ChangedCandidates, _pr57378MaxChurn;
    long _pr57378Zero, _pr57378OneToTen, _pr57378ElevenToHundred, _pr57378OverHundred;
    long _pr57378NoWriterPairs, _pr57378NoWriterChurn;
    long _pr57378QuietPairs, _pr57378QuietChurn;
    long _pr57378ComparisonTicks, _pr57378LastExternalEvents;
    int _pr57378PreviousWriters;
    long _pr57378PreviousExternal;

    void Pr57378Compare()
    {
        if(!Pr57378Enabled || !_pr57377Active)return;
        long slice = _pr57374PhysicalSlices;
        if(slice<2 || slice>Pr57377SliceLimit+1)return;
        // Before the #377 rotation: Previous = slice n-2, Current = slice n-1.
        int writers = _pr57377WrittenThisSlice.Count;
        long external = _pr57377ExternalEvents-_pr57378LastExternalEvents;
        _pr57378LastExternalEvents=_pr57377ExternalEvents;
        if(slice==2)
        {
            _pr57378PreviousWriters=writers;
            _pr57378PreviousExternal=external;
            return;
        }
        long started=Stopwatch.GetTimestamp();
        long added=0,removed=0,stable=0;
        int changedCandidates=0;
        foreach(var pair in _pr57377Current)
        {
            var old=_pr57377Previous.GetValueOrDefault(pair.Key);
            var now=pair.Value;
            added+=BitOperations.PopCount(now.Lo & ~old.Lo)+
                   BitOperations.PopCount(now.Hi & ~old.Hi);
            removed+=BitOperations.PopCount(old.Lo & ~now.Lo)+
                     BitOperations.PopCount(old.Hi & ~now.Hi);
            stable+=BitOperations.PopCount(now.Lo & old.Lo)+
                    BitOperations.PopCount(now.Hi & old.Hi);
            if(now.Lo!=old.Lo || now.Hi!=old.Hi)changedCandidates++;
        }
        foreach(var pair in _pr57377Previous)
        {
            if(_pr57377Current.ContainsKey(pair.Key))continue;
            removed+=pair.Value.Count;
            if(pair.Value.Count>0)changedCandidates++;
        }
        long churn=added+removed;
        _pr57378Transitions++;
        _pr57378Added+=added;
        _pr57378Removed+=removed;
        _pr57378Stable+=stable;
        _pr57378ChangedCandidates+=changedCandidates;
        _pr57378MaxChurn=Math.Max(_pr57378MaxChurn,churn);
        if(churn==0)_pr57378Zero++;
        else
        {
            _pr57378Changed++;
            if(churn<=10)_pr57378OneToTen++;
            else if(churn<=100)_pr57378ElevenToHundred++;
            else _pr57378OverHundred++;
        }
        if(writers==0 && _pr57378PreviousWriters==0)
        {
            _pr57378NoWriterPairs++;
            _pr57378NoWriterChurn+=churn;
            if(external==0 && _pr57378PreviousExternal==0)
            {
                _pr57378QuietPairs++;
                _pr57378QuietChurn+=churn;
            }
        }
        _pr57378PreviousWriters=writers;
        _pr57378PreviousExternal=external;
        _pr57378ComparisonTicks+=Stopwatch.GetTimestamp()-started;
    }

    void Pr57378Finish()
    {
        if(!Pr57378Enabled)return;
        if(_pr57377Slices!=Pr57377SliceLimit || _pr57378Transitions!=Pr57377SliceLimit-1)
            throw new InvalidOperationException("#378 incomplete bounded observation");
        Console.WriteLine($"PR57_378_SUMMARY physicalSlices={_pr57377Slices} "+
            $"transitions={_pr57378Transitions} changedTransitions={_pr57378Changed} "+
            $"unchangedTransitions={_pr57378Zero} addedEdges={_pr57378Added} "+
            $"removedEdges={_pr57378Removed} stableEdges={_pr57378Stable} "+
            $"changedCandidates={_pr57378ChangedCandidates} maxChurn={_pr57378MaxChurn} "+
            $"churn1to10={_pr57378OneToTen} churn11to100={_pr57378ElevenToHundred} "+
            $"churnOver100={_pr57378OverHundred} noWriterPairs={_pr57378NoWriterPairs} "+
            $"noWriterChurn={_pr57378NoWriterChurn} quietPairs={_pr57378QuietPairs} "+
            $"quietChurn={_pr57378QuietChurn} comparisonTicks={_pr57378ComparisonTicks} "+
            $"tickFrequency={Stopwatch.Frequency} "+
            "scope=FIRST_600_PHYSICAL_SLICES_PARTIAL_OCCUPIES_XOR "+
            "noCausalClosure=True noDecisionReuse=True noSkippedSlices=True observational=True");
    }
}
''', encoding="utf-8")
print("PR57_378_INJECTED bounded partial reader-edge XOR shadow")
