using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC: "Pedestres atravessam vias urbanas apenas em faixas de pedestres" and
// shared crosswalks (2026-10-13). Cities are built with the normal APIs; direct
// state is used only for a road layout the tools cannot isolate.
public sealed class PedestrianSidewalkTests
{
    [Fact]
    public void A_sidewalk_change_at_the_highway_end_crosses_at_the_urban_paint()
    {
        var sim = new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success);
        Assert.True(sim.PlaceRoad(new(-192, 0), new(-120, 0)).Success);
        Assert.True(sim.PlaceRoad(new(-228, 0), new(-228, 40)).Success);
        var placed = sim.PlaceBuilding(BuildingKind.House, new(-243, 18), -Math.PI / 2); Assert.True(placed.Success);
        sim.Advance(7200);
        var home = sim.State.Buildings.Single(b => b.Id == placed.Id); Assert.True(home.Completed,PavingTests.Diagnostic(sim));
        var origin = new Point(-188, -4.5); var before = sim.SaveToJson();

        var route = sim.PedestrianRoute(origin, home);
        Assert.NotEmpty(route); Assert.Equal(origin, route[0]); Assert.Equal(home.Door, route[^1]);
        Assert.Empty(WalkOracle.UnpaintedCrossings(sim.State, route)); Assert.Empty(WalkOracle.ThroughBuildings(sim.State, route));
        // The street beside the highway end is crossed inside its real band.
        var paint = sim.State.Roads.Single(r => r.Position == new Point(-188, 0));
        var band = Assert.Single(CitySimulation.CrosswalkLocations(paint, sim.State));
        var crossings = AxisCrossings(route, horizontalAxis: 0).Where(x => x >= -192).ToArray();
        Assert.NotEmpty(crossings);
        Assert.All(crossings, x => Assert.InRange(x, band.Centre.X - 1.95, band.Centre.X + 1.95));
        Assert.Equal(before, sim.SaveToJson());
        var restored = CitySimulation.FromJson(before);
        Assert.Equal(route, restored.PedestrianRoute(origin, restored.State.Buildings.Single(b => b.Id == home.Id)));
        sim.CheckInvariants();
    }

    [Fact]
    public void A_walker_follows_the_rebuilt_route_physically_and_waits_at_the_real_crossing()
    {
        var sim = new CitySimulation(42, new() { ExternalFamilyCandidates = 0, ExternalCompanyCandidates = 0 });
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success);
        Assert.True(sim.PlaceRoad(new(-192, 0), new(-120, 0)).Success);
        Assert.True(sim.PlaceRoad(new(-228, 0), new(-228, 40)).Success);
        var placed = sim.PlaceBuilding(BuildingKind.House, new(-243, 18), -Math.PI / 2); Assert.True(placed.Success);
        sim.Advance(7200);
        var home = sim.State.Buildings.Single(b => b.Id == placed.Id);
        var origin = new Point(-188, -4.5); var route = sim.PedestrianRoute(origin, home);
        var paint = sim.State.Roads.Single(r => r.Position == new Point(-188, 0));
        // Fixture walker on a real route; no money, goods or vehicle is added.
        var walker = new Citizen { Id = sim.State.NextId++, Name = "Pedestre de fixture", Age = 30, Resident = false, InsideMap = true,
            Position = origin, DestinationId = home.Id, Purpose = JourneyPurpose.Home, Path = route,
            PathRoadRevision = sim.State.RoadRevision, NextDecisionAt = double.MaxValue };
        sim.State.Citizens.Add(walker); sim.State.Wallets[walker.Account] = 0;
        var money = sim.MonetaryTotal; var usedPaint = false;
        var limit = route.Zip(route.Skip(1)).Sum(p => p.First.Distance(p.Second)) / sim.State.Tuning.WalkingSpeed + 120;
        for (var elapsed = 0.0; walker.Moving && elapsed < limit; elapsed += .25)
        {
            var step = walker.Position; sim.Advance(.25);
            Assert.True(step.Distance(walker.Position) <= sim.State.Tuning.WalkingSpeed * .25 + 1e-6, $"Salto físico: {step} -> {walker.Position}");
            Assert.Empty(WalkOracle.UnpaintedCrossings(sim.State, [step, walker.Position])); Assert.Empty(WalkOracle.ThroughBuildings(sim.State, [step, walker.Position]));
            usedPaint |= walker.CrossingNodeId == paint.Id;
        }
        Assert.False(walker.Moving, walker.Activity); Assert.Equal(home.Door, walker.Position);
        Assert.True(usedPaint, "A travessia da rua não passou pela faixa real ao lado da rodovia.");
        Assert.Null(walker.CrossingNodeId); Assert.Equal(money, sim.MonetaryTotal);
        sim.CheckInvariants();
    }

    [Fact]
    public void A_door_beyond_an_outer_corner_walks_around_the_curb_instead_of_down_the_lane()
    {
        var sim = new CitySimulation(42);
        var hall = sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)); Assert.True(hall.Success);
        Assert.True(sim.PlaceRoad(new(-192, 0), new(-100, 40)).Success);
        var placed = sim.PlaceBuilding(BuildingKind.House, new(-85, 0), Math.PI / 2); Assert.True(placed.Success);
        sim.Advance(7200);
        var house = sim.State.Buildings.Single(b => b.Id == placed.Id); Assert.True(house.Completed);
        var cityHall = sim.State.Buildings.Single(b => b.Id == hall.Id);
        var corner = sim.State.Roads.Single(r => r.Position == new Point(-100, 0));
        Assert.Equal(corner.Id, sim.Access(house)!.Id); Assert.Equal(2, corner.Neighbours.Count);

        var leaving = sim.PedestrianRoute(house.Door, cityHall);
        var arriving = sim.PedestrianRoute(cityHall.Door, house);
        foreach (var (route, from, to) in new[] { (leaving, house.Door, cityHall.Door), (arriving, cityHall.Door, house.Door) })
        {
            Assert.NotEmpty(route); Assert.Equal(from, route[0]); Assert.Equal(to, route[^1]);
            Assert.Empty(WalkOracle.UnpaintedCrossings(sim.State, route)); Assert.Empty(WalkOracle.ThroughBuildings(sim.State, route));
        }
        sim.CheckInvariants();
    }

    [Fact]
    public void A_curb_under_a_separate_road_end_is_not_a_lane_to_walk_along()
    {
        // Explicit layout from a real save: a street ends 8 m above another
        // one, without a connecting cell, and has paint on its first segment.
        var initial = new CitySimulation(42);
        for (var x = -40; x <= 40; x += 4) initial.State.Roads.Add(new() { Id = 2000 + x, Position = new(x, 0) });
        for (var z = 8; z <= 48; z += 4) initial.State.Roads.Add(new() { Id = 3000 + z, Position = new(0, z), Crosswalk = z == 12 });
        var home = new Building { Id = 4000, Kind = BuildingKind.House, Position = new(-15, 30), Rotation = -Math.PI / 2, Completed = true };
        initial.State.Buildings.Add(home);
        var sim = new CitySimulation(initial.State);
        Assert.Equal(3028, sim.Access(home)!.Id);
        var origin = new Point(0, 4.55);
        Assert.Empty(WalkOracle.UnpaintedCrossings(sim.State, [origin, origin]));

        var route = sim.PedestrianRoute(origin, home);
        Assert.NotEmpty(route); Assert.Equal(origin, route[0]); Assert.Equal(home.Door, route[^1]);
        Assert.Empty(WalkOracle.UnpaintedCrossings(sim.State, route)); Assert.Empty(WalkOracle.ThroughBuildings(sim.State, route));
        // Nothing joins the two streets: the main street does not reach the stub.
        var shop = new Building { Id = 4001, Kind = BuildingKind.House, Position = new(24, -14), Completed = true };
        sim.State.Buildings.Add(shop);
        Assert.Empty(sim.PedestrianRoute(home.Door, shop));
        sim.CheckInvariants();
    }

    [Fact]
    public void Where_an_urban_road_meets_the_highway_walkers_still_cross_it_beside_the_junction()
    {
        var sim = new CitySimulation(42);
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success);
        Assert.True(sim.PlaceRoad(new(-228, 0), new(-228, 40)).Success);
        Assert.True(sim.PlaceRoad(new(-228, 0), new(-228, -40)).Success);
        var north = sim.PlaceBuilding(BuildingKind.House, new(-243, 24), -Math.PI / 2); Assert.True(north.Success);
        var south = sim.PlaceBuilding(BuildingKind.House, new(-243, -24), -Math.PI / 2); Assert.True(south.Success);
        sim.Advance(7200);
        var from = sim.State.Buildings.Single(b => b.Id == south.Id); var to = sim.State.Buildings.Single(b => b.Id == north.Id);
        Assert.True(from.Completed && to.Completed);
        Assert.True(sim.State.Roads.Single(r => r.Position == new Point(-228, 0)).Highway);

        var route = sim.PedestrianRoute(from.Door, to);
        Assert.NotEmpty(route); Assert.Equal(to.Door, route[^1]);
        Assert.Empty(WalkOracle.UnpaintedCrossings(sim.State, route)); Assert.Empty(WalkOracle.ThroughBuildings(sim.State, route));
        Assert.All(AxisCrossings(route, horizontalAxis: 0), x => Assert.InRange(x, -228 - 6.5, -228 + 6.5));
        Assert.True(route.Zip(route.Skip(1)).Sum(p => p.First.Distance(p.Second)) < 70);
        sim.CheckInvariants();
    }

    [Fact]
    public void A_street_leaving_the_highway_southwards_is_still_reached_along_the_highway_side()
    {
        var sim = new CitySimulation(42);
        var hall = sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)); Assert.True(hall.Success);
        Assert.True(sim.PlaceRoad(new(-192, 0), new(-120, 0)).Success);
        Assert.True(sim.PlaceRoad(new(-228, 0), new(-228, -40)).Success);
        var placed = sim.PlaceBuilding(BuildingKind.House, new(-243, -24), -Math.PI / 2); Assert.True(placed.Success);
        sim.Advance(7200);
        var house = sim.State.Buildings.Single(b => b.Id == placed.Id); Assert.True(house.Completed);
        var origin = sim.State.Buildings.Single(b => b.Id == hall.Id).Door;

        var route = sim.PedestrianRoute(origin, house);
        Assert.NotEmpty(route); Assert.Equal(house.Door, route[^1]);
        Assert.Empty(WalkOracle.UnpaintedCrossings(sim.State, route)); Assert.Empty(WalkOracle.ThroughBuildings(sim.State, route));
        // The highway is crossed only beside the T, where the street leaves it.
        Assert.All(AxisCrossings(route, horizontalAxis: 0), x => Assert.InRange(x, -228 - 6.5, -228 + 6.5));
        sim.CheckInvariants();
    }

    static IEnumerable<double> AxisCrossings(IReadOnlyList<Point> route, double horizontalAxis)
    {
        foreach (var (a, b) in route.Zip(route.Skip(1)))
        {
            var first = a.Z - horizontalAxis; var second = b.Z - horizontalAxis;
            if (first * second >= 0) continue;
            yield return a.X + (b.X - a.X) * (first / (first - second));
        }
    }
}

