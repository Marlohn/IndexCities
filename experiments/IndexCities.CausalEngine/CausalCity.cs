using System.Diagnostics;
using System.IO.Compression;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace IndexCities.CausalEngine;

// Independent vertical-slice engine for falsifying the architecture.
// RULES AND FIXTURES HERE ARE NOT AN IMPLEMENTATION OF ALL OF INDEXCITIES SPEC.
internal readonly record struct LabConfig(int Sims, int Days, int Seed, string? History, bool Adversarial, bool WageFeedback = false, bool CohortRenewal = false, bool DemandStaffing = false, bool VacancyMarket = false, bool FreightLabor = false, bool ConstructionPilot = false, bool FairHouseholdHiring = false, bool CostBasedFoodPrice = false, bool CapacityPricedFood = false, bool AttendanceAwareFarms = false, bool CrossFarmFreight = false, bool FarmCostPricing = false, bool BoundedRetailService = false, bool BasketRetailService = false, bool RetailShopFallback = false, int RetailBasketsPerShift = 5, bool MunicipalSanitationPilot = false, int MunicipalCollectionIntervalDays = 6, bool MunicipalWasteTreatmentPilot = false, bool FairSanitationDispatch = false, bool AttendanceAwareMunicipalShifts = false, bool TaxInclusiveProductPrice = false, int TaxCostRecoveryPercent = 100, int MonthlyResidentialTaxCents = 0, int WasteTreatmentUnitsPerShift = 50, bool LivingOccupancyBirthCap = false, int TreatmentBacklogMonths = 0, bool IntegratedV1Stress = false, bool BirthEligibleFallback = false);
internal enum EventKind : byte { Day, Hour, StartWork, EndWork, Move, Delivery, RoadClose, RoadOpen, HouseholdShop }
internal readonly record struct SimEvent(EventKind Kind, int Id);
internal sealed class Person
{
    public required int Id;
    public required int HouseholdId;
    public required DateTime Born;
    public required int Job;
    public required int Home;
    public long Cash;
    public int Health = 100;
    public bool Alive = true;
    public int Node;
    public int CarryFood;
    public int ContributedDays;
    public long GrossEarnings, PensionCredit, PensionReceived;
    public bool AtWork;
    public bool Traveling;
    public bool HeadingToWork;
    public int TripId = -1;
    public int FreightShopId = -1;
    public int FreightCargo;
    public bool FreightShift;
    public bool IsFreightDriver;
    public int ConstructionSiteId = -1;
    // The alternative-shop search is bounded and follows real road trips.
    public ulong RetailVisitedStoresMask;
    public int RetailFallbackAttempts;
    public long RetailAlternativeTrips, RetailAlternativePurchases, RetailAlternativeFailures;
    public int SanitationHouseholdId = -1;
    public long SanitationCargo;
}
internal sealed class Household
{
    public required int Id;
    public required int Home;
    public required List<int> Members;
    public int Pantry = 40;
    public int LastBirthDay = -10_000;
    public long LastShoppingDepartureDay = -1;
    public long TrashUnits;
    public long LastSanitationBookedDay = -1;
    public long LastSanitationServicedDay = -1;
    // Experimental initial private residential deed, with a single real
    // household member as taxpayer and debt kept against THIS property.
    public int PropertyOwnerPersonId = -1;
    public long ResidentialPropertyTaxDebtCents;
}
internal sealed class Firm
{
    public required int Id;
    public required int Node;
    public long Cash = 20_000_000;
    public int Stock = 100;
    // Laboratory-only accounting of yesterday's real revenues and paid shifts.
    // Used to test whether undistributed company income drives the famine.
    public long DayReceipts, DaySupplies;
    public int DayPaidShifts, WageForDay;
    // Earmarked portion of this firm's REAL existing cash, not a second wallet.
    // Unspent payroll allocation survives the gap between customer visits.
    public long PayrollBudget;
    // Bounded pilot: monthly observed product demand determines genuine
    // open positions. Receipts are NOT a second bank account.
    public int OfferedPositions;
    public int PresentWorkers;
    public long MonthFoodProduced;
    public int MonthFarmWorkdays;
    // Optional price pilot: every field follows real trade/paid payroll, not theoretical units.
    public long MonthRetailSalesUnits, MonthRetailPaidWagesCents;
    public int RetailUnitPriceCents = 50;
    // Real accrued costs and trade quantities (NOT new wallets).
    public long MonthFarmPaidWagesCents, MonthWholesaleSalesUnits;
    // Only diesel actually consumed by this firm's own truck counts as
    // an operating cost; the fuel purchase itself bought physical stock.
    public long MonthFreightFuelConsumedCents;
    public long MonthWholesaleBoughtUnits, MonthWholesalePaidCents;
    public int FarmWholesaleUnitPriceCents = 7;
    // Optional physical checkout capacity on actual attended shifts.
    public int TodayCheckoutUnitsLeft;
    public long ReservedCheckoutWagesCents;
    // Exactly one checkout event is one physically arriving buyer with a basket.
    // Counters are observations, NOT payments, inventory or agent creation.
    public long MonthRetailBaskets, LifetimeRetailBaskets, LifetimeCheckoutCapacityDenied;
    // Real taxes already debited from this business; accounting only.
    public long MonthPropertyTaxActuallyPaidCents;
    public int TruckFuelUnits;
    public bool TruckFuelOnOrder;
    public bool TruckFuelDemandPending;
    public bool TruckBusy;
}
internal sealed class Trip
{
    public required int Id;
    public required int OwnerId;        // >=0 citizen, -1 delivery
    public required int CurrentNode;
    public required int Destination;
    public required EventKind Completion;
    public int Payload;                 // shop/firm id for deliveries
    public int Cargo;
    public int MaterialCargo;
    public int From, To;
    public long EnterAt, ExitAt;
    public bool Completed;
}
internal readonly record struct HistoricalFact(long At, int Owner, byte Type, long Amount);
internal sealed class HistorySink : IDisposable
{
    private readonly BinaryWriter? _writer;
    private long _lastAt;
    public long LastAt => _lastAt;
    public long Entries { get; private set; }
    public long BytesWritten { get; private set; }
    public HistorySink(string? path, bool append = false, long lastAt = 0, long entries = 0)
    {
        _lastAt = lastAt;
        Entries = entries;
        if (path is null) return;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        Stream output = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write,
            FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
        if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            output = new GZipStream(output, CompressionLevel.Fastest);
        _writer = new BinaryWriter(output);
        if (!append)
        {
            _writer.Write(new byte[] { (byte)'I', (byte)'C', (byte)'H', (byte)'2' });
            BytesWritten = 4;
        }
    }
    private void Var(ulong value)
    {
        if (_writer is null) return;
        while (value >= 128)
        {
            _writer.Write((byte)((value & 127UL) | 128UL));
            BytesWritten++;
            value >>= 7;
        }
        _writer.Write((byte)value);
        BytesWritten++;
    }
    private static ulong ZigZag(long number)
        => unchecked((ulong)((number << 1) ^ (number >> 63)));
    private static long UnZigZag(ulong number)
        => unchecked((long)(number >> 1) ^ -((long)(number & 1UL)));
    public void Fact(long at, int owner, byte type, long amount)
    {
        if (at < _lastAt) throw new InvalidOperationException("Historical event not in global causal order");
        Entries++;
        if (_writer is null) { _lastAt = at; return; }
        Var((ulong)(at - _lastAt));
        Var(ZigZag(owner));
        _writer.Write(type);
        BytesWritten++;
        Var(ZigZag(amount));
        _lastAt = at;
    }
    private static ulong ReadVar(BinaryReader input, byte first)
    {
        ulong value = 0;
        var current = first;
        for (var shift = 0; shift < 70; shift += 7)
        {
            if (shift == 63 && current > 1)
                throw new InvalidDataException("Invalid varint overflow");
            value |= (ulong)(current & 127) << shift;
            if ((current & 128) == 0) return value;
            current = input.ReadByte();
        }
        throw new InvalidDataException("Invalid varint length");
    }
    private static ulong ReadVar(BinaryReader input) => ReadVar(input, input.ReadByte());
    public static IEnumerable<HistoricalFact> Read(string path)
    {
        using Stream raw = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            1024 * 1024, FileOptions.SequentialScan);
        using Stream data = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(raw, CompressionMode.Decompress) : raw;
        using var input = new BinaryReader(data);
        var magic = input.ReadBytes(4);
        if (!magic.SequenceEqual(new byte[] { 73, 67, 72, 50 }))
            throw new InvalidDataException("Not an IndexCities experimental ICH2 history");
        long at = 0;
        while (true)
        {
            int first = input.BaseStream.ReadByte();
            if (first < 0) yield break;
            at = checked(at + (long)ReadVar(input, (byte)first));
            var id = checked((int)UnZigZag(ReadVar(input)));
            var kind = input.ReadByte();
            var amount = UnZigZag(ReadVar(input));
            yield return new HistoricalFact(at, id, kind, amount);
        }
    }
    public void Flush() => _writer?.Flush();
    public void Dispose() => _writer?.Dispose();
}
internal sealed partial class CausalCity : IDisposable
{
    public static readonly DateTime Epoch = new(2020, 1, 1);
    public const long DaySeconds = 86400;
    private const long TravelSeconds = 12, Headway = 3, CrossingSeconds = 2;
    private const int Grid = 8, Nodes = Grid * Grid;
    private const int StoreCount = 64, FarmCount = 8;
    // Fixture calibration ONLY, not approved IndexCities prices/wages.
    private const int FoodPrice = 50, WholesalePrice = 7;
    private const long InitialBusinessCashCents = (StoreCount + FarmCount) * 20_000_000L;
    private LabConfig _config;
    private readonly PriorityQueue<SimEvent, (long At, long Sequence)> _events = new();
    private readonly List<Person> _people = [];
    private readonly List<Trip> _trips = [];
    private readonly Stack<int> _freeTripSlots = new();
    private readonly Household[] _households;
    private readonly Firm[] _shops;
    private readonly Firm[] _farms;
    private readonly long[] _laneNextEntry = new long[Nodes * 4];
    private readonly long[] _laneLastEntry = new long[Nodes * 4];
    private readonly long[] _junctionAvailable = new long[Nodes];
    private readonly bool[] _closed = new bool[Nodes * 4];
    private HistorySink _history;
    private long _sequence, _time, _initialMoney, _treasury, _reserveGlobal;
    private long _days, _journeys, _segments, _waitSeconds, _sales, _shipments, _workdays;
    private long _births, _deaths, _unservedMeals, _paidWages, _unsatisfiedSales, _replans;
    // Diagnostic counters only; never change the mortality rules.
    private long _deathsWithCollapsedHealth, _deathsAtAgeLimit;
    // Integrated laboratory observability only. Rejection causes may overlap;
    // these counters never affect births, resources, simulation or fingerprints.
    private long _birthAppointments, _birthBlockedEverSlots, _birthBlockedLiveSlots;
    private long _birthFallbackCandidatesChecked, _birthFallbackBirths, _birthFallbackDaysWithoutEligible;
    private long _birthBlockedPantry, _birthBlockedSpacing, _birthBlockedCarers;
    private long _birthBlockedCash, _birthBlockedWater;
    private long _lastDemographyBirths, _lastDemographyDeaths;
    private long _lastDemographyAgeDeaths, _lastDemographyHealthDeaths;
    private long _producedFood, _eatenFood, _initialFood;
    private long _paidFreightCents, _freightLaborTrips;
    private long _retailSalesCents;
    private long _municipalTaxPaidCents, _municipalWagesPaidCents;
    private long _residentialTaxPaidCents, _residentialTaxAssessedCents;
    private int _fairSanitationCursor;
    private long _garbageGenerated, _garbageCollected, _garbageAtDepot, _sanitationPaidVisits, _garbageInTransit;
    private long _peakEvents;
    private long _firstDollarViolation = -1;
    private int _active, _maxActive, _minActive;
    private double _windFactor = 1, _waterRatio = 1;
    public CausalCity(LabConfig config)
    {
        _config = config;
        _history = new HistorySink(config.History);
        var families = Math.Max(1, config.Sims / 4);
        _households = new Household[families];
        for (var i = 0; i < families; i++)
            _households[i] = new Household { Id = i, Home = (i * 11) % Nodes, Members = [] };
        _shops = Enumerable.Range(0, StoreCount)
            .Select(i => new Firm { Id = i, Node = i }).ToArray();
        _farms = Enumerable.Range(0, FarmCount)
            .Select(i => new Firm { Id = i, Node = (i * 9) % Nodes, Stock = 5000 }).ToArray();
        for (var i = 0; i < config.Sims; i++)
        {
            var family = _households[i % families];
            var birthday = Epoch.AddYears(-(16 + (i * 47 + config.Seed) % 49));
            var p = new Person { Id = i, HouseholdId = family.Id, Born = birthday,
                Home = family.Home, Node = family.Home,
                Job = config.VacancyMarket ? -1 : (i * 17 + 7) % (StoreCount + FarmCount),
                Cash = 30000 };
            _people.Add(p);
            family.Members.Add(i);
        }
        if (_config.IntegratedV1Stress) AcquireExistingDeeds();
        if (_config.MonthlyResidentialTaxCents > 0)
            foreach (var h in _households)
                h.PropertyOwnerPersonId = h.Members.FirstOrDefault(id =>
                    Age(_people[id], Epoch) >= 18, -1);
        _construction = Enumerable.Range(0, families)
            .Select(i => new ConstructionSite { HouseholdId = i }).ToArray();
        _active = _maxActive = _minActive = config.Sims;
        _initialMoney = CalculateMoney();
        _initialFood = CalculateFood();
        Schedule(0, EventKind.Day, 0);
        Schedule(0, EventKind.Hour, 0);
        if (config.Adversarial)
        {
            Schedule(8 * 3600 + 30, EventKind.RoadClose, 0);
            Schedule(8 * 3600 + 900, EventKind.RoadOpen, 0);
        }
    }

