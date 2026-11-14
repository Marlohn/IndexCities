namespace IndexCities.Simulation;

// PR57 #216: bounded, observational candidate-to-reader exposure in the #214
// sample. Differences are between the last read in one physical slice and the
// first read in the next. They are NOT a census of actual write events, nor a
// causal dependency closure (signals, time, pedestrians, roads, transient
// writes, rejected candidates and external edits are not represented).
public sealed partial class CitySimulation
{
    static readonly bool Pr57FanoutEnabled =
        Environment.GetEnvironmentVariable("PR57_216_CANDIDATE_FANOUT") == "1";

    readonly Dictionary<int, HashSet<(int Id, int Kind)>> _pr57FanoutReaders = new();
    readonly HashSet<(int Id, int Kind)> _pr57FanoutComparedReaders = new();
    readonly HashSet<(int Id, int Kind)> _pr57FanoutAffectedReaders = new();
    long _pr57FanoutAlignedRows, _pr57FanoutChangedJsonRows;
    long _pr57FanoutPoseRows, _pr57FanoutReservationRows, _pr57FanoutOtherOnlyRows;
    long _pr57FanoutUnalignedRows, _pr57FanoutTier3Pose, _pr57FanoutTier9Pose;

    void Pr57FanoutBeginSlice()
    {
        if (!Pr57FanoutEnabled) return;
        _pr57FanoutReaders.Clear();
        _pr57FanoutComparedReaders.Clear();
        _pr57FanoutAffectedReaders.Clear();
        _pr57FanoutAlignedRows = _pr57FanoutChangedJsonRows = 0;
        _pr57FanoutPoseRows = _pr57FanoutReservationRows = 0;
        _pr57FanoutOtherOnlyRows = _pr57FanoutUnalignedRows = 0;
        _pr57FanoutTier3Pose = _pr57FanoutTier9Pose = 0;
    }

    static bool Pr57FanoutFieldChanged(
        System.Text.Json.JsonElement oldState,
        System.Text.Json.JsonElement newState, string field)
    {
        var oldFound = oldState.TryGetProperty(field, out var oldValue);
        var newFound = newState.TryGetProperty(field, out var newValue);
        return oldFound != newFound ||
            oldFound && !string.Equals(oldValue.GetRawText(), newValue.GetRawText(),
                StringComparison.Ordinal);
    }

    void Pr57FanoutCompare(
        (int Id, int Kind) reader,
        List<(int Id, string State)> previous,
        List<(int Id, string State)> current, int tier)
    {
        if (!Pr57FanoutEnabled) return;
        _pr57FanoutComparedReaders.Add(reader);
        var aligned = Math.Min(previous.Count, current.Count);
        _pr57FanoutUnalignedRows += Math.Abs(previous.Count - current.Count);
        if (previous.Count != current.Count) _pr57FanoutAffectedReaders.Add(reader);
        for (var i = 0; i < aligned; i++)
        {
            if (previous[i].Id != current[i].Id)
            {
                _pr57FanoutUnalignedRows++;
                _pr57FanoutAffectedReaders.Add(reader);
                continue;
            }
            _pr57FanoutAlignedRows++;
            if (string.Equals(previous[i].State, current[i].State,
                StringComparison.Ordinal)) continue;
            _pr57FanoutChangedJsonRows++;
            using var oldJson = System.Text.Json.JsonDocument.Parse(previous[i].State);
            using var newJson = System.Text.Json.JsonDocument.Parse(current[i].State);
            var oldState = oldJson.RootElement;
            var newState = newJson.RootElement;
            var pose =
                Pr57FanoutFieldChanged(oldState, newState, "Position") ||
                Pr57FanoutFieldChanged(oldState, newState, "Heading") ||
                Pr57FanoutFieldChanged(oldState, newState, "Status");
            var reservation =
                Pr57FanoutFieldChanged(oldState, newState, "IntersectionClaim") ||
                Pr57FanoutFieldChanged(oldState, newState, "IntersectionEnteredAt") ||
                Pr57FanoutFieldChanged(oldState, newState, "ManoeuvreSpaces") ||
                Pr57FanoutFieldChanged(oldState, newState, "ManoeuvreDock") ||
                Pr57FanoutFieldChanged(oldState, newState, "DockCorridor") ||
                Pr57FanoutFieldChanged(oldState, newState, "ManoeuvreDeparture") ||
                Pr57FanoutFieldChanged(oldState, newState, "MovementAdmission");
            if (pose)
            {
                _pr57FanoutPoseRows++;
                if (tier == 3) _pr57FanoutTier3Pose++;
                else _pr57FanoutTier9Pose++;
            }
            if (reservation) _pr57FanoutReservationRows++;
            if (!pose && !reservation) _pr57FanoutOtherOnlyRows++;
            if (!pose && !reservation) continue;
            _pr57FanoutAffectedReaders.Add(reader);
            if (!_pr57FanoutReaders.TryGetValue(previous[i].Id, out var readers))
                _pr57FanoutReaders[previous[i].Id] = readers = new();
            readers.Add(reader); // dedupe tier 3/9 and repeated occurrences
        }
    }

    void Pr57FanoutEndSlice()
    {
        if (!Pr57FanoutEnabled) return;
        var edges = _pr57FanoutReaders.Values.Sum(readers => readers.Count);
        var top = _pr57FanoutReaders
            .OrderByDescending(pair => pair.Value.Count)
            .ThenBy(pair => pair.Key)
            .FirstOrDefault();
        Console.WriteLine(
            $"PR57_216_FANOUT sample={_pr57ExclusiveSamples} " +
            $"comparedReaders={_pr57FanoutComparedReaders.Count} " +
            $"affectedReaders={_pr57FanoutAffectedReaders.Count} " +
            $"alignedRows={_pr57FanoutAlignedRows} changedJsonRows={_pr57FanoutChangedJsonRows} " +
            $"poseRows={_pr57FanoutPoseRows} reservationRows={_pr57FanoutReservationRows} " +
            $"otherOnlyRows={_pr57FanoutOtherOnlyRows} unalignedRows={_pr57FanoutUnalignedRows} " +
            $"tier3Pose={_pr57FanoutTier3Pose} tier9Pose={_pr57FanoutTier9Pose} " +
            $"changedCandidateIds={_pr57FanoutReaders.Count} " +
            $"distinctCandidateReaderEdges={edges} maxObservedFanout={top.Value?.Count ?? 0} " +
            $"maxFanoutCandidateId={top.Key} " +
            "scope=selected48PairsTier3Tier9 observational=True intrusive=True " +
            "actualWritersMeasured=False causalClosureProven=False");
    }
}
