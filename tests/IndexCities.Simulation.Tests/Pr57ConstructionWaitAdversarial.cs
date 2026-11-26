using System.Collections;
using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only adversarial causal replay. No production rules or tuning changes.
// A real one-truck maintenance cycle must wake after the truck returns; the
// second scenario uses paid public road commands to invalidate a waiting site.
public sealed class Pr57ConstructionWaitAdversarial
{
    static (CitySimulation Sim, int RoadId, int ProjectId) Setup()
    {
        var sim=new CitySimulation(42,new(){ExternalTruckCount=1});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-80,0)).Success);
        sim.Advance(7200);
        var road=sim.State.Roads.Single(r=>r.Position==new Point(-80,0));
        road.MaintenanceDueAt=sim.State.ElapsedSeconds-1;
        sim.State.EconomyNextAt=sim.State.ElapsedSeconds;
        sim.Advance(2);
        var project=Assert.Single(sim.State.Projects,p=>p.IsMaintenance&&p.MaintenanceRoadIds.Contains(road.Id));
        Assert.True(project.Required.Count>=2);
        return (sim,road.Id,project.Id);
    }

    static void Capture(CitySimulation sim,string scenario,int index)
    {
        sim.CheckInvariants();
        var snapshot=sim.SaveToJson();
        Assert.Equal(snapshot,CitySimulation.FromJson(snapshot).SaveToJson());
        var dir=Environment.GetEnvironmentVariable("PR57_WAIT_ADVERSARIAL_DIR");
        if(string.IsNullOrEmpty(dir))return;
        var path=Path.Combine(dir,scenario);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path,$"checkpoint-{index}.json"),snapshot);
    }

    static int Cached(CitySimulation sim)
    {
        var field=typeof(CitySimulation).GetField("_pr57ConstructionWaitAccess",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.NotNull(field);
        return ((IDictionary)field.GetValue(sim)!).Count;
    }

    [Fact]
    public void One_truck_physically_returns_and_dispatches_next_real_maintenance_load()
    {
        var (sim,_,id)=Setup();
        var initial=sim.State.Shipments.Count(s=>s.ProjectId==id);
        Assert.True(initial>=1,"First material shipment must actually be dispatched");
        Capture(sim,"truck-return",0);
        var everCached=false;
        for(var i=1;i<=24;i++)
        {
            sim.Advance(120); // 60 ordinary 2-second quanta, no skipped time
            everCached|=Cached(sim)>0;
            Capture(sim,"truck-return",i);
        }
        var shipments=sim.State.Shipments.Where(s=>s.ProjectId==id).ToArray();
        Assert.True(shipments.Length>initial,"Truck return must enable a later shipment");
        Assert.True(shipments.Any(s=>s.Complete),"At least one load must physically arrive");
        if(Environment.GetEnvironmentVariable("PR57_CONSTRUCTION_WAIT_AGENDA")=="1")
            Assert.True(everCached,"Candidate must actually exercise its waiting-project index");
        Console.WriteLine($"PR57_WAIT_ADVERSARIAL truck-return initial={initial} final={shipments.Length} completed={shipments.Count(s=>s.Complete)} cacheExercised={everCached}");
    }

    [Fact]
    public void Removing_waiting_maintenance_target_cancels_then_paid_reconnection_remains_physical()
    {
        var (sim,roadId,id)=Setup();
        var point=sim.State.Roads.Single(r=>r.Id==roadId).Position;
        Capture(sim,"road-rebuild",0);
        sim.Advance(20);Capture(sim,"road-rebuild",1);
        Assert.NotEqual(WorkStatus.Completed,sim.State.Projects.Single(p=>p.Id==id).Status);
        Assert.True(sim.DemolishRoad(roadId).Success);
        Assert.Equal(WorkStatus.Cancelled,sim.State.Projects.Single(p=>p.Id==id).Status);
        Capture(sim,"road-rebuild",2);
        var replacement=sim.PlaceRoad(point,point);
        Assert.True(replacement.Success,"Reconnection must be ordered through paid public command");
        sim.Advance(120);
        Capture(sim,"road-rebuild",3);
        Assert.True(sim.State.Projects.Any(p=>p.Id==replacement.Id&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress or WorkStatus.Completed));
        Console.WriteLine($"PR57_WAIT_ADVERSARIAL road-rebuild old={id} replacement={replacement.Id} elapsed={sim.State.ElapsedSeconds}");
    }
}
