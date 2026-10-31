using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #563: external/intra-step adversarials for the #560 blocked-tail extraction.
// This is an ARTIFICIAL causal regression test, not a scale benchmark.
[CollectionDefinition("Pr57BlockedTail560Environment", DisableParallelization = true)]
public sealed class Pr57BlockedTail560EnvironmentCollection { }

[Collection("Pr57BlockedTail560Environment")]
public sealed class Pr57BlockedTail560AdversarialTests
{
    static CitySimulation BlockedStreet()
    {
        var initial = new CitySimulation(42, new()
        {
            ExternalFamilyCandidates = 0, ExternalCompanyCandidates = 0,
            ExternalTruckCount = 0
        });
        for (var x = -24; x <= 24; x += 4)
            initial.State.Roads.Add(new RoadNode { Id = 1000 + x, Position = new Point(x, 40) });
        var sim = new CitySimulation(initial.State);
        sim.State.Vehicles.Clear();
        sim.State.EconomyNextAt = 86400;
        var east = Enumerable.Range(0, 13).Select(i => 976 + i * 4).ToList();
        sim.State.Vehicles.Add(new Vehicle
        {
            Id = sim.State.NextId++, External = true, PassengerCar = true,
            Position = new Point(-4, 38.35), Heading = Math.PI / 2,
            Fuel = 0, Status = VehicleStatus.Stranded,
            Route = east.ToList(), RouteIndex = 6
        });
        sim.State.Vehicles.Add(new Vehicle
        {
            Id = sim.State.NextId++, External = true, PassengerCar = true,
            Position = new Point(-8, 38.35), Heading = Math.PI / 2,
            Fuel = 40, Status = VehicleStatus.Travelling,
            Route = east.ToList(), RouteIndex = 5
        });
        return sim;
    }

    static void ExternalEdit(CitySimulation sim, int quantum)
    {
        var front = sim.State.Vehicles[0];
        var follower = sim.State.Vehicles[1];
        switch (quantum)
        {
            case 1:
                front.Position = new Point(front.Position.X + .2, front.Position.Z);
                front.Heading = -Math.PI / 2;
                break;
            case 2:
                follower.RouteIndex = Math.Min(follower.Route.Count - 1, follower.RouteIndex + 1);
                follower.DockPosition = new Point(-12, 38.35);
                break;
            case 3:
                var road = sim.State.Roads.Single(r => r.Id == 1000);
                road.Control = JunctionControl.Signal;
                road.Crosswalk = !road.Crosswalk;
                break;
            case 4:
                front.IntersectionClaim = 1000;
                if (!front.ManoeuvreSpaces.Contains(1000)) front.ManoeuvreSpaces.Add(1000);
                break;
            case 5:
                follower.Route.Reverse(); // in-place route writer, same List identity
                break;
            case 7:
                front.Status = VehicleStatus.Travelling;
                front.Fuel = 40;
                front.RouteIndex = Math.Min(front.Route.Count - 1, front.RouteIndex + 1);
                break;
            case 9:
                var node = sim.State.Roads.Single(r => r.Id == 1000);
                node.Position = new Point(node.Position.X + .125, node.Position.Z);
                break;
        }
    }

    [Fact]
    public void Blocked_tail_preserves_every_public_two_second_quantum_under_nonlocal_edits_and_save_load()
    {
        var original = Environment.GetEnvironmentVariable("PR57_BLOCKED_TAIL_560");
        try
        {
            var baseline = BlockedStreet();
            var candidate = CitySimulation.FromJson(baseline.SaveToJson());
            var calls = typeof(CitySimulation).GetField(
                "_pr57BlockedTail560Calls", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(calls);

            for (var quantum = 0; quantum < 12; quantum++)
            {
                ExternalEdit(baseline, quantum);
                ExternalEdit(candidate, quantum);
                Assert.Equal(baseline.SaveToJson(), candidate.SaveToJson());

                Environment.SetEnvironmentVariable("PR57_BLOCKED_TAIL_560", "0");
                baseline.Advance(2);
                Environment.SetEnvironmentVariable("PR57_BLOCKED_TAIL_560", "1");
                candidate.Advance(2);

                Assert.Equal(baseline.SaveToJson(), candidate.SaveToJson());
                baseline.CheckInvariants();
                candidate.CheckInvariants();
                if (quantum == 0)
                    Assert.True((long)calls!.GetValue(candidate)! > 0,
                        "The artificial queue must actually execute the #560 blocked tail.");

                if (quantum == 6)
                {
                    baseline = CitySimulation.FromJson(baseline.SaveToJson());
                    candidate = CitySimulation.FromJson(candidate.SaveToJson());
                    Assert.Equal(baseline.SaveToJson(), candidate.SaveToJson());
                }
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("PR57_BLOCKED_TAIL_560", original);
        }
    }
}
