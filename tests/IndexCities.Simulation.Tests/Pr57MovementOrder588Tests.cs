using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class Pr57MovementOrder588Tests
{
    [Fact]
    public void Live_priority_replacements_and_public_reorders_preserve_canonical_order()
    {
        var sim=new CitySimulation(42);
        var list=sim.State.Vehicles;
        list.Clear();
        var a=new Vehicle{Id=11,WaitingSince=5};
        var b=new Vehicle{Id=12,WaitingSince=2};
        var c=new Vehicle{Id=13,WaitingSince=3};
        list.AddRange([a,b,c]);
        var method=typeof(CitySimulation).GetMethod("Pr57588MovementOrder",BindingFlags.NonPublic|BindingFlags.Instance)!;
        void Check()
        {
            var expected=list.ToArray();
            Array.Sort(expected,(x,y)=>x.WaitingSince.CompareTo(y.WaitingSince) is var d and not 0?d:x.Id.CompareTo(y.Id));
            var actual=(Vehicle[])method.Invoke(sim,new object?[]{null})!;
            Assert.Equal(expected,actual);
        }
        Check();Check();
        a.WaitingSince=1;Check();Check();
        list.Reverse();Check();Check();
        list[0]=new Vehicle{Id=17,WaitingSince=4};Check();Check();
        c.Id=1;c.WaitingSince=0;Check();Check();
        list[0].Id=b.Id;list[0].WaitingSince=b.WaitingSince;Check();Check();
        list.RemoveAt(0);Check();
        list.Add(new Vehicle{Id=99,WaitingSince=-3});Check();
        var batch=new HashSet<int>{11,99};
        var filtered=(Vehicle[])method.Invoke(sim,new object?[]{batch})!;
        var expectedBatch=list.Where(x=>batch.Contains(x.Id)).ToArray();
        Array.Sort(expectedBatch,(x,y)=>x.WaitingSince.CompareTo(y.WaitingSince) is var d and not 0?d:x.Id.CompareTo(y.Id));
        Assert.Equal(expectedBatch,filtered);
    }
}
