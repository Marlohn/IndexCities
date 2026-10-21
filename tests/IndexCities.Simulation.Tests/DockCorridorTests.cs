using System.Reflection;
using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// ARCHITECTURE: exclusive dock sweeps, atomic admission and physical release.
public sealed class DockCorridorTests
{
    static CitySimulation OppositeDocks(bool junctionNearExit=false)
    {
        var initial=new CitySimulation(42,new(){ExternalFamilyCandidates=0});
        for(var x=-256;x<=-120;x+=4)initial.State.Roads.Add(new(){Id=initial.State.NextId++,Position=new(x,40)});
        if(junctionNearExit)initial.State.Roads.Add(new(){Id=initial.State.NextId++,Position=new(-172,44)});
        var sim=new CitySimulation(initial.State);sim.State.EconomyNextAt=1e9;
        int N(int x)=>sim.State.Roads.Single(r=>r.Position==new Point(x,40)).Id;
        void Setup(Vehicle v,Point at,List<int> route){
            v.Status=VehicleStatus.Travelling;v.Position=at;v.Heading=-Math.PI/2;
            v.Route=route;v.RouteIndex=0;v.Fuel=40;v.ConstructionAccess=true;
        }
        // Continue to the exterior: ending on the lane would turn the leader
        // into an unloading body at an artificial destination.
        Setup(sim.State.Vehicles[0],new(-160,46.75),Enumerable.Range(0,25).Select(i=>N(-160-4*i)).ToList());
        Setup(sim.State.Vehicles[1],new(-168,33.25),Enumerable.Range(0,23).Select(i=>N(-168-4*i)).ToList());
        return sim;
    }
    static List<(Vehicle Body,Point Position,double Heading)> Exit(CitySimulation sim,Vehicle v)
        =>(List<(Vehicle,Point,double)>)typeof(CitySimulation).GetMethods(BindingFlags.Instance|BindingFlags.NonPublic)
            .Single(m=>m.Name=="WayOut"&&m.GetParameters().Length==2).Invoke(sim,[v,null])!;
    static bool Overlaps(Vehicle body,IEnumerable<(Vehicle Body,Point Position,double Heading)> poses)
        =>poses.Any(p=>MachineExitTests.Penetration(body,new(){Position=p.Position,Heading=p.Heading})>0);

    [Fact]
    public void Conflicting_opposite_exits_acquire_whole_and_wait_at_the_dock_until_release()
    {
        var sim=OppositeDocks();var a=sim.State.Vehicles[0];var b=sim.State.Vehicles[1];var dock=b.Position;
        var aWay=Exit(sim,a);var bWay=Exit(sim,b);
        Assert.True(aWay.Any(p=>Overlaps(new(){Position=p.Position,Heading=p.Heading},bWay)),"fixture must have conflicting physical sweeps");
        var sawOwner=false;var released=false;var bLeft=false;
        for(var step=0;step<100;step++){
            sim.Advance(.1);
            Assert.True(MachineExitTests.Penetration(a,b)<=0,$"contact at step {step}");
            if(a.ManoeuvreDock!=null){sawOwner=true;Assert.Null(b.ManoeuvreDock);Assert.Equal(dock,b.Position);}
            else if(sawOwner)released=true;
            if(b.Position!=dock){Assert.True(released,"second exit moved before first physically released");bLeft=true;}
        }
        Assert.True(sawOwner&&released&&bLeft,"both complete exits must progress");
    }

