#!/usr/bin/env python3
"""PR57 #377: opt-in compact bitset reverse-reader shadow, after #376.

Compare with #376 HashSet subscription on the same original OccupiesLane calls.
No decisions reused, slices skipped, causal closure claimed, or product changed.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "src/IndexCities.Simulation"
p = root / "Mobility.NextSliceFanout374.Pr57.cs"
s = p.read_text(encoding="utf-8")

def patch(old, new):
    global s
    assert s.count(old) == 1, (old, s.count(old))
    s = s.replace(old, new)

patch("        Pr57376Begin();", "        Pr57376Begin();\n        Pr57377Begin();")
patch("        Pr57376Writer(v);", "        Pr57376Writer(v);\n        Pr57377Writer(v);")
patch("        Pr57376BeginSlice();", "        Pr57376BeginSlice();\n        Pr57377BeginSlice();")
patch("        Pr57376Read(reader,candidate);",
      "        Pr57376Read(reader,candidate);\n        Pr57377Read(reader,candidate);")
patch("        Pr57376Finish();", "        Pr57376Finish();\n        Pr57377Finish();")
p.write_text(s, encoding="utf-8")

out = root / "Mobility.BitsetReader377.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;
using System.Numerics;

namespace IndexCities.Simulation;

// Only a compact representation experiment for the same PARTIAL occupancy gate.
// Vehicle IDs may be sparse; the ordinal map is built from the authentic city.
// Max 128 vehicles in this bounded experiment; no implicit truncation allowed.
public sealed partial class CitySimulation
{
    static readonly bool Pr57377Enabled =
        Environment.GetEnvironmentVariable("PR57_377_BITSET_SHADOW") == "1";
    const long Pr57377SliceLimit = 600;
    bool _pr57377Active;
    long _pr57377Slices, _pr57377ReadCalls, _pr57377UniqueReadPairs;
    long _pr57377WriterEvents, _pr57377UniqueWriters, _pr57377ExternalEvents;
    long _pr57377InvalidatedReaderPairs, _pr57377WritersWithReaders;
    long _pr57377RegistrationTicks, _pr57377PropagationTicks;
    int _pr57377MaxReaders;
    readonly Dictionary<int,int> _pr57377Ordinals = new();
    Dictionary<int,Pr57377Mask> _pr57377Previous = new(128);
    Dictionary<int,Pr57377Mask> _pr57377Current = new(128);
    readonly HashSet<int> _pr57377WrittenThisSlice = new();

    readonly struct Pr57377Mask
    {
        public readonly ulong Lo, Hi;
        public Pr57377Mask(ulong lo, ulong hi) { Lo=lo; Hi=hi; }
        public Pr57377Mask Add(int ordinal) => ordinal < 64
            ? new(Lo | (1UL << ordinal), Hi)
            : new(Lo, Hi | (1UL << (ordinal-64)));
        public Pr57377Mask Or(Pr57377Mask other) =>
            new(Lo | other.Lo, Hi | other.Hi);
        public int Count => BitOperations.PopCount(Lo)+BitOperations.PopCount(Hi);
    }

    void Pr57377Begin()
    {
        _pr57377Active=Pr57377Enabled && _pr57374Active;
        if(!_pr57377Active)return;
        if(State.Vehicles.Count>128)
            throw new InvalidOperationException("#377 only supports <=128 vehicles");
        _pr57377Ordinals.Clear();
        for(int i=0;i<State.Vehicles.Count;i++)
            if(!_pr57377Ordinals.TryAdd(State.Vehicles[i].Id,i))
                throw new InvalidOperationException("#377 duplicate vehicle ID");
    }

    void Pr57377BeginSlice()
    {
        if(!_pr57377Active)return;
        if(_pr57374PhysicalSlices>Pr57377SliceLimit)
        {
            if(_pr57374PhysicalSlices==Pr57377SliceLimit+1)
            {
                _pr57377Previous.Clear();
                _pr57377Current.Clear();
            }
            return;
        }
        _pr57377Slices++;
        var reuse=_pr57377Previous;
        _pr57377Previous=_pr57377Current;
        _pr57377Current=reuse;
        _pr57377Current.Clear();
        _pr57377WrittenThisSlice.Clear();
    }

    void Pr57377Read(Vehicle reader,Vehicle candidate)
    {
        if(!_pr57377Active || _pr57374PhysicalSlices==0 ||
           _pr57374PhysicalSlices>Pr57377SliceLimit)return;
        _pr57377ReadCalls++;
        long started=Stopwatch.GetTimestamp();
        if(!_pr57377Ordinals.TryGetValue(reader.Id,out var ordinal))
            throw new InvalidOperationException("#377 reader not in canonical roster");
        var old=_pr57377Current.GetValueOrDefault(candidate.Id);
        var updated=old.Add(ordinal);
        if(old.Lo!=updated.Lo || old.Hi!=updated.Hi)
            _pr57377UniqueReadPairs++;
        _pr57377Current[candidate.Id]=updated;
        _pr57377RegistrationTicks+=Stopwatch.GetTimestamp()-started;
    }

    void Pr57377Writer(Vehicle v)
    {
        if(!_pr57377Active || _pr57374PhysicalSlices==0 ||
           _pr57374PhysicalSlices>Pr57377SliceLimit)return;
        _pr57377WriterEvents++;
        if(_pr57367Phase!=0){_pr57377ExternalEvents++;return;}
        if(!_pr57377WrittenThisSlice.Add(v.Id))return;
        _pr57377UniqueWriters++;
        long started=Stopwatch.GetTimestamp();
        var mask=_pr57377Previous.GetValueOrDefault(v.Id)
            .Or(_pr57377Current.GetValueOrDefault(v.Id));
        int affected=mask.Count;
        _pr57377InvalidatedReaderPairs+=affected;
        if(affected>0)_pr57377WritersWithReaders++;
        _pr57377MaxReaders=Math.Max(_pr57377MaxReaders,affected);
        _pr57377PropagationTicks+=Stopwatch.GetTimestamp()-started;
    }

    void Pr57377Finish()
    {
        if(!_pr57377Active)return;
        _pr57377Active=false;
        Console.WriteLine($"PR57_377_SUMMARY physicalSlices={_pr57377Slices} "+
            $"readCalls={_pr57377ReadCalls} uniqueReadPairs={_pr57377UniqueReadPairs} "+
            $"writerEvents={_pr57377WriterEvents} uniqueWriters={_pr57377UniqueWriters} "+
            $"externalEvents={_pr57377ExternalEvents} "+
            $"writersWithReaders={_pr57377WritersWithReaders} "+
            $"invalidatedReaderPairs={_pr57377InvalidatedReaderPairs} "+
            $"maxReadersPerWriter={_pr57377MaxReaders} "+
            $"registrationTicks={_pr57377RegistrationTicks} "+
            $"propagationTicks={_pr57377PropagationTicks} tickFrequency={Stopwatch.Frequency} "+
            "scope=FIRST_600_PHYSICAL_SLICES_PARTIAL_OCCUPIES_BITSET "+
            "noCausalClosure=True noDecisionReuse=True noSkippedSlices=True observational=True");
    }
}
''', encoding="utf-8")
print("PR57_377_INJECTED compact partial reverse-reader bitset shadow")
