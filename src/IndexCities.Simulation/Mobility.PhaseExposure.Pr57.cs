namespace IndexCities.Simulation;

// PR57 #228: observational exposure of the canonical s3 lane scan to
// writes after the movement slice began. This is NOT a phase-batched
// executor, readset closure or cache. The canonical predicate always runs.
public sealed partial class CitySimulation
{
    static readonly bool Pr57PhaseExposureEnabled =
        Environment.GetEnvironmentVariable("PR57_228_PHASE_EXPOSURE") == "1";

    readonly record struct Pr57PhaseExposureSnapshot(
        VehicleStatus Status, Point Position, long HeadingBits,
        int RouteIndex, int? Claim, int Spaces);

    readonly Dictionary<int, Pr57PhaseExposureSnapshot> _pr57PhaseStart = new();
    readonly HashSet<int> _pr57PhaseProcessed = new();
    readonly HashSet<int> _pr57PhaseChangedReaders = new();
    readonly HashSet<int> _pr57PhaseChangedCandidates = new();
    bool _pr57PhaseActive;
    long _pr57PhaseVisits, _pr57PhaseFrozenEntry, _pr57PhaseChangedSelf;
    long _pr57PhaseChangedThird, _pr57PhaseChangedProcessed;
    long _pr57PhaseChangedUnprocessed, _pr57PhaseGateFlips;
    long _pr57PhasePositionChanges, _pr57PhaseStatusChanges, _pr57PhaseHeadingChanges;
    long _pr57PhaseMissing, _pr57PhaseProcessedActors;

    static Pr57PhaseExposureSnapshot Pr57PhaseSnapshot(Vehicle v) =>
        new(v.Status, v.Position, BitConverter.DoubleToInt64Bits(v.Heading),
            v.RouteIndex, v.IntersectionClaim, v.ManoeuvreSpaces.Count);

    void Pr57PhaseExposureBeginSlice()
    {
        if (!Pr57PhaseExposureEnabled || !_pr57ExclusiveActive) return;
        Pr57PhaseExposureBuild();
    }

    // Test seam; does not enable the observer in ordinary simulation.
    void Pr57PhaseExposureBuild()
    {
        _pr57PhaseActive = true;
        _pr57PhaseStart.Clear();
        _pr57PhaseProcessed.Clear();
        _pr57PhaseChangedReaders.Clear();
        _pr57PhaseChangedCandidates.Clear();
        _pr57PhaseVisits = _pr57PhaseFrozenEntry = _pr57PhaseChangedSelf = 0;
        _pr57PhaseChangedThird = _pr57PhaseChangedProcessed = 0;
        _pr57PhaseChangedUnprocessed = _pr57PhaseGateFlips = 0;
        _pr57PhasePositionChanges = _pr57PhaseStatusChanges = 0;
        _pr57PhaseHeadingChanges = _pr57PhaseMissing = _pr57PhaseProcessedActors = 0;
        foreach (var v in State.Vehicles) _pr57PhaseStart[v.Id] = Pr57PhaseSnapshot(v);
    }

    void Pr57PhaseExposureRead(Vehicle reader, Vehicle candidate, bool live)
    {
        if (!_pr57PhaseActive) return;
        _pr57PhaseVisits++;
        // Entering-lane scans deliberately use an earlier frozen entry frame.
        // Its candidate must not be mistaken for the current live vehicle.
        if (!live) { _pr57PhaseFrozenEntry++; return; }
        if (!_pr57PhaseStart.TryGetValue(candidate.Id, out var initial))
        {
            _pr57PhaseMissing++;
            return;
        }
        var current = Pr57PhaseSnapshot(candidate);
        if (initial == current) return;
        if (reader.Id == candidate.Id) { _pr57PhaseChangedSelf++; return; }
        _pr57PhaseChangedThird++;
        if (_pr57PhaseProcessed.Contains(candidate.Id)) _pr57PhaseChangedProcessed++;
        else _pr57PhaseChangedUnprocessed++;
        _pr57PhaseChangedReaders.Add(reader.Id);
        _pr57PhaseChangedCandidates.Add(candidate.Id);
        if (initial.Position != current.Position) _pr57PhasePositionChanges++;
        if (initial.Status != current.Status) _pr57PhaseStatusChanges++;
        if (initial.HeadingBits != current.HeadingBits) _pr57PhaseHeadingChanges++;

        // A necessary, not sufficient, s3 geometric gate. This comparison
        // holds the reader's CURRENT pose constant, isolating candidate drift.
        // It is not the complete lane-leader predicate or a decision replay.
        var sin = Math.Sin(reader.Heading);
        var cos = Math.Cos(reader.Heading);
        static bool Gate(Point readerAt, Point otherAt, double sin, double cos)
        {
            var dx = otherAt.X - readerAt.X;
            var dz = otherAt.Z - readerAt.Z;
            return dx * sin + dz * cos > 0 &&
                   Math.Abs(dx * cos - dz * sin) < 2.6;
        }
        if (Gate(reader.Position, initial.Position, sin, cos) !=
            Gate(reader.Position, current.Position, sin, cos))
        {
            _pr57PhaseGateFlips++;
            Pr57PhaseImpactFlip(reader, candidate, initial, current);
        }
    }

    void Pr57PhaseExposureAfterVehicle(Vehicle vehicle)
    {
        if (!_pr57PhaseActive) return;
        _pr57PhaseProcessedActors++;
        _pr57PhaseProcessed.Add(vehicle.Id);
    }

    void Pr57PhaseExposureEndSlice()
    {
        if (!_pr57PhaseActive) return;
        Console.WriteLine(
            $"PR57_228_PHASE_EXPOSURE sample={_pr57ExclusiveSamples} " +
            $"vehicles={_pr57PhaseStart.Count} processedActors={_pr57PhaseProcessedActors} " +
            $"s3Visits={_pr57PhaseVisits} frozenEntryVisits={_pr57PhaseFrozenEntry} " +
            $"changedSelf={_pr57PhaseChangedSelf} changedThird={_pr57PhaseChangedThird} " +
            $"changedProcessed={_pr57PhaseChangedProcessed} " +
            $"changedUnprocessed={_pr57PhaseChangedUnprocessed} " +
            $"gateFlips={_pr57PhaseGateFlips} positionChanges={_pr57PhasePositionChanges} " +
            $"statusChanges={_pr57PhaseStatusChanges} headingChanges={_pr57PhaseHeadingChanges} " +
            $"changedReaders={_pr57PhaseChangedReaders.Count} " +
            $"changedCandidates={_pr57PhaseChangedCandidates.Count} missing={_pr57PhaseMissing} " +
            "scope=s3-live-candidates-vs-slice-start onlyNecessaryForwardSideGate=True " +
            "doesNotAttributeWriter=True doesNotCoverS4S5S6S7=True " +
            "noPhaseBatching=True canonicalAlwaysRuns=True observational=True intrusive=True");
        _pr57PhaseActive = false;
        _pr57PhaseStart.Clear();
        _pr57PhaseProcessed.Clear();
        _pr57PhaseChangedReaders.Clear();
        _pr57PhaseChangedCandidates.Clear();
    }
}