    private long CalculateMoney()
    {
        long total = checked(_treasury + _reserveGlobal + _builder.Cash +
            (_config.IntegratedV1Stress ? _fuelStation.Cash : 0));
        foreach (var p in _people) total = checked(total + p.Cash);
        foreach (var f in _shops) total = checked(total + f.Cash);
        foreach (var f in _farms) total = checked(total + f.Cash);
        return total;
    }
    private long CalculateFood()
    {
        long total = 0;
        foreach (var h in _households) total += h.Pantry;
        foreach (var f in _shops) total += f.Stock;
        foreach (var f in _farms) total += f.Stock;
        foreach (var p in _people) total += p.CarryFood + p.FreightCargo;
        foreach (var trip in _trips) if (!trip.Completed && trip.OwnerId < 0) total += trip.Cargo;
        return total;
    }
    private void Schedule(long when, EventKind kind, int id)
    {
        if (when < _time) throw new InvalidOperationException("Event in the past");
        _events.Enqueue(new SimEvent(kind, id), (when, ++_sequence));
        _peakEvents = Math.Max(_peakEvents, _events.Count);
    }
    public void Run() => RunUntilDay(_config.Days);
    public void RunUntilDay(int day)
    {
        if (day < 0 || day > _config.Days || (long)day * DaySeconds < _time)
            throw new ArgumentOutOfRangeException(nameof(day));
        var end = day * DaySeconds;
        var stopWatch = Stopwatch.StartNew();
        while (_events.TryPeek(out _, out var priority) && priority.At < end)
        {
            _events.TryDequeue(out var e, out priority);
            _time = priority.At;
            switch (e.Kind)
            {
                case EventKind.Day: DoDay(); break;
                case EventKind.Hour: DoHour(); break;
                case EventKind.StartWork: StartWork(e.Id); break;
                case EventKind.EndWork: EndWork(e.Id); break;
                case EventKind.Move: Move(e.Id); break;
                case EventKind.Delivery: CompleteDelivery(e.Id); break;
                case EventKind.RoadClose: CloseRoad(); break;
                case EventKind.RoadOpen: OpenRoad(); break;
                case EventKind.HouseholdShop: ShopAtHome(e.Id); break;
            }
            if ((_sequence & 0xFFFFFF) == 0 && _firstDollarViolation == -1)
                if (CalculateMoney() != _initialMoney) _firstDollarViolation = _time;
        }
        _time = end;
        _history.Flush();
        if (_days == _config.Days && _config.Days > 366)
            Console.Error.WriteLine($"LAB_PROGRESS days={_days} live={_active} wall_s={stopWatch.Elapsed.TotalSeconds:F2}");
    }
    private void DoDay()
    {
        var day = _time / DaySeconds;
        _days++;
        var date = Epoch.AddDays(day);
        if (_config.IntegratedV1Stress) RefreshIntegratedHourlyPower();
        // Observability only: monthly physical and fiscal bottlenecks around
        // late-generation collapses. No gameplay state or money is changed.
        if (_config.MunicipalSanitationPilot && _days % 30 == 0)
            Console.Error.WriteLine(
                $"LAB_WASTE_AUDIT day={_days} live={_active} treasury={_treasury} " +
                $"dirtyHomes={_households.Count(h => h.TrashUnits > 80 && h.Members.Any(id => _people[id].Alive))} " +
                $"worstHomeWaste={_households.Max(h => h.TrashUnits)} " +
                $"staleHomes={_households.Count(h => h.TrashUnits >= 4 && h.Members.Any(id => _people[id].Alive) && _days - h.LastSanitationServicedDay > 15)} " +
                $"rawDepot={_garbageAtDepot} rawTransit={_garbageInTransit} treatedLandfill={_wasteTreated} " +
                $"municipalJobs={_people.Count(p => p.Alive && (p.Job == SanitationJob || p.Job == WasteTreatmentJob))} " +
                $"paidCollections={_sanitationPaidVisits} treatmentShifts={_wasteTreatmentPaidShifts} " +
                $"businessTaxPaid={_municipalTaxPaidCents} residentialTaxPaid={_residentialTaxPaidCents} " +
                $"municipalWages={_municipalWagesPaidCents}");
        if (_config.WageFeedback && !_config.DemandStaffing)
        {
            // Hypothetical stress fixture, NOT a new IndexCities wage policy:
            // the prior day's net trading receipts are distributed through
            // actual workers' next paid shifts. Neither food nor money appears
            // from nowhere, and firms still refuse unaffordable payroll.
            foreach (var shop in _shops) ResetDailyWage(shop, 65, shop.DayReceipts - shop.DaySupplies);
            foreach (var farm in _farms) ResetDailyWage(farm, 80, farm.DayReceipts);
        }
        if (_config.DemandStaffing)
            ScheduleDemandFundedShifts(date, day);
        if (_config.MunicipalSanitationPilot && day % 30 == 0)
            CollectMunicipalPropertyTaxes();
        if (_config.MonthlyResidentialTaxCents > 0 && day % 30 == 0)
            CollectExistingResidentialPropertyTaxes();
        if (_config.ConstructionPilot && day % 30 == 0)
            OrderFamilyConstruction(day);
        if (_config.VacancyMarket && day % 30 == 0)
            ReconcileVacancies(date, day);
        // A fixture-specific, deterministic demographic policy. NOT an approved
        // IndexCities birth/death algorithm. Birth requires real adult carers,
        // a housing slot, water, pantry and existing household money.
        if (day != 0)
        {
            if (_config.CohortRenewal)
            {
                // Counterfactual only: no citywide one-birth-per-two-days cap.
                // A rotating deterministic family appointment still requires
                // two living adult carers, actual food, money and housing room.
                // This does NOT implement the SPEC's complete relationships,
                // pregnancy, childcare or migration.
                var start = (int)(((long)day * 151 + _config.Seed) % _households.Length);
                if (_config.BirthEligibleFallback)
                {
                    // Opt-in demographic counterfactual: at most ONE birth per day
                    // with ALL the existing family eligibility requirements.
                    var found = false;
                    for (var k = 0; k < _households.Length; k++)
                    {
                        _birthFallbackCandidatesChecked++;
                        var candidate = _households[(start + k) % _households.Length];
                        if (!CanBirth(candidate, date, day)) continue;
                        if (!TryBirth(candidate, date, day))
                            throw new InvalidOperationException("Eligible birth candidate was rejected");
                        _birthFallbackBirths++;
                        found = true;
                        break;
                    }
                    if (!found) _birthFallbackDaysWithoutEligible++;
                }
                else TryBirth(_households[start], date, day);
            }
            if (!_config.CohortRenewal && day % 2 == 0)
            {
                var start = (int)((day * 19) % _households.Length);
                for (var k = 0; k < _households.Length; k++)
                {
                    if (TryBirth(_households[(start + k) % _households.Length], date, day)) break;
                }
            }
            // Natural deaths in this fixture happen at the calendrical 85th
            // birthday, and health collapse can cause earlier death.
            foreach (var p in _people)
            {
                if (!p.Alive) continue;
                if (Age(p, date) >= 85 || p.Health <= 0) Die(p);
            }
        }
        PayContributoryPensions(date);
        if (_config.BoundedRetailService)
            foreach (var shop in _shops) shop.TodayCheckoutUnitsLeft = 0;
        // Food and jobs are not simulated by a population aggregate: each
        // household's real stock and each person's physical state matter.
        foreach (var h in _households)
        {
            foreach (var id in h.Members)
            {
                var p = _people[id]; if (!p.Alive) continue;
                var ateRealFoodToday = h.Pantry > 0;
                if (ateRealFoodToday)
                {
                    h.Pantry--;
                    _eatenFood++;
                    if (_config.MunicipalSanitationPilot) { h.TrashUnits++; _garbageGenerated++; }
                    if (!_config.IntegratedV1Stress)
                        p.Health = Math.Min(100, p.Health + (_waterRatio >= .5 ? 1 : 0));
                    if (_config.MunicipalSanitationPilot && h.TrashUnits > 80)
                        p.Health = Math.Max(0, p.Health - 2); // material service consequence: unhygienic accumulation
                    _history.Fact(_time, id, 3, 1);
                }
                else
                {
                    _unservedMeals++;
                    p.Health -= 1;
                    if (p.Health <= 0) Die(p);
                }
                if (_config.IntegratedV1Stress && p.Alive) ConsumeIntegratedServices(p, ateRealFoodToday);
                if (!_config.DemandStaffing && p.Alive && Age(p, date) >= 18 && Age(p, date) < 70 &&
                    (!_config.VacancyMarket ||
                     (p.Job >= 0 && (day + p.Id) % 7 != 0)))
                {
                    var startHour = _config.VacancyMarket && p.Job < StoreCount
                        ? (p.Id % 2 == 0 ? 11 : 14) : 8;
                    var endHour = _config.VacancyMarket && p.Job < StoreCount
                        ? (p.Id % 2 == 0 ? 21 : 23) : 17;
                    Schedule(_time + startHour * 3600, EventKind.StartWork, id);
                    Schedule(_time + endHour * 3600, EventKind.EndWork, id);
                }
            }
            // A retired household cannot depend on a worker's EndWork event to
            // buy groceries. Its shopper still has to travel, pay and return.
            if (h.Pantry < 30 && (_config.DemandStaffing || _config.VacancyMarket ||
                !h.Members.Any(id =>
                _people[id].Alive && Age(_people[id], date) is >= 18 and < 70)))
            {
                var buyer = h.Members.FirstOrDefault(id =>
                    _people[id].Alive && Age(_people[id], date) >= 18 &&
                    (!_config.VacancyMarket || (!_people[id].Traveling &&
                        _people[id].Node == h.Home)), -1);
                if (buyer >= 0)
                {
                    // Desynchronise residential errands. Shops require
                    // physically present workers; an 18:00 citywide rush was
                    // arriving after actual retail shifts in congested roads.
                    var shoppingHour = _config.VacancyMarket
                        ? 15 + (int)((h.Id * 7L + day) % 4) : 18;
                    Schedule(_time + shoppingHour * 3600, EventKind.HouseholdShop, buyer);
                }
            }
        }
        for (var shopId = 0; shopId < _shops.Length; shopId++)
        {
            var farmId = shopId % _farms.Length;
            var shop = _shops[shopId];
            var farm = _farms[farmId];
            var want = Math.Max(0, 1000 - shop.Stock);
            var count = Math.Min(500, Math.Min(want, farm.Stock));
            if (count <= 0 || shop.Cash < count * WholesalePrice) continue;
            if (_config.FreightLabor) continue; // a real farm employee dispatches the cargo
            // Claim cargo and pay supplier before departure; actual store stock
            // increases ONLY after the truck reaches the destination.
            shop.Cash -= count * WholesalePrice;
            farm.Cash += count * WholesalePrice;
            if (_config.WageFeedback || _config.DemandStaffing || _config.VacancyMarket)
            {
                shop.DaySupplies += count * WholesalePrice;
                farm.DayReceipts += count * WholesalePrice;
            }
            farm.Stock -= count;
            var t = NewTrip(-1, farm.Node, shop.Node, EventKind.Delivery, shopId, count);
            Schedule(_time + 4 * 3600, EventKind.Move, t.Id);
            _history.Fact(_time, -2000 - shopId, 9, count);
        }
        // Combined early-population diagnosis. No state, price or health changes.
        if (_config.IntegratedV1Stress && (_days is 30 or 60 or 90 or 95 or 100
            or 105 or 110 or 115 or 120 or 150 or 180))
        {
            var hungryHomes = _households.Count(h => h.Pantry == 0 &&
                h.Members.Any(id => _people[id].Alive));
            var hungryAffordable = _households.Count(h => h.Pantry == 0 &&
                h.Members.Any(id => _people[id].Alive) &&
                h.Members.Where(id => _people[id].Alive).Sum(id => _people[id].Cash) >=
                    _shops[(h.Home + 1) % Nodes].RetailUnitPriceCents);
            var hungryWithStoreFood = _households.Count(h => h.Pantry == 0 &&
                h.Members.Any(id => _people[id].Alive) &&
                _shops[(h.Home + 1) % Nodes].Stock > 0);
            var potentialShoppers = _households.Count(h => h.Pantry < 30 &&
                h.Members.Any(id => _people[id].Alive && Age(_people[id], date) >= 18 &&
                    !_people[id].Traveling && _people[id].Node == h.Home));
            Console.Error.WriteLine(
                $"LAB_EARLY_CAUSAL day={_days} alive={_active} births={_births} deaths={_deaths} " +
                $"healthDeaths={_deathsWithCollapsedHealth} ageDeaths={_deathsAtAgeLimit} " +
                $"livingHealthLE10={_people.Count(p => p.Alive && p.Health <= 10)} " +
                $"livingHealthLE50={_people.Count(p => p.Alive && p.Health <= 50)} " +
                $"unservedMeals={_unservedMeals} deniedPurchases={_unsatisfiedSales} " +
                $"hungryHomes={hungryHomes} hungryCanAfford={hungryAffordable} " +
                $"hungryWithStoreStock={hungryWithStoreFood} potentialShopperHomes={potentialShoppers} " +
                $"householdFood={_households.Sum(h => (long)h.Pantry)} " +
                $"storeFood={_shops.Sum(s => (long)s.Stock)} farmFood={_farms.Sum(f => (long)f.Stock)} " +
                $"storeNoStock={_shops.Count(s => s.Stock == 0)} " +
                $"retailContractedWorkers={_people.Count(p => p.Alive && p.Job >= 0 && p.Job < StoreCount)} " +
                $"realFreightTrips={_freightLaborTrips} busyFarmTrucks={_farms.Count(f => f.TruckBusy)} " +
                $"storePriceMin={_shops.Min(s => s.RetailUnitPriceCents)} " +
                $"storePriceMax={_shops.Max(s => s.RetailUnitPriceCents)} " +
                $"retailSales={_sales} employedAdults={_people.Count(p => p.Alive && p.Job >= 0 && Age(p, date) >= 18)} " +
                $"farmCash={_farms.Sum(f => f.Cash)} shopCash={_shops.Sum(s => s.Cash)} " +
                $"citizenCash={_people.Sum(p => p.Cash)}");
        }
        _minActive = Math.Min(_minActive, _active);
        if (_days % 365 == 0)
        {
            AssertConservation();
            Console.Error.WriteLine($"LAB_PROGRESS days={_days} live={_active} births={_births} deaths={_deaths} trips={_journeys} queue={_events.Count}");
            var childCount = _people.Count(p => p.Alive && Age(p, date) < 18);
            var adultCount = _people.Count(p => p.Alive && Age(p, date) is >= 18 and < 70);
            var olderCount = _active - childCount - adultCount;
            var emptyWithFunds = _households.Count(h => h.Pantry == 0 &&
                h.Members.Any(id => _people[id].Alive) &&
                h.Members.Where(id => _people[id].Alive).Sum(id => _people[id].Cash) >= FoodPrice);
            var incomeHouseholds = _households.Count(h => h.Members.Any(id => _people[id].Alive && _people[id].Job >= 0 && Age(_people[id], date) is >= 18 and < 70));
            var occupiedHomes = _households.Count(h => h.Members.Any(id => _people[id].Alive));
            var childOnlyHomes = _households.Count(h =>
                h.Members.Any(id => _people[id].Alive) &&
                !h.Members.Any(id => _people[id].Alive && Age(_people[id], date) >= 18));
            Console.Error.WriteLine($"LAB_ECON day={_days} peopleCash={_people.Sum(p=>p.Cash)} shopCash={_shops.Sum(s=>s.Cash)} farmCash={_farms.Sum(f=>f.Cash)} treasury={_treasury} reserve={_reserveGlobal} familyEmpty={_households.Count(h=>h.Pantry==0)} noMoneyAdults={_people.Count(p=>p.Alive&&p.Cash==0&&Age(p,date)>=18)} unmet={_unservedMeals} deniedPurchases={_unsatisfiedSales} realSales={_sales} tripWaitSec={_waitSeconds}");
            var inhabitedEmptyPantries = _households.Count(h => h.Pantry == 0 &&
                h.Members.Any(id => _people[id].Alive));
            var hungryWithEmptyStore = _households.Count(h => h.Pantry == 0 &&
                h.Members.Any(id => _people[id].Alive) &&
                _shops[(h.Home + 1) % Nodes].Stock == 0);
            var hungryWithStockAndFunds = _households.Count(h => h.Pantry == 0 &&
                h.Members.Where(id => _people[id].Alive).Sum(id => _people[id].Cash) >= FoodPrice &&
                _shops[(h.Home + 1) % Nodes].Stock > 0);
            Console.Error.WriteLine($"LAB_INCOME day={_days} earningHomes={incomeHouseholds} occupiedHomes={occupiedHomes} employed={_people.Count(p => p.Alive && p.Job>=0)} hungryWithStockAndFunds={hungryWithStockAndFunds} foodSpendNeededDailyCents={_active * (_config.CostBasedFoodPrice ? _shops.Average(f=>f.RetailUnitPriceCents) : FoodPrice):F0} wagePaidCumulativeCents={_paidWages} actualRetailSalesCents={_retailSalesCents} realFoodSalesCumulativeUnits={_sales}");
            Console.Error.WriteLine($"LAB_CAUSE day={_days} children={childCount} adults={adultCount} retired={olderCount} childOnlyHomes={childOnlyHomes} livedInEmptyPantries={inhabitedEmptyPantries} emptyWithFunds={emptyWithFunds} hungryEmptyStore={hungryWithEmptyStore} hungryStockAndFunds={hungryWithStockAndFunds} shopsNoStock={_shops.Count(f=>f.Stock==0)} shopsNoCash={_shops.Count(f=>f.Cash<WholesalePrice*32)} shopFood={_shops.Sum(f=>f.Stock)} farmFood={_farms.Sum(f=>f.Stock)} eligibleCarerHomes={_households.Count(h=>(_config.LivingOccupancyBirthCap ? h.Members.Count(id=>_people[id].Alive) : h.Members.Count)<16 && h.Members.Count(id=>_people[id].Alive&&Age(_people[id],date) is >=18 and <60)>=2)}");
            if (_config.IntegratedV1Stress)
            {
                var elderly80 = _people.Count(p => p.Alive && Age(p, date) >= 80);
                var familiesEverFull = _households.Count(h => h.Members.Count >= 16);
                var familiesLiveFull = _households.Count(h => h.Members.Count(id => _people[id].Alive) >= 16);
                Console.Error.WriteLine(
                    $"LAB_DEMOGRAPHY day={_days} alive={_active} birthsYear={_births - _lastDemographyBirths} " +
                    $"deathsYear={_deaths - _lastDemographyDeaths} ageDeathsYear={_deathsAtAgeLimit - _lastDemographyAgeDeaths} " +
                    $"healthDeathsYear={_deathsWithCollapsedHealth - _lastDemographyHealthDeaths} " +
                    $"ageDeathsTotal={_deathsAtAgeLimit} healthDeathsTotal={_deathsWithCollapsedHealth} " +
                    $"elderly80={elderly80} birthAppointments={_birthAppointments} " +
                    $"blockedEverSlots={_birthBlockedEverSlots} blockedLiveSlots={_birthBlockedLiveSlots} " +
                    $"blockedPantry={_birthBlockedPantry} blockedSpacing={_birthBlockedSpacing} " +
                    $"blockedCarers={_birthBlockedCarers} blockedCash={_birthBlockedCash} " +
                    $"blockedWater={_birthBlockedWater} everFullHomes={familiesEverFull} liveFullHomes={familiesLiveFull} " +
                    $"livingOccupancyCapEnabled={_config.LivingOccupancyBirthCap} " +
                    $"birthFallbackCandidatesChecked={_birthFallbackCandidatesChecked} birthFallbackBirths={_birthFallbackBirths} " +
                    $"birthFallbackNoEligibleDays={_birthFallbackDaysWithoutEligible}");
                _lastDemographyBirths = _births;
                _lastDemographyDeaths = _deaths;
                _lastDemographyAgeDeaths = _deathsAtAgeLimit;
                _lastDemographyHealthDeaths = _deathsWithCollapsedHealth;
            }
            // Bounded ten-year trace for families who can pay and whose local
            // store has food, yet their household inventory is empty.
            if (_days == 3650)
            {
                foreach (var h in _households.Where(h => h.Pantry == 0 &&
                    h.Members.Where(id => _people[id].Alive).Sum(id => _people[id].Cash) >= FoodPrice &&
                    _shops[(h.Home + 1) % Nodes].Stock > 0).Take(16))
                {
                    var members = h.Members.Where(id => _people[id].Alive).Select(id =>
                    {
                        var p = _people[id];
                        var t = p.TripId < 0 ? null : _trips[p.TripId];
                        return $"{id}:a{Age(p,date)}:c{p.Cash}:at{p.AtWork}:travel{p.Traveling}:node{p.Node}:to{t?.Destination ?? -1}:exit{t?.ExitAt ?? -1}";
                    });
                    Console.Error.WriteLine($"LAB_HUNGRY_FAMILY id={h.Id} home={h.Home} lastShopDay={h.LastShoppingDepartureDay} shopStock={_shops[(h.Home + 1) % Nodes].Stock} members=[{string.Join(';', members)}]");
                }
            }

        }
        if (_days < _config.Days) Schedule(_time + DaySeconds, EventKind.Day, 0);
    }
    private void ReconcileVacancies(DateTime date, long day)
    {
        // Counterfactual V1-scale job market: firm-funded positions are formed
        // from actual 30-day grocery sales / wholesale orders, never guaranteed
        // inhabitants or new money. This is an intentionally narrow test, NOT
        // the full autonomous employer, wage negotiation or skills contract.
        Span<int> positions = stackalloc int[StoreCount + FarmCount + (_config.ConstructionPilot ? 1 : 0) + (_config.MunicipalSanitationPilot ? 1 : 0) + (_config.MunicipalWasteTreatmentPilot ? 1 : 0) + (_config.IntegratedV1Stress ? 5 : 0)];
        Span<int> occupied = stackalloc int[StoreCount + FarmCount + (_config.ConstructionPilot ? 1 : 0) + (_config.MunicipalSanitationPilot ? 1 : 0) + (_config.MunicipalWasteTreatmentPilot ? 1 : 0) + (_config.IntegratedV1Stress ? 5 : 0)];
        long municipalPayrollBudget = _treasury;
        // Paid service jobs work at most 26 days per 30-day calendar month
        // (one day of rest in seven). Monthly funding follows ACTUAL possible
        // shifts instead of fictitious 30 worked days.
        var municipalPaidShiftDays = _config.AttendanceAwareMunicipalShifts ? 26L : 30L;
        for (var i = 0; i < positions.Length; i++)
        {
            if (_config.IntegratedV1Stress && i >= IntakeJob && i <= FuelStationJob)
            {
                var budget = i == FuelStationJob ? _fuelStation.Cash : municipalPayrollBudget;
                positions[i] = (int)Math.Min(IntegratedWantedWorkers(i),
                    budget / (UtilityGrossWage * municipalPaidShiftDays));
                if (i != FuelStationJob) municipalPayrollBudget -= positions[i] * UtilityGrossWage * municipalPaidShiftDays;
                continue;
            }
            if (_config.MunicipalWasteTreatmentPilot && i == WasteTreatmentJob)
            {
                // Water/food consumption already creates household waste.
                // Keep finite treatment throughput per real paid shift, and
                // use ONLY the municipal cash left after collection contracts.
                // Capacity planning from both NEW household waste and REAL
                // inventory waiting for processing, with a bounded optional
                // debt-clearance horizon. Paid workers and physical material
                // movements remain mandatory; this is a laboratory pilot.
                var backlog = Math.Max(0L, _garbageAtDepot + _garbageInTransit);
                var neededWaste = (long)_active * 30 +
                    (_config.TreatmentBacklogMonths > 0
                        ? (backlog + _config.TreatmentBacklogMonths - 1) / _config.TreatmentBacklogMonths
                        : 0L);
                var demanded = (int)Math.Min(MaxWasteTreatmentWorkers,
                    (neededWaste + _config.WasteTreatmentUnitsPerShift * 26 - 1) /
                    (_config.WasteTreatmentUnitsPerShift * 26));
                positions[i] = (int)Math.Min(demanded,
                    municipalPayrollBudget / (WasteTreatmentGrossWage * municipalPaidShiftDays));
                municipalPayrollBudget -= positions[i] * WasteTreatmentGrossWage * municipalPaidShiftDays;
                continue;
            }
            if (_config.MunicipalSanitationPilot && i == SanitationJob)
            {
                // One actual home served per real shift, ~weekly residence service.
                var liveHomes = _households.Count(h => h.Members.Any(id => _people[id].Alive));
                var interval = _config.MunicipalCollectionIntervalDays;
                var demanded = (int)Math.Min(1600L,
                    _config.AttendanceAwareMunicipalShifts
                        ? ((long)liveHomes * 30 + interval * 26 - 1) / (interval * 26L)
                        : (liveHomes + interval - 1) / interval);
                positions[i] = Math.Min(demanded, (int)(municipalPayrollBudget / (80L * municipalPaidShiftDays)));
                municipalPayrollBudget -= positions[i] * 80L * municipalPaidShiftDays;
                continue;
            }
            if (i == BuilderJob)
            {
                var sitesReady = _construction.Count(site => site.Ordered &&
                    !site.Finished && site.TimberOnSite > 0);
                var offered = Math.Min(32, sitesReady);
                positions[i] = Math.Min(offered, (int)Math.Min(int.MaxValue, _builder.Cash / (80L * 30)));
                _builder.OfferedPositions = positions[i];
                continue;
            }
            var farm = i >= StoreCount;
            var firm = farm ? _farms[i - StoreCount] : _shops[i];
            // A variable price makes revenue/old-fixed-price a false volume estimate.
            var monthlyUnits = farm ? (_config.FarmCostPricing ? firm.MonthWholesaleSalesUnits : firm.DayReceipts / WholesalePrice) :
                (_config.CostBasedFoodPrice ? firm.MonthRetailSalesUnits : firm.DayReceipts / FoodPrice);
            if (_config.CostBasedFoodPrice && !farm &&
                (firm.MonthRetailSalesUnits > 0 || _config.CapacityPricedFood))
            {
                // Price is a transparent observed cost estimate, not a target survival
                // price. Wages are ACTUALLY paid and all goods still have a real
                // wholesale acquisition cost. 5% markup is only a lab calibration.
                // Upward/downward price is bounded solely by observed costs, not need.
                var units = firm.MonthRetailSalesUnits;
                var acquisition = checked(units * WholesalePrice);
                var observedCosts = checked(firm.MonthRetailPaidWagesCents + acquisition);
                long priced;
                if (_config.CapacityPricedFood)
                {
                    // In real shops a slow month is a LOSS, not permission to
                    // charge the next buyer for all idle staff. Base the quote
                    // on the observed worker wage per scheduled capacity of
                    // five servings per paid shift, already the staffed retail
                    // throughput assumption in this laboratory.
                    var shifts = firm.MonthRetailPaidWagesCents / 65;
                    var wagePerShift = shifts > 0
                        ? (firm.MonthRetailPaidWagesCents + shifts - 1) / shifts : 65;
                    var purchaseUnitCost = _config.FarmCostPricing && firm.MonthWholesaleBoughtUnits > 0
                        ? (firm.MonthWholesalePaidCents + firm.MonthWholesaleBoughtUnits - 1) / firm.MonthWholesaleBoughtUnits
                        : WholesalePrice;
                    var taxPerRetailUnit = _config.TaxInclusiveProductPrice
                        ? ((firm.MonthPropertyTaxActuallyPaidCents * _config.TaxCostRecoveryPercent / 100) +
                           Math.Max(1L, (long)_active * 30 / StoreCount) - 1) /
                          Math.Max(1L, (long)_active * 30 / StoreCount)
                        : 0L;
                    var normalUnitCost = purchaseUnitCost + taxPerRetailUnit +
                        (_config.BasketRetailService
                            ? (wagePerShift + _config.RetailBasketsPerShift * 32 - 1) / (_config.RetailBasketsPerShift * 32)
                            : (wagePerShift + 4) / 5);
                    priced = (checked(normalUnitCost * 105) + 99) / 100;
                }
                else
                    priced = (checked(observedCosts * 105) + units * 100 - 1) / (units * 100);
                firm.RetailUnitPriceCents = checked((int)Math.Clamp(priced, WholesalePrice + 1L, int.MaxValue));
            }
            if (_config.FarmCostPricing && farm && firm.MonthFoodProduced > 0)
            {
                var numerator = checked((firm.MonthFarmPaidWagesCents +
                    (_config.IntegratedV1Stress ? firm.MonthFreightFuelConsumedCents : 0) +
                    (_config.TaxInclusiveProductPrice ? firm.MonthPropertyTaxActuallyPaidCents * _config.TaxCostRecoveryPercent / 100 : 0)) * 105);
                var denominator = checked(firm.MonthFoodProduced * 100);
                var unitPrice = (numerator + denominator - 1) / denominator;
                firm.FarmWholesaleUnitPriceCents = checked((int)Math.Clamp(unitPrice, 1, int.MaxValue));
            }
            // Farm staffing is sized from REAL food physically produced in
            // previous paid shifts, not its theoretical maximum yield of 35.
            // Before any actual production, use a conservative 10 units/shift
            // stress estimate: demand exists before initial sales or harvest.
            var realisedFoodPerShift = farm && firm.MonthFarmWorkdays > 0
                ? Math.Max(1L, firm.MonthFoodProduced / firm.MonthFarmWorkdays)
                : 10L;
            // Farm workers in the current calendar are deliberately absent 1 day
            // out of 7. Counting them as present on 30/30 days consistently
            // underfunds actual harvest. Only harvest is physically capacity-
            // limited here; retail throughput is not yet faithfully capped.
            var farmPaidShiftsInMonth = _config.AttendanceAwareFarms ? 26L : 30L;
            // One retail shift can check out five HOUSEHOLD BASKETS, not five
            // individual meal units. A physical basket carries at most 32 food
            // items; absent staff cannot serve. Count 26 paid shifts/month
            // under the actual calendar (one day off per 7). The eight-job
            // maximum is a small-store fixture cap, NOT a V1 product rule.
            var unitsPerEmployee = farm ? realisedFoodPerShift * farmPaidShiftsInMonth :
                _config.BasketRetailService ? (long)_config.RetailBasketsPerShift * 32 * 26 : 5L * 30;
            // Real, already-resident households have a daily food need even
            // before the first retail sale. Staff planning must consider that
            // demand; otherwise eight farms hire only 16 starters and all
            // stores run dry while the population starves with money available.
            // These capacity factors are laboratory calibration ONLY.
            var expectedMonthlyDemand = (long)_active * 30 /
                (farm ? FarmCount : StoreCount);
            var requiredUnits = Math.Max(monthlyUnits, expectedMonthlyDemand);
            var wanted = (int)Math.Clamp(
                (requiredUnits + unitsPerEmployee - 1) / unitsPerEmployee,
                farm ? 2 : (_config.BasketRetailService ? 2 : 1),
                farm ? 128 : (_config.BasketRetailService ? 8 : 96));
            if (_config.BasketRetailService && !farm)
            {
                // A family with too little money or stock may buy only two
                // servings and STILL need one entire cashier interaction.
                // Demand for real workers is measured in actual successful
                // customer visits, with an explicit floor from living
                // households' 32-item maximum basket consumption.
                var expectedBaskets = (expectedMonthlyDemand + 31) / 32;
                var neededBaskets = Math.Max(firm.MonthRetailBaskets, expectedBaskets);
                var basketsPerFundedWorkerMonth = (long)_config.RetailBasketsPerShift * 26;
                wanted = (int)Math.Clamp(
                    (neededBaskets + basketsPerFundedWorkerMonth - 1) /
                    basketsPerFundedWorkerMonth, 2, 8);
            }
            var wage = farm ? 80 : 65;
            // Freight positions are extra real hires, never substitutes for
            // the staff producing the meals used by the city.
            if (_config.FreightLabor && farm) wanted += 8;
            var funded = (int)Math.Min(firm.Cash / (wage * 30L), wanted);
            positions[i] = funded;
            firm.OfferedPositions = funded;
            firm.DayReceipts = 0;
            firm.DaySupplies = 0;
            firm.MonthFoodProduced = 0;
            firm.MonthFarmWorkdays = 0;
            firm.MonthRetailSalesUnits = 0;
            firm.MonthRetailPaidWagesCents = 0;
            firm.MonthRetailBaskets = 0;
            firm.MonthFarmPaidWagesCents = 0;
            firm.MonthFreightFuelConsumedCents = 0;
            firm.MonthWholesaleSalesUnits = 0;
            firm.MonthWholesaleBoughtUnits = 0;
            firm.MonthWholesalePaidCents = 0;
            firm.MonthPropertyTaxActuallyPaidCents = 0;
        }
        // Keep valid jobs before recruiting; losing sales may end a vacancy.
        // Changing an employer at midnight never materializes transportation
        // or wages. Only someone actually arriving for the shift is paid.
        for (var i = 0; i < _people.Count; i++)
        {
            var p = _people[i];
            if (p.Job < 0) continue;
            var age = Age(p, date);
            if (!p.Alive || age is < 18 or >= 70 ||
                occupied[p.Job] >= positions[p.Job])
            {
                p.Job = -1;
                p.IsFreightDriver = false;
                p.AtWork = false;
                continue;
            }
            occupied[p.Job]++;
        }
        // A/B stress test: does the household distribution of REAL, FUNDED
        // jobs explain starvation, even with the same firms and no new wages?
        // First pass only considers households without a paid worker. Retained
        // contracts remain intact; second pass fills remaining real positions.
        var householdHasWorker = new bool[_households.Length];
        if (_config.FairHouseholdHiring)
            foreach (var p in _people)
                if (p.Alive && p.Job >= 0 && Age(p, date) is >= 18 and < 70)
                    householdHasWorker[p.HouseholdId] = true;
        var start = (int)(((long)_config.Seed + day * 43) % _people.Count);
        var passes = _config.FairHouseholdHiring ? 2 : 1;
        for (var pass = 0; pass < passes; pass++)
        {
            for (var k = 0; k < _people.Count; k++)
            {
                var p = _people[(start + k) % _people.Count];
                if (!p.Alive || p.Job >= 0 || Age(p, date) is < 18 or >= 70 ||
                    (_config.FairHouseholdHiring && pass == 0 && householdHasWorker[p.HouseholdId]))
                    continue;
                var best = -1;
                var bestDistance = int.MaxValue;
                for (var i = 0; i < positions.Length; i++)
                {
                    if (occupied[i] >= positions[i]) continue;
                    var node = i == BuilderJob ? _builder.Node :
                        (_config.IntegratedV1Stress && i >= IntakeJob && i <= FuelStationJob) ? IntegratedJobNode(i) :
                        (_config.MunicipalWasteTreatmentPilot && i == WasteTreatmentJob) ? SanitationDepotNode :
                        (_config.MunicipalSanitationPilot && i == SanitationJob) ? p.Home :
                        i < StoreCount ? _shops[i].Node : _farms[i - StoreCount].Node;
                    var distance = Math.Abs(p.Home % Grid - node % Grid) +
                                   Math.Abs(p.Home / Grid - node / Grid);
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    best = i;
                }
                if (best < 0) break;
                p.Job = best;
                occupied[best]++;
                if (_config.FairHouseholdHiring) householdHasWorker[p.HouseholdId] = true;
            }
        }
        if (_config.FreightLabor)
        {
            Span<int> drivers = stackalloc int[FarmCount];
            foreach (var p in _people)
            {
                p.IsFreightDriver = false;
                if (!p.Alive || p.Job < StoreCount ||
                    p.Job >= StoreCount + FarmCount) continue;
                var farmId = p.Job - StoreCount;
                if (drivers[farmId] >= 8) continue;
                p.IsFreightDriver = true;
                drivers[farmId]++;
            }
        }
    }

