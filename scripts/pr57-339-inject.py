#!/usr/bin/env python3
"""#339: opt-in, checkout-only tracing of *actual* junction return transitions.

Does not change canonical decisions, actors, scheduling, persistence or source in git.
Requires PR57_246_JUNCTION_RETURNS=1, PR57_VEHICLE_COST_PROBE=1 and
PR57_339_REASON_TRANSITIONS=1 during the authentic Advance(2) replay.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
sim = root / 'src/IndexCities.Simulation'

def replace_one(path, old, new):
    source = path.read_text(encoding='utf-8')
    if source.count(old) != 1:
        raise RuntimeError(f'{path}: expected one anchor, found {source.count(old)}')
    path.write_text(source.replace(old, new, 1), encoding='utf-8')

returns = sim / 'Mobility.JunctionReturns.Pr57.cs'
replace_one(returns,
    '        Pr57Junction247Decision(reader,junction,reason);',
    '        Pr57339ReasonTransition(reader,junction,reason,allowed);\n'
    '        Pr57Junction247Decision(reader,junction,reason);')
replace_one(returns,
    '        Pr57Junction247Report(slices);',
    '        Pr57Junction247Report(slices);\n'
    '        Pr57339Summary(slices);')
replace_one(sim / 'Mobility.JunctionBlocker247.Pr57.cs',
    '        Pr57304Body(reader,other,kind);',
    '        Pr57304Body(reader,other,kind);\n'
    '        Pr57339SelectedBlocker(reader,other,kind);')

new_file = sim / 'Mobility.JunctionTransitions339.Pr57.cs'
if new_file.exists():
    raise RuntimeError('Refusing to overwrite existing #339 partial')
new_file.write_text('''namespace IndexCities.Simulation;

// #339: actual-return transition trace, opt-in and observational. A body
// recorded here is only the selected blocker; negative candidates and the
// complete causal read set are NOT recorded. Pedestrian IDs are a post-return
// read-only snapshot, not the original short-circuit trace. No reuse or skip.
public sealed partial class CitySimulation
{
    static readonly bool Pr57339Enabled =
        Environment.GetEnvironmentVariable("PR57_339_REASON_TRANSITIONS") == "1";

    readonly record struct Pr57339Body(
        int Id, double X, double Z, double Heading, VehicleStatus Status,
        int RouteIndex, int? Claim, TowPhase TowPhase);
    readonly Dictionary<int, Pr57339Body> _pr57339Pending = new();
    readonly Dictionary<(int Reader, int Junction), Pr57339Body> _pr57339LastBody = new();
    long _pr57339Transitions, _pr57339DecisionChanges, _pr57339MissingBody;

    void Pr57339SelectedBlocker(Vehicle reader, Vehicle other, string kind)
    {
        if (!Pr57339Enabled || kind is not ("bendBody" or "onwardBody")) return;
        _pr57339Pending[reader.Id] = new Pr57339Body(
            other.Id, other.Position.X, other.Position.Z, other.Heading,
            other.Status, other.RouteIndex, other.IntersectionClaim, other.TowPhase);
    }

    static string Pr57339BodyText(Pr57339Body body) =>
        $"{body.Id}@{body.X:F3},{body.Z:F3},h={body.Heading:F5},status={body.Status},route={body.RouteIndex},claim={body.Claim?.ToString() ?? "none"},tow={body.TowPhase}";

    void Pr57339ReasonTransition(Vehicle reader, RoadNode junction,
        Pr57JunctionExit reason, bool allowed)
    {
        if (!Pr57339Enabled) return;
        var key = (reader.Id, junction.Id);
        var isBody = reason is Pr57JunctionExit.BendBody or Pr57JunctionExit.OnwardBody;
        var hasBody = _pr57339Pending.Remove(reader.Id, out var body);
        if (isBody && !hasBody) _pr57339MissingBody++;
        if (_pr57JunctionPrevious.TryGetValue(key, out var previous) &&
            (previous.Reason != reason || previous.Allowed != allowed))
        {
            _pr57339Transitions++;
            if (previous.Allowed != allowed) _pr57339DecisionChanges++;
            var oldBody = _pr57339LastBody.TryGetValue(key, out var last)
                ? Pr57339BodyText(last) : "none";
            var newBody = isBody && hasBody ? Pr57339BodyText(body) : "none";
            // Post-return state; never called as a replacement for the gate.
            var walkers = State.Citizens.Where(c => c.Alive && c.VehicleId == null &&
                c.Moving && c.CrossingNodeId == junction.Id &&
                c.Position.Distance(junction.Position) < State.Tuning.RoadWidth / 2 + 1.5)
                .Select(c => c.Id).OrderBy(id => id).ToArray();
            Console.WriteLine(
                $"PR57_339_TRANSITION reader={reader.Id} junction={junction.Id} " +
                $"old={previous.Reason} new={reason} oldAllowed={(previous.Allowed ? 1 : 0)} " +
                $"newAllowed={(allowed ? 1 : 0)} clock={VehicleMovementTime:F9} " +
                $"now={State.Now:O} readerX={reader.Position.X:F3} readerZ={reader.Position.Z:F3} " +
                $"readerHeading={reader.Heading:F5} readerRoute={reader.RouteIndex} " +
                $"oldBody={oldBody} newBody={newBody} " +
                $"pedestrianCount={walkers.Length} pedestrianIds={string.Join(",", walkers)} " +
                "scope=ACTUAL_RETURN_SELECTED_BODY_ONLY_POST_RETURN_WALKERS_NOT_CAUSAL_CLOSURE");
        }
        if (isBody && hasBody) _pr57339LastBody[key] = body;
        else _pr57339LastBody.Remove(key);
    }

    void Pr57339Summary(long slices)
    {
        if (!Pr57339Enabled) return;
        Console.WriteLine(
            $"PR57_339_SUMMARY slices={slices} transitions={_pr57339Transitions} " +
            $"decisionChanges={_pr57339DecisionChanges} missingSelectedBody={_pr57339MissingBody} " +
            "scope=ACTUAL_RETURNS_NO_REUSE_NO_SKIP_INTRUSIVE");
    }
}
''', encoding='utf-8')
print('PR57_339_INJECTED: return transitions + selected body hooks; observational only')
