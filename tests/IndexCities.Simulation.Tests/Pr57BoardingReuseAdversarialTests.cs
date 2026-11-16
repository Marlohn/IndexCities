using System.Reflection;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

// PR57: force the otherwise rare failed-boarding path in the real simulation.
// This checks causal equivalence, not a speedup or an end-to-end benchmark.
public sealed class Pr57BoardingReuseAdversarialTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Failed_boarding_reuse_matches_original_full_json(bool car, bool matchingDriver)
    {
        var seed = new CitySimulation(42);
        var citizen = new Citizen
        {
            Id = 901001, Position = new Point(0, 0),
            Purpose = JourneyPurpose.Boarding, BoardingVehicleId = 901002
        };
        seed.State.Citizens.Add(citizen);
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 901002, PassengerCar = car,
            DriverId = matchingDriver ? citizen.Id : 901003,
            Position = new Point(50, 50), Status = VehicleStatus.Parked
        });
        var initial = seed.SaveToJson();
        var plain = CitySimulation.FromJson(initial);
        var reuse = CitySimulation.FromJson(initial);
        var previous = Environment.GetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP");
        try
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", null);
            Arrive(plain);
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", "1");
            Arrive(reuse);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", previous);
        }
        Assert.Equal(plain.SaveToJson(), reuse.SaveToJson());
        Assert.Null(reuse.State.Citizens.Single(c => c.Id == 901001).BoardingVehicleId);
        Assert.Equal(car && matchingDriver ? (int?)null : matchingDriver ? 901001 : 901003,
            reuse.State.Vehicles.Single(v => v.Id == 901002).DriverId);
        Assert.Equal(reuse.SaveToJson(), CitySimulation.FromJson(reuse.SaveToJson()).SaveToJson());
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Available_boarding_preserves_vehicle_and_full_json(bool car)
    {
        var seed = new CitySimulation(42);
        var citizen = new Citizen
        {
            Id = 901101, Position = new Point(0, 0),
            Purpose = JourneyPurpose.Boarding, BoardingVehicleId = 901102
        };
        seed.State.Citizens.Add(citizen);
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 901102, PassengerCar = car, DriverId = citizen.Id,
            Position = new Point(0, 0), Status = VehicleStatus.Parked
        });
        var initial = seed.SaveToJson();
        var plain = CitySimulation.FromJson(initial);
        var reuse = CitySimulation.FromJson(initial);
        var previous = Environment.GetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP");
        try
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", null);
            Arrive(plain, citizen.Id);
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", "1");
            Arrive(reuse, citizen.Id);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", previous);
        }
        Assert.Equal(plain.SaveToJson(), reuse.SaveToJson());
        Assert.Equal((int?)901102, reuse.State.Citizens.Single(c => c.Id == citizen.Id).VehicleId);
        Assert.Null(reuse.State.Citizens.Single(c => c.Id == citizen.Id).BoardingVehicleId);
        Assert.Equal(car ? VehicleStatus.Travelling : VehicleStatus.Parked,
            reuse.State.Vehicles.Single(v => v.Id == 901102).Status);
        Assert.Equal(reuse.SaveToJson(), CitySimulation.FromJson(reuse.SaveToJson()).SaveToJson());
    }

    [Fact]
    public void Missing_reserved_vehicle_falls_back_without_mutating_other_drivers()
    {
        var seed = new CitySimulation(42);
        seed.State.Citizens.Add(new Citizen
        {
            Id = 901201, Position = new Point(0, 0),
            Purpose = JourneyPurpose.Boarding, BoardingVehicleId = 999999
        });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 901202, PassengerCar = true, DriverId = 901201,
            Position = new Point(0, 0), Status = VehicleStatus.Parked
        });
        var initial = seed.SaveToJson();
        var plain = CitySimulation.FromJson(initial);
        var reuse = CitySimulation.FromJson(initial);
        var previous = Environment.GetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP");
        try
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", null);
            Arrive(plain, 901201);
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", "1");
            Arrive(reuse, 901201);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", previous);
        }
        Assert.Equal(plain.SaveToJson(), reuse.SaveToJson());
        Assert.Null(reuse.State.Citizens.Single(c => c.Id == 901201).BoardingVehicleId);
        Assert.Equal((int?)901201, reuse.State.Vehicles.Single(v => v.Id == 901202).DriverId);
        Assert.Equal(reuse.SaveToJson(), CitySimulation.FromJson(reuse.SaveToJson()).SaveToJson());
    }

    [Fact]
    public void External_driver_change_between_arrivals_preserves_full_json()
    {
        var seed = new CitySimulation(42);
        seed.State.Citizens.Add(new Citizen
        {
            Id = 901301, Position = new Point(0, 0),
            Purpose = JourneyPurpose.Boarding, BoardingVehicleId = 901303
        });
        seed.State.Citizens.Add(new Citizen
        {
            Id = 901302, Position = new Point(0, 0),
            Purpose = JourneyPurpose.Boarding, BoardingVehicleId = 901303
        });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 901303, PassengerCar = true, DriverId = 901301,
            Position = new Point(50, 50), Status = VehicleStatus.Parked
        });
        var initial = seed.SaveToJson();
        var plain = CitySimulation.FromJson(initial);
        var reuse = CitySimulation.FromJson(initial);
        var previous = Environment.GetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP");
        try
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", null);
            Arrive(plain, 901301);
            // External mutation between arrivals must be visible to the next lookup.
            plain.State.Vehicles.Single(v => v.Id == 901303).DriverId = 901302;
            Arrive(plain, 901302);

            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", "1");
            Arrive(reuse, 901301);
            reuse.State.Vehicles.Single(v => v.Id == 901303).DriverId = 901302;
            Arrive(reuse, 901302);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", previous);
        }
        Assert.Equal(plain.SaveToJson(), reuse.SaveToJson());
        Assert.Null(reuse.State.Vehicles.Single(v => v.Id == 901303).DriverId);
        Assert.All(reuse.State.Citizens, c => Assert.Null(c.BoardingVehicleId));
        Assert.Equal(reuse.SaveToJson(), CitySimulation.FromJson(reuse.SaveToJson()).SaveToJson());
    }

    [Fact]
    public void Duplicate_reservation_candidates_preserve_first_match_and_save_load()
    {
        // A later matching candidate must not replace the first match merely
        // because it is closer. This is an adversarial serialized-state case.
        var seed = new CitySimulation(42);
        seed.State.Citizens.Add(new Citizen
        {
            Id = 901401, Position = new Point(0, 0),
            Purpose = JourneyPurpose.Boarding, BoardingVehicleId = 901402
        });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 901402, PassengerCar = true, DriverId = 901401,
            Position = new Point(50, 50), Status = VehicleStatus.Parked
        });
        seed.State.Vehicles.Add(new Vehicle
        {
            Id = 901402, PassengerCar = true, DriverId = 901401,
            Position = new Point(0, 0), Status = VehicleStatus.Parked
        });
        var initial = seed.SaveToJson();
        var plain = CitySimulation.FromJson(initial);
        var reuse = CitySimulation.FromJson(initial);
        var previous = Environment.GetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP");
        try
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", null);
            Arrive(plain, 901401);
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", "1");
            Arrive(reuse, 901401);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PR57_BOARDING_REUSE_LOOKUP", previous);
        }
        Assert.Equal(plain.SaveToJson(), reuse.SaveToJson());
        Assert.Null(reuse.State.Citizens.Single(c => c.Id == 901401).VehicleId);
        Assert.Null(reuse.State.Citizens.Single(c => c.Id == 901401).BoardingVehicleId);
        Assert.Null(reuse.State.Vehicles[0].DriverId);
        Assert.Equal((int?)901401, reuse.State.Vehicles[1].DriverId);
        Assert.Equal(reuse.SaveToJson(), CitySimulation.FromJson(reuse.SaveToJson()).SaveToJson());
    }

    private static void Arrive(CitySimulation sim, int citizenId = 901001)
    {
        var method = typeof(CitySimulation).GetMethod("CitizenArrived",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(sim, new object[] { sim.State.Citizens.Single(c => c.Id == citizenId) });
    }
}
