namespace IndexCities.Simulation;

// PR57 #304: bounded identity trace for the #303 authentic REAL 14h
// pedestrian and onward-body denials. Opt-in, observational, intrusive.
// The selected body is the first returned by BodyOnWay, NOT a complete
// dependency set. Pedestrian IDs are the matching set at the actual return,
// NOT a substitute for the canonical Any short-circuit.
public sealed partial class CitySimulation
{
    static readonly bool Pr57304Enabled =
        Environment.GetEnvironmentVariable("PR57_304_BLOCKERS") == "1";

    bool Pr57304Target(Vehicle v) =>
        Pr57304Enabled && _pr57300Calls is >= 76 and <= 100 &&
        v.Id is 1243 or 1282 or 1071 or 1380;

    void Pr57304Body(Vehicle reader, Vehicle blocker, string kind)
    {
        if (!Pr57304Target(reader) || kind != "onwardBody") return;
        Console.WriteLine(
            $"PR57_304_BODY call={_pr57300Calls} slice={_pr57302CurrentSlice} " +
            $"id={reader.Id} blocker={blocker.Id} status={blocker.Status} " +
            $"x={blocker.Position.X:F6} z={blocker.Position.Z:F6} " +
            $"heading={blocker.Heading:F9} routeIndex={blocker.RouteIndex} " +
            $"claim={blocker.IntersectionClaim?.ToString() ?? "none"} " +
            "scope=ACTUAL_SELECTED_BODY_NOT_ALL_CANDIDATES");
    }

    void Pr57304Walk(Vehicle reader, RoadNode junction)
    {
        if (!Pr57304Target(reader) || junction.Id is not (346 or 466)) return;
        var ids = new List<int>();
        foreach (var c in State.Citizens)
            if (c.Alive && c.VehicleId == null && c.Moving &&
                c.CrossingNodeId == junction.Id &&
                c.Position.Distance(junction.Position) <
                    State.Tuning.RoadWidth / 2 + 1.5)
                ids.Add(c.Id);
        Console.WriteLine(
            $"PR57_304_WALK call={_pr57300Calls} slice={_pr57302CurrentSlice} " +
            $"id={reader.Id} node={junction.Id} count={ids.Count} " +
            $"first={(ids.Count == 0 ? -1 : ids[0])} ids={string.Join(",", ids)} " +
            "scope=RETURN_TIME_MATCHES_NOT_CANONICAL_ANY_TRACE");
    }
}
