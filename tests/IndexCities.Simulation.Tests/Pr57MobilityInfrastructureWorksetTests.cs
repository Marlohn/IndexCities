using System.Diagnostics;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #181: read-only, same-replay census of mobility/utility worksets.
// All 13 phases and every public Advance(2) still execute. "Due" is only a
// snapshot of a scheduling predicate, not proof that a citizen can be skipped.
public sealed class Pr57MobilityInfrastructureWorksetTests
{
    [Fact]
    public void Authentic_replay_counts_worksets_and_phase_cost_without_changing_state()
    {
        var source=Environment.GetEnvironmentVariable("PR57_181_SOURCE");
        if(string.IsNullOrWhiteSpace(source))
        {
            Console.WriteLine("PR57_181_SKIP authentic checkpoint not provided");
            return;
        }
        Assert.True(File.Exists(source),"An authentic checkpoint is required");
        var label=Environment.GetEnvironmentVariable("PR57_181_CASE")??"unknown";
        var steps=int.Parse(Environment.GetEnvironmentVariable("PR57_181_STEPS")??"150");
        Assert.True(steps>0&&steps%3==0);
        var initial=File.ReadAllText(source);
        var sim=CitySimulation.FromJson(initial);
        var control=CitySimulation.FromJson(initial);
        var profile=new StepCostProfile { PhaseEffects=new Pr57PhaseEffects(steps/6) };
        if(Environment.GetEnvironmentVariable("PR57_183_PROBE")=="1")
            profile.CrossPhasePresence=new Pr57CrossPhasePresenceProbe(10);
        sim.CostProfile=profile;
        var output=Environment.GetEnvironmentVariable("PR57_181_OUTPUT");
        if(!string.IsNullOrWhiteSpace(output))
        {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"checkpoint-0.json"),control.SaveToJson());
        }
        long liveVisits=0,movingVisits=0,inVehicleVisits=0,dueVisits=0,waitingVisits=0;
        long activeBuildingVisits=0,utilityBuildingVisits=0,waterDemandVisits=0,electricDemandVisits=0;
        long roadVisits=0,crosswalkVisits=0;
        long candidateTicks=0;
        for(var step=1;step<=steps;step++)
        {
            // Count the actual entry-state workload; never use it to select actors.
            foreach(var c in sim.State.Citizens)
            {
                if(!c.Alive)continue;
                liveVisits++;
                if(c.VehicleId!=null)inVehicleVisits++;
                else if(c.Moving)movingVisits++;
                else if(sim.State.ElapsedSeconds>=c.NextDecisionAt)dueVisits++;
                else waitingVisits++;
            }
            foreach(var b in sim.State.Buildings)
            {
                if(!b.Completed||b.Removed)continue;
                activeBuildingVisits++;
                if(b.Definition.Utility)utilityBuildingVisits++;
                if(b.WaterDemand>0)waterDemandVisits++;
                if(b.ElectricityDemand>0)electricDemandVisits++;
            }
            roadVisits+=sim.State.Roads.Count;
            crosswalkVisits+=sim.State.Roads.Count(r=>r.Crosswalk);
            var start=Stopwatch.GetTimestamp();
            sim.Advance(2);
            candidateTicks+=Stopwatch.GetTimestamp()-start;
            control.Advance(2);
            if(step%(steps/3)!=0)continue;
            sim.CheckInvariants();
            control.CheckInvariants();
            var actual=sim.SaveToJson();
            Assert.Equal(control.SaveToJson(),actual);
            Assert.Equal(actual,CitySimulation.FromJson(actual).SaveToJson());
            if(!string.IsNullOrWhiteSpace(output))
                File.WriteAllText(Path.Combine(output,$"checkpoint-{step}.json"),actual);
            Console.WriteLine($"PR57_181_CHECKPOINT case={label} step={step} alive={sim.State.Citizens.Count(c=>c.Alive)} parity=FULL_JSON");
        }
        Assert.Equal(steps,profile.Steps);
        Assert.Equal(6,profile.PhaseEffects!.Samples);
        Assert.True(liveVisits>0);
        var phaseTicks=profile.Ticks.Sum();
        Assert.True(phaseTicks>0);
        var mobility=profile.Ticks[2];
        var infrastructure=profile.Ticks[6];
        var fraction=(double)(mobility+infrastructure)/phaseTicks;
        var maxSpeedup=1/(1-fraction);
        Console.WriteLine($"PR57_181_RESULT case={label} steps={steps} simSeconds={steps*2} " +
            $"liveVisits={liveVisits} movingVisits={movingVisits} inVehicleVisits={inVehicleVisits} " +
            $"dueVisits={dueVisits} waitingVisits={waitingVisits} activeBuildingVisits={activeBuildingVisits} " +
            $"utilityBuildingVisits={utilityBuildingVisits} waterDemandVisits={waterDemandVisits} " +
            $"electricDemandVisits={electricDemandVisits} roadVisits={roadVisits} crosswalkVisits={crosswalkVisits} " +
            $"mobilityTicks={mobility} infrastructureTicks={infrastructure} allPhaseTicks={phaseTicks} " +
            $"mobilityPct={100.0*mobility/phaseTicks:F3} infrastructurePct={100.0*infrastructure/phaseTicks:F3} " +
            $"impossibleBothPhaseSpeedup={maxSpeedup:F4} candidateIntrusiveSeconds={candidateTicks/(double)Stopwatch.Frequency:F3} " +
            "scope=ENTRY_STATE_WORKSETS_NOT_CAUSAL_READ_SETS_OR_PLAIN_THROUGHPUT");
        // #182: joint Amdahl filter on the SAME Step(2) replay, not a speedup.
        // Phase timings are intrusive; this counterfactual deletes real work and
        // cannot be used to justify skipping any phase or causal substep.
        var jointTicks=profile.Ticks[0]+profile.Ticks[3]+profile.Ticks[6]+profile.Ticks[10];
        var remainderTicks=phaseTicks-jointTicks;
        Assert.True(remainderTicks>0);
        Console.WriteLine($"PR57_182_JOINT case={label} steps={steps} " +
            $"vehicleTicks={profile.Ticks[0]} constructionTicks={profile.Ticks[3]} " +
            $"infrastructureTicks={profile.Ticks[6]} dispatchTicks={profile.Ticks[10]} " +
            $"jointTicks={jointTicks} remainingTicks={remainderTicks} totalTicks={phaseTicks} " +
            $"jointPct={100.0*jointTicks/phaseTicks:F3} remainingPct={100.0*remainderTicks/phaseTicks:F3} " +
            $"impossibleJointSpeedup={(double)phaseTicks/remainderTicks:F4} " +
            "scope=INTRUSIVE_PHASE_TIMING_NOT_WALL_OR_CAUSAL_READ_SETS");
        for(var i=0;i<StepCostProfile.Names.Length;i++)
            Console.WriteLine($"PR57_182_PHASE case={label} name={StepCostProfile.Names[i]} " +
                $"ticks={profile.Ticks[i]} pct={100.0*profile.Ticks[i]/phaseTicks:F3}");
        // Existing #174 observer already tracks NET repeated writes of entity IDs
        // across phase boundaries; emit all phases, not only the two from #181.
        foreach(var line in profile.PhaseEffects.ReportLines().Where(line=>
                     line.StartsWith("PR57_PHASE_EFFECT name=",StringComparison.Ordinal)))
            Console.WriteLine("PR57_182_"+line);
        if(profile.CrossPhasePresence is { } presence)
        {
            Assert.True(presence.Samples>0);
            foreach(var line in presence.ReportLines())Console.WriteLine(line);
        }
        foreach(var line in profile.PhaseEffects.ReportLines().Where(line=>
                     line.StartsWith("PR57_PHASE_EFFECT_SUMMARY",StringComparison.Ordinal)
                     ||line.StartsWith("PR57_PHASE_EFFECT name=citizen-mobility ",StringComparison.Ordinal)
                     ||line.StartsWith("PR57_PHASE_EFFECT name=infrastructure ",StringComparison.Ordinal)))
            Console.WriteLine("PR57_181_"+line);
    }
}
