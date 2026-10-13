using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class MaintenanceTests
{
    static CitySimulation Founded(){var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);Assert.True(sim.PlaceRoad(new(-192,0),new(-80,0)).Success);sim.Advance(7200);Assert.True(sim.State.Buildings.Single().Completed);return sim;}
    [Fact] public void Maintenance_requires_delivered_materials_and_present_paid_external_crew()
    {
        var sim=Founded();var hall=sim.State.Buildings.Single();hall.MaintenanceDueAt=sim.State.ElapsedSeconds-1;hall.MaintenanceRatio=.5;var roads=sim.State.Roads.Where(r=>!r.Highway).Take(3).ToList();foreach(var road in roads){road.MaintenanceDueAt=hall.MaintenanceDueAt;road.MaintenanceRatio=.7;}sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);
        var maintenance=sim.State.Projects.Where(p=>p.IsMaintenance).ToList();Assert.NotEmpty(maintenance);Assert.All(maintenance,p=>Assert.Equal(0,p.Progress));Assert.True(hall.MaintenanceRatio<=.5);
        for(var i=0;i<8&&maintenance.Any(p=>p.Status!=WorkStatus.Completed);i++){sim.Advance(1800);sim.CheckInvariants();}
        Assert.All(maintenance,p=>Assert.Equal(WorkStatus.Completed,p.Status));Assert.Equal(1,hall.MaintenanceRatio);Assert.All(roads,r=>Assert.Equal(1,r.MaintenanceRatio));Assert.All(maintenance,p=>Assert.All(p.Required,k=>Assert.Equal(k.Value,p.Delivered[k.Key],5)));
        Assert.Contains(sim.State.Ledger,l=>l.Category.StartsWith("Manutenção /")&&l.To=="global"&&l.Amount>0);Assert.Equal(sim.SaveToJson(),CitySimulation.FromJson(sim.SaveToJson()).SaveToJson());Assert.False(sim.CancelProject(maintenance[0].Id).Success);
    }
    [Fact] public void No_cash_does_not_fake_maintenance_and_recovery_uses_real_loan_and_deliveries()
    {
        var sim=Founded();var hall=sim.State.Buildings.Single();sim.State.Wallets["global"]+=sim.CityBalance;sim.State.Wallets["city"]=0;hall.MaintenanceDueAt=sim.State.ElapsedSeconds-1;sim.State.EconomyNextAt=sim.State.ElapsedSeconds;var total=sim.MonetaryTotal;sim.Advance(86400);
        Assert.True(hall.MaintenanceRatio>0&&hall.MaintenanceRatio<1);Assert.DoesNotContain(sim.State.Projects,p=>p.IsMaintenance);Assert.Equal(total,sim.MonetaryTotal);Assert.Equal(0,sim.CityBalance);
        Assert.True(sim.Borrow(10000).Success);sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);var project=Assert.Single(sim.State.Projects,p=>p.IsMaintenance&&p.BuildingId==hall.Id);Assert.Equal(0,project.Progress);Assert.True(hall.MaintenanceRatio<1);
        sim.Advance(14400);Assert.Equal(WorkStatus.Completed,project.Status);Assert.Equal(1,hall.MaintenanceRatio);sim.CheckInvariants();
    }
}
