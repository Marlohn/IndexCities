namespace IndexCities.Simulation;

// E15: memoiza apenas a geometria deterministica de Manoeuvre, SEM callback.
// A decisao de admissao, corpos dinamicos e preferencias continuam no codigo real.
// Branch experimental: nao incorporar sem verificacao de equivalencia e custo.
public sealed partial class CitySimulation
{
    public bool E15Enable {get;set;}
    public int E15VerifyEvery {get;set;}
    long _e15Calls,_e15Eligible,_e15Hits,_e15Misses,_e15Closure,_e15Verified;
    bool _e15Bypass;
    readonly Dictionary<Vehicle,E15Entry> _e15Geometry=new();

    sealed class E15Entry
    {
        public required int RoadEpoch {get;init;}
        public required int From {get;init;}
        public required Point At {get;init;}
        public required double Facing {get;init;}
        public required bool Approach {get;init;}
        public required int Minimum {get;init;}
        public required int? Through {get;init;}
        public required double? SiteHeading {get;init;}
        public required double HalfLength {get;init;}
        public required double HalfWidth {get;init;}
        public required int[] Route {get;init;}
        public required Point[] Site {get;init;}
        public required (Vehicle Body,Point Position,double Heading)[] Poses {get;init;}
        public required RoadNode[] Spaces {get;init;}
        public required int EndIndex {get;init;}
    }

    // No .ToArray() on cache hits: compare against immutable snapshots in-place.
    static bool E15MatchesSite(Point[] old,IReadOnlyList<Point> site)
    {
        if(old.Length!=site.Count)return false;
        for(var i=0;i<old.Length;i++)if(old[i]!=site[i])return false;
        return true;
    }
    static bool E15MatchesRoute(int[] old,List<int> route)
    {
        if(old.Length!=route.Count)return false;
        for(var i=0;i<old.Length;i++)if(old[i]!=route[i])return false;
        return true;
    }

    bool E15Lookup(Vehicle vehicle,List<int> route,int from,Point at,double facing,
        IReadOnlyList<Point> site,bool approach,int minimum,int? through,
        out List<(Vehicle Body,Point Position,double Heading)> poses,
        out List<RoadNode> spaces,out int endIndex)
    {
        _e15Eligible++;
        if(_e15Geometry.TryGetValue(vehicle,out var c)
            && c.RoadEpoch==_roadEpoch
            && c.From==from&&c.At==at&&c.Facing==facing
            && c.Approach==approach&&c.Minimum==minimum&&c.Through==through
            && c.SiteHeading==vehicle.SiteDockHeading
            && c.HalfLength==BodyHalfLength(vehicle)&&c.HalfWidth==BodyHalfWidth(vehicle)
            && E15MatchesRoute(c.Route,route)&&E15MatchesSite(c.Site,site))
        {
            _e15Hits++;
            // Fresh mutable result lists: some callers append to poses/spaces.
            // Immutable stored arrays never escape into the caller.
            poses=new List<(Vehicle Body,Point Position,double Heading)>(c.Poses);
            spaces=new List<RoadNode>(c.Spaces);
            endIndex=c.EndIndex;
            return true;
        }
        _e15Misses++;
        poses=[];spaces=[];endIndex=from-1;
        return false;
    }
    void E15Remember(Vehicle vehicle,List<int> route,int from,Point at,double facing,
        IReadOnlyList<Point> site,bool approach,int minimum,int? through,
        List<(Vehicle Body,Point Position,double Heading)> poses,List<RoadNode> spaces,int endIndex)
    {
        _e15Geometry[vehicle]=new E15Entry{
            RoadEpoch=_roadEpoch,From=from,At=at,Facing=facing,
            Approach=approach,Minimum=minimum,Through=through,
            SiteHeading=vehicle.SiteDockHeading,
            HalfLength=BodyHalfLength(vehicle),HalfWidth=BodyHalfWidth(vehicle),
            Route=route.ToArray(),Site=site.ToArray(),Poses=poses.ToArray(),
            Spaces=spaces.ToArray(),EndIndex=endIndex
        };
    }
    void E15Verify(List<(Vehicle Body,Point Position,double Heading)> cached,
        List<(Vehicle Body,Point Position,double Heading)> calculated,
        List<RoadNode> oldSpaces,List<RoadNode> newSpaces,int oldEnd,int newEnd)
    {
        if(oldEnd!=newEnd||cached.Count!=calculated.Count||oldSpaces.Count!=newSpaces.Count
            ||!cached.SequenceEqual(calculated)
            ||!oldSpaces.Select(x=>x.Id).SequenceEqual(newSpaces.Select(x=>x.Id)))
            throw new InvalidOperationException("E15: cached manoeuvre diverged from fresh geometry");
        _e15Verified++;
    }
    public string E15Summary()
        =>$"E15_RESULT enabled={E15Enable} calls={_e15Calls} eligible={_e15Eligible} hits={_e15Hits} misses={_e15Misses} closure={_e15Closure} checkedHits={_e15Verified} lastEntries={_e15Geometry.Count}";
}
