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
        if(!_walkRoutesActive)return FindPedestrianPath(from,door,b);
        if(_walkRoutesStamp!=WalkStamp){_walkRoutes.Clear();_walkRoutesStamp=WalkStamp;}
        var key=new WalkKey(BitConverter.DoubleToInt64Bits(from.X),BitConverter.DoubleToInt64Bits(from.Z),BitConverter.DoubleToInt64Bits(door.X),BitConverter.DoubleToInt64Bits(door.Z),b);
        if(!_walkRoutes.TryGetValue(key,out var route)){if(_walkRoutes.Count>=65536)_walkRoutes.Clear();_walkRoutes[key]=route=FindPedestrianPath(from,door,b).ToArray();}
        // Callers own and edit their path list.
        return new List<Point>(route);
    }
    void BeginWalkRoutes()
    {
        // The copy goes with the routes it describes: routes stored after a
        // change made outside are taken again at the end of this call.
        if(_walkRoutes.Count>0&&(_walkRoutesStamp!=WalkStamp||_walkInputs==null||!_walkInputs.Matches(this))){_walkRoutes.Clear();_walkInputs=null;}
        _walkRoutesActive=true;
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
