using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// ARCHITECTURE: a local admission ends at the first safe waiting pose and
// cannot authorize a different route or an entire journey to the same dock.
public sealed class LocalAdmissionTests
{
    static (CitySimulation Sim,Vehicle Entering,Vehicle Owner) City()
    {
        var initial=new CitySimulation(42,new(){ExternalFamilyCandidates=0});
        for(var x=-188;x<=-144;x+=4)initial.State.Roads.Add(new(){Id=initial.State.NextId++,Position=new(x,0)});
        for(var z=4;z<=40;z+=4)initial.State.Roads.Add(new(){Id=initial.State.NextId++,Position=new(-156,z)});
        var sim=new CitySimulation(initial.State);sim.State.EconomyNextAt=1e9;
        int Node(double x,double z)=>sim.State.Roads.Single(r=>r.Position==new Point(x,z)).Id;
        var entering=sim.State.Vehicles[0];var owner=sim.State.Vehicles[1];
        entering.Status=VehicleStatus.Travelling;entering.Position=new(-164,-1.65);entering.Heading=Math.PI/2;entering.Fuel=40;
        entering.Route=[Node(-168,0),Node(-164,0),Node(-160,0),Node(-156,0),..Enumerable.Range(1,8).Select(i=>Node(-156,i*4))];entering.RouteIndex=2;
        entering.DockPosition=new(-149.6,34.13);
        var shipment=new Shipment{Id=sim.State.NextId++,VehicleId=entering.Id,PickedUp=true};
        sim.State.Shipments.Add(shipment);entering.ShipmentId=shipment.Id;
        owner.Status=VehicleStatus.Unloading;owner.Position=new(-150.4,11.74);owner.DockPosition=owner.Position;owner.Heading=0;owner.Fuel=40;owner.LoadTimer=60;
        owner.Route=[Node(-168,0),Node(-164,0),Node(-160,0),Node(-156,0),Node(-156,4),Node(-156,8),Node(-156,12)];owner.RouteIndex=owner.Route.Count;
        return(sim,entering,owner);
    }

    [Fact]
    public void Admission_before_a_junction_with_more_than_four_nodes_left_stops_at_the_first_safe_place()
    {
        var (sim,entering,_)=City();var route=entering.Route.ToArray();
        sim.Advance(.1);
        var admission=Assert.IsType<LocalMovementAdmission>(entering.MovementAdmission);
        Assert.True(entering.Route.Count-entering.RouteIndex>4);
        Assert.True(admission.EndRouteIndex>entering.RouteIndex);
        Assert.True(admission.EndRouteIndex<entering.Route.Count-1);
        Assert.False(admission.ReachesDock);
        Assert.Equal(route,entering.Route);
        Assert.NotEmpty(entering.ManoeuvreSpaces);
        Assert.All(entering.ManoeuvreSpaces,id=>Assert.Contains(id,route.Take(admission.EndRouteIndex+1)));
        for(var t=0;t<12;t++)sim.Advance(.5);
        Assert.True(entering.Position.Z>20,entering.Cause);
        sim.CheckInvariants();
    }

    [Fact]
    public void Save_at_early_admission_preserves_the_local_limit_and_continues_identically()
    {
        var (a,entering,_)=City();a.Advance(.1);
        Assert.NotNull(entering.MovementAdmission);
        var b=CitySimulation.FromJson(a.SaveToJson());
        var loaded=b.State.Vehicles.Single(v=>v.Id==entering.Id);
        Assert.Equal(entering.MovementAdmission!.EndRouteIndex,loaded.MovementAdmission!.EndRouteIndex);
        Assert.Equal(entering.MovementAdmission.Route,loaded.MovementAdmission.Route);
        for(var t=0;t<24;t++){a.Advance(.5);b.Advance(.5);}
        Assert.Equal(a.SaveToJson(),b.SaveToJson());a.CheckInvariants();
    }

    [Fact]
    public void Rerouting_does_not_reuse_an_admission_with_the_same_numeric_endpoint()
    {
        var (sim,entering,_)=City();sim.Advance(.1);
        var admission=entering.MovementAdmission!;var spaces=entering.ManoeuvreSpaces.ToArray();
        // The new destination changes only the future route, keeping the same
        // current index: that index alone is not proof of the admitted path.
        entering.Route=entering.Route.Take(entering.Route.Count-1).ToList();
        var admitted=typeof(CitySimulation).GetMethod("DockAdmitted",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Assert.False((bool)admitted.Invoke(sim,[entering,entering.DockPosition])!);
        Assert.Equal(spaces,entering.ManoeuvreSpaces);
        Assert.NotEqual(entering.Route.Count,admission.Route.Count);
        var loaded=CitySimulation.FromJson(sim.SaveToJson());
        sim.Advance(.1);loaded.Advance(.1);
        Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());
        Assert.NotSame(admission,entering.MovementAdmission);
        Assert.Equal(entering.Route,entering.MovementAdmission!.Route);sim.CheckInvariants();
    }

    [Fact]
    public void An_unadmitted_delivery_does_not_hold_free_through_traffic_for_its_dock_owner()
    {
        var (sim,entering,owner)=City();entering.DockPosition=owner.Position;
        entering.Route=entering.Route.Take(7).ToList(); // this occupied dock's actual access at z=12
        var passing=sim.State.Vehicles[2];passing.Status=VehicleStatus.Travelling;passing.Position=new(-148,1.65);passing.Heading=-Math.PI/2;passing.Fuel=40;
        passing.Route=sim.State.Roads.Where(r=>r.Position.Z==0&&r.Position.X>=-176&&r.Position.X<=-144).OrderByDescending(r=>r.Position.X).Select(r=>r.Id).ToList();passing.RouteIndex=2;
        var before=passing.Position;
        sim.Advance(2);
        Assert.True(passing.Position.Distance(before)>4,passing.Cause);
        Assert.True(entering.Position.Z<8,entering.Cause);
        Assert.Null(entering.MovementAdmission);
        sim.CheckInvariants();
    }
}
