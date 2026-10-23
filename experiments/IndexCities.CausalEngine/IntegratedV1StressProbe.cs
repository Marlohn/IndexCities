namespace IndexCities.CausalEngine;

// BIG, OPT-IN EXPERIMENT ONLY. Physical inventories, actual worker travel,
// paid shifts and external deliveries are intentionally more demanding than
// the baseline, but do NOT implement the complete V1 networks or vehicles.
internal sealed partial class CausalCity
{
    private const int IntakeJob = WasteTreatmentJob + 1;
    private const int PurificationJob = IntakeJob + 1;
    private const int SewageJob = PurificationJob + 1;
    private const int WindJob = SewageJob + 1;
    private const int FuelStationJob = WindJob + 1;
    private const int IntakeNode = 1, PurificationNode = 2, SewageNode = 3;
    private const int WindNode = 4, FuelStationNode = 5;
    private const long WaterTankCapacity = 400_000, RawTankCapacity = 400_000;
    private const long SewageTankCapacity = 400_000, WindHourlyCapacity = 100_000;
    private const int UtilityGrossWage = 80, FuelImportBatch = 5000;
    private const int FuelImportPrice = 2, FuelRetailPrice = 4;
    private const int FuelImportPayload = -200;
    private const int FuelFarmPayloadBase = -300;
    private const int FuelStationStartingCash = 20_000_000;

    private Firm _fuelStation = new() { Id = 1000, Node = FuelStationNode, Cash = FuelStationStartingCash, Stock = 0 };
    private long _rawWater, _potableWater, _sewage, _energy;
    private long _waterExtracted, _waterPurified, _waterConsumed, _waterShortage;
    private long _sewageProcessed, _powerGenerated, _powerConsumed, _powerShortage;
    private long _powerCurtailed, _lastPowerHour = -1;
    private int _lastWindMaintainedDay;
    private long _utilityShiftsPaid, _fuelImported, _fuelBurned, _fuelDelivered;
    private long _fuelImportSpending, _fuelStationWages, _deedPaid;
    private int _fuelInTransit;
    private bool _fuelImportPending;

    private static int IntegratedJobNode(int job) => job switch
    {
        IntakeJob => IntakeNode,
        PurificationJob => PurificationNode,
        SewageJob => SewageNode,
        WindJob => WindNode,
        FuelStationJob => FuelStationNode,
        _ => -1
    };

    private int IntegratedWantedWorkers(int job) => job switch
    {
        IntakeJob => 2,
        PurificationJob => 2,
        SewageJob => 1,
        WindJob => 3,
        FuelStationJob => 1,
        _ => 0
    };

    private void AcquireExistingDeeds()
    {
        // Existing houses are still a simplified starting-city assumption.
        // Title is NOT free: a living adult buys it from the external reserve.
        foreach (var home in _households)
        {
            var ownerId = home.Members.FirstOrDefault(id => Age(_people[id], Epoch) >= 18, -1);
            if (ownerId < 0 || _people[ownerId].Cash < 2000) continue;
            _people[ownerId].Cash -= 2000;
            _reserveGlobal += 2000;
            _deedPaid += 2000;
            home.PropertyOwnerPersonId = ownerId;
            _history.Fact(0, ownerId, 37, 2000);
        }
    }

    // The grid has NO BATTERY. Every hour has its own variable wind supply;
    // unused electricity is explicitly curtailed, not carried to tomorrow.
    // Wind stops after a prolonged lack of ACTUAL paid maintenance shifts.
    private void RefreshIntegratedHourlyPower()
    {
        if (!_config.IntegratedV1Stress) return;
        var hour = _time / 3600;
        if (hour == _lastPowerHour) return;
        _powerCurtailed += _energy;
        _energy = 0;
        _lastPowerHour = hour;
        if (_time / DaySeconds - _lastWindMaintainedDay > 30) return;
        var factor = Math.Clamp(.55 + .25 * Math.Sin(hour * 3600.0 / 21600 + _config.Seed) +
                                .15 * Math.Sin(hour * 3600.0 / 172800 + _config.Seed * .3), .15, .95);
        var actual = (long)(WindHourlyCapacity * factor);
        _powerGenerated += actual;
        _energy = actual;
        _history.Fact(_time, -7010, 40, actual);
    }

