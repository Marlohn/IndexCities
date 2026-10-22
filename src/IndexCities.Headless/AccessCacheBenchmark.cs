using System.Collections;
using System.Diagnostics;
using System.Reflection;
using IndexCities.Simulation;

namespace IndexCities.Headless;

// Microbenchmark: real CitySimulation.Access against its previous 4096-entry
// clearing policy; synthetic buildings and roads. NOT a simulation-day result.
static class AccessCacheBenchmark
{
    public static int Run(string[] args)
    {
        var count=args.Length>0?int.Parse(args[0]):10000;
        if(count<100||count>50000){Console.Error.WriteLine("count entre 100 e 50000");return 2;}
        var sim=new CitySimulation(42);
        // 19x19 roads; real road connectivity is irrelevant for the Access query.
        var id=200000;
        for(var x=-180;x<=180;x+=20)
            for(var z=-180;z<=180;z+=20)
                sim.State.Roads.Add(new RoadNode{Id=++id,Position=new Point(x,z)});
        var rebuild=typeof(CitySimulation).GetMethod("RebuildRoadGraph",BindingFlags.NonPublic|BindingFlags.Instance)
            ??throw new InvalidOperationException("RebuildRoadGraph ausente");
        rebuild.Invoke(sim,[true]);
        var rng=new Random(42);
        for(var i=0;i<count;i++)
            sim.State.Buildings.Add(new Building{Id=i+1,Kind=BuildingKind.House,
                Position=new Point(rng.NextDouble()*340-170+(i+1)*1e-8,
                                   rng.NextDouble()*340-170),
                Rotation=0});
        var buildings=sim.State.Buildings;
        var oldCache=new Dictionary<(BuildingKind,Point,double),int?>();
        RoadNode? OldAccess(Building b)
        {
            var key=(b.Kind,b.Position,b.Rotation);
            if(oldCache.TryGetValue(key,out var roadId))
                return roadId==null?null:sim.State.Roads.FirstOrDefault(r=>r.Id==roadId);
            var result=sim.State.Roads.Where(r=>!r.Highway&&!r.ConstructionOnly&&r.MaintenanceRatio>.05&&r.Position.Distance(b.Door)<=sim.State.Tuning.RoadWidth/2+3)
                .MinBy(r=>r.Position.Distance(b.Door));
            if(oldCache.Count>4096)oldCache.Clear();
            oldCache[key]=result?.Id;
            return result;
        }
        var accessField=typeof(CitySimulation).GetField("_accessCache",BindingFlags.Instance|BindingFlags.NonPublic)
            ??throw new InvalidOperationException("_accessCache ausente");
        var newCache=(IDictionary)accessField.GetValue(sim)!;
        // JIT warming; both caches are reset before the measured series.
        for(var i=0;i<Math.Min(128,count);i++){OldAccess(buildings[i]);sim.Access(buildings[i]);}
        oldCache.Clear();newCache.Clear();

        var expected=new int[count];
        var actual=new int[count];
        const int passes=4;
        var watch=Stopwatch.StartNew();
        var originalChecks=0L;
        for(var wave=0;wave<passes;wave++)for(var i=0;i<count;i++){
            var selected=OldAccess(buildings[i])?.Id??-1;
            if(wave==passes-1)expected[i]=selected;
            originalChecks+=selected;
        }
        var oldMs=watch.Elapsed.TotalMilliseconds;
        watch.Restart();
        var optimizedChecks=0L;
        for(var wave=0;wave<passes;wave++)for(var i=0;i<count;i++){
            var selected=sim.Access(buildings[i])?.Id??-1;
            if(wave==passes-1)actual[i]=selected;
            optimizedChecks+=selected;
        }
        var newMs=watch.Elapsed.TotalMilliseconds;
        if(originalChecks!=optimizedChecks||!expected.SequenceEqual(actual))
            throw new InvalidOperationException("DIVERGENCIA Access antigo vs novo");
        Console.WriteLine($"ACCESSCACHE buildings={count} roads={sim.State.Roads.Count} waves={passes} baselineMs={oldMs:F2} optimizedMs={newMs:F2} speedup={oldMs/newMs:F2}x oldEntries={oldCache.Count} newEntries={newCache.Count} sameResults=YES");
        return 0;
    }
}
