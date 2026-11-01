namespace IndexCities.Simulation;

// PR57 #588: reversible execution-frequency experiment. Every actor, writer,
// physical sub-slice and event still runs in canonical priority order.
public sealed partial class CitySimulation
{
    static readonly bool Pr57588Enabled=Environment.GetEnvironmentVariable("PR57_588_ORDER_REUSE")=="1";
    Vehicle[]? _pr57588Source;
    Vehicle[]? _pr57588Sorted;
    long _pr57588Hits,_pr57588Builds;
    static int Pr57588Compare(Vehicle a,Vehicle b)
    {
        var c=a.WaitingSince.CompareTo(b.WaitingSince);
        return c!=0?c:a.Id.CompareTo(b.Id);
    }
    Vehicle[] Pr57588MovementOrder(HashSet<int>? batch)
    {
        var vehicles=State.Vehicles;
        if(Pr57588Enabled&&batch is null&&vehicles.Count>0&&
           _pr57588Source is { } source&&_pr57588Sorted is { } sorted&&
           source.Length==vehicles.Count&&sorted.Length==vehicles.Count)
        {
            var same=true;
            // Same-count public replacements and list reorder are not invisible.
            for(var i=0;i<source.Length;i++)
                if(!ReferenceEquals(source[i],vehicles[i])){same=false;break;}
            // Validate live priority keys; duplicate ties force canonical sort.
            if(same)for(var i=1;i<sorted.Length;i++)
                if(Pr57588Compare(sorted[i-1],sorted[i])>=0){same=false;break;}
            if(same)
            {
                _pr57588Hits++;
                if(_pr57588Hits==1||_pr57588Hits%1024==0)
                    Console.WriteLine($"PR57_588_ORDER_HIT hits={_pr57588Hits} builds={_pr57588Builds} vehicles={vehicles.Count}");
                return sorted;
            }
        }
        var ordered=batch is null?vehicles.ToArray():vehicles.Where(v=>batch.Contains(v.Id)).ToArray();
        Array.Sort(ordered,Pr57588Compare);
        if(Pr57588Enabled&&batch is null)
        {
            _pr57588Source=vehicles.ToArray();
            _pr57588Sorted=ordered;
            _pr57588Builds++;
        }
        return ordered;
    }
}
