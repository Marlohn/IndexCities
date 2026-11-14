using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57-only, artificial physics-state adversarial. No gameplay or population claim.
public sealed class Pr57StrandedGraphAdversarial
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Begin = typeof(CitySimulation).GetMethod("Pr57BeginStrandedGraph", Private)!;
    static readonly MethodInfo End = typeof(CitySimulation).GetMethod("Pr57EndStrandedGraph", Private)!;
    static readonly MethodInfo Original = typeof(CitySimulation).GetMethod("HeldInStrandedChain", Private)!;
    static readonly MethodInfo Candidate = typeof(CitySimulation).GetMethod("HeldBehindStranded", Private)!;

    [Fact]
    public void Exact_graph_matches_original_chains_cycles_and_external_changes()
    {
        if (Environment.GetEnvironmentVariable("PR57_STRANDED_GRAPH") != "1") return;
        var random = new Random(57);
        for (var trial = 0; trial < 12; trial++)
        {
            var sim = new CitySimulation(42);
            sim.State.Vehicles.Clear();
            for (var i = 0; i < 70; i++)
            {
                var status = i % 13 == 0 ? VehicleStatus.Stranded :
                    i % 7 == 0 ? VehicleStatus.Parked :
                    i % 11 == 0 ? VehicleStatus.Waiting : VehicleStatus.Travelling;
                sim.State.Vehicles.Add(new Vehicle
                {
                    Id = 800000 + i,
                    Status = status,
                    Position = new Point(random.Next(-8, 9) * 1.4, random.Next(-8, 9) * 1.4),
                    Heading = random.Next(4) * Math.PI / 2,
                    WaitingSince = i % 3 == 0 ? 0 : 1,
                    PassengerCar = i % 2 == 0
                });
            }

            void CompareFrame()
            {
                Begin.Invoke(sim, null);
                try
                {
                    foreach (var v in sim.State.Vehicles)
                        Assert.Equal((bool)Original.Invoke(sim, [v, false])!,
                                     (bool)Candidate.Invoke(sim, [v])!);
                    // A same-ID copy is not the frame's original object and
                    // must use the unindexed path, even if its pose differs.
                    var copy = new Vehicle
                    {
                        Id = sim.State.Vehicles[1].Id,
                        Position = new Point(900, 900),
                        Heading = Math.PI,
                        Status = VehicleStatus.Travelling,
                        WaitingSince = 1
                    };
                    Assert.Equal((bool)Original.Invoke(sim, [copy, false])!,
                                 (bool)Candidate.Invoke(sim, [copy])!);
                }
                finally { End.Invoke(sim, null); }
            }

            CompareFrame();
            // Publicly mutable state changes between Advance(2) calls:
            // no graph is allowed to survive this boundary.
            foreach (var v in sim.State.Vehicles.Take(12))
            {
                v.Position = new Point(random.Next(-20, 21), random.Next(-20, 21));
                v.WaitingSince = v.WaitingSince > 0 ? 0 : 2;
                if (v.Status == VehicleStatus.Travelling) v.Status = VehicleStatus.Stranded;
            }
            sim.State.Vehicles.Reverse();
            CompareFrame();
        }
    }
}
