namespace IndexCities.Simulation;

// PR57 #319: strictly observational, opt-in publication at the PathIndex++
// writer. Moving is derived from PathIndex < Path.Count, so a subsequent
// CrossingNodeId setter is too late to detect its causal gate transition.
// Does NOT cover Path replacement/clear/reset, other phases or nonlocal writers.
public sealed partial class CitySimulation
{
    static readonly bool Pr57319Enabled =
        Environment.GetEnvironmentVariable("PR57_319_PATH_INDEX") == "1";

    long _pr57319Increments, _pr57319GateDeltas;

    void Pr57319Begin()
    {
        if (!Pr57319Enabled || !_pr57317Active) return;
        _pr57319Increments = _pr57319GateDeltas = 0;
    }

    void Pr57319AdvancePathIndex(Citizen c)
    {
        if (!Pr57319Enabled || !_pr57317Active)
        {
            c.PathIndex++;
            return;
        }

        var before = Pr57317Capture(c);
        var beforeGate = Pr57317Gate(before);
        c.PathIndex++; // The original canonical mutation, at the original point.
        var after = Pr57317Capture(c);
        var afterGate = Pr57317Gate(after);
        _pr57319Increments++;
        if (beforeGate != afterGate)
        {
            _pr57319GateDeltas++;
            Console.WriteLine(
                $"PR57_319_PATH call={_pr57317Call} citizen={c.Id} " +
                $"node={before.Crossing?.ToString() ?? "null"} " +
                $"beforeMoving={before.Moving} afterMoving={after.Moving} " +
                $"beforeGate={beforeGate} afterGate={afterGate} " +
                "scope=ONLY_PATHINDEX_INCREMENT");
        }
        Pr57317After(c, before, "pathIndex");
    }

    void Pr57319End()
    {
        if (!Pr57319Enabled || !_pr57317Active) return;
        Console.WriteLine(
            $"PR57_319_SUMMARY call={_pr57317Call} increments={_pr57319Increments} " +
            $"gateDeltas={_pr57319GateDeltas} scope=ONLY_PATHINDEX_INCREMENT");
    }
}
