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
        if(Pr57GraphProbeEnabled){_pr57GraphBlocked.Clear();_pr57GraphEdges.Clear();}
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
        if(Pr57GraphProbeEnabled){
            _pr57GraphBlocked.Add(v.Id);
            if(blocker is not null&&blocker.Id!=v.Id)
                _pr57GraphEdges.Add((Math.Min(v.Id,blocker.Id),Math.Max(v.Id,blocker.Id)));
        }
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


    // #159: observational. Direct blocker edges are a LOWER bound; 32m
    // proximity and shared reservations are a proxy, NOT an exact read/write set.
    static readonly bool Pr57GraphProbeEnabled=Environment.GetEnvironmentVariable("PR57_GRAPH_PROBE")=="1";
    readonly HashSet<int> _pr57GraphBlocked=new();
    readonly HashSet<(int A,int B)> _pr57GraphEdges=new();
    long _pr57GraphSamples,_pr57GraphSumBlocked,_pr57GraphSumDirect,_pr57GraphSumLocal,
        _pr57GraphMaxLocal,_pr57GraphSumTravelling,_pr57GraphMaxTravelling;

    void Pr57GraphEndSlice()
    {
        // Sampling is independent of signal 30s phase; physics is never sampled/skipped.
        if((_pr57DepSlices+1)%37!=0)return;
        var vehicles=State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside).ToArray();
        var indices=vehicles.Select((v,i)=>(v.Id,i)).ToDictionary(x=>x.Id,x=>x.i);
        var direct=Enumerable.Range(0,vehicles.Length).ToArray();
        var local=Enumerable.Range(0,vehicles.Length).ToArray();
        static int Root(int[] p,int x){while(p[x]!=x){p[x]=p[p[x]];x=p[x];}return x;}
        static void Join(int[] p,int a,int b){a=Root(p,a);b=Root(p,b);if(a!=b)p[Math.Max(a,b)]=Math.Min(a,b);}
        foreach(var (a,b) in _pr57GraphEdges)
            if(indices.TryGetValue(a,out var ai)&&indices.TryGetValue(b,out var bi)){
                Join(direct,ai,bi);Join(local,ai,bi);
            }
        var nearby=0;var reservations=0;
        for(var i=0;i<vehicles.Length;i++)for(var j=i+1;j<vehicles.Length;j++){
            var a=vehicles[i];var b=vehicles[j];
            var near=a.Position.Distance(b.Position)<=32;
            var shared=(a.IntersectionClaim is {} ac&&(b.IntersectionClaim==ac||b.ManoeuvreSpaces.Contains(ac)))||
                (b.IntersectionClaim is {} bc&&a.ManoeuvreSpaces.Contains(bc))||
                a.ManoeuvreSpaces.Any(b.ManoeuvreSpaces.Contains);
            if(near)nearby++;
            if(shared)reservations++;
            if(near||shared)Join(local,i,j);
        }
        static int Largest(int[] p,IEnumerable<int> ids,Dictionary<int,int> indices){
            var groups=new Dictionary<int,int>();var max=0;
            foreach(var id in ids)if(indices.TryGetValue(id,out var i)){
                var root=Root(p,i);groups.TryGetValue(root,out var count);
                groups[root]=++count;max=Math.Max(max,count);
            }
            return max;
        }
        var directBlocked=Largest(direct,_pr57GraphBlocked,indices);
        var localBlocked=Largest(local,_pr57GraphBlocked,indices);
        var travellers=vehicles.Where(v=>v.Status==VehicleStatus.Travelling).Select(v=>v.Id).ToArray();
        var localTravelling=Largest(local,travellers,indices);
        _pr57GraphSamples++;_pr57GraphSumBlocked+=_pr57GraphBlocked.Count;
        _pr57GraphSumDirect+=directBlocked;_pr57GraphSumLocal+=localBlocked;
        _pr57GraphMaxLocal=Math.Max(_pr57GraphMaxLocal,localBlocked);
        _pr57GraphSumTravelling+=localTravelling;
        _pr57GraphMaxTravelling=Math.Max(_pr57GraphMaxTravelling,localTravelling);
        if(_pr57GraphSamples%10!=0)return;
        Console.WriteLine($"PR57_GRAPH samples={_pr57GraphSamples} vehicles={vehicles.Length} travelling={travellers.Length} blocked={_pr57GraphBlocked.Count} directEdges={_pr57GraphEdges.Count} near32Pairs={nearby} reservationPairs={reservations} directLargestBlocked={directBlocked} localLargestBlocked={localBlocked} localLargestTravelling={localTravelling} meanBlocked={_pr57GraphSumBlocked/(double)_pr57GraphSamples:F3} meanDirectLargestBlocked={_pr57GraphSumDirect/(double)_pr57GraphSamples:F3} meanLocalLargestBlocked={_pr57GraphSumLocal/(double)_pr57GraphSamples:F3} maxLocalLargestBlocked={_pr57GraphMaxLocal} meanLocalLargestTravelling={_pr57GraphSumTravelling/(double)_pr57GraphSamples:F3} maxLocalLargestTravelling={_pr57GraphMaxTravelling} observational=True exactDependencies=False");
    }

    void Pr57DepEndSlice()
    {
        if(_pr57DepStart is null)return;
        var end=Pr57DepWorld();
        _pr57DepSliceChanged+=end.Count(kv=>!_pr57DepStart.TryGetValue(kv.Key,out var old)||old!=kv.Value);
        foreach(var (id,atDecision) in _pr57DepPending)
            if(!end.TryGetValue(id,out var later)||later!=atDecision)
                _pr57DepLaterBlockerChanged++;
        if(Pr57GraphProbeEnabled)Pr57GraphEndSlice();
        _pr57DepPending.Clear();
        _pr57DepStart=null;
        if(++_pr57DepSlices%60!=0)return;
        Console.WriteLine(
            $"PR57_BLOCK_DEP slices={_pr57DepSlices} zeroStep={_pr57DepBlocks} noBlocker={_pr57DepNoBlocker} repeats={_pr57DepRepeat} sameBlocker={_pr57DepSameBlocker} stableOther={_pr57DepStableOtherVehicles} stableOtherAndSignal={_pr57DepStableOtherVehiclesAndSignal} otherChanged={_pr57DepOtherChangedBetween} blockerChanged={_pr57DepBlockerChangedBetween} priorBlockerChanged={_pr57DepPriorBlockerChanged} laterBlockerChanged={_pr57DepLaterBlockerChanged} cleanupChanged={_pr57DepCleanupChanged} sliceChanged={_pr57DepSliceChanged} observational=True");
    }
}
