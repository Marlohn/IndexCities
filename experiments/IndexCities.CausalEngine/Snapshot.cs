using System.Text.Json;

namespace IndexCities.CausalEngine;

internal sealed partial class CausalCity
{
    private sealed class TimedSnapshotEvent
    {
        public SimEvent Event { get; set; }
        public long At { get; set; }
        public long Sequence { get; set; }
    }
    private sealed class Snapshot
    {
        public LabConfig Config { get; set; }
        public Person[] People { get; set; } = [];
        public Household[] Households { get; set; } = [];
        public Firm[] Shops { get; set; } = [];
        public Firm[] Farms { get; set; } = [];
        public Firm? Builder { get; set; }
        public ConstructionSite[] ConstructionSites { get; set; } = [];
        public Trip[] Trips { get; set; } = [];
        public int[] FreeTripSlots { get; set; } = [];
        public TimedSnapshotEvent[] Future { get; set; } = [];
        public long[] LaneNextEntry { get; set; } = [];
        public long[] LaneLastEntry { get; set; } = [];
        public long[] JunctionAvailable { get; set; } = [];
        public bool[] Closed { get; set; } = [];
        public long[] Counters { get; set; } = [];
        public int[] Populations { get; set; } = [];
        public double[] Factors { get; set; } = [];
        public long HistoryLastAt { get; set; }
        public long HistoryEntries { get; set; }
    }
    private static readonly JsonSerializerOptions SnapshotJson = new() { IncludeFields = true };
    public void SaveSnapshot(string path)
    {
        AssertConservation();
        // Snapshot is a terminal checkpoint for this engine instance. Its
        // factual stream is closed before the snapshot is committed, so a
        // resumed process can append a new GZip member without corrupting it.
        _history.Dispose();
        var state = new Snapshot
        {
            Config = _config, People = _people.ToArray(),
            Households = _households, Shops = _shops, Farms = _farms,
            Builder = _builder, ConstructionSites = _construction,
            Trips = _trips.ToArray(), FreeTripSlots = _freeTripSlots.ToArray(),
            Future = _events.UnorderedItems.Select(e => new TimedSnapshotEvent
            {
                Event = e.Element, At = e.Priority.At, Sequence = e.Priority.Sequence
            }).OrderBy(e => e.At).ThenBy(e => e.Sequence).ToArray(),
            LaneNextEntry = _laneNextEntry, LaneLastEntry = _laneLastEntry,
            JunctionAvailable = _junctionAvailable, Closed = _closed,
            Counters =
            [
                _sequence, _time, _initialMoney, _treasury, _days, _journeys,
                _segments, _waitSeconds, _sales, _shipments, _workdays, _births,
                _deaths, _unservedMeals, _paidWages, _unsatisfiedSales, _replans,
                _producedFood, _eatenFood, _initialFood, _peakEvents,
                _firstDollarViolation, _reserveGlobal, _paidFreightCents, _freightLaborTrips,
                _outsideTimber, _constructionStarted, _constructionFinished, _constructionPaidShifts,
                _retailSalesCents,
                _municipalTaxPaidCents, _municipalWagesPaidCents,
                _garbageGenerated, _garbageCollected, _garbageAtDepot, _sanitationPaidVisits,
                _garbageInTransit, _wasteTreated, _wasteTreatmentPaidShifts
            ],
            Populations = [_active, _maxActive, _minActive],
            Factors = [_windFactor, _waterRatio],
            HistoryLastAt = _history.LastAt, HistoryEntries = _history.Entries,
        };
        var destination = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var partial = destination + ".partial";
        File.WriteAllText(partial, JsonSerializer.Serialize(state, SnapshotJson));
        File.Move(partial, destination, overwrite: true);
    }

    public static CausalCity FromSnapshot(string path)
    {
        var state = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path), SnapshotJson)
            ?? throw new InvalidDataException("Empty snapshot");
        if ((state.Counters.Length != 37 && state.Counters.Length != 39) || state.Populations.Length != 3 ||
            state.Factors.Length != 2 || state.Households.Length < 1 ||
            state.Shops.Length != StoreCount || state.Farms.Length != FarmCount ||
            state.Builder is null || state.ConstructionSites.Length != state.Households.Length ||
            state.LaneNextEntry.Length != Nodes * 4 ||
            state.LaneLastEntry.Length != Nodes * 4 ||
            state.JunctionAvailable.Length != Nodes ||
            state.Closed.Length != Nodes * 4)
            throw new InvalidDataException("Invalid snapshot schema");

        // Construct without touching the existing history file.
        var city = new CausalCity(state.Config with { History = null });
        city._config = state.Config;
        city._events.Clear();
        city._people.Clear();
        city._people.AddRange(state.People);
        city._trips.Clear();
        city._trips.AddRange(state.Trips);
        city._freeTripSlots.Clear();
        for (var i = state.FreeTripSlots.Length - 1; i >= 0; i--)
            city._freeTripSlots.Push(state.FreeTripSlots[i]);
        state.Households.CopyTo(city._households, 0);
        state.Shops.CopyTo(city._shops, 0);
        state.Farms.CopyTo(city._farms, 0);
        city._builder = state.Builder;
        city._construction = state.ConstructionSites;
        state.LaneNextEntry.CopyTo(city._laneNextEntry, 0);
        state.LaneLastEntry.CopyTo(city._laneLastEntry, 0);
        state.JunctionAvailable.CopyTo(city._junctionAvailable, 0);
        state.Closed.CopyTo(city._closed, 0);
        foreach (var e in state.Future)
            city._events.Enqueue(e.Event, (e.At, e.Sequence));
        var c = state.Counters;
        (city._sequence, city._time, city._initialMoney, city._treasury,
         city._days, city._journeys, city._segments, city._waitSeconds,
         city._sales, city._shipments, city._workdays, city._births,
         city._deaths, city._unservedMeals, city._paidWages,
         city._unsatisfiedSales, city._replans, city._producedFood,
         city._eatenFood, city._initialFood, city._peakEvents,
         city._firstDollarViolation) =
         (c[0], c[1], c[2], c[3], c[4], c[5], c[6], c[7],
          c[8], c[9], c[10], c[11], c[12], c[13], c[14],
          c[15], c[16], c[17], c[18], c[19], c[20], c[21]);
        city._reserveGlobal = c[22];
        city._paidFreightCents = c[23];
        city._freightLaborTrips = c[24];
        city._outsideTimber = (int)c[25];
        city._constructionStarted = c[26];
        city._constructionFinished = c[27];
        city._constructionPaidShifts = c[28];
        city._retailSalesCents = c[29];
        city._municipalTaxPaidCents = c[30];
        city._municipalWagesPaidCents = c[31];
        city._garbageGenerated = c[32];
        city._garbageCollected = c[33];
        city._garbageAtDepot = c[34];
        city._sanitationPaidVisits = c[35];
        city._garbageInTransit = c[36];
        if (c.Length >= 39)
        {
            city._wasteTreated = c[37];
            city._wasteTreatmentPaidShifts = c[38];
        }
        city._active = state.Populations[0];
        city._maxActive = state.Populations[1];
        city._minActive = state.Populations[2];
        city._windFactor = state.Factors[0];
        city._waterRatio = state.Factors[1];
        city._history.Dispose();
        city._history = new HistorySink(state.Config.History, append: true,
            lastAt: state.HistoryLastAt, entries: state.HistoryEntries);
        city.AssertConservation();
        return city;
    }
}