    private void ScheduleDemandFundedShifts(DateTime date, long day)
    {
        // Isolated counterfactual: only real previous-day customer receipts
        // finance new shift offers. Wholesale is an expense to stores and
        // genuine income to farms. No salaries, jobs or cash are created.
        // This models shift allocations, not complete employment contracts.
        Span<int> quotas = stackalloc int[StoreCount + FarmCount];
        Span<int> used = stackalloc int[StoreCount + FarmCount];
        for (var i = 0; i < quotas.Length; i++)
        {
            var farm = i >= StoreCount;
            var employer = farm ? _farms[i - StoreCount] : _shops[i];
            var revenue = employer.DayReceipts - (farm ? 0 : employer.DaySupplies);
            var grossWage = farm ? 80 : 65;
            // Up to 98% of realised net daily receipts finance new shifts.
            // Positive buffers may subsidise only one trial shift, never
            // unlimited workers. Minimum staff does not imply a sale.
            employer.PayrollBudget = Math.Clamp(
                employer.PayrollBudget + revenue * 98 / 100, 0, employer.Cash);
            // A one-shift exploratory floor is paid only from company cash.
            // Excess receipts remain earmarked for genuine future shifts.
            var offeredBudget = Math.Max(employer.PayrollBudget,
                employer.Cash >= grossWage ? grossWage : 0);
            quotas[i] = (int)Math.Min(offeredBudget / grossWage, employer.Cash / grossWage);
            employer.DayReceipts = 0;
            employer.DaySupplies = 0;
            employer.DayPaidShifts = 0;
        }
        // Rotate physical people, not aggregate workers: years of identical
        // offers to the first N workers would concentrate income by household.
        var count = _people.Count;
        if (count == 0) return;
        var start = (int)((day * 37 + _config.Seed) % count);
        for (var k = 0; k < count; k++)
        {
            var p = _people[(start + k) % count];
            if (!p.Alive || Age(p, date) is < 18 or >= 70) continue;
            if (used[p.Job] >= quotas[p.Job]) continue;
            used[p.Job]++;
            Schedule(_time + 8 * 3600, EventKind.StartWork, p.Id);
            Schedule(_time + 17 * 3600, EventKind.EndWork, p.Id);
        }
    }
    private bool CanBirth(Household h, DateTime date, long day)
    {
        if ((_config.LivingOccupancyBirthCap
                ? h.Members.Count(id => _people[id].Alive)
                : h.Members.Count) >= 16 || h.Pantry < 2)
            return false;
        if (_config.CohortRenewal && day - h.LastBirthDay < 4 * 365)
            return false;
        var carers = 0;
        long cash = 0;
        foreach (var id in h.Members)
        {
            var p = _people[id];
            if (!p.Alive) continue;
            if (Age(p, date) is >= 18 and < 60) carers++;
            cash += p.Cash;
        }
        return carers >= 2 && cash >= 300 && _waterRatio >= .4;
    }

