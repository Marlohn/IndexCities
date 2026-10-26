using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only adversarials for the opt-in reservation-owner superset.
// The public State is mutable between Advance(2) calls. This exercises
// exactly that boundary, plus the same-slice vehicle-count fallback.
public sealed class Pr57SiteOwnerRosterAdversarial
{
    static readonly BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly MethodInfo Prepare=typeof(CitySimulation).GetMethod("Pr57PrepareSiteOwnerRoster",PrivateInstance)!;
    static readonly MethodInfo Travel=typeof(CitySimulation).GetMethod("SafeSiteTravel",PrivateInstance)!;
    static readonly FieldInfo Roster=typeof(CitySimulation).GetField("_pr57SiteOwnerRoster",PrivateInstance)!;

    static bool Enabled=>Environment.GetEnvironmentVariable("PR57_SITE_OWNER_ROSTER")=="1";

    static List<Vehicle> Owners(CitySimulation sim)=>(List<Vehicle>)Roster.GetValue(sim)!;

    static double SiteTravel(CitySimulation sim,Vehicle mover)
        =>(double)Travel.Invoke(sim,[mover,new Point(0,8),8.0,true])!;

    // Nulling the roster forces the original complete State.Vehicles scan,
    // without changing the state, vehicle order or the queried predicate.
    static double FullScan(CitySimulation sim,Vehicle mover)
    {
        var roster=Roster.GetValue(sim);
        Roster.SetValue(sim,null);
        try{return SiteTravel(sim,mover);}
        finally{Roster.SetValue(sim,roster);}
    }

