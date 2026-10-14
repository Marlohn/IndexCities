using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

public sealed class ServiceDiagnosticsTests
{
    [Fact] public void Schedule_does_not_count_hiring_as_arrival_and_queries_preserve_the_city()
    {
        // Controlled diagnostic fixture, not evidence of a live city's activity.
        var initial=new CitySimulation(42);
        var intake=new Building{Id=1000,Kind=BuildingKind.WaterIntake,Position=new(0,40),Completed=true};
        initial.State.Buildings.Add(intake);var sim=new CitySimulation(initial.State);
        sim.State.Now=new DateTime(2026,1,5,8,0,0);
        Citizen Worker(int id,int shift,Point position)=>new(){Id=id,Age=30,Resident=true,Alive=true,WorkplaceId=intake.Id,Shift=shift,WeeklyDayOff=5,Position=position};
        var here=Worker(1001,0,intake.Door);var approaching=Worker(1002,0,new(0,100));
        var otherShift=Worker(1003,1,intake.Door);var driving=Worker(1004,0,intake.Door);driving.VehicleId=sim.State.Vehicles.First().Id;
        var off=Worker(1005,0,intake.Door);off.WeeklyDayOff=1;
        var dead=Worker(1006,0,intake.Door);dead.Alive=false;
        var exterior=Worker(1007,0,intake.Door);exterior.Resident=false;
        sim.State.Citizens.AddRange([here,approaching,otherShift,driving,off,dead,exterior]);
        var before=sim.SaveToJson();
        Assert.Equal(3,sim.WorkersScheduled(intake));Assert.Equal(1,sim.WorkersPresent(intake));Assert.Equal(.5,sim.ServiceStaffFactor(intake));
        Assert.True(sim.WorkerPresentAt(here,intake));Assert.False(sim.WorkerPresentAt(driving,intake));
        Assert.Equal(new[]{1001,1002,1004},sim.ScheduledWorkers(intake).Select(c=>c.Id));
        Assert.Equal(before,sim.SaveToJson());
        approaching.Position=intake.Door;
        Assert.Equal(3,sim.WorkersScheduled(intake));Assert.Equal(2,sim.WorkersPresent(intake));Assert.Equal(1,sim.ServiceStaffFactor(intake));
        sim.State.Now=new DateTime(2026,1,5,14,0,0);
        Assert.Equal(1,sim.WorkersScheduled(intake));Assert.Equal(1,sim.WorkersPresent(intake));
    }

    [Fact] public void Water_source_query_uses_the_same_real_band_and_does_not_confer_access_or_capacity()
    {
        var sim=new CitySimulation(42);sim.State.SchemaVersion=1;sim.State.LakeBoundary=null;sim.State.LakeCentre=new(0,0);sim.State.LakeRadius=40;
        var intake=new Building{Id=1000,Kind=BuildingKind.WaterIntake,Completed=true,Position=new(62,0)};
        sim.State.Buildings.Add(intake);var before=sim.SaveToJson();
        Assert.True(sim.HasNaturalWaterSource(intake));Assert.False(sim.HasExteriorAccess(intake));Assert.Equal(0,intake.OperationalCapacity);
        Assert.Equal(before,sim.SaveToJson());
        intake.Position=new(62.001,0);Assert.False(sim.HasNaturalWaterSource(intake));
        intake.Position=new(18,0);Assert.True(sim.HasNaturalWaterSource(intake));
        intake.Kind=BuildingKind.House;Assert.False(sim.HasNaturalWaterSource(intake));
    }

    [Fact] public void Treatment_potential_exposes_the_existing_staff_energy_and_pollution_limits_without_supplying_water()
    {
        // Controlled diagnostic state; no resources or operational capacity are granted by queries.
        var initial=new CitySimulation(42);
        var roadPosition=new Point(initial.State.Entry.X+64+initial.State.Tuning.RoadStep,0);
        initial.State.Roads.Add(new RoadNode{Id=10000,Position=roadPosition});
        var treatment=new Building{Id=1000,Kind=BuildingKind.WaterTreatment,Position=roadPosition+new Point(0,13),Completed=true,MaintenanceRatio=.5,ElectricityRatio=1,OperationalCapacity=27};
        initial.State.Buildings.Add(treatment);var sim=new CitySimulation(initial.State);
        sim.State.Now=new DateTime(2026,1,5,8,0,0);sim.State.LakePollution=2;
        foreach(var id in new[]{1001,1002})sim.State.Citizens.Add(new Citizen{Id=id,Age=30,Alive=true,Resident=true,WorkplaceId=treatment.Id,Shift=0,WeeklyDayOff=5,Position=treatment.Door});
        Assert.True(sim.HasExteriorAccess(treatment));var before=sim.SaveToJson();
        Assert.Equal(2,CitySimulation.ServiceStaffReference(treatment));
        Assert.Equal(208.695652173913,sim.WaterTreatmentPotential(treatment),10);
        Assert.Equal(sim.WaterTreatmentPotential(treatment),sim.CurrentWaterTreatmentPotential);
        Assert.Equal(.434782608695652,sim.WaterPollutionTreatmentFactor,12);
        Assert.Equal(27,treatment.OperationalCapacity);Assert.Equal(0,sim.State.PotableCapacity);Assert.Equal(before,sim.SaveToJson());
        treatment.Completed=false;Assert.Equal(0,sim.WaterTreatmentPotential(treatment));
        treatment.Completed=true;treatment.Removed=true;Assert.Equal(0,sim.CurrentWaterTreatmentPotential);
    }
}
