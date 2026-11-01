using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #587: exact lookup behavior under same-count public edits. These
// adversarials execute with the opt-in both off and on in separate .NET runs.
public sealed class Pr57EntityIndexMissTests
{
    [Fact]
    public void Missing_keys_reorders_replacements_and_id_edits_preserve_live_first_match()
    {
        var sim=new CitySimulation(42);
        var find=typeof(CitySimulation).GetMethod("CitizenById",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Citizen? Lookup(int id)=>(Citizen?)find.Invoke(sim,new object?[]{id});
        var a=new Citizen{Id=sim.State.NextId++};
        var b=new Citizen{Id=sim.State.NextId++};
        var c=new Citizen{Id=sim.State.NextId++};
        var list=sim.State.Citizens;
        list.AddRange([a,b]);
        Assert.Same(a,Lookup(a.Id));
        Assert.Null(Lookup(c.Id));
        Assert.Null(Lookup(c.Id)); // repeated negative, no stale negative cache
        list[0]=c; // same count, external replacement, no mutation signal
        Assert.Same(c,Lookup(c.Id));
        Assert.Null(Lookup(a.Id));
        (list[0],list[1])=(list[1],list[0]); // same count, reordering
        Assert.Same(b,Lookup(b.Id));
        Assert.Same(c,Lookup(c.Id));
        b.Id=a.Id; // same object, external ID mutation
        Assert.Same(b,Lookup(a.Id));
        Assert.Null(Lookup(b.Id+10000));
        list.RemoveAt(0); // count changes
        Assert.Null(Lookup(a.Id));
        list.Add(a);
        Assert.Same(a,Lookup(a.Id));
        list.Clear();
        Assert.Null(Lookup(a.Id));
    }

    [Fact]
    public void Vehicle_index_handles_public_replacement_and_miss_without_advance()
    {
        var sim=new CitySimulation(42);
        var find=typeof(CitySimulation).GetMethod("VehicleById",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Vehicle? Lookup(int id)=>(Vehicle?)find.Invoke(sim,new object?[]{id});
        var list=sim.State.Vehicles;
        var original=list[0];
        var replacement=new Vehicle{Id=sim.State.NextId++,Status=VehicleStatus.Outside};
        Assert.Same(original,Lookup(original.Id));
        Assert.Null(Lookup(replacement.Id));
        list[0]=replacement; // same count and same public List<T> instance
        Assert.Same(replacement,Lookup(replacement.Id));
        Assert.Null(Lookup(original.Id));
    }
}