    private void ConsumeIntegratedServices(Person person, bool ateRealFoodToday)
    {
        // The city can run dry or lose electricity. A numerical wind/weather
        // factor alone NEVER grants service to a resident in this pilot.
        if (_potableWater > 0 && _sewage < SewageTankCapacity)
        {
            _potableWater--;
            _waterConsumed++;
            _sewage++;
            // Potable water prevents dehydration, but cannot undo hunger.
            if (ateRealFoodToday) person.Health = Math.Min(100, person.Health + 1);
            _history.Fact(_time, person.Id, 33, 1);
        }
        else
        {
            _waterShortage++;
            _history.Fact(_time, person.Id, 43, 1);
            person.Health = Math.Max(0, person.Health - 1);
        }
        if (_energy > 0)
        {
            _energy--;
            _powerConsumed++;
            _history.Fact(_time, person.Id, 41, 1);
        }
        else
        {
            _powerShortage++;
            _history.Fact(_time, person.Id, 42, 1);
        }
        if (person.Health <= 0 && person.Alive) Die(person);
    }

    private void FinishIntegratedShift(Person worker)
    {
        worker.AtWork = false;
        worker.HeadingToWork = false;
        var job = worker.Job;
        var employer = job == FuelStationJob ? _fuelStation : null;
        if (worker.Node != IntegratedJobNode(job) ||
            (employer?.Cash ?? _treasury) < UtilityGrossWage)
        {
            StartTrip(worker, worker.Home);
            return;
        }
        if (employer is null) _treasury -= UtilityGrossWage;
        else { employer.Cash -= UtilityGrossWage; _fuelStationWages += UtilityGrossWage; }
        var pension = UtilityGrossWage / 10;
        _treasury += pension;
        worker.Cash += UtilityGrossWage - pension;
        worker.ContributedDays++;
        worker.GrossEarnings += UtilityGrossWage;
        _paidWages += UtilityGrossWage;
        _workdays++;
        if (employer is null) _municipalWagesPaidCents += UtilityGrossWage;
        _utilityShiftsPaid++;
        _history.Fact(_time, worker.Id, 7, UtilityGrossWage - pension);
        _history.Fact(_time, worker.Id, 15, pension);
        long produced = 0;
        if (job == IntakeJob)
        {
            // Physical river intake: replenishment is from an external natural
            // source, not generated by a household or an unstaffed building.
            produced = Math.Min(20_000L, RawTankCapacity - _rawWater);
            _rawWater += produced;
            _waterExtracted += produced;
        }
        else if (job == PurificationJob)
        {
            produced = Math.Min(20_000L, Math.Min(_rawWater,
                Math.Min(WaterTankCapacity - _potableWater, _energy)));
            _rawWater -= produced;
            _potableWater += produced;
            _waterPurified += produced;
            _energy -= produced;
            _powerConsumed += produced;
        }
        else if (job == SewageJob)
        {
            produced = Math.Min(20_000L, Math.Min(_sewage, _energy));
            _sewage -= produced;
            _sewageProcessed += produced;
            _energy -= produced;
            _powerConsumed += produced;
        }
        else if (job == WindJob)
        {
            // A real paid worker maintains an existing wind turbine. Wind,
            // not the salary payment, generates electricity each hour.
            _lastWindMaintainedDay = (int)(_time / DaySeconds);
        }
        else if (job == FuelStationJob)
        {
            OrderExternalFuelIfNeeded();
            // Customers may request fuel before this shop's employee arrives.
            // Queue REAL purchases, then dispatch on a paid staffed shift;
            // no instant fuel, ghost attendant or fictional cash.
            for (var i = 0; i < FarmCount; i++)
                if (_farms[i].TruckFuelDemandPending) RequestFarmFuel(i);
        }
        _history.Fact(_time, worker.Id, job == FuelStationJob ? (byte)38 : (byte)32, produced);
        StartTrip(worker, worker.Home);
    }

    private void OrderExternalFuelIfNeeded()
    {
        if (_fuelImportPending || _fuelStation.Stock > 2000 ||
            _fuelStation.Cash < FuelImportBatch * FuelImportPrice) return;
        var cost = FuelImportBatch * FuelImportPrice;
        _fuelStation.Cash -= cost;
        _reserveGlobal += cost;
        _fuelImportSpending += cost;
        _fuelImportPending = true;
        // An actual external truck starts at the city connection and
        // deposits cargo ONLY after traveling the road to the station.
        var delivery = NewTrip(-1, 0, FuelStationNode, EventKind.Delivery,
            FuelImportPayload, FuelImportBatch);
        _fuelInTransit += FuelImportBatch;
        Schedule(_time + 1, EventKind.Move, delivery.Id);
        _history.Fact(_time, -7000, 34, FuelImportBatch);
    }

