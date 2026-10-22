using System.Collections;
using System.Diagnostics;
using System.Reflection;
using IndexCities.Simulation;

namespace IndexCities.Headless;

// Same real CitySimulation.Access method measured using the legacy 4096
// eviction limit and the candidate building-sized limit. This is an isolated
// experiment: the policy toggle is not intended for production integration.
static class AccessCacheBenchmark
{
    public static int Run(string[] args)
    {
        var count=args.Length>0?int.Parse(args[0]):10000;
        if(count<100||count>50000){Console.Error.WriteLine("count entre 100 e 50000");return 2;}
        var sim=new CitySimulation(42);
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
                Position=new Point(rng.NextDouble()*340-170+(i+1)*1e-8,rng.NextDouble()*340-170),Rotation=0});

        var toggle=typeof(CitySimulation).GetField("_benchmarkLegacyAccessLimit",BindingFlags.Instance|BindingFlags.NonPublic)
            ??throw new InvalidOperationException("Toggle experimental ausente");
        var cacheField=typeof(CitySimulation).GetField("_accessCache",BindingFlags.Instance|BindingFlags.NonPublic)
            ??throw new InvalidOperationException("Cache ausente");
        var cache=(IDictionary)cacheField.GetValue(sim)!;
        var missesField=typeof(CitySimulation).GetField("_benchmarkAccessMisses",BindingFlags.Instance|BindingFlags.NonPublic)
            ??throw new InvalidOperationException("contador ausente");
        var buildings=sim.State.Buildings;
        const int waves=4;
        // Warm exactly the same method/JIT before both policies are measured.
        for(var i=0;i<Math.Min(128,count);i++)sim.Access(buildings[i]);
        cache.Clear();

        (double Time,long Checksum,int[] Ids,int Entries,int Misses) Measure(bool legacy)
        {
            toggle.SetValue(sim,legacy);
            cache.Clear();
            missesField.SetValue(sim,0);
            var output=new int[count];
            var checksum=0L;
            var watch=Stopwatch.StartNew();
            for(var wave=0;wave<waves;wave++)
                for(var i=0;i<count;i++){
                    var selected=sim.Access(buildings[i])?.Id??-1;
                    if(wave==waves-1)output[i]=selected;
                    checksum+=selected;
                }
            return (watch.Elapsed.TotalMilliseconds,checksum,output,cache.Count,(int)missesField.GetValue(sim)!);
        }

        // ABBA balances temporal drift and shared JIT/cache warming.
        var old1=Measure(true);var new1=Measure(false);
        var new2=Measure(false);var old2=Measure(true);
        if(old1.Checksum!=new1.Checksum||old1.Checksum!=new2.Checksum||old1.Checksum!=old2.Checksum
            ||!old1.Ids.SequenceEqual(new1.Ids)||!old1.Ids.SequenceEqual(new2.Ids)
            ||!old1.Ids.SequenceEqual(old2.Ids))
            throw new InvalidOperationException("DIVERGENCIA entre politicas Access");

        var oldMs=(old1.Time+old2.Time)/2;
        var newMs=(new1.Time+new2.Time)/2;
        Console.WriteLine($"ACCESSCACHE buildings={count} roads={sim.State.Roads.Count} waves={waves} baselineMs={oldMs:F2} optimizedMs={newMs:F2} speedup={oldMs/newMs:F2}x oldEntries={old2.Entries} newEntries={new2.Entries} oldMisses={old2.Misses} newMisses={new2.Misses} sameResults=YES method=identical design=ABBA");
        return 0;
    }
}
