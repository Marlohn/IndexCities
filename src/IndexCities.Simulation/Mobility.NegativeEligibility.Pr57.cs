namespace IndexCities.Simulation;

// PR57 #217: bounded observation of NEGATIVE lane-candidate filters.
// Captures the exact branch outcome of the body-across/occupancy gates and
// a compact, conservative state projection at the first candidate visit.
// It does not identify all fields read by those gates, record actual writers,
// or authorize reuse of a previous decision. Enabled only together with #214.
public sealed partial class CitySimulation
{
    static readonly bool Pr57NegativeEnabled =
        Environment.GetEnvironmentVariable("PR57_217_NEGATIVE_FILTERS") == "1";

    readonly record struct Pr57NegativeRow(
        int Id, byte Stage, VehicleStatus Status,
        long X, long Z, long Heading, int? Claim,
        bool Dock, int CorridorCount, int SpacesCount,
        int RouteIndex, TowPhase TowPhase, bool PassengerCar, PavingMachine Machine);

    readonly Dictionary<int, HashSet<(int Id, int Kind)>> _pr57NegativeEdges = new();
    readonly HashSet<(int Id, int Kind)> _pr57NegativeReaders = new();
    readonly HashSet<(int Id, int Kind)> _pr57NegativeAffected = new();
    long _pr57NegativeAligned, _pr57NegativeUnaligned, _pr57NegativeRows;
    long _pr57NegativePose, _pr57NegativeGate, _pr57NegativeTransitions;
    long _pr57NegativeBecameOccupied, _pr57NegativeBecameDock, _pr57NegativeLostOccupied;
    long _pr57NegativeCaptured;

    void Pr57NegativeBeginSlice()
    {
        if (!Pr57NegativeEnabled) return;
        _pr57NegativeEdges.Clear();
        _pr57NegativeReaders.Clear();
        _pr57NegativeAffected.Clear();
        _pr57NegativeAligned = _pr57NegativeUnaligned = _pr57NegativeRows = 0;
        _pr57NegativePose = _pr57NegativeGate = _pr57NegativeTransitions = 0;
        _pr57NegativeBecameOccupied = _pr57NegativeBecameDock = _pr57NegativeLostOccupied = 0;
        _pr57NegativeCaptured = 0;
    }

    void Pr57NegativeBeginCandidate(Vehicle other)
    {
        if (!Pr57NegativeEnabled || _pr57CandidateActive is not { } trace ||
            _pr57CandidateKey.Kind != 0) return;
        trace.Negatives.Add(new Pr57NegativeRow(
            other.Id, 0, other.Status,
            BitConverter.DoubleToInt64Bits(other.Position.X),
            BitConverter.DoubleToInt64Bits(other.Position.Z),
            BitConverter.DoubleToInt64Bits(other.Heading),
            other.IntersectionClaim, other.DockPosition.HasValue || other.ManoeuvreDock.HasValue,
            other.DockCorridor.Count, other.ManoeuvreSpaces.Count,
            other.RouteIndex, other.TowPhase, other.PassengerCar, other.Machine));
        _pr57NegativeCaptured++;
    }

    // Stage 1: held by the dock-body exception; 2: self or not an occupant;
    // 3: passed the occupancy gate. A stage is assigned exactly once per visit.
    void Pr57NegativeStage(byte stage)
    {
        if (!Pr57NegativeEnabled || _pr57CandidateActive is not { } trace ||
            _pr57CandidateKey.Kind != 0 || trace.Negatives.Count == 0) return;
        var last = trace.Negatives.Count - 1;
        trace.Negatives[last] = trace.Negatives[last] with { Stage = stage };
    }

    void Pr57NegativeCompare(
        (int Id, int Kind) reader,
        List<Pr57NegativeRow> oldRows,
        List<Pr57NegativeRow> newRows)
    {
        if (!Pr57NegativeEnabled || reader.Kind != 0) return;
        _pr57NegativeReaders.Add(reader);
        if (oldRows.Count != newRows.Count)
        {
            _pr57NegativeUnaligned += Math.Abs(oldRows.Count - newRows.Count);
            _pr57NegativeAffected.Add(reader);
        }
        var length = Math.Min(oldRows.Count, newRows.Count);
        for (var i = 0; i < length; i++)
        {
            var a = oldRows[i];
            var b = newRows[i];
            if (a.Id != b.Id)
            {
                _pr57NegativeUnaligned++;
                _pr57NegativeAffected.Add(reader);
                continue; // Do not equate different IDs at the same index.
            }
            if (a.Id == reader.Id) continue; // The actor itself is never a blocker.
            _pr57NegativeAligned++;
            if (a.Stage != 2 && b.Stage != 2) continue;
            _pr57NegativeRows++;
            var pose = a.X != b.X || a.Z != b.Z || a.Heading != b.Heading;
            var gate = a.Status != b.Status || a.Claim != b.Claim ||
                       a.Dock != b.Dock || a.CorridorCount != b.CorridorCount ||
                       a.SpacesCount != b.SpacesCount || a.RouteIndex != b.RouteIndex ||
                       a.TowPhase != b.TowPhase || a.PassengerCar != b.PassengerCar ||
                       a.Machine != b.Machine;
            var transition = a.Stage != b.Stage;
            if (pose) _pr57NegativePose++;
            if (gate) _pr57NegativeGate++;
            if (transition)
            {
                _pr57NegativeTransitions++;
                if (a.Stage == 2 && b.Stage == 3) _pr57NegativeBecameOccupied++;
                if (a.Stage == 2 && b.Stage == 1) _pr57NegativeBecameDock++;
                if (a.Stage == 3 && b.Stage == 2) _pr57NegativeLostOccupied++;
            }
            if (!pose && !gate && !transition) continue;
            _pr57NegativeAffected.Add(reader);
            if (!_pr57NegativeEdges.TryGetValue(a.Id, out var readers))
                _pr57NegativeEdges[a.Id] = readers = new();
            readers.Add(reader);
        }
    }

    void Pr57NegativeEndSlice()
    {
        if (!Pr57NegativeEnabled || !_pr57CandidateSliceActive) return;
        var edges = _pr57NegativeEdges.Values.Sum(readers => readers.Count);
        var max = _pr57NegativeEdges.Values.Select(readers => readers.Count)
            .DefaultIfEmpty(0).Max();
        Console.WriteLine(
            $"PR57_217_NEGATIVE sample={_pr57ExclusiveSamples} selected={_pr57CandidateSelected.Count} " +
            $"comparedReaders={_pr57NegativeReaders.Count} affectedReaders={_pr57NegativeAffected.Count} " +
            $"capturedRows={_pr57NegativeCaptured} alignedRows={_pr57NegativeAligned} " +
            $"unalignedRows={_pr57NegativeUnaligned} negativeRows={_pr57NegativeRows} " +
            $"negativePoseChanges={_pr57NegativePose} negativeGateChanges={_pr57NegativeGate} " +
            $"stageTransitions={_pr57NegativeTransitions} becameOccupied={_pr57NegativeBecameOccupied} " +
            $"becameDock={_pr57NegativeBecameDock} lostOccupied={_pr57NegativeLostOccupied} " +
            $"changedCandidateIds={_pr57NegativeEdges.Count} candidateReaderEdges={edges} maxFanout={max} " +
            "scope=selectedLaneQueries37to39 observational=True intrusive=True " +
            "negativeReadsNotExact=True writersNotObserved=True causalClosureProven=False");
    }
}