    private bool TryBirth(Household h, DateTime date, long day)
    {
        if (_config.IntegratedV1Stress)
        {
            // Independent, overlapping diagnostic reasons for the SAME real
            // family appointment. Do not bypass any existing birth condition.
            _birthAppointments++;
            var livingMembers = h.Members.Count(id => _people[id].Alive);
            var eligibleCarers = h.Members.Count(id => _people[id].Alive &&
                Age(_people[id], date) is >= 18 and < 60);
            var livingCash = h.Members.Where(id => _people[id].Alive)
                .Sum(id => _people[id].Cash);
            if (h.Members.Count >= 16) _birthBlockedEverSlots++;
            if (livingMembers >= 16) _birthBlockedLiveSlots++;
            if (h.Pantry < 2) _birthBlockedPantry++;
            if (day - h.LastBirthDay < 4 * 365) _birthBlockedSpacing++;
            if (eligibleCarers < 2) _birthBlockedCarers++;
            if (livingCash < 300) _birthBlockedCash++;
            if (_waterRatio < .4) _birthBlockedWater++;
        }
        // The stress fixture historically counted dead relatives against
        // the physical 16-person housing limit. A deceased resident cannot
        // occupy a housing slot; preserve the lineage but allow its slot to
        // become available in the opt-in counterfactual.
        if (!CanBirth(h, date, day)) return false;
        var child = new Person { Id = _people.Count, HouseholdId = h.Id,
            Born = date, Home = h.Home, Node = h.Home,
            Job = _config.VacancyMarket ? -1 : (h.Id * 7) % 72, Cash = 0 };
        _people.Add(child);
        h.Members.Add(child.Id);
        h.LastBirthDay = (int)day;
        _active++;
        _births++;
        _maxActive = Math.Max(_maxActive, _active);
        _history.Fact(_time, child.Id, 5, h.Id);
        return true;
    }
    private static void ResetDailyWage(Firm firm, int baseWage, long netRevenue)
    {
        firm.WageForDay = firm.DayPaidShifts > 0
            ? (int)Math.Clamp(netRevenue / firm.DayPaidShifts, baseWage, 1000)
            : baseWage;
        firm.DayReceipts = 0;
        firm.DaySupplies = 0;
        firm.DayPaidShifts = 0;
    }
    private void PayContributoryPensions(DateTime date)
    {
        // Approved product DIRECTION: municipal pay-as-you-go, real employee
        // contributions, accumulated individual rights and proportional payout
        // when municipal funds are insufficient. Ages/rates here are STRESS
        // FIXTURE calibration, not newly approved gameplay values.
        var eligible = new List<Person>();
        long totalClaims = 0;
        foreach (var p in _people)
        {
            if (!p.Alive || Age(p, date) < 70 || p.ContributedDays < 5 * 365) continue;
            var dailyDue = Math.Min(80, p.GrossEarnings * 4 / (5L * p.ContributedDays));
            p.PensionCredit = checked(p.PensionCredit + dailyDue);
            totalClaims = checked(totalClaims + p.PensionCredit);
            eligible.Add(p);
        }
        if (totalClaims <= 0 || _treasury <= 0) return;
        var budget = Math.Min(_treasury, totalClaims);
        long distributed = 0;
        foreach (var p in eligible)
        {
            var paid = Math.Min(p.PensionCredit,
                (long)((decimal)budget * p.PensionCredit / totalClaims));
            if (paid <= 0) continue;
            p.PensionCredit -= paid;
            p.PensionReceived += paid;
            p.Cash += paid;
            _treasury -= paid;
            distributed += paid;
            _history.Fact(_time, p.Id, 16, paid);
        }
        var remainder = budget - distributed;
        foreach (var p in eligible)
        {
            if (remainder <= 0) break;
            if (p.PensionCredit <= 0) continue;
            p.PensionCredit--;
            p.PensionReceived++;
            p.Cash++;
            _treasury--;
            remainder--;
            _history.Fact(_time, p.Id, 16, 1);
        }
    }
    private void DoHour()
    {
        var hour = _time / 3600;
        _windFactor = Math.Clamp(.55 + .25 * Math.Sin(hour * 3600.0 / 21600 + _config.Seed) +
                                 .15 * Math.Sin(hour * 3600.0 / 172800 + _config.Seed * .3), .15, .95);
        _waterRatio = Math.Clamp((_windFactor - .15) / .55, 0, 1);
        if (_config.IntegratedV1Stress) RefreshIntegratedHourlyPower();
        if (hour < (_config.Days * DaySeconds) / 3600)
            Schedule(_time + 3600, EventKind.Hour, 0);
    }
    private static int Age(Person p, DateTime now)
    {
        var year = now.Year - p.Born.Year;
        return p.Born.AddYears(year) > now ? year - 1 : year;
    }
    private void Die(Person p)
    {
        if (_config.IntegratedV1Stress && p.AtWork && p.Job >= IntakeJob && p.Job <= FuelStationJob)
        {
            if (p.Job == FuelStationJob) _fuelStation.PresentWorkers--;
            p.AtWork = false;
        }
        if (_config.MunicipalWasteTreatmentPilot && p.Job == WasteTreatmentJob && p.AtWork)
            p.AtWork = false;
        if (_config.MunicipalSanitationPilot && p.Job == SanitationJob && p.AtWork)
            p.AtWork = false; // No private-firm presence for municipal employees.
        if (_config.VacancyMarket && p.AtWork && p.Job >= 0 && p.Job <= BuilderJob)
        {
            var current = p.Job == BuilderJob ? _builder :
                p.Job < StoreCount ? _shops[p.Job] : _farms[p.Job - StoreCount];
            current.PresentWorkers--;
            if (_config.BoundedRetailService && p.Job < StoreCount)
                current.ReservedCheckoutWagesCents -= 65;
            p.AtWork = false;
        }
        if (_config.IntegratedV1Stress && p.FreightShift &&
            p.Job >= StoreCount && p.Job < StoreCount + FarmCount)
            _farms[p.Job - StoreCount].TruckBusy = false;
        // Observability: diagnose whether early losses stem from health or age.
        if (p.Health <= 0) _deathsWithCollapsedHealth++;
        else _deathsAtAgeLimit++;
        p.Alive = false;
        _active--; _deaths++;
        // The 10k requirement is continuous. A death before the next daily
        // census must count even if a subsequent birth masks the dip.
        _minActive = Math.Min(_minActive, _active);
        var family = _households[p.HouseholdId];
        var heir = family.Members.Select(id => _people[id]).FirstOrDefault(x => x.Alive);
        if ((_config.MonthlyResidentialTaxCents > 0 || _config.IntegratedV1Stress) && family.PropertyOwnerPersonId == p.Id)
        {
            family.PropertyOwnerPersonId = heir?.Id ?? -1;
            if (_config.IntegratedV1Stress && heir is not null)
                _history.Fact(_time, heir.Id, 45, p.Id);
        }
        if (heir is null)
        {
            // SPEC: unclaimed private money goes to the GLOBAL RESERVE,
            // never into the municipal payroll-funded pension account.
            _reserveGlobal = checked(_reserveGlobal + p.Cash);
            if (p.Cash > 0) _history.Fact(_time, p.Id, 17, p.Cash);
        }
        else heir.Cash = checked(heir.Cash + p.Cash);
        p.Cash = 0;
        p.PensionCredit = 0; // Unpaid pension claim is not inherited.
        _history.Fact(_time, p.Id, 6, heir?.Id ?? -1);
    }
    private void StartWork(int personId)
    {
        var p = _people[personId];
        if (!p.Alive || p.Job < 0 || p.Traveling || p.Node != p.Home) return;
        var workNode = p.Job < StoreCount ? _shops[p.Job].Node :
            p.Job < BuilderJob ? _farms[p.Job - StoreCount].Node : -1;
        if (_config.MunicipalWasteTreatmentPilot && p.Job == WasteTreatmentJob)
            workNode = SanitationDepotNode;
        if (_config.IntegratedV1Stress && p.Job >= IntakeJob && p.Job <= FuelStationJob)
            workNode = IntegratedJobNode(p.Job);
        if (_config.MunicipalSanitationPilot && p.Job == SanitationJob)
        {
            p.SanitationHouseholdId = PickMunicipalCollectionSite(p);
            if (p.SanitationHouseholdId < 0) return;
            workNode = _households[p.SanitationHouseholdId].Home;
        }
        if (_config.ConstructionPilot && p.Job == BuilderJob)
        {
            p.ConstructionSiteId = PickConstructionSite(p);
            if (p.ConstructionSiteId < 0) return;
            workNode = _households[p.ConstructionSiteId].Home;
        }
        p.HeadingToWork = _config.VacancyMarket;
        StartTrip(p, workNode);
    }
    private void EndWork(int personId)
    {
        var p = _people[personId];
        if (!p.Alive || (_config.VacancyMarket && p.Job < 0)) return;
        if (_config.FreightLabor && p.FreightShift)
            return; // delivered a physical, already-paid freight shift
        if (p.Traveling || !p.AtWork)
        {
            // A worker who missed their shift must not block all other adults
            // from feeding their real household. An idle adult at home may
            // shop after work hours, provided nobody shopped earlier today.
            if (!p.Traveling && p.Node == p.Home &&
                _households[p.HouseholdId].Pantry < 30)
                Schedule(_time + 3600, EventKind.HouseholdShop, personId);
            return;
        }
        if (_config.IntegratedV1Stress && p.Job >= IntakeJob && p.Job <= FuelStationJob)
        {
            FinishIntegratedShift(p);
            if (p.Job == FuelStationJob) _fuelStation.PresentWorkers--;
            return;
        }
        if (_config.MunicipalWasteTreatmentPilot && p.Job == WasteTreatmentJob)
        {
            FinishMunicipalTreatmentShift(p);
            return;
        }
        if (_config.MunicipalSanitationPilot && p.Job == SanitationJob)
        {
            FinishMunicipalSanitationShift(p);
            return;
        }
        var employer = p.Job == BuilderJob ? _builder :
            p.Job < StoreCount ? _shops[p.Job] : _farms[p.Job - StoreCount];
        if (_config.ConstructionPilot && p.Job == BuilderJob && employer.Cash >= 80)
            PerformConstructionShift(p);
        // Paid and physically present shifts drive real harvest. In this new
        // market fixture, an insolvent employer does NOT receive free harvest
        // from workers it cannot pay.
        var wage = _config.WageFeedback && !_config.DemandStaffing
            ? employer.WageForDay : (p.Job < StoreCount ? 65 : 80);
        if (p.Job >= StoreCount && p.Job < BuilderJob &&
            !p.FreightShift && (!_config.VacancyMarket || employer.Cash >= wage))
        {
            var food = (int)(35 * Math.Min(_windFactor, _waterRatio));
            employer.Stock = checked(employer.Stock + food);
            _producedFood += food;
            if (_config.VacancyMarket)
            {
                employer.MonthFoodProduced += food;
                employer.MonthFarmWorkdays++;
            }
            _history.Fact(_time, p.Id, 8, food);
        }
        if (employer.Cash >= wage)
        {
            employer.Cash -= wage;
            if (_config.DemandStaffing)
                employer.PayrollBudget = Math.Max(0, employer.PayrollBudget - wage);
            if (_config.BoundedRetailService && p.Job < StoreCount)
                employer.ReservedCheckoutWagesCents -= 65;
            var contribution = wage / 10; // 10% illustrative contributory fixture
            _treasury += contribution;
            p.Cash += wage - contribution;
            p.ContributedDays++;
            p.GrossEarnings += wage;
            _paidWages += wage;
            _workdays++;
            if (_config.WageFeedback || _config.DemandStaffing || _config.VacancyMarket) employer.DayPaidShifts++;
            if (_config.CostBasedFoodPrice && p.Job < StoreCount)
                employer.MonthRetailPaidWagesCents += wage;
            if (_config.FarmCostPricing && p.Job >= StoreCount && p.Job < BuilderJob)
                employer.MonthFarmPaidWagesCents += wage;
            _history.Fact(_time, p.Id, 7, wage - contribution);
            _history.Fact(_time, p.Id, 15, contribution);
        }
        p.AtWork = false;
        p.FreightShift = false;
        p.ConstructionSiteId = -1;
        if (_config.VacancyMarket)
            employer.PresentWorkers--;
        var h = _households[p.HouseholdId];
        // Select the first *working* adult. The oldest adult might already
        // have retired; choosing them would prevent employed relatives from
        // shopping even when the family has money and local food available.
        var shopper = h.Members.FirstOrDefault(id =>
        {
            var member = _people[id];
            var age = Age(member, Epoch.AddSeconds(_time));
            return member.Alive && age >= 18 && age < 70 && (member.AtWork || member.Id == p.Id);
        }, -1);
        if (shopper == p.Id && TryStartHouseholdShopping(p)) return;
        StartTrip(p, p.Home);
    }
    private void ShopAtHome(int personId)
    {
        var p = _people[personId];
        if (!p.Alive || p.Traveling || p.Node != p.Home) return;
        TryStartHouseholdShopping(p);
    }
    private bool TryStartHouseholdShopping(Person p)
    {
        var h = _households[p.HouseholdId];
        if (h.Pantry >= 30 || h.LastShoppingDepartureDay == _time / DaySeconds) return false;
        // Actual intrafamily transfers, never a fictitious pooled balance.
        var price = _config.CostBasedFoodPrice
            ? _shops[(p.Home + 1) % Nodes].RetailUnitPriceCents : FoodPrice;
        var needed = Math.Max(0, 32L * price - p.Cash);
        if (needed > 0)
            foreach (var id in h.Members)
            {
                var donor = _people[id];
                if (donor.Id == p.Id || !donor.Alive || donor.Cash <= 0) continue;
                var payment = Math.Min(needed, donor.Cash);
                donor.Cash -= payment;
                p.Cash += payment;
                needed -= payment;
                _history.Fact(_time, donor.Id, 14, payment);
                if (needed == 0) break;
            }
        if (p.Cash < price) return false;
        p.RetailVisitedStoresMask = 0;
        p.RetailFallbackAttempts = 0;
        StartTrip(p, _shops[(p.Home + 1) % Nodes].Node);
        h.LastShoppingDepartureDay = _time / DaySeconds;
        return true;
    }
    private Trip NewTrip(int owner, int from, int destination, EventKind completion, int payload = 0, int cargo = 0)
    {
        if (_freeTripSlots.TryPop(out var reused))
        {
            var old = _trips[reused];
            old.OwnerId = owner; old.CurrentNode = from;
            old.Destination = destination; old.Completion = completion;
            old.Payload = payload; old.Cargo = cargo;
            old.From = old.To = 0; old.EnterAt = old.ExitAt = 0;
            old.Completed = false;
            return old;
        }
        var trip = new Trip { Id = _trips.Count, OwnerId = owner, CurrentNode = from,
            Destination = destination, Completion = completion, Payload = payload, Cargo = cargo };
        _trips.Add(trip);
        return trip;
    }
    // One existing farm truck is shared SEQUENTIALLY by real contracted
    // drivers already on site. A returned truck can take another paid trip
    // during the same shift; no second vehicle, free salary or cargo is created.
    private void DispatchNextFreightFromFarm(int farmId)
    {
        var farm = _farms[farmId];
        if (!_config.IntegratedV1Stress || farm.TruckBusy ||
            _time % DaySeconds >= 17 * 3600) return;
        foreach (var candidate in _people)
        {
            if (!candidate.Alive || candidate.Job != StoreCount + farmId ||
                !candidate.IsFreightDriver || !candidate.AtWork ||
                candidate.Traveling || candidate.FreightShift ||
                candidate.Node != farm.Node) continue;
            if (!TryAssignFreight(candidate)) return;
            candidate.FreightShift = true;
            candidate.AtWork = false;
            farm.PresentWorkers--;
            return;
        }
    }

