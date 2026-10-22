using System.Diagnostics;
using IndexCities.Simulation;

namespace IndexCities.Headless;

// E13: microbench de layout sobre POSICOES REAIS de um snapshot de cidade.
// Testa uma operacao representativa de busca de lider, nao troca a estrutura
// canonica nem mede o dia integrado. Resultados sao limite de oportunidade.
internal static class ExperimentE13
{
    readonly record struct Hot(int Id,double X,double Z,double Heading,VehicleStatus Status);
    static Hot Of(Vehicle v)=>new(v.Id,v.Position.X,v.Position.Z,v.Heading,v.Status);

    static int ObjectScan(List<Vehicle> source,int selfIndex)
    {
        var v=source[selfIndex];var x=v.Position.X;var z=v.Position.Z;
        var sin=Math.Sin(v.Heading);var cos=Math.Cos(v.Heading);
        var best=-1;var nearest=20.0;
        for(var j=0;j<source.Count;j++)
        {
            var other=source[j];
            if(j==selfIndex||other.Status is not (VehicleStatus.Travelling or VehicleStatus.Stranded))continue;
            if(Math.Cos(other.Heading-v.Heading)<=.5)continue;
            var dx=other.Position.X-x;var dz=other.Position.Z-z;
            var ahead=dx*sin+dz*cos;
            if(ahead<=0||ahead>=nearest||Math.Abs(dx*cos-dz*sin)>=2.6)continue;
            nearest=ahead;best=other.Id;
        }
        return best;
    }

    static int HotScan(Hot[] source,int selfIndex)
    {
        var v=source[selfIndex];var x=v.X;var z=v.Z;
        var sin=Math.Sin(v.Heading);var cos=Math.Cos(v.Heading);
        var best=-1;var nearest=20.0;
        for(var j=0;j<source.Length;j++)
        {
            var other=source[j];
            if(j==selfIndex||other.Status is not (VehicleStatus.Travelling or VehicleStatus.Stranded))continue;
            if(Math.Cos(other.Heading-v.Heading)<=.5)continue;
            var dx=other.X-x;var dz=other.Z-z;
            var ahead=dx*sin+dz*cos;
            if(ahead<=0||ahead>=nearest||Math.Abs(dx*cos-dz*sin)>=2.6)continue;
            nearest=ahead;best=other.Id;
        }
        return best;
    }

    public static int Run(int rows,int length,int queries)
    {
        var (sim,desc)=Fixtures.Grid(42,rows,length,true);
        Console.WriteLine("E13_FIXTURE "+desc);
        sim.Advance(86400);
        var vehicles=sim.State.Vehicles;
        if(vehicles.Count==0){Console.Error.WriteLine("E13 no vehicles");return 1;}
        var build=Stopwatch.StartNew();
        var hot=vehicles.Select(Of).ToArray();
        build.Stop();
        Console.WriteLine($"E13_SNAPSHOT vehicles={vehicles.Count} pop={sim.Population} build={build.Elapsed.TotalMilliseconds:F3}ms bytesSnapshotEstimate={hot.Length*System.Runtime.InteropServices.Marshal.SizeOf<Hot>()}");
        for(var i=0;i<vehicles.Count;i++)
            if(ObjectScan(vehicles,i)!=HotScan(hot,i))
            {
                Console.Error.WriteLine($"E13_INVALID query={i} results differ");
                return 1;
            }
        var indices=new int[queries];
        for(var i=0;i<queries;i++)indices[i]=(i*37+17)%vehicles.Count;
        // JIT aquecimento antes dos tempos.
        for(var i=0;i<Math.Min(indices.Length,5000);i++){ObjectScan(vehicles,indices[i]);HotScan(hot,indices[i]);}
        long checksum=0;var alloc=GC.GetTotalAllocatedBytes();
        var sw=Stopwatch.StartNew();
        foreach(var idx in indices)checksum+=ObjectScan(vehicles,idx);
        sw.Stop();
        var objectMs=sw.Elapsed.TotalMilliseconds;var objectAlloc=GC.GetTotalAllocatedBytes()-alloc;
        long otherChecksum=0;alloc=GC.GetTotalAllocatedBytes();sw.Restart();
        foreach(var idx in indices)otherChecksum+=HotScan(hot,idx);
        sw.Stop();
        var hotMs=sw.Elapsed.TotalMilliseconds;var hotAlloc=GC.GetTotalAllocatedBytes()-alloc;
        Console.WriteLine($"E13_RESULT vehicles={vehicles.Count} queries={queries} objectMs={objectMs:F2} hotMs={hotMs:F2} ratio={objectMs/Math.Max(.0001,hotMs):F3}x objectAllocBytes={objectAlloc} hotAllocBytes={hotAlloc} objectChecksum={checksum} hotChecksum={otherChecksum} identical={checksum==otherChecksum}");
        Console.WriteLine("E13_LIMIT medicao de consultas simples sobre snapshot fixo; nao inclui manter layout sincronizado, estados dinamicos ou custo de simular um dia. Nao implica ganho integrado.");
        return checksum==otherChecksum?0:1;
    }
}
