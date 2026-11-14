#!/usr/bin/env python3
"""#530 disposable observational probe: per-query preference candidate churn.

The canonical LINQ guard and CanTurnNow evaluation are preserved. This probe
observes candidates that actually pass the pre-CanTurnNow guard, not all
potential writers or negative guards. It is not a causal certificate.
"""
from pathlib import Path

root = Path('src/IndexCities.Simulation')
mobility = root / 'Mobility.cs'
s = mobility.read_text(encoding='utf-8')

def once(old, new):
    global s
    assert s.count(old) == 1, (old[:100], s.count(old))
    s = s.replace(old, new, 1)

once('        Pr57JunctionCensusBeginSlice(dt,operationsDt);',
     '        Pr57530BeginSlice(dt,operationsDt);\n        Pr57JunctionCensusBeginSlice(dt,operationsDt);')
once('        Pr57JunctionCensusEndSlice();',
     '        Pr57530EndSlice();\n        Pr57JunctionCensusEndSlice();')
old = '        var competitors=State.Vehicles.Where(o=>o.Id!=v.Id&&o.Status==VehicleStatus.Travelling&&o.RouteIndex<o.Route.Count&&o.Route[o.RouteIndex]==junction.Id&&o.Position.Distance(junction.Position)<14&&CanTurnNow(o)).ToList();'
new = '''        var pr57530Query=Pr57530Begin(v,junction);
        var competitors=State.Vehicles.Where(o=>o.Id!=v.Id&&o.Status==VehicleStatus.Travelling&&o.RouteIndex<o.Route.Count&&o.Route[o.RouteIndex]==junction.Id&&o.Position.Distance(junction.Position)<14&&Pr57530CanTurn(pr57530Query,o)).ToList();
        Pr57530Finish(pr57530Query);'''
once(old, new)
mobility.write_text(s, encoding='utf-8')

probe = r'''using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexCities.Simulation;

// #530 CI-only observational probe. No cached result influences the engine.
// A query is a canonical preference evaluation after the signal early guard.
public sealed partial class CitySimulation
{
    static readonly bool Pr57530Enabled =
        Environment.GetEnvironmentVariable("PR57_530_PREFERENCE_CHURN") == "1";

    sealed class Pr57530Query
    {
        public int ReaderId;
        public int JunctionId;
        public string ReaderState = "";
        public readonly List<int> Candidates = new();
        public int Passed;
    }
    sealed class Pr57530Previous
    {
        public string ReaderState = "";
        public int[] Candidates = Array.Empty<int>();
    }

    readonly Dictionary<(int Reader, int Junction), Pr57530Previous> _pr57530Last = new();
    long _pr57530Slices, _pr57530Queries, _pr57530Nested, _pr57530Passed,
         _pr57530Transitions, _pr57530SameReaderTransitions,
         _pr57530SameReaderQueries, _pr57530UnchangedSets,
         _pr57530Added, _pr57530Removed, _pr57530ZeroToNonzero,
         _pr57530NonzeroToZero, _pr57530Reorders, _pr57530MaxFanout;
    int _pr57530Witnesses;

    void Pr57530BeginSlice(double dt, double? operationsDt)
    {
        if (!Pr57530Enabled) return;
        _pr57530Slices++; // includes dt=0 operational invocations
    }

    Pr57530Query? Pr57530Begin(Vehicle reader, RoadNode junction)
    {
        if (!Pr57530Enabled) return null;
        return new Pr57530Query {
            ReaderId = reader.Id,
            JunctionId = junction.Id,
            ReaderState = Pr57MutationSnapshot(reader)
        };
    }

    bool Pr57530CanTurn(Pr57530Query? query, Vehicle candidate)
    {
        if (query is not null) query.Candidates.Add(candidate.Id);
        var result = CanTurnNow(candidate); // exactly one canonical evaluation
        if (query is not null && result) query.Passed++;
        return result;
    }

    void Pr57530Finish(Pr57530Query? query)
    {
        if (query is null) return;
        _pr57530Queries++;
        _pr57530Nested += query.Candidates.Count;
        _pr57530Passed += query.Passed;
        if (query.Candidates.Count > _pr57530MaxFanout)
            _pr57530MaxFanout = query.Candidates.Count;
        var key = (query.ReaderId, query.JunctionId);
        if (_pr57530Last.TryGetValue(key, out var previous))
        {
            bool readerUnchanged = string.Equals(previous.ReaderState,
                query.ReaderState, StringComparison.Ordinal);
            if (readerUnchanged) _pr57530SameReaderQueries++;
            if (previous.Candidates.SequenceEqual(query.Candidates))
                _pr57530UnchangedSets++;
            else
            {
                _pr57530Transitions++;
                var oldSet = new HashSet<int>(previous.Candidates);
                var newSet = new HashSet<int>(query.Candidates);
                int added = newSet.Count(id => !oldSet.Contains(id));
                int removed = oldSet.Count(id => !newSet.Contains(id));
                if (added == 0 && removed == 0) _pr57530Reorders++;
                _pr57530Added += added;
                _pr57530Removed += removed;
                if (previous.Candidates.Length == 0 && query.Candidates.Count > 0)
                    _pr57530ZeroToNonzero++;
                if (previous.Candidates.Length > 0 && query.Candidates.Count == 0)
                    _pr57530NonzeroToZero++;
                if (readerUnchanged)
                {
                    _pr57530SameReaderTransitions++;
                    if (_pr57530Witnesses < 24)
                    {
                        _pr57530Witnesses++;
                        Console.WriteLine($"PR57_530_WITNESS reader={query.ReaderId} " +
                            $"junction={query.JunctionId} slice={_pr57530Slices} " +
                            $"old={string.Join(",", previous.Candidates)} " +
                            $"now={string.Join(",", query.Candidates)} " +
                            "sameFullPersistedReader=True firstWriterUnknown=True");
                    }
                }
            }
        }
        _pr57530Last[key] = new Pr57530Previous {
            ReaderState = query.ReaderState,
            Candidates = query.Candidates.ToArray()
        };
    }

    void Pr57530EndSlice()
    {
        if (!Pr57530Enabled) return;
        if (_pr57530Slices == 1 || _pr57530Slices % 25 == 0)
            Console.WriteLine($"PR57_530_SUM slices={_pr57530Slices} " +
                $"queries={_pr57530Queries} nested={_pr57530Nested} " +
                $"passed={_pr57530Passed} distinctReaderJunctions={_pr57530Last.Count} " +
                $"transitions={_pr57530Transitions} " +
                $"sameReaderTransitions={_pr57530SameReaderTransitions} " +
                $"sameReaderQueries={_pr57530SameReaderQueries} " +
                $"unchangedSets={_pr57530UnchangedSets} " +
                $"added={_pr57530Added} removed={_pr57530Removed} " +
                $"zeroToNonzero={_pr57530ZeroToNonzero} " +
                $"nonzeroToZero={_pr57530NonzeroToZero} " +
                $"reorders={_pr57530Reorders} maxFanout={_pr57530MaxFanout} " +
                "scope=PRE_CANTURN_GUARD_ONLY firstWriterUnknown=True observational=True");
    }
}
'''
(root / 'Pr57530PreferenceChurn.CiOnly.cs').write_text(probe, encoding='utf-8')
print('PR57_530_DISPOSABLE_PREFERENCE_CHURN_INJECTION_PASS')