    private bool TryAssignFreight(Person driver)
    {
        var farmId = driver.Job - StoreCount;
        var farm = _farms[farmId];
        if (farm.Stock <= 0) return false;
        // Original lab traps each of eight shops behind a single farm, even
        // if another farm has a physical surplus. Optional counterfactual:
        // choose an actual solvent buyer among all 64 shops, preferring the
        // most depleted actual stock, then shortest existing-road distance.
        // No extra food, sellers, trucks or employees are created.
        const int freightPrice = 35;
        var target = -1;
        var chosenStock = int.MaxValue;
        var chosenDistance = int.MaxValue;
        var options = _config.CrossFarmFreight ? StoreCount : 8;
        for (var offset = 0; offset < options; offset++)
        {
            var shopId = _config.CrossFarmFreight
                ? (driver.Id + offset) % StoreCount
                : farmId + 8 * ((driver.Id + offset) % 8);
            var shop = _shops[shopId];
            var amount = Math.Min(500, Math.Min(farm.Stock, Math.Max(0, 1000 - shop.Stock)));
            var invoice = amount * (_config.FarmCostPricing ? farm.FarmWholesaleUnitPriceCents : WholesalePrice) + freightPrice;
            if (amount <= 0 || shop.Cash < invoice || farm.Cash < 80) continue;
            var distance = Math.Abs(farm.Node % Grid - shop.Node % Grid) +
                           Math.Abs(farm.Node / Grid - shop.Node / Grid);
            if (target < 0 || (shop.Stock < chosenStock) ||
                (shop.Stock == chosenStock && distance < chosenDistance))
            {
                target = shopId;
                chosenStock = shop.Stock;
                chosenDistance = distance;
            }
            if (!_config.CrossFarmFreight) break;
        }
        if (target < 0) return false;
        if (_config.IntegratedV1Stress)
        {
            if (!HasTruckFuelForTrip(farmId, target)) return false;
            farm.TruckBusy = true;
        }
        {
            var shopId = target;
            var shop = _shops[shopId];
            var amount = Math.Min(500, Math.Min(farm.Stock, Math.Max(0, 1000 - shop.Stock)));
            var invoice = amount * (_config.FarmCostPricing ? farm.FarmWholesaleUnitPriceCents : WholesalePrice) + freightPrice;
            // The assigned worker is already physically at the real farm
            // and performs paid loading + road delivery, not free overtime.
            const int grossFreightWage = 80;
            farm.Cash -= grossFreightWage;
            _treasury += grossFreightWage / 10;
            driver.Cash += grossFreightWage - grossFreightWage / 10;
            driver.ContributedDays++;
            driver.GrossEarnings += grossFreightWage;
            _paidWages += grossFreightWage;
            _workdays++;
            _history.Fact(_time, driver.Id, 7, grossFreightWage - grossFreightWage / 10);
            _history.Fact(_time, driver.Id, 15, grossFreightWage / 10);
            shop.Cash -= invoice;
            farm.Cash += invoice;
            shop.DaySupplies += invoice;
            farm.DayReceipts += invoice;
            if (_config.FarmCostPricing)
            {
                shop.MonthWholesaleBoughtUnits += amount;
                shop.MonthWholesalePaidCents += invoice;
                farm.MonthWholesaleSalesUnits += amount;
                farm.MonthFarmPaidWagesCents += grossFreightWage;
            }
            _paidFreightCents += freightPrice;
            farm.Stock -= amount;
            driver.FreightCargo = amount;
            driver.FreightShopId = shopId;
            _history.Fact(_time, driver.Id, 9, amount);
            StartTrip(driver, shop.Node);
            return true;
        }
    }

