using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57 #393: ARTIFICIAL, exact canonical-predicate counterexamples.
// The #391/#392 compact projection does not include route contents or
// admission contents. These tests do not benchmark integrated throughput.
public sealed class Pr57CompactPath393Tests
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static T Invoke<T>(CitySimulation sim, string name, params object[] args) =>
        (T)typeof(CitySimulation).GetMethod(name, Private)!.Invoke(sim, args)!;

    // Exactly the selected scalar fields captured by the #391 shadow.
    static (long X, long Z, long Heading, int Status, long Waiting,
        int RouteIndex, int RouteCount, int DetourCount, int TowPhase,
        int Machine, int ManoeuvreCount) Compact(Vehicle v) => (
        BitConverter.DoubleToInt64Bits(v.Position.X),
        BitConverter.DoubleToInt64Bits(v.Position.Z),
        BitConverter.DoubleToInt64Bits(v.Heading), (int)v.Status,
        BitConverter.DoubleToInt64Bits(v.WaitingSince), v.RouteIndex,
        v.Route.Count, v.Detour.Count, (int)v.TowPhase, (int)v.Machine,
        v.ManoeuvreSpaces.Count);

    [Fact]
    public void Same_length_route_content_edit_changes_canonical_local_admission()
    {
        var sim = new CitySimulation(42);
        var ids = sim.State.Roads.Take(3).Select(r => r.Id).ToArray();
        Assert.Equal(3, ids.Length);
        var dock = new Point(-160, -1.65);
        var v = new Vehicle {
            Id = sim.State.NextId++, Body = TruckBody.Cargo,
            Status = VehicleStatus.Travelling, Position = dock,
            Heading = Math.PI / 2, Route = [ids[0], ids[1]],
            RouteIndex = 0, ManoeuvreDock = dock, ManoeuvreDeparture = true,
            MovementAdmission = new LocalMovementAdmission {
                Route = [ids[0], ids[1]], SitePath = [],
                EndRouteIndex = 0, ReachesDock = true
            }
        };
        sim.State.Vehicles.Add(v);
        var before = Compact(v);
        Assert.True(Invoke<bool>(sim, "LeavesDock", v));
        Assert.True(Invoke<bool>(sim, "AdmissionPathMatches", v));
        Assert.True(Invoke<bool>(sim, "DockAdmitted", v, dock));

        // External in-place edit: same list reference, same length and index.
        v.Route[1] = ids[2];
        Assert.Equal(before, Compact(v));
        Assert.True(Invoke<bool>(sim, "LeavesDock", v));
        Assert.False(Invoke<bool>(sim, "AdmissionPathMatches", v));
        Assert.False(Invoke<bool>(sim, "DockAdmitted", v, dock));
    }

    [Fact]
    public void Same_length_route_content_edit_changes_canonical_dock_body_branch()
    {
        var sim = new CitySimulation(42);
        // Bootstrap roads are an east-west chain with 4m spacing.
        var roads = sim.State.Roads;
        Assert.True(roads.Count >= 6);
        var first = roads[4];
        var east = roads[5];
        var west = roads[3];
        Assert.Equal(first.Position.Z, east.Position.Z);
        Assert.Equal(first.Position.Z, west.Position.Z);
        Assert.True(east.Position.X > first.Position.X);
        Assert.True(west.Position.X < first.Position.X);
        var lane = first.Position + new Point(0, -1.65);
        var reader = new Vehicle {
            Id = sim.State.NextId++, Status = VehicleStatus.Travelling,
            Position = lane + new Point(-9, 0), Heading = Math.PI / 2
        };
        var candidate = new Vehicle {
            Id = sim.State.NextId++, Body = TruckBody.Cargo,
            Status = VehicleStatus.Travelling, Position = lane,
            Heading = Math.PI / 2, Route = [first.Id, east.Id],
            RouteIndex = 0
        };
        sim.State.Vehicles.AddRange([reader, candidate]);
        var before = Compact(candidate);
        Assert.True(Invoke<bool>(sim, "LeavesDock", candidate));
        Assert.True(Invoke<bool>(sim, "OnCarriageway", candidate));
        Assert.Null(Invoke<double?>(sim, "BodyAcrossLane", reader, candidate));

        candidate.Route[1] = west.Id; // Same compact state, different road axis.
        Assert.Equal(before, Compact(candidate));
        Assert.True(Invoke<bool>(sim, "LeavesDock", candidate));
        Assert.False(Invoke<bool>(sim, "OnCarriageway", candidate));
        var distance = Invoke<double?>(sim, "BodyAcrossLane", reader, candidate);
        Assert.NotNull(distance);
        Assert.Equal(9.0, distance!.Value, 9);
        // This proves a different canonical predicate branch, NOT a different
        // final lane decision: the normal OccupiesLane path may also hold.
    }
}
