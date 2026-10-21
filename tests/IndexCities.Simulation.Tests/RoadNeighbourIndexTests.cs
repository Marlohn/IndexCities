using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class RoadNeighbourIndexTests
{
    static void Indexed(List<RoadNode> roads,double step)
    {
        var method=typeof(CitySimulation).GetMethod("RebuildRoadNeighbours",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(null,[roads,step]);
    }

    static List<int[]> Reference(List<RoadNode> roads,double step)
        =>roads.Select(n=>roads.Where(o=>o.Id!=n.Id&&o.Position.Distance(n.Position)<=step+.01).Select(o=>o.Id).ToArray()).ToList();

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(2026)]
    public void MatchesFullScanAndNeighbourOrder(int seed)
    {
        var rng=new Random(seed);
        var roads=new List<RoadNode>();
        for(var i=0;i<900;i++){
            // Grid, negative coordinates, boundary offsets, arbitrary positions
            // and some co-located nodes exercise ties and hash buckets.
            Point point;
            if(i%13==0)point=new(8.01*(i%17)-64.08,4.01*(i%19)-40.1);
            else if(i%17==0&&roads.Count>0)point=roads[^1].Position;
            else if(i%5==0)point=new(rng.NextDouble()*280-140,rng.NextDouble()*280-140);
            else point=new(rng.Next(-35,36)*4.0,rng.Next(-35,36)*4.0);
            roads.Add(new RoadNode{Id=i+1,Position=point});
        }
        var reference=Reference(roads,4);
        Indexed(roads,4);
        for(var i=0;i<roads.Count;i++)Assert.Equal(reference[i],roads[i].Neighbours.ToArray());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void UnusualRoadStepsFallBackWithoutChangingResults(double step)
    {
        var roads=new List<RoadNode>{
            new(){Id=1,Position=new(-4,0)},
            new(){Id=2,Position=new(0,0)},
            new(){Id=3,Position=new(4,0)}
        };
        var reference=Reference(roads,step);
        Indexed(roads,step);
        for(var i=0;i<roads.Count;i++)Assert.Equal(reference[i],roads[i].Neighbours.ToArray());
    }

    [Fact]
    public void HighlyConcentratedRoadsUseSafeFallback()
    {
        var roads=Enumerable.Range(0,150).Select(i=>new RoadNode{
            Id=i+1,Position=new(i%3*.001,i%5*.001)
        }).ToList();
        var reference=Reference(roads,4);
        Indexed(roads,4);
        for(var i=0;i<roads.Count;i++)Assert.Equal(reference[i],roads[i].Neighbours.ToArray());
    }

    [Fact]
    public void NeighboursRemainIdenticalAfterRoadsAreAddedAndRemoved()
    {
        var roads=Enumerable.Range(0,250).Select(i=>new RoadNode{Id=i+1,Position=new(i%25*4,(i/25)*4)}).ToList();
        Indexed(roads,4);
        roads.RemoveRange(80,50);
        roads.Add(new RoadNode{Id=1001,Position=new(4.01,4)});
        roads.Add(new RoadNode{Id=1002,Position=new(-.01,4)});
        var reference=Reference(roads,4);
        Indexed(roads,4);
        for(var i=0;i<roads.Count;i++)Assert.Equal(reference[i],roads[i].Neighbours.ToArray());
    }
}
