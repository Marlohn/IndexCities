namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57-only, opt-in exact broad phase for access to unfinished BUILDINGS.
    // The earlier WorkAccess index covers road/maintenance projects, not this
    // repeatedly scanned ConstructionAccess(Building) path.
    static readonly bool Pr57BuildingAccessEnabled =
        Environment.GetEnvironmentVariable("PR57_BUILDING_WORK_ACCESS")=="1";
    readonly Dictionary<(int X,int Z),List<(int Order,RoadNode Road)>> _pr57BuildingAccessCells=new();
    bool _pr57BuildingAccessActive,_pr57BuildingAccessAnnounced,_pr57BuildingAccessEngaged;
    int _pr57BuildingAccessBuiltEpoch=-1,_pr57BuildingAccessBuiltCount=-1;
    long _pr57BuildingAccessAdvances,_pr57BuildingAccessQueries,_pr57BuildingAccessBaselineRows;
    long _pr57BuildingAccessCandidateRows,_pr57BuildingAccessRebuilds,_pr57BuildingAccessInvalidations;
    long _pr57BuildingAccessEmptyResults,_pr57BuildingAccessIndexedRows;

    // State is externally mutable. Never use the index outside a public Advance
    // call; every Advance(2) starts with a fresh membership snapshot.
    void Pr57BeginBuildingAccessAdvance()
    {
        _pr57BuildingAccessActive=Pr57BuildingAccessEnabled;
        _pr57BuildingAccessBuiltEpoch=-1;
        if(!_pr57BuildingAccessAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine($"PR57_BUILDING_WORK_ACCESS active={_pr57BuildingAccessActive}");
            _pr57BuildingAccessAnnounced=true;
        }
    }

    void Pr57EndBuildingAccessAdvance()
    {
        if(_pr57BuildingAccessActive)
        {
            _pr57BuildingAccessAdvances++;
            // Cumulative opt-in diagnostic. Count rows, not assumed CPU savings.
            if(Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1" &&
               (_pr57BuildingAccessAdvances==1||_pr57BuildingAccessAdvances%256==0||
                _pr57BuildingAccessAdvances==43200||_pr57BuildingAccessAdvances==86400))
                Console.WriteLine($"PR57_BUILDING_ACCESS_WORK advances={_pr57BuildingAccessAdvances} queries={_pr57BuildingAccessQueries} baselineRows={_pr57BuildingAccessBaselineRows} candidateRows={_pr57BuildingAccessCandidateRows} avoidedRows={_pr57BuildingAccessBaselineRows-_pr57BuildingAccessCandidateRows} rebuilds={_pr57BuildingAccessRebuilds} invalidations={_pr57BuildingAccessInvalidations} emptyResults={_pr57BuildingAccessEmptyResults} indexedRows={_pr57BuildingAccessIndexedRows}");
        }
        _pr57BuildingAccessActive=false;
        _pr57BuildingAccessBuiltEpoch=-1;
    }

    void Pr57EnsureBuildingAccessIndex()
    {
        if(_pr57BuildingAccessBuiltEpoch==_roadEpoch&&
           _pr57BuildingAccessBuiltCount==State.Roads.Count)return;
        if(_pr57BuildingAccessBuiltEpoch>=0)_pr57BuildingAccessInvalidations++;
        _pr57BuildingAccessRebuilds++;
        // Rebuilding visits every road, even if indexed queries skip most rows.
        _pr57BuildingAccessIndexedRows+=State.Roads.Count;
        foreach(var bucket in _pr57BuildingAccessCells.Values)bucket.Clear();
        var width=Math.Max(1,State.Tuning.RoadStep);
        for(var i=0;i<State.Roads.Count;i++)
        {
            var road=State.Roads[i];
            var key=((int)Math.Floor(road.Position.X/width),
                     (int)Math.Floor(road.Position.Z/width));
            if(!_pr57BuildingAccessCells.TryGetValue(key,out var bucket))
                _pr57BuildingAccessCells[key]=bucket=[];
            bucket.Add((i,road));
        }
        _pr57BuildingAccessBuiltEpoch=_roadEpoch;
        _pr57BuildingAccessBuiltCount=State.Roads.Count;
        if(!_pr57BuildingAccessEngaged&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine($"PR57_BUILDING_WORK_ACCESS built=True roads={State.Roads.Count}");
            _pr57BuildingAccessEngaged=true;
        }
    }

    RoadNode? Pr57IndexedBuildingAccess(Building building)
    {
        Pr57EnsureBuildingAccessIndex();
        _pr57BuildingAccessQueries++;
        _pr57BuildingAccessBaselineRows+=State.Roads.Count;
        var door=building.Door;
        var radius=State.Tuning.RoadWidth/2+3;
        var width=Math.Max(1,State.Tuning.RoadStep);
        // Conservative envelope only; the original exact distance and live
        // connectivity/maintenance predicates are still authoritative.
        const double envelopeMargin=1e-8;
        var minX=(int)Math.Floor((door.X-radius-envelopeMargin)/width);
        var maxX=(int)Math.Floor((door.X+radius+envelopeMargin)/width);
        var minZ=(int)Math.Floor((door.Z-radius-envelopeMargin)/width);
        var maxZ=(int)Math.Floor((door.Z+radius+envelopeMargin)/width);
        RoadNode? nearest=null;
        var bestDistance=double.PositiveInfinity;
        var bestOrder=int.MaxValue;
        for(var x=minX;x<=maxX;x++)
        for(var z=minZ;z<=maxZ;z++)
        {
            if(!_pr57BuildingAccessCells.TryGetValue((x,z),out var bucket))continue;
            foreach(var (order,road) in bucket)
            {
                _pr57BuildingAccessCandidateRows++;
                if(road.Highway||road.MaintenanceRatio<=.05||
                   !_workExteriorConnected.Contains(road.Id))continue;
                var distance=road.Position.Distance(door);
                if(distance>radius)continue;
                // MinBy preserves first State.Roads entry on exact distance ties.
                if(distance<bestDistance||(distance==bestDistance&&order<bestOrder))
                {
                    nearest=road;bestDistance=distance;bestOrder=order;
                }
            }
        }
        if(nearest==null)_pr57BuildingAccessEmptyResults++;
        return nearest;
    }
}
