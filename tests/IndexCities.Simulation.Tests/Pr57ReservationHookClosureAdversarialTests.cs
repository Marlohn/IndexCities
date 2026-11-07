using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #199: ARTIFICIAL adversarial fixtures for the #198 hook-closure
// hypothesis. They exercise real mutation paths; they do NOT implement an
// index, change rules, or demonstrate throughput.
public sealed class Pr57ReservationHookClosureAdversarialTests
{
    [Fact]
    public void Road_demolition_clears_another_vehicles_claim_without_advance()
    {
        var sim = new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success);
        Assert.True(sim.PlaceRoad(new(-192, 0), new(-80, 0)).Success);
        sim.Advance(7200);
        Assert.All(sim.State.Projects, p => Assert.Equal(WorkStatus.Completed, p.Status));
        var road = sim.State.Roads.First(r => !r.Highway);
        var vehicle = new Vehicle
        {
            Id = sim.State.NextId++,
            Status = VehicleStatus.Waiting,
            Position = road.Position,
            IntersectionClaim = road.Id
        };
        sim.State.Vehicles.Add(vehicle);
        Assert.Equal(road.Id, vehicle.IntersectionClaim);
        Assert.True(sim.QuoteRoadDemolition(road.Id).Allowed);
        var beforeTime = sim.State.ElapsedSeconds;

        Assert.True(sim.DemolishRoad(road.Id).Success);

        Assert.Null(vehicle.IntersectionClaim);
        Assert.Equal(beforeTime, sim.State.ElapsedSeconds);
        // RoadDemolition.DemolishRoad writes IntersectionClaim directly, not
        // through #198's Pr57ReservationWrite. An across-Advance index cannot
        // infer this external command from elapsed simulation time alone.
    }

    [Fact]
    public void Tow_loading_clears_a_third_party_claim_during_operate_tow()
    {
        var sim = new CitySimulation(42);
        var state = sim.State;
        state.Vehicles.Clear();
        var roadId = state.Roads.First().Id;
        var tow = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.TowHeavy,
            Status = VehicleStatus.Waiting, TowPhase = TowPhase.Loading,
            LoadTimer = 0, Position = new Point(80, 40),
            Heading = Math.PI / 2, Fuel = 40
        };
        var target = new Vehicle
        {
            Id = state.NextId++, External = true, Body = TruckBody.Cargo,
            Status = VehicleStatus.Stranded, TowTruckId = tow.Id,
            IntersectionClaim = roadId, Position = new Point(20, 20),
            Heading = 0, Cause = "Sem combustível"
        };
        tow.TowTargetId = target.Id;
        state.Vehicles.AddRange([tow, target]);
        var beforeTime = state.ElapsedSeconds;

        var operateTow = typeof(CitySimulation).GetMethod(
            "OperateTow", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(operateTow);
        Assert.Equal(true, operateTow!.Invoke(sim, [tow, 2.0]));

        Assert.Equal(VehicleStatus.Towed, target.Status);
        Assert.Null(target.IntersectionClaim);
        Assert.Equal(beforeTime, state.ElapsedSeconds);
        // Towing.LoadTowed writes a different vehicle's claim directly.
        // A writer hook only on the currently processed vehicle misses it.
    }

    [Fact]
    public void Public_vehicle_reservation_properties_can_change_without_any_engine_hook()
    {
        var sim = new CitySimulation(42);
        var vehicle = new Vehicle { Id = sim.State.NextId++ };
        sim.State.Vehicles.Add(vehicle);
        var beforeTime = sim.State.ElapsedSeconds;
        var roadId = sim.State.Roads.First().Id;

        vehicle.IntersectionClaim = roadId;
        vehicle.ManoeuvreSpaces.Add(roadId);
        Assert.Equal(roadId, vehicle.IntersectionClaim);
        Assert.Contains(roadId, vehicle.ManoeuvreSpaces);
        Assert.Equal(beforeTime, sim.State.ElapsedSeconds);

        // Public setters and a mutable List<int> permit edits between
        // Advance(2) calls without entering any #198 engine callsite.
    }
}
