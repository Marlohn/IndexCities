#!/usr/bin/env python3
"""PR57 #376: bounded reverse-reader subscription shadow, after #374.

Checkout-only instrumentation: observes the *original* Pr57StageOccupies call,
subscribes reader IDs to candidate IDs for at most 600 physical sub-slices,
and simulates notification fanout on selected canonical writer events.
Never changes a predicate, skips a slice, reuses a decision, or claims closure.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation"
p=root/"Mobility.NextSliceFanout374.Pr57.cs"
s=p.read_text(encoding="utf-8")

def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new)

patch("        if(Pr57374Enabled&&_pr57372Active)_pr57374Active=true;",
      "        if(Pr57374Enabled&&_pr57372Active)_pr57374Active=true;\n        Pr57376Begin();")
patch("        _pr57374Events++;",
      "        _pr57374Events++;\n        Pr57376Writer(v);")
patch("        _pr57374ReadCandidates.Clear();",
      "        _pr57374ReadCandidates.Clear();\n        Pr57376BeginSlice();")
patch("        bool answer=Pr57StageOccupies(candidate);",
      "        bool answer=Pr57StageOccupies(candidate);\n        Pr57376Read(reader,candidate);")
patch("        Pr57374CloseSlice();\n        _pr57374Active=false;",
      "        Pr57374CloseSlice();\n        Pr57376Finish();\n        _pr57374Active=false;")
p.write_text(s,encoding="utf-8")

out=root/"Mobility.ReverseReader376.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;

namespace IndexCities.Simulation;

// Bounded PARTIAL read graph for the lane-occupancy gate only. A read edge is
// not proof that the candidate controls the final motion decision. This graph
// omits other physical gates, pedestrians, signals, clock, roads and commands.
// All canonical readers and writers continue to execute without modification.
public sealed partial class CitySimulation
{
    static readonly bool Pr57376Enabled=
        Environment.GetEnvironmentVariable("PR57_376_REVERSE_SHADOW")=="1";
    const long Pr57376SliceLimit=600;
    bool _pr57376Active;
    long _pr57376Slices,_pr57376ReadCalls,_pr57376UniqueReadPairs;
    long _pr57376WriterEvents,_pr57376UniqueWriters,_pr57376ExternalEvents;
    long _pr57376InvalidatedReaderPairs,_pr57376WritersWithReaders;
    long _pr57376RegistrationTicks,_pr57376PropagationTicks;
    int _pr57376MaxReaders;
    Dictionary<int,HashSet<int>> _pr57376Previous=new();
    Dictionary<int,HashSet<int>> _pr57376Current=new();
    readonly HashSet<int> _pr57376WrittenThisSlice=new();

    void Pr57376Begin()
    {
        _pr57376Active=Pr57376Enabled&&_pr57374Active;
    }

    void Pr57376BeginSlice()
    {
        if(!_pr57376Active)return;
        if(_pr57374PhysicalSlices>Pr57376SliceLimit)
        {
            if(_pr57374PhysicalSlices==Pr57376SliceLimit+1)
            {
                _pr57376Previous.Clear();
                _pr57376Current.Clear();
            }
            return;
        }
        _pr57376Slices++;
        var reuse=_pr57376Previous;
        _pr57376Previous=_pr57376Current;
        _pr57376Current=reuse;
        _pr57376Current.Clear();
        _pr57376WrittenThisSlice.Clear();
    }

    void Pr57376Read(Vehicle reader,Vehicle candidate)
    {
        if(!_pr57376Active||_pr57374PhysicalSlices==0||
           _pr57374PhysicalSlices>Pr57376SliceLimit)return;
        _pr57376ReadCalls++;
        long started=Stopwatch.GetTimestamp();
        if(!_pr57376Current.TryGetValue(candidate.Id,out var readers))
        {
            readers=new HashSet<int>();
            _pr57376Current.Add(candidate.Id,readers);
        }
        if(readers.Add(reader.Id))_pr57376UniqueReadPairs++;
        _pr57376RegistrationTicks+=Stopwatch.GetTimestamp()-started;
    }

    void Pr57376Writer(Vehicle v)
    {
        if(!_pr57376Active||_pr57374PhysicalSlices==0||
           _pr57374PhysicalSlices>Pr57376SliceLimit)return;
        _pr57376WriterEvents++;
        // External phases are counted but excluded from same-slice
        // coalescing: their canonical barrier is not this physical slice.
        if(_pr57367Phase!=0){_pr57376ExternalEvents++;return;}
        if(!_pr57376WrittenThisSlice.Add(v.Id))return;
        _pr57376UniqueWriters++;
        long started=Stopwatch.GetTimestamp();
        var affected=new HashSet<int>();
        if(_pr57376Previous.TryGetValue(v.Id,out var prior))
            affected.UnionWith(prior);
        if(_pr57376Current.TryGetValue(v.Id,out var current))
            affected.UnionWith(current);
        _pr57376InvalidatedReaderPairs+=affected.Count;
        if(affected.Count>0)_pr57376WritersWithReaders++;
        _pr57376MaxReaders=Math.Max(_pr57376MaxReaders,affected.Count);
        _pr57376PropagationTicks+=Stopwatch.GetTimestamp()-started;
    }

    void Pr57376Finish()
    {
        if(!_pr57376Active)return;
        _pr57376Active=false;
        Console.WriteLine($"PR57_376_SUMMARY physicalSlices={_pr57376Slices} "+
            $"readCalls={_pr57376ReadCalls} uniqueReadPairs={_pr57376UniqueReadPairs} "+
            $"writerEvents={_pr57376WriterEvents} uniqueWriters={_pr57376UniqueWriters} "+
            $"externalEvents={_pr57376ExternalEvents} "+
            $"writersWithReaders={_pr57376WritersWithReaders} "+
            $"invalidatedReaderPairs={_pr57376InvalidatedReaderPairs} "+
            $"maxReadersPerWriter={_pr57376MaxReaders} "+
            $"registrationTicks={_pr57376RegistrationTicks} "+
            $"propagationTicks={_pr57376PropagationTicks} tickFrequency={Stopwatch.Frequency} "+
            "scope=FIRST_600_PHYSICAL_SLICES_PARTIAL_OCCUPIES "+
            "noCausalClosure=True noDecisionReuse=True noSkippedSlices=True observational=True");
    }
}
''',encoding="utf-8")
print("PR57_376_INJECTED bounded partial reverse-reader subscription shadow")
