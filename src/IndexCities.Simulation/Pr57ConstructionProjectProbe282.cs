namespace IndexCities.Simulation;

// PR57 #282: observation only. These snapshots cover selected project fields,
// NOT a complete causal read/write set and never authorize skipping a phase.
public sealed class Pr57ConstructionProjectProbe
{
    public readonly record struct Snapshot(
        string Cause, WorkStatus Status, double Progress,
        int? CrewVehicleId, int LocalCrewCount, int ShipmentCount, int LedgerCount);

    sealed class Totals
    {
        public long Calls, Ticks, CauseChanges, StatusChanges, ProgressChanges;
        public long CrewChanges, LocalCrewChanges, ShipmentCountChanges, LedgerCountChanges;
        public long NoSelectedFieldChange;
    }

    readonly Dictionary<(string Phase, int ProjectId), Totals> _totals = new();

    public static Snapshot Capture(WorkProject project, int shipmentCount, int ledgerCount)
        => new(project.Cause, project.Status, project.Progress,
            project.CrewVehicleId, project.LocalCrew.Count, shipmentCount, ledgerCount);

    public void Observe(string phase, int projectId, Snapshot before, Snapshot after, long elapsedTicks)
    {
        var key = (phase, projectId);
        if (!_totals.TryGetValue(key, out var totals))
            _totals[key] = totals = new Totals();
        totals.Calls++;
        totals.Ticks += elapsedTicks;
        var cause = before.Cause != after.Cause;
        var status = before.Status != after.Status;
        var progress = before.Progress != after.Progress;
        var crew = before.CrewVehicleId != after.CrewVehicleId;
        var localCrew = before.LocalCrewCount != after.LocalCrewCount;
        var shipments = before.ShipmentCount != after.ShipmentCount;
        var ledger = before.LedgerCount != after.LedgerCount;
        if (cause) totals.CauseChanges++;
        if (status) totals.StatusChanges++;
        if (progress) totals.ProgressChanges++;
        if (crew) totals.CrewChanges++;
        if (localCrew) totals.LocalCrewChanges++;
        if (shipments) totals.ShipmentCountChanges++;
        if (ledger) totals.LedgerCountChanges++;
        if (!(cause || status || progress || crew || localCrew || shipments || ledger))
            totals.NoSelectedFieldChange++;
    }

    static string Metrics(Totals totals)
        => $"calls={totals.Calls} wallMs={1000.0 * totals.Ticks / System.Diagnostics.Stopwatch.Frequency:F3}" +
           $" causeChanges={totals.CauseChanges} statusChanges={totals.StatusChanges}" +
           $" progressChanges={totals.ProgressChanges} crewChanges={totals.CrewChanges}" +
           $" localCrewChanges={totals.LocalCrewChanges} shipmentCountChanges={totals.ShipmentCountChanges}" +
           $" ledgerCountChanges={totals.LedgerCountChanges}" +
           $" noSelectedFieldChange={totals.NoSelectedFieldChange}";

    public IEnumerable<string> Rows()
    {
        foreach (var phase in new[] { "order", "progress" })
        {
            var entries = _totals.Where(entry => entry.Key.Phase == phase).ToArray();
            var sum = new Totals();
            foreach (var (_, value) in entries)
            {
                sum.Calls += value.Calls;
                sum.Ticks += value.Ticks;
                sum.CauseChanges += value.CauseChanges;
                sum.StatusChanges += value.StatusChanges;
                sum.ProgressChanges += value.ProgressChanges;
                sum.CrewChanges += value.CrewChanges;
                sum.LocalCrewChanges += value.LocalCrewChanges;
                sum.ShipmentCountChanges += value.ShipmentCountChanges;
                sum.LedgerCountChanges += value.LedgerCountChanges;
                sum.NoSelectedFieldChange += value.NoSelectedFieldChange;
            }
            yield return $"PR57_282_SUM phase={phase} projects={entries.Length} {Metrics(sum)} note=SELECTED_FIELDS_ONLY_NOT_CAUSAL_INACTIVITY";
            foreach (var entry in entries.OrderByDescending(entry => entry.Value.Ticks).Take(10))
                yield return $"PR57_282_PROJECT phase={phase} id={entry.Key.ProjectId} {Metrics(entry.Value)}";
        }
    }
}
