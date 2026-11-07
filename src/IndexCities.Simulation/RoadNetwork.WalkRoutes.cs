namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // FindPedestrianPath(from,door,access) results, consulted only while Advance
    // runs (any other caller recomputes). A walk reads only the road list
    // (order, positions, neighbours, crossings, construction and highway flags,
    // maintenance above .05), the building lots, a few tuning lengths and its
    // own points: no walker, vehicle, clock or random draw. Inside Advance those
    // inputs change only through code that moves the road epoch, adds a building
    // or removes one, so a route stored under the same stamp is the same route.
    // Between calls the host or a test may change anything: the next Advance
    // keeps the routes only if every input still equals the copy taken when
    // they were stored. Keys use the exact bits of each coordinate.
    readonly record struct WalkKey(long FromX,long FromZ,long DoorX,long DoorZ,RoadNode? Access);
    readonly Dictionary<WalkKey,Point[]> _walkRoutes=new();
    (int RoadEpoch,int Buildings,int Removals) _walkRoutesStamp;
    WalkInputs? _walkInputs;bool _walkRoutesActive;int _buildingRemovals;
    (int,int,int) WalkStamp=>(_roadEpoch,State.Buildings.Count,_buildingRemovals);

    List<Point> PedestrianPath(Point from,Point door,RoadNode? b)
    {
        if(Pr57324Enabled)_pr57324PedPathCalls++;
        if(!_walkRoutesActive)return Pr57324ComputeWalk(from,door,b);
        if(_walkRoutesStamp!=WalkStamp){_walkRoutes.Clear();_walkRoutesStamp=WalkStamp;}
        var key=new WalkKey(BitConverter.DoubleToInt64Bits(from.X),BitConverter.DoubleToInt64Bits(from.Z),BitConverter.DoubleToInt64Bits(door.X),BitConverter.DoubleToInt64Bits(door.Z),b);
        if(!_walkRoutes.TryGetValue(key,out var route)){if(_walkRoutes.Count>=65536)_walkRoutes.Clear();_walkRoutes[key]=route=Pr57324ComputeWalk(from,door,b).ToArray();}
        // Callers own and edit their path list.
        return new List<Point>(route);
    }
    // PR57 #281: opt-in observation of the exact public Advance(2) boundary
    // validation. No route is reused, rejected or invalidated differently.
    static readonly bool Pr57WalkValidation281Enabled =
        Environment.GetEnvironmentVariable("PR57_281_WALK_VALIDATION") == "1";
    long _pr57Walk281Calls, _pr57Walk281Cached, _pr57Walk281Checked, _pr57Walk281Matched;
    long _pr57Walk281Invalidated, _pr57Walk281RouteEntries, _pr57Walk281RoadVisits;
    long _pr57Walk281NeighbourVisits, _pr57Walk281BuildingVisits;
    long _pr57Walk281BeginTicks, _pr57Walk281MatchTicks, _pr57Walk281BeginBytes;
    void BeginWalkRoutes()
    {
        // The copy goes with the routes it describes: routes stored after a
        // change made outside are taken again at the end of this call.
        if(!Pr57WalkValidation281Enabled)
        {
            if(_walkRoutes.Count>0&&(_walkRoutesStamp!=WalkStamp||_walkInputs==null||!_walkInputs.Matches(this))){_walkRoutes.Clear();_walkInputs=null;}
            _walkRoutesActive=true;
            return;
        }
        var begin=System.Diagnostics.Stopwatch.GetTimestamp();
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        var cached=_walkRoutes.Count>0;
        var invalidated=false;
        if(cached)
        {
            _pr57Walk281Cached++;
            _pr57Walk281RouteEntries+=_walkRoutes.Count;
            if(_walkRoutesStamp!=WalkStamp||_walkInputs==null)invalidated=true;
            else
            {
                var started=System.Diagnostics.Stopwatch.GetTimestamp();
                var matched=_walkInputs.Matches(this);
                _pr57Walk281MatchTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
                _pr57Walk281Checked++;
                if(matched)
                {
                    _pr57Walk281Matched++;
                    // A successful Matches() necessarily visited every road,
                    // every neighbour and every building. Failed early checks
                    // are excluded rather than pretending they scanned all.
                    _pr57Walk281RoadVisits+=State.Roads.Count;
                    foreach(var road in State.Roads)_pr57Walk281NeighbourVisits+=road.Neighbours.Count;
                    _pr57Walk281BuildingVisits+=State.Buildings.Count;
                }
                else invalidated=true;
            }
            if(invalidated){_walkRoutes.Clear();_walkInputs=null;_pr57Walk281Invalidated++;}
        }
        _walkRoutesActive=true;
        _pr57Walk281Calls++;
        _pr57Walk281BeginTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-begin;
        _pr57Walk281BeginBytes+=GC.GetAllocatedBytesForCurrentThread()-allocated;
        if(_pr57Walk281Calls%50==0)
        {
            var hz=(double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine($"PR57_281_WALK calls={_pr57Walk281Calls} cached={_pr57Walk281Cached} checked={_pr57Walk281Checked} matched={_pr57Walk281Matched} invalidated={_pr57Walk281Invalidated} routeEntriesSeen={_pr57Walk281RouteEntries} roadsVisitedOnMatch={_pr57Walk281RoadVisits} neighboursVisitedOnMatch={_pr57Walk281NeighbourVisits} buildingsVisitedOnMatch={_pr57Walk281BuildingVisits} beginMs={_pr57Walk281BeginTicks*1000/hz:F5} matchesMs={_pr57Walk281MatchTicks*1000/hz:F5} beginAllocatedBytes={_pr57Walk281BeginBytes} scope=INTRUSIVE_OBSERVATIONAL");
        }
    }
    void EndWalkRoutes()
    {
        _walkRoutesActive=false;
        if(_walkRoutesStamp!=WalkStamp)_walkRoutes.Clear();
        // A copy checked at the start still holds while the stamp did not move.
        if(_walkRoutes.Count==0)_walkInputs=null;
        else if(_walkInputs?.Stamp!=_walkRoutesStamp)_walkInputs=new WalkInputs(this,_walkRoutesStamp);
    }

    // Every value a walk reads, as it was when the stored routes were valid.
    sealed class WalkInputs
    {
        public readonly (int,int,int) Stamp;
        readonly CityState _state;readonly SimulationTuning _tuning;readonly Dictionary<int,RoadNode> _graph;
        readonly double _roadWidth,_roadStep,_crosswalkSpacing,_mapHalfSize;
        readonly (RoadNode Node,int Id,Point Position,bool Highway,bool Crosswalk,bool ConstructionOnly,bool Usable,List<int> Neighbours,int Count)[] _roads;
        readonly int[] _neighbours;
        readonly (Building Building,int Id,Point Position,double Rotation,BuildingKind Kind,bool Removed)[] _buildings;
        public WalkInputs(CitySimulation sim,(int,int,int) stamp)
        {
            Stamp=stamp;_state=sim.State;_tuning=_state.Tuning;_graph=sim._roads;
            (_roadWidth,_roadStep,_crosswalkSpacing,_mapHalfSize)=(_tuning.RoadWidth,_tuning.RoadStep,_tuning.CrosswalkSpacing,_tuning.MapHalfSize);
            _roads=_state.Roads.Select(Road).ToArray();_neighbours=_state.Roads.SelectMany(r=>r.Neighbours).ToArray();
            _buildings=_state.Buildings.Select(Lot).ToArray();
        }
        static (RoadNode,int,Point,bool,bool,bool,bool,List<int>,int) Road(RoadNode r)=>(r,r.Id,r.Position,r.Highway,r.Crosswalk,r.ConstructionOnly,r.MaintenanceRatio>.05,r.Neighbours,r.Neighbours.Count);
        static (Building,int,Point,double,BuildingKind,bool) Lot(Building b)=>(b,b.Id,b.Position,b.Rotation,b.Kind,b.Removed);
        public bool Matches(CitySimulation sim)
        {
            var state=sim.State;var tuning=state.Tuning;
            if(!ReferenceEquals(state,_state)||!ReferenceEquals(tuning,_tuning)||!ReferenceEquals(sim._roads,_graph)||(tuning.RoadWidth,tuning.RoadStep,tuning.CrosswalkSpacing,tuning.MapHalfSize)!=(_roadWidth,_roadStep,_crosswalkSpacing,_mapHalfSize))return false;
            if(state.Roads.Count!=_roads.Length||state.Buildings.Count!=_buildings.Length)return false;
            // Tuple equality compares the node, building and list references.
            var k=0;
            for(var i=0;i<_roads.Length;i++){
                var r=state.Roads[i];if(Road(r)!=_roads[i])return false;
                foreach(var id in r.Neighbours)if(_neighbours[k++]!=id)return false;
            }
            for(var i=0;i<_buildings.Length;i++)if(Lot(state.Buildings[i])!=_buildings[i])return false;
            return true;
        }
    }
}
