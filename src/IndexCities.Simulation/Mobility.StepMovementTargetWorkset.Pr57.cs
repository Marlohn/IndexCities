namespace IndexCities.Simulation;

// PR57 #542: Step-scoped route-derived movement geometry shared by the
// event predictor, entry evaluation and actual physical mover. This is a
// deliberately bounded execution POC, not a persisted route cache.
// It never caches a physical admission, blocker, clock, vehicle position,
// fuel, waiting status, collision, pedestrian or reservation decision.
public sealed partial class CitySimulation
{
    static readonly bool Pr57MovementTargetEnabled =
        Environment.GetEnvironmentVariable("PR57_STEP_MOVEMENT_TARGET_WORKSET") == "1";
    static readonly bool Pr57MovementTargetVerify =
        Environment.GetEnvironmentVariable("PR57_STEP_MOVEMENT_TARGET_VERIFY") == "1";
    static readonly bool Pr57MovementTargetDiag =
        Environment.GetEnvironmentVariable("PR57_STEP_MOVEMENT_TARGET_DIAG") == "1";

    sealed class Pr57MovementTargetEntry
    {
        public int RouteIndex, RouteCount, RoadEpoch, CurrentId, PreviousId, NextId;
        public RoadNode Current = null!;
        public RoadNode? Previous, Next;
        public Point CurrentPosition, PreviousPosition, NextPosition;
        public bool Highway, Crosswalk;
        public int NeighbourCount;
        public long MaintenanceRatioUnits;
        public double VehicleSpeed, HighwaySpeed;
        public Point Target, Axis;
        public double Length, Speed;
    }

    readonly Dictionary<Vehicle,Pr57MovementTargetEntry> _pr57MovementTargets =
        new(ReferenceEqualityComparer.Instance);
    bool _pr57MovementTargetActive;
    long _pr57MovementTargetSteps, _pr57MovementTargetHits;
    long _pr57MovementTargetMisses, _pr57MovementTargetInvalidations;

    void Pr57MovementTargetBegin()
    {
        _pr57MovementTargets.Clear();
        _pr57MovementTargetActive = Pr57MovementTargetEnabled && CostProfile is null;
        if(_pr57MovementTargetActive)_pr57MovementTargetSteps++;
    }

    void Pr57MovementTargetEnd()
    {
        _pr57MovementTargetActive=false;
        _pr57MovementTargets.Clear();
        if(Pr57MovementTargetDiag && Pr57MovementTargetEnabled &&
           _pr57MovementTargetSteps>0 && _pr57MovementTargetSteps%100==0)
            Console.WriteLine($"PR57_542_MOVEMENT_TARGET steps={_pr57MovementTargetSteps} hits={_pr57MovementTargetHits} misses={_pr57MovementTargetMisses} invalidations={_pr57MovementTargetInvalidations}");
    }

    bool Pr57MovementTargetTryGet(Vehicle v,out RoadNode node,
        out Point target,out Point axis,out double length,out double speed)
    {
        node=null!;target=axis=default;length=speed=0;
        if(!_pr57MovementTargetActive)return false;
        if(!_pr57MovementTargets.TryGetValue(v,out var e))
        {
            _pr57MovementTargetMisses++;
            return false;
        }
        var index=v.RouteIndex;
        // Only the current, preceding and following route IDs affect the
        // canonical target. Guard these O(1) inputs, including in-place edits.
        var valid=index==e.RouteIndex && index>=0 && index<v.Route.Count &&
            v.Route.Count==e.RouteCount && _roadEpoch==e.RoadEpoch &&
            v.Route[index]==e.CurrentId &&
            (index==0 || v.Route[index-1]==e.PreviousId) &&
            (index+1>=v.Route.Count || v.Route[index+1]==e.NextId) &&
            _roads.TryGetValue(e.CurrentId,out var current) &&
            ReferenceEquals(current,e.Current) &&
            e.Current.Position.Equals(e.CurrentPosition) &&
            e.Current.Highway==e.Highway &&
            e.Current.MaintenanceRatioUnits==e.MaintenanceRatioUnits &&
            e.Current.Neighbours.Count==e.NeighbourCount &&
            e.Current.Crosswalk==e.Crosswalk &&
            State.Tuning.VehicleSpeed==e.VehicleSpeed &&
            State.Tuning.HighwaySpeed==e.HighwaySpeed &&
            (e.Previous is null ||
                _roads.TryGetValue(e.PreviousId,out var previous) &&
                ReferenceEquals(previous,e.Previous) &&
                e.Previous.Position.Equals(e.PreviousPosition)) &&
            (e.Next is null ||
                _roads.TryGetValue(e.NextId,out var next) &&
                ReferenceEquals(next,e.Next) &&
                e.Next.Position.Equals(e.NextPosition));
        if(!valid)
        {
            _pr57MovementTargetInvalidations++;
            _pr57MovementTargetMisses++;
            return false;
        }
        if(Pr57MovementTargetVerify)
        {
            if(!Pr57MovementTargetFresh(v,out var freshNode,out var freshTarget,
                 out var freshAxis,out var freshLength,out var freshSpeed) ||
               !ReferenceEquals(freshNode,e.Current) ||
               !freshTarget.Equals(e.Target) || !freshAxis.Equals(e.Axis) ||
               BitConverter.DoubleToInt64Bits(freshLength)!=BitConverter.DoubleToInt64Bits(e.Length) ||
               BitConverter.DoubleToInt64Bits(freshSpeed)!=BitConverter.DoubleToInt64Bits(e.Speed))
                throw new InvalidOperationException("PR57_542_MOVEMENT_TARGET_SHADOW_MISMATCH");
        }
        _pr57MovementTargetHits++;
        node=e.Current;target=e.Target;axis=e.Axis;length=e.Length;speed=e.Speed;
        return true;
    }

    void Pr57MovementTargetStore(Vehicle v,RoadNode node,Point target,
        Point axis,double length,double speed)
    {
        if(!_pr57MovementTargetActive)return;
        var index=v.RouteIndex;
        if(index<0 || index>=v.Route.Count)return;
        // With a single-node route at index zero the original axis depends
        // on the current vehicle pose; do not cache it. Likewise, missing
        // neighbouring roads cause a pose-dependent fallback.
        if(index==0 && v.Route.Count<=1)return;
        RoadNode? previous=null,next=null;
        if(index>0 && !_roads.TryGetValue(v.Route[index-1],out previous))return;
        if(index+1<v.Route.Count && !_roads.TryGetValue(v.Route[index+1],out next))return;
        _pr57MovementTargets[v]=new Pr57MovementTargetEntry
        {
            RouteIndex=index, RouteCount=v.Route.Count, RoadEpoch=_roadEpoch,
            CurrentId=node.Id, PreviousId=previous?.Id??-1, NextId=next?.Id??-1,
            Current=node, Previous=previous, Next=next,
            CurrentPosition=node.Position, PreviousPosition=previous?.Position??default,
            NextPosition=next?.Position??default,
            Highway=node.Highway, Crosswalk=node.Crosswalk,
            NeighbourCount=node.Neighbours.Count,
            MaintenanceRatioUnits=node.MaintenanceRatioUnits,
            VehicleSpeed=State.Tuning.VehicleSpeed,
            HighwaySpeed=State.Tuning.HighwaySpeed,
            Target=target, Axis=axis, Length=length, Speed=speed
        };
    }
}