// Independent geometric oracle: the asphalt rectangles CityWorld draws for
// each road cell, the five-stripe bands and, on the unpainted highway, the
// stretch where an urban road branches off it. It samples the walk every
// 5 cm; a walk that starts inside a lane is exempt until it first leaves it.
static class WalkOracle
{
    sealed record Box(double X0, double X1, double Z0, double Z1)
    {
        public bool Has(Point p) => p.X > X0 + 1e-6 && p.X < X1 - 1e-6 && p.Z > Z0 + 1e-6 && p.Z < Z1 - 1e-6;
        public bool Covers(Point p) => p.X >= X0 - 1e-6 && p.X <= X1 + 1e-6 && p.Z >= Z0 - 1e-6 && p.Z <= Z1 + 1e-6;
    }

    // Samples strictly inside a building footprint (5 cm inset); a door lies on
    // its edge, so walks may start or end there.
    public static List<string> ThroughBuildings(CityState state, IReadOnlyList<Point> route)
    {
        var found = new List<string>();
        foreach (var (a, b) in route.Zip(route.Skip(1)))
        {
            var count = Math.Max(1, (int)Math.Ceiling(a.Distance(b) / .25));
            for (var k = 0; k <= count; k++)
            {
                var p = a + (b - a) * (k / (double)count);
                foreach (var lot in state.Buildings.Where(l => !l.Removed))
                {
                    var d = p - lot.Position; var x = d.X * Math.Cos(lot.Rotation) - d.Z * Math.Sin(lot.Rotation); var z = d.X * Math.Sin(lot.Rotation) + d.Z * Math.Cos(lot.Rotation);
                    if (Math.Abs(x) < lot.Definition.Width / 2 - .05 && Math.Abs(z) < lot.Definition.Depth / 2 - .05) found.Add($"{p} dentro do prédio {lot.Id}");
                }
            }
        }
        return found;
    }

