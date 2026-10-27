namespace IndexCities.Simulation;

// PR57 #192: bounded observational census across actual GRID movement sub-slices.
// Presence is NOT a complete read set or proof of causal independence.
// Disabled by default; no simulation decision or persistence reads these counters.
public sealed partial class CitySimulation
{
    static readonly bool Pr57JunctionCensusEnabled =
        Environment.GetEnvironmentVariable("PR57_192_JUNCTION_CENSUS") == "1";
    bool _pr57JunctionCensusSliceActive, _pr57JunctionCensusReported;
    long _pr57JunctionCensusSlices, _pr57JunctionCensusAdmissions;
    long _pr57JunctionCensusSignalPotential, _pr57JunctionCensusSignalReached;
    long _pr57JunctionCensusRedReached, _pr57JunctionCensusPedestrianPresence;
    long _pr57JunctionCensusReservationPresence, _pr57JunctionCensusTicks;
    double _pr57JunctionCensusFirstSignal = -1;
    double _pr57JunctionCensusFirstReservation = -1;
    double _pr57JunctionCensusFirstPedestrian = -1;

    void Pr57JunctionCensusBeginSlice(double dt, double? operationsDt)
    {
        _pr57JunctionCensusSliceActive =
            Pr57JunctionCensusEnabled && CostProfile is not null &&
            _vehicleMovementBatch is not null && operationsDt is null && dt > 0;
        if (_pr57JunctionCensusSliceActive) _pr57JunctionCensusSlices++;
    }

    // Invoked only when MoveVehiclesUnsplit actually attempts MayEnterJunction.
    // The presence checks may see entities that the actual admission exits before reading.
    void Pr57JunctionCensusObserve(Vehicle reader, RoadNode junction)
    {
        if (!_pr57JunctionCensusSliceActive) return;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57JunctionCensusAdmissions++;
        if (junction.Neighbours.Count >= 3 && junction.Control == JunctionControl.Signal)
        {
            _pr57JunctionCensusSignalPotential++;
            if (_pr57JunctionCensusFirstSignal < 0)
                _pr57JunctionCensusFirstSignal = VehicleMovementTime;
        }
        if (State.Citizens.Any(c => c.Alive && c.VehicleId == null && c.Moving &&
            c.CrossingNodeId == junction.Id &&
            c.Position.Distance(junction.Position) < State.Tuning.RoadWidth / 2 + 1.5))
        {
            _pr57JunctionCensusPedestrianPresence++;
            if (_pr57JunctionCensusFirstPedestrian < 0)
                _pr57JunctionCensusFirstPedestrian = VehicleMovementTime;
        }
        if (State.Vehicles.Any(o => o.Id != reader.Id &&
            (o.IntersectionClaim == junction.Id ||
             o.ManoeuvreSpaces.Contains(junction.Id))))
        {
            _pr57JunctionCensusReservationPresence++;
            if (_pr57JunctionCensusFirstReservation < 0)
                _pr57JunctionCensusFirstReservation = VehicleMovementTime;
        }
        _pr57JunctionCensusTicks +=
            System.Diagnostics.Stopwatch.GetTimestamp() - started;
    }

    // Called at the exact source location immediately before the existing signal predicate.
    // No predicate result is substituted; this counter cannot affect admission.
    void Pr57JunctionCensusSignalReached(Vehicle reader, RoadNode junction)
    {
        if (!_pr57JunctionCensusSliceActive ||
            junction.Control != JunctionControl.Signal) return;
        _pr57JunctionCensusSignalReached++;
        var horizontal = Math.Abs(Math.Sin(reader.Heading)) > .7;
        if ((VehicleMovementTime % 60 < 30) != horizontal)
            _pr57JunctionCensusRedReached++;
    }

    void Pr57JunctionCensusEndSlice() => _pr57JunctionCensusSliceActive = false;

    void Pr57JunctionCensusReportIfReady(double endTime)
    {
        // The authentic GRID fixture begins at t=3600; 200 Advance(2) calls end at t=4000.
        if (!Pr57JunctionCensusEnabled || _pr57JunctionCensusReported ||
            endTime < 4000 - .000001) return;
        _pr57JunctionCensusReported = true;
        Console.WriteLine(
            $"PR57_192_JUNCTION_CENSUS slices={_pr57JunctionCensusSlices} " +
            $"admissions={_pr57JunctionCensusAdmissions} " +
            $"signalPotential={_pr57JunctionCensusSignalPotential} " +
            $"signalReached={_pr57JunctionCensusSignalReached} " +
            $"signalRedReached={_pr57JunctionCensusRedReached} " +
            $"pedestrianPresence={_pr57JunctionCensusPedestrianPresence} " +
            $"reservationPresence={_pr57JunctionCensusReservationPresence} " +
            $"firstSignal={_pr57JunctionCensusFirstSignal:F6} " +
            $"firstReservation={_pr57JunctionCensusFirstReservation:F6} " +
            $"firstPedestrian={_pr57JunctionCensusFirstPedestrian:F6} " +
            $"observerMs={_pr57JunctionCensusTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F3} " +
            "scope=OBSERVATIONAL_ALL_ACTIVE_SLICES_PRESENCE_NOT_EXACT_READ_SET");
    }
}
