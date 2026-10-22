namespace IndexCities.CausalEngine;

// Only an opt-in causal stress slice. The SPEC's full construction contracts,
// permits, contractor ownership, truck models, fuel and city budgets are NOT
// implemented here. No property or material is spontaneously manufactured.
internal sealed class ConstructionSite
{
    public int HouseholdId;
    public bool Ordered, Finished;
    public int TimberOnSite, TimberBuiltIntoHouse, PaidShifts;
    public long LastCrewDay = -1;
}

internal sealed partial class CausalCity
{
    private const int BuilderJob = StoreCount + FarmCount;
    private const int TimberPerExtension = 10;
    private const int TimberUnitPrice = 50;
    private const int ContractorLaborInvoice = 2000;
    private readonly int _initialOutsideTimber = 100_000;
    private int _outsideTimber = 100_000;
    private Firm _builder = new() { Id = BuilderJob, Node = 0, Cash = 0, Stock = 0 };
    private ConstructionSite[] _construction = [];
    private long _constructionStarted, _constructionFinished, _constructionPaidShifts;

    private long CalculateConstructionTimber()
    {
        long total = _outsideTimber;
        foreach (var site in _construction)
            total += site.TimberOnSite + site.TimberBuiltIntoHouse;
        foreach (var trip in _trips)
            if (!trip.Completed) total += trip.MaterialCargo;
        return total;
    }

    private void OrderFamilyConstruction(long day)
    {
        // Houses already have 4 people at start; a later fifth live resident
        // causes actual space demand. Families may fund a finite enlargement
        // from their own existing money, not from a subsidy/created budget.
        if (_outsideTimber < TimberPerExtension) return;
        var scheduled = 0;
        for (var i = (int)((day + _config.Seed) % _construction.Length);
             scheduled < 12 && i < _construction.Length; i++)
        {
            var site = _construction[i];
            if (site.Ordered) continue;
            var home = _households[i];
            if (home.Members.Count(id => _people[id].Alive) < 5) continue;
            const int totalInvoice = TimberPerExtension * TimberUnitPrice + ContractorLaborInvoice;
            var familyCash = home.Members.Where(id => _people[id].Alive).Sum(id => _people[id].Cash);
            if (familyCash < totalInvoice) continue;
            var due = totalInvoice;
            foreach (var id in home.Members)
            {
                var person = _people[id];
                if (!person.Alive || person.Cash <= 0) continue;
                var paid = Math.Min(person.Cash, due);
                person.Cash -= paid;
                due -= (int)paid;
                _history.Fact(_time, person.Id, 19, paid);
                if (due == 0) break;
            }
            if (due != 0) throw new InvalidOperationException("Worksite invoice not funded");
            _builder.Cash += ContractorLaborInvoice;
            _reserveGlobal += TimberPerExtension * TimberUnitPrice;
            _outsideTimber -= TimberPerExtension;
            site.Ordered = true;
            _constructionStarted++;
            // Real finite timber leaves outside stock, enters truck cargo and
            // appears on site ONLY when the physical trip reaches the home.
            var delivery = NewTrip(-1, 0, home.Home, EventKind.Delivery,
                payload: 10_000 + home.Id);
            delivery.MaterialCargo = TimberPerExtension;
            Schedule(_time + 4 * 3600, EventKind.Move, delivery.Id);
            _history.Fact(_time, -3000 - home.Id, 20, TimberPerExtension);
            scheduled++;
        }
    }

    private int PickConstructionSite(Person worker)
    {
        var day = _time / DaySeconds;
        var start = (int)((day * 13 + worker.Id) % _construction.Length);
        for (var i = 0; i < _construction.Length; i++)
        {
            var id = (start + i) % _construction.Length;
            var s = _construction[id];
            if (!s.Ordered || s.Finished || s.TimberOnSite <= 0 ||
                s.LastCrewDay == day) continue;
            s.LastCrewDay = day;
            return id;
        }
        return -1;
    }

    private void CompleteConstructionDelivery(Trip trip)
    {
        var homeId = trip.Payload - 10_000;
        if (homeId < 0 || homeId >= _construction.Length ||
            trip.MaterialCargo != TimberPerExtension ||
            !_construction[homeId].Ordered ||
            _construction[homeId].Finished)
            throw new InvalidOperationException("Invalid imported building material delivery");
        _construction[homeId].TimberOnSite += trip.MaterialCargo;
        _history.Fact(_time, -3000 - homeId, 21, trip.MaterialCargo);
        trip.MaterialCargo = 0;
    }

    private void PerformConstructionShift(Person p)
    {
        if (p.ConstructionSiteId < 0) return;
        var site = _construction[p.ConstructionSiteId];
        if (site.Finished || site.TimberOnSite <= 0 || _builder.Cash < 80) return;
        site.PaidShifts++;
        _constructionPaidShifts++;
        if (site.PaidShifts % 2 == 0)
        {
            site.TimberOnSite--;
            site.TimberBuiltIntoHouse++;
        }
        if (site.PaidShifts == 20)
        {
            site.Finished = true;
            _constructionFinished++;
            _history.Fact(_time, -3000 - site.HouseholdId, 23, 1);
        }
        _history.Fact(_time, p.Id, 22, site.HouseholdId);
    }

    private void AssertConstructionConservation()
    {
        if (!_config.ConstructionPilot) return;
        if (_outsideTimber < 0 || CalculateConstructionTimber() != _initialOutsideTimber)
            throw new InvalidOperationException("Timber created/destroyed in worksite import or labour");
        if (_builder.Cash < 0 || _constructionFinished > _constructionStarted)
            throw new InvalidOperationException("Construction contractor cash or completions invalid");
        foreach (var site in _construction)
        {
            if (site.TimberOnSite < 0 || site.TimberBuiltIntoHouse < 0 ||
                (site.Finished && (site.PaidShifts != 20 ||
                    site.TimberBuiltIntoHouse != TimberPerExtension)))
                throw new InvalidOperationException("Physical worksite or materials inconsistent");
        }
    }
}
