using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class CityCycleTests
{
    internal static CitySimulation City()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(240,0)).Success);sim.Advance(14400);
        Assert.True(sim.PlaceRoad(new(-184,0),new(116,44),true).Success);sim.Advance(7200);
        foreach(var x in new[]{-128,-108,-88,-68,-48,-28,-8,12,32,52,72,92,112,132,152,172,192,212})Assert.True(sim.PlaceBuilding(BuildingKind.House,new(x,14)).Success,"house "+x);
        foreach(var x in new[]{-160,-140,-120,-100,-80,-60,-40,-20,0,20,40,60,80,100})Assert.True(sim.PlaceBuilding(BuildingKind.House,new(x,58)).Success,"house north "+x);
        Assert.True(sim.PlaceBuilding(BuildingKind.WorksDepot,new(-160,-18),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.Grocery,new(-124,-16),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.FuelStation,new(-88,-20),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.Farm,new(-44,-28),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.WindPower,new(0,-16),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.WindPower,new(120,-16),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.WaterTreatment,new(40,-20),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.SewageTreatment,new(80,-20),Math.PI).Success);
        var lake=sim.State.LakeCentre;var z=Math.Round((lake.Z-sim.State.LakeRadius-30)/4)*4;
        Assert.True(sim.PlaceRoad(new(240,0),new(132,z),true).Success);sim.Advance(14400);
        Assert.True(sim.PlaceBuilding(BuildingKind.WaterIntake,new(144,z+14)).Success);
        return sim;
    }
    // Parked cars occupy real space: two parked cars never overlap in one lot space.
    static void AssertNoSharedParkingSpace(CitySimulation sim){var parked=sim.State.Vehicles.Where(v=>v.PassengerCar&&v.Status==VehicleStatus.Parked).ToList();foreach(var a in parked)foreach(var b in parked)if(a.Id<b.Id)Assert.True(a.Position.Distance(b.Position)>=2,$"carros {a.Id} e {b.Id} na mesma vaga em {a.Position}");}
    static void Dump(CitySimulation sim){var folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,"../../../../../artifacts/verification"));System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"city-cycle-snapshot.json"),sim.SaveToJson());}
    static string Details(CitySimulation sim)=>string.Join(" | ",sim.State.Buildings.Where(b=>!b.Removed).Select(b=>$"{b.Id}:{b.Kind} built={b.Completed} owner={b.OwnerAccount} workers={sim.State.Citizens.Count(c=>c.WorkplaceId==b.Id)} op={b.OperationalCapacity:F1} stock={string.Join(',',b.Stock.Select(k=>$"{k.Key}={k.Value:F1}"))} cause={b.Cause}"))+" | "+string.Join(" | ",sim.State.Projects.Where(p=>p.Status!=WorkStatus.Completed).Select(p=>$"work{p.Id}:{p.Cause} {p.Progress:P0} budget={p.BudgetRemaining}"));
    [Fact] public void Entire_v1_catalog_can_be_built_and_operate_with_real_people_and_payments()
    {
        var sim=City();var snapshots=new List<string>();bool farmProduced=false,foodBought=false,waterRan=false,powerRan=false,maintenanceCapacityChecked=false;
        for(var hour=0;hour<48;hour++){sim.Advance(3600);sim.CheckInvariants();AssertNoSharedParkingSpace(sim);farmProduced|=sim.State.Buildings.Any(b=>b.FoodProduced>0);foodBought|=sim.State.Ledger.Any(l=>l.Category=="Alimentos");waterRan|=sim.State.PotableCapacity>0;powerRan|=sim.State.ElectricityCapacity>0;if(!maintenanceCapacityChecked&&sim.State.PotableCapacity>.001){var treatment=sim.State.Buildings.First(b=>b.Kind==BuildingKind.WaterTreatment&&b.OperationalCapacity>0);var condition=treatment.MaintenanceRatio;treatment.MaintenanceRatio=.5;sim.Advance(2);Assert.Equal(sim.State.PotableCapacity,sim.State.Buildings.Where(b=>b.Kind==BuildingKind.WaterTreatment&&!b.Removed).Sum(b=>b.OperationalCapacity),6);treatment.MaintenanceRatio=condition;sim.Advance(2);maintenanceCapacityChecked=true;}if(hour%12==0){snapshots.Add(Details(sim));Console.WriteLine($"CYCLE hour={hour} complete={sim.State.Buildings.Count(b=>b.Completed)} population={sim.Population} power={sim.State.ElectricityCapacity} water={sim.State.PotableCapacity}");Dump(sim);}}
        Dump(sim);
        var detail=string.Join("\n",snapshots)+"\nFINAL "+Details(sim)+"\nVEHICLES "+string.Join(" | ",sim.State.Vehicles.Select(v=>$"{v.Id}: {v.Status} p={v.Position} heading={v.Heading} routeIndex={v.RouteIndex}/{v.Route.Count} fuel={v.Fuel} shipment={v.ShipmentId} cause={v.Cause}"));
        Assert.True(sim.State.Buildings.Where(b=>!b.Removed).All(b=>b.Completed),detail);
        Assert.True(sim.State.Citizens.Count>0,detail);Assert.True(sim.State.Companies.Count>0,detail);
        Assert.True(maintenanceCapacityChecked,detail);Assert.True(powerRan,detail);Assert.True(waterRan,detail);Assert.True(farmProduced,detail);Assert.True(foodBought,detail);
        Assert.Contains(sim.State.Ledger,l=>l.Category.StartsWith("Salários"));Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);
    }
    [Fact] public void Inaccessible_private_building_never_gets_fake_occupancy_or_stock()
    {
        var sim=new CitySimulation(42);sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40));sim.PlaceBuilding(BuildingKind.Grocery,new(-70,40));sim.Advance(172800);
        Assert.Empty(sim.State.Citizens);Assert.Empty(sim.State.Companies);Assert.All(sim.State.Buildings,b=>Assert.Equal(0,b.Stock.Values.Sum()));sim.CheckInvariants();
    }

    // PR57: the pure allocation replay must actually skip repeated work, and
    // must fall back on every changed causal input (including ordered sources).
    [Fact] public void Pr57_utility_replay_exact_inputs_and_fallback_match_uncached_allocation()
    {
        var replay=new CapacityAllocation(false,true);
        var plain=new CapacityAllocation();
        var sources=new List<Point>{new(0,0),new(120,0)};
        var positions=new[]{new Point(8,0),new Point(92,0),new Point(60,20)};
        var demands=new[]{10.0,20.0,15.0};
        void Compare(double capacity)
        {
            foreach(var allocation in new[]{replay,plain})
            {
                allocation.Reset(positions.Length);
                for(var i=0;i<positions.Length;i++)allocation.Set(i,positions[i],demands[i]);
                allocation.Allocate(capacity,sources);
            }
            Assert.Equal(plain.Result.Take(positions.Length),replay.Result.Take(positions.Length));
        }
        Compare(18);
        Assert.Equal(0,replay.ReplayHits);
        Assert.Equal(1,replay.ReplayMisses);
        Compare(18);
        Assert.Equal(1,replay.ReplayHits);
        demands[1]=21;Compare(18);
        positions[0]=new Point(9,0);Compare(18);
        sources.Reverse();Compare(18);
        Compare(17);
        Assert.Equal(1,replay.ReplayHits);
        Assert.Equal(5,replay.ReplayMisses);
        Compare(17);
        Assert.Equal(2,replay.ReplayHits);
        Assert.Equal(5,replay.ReplayMisses);

        // Exact replay must reject changes to the source geometry and cardinality,
        // including edits that preserve capacity and total consumer demand.
        // These are legal between-Advance(2) mutations in the integrated engine.
        sources[0]=new Point(1,0);
        Compare(17);
        sources.Add(new Point(240,0));
        Compare(17);
        sources.RemoveAt(2);
        Compare(17);
        Assert.Equal(2,replay.ReplayHits);
        Assert.Equal(8,replay.ReplayMisses);

        // Zero demand takes the original early-return path without caching.
        // The next nonzero demand must not reuse the previous result.
        Array.Fill(demands,0.0);
        Compare(17);
        Compare(17);
        Assert.Equal(2,replay.ReplayHits);
        Assert.Equal(8,replay.ReplayMisses);
        demands[2]=15;
        Compare(17);
        Assert.Equal(2,replay.ReplayHits);
        Assert.Equal(9,replay.ReplayMisses);
    }

    // PR57: the cross-phase ceiling is observation only. Verify every integrated
    // Advance(2) boundary, not just a final aggregate or a laboratory phase.
    [Fact] public void Pr57_cross_phase_census_preserves_every_intermediate_save()
    {
        // Exercise the actual integrated city fixture (roads, paid projects, utilities,
        // logistics and businesses), not a newly created, effectively empty city.
        var baseline=City();
        Assert.NotEmpty(baseline.State.Projects);
        Assert.NotEmpty(baseline.State.Buildings);
        var candidate=CitySimulation.FromJson(baseline.SaveToJson());
        var profile=new StepCostProfile();
        candidate.CostProfile=profile;
        for(var step=1;step<=60;step++)
        {
            baseline.Advance(2);
            candidate.Advance(2);
            Assert.Equal(step,profile.Steps);
            var baselineJson=baseline.SaveToJson();
            var candidateJson=candidate.SaveToJson();
            Assert.Equal(baselineJson,candidateJson);
            // External, between-step mutation: the diagnostic observer must not
            // retain a stale view of a causal building field across Advance(2).
            // Apply the same legal state edit independently to both simulations.
            if(step==30)
            {
                var original=baseline.State.Buildings.First(b=>b.Kind==BuildingKind.WaterTreatment);
                var observed=candidate.State.Buildings.Single(b=>b.Id==original.Id);
                original.MaintenanceRatio=0.5;
                observed.MaintenanceRatio=0.5;
                // Refresh the checkpoint after the mutation: the independent
                // save/load continuation must exercise the changed maintenance
                // condition, not the pre-mutation state captured above.
                baselineJson=baseline.SaveToJson();
                candidateJson=candidate.SaveToJson();
                Assert.Equal(baselineJson,candidateJson);
            }
            // An observational profile must also leave each persisted checkpoint
            // loadable, not merely equal in the two in-memory simulations.
            if(step==30||step==60)
            {
                var resumed=CitySimulation.FromJson(candidateJson);
                resumed.CheckInvariants();
                Assert.Equal(candidateJson,resumed.SaveToJson());
                // A persisted checkpoint must survive an entire independent
                // continuation, not just the first Step after reload. This
                // exercises recurring phase invalidations after save/load.
                if(step==30)
                {
                    var unprofiled=CitySimulation.FromJson(baselineJson);
                    // Re-enable the observation after load: a fresh profiler must
                    // not alter any subsequent integrated causal boundary.
                    var resumedProfile=new StepCostProfile();
                    resumed.CostProfile=resumedProfile;
                    for(var continuation=1;continuation<=30;continuation++)
                    {
                        resumed.Advance(2);
                        unprofiled.Advance(2);
                        Assert.Equal(continuation,resumedProfile.Steps);
                        Assert.Equal(unprofiled.SaveToJson(),resumed.SaveToJson());
                        resumed.CheckInvariants();
                    }
                    Assert.True(resumedProfile.Ticks.Sum()>0);
                }
            }
        }
        baseline.CheckInvariants();
        candidate.CheckInvariants();
        Assert.Equal(13,StepCostProfile.Names.Length);
        Assert.Equal(13,profile.Ticks.Length);
        Assert.True(profile.Ticks.Sum()>0);
        var recurring=new[]{0,2,3,6,7,8,9,10,11};
        Assert.Equal(9,recurring.Distinct().Count());
        Assert.All(recurring,i=>Assert.InRange(i,0,profile.Ticks.Length-1));
        var recurringTicks=recurring.Sum(i=>profile.Ticks[i]);
        var otherTicks=profile.Ticks.Sum()-recurringTicks;
        Assert.True(recurringTicks>=0&&otherTicks>=0);
        Assert.Equal(profile.Ticks.Sum(),recurringTicks+otherTicks);
    }
}
