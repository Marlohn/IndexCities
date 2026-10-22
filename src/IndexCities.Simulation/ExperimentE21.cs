using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace IndexCities.Simulation;

// E21 observational only. The reference traffic algorithm always executes.
// Two mutually exclusive probes keep Stopwatch costs separate from the costly
// neighbour/dependency shadow tracing. No game/save field or rule is changed.
public sealed partial class CitySimulation
{
    public string E21Probe { get; set; } = "off"; // off|cost|causes

    long _e21Calls, _e21Visits, _e21VehicleTicks, _e21UnsplitTicks;
    long _e21OuterCalls,_e21OuterTicks,_e21ForecastCalls,_e21ForecastTicks;
    long _e21ActiveVisits, _e21ActiveTicks, _e21MovedVisits, _e21MovedTicks;
    long _e21BlockedVisits, _e21BlockedTicks, _e21StillVisits, _e21StillTicks;
    long _e21InactiveVisits, _e21InactiveTicks;
    long _e21OperationalVisits, _e21OperationalTicks;
    long _e21BlockedEvents, _e21BlockedByCar, _e21BlockedJunction, _e21BlockedOther;
    long _e21Rechecks, _e21SelfChanged, _e21BlockerChanged, _e21NeighboursChanged;
    long _e21RoadChanged, _e21DeadlineReached, _e21DeadlineAlreadyOverdue, _e21NoObservedTrigger;
    readonly List<string> _e21UnclassifiedSamples=[];
    long _e21NoTriggerPhysicalChange, _e21NoTriggerStill;
    readonly Dictionary<int,E21Wait> _e21Waits = new();
    readonly HashSet<int> _e21Marked = new();
    readonly HashSet<int> _e21NoTrigger = new();
    const double E21Radius=64; // sensitivity-analysis probe, NOT a proven dependency boundary

    readonly record struct E21Pose(Point Pos,double Heading,int RouteIndex,
        VehicleStatus Status,long Fuel,long Condition,double WaitingSince,int RouteLength,
        int? Claim,Point? Dock, TowPhase TowPhase,ServicePhase ServicePhase);

    static E21Pose E21PoseOf(Vehicle v)=>new(v.Position,v.Heading,v.RouteIndex,
        v.Status,v.FuelUnits,v.ConditionUnits,v.WaitingSince,v.Route.Count,
        v.IntersectionClaim,v.ManoeuvreDock,v.TowPhase,v.ServicePhase);

    sealed class E21Wait
    {
        public E21Pose Self;
        public int? BlockerId;
        public E21Pose Blocker;
        public int NeighbourHash;
        public int RoadRevision;
        public double At;
    }

    // Captures nearby physical AND shared-space state of all vehicles in 64m,
    // INCLUDING some vehicles which may be too far to interact this slice.
    // It is *not* a proof that a distant source cannot affect the next step.
    int E21NearbyFingerprint(Vehicle v)
    {
        var h=new HashCode();
        foreach(var o in State.Vehicles)
        {
            if(o.Id==v.Id||o.Position.Distance(v.Position)>E21Radius)continue;
            h.Add(o.Id);h.Add(o.Position);h.Add(o.Heading);h.Add(o.Status);
            h.Add(o.WaitingSince);h.Add(o.IntersectionClaim);h.Add(o.RouteIndex);
            h.Add(o.ManoeuvreDock);h.Add(o.TowPhase);h.Add(o.ServicePhase);
            h.Add(o.MovementAdmission);h.Add(o.BrokenDown);h.Add(o.FuelUnits);
            h.Add(o.DockPosition);h.Add(o.DriverId);
        }
        return h.ToHashCode();
    }

    void E21BeforeVehicle(Vehicle v)
    {
        if(E21Probe!="causes")return;
        _e21Marked.Remove(v.Id);_e21NoTrigger.Remove(v.Id);
        if(!_e21Waits.TryGetValue(v.Id,out var old))return;
        _e21Rechecks++;
        var self=E21PoseOf(v);
        if(self!=old.Self){_e21SelfChanged++;return;}
        if(old.RoadRevision!=State.RoadRevision){_e21RoadChanged++;return;}
        var blocker=old.BlockerId is {} id?State.Vehicles.FirstOrDefault(x=>x.Id==id):null;
        if(old.BlockerId!=null&&(blocker==null||E21PoseOf(blocker)!=old.Blocker))
        {_e21BlockerChanged++;return;}
        if(E21NearbyFingerprint(v)!=old.NeighbourHash){_e21NeighboursChanged++;return;}
        // Thresholds tested by traffic code are measured, but NOT every timing
        // rule is enumerated here (service, lane priority, tow and road access).
        var expiry=v.WaitingSince+State.Tuning.UTurnWaitSeconds;
        // Crossing a threshold is an EVENT. Remaining beyond an old threshold
        // on thousands of consecutive slices is NOT thousands of new events.
        if(v.WaitingSince>0&&old.At<expiry&&VehicleMovementTime>=expiry)
        {_e21DeadlineReached++;return;}
        if(v.WaitingSince>0&&old.At>=expiry)_e21DeadlineAlreadyOverdue++;
        _e21NoObservedTrigger++;_e21NoTrigger.Add(v.Id);
    }

