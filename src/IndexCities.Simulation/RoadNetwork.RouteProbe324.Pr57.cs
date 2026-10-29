namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 #324: observational counters, never read by routing, movement or saves.
    // Nested timings overlap: cache miss includes sidewalk; sidewalk includes surface.
    static readonly bool Pr57324Enabled =
        Environment.GetEnvironmentVariable("PR57_324_ROUTE_EXPANSIONS") == "1";
    long _pr57324PedPathCalls, _pr57324PedPathMisses, _pr57324PedPathMissTicks;
    long _pr57324SurfaceCalls, _pr57324SurfaceBuilds, _pr57324SurfaceTicks;
    long _pr57324SidewalkCalls, _pr57324SidewalkTicks;
    long _pr57324SidewalkEdgesBuilt, _pr57324SidewalkExpanded;
    long _pr57324SidewalkPops, _pr57324SidewalkLegalChecks, _pr57324SidewalkGoalLinks;

    List<Point> Pr57324ComputeWalk(Point from,Point door,RoadNode? access)
    {
        if(!Pr57324Enabled)return FindPedestrianPath(from,door,access);
        _pr57324PedPathMisses++;
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        try{return FindPedestrianPath(from,door,access);}
        finally{_pr57324PedPathMissTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;}
    }

    public IEnumerable<string> Pr57324RouteReport()
    {
        if(!Pr57324Enabled)yield break;
        var hz=(double)System.Diagnostics.Stopwatch.Frequency;
        yield return $"PR57_324_ROUTE name=walkCache calls={_pr57324PedPathCalls} misses={_pr57324PedPathMisses} missMs={_pr57324PedPathMissTicks*1000/hz:F5} scope=INTRUSIVE_OBSERVATIONAL";
        yield return $"PR57_324_ROUTE name=surface calls={_pr57324SurfaceCalls} builds={_pr57324SurfaceBuilds} ms={_pr57324SurfaceTicks*1000/hz:F5} scope=INTRUSIVE_OBSERVATIONAL";
        yield return $"PR57_324_ROUTE name=sidewalk calls={_pr57324SidewalkCalls} ms={_pr57324SidewalkTicks*1000/hz:F5} edgesBuilt={_pr57324SidewalkEdgesBuilt} expanded={_pr57324SidewalkExpanded} pops={_pr57324SidewalkPops} legalChecks={_pr57324SidewalkLegalChecks} goalLinks={_pr57324SidewalkGoalLinks} scope=INTRUSIVE_OBSERVATIONAL";
        if(Pr57325Enabled)foreach(var line in Pr57325BreakdownReport())yield return line;
    }
}
