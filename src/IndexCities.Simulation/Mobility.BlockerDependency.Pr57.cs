namespace IndexCities.Simulation;

// PR57 #157: intrusive OBSERVATION ONLY. No physics/scheduler reads these fields.
// This is a sampled physical read/write census, NOT an exact invalidation set:
// road edits, transient writes, pedestrian state and other domain objects are absent.
public sealed partial class CitySimulation
{
    static readonly bool Pr57BlockDepProbeEnabled=Environment.GetEnvironmentVariable("PR57_BLOCK_DEP_PROBE")=="1";

    readonly record struct Pr57DepState(
        Point Position,double Heading,VehicleStatus Status,int RouteIndex,
        int? IntersectionClaim,double WaitingSince,long FuelUnits,
        TowPhase TowPhase,int? TowTargetId,bool BrokenDown,
        Point? DockPosition,Point? ManoeuvreDock,
        long RouteHash,long SpaceHash,long DetourHash);

    sealed record Pr57DepAttempt(int? BlockerId,Dictionary<int,Pr57DepState> World,long SignalPhase);

    readonly Dictionary<int,Pr57DepAttempt> _pr57DepPrevious=new();
    readonly List<(int Id,Pr57DepState AtDecision)> _pr57DepPending=new();
    Dictionary<int,Pr57DepState>? _pr57DepStart;
    long _pr57DepSlices,_pr57DepBlocks,_pr57DepRepeat,_pr57DepSameBlocker,
        _pr57DepStableOtherVehicles,_pr57DepStableOtherVehiclesAndSignal,
        _pr57DepBlockerChangedBetween,_pr57DepOtherChangedBetween,
        _pr57DepPriorBlockerChanged,_pr57DepLaterBlockerChanged,
        _pr57DepCleanupChanged,_pr57DepSliceChanged,_pr57DepNoBlocker;

    static long Pr57DepIntHash(IEnumerable<int> values)
    {
        unchecked {
            long h=1469598103934665603L;
            foreach(var x in values)h=(h^x)*1099511628211L;
            return h;
        }
    }

    static long Pr57DepPointHash(IEnumerable<Point> values)
    {
        unchecked {
            long h=1469598103934665603L;
            foreach(var p in values){
                h=(h^BitConverter.DoubleToInt64Bits(p.X))*1099511628211L;
                h=(h^BitConverter.DoubleToInt64Bits(p.Z))*1099511628211L;
            }
            return h;
        }
    }

    static Pr57DepState Pr57DepSnapshot(Vehicle v)=>new(
        v.Position,v.Heading,v.Status,v.RouteIndex,v.IntersectionClaim,
        v.WaitingSince,v.FuelUnits,v.TowPhase,v.TowTargetId,v.BrokenDown,
        v.DockPosition,v.ManoeuvreDock,
        Pr57DepIntHash(v.Route),Pr57DepIntHash(v.ManoeuvreSpaces),
        Pr57DepPointHash(v.Detour));

    Dictionary<int,Pr57DepState> Pr57DepWorld()=>
        State.Vehicles.ToDictionary(v=>v.Id,Pr57DepSnapshot);

    void Pr57DepBeginSlice()
    {
        _pr57DepStart=Pr57DepWorld();
        _pr57DepPending.Clear();
    }

    void Pr57DepAfterCleanup()
    {
        if(_pr57DepStart is null)return;
        var after=Pr57DepWorld();
        _pr57DepCleanupChanged+=after.Count(kv=>!_pr57DepStart.TryGetValue(kv.Key,out var old)||old!=kv.Value);
        _pr57DepStart=after;
    }

    // Called only when the full lane/geometry evaluation has produced step=0,
    // before any bypass, tow or reroute reaction. The result is NOT an end-of-vehicle outcome.
    void Pr57DepBlocked(Vehicle v,Vehicle? blocker)
    {
        if(_pr57DepStart is null)return;
        _pr57DepBlocks++;
        if(blocker is null)_pr57DepNoBlocker++;
        var world=Pr57DepWorld();
        var phase=(long)Math.Floor(VehicleMovementTime/30);
        if(_pr57DepPrevious.TryGetValue(v.Id,out var previous)){
            _pr57DepRepeat++;
            if(previous.BlockerId==blocker?.Id)_pr57DepSameBlocker++;
            var otherChanged=world.Any(kv=>kv.Key!=v.Id&&
                (!previous.World.TryGetValue(kv.Key,out var old)||old!=kv.Value))||
                previous.World.Keys.Any(id=>id!=v.Id&&!world.ContainsKey(id));
            if(otherChanged)_pr57DepOtherChangedBetween++;
            else{
                _pr57DepStableOtherVehicles++;
                if(previous.SignalPhase==phase)_pr57DepStableOtherVehiclesAndSignal++;
            }
            if(blocker is not null&&previous.World.TryGetValue(blocker.Id,out var prior)&&
                world.TryGetValue(blocker.Id,out var current)&&prior!=current)
                _pr57DepBlockerChangedBetween++;
        }
        if(blocker is not null&&world.TryGetValue(blocker.Id,out var blockerNow)){
            if(_pr57DepStart.TryGetValue(blocker.Id,out var start)&&start!=blockerNow)
                _pr57DepPriorBlockerChanged++;
            _pr57DepPending.Add((blocker.Id,blockerNow));
        }
        _pr57DepPrevious[v.Id]=new(blocker?.Id,world,phase);
    }

    void Pr57DepEndSlice()
    {
        if(_pr57DepStart is null)return;
        var end=Pr57DepWorld();
        _pr57DepSliceChanged+=end.Count(kv=>!_pr57DepStart.TryGetValue(kv.Key,out var old)||old!=kv.Value);
        foreach(var (id,atDecision) in _pr57DepPending)
            if(!end.TryGetValue(id,out var later)||later!=atDecision)
                _pr57DepLaterBlockerChanged++;
        _pr57DepPending.Clear();
        _pr57DepStart=null;
        if(++_pr57DepSlices%60!=0)return;
        Console.WriteLine(
            $"PR57_BLOCK_DEP slices={_pr57DepSlices} zeroStep={_pr57DepBlocks} noBlocker={_pr57DepNoBlocker} repeats={_pr57DepRepeat} sameBlocker={_pr57DepSameBlocker} stableOther={_pr57DepStableOtherVehicles} stableOtherAndSignal={_pr57DepStableOtherVehiclesAndSignal} otherChanged={_pr57DepOtherChangedBetween} blockerChanged={_pr57DepBlockerChangedBetween} priorBlockerChanged={_pr57DepPriorBlockerChanged} laterBlockerChanged={_pr57DepLaterBlockerChanged} cleanupChanged={_pr57DepCleanupChanged} sliceChanged={_pr57DepSliceChanged} observational=True");
    }
}
