namespace IndexCities.Simulation;

// PR57 #547: opt-in monotone negative certificate for CanTurnNow.
// A single STILL-blocking body proves the Boolean decision remains false even
// when unrelated vehicles move. No positive decision is memoized. All actors,
// physical slices, admission calls, money, history and save semantics remain.
public sealed partial class CitySimulation
{
    static readonly bool Pr57NegativeWitnessEnabled =
        Environment.GetEnvironmentVariable("PR57_NEGATIVE_WITNESS") == "1";
    static readonly bool Pr57NegativeWitnessVerify =
        Environment.GetEnvironmentVariable("PR57_NEGATIVE_WITNESS_VERIFY") == "1";
    static readonly bool Pr57NegativeWitnessDiag =
        Environment.GetEnvironmentVariable("PR57_NEGATIVE_WITNESS_DIAG") == "1";

    sealed class Pr57NegativeCertificate
    {
        public Vehicle Blocker = null!;
        public int BlockerIndex, RouteIndex, RoadEpoch;
        public long RoadRevision;
        public Point Position;
        public double Heading, HalfLength, HalfWidth, Reach;
        public int[] Route = [];
        public List<(Vehicle Body, Point Position, double Heading)> Poses = null!;
    }

    readonly Dictionary<Vehicle, Pr57NegativeCertificate> _pr57NegativeCertificates =
        new(ReferenceEqualityComparer.Instance);
    bool _pr57NegativeActive, _pr57NegativeShadowActive;
    long _pr57NegativeSteps, _pr57NegativeHits, _pr57NegativeMisses,
         _pr57NegativeWitnessLost, _pr57NegativeStores;

    void Pr57NegativeWitnessBegin()
    {
        _pr57NegativeCertificates.Clear();
        // The existing TurnWay lease owns a complete mutable-road snapshot.
        // Do not run with profiling observers that require every physical call.
        _pr57NegativeActive = Pr57NegativeWitnessEnabled && _pr57TurnWayActive &&
            !Pr57261Enabled && !Pr57262Enabled && !Pr57263Enabled;
        if(_pr57NegativeActive)_pr57NegativeSteps++;
    }

    void Pr57NegativeWitnessEnd()
    {
        _pr57NegativeActive = false;
        _pr57NegativeCertificates.Clear();
        if(Pr57NegativeWitnessDiag && Pr57NegativeWitnessEnabled &&
           _pr57NegativeSteps > 0 && _pr57NegativeSteps % 100 == 0)
            Console.WriteLine($"PR57_547_NEGATIVE steps={_pr57NegativeSteps} hits={_pr57NegativeHits} misses={_pr57NegativeMisses} lost={_pr57NegativeWitnessLost} stores={_pr57NegativeStores}");
    }

    // True means the same physical body still obstructs at least one sampled
    // pose. It does NOT mean that this body is still the first blocker; the
    // caller only consumes CanTurnNow's Boolean. Instrumented paths fall back.
    bool Pr57NegativeWitnessTry(Vehicle vehicle)
    {
        if(!_pr57NegativeActive || _pr57NegativeShadowActive)return false;
        if(!_pr57NegativeCertificates.TryGetValue(vehicle,out var c))
        {
            _pr57NegativeMisses++;
            return false;
        }
        var blocker=c.Blocker;
        if(!vehicle.Position.Equals(c.Position) || vehicle.Heading!=c.Heading ||
           vehicle.RouteIndex!=c.RouteIndex || _roadEpoch!=c.RoadEpoch ||
           _pr57TurnWayRoadRevision!=c.RoadRevision ||
           BodyHalfLength(vehicle)!=c.HalfLength || BodyHalfWidth(vehicle)!=c.HalfWidth ||
           !vehicle.Route.SequenceEqual(c.Route) ||
           !Pr57TurnWayRoadsUnchanged() ||
           c.BlockerIndex>=State.Vehicles.Count ||
           !ReferenceEquals(State.Vehicles[c.BlockerIndex],blocker) ||
           blocker.Id==vehicle.Id || blocker.Id==vehicle.TowTargetId ||
           blocker.TowTruckId==vehicle.Id ||
           blocker.Status is VehicleStatus.Outside or VehicleStatus.Towed ||
           blocker.Position.Distance(vehicle.Position)>c.Reach ||
           AtGateway(blocker)&&AtGateway(vehicle))
        {
            _pr57NegativeCertificates.Remove(vehicle);
            _pr57NegativeWitnessLost++;
            return false;
        }
        var now=Penetration(vehicle,vehicle.Position,vehicle.Heading,
                            blocker,blocker.Position,blocker.Heading);
        var limit=Math.Max(-.1,now+.001);
        var stillBlocked=false;
        foreach(var pose in c.Poses)
            if(pose.Position.Distance(blocker.Position)<12 &&
               Penetration(vehicle,pose.Position,pose.Heading,
                           blocker,blocker.Position,blocker.Heading)>limit)
            {
                stillBlocked=true;
                break;
            }
        if(!stillBlocked)
        {
            _pr57NegativeCertificates.Remove(vehicle);
            _pr57NegativeWitnessLost++;
            return false;
        }
        _pr57NegativeHits++;
        if(Pr57NegativeWitnessVerify)
        {
            _pr57NegativeShadowActive=true;
            try
            {
                if(CanTurnNow(vehicle))
                    throw new InvalidOperationException("PR57_547_NEGATIVE_WITNESS_CAUSAL_MISMATCH");
            }
            finally { _pr57NegativeShadowActive=false; }
        }
        return true;
    }

    void Pr57NegativeWitnessStore(Vehicle vehicle,
        List<(Vehicle Body,Point Position,double Heading)> poses, Vehicle blocker)
    {
        if(!_pr57NegativeActive || _pr57NegativeShadowActive)return;
        var index=State.Vehicles.IndexOf(blocker);
        if(index<0)return;
        _pr57NegativeCertificates[vehicle]=new Pr57NegativeCertificate
        {
            Blocker=blocker, BlockerIndex=index,
            Position=vehicle.Position, Heading=vehicle.Heading,
            RouteIndex=vehicle.RouteIndex, Route=vehicle.Route.ToArray(),
            RoadEpoch=_roadEpoch, RoadRevision=_pr57TurnWayRoadRevision,
            HalfLength=BodyHalfLength(vehicle), HalfWidth=BodyHalfWidth(vehicle),
            Reach=poses.Max(p=>p.Position.Distance(vehicle.Position))+12,
            Poses=poses
        };
        _pr57NegativeStores++;
    }
}
