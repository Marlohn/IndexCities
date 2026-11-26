namespace IndexCities.Simulation;

// PR57 #203: strictly observational, bounded pair-geometry reuse census.
// An equal pose is NOT proof of reusable HoldFor decisions: road, dock,
// reservation, signals, pedestrian, tow, route and transient writers are
// intentionally not represented. No physics reads these dictionaries.
public sealed partial class CitySimulation
{
    static readonly bool Pr57PairGeometryProbeEnabled =
        Environment.GetEnvironmentVariable("PR57_203_PAIR_GEOMETRY") == "1";

    readonly record struct Pr57PairPose(Point A, double HeadingA, Point B, double HeadingB);
    readonly record struct Pr57DirectedPairPose(Pr57PairPose Pose, bool EnteringLane);
    readonly record struct Pr57UndirectedPairPose(Pr57PairPose Pose, bool Forward);

    readonly Dictionary<(int A, int B), Pr57DirectedPairPose> _pr57PairDirected = new();
    readonly Dictionary<(int A, int B), Pr57UndirectedPairPose> _pr57PairUndirected = new();
    long _pr57PairEligibleSlices, _pr57PairSamples, _pr57PairVisits,
         _pr57PairSelf, _pr57PairDirectedRepeats, _pr57PairDirectedSameGeometry,
         _pr57PairDirectedSameInputs, _pr57PairUndirectedRepeats,
         _pr57PairUndirectedSameGeometry, _pr57PairReverseDirection;
    bool _pr57PairActive;

    void Pr57PairBeginSlice(double dt, double? operationsDt, bool profiled)
    {
        _pr57PairActive = false;
        if (!Pr57PairGeometryProbeEnabled || !profiled || dt <= 0 || operationsDt is not null)
            return;
        _pr57PairEligibleSlices++;
        // Three independent physical sub-slices, never all scans in a long replay.
        if (_pr57PairSamples >= 3 || _pr57PairEligibleSlices % 37 != 0) return;
        _pr57PairSamples++;
        _pr57PairActive = true;
        _pr57PairDirected.Clear();
        _pr57PairUndirected.Clear();
        _pr57PairVisits = _pr57PairSelf = _pr57PairDirectedRepeats = 0;
        _pr57PairDirectedSameGeometry = _pr57PairDirectedSameInputs = 0;
        _pr57PairUndirectedRepeats = _pr57PairUndirectedSameGeometry = 0;
        _pr57PairReverseDirection = 0;
    }

    void Pr57PairBeforeLaneCandidate(Vehicle actor, Vehicle other, bool enteringLane)
    {
        if (!_pr57PairActive) return;
        _pr57PairVisits++;
        if (actor.Id == other.Id) { _pr57PairSelf++; return; }
        var directKey = (actor.Id, other.Id);
        var directPose = new Pr57PairPose(actor.Position, actor.Heading, other.Position, other.Heading);
        if (_pr57PairDirected.TryGetValue(directKey, out var priorDirected))
        {
            _pr57PairDirectedRepeats++;
            if (priorDirected.Pose == directPose)
            {
                _pr57PairDirectedSameGeometry++;
                if (priorDirected.EnteringLane == enteringLane) _pr57PairDirectedSameInputs++;
            }
        }
        _pr57PairDirected[directKey] = new Pr57DirectedPairPose(directPose, enteringLane);

        var forward = actor.Id < other.Id;
        var undirectedKey = forward ? (actor.Id, other.Id) : (other.Id, actor.Id);
        var canonicalPose = forward ? directPose :
            new Pr57PairPose(other.Position, other.Heading, actor.Position, actor.Heading);
        if (_pr57PairUndirected.TryGetValue(undirectedKey, out var priorUndirected))
        {
            _pr57PairUndirectedRepeats++;
            if (priorUndirected.Pose == canonicalPose) _pr57PairUndirectedSameGeometry++;
            if (priorUndirected.Forward != forward) _pr57PairReverseDirection++;
        }
        _pr57PairUndirected[undirectedKey] = new Pr57UndirectedPairPose(canonicalPose, forward);
    }

    void Pr57PairEndSlice()
    {
        if (!_pr57PairActive) return;
        _pr57PairActive = false;
        Console.WriteLine(
            $"PR57_203_PAIR sample={_pr57PairSamples} eligibleSlice={_pr57PairEligibleSlices} " +
            $"visits={_pr57PairVisits} self={_pr57PairSelf} " +
            $"distinctDirected={_pr57PairDirected.Count} directedRepeats={_pr57PairDirectedRepeats} " +
            $"sameDirectedGeometry={_pr57PairDirectedSameGeometry} " +
            $"sameDirectedGeometryAndEntry={_pr57PairDirectedSameInputs} " +
            $"distinctUndirected={_pr57PairUndirected.Count} undirectedRepeats={_pr57PairUndirectedRepeats} " +
            $"sameUndirectedGeometry={_pr57PairUndirectedSameGeometry} " +
            $"reverseDirection={_pr57PairReverseDirection} " +
            "scope=lane-candidate-only observational=True causalReuseProven=False");
        _pr57PairDirected.Clear();
        _pr57PairUndirected.Clear();
    }
}
