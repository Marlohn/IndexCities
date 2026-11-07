using System.Diagnostics;

namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // #325: strictly observational subcall counters. Never consulted by routing.
    static readonly bool Pr57325Enabled =
        Environment.GetEnvironmentVariable("PR57_325_WALK_BREAKDOWN") == "1";
    bool _pr57325InsideRoute;
    long _pr57325NearestCalls, _pr57325NearestTicks, _pr57325NearestBytes;
    long _pr57325RoadCalls, _pr57325RoadEmpty, _pr57325RoadTicks, _pr57325RoadBytes, _pr57325BfsPops;
    long _pr57325RecoveryCalls, _pr57325RecoveryNodes, _pr57325RecoveryTicks, _pr57325RecoveryBytes;
    long _pr57325LegalCalls, _pr57325LegalRejected, _pr57325LegalTicks, _pr57325LegalBytes;
    long _pr57325FallbackCalls;

    RoadNode? Pr57325Nearest(Point from)
    {
        if(!Pr57325Enabled)return NearestRoad(from);
        var start=Stopwatch.GetTimestamp();var bytes=GC.GetAllocatedBytesForCurrentThread();
        try{return NearestRoad(from);}
        finally{
            _pr57325NearestCalls++;
            _pr57325NearestTicks+=Stopwatch.GetTimestamp()-start;
            _pr57325NearestBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
        }
    }

    List<int> Pr57325RoadRoute(int from,int to)
    {
        if(!Pr57325Enabled)return FindRoadRoute(from,to);
        var start=Stopwatch.GetTimestamp();var bytes=GC.GetAllocatedBytesForCurrentThread();
        _pr57325InsideRoute=true;
        try{
            var route=FindRoadRoute(from,to);
            if(route.Count==0)_pr57325RoadEmpty++;
            return route;
        }
        finally{
            _pr57325InsideRoute=false;
            _pr57325RoadCalls++;
            _pr57325RoadTicks+=Stopwatch.GetTimestamp()-start;
            _pr57325RoadBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
        }
    }

    Dictionary<int,double>? Pr57325RouteLengths(int from,double limit)
    {
        if(!Pr57325Enabled)return RouteLengthsFrom(from,limit);
        var start=Stopwatch.GetTimestamp();var bytes=GC.GetAllocatedBytesForCurrentThread();
        try{
            var lengths=RouteLengthsFrom(from,limit);
            _pr57325RecoveryNodes+=lengths?.Count??0;
            return lengths;
        }
        finally{
            _pr57325RecoveryCalls++;
            _pr57325RecoveryTicks+=Stopwatch.GetTimestamp()-start;
            _pr57325RecoveryBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
        }
    }

    bool Pr57325Legal(IReadOnlyList<Point> route)
    {
        if(!Pr57325Enabled)return LegalWalk(route);
        var start=Stopwatch.GetTimestamp();var bytes=GC.GetAllocatedBytesForCurrentThread();
        try{
            var legal=LegalWalk(route);
            if(!legal)_pr57325LegalRejected++;
            return legal;
        }
        finally{
            _pr57325LegalCalls++;
            _pr57325LegalTicks+=Stopwatch.GetTimestamp()-start;
            _pr57325LegalBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
        }
    }

    IEnumerable<string> Pr57325BreakdownReport()
    {
        var hz=(double)Stopwatch.Frequency;
        yield return $"PR57_324_ROUTE name=nearest calls={_pr57325NearestCalls} ms={_pr57325NearestTicks*1000/hz:F5} bytes={_pr57325NearestBytes} scope=INTRUSIVE_OBSERVATIONAL";
        yield return $"PR57_324_ROUTE name=roadRoute calls={_pr57325RoadCalls} empty={_pr57325RoadEmpty} bfsPops={_pr57325BfsPops} ms={_pr57325RoadTicks*1000/hz:F5} bytes={_pr57325RoadBytes} scope=INTRUSIVE_OBSERVATIONAL";
        yield return $"PR57_324_ROUTE name=recovery calls={_pr57325RecoveryCalls} nodes={_pr57325RecoveryNodes} ms={_pr57325RecoveryTicks*1000/hz:F5} bytes={_pr57325RecoveryBytes} scope=INTRUSIVE_OBSERVATIONAL";
        yield return $"PR57_324_ROUTE name=legalWalk calls={_pr57325LegalCalls} rejected={_pr57325LegalRejected} ms={_pr57325LegalTicks*1000/hz:F5} bytes={_pr57325LegalBytes} scope=INTRUSIVE_OBSERVATIONAL";
        yield return $"PR57_324_ROUTE name=fallback calls={_pr57325FallbackCalls} scope=INTRUSIVE_OBSERVATIONAL";
    }
}
