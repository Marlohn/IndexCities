using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 isolated adversarial experiment, not a game rule or a general regression proof.
// Both external cars begin opposite-direction lot departures at the same instant.
// The longitudinal separation and 7.7m lateral offset follow the observed failure
// geometry, reduced to a straight local road. Failure to reproduce is evidence too.
public sealed class DualExitConflictExperiment
{
    static CitySimulation Pair()
    {
        var initial=new CitySimulation(73);
        for(var z=0;z<=80;z+=4)
            initial.State.Roads.Add(new(){Id=2000+z,Position=new(0,z)});
        var sim=new CitySimulation(initial.State);
        sim.State.Vehicles.Clear();
        sim.State.EconomyNextAt=86400;
        sim.State.Vehicles.Add(new(){
            Id=5183,External=true,ExternalDriverId="pr57",PassengerCar=true,
            Position=new(7.7,47.7),Heading=Math.PI,Status=VehicleStatus.Travelling,
            Fuel=40,Route=Enumerable.Range(0,10).Select(i=>2040-4*i).ToList()
        });
        sim.State.Vehicles.Add(new(){
            Id=5294,External=true,ExternalDriverId="pr57",PassengerCar=true,
            Position=new(7.7,32.3),Heading=0,Status=VehicleStatus.Travelling,
            Fuel=40,Route=Enumerable.Range(0,10).Select(i=>2040+4*i).ToList()
        });
        return sim;
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(2.0)]
    public void Opposed_simultaneous_departures_report_progress_or_mutual_stall(double dt)
    {
        var sim=Pair();
        var first=sim.State.Vehicles.Single(v=>v.Id==5183);
        var second=sim.State.Vehicles.Single(v=>v.Id==5294);
        var originFirst=first.Position;
        var originSecond=second.Position;
        double continuousMutualStall=0,maxMutualStall=0,minSeparation=double.PositiveInfinity;
        for(var step=0;step<(int)Math.Round(30/dt);step++){
            var beforeFirst=first.Position;
            var beforeSecond=second.Position;
            sim.Advance(dt);
            minSeparation=Math.Min(minSeparation,first.Position.Distance(second.Position));
            if(first.Status==VehicleStatus.Travelling&&second.Status==VehicleStatus.Travelling&&
               first.RouteIndex==0&&second.RouteIndex==0&&
               first.Position.Distance(beforeFirst)<.000001&&second.Position.Distance(beforeSecond)<.000001)
                continuousMutualStall+=dt;
            else continuousMutualStall=0;
            maxMutualStall=Math.Max(maxMutualStall,continuousMutualStall);
        }
        Console.WriteLine($"PR57_DUAL_EXIT dt={dt:F1} firstMoved={first.Position.Distance(originFirst):F6} secondMoved={second.Position.Distance(originSecond):F6} firstRoute={first.RouteIndex} secondRoute={second.RouteIndex} maxMutualStall={maxMutualStall:F3} minSeparation={minSeparation:F6} firstCause={first.Cause?.Replace(' ','_')} secondCause={second.Cause?.Replace(' ','_')}");
        Assert.Equal(20_000_000,sim.MonetaryTotal);
        sim.CheckInvariants();
        var save=sim.SaveToJson();
        Assert.Equal(save,CitySimulation.FromJson(save).SaveToJson());
    }
    [Fact]
    public void Fast_scan_modes_match_full_adversarial_checkpoints()
    {
        // Restore exactly the same starting state for all three candidate modes.
        var snapshot=Pair().SaveToJson();
        var oldActive=Environment.GetEnvironmentVariable("PR57_ACTIVE_SCAN");
        var oldSpatial=Environment.GetEnvironmentVariable("PR57_SPATIAL_SCAN");
        var oldBitset=Environment.GetEnvironmentVariable("PR57_BITSET_SCAN");
        var oldFast=Environment.GetEnvironmentVariable("PR57_FAST_SCAN");
        var oldBend=Environment.GetEnvironmentVariable("PR57_BEND_PREFILTER");
        var oldReuse=Environment.GetEnvironmentVariable("PR57_BEND_REUSE");
        try{
            Environment.SetEnvironmentVariable("PR57_ACTIVE_SCAN","1");
            Environment.SetEnvironmentVariable("PR57_SPATIAL_SCAN","0");
            List<string>? reference=null;
            foreach(var mode in new[]{(Fast:"0",Bend:"0",Bitset:"0",Reuse:"0"),(Fast:"1",Bend:"0",Bitset:"0",Reuse:"0"),(Fast:"1",Bend:"0",Bitset:"1",Reuse:"0"),(Fast:"1",Bend:"0",Bitset:"1",Reuse:"1"),(Fast:"1",Bend:"1",Bitset:"0",Reuse:"0")}){
                Environment.SetEnvironmentVariable("PR57_FAST_SCAN",mode.Fast);
                Environment.SetEnvironmentVariable("PR57_BITSET_SCAN",mode.Bitset);
                Environment.SetEnvironmentVariable("PR57_BEND_PREFILTER",mode.Bend);
                Environment.SetEnvironmentVariable("PR57_BEND_REUSE",mode.Reuse);
                var sim=CitySimulation.FromJson(snapshot);
                var checkpoints=new List<string>();
                for(var i=1;i<=60;i++){
                    sim.Advance(.5);
                    if(i%10==0){sim.CheckInvariants();checkpoints.Add(sim.SaveToJson());}
                }
                if(reference is null)reference=checkpoints;
                else Assert.Equal(reference,checkpoints);
            }
        }finally{
            Environment.SetEnvironmentVariable("PR57_ACTIVE_SCAN",oldActive);
            Environment.SetEnvironmentVariable("PR57_SPATIAL_SCAN",oldSpatial);
            Environment.SetEnvironmentVariable("PR57_BITSET_SCAN",oldBitset);
            Environment.SetEnvironmentVariable("PR57_FAST_SCAN",oldFast);
            Environment.SetEnvironmentVariable("PR57_BEND_PREFILTER",oldBend);
            Environment.SetEnvironmentVariable("PR57_BEND_REUSE",oldReuse);
        }
    }

}
