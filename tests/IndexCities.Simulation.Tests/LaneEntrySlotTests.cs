using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC 2026-10-14: use the physical slot for the complete manoeuvre, preserving
// road priority. Fixtures preload the flow instead of exploiting its first gap.
public sealed class LaneEntrySlotTests
{
    static CitySimulation Street(bool mirror=false)
    {
        var initial=new CitySimulation(42);
        for(var x=-40;x<=40;x+=4)initial.State.Roads.Add(new(){Id=1000+x,Position=new(mirror?-x:x,40)});
        var sim=new CitySimulation(initial.State);sim.State.Vehicles.Clear();sim.State.EconomyNextAt=86400;return sim;
    }
    static Vehicle Traffic(int id,double x,bool mirror=false)=>new(){Id=id,External=true,ExternalDriverId="fixture",PassengerCar=true,Position=new(mirror?-x:x,mirror?41.65:38.35),Heading=mirror?-Math.PI/2:Math.PI/2,Status=VehicleStatus.Travelling,Fuel=40,Route=Enumerable.Range(0,21).Select(n=>960+n*4).ToList(),RouteIndex=(int)Math.Ceiling((40+x)/4)};
    static Vehicle Exit(int id=1,bool mirror=false)=>new(){Id=id,External=true,ExternalDriverId="fixture",PassengerCar=true,Position=new(0,mirror?46.75:33.25),Heading=mirror?Math.PI/2:-Math.PI/2,Status=VehicleStatus.Travelling,Fuel=40,Route=Enumerable.Range(0,11).Select(n=>1000-n*4).ToList()};
    [Theory]
    [InlineData(25,.1)][InlineData(25,.5)][InlineData(25,2)]
    [InlineData(26,.1)][InlineData(26,.5)][InlineData(26,2)]
    [InlineData(28,.1)][InlineData(28,.5)][InlineData(28,2)]
    public void A_waiting_lot_entry_uses_a_real_slot_in_preloaded_periodic_flow(double space,double dt)
    {
        foreach(var mirror in new[]{false,true}){
            var sim=Street(mirror);double clock=0,next=0;int id=100;
            void Spawn(){while(next<=clock+1e-9){sim.State.Vehicles.Add(Traffic(id++,-40+7*(clock-next),mirror));next+=space/7;}}
            while(clock<20-1e-9){Spawn();var s=Math.Min(dt,20-clock);sim.Advance(s);clock+=s;}
            var exit=Exit(mirror:mirror);sim.State.Vehicles.Add(exit);bool entered=false;
            while(clock<28-1e-9){Spawn();sim.Advance(dt);clock+=dt;if(exit.RouteIndex>=1)entered=true;}
            Assert.True(entered,exit.Cause);Assert.True(exit.Fuel<40);Assert.Equal(20_000_000,sim.MonetaryTotal);sim.CheckInvariants();
            var restored=CitySimulation.FromJson(sim.SaveToJson());sim.Advance(2);restored.Advance(2);Assert.Equal(sim.SaveToJson(),restored.SaveToJson());
        }
    }
    [Theory][InlineData(.1)][InlineData(.5)][InlineData(2)]
    public void Competing_lot_entries_do_not_project_a_blocked_car_as_free_flow(double dt)
    {
        var sim=Street();double clock=0,next=0;int id=100;
        void Spawn(){while(next<=clock+1e-9){sim.State.Vehicles.Add(Traffic(id++,-40+7*(clock-next)));next+=25.0/7;}}
        while(clock<20-1e-9){Spawn();var s=Math.Min(dt,20-clock);sim.Advance(s);clock+=s;}
        var exits=Enumerable.Range(1,3).Select(id=>Exit(id)).ToList();sim.State.Vehicles.AddRange(exits);var entered=new Dictionary<int,double>();
        while(clock<36-1e-9){Spawn();sim.Advance(dt);clock+=dt;foreach(var v in exits)if(v.RouteIndex>=1)entered.TryAdd(v.Id,clock);}
        Assert.Equal(3,entered.Count);Assert.True(entered[1]<entered[2]&&entered[2]<entered[3]);sim.CheckInvariants();
    }
    [Fact]
    public void A_mobility_event_does_not_repeat_driver_work_or_existing_loading()
    {
        var sim=Street();sim.State.Buildings.Add(new(){Id=700,Kind=BuildingKind.WorksDepot,Position=new(140,140),Completed=true,OwnerAccount="city",SalaryOffer=3600});
        var driver=new Citizen{Id=800,Alive=true,Resident=true,InsideMap=true,Age=30,VehicleId=100,WorkplaceId=700,NextDecisionAt=double.MaxValue};sim.State.Citizens.Add(driver);sim.State.Families.Add(new(){Id=0,Members=[800]});sim.State.Wallets[driver.Account]=0;
        var truck=Traffic(100,-30);truck.PassengerCar=false;truck.External=false;truck.DriverId=800;truck.ExternalDriverId=null;sim.State.Vehicles.AddRange([truck,Exit()]);
        sim.State.Vehicles.Add(new(){Id=900,External=true,Status=VehicleStatus.Unloading,Position=new(130,130),LoadTimer=15,Fuel=40});sim.Advance(2);
        Assert.Equal(2,driver.WorkSecondsToday);Assert.InRange(driver.WagesAccrued,1.9999999999m,2.0000000001m);Assert.Equal(13,sim.State.Vehicles.Single(v=>v.Id==900).LoadTimer);
        Assert.InRange(40-truck.Fuel,.2099999999,.2100000001);Assert.InRange(truck.FuelBurnedLastStep,.2099999999,.2100000001);Assert.Equal(20_000_000,sim.MonetaryTotal);sim.CheckInvariants();
    }
    [Fact]
    public void Cargo_arriving_within_a_refined_step_keeps_its_new_loading_timer()
    {
        var sim=Street();sim.State.Vehicles.Add(Exit());sim.State.Vehicles.Add(new(){Id=900,External=true,Status=VehicleStatus.Travelling,Position=new(0,38.35),Heading=Math.PI/2,Fuel=40,Route=[1000,1004],RouteIndex=2});sim.Advance(2);
        var cargo=sim.State.Vehicles.Single(v=>v.Id==900);Assert.Equal(VehicleStatus.Unloading,cargo.Status);Assert.Equal(15,cargo.LoadTimer);sim.CheckInvariants();
    }
    [Fact]
    public void A_lot_entry_still_yields_to_a_physically_crossing_pedestrian()
    {
        var sim=Street();sim.State.Roads.Single(r=>r.Id==1000).Crosswalk=true;
        var walker=new Citizen{Id=800,Alive=true,Resident=true,InsideMap=true,Age=30,Position=new(0,40),Path=[new(0,40),new(0,47)],PathIndex=1,PathRoadRevision=sim.State.RoadRevision,CrossingNodeId=1000,NextDecisionAt=double.MaxValue};
        sim.State.Citizens.Add(walker);sim.State.Families.Add(new(){Id=0,Members=[800]});sim.State.Wallets[walker.Account]=0;var exit=Exit();var origin=exit.Position;sim.State.Vehicles.Add(exit);sim.Advance(.5);
        Assert.Equal(origin,exit.Position);Assert.Equal(40,exit.Fuel);Assert.InRange(walker.Position.Distance(new(0,40)),.699999,.700001);sim.CheckInvariants();
    }
}
