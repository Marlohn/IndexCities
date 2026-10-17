using IndexCities.Simulation;

namespace IndexCities.Headless;

// Rough upper bound for "interaction bubbles": after each 2 s step, how long
// could each moving agent go before any other moving agent could get within
// interaction range, given worst-case closing speeds? Static obstacles,
// signals and crossings are ignored, so real horizons are shorter.
sealed class Isolation
{
    static readonly double[] Edges=[2,10,30,120];
    readonly long[] _walk=new long[5],_drive=new long[5];
    long _walkSum,_driveSum;
    public void Sample(CitySimulation sim)
    {
        var t=sim.State.Tuning;var vmax=Math.Max(t.VehicleSpeed,t.HighwaySpeed);
        var walkers=sim.State.Citizens.Where(c=>c.Alive&&c.Moving&&c.VehicleId==null).Select(c=>c.Position).ToList();
        var cars=sim.State.Vehicles.Where(v=>v.Status==VehicleStatus.Travelling).Select(v=>v.Position).ToList();
        for(var i=0;i<walkers.Count;i++){
            var h=double.MaxValue;
            for(var j=0;j<walkers.Count;j++)if(j!=i)h=Math.Min(h,(walkers[i].Distance(walkers[j])-10)/(2*t.WalkingSpeed));
            foreach(var car in cars)h=Math.Min(h,(walkers[i].Distance(car)-10)/(vmax+t.WalkingSpeed));
            Add(_walk,h);_walkSum++;
        }
        for(var i=0;i<cars.Count;i++){
            var h=double.MaxValue;
            for(var j=0;j<cars.Count;j++)if(j!=i)h=Math.Min(h,(cars[i].Distance(cars[j])-20)/(2*vmax));
            foreach(var w in walkers)h=Math.Min(h,(cars[i].Distance(w)-10)/(vmax+t.WalkingSpeed));
            Add(_drive,h);_driveSum++;
        }
    }
    static void Add(long[] b,double h){var k=0;while(k<Edges.Length&&h>=Edges[k])k++;b[k]++;}
    public void Print()
    {
        string Row(long[] b,long n)=>n==0?"-":string.Join("  ",new[]{"<2s","2-10s","10-30s","30-120s",">=120s"}.Select((l,k)=>$"{l}:{100.0*b[k]/n:F1}%"));
        Console.WriteLine($"ISOLATION pedestres (passos={_walkSum}): {Row(_walk,_walkSum)}");
        Console.WriteLine($"ISOLATION veículos  (passos={_driveSum}): {Row(_drive,_driveSum)}");
    }
}