    [Fact]
    public void Public_boundary_rebuild_includes_every_special_owner_and_preserves_order()
    {
        if(!Enabled)return; // The paired CI runs this test again with the flag on.
        var sim=new CitySimulation(42);
        sim.State.Vehicles.Clear();
        var mover=new Vehicle{Id=900001,Position=new(0,0),Heading=0,Status=VehicleStatus.Travelling};
        var car=new Vehicle{Id=900002,PassengerCar=true,Position=new(0,4),Heading=0,Status=VehicleStatus.Parked};
        sim.State.Vehicles.AddRange([mover,car]);

        void AssertRoster(bool includesCar)
        {
            Prepare.Invoke(sim,null); // Next unsplit slice / public quantum.
            Assert.Equal(sim.State.Vehicles.Where(v=>!v.PassengerCar||
                v.Machine!=PavingMachine.None||v.SitePath.Count>0||
                v.ManoeuvreDock!=null||v.ShipmentId!=null).Select(v=>v.Id),
                Owners(sim).Select(v=>v.Id));
            Assert.Equal(includesCar,Owners(sim).Contains(car));
        }

        AssertRoster(false);
        Assert.Equal(8.0,SiteTravel(sim,mover));
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));

        // A vehicle externally reclassified before the next public quantum
        // must be seen by both the roster and the original machine-body test.
        car.Machine=PavingMachine.Paver;
        AssertRoster(true);
        Assert.Equal(0.0,SiteTravel(sim,mover));
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));

        car.Machine=PavingMachine.None;
        car.SitePath.Add(new Point(0,5));
        AssertRoster(true);
        car.SitePath.Clear();
        car.ManoeuvreDock=new Point(0,5);
        AssertRoster(true);
        car.ManoeuvreDock=null;
        car.ShipmentId=123;
        AssertRoster(true);
        car.ShipmentId=null;
        car.PassengerCar=false;
        AssertRoster(true);
        sim.State.Vehicles.Reverse(); // Same-count public reordering between calls.
        AssertRoster(true);
        car.PassengerCar=true;
        AssertRoster(false);
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));
    }

    [Fact]
    public void Same_slice_vehicle_count_change_falls_back_to_complete_physics_scan()
    {
        if(!Enabled)return;
        var sim=new CitySimulation(42);
        sim.State.Vehicles.Clear();
        var mover=new Vehicle{Id=900011,Position=new(0,0),Heading=0,Status=VehicleStatus.Travelling};
        var car=new Vehicle{Id=900012,PassengerCar=true,Position=new(0,4),Heading=0,Status=VehicleStatus.Parked};
        sim.State.Vehicles.AddRange([mover,car]);
        Prepare.Invoke(sim,null);
        Assert.DoesNotContain(car,Owners(sim));
        Assert.Equal(8.0,SiteTravel(sim,mover));

        // A new owner appears after the roster was prepared but before the
        // next SafeSiteTravel: count mismatch must force the original scan.
        var machine=new Vehicle{Id=900013,Machine=PavingMachine.Paver,
            Status=VehicleStatus.Parked,Position=new(0,4),Heading=0};
        sim.State.Vehicles.Add(machine);
        Assert.Equal(0.0,SiteTravel(sim,mover));
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));

        // At the next slice, the superset is rebuilt with the new owner.
        Prepare.Invoke(sim,null);
        Assert.Contains(machine,Owners(sim));
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));
    }
    [Fact]
    public void Same_count_list_replacement_reordering_and_swap_invalidate_in_slice_roster()
    {
        if(!Enabled)return;
        var sim=new CitySimulation(42);
        sim.State.Vehicles.Clear();
        var mover=new Vehicle{Id=900021,Position=new(0,0),Heading=0,Status=VehicleStatus.Travelling};
        var car=new Vehicle{Id=900022,PassengerCar=true,Position=new(0,4),Heading=0,Status=VehicleStatus.Parked};
        var machine=new Vehicle{Id=900023,Machine=PavingMachine.Paver,
            Status=VehicleStatus.Parked,Position=new(0,4),Heading=0};
        sim.State.Vehicles.AddRange([mover,car]);
        Prepare.Invoke(sim,null);
        Assert.DoesNotContain(car,Owners(sim));
        for(var i=0;i<6;i++)Assert.Equal(8.0,SiteTravel(sim,mover));
        Assert.NotNull(Roster.GetValue(sim)); // Exhausting the guard must not invalidate it.

        // Count is unchanged: the former count-only guard missed this machine.
        sim.State.Vehicles[1]=machine;
        Assert.Equal(0.0,SiteTravel(sim,mover));
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));
        Assert.Null(Roster.GetValue(sim));

        sim.State.Vehicles[1]=car;
        Prepare.Invoke(sim,null);
        sim.State.Vehicles.Reverse();
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));
        Assert.Null(Roster.GetValue(sim)); // Reordering also invalidates.

        Prepare.Invoke(sim,null);
        sim.State.Vehicles.Remove(car);
        sim.State.Vehicles.Add(machine); // Two edits, same final count.
        Assert.Equal(0.0,SiteTravel(sim,mover));
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));
        Assert.Null(Roster.GetValue(sim));

        // Public replacement of the entire list preserves Count but not identity.
        sim.State.Vehicles=[mover,car];
        Prepare.Invoke(sim,null);
        sim.State.Vehicles=[mover,machine];
        Assert.Equal(0.0,SiteTravel(sim,mover));
        Assert.Equal(FullScan(sim,mover),SiteTravel(sim,mover));
    }


    [Fact]
    public void In_slice_admission_of_excluded_car_invalidates_reservation_roster()
    {
        if(!Enabled)return;
        // Build a real drivable route: an empty route cannot reserve a
        // physical entry sweep, so it would not test the missed-owner risk.
        var initial=new CitySimulation(42,new(){ExternalFamilyCandidates=0});
        for(var z=0;z<=20;z+=4)
            initial.State.Roads.Add(new(){Id=initial.State.NextId++,Position=new(0,z)});
        var sim=new CitySimulation(initial.State);
        sim.State.Vehicles.Clear();
        var mover=new Vehicle{Id=900031,Position=new(0,0),Heading=0,Status=VehicleStatus.Travelling};
        var car=new Vehicle{Id=900032,PassengerCar=true,Position=new(0,4),
            Heading=0,Status=VehicleStatus.Travelling,
            Route=sim.State.Roads.Where(r=>r.Position.X==0&&r.Position.Z>=0&&r.Position.Z<=20)
                .OrderBy(r=>r.Position.Z).Select(r=>r.Id).ToList(),RouteIndex=1};
        sim.State.Vehicles.AddRange([mover,car]);
        Prepare.Invoke(sim,null);
        Assert.DoesNotContain(car,Owners(sim));
        Assert.Equal(8.0,SiteTravel(sim,mover));

        // Exercise the actual internal admission setter after roster creation,
        // without editing the vehicle list or advancing to the next quantum.
        var admit=typeof(CitySimulation).GetMethod("AdmitManoeuvre",PrivateInstance)!;
        var way=new List<(Vehicle Body,Point Position,double Heading)>();
        var spaces=new List<RoadNode>();
        var limit=new LocalMovementAdmission{Route=car.Route.ToList(),
            SitePath=car.SitePath.ToList(),EndRouteIndex=0,ReachesDock=true};
        var accepted=(bool)admit.Invoke(sim,[car,way,spaces,new Point(0,12),false,limit])!;
        Assert.True(accepted);
        Assert.NotNull(car.ManoeuvreDock);
        Assert.Null(Roster.GetValue(sim)); // Exact fallback within the same slice.

        // The original scan is the oracle for this synthetic admission.
        // This fixture proves in-slice invalidation, not a collision at this
        // particular pose; integrated saves separately exercise real traffic.
        var full=FullScan(sim,mover);
        Assert.Equal(full,SiteTravel(sim,mover));
    }
}
