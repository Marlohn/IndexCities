using System.Diagnostics;
using System.Globalization;

namespace IndexCities.Simulation;

// E22 experimental duel. Neither candidate is a validated causal executor.
// Both use the CURRENT motor for all non-sleeping vehicles, preserve the
// original ordering, and are A/B compared against full saved state.
public sealed partial class CitySimulation
{
    public string E22Mode { get; set; }="off"; // off | events | queue
    sealed class E22Subscription
    {
        public int BlockerId,GroupRootId,RoadRevision;
        public E22Signature Self;
        public bool Awake;
        public double RegisteredAt;
    }
    readonly record struct E22Signature(
        Point Position,double Heading,VehicleStatus Status,int RouteIndex,
        int RouteCount,long Fuel,long Condition,double WaitingSince,
        int? Claim,int? DriverId,int? TowTruckId,TowPhase TowPhase,
        ServicePhase ServicePhase,int DetourCount,int SpacesCount,string Cause);
    static E22Signature E22State(Vehicle v)=>new(v.Position,v.Heading,v.Status,
        v.RouteIndex,v.Route.Count,v.FuelUnits,v.ConditionUnits,v.WaitingSince,
        v.IntersectionClaim,v.DriverId,v.TowTruckId,v.TowPhase,v.ServicePhase,
        v.Detour.Count,v.ManoeuvreSpaces.Count,v.Cause);

    readonly Dictionary<int,E22Subscription> _e22Waits=new();
    // Vehicle -> directly blocked followers, event subscriptions.
    readonly Dictionary<int,HashSet<int>> _e22Followers=new();
    // Root vehicle -> waiting members, physically connected queue cluster.
    readonly Dictionary<int,HashSet<int>> _e22Queues=new();
    readonly Dictionary<int,int> _e22QueueByMember=new();
    readonly HashSet<int> _e22BatchSkip=new();
    long _e22Visits,_e22Registered,_e22Eligible,_e22Skipped,_e22Awakened;
    long _e22WokeByLeader,_e22WokeByGroup,_e22WokeByOwnState,_e22WokeByTime;
    long _e22QueuesPrepared,_e22QueuesSlept,_e22GroupMembers,_e22GroupsMax;
    long _e22ChangedVehicles,_e22EventEdges;
    long _e22OriginalPhysicalVisits;
    double _e22BatchPrepSeconds;

    bool E22Enabled=>E22Mode is "events" or "queue";

    void E22Unsubscribe(int id)
    {
        if(!_e22Waits.Remove(id,out var sub))return;
        if(_e22Followers.TryGetValue(sub.BlockerId,out var followers))
        {followers.Remove(id);if(followers.Count==0)_e22Followers.Remove(sub.BlockerId);}
        if(_e22QueueByMember.Remove(id,out var root)&&_e22Queues.TryGetValue(root,out var members))
        {members.Remove(id);if(members.Count==0)_e22Queues.Remove(root);}
        _e22BatchSkip.Remove(id);
    }

    int E22Root(int blockerId)
    {
        var id=blockerId;
        var seen=new HashSet<int>();
        for(var hops=0;hops<State.Vehicles.Count&&seen.Add(id);hops++)
        {
            if(!_e22Waits.TryGetValue(id,out var next)||next.BlockerId==id)break;
            id=next.BlockerId;
        }
        return id;
    }

    void E22Subscribe(Vehicle v,Vehicle? leader)
    {
        if(!E22Enabled||leader==null||!v.PassengerCar||
           v.Status!=VehicleStatus.Travelling||v.Detour.Count!=0||
           v.TowPhase!=TowPhase.None||v.ServicePhase!=ServicePhase.None||
           v.ManoeuvreSpaces.Count!=0||v.WaitingSince<=0||
           v.IntersectionClaim!=null||v.RouteIndex>=v.Route.Count||
           !_roads.TryGetValue(v.Route[v.RouteIndex],out var road)||
           road.Crosswalk||road.Neighbours.Count>=3||v.Cause!="Fila na faixa")
        {E22Unsubscribe(v.Id);return;}
        _e22Eligible++;
        E22Unsubscribe(v.Id);
        var root=E22Mode=="queue"?E22Root(leader.Id):leader.Id;
        var info=new E22Subscription{
            BlockerId=leader.Id,GroupRootId=root,Self=E22State(v),
            RoadRevision=State.RoadRevision,RegisteredAt=VehicleMovementTime
        };
        _e22Waits[v.Id]=info;
        if(!_e22Followers.TryGetValue(leader.Id,out var followers))_e22Followers[leader.Id]=followers=new();
        followers.Add(v.Id);
        if(E22Mode=="queue")
        {
            if(!_e22Queues.TryGetValue(root,out var members))_e22Queues[root]=members=new();
            members.Add(v.Id);_e22QueueByMember[v.Id]=root;
        }
        _e22Registered++;
    }