    [Fact]
    public void New_exit_sweep_starts_at_the_body_even_if_a_reroute_retained_the_old_dock_identity()
    {
        var sim=OppositeDocks();var a=sim.State.Vehicles[0];
        sim.State.Vehicles[1].Status=VehicleStatus.Outside;
        var start=a.Position;var oldDock=new Point(-120,33.25);
        a.ManoeuvreDock=oldDock;a.DockPosition=oldDock;a.ManoeuvreDeparture=false;
        var way=Exit(sim,a);
        var admit=typeof(CitySimulation).GetMethod("AdmitManoeuvre",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Assert.True((bool)admit.Invoke(sim,[a,way,new List<RoadNode>(),oldDock,true,null])!);
        Assert.Equal(start,a.DockCorridor[0].Position);
        Assert.DoesNotContain(a.DockCorridor,p=>p.Position==oldDock);
    }

    [Fact]
    public void A_matching_destination_does_not_keep_an_expired_departure_after_physical_clearance()
    {
        var sim=OppositeDocks();sim.Advance(.8);var a=sim.State.Vehicles[0];
        Assert.NotEmpty(a.DockCorridor);
        // Component fixture for the state after a route prefix was passed and
        // cargo was redirected to the same destination as the old dock tag.
        a.Position=new(-180,41.65);a.RouteIndex=5;a.DockPosition=a.ManoeuvreDock;
        var at=a.Position;
        typeof(CitySimulation).GetMethod("ReleaseManoeuvre",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(sim,[a]);
        Assert.Null(a.ManoeuvreDock);Assert.Empty(a.DockCorridor);Assert.Equal(at,a.Position);
    }

    [Fact]
    public void Machine_does_not_enter_an_admitted_dock_sweep_just_because_no_body_is_there_yet()
    {
        var sim=OppositeDocks();sim.Advance(.1);
        Assert.NotNull(sim.State.Vehicles[0].ManoeuvreDock);
        var machine=sim.State.Vehicles[1];
        machine.Machine=PavingMachine.Roller;machine.MachinePhase=MachinePhase.LeavingSite;
        machine.Status=VehicleStatus.Parked;machine.Heading=0;
        machine.SitePath=[new(-168,41.65)];machine.SitePathIndex=0;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        object? Call(string name,params object?[] values)=>typeof(CitySimulation).GetMethods(flags)
            .Single(m=>m.Name==name&&m.GetParameters().Length==values.Length).Invoke(sim,values);
        var ahead=Call("MachinePathAhead",machine);
        Assert.Null(Call("BodyOnWay",machine,ahead));
        Assert.False((bool)Call("MachineMayEnter",machine)!);
        Assert.Empty(machine.ManoeuvreSpaces);
        Assert.Empty((List<(Vehicle Body,Point Position,double Heading)>)Call("MachineReservation",machine)!);
    }

    [Fact]
    public void Exit_acquires_next_junction_when_the_previous_lane_pose_still_occupies_its_dock_sweep()
    {
        var sim=OppositeDocks(true);var a=sim.State.Vehicles[0];
        sim.State.Vehicles[1].Status=VehicleStatus.Outside;
        sim.State.Vehicles[1].Position=sim.State.Entry;
        sim.Advance(.1);
        var junction=sim.State.Roads.Single(r=>r.Position==new Point(-172,40));
        Assert.NotNull(a.ManoeuvreDock);
        Assert.Contains(junction.Id,a.ManoeuvreSpaces);
    }

    [Fact]
    public void Ownership_survives_the_turn_to_the_lane_until_body_clears_original_dock_sweep()
    {
        var sim=OppositeDocks();var a=sim.State.Vehicles[0];
        // Independent oracle: retain only the dock -> lane part of the original
        // sweep, excluding the onward horizontal trip to the safe waiting pose.
        var corridor=Exit(sim,a).Where(p=>p.Position.Z>41.65).ToList();
        var sawLane=false;var released=false;
        for(var step=0;step<100;step++){
            sim.Advance(.1);
            if(a.RouteIndex>0&&Overlaps(a,corridor)){sawLane=true;Assert.NotNull(a.ManoeuvreDock);}
            if(sawLane&&a.ManoeuvreDock==null){Assert.False(Overlaps(a,corridor));released=true;break;}
        }
        Assert.True(sawLane&&released,"must observe occupied lane transition and later physical release");
    }

    [Fact]
    public void Save_during_exit_retains_owner_and_other_dock_wait_and_replays_identically()
    {
        var original=OppositeDocks();original.Advance(.8);
        Assert.NotNull(original.State.Vehicles[0].ManoeuvreDock);
        var loaded=CitySimulation.FromJson(original.SaveToJson());
        Assert.NotEmpty(loaded.State.Vehicles[0].DockCorridor);
        for(var step=0;step<100;step++){
            original.Advance(.1);loaded.Advance(.1);
            Assert.Equal(original.SaveToJson(),loaded.SaveToJson());
            var a=loaded.State.Vehicles[0];var b=loaded.State.Vehicles[1];
            Assert.True(MachineExitTests.Penetration(a,b)<=0);
            if(a.ManoeuvreDock!=null){Assert.Null(b.ManoeuvreDock);Assert.Equal(new Point(-168,33.25),b.Position);}
        }
    }
}
