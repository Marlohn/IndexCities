namespace IndexCities.Simulation;

// PR57 #320: opt-in observation at selected canonical Path/PathIndex writes.
// This is deliberately NOT a complete causal journal. It covers only sites
// routed through these wrappers and does not replace any physical decision.
public sealed partial class CitySimulation
{
    static readonly bool Pr57320Enabled =
        Environment.GetEnvironmentVariable("PR57_320_PATH_WRITERS") == "1";

    long _pr57320Replace, _pr57320Clear, _pr57320Reset;
    long _pr57320MovingDeltas, _pr57320GateDeltas;

    bool Pr57320Active => Pr57320Enabled && Pr57319Enabled && _pr57317Active;

    void Pr57320Begin()
    {
        if (!Pr57320Active) return;
        _pr57320Replace = _pr57320Clear = _pr57320Reset = 0;
        _pr57320MovingDeltas = _pr57320GateDeltas = 0;
    }

    void Pr57320After(Citizen c, Pr57317CitizenRow before, string site)
    {
        var after = Pr57317Capture(c);
        var oldGate = Pr57317Gate(before);
        var newGate = Pr57317Gate(after);
        if (before.Moving != after.Moving) _pr57320MovingDeltas++;
        if (oldGate != newGate || oldGate && before.Crossing != after.Crossing)
        {
            _pr57320GateDeltas += oldGate && newGate && before.Crossing != after.Crossing ? 2 : 1;
            Console.WriteLine(
                $"PR57_320_PATH call={_pr57317Call} citizen={c.Id} site={site} " +
                $"beforeMoving={before.Moving} afterMoving={after.Moving} " +
                $"beforeGate={oldGate} afterGate={newGate} " +
                $"beforeCrossing={before.Crossing?.ToString() ?? "null"} " +
                $"afterCrossing={after.Crossing?.ToString() ?? "null"} " +
                "scope=SELECTED_PATH_WRITERS");
        }
        Pr57317After(c, before, site);
    }

    void Pr57320ReplacePath(Citizen c, List<Point> next, string site)
    {
        if (!Pr57320Active) { c.Path = next; return; }
        var before = Pr57317Capture(c);
        c.Path = next;
        _pr57320Replace++;
        Pr57320After(c, before, site);
    }

    void Pr57320ClearPath(Citizen c, string site)
    {
        if (!Pr57320Active) { c.Path.Clear(); return; }
        var before = Pr57317Capture(c);
        c.Path.Clear();
        _pr57320Clear++;
        Pr57320After(c, before, site);
    }

    void Pr57320ResetIndex(Citizen c, string site)
    {
        if (!Pr57320Active) { c.PathIndex = 0; return; }
        var before = Pr57317Capture(c);
        c.PathIndex = 0;
        _pr57320Reset++;
        Pr57320After(c, before, site);
    }

    void Pr57320End()
    {
        if (!Pr57320Active) return;
        Console.WriteLine(
            $"PR57_320_SUMMARY call={_pr57317Call} replace={_pr57320Replace} " +
            $"clear={_pr57320Clear} reset={_pr57320Reset} " +
            $"movingDeltas={_pr57320MovingDeltas} gateDeltas={_pr57320GateDeltas} " +
            "scope=SELECTED_PATH_WRITERS_NOT_COMPLETE_CAUSAL_JOURNAL");
    }
}
