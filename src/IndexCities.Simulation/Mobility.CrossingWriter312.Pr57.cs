namespace IndexCities.Simulation;

// PR57 #312: actor-boundary observation of the *actual* pedestrian junction
// predicate. The canonical movement and admission predicates are untouched.
// This is a bounded necessary-input census, NOT a complete mutation journal:
// intermediate writes within one citizen's movement can cancel before this hook.
public sealed partial class CitySimulation
{
    static readonly bool Pr57312Enabled =
        Environment.GetEnvironmentVariable("PR57_312_CROSSING_WRITER") == "1";

    readonly record struct Pr57312Citizen(
        int? Crossing, int? Vehicle, bool Alive, bool Moving, bool Gate,
        long X, long Z);

    int _pr57312Call;
    bool _pr57312Active;
    int _pr57312Pose, _pr57312Crossing, _pr57312Moving, _pr57312Vehicle;
    int _pr57312GateFlips, _pr57312SameNodeFlips, _pr57312PositionOnlyFlips;

    Pr57312Citizen Pr57312Capture(Citizen c)
    {
        if (!_pr57312Active) return default;
        var gate = c.Alive && c.VehicleId is null && c.Moving &&
            c.CrossingNodeId is int nodeId &&
            _roads.TryGetValue(nodeId, out var node) &&
            c.Position.Distance(node.Position) < State.Tuning.RoadWidth / 2 + 1.5;
        return new Pr57312Citizen(
            c.CrossingNodeId, c.VehicleId, c.Alive, c.Moving, gate,
            BitConverter.DoubleToInt64Bits(c.Position.X),
            BitConverter.DoubleToInt64Bits(c.Position.Z));
    }

    void Pr57312Begin()
    {
        if (!Pr57312Enabled) return;
        _pr57312Call++;
        var start = int.TryParse(Environment.GetEnvironmentVariable("PR57_312_START"),
            out var n) ? n : 76;
        _pr57312Active = _pr57312Call >= start;
        _pr57312Pose = _pr57312Crossing = _pr57312Moving = _pr57312Vehicle = 0;
        _pr57312GateFlips = _pr57312SameNodeFlips = _pr57312PositionOnlyFlips = 0;
    }

    void Pr57312After(Citizen c, Pr57312Citizen before)
    {
        if (!_pr57312Active) return;
        var after = Pr57312Capture(c);
        var poseChanged = before.X != after.X || before.Z != after.Z;
        if (poseChanged) _pr57312Pose++;
        if (before.Crossing != after.Crossing) _pr57312Crossing++;
        if (before.Moving != after.Moving) _pr57312Moving++;
        if (before.Vehicle != after.Vehicle) _pr57312Vehicle++;
        if (before.Gate == after.Gate) return;

        _pr57312GateFlips++;
        var sameNode = before.Crossing == after.Crossing;
        if (sameNode) _pr57312SameNodeFlips++;
        var positionOnly = sameNode && before.Vehicle == after.Vehicle &&
            before.Alive == after.Alive && before.Moving == after.Moving &&
            poseChanged;
        if (positionOnly) _pr57312PositionOnlyFlips++;
        Console.WriteLine(
            $"PR57_312_GATE call={_pr57312Call} citizen={c.Id} " +
            $"beforeNode={before.Crossing?.ToString() ?? "null"} " +
            $"afterNode={after.Crossing?.ToString() ?? "null"} " +
            $"before={before.Gate} after={after.Gate} sameNode={sameNode} " +
            $"positionOnly={positionOnly} beforeMoving={before.Moving} " +
            $"afterMoving={after.Moving} beforeVehicle={before.Vehicle?.ToString() ?? "null"} " +
            $"afterVehicle={after.Vehicle?.ToString() ?? "null"}");
    }

    void Pr57312End()
    {
        if (!_pr57312Active) return;
        Console.WriteLine(
            $"PR57_312_SUM call={_pr57312Call} pose={_pr57312Pose} " +
            $"crossing={_pr57312Crossing} moving={_pr57312Moving} " +
            $"vehicle={_pr57312Vehicle} gateFlips={_pr57312GateFlips} " +
            $"sameNodeGateFlips={_pr57312SameNodeFlips} " +
            $"positionOnlyGateFlips={_pr57312PositionOnlyFlips} " +
            "scope=NET_ACTOR_BOUNDARY_NOT_COMPLETE_CAUSAL_JOURNAL");
        _pr57312Active = false;
    }
}
