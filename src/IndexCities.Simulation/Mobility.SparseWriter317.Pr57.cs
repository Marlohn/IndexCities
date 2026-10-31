namespace IndexCities.Simulation;

// PR57 #317: bounded, opt-in mutation-site publication of selected citizen
// position/crossing writers. Every setter still writes canonical state immediately.
// This is NOT a complete journal: other phases, CitizenArrived, changes to
// Moving/VehicleId, external edits and intermediate nonlocal writers are omitted.
// No decision is reused, no physical slice is skipped, and no data enters saves.
public sealed partial class CitySimulation
{
    static readonly bool Pr57317Enabled =
        Environment.GetEnvironmentVariable("PR57_317_SPARSE_WRITERS") == "1";

    readonly record struct Pr57317CitizenRow(
        long X, long Z, int? Crossing, int? Vehicle, bool Moving, bool Alive);

    int _pr57317Call;
    bool _pr57317Active;
    long _pr57317Changed, _pr57317Noop, _pr57317Position, _pr57317Crossing;
    long _pr57317IndividualGateDeltas, _pr57317Ticks;
    long _pr57317Walk, _pr57317Aboard, _pr57317Acquire, _pr57317Expire, _pr57317Arrive;

    static Pr57317CitizenRow Pr57317Capture(Citizen c) => new(
        BitConverter.DoubleToInt64Bits(c.Position.X),
        BitConverter.DoubleToInt64Bits(c.Position.Z),
        c.CrossingNodeId, c.VehicleId, c.Moving, c.Alive);

    bool Pr57317Gate(Pr57317CitizenRow c) =>
        c.Alive && c.Vehicle is null && c.Moving &&
        c.Crossing is int id && _roads.TryGetValue(id, out var road) &&
        new Point(BitConverter.Int64BitsToDouble(c.X),
                  BitConverter.Int64BitsToDouble(c.Z)).Distance(road.Position)
            < State.Tuning.RoadWidth / 2 + 1.5;

    void Pr57317Begin()
    {
        if (!Pr57317Enabled) return;
        _pr57317Call++;
        var start = int.TryParse(Environment.GetEnvironmentVariable("PR57_317_START"),
            out var n) ? n : 76;
        _pr57317Active = _pr57317Call >= start;
        if (!_pr57317Active) return;
        _pr57317Changed = _pr57317Noop = _pr57317Position = _pr57317Crossing = 0;
        _pr57317IndividualGateDeltas = _pr57317Ticks = 0;
        _pr57317Walk = _pr57317Aboard = _pr57317Acquire =
            _pr57317Expire = _pr57317Arrive = 0;
    }

    void Pr57317After(Citizen c, Pr57317CitizenRow before, string site)
    {
        var after = Pr57317Capture(c);
        if (before == after) { _pr57317Noop++; return; }
        _pr57317Changed++;
        if (before.X != after.X || before.Z != after.Z) _pr57317Position++;
        if (before.Crossing != after.Crossing) _pr57317Crossing++;
        var beforeGate = Pr57317Gate(before);
        var afterGate = Pr57317Gate(after);
        if (beforeGate && (!afterGate || before.Crossing != after.Crossing))
            _pr57317IndividualGateDeltas++;
        if (afterGate && (!beforeGate || before.Crossing != after.Crossing))
            _pr57317IndividualGateDeltas++;
        switch (site)
        {
            case "walk": _pr57317Walk++; break;
            case "aboard": _pr57317Aboard++; break;
            case "acquire": _pr57317Acquire++; break;
            case "expire": _pr57317Expire++; break;
            case "arrive": _pr57317Arrive++; break;
        }
    }

    void Pr57317SetPosition(Citizen c, Point next, string site)
    {
        if (!_pr57317Active) { c.Position = next; return; }
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var before = Pr57317Capture(c);
        c.Position = next;
        Pr57317After(c, before, site);
        _pr57317Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57317SetCrossing(Citizen c, int? next, string site)
    {
        if (!_pr57317Active) { c.CrossingNodeId = next; return; }
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var before = Pr57317Capture(c);
        c.CrossingNodeId = next;
        Pr57317After(c, before, site);
        _pr57317Ticks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57317End()
    {
        if (!_pr57317Active) return;
        var ms = _pr57317Ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_317_WRITERS call={_pr57317Call} changed={_pr57317Changed} " +
            $"noops={_pr57317Noop} position={_pr57317Position} " +
            $"crossing={_pr57317Crossing} individualGateDeltas={_pr57317IndividualGateDeltas} " +
            $"walk={_pr57317Walk} aboard={_pr57317Aboard} " +
            $"acquire={_pr57317Acquire} expire={_pr57317Expire} arrive={_pr57317Arrive} " +
            $"writerScopeMs={ms:F6} " +
            "scope=SELECTED_CANONICAL_SETTERS_NOT_COMPLETE_CAUSAL_JOURNAL");
        _pr57317Active = false;
    }
}
