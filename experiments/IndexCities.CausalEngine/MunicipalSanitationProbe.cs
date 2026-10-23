namespace IndexCities.CausalEngine;

// Isolated counterfactual for an already approved V1 kind of municipal work:
// real homes accumulate real waste; a resident travels to that home, collects
// waste and carries it physically to a single municipal depot. Actual
// commercial property tax receipts pay workers. The initial city facilities,
// valuations, pickup cadence and waste treatment are NOT SPEC-complete.
internal sealed partial class CausalCity
{
    private const int SanitationJob = BuilderJob + 1;
    private const int SanitationDepotNode = 0;
    private const int SanitationGrossWage = 80;
    private const long BaseMonthlyAssessedPropertyTaxCents = 20_000;
    private const long SanitationDepotCapacity = 50_000_000L;

    private void CollectMunicipalPropertyTaxes()
    {
        // Each of the existing 64 shops and eight farms occupies its own
        // taxable commercial/industrial building. Only its CURRENT wallet
        // can pay a 20,000-cent monthly experimental levy on its assessed lot.
        // This is NOT a tax on sales, and cash never appears from nowhere.
        var assessment = BaseMonthlyAssessedPropertyTaxCents * 6 /
            _config.MunicipalCollectionIntervalDays;
        foreach (var firm in _shops.Concat(_farms))
        {
            var paid = Math.Min(assessment, firm.Cash);
            if (paid <= 0) continue;
            firm.Cash -= paid;
            _treasury = checked(_treasury + paid);
            _municipalTaxPaidCents += paid;
            _history.Fact(_time, -5000 - firm.Id, 24, paid);
        }
    }

    private int PickMunicipalCollectionSite(Person worker)
    {
        if (_treasury < SanitationGrossWage ||
            _garbageAtDepot + _garbageInTransit >= SanitationDepotCapacity) return -1;
        var day = _time / DaySeconds;
        if (_config.FairSanitationDispatch)
        {
            // Economical O(1) amortized coverage of ALL actual homes, not a
            // nearby-house lottery that can starve a few blocks indefinitely.
            // The dispatcher has no shortcut: the chosen real employee must
            // still travel physically to the home, then the finite depot.
            // The daily booking prevents two public workers servicing a
            // household's same physical waste at the same time.
            for (var n = 0; n < _households.Length; n++)
            {
                var id = (_fairSanitationCursor + n) % _households.Length;
                var h = _households[id];
                if (h.LastSanitationBookedDay == day || h.TrashUnits < 4 ||
                    !h.Members.Any(pid => _people[pid].Alive)) continue;
                _fairSanitationCursor = (id + 1) % _households.Length;
                h.LastSanitationBookedDay = day;
                return id;
            }
            return -1;
        }
        var best = -1;
        var bestTravel = int.MaxValue;
        var bestWaste = -1L;
        // A bounded search over real homes, with stable seeded starting
        // point. A slot is reserved only for today's current paid worker;
        // neither stock nor revenue is fabricated by a "scheduled" visit.
        var start = (int)((day * 269 + worker.Id * 131L + _config.Seed) % _households.Length);
        for (var offset = 0; offset < Math.Min(72, _households.Length); offset++)
        {
            var id = (start + offset * 43) % _households.Length;
            var home = _households[id];
            if (home.LastSanitationBookedDay == day || home.TrashUnits < 4 ||
                !home.Members.Any(pid => _people[pid].Alive)) continue;
            var distance = Math.Abs(worker.Home % Grid - home.Home % Grid) +
                           Math.Abs(worker.Home / Grid - home.Home / Grid);
            if (best < 0 || distance < bestTravel ||
                (distance == bestTravel && home.TrashUnits > bestWaste))
            {
                best = id;
                bestTravel = distance;
                bestWaste = home.TrashUnits;
                if (distance == 0) break;
            }
        }
        if (best >= 0)
            _households[best].LastSanitationBookedDay = day;
        return best;
    }

    private void FinishMunicipalSanitationShift(Person worker)
    {
        var homeId = worker.SanitationHouseholdId;
        worker.AtWork = false;
        worker.HeadingToWork = false;
        worker.SanitationHouseholdId = -1;
        if (homeId < 0 || worker.SanitationCargo != 0 ||
            _treasury < SanitationGrossWage ||
            _households[homeId].TrashUnits == 0)
        {
            StartTrip(worker, worker.Home);
            return;
        }

        // The actual worker is present at the real collection site. Pickup
        // has finite carrying capacity and cannot duplicate household waste.
        var home = _households[homeId];
        var remainingDepotCapacity = SanitationDepotCapacity -
            _garbageAtDepot - _garbageInTransit;
        var amount = Math.Min(500L, Math.Min(home.TrashUnits, remainingDepotCapacity));
        if (amount <= 0) { StartTrip(worker, worker.Home); return; }
        home.TrashUnits -= amount;
        home.LastSanitationServicedDay = _time / DaySeconds;
        worker.SanitationCargo = amount;
        _garbageInTransit += amount;
        _garbageCollected += amount;
        _sanitationPaidVisits++;
        _treasury -= SanitationGrossWage;
        var pensionContribution = SanitationGrossWage / 10;
        _treasury += pensionContribution;
        worker.Cash += SanitationGrossWage - pensionContribution;
        worker.GrossEarnings += SanitationGrossWage;
        worker.ContributedDays++;
        _paidWages += SanitationGrossWage;
        _municipalWagesPaidCents += SanitationGrossWage;
        _workdays++;
        _history.Fact(_time, worker.Id, 25, amount);
        _history.Fact(_time, worker.Id, 7, SanitationGrossWage - pensionContribution);
        _history.Fact(_time, worker.Id, 15, pensionContribution);

        // Carry the physical waste to the public collection depot; only the
        // subsequent road trip may credit the depot inventory.
        StartTrip(worker, SanitationDepotNode);
    }
}