    bool E22CanSleep(Vehicle v)
    {
        if(!E22Enabled||!_e22Waits.TryGetValue(v.Id,out var sub))return false;
        _e22Visits++;
        if(sub.Awake){_e22Awakened++;E22Unsubscribe(v.Id);return false;}
        if(sub.Self!=E22State(v)||sub.RoadRevision!=State.RoadRevision)
        {_e22WokeByOwnState++;E22Unsubscribe(v.Id);return false;}
        // A time-driven wake is mandatory at the original U-turn threshold
        // or signal-cycle boundary. Other time dependencies remain unproven.
        if(v.WaitingSince>0&&VehicleMovementTime>=v.WaitingSince+State.Tuning.UTurnWaitSeconds||
           (int)(sub.RegisteredAt/30)!=(int)(VehicleMovementTime/30))
        {_e22WokeByTime++;E22Unsubscribe(v.Id);return false;}
        if(E22Mode=="queue"&&!_e22BatchSkip.Contains(v.Id))
        {E22Unsubscribe(v.Id);return false;}
        _e22Skipped++;return true;
    }

    // Batch candidate: only wake the entire subscribed queue when its shared
    // physical head/body changed. This is NOT an independent lane solver, and
    // it does NOT remove the original per-vehicle scheduling overhead.
    void E22PrepareBatch()
    {
        if(E22Mode!="queue")return;
        var t=Stopwatch.GetTimestamp();
        _e22BatchSkip.Clear();_e22QueuesPrepared++;
        foreach(var (_,members) in _e22Queues)
        {
            var canSleep=true;
            foreach(var id in members)
            {
                _e22Visits++;
                if(!_e22Waits.TryGetValue(id,out var sub)||sub.Awake||
                   State.Vehicles.FirstOrDefault(v=>v.Id==id) is not {} vehicle||
                   sub.Self!=E22State(vehicle)||
                   sub.RoadRevision!=State.RoadRevision||
                   vehicle.WaitingSince>0&&VehicleMovementTime>=vehicle.WaitingSince+State.Tuning.UTurnWaitSeconds||
                   (int)(sub.RegisteredAt/30)!=(int)(VehicleMovementTime/30))
                {canSleep=false;break;}
            }
            if(!canSleep)continue;
            _e22QueuesSlept++;_e22GroupMembers+=members.Count;
            _e22GroupsMax=Math.Max(_e22GroupsMax,members.Count);
            foreach(var id in members)_e22BatchSkip.Add(id);
        }
        _e22BatchPrepSeconds+=(Stopwatch.GetTimestamp()-t)/(double)Stopwatch.Frequency;
    }

    // Called AFTER the original algorithm's vehicle commit, in original order.
    // Actual vehicle changes publish invalidations, avoiding a scan of nearby
    // vehicles for EVERY sleeping car. Other game domains still can invalidate
    // unexpectedly; the full save comparison deliberately exposes that risk.
    void E22Publish(Vehicle v,E22Signature before)
    {
        if(!E22Enabled||E22State(v)==before)return;
        _e22ChangedVehicles++;
        if(_e22Followers.TryGetValue(v.Id,out var dependents))
            foreach(var follower in dependents)
            {
                _e22EventEdges++;
                if(_e22Waits.TryGetValue(follower,out var sub)&&!sub.Awake)
                {sub.Awake=true;_e22WokeByLeader++;}
                _e22BatchSkip.Remove(follower);
            }
        if(E22Mode=="queue")
        {
            // Root groups are woken together when a member changes: a queue
            // is NOT independent simply because its head is stopped.
            var root=_e22QueueByMember.GetValueOrDefault(v.Id,v.Id);
            if(_e22Queues.TryGetValue(root,out var group))
                foreach(var member in group)
                {
                    _e22EventEdges++;
                    if(_e22Waits.TryGetValue(member,out var sub)&&!sub.Awake)
                    {sub.Awake=true;_e22WokeByGroup++;}
                    _e22BatchSkip.Remove(member);
                }
        }
    }

    public string E22Report()
    {
        static string F(double n)=>n.ToString("F4",CultureInfo.InvariantCulture);
        return $"E22_RESULT mode={E22Mode} registered={_e22Registered} eligible={_e22Eligible} " +
            $"checks={_e22Visits} skippedPhysical={_e22Skipped} originalPhysical={_e22OriginalPhysicalVisits} " +
            $"awake={_e22Awakened} wokeOwn={_e22WokeByOwnState} wokeTime={_e22WokeByTime} " +
            $"wokeLeader={_e22WokeByLeader} wokeGroup={_e22WokeByGroup} changed={_e22ChangedVehicles} " +
            $"invalidationEdges={_e22EventEdges} queuesPrepared={_e22QueuesPrepared} " +
            $"queuesSlept={_e22QueuesSlept} groupedMembers={_e22GroupMembers} " +
            $"largestGroup={_e22GroupsMax} batchSeconds={F(_e22BatchPrepSeconds)} " +
            $"subscriptionsRemaining={_e22Waits.Count}";
    }
}