    public static List<string> UnpaintedCrossings(CityState state, IReadOnlyList<Point> route)
    {
        var roads = state.Roads.ToDictionary(r => r.Id); var half = state.Tuning.RoadWidth / 2; var step = state.Tuning.RoadStep;
        var asphalt = state.Tuning.RoadWidth * 6.5 / 10.5; var shoulder = half - asphalt / 2; var side = half - .7;
        var urban = new List<Box>(); var highway = new List<Box>(); var bands = new List<Box>(); var zones = new List<Box>();
        foreach (var road in state.Roads)
        {
            var lanes = road.Highway ? highway : urban;
            var horizontal = road.Highway || road.Neighbours.Any(id => Math.Abs(roads[id].Position.X - road.Position.X) > .1);
            var vertical = road.Neighbours.Any(id => Math.Abs(roads[id].Position.Z - road.Position.Z) > .1);
            Box At(double x, double z, double w, double d) => new(x - w / 2, x + w / 2, z - d / 2, z + d / 2);
            var (px, pz) = (road.Position.X, road.Position.Z);
            if (horizontal && vertical)
            {
                lanes.Add(At(px, pz, asphalt, asphalt));
                foreach (var delta in road.Neighbours.Select(id => roads[id].Position - road.Position))
                {
                    if (Math.Abs(delta.X) > .1) lanes.Add(At(px + Math.Sign(delta.X) * (asphalt + shoulder) / 2, pz, shoulder, asphalt));
                    else lanes.Add(At(px, pz + Math.Sign(delta.Z) * (asphalt + shoulder) / 2, asphalt, shoulder));
                }
            }
            else if (vertical) lanes.Add(At(px, pz, asphalt, step));
            else lanes.Add(At(px, pz, step, asphalt));
            foreach (var band in CitySimulation.CrosswalkLocations(road, state))
                bands.Add(band.HorizontalRoad ? At(band.Centre.X, band.Centre.Z, 3.9, asphalt) : At(band.Centre.X, band.Centre.Z, asphalt, 3.9));
            if (road.Highway && road.Neighbours.Any(id => !roads[id].Highway && Math.Abs(roads[id].Position.Z - pz) > .1))
                zones.Add(At(px, pz, (side + 1.95) * 2, asphalt));
        }
        bool Off(Point p) => urban.Any(b => b.Has(p)) && !bands.Any(b => b.Covers(p)) || highway.Any(b => b.Has(p)) && !zones.Any(b => b.Covers(p));
        var found = new List<string>(); var recovering = Off(route[0]);
        foreach (var (a, b) in route.Zip(route.Skip(1)))
        {
            var count = Math.Max(1, (int)Math.Ceiling(a.Distance(b) / .05)); var bad = 0.0; Point? where = null;
            for (var k = 1; k <= count; k++)
            {
                var p = a + (b - a) * ((k - .5) / count); var off = Off(p);
                if (recovering) { if (off) continue; recovering = false; }
                if (off) { bad += a.Distance(b) / count; where ??= p; }
            }
            if (bad > .05) found.Add($"{a} -> {b}: {bad:0.##} m fora da faixa perto de {where}");
        }
        return found;
    }
}