    private void StartTrip(Person p, int destination)
    {
        if (p.Traveling) throw new InvalidOperationException("Double person trip");
        p.Traveling = true;
        var t = NewTrip(p.Id, p.Node, destination, EventKind.Move);
        p.TripId = t.Id;
        Schedule(_time + 1, EventKind.Move, t.Id);
    }
    private int Edge(int from, int to)
    {
        if (to == from + 1 && from / Grid == to / Grid) return from * 4;
        if (to == from - 1 && from / Grid == to / Grid) return from * 4 + 1;
        if (to == from + Grid) return from * 4 + 2;
        if (to == from - Grid) return from * 4 + 3;
        throw new InvalidOperationException($"Non-adjacent nodes: {from}, {to}");
    }
    private int NextNode(int start, int target)
    {
        if (start == target) return target;
        // BFS is only needed for closures; default route is Manhattan via x
        // then y. Route is recalculated on every link to respond to closures.
        var sx = start % Grid; var tx = target % Grid;
        if (sx != tx)
        {
            var next = start + Math.Sign(tx - sx);
            if (!_closed[Edge(start, next)]) return next;
        }
        var sy = start / Grid; var ty = target / Grid;
        if (sy != ty)
        {
            var next = start + Grid * Math.Sign(ty - sy);
            if (!_closed[Edge(start, next)]) return next;
        }
        Span<int> queue = stackalloc int[Nodes];
        Span<int> parents = stackalloc int[Nodes];
        parents.Fill(-1);
        var head = 0; var tail = 0;
        queue[tail++] = start;
        parents[start] = start;
        while (head < tail)
        {
            var n = queue[head++];
            foreach (var direction in new[] { 1, -1, Grid, -Grid })
            {
                var m = n + direction;
                if (m < 0 || m >= Nodes || Math.Abs(m % Grid - n % Grid) +
                    Math.Abs(m / Grid - n / Grid) != 1 || parents[m] >= 0) continue;
                if (_closed[Edge(n, m)]) continue;
                parents[m] = n;
                if (m == target)
                {
                    var first = m;
                    while (parents[first] != start) first = parents[first];
                    _replans++;
                    return first;
                }
                queue[tail++] = m;
            }
        }
        return start; // blocked route: must wait, never teleport.
    }
    private void Move(int tripId)
    {
        var trip = _trips[tripId]; if (trip.Completed) return;
        if (trip.ExitAt != 0)
        {
            if (_time != trip.ExitAt) throw new InvalidOperationException("Out-of-order lane completion");
            if (trip.OwnerId >= 0) BurnTruckFuelForSegment(_people[trip.OwnerId]);
            trip.CurrentNode = trip.To;
            trip.ExitAt = 0;
            if (trip.OwnerId >= 0) _people[trip.OwnerId].Node = trip.To;
            _segments++;
            _history.Fact(_time, trip.OwnerId, 1, trip.CurrentNode);
        }
        if (trip.CurrentNode == trip.Destination)
        {
            trip.Completed = true;
            _journeys++;
            _history.Fact(_time, trip.OwnerId, 2, trip.Destination);
            if (trip.OwnerId < 0)
                CompleteDelivery(trip.Id);
            else
                ArrivePerson(_people[trip.OwnerId]);
            _freeTripSlots.Push(trip.Id);
            return;
        }
        var next = NextNode(trip.CurrentNode, trip.Destination);
        if (next == trip.CurrentNode)
        {
            // A road closure is an actual barrier. Retry later, do not invent
            // travel or an alternate edge that does not physically exist.
            Schedule(_time + 30, EventKind.Move, trip.Id);
            _waitSeconds += 30;
            return;
        }
        var edge = Edge(trip.CurrentNode, next);
        var entry = Math.Max(_time, _laneNextEntry[edge]);
        // Reserve receiving junction and the lane *before* departure. Avoid
        // crossing reservations by delaying departure, not teleporting.
        entry = Math.Max(entry, _junctionAvailable[next] - TravelSeconds);
        var arrival = checked(entry + TravelSeconds);
        _laneNextEntry[edge] = entry + Headway;
        _laneLastEntry[edge] = entry;
        _junctionAvailable[next] = arrival + CrossingSeconds;
        _waitSeconds += entry - _time;
        trip.From = trip.CurrentNode; trip.To = next;
        trip.EnterAt = entry; trip.ExitAt = arrival;
        Schedule(arrival, EventKind.Move, trip.Id);
    }
    private void ArrivePerson(Person p)
    {
        p.Traveling = false;
        p.TripId = -1;
        if (_config.MunicipalSanitationPilot && p.SanitationCargo > 0 && p.Node == SanitationDepotNode)
        {
            _garbageAtDepot += p.SanitationCargo;
            _garbageInTransit -= p.SanitationCargo;
            _history.Fact(_time, p.Id, 27, p.SanitationCargo);
            p.SanitationCargo = 0;
            if (p.Node != p.Home) { StartTrip(p, p.Home); return; }
        }
        var workplace = p.Job < 0 ? -1 :
            (_config.MunicipalWasteTreatmentPilot && p.Job == WasteTreatmentJob) ? SanitationDepotNode :
            (_config.IntegratedV1Stress && p.Job >= IntakeJob && p.Job <= FuelStationJob) ? IntegratedJobNode(p.Job) :
            (_config.MunicipalSanitationPilot && p.Job == SanitationJob) ?
                (p.SanitationHouseholdId >= 0 ? _households[p.SanitationHouseholdId].Home : -1) :
            p.Job == BuilderJob ?
                (p.ConstructionSiteId >= 0 ? _households[p.ConstructionSiteId].Home : -1) :
            p.Job < StoreCount ? _shops[p.Job].Node : _farms[p.Job - StoreCount].Node;
        if (_config.VacancyMarket && p.HeadingToWork && p.Job < 0)
        {
            p.HeadingToWork = false;
            StartTrip(p, p.Home); // cancelled shift; not a grocery purchase.
            return;
        }
        if (_config.FreightLabor && p.FreightShift &&
            p.FreightShopId < 0 && p.Node == workplace)
        {
            if (_config.IntegratedV1Stress && p.Job >= StoreCount && p.Job < StoreCount + FarmCount)
            {
                // The paid trucking leg ENDS at the farm; the driver walks
                // home separately and must not burn company diesel on foot.
                _farms[p.Job - StoreCount].TruckBusy = false;
                p.FreightShift = false;
                DispatchNextFreightFromFarm(p.Job - StoreCount);
            }
            // Return from physical delivery, without a second paid shift.
            // A person can LIVE on the same grid node as the farm: do not
            // spawn a self-trip that repeats infinitely before going home.
            if (p.Node != p.Home)
            {
                StartTrip(p, p.Home);
                return;
            }
            p.FreightShift = false;
        }
        var earliest = _config.VacancyMarket && p.Job < StoreCount
            ? (p.Id % 2 == 0 ? 11 : 14) * 3600 : 8 * 3600;
        var latest = _config.VacancyMarket && p.Job < StoreCount
            ? (p.Id % 2 == 0 ? 21 : 23) * 3600 : 17 * 3600;
        if (_config.FreightLabor && p.FreightShopId >= 0 &&
            p.Node == _shops[p.FreightShopId].Node)
        {
            // A local supplier delivers ONLY with a real person, real cargo
            // and an actual road trip. Stock appears on arrival, never at order.
            _shops[p.FreightShopId].Stock += p.FreightCargo;
            _history.Fact(_time, p.Id, 11, p.FreightCargo);
            p.FreightShopId = -1;
            p.FreightCargo = 0;
            _shipments++;
            _freightLaborTrips++;
            StartTrip(p, workplace);
            return;
        }
        if (p.Node == workplace && _time % DaySeconds < latest &&
            _time % DaySeconds >= earliest &&
            (!_config.VacancyMarket || p.HeadingToWork || p.FreightShift))
        {
            p.HeadingToWork = false;
            p.AtWork = true;
            if (_config.VacancyMarket)
            {
                if (_config.IntegratedV1Stress && p.Job >= IntakeJob && p.Job <= FuelStationJob)
                {
                    if (p.Job == FuelStationJob) _fuelStation.PresentWorkers++;
                    return;
                }
                if (_config.MunicipalWasteTreatmentPilot && p.Job == WasteTreatmentJob)
                    return; // Physical arrival at treatment depot.
                if (_config.MunicipalSanitationPilot && p.Job == SanitationJob)
                    return; // Physical arrival, no fictional private firm.
                var employer = p.Job == BuilderJob ? _builder :
                    p.Job < StoreCount ? _shops[p.Job] : _farms[p.Job - StoreCount];
                if (_config.BoundedRetailService && p.Job < StoreCount)
                {
                    // A cashier who cannot be paid must not grant the shop
                    // fictitious service throughput before their shift ends.
                    if (employer.Cash - employer.ReservedCheckoutWagesCents < 65)
                    {
                        p.AtWork = false;
                        StartTrip(p, p.Home);
                        return;
                    }
                    employer.ReservedCheckoutWagesCents += 65;
                    employer.TodayCheckoutUnitsLeft += _config.BasketRetailService ? _config.RetailBasketsPerShift : 5;
                }
                employer.PresentWorkers++;
            }
            if (_config.FreightLabor && p.Job >= StoreCount && p.IsFreightDriver &&
                !p.FreightShift && TryAssignFreight(p))
            {
                // One contracted shift performs one delivery instead of
                // harvesting. No free driver, no free food and no teleport.
                p.FreightShift = true;
                p.AtWork = false;
                _farms[p.Job - StoreCount].PresentWorkers--;
            }
            return;
        }
        if (_config.VacancyMarket && p.HeadingToWork)
        {
            // An employee delayed beyond the real shift cannot be treated as
            // a customer merely because their worksite shares the store node.
            p.HeadingToWork = false;
            StartTrip(p, p.Home);
            return;
        }
        p.HeadingToWork = false;
        if (p.Node == p.Home)
        {
            if (_config.FreightLabor) p.FreightShift = false;
            var h = _households[p.HouseholdId];
            h.Pantry = checked(h.Pantry + p.CarryFood);
            if (p.CarryFood > 0) _history.Fact(_time, p.Id, 4, p.CarryFood);
            p.CarryFood = 0;
            return;
        }
        // Shop visit is physical; purchase only at actual arrival.
        var shop = _shops[p.Node];
        // The earlier laboratory offered food without needing a cashier.
        // In the market pilot, no real employee on site means no retail sale.
        var price = _config.CostBasedFoodPrice ? shop.RetailUnitPriceCents : FoodPrice;
        var buy = _config.VacancyMarket && shop.PresentWorkers <= 0
            ? 0 : Math.Min(32, Math.Min(shop.Stock, (int)(p.Cash / price)));
        if (_config.BoundedRetailService)
        {
            if (_config.BasketRetailService)
            {
                // Each completed in-person basket uses one staffed checkout
                // slot, regardless of how many real food units it contains.
                if (shop.TodayCheckoutUnitsLeft <= 0)
                {
                    if (shop.PresentWorkers > 0)
                        shop.LifetimeCheckoutCapacityDenied++;
                    buy = 0;
                }
            }
            else buy = Math.Min(buy, shop.TodayCheckoutUnitsLeft);
        }
        if (buy > 0)
        {
            var paid = checked((long)buy * price);
            p.Cash -= paid;
            shop.Cash += paid;
            if (_config.BoundedRetailService)
                shop.TodayCheckoutUnitsLeft -= _config.BasketRetailService ? 1 : buy;
            if (_config.BasketRetailService)
            {
                shop.MonthRetailBaskets++;
                shop.LifetimeRetailBaskets++;
            }
            if (_config.RetailShopFallback && p.RetailFallbackAttempts > 0)
                p.RetailAlternativePurchases++;
            _retailSalesCents = checked(_retailSalesCents + paid);
            if (_config.CostBasedFoodPrice) shop.MonthRetailSalesUnits += buy;
            if (_config.WageFeedback || _config.DemandStaffing || _config.VacancyMarket) shop.DayReceipts += paid;
            shop.Stock -= buy;
            p.CarryFood += buy;
            _sales += buy;
            _history.Fact(_time, p.Id, 10, buy);
        }
        else
        {
            _unsatisfiedSales++;
            if (_config.RetailShopFallback && TryAlternativeRetailShop(p))
                return;
            if (_config.RetailShopFallback && p.RetailFallbackAttempts > 0)
                p.RetailAlternativeFailures++;
        }
        if (_config.RetailShopFallback)
        {
            p.RetailVisitedStoresMask = 0;
            p.RetailFallbackAttempts = 0;
        }
        StartTrip(p, p.Home);
    }
    // Approved V1 principle: search another accessible real seller after
    // failure. Experimental radius/three extra visits are NOT product values.
    private bool TryAlternativeRetailShop(Person buyer)
    {
        buyer.RetailVisitedStoresMask |= 1UL << buyer.Node;
        if (buyer.RetailFallbackAttempts >= 3) return false;
        var best = -1;
        var bestScore = int.MaxValue;
        for (var id = 0; id < StoreCount; id++)
        {
            if ((buyer.RetailVisitedStoresMask & (1UL << id)) != 0 ||
                id == buyer.Home) continue;
            var distance = Math.Abs((id % Grid) - (buyer.Node % Grid)) +
                           Math.Abs((id / Grid) - (buyer.Node / Grid));
            // Neighbouring existing roads, rather than global free delivery.
            if (distance > 3 || distance == 0) continue;
            var shop = _shops[id];
            if (shop.Stock <= 0 || shop.PresentWorkers <= 0 ||
                shop.TodayCheckoutUnitsLeft <= 0 ||
                buyer.Cash < shop.RetailUnitPriceCents) continue;
            var score = distance * 100 + shop.RetailUnitPriceCents;
            if (score >= bestScore) continue;
            best = id;
            bestScore = score;
        }
        if (best < 0) return false;
        buyer.RetailFallbackAttempts++;
        buyer.RetailAlternativeTrips++;
        StartTrip(buyer, _shops[best].Node);
        return true;
    }
    private void CompleteDelivery(int tripId)
    {
        var t = _trips[tripId];
        if (!t.Completed || t.OwnerId != -1) throw new InvalidOperationException("Fictitious shipment completion");
        if (_config.IntegratedV1Stress && t.Payload == FuelImportPayload)
        {
            FinishFuelImport(t);
            return;
        }
        if (_config.IntegratedV1Stress && t.Payload <= FuelFarmPayloadBase &&
            t.Payload > FuelFarmPayloadBase - FarmCount)
        {
            FinishFarmFuel(t);
            return;
        }
        if (_config.ConstructionPilot && t.Payload >= 10_000)
        {
            CompleteConstructionDelivery(t);
            return;
        }
        _shops[t.Payload].Stock = checked(_shops[t.Payload].Stock + t.Cargo);
        _shipments++;
        _history.Fact(_time, -2000 - t.Payload, 11, t.Cargo);
    }
    private void CloseRoad()
    {
        // An adversarial test: only future entries are blocked. An already
        // traveling vehicle must finish the occupied physical segment.
        _closed[Edge(1, 2)] = true;
        _closed[Edge(2, 1)] = true;
        _history.Fact(_time, -3, 12, 0);
    }
    private void OpenRoad()
    {
        _closed[Edge(1, 2)] = false;
        _closed[Edge(2, 1)] = false;
        _history.Fact(_time, -3, 13, 0);
    }
    public (double X, double Z) PositionAt(int personId, long second)
    {
        var p = _people[personId];
        if (p.TripId >= 0)
        {
            var t = _trips[p.TripId];
            if (!t.Completed && t.ExitAt > t.EnterAt && second >= t.EnterAt && second <= t.ExitAt)
            {
                var a = Math.Clamp((double)(second - t.EnterAt) / (t.ExitAt - t.EnterAt), 0, 1);
                return (((t.From % Grid) * (1 - a) + (t.To % Grid) * a) * 100,
                    ((t.From / Grid) * (1 - a) + (t.To / Grid) * a) * 100);
            }
        }
        return ((p.Node % Grid) * 100, (p.Node / Grid) * 100);
    }
    public void AssertConservation()
    {
        if (CalculateMoney() != _initialMoney)
            throw new InvalidOperationException("Money created/destroyed. First mismatch: " + _firstDollarViolation);
        // Here food is the sole paid consumer good. Wholesale transfers stay
        // inside the business sector; every paid salary leaves it. This
        // independent ledger exposes structural insolvency that total-money
        // conservation by itself would miss.
        var businessesCash = _shops.Sum(f => f.Cash) + _farms.Sum(f => f.Cash) + _builder.Cash +
            (_config.IntegratedV1Stress ? _fuelStation.Cash : 0);
        // Private household building invoices are real receipts for the
        // builder; timber is separately bought from the finite external
        // reserve and is not magically credited to local businesses.
        var contractorRevenue = checked(_constructionStarted * ContractorLaborInvoice);
        if (businessesCash != checked(InitialBusinessCashCents +
            (_config.IntegratedV1Stress ? FuelStationStartingCash : 0) + _retailSalesCents +
            contractorRevenue - (_paidWages - _municipalWagesPaidCents) -
            _municipalTaxPaidCents - _fuelImportSpending))
            throw new InvalidOperationException("Business ledger differs from food + construction sales minus actual wages");
        if (CalculateFood() != checked(_initialFood + _producedFood - _eatenFood))
            throw new InvalidOperationException("Food created/destroyed outside farm work and meals");
        if (_days > _config.Days || _active < 0) throw new InvalidOperationException("Invalid days/population");
        if (_people.Count(p => p.Alive) != _active) throw new InvalidOperationException("Population count inconsistent");
        foreach (var h in _households) if (h.Pantry < 0) throw new InvalidOperationException("Negative pantry");
        foreach (var store in _shops)
            if (store.Stock < 0 || store.Cash < 0 ||
                (_config.BoundedRetailService &&
                  (store.TodayCheckoutUnitsLeft < 0 || store.ReservedCheckoutWagesCents < 0 ||
                    store.ReservedCheckoutWagesCents > store.Cash)))
                throw new InvalidOperationException("Invalid physical shop or unpaid reserved cashier");
        foreach (var farm in _farms) if (farm.Stock < 0 || farm.Cash < 0) throw new InvalidOperationException("Invalid farm");
        foreach (var p in _people) if (p.Cash < 0 || p.CarryFood < 0 || p.FreightCargo < 0)
            throw new InvalidOperationException("Invalid person or freight cargo");
        AssertConstructionConservation();
        AssertIntegratedConservation();
        if (_config.MonthlyResidentialTaxCents > 0 &&
            _residentialTaxAssessedCents != _residentialTaxPaidCents + _households.Sum(h=>h.ResidentialPropertyTaxDebtCents))
            throw new InvalidOperationException("Residential property taxes were counted as revenue before payment");
        if (_config.MunicipalSanitationPilot &&
            (_garbageGenerated != _garbageAtDepot + _wasteTreated + _garbageInTransit + _households.Sum(h => h.TrashUnits) ||
             _garbageCollected != _garbageAtDepot + _wasteTreated + _garbageInTransit ||
             _garbageInTransit != _people.Sum(p => p.SanitationCargo) || _treasury < 0))
            throw new InvalidOperationException("Municipal waste or city cash violated causal accounting");
        if (_config.FreightLabor && _shipments != _freightLaborTrips)
            throw new InvalidOperationException("A delivery was recorded without a real freight worker");
        if (_config.VacancyMarket)
        {
            for (var firmId = 0;
                firmId < StoreCount + FarmCount + (_config.ConstructionPilot ? 1 : 0); firmId++)
            {
                var offer = firmId == BuilderJob ? _builder :
                    firmId < StoreCount ? _shops[firmId] : _farms[firmId - StoreCount];
                if (_people.Count(p => p.Alive && p.Job == firmId) > offer.OfferedPositions)
                    throw new InvalidOperationException("More contracted people than market vacancies");
                if (offer.PresentWorkers != _people.Count(p => p.Alive && p.Job == firmId && p.AtWork))
                    throw new InvalidOperationException("Ghost or missing physically present employees");
            }
        }
    }
    public string Fingerprint()
    {
        // Deterministic FNV-1a-style state fingerprint; independent of .NET
        // HashCode process salts and excludes timing/perf counters.
        ulong hash = 14695981039346656037UL;
        void Mix(long x) { hash ^= unchecked((ulong)x); hash *= 1099511628211UL; }
        Mix(_time); Mix(_active); Mix(_births); Mix(_deaths); Mix(_treasury); Mix(_reserveGlobal);
        Mix(_journeys); Mix(_segments); Mix(_sales); Mix(_shipments); Mix(_retailSalesCents);
        Mix(_municipalTaxPaidCents); Mix(_municipalWagesPaidCents);
        Mix(_residentialTaxPaidCents); Mix(_residentialTaxAssessedCents);
        Mix(_rawWater); Mix(_potableWater); Mix(_sewage); Mix(_energy);
        Mix(_waterExtracted); Mix(_waterConsumed); Mix(_sewageProcessed); Mix(_powerGenerated);
        Mix(_fuelImported); Mix(_fuelBurned); Mix(_fuelInTransit); Mix(_fuelStation.Cash); Mix(_fuelStation.Stock);
        Mix(_fuelImportPending ? 1 : 0); Mix(_deedPaid);
        Mix(_powerCurtailed); Mix(_lastPowerHour); Mix(_lastWindMaintainedDay);
        Mix(_garbageGenerated); Mix(_garbageCollected); Mix(_garbageAtDepot); Mix(_sanitationPaidVisits); Mix(_wasteTreated); Mix(_wasteTreatmentPaidShifts); Mix(_fairSanitationCursor); Mix(_garbageInTransit);
        foreach (var p in _people)
        {
            Mix(p.Id); Mix(p.Node); Mix(p.Job); Mix(p.Cash); Mix(p.Alive ? 1 : 0); Mix(p.HeadingToWork ? 1 : 0);
            Mix(p.Health); Mix(p.CarryFood); Mix(p.Traveling ? 1 : 0);
            Mix(p.FreightShopId); Mix(p.FreightCargo); Mix(p.FreightShift ? 1 : 0);
            Mix(p.IsFreightDriver ? 1 : 0);
            Mix(p.ConstructionSiteId); Mix(p.SanitationHouseholdId); Mix(p.SanitationCargo);
            Mix(unchecked((long)p.RetailVisitedStoresMask));
            Mix(p.RetailFallbackAttempts); Mix(p.RetailAlternativeTrips);
            Mix(p.RetailAlternativePurchases); Mix(p.RetailAlternativeFailures);
            Mix(p.ContributedDays); Mix(p.GrossEarnings); Mix(p.PensionCredit); Mix(p.PensionReceived);
        }
        foreach (var h in _households) { Mix(h.Pantry); Mix(h.Members.Count); Mix(h.LastBirthDay); Mix(h.LastShoppingDepartureDay); Mix(h.TrashUnits); Mix(h.LastSanitationBookedDay); Mix(h.LastSanitationServicedDay); Mix(h.PropertyOwnerPersonId); Mix(h.ResidentialPropertyTaxDebtCents); }
        foreach (var f in _shops) { Mix(f.Cash); Mix(f.Stock); Mix(f.DayReceipts); Mix(f.DaySupplies); Mix(f.DayPaidShifts); Mix(f.WageForDay); Mix(f.PayrollBudget); Mix(f.OfferedPositions); Mix(f.PresentWorkers); Mix(f.MonthFoodProduced); Mix(f.MonthFarmWorkdays); Mix(f.MonthRetailSalesUnits); Mix(f.MonthRetailPaidWagesCents); Mix(f.RetailUnitPriceCents); Mix(f.MonthFarmPaidWagesCents); Mix(f.MonthFreightFuelConsumedCents); Mix(f.MonthWholesaleSalesUnits); Mix(f.MonthWholesaleBoughtUnits); Mix(f.MonthWholesalePaidCents); Mix(f.FarmWholesaleUnitPriceCents); Mix(f.TruckFuelUnits); Mix(f.TruckBusy ? 1 : 0); Mix(f.TruckFuelOnOrder ? 1 : 0); Mix(f.TruckFuelDemandPending ? 1 : 0); Mix(f.TodayCheckoutUnitsLeft); Mix(f.ReservedCheckoutWagesCents); Mix(f.MonthRetailBaskets); Mix(f.LifetimeRetailBaskets); Mix(f.LifetimeCheckoutCapacityDenied); Mix(f.MonthPropertyTaxActuallyPaidCents); }
        foreach (var f in _farms) { Mix(f.TruckFuelUnits); Mix(f.TruckBusy ? 1 : 0); Mix(f.TruckFuelOnOrder ? 1 : 0); Mix(f.TruckFuelDemandPending ? 1 : 0); Mix(f.Cash); Mix(f.Stock); Mix(f.DayReceipts); Mix(f.DaySupplies); Mix(f.DayPaidShifts); Mix(f.WageForDay); Mix(f.PayrollBudget); Mix(f.OfferedPositions); Mix(f.PresentWorkers); Mix(f.MonthFoodProduced); Mix(f.MonthFarmWorkdays); Mix(f.MonthFreightFuelConsumedCents); }
        Mix(_builder.Cash); Mix(_builder.OfferedPositions); Mix(_builder.PresentWorkers);
        Mix(_outsideTimber); Mix(_constructionStarted); Mix(_constructionFinished); Mix(_constructionPaidShifts);
        foreach (var site in _construction)
        {
            Mix(site.Ordered ? 1 : 0); Mix(site.Finished ? 1 : 0);
            Mix(site.TimberOnSite); Mix(site.TimberBuiltIntoHouse);
            Mix(site.PaidShifts); Mix(site.LastCrewDay);
        }
        Mix(_producedFood); Mix(_eatenFood);
        foreach (var x in _closed) Mix(x ? 1 : 0);
        foreach (var t in _trips) if (!t.Completed)
        {
            Mix(t.OwnerId); Mix(t.Destination); Mix(t.CurrentNode);
            Mix(t.EnterAt); Mix(t.ExitAt);
        }
        return hash.ToString("X16", CultureInfo.InvariantCulture);
    }
    public long Journeys => _journeys;
    public long Sales => _sales;
    public long Births => _births;
    public int PeakActive => _maxActive;
    public int MinimumActive => _minActive;
    public long RecordedFacts => _history.Entries;
    public long ReserveGlobalCents => _reserveGlobal;

