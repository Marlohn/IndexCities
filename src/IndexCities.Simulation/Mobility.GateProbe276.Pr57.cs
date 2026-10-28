namespace IndexCities.Simulation;

// PR57 #276: strictly observational FIRST-EXIT census on canonical admission
// and bay hold. The same predicates execute in the same order, with no cache,
// skipping, physics writes, or persisted diagnostics. Disabled by default.
public sealed partial class CitySimulation
{
    static readonly bool Pr57276Enabled =
        Environment.GetEnvironmentVariable("PR57_276_GATES") == "1";
    static readonly string[] Pr57276Names =
    [
        "admit-dock-way", "admit-space", "admit-traffic",
        "admit-body", "admit-true", "bay-no-dock",
        "bay-already-admitted", "bay-no-need", "bay-crew-blocked",
        "bay-occupied", "bay-admit-denied", "bay-admit-true",
        "bay-held-return"
    ];
    readonly long[,,] _pr57276Counts = new long[2, 3, 13];

    void Pr57276Gate(Vehicle v, int gate)
    {
        if (!Pr57276Enabled || !_pr57273Sampling) return;
        var slot = v.Id == 1453 ? 0 : v.Id == 1477 ? 1
            : v.Id == 23 ? 2 : -1;
        if (slot >= 0) _pr57276Counts[_pr57273Window, slot, gate]++;
    }

    void Pr57276Report()
    {
        if (!Pr57276Enabled) return;
        for (var w = 0; w < 2; w++)
        for (var s = 0; s < 3; s++)
        for (var g = 0; g < Pr57276Names.Length; g++)
        {
            var count = _pr57276Counts[w, s, g];
            if (count == 0) continue;
            Console.WriteLine(
                $"PR57_276_GATE window={w} id={Pr57273Ids[s]} " +
                $"gate={Pr57276Names[g]} calls={count} " +
                "observational=True firstExit=True noSkip=True");
        }
    }
}
