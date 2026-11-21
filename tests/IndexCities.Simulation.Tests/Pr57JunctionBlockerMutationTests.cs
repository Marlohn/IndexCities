using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57: the blocker is re-evaluated after an external position mutation.
// This deliberately exercises the integrated admission predicate, not a synthetic cache.
public sealed class Pr57JunctionBlockerMutationTests
{
    [Fact]
    public void Pedestrian_crosswalk_snapshot_rechecks_external_flag_mutation_on_next_move()
    {
        // An external editor can mutate Crosswalk without incrementing RoadRevision.
        // A cross-call cache keyed only by revision would silently retain stale nodes.
        var sim = new CitySimulation(42);
        sim.State.Roads.Add(new RoadNode { Id = 998101, Position = new Point(0, 0) });
        sim.State.Roads.Add(new RoadNode { Id = 998102, Position = new Point(40, 0) });
        sim = CitySimulation.FromJson(sim.SaveToJson());
        var move = typeof(CitySimulation).GetMethod("MoveCitizens",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var field = typeof(CitySimulation).GetField("_crosswalks",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(move);
        Assert.NotNull(field);
        void Tick() => move!.Invoke(sim, new object[] { 2.0 });
        List<RoadNode> Crossings() => (List<RoadNode>)field!.GetValue(sim)!;

        Tick(); // initialize old-save derived crossings before injecting mutations
        var road = sim.State.Roads.Single(r => r.Id == 998101);
        road.Crosswalk = true;
        Tick();
        Assert.Contains(Crossings(), r => r.Id == road.Id);
        road.Crosswalk = false;
        Tick();
        Assert.DoesNotContain(Crossings(), r => r.Id == road.Id);

        // Rehydration must not restore the previously cached crossing.
        sim = CitySimulation.FromJson(sim.SaveToJson());
        Tick();
        Assert.DoesNotContain(Crossings(), r => r.Id == road.Id);
    }

    [Fact]
    public void Junction_occupant_selection_rechecks_live_position_and_first_match()
    {
        var seed = new CitySimulation(42);
        var junction = new RoadNode
        {
            Id = 990101, Position = new Point(0, 0),
            Neighbours = new List<int> { 990102, 990103, 990104, 990105 }
        };
        seed.State.Roads.Add(junction);
        // The area-owner branch exists only when two nearby junctions are
        // connected in the road graph. A lone synthetic node cannot exercise it.
        seed.State.Roads.Add(new RoadNode
        {
            Id = 990102, Position = new Point(4, 0),
            Neighbours = new List<int> { 990101, 990106, 990107 }
        });
        seed.State.Roads.Add(new RoadNode { Id = 990103, Position = new Point(0, 4), Neighbours = new List<int> { 990101 } });
        seed.State.Roads.Add(new RoadNode { Id = 990104, Position = new Point(-4, 0), Neighbours = new List<int> { 990101 } });
        seed.State.Roads.Add(new RoadNode { Id = 990105, Position = new Point(0, -4), Neighbours = new List<int> { 990101 } });
        seed.State.Roads.Add(new RoadNode { Id = 990106, Position = new Point(8, 0), Neighbours = new List<int> { 990102 } });
        seed.State.Roads.Add(new RoadNode { Id = 990107, Position = new Point(4, 4), Neighbours = new List<int> { 990102 } });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 990201, Position = new Point(0, 0),
            Status = VehicleStatus.Travelling
        });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 990202, Position = new Point(1, 0),
            Status = VehicleStatus.Travelling, IntersectionClaim = junction.Id
        });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 990203, Position = new Point(2, 0),
            Status = VehicleStatus.Travelling, IntersectionClaim = junction.Id
        });

        var initial = seed.SaveToJson();
        var sim = CitySimulation.FromJson(initial);
        var node = sim.State.Roads.Single(r => r.Id == junction.Id);
        var entrant = sim.State.Vehicles.Single(v => v.Id == 990201);
        var first = sim.State.Vehicles.Single(v => v.Id == 990202);
        var second = sim.State.Vehicles.Single(v => v.Id == 990203);
        var method = typeof(CitySimulation).GetMethod("MayEnterJunction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        bool Admit() => (bool)method!.Invoke(sim, new object[] { entrant, node })!;

        Assert.False(Admit());
        Assert.Contains(first.Id.ToString(), entrant.Cause);
        first.Position = new Point(30, 0); // external mutation between admissions
        Assert.False(Admit());
        Assert.Contains(second.Id.ToString(), entrant.Cause);
        Assert.DoesNotContain(first.Id.ToString(), entrant.Cause);
        Assert.Equal(sim.SaveToJson(), CitySimulation.FromJson(sim.SaveToJson()).SaveToJson());
    }
    [Fact]
    public void Cleanup_of_idle_vehicle_must_preserve_claim_clock_and_empty_manoeuvre()
    {
        var sim = new CitySimulation(42);
        var road = new RoadNode { Id = 991001, Position = new Point(0, 0) };
        sim.State.Roads.Add(road);
        var vehicle = new Vehicle
        {
            Id = 991002, Position = new Point(0, 0),
            Status = VehicleStatus.Travelling,
            IntersectionClaim = road.Id,
            IntersectionEnteredAt = null,
            WaitingSince = 12
        };
        sim.State.Vehicles.Add(vehicle);
        // Rehydrate the road lookup as in a real save/load boundary.
        sim = CitySimulation.FromJson(sim.SaveToJson());
        vehicle = sim.State.Vehicles.Single(v => v.Id == 991002);
        var clear = typeof(CitySimulation).GetMethod("ClearPassedJunction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var release = typeof(CitySimulation).GetMethod("ReleaseManoeuvre",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(clear);
        Assert.NotNull(release);

        // An idle body can retain its claim. Moving it externally must release
        // the claim even without a movement step; stale clock must be cleared.
        clear!.Invoke(sim, new object[] { vehicle });
        Assert.Equal(road.Id, vehicle.IntersectionClaim);
        vehicle.Position = new Point(40, 0);
        clear.Invoke(sim, new object[] { vehicle });
        Assert.Null(vehicle.IntersectionClaim);
        Assert.Null(vehicle.IntersectionEnteredAt);

        // The truly empty state is the only unconditional release no-op.
        var before = sim.SaveToJson();
        release!.Invoke(sim, new object[] { vehicle });
        Assert.Equal(before, sim.SaveToJson());
        Assert.Equal(before, CitySimulation.FromJson(before).SaveToJson());
    }

    [Fact]
    public void Junction_occupant_rechecks_external_claim_mutation_and_save_reload()
    {
        var sim = new CitySimulation(42);
        var junction = new RoadNode
        {
            Id = 992101, Position = new Point(0, 0),
            Neighbours = new List<int> { 992102, 992103, 992104 }
        };
        sim.State.Roads.Add(junction);
        sim.State.Roads.Add(new RoadNode { Id = 992102, Position = new Point(4, 0), Neighbours = new List<int> { 992101 } });
        sim.State.Roads.Add(new RoadNode { Id = 992103, Position = new Point(0, 4), Neighbours = new List<int> { 992101 } });
        sim.State.Roads.Add(new RoadNode { Id = 992104, Position = new Point(-4, 0), Neighbours = new List<int> { 992101 } });
        sim.State.Vehicles.Add(new Vehicle { Id = 992201, Position = new Point(0, 0), Status = VehicleStatus.Travelling });
        sim.State.Vehicles.Add(new Vehicle
        {
            Id = 992202, Position = new Point(1, 0),
            Status = VehicleStatus.Travelling, IntersectionClaim = junction.Id
        });
        sim = CitySimulation.FromJson(sim.SaveToJson());
        var entrant = sim.State.Vehicles.Single(v => v.Id == 992201);
        var occupant = sim.State.Vehicles.Single(v => v.Id == 992202);
        var node = sim.State.Roads.Single(r => r.Id == junction.Id);
        var method = typeof(CitySimulation).GetMethod("MayEnterJunction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        bool Admit() => (bool)method!.Invoke(sim, new object[] { entrant, node })!;

        Assert.False(Admit());
        Assert.Contains(occupant.Id.ToString(), entrant.Cause);
        // A live external mutation must invalidate any remembered blocker.
        occupant.Position = new Point(40, 0);
        occupant.IntersectionClaim = null;
        var afterMutation = sim.SaveToJson();
        var admittedLive = Admit();
        var restored = CitySimulation.FromJson(afterMutation);
        var restoredEntrant = restored.State.Vehicles.Single(v => v.Id == entrant.Id);
        var restoredNode = restored.State.Roads.Single(r => r.Id == node.Id);
        var admittedReloaded = (bool)method!.Invoke(restored, new object[] { restoredEntrant, restoredNode })!;
        // The admission decision must agree across a reload. The human-readable
        // cause can be regenerated by the predicate and is not an identity key.
        Assert.Equal(admittedLive, admittedReloaded);
        Assert.DoesNotContain(occupant.Id.ToString(), entrant.Cause);
        Assert.DoesNotContain(occupant.Id.ToString(), restoredEntrant.Cause);
        // An admitted entrant owns its crossing space and must not be forced
        // to yield backwards. Explicitly withdraw that admission before testing
        // a fresh attempt against the reintroduced external blocker.
        entrant.ManoeuvreSpaces.Clear();
        entrant.IntersectionClaim = null;
        entrant.IntersectionEnteredAt = null;
        // Reintroduce the same blocker after a save/load boundary. The next
        // admission must observe both external fields, not a stale no-blocker result.
        occupant.Position = new Point(1, 0);
        occupant.IntersectionClaim = node.Id;
        Assert.False(Admit());
        Assert.Contains(occupant.Id.ToString(), entrant.Cause);
        var restoredBlocked = CitySimulation.FromJson(sim.SaveToJson());
        var restoredBlockedEntrant = restoredBlocked.State.Vehicles.Single(v => v.Id == entrant.Id);
        var restoredBlockedNode = restoredBlocked.State.Roads.Single(r => r.Id == node.Id);
        Assert.False((bool)method.Invoke(restoredBlocked, new object[] { restoredBlockedEntrant, restoredBlockedNode })!);
        Assert.Contains(occupant.Id.ToString(), restoredBlockedEntrant.Cause);
    }


    [Fact]
    public void Junction_manoeuvre_holder_rechecks_external_space_mutation_after_reload()
    {
        var sim = new CitySimulation(42);
        var junction = new RoadNode
        {
            Id = 993101, Position = new Point(0, 0),
            Neighbours = new List<int> { 993102, 993103, 993104 }
        };
        sim.State.Roads.Add(junction);
        sim.State.Roads.Add(new RoadNode { Id = 993102, Position = new Point(4, 0), Neighbours = new List<int> { 993101 } });
        sim.State.Roads.Add(new RoadNode { Id = 993103, Position = new Point(0, 4), Neighbours = new List<int> { 993101 } });
        sim.State.Roads.Add(new RoadNode { Id = 993104, Position = new Point(-4, 0), Neighbours = new List<int> { 993101 } });
        sim.State.Vehicles.Add(new Vehicle { Id = 993201, Position = new Point(0, 0), Status = VehicleStatus.Travelling });
        sim.State.Vehicles.Add(new Vehicle { Id = 993202, Position = new Point(1, 0), Status = VehicleStatus.Travelling });
        sim = CitySimulation.FromJson(sim.SaveToJson());
        var entrant = sim.State.Vehicles.Single(v => v.Id == 993201);
        var holder = sim.State.Vehicles.Single(v => v.Id == 993202);
        var node = sim.State.Roads.Single(r => r.Id == junction.Id);
        var method = typeof(CitySimulation).GetMethod("MayEnterJunction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        bool Admit(CitySimulation city, Vehicle vehicle, RoadNode road) =>
            (bool)method!.Invoke(city, new object[] { vehicle, road })!;

        // A serialized manoeuvre-space reservation must be observed even when
        // the holder has no IntersectionClaim. Removing it externally must
        // invalidate the blocker without retaining a cross-slice roster.
        holder.ManoeuvreSpaces.Add(node.Id);
        Assert.False(Admit(sim, entrant, node));
        Assert.Contains(holder.Id.ToString(), entrant.Cause);
        holder.ManoeuvreSpaces.Clear();
        var unblocked = sim.SaveToJson();
        var liveDecision = Admit(sim, entrant, node);
        var reloaded = CitySimulation.FromJson(unblocked);
        var reloadEntrant = reloaded.State.Vehicles.Single(v => v.Id == entrant.Id);
        var reloadNode = reloaded.State.Roads.Single(r => r.Id == node.Id);
        Assert.Equal(liveDecision, Admit(reloaded, reloadEntrant, reloadNode));
        Assert.DoesNotContain(holder.Id.ToString(), entrant.Cause);
        Assert.DoesNotContain(holder.Id.ToString(), reloadEntrant.Cause);

        // Test a fresh admission, not a vehicle already admitted to the area.
        entrant.ManoeuvreSpaces.Clear();
        entrant.IntersectionClaim = null;
        entrant.IntersectionEnteredAt = null;
        holder.ManoeuvreSpaces.Add(node.Id);
        Assert.False(Admit(sim, entrant, node));
        Assert.Contains(holder.Id.ToString(), entrant.Cause);
        var blocked = CitySimulation.FromJson(sim.SaveToJson());
        Assert.False(Admit(blocked,
            blocked.State.Vehicles.Single(v => v.Id == entrant.Id),
            blocked.State.Roads.Single(r => r.Id == node.Id)));
    }
    [Fact]
    public void Integrated_advance_after_external_blocker_mutation_matches_reloaded_state_at_each_checkpoint()
    {
        // Exercise the complete integrated tick after direct external writes.
        // Both instances use the CI-selected experimental mode; this is a
        // save/load causal gate, not a baseline-versus-opt-in speedup claim.
        var live = new CitySimulation(42);
        live.State.Roads.Add(new RoadNode
        {
            Id = 994101, Position = new Point(0, 0),
            Neighbours = new List<int> { 994102, 994103, 994104 }
        });
        live.State.Roads.Add(new RoadNode { Id = 994102, Position = new Point(4, 0), Neighbours = new List<int> { 994101 } });
        live.State.Roads.Add(new RoadNode { Id = 994103, Position = new Point(0, 4), Neighbours = new List<int> { 994101 } });
        live.State.Roads.Add(new RoadNode { Id = 994104, Position = new Point(-4, 0), Neighbours = new List<int> { 994101 } });
        live.State.Vehicles.Add(new Vehicle
        {
            Id = 994201, Position = new Point(0, 0), Status = VehicleStatus.Travelling
        });
        live.State.Vehicles.Add(new Vehicle
        {
            Id = 994202, Position = new Point(1, 0),
            Status = VehicleStatus.Travelling, IntersectionClaim = 994101
        });
        live = CitySimulation.FromJson(live.SaveToJson());
        live.Advance(2);
        var blocker = live.State.Vehicles.Single(v => v.Id == 994202);
        blocker.Position = new Point(40, 0);
        blocker.IntersectionClaim = null;

        for (var checkpoint = 0; checkpoint < 3; checkpoint++)
        {
            var restored = CitySimulation.FromJson(live.SaveToJson());
            live.Advance(2);
            restored.Advance(2);
            Assert.Equal(restored.SaveToJson(), live.SaveToJson());
        }
    }


    [Fact]
    public void Integrated_advance_rechecks_external_claim_only_mutation_across_reload()
    {
        // A caller may change a crossing reservation without moving the vehicle.
        // This catches indexes invalidated only by position or RoadRevision.
        var live = new CitySimulation(42);
        live.State.Roads.Add(new RoadNode
        {
            Id = 995101, Position = new Point(0, 0),
            Neighbours = new List<int> { 995102, 995103, 995104 }
        });
        live.State.Roads.Add(new RoadNode { Id = 995102, Position = new Point(4, 0), Neighbours = new List<int> { 995101 } });
        live.State.Roads.Add(new RoadNode { Id = 995103, Position = new Point(0, 4), Neighbours = new List<int> { 995101 } });
        live.State.Roads.Add(new RoadNode { Id = 995104, Position = new Point(-4, 0), Neighbours = new List<int> { 995101 } });
        live.State.Vehicles.Add(new Vehicle { Id = 995201, Position = new Point(0, 0), Status = VehicleStatus.Travelling });
        live.State.Vehicles.Add(new Vehicle
        {
            Id = 995202, Position = new Point(1, 0),
            Status = VehicleStatus.Travelling, IntersectionClaim = 995101
        });
        live = CitySimulation.FromJson(live.SaveToJson());
        live.Advance(2);
        var blocker = live.State.Vehicles.Single(v => v.Id == 995202);
        blocker.IntersectionClaim = blocker.IntersectionClaim is null ? 995101 : null;
        for (var checkpoint = 0; checkpoint < 3; checkpoint++)
        {
            var restored = CitySimulation.FromJson(live.SaveToJson());
            live.Advance(2);
            restored.Advance(2);
            Assert.Equal(restored.SaveToJson(), live.SaveToJson());
        }
    }
}
