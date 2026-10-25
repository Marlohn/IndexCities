namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 POC only. Index road *positions* for WorkAccess; no persisted cache,
    // no changed dispatch decisions. A new Step invalidates the whole index.
    // Rebuild within a Step if construction changes the road revision/count.
    static readonly bool Pr57WorkAccessIndexEnabled=Environment.GetEnvironmentVariable("PR57_WORK_ACCESS_INDEX")=="1";
    readonly Dictionary<(int X,int Z),List<(int Order,RoadNode Road)>> _pr57WorkAccessCells=new();
    int _pr57WorkAccessStep,_pr57WorkAccessBuiltStep=-1;
    int _pr57WorkAccessBuiltRevision=-1,_pr57WorkAccessBuiltCount=-1;
    bool _pr57WorkAccessAnnounced;
    void Pr57WorkAccessBeginStep()
    {
        if(!Pr57WorkAccessIndexEnabled)return;
        _pr57WorkAccessStep++;
        if(!_pr57WorkAccessAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine("PR57_WORK_ACCESS_INDEX active=True");
            _pr57WorkAccessAnnounced=true;
        }
    }
    (int X,int Z) Pr57WorkAccessCell(Point p)
    {
        var size=Math.Max(1,State.Tuning.RoadStep);
        return ((int)Math.Floor(p.X/size),(int)Math.Floor(p.Z/size));
    }
    void Pr57EnsureWorkAccessIndex()
    {
        if(_pr57WorkAccessBuiltStep==_pr57WorkAccessStep
           &&_pr57WorkAccessBuiltRevision==State.RoadRevision
           &&_pr57WorkAccessBuiltCount==State.Roads.Count)return;
        _pr57WorkAccessCells.Clear();
        for(var i=0;i<State.Roads.Count;i++)
        {
            var road=State.Roads[i];var key=Pr57WorkAccessCell(road.Position);
            if(!_pr57WorkAccessCells.TryGetValue(key,out var list))_pr57WorkAccessCells[key]=list=[];
            list.Add((i,road));
        }
        _pr57WorkAccessBuiltStep=_pr57WorkAccessStep;
        _pr57WorkAccessBuiltRevision=State.RoadRevision;
        _pr57WorkAccessBuiltCount=State.Roads.Count;
    }
    // Superset of the original distance predicate. The original exact
    // Euclidean distance and connected-road predicates still decide inclusion.
    IEnumerable<(int Order,RoadNode Road)> Pr57WorkAccessNear(Point point)
    {
        Pr57EnsureWorkAccessIndex();
        var radius=State.Tuning.RoadStep+.05;
        var first=Pr57WorkAccessCell(new Point(point.X-radius,point.Z-radius));
        var last=Pr57WorkAccessCell(new Point(point.X+radius,point.Z+radius));
        for(var x=first.X;x<=last.X;x++)
        for(var z=first.Z;z<=last.Z;z++)
            if(_pr57WorkAccessCells.TryGetValue((x,z),out var list))
                foreach(var entry in list)yield return entry;
    }
    RoadNode? Pr57IndexedMaintenanceAccess(Point target)
    {
        RoadNode? best=null;var distance=double.PositiveInfinity;var order=int.MaxValue;
        var radius=State.Tuning.RoadStep+.05;
        foreach(var (index,road) in Pr57WorkAccessNear(target))
        {
            if(!_workExteriorConnected.Contains(road.Id))continue;
            var d=road.Position.Distance(target);
            if(d>radius)continue;
            if(best==null||d<distance||(d==distance&&index<order))
            {best=road;distance=d;order=index;}
        }
        return best;
    }
    RoadNode? Pr57IndexedRoadProjectAccess(WorkProject project)
    {
        RoadNode? best=null;var distance=double.PositiveInfinity;var order=int.MaxValue;
        var radius=State.Tuning.RoadStep+.05;
        var seen=new HashSet<int>();
        foreach(var point in project.RoadPoints)
        foreach(var (index,road) in Pr57WorkAccessNear(point))
        {
            if(!seen.Add(index)||!_workExteriorConnected.Contains(road.Id))continue;
            if(road.Position.Distance(point)>radius)continue;
            var d=road.Position.Distance(project.Position);
            if(best==null||d<distance||(d==distance&&index<order))
            {best=road;distance=d;order=index;}
        }
        return best;
    }
}
