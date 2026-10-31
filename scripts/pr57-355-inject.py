#!/usr/bin/env python3
"""PR57 #355: observe exact canonical lane-scan outputs across consecutive REAL slices.

Requires #353 and #354 checkout-only injections. Does not skip any candidate,
slice, predicate, admission, publication or canonical state mutation. This is
NOT a causal closure or a performance optimization.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
base = root / "src/IndexCities.Simulation"

def once(source, old, new):
    assert source.count(old) == 1, (old, source.count(old))
    return source.replace(old, new, 1)

mob = base / "Mobility.cs"
source = mob.read_text()
source = once(source,
    "Pr57353Read(v,blocker,leaderDistance,step,enteringLane);",
    "Pr57353Read(v,blocker,leaderDistance,step,enteringLane);\n"
    "                Pr57355Read(v,blocker,leaderDistance,step,enteringLane);")
mob.write_text(source)

generated = base / "Mobility.WriterJournal353.Pr57.cs"
source = generated.read_text()
source = once(source,
    "        Pr57354BeginSlice();\n        Vehicle.Pr57353Write = Pr57353Write;",
    "        Pr57354BeginSlice();\n        Pr57355BeginSlice();\n"
    "        Vehicle.Pr57353Write = Pr57353Write;")
source = once(source,
    "        Pr57354EndSlice();\n        Vehicle.Pr57353Write = null;",
    "        Pr57355EndSlice();\n        Pr57354EndSlice();\n"
    "        Vehicle.Pr57353Write = null;")
generated.write_text(source)

out = base / "Mobility.LaneOutput355.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// #355: selected lane-scan result witness for the same 20 queue readers as #351.
// No result is substituted; the entire canonical scan runs in its original order.
// Only the winner/distance/step and selected reader context are observed.
// Not a certificate for reuse: external dependencies, intermediate writes and
// other physical stages are not covered.
public sealed partial class CitySimulation
{
    readonly record struct Pr57355Mark(
        long Slice, int WinnerId, long DistanceBits, long StepBits,
        Point Position, long HeadingBits, int RouteIndex,
        Point Entry, double Time);
    readonly Dictionary<int,Pr57355Mark> _pr57355Last = new();
    readonly HashSet<int> _pr57355Seen = new();
    long _pr57355Window, _pr57355Slice;
    long _pr57355Eligible, _pr57355Compared, _pr57355WinnerSame,
         _pr57355DistanceSame, _pr57355StepSame, _pr57355ReaderSame,
         _pr57355AllSame, _pr57355ClockChanged, _pr57355EntryChanged;

    void Pr57355BeginSlice()
    {
        if (!_pr57353Active) return;
        _pr57355Window = _pr57353Window;
        _pr57355Slice = -1;
        _pr57355Last.Clear();
        _pr57355Seen.Clear();
        _pr57355Eligible = _pr57355Compared = _pr57355WinnerSame = 0;
        _pr57355DistanceSame = _pr57355StepSame = _pr57355ReaderSame = 0;
        _pr57355AllSame = _pr57355ClockChanged = _pr57355EntryChanged = 0;
    }

    void Pr57355Read(Vehicle reader, Vehicle? winner, double distance,
        double step, bool entering)
    {
        if (!_pr57353Active || !_pr57ExclusiveActive || entering ||
            winner is null || winner.Status == VehicleStatus.Stranded ||
            distance > 8 || step > .00001) return;
        var slice = _pr57ExclusiveEligibleSlices;
        if (_pr57355Slice != slice)
        {
            _pr57355Slice = slice;
            _pr57355Seen.Clear();
        }
        if (!_pr57355Seen.Add(reader.Id)) return;
        _pr57355Eligible++;
        var now = new Pr57355Mark(slice, winner.Id,
            BitConverter.DoubleToInt64Bits(distance),
            BitConverter.DoubleToInt64Bits(step), reader.Position,
            BitConverter.DoubleToInt64Bits(reader.Heading),
            reader.RouteIndex, State.Entry, VehicleMovementTime);
        if (_pr57355Last.TryGetValue(reader.Id, out var old) &&
            old.Slice + 1 == slice)
        {
            _pr57355Compared++;
            var winnerSame = now.WinnerId == old.WinnerId;
            var distanceSame = now.DistanceBits == old.DistanceBits;
            var stepSame = now.StepBits == old.StepBits;
            var readerSame = now.Position == old.Position &&
                now.HeadingBits == old.HeadingBits &&
                now.RouteIndex == old.RouteIndex;
            if (winnerSame) _pr57355WinnerSame++;
            if (distanceSame) _pr57355DistanceSame++;
            if (stepSame) _pr57355StepSame++;
            if (readerSame) _pr57355ReaderSame++;
            if (winnerSame && distanceSame && stepSame && readerSame)
                _pr57355AllSame++;
            if (now.Time != old.Time) _pr57355ClockChanged++;
            if (now.Entry != old.Entry) _pr57355EntryChanged++;
        }
        _pr57355Last[reader.Id] = now;
    }

    void Pr57355EndSlice()
    {
        if (!_pr57353Active ||
            _pr57ExclusiveEligibleSlices != _pr57355Window + 2) return;
        Console.WriteLine(
            $"PR57_355_OUTPUT window={_pr57355Window} " +
            $"eligible={_pr57355Eligible} compared={_pr57355Compared} " +
            $"winnerSame={_pr57355WinnerSame} " +
            $"distanceBitsSame={_pr57355DistanceSame} " +
            $"stepBitsSame={_pr57355StepSame} " +
            $"readerSame={_pr57355ReaderSame} allSame={_pr57355AllSame} " +
            $"clockChanged={_pr57355ClockChanged} " +
            $"entryChanged={_pr57355EntryChanged} " +
            "scope=20_SELECTED_QUEUE_READERS noSkippedSlices=True " +
            "canonicalScanAlwaysRuns=True observational=True intrusive=True " +
            "notCausalClosure=True");
        _pr57355Last.Clear();
        _pr57355Seen.Clear();
    }
}
''')
print("PR57_355_INJECTED lane output witness; no decisions replaced")
