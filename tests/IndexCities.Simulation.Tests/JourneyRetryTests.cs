using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class JourneyRetryTests
{
    static readonly MethodInfo RebuildRoadGraph=typeof(CitySimulation).GetMethod("RebuildRoadGraph",BindingFlags.Instance|BindingFlags.NonPublic)!;

    static (CitySimulation Sim,Citizen Walker,Building Home) City(bool connected=false,bool car=false)
    {
        var initial=new CitySimulation(42);
        for(var step=0;step<=15;step++)if(connected||step!=8)
            initial.State.Roads.Add(new(){Id=1000+step,Position=new(0,40+step*4),Crosswalk=step==1});
        var home=new Building{Id=1100,Kind=BuildingKind.House,Position=new(-7,100),Completed=true,FamilyId=1200};
        var walker=new Citizen{Id=1300,FamilyId=1200,Age=30,Resident=true,InsideMap=true,Position=new(-5.5,44),PathRoadRevision=-1};
        initial.State.Buildings.Add(home);initial.State.Citizens.Add(walker);
        initial.State.Families.Add(new(){Id=1200,HomeId=home.Id,Members=[walker.Id],Pantry=100});
        initial.State.Wallets[walker.Account]=0;initial.State.EconomyNextAt=double.MaxValue;
        if(car)initial.State.Vehicles.Add(new(){Id=1400,PassengerCar=true,OwnerAccount=walker.Account,Position=walker.Position,Status=VehicleStatus.Parked,Fuel=30});
        return(new(initial.State),walker,home);
    }

    static void Reconnect(CitySimulation sim)
    {
        sim.State.Roads.Add(new(){Id=1008,Position=new(0,72)});
        RebuildRoadGraph.Invoke(sim,[true]);
    }

    static void WaitUntilRetry(CitySimulation sim,Citizen walker)
    {
        var due=walker.NextDecisionAt;var activity=walker.Activity;
        while(sim.State.ElapsedSeconds<due){
            sim.Advance(2);
            Assert.Equal(due,walker.NextDecisionAt);Assert.Equal(activity,walker.Activity);
            Assert.False(walker.Moving);
        }
        // Decisions run at the start of a step, before its two seconds elapse.
        Assert.Equal(due,sim.State.ElapsedSeconds);sim.Advance(2);
        Assert.Equal(due+300,walker.NextDecisionAt);Assert.StartsWith("Sem rota",walker.Activity);
        Assert.Equal(sim.State.RoadRevision,walker.PathRoadRevision);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Failed_walk_and_car_fallback_wait_five_minutes_instead_of_retrying_each_step(bool car)
    {
        var (sim,walker,home)=City(car:car);Assert.Empty(sim.PedestrianRoute(walker.Position,home));
        sim.Advance(2);Assert.StartsWith("Sem rota",walker.Activity);Assert.Equal(300,walker.NextDecisionAt);
        WaitUntilRetry(sim,walker);sim.CheckInvariants();
    }

    [Fact]
    public void Road_change_that_still_leaves_no_route_retries_once_and_restarts_the_wait()
    {
        var (sim,walker,_)=City();sim.Advance(2);sim.Advance(60);
        sim.State.Roads.Add(new(){Id=1500,Position=new(80,80)});RebuildRoadGraph.Invoke(sim,[true]);
        var retryAt=sim.State.ElapsedSeconds;sim.Advance(2);
        Assert.Equal(retryAt+300,walker.NextDecisionAt);
        Assert.Equal(sim.State.RoadRevision,walker.PathRoadRevision);
        WaitUntilRetry(sim,walker);sim.CheckInvariants();
    }

    [Fact]
    public void Reconnected_street_starts_the_trip_on_the_next_step_before_the_deadline()
    {
        var (sim,walker,home)=City();sim.Advance(2);sim.Advance(60);
        var due=walker.NextDecisionAt;Reconnect(sim);Assert.NotEmpty(sim.PedestrianRoute(walker.Position,home));
        sim.Advance(2);
        Assert.True(sim.State.ElapsedSeconds<due);Assert.True(walker.Moving);
        Assert.Equal(home.Id,walker.DestinationId);Assert.Equal(sim.State.RoadRevision,walker.PathRoadRevision);
        Assert.Equal("Voltando para casa",walker.Activity);sim.CheckInvariants();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Removed_route_obeys_the_same_wait_and_recovers_immediately_when_reconnected(bool reconnect)
    {
        var (sim,walker,_)=City(connected:true);sim.Advance(2);Assert.True(walker.Moving);
        sim.State.Roads.RemoveAll(r=>r.Id==1008);RebuildRoadGraph.Invoke(sim,[true]);sim.Advance(2);
        Assert.Equal("Viagem interrompida: rota removida",walker.Activity);Assert.False(walker.Moving);
        var due=walker.NextDecisionAt;Assert.Equal(302,due);
        if(!reconnect)WaitUntilRetry(sim,walker);
        else{
            sim.Advance(60);Assert.Equal(due,walker.NextDecisionAt);Reconnect(sim);sim.Advance(2);
            Assert.True(sim.State.ElapsedSeconds<due);Assert.True(walker.Moving);
            Assert.Equal("Voltando para casa",walker.Activity);
        }
        Assert.Equal(sim.State.RoadRevision,walker.PathRoadRevision);sim.CheckInvariants();
    }
}