    void E21Blocked(Vehicle v,Vehicle? blocker,bool junction=false)
    {
        if(E21Probe!="causes")return;
        _e21Marked.Add(v.Id);_e21BlockedEvents++;
        if(junction)_e21BlockedJunction++;
        else if(blocker!=null)_e21BlockedByCar++;
        else _e21BlockedOther++;
        _e21Waits[v.Id]=new E21Wait{
            Self=E21PoseOf(v),BlockerId=blocker?.Id,
            Blocker=blocker==null?default:E21PoseOf(blocker),
            NeighbourHash=E21NearbyFingerprint(v),
            RoadRevision=State.RoadRevision,At=VehicleMovementTime
        };
    }

    void E21AfterVehicle(Vehicle v,long start,E21Pose before,bool operational)
    {
        if(E21Probe=="cost")
        {
            var elapsed=Stopwatch.GetTimestamp()-start;
            _e21Visits++;_e21VehicleTicks+=elapsed;
            if(operational){_e21OperationalVisits++;_e21OperationalTicks+=elapsed;}
            if(before.Status!=VehicleStatus.Travelling){_e21InactiveVisits++;_e21InactiveTicks+=elapsed;return;}
            _e21ActiveVisits++;_e21ActiveTicks+=elapsed;
            var moved=before.Pos!=v.Position||before.Heading!=v.Heading||before.RouteIndex!=v.RouteIndex;
            if(moved){_e21MovedVisits++;_e21MovedTicks+=elapsed;}
            else if(before.WaitingSince>0||v.WaitingSince>0){_e21BlockedVisits++;_e21BlockedTicks+=elapsed;}
            else {_e21StillVisits++;_e21StillTicks+=elapsed;}
            return;
        }
        if(E21Probe=="causes")
        {
            if(_e21NoTrigger.Remove(v.Id))
            {
                var after=E21PoseOf(v);
                if(after!=before){
                    _e21NoTriggerPhysicalChange++;
                    if(_e21UnclassifiedSamples.Count<8)
                        _e21UnclassifiedSamples.Add(
                            $"E21_UNEXPLAINED at={VehicleMovementTime:F6} vehicle={v.Id} before={before} after={after}");
                }else _e21NoTriggerStill++;
            }
            if(!_e21Marked.Remove(v.Id))_e21Waits.Remove(v.Id);
        }
    }

    void E21OuterComplete(long start)
    {
        if(E21Probe!="cost")return;
        _e21OuterCalls++;_e21OuterTicks+=Stopwatch.GetTimestamp()-start;
    }
    void E21ForecastComplete(long start)
    {
        if(E21Probe!="cost")return;
        _e21ForecastCalls++;_e21ForecastTicks+=Stopwatch.GetTimestamp()-start;
    }
    void E21BeginUnsplit(){if(E21Probe!="off")_e21Calls++;}
    void E21EndUnsplit(long start)
    {
        if(E21Probe=="cost")_e21UnsplitTicks+=Stopwatch.GetTimestamp()-start;
    }

    public string E21Report()
    {
        static string F(long ticks)=>(ticks/(double)Stopwatch.Frequency).ToString("F4",CultureInfo.InvariantCulture);
        var sb=new StringBuilder();
        sb.Append("E21_TOTAL mode=").Append(E21Probe)
          .Append(" outerCalls=").Append(_e21OuterCalls)
          .Append(" outerSeconds=").Append(F(_e21OuterTicks))
          .Append(" forecastCalls=").Append(_e21ForecastCalls)
          .Append(" forecastSeconds=").Append(F(_e21ForecastTicks))
          .Append(" unsplitCalls=").Append(_e21Calls)
          .Append(" vehicleVisits=").Append(_e21Visits)
          .Append(" unsplitSeconds=").Append(F(_e21UnsplitTicks))
          .Append(" vehicleBodySeconds=").Append(F(_e21VehicleTicks))
          .AppendLine();
        foreach(var (n,c,t) in new (string,long,long)[]{
            ("active",_e21ActiveVisits,_e21ActiveTicks),
            ("moved_or_route_advanced",_e21MovedVisits,_e21MovedTicks),
            ("blocked_stationary",_e21BlockedVisits,_e21BlockedTicks),
            ("other_stationary_travelling",_e21StillVisits,_e21StillTicks),
            ("inactive",_e21InactiveVisits,_e21InactiveTicks),
            ("operational_overlap",_e21OperationalVisits,_e21OperationalTicks)})
            sb.Append("E21_COST group=").Append(n).Append(" visits=").Append(c)
              .Append(" seconds=").Append(F(t)).AppendLine();
        sb.Append("E21_CAUSES blockedObservations=").Append(_e21BlockedEvents)
          .Append(" car=").Append(_e21BlockedByCar)
          .Append(" junction=").Append(_e21BlockedJunction)
          .Append(" other=").Append(_e21BlockedOther)
          .Append(" rechecks=").Append(_e21Rechecks)
          .Append(" selfChanged=").Append(_e21SelfChanged)
          .Append(" blockerChanged=").Append(_e21BlockerChanged)
          .Append(" neighboursChanged=").Append(_e21NeighboursChanged)
          .Append(" roadChanged=").Append(_e21RoadChanged)
          .Append(" deadlineCrossings=").Append(_e21DeadlineReached)
          .Append(" alreadyOverdueRechecks=").Append(_e21DeadlineAlreadyOverdue)
          .Append(" noObservedTrigger=").Append(_e21NoObservedTrigger)
          .Append(" noTriggerStateChanged=").Append(_e21NoTriggerPhysicalChange)
          .Append(" noTriggerStill=").Append(_e21NoTriggerStill)
          .AppendLine();
        foreach(var example in _e21UnclassifiedSamples)sb.AppendLine(example);
        return sb.ToString();
    }
}
