#!/usr/bin/env python3
"""PR57 #536: disposable, observational shipment membership phase census.

This injects a read-only snapshot at the start and after each of the 13 Step
phases in a CI checkout. It measures NET membership/predicate changes only.
It cannot see transient writes, direct external edits or all cargo mutations.
It does not cache, reorder, skip or change gameplay decisions.
"""
from pathlib import Path

target = Path("src/IndexCities.Simulation/CitySimulation.cs")
s = target.read_text(encoding="utf-8")
old = "        Pr57WorkAccessBeginStep();"
assert s.count(old) == 1, ("begin", s.count(old))
s = s.replace(old, old + "\n        Pr57536BeginStep();", 1)
for i in range(13):
    old = f"        if(profile is not null)profile.Mark({i},ref started);"
    assert s.count(old) == 1, (i, s.count(old))
    s = s.replace(old, f"        Pr57536ObservePhase({i});\n" + old, 1)
target.write_text(s, encoding="utf-8")

probe = r'''using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IndexCities.Simulation;

// CI-only intrusive NET phase-boundary observation. All 13 phases execute.
// Not an exact writer trace; transient same-phase mutations may cancel out.
public sealed partial class CitySimulation
{
    static readonly bool Pr57536Enabled =
        Environment.GetEnvironmentVariable("PR57_536_SHIPMENT_PHASE") == "1";

    readonly record struct Pr57536Shipment(
        int Id, int? ProjectId, bool Complete, bool Cancelled, bool PickedUp);

    List<Pr57536Shipment>? _pr57536Prior, _pr57536AfterProgress;
    long _pr57536Steps, _pr57536Visits, _pr57536Ticks;
    long _pr57536ProgressToDispatchStable, _pr57536ProgressToDispatchChanged;
    readonly long[] _pr57536ChangedSteps = new long[13];
    readonly long[] _pr57536ChangedRecords = new long[13];
    readonly long[] _pr57536Appends = new long[13];
    readonly long[] _pr57536Removes = new long[13];
    readonly long[] _pr57536Reassigns = new long[13];
    readonly long[] _pr57536CompletionFlips = new long[13];
    readonly long[] _pr57536OtherFlips = new long[13];
    int _pr57536Witnesses;

    List<Pr57536Shipment> Pr57536Snapshot()
    {
        var started = Stopwatch.GetTimestamp();
        var list = new List<Pr57536Shipment>(State.Shipments.Count);
        foreach (var s in State.Shipments)
        {
            list.Add(new(s.Id, s.ProjectId, s.Complete, s.Cancelled, s.PickedUp));
            _pr57536Visits++;
        }
        _pr57536Ticks += Stopwatch.GetTimestamp() - started;
        return list;
    }

    void Pr57536BeginStep()
    {
        if (!Pr57536Enabled) return;
        _pr57536Steps++;
        _pr57536Prior = Pr57536Snapshot();
        _pr57536AfterProgress = null;
    }

    void Pr57536ObservePhase(int phase)
    {
        if (!Pr57536Enabled) return;
        var now = Pr57536Snapshot();
        var before = _pr57536Prior ?? throw new InvalidOperationException("Missing #536 phase baseline");
        var old = before.ToDictionary(x => x.Id);
        long changed = 0;
        foreach (var item in now)
        {
            if (!old.Remove(item.Id, out var previous))
            {
                _pr57536Appends[phase]++;
                changed++;
                continue;
            }
            if (item == previous) continue;
            changed++;
            if (item.ProjectId != previous.ProjectId) _pr57536Reassigns[phase]++;
            if (item.Complete != previous.Complete) _pr57536CompletionFlips[phase]++;
            if (item.Cancelled != previous.Cancelled || item.PickedUp != previous.PickedUp)
                _pr57536OtherFlips[phase]++;
        }
        _pr57536Removes[phase] += old.Count;
        changed += old.Count;
        // A reordering of otherwise identical IDs is still a relevant change.
        if (changed == 0 && !before.SequenceEqual(now)) changed = 1;
        if (changed > 0)
        {
            _pr57536ChangedSteps[phase]++;
            _pr57536ChangedRecords[phase] += changed;
            if (_pr57536Witnesses < 16)
            {
                _pr57536Witnesses++;
                Console.WriteLine($"PR57_536_WITNESS step={_pr57536Steps} phase={StepCostProfile.Names[phase]} " +
                    $"changed={changed} before={before.Count} after={now.Count} " +
                    "scope=NET_ONLY");
            }
        }
        if (phase == 3) _pr57536AfterProgress = now;
        if (phase == 9)
        {
            if (_pr57536AfterProgress is not null && _pr57536AfterProgress.SequenceEqual(now))
                _pr57536ProgressToDispatchStable++;
            else _pr57536ProgressToDispatchChanged++;
        }
        _pr57536Prior = phase == 12 ? null : now;
        if (phase == 12)
        {
            _pr57536AfterProgress = null;
            if (_pr57536Steps == 1 || _pr57536Steps % 25 == 0)
            {
                Console.WriteLine($"PR57_536_SUM steps={_pr57536Steps} visits={_pr57536Visits} " +
                    $"stableProgressToDispatch={_pr57536ProgressToDispatchStable} " +
                    $"changedProgressToDispatch={_pr57536ProgressToDispatchChanged} " +
                    $"changedPhaseSteps={_pr57536ChangedSteps.Sum()} " +
                    $"appends={_pr57536Appends.Sum()} removes={_pr57536Removes.Sum()} " +
                    $"reassigns={_pr57536Reassigns.Sum()} completionFlips={_pr57536CompletionFlips.Sum()} " +
                    $"otherFlips={_pr57536OtherFlips.Sum()} " +
                    "scope=NET_PHASE_BOUNDARIES_ONLY");
                for (int i = 0; i < 13; i++)
                    Console.WriteLine($"PR57_536_PHASE steps={_pr57536Steps} name={StepCostProfile.Names[i]} " +
                        $"changedSteps={_pr57536ChangedSteps[i]} changedRecords={_pr57536ChangedRecords[i]} " +
                        $"appends={_pr57536Appends[i]} removes={_pr57536Removes[i]} " +
                        $"reassigns={_pr57536Reassigns[i]} completionFlips={_pr57536CompletionFlips[i]} " +
                        $"otherFlips={_pr57536OtherFlips[i]}");
                Console.WriteLine($"PR57_536_COST steps={_pr57536Steps} " +
                    $"snapshotTicks={_pr57536Ticks} stopwatchFrequency={Stopwatch.Frequency} " +
                    "scope=INTRUSIVE_SNAPSHOT_ONLY_NOT_ENGINE_GAIN");
            }
        }
    }
}
'''
out = Path("src/IndexCities.Simulation/Pr57536ShipmentPhase.CiOnly.cs")
out.write_text(probe, encoding="utf-8")
print("PR57_536_DISPOSABLE_SHIPMENT_PHASE_INJECTION_PASS")
