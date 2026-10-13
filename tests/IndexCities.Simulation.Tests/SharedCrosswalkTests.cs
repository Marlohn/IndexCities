using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// SPEC: Vias, calçadas e estacionamento; aprovação humana de 2026-10-13.
// Obras, dinheiro, cargas e combustível seguem as APIs normais. Candidatos externos
// desativados e pedestres/veículos posicionados explicitamente são fixtures, não uma partida.
public sealed class SharedCrosswalkTests
{
    sealed record Street(Point Start, Point Along, Point Across, double Length)
    {
        public Point At(double along, double across = 0) => Start + Along * along + Across * across;
        public double Longitudinal(Point p) => (p.X - Start.X) * Along.X + (p.Z - Start.Z) * Along.Z;
        public double Lateral(Point p) => (p.X - Start.X) * Across.X + (p.Z - Start.Z) * Across.Z;
    }

    sealed record Neighbourhood(CitySimulation Sim, Street Street, Building[] Houses);

    [Fact]
    public void L_entrance_beside_the_highway_never_paints_a_crossing_over_its_lanes()
    {
        var sim=NewFixtureCity();
        BuildRoad(sim,new(-224,0),new(-224,4));
        BuildRoad(sim,new(-224,4),new(-216,4));
        var corner=sim.State.Roads.Single(r=>r.Position==new Point(-224,4));
        Assert.False(corner.Crosswalk);
        Assert.Empty(CitySimulation.CrosswalkLocations(corner,sim.State));
        sim.CheckInvariants();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Nearby_doors_on_both_sides_share_crosswalks_instead_of_one_per_access(bool vertical)
    {
        var city = NeighbourhoodWithSixDoors(vertical);
        var accesses = city.Houses.Select(h => city.Sim.Access(h)!.Id).Distinct().ToArray();
        Assert.Equal(city.Houses.Length, accesses.Length);
        var markings = StreetCrosswalks(city.Sim, city.Street);

        Assert.NotEmpty(markings);
        Assert.True(markings.Length < accesses.Length,
            $"{accesses.Length} acessos próximos receberam {markings.Length} faixas: {Describe(markings)}");
        city.Sim.CheckInvariants();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void All_opposite_door_pairs_walk_through_shared_markings_and_keep_their_real_endpoints(bool vertical)
    {
        var city = NeighbourhoodWithSixDoors(vertical);
        var before = city.Sim.SaveToJson();
        foreach (var source in city.Houses)
        foreach (var destination in city.Houses.Where(h =>
                     Math.Sign(city.Street.Lateral(h.Door)) != Math.Sign(city.Street.Lateral(source.Door))))
        {
            var route = city.Sim.PedestrianRoute(source.Door, destination);
            var crossings = AssertStreetRoute(city.Sim, city.Street, source.Door, destination, route);
            Assert.NotEmpty(crossings);
        }

        Assert.Equal(before, city.Sim.SaveToJson());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_unmarked_access_remains_reachable_by_walking_to_a_valid_crosswalk(bool vertical)
    {
        var city = NeighbourhoodWithSixDoors(vertical);
        var destination = city.Houses.FirstOrDefault(h => !city.Sim.Access(h)!.Crosswalk);
        Assert.True(destination != null, "O bairro precisa ter acesso sem faixa própria para exercitar o desvio físico.");
        var source = city.Houses.Where(h =>
                Math.Sign(city.Street.Lateral(h.Door)) != Math.Sign(city.Street.Lateral(destination!.Door)))
            .MinBy(h => h.Door.Distance(destination!.Door))!;
        var walker = FixtureWalker(city.Sim, source.Door, destination!);
        var crossed = WalkToDoor(city.Sim, city.Street, walker, destination!);

        Assert.NotEmpty(crossed);
        Assert.DoesNotContain(city.Sim.Access(destination!)!.Id, crossed);
        city.Sim.CheckInvariants();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Removing_per_door_paint_does_not_erase_a_physically_possible_route_to_the_remaining_crosswalk(bool vertical)
    {
        var city = NeighbourhoodWithSixDoors(vertical);
        // Explicit paint-only fixture: keep one physical straight crossing 16 m
        // past the destination access. This is not a rule for automatic spacing.
        foreach (var node in city.Sim.State.Roads.Where(r => !r.Highway && Math.Abs(city.Street.Lateral(r.Position)) < .01))
            node.Crosswalk = node.Position == city.Street.At(72);
        var source = city.Houses[0].Door;
        var destination = city.Houses[3];
        Assert.False(city.Sim.Access(destination)!.Crosswalk);
        Assert.Single(StreetCrosswalks(city.Sim, city.Street));
        var before = city.Sim.SaveToJson();
        var restored = CitySimulation.FromJson(before);

        var route = city.Sim.PedestrianRoute(source, destination);
        var crossings = AssertStreetRoute(city.Sim, city.Street, source, destination, route);
        Assert.Single(crossings);
        Assert.Equal(route, restored.PedestrianRoute(source, restored.State.Buildings.Single(b => b.Id == destination.Id)));
        Assert.Equal(before, city.Sim.SaveToJson());
        Assert.Equal(before, restored.SaveToJson());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_L_bend_without_adjacent_building_access_does_not_generate_a_crosswalk(bool reverse)
    {
        var sim = NewFixtureCity();
        BuildRoad(sim, new(-224, 0), new(-120, 96), reverse);
        var corner = reverse ? new Point(-224, 96) : new Point(-120, 0);
        var bend = sim.State.Roads.Single(r => r.Position == corner);
        Assert.Equal(2, bend.Neighbours.Count);
        Assert.All(sim.State.Buildings, b => Assert.True(sim.Access(b) == null || sim.Access(b)!.Position.Distance(corner) > 24));

        Assert.False(bend.Crosswalk, $"A curva em L criou faixa em {corner} sem acesso adjacente.");
        Assert.DoesNotContain(sim.State.Roads,
            r => r.Crosswalk && r.Position.Distance(corner) <= sim.State.Tuning.RoadWidth);
        sim.CheckInvariants();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void A_continuous_sidewalk_around_an_L_remains_walkable_without_cutting_a_lane(bool reverse, bool inner)
    {
        var sim = NewFixtureCity();
        BuildRoad(sim, new(-224, 0), new(-120, 96), reverse);
        var site = reverse
            ? (new Point(-144, inner ? 82 : 110), inner ? Math.PI : 0)
            : (new Point(inner ? -134 : -106, 72), inner ? -Math.PI / 2 : Math.PI / 2);
        var house = BuildHouses(sim, [site]).Single();
        var source = reverse
            ? new Point(inner ? -217.75 : -230.25, 60)
            : new Point(-176, inner ? 6.25 : -6.25);
        var route = sim.PedestrianRoute(source, house);
        AssertEndpoints(source, house, route);
        var streets = reverse
            ? new[] { new Street(new(-224, 0), new(0, 1), new(1, 0), 96), new Street(new(-224, 96), new(1, 0), new(0, 1), 104) }
            : new[] { new Street(new(-224, 0), new(1, 0), new(0, 1), 104), new Street(new(-120, 0), new(0, 1), new(1, 0), 96) };

        foreach (var point in Samples(route))
        foreach (var street in streets)
            Assert.False(InLaneInterior(sim, street, point), $"A calçada {(inner ? "interna" : "externa")} cortou a pista em {point}.");
        sim.CheckInvariants();
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    public void T_and_four_entry_junctions_keep_crosswalks_and_real_routes(bool vertical, int entries)
    {
        var city = StreetWithoutHouses(vertical);
        var junctionPosition = city.Street.At(112);
        BuildRoad(city.Sim, junctionPosition, junctionPosition + city.Street.Across * 48);
        if (entries == 4)
            BuildRoad(city.Sim, junctionPosition, junctionPosition - city.Street.Across * 48);
        var junction = city.Sim.State.Roads.Single(r => r.Position == junctionPosition);
        Assert.Equal(entries, junction.Neighbours.Count);
        var rotation = vertical ? -Math.PI / 2 : 0;
        var house = BuildHouses(city.Sim, [(city.Street.At(144, 14), rotation)]).Single();
        Assert.True(junction.Crosswalk, $"Cruzamento com {entries} entradas perdeu a travessia.");
        var source = city.Street.At(104, -6.25);
        var crossings = AssertStreetRoute(city.Sim, city.Street, source, house, city.Sim.PedestrianRoute(source, house));

        Assert.NotEmpty(crossings);
        city.Sim.CheckInvariants();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Saving_on_the_approach_and_inside_a_crosswalk_preserves_routes_markings_and_continuation(bool vertical)
    {
        var city = NeighbourhoodWithSixDoors(vertical);
        var destination = city.Houses[3];
        var walker = FixtureWalker(city.Sim, city.Houses[0].Door, destination);
        AssertStreetRoute(city.Sim, city.Street, walker.Position, destination, walker.Path);
        AdvanceWalker(city.Sim, city.Street, walker, .25);
        Assert.True(walker.Moving);
        AssertRoundTrip(city, walker, destination);

        ReachLaneInterior(city, walker);
        Assert.True(walker.Moving);
        var restored = AssertRoundTrip(city, walker, destination);
        var restoredWalker = restored.State.Citizens.Single(c => c.Id == walker.Id);
        var timeLimit = RouteLength(walker.Path) / city.Sim.State.Tuning.WalkingSpeed + 120;
        for (var elapsed = 0.0; walker.Moving && elapsed < timeLimit; elapsed += .25)
        {
            AdvanceWalker(city.Sim, city.Street, walker, .25);
            AdvanceWalker(restored, city.Street, restoredWalker, .25);
            Assert.Equal(city.Sim.SaveToJson(), restored.SaveToJson());
        }

        Assert.False(walker.Moving, walker.Activity);
        Assert.False(restoredWalker.Moving, restoredWalker.Activity);
        Assert.Equal(destination.Door, walker.Position);
        Assert.Equal(destination.Door, restoredWalker.Position);
        Assert.Null(walker.CrossingNodeId);
        Assert.Null(restoredWalker.CrossingNodeId);
        Assert.Equal(city.Sim.SaveToJson(), restored.SaveToJson());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_real_truck_waits_for_a_physical_walker_at_a_straight_crosswalk_then_continues(bool vertical)
    {
        var city = NeighbourhoodWithSixDoors(vertical);
        var walker = FixtureWalker(city.Sim, city.Houses[0].Door, city.Houses[3]);
        ReachLaneInterior(city, walker);
        Assert.NotNull(walker.CrossingNodeId);
        var crossing = city.Sim.State.Roads.Single(r => r.Id == walker.CrossingNodeId);
        Assert.True(crossing.Crosswalk);
        Assert.False(crossing.Highway);
        Assert.Equal(2, crossing.Neighbours.Count);
        var along = city.Street.Longitudinal(crossing.Position);
        var start = city.Sim.State.Roads.Single(r => r.Position == city.Street.At(along - city.Sim.State.Tuning.RoadStep));
        var finish = city.Sim.State.Roads.Single(r => r.Position == city.Street.At(along + city.Sim.State.Tuning.RoadStep));

        // Reuse a paid, fuelled external truck after its completed deliveries.
        // Only its traffic situation is staged; no goods, fuel or money are added.
        var truck = city.Sim.State.Vehicles.First(v => v.External && v.Status == VehicleStatus.Outside && v.Fuel > 1);
        truck.Position = start.Position + city.Street.Across * -1.65;
        truck.Heading = Math.Atan2(city.Street.Along.X, city.Street.Along.Z);
        truck.Route = city.Sim.FindRoadRoute(start.Id, finish.Id);
        truck.RouteIndex = 1;
        truck.DockPosition = null;
        truck.ShipmentId = null;
        truck.IntersectionClaim = null;
        truck.WaitingSince = 0;
        truck.Status = VehicleStatus.Travelling;
        var truckPosition = truck.Position;
        var fuel = truck.Fuel;
        var money = city.Sim.MonetaryTotal;
        var cargo = truck.Cargo.OrderBy(kv => kv.Key).ToArray();

        AdvanceWalker(city.Sim, city.Street, walker, .25);
        Assert.Equal(truckPosition, truck.Position);
        Assert.Equal(fuel, truck.Fuel);
        WalkToDoor(city.Sim, city.Street, walker, city.Houses[3]);
        city.Sim.Advance(5);
        Assert.True(truck.Position.Distance(truckPosition) > 1, truck.Cause);
        Assert.True(truck.Fuel < fuel);
        Assert.Equal(cargo, truck.Cargo.OrderBy(kv => kv.Key).ToArray());
        Assert.Equal(money, city.Sim.MonetaryTotal);
        city.Sim.CheckInvariants();
    }

    [Fact]
    public void Highway_frontage_does_not_become_building_access_or_a_pedestrian_crosswalk()
    {
        var sim = NewFixtureCity();
        var before = sim.SaveToJson();
        var quote = sim.QuoteBuilding(BuildingKind.House, new(-232, 14));
        Assert.True(quote.PhysicallyValid, quote.Message);
        Assert.False(quote.Accessible);
        Assert.Equal(before, sim.SaveToJson());
        var placed = sim.PlaceBuilding(BuildingKind.House, new(-232, 14));
        Assert.True(placed.Success, placed.Message);
        var house = sim.State.Buildings.Single(b => b.Id == placed.Id);
        var project = sim.State.Projects.Single(p => p.Id == house.ProjectId);
        var balance = sim.CityBalance;
        sim.Advance(600);

        Assert.Null(sim.Access(house));
        Assert.False(sim.HasExteriorAccess(house));
        Assert.Empty(sim.PedestrianRoute(sim.State.Entry, house));
        Assert.False(house.Completed);
        Assert.Equal(0, project.Progress);
        Assert.Equal(0, project.CostPaid);
        Assert.Empty(project.Delivered);
        Assert.DoesNotContain(sim.State.Shipments, s => s.ProjectId == project.Id);
        Assert.Equal(balance, sim.CityBalance);
        Assert.All(sim.State.Roads.Where(r => r.Highway), r => Assert.False(r.Crosswalk));
        Assert.Equal(sim.SaveToJson(), CitySimulation.FromJson(sim.SaveToJson()).SaveToJson());
        sim.CheckInvariants();
    }

    static CitySimulation NewFixtureCity()
    {
        var sim = new CitySimulation(42, new() { ExternalFamilyCandidates = 0, ExternalCompanyCandidates = 0 });
        var placed = sim.PlaceBuilding(BuildingKind.CityHall, new(-80, -60));
        Assert.True(placed.Success, placed.Message);
        return sim;
    }

    static Neighbourhood StreetWithoutHouses(bool vertical)
    {
        var sim = NewFixtureCity();
        var street = vertical
            ? new Street(new(-112, 40), new(0, 1), new(-1, 0), 160)
            : new Street(new(-224, 80), new(1, 0), new(0, 1), 160);
        BuildRoad(sim, vertical ? new(-192, 0) : new(-224, 0), street.Start);
        BuildRoad(sim, street.Start, street.At(street.Length));
        return new(sim, street, []);
    }

    static Neighbourhood NeighbourhoodWithSixDoors(bool vertical)
    {
        var city = StreetWithoutHouses(vertical);
        var rotation = vertical ? -Math.PI / 2 : 0;
        // Staggered opposite doors create six distinct accesses in a 40 m cluster.
        var sites = new[] { 48.0, 64, 80 }.Select(u => (city.Street.At(u, 14), rotation))
            .Concat(new[] { 56.0, 72, 88 }.Select(u => (city.Street.At(u, -14), rotation + Math.PI))).ToArray();
        return city with { Houses = BuildHouses(city.Sim, sites) };
    }

    static void BuildRoad(CitySimulation sim, Point start, Point end, bool reverse = false)
    {
        var quote = sim.QuoteRoad(start, end, reverse);
        Assert.True(quote.PhysicallyValid, quote.Message);
        Assert.True(quote.Accessible, quote.Message);
        var balance = sim.CityBalance;
        var placed = sim.PlaceRoad(start, end, reverse);
        Assert.True(placed.Success, placed.Message);
        Assert.Equal(balance, sim.CityBalance);
        CompleteProjects(sim, [sim.State.Projects.Single(p => p.Id == placed.Id)]);
    }

    static Building[] BuildHouses(CitySimulation sim, (Point Position, double Rotation)[] sites)
    {
        var houses = new List<Building>();
        foreach (var (position, rotation) in sites)
        {
            var quote = sim.QuoteBuilding(BuildingKind.House, position, rotation);
            Assert.True(quote.PhysicallyValid, quote.Message);
            Assert.True(quote.Accessible, quote.Message);
            var placed = sim.PlaceBuilding(BuildingKind.House, position, rotation);
            Assert.True(placed.Success, placed.Message);
            houses.Add(sim.State.Buildings.Single(b => b.Id == placed.Id));
        }
        CompleteProjects(sim, houses.Select(b => sim.State.Projects.Single(p => p.Id == b.ProjectId)).ToArray());
        Assert.All(houses, h => Assert.True(h.Completed));
        return houses.ToArray();
    }

    static void CompleteProjects(CitySimulation sim, WorkProject[] projects)
    {
        for (var elapsed = 0; elapsed < 7200 && projects.Any(p => p.Status != WorkStatus.Completed); elapsed += 2)
            sim.Advance(2);
        foreach (var project in projects)
        {
            Assert.True(project.Status == WorkStatus.Completed, $"Obra #{project.Id}: {project.Status}; {project.Cause}");
            Assert.True(project.CostPaid > 0);
            foreach (var (resource, required) in project.Required)
                Assert.Equal(required, project.Delivered.GetValueOrDefault(resource), 5);
            Assert.Contains(sim.State.Ledger, m => m.EntityId == project.Id && m.From == "city" && m.Amount > 0);
            Assert.Contains(sim.State.Shipments, s => s.ProjectId == project.Id && s.Complete);
        }
        for (var elapsed = 0; elapsed < 7200 && sim.State.Vehicles.Any(v => v.External && v.Status != VehicleStatus.Outside); elapsed += 2)
            sim.Advance(2);
        Assert.All(sim.State.Vehicles.Where(v => v.External), v => Assert.True(v.Status == VehicleStatus.Outside, v.Cause));
        sim.CheckInvariants();
    }

    static Citizen FixtureWalker(CitySimulation sim, Point source, Building destination)
    {
        var route = sim.PedestrianRoute(source, destination);
        AssertEndpoints(source, destination, route);
        var walker = new Citizen
        {
            Id = sim.State.NextId++, Name = "Pedestre de fixture", Age = 30, Resident = false, InsideMap = true,
            Position = source, DestinationId = destination.Id, Purpose = JourneyPurpose.Home,
            Path = route, PathRoadRevision = sim.State.RoadRevision, NextDecisionAt = double.MaxValue
        };
        sim.State.Citizens.Add(walker);
        sim.State.Wallets[walker.Account] = 0;
        return walker;
    }

    static void ReachLaneInterior(Neighbourhood city, Citizen walker)
    {
        var limit = RouteLength(walker.Path) / city.Sim.State.Tuning.WalkingSpeed + 120;
        for (var elapsed = 0.0; walker.Moving && !InLaneInterior(city.Sim, city.Street, walker.Position) && elapsed < limit; elapsed += .25)
            AdvanceWalker(city.Sim, city.Street, walker, .25);
        Assert.True(InLaneInterior(city.Sim, city.Street, walker.Position), $"Não houve travessia física: {walker.Position}; {walker.Activity}");
    }

    static HashSet<int> WalkToDoor(CitySimulation sim, Street street, Citizen walker, Building destination)
    {
        var crossed = new HashSet<int>();
        var limit = RouteLength(walker.Path) / sim.State.Tuning.WalkingSpeed + 120;
        for (var elapsed = 0.0; walker.Moving && elapsed < limit; elapsed += .25)
            crossed.UnionWith(AdvanceWalker(sim, street, walker, .25));
        Assert.False(walker.Moving, $"{walker.Position}; {walker.Activity}");
        Assert.Equal(destination.Door, walker.Position);
        Assert.Null(walker.CrossingNodeId);
        return crossed;
    }

    static HashSet<int> AdvanceWalker(CitySimulation sim, Street street, Citizen walker, double seconds)
    {
        var before = walker.Position;
        sim.Advance(seconds);
        Assert.True(before.Distance(walker.Position) <= sim.State.Tuning.WalkingSpeed * seconds + .00001,
            $"Salto físico: {before} -> {walker.Position} em {seconds}s.");
        return AssertMarkedLaneSamples(sim, street, [before, walker.Position]);
    }

    static CitySimulation AssertRoundTrip(Neighbourhood city, Citizen walker, Building destination)
    {
        var json = city.Sim.SaveToJson();
        var restored = CitySimulation.FromJson(json);
        Assert.Equal(json, restored.SaveToJson());
        Assert.Equal(StreetCrosswalks(city.Sim, city.Street).Select(r => r.Id), StreetCrosswalks(restored, city.Street).Select(r => r.Id));
        var copy = restored.State.Buildings.Single(b => b.Id == destination.Id);
        Assert.Equal(city.Sim.PedestrianRoute(walker.Position, destination), restored.PedestrianRoute(walker.Position, copy));
        Assert.Equal(city.Sim.MonetaryTotal, restored.MonetaryTotal);
        return restored;
    }

    static HashSet<int> AssertStreetRoute(CitySimulation sim, Street street, Point source, Building destination, IReadOnlyList<Point> route)
    {
        AssertEndpoints(source, destination, route);
        return AssertMarkedLaneSamples(sim, street, route);
    }

    static void AssertEndpoints(Point source, Building destination, IReadOnlyList<Point> route)
    {
        Assert.NotEmpty(route);
        Assert.Equal(source, route[0]);
        Assert.Equal(destination.Door, route[^1]);
        Assert.All(route, p => Assert.True(double.IsFinite(p.X) && double.IsFinite(p.Z)));
    }

    static bool InLaneInterior(CitySimulation sim, Street street, Point point) =>
        street.Longitudinal(point) >= 0 && street.Longitudinal(point) <= street.Length &&
        Math.Abs(street.Lateral(point)) < sim.State.Tuning.RoadWidth / 4;

    static HashSet<int> AssertMarkedLaneSamples(CitySimulation sim, Street street, IReadOnlyList<Point> route)
    {
        var used = new HashSet<int>();
        var markings = StreetCrosswalks(sim, street);
        // Geometric oracle independent of PedestrianPath/CrossesLane: sample each
        // continuous segment inside the middle of the carriageway, not just endpoints.
        foreach (var point in Samples(route).Where(p => InLaneInterior(sim, street, p)))
        {
            var crossing = markings.Where(r =>
            {
                var along = street.Longitudinal(CitySimulation.CrosswalkCentre(r, sim.State));
                // A junction's marking surrounds its centre; a straight marking
                // occupies one road sample. This tolerance does not fix spacing.
                var tolerance = r.Neighbours.Count >= 3
                    ? sim.State.Tuning.RoadWidth / 2 + sim.State.Tuning.RoadStep / 2
                    : sim.State.Tuning.RoadStep / 2 + .5;
                return Math.Abs(street.Longitudinal(point) - along) <= tolerance;
            }).MinBy(r => r.Position.Distance(point));
            Assert.True(crossing != null, $"Pista atravessada sem faixa válida em {point}; faixas: {Describe(markings)}");
            used.Add(crossing!.Id);
        }
        return used;
    }

    static RoadNode[] StreetCrosswalks(CitySimulation sim, Street street) => sim.State.Roads
        .Where(r => r.Crosswalk && !r.Highway && Math.Abs(street.Lateral(r.Position)) < .01 &&
                    street.Longitudinal(r.Position) >= 0 && street.Longitudinal(r.Position) <= street.Length)
        .OrderBy(r => street.Longitudinal(r.Position)).ToArray();

    static IEnumerable<Point> Samples(IReadOnlyList<Point> route)
    {
        foreach (var (from, to) in route.Zip(route.Skip(1)))
        {
            var count = Math.Max(1, (int)Math.Ceiling(from.Distance(to) / .5));
            for (var i = 0; i <= count; i++) yield return from + (to - from) * (i / (double)count);
        }
    }

    static double RouteLength(IReadOnlyList<Point> route) => route.Zip(route.Skip(1)).Sum(pair => pair.First.Distance(pair.Second));
    static string Describe(IEnumerable<RoadNode> nodes) => string.Join(", ", nodes.Select(r => $"#{r.Id}@{r.Position}"));
}
