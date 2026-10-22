using System.Diagnostics;
using System.IO.Compression;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace IndexCities.CausalEngine;

// Independent vertical-slice engine for falsifying the architecture.
// RULES AND FIXTURES HERE ARE NOT AN IMPLEMENTATION OF ALL OF INDEXCITIES SPEC.
internal readonly record struct LabConfig(int Sims, int Days, int Seed, string? History, bool Adversarial, bool WageFeedback = false, bool CohortRenewal = false, bool DemandStaffing = false, bool VacancyMarket = false);
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
}
internal sealed class Household
{
    public required int Id;
    public required int Home;
    public required List<int> Members;
    public int Pantry = 40;
    public int LastBirthDay = -10_000;
    public long LastShoppingDepartureDay = -1;
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
    private long _producedFood, _eatenFood, _initialFood;
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
        long total = checked(_treasury + _reserveGlobal);
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
        foreach (var p in _people) total += p.CarryFood;
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
                var household = _households[(int)(((long)day * 151 + _config.Seed) % _households.Length)];
                TryBirth(household, date, day);
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
        // Food and jobs are not simulated by a population aggregate: each
        // household's real stock and each person's physical state matter.
        foreach (var h in _households)
        {
            foreach (var id in h.Members)
            {
                var p = _people[id]; if (!p.Alive) continue;
                if (h.Pantry > 0)
                {
                    h.Pantry--;
                    _eatenFood++;
                    p.Health = Math.Min(100, p.Health + (_waterRatio >= .5 ? 1 : 0));
                    _history.Fact(_time, id, 3, 1);
                }
                else
                {
                    _unservedMeals++;
                    p.Health -= 1;
                    if (p.Health <= 0) Die(p);
                }
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
            Console.Error.WriteLine($"LAB_CAUSE day={_days} children={childCount} adults={adultCount} retired={olderCount} childOnlyHomes={childOnlyHomes} livedInEmptyPantries={inhabitedEmptyPantries} emptyWithFunds={emptyWithFunds} hungryEmptyStore={hungryWithEmptyStore} hungryStockAndFunds={hungryWithStockAndFunds} shopsNoStock={_shops.Count(f=>f.Stock==0)} shopsNoCash={_shops.Count(f=>f.Cash<WholesalePrice*32)} shopFood={_shops.Sum(f=>f.Stock)} farmFood={_farms.Sum(f=>f.Stock)} eligibleCarerHomes={_households.Count(h=>h.Members.Count<16 && h.Members.Count(id=>_people[id].Alive&&Age(_people[id],date) is >=18 and <60)>=2)}");
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
        Span<int> positions = stackalloc int[StoreCount + FarmCount];
        Span<int> occupied = stackalloc int[StoreCount + FarmCount];
        for (var i = 0; i < positions.Length; i++)
        {
            var farm = i >= StoreCount;
            var firm = farm ? _farms[i - StoreCount] : _shops[i];
            var unitPrice = farm ? WholesalePrice : FoodPrice;
            var monthlyUnits = firm.DayReceipts / unitPrice;
            var unitsPerEmployee = farm ? 35 * 30 : 5 * 30;
            // One trial position per solvent business even with no demand:
            // startup must be possible without fabricated sales or wages.
            var wanted = (int)Math.Clamp(
                (monthlyUnits + unitsPerEmployee - 1) / unitsPerEmployee,
                farm ? 2 : 1, farm ? 60 : 96);
            var wage = farm ? 80 : 65;
            var funded = (int)Math.Min(firm.Cash / (wage * 30L), wanted);
            positions[i] = funded;
            firm.OfferedPositions = funded;
            firm.DayReceipts = 0;
            firm.DaySupplies = 0;
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
                p.AtWork = false;
                continue;
            }
            occupied[p.Job]++;
        }
        // Applicants individually find the nearest offer. Rotating the start
        // ID avoids claiming every offer for the same initial families.
        var start = (int)(((long)_config.Seed + day * 43) % _people.Count);
        for (var k = 0; k < _people.Count; k++)
        {
            var p = _people[(start + k) % _people.Count];
            if (!p.Alive || p.Job >= 0 || Age(p, date) is < 18 or >= 70)
                continue;
            var best = -1;
            var bestDistance = int.MaxValue;
            for (var i = 0; i < positions.Length; i++)
            {
                if (occupied[i] >= positions[i]) continue;
                var node = i < StoreCount ? _shops[i].Node : _farms[i - StoreCount].Node;
                var distance = Math.Abs(p.Home % Grid - node % Grid) +
                               Math.Abs(p.Home / Grid - node / Grid);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = i;
            }
            if (best < 0) break;
            p.Job = best;
            occupied[best]++;
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
    private bool TryBirth(Household h, DateTime date, long day)
    {
        if (h.Members.Count >= 16 || h.Pantry < 2) return false;
        if (_config.CohortRenewal && day - h.LastBirthDay < 4 * 365) return false;
        var carers = 0;
        long cash = 0;
        foreach (var id in h.Members)
        {
            var p = _people[id];
            if (!p.Alive) continue;
            if (Age(p, date) is >= 18 and < 60) carers++;
            cash += p.Cash;
        }
        if (carers < 2 || cash < 300 || _waterRatio < .4) return false;
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
        if (_config.VacancyMarket && p.AtWork && p.Job >= 0)
        {
            var current = p.Job < StoreCount ? _shops[p.Job] : _farms[p.Job - StoreCount];
            current.PresentWorkers--;
            p.AtWork = false;
        }
        p.Alive = false;
        _active--; _deaths++;
        var family = _households[p.HouseholdId];
        var heir = family.Members.Select(id => _people[id]).FirstOrDefault(x => x.Alive);
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
        var workNode = p.Job < StoreCount ? _shops[p.Job].Node : _farms[p.Job - StoreCount].Node;
        p.HeadingToWork = _config.VacancyMarket;
        StartTrip(p, workNode);
    }
    private void EndWork(int personId)
    {
        var p = _people[personId];
        if (!p.Alive || (_config.VacancyMarket && p.Job < 0)) return;
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
        var employer = p.Job < StoreCount ? _shops[p.Job] : _farms[p.Job - StoreCount];
        // Only physically present workers earn wages. Farm output additionally
        // requires the farmworker actually to have arrived for their shift.
        if (p.Job >= StoreCount)
        {
            var food = (int)(35 * Math.Min(_windFactor, _waterRatio));
            employer.Stock = checked(employer.Stock + food);
            _producedFood += food;
            _history.Fact(_time, p.Id, 8, food);
        }
        var wage = _config.WageFeedback && !_config.DemandStaffing
            ? employer.WageForDay : (p.Job < StoreCount ? 65 : 80);
        if (employer.Cash >= wage)
        {
            employer.Cash -= wage;
            if (_config.DemandStaffing)
                employer.PayrollBudget = Math.Max(0, employer.PayrollBudget - wage);
            var contribution = wage / 10; // 10% illustrative contributory fixture
            _treasury += contribution;
            p.Cash += wage - contribution;
            p.ContributedDays++;
            p.GrossEarnings += wage;
            _paidWages += wage;
            _workdays++;
            if (_config.WageFeedback || _config.DemandStaffing || _config.VacancyMarket) employer.DayPaidShifts++;
            _history.Fact(_time, p.Id, 7, wage - contribution);
            _history.Fact(_time, p.Id, 15, contribution);
        }
        p.AtWork = false;
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
        var needed = Math.Max(0, 32 * FoodPrice - p.Cash);
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
        if (p.Cash < FoodPrice) return false;
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
        var workplace = p.Job < 0 ? -1 : p.Job < StoreCount
            ? _shops[p.Job].Node : _farms[p.Job - StoreCount].Node;
        if (_config.VacancyMarket && p.HeadingToWork && p.Job < 0)
        {
            p.HeadingToWork = false;
            StartTrip(p, p.Home); // cancelled shift; not a grocery purchase.
            return;
        }
        var earliest = _config.VacancyMarket && p.Job < StoreCount
            ? (p.Id % 2 == 0 ? 11 : 14) * 3600 : 8 * 3600;
        var latest = _config.VacancyMarket && p.Job < StoreCount
            ? (p.Id % 2 == 0 ? 21 : 23) * 3600 : 17 * 3600;
        if (p.Node == workplace && _time % DaySeconds < latest &&
            _time % DaySeconds >= earliest &&
            (!_config.VacancyMarket || p.HeadingToWork))
        {
            p.HeadingToWork = false;
            p.AtWork = true;
            if (_config.VacancyMarket)
            {
                var employer = p.Job < StoreCount ? _shops[p.Job] : _farms[p.Job - StoreCount];
                employer.PresentWorkers++;
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
        var buy = _config.VacancyMarket && shop.PresentWorkers <= 0
            ? 0 : Math.Min(32, Math.Min(shop.Stock, (int)(p.Cash / FoodPrice)));
        if (buy > 0)
        {
            p.Cash -= buy * FoodPrice;
            shop.Cash += buy * FoodPrice;
            if (_config.WageFeedback || _config.DemandStaffing || _config.VacancyMarket) shop.DayReceipts += buy * FoodPrice;
            shop.Stock -= buy;
            p.CarryFood += buy;
            _sales += buy;
            _history.Fact(_time, p.Id, 10, buy);
        }
        else _unsatisfiedSales++;
        StartTrip(p, p.Home);
    }
    private void CompleteDelivery(int tripId)
    {
        var t = _trips[tripId];
        if (!t.Completed || t.OwnerId != -1) throw new InvalidOperationException("Fictitious shipment completion");
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
        var businessesCash = _shops.Sum(f => f.Cash) + _farms.Sum(f => f.Cash);
        if (businessesCash != checked(InitialBusinessCashCents + _sales * FoodPrice - _paidWages))
            throw new InvalidOperationException("Business sector ledger differs from paid sales minus payroll");
        if (CalculateFood() != checked(_initialFood + _producedFood - _eatenFood))
            throw new InvalidOperationException("Food created/destroyed outside farm work and meals");
        if (_days > _config.Days || _active < 0) throw new InvalidOperationException("Invalid days/population");
        if (_people.Count(p => p.Alive) != _active) throw new InvalidOperationException("Population count inconsistent");
        foreach (var h in _households) if (h.Pantry < 0) throw new InvalidOperationException("Negative pantry");
        foreach (var store in _shops) if (store.Stock < 0 || store.Cash < 0) throw new InvalidOperationException("Invalid shop");
        foreach (var farm in _farms) if (farm.Stock < 0 || farm.Cash < 0) throw new InvalidOperationException("Invalid farm");
        foreach (var p in _people) if (p.Cash < 0 || p.CarryFood < 0) throw new InvalidOperationException("Invalid person");
        if (_config.VacancyMarket)
        {
            for (var firmId = 0; firmId < StoreCount + FarmCount; firmId++)
            {
                var offer = firmId < StoreCount ? _shops[firmId] : _farms[firmId - StoreCount];
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
        Mix(_journeys); Mix(_segments); Mix(_sales); Mix(_shipments);
        foreach (var p in _people)
        {
            Mix(p.Id); Mix(p.Node); Mix(p.Job); Mix(p.Cash); Mix(p.Alive ? 1 : 0); Mix(p.HeadingToWork ? 1 : 0);
            Mix(p.Health); Mix(p.CarryFood); Mix(p.Traveling ? 1 : 0);
            Mix(p.ContributedDays); Mix(p.GrossEarnings); Mix(p.PensionCredit); Mix(p.PensionReceived);
        }
        foreach (var h in _households) { Mix(h.Pantry); Mix(h.Members.Count); Mix(h.LastBirthDay); Mix(h.LastShoppingDepartureDay); }
        foreach (var f in _shops) { Mix(f.Cash); Mix(f.Stock); Mix(f.DayReceipts); Mix(f.DaySupplies); Mix(f.DayPaidShifts); Mix(f.WageForDay); Mix(f.PayrollBudget); Mix(f.OfferedPositions); Mix(f.PresentWorkers); }
        foreach (var f in _farms) { Mix(f.Cash); Mix(f.Stock); Mix(f.DayReceipts); Mix(f.DaySupplies); Mix(f.DayPaidShifts); Mix(f.WageForDay); Mix(f.PayrollBudget); Mix(f.OfferedPositions); Mix(f.PresentWorkers); }
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
        demandStaffingFixture = _config.DemandStaffing,
        vacancyMarketFixture = _config.VacancyMarket,
        employedAlive = _people.Count(p => p.Alive && p.Job >= 0),
        unemployedAdults = _people.Count(p => p.Alive && p.Job < 0 &&
            Age(p, Epoch.AddSeconds(_time)) is >= 18 and < 70),
        vacanciesOffered = _shops.Sum(f => f.OfferedPositions) + _farms.Sum(f => f.OfferedPositions),
        requestedSims = _config.Sims,
        peopleEverCreated = _people.Count,
        aliveEnd = _active,
        peakAlive = _maxActive,
        minimumAliveAtDayBoundary = _minActive,
        simulatedDays = _days,
        gregorianEnd = Epoch.AddDays(_config.Days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        wallSeconds,
        wallSecondsPerDay = wallSeconds / _days,
        speedBudgetOnly_notPassingCriterion = (wallSeconds / _days) * 29220 <= 21600,
        demonstrated10kSustained = _minActive >= 10_000 && _config.Sims >= 10_000,
        journeys = _journeys, roadSegments = _segments, roadWaitSeconds = _waitSeconds,
        salesFoodUnits = _sales, cargoDeliveries = _shipments, paidWagesCents = _paidWages,
        businessSalesRevenueCents = checked(_sales * FoodPrice),
        businessOperatingNetCents = checked(_sales * FoodPrice - _paidWages),
        businessInitialCashCents = InitialBusinessCashCents,
        businessCurrentCashCents = _shops.Sum(f => f.Cash) + _farms.Sum(f => f.Cash),
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
    public void Dispose() => _history.Dispose();
}
