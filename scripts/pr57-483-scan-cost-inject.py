#!/usr/bin/env python3
"""PR57 #483: measure a read-only full-fleet physical projection scan.

Injected only into a disposable CI checkout. It never reuses decisions,
skips sub-slices, changes the ordered first-blocker scan or edits saves.
The scan deliberately omits storage, comparison, writer tracking and
external invalidators: its measured cost is NOT a complete validator.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]

def once(path, old, new):
    data = path.read_text(encoding="utf-8")
    assert data.count(old) == 1, (path, old[:100], data.count(old))
    path.write_text(data.replace(old, new, 1), encoding="utf-8")

mob = root / "src/IndexCities.Simulation/Mobility.cs"
once(mob, "&&CanTurnNow(o)).ToList();",
     "&&Pr57483PreferenceCanTurn(o)).ToList();")

shared = root / "src/IndexCities.Simulation/Mobility.SharedSpace.cs"
once(shared, "    bool CanTurnNow(Vehicle o)\n    {", """    // #483: observational, read-only O(fleet) projection before canonical work.
    static readonly bool Pr57483Enabled =
        Environment.GetEnvironmentVariable("PR57_483_SCAN_COST") == "1";
    long _pr57483Calls, _pr57483Visits, _pr57483ScanTicks,
         _pr57483CanonicalTicks, _pr57483ScanBytes, _pr57483CanonicalBytes,
         _pr57483Checksum;
    bool Pr57483PreferenceCanTurn(Vehicle actor)
    {
        if (!Pr57483Enabled) return CanTurnNow(actor);
        var bytes0 = GC.GetAllocatedBytesForCurrentThread();
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        long bits = 0;
        // Full roster, not just the canonical prefix. The actual projection
        // compares nothing, has no saved versions, and cannot prove reuse.
        foreach (var other in State.Vehicles)
        {
            unchecked
            {
                bits ^= other.Id;
                bits ^= other.TowTargetId ?? -1;
                bits ^= other.TowTruckId ?? -1;
                bits ^= (int)other.Status;
                bits ^= BitConverter.DoubleToInt64Bits(other.Position.X);
                bits ^= BitConverter.DoubleToInt64Bits(other.Position.Z);
                bits ^= BitConverter.DoubleToInt64Bits(other.Heading);
                bits ^= BitConverter.DoubleToInt64Bits(BodyHalfLength(other));
                bits ^= BitConverter.DoubleToInt64Bits(BodyHalfWidth(other));
            }
            _pr57483Visits++;
        }
        bits ^= BitConverter.DoubleToInt64Bits(State.Entry.X);
        bits ^= BitConverter.DoubleToInt64Bits(State.Entry.Z);
        _pr57483Checksum ^= bits;
        _pr57483ScanTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57483ScanBytes += GC.GetAllocatedBytesForCurrentThread() - bytes0;
        var bytes1 = GC.GetAllocatedBytesForCurrentThread();
        var canonicalStart = System.Diagnostics.Stopwatch.GetTimestamp();
        var answer = CanTurnNow(actor);
        _pr57483CanonicalTicks += System.Diagnostics.Stopwatch.GetTimestamp() - canonicalStart;
        _pr57483CanonicalBytes += GC.GetAllocatedBytesForCurrentThread() - bytes1;
        _pr57483Calls++;
        // Exact known REAL fixture horizon, 100 public Advance(2) calls.
        if (_pr57483Calls == 1596)
        {
            var hz = (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine($"PR57_483_SCAN calls={_pr57483Calls} visits={_pr57483Visits} " +
                $"scanTicks={_pr57483ScanTicks} canonicalTicks={_pr57483CanonicalTicks} " +
                $"scanMs={_pr57483ScanTicks * 1000.0 / hz:F6} " +
                $"canonicalMs={_pr57483CanonicalTicks * 1000.0 / hz:F6} " +
                $"scanBytes={_pr57483ScanBytes} canonicalBytes={_pr57483CanonicalBytes} " +
                $"checksum={_pr57483Checksum} observational=True closure=False");
        }
        return answer;
    }
    bool CanTurnNow(Vehicle o)
    {""")
print("PR57_483_INJECTED observational=True fullFleetScan=True noCache=True noSkip=True")
