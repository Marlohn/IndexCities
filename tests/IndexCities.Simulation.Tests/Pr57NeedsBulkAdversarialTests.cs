using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #565: ARTIFICIAL adversarials, not a scale benchmark.
[CollectionDefinition("Pr57NeedsBulkEnvironment", DisableParallelization = true)]
public sealed class Pr57NeedsBulkEnvironmentCollection { }

[Collection("Pr57NeedsBulkEnvironment")]
public sealed class Pr57NeedsBulkAdversarialTests
{
    static CitySimulation Fixture()
    {
        var sim=new CitySimulation(42,new SimulationTuning
        {
            ExternalFamilyCandidates=0,ExternalCompanyCandidates=0,ExternalTruckCount=0
        });
        var home=new Building{Id=800000,Kind=BuildingKind.House,Completed=true,
            Position=new Point(20,20),FamilyId=800001,WaterRatio=1,ElectricityRatio=1};
        sim.State.Buildings.Add(home);
        var family=new Family{Id=800001,HomeId=home.Id,Pantry=2};
        sim.State.Families.Add(family);
        for(var i=0;i<16;i++)
        {
            var c=new Citizen{Id=800100+i,Name="Bulk "+i,FamilyId=family.Id,
                Resident=true,Alive=true,InsideMap=true,Position=home.Door,
                Health=100,HungerHours=i<12?1:13};
            if(i==15){c.HealthUnits=1;c.HungerHours=80;}
            sim.State.Citizens.Add(c);
            family.Members.Add(c.Id);
        }
        sim.State.NextId=900000;
        sim.State.EconomyNextAt=86400;
        return sim;
    }
    static void Edit(CitySimulation sim,int step)
    {
        var home=sim.State.Buildings.Single(b=>b.Id==800000);
        var family=sim.State.Families.Single(f=>f.Id==800001);
        var citizen=sim.State.Citizens.Single(c=>c.Id==800100);
        switch(step)
        {
            case 1: family.Pantry=4;break;
            case 2: home.WaterRatio=.2;home.ElectricityRatio=.4;break;
            case 3: citizen.HungerHours=13;break;
            case 4: citizen.HealthUnits=1;citizen.HungerHours=90;break;
            case 5: home.Position=new Point(21,20);break;
            case 7: family.Pantry=0;break;
            case 8:
                var newcomer=new Citizen{Id=800200,Name="Newcomer",FamilyId=family.Id,
                    Alive=true,Resident=true,InsideMap=true,Position=home.Door,
                    Health=100,HungerHours=1};
                family.Members.Add(newcomer.Id);
                sim.State.Citizens.Add(newcomer);
                break;
            case 9: home.WaterRatio=1;home.ElectricityRatio=1;break;
        }
    }
    [Fact]
    public void Every_two_second_step_preserves_full_json_with_meals_death_edits_and_save_load()
    {
        var original=Environment.GetEnvironmentVariable("PR57_NEEDS_BULK");
        try
        {
            var baseline=Fixture();
            var candidate=CitySimulation.FromJson(baseline.SaveToJson());
            var hits=typeof(CitySimulation).GetField("_pr57NeedsBulkRowsCommitted",
                BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(hits);
            for(var step=0;step<12;step++)
            {
                Edit(baseline,step);Edit(candidate,step);
                Assert.Equal(baseline.SaveToJson(),candidate.SaveToJson());
                Environment.SetEnvironmentVariable("PR57_NEEDS_BULK","0");
                baseline.Advance(2);
                Environment.SetEnvironmentVariable("PR57_NEEDS_BULK","1");
                candidate.Advance(2);
                Assert.Equal(baseline.SaveToJson(),candidate.SaveToJson());
                if(step==0)Assert.True((long)hits!.GetValue(candidate)!>=12,
                    "The contiguous primitive lane must actually run.");
                if(step==6)
                {
                    baseline=CitySimulation.FromJson(baseline.SaveToJson());
                    candidate=CitySimulation.FromJson(candidate.SaveToJson());
                }
            }
        }
        finally{Environment.SetEnvironmentVariable("PR57_NEEDS_BULK",original);}
    }
}
