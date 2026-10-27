using System.Text.Json;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #177: authentic, read-only census of the reservation workset.
// No product state is edited except through the real public Advance(2).
// A second restored instance checks complete causal/save parity.
public sealed class Pr57ReservationRealWorksetTests
{
    static Dictionary<int,string> Reservations(CitySimulation sim) =>
        sim.State.Projects
            .Where(p => p.Status is WorkStatus.Waiting or WorkStatus.InProgress)
            .ToDictionary(p => p.Id, p => JsonSerializer.Serialize(p.LocalReserved));

    static string Shipments(CitySimulation sim) => JsonSerializer.Serialize(
        sim.State.Shipments.Select(s => new { s.Id, s.ProjectId, s.Complete, s.Cargo }).ToArray());

    static string DepotStock(CitySimulation sim) => JsonSerializer.Serialize(
        sim.State.Buildings.Where(b => b.Kind == BuildingKind.WorksDepot && b.Completed && !b.Removed && !b.OperationClosed)
            .Select(b => new { b.Id, b.Stock }).ToArray());

    [Fact]
    public void Authentic_works_count_reservation_reads_versus_net_changes()
    {
        var source=Environment.GetEnvironmentVariable("PR57_RESERVE_REAL_SAVE");
        if(string.IsNullOrWhiteSpace(source))
        {
            Console.WriteLine("PR57_177_SKIP real checkpoint not provided; no synthetic substitution");
            return;
        }
        Assert.True(File.Exists(source), "Authentic works checkpoint must exist");
        var json=File.ReadAllText(source);
        var sim=CitySimulation.FromJson(json);
        var control=CitySimulation.FromJson(json);
        var profile=new StepCostProfile();
        sim.CostProfile=profile;
        var before=Reservations(sim);
        var previousShipments=Shipments(sim);
        var previousStock=DepotStock(sim);
        long activeProjectSteps=0,changedProjectSteps=0,changedProjectRecords=0;
        long shipmentChangedSteps=0,stockChangedSteps=0,rosterChangedSteps=0;
        long stockOrShipmentChangedButNoReservation=0;
        long noReservationChangeSteps=0;
        var maxActive=0;
        var output=Environment.GetEnvironmentVariable("PR57_RESERVE_OUTPUT_DIR");
        if(!string.IsNullOrWhiteSpace(output))
        {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"checkpoint-0.json"),json);
        }
        for(var step=1;step<=900;step++)
        {
            // Both instances execute every phase of every public 2-second call.
            sim.Advance(2);
            control.Advance(2);
            var after=Reservations(sim);
            var shipment=Shipments(sim);
            var stock=DepotStock(sim);
            activeProjectSteps+=after.Count;
            maxActive=Math.Max(maxActive,after.Count);
            var changed=after.Count(kv=>before.TryGetValue(kv.Key,out var prior)&&prior!=kv.Value);
            var rosterChanged=before.Count!=after.Count||before.Keys.Except(after.Keys).Any();
            if(rosterChanged)rosterChangedSteps++;
            if(changed>0){changedProjectSteps++;changedProjectRecords+=changed;}
            else noReservationChangeSteps++;
            var shipmentChanged=shipment!=previousShipments;
            var stockChanged=stock!=previousStock;
            if(shipmentChanged)shipmentChangedSteps++;
            if(stockChanged)stockChangedSteps++;
            if((shipmentChanged||stockChanged)&&changed==0)stockOrShipmentChangedButNoReservation++;
            before=after;
            previousShipments=shipment;
            previousStock=stock;
            if(step%300!=0)continue;
            sim.CheckInvariants();
            control.CheckInvariants();
            var candidate=sim.SaveToJson();
            var baseline=control.SaveToJson();
            Assert.Equal(baseline,candidate);
            Assert.Equal(candidate,CitySimulation.FromJson(candidate).SaveToJson());
            if(!string.IsNullOrWhiteSpace(output))
                File.WriteAllText(Path.Combine(output,$"checkpoint-{step}.json"),candidate);
            Console.WriteLine($"PR57_177_CHECKPOINT step={step} alive={sim.State.Citizens.Count(c=>c.Alive)} active={after.Count} shipments={sim.State.Shipments.Count} parity=FULL_JSON");
        }
        Assert.True(profile.Steps>=900,"All public Step(2) calls must execute");
        Assert.True(maxActive>0,"Authentic save must contain actual construction");
        Console.WriteLine($"PR57_177_RESULT steps={profile.Steps} activeProjectSteps={activeProjectSteps} maxActive={maxActive} " +
            $"changedProjectSteps={changedProjectSteps} changedProjectRecords={changedProjectRecords} " +
            $"noReservationChangeSteps={noReservationChangeSteps} rosterChangedSteps={rosterChangedSteps} " +
            $"shipmentChangedSteps={shipmentChangedSteps} depotStockChangedSteps={stockChangedSteps} " +
            $"stockOrShipmentChangedButNoReservation={stockOrShipmentChangedButNoReservation} " +
            $"reserveCalls={profile.Pr57DispatchReserveCalls} reserveProjectVisits={profile.ConstructionReserveProjects} " +
            $"shipmentQueries={profile.ConstructionShipmentQueries} shipmentVisits={profile.ConstructionShipmentVisits} " +
            $"reserveTicks={profile.Pr57DispatchReserveTicks} dispatchTicks={profile.Ticks[10]} " +
            "scope=NET_RESERVATIONS_AND_ACTUAL_ENUMERATIONS_NOT_COMPLETE_CAUSAL_READ_SET");
    }
}
