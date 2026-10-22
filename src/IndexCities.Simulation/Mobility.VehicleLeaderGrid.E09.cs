namespace IndexCities.Simulation;

// E09 experimental, transient broad phase. It does not choose a blocker:
// original ordered geometry in MoveVehiclesUnsplit still makes that decision.
public sealed partial class CitySimulation
{
    private VehicleLeaderGrid? _e09LeaderGrid;
    private bool _e09CollectStats;
    private long _e09Queries, _e09Candidates, _e09FullFallbacks, _e09IndexedBuilds;
    private long _e10Refreshes, _e10Rebuilds;
    public void StartE09Counters()
    {
        _e09CollectStats=true;
        _e09Queries=_e09Candidates=_e09FullFallbacks=_e09IndexedBuilds=0;
        _e10Refreshes=_e10Rebuilds=0;
    }
    public string E09Counters()
        =>$"E09 queries={_e09Queries} inspected={_e09Candidates} fallbackFullScans={_e09FullFallbacks} indexBuilds={_e09IndexedBuilds} refreshes={_e10Refreshes} rosterRebuilds={_e10Rebuilds}";

    private sealed class VehicleLeaderGrid
    {
        private const double Cell=16.0;
        private readonly Dictionary<(long X,long Z),List<Vehicle>> _cells=new();
        private readonly Dictionary<int,(long X,long Z)> _location=new();
        private readonly Dictionary<int,int> _originalOrder=new();
        private readonly Dictionary<int,Vehicle> _members=new();
        private readonly List<Vehicle> _scratch=new();
        private readonly List<Vehicle> _vehicles;
        private bool _valid=true;

        static bool TryCell(Point p,out (long X,long Z) key)
        {
            var x=Math.Floor(p.X/Cell);var z=Math.Floor(p.Z/Cell);
            if(!double.IsFinite(x)||!double.IsFinite(z)||x<=long.MinValue+128||x>=long.MaxValue-128
                ||z<=long.MinValue+128||z>=long.MaxValue-128){key=default;return false;}
            key=((long)x,(long)z);return true;
        }

        public VehicleLeaderGrid(List<Vehicle> vehicles)
        {
            _vehicles=vehicles;
            for(var i=0;i<vehicles.Count;i++)
            {
                var v=vehicles[i];_originalOrder[v.Id]=i;_members[v.Id]=v;
                if(!TryCell(v.Position,out var key)){_valid=false;break;}
                Add(v,key);
            }
        }

        void Add(Vehicle v,(long X,long Z) key)
        {
            if(!_cells.TryGetValue(key,out var list))_cells[key]=list=[];
            list.Add(v);_location[v.Id]=key;
        }

        public void Sync(Vehicle v)
        {
            if(!_valid)return;
            if(!TryCell(v.Position,out var key)){_valid=false;return;}
            if(!_members.TryGetValue(v.Id,out var member)||!ReferenceEquals(member,v)){_valid=false;return;}
            if(!_location.TryGetValue(v.Id,out var previous)){_valid=false;return;}
            if(key==previous)return;
            var cell=_cells[previous];
            cell.Remove(v);
            if(cell.Count==0)_cells.Remove(previous);
            Add(v,key);
        }

        // All vehicles can be moved by other subsystems between outer ticks
        // (tows, parking, service, repair etc.). Reconcile once per unsplit
        // substep without allocating a new grid. Roster or identity changes
        // force a full rebuild before further spatial queries.
        public bool Refresh()
        {
            if(!_valid||_vehicles.Count!=_originalOrder.Count)return false;
            for(var i=0;i<_vehicles.Count;i++)
            {
                var v=_vehicles[i];
                if(!_originalOrder.TryGetValue(v.Id,out var oldIndex)||oldIndex!=i
                    ||!_members.TryGetValue(v.Id,out var member)||!ReferenceEquals(v,member))return false;
                Sync(v);
                if(!_valid)return false;
            }
            return true;
        }

        public IEnumerable<Vehicle> Near(Point position,double radius)
        {
            if(!_valid||!double.IsFinite(radius)||radius<0||radius>256||_vehicles.Count!=_originalOrder.Count)
                return _vehicles;
            if(!TryCell(new Point(position.X-radius,position.Z-radius),out var min)
                ||!TryCell(new Point(position.X+radius,position.Z+radius),out var max)
                ||max.X-min.X>32||max.Z-min.Z>32)return _vehicles;
            _scratch.Clear();
            for(var x=min.X;x<=max.X;x++)
                for(var z=min.Z;z<=max.Z;z++)
                    if(_cells.TryGetValue((x,z),out var list))_scratch.AddRange(list);
            _scratch.Sort((a,b)=>_originalOrder[a.Id].CompareTo(_originalOrder[b.Id]));
            return _scratch;
        }
    }
}
