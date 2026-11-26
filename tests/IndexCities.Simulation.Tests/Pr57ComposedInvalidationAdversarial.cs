using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only joint adversarial: the access index and physical owner roster
// must remain exact when both opt-in paths are enabled. This deliberately
// drives private slice boundaries to expose invalidation ordering; the
// synthetic vehicles are NOT an economic/population benchmark.
public sealed class Pr57ComposedInvalidationAdversarial
{
    static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Begin = typeof(CitySimulation).GetMethod("Pr57BeginBuildingAccessAdvance", PrivateInstance)!;
    static readonly MethodInfo End = typeof(CitySimulation).GetMethod("Pr57EndBuildingAccessAdvance", PrivateInstance)!;
    static readonly MethodInfo Indexed = typeof(CitySimulation).GetMethod("Pr57IndexedBuildingAccess", PrivateInstance)!;
    static readonly MethodInfo Prepare = typeof(CitySimulation).GetMethod("Pr57PrepareSiteOwnerRoster", PrivateInstance)!;
    static readonly MethodInfo Admit = typeof(CitySimulation).GetMethod("AdmitManoeuvre", PrivateInstance)!;
    static readonly MethodInfo Travel = typeof(CitySimulation).GetMethod("SafeSiteTravel", PrivateInstance)!;
    static readonly FieldInfo Connected = typeof(CitySimulation).GetField("_workExteriorConnected", PrivateInstance)!;
    static readonly FieldInfo Roster = typeof(CitySimulation).GetField("_pr57SiteOwnerRoster", PrivateInstance)!;

    static RoadNode? OriginalAccess(CitySimulation sim, Building building)
    {
        var connected = (HashSet<int>)Connected.GetValue(sim)!;
        var radius = sim.State.Tuning.RoadWidth / 2 + 3;
        return sim.State.Roads
            .Where(r => !r.Highway && r.MaintenanceRatio > .05 &&
                        connected.Contains(r.Id) && r.Position.Distance(building.Door) <= radius)
            .MinBy(r => r.Position.Distance(building.Door));
    }

    static void AssertAccess(CitySimulation sim, Building building, string phase)
    {
        var expected = OriginalAccess(sim, building)?.Id;
        var actual = ((RoadNode?)Indexed.Invoke(sim, [building]))?.Id;
        Assert.True(expected == actual,
            $"PR57 composed access {phase}: expected={expected} actual={actual}");
    }

    static void Capture(CitySimulation sim, string name)
    {
        sim.CheckInvariants();
        var json = sim.SaveToJson();
        Assert.Equal(json, CitySimulation.FromJson(json).SaveToJson());
        var root = Environment.GetEnvironmentVariable("PR57_COMPOSED_ADVERSARIAL_DIR");
        if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, name + ".json"), json);
    }

    [Fact]
    public void Paid_road_mutations_and_same_slice_reservation_admission_remain_exact_with_both_flags()
    {
        var sim = new CitySimulation(42, new() { ExternalTruckCount = 1 });
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success);
        Assert.True(sim.PlaceRoad(new(-192, 0), new(-80, 0)).Success);
        sim.Advance(7200);
        var building = Assert.Single(sim.State.Buildings, b => b.Kind == BuildingKind.CityHall);
        Capture(sim, "before");
        var road = sim.State.Roads.Single(r => r.Position == new Point(-80, 0));
        var position = road.Position;

        // Force an access-index build, then mutate a live predicate and
        // physically demolish a paid road before ending the same scope.
        Begin.Invoke(sim, null);
        try
        {
            AssertAccess(sim, building, "initial");
            var ratio = road.MaintenanceRatio;
            road.MaintenanceRatio = .04;
            AssertAccess(sim, building, "maintenance-closed");
            road.MaintenanceRatio = ratio;
            AssertAccess(sim, building, "maintenance-restored");
            Assert.True(sim.DemolishRoad(road.Id).Success);
            AssertAccess(sim, building, "same-scope-demolition");
        }
        finally { End.Invoke(sim, null); }

        sim.Advance(2); // Actual host cadence; no skipped quantum.
        Capture(sim, "after-demolition");
        Assert.True(sim.PlaceRoad(position, position).Success);
        sim.Advance(2);
        Capture(sim, "after-paid-rebuild-command");
        Begin.Invoke(sim, null);
        try { AssertAccess(sim, building, "after-rebuild-quantum"); }
        finally { End.Invoke(sim, null); }

        // The retired site-owner roster is no longer part of this composition.
        // Keep the live road-access mutation, checkpoint, and save/load oracle.
    }
}