    public object Report(double wallSeconds) => new
    {
        status = "PARTIAL_ARCHITECTURAL_FIXTURE_NOT_FULL_INDEXCITIES",
        stateFingerprint = Fingerprint(),
        warning = "Not SPEC-complete: simplified lanes/intersections, no pedestrian crosswalks, real towing, building construction, complex health or full demography. NEVER claim full 10k/80y achieved.",
        seed = _config.Seed,
        wageFeedbackFixture = _config.WageFeedback,
        cohortRenewalFixture = _config.CohortRenewal,
        birthEligibilityFallbackFixture = _config.BirthEligibleFallback,
        birthFallbackCandidatesChecked = _birthFallbackCandidatesChecked,
        birthFallbackBirths = _birthFallbackBirths,
        birthFallbackNoEligibleDays = _birthFallbackDaysWithoutEligible,
        demandStaffingFixture = _config.DemandStaffing,
        vacancyMarketFixture = _config.VacancyMarket,
        freightLaborFixture = _config.FreightLabor,
        constructionPilotFixture = _config.ConstructionPilot,
        fairHouseholdHiringFixture = _config.FairHouseholdHiring,
        importedBuildingTimberOutside = _outsideTimber,
        builderFirmCashCents = _builder.Cash,
        constructionContractsStarted = _constructionStarted,
        constructionContractsCompleted = _constructionFinished,
        constructionPaidShifts = _constructionPaidShifts,
        materialConserved = CalculateConstructionTimber() == _initialOutsideTimber,
        realWorkerFreightTrips = _freightLaborTrips,
        freightChargesPaidCents = _paidFreightCents,
        integratedV1StressFixture = _config.IntegratedV1Stress,
        physicallyExtractedWater = _waterExtracted,
        treatedPotableWater = _waterPurified,
        consumedPotableWater = _waterConsumed,
        deniedPotableWater = _waterShortage,
        untreatedSewageStock = _sewage,
        sewagePhysicallyProcessed = _sewageProcessed,
        generatedWindElectricity = _powerGenerated,
        windEnergyCurtailedWithoutBattery = _powerCurtailed,
        currentHourUnspentPower = _energy,
        usedElectricity = _powerConsumed,
        unservedElectricity = _powerShortage,
        actualUtilityWorkShifts = _utilityShiftsPaid,
        actualFuelImported = _fuelImported,
        fuelConsumedByRealFreight = _fuelBurned,
        fuelPhysicallyDeliveredToFarms = _fuelDelivered,
        fuelImportPaidCents = _fuelImportSpending,
        residentialDeedPurchasePaymentsCents = _deedPaid,
        fuelStationInventory = _fuelStation.Stock,
        farmEmployeesAssignedToFreight = _people.Count(p => p.Alive && p.IsFreightDriver),
        employedAlive = _people.Count(p => p.Alive && p.Job >= 0),
        unemployedAdults = _people.Count(p => p.Alive && p.Job < 0 &&
            Age(p, Epoch.AddSeconds(_time)) is >= 18 and < 70),
        vacanciesOffered = _shops.Sum(f => f.OfferedPositions) + _farms.Sum(f => f.OfferedPositions) + _builder.OfferedPositions + (_config.MunicipalSanitationPilot ? Math.Min(512, (int)(_treasury / (80L * 30))) : 0),
        occupiedHouseholds = _households.Count(h => h.Members.Any(id => _people[id].Alive)),
        livingOccupancyBirthCapFixture = _config.LivingOccupancyBirthCap,
        householdsHistoricallyFullButCurrentlyVacant = _households.Count(h => h.Members.Count >= 16 && h.Members.Count(id => _people[id].Alive) < 16),
        householdsWithEmployedAdults = _households.Count(h => h.Members.Any(id =>
            _people[id].Alive && _people[id].Job >= 0 &&
            Age(_people[id], Epoch.AddSeconds(_time)) is >= 18 and < 70)),
        requestedSims = _config.Sims,
        peopleEverCreated = _people.Count,
        aliveEnd = _active,
        peakAlive = _maxActive,
        minimumAliveAtDayBoundary = _minActive, // compatibility: now also observes deaths between boundaries
        minimumAliveAtAnyEvent = _minActive,
        simulatedDays = _days,
        gregorianEnd = Epoch.AddDays(_config.Days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        wallSeconds,
        wallSecondsPerDay = wallSeconds / _days,
        speedBudgetOnly_notPassingCriterion = (wallSeconds / _days) * 29220 <= 21600,
        // A 30-day or one-year run may sustain 10k within its observed window
        // but must NEVER look like a completed 80-year history validation.
        tenKAliveDuringObservedWindow = _minActive >= 10_000 && _config.Sims >= 10_000,
        demonstrated10kSustained = _days == 29_220 && _minActive >= 10_000 &&
            _config.Sims >= 10_000 && _config.History is not null,
        fullIndexCitiesTargetValidated = false, // this laboratory omits approved V1 systems

        journeys = _journeys, roadSegments = _segments, roadWaitSeconds = _waitSeconds,
        salesFoodUnits = _sales, cargoDeliveries = _shipments, paidWagesCents = _paidWages,
        businessSalesRevenueCents = _retailSalesCents,
        averageRetailUnitPriceCents = _shops.Average(f => f.RetailUnitPriceCents),
        minimumRetailUnitPriceCents = _shops.Min(f => f.RetailUnitPriceCents),
        maximumRetailUnitPriceCents = _shops.Max(f => f.RetailUnitPriceCents),
        costBasedFoodPriceFixture = _config.CostBasedFoodPrice,
        capacityPricedFoodFixture = _config.CapacityPricedFood,
        attendanceAwareFarmsFixture = _config.AttendanceAwareFarms,
        crossFarmFreightFixture = _config.CrossFarmFreight,
        boundedRetailServiceFixture = _config.BoundedRetailService,
        basketRetailServiceFixture = _config.BasketRetailService,
        retailShopFallbackFixture = _config.RetailShopFallback,
        retailAlternativeTrips = _people.Sum(p => p.RetailAlternativeTrips),
        retailAlternativePurchases = _people.Sum(p => p.RetailAlternativePurchases),
        retailAlternativeFailures = _people.Sum(p => p.RetailAlternativeFailures),
        municipalSanitationPilotFixture = _config.MunicipalSanitationPilot,
        taxInclusiveProductPriceFixture = _config.TaxInclusiveProductPrice,
        taxCostRecoveryPercent = _config.TaxInclusiveProductPrice ? _config.TaxCostRecoveryPercent : 0,
        monthlyResidentialPropertyTaxCents = _config.MonthlyResidentialTaxCents,
        residentialTaxAssessedCents = _residentialTaxAssessedCents,
        residentialPropertyTaxActuallyPaidCents = _residentialTaxPaidCents,
        residentialPropertyTaxDebtOutstandingCents = _households.Sum(h=>h.ResidentialPropertyTaxDebtCents),
        privateOwnedResidenceCount = _households.Count(h=>h.PropertyOwnerPersonId >= 0),
        municipalWasteTreatmentPilotFixture = _config.MunicipalWasteTreatmentPilot,
        fairSanitationDispatchFixture = _config.FairSanitationDispatch,
        attendanceAwareMunicipalShiftsFixture = _config.AttendanceAwareMunicipalShifts,
        homesPastSanitationHygieneThreshold = _households.Count(h => h.TrashUnits > 80 && h.Members.Any(id => _people[id].Alive)),
        highestUntreatedHomeWaste = _households.Max(h => h.TrashUnits),
        homesWithoutPickupFor15Days = _households.Count(h => h.TrashUnits >= 4 &&
            h.Members.Any(id => _people[id].Alive) &&
            _days - h.LastSanitationServicedDay > 15),
        wasteTreatedAtDepot = _wasteTreated,
        wasteTreatmentUnitsPerPaidShift = _config.WasteTreatmentUnitsPerShift,
        treatmentBacklogClearanceMonths = _config.TreatmentBacklogMonths,
        paidWasteTreatmentShifts = _wasteTreatmentPaidShifts,
        municipalWasteTreatmentJobs = _people.Count(p => p.Alive && p.Job == WasteTreatmentJob),
        municipalCollectionIntervalDays = _config.MunicipalCollectionIntervalDays,
        municipalTaxCollectedCents = _municipalTaxPaidCents,
        municipalSanitationWagesPaidCents = _municipalWagesPaidCents,
        municipalPaidCollections = _sanitationPaidVisits,
        totalWasteCreated = _garbageGenerated,
        totalWasteCollected = _garbageCollected,
        wasteStoredAtRealDepot = _garbageAtDepot,
        wasteInTransitByRealWorkers = _garbageInTransit,
        householdsWithUncollectedWaste = _households.Count(h => h.TrashUnits > 0),
        municipalSanitationJobs = _people.Count(p => p.Alive && p.Job == SanitationJob),
        checkoutBasketTransactions = _shops.Sum(f => f.LifetimeRetailBaskets),
        checkoutCapacityDeniedVisits = _shops.Sum(f => f.LifetimeCheckoutCapacityDenied),
        checkoutFixtureMaxEmployeesPerSmallStore = _config.BasketRetailService ? 8 : 0,
        basketCheckoutsPerPaidWorkerShift = _config.BasketRetailService ? _config.RetailBasketsPerShift : 0,
        farmCostPricingFixture = _config.FarmCostPricing,
        meanFarmWholesaleUnitPriceCents = _farms.Average(f=>f.FarmWholesaleUnitPriceCents),
        contractorServiceRevenueCents = checked(_constructionStarted * ContractorLaborInvoice),
        businessOperatingNetCents = checked(_retailSalesCents +
            _constructionStarted * ContractorLaborInvoice - (_paidWages - _municipalWagesPaidCents) - _municipalTaxPaidCents - _fuelImportSpending),
        businessInitialCashCents = InitialBusinessCashCents + (_config.IntegratedV1Stress ? FuelStationStartingCash : 0),
        businessCurrentCashCents = _shops.Sum(f => f.Cash) + _farms.Sum(f => f.Cash) + _builder.Cash + (_config.IntegratedV1Stress ? _fuelStation.Cash : 0),
        unspentRealPayrollBudgetCents = _shops.Sum(f => f.PayrollBudget) + _farms.Sum(f => f.PayrollBudget),
        personalCashCents = _people.Sum(p => p.Cash),
        shopCashCents = _shops.Sum(s => s.Cash),
        farmCashCents = _farms.Sum(f => f.Cash),
        treasuryCashCents = _treasury,
        reserveGlobalCashCents = _reserveGlobal,
        unpaidPensionCreditsCents = _people.Sum(p => p.PensionCredit),
        historicalPensionsPaidCents = _people.Sum(p => p.PensionReceived),
        contributors = _people.Count(p => p.ContributedDays > 0),
        adultCitizensWithZeroCash = _people.Count(p => p.Alive && p.Cash == 0 && Age(p, Epoch.AddSeconds(_time)) >= 18),
        householdsWithNoPantry = _households.Count(h => h.Pantry == 0),
        shopFoodAvailable = _shops.Sum(s => (long)s.Stock),
        farmFoodAvailable = _farms.Sum(f => (long)f.Stock),
        shopsWithFoodAtEnd = _shops.Count(s => s.Stock > 0),
        farmsWithFoodAtEnd = _farms.Count(f => f.Stock > 0),
        occupiedHouseholdsWithNoFood = _households.Count(h => h.Pantry == 0 &&
            h.Members.Any(id => _people[id].Alive)),
        producedFoodUnits = _producedFood, eatenFoodUnits = _eatenFood,
        foodConserved = CalculateFood() == _initialFood + _producedFood - _eatenFood,
        workingDays = _workdays, births = _births, deaths = _deaths,
        unmetMeals = _unservedMeals, deniedPurchases = _unsatisfiedSales,
        routeReroutes = _replans, recordedFacts = _history.Entries, peakQueue = _peakEvents,
        pendingQueue = _events.Count,
        allocatedTripSlots = _trips.Count,
        reusableTripSlots = _freeTripSlots.Count,
        moneyConserved = CalculateMoney() == _initialMoney,
        initialMoneyCents = _initialMoney,
        currentMoneyCents = CalculateMoney(),
        historyPersistent = _config.History is not null
    };
    public void CompleteHistory() => _history.Dispose();
    public void Dispose() => _history.Dispose();
}
