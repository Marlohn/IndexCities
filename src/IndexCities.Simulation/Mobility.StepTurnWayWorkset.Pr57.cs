namespace IndexCities.Simulation;

// PR57 #541: an opt-in first cut of an exact-input physical workset.
// Only TurnWay's own immutable pose geometry is memoized; BodyOnWay,
// preference, signals, pedestrian gates, reservations and all writers remain
// live and ordered. Nothing crosses a Step/Advance boundary or enters a save.
public sealed partial class CitySimulation
{
    static readonly bool Pr57StepTurnWayWorksetEnabled =
        Environment.GetEnvironmentVariable("PR57_STEP_TURNWAY_WORKSET") == "1";
    static readonly bool Pr57StepTurnWayVerify =
        Environment.GetEnvironmentVariable("PR57_STEP_TURNWAY_VERIFY") == "1";
    static readonly bool Pr57StepTurnWayDiag =
        Environment.GetEnvironmentVariable("PR57_STEP_TURNWAY_DIAG") == "1";

    sealed class Pr57TurnWayEntry
    {
        public Point Position;
        public double Heading, HalfLength, HalfWidth;
        public int RouteIndex, RoadEpoch;
        public int[] Route = [];
        public (Vehicle Body, Point Position, double Heading)[] Poses = [];
        public RoadNode[] Spaces = [];
    }

    readonly Dictionary<Vehicle, Pr57TurnWayEntry> _pr57TurnWayWorkset =
        new(ReferenceEqualityComparer.Instance);
    bool _pr57TurnWayActive;
    long _pr57TurnWaySteps, _pr57TurnWayHits, _pr57TurnWayMisses, _pr57TurnWayInvalidations;

    void Pr57TurnWayWorksetBegin()
    {
        _pr57TurnWayWorkset.Clear();
        _pr57TurnWayActive = Pr57StepTurnWayWorksetEnabled && CostProfile is null;
        if(_pr57TurnWayActive)_pr57TurnWaySteps++;
    }

    void Pr57TurnWayWorksetEnd()
    {
        _pr57TurnWayActive = false;
        _pr57TurnWayWorkset.Clear();
        if(Pr57StepTurnWayDiag && Pr57StepTurnWayWorksetEnabled && _pr57TurnWaySteps > 0
           && _pr57TurnWaySteps % 100 == 0)
            Console.WriteLine($"PR57_541_TURNWAY steps={_pr57TurnWaySteps} hits={_pr57TurnWayHits} misses={_pr57TurnWayMisses} invalidations={_pr57TurnWayInvalidations}");
    }

    bool Pr57TurnWayWorksetTryGet(Vehicle vehicle,
        out List<(Vehicle Body,Point Position,double Heading)> poses,
        out List<RoadNode> spaces)
    {
        poses = null!;
        spaces = null!;
        if(!_pr57TurnWayActive)return false;
        if(!_pr57TurnWayWorkset.TryGetValue(vehicle,out var entry))
        {
            _pr57TurnWayMisses++;
            return false;
        }
        // Compare all inputs to TurnWay's pure geometric construction, not
        // only an ID, Count, time or a vehicle's stationary status. Route can
        // mutate in place; road edits change _roadEpoch. Other vehicles are
        // deliberately NOT cached: the live BodyOnWay test always reruns.
        if(!entry.Position.Equals(vehicle.Position) || entry.Heading != vehicle.Heading
           || entry.RouteIndex != vehicle.RouteIndex || entry.RoadEpoch != _roadEpoch
           || entry.HalfLength != BodyHalfLength(vehicle)
           || entry.HalfWidth != BodyHalfWidth(vehicle)
           || !vehicle.Route.SequenceEqual(entry.Route))
        {
            _pr57TurnWayInvalidations++;
            _pr57TurnWayMisses++;
            return false;
        }
        if(Pr57StepTurnWayVerify)
        {
            var fresh = Pr57TurnWayFresh(vehicle,out var freshSpaces);
            if(!fresh.SequenceEqual(entry.Poses)
               || !freshSpaces.Select(x=>x.Id).SequenceEqual(entry.Spaces.Select(x=>x.Id)))
                throw new InvalidOperationException("PR57_541_TURNWAY_CAUSAL_SHADOW_MISMATCH");
        }
        _pr57TurnWayHits++;
        // New lists preserve the old call's ownership and mutation semantics;
        // only the expensive geometric construction is shared.
        poses = new List<(Vehicle Body,Point Position,double Heading)>(entry.Poses);
        spaces = new List<RoadNode>(entry.Spaces);
        return true;
    }

    void Pr57TurnWayWorksetStore(Vehicle vehicle,
        List<(Vehicle Body,Point Position,double Heading)> poses,
        List<RoadNode> spaces)
    {
        if(!_pr57TurnWayActive)return;
        _pr57TurnWayWorkset[vehicle] = new Pr57TurnWayEntry
        {
            Position = vehicle.Position,
            Heading = vehicle.Heading,
            RouteIndex = vehicle.RouteIndex,
            RoadEpoch = _roadEpoch,
            HalfLength = BodyHalfLength(vehicle),
            HalfWidth = BodyHalfWidth(vehicle),
            Route = vehicle.Route.ToArray(),
            Poses = poses.ToArray(),
            Spaces = spaces.ToArray()
        };
    }
}
