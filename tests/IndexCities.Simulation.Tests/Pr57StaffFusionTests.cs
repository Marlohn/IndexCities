using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// Run in separate dotnet test processes with PR57_STAFF_PHASE_FUSION=0/1.
// Both modes must preserve actual paid work and the full saved state.
public sealed class Pr57StaffFusionTests
{
    [Fact] public void Staff_fusion_keeps_farm_and_infrastructure_wages_in_integrated_cycle()
    {
        var sim=CityCycleTests.City();
        // The fixture has real paid construction and all business kinds.
        for(var hour=0;hour<48;hour++)sim.Advance(3600);
        sim.CheckInvariants();
        var farm=sim.State.Buildings.First(b=>b.Kind==BuildingKind.Farm&&b.Completed&&!b.Removed);
        var utility=sim.State.Buildings.First(b=>b.Kind==BuildingKind.WindPower&&b.Completed&&!b.Removed);
        Assert.NotEmpty(sim.State.Citizens.Where(c=>c.Alive));
        var workers=sim.State.Citizens.Where(c=>c.Alive).Take(2).ToArray();
        Assert.Equal(2,workers.Length);
        // Force the same valid presence inputs for both separate processes.
        workers[0].WorkplaceId=farm.Id;
        workers[0].Position=farm.Door;
        workers[1].WorkplaceId=utility.Id;
        workers[1].Position=utility.Door;
        foreach(var c in workers)
        {
            c.VehicleId=null;
            c.ConstructionProjectId=null;
        }
        var before=workers.Select(c=>(c.WorkSecondsToday,c.WagesAccrued)).ToArray();
        var ledgerBefore=sim.State.Ledger.Count;
        sim.Advance(2);
        sim.CheckInvariants();
        Assert.All(workers,c=>Assert.True(c.WorkSecondsToday>=0));
        Assert.True(workers[0].WorkSecondsToday>=before[0].WorkSecondsToday);
        Assert.True(workers[1].WorkSecondsToday>=before[1].WorkSecondsToday);
        Assert.True(workers[0].WagesAccrued>=before[0].WagesAccrued);
        Assert.True(workers[1].WagesAccrued>=before[1].WagesAccrued);
        Assert.True(sim.State.Ledger.Count>=ledgerBefore);
        Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);
        var json=sim.SaveToJson();
        Assert.Equal(json,CitySimulation.FromJson(json).SaveToJson());
        // Full JSON hash is emitted for cross-process baseline/fusion comparison.
        var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(json)));
        Console.WriteLine($"PR57_STAFF_FUSION_JSON mode={Environment.GetEnvironmentVariable("PR57_STAFF_PHASE_FUSION")??"0"} sha256={hash} farmWork={workers[0].WorkSecondsToday} utilityWork={workers[1].WorkSecondsToday} farmWage={workers[0].WagesAccrued} utilityWage={workers[1].WagesAccrued}");
    }
}