    private void FinishFuelImport(Trip trip)
    {
        if (!_fuelImportPending || trip.Cargo != FuelImportBatch)
            throw new InvalidOperationException("Unordered or imaginary imported fuel");
        _fuelStation.Stock += trip.Cargo;
        _fuelInTransit -= trip.Cargo;
        _fuelImported += trip.Cargo;
        _fuelImportPending = false;
        _history.Fact(_time, -7000, 35, trip.Cargo);
    }

    private bool RequestFarmFuel(int farmId)
    {
        var farm = _farms[farmId];
        if (farm.TruckFuelOnOrder || _fuelStation.Stock < 80 ||
            _fuelStation.PresentWorkers <= 0 || farm.Cash < 80L * FuelRetailPrice) return false;
        // Fuel is sold by an actual company, from physical inventory; the
        // external delivery vehicle must drive from its stock to the farm.
        farm.Cash -= 80L * FuelRetailPrice;
        _fuelStation.Cash += 80L * FuelRetailPrice;
        _fuelStation.Stock -= 80;
        _fuelInTransit += 80;
        farm.TruckFuelOnOrder = true;
        farm.TruckFuelDemandPending = false;
        var delivery = NewTrip(-1, FuelStationNode, farm.Node,
            EventKind.Delivery, FuelFarmPayloadBase - farmId, 80);
        Schedule(_time + 1, EventKind.Move, delivery.Id);
        _history.Fact(_time, -7001 - farmId, 36, 80);
        return true;
    }

    private void FinishFarmFuel(Trip trip)
    {
        var farmId = FuelFarmPayloadBase - trip.Payload;
        if (farmId < 0 || farmId >= FarmCount || ! _farms[farmId].TruckFuelOnOrder)
            throw new InvalidOperationException("Imaginary farm fuel delivery");
        var farm = _farms[farmId];
        farm.TruckFuelOnOrder = false;
        farm.TruckFuelUnits += trip.Cargo;
        _fuelDelivered += trip.Cargo;
        _fuelInTransit -= trip.Cargo;
        _history.Fact(_time, -7001 - farmId, 39, trip.Cargo);
    }

    private bool HasTruckFuelForTrip(int farmId, int shopId)
    {
        var farm = _farms[farmId];
        var shop = _shops[shopId];
        var distance = Math.Abs(farm.Node % Grid - shop.Node % Grid) +
                       Math.Abs(farm.Node / Grid - shop.Node / Grid);
        if (!farm.TruckBusy && farm.TruckFuelUnits >= 2 * distance + 4)
        {
            farm.TruckFuelDemandPending = false;
            return true;
        }
        if (!farm.TruckBusy && farm.TruckFuelUnits < 2 * distance + 4)
        {
            farm.TruckFuelDemandPending = true;
            if (_fuelStation.PresentWorkers > 0) RequestFarmFuel(farmId);
        }
        return false;
    }

    private void BurnTruckFuelForSegment(Person driver)
    {
        if (!_config.IntegratedV1Stress || !driver.FreightShift ||
            driver.Job < StoreCount || driver.Job >= StoreCount + FarmCount) return;
        var farm = _farms[driver.Job - StoreCount];
        if (!farm.TruckBusy || farm.TruckFuelUnits <= 0)
            throw new InvalidOperationException("Moving a freight truck without real fuel");
        farm.TruckFuelUnits--;
        _fuelBurned++;
        _history.Fact(_time, driver.Id, 44, 1);
    }

    private void AssertIntegratedConservation()
    {
        if (!_config.IntegratedV1Stress) return;
        if (_rawWater < 0 || _potableWater < 0 || _sewage < 0 || _energy < 0 ||
            _rawWater > RawTankCapacity || _potableWater > WaterTankCapacity ||
            _sewage > SewageTankCapacity || _energy > WindHourlyCapacity ||
            _waterExtracted != _rawWater + _potableWater + _waterConsumed ||
            _waterConsumed != _sewage + _sewageProcessed ||
            _powerGenerated != _energy + _powerConsumed + _powerCurtailed ||
            _fuelImported + (_fuelImportPending ? FuelImportBatch : 0) !=
                _fuelStation.Stock + _fuelInTransit +
                _farms.Sum(f => f.TruckFuelUnits) + _fuelBurned ||
            _fuelStation.Stock < 0 || _fuelStation.Cash < 0 || _fuelInTransit < 0 ||
            _fuelStation.PresentWorkers != _people.Count(p => p.Alive &&
                p.Job == FuelStationJob && p.AtWork) ||
            _households.Any(h => h.PropertyOwnerPersonId >= 0 &&
                (!_people[h.PropertyOwnerPersonId].Alive ||
                 _people[h.PropertyOwnerPersonId].HouseholdId != h.Id)))
            throw new InvalidOperationException("Integrated city violates physical, fiscal or ownership conservation");
    }
}
