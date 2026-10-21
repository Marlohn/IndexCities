using System.Diagnostics;
using System.Reflection;
using IndexCities.Simulation;

namespace IndexCities.Headless;

// Optional microbenchmark of the real simulation neighbour builder. Does not
// simulate a day or claim a 10k-population gameplay result.
static class RoadGraphBenchmark
{
    public static int Run(string[] args)
    {
        var count=args.Length==0?10000:int.Parse(args[0]);
        if(count<1||count>100000){Console.Error.WriteLine("N deve estar entre 1 e 100000");return 2;}
        var side=(int)Math.Ceiling(Math.Sqrt(count));
        var roads=Enumerable.Range(0,count).Select(i=>new RoadNode{Id=i+1,Position=new((i%side)*4.0,(i/side)*4.0)}).ToList();
        var candidate=roads.Select(r=>new RoadNode{Id=r.Id,Position=r.Position}).ToList();
        var method=typeof(CitySimulation).GetMethod("RebuildRoadNeighbours",BindingFlags.Static|BindingFlags.NonPublic)
                   ??throw new InvalidOperationException("RebuildRoadNeighbours ausente");
        var sw=Stopwatch.StartNew();
        foreach(var node in roads)node.Neighbours=roads.Where(o=>o.Id!=node.Id&&o.Position.Distance(node.Position)<=4.01).Select(o=>o.Id).ToList();
        var original=sw.Elapsed.TotalMilliseconds;
        sw.Restart();method.Invoke(null,[candidate,4.0]);var indexed=sw.Elapsed.TotalMilliseconds;
        for(var i=0;i<count;i++)if(!roads[i].Neighbours.SequenceEqual(candidate[i].Neighbours))
            throw new InvalidOperationException($"DIVERGENCIA node={i+1}");
        Console.WriteLine($"ROADGRAPH n={count} baselineMs={original:F2} indexedMs={indexed:F2} speedup={original/indexed:F2}x sameNeighbours=YES");
        return 0;
    }
}
