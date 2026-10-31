using System.Reflection;
using System.Text.Json;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// #174: phase-boundary NET write overlap is an observation, never a read set.
public sealed class Pr57PhaseOverlapTests
{
    static string Snapshot(int vehiclePosition,int projectCause) =>
        JsonSerializer.Serialize(new {
            Vehicles=new[]{new {Id=1,Position=vehiclePosition}},
            Citizens=Array.Empty<object>(),
            Projects=new[]{new {Id=2,Cause=projectCause}},
            Buildings=Array.Empty<object>(),
            Shipments=Array.Empty<object>()
        });

    [Fact]
    public void Repeated_net_entity_writes_count_once_per_later_phase_and_reset_each_step()
    {
        var probe=new Pr57PhaseEffects(1);
        var begin=typeof(Pr57PhaseEffects).GetMethod("Begin",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var observe=typeof(Pr57PhaseEffects).GetMethod("Observe",BindingFlags.Instance|BindingFlags.NonPublic)!;
        void Run(int startVehicle,int startCause,bool first)
        {
            begin.Invoke(probe,new object[]{Snapshot(startVehicle,startCause)});
            var vehicle=startVehicle;
            var cause=startCause;
            for(var phase=0;phase<StepCostProfile.Names.Length;phase++)
            {
                if(first&&phase==0)vehicle=1;
                if(first&&phase==1)cause=1;
                if(first&&phase==3)vehicle=2;
                if(first&&phase==10)cause=2;
                if(!first&&phase==0)vehicle=3;
                observe.Invoke(probe,new object[]{phase,Snapshot(vehicle,cause)});
            }
        }
        Run(0,0,true);
        Run(2,2,false);
        var lines=probe.ReportLines().ToArray();
        string Phase(int index)=>lines.Single(line=>line.StartsWith(
            "PR57_PHASE_EFFECT name="+StepCostProfile.Names[index]+" ",StringComparison.Ordinal));
        Assert.Contains("revisitedEarlierPhase=Vehicles:1",Phase(3));
        Assert.Contains("Projects:1",Phase(10).Split("revisitedEarlierPhase=")[1]);
        Assert.Contains("revisitedEarlierPhase=Vehicles:0",Phase(0));
        Assert.Contains("netEntityChanges=Vehicles:2",Phase(0)); // one in each sampled Step
        Assert.Contains("samples=2",lines[0]);
    }
}
