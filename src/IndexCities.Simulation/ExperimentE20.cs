namespace IndexCities.Simulation;

// E20: a falsifiable, *experimental* vehicle-sleep trial, not production logic.
// A blocked car is a candidate to sleep until the local set of bodies changes.
// This is deliberately conservative about route, state, position and elapsed
// time, but 64m is not a proven causal bound; A/B saves decide whether it fails.
public sealed partial class CitySimulation
{
    public bool E20SleepBlockedCars { get; set; }

    sealed class E20Wait
    {
        public int BlockingId;
        public Point Position;
        public double Heading;
        public long Fuel, Condition;
        public double WaitingSince, LastPhysicalCheck;
        public int RouteIndex, RouteCount, RoadRevision;
        public VehicleStatus Status;
        public string Cause="";
        public int NeighboursSignature;
    }

    readonly Dictionary<int,E20Wait> _e20Waiting = new();
    long _e20Candidates, _e20Skipped, _e20Rechecks, _e20Wakes;

    int E20NeighboursSignature(Vehicle v)
    {
        var hash = new HashCode();
        foreach(var o in State.Vehicles)
        {
            if(o.Position.Distance(v.Position)>64)continue;
            hash.Add(o.Id);hash.Add(o.Position);hash.Add(o.Heading);
            hash.Add(o.Status);hash.Add(o.WaitingSince);
            hash.Add(o.RouteIndex);hash.Add(o.FuelUnits);
            hash.Add(o.IntersectionClaim);hash.Add(o.ManoeuvreDock);
            hash.Add(o.TowPhase);hash.Add(o.ServicePhase);hash.Add(o.BrokenDown);
            hash.Add(o.MovementAdmission);
        }
        return hash.ToHashCode();
    }

    void E20MarkBlocked(Vehicle v, Vehicle? blocker)
    {
        if(!E20SleepBlockedCars)return;
        // Track only plain cars blocked in traffic, never tow, paving, dock,
        // overtaking, emergency or recovery trajectories.
        if(blocker==null||!v.PassengerCar||v.Detour.Count!=0||
           v.TowPhase!=TowPhase.None||v.Machine!=PavingMachine.None||
           v.WaitingSince<=0||v.Status!=VehicleStatus.Travelling)
        {
            _e20Waiting.Remove(v.Id);
            return;
        }
        _e20Candidates++;
        _e20Waiting[v.Id]=new E20Wait{
            BlockingId=blocker.Id,Position=v.Position,Heading=v.Heading,
            Fuel=v.FuelUnits,Condition=v.ConditionUnits,WaitingSince=v.WaitingSince,
            LastPhysicalCheck=VehicleMovementTime,
            RouteIndex=v.RouteIndex,RouteCount=v.Route.Count,RoadRevision=State.RoadRevision,
            Status=v.Status,Cause=v.Cause,NeighboursSignature=E20NeighboursSignature(v)
        };
    }

    bool E20CanRemainBlocked(Vehicle v)
    {
        if(!E20SleepBlockedCars||_vehicleMovementClock==null||
           !_e20Waiting.TryGetValue(v.Id,out var prior))return false;
        _e20Rechecks++;
        // Every skipped step remains bounded by the original 2-second quantum.
        // This bound is NOT a proof that all time-driven decisions are covered.
        var now=VehicleMovementTime;
        var same=now>=prior.LastPhysicalCheck&&now-prior.LastPhysicalCheck<0.75
            &&now-v.WaitingSince+0.000001<State.Tuning.UTurnWaitSeconds
            &&prior.RoadRevision==State.RoadRevision
            &&prior.Position==v.Position&&prior.Heading==v.Heading
            &&prior.Fuel==v.FuelUnits&&prior.Condition==v.ConditionUnits
            &&prior.WaitingSince==v.WaitingSince
            &&prior.RouteIndex==v.RouteIndex&&prior.RouteCount==v.Route.Count
            &&prior.Status==v.Status&&prior.Cause==v.Cause
            &&v.Detour.Count==0&&v.TowPhase==TowPhase.None&&v.Machine==PavingMachine.None
            &&State.Vehicles.Any(b=>b.Id==prior.BlockingId&&b.Status!=VehicleStatus.Outside)
            &&prior.NeighboursSignature==E20NeighboursSignature(v);
        if(!same){_e20Wakes++;_e20Waiting.Remove(v.Id);return false;}
        _e20Skipped++;
        return true;
    }

    public string E20Summary()=>
        $"E20_RESULT markedBlocked={_e20Candidates} rechecks={_e20Rechecks} " +
        $"skippedPhysicalRechecks={_e20Skipped} woke={_e20Wakes} activeSleeps={_e20Waiting.Count} " +
        "(UNPROVEN physical sleep, no change to economy clock, not safe for production)";
}
