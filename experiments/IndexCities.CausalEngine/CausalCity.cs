using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace IndexCities.CausalEngine;

// Independent vertical-slice engine for falsifying the architecture.
// RULES AND FIXTURES HERE ARE NOT AN IMPLEMENTATION OF ALL OF INDEXCITIES SPEC.
internal readonly record struct LabConfig(int Sims, int Days, int Seed, string? History, bool Adversarial);
internal enum EventKind : byte { Day, Hour, StartWork, EndWork, Move, Delivery, RoadClose, RoadOpen }
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
    public bool AtWork;
    public bool Traveling;
    public int TripId = -1;
}
internal sealed class Household
{
    public required int Id;
    public required int Home;
    public required List<int> Members;
    public int Pantry = 40;
}
internal sealed class Firm
{
    public required int Id;
    public required int Node;
    public long Cash = 20_000_000;
    public int Stock = 1000;
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
internal sealed class HistorySink : IDisposable
{
    private readonly BinaryWriter? _writer;
    public long Entries { get; private set; }
    public HistorySink(string? path)
    {
        if (path is null) return;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        _writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write,
            FileShare.Read, 1024 * 1024, FileOptions.SequentialScan));
    }
    public void Fact(long at, int owner, byte type, long amount)
    {
        // Compact 21-byte append-only factual record; never sampled or truncated.
        Entries++;
        if (_writer is null) return;
        _writer.Write(at);
        _writer.Write(owner);
        _writer.Write(type);
        _writer.Write(amount);
    }
    public void Flush() => _writer?.Flush();
    public void Dispose() => _writer?.Dispose();
}
internal sealed class CausalCity : IDisposable
{
    public static readonly DateTime Epoch = new(2020, 1, 1);
    public const long DaySeconds = 86400;
    private const long TravelSeconds = 12, Headway = 3, CrossingSeconds = 2;
    private const int Grid = 8, Nodes = Grid * Grid;
    private const int StoreCount = 64, FarmCount = 8;
    private const int FoodPrice = 50, WholesalePrice = 20;
    private readonly LabConfig _config;
    private readonly PriorityQueue<SimEvent, (long At, long Sequence)> _events = new();
    private readonly List<Person> _people = [];
    private readonly List<Trip> _trips = [];
    private readonly Household[] _households;
    private readonly Firm[] _shops;
    private readonly Firm[] _farms;
    private readonly long[] _laneNextEntry = new long[Nodes * 4];
    private readonly long[] _laneLastEntry = new long[Nodes * 4];
    private readonly long[] _junctionAvailable = new long[Nodes];
    private readonly bool[] _closed = new bool[Nodes * 4];
    private readonly HistorySink _history;
    private readonly Random _random;
    private long _sequence, _time, _initialMoney, _treasury;
    private long _days, _journeys, _segments, _waitSeconds, _sales, _shipments, _workdays;
    private long _births, _deaths, _unservedMeals, _paidWages, _unsatisfiedSales, _replans;
    private long _peakEvents, _oldestQueue;
    private long _firstDollarViolation = -1;
    private int _active, _maxActive, _minActive;
    private double _windFactor = 1, _waterRatio = 1;
    private int _deliveryVanId;
    public CausalCity(LabConfig config)
    {
        _config = config;
        _history = new HistorySink(config.History);
        _random = new Random(config.Seed);
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
                Home = family.Home, Node = family.Home, Job = (i * 17 + 7) % (StoreCount + FarmCount),
                Cash = 30000 };
            _people.Add(p);
            family.Members.Add(i);
        }
        _active = _maxActive = _minActive = config.Sims;
        _initialMoney = CalculateMoney();
        Schedule(0, EventKind.Day, 0);
        Schedule(0, EventKind.Hour, 0);
        if (config.Adversarial)
        {
            Schedule(1860, EventKind.RoadClose, 0);
            Schedule(3000, EventKind.RoadOpen, 0);
        }
    }

    private long CalculateMoney()
    {
        long total = _treasury;
        foreach (var p in _people) total = checked(total + p.Cash);
        foreach (var f in _shops) total = checked(total + f.Cash);
        foreach (var f in _farms) total = checked(total + f.Cash);
        return total;
    }
    private void Schedule(long when, EventKind kind, int id)
    {
        if (when < _time) throw new InvalidOperationException("Event in the past");
        _events.Enqueue(new SimEvent(kind, id), (when, ++_sequence));
        _peakEvents = Math.Max(_peakEvents, _events.Count);
    }
    public void Run()
    {
        var end = _config.Days * DaySeconds;
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
        // A fixture-specific, deterministic demographic policy. NOT an approved
        // IndexCities birth/death algorithm. Birth requires real adult carers,
        // a housing slot, water, pantry and existing household money.
        if (day != 0)
        {
            if (day % 2 == 0)
            {
                var start = (int)((day * 19) % _households.Length);
                for (var k = 0; k < _households.Length; k++)
                {
                    var h = _households[(start + k) % _households.Length];
                    if (h.Members.Count >= 16 || h.Pantry < 2) continue;
                    var carers = 0; long cash = 0;
                    foreach (var id in h.Members)
                    {
                        var p = _people[id]; if (!p.Alive) continue;
                        if (Age(p, date) >= 18 && Age(p, date) < 60) carers++;
                        cash += p.Cash;
                    }
                    if (carers < 2 || cash < 300 || _waterRatio < .4) continue;
                    var child = new Person { Id = _people.Count, HouseholdId = h.Id,
                        Born = date, Home = h.Home, Node = h.Home, Job = (h.Id * 7) % 72, Cash = 0 };
                    _people.Add(child);
                    h.Members.Add(child.Id);
                    _active++; _births++;
                    _maxActive = Math.Max(_maxActive, _active);
                    _history.Fact(_time, child.Id, 5, h.Id);
                    break;
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
                    p.Health = Math.Min(100, p.Health + (_waterRatio >= .5 ? 1 : 0));
                    _history.Fact(_time, id, 3, 1);
                }
                else
                {
                    _unservedMeals++;
                    p.Health -= 1;
                    if (p.Health <= 0) Die(p);
                }
                if (p.Alive && Age(p, date) >= 18 && Age(p, date) < 65)
                {
                    Schedule(_time + 8 * 3600, EventKind.StartWork, id);
                    Schedule(_time + 17 * 3600, EventKind.EndWork, id);
                }
            }
        }
        foreach (var farm in _farms)
        {
            // Farm output requires energy and water. Source is real production
            // in the fixture, not magically replenishing shop inventory.
            var quantity = (int)(1500 * Math.Min(_windFactor, _waterRatio));
            farm.Stock = checked(farm.Stock + quantity);
            _history.Fact(_time, -1000 - farm.Id, 8, quantity);
        }
        for (var shopId = 0; shopId < _shops.Length; shopId++)
        {
            var farmId = shopId % _farms.Length;
            var shop = _shops[shopId];
            var farm = _farms[farmId];
            var want = Math.Max(0, 240 - shop.Stock);
            var count = Math.Min(240, Math.Min(want, farm.Stock));
            if (count <= 0 || shop.Cash < count * WholesalePrice) continue;
            // Claim cargo and pay supplier before departure; actual store stock
            // increases ONLY after the truck reaches the destination.
            shop.Cash -= count * WholesalePrice;
            farm.Cash += count * WholesalePrice;
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
        }
        if (_days < _config.Days) Schedule(_time + DaySeconds, EventKind.Day, 0);
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
        p.Alive = false;
        _active--; _deaths++;
        var family = _households[p.HouseholdId];
        var heir = family.Members.Select(id => _people[id]).FirstOrDefault(x => x.Alive);
        if (heir is null) _treasury = checked(_treasury + p.Cash);
        else heir.Cash = checked(heir.Cash + p.Cash);
        p.Cash = 0;
        _history.Fact(_time, p.Id, 6, heir?.Id ?? -1);
    }
    private void StartWork(int personId)
    {
        var p = _people[personId];
        if (!p.Alive || p.Traveling || p.Node != p.Home) return;
        var workNode = p.Job < StoreCount ? _shops[p.Job].Node : _farms[p.Job - StoreCount].Node;
        StartTrip(p, workNode);
    }
    private void EndWork(int personId)
    {
        var p = _people[personId];
        if (!p.Alive || p.Traveling || !p.AtWork) return;
        var employer = p.Job < StoreCount ? _shops[p.Job] : _farms[p.Job - StoreCount];
        // Only physically present workers earn wages.
        var wage = p.Job < StoreCount ? 50 : 200;
        if (employer.Cash >= wage)
        {
            employer.Cash -= wage;
            p.Cash += wage;
            _paidWages += wage;
            _workdays++;
            _history.Fact(_time, p.Id, 7, wage);
        }
        p.AtWork = false;
        var h = _households[p.HouseholdId];
        if (h.Members.FirstOrDefault(id => _people[id].Alive && Age(_people[id], Epoch.AddSeconds(_time)) >= 18) == p.Id && h.Pantry < 30 && p.Cash >= 100)
            StartTrip(p, _shops[p.Home].Node);
        else StartTrip(p, p.Home);
    }
    private Trip NewTrip(int owner, int from, int destination, EventKind completion, int payload = 0, int cargo = 0)
    {
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
        var workplace = p.Job < StoreCount ? _shops[p.Job].Node : _farms[p.Job - StoreCount].Node;
        if (p.Node == workplace && _time % DaySeconds < 17 * 3600 && _time % DaySeconds >= 8 * 3600)
        {
            p.AtWork = true;
            return;
        }
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
        var buy = Math.Min(32, Math.Min(shop.Stock, (int)(p.Cash / FoodPrice)));
        if (buy > 0)
        {
            p.Cash -= buy * FoodPrice;
            shop.Cash += buy * FoodPrice;
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
        if (_days > _config.Days || _active < 0) throw new InvalidOperationException("Invalid days/population");
        if (_people.Count(p => p.Alive) != _active) throw new InvalidOperationException("Population count inconsistent");
        foreach (var h in _households) if (h.Pantry < 0) throw new InvalidOperationException("Negative pantry");
        foreach (var store in _shops) if (store.Stock < 0 || store.Cash < 0) throw new InvalidOperationException("Invalid shop");
        foreach (var farm in _farms) if (farm.Stock < 0 || farm.Cash < 0) throw new InvalidOperationException("Invalid farm");
        foreach (var p in _people) if (p.Cash < 0 || p.CarryFood < 0) throw new InvalidOperationException("Invalid person");
    }
    public object Report(double wallSeconds) => new
    {
        status = "PARTIAL_ARCHITECTURAL_FIXTURE_NOT_FULL_INDEXCITIES",
        warning = "Not SPEC-complete: simplified lanes/intersections, no pedestrian crosswalks, real towing, building construction, complex health or full demography. NEVER claim full 10k/80y achieved.",
        seed = _config.Seed,
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
        workingDays = _workdays, births = _births, deaths = _deaths,
        unmetMeals = _unservedMeals, deniedPurchases = _unsatisfiedSales,
        routeReroutes = _replans, recordedFacts = _history.Entries, peakQueue = _peakEvents,
        pendingQueue = _events.Count,
        moneyConserved = CalculateMoney() == _initialMoney,
        initialMoneyCents = _initialMoney,
        currentMoneyCents = CalculateMoney(),
        historyPersistent = _config.History is not null
    };
    public void Dispose() => _history.Dispose();
}
